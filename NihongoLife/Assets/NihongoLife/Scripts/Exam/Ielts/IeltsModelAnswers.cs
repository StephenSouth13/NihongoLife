using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Exam.Ielts
{
    /// <summary>A text in the four UI languages (vi/en/ja/ru). Empty fields fall back to English.</summary>
    [Serializable]
    public sealed class IeltsText
    {
        public string vi, en, ja, ru;

        public string Get(string code)
        {
            string v = code switch { "vi" => vi, "ja" => ja, "ru" => ru, _ => en };
            return !string.IsNullOrWhiteSpace(v) ? v : !string.IsNullOrWhiteSpace(en) ? en : vi ?? "";
        }

        /// <summary>The text in the current interface language.</summary>
        public string Current => Get(Loc.Code);
        public bool Complete => new[] { vi, en, ja, ru }.All(s => !string.IsNullOrWhiteSpace(s));
    }

    [Serializable] public sealed class IeltsModelCriterion { public string code; public float band; public IeltsText note; }
    [Serializable] public sealed class IeltsModelHighlight { public string phrase; public string kind; public IeltsText note; public bool Good => kind == "good"; }
    [Serializable] public sealed class IeltsModelRow { public string[] cells; }

    /// <summary>One illustrative sample at one band, with examiner-style notes.</summary>
    [Serializable]
    public sealed class IeltsModelSample
    {
        public float band;
        public int words;
        public List<IeltsModelCriterion> criteria = new();
        public string text;
        public IeltsText summary;
        public List<IeltsModelHighlight> highlights = new();
        public IeltsText nextBand;
    }

    /// <summary>One prompt (Writing Task 1/2 or Speaking Part 1/2/3) with samples at bands 5–9.</summary>
    [Serializable]
    public sealed class IeltsModelSet
    {
        public string schema;
        public string id;
        public string skill;   // writing | speaking
        public int task;       // writing: 1|2 · speaking: part 1|2|3
        public string type;
        public IeltsText title;
        public string prompt;
        public IeltsText note;
        public string dataCaption;
        public string[] dataColumns;
        public List<IeltsModelRow> dataRows = new();
        public List<IeltsModelSample> samples = new();

        public IeltsModelSample AtBand(float band) => samples.FirstOrDefault(s => Mathf.Approximately(s.band, band));
    }

    [Serializable] public sealed class IeltsModelIndexEntry { public string id; public string skill; public int task; public IeltsText title; }
    [Serializable] public sealed class IeltsModelIndex { public string schema; public List<IeltsModelIndexEntry> sets = new(); }

    /// <summary>
    /// Pre-written model answers (Resources/Content/Ielts/ModelAnswers). Original writing by NihongoLife, labelled as
    /// illustrative band samples — not real candidate scripts and not official marking. No runtime AI.
    /// </summary>
    public static class IeltsModelAnswers
    {
        public const string Folder = "Content/Ielts/ModelAnswers/";
        private static List<IeltsModelSet> _all;

        public static IReadOnlyList<IeltsModelSet> All
        {
            get
            {
                if (_all != null) return _all;
                _all = new List<IeltsModelSet>();
                var index = Resources.Load<TextAsset>(Folder + "index");
                if (index == null) return _all;
                IeltsModelIndex parsed = null;
                try { parsed = JsonUtility.FromJson<IeltsModelIndex>(index.text); } catch (Exception) { }
                foreach (var entry in parsed?.sets ?? new List<IeltsModelIndexEntry>())
                {
                    var set = Load(entry.id);
                    if (set != null) _all.Add(set);
                }
                return _all;
            }
        }

        public static IeltsModelSet Load(string id)
        {
            var asset = Resources.Load<TextAsset>(Folder + id);
            if (asset == null) return null;
            try
            {
                var set = JsonUtility.FromJson<IeltsModelSet>(asset.text);
                return set != null && !string.IsNullOrEmpty(set.id) ? set : null;
            }
            catch (Exception) { return null; }
        }

        public static IEnumerable<IeltsModelSet> For(string skill, int task = 0) =>
            All.Where(s => s.skill == skill && (task == 0 || s.task == task));

        public static IeltsModelSet Find(string id) => All.FirstOrDefault(s => s.id == id);

        /// <summary>Bands every set ships with.</summary>
        public static readonly float[] Bands = { 5f, 6f, 7f, 8f, 9f };

        public static void ResetCache() => _all = null;
    }
}
