using System.Linq;
using NihongoLife.Interaction;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Progression
{
    /// <summary>
    /// An employer the player talks to (e.g. Hana on the Midori farm): offers the job when it is available, says what
    /// is still missing during the shift, and pays when every step is done ("talk" objective → QuestService).
    /// NPCs that already have a dialogue (Ito, Aoki) report through NPCController instead.
    /// </summary>
    public sealed class JobGiver : MonoBehaviour, IInteractable, IInteractionPriority
    {
        [SerializeField] private string npcId = "npc_farm_manager";
        [SerializeField] private string nameJa = "はな";
        [SerializeField] private string nameVi = "Cô Hana";
        [SerializeField] private string nameEn = "Hana";

        private string Name => NihongoLife.Core.Loc.Pick(nameVi, nameEn, nameJa);
        [SerializeField] private string[] questIds = { "job_farm_shift" };

        public void Configure(string id, string ja, string vi, params string[] quests) { npcId = id; nameJa = ja; nameVi = vi; questIds = quests; }

        public float InteractionPriority => 0.6f;
        public string GetPromptJa() => $"{nameJa} · はなす";
        public string GetpromptEn() => questIds.Any(QuestService.ReadyToReport) ? NihongoLife.Core.Loc.F("Báo cáo với {0} — nhận lương", "Report to {0} — get paid", "{0}に報告して給料をもらう", Name) : NihongoLife.Core.Loc.F("Nói chuyện với {0}", "Talk to {0}", "{0}と話す", Name);
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            var catalog = QuestService.Catalog;
            bool reporting = questIds.Any(QuestService.ReadyToReport);
            QuestService.Raise("talk", npcId);
            if (reporting) { HudFeed.Post("「おつかれさまでした！」 — " + NihongoLife.Core.Loc.F("{0} trả lương cho bạn.", "{0} pays you.", "{0}から給料をもらいました。", Name), HudFeed.Kind.Info, 4f); return; }

            foreach (var id in questIds)
            {
                var quest = catalog.Quest(id);
                if (quest == null) continue;
                var availability = QuestService.Availability(quest);
                if (availability == QuestAvailability.Active)
                {
                    var state = QuestService.State(id);
                    var missing = quest.objectives.Where((o, i) => state.progress[i] < o.count && !o.afterAll).Select(o => o.textVi).ToList();
                    HudFeed.Post("「がんばって！」 — " + NihongoLife.Core.Loc.T("Còn: ", "Still to do: ", "残り：") + string.Join(" · ", missing), HudFeed.Kind.Info, 5f);
                    return;
                }
                if (availability == QuestAvailability.Available)
                {
                    TaskJournalUI.Open(id);
                    return;
                }
                if (availability == QuestAvailability.Cooldown)
                {
                    HudFeed.Post("「また あとで ね」 — " + NihongoLife.Core.Loc.F("Ca tiếp theo sau {0} phút.", "Next shift in {0} min.", "次のシフトは{0}分後。", Mathf.CeilToInt((float)QuestService.CooldownLeft(quest).TotalMinutes)), HudFeed.Kind.Info, 4f);
                    return;
                }
            }
            HudFeed.Post($"「こんにちは！」 — {nameVi}", HudFeed.Kind.Info, 3f);
        }
    }
}
