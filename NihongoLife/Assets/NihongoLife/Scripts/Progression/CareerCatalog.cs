using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Progression
{
    // ─────────── Career definitions (read-only data: Resources/Progression/careers.json) ───────────

    [Serializable]
    public sealed class CareerLevelDefinition
    {
        public string titleVi, titleEn, titleJa;
        /// <summary>Salary paid on top of the shift's own reward for a shift worked in working hours.</summary>
        public int salary;
        /// <summary>Good shifts to fill the performance bar (0 = top of the ladder).</summary>
        public int shiftsToPromote;
        /// <summary>Trí tuệ needed before the promotion interview.</summary>
        public int knowledgeToPromote;
        public string Title => Loc.Pick(titleVi, titleEn, titleJa);
    }

    [Serializable]
    public sealed class CareerBranchDefinition
    {
        public string id, nameVi, nameEn, nameJa, descriptionVi, descriptionEn, descriptionJa;
        public CareerLevelDefinition[] levels = Array.Empty<CareerLevelDefinition>();
        public string Name => Loc.Pick(nameVi, nameEn, nameJa);
        public string Description => Loc.Pick(descriptionVi, descriptionEn, descriptionJa);
    }

    [Serializable]
    public sealed class CareerChoiceDefinition
    {
        public string ja, vi, en, hintVi, hintEn;
        public bool correct;
        /// <summary>Interview at the branch level: the branch this answer chooses.</summary>
        public string branch;
        /// <summary>Work events: effects of this answer.</summary>
        public int performance, reputation;
        public string mood;
        public string Gloss => Loc.Pick(vi, en, vi);
        public string Hint => Loc.Pick(hintVi, hintEn, hintVi);
    }

    /// <summary>A promotion interview (toLevel) or a work event: a Japanese line and answers.</summary>
    [Serializable]
    public sealed class CareerPromptDefinition
    {
        public string id;
        public int toLevel;
        /// <summary>Interviews above the branch level belong to one branch.</summary>
        public string branch;
        public string lineJa, readingJa, meaningVi, meaningEn;
        public CareerChoiceDefinition[] choices = Array.Empty<CareerChoiceDefinition>();
        public string Meaning => Loc.Pick(meaningVi, meaningEn, meaningVi);
    }

    [Serializable]
    public sealed class CareerDefinition
    {
        public string id, nameVi, nameEn, nameJa, descriptionVi, descriptionEn, descriptionJa;
        public string workplaceVi, workplaceEn, workplaceJa, bossNameJa, bossNameVi;
        /// <summary>The part-time shift (progression.json) worked in this career.</summary>
        public string shiftQuest;
        public int workStart = 8, workEnd = 14;
        /// <summary>Days of the week with a shift (0 = Sunday … 6 = Saturday).</summary>
        public int[] workDays = { 1, 2, 3, 4, 5 };
        /// <summary>The level whose promotion picks a branch (Sims-style split).</summary>
        public int branchAtLevel = 4;
        public CareerLevelDefinition[] levels = Array.Empty<CareerLevelDefinition>();
        public CareerBranchDefinition[] branches = Array.Empty<CareerBranchDefinition>();
        public CareerPromptDefinition[] interviews = Array.Empty<CareerPromptDefinition>();
        public CareerPromptDefinition[] events = Array.Empty<CareerPromptDefinition>();

        public string Name => Loc.Pick(nameVi, nameEn, nameJa);
        public string Description => Loc.Pick(descriptionVi, descriptionEn, descriptionJa);
        public string Workplace => Loc.Pick(workplaceVi, workplaceEn, workplaceJa);
        public string BossName => Loc.Pick(bossNameVi, bossNameJa, bossNameJa);

        public CareerBranchDefinition Branch(string branchId) => branches.FirstOrDefault(b => b.id == branchId);

        /// <summary>Highest level of the ladder (base levels + one branch).</summary>
        public int MaxLevel => levels.Length + (branches.Length > 0 ? branches.Max(b => b.levels.Length) : 0);

        /// <summary>Level 1-based; levels past the shared ones come from the chosen branch.</summary>
        public CareerLevelDefinition Level(int level, string branchId)
        {
            if (level <= 0) return null;
            if (level <= levels.Length) return levels[level - 1];
            var branch = Branch(branchId) ?? branches.FirstOrDefault();
            int index = level - levels.Length - 1;
            return branch != null && index < branch.levels.Length ? branch.levels[index] : null;
        }

        public CareerPromptDefinition Interview(int toLevel, string branchId) =>
            interviews.FirstOrDefault(i => i.toLevel == toLevel && (string.IsNullOrEmpty(i.branch) || i.branch == branchId));

        public bool IsWorkDay(DayOfWeek day) => workDays.Contains((int)day);

        /// <summary>"T2–T6 · 8:00–14:00" in the interface language.</summary>
        public string Schedule
        {
            get
            {
                string days = string.Join(" ", workDays.OrderBy(d => d == 0 ? 7 : d).Select(DayName));
                return $"{days} · {workStart}:00–{workEnd}:00";
            }
        }

        public static string DayName(int day) => day switch
        {
            0 => Loc.T("CN", "Sun", "日"),
            1 => Loc.T("T2", "Mon", "月"),
            2 => Loc.T("T3", "Tue", "火"),
            3 => Loc.T("T4", "Wed", "水"),
            4 => Loc.T("T5", "Thu", "木"),
            5 => Loc.T("T6", "Fri", "金"),
            _ => Loc.T("T7", "Sat", "土"),
        };
    }

    /// <summary>
    /// The careers a player can follow from level <see cref="unlockLevel"/> (The Sims-style: ladder, schedule,
    /// performance, promotions, a branch half way up, events at work). Edit the JSON to add a career, a level, an
    /// interview or an event; EditMode CareerCatalogTests checks the file.
    /// </summary>
    [Serializable]
    public sealed class CareerCatalog
    {
        public const string ResourcePath = "Progression/careers";

        public string schema;
        public int unlockLevel = 10;
        public int missedShiftPenalty = 15;
        public CareerDefinition[] careers = Array.Empty<CareerDefinition>();

        private static CareerCatalog _cached;

        public static CareerCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>(ResourcePath);
            _cached = text != null ? Parse(text.text) : new CareerCatalog();
            return _cached;
        }

        public static CareerCatalog Parse(string json) => JsonUtility.FromJson<CareerCatalog>(json) ?? new CareerCatalog();

        public CareerDefinition Career(string id) => careers.FirstOrDefault(c => c.id == id);

        /// <summary>Problems in the data (empty when the file is valid).</summary>
        public List<string> Validate(ProgressionCatalog progression)
        {
            var errors = new List<string>();
            if (unlockLevel < 1) errors.Add("unlockLevel must be ≥ 1");
            foreach (var c in careers)
            {
                string at = "career " + c.id + ": ";
                if (string.IsNullOrEmpty(c.id)) errors.Add("a career without id");
                if (progression != null && progression.Quest(c.shiftQuest) == null) errors.Add(at + "shiftQuest " + c.shiftQuest + " is not in progression.json");
                if (c.levels.Length == 0) errors.Add(at + "no levels");
                if (c.workStart < 0 || c.workEnd > 24 || c.workEnd <= c.workStart) errors.Add(at + "bad working hours");
                if (c.workDays.Length == 0 || c.workDays.Any(d => d < 0 || d > 6)) errors.Add(at + "bad workDays");
                if (c.branches.Length > 0 && c.branchAtLevel != c.levels.Length + 1) errors.Add(at + "branchAtLevel must be the first level after the shared ones");
                for (int level = 2; level <= c.MaxLevel; level++)
                {
                    foreach (var branch in level > c.levels.Length && c.branches.Length > 0 ? c.branches.Select(b => b.id) : new[] { (string)null })
                    {
                        var interview = c.Interview(level, branch);
                        if (interview == null) { errors.Add(at + $"no interview to level {level}{(branch != null ? " (" + branch + ")" : "")}"); continue; }
                        if (!interview.choices.Any(x => x.correct)) errors.Add(at + $"interview to level {level} has no correct answer");
                        if (level == c.branchAtLevel && c.branches.Length > 0)
                            foreach (var b in c.branches)
                                if (!interview.choices.Any(x => x.correct && x.branch == b.id)) errors.Add(at + "branch interview misses branch " + b.id);
                    }
                }
                foreach (var e in c.events)
                    if (e.choices.Length < 2) errors.Add(at + "event " + e.id + " needs at least two answers");
            }
            return errors;
        }
    }
}
