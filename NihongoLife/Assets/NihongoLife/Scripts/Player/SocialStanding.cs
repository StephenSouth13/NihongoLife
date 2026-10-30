using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Data;
using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>Sims-style feeling. Gameplay only nudges XP, dialogue tone and NPC reactions.</summary>
    public enum SocialMood
    {
        Neutral = 0,
        Happy = 1,
        Embarrassed = 2,
        Confident = 3,
        Tense = 4,
        Tired = 5,
        Inspired = 6,
        Proud = 7,
        Annoyed = 8
    }

    [Serializable]
    public class NpcAffinityRecord
    {
        public string npcId;
        public int affinity;
    }

    [Serializable]
    public class MoodletRecord
    {
        public string id;
        public int mood;
        public float strength;
        public float hoursLeft;
    }

    /// <summary>
    /// Social standing for the player: per-NPC affinity, reputation (danh tiếng / 名声) and notoriety
    /// (tai tiếng / 悪名), plus moodlets that decay in in-game hours. Works offline for a guest —
    /// nothing here checks account level or <see cref="OnlineAccess"/>. Saved on <see cref="PlayerProgressDto"/>
    /// and merged explicitly so JsonUtility does not drop the fields.
    /// </summary>
    public static class SocialStanding
    {
        public const int MaxStat = 100;

        private static readonly List<MoodletRecord> Moods = new();
        private static readonly Dictionary<string, int> Affinity = new();
        private static readonly Dictionary<string, float> NextByReason = new();
        private static bool _loaded;

        public static int Reputation { get; private set; }
        public static int Notoriety { get; private set; }

        public static SocialMood Dominant
        {
            get
            {
                SocialMood mood = SocialMood.Neutral;
                float best = 0f;
                for (int i = 0; i < Moods.Count; i++)
                {
                    if (Moods[i] == null || Moods[i].hoursLeft <= 0f || Moods[i].strength < best) continue;
                    best = Moods[i].strength;
                    mood = (SocialMood)Moods[i].mood;
                }
                return mood;
            }
        }

        /// <summary>Happy / confident / inspired / proud earn a little more XP. Tired and tense earn a little less.</summary>
        public static float XpMultiplier
        {
            get
            {
                switch (Dominant)
                {
                    case SocialMood.Happy:
                    case SocialMood.Confident:
                    case SocialMood.Inspired:
                    case SocialMood.Proud:
                        return 1.08f;
                    case SocialMood.Tired:
                        return 0.92f;
                    case SocialMood.Tense:
                    case SocialMood.Annoyed:
                        return 0.95f;
                    default:
                        return 1f;
                }
            }
        }

        public static int ScaleXp(int amount)
        {
            if (amount <= 0) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(amount * XpMultiplier));
        }

        public static int AffinityOf(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return 0;
            return Affinity.TryGetValue(npcId, out int value) ? value : 0;
        }

        public static void ResetForTests()
        {
            Reputation = 0;
            Notoriety = 0;
            Moods.Clear();
            Affinity.Clear();
            NextByReason.Clear();
            _loaded = true;
        }

        public static void Load(PlayerProgressDto progress)
        {
            if (progress == null) return;
            Reputation = Mathf.Clamp(progress.reputation, 0, MaxStat);
            Notoriety = Mathf.Clamp(progress.notoriety, 0, MaxStat);
            Affinity.Clear();
            if (progress.affinities != null)
            {
                for (int i = 0; i < progress.affinities.Count; i++)
                {
                    var row = progress.affinities[i];
                    if (row == null || string.IsNullOrEmpty(row.npcId)) continue;
                    Affinity[row.npcId] = Mathf.Clamp(row.affinity, -MaxStat, MaxStat);
                }
            }
            Moods.Clear();
            if (progress.moodlets != null)
            {
                for (int i = 0; i < progress.moodlets.Count; i++)
                {
                    var row = progress.moodlets[i];
                    if (row == null || string.IsNullOrEmpty(row.id) || row.hoursLeft <= 0f) continue;
                    Moods.Add(Copy(row));
                }
            }
            _loaded = true;
        }

        public static void WriteTo(PlayerProgressDto progress)
        {
            if (!_loaded || progress == null) return;
            progress.reputation = Reputation;
            progress.notoriety = Notoriety;
            progress.affinities = new List<NpcAffinityRecord>(Affinity.Count);
            foreach (var pair in Affinity)
                progress.affinities.Add(new NpcAffinityRecord { npcId = pair.Key, affinity = pair.Value });
            progress.moodlets = new List<MoodletRecord>(Moods.Count);
            for (int i = 0; i < Moods.Count; i++)
                if (Moods[i] != null && Moods[i].hoursLeft > 0f) progress.moodlets.Add(Copy(Moods[i]));
        }

        /// <summary>One in-game hour passes per real minute (the 24-minute day). Moodlets tick down; standing does not.</summary>
        public static void AdvanceHours(float hours)
        {
            if (hours <= 0f || Moods.Count == 0) return;
            bool removed = false;
            for (int i = Moods.Count - 1; i >= 0; i--)
            {
                if (Moods[i] == null) { Moods.RemoveAt(i); removed = true; continue; }
                Moods[i].hoursLeft -= hours;
                if (Moods[i].hoursLeft <= 0f) { Moods.RemoveAt(i); removed = true; }
            }
            if (removed) _loaded = true;
        }

        /// <summary>Hunger, thirst, sleep, stamina and health keep a moodlet alive while the need is low, and drop it when it recovers.</summary>
        public static void ReflectNeeds(float hunger, float thirst, float sleepiness, float energy, float health01)
        {
            SetNeed("tired", sleepiness >= 60f || energy <= 25f, SocialMood.Tired, 0.75f);
            SetNeed("hungry", hunger < 30f || thirst < 30f, SocialMood.Tense, 0.6f);
            SetNeed("hurt", health01 < 0.35f, SocialMood.Tense, 0.7f);
        }

        public static void OnRested()
        {
            Remove("tired");
            Remove("hungry");
            AddMoodlet("rested", SocialMood.Happy, 0.55f, 1.5f);
        }

        public static void AddReputation(int amount, string reason = null)
        {
            if (amount <= 0) return;
            int before = Reputation;
            Reputation = Mathf.Clamp(Reputation + amount, 0, MaxStat);
            _loaded = true;
            if (Reputation > before && amount >= 3) Toast(Loc.F("Danh tiếng +{0}", "Reputation +{0}", "名声 +{0}", Reputation - before), "reputation");
        }

        public static void AddNotoriety(int amount, string reason = null, float cooldownSeconds = 0f)
        {
            if (amount <= 0) return;
            if (cooldownSeconds > 0f && !string.IsNullOrEmpty(reason))
            {
                float now = Time.unscaledTime;
                if (NextByReason.TryGetValue(reason, out float until) && now < until) return;
                NextByReason[reason] = now + cooldownSeconds;
            }
            int before = Notoriety;
            Notoriety = Mathf.Clamp(Notoriety + amount, 0, MaxStat);
            _loaded = true;
            if (Notoriety > before)
                Toast(Loc.F("Tai tiếng +{0}", "Notoriety +{0}", "悪名 +{0}", Notoriety - before), "notoriety");
        }

        /// <summary>Redemption: notoriety fades through good shifts, apologies and kind acts. Never below 0.</summary>
        public static int ReduceNotoriety(int amount, string reason = null, float cooldownSeconds = 0f)
        {
            if (amount <= 0 || Notoriety <= 0) return 0;
            if (cooldownSeconds > 0f && !string.IsNullOrEmpty(reason))
            {
                float now = Time.unscaledTime;
                string key = "-" + reason;
                if (NextByReason.TryGetValue(key, out float until) && now < until) return 0;
                NextByReason[key] = now + cooldownSeconds;
            }
            int before = Notoriety;
            Notoriety = Mathf.Clamp(Notoriety - amount, 0, MaxStat);
            _loaded = true;
            int dropped = before - Notoriety;
            if (dropped >= 2) Toast(Loc.F("Tai tiếng -{0}", "Notoriety -{0}", "悪評 -{0}", dropped), "notoriety");
            return dropped;
        }

        public static void AddAffinity(string npcId, int delta)
        {
            if (string.IsNullOrEmpty(npcId) || delta == 0) return;
            Affinity.TryGetValue(npcId, out int value);
            Affinity[npcId] = Mathf.Clamp(value + delta, -MaxStat, MaxStat);
            _loaded = true;
        }

        public static void AddMoodlet(string id, SocialMood mood, float strength, float hours)
        {
            if (string.IsNullOrEmpty(id) || hours <= 0f) return;
            strength = Mathf.Clamp(strength, 0.05f, 1f);
            for (int i = 0; i < Moods.Count; i++)
            {
                if (Moods[i] == null || Moods[i].id != id) continue;
                Moods[i].mood = (int)mood;
                Moods[i].strength = Mathf.Max(Moods[i].strength, strength);
                Moods[i].hoursLeft = Mathf.Max(Moods[i].hoursLeft, hours);
                _loaded = true;
                return;
            }
            Moods.Add(new MoodletRecord { id = id, mood = (int)mood, strength = strength, hoursLeft = hours });
            _loaded = true;
        }

        public static void Remove(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = Moods.Count - 1; i >= 0; i--)
            {
                if (Moods[i] != null && Moods[i].id == id) Moods.RemoveAt(i);
            }
        }

        /// <summary>A polite (positive) answer raises reputation and affinity. A rude or negative one raises notoriety.</summary>
        public static void NoteAnswer(string npcId, int score, string reason)
        {
            bool rude = score < 0 || IsRude(reason);
            if (rude)
            {
                AddNotoriety(4, "rude-answer", 2f);
                AddAffinity(npcId, -6);
                AddMoodlet("slipped", SocialMood.Embarrassed, 0.7f, 1.2f);
                return;
            }
            if (score <= 0) return;
            int rep = Dominant == SocialMood.Confident || Dominant == SocialMood.Inspired ? 3 : 2;
            AddReputation(rep, "polite");
            int affinity = Dominant == SocialMood.Happy || Dominant == SocialMood.Proud ? 5 : 3;
            AddAffinity(string.IsNullOrEmpty(npcId) ? "stranger" : npcId, affinity);
            AddMoodlet("polite", SocialMood.Confident, 0.45f, 1f);
        }

        /// <summary>One short line when a conversation opens. Null when nothing about mood or standing would change the exchange.</summary>
        public static string DialogueHint(string npcId)
        {
            int affinity = AffinityOf(npcId);
            if (Notoriety >= 45 && affinity < 15)
                return Loc.T("Họ hơi dè chừng vì tai tiếng của bạn.", "They're a little wary of your notoriety.", "たいせいが あるので すこし けいかいされています。");
            if (affinity >= 35)
                return Loc.T("Họ vui vì bạn là người quen.", "They're glad — they know you.", "なじみなので うれしそうです。");
            switch (Dominant)
            {
                case SocialMood.Embarrassed:
                    return Loc.T("Đang ngượng: câu lịch sự sẽ đỡ hơn.", "Embarrassed: a polite answer goes over better.", "はずかしい きぶん — ていねいな へんじが いいです。");
                case SocialMood.Confident:
                case SocialMood.Inspired:
                    return Loc.T("Đang tự tin: câu đúng được thêm chút danh tiếng.", "Confident: a correct answer earns a little extra reputation.", "じしんが ある — ただしい こたえは めいせいが すこし ふえます。");
                case SocialMood.Tired:
                    return Loc.T("Đang mệt: kinh nghiệm nhận được giảm nhẹ.", "Tired: experience gain is a little lower.", "つかれている — けいけんちが すこし さがります。");
                case SocialMood.Tense:
                case SocialMood.Annoyed:
                    return Loc.T("Đang căng: người đối diện dễ khó chịu.", "Tense: the other person is easier to rub the wrong way.", "きんちょう している — あいてが イライラしやすいです。");
                default:
                    return null;
            }
        }

        public static string MoodLabel(SocialMood mood) => mood switch
        {
            SocialMood.Happy => Loc.T("vui", "happy", "うれしい"),
            SocialMood.Embarrassed => Loc.T("ngượng", "embarrassed", "はずかしい"),
            SocialMood.Confident => Loc.T("tự tin", "confident", "じしん"),
            SocialMood.Tense => Loc.T("căng", "tense", "きんちょう"),
            SocialMood.Tired => Loc.T("mệt", "tired", "つかれた"),
            SocialMood.Inspired => Loc.T("hứng", "inspired", "ひらめき"),
            SocialMood.Proud => Loc.T("tự hào", "proud", "じまん"),
            SocialMood.Annoyed => Loc.T("bực", "annoyed", "いらいら"),
            _ => Loc.T("bình thường", "calm", "ふつう")
        };

        public static string HudLine =>
            Loc.F("{0}  ·  danh tiếng {1}  ·  tai tiếng {2}", "{0}  ·  reputation {1}  ·  notoriety {2}", "{0}  ·  名声 {1}  ·  悪名 {2}", MoodLabel(Dominant), Reputation, Notoriety);

        public static string ProfileBlock =>
            Loc.F("Tâm trạng: {0}\nDanh tiếng {1}  ·  Tai tiếng {2}", "Mood: {0}\nReputation {1}  ·  Notoriety {2}", "気分: {0}\n名声 {1}  ·  悪名 {2}", MoodLabel(Dominant), Reputation, Notoriety);

        public static string ProfileBlockFrom(PlayerProgressDto progress)
        {
            if (progress == null) return "";
            var mood = SocialMood.Neutral;
            float best = 0f;
            if (progress.moodlets != null)
            {
                for (int i = 0; i < progress.moodlets.Count; i++)
                {
                    var row = progress.moodlets[i];
                    if (row == null || row.hoursLeft <= 0f || row.strength < best) continue;
                    best = row.strength;
                    mood = (SocialMood)row.mood;
                }
            }
            return Loc.F("Tâm trạng: {0}\nDanh tiếng {1}  ·  Tai tiếng {2}", "Mood: {0}\nReputation {1}  ·  Notoriety {2}", "気分: {0}\n名声 {1}  ·  悪名 {2}", MoodLabel(mood), Mathf.Clamp(progress.reputation, 0, MaxStat), Mathf.Clamp(progress.notoriety, 0, MaxStat));
        }

        public static void Merge(PlayerProgressDto merged, PlayerProgressDto local, PlayerProgressDto cloud)
        {
            if (merged == null || local == null || cloud == null) return;
            merged.reputation = Mathf.Clamp(Mathf.Max(local.reputation, cloud.reputation), 0, MaxStat);
            merged.notoriety = Mathf.Clamp(Mathf.Max(local.notoriety, cloud.notoriety), 0, MaxStat);
            merged.affinities = MergeAffinity(local.affinities, cloud.affinities);
            bool anyCalendar = local.calendarStarted || cloud.calendarStarted;
            bool localAhead = local.calendarStarted && (!cloud.calendarStarted || local.calendarDay > cloud.calendarDay);
            merged.moodlets = anyCalendar
                ? CopyList(localAhead ? local.moodlets : cloud.moodlets)
                : MergeMoods(local.moodlets, cloud.moodlets);
        }

        private static void SetNeed(string id, bool on, SocialMood mood, float strength)
        {
            if (on) AddMoodlet(id, mood, strength, 0.45f);
            else Remove(id);
        }

        private static bool IsRude(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return false;
            string r = reason.ToLowerInvariant();
            return r.Contains("rude") || r.Contains("impolite") || r.Contains("失礼") || r.Contains("thô") || r.Contains("lỗ mãng");
        }

        private static void Toast(string text, string key)
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(text)) return;
            UI.HudFeed.Post(text, UI.HudFeed.Kind.Info, 3.5f, key);
        }

        private static List<NpcAffinityRecord> MergeAffinity(List<NpcAffinityRecord> local, List<NpcAffinityRecord> cloud)
        {
            var map = new Dictionary<string, int>();
            void Take(List<NpcAffinityRecord> rows)
            {
                if (rows == null) return;
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row == null || string.IsNullOrEmpty(row.npcId)) continue;
                    int value = Mathf.Clamp(row.affinity, -MaxStat, MaxStat);
                    if (!map.TryGetValue(row.npcId, out int have) || value > have) map[row.npcId] = value;
                }
            }
            Take(local);
            Take(cloud);
            var list = new List<NpcAffinityRecord>(map.Count);
            foreach (var pair in map) list.Add(new NpcAffinityRecord { npcId = pair.Key, affinity = pair.Value });
            return list;
        }

        private static List<MoodletRecord> MergeMoods(List<MoodletRecord> local, List<MoodletRecord> cloud)
        {
            var map = new Dictionary<string, MoodletRecord>();
            void Take(List<MoodletRecord> rows)
            {
                if (rows == null) return;
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row == null || string.IsNullOrEmpty(row.id) || row.hoursLeft <= 0f) continue;
                    if (!map.TryGetValue(row.id, out var have) || row.hoursLeft > have.hoursLeft) map[row.id] = Copy(row);
                }
            }
            Take(local);
            Take(cloud);
            return new List<MoodletRecord>(map.Values);
        }

        private static List<MoodletRecord> CopyList(List<MoodletRecord> rows)
        {
            var list = new List<MoodletRecord>();
            if (rows == null) return list;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i] != null && rows[i].hoursLeft > 0f) list.Add(Copy(rows[i]));
            return list;
        }

        private static MoodletRecord Copy(MoodletRecord row) => new MoodletRecord
        {
            id = row.id,
            mood = row.mood,
            strength = row.strength,
            hoursLeft = row.hoursLeft
        };
    }
}
