using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NihongoLife.MiniGames
{
    /// <summary>The English tense exercises (Resources/Content/English/tense_exercises.json) that the arcade can ask: fill-in-the-blank sentences with their accepted answers.</summary>
    public static class TenseExerciseBank
    {
        public const string ResourcePath = "Content/English/tense_exercises";

        [Serializable]
        public sealed class Exercise
        {
            public string id;
            public string tense;
            public string type;
            public string promptEn;
            public string sentence;
            public List<string> acceptedAnswers = new();

            /// <summary>"present_simple" becomes "Present Simple".</summary>
            public string TenseName => string.Join(" ", (tense ?? "").Split('_').Where(w => w.Length > 0).Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));

            public bool Accepts(string typed)
            {
                string norm = Normalize(typed);
                return norm.Length > 0 && acceptedAnswers.Any(a => Normalize(a) == norm);
            }
        }

        [Serializable]
        private sealed class File
        {
            public List<Exercise> exercises = new();
        }

        private static List<Exercise> _cache;

        /// <summary>Every single-blank fill-in exercise.</summary>
        public static IReadOnlyList<Exercise> Load()
        {
            if (_cache != null) return _cache;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            _cache = asset == null
                ? new List<Exercise>()
                : JsonUtility.FromJson<File>(asset.text).exercises
                    .Where(e => e.type == "FillBlank" && e.promptEn != null && e.promptEn.Contains("_____") && e.acceptedAnswers.Count > 0)
                    .ToList();
            return _cache;
        }

        public static string Normalize(string text) => (text ?? "").Trim().ToLowerInvariant().Replace('\u2019', '\'').Replace("  ", " ");
    }
}
