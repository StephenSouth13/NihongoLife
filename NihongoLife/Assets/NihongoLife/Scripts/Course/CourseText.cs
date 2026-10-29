using System.Collections.Generic;
using NihongoLife.Core;

namespace NihongoLife.Course
{
    /// <summary>
    /// A text from the school data: either plain (target-language content such as a Japanese word) or a per-language
    /// object {vi,en,ja,ru} (glosses, titles, explanations). Resolves to the UI language with a fixed fallback chain.
    /// </summary>
    public sealed class CourseText
    {
        public static readonly CourseText Empty = new CourseText(null, null);
        private static readonly string[] Fallback = { "en", "vi", "ru", "ja" };

        private readonly Dictionary<string, string> _by;
        public string Plain { get; }

        private CourseText(string plain, Dictionary<string, string> by) { Plain = plain; _by = by; }

        public static CourseText Of(string plain) => string.IsNullOrEmpty(plain) ? Empty : new CourseText(plain, null);

        public static CourseText From(object node)
        {
            if (node is string s) return Of(s);
            if (node is Dictionary<string, object> d)
            {
                var by = new Dictionary<string, string>();
                foreach (var kv in d) if (kv.Value is string v && v.Length > 0) by[kv.Key] = v;
                return by.Count == 0 ? Empty : new CourseText(null, by);
            }
            return Empty;
        }

        public bool IsEmpty => Plain == null && _by == null;
        public bool IsPlain => Plain != null;

        /// <summary>The text in a specific language, or null.</summary>
        public string In(string lang) => _by != null && lang != null && _by.TryGetValue(lang, out var v) ? v : null;

        /// <summary>Plain text, else the UI language, else en / vi / ru / ja. Never null.</summary>
        public string Resolve()
        {
            if (Plain != null) return Plain;
            if (_by == null) return "";
            var own = In(Loc.Code);
            if (own != null) return own;
            foreach (var code in Fallback) { var v = In(code); if (v != null) return v; }
            return "";
        }

        /// <summary>Helper line for the learner: null when the UI language is the language being learned (no gloss needed).</summary>
        public string Gloss(string target)
        {
            if (IsEmpty || Loc.Code == target) return null;
            var text = Resolve();
            return text.Length == 0 ? null : text;
        }

        public override string ToString() => Resolve();
    }
}