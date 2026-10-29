using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.StudyModes;
using UnityEngine;

namespace NihongoLife.Course
{
    public sealed class CourseGoal
    {
        public string Id;
        public CourseText Label = CourseText.Empty;
        public List<string> PriorityThemes = new List<string>();
    }

    public sealed class CourseRouteStep
    {
        public string Type, Id;
        public bool Priority;
    }

    public sealed class CourseRoute
    {
        public string Id, Target, Goal, StartLevel;
        public CourseText Title = CourseText.Empty;
        public List<CourseRouteStep> Steps = new List<CourseRouteStep>();
    }

    /// <summary>The learner's choice (language to learn, goal, starting level), saved per player, plus progress helpers over the route.</summary>
    public static class LearningPath
    {
        private const string PrefKey = "nl.course.path";
        public static event Action Changed;

        public struct Choice
        {
            public string Target, Goal, StartLevel;
            public bool IsSet => !string.IsNullOrEmpty(Target) && !string.IsNullOrEmpty(Goal) && !string.IsNullOrEmpty(StartLevel);
        }

        public static Choice Current
        {
            get
            {
                var raw = PlayerPrefs.GetString(PrefKey, "");
                var parts = raw.Split('|');
                if (parts.Length != 3 || !SchoolCatalog.IsTarget(parts[0])) return default;
                return new Choice { Target = parts[0], Goal = parts[1], StartLevel = parts[2] };
            }
        }

        public static void Save(Choice choice)
        {
            PlayerPrefs.SetString(PrefKey, choice.IsSet ? choice.Target + "|" + choice.Goal + "|" + choice.StartLevel : "");
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Clear() => Save(default);

        /// <summary>Language of the lessons: the saved choice, else English.</summary>
        public static string Target => Current.IsSet ? Current.Target : "en";

        // ---- model ----

        public static List<CourseGoal> Goals()
        {
            var list = new List<CourseGoal>();
            var goals = SchoolCatalog.Options.Obj("goals");
            if (goals == null) return list;
            foreach (var kv in goals)
            {
                var g = kv.Value as Dictionary<string, object>;
                if (g == null) continue;
                list.Add(new CourseGoal { Id = kv.Key, Label = CourseText.From(g.ContainsKey("label") ? g["label"] : null), PriorityThemes = CourseQuestionParser.Strings(g, "priority_themes") });
            }
            return list;
        }

        public static CourseText TargetName(string target)
        {
            var names = SchoolCatalog.Options.Obj("target_names");
            return CourseText.From(names != null && names.ContainsKey(target) ? names[target] : null);
        }

        /// <summary>Level label as the target language's own system shows it (Bậc 1, N5, A1, TORFL A1).</summary>
        public static string LevelLabel(string target, string cefr)
        {
            var labels = SchoolCatalog.Options.Obj("level_labels")?.Obj(target);
            var node = labels?.Obj(cefr);
            var text = CourseText.From(node);
            return text.IsEmpty ? cefr : text.Resolve();
        }

        public static CourseRoute Route(string target, string goal, string startLevel)
        {
            foreach (var node in CourseQuestionParser.Items(SchoolCatalog.Options, "routes"))
            {
                var r = node as Dictionary<string, object>;
                if (r == null || r.Str("target") != target || r.Str("goal") != goal || r.Str("start_level") != startLevel) continue;
                var route = new CourseRoute { Id = r.Str("id"), Target = target, Goal = goal, StartLevel = startLevel, Title = CourseText.From(r.ContainsKey("title") ? r["title"] : null) };
                foreach (var sn in CourseQuestionParser.Items(r, "steps"))
                {
                    var s = sn as Dictionary<string, object>;
                    if (s != null) route.Steps.Add(new CourseRouteStep { Type = s.Str("type"), Id = s.Str("id"), Priority = s.Bool("priority") });
                }
                return route;
            }
            return null;
        }

        public static CourseRoute CurrentRoute()
        {
            var c = Current;
            return c.IsSet ? Route(c.Target, c.Goal, c.StartLevel) : null;
        }

        // ---- progress (LearningProgressStore keys) ----

        public static string UnitKey(string unitId) => "unit:" + unitId;
        public static string AlphabetKey(string id) => "alpha:" + id;
        public static string CertificateKey(string id) => "cert:" + id;
        public static string TenseKey(string tenseId) => "tense:" + tenseId;
        public static string StepKey(CourseRouteStep s) => s.Type == "alphabet" ? AlphabetKey(s.Id) : s.Type == "certificate" ? CertificateKey(s.Id) : UnitKey(s.Id);

        /// <summary>A unit / alphabet counts as done at two stars (at least 2/3 right); a certificate at its pass ratio.</summary>
        public static bool IsDone(CourseRouteStep step)
        {
            var store = LearningProgressStore.Shared;
            if (step.Type == "certificate") return CertificatePassed(step.Id);
            return store.StarsFor(StepKey(step)) >= 2;
        }

        public static bool CertificatePassed(string certificateId)
        {
            var e = LearningProgressStore.Shared.Get(CertificateKey(certificateId));
            var cert = SchoolCatalog.Certificate(certificateId);
            if (e == null || e.total <= 0 || cert == null) return false;
            return (double)e.bestCorrect / e.total >= cert.PassRatio;
        }

        public static CourseRouteStep NextStep(CourseRoute route)
        {
            if (route == null) return null;
            foreach (var s in route.Steps) if (!IsDone(s)) return s;
            return null;
        }

        public static int DoneCount(CourseRoute route)
        {
            int n = 0;
            if (route != null) foreach (var s in route.Steps) if (IsDone(s)) n++;
            return n;
        }

        public static string StepTitle(CourseRouteStep step)
        {
            switch (step.Type)
            {
                case "alphabet": return SchoolCatalog.Alphabet(step.Id)?.Title.Resolve() ?? step.Id;
                case "certificate": return SchoolCatalog.Certificate(step.Id)?.Label.Resolve() ?? step.Id;
                default:
                    var unit = SchoolCatalog.Unit(step.Id);
                    return unit == null ? step.Id : unit.No + ". " + unit.Title.Resolve();
            }
        }

        /// <summary>Exam level a learner of this path should look at first (JLPT N5..N1 for Japanese, A1..C1 otherwise); null without a path.</summary>
        public static int StartLevelIndex => Current.IsSet ? Math.Max(0, Array.IndexOf(SchoolCatalog.Levels, Current.StartLevel)) : -1;
    }
}