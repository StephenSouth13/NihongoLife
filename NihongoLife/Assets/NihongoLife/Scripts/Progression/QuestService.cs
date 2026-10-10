using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Progression
{
    /// <summary>Per-player state of one quest (saved in PlayerProgressDto.quests; definitions stay read-only).</summary>
    [Serializable]
    public sealed class QuestStateRecord
    {
        public string questId;
        /// <summary>"active" or "completed" (a cancelled quest simply has no "active" state any more).</summary>
        public string status;
        public List<int> progress = new();
        public long acceptedTicks, completedTicks, cooldownUntilTicks;
        public int timesCompleted;
        /// <summary>Reward of the current run already paid — the guard that makes payment exactly-once.</summary>
        public bool rewardClaimed;
    }

    public enum QuestAvailability { Locked, Available, Active, Cooldown, Completed }

    /// <summary>
    /// Runtime quest/job service. Gameplay code only reports what happened — QuestService.Raise("water", "carrot") —
    /// and this service advances matching objectives of active quests, completes them and pays the reward once
    /// (money to the one shared wallet, XP to the level, knowledge separately). Accepting, cancelling, tracking and
    /// cooldowns live here too, so a new task needs only a JSON entry and, if it uses a new kind of action, one Raise call.
    /// </summary>
    public static class QuestService
    {
        public static event Action Changed;
        public static event Action<QuestDefinition> Completed;
        /// <summary>Test hook: shifts "now" for cooldowns.</summary>
        public static TimeSpan ClockOffset = TimeSpan.Zero;
        private static DateTime Now => DateTime.UtcNow + ClockOffset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Changed = null; Completed = null; ClockOffset = TimeSpan.Zero;
            HudObjective.Provider = TrackedHud;
        }

        private static PlayerProgressDto Progress =>
            GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;

        private static void Save()
        {
            if (GameServices.TryGet(out IProgressRepository repository)) repository.SaveProgress(repository.GetProgress());
        }

        public static ProgressionCatalog Catalog => ProgressionCatalog.Load();

        public static QuestStateRecord State(string questId)
        {
            var progress = Progress;
            return progress?.quests?.Find(s => s.questId == questId);
        }

        public static IEnumerable<QuestDefinition> Active =>
            Catalog.quests.Where(q => State(q.id)?.status == "active");

        public static QuestAvailability Availability(QuestDefinition quest)
        {
            var state = State(quest.id);
            if (state?.status == "active") return QuestAvailability.Active;
            if (state?.status == "completed")
            {
                if (!quest.repeatable) return QuestAvailability.Completed;
                if (Now.Ticks < state.cooldownUntilTicks) return QuestAvailability.Cooldown;
            }
            return Requirement(quest) == null ? QuestAvailability.Available : QuestAvailability.Locked;
        }

        /// <summary>Why the quest cannot be accepted yet (null when it can).</summary>
        public static string Requirement(QuestDefinition quest)
        {
            var status = PlayerStatus.Instance;
            int level = status != null ? status.Level : Catalog.LevelFor(Progress?.xp ?? 0);
            int knowledge = status != null ? status.Knowledge : Progress?.knowledge ?? 0;
            if (level < quest.minLevel) return $"Cần cấp {quest.minLevel} (bạn đang cấp {level}).";
            if (knowledge < quest.minKnowledge) return $"Cần {quest.minKnowledge} điểm kiến thức (bạn có {knowledge}).";
            foreach (var required in quest.requiresQuests ?? Array.Empty<string>())
                if ((State(required)?.timesCompleted ?? 0) == 0) return $"Cần hoàn thành \"{Catalog.Quest(required)?.titleVi ?? required}\" trước.";
            return null;
        }

        public static TimeSpan CooldownLeft(QuestDefinition quest)
        {
            var state = State(quest.id);
            if (state == null) return TimeSpan.Zero;
            long left = state.cooldownUntilTicks - Now.Ticks;
            return left > 0 ? TimeSpan.FromTicks(left) : TimeSpan.Zero;
        }

        public static string Accept(string questId)
        {
            var quest = Catalog.Quest(questId);
            var progress = Progress;
            if (quest == null || progress == null) return "Không tìm thấy nhiệm vụ.";
            switch (Availability(quest))
            {
                case QuestAvailability.Active: return "Bạn đang làm việc này rồi.";
                case QuestAvailability.Completed: return "Việc này đã hoàn thành.";
                case QuestAvailability.Cooldown: return $"Ca tiếp theo mở sau {Mathf.CeilToInt((float)CooldownLeft(quest).TotalMinutes)} phút.";
                case QuestAvailability.Locked: return Requirement(quest);
            }
            if (quest.IsJob && Active.Any(q => q.IsJob)) return "Mỗi lúc chỉ làm một ca — hoàn thành hoặc huỷ ca đang làm trước.";
            var inventory = PlayerInventory.Instance;
            foreach (var grant in quest.grantOnAccept ?? Array.Empty<QuestItemGrant>())
                if (inventory == null || (!inventory.HasItem(grant.itemId) && inventory.IsFull)) return "Balo đầy — cần chỗ để nhận đồ nghề.";

            progress.quests ??= new List<QuestStateRecord>();
            var state = progress.quests.Find(s => s.questId == quest.id);
            if (state == null) { state = new QuestStateRecord { questId = quest.id }; progress.quests.Add(state); }
            state.status = "active";
            state.progress = quest.objectives.Select(_ => 0).ToList();
            state.acceptedTicks = Now.Ticks;
            state.rewardClaimed = false;
            if (string.IsNullOrEmpty(progress.trackedQuestId) || Catalog.Quest(progress.trackedQuestId) == null || State(progress.trackedQuestId)?.status != "active")
                progress.trackedQuestId = quest.id;
            Save();
            foreach (var grant in quest.grantOnAccept ?? Array.Empty<QuestItemGrant>()) GiveItem(grant.itemId, grant.quantity);
            HudFeed.Post($"Đã nhận: {quest.titleVi}", HudFeed.Kind.Info);
            Changed?.Invoke();
            return null;
        }

        /// <summary>Cancels an active quest: progress is dropped and no reward is paid. Items handed out stay with the player.</summary>
        public static bool Abandon(string questId)
        {
            var progress = Progress;
            var state = State(questId);
            if (progress == null || state == null || state.status != "active") return false;
            progress.quests.Remove(state);
            if (state.timesCompleted > 0)
                progress.quests.Add(new QuestStateRecord { questId = questId, status = "completed", timesCompleted = state.timesCompleted, completedTicks = state.completedTicks, cooldownUntilTicks = state.cooldownUntilTicks, rewardClaimed = true });
            if (progress.trackedQuestId == questId) progress.trackedQuestId = Active.FirstOrDefault()?.id ?? string.Empty;
            Save();
            HudFeed.Post($"Đã huỷ: {Catalog.Quest(questId)?.titleVi}", HudFeed.Kind.Warning);
            Changed?.Invoke();
            return true;
        }

        public static void Track(string questId)
        {
            var progress = Progress;
            if (progress == null) return;
            progress.trackedQuestId = State(questId)?.status == "active" ? questId : string.Empty;
            Save();
            Changed?.Invoke();
        }

        public static string TrackedId => Progress?.trackedQuestId;

        /// <summary>Reports a gameplay event. Safe to call from anywhere, any number of times.</summary>
        public static void Raise(string evt, string target, int amount = 1)
        {
            if (amount <= 0 || string.IsNullOrEmpty(evt)) return;
            var progress = Progress;
            if (progress?.quests == null) return;
            bool changed = false;
            foreach (var quest in Catalog.quests)
            {
                var state = progress.quests.Find(s => s.questId == quest.id);
                if (state == null || state.status != "active") continue;
                if (state.progress == null || state.progress.Count != quest.objectives.Length) state.progress = quest.objectives.Select(_ => 0).ToList();
                for (int i = 0; i < quest.objectives.Length; i++)
                {
                    var o = quest.objectives[i];
                    if (o.@event != evt || state.progress[i] >= o.count) continue;
                    if (o.target != "*" && !string.Equals(o.target, target, StringComparison.OrdinalIgnoreCase)) continue;
                    if (o.afterAll && !OthersDone(quest, state, i)) continue;
                    state.progress[i] = Mathf.Min(o.count, state.progress[i] + amount);
                    changed = true;
                    if (!o.afterAll) HudFeed.Post($"{o.textVi}  {state.progress[i]}/{o.count}", HudFeed.Kind.Info, 3.2f, quest.id + "/" + o.id);
                }
                if (changed && IsComplete(quest, state)) Complete(quest, state);
            }
            if (!changed) return;
            Save();
            Changed?.Invoke();
        }

        /// <summary>The objective that a given talk/report would complete is waiting only on the others.</summary>
        public static bool ReadyToReport(string questId)
        {
            var quest = Catalog.Quest(questId);
            var state = State(questId);
            if (quest == null || state == null || state.status != "active") return false;
            for (int i = 0; i < quest.objectives.Length; i++)
                if (quest.objectives[i].afterAll && state.progress[i] < quest.objectives[i].count && OthersDone(quest, state, i)) return true;
            return false;
        }

        public static int ObjectiveProgress(string questId, string objectiveId)
        {
            var quest = Catalog.Quest(questId);
            var state = State(questId);
            if (quest == null || state?.progress == null) return 0;
            int index = Array.FindIndex(quest.objectives, o => o.id == objectiveId);
            return index >= 0 && index < state.progress.Count ? state.progress[index] : 0;
        }

        public static bool ObjectiveOpen(string questId, string objectiveId)
        {
            var quest = Catalog.Quest(questId);
            var o = quest?.objectives.FirstOrDefault(x => x.id == objectiveId);
            return o != null && State(questId)?.status == "active" && ObjectiveProgress(questId, objectiveId) < o.count;
        }

        private static bool OthersDone(QuestDefinition quest, QuestStateRecord state, int except)
        {
            for (int i = 0; i < quest.objectives.Length; i++)
                if (i != except && state.progress[i] < quest.objectives[i].count) return false;
            return true;
        }

        private static bool IsComplete(QuestDefinition quest, QuestStateRecord state)
        {
            for (int i = 0; i < quest.objectives.Length; i++)
                if (state.progress[i] < quest.objectives[i].count) return false;
            return true;
        }

        private static void Complete(QuestDefinition quest, QuestStateRecord state)
        {
            if (state.rewardClaimed || state.status != "active") return;
            // Mark first and save, then pay: a repeated event, a double click or a reload can never pay twice.
            state.status = "completed";
            state.rewardClaimed = true;
            state.timesCompleted++;
            state.completedTicks = Now.Ticks;
            state.cooldownUntilTicks = Now.AddMinutes(quest.cooldownMinutes).Ticks;
            var progress = Progress;
            if (quest.IsJob)
            {
                progress.careers ??= new List<CareerRecord>();
                var career = progress.careers.Find(c => c.roleId == quest.id);
                if (career == null) { career = new CareerRecord { roleId = quest.id, rank = 1 }; progress.careers.Add(career); }
                career.totalShifts++;
                career.completedShifts++;
                career.reputation += 10;
            }
            if (progress.trackedQuestId == quest.id) progress.trackedQuestId = Active.FirstOrDefault(q => q.id != quest.id)?.id ?? string.Empty;
            Save();

            var rewards = quest.rewards ?? new QuestRewards();
            if (rewards.xp > 0) PlayerStatus.Instance?.AddExp(rewards.xp);
            if (rewards.knowledge > 0) PlayerStatus.Instance?.AddKnowledge(rewards.knowledge);
            if (rewards.yen > 0) PlayerInventory.Instance?.AddYen(rewards.yen);
            if (PlayerStatus.Instance == null)
            {
                progress.xp += rewards.xp;
                progress.level = ProgressionCatalog.Load().LevelFor(progress.xp);
                progress.knowledge += rewards.knowledge;
                if (PlayerInventory.Instance == null) progress.yen += rewards.yen;
            }
            Save();
            string pay = string.Join("  ·  ", new[] { rewards.yen > 0 ? $"+¥{rewards.yen:N0}" : null, rewards.xp > 0 ? $"+{rewards.xp} XP" : null, rewards.knowledge > 0 ? $"+{rewards.knowledge} kiến thức" : null }.Where(x => x != null));
            HudFeed.Post($"Hoàn thành: {quest.titleVi}   {pay}", HudFeed.Kind.Reward, 6f);
            Completed?.Invoke(quest);
        }

        private static void GiveItem(string itemId, int quantity)
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return;
            var island = NihongoLife.Island.IslandCatalog.Load();
            if (island.TryDescribe(itemId, out string ja, out string vi, out int price)) inventory.AddItem(itemId, ja, vi, price, quantity);
            else inventory.AddItem(itemId, itemId, itemId, 0, quantity);
        }

        // ─────────── HUD: the tracked objective chip ───────────

        private static (string kicker, string text, string progress)? TrackedHud()
        {
            var progress = Progress;
            if (progress == null || string.IsNullOrEmpty(progress.trackedQuestId)) return null;
            var quest = Catalog.Quest(progress.trackedQuestId);
            var state = State(progress.trackedQuestId);
            if (quest == null || state?.status != "active") return null;
            if (state.progress == null || state.progress.Count != quest.objectives.Length) return null;
            int next = -1;
            for (int i = 0; i < quest.objectives.Length; i++)
            {
                if (state.progress[i] >= quest.objectives[i].count) continue;
                if (quest.objectives[i].afterAll && !OthersDone(quest, state, i)) continue;
                next = i; break;
            }
            if (next < 0) return null;
            var o = quest.objectives[next];
            int done = quest.objectives.Where((x, i) => state.progress[i] >= x.count).Count();
            string kicker = quest.IsJob ? "◆ LÀM THÊM" : quest.category == "farm" ? "◆ NÔNG TRẠI" : quest.category == "learning" ? "◆ HỌC TẬP" : "◆ NHIỆM VỤ";
            return ($"{kicker} · {quest.titleVi}", $"{o.textVi}  ({state.progress[next]}/{o.count})", $"{done}/{quest.objectives.Length} mục  ·  N mở sổ nhiệm vụ");
        }
    }
}
