using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NihongoLife.Progression
{
    // ─────────── Definitions (read-only data: Resources/Progression/progression.json) ───────────

    [Serializable] public sealed class LevelTable { public int[] xpToNext; }
    [Serializable] public sealed class QuestItemGrant { public string itemId; public int quantity; }
    [Serializable] public sealed class QuestRewards { public int yen, xp, knowledge; }

    [Serializable]
    public sealed class QuestObjective
    {
        public string id;
        /// <summary>Gameplay event that advances it (see ProgressionCatalog.SupportedEvents).</summary>
        public string @event;
        /// <summary>Event target id, or "*" for any target.</summary>
        public string target;
        public int count = 1;
        /// <summary>Counts only once every other objective is complete (e.g. "report to the manager").</summary>
        public bool afterAll;
        public string textVi, textEn;
    }

    [Serializable]
    public sealed class QuestDefinition
    {
        public string id, category;
        public string titleVi, titleEn, titleJa, descriptionVi, descriptionEn;
        public string giverNpc, giverNameVi, locationVi, scene;
        public int minLevel = 1, minKnowledge;
        public string[] requiresQuests = Array.Empty<string>();
        public bool repeatable;
        public int cooldownMinutes;
        public QuestItemGrant[] grantOnAccept = Array.Empty<QuestItemGrant>();
        public QuestObjective[] objectives = Array.Empty<QuestObjective>();
        public QuestRewards rewards = new QuestRewards();

        public bool IsJob => category == "job";
    }

    /// <summary>
    /// The single editable source for missions, jobs, rewards and level thresholds. Definitions are never written to
    /// at runtime: per-player state lives in PlayerProgressDto.quests (QuestStateRecord). Edit the JSON, run the
    /// EditMode test ProgressionCatalogTests, and the journal / HUD / rewards pick the change up automatically.
    /// </summary>
    [Serializable]
    public sealed class ProgressionCatalog
    {
        public const string ResourcePath = "Progression/progression";
        public static readonly string[] Categories = { "story", "job", "farm", "learning", "daily" };
        public static readonly string[] SupportedEvents =
        {
            "till", "plant", "water", "harvest", "feed", "pet", "buy", "sell", "talk", "learn",
            "restock", "assist", "checkout", "order", "serve", "fish",
        };

        public string schema;
        public LevelTable levels = new LevelTable();
        public QuestDefinition[] quests = Array.Empty<QuestDefinition>();

        private static ProgressionCatalog _cached;

        public static ProgressionCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>(ResourcePath);
            _cached = text != null ? Parse(text.text) : new ProgressionCatalog();
            return _cached;
        }

        public static ProgressionCatalog Parse(string json) => JsonUtility.FromJson<ProgressionCatalog>(json) ?? new ProgressionCatalog();

        public QuestDefinition Quest(string id) => quests?.FirstOrDefault(q => q.id == id);

        // ── Levels: progress.xp is the total XP ever earned; the level is derived from it. ──

        public int MaxLevel => (levels?.xpToNext?.Length ?? 0) + 1;

        /// <summary>Total XP needed to reach <paramref name="level"/> (level 1 = 0).</summary>
        public int XpForLevel(int level)
        {
            var steps = levels?.xpToNext ?? Array.Empty<int>();
            int total = 0;
            for (int i = 0; i < Mathf.Min(level - 1, steps.Length); i++) total += steps[i];
            if (level - 1 > steps.Length && steps.Length > 0) total += (level - 1 - steps.Length) * steps[steps.Length - 1];
            return total;
        }

        public int LevelFor(int totalXp)
        {
            int level = 1;
            while (level < 99 && totalXp >= XpForLevel(level + 1)) level++;
            return level;
        }

        // ── Validation (EditMode test + runtime warning) ──

        public List<string> Validate(Func<string, bool> itemExists = null)
        {
            var errors = new List<string>();
            if (schema != "nihongolife.progression.v1") errors.Add($"schema must be nihongolife.progression.v1 (was '{schema}')");
            var steps = levels?.xpToNext ?? Array.Empty<int>();
            if (steps.Length == 0) errors.Add("levels.xpToNext is empty");
            for (int i = 0; i < steps.Length; i++)
                if (steps[i] <= 0) errors.Add($"levels.xpToNext[{i}] must be positive");

            var ids = new HashSet<string>();
            foreach (var q in quests ?? Array.Empty<QuestDefinition>())
            {
                string where = $"quest '{q.id}'";
                if (string.IsNullOrWhiteSpace(q.id)) { errors.Add("a quest has no id"); continue; }
                if (!ids.Add(q.id)) errors.Add($"duplicate quest id '{q.id}'");
                if (!Categories.Contains(q.category)) errors.Add($"{where}: unknown category '{q.category}'");
                if (string.IsNullOrWhiteSpace(q.titleVi) || string.IsNullOrWhiteSpace(q.titleEn)) errors.Add($"{where}: missing Vietnamese or English title");
                if (string.IsNullOrWhiteSpace(q.descriptionVi)) errors.Add($"{where}: missing Vietnamese description");
                if (q.minLevel < 1) errors.Add($"{where}: minLevel must be ≥ 1");
                if (q.minKnowledge < 0) errors.Add($"{where}: minKnowledge must be ≥ 0");
                if (q.cooldownMinutes < 0) errors.Add($"{where}: cooldownMinutes must be ≥ 0");
                if (q.rewards == null || q.rewards.yen < 0 || q.rewards.xp < 0 || q.rewards.knowledge < 0) errors.Add($"{where}: rewards must be present and non-negative");
                if (q.objectives == null || q.objectives.Length == 0) errors.Add($"{where}: no objectives");
                var objectiveIds = new HashSet<string>();
                foreach (var o in q.objectives ?? Array.Empty<QuestObjective>())
                {
                    if (string.IsNullOrWhiteSpace(o.id) || !objectiveIds.Add(o.id)) errors.Add($"{where}: missing or duplicate objective id '{o.id}'");
                    if (!SupportedEvents.Contains(o.@event)) errors.Add($"{where}/{o.id}: unsupported event '{o.@event}'");
                    if (string.IsNullOrWhiteSpace(o.target)) errors.Add($"{where}/{o.id}: target is empty (use \"*\" for any)");
                    if (o.count < 1) errors.Add($"{where}/{o.id}: count must be ≥ 1");
                    if (string.IsNullOrWhiteSpace(o.textVi) || string.IsNullOrWhiteSpace(o.textEn)) errors.Add($"{where}/{o.id}: missing Vietnamese or English text");
                }
                if ((q.objectives ?? Array.Empty<QuestObjective>()).All(o => o.afterAll)) errors.Add($"{where}: every objective is afterAll — it could never start");
                foreach (var g in q.grantOnAccept ?? Array.Empty<QuestItemGrant>())
                {
                    if (g.quantity < 1) errors.Add($"{where}: grantOnAccept '{g.itemId}' quantity must be ≥ 1");
                    if (itemExists != null && !itemExists(g.itemId)) errors.Add($"{where}: grantOnAccept item '{g.itemId}' is not a known item");
                }
            }
            foreach (var q in quests ?? Array.Empty<QuestDefinition>())
                foreach (var r in q.requiresQuests ?? Array.Empty<string>())
                    if (!ids.Contains(r)) errors.Add($"quest '{q.id}': requires unknown quest '{r}'");
            foreach (var q in quests ?? Array.Empty<QuestDefinition>())
                if (HasCycle(q.id, new HashSet<string>())) errors.Add($"quest '{q.id}': circular requiresQuests");
            return errors;
        }

        private bool HasCycle(string id, HashSet<string> path)
        {
            if (!path.Add(id)) return true;
            var q = Quest(id);
            foreach (var r in q?.requiresQuests ?? Array.Empty<string>())
                if (HasCycle(r, new HashSet<string>(path))) return true;
            return false;
        }
    }
}
