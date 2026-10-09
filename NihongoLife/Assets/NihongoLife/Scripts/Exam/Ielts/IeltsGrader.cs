using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NihongoLife.Exam.Ielts
{
    /// <summary>
    /// Marks IELTS answers the way the printed keys are written:
    /// · "(...)" in a key is optional ("red(dish)" → red | reddish; "12 (dollars)" → 12 | 12 dollars);
    /// · case, surrounding spaces, a trailing full stop and a leading currency sign are ignored; spelling is not;
    /// · digit groups match with or without spaces ("123 4567" = "1234567");
    /// · the word limit of the question is enforced (an answer over the limit scores 0, as in IELTS);
    /// · "IN EITHER ORDER" pairs score one mark per correct letter, whichever box it is written in.
    /// The band is an estimate from the published raw-score conversion, not an official IELTS result.
    /// </summary>
    public static class IeltsGrader
    {
        public sealed class ItemResult
        {
            public int Number;
            public string Given;
            public string[] Correct;
            public bool IsCorrect;
            public bool OverLimit;
        }

        public sealed class Result
        {
            public int Raw;
            public int Max;
            public float Band;
            public List<ItemResult> Items = new();
            public Dictionary<int, int> RawByPart = new();
            public Dictionary<int, int> MaxByPart = new();
        }

        public static IEnumerable<string> Expand(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation)) yield break;
            var parts = Regex.Split(notation, @"(\([^)]*\))");
            var results = new List<string> { string.Empty };
            foreach (string part in parts)
            {
                if (part.StartsWith("(") && part.EndsWith(")"))
                {
                    string inner = part.Substring(1, part.Length - 2);
                    results = results.SelectMany(r => new[] { r, r + inner }).ToList();
                }
                else results = results.Select(r => r + part).ToList();
            }
            foreach (string r in results.Select(Normalize).Distinct()) if (r.Length > 0) yield return r;
        }

        public static string Normalize(string value)
        {
            if (value == null) return string.Empty;
            string s = value.Trim().ToLowerInvariant();
            s = s.Replace('’', '\'').Replace('–', '-').Replace('—', '-');
            s = s.TrimStart('£', '$', '€');
            s = s.TrimEnd('.', ',', ';');
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s;
        }

        private static string Compact(string s) => Regex.IsMatch(s, @"^[\d\s]+$") ? s.Replace(" ", "") : s;

        /// <summary>Counts words and numbers in an answer; consecutive digit groups count as one number.</summary>
        public static (int words, int numbers) Count(string answer)
        {
            int words = 0, numbers = 0;
            bool lastWasNumber = false;
            foreach (string token in Normalize(answer).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                bool number = Regex.IsMatch(token, @"^[\d.,:/]+(st|nd|rd|th)?$");
                if (number) { if (!lastWasNumber) numbers++; lastWasNumber = true; }
                else { words++; lastWasNumber = false; }
            }
            return (words, numbers);
        }

        public static bool WithinLimit(string answer, string limit)
        {
            if (string.IsNullOrEmpty(limit)) return true;
            var (words, numbers) = Count(answer);
            switch (limit)
            {
                case "ONE_WORD": return words + numbers <= 1;
                case "ONE_WORD_AND_OR_NUMBER": return words <= 1 && numbers <= 1;
                case "TWO_WORDS": return words + numbers <= 2;
                case "NO_MORE_THAN_TWO_WORDS_AND_OR_A_NUMBER": return words <= 2 && numbers <= 1;
                case "NO_MORE_THAN_THREE_WORDS": return words + numbers <= 3;
                case "NO_MORE_THAN_THREE_WORDS_AND_OR_A_NUMBER": return words <= 3 && numbers <= 1;
                default: return true;
            }
        }

        public static bool Matches(string given, IEnumerable<string> acceptedNotation)
        {
            string g = Compact(Normalize(given));
            if (g.Length == 0) return false;
            foreach (string notation in acceptedNotation)
                foreach (string accepted in Expand(notation))
                    if (Compact(accepted) == g) return true;
            return false;
        }

        public static Result Grade(IeltsTest test, IeltsKey key, IReadOnlyDictionary<int, string> responses)
        {
            var result = new Result();
            var byNumber = key.answers.ToDictionary(a => a.number);
            var setsDone = new HashSet<string>();
            foreach (var part in test.parts)
            {
                result.RawByPart[part.number] = 0;
                result.MaxByPart[part.number] = 0;
                foreach (var group in part.groups ?? Array.Empty<IeltsGroup>())
                {
                    if (group.type == "writing_task" || group.type == "speaking_part") continue;
                    for (int n = group.from; n <= group.to; n++)
                    {
                        if (!byNumber.TryGetValue(n, out var answer)) continue;
                        responses.TryGetValue(n, out string given);
                        var item = new ItemResult { Number = n, Given = given ?? string.Empty, Correct = answer.accepted };
                        if (answer.set != null && answer.set.Length > 1)
                        {
                            // Either order: count each correct letter once across the set's boxes.
                            string setId = string.Join(",", answer.set);
                            var letters = answer.set.Select(q => responses.TryGetValue(q, out string v) ? Normalize(v).ToUpperInvariant() : "").ToList();
                            var accepted = new HashSet<string>(answer.accepted.Select(a => a.ToUpperInvariant()));
                            int position = Array.IndexOf(answer.set, n);
                            string mine = letters[position];
                            bool duplicateEarlier = letters.Take(position).Contains(mine);
                            item.IsCorrect = accepted.Contains(mine) && !duplicateEarlier;
                            item.Correct = answer.accepted;
                            setsDone.Add(setId);
                        }
                        else if (group.type == "completion")
                        {
                            item.OverLimit = !string.IsNullOrWhiteSpace(given) && !WithinLimit(given, group.limit);
                            item.IsCorrect = !item.OverLimit && Matches(given, answer.accepted);
                        }
                        else item.IsCorrect = Matches(given, answer.accepted);

                        result.Items.Add(item);
                        result.MaxByPart[part.number]++;
                        if (item.IsCorrect) result.RawByPart[part.number]++;
                    }
                }
            }
            result.Raw = result.Items.Count(i => i.IsCorrect);
            result.Max = result.Items.Count;
            result.Band = test.skill == "reading" ? AcademicReadingBand(result.Raw) : ListeningBand(result.Raw);
            return result;
        }

        /// <summary>Published approximate conversion for a 40-question Listening test.</summary>
        public static float ListeningBand(int raw)
        {
            if (raw >= 39) return 9f; if (raw >= 37) return 8.5f; if (raw >= 35) return 8f; if (raw >= 32) return 7.5f;
            if (raw >= 30) return 7f; if (raw >= 26) return 6.5f; if (raw >= 23) return 6f; if (raw >= 18) return 5.5f;
            if (raw >= 16) return 5f; if (raw >= 13) return 4.5f; if (raw >= 10) return 4f; if (raw >= 8) return 3.5f;
            if (raw >= 6) return 3f; if (raw >= 4) return 2.5f; if (raw >= 2) return 2f; return raw >= 1 ? 1f : 0f;
        }

        /// <summary>Published approximate conversion for a 40-question Academic Reading test.</summary>
        public static float AcademicReadingBand(int raw)
        {
            if (raw >= 39) return 9f; if (raw >= 37) return 8.5f; if (raw >= 35) return 8f; if (raw >= 33) return 7.5f;
            if (raw >= 30) return 7f; if (raw >= 27) return 6.5f; if (raw >= 23) return 6f; if (raw >= 19) return 5.5f;
            if (raw >= 15) return 5f; if (raw >= 13) return 4.5f; if (raw >= 10) return 4f; if (raw >= 8) return 3.5f;
            if (raw >= 6) return 3f; if (raw >= 4) return 2.5f; return raw >= 1 ? 1f : 0f;
        }

        public static string KeyDisplay(IEnumerable<string> accepted) => string.Join(" / ", accepted ?? Array.Empty<string>());
    }
}
