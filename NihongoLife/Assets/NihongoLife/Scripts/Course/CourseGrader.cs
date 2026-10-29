using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NihongoLife.Course
{
    /// <summary>Pure grading rules for <see cref="CourseQuestion"/> (no UI, no state).</summary>
    public static class CourseGrader
    {
        public static string Normalize(string text, bool ignoreCase, bool ignorePunctuation, bool ignoreAccents)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string s = text.Normalize(NormalizationForm.FormC);
            if (ignoreAccents)
            {
                var d = s.Normalize(NormalizationForm.FormD);
                var sb = new StringBuilder(d.Length);
                foreach (var ch in d) if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
                // đ/Đ have no combining form but are an "accent" in Vietnamese.
                s = sb.ToString().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormC);
            }
            if (ignorePunctuation)
            {
                var sb = new StringBuilder(s.Length);
                foreach (var ch in s)
                {
                    var cat = char.GetUnicodeCategory(ch);
                    bool punct = char.IsPunctuation(ch) || char.IsSymbol(ch) || cat == UnicodeCategory.OtherPunctuation;
                    sb.Append(punct ? ' ' : ch);
                }
                s = sb.ToString();
            }
            if (ignoreCase) s = s.ToLowerInvariant();
            var parts = s.Split(new[] { ' ', '\t', '\n', '\u3000' }, System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        public static bool Choice(CourseQuestion q, string choiceId) => !string.IsNullOrEmpty(choiceId) && choiceId == q.AnswerId;

        public static bool Typing(CourseQuestion q, string input)
        {
            string given = Normalize(input, q.IgnoreCase, q.IgnorePunctuation, q.IgnoreAccents);
            if (given.Length == 0) return false;
            foreach (var answer in q.Accepted)
                if (Normalize(answer, q.IgnoreCase, q.IgnorePunctuation, q.IgnoreAccents) == given) return true;
            return false;
        }

        public static bool Ordering(CourseQuestion q, IList<string> built)
        {
            if (built == null || built.Count != q.AnswerTokens.Count) return false;
            for (int i = 0; i < built.Count; i++) if (built[i] != q.AnswerTokens[i]) return false;
            return true;
        }

        public static bool TrueFalse(CourseQuestion q, bool value) => value == q.AnswerBool;

        /// <summary>Placement: highest level L with at least <paramref name="passPerLevel"/> right at L and at every lower level.</summary>
        public static int PlacementLevelIndex(IList<string> levels, IDictionary<string, int> rightByLevel, int passPerLevel)
        {
            int reached = -1;
            for (int i = 0; i < levels.Count; i++)
            {
                rightByLevel.TryGetValue(levels[i], out int right);
                if (right >= passPerLevel) reached = i; else break;
            }
            return reached;
        }
    }
}