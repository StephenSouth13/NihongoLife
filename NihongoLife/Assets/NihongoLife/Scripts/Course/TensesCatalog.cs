using System.Collections.Generic;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Course
{
    public sealed class TenseLesson
    {
        public string Id, TenseId, Cefr;
        public int Order;
        public CourseText Title = CourseText.Empty;
        public string Affirmative, Negative, Question;
        public Dictionary<string, List<string>> UsageBy = new Dictionary<string, List<string>>();
        public Dictionary<string, string> MistakeBy = new Dictionary<string, string>();
        public List<string> SignalWords = new List<string>();
        public List<CourseText> Examples = new List<CourseText>();
        public List<string> ExerciseIds = new List<string>();

        public List<string> Usage() => TensesCatalog.Pick(UsageBy);
        public string CommonMistake() => TensesCatalog.Pick(MistakeBy) ?? "";
        public string ExampleEnglish(int i) => Examples[i].In("en") ?? Examples[i].Resolve();
    }

    public sealed class TenseVerb
    {
        public string Verb, Base, ThirdPerson, Past, PastParticiple, Ing;
        public bool Irregular;
        public CourseText Meaning = CourseText.Empty;
        /// <summary>tenseId -> subject -> (affirmative, negative, question).</summary>
        public Dictionary<string, Dictionary<string, string[]>> Forms = new Dictionary<string, Dictionary<string, string[]>>();
    }

    /// <summary>The 12-tense English course: lessons, 120 exercises (as CourseQuestion) and the verb conjugation key.</summary>
    public static class TensesCatalog
    {
        private const string Dir = "Content/English/";
        private static List<TenseLesson> _lessons;
        private static Dictionary<string, List<CourseQuestion>> _exercises;
        private static List<TenseVerb> _verbs;

        public static void ClearCache() { _lessons = null; _exercises = null; _verbs = null; }

        private static Dictionary<string, object> Read(string name)
        {
            var asset = Resources.Load<TextAsset>(Dir + name);
            if (asset == null) return null;
            var parsed = MiniJson.ParseObject(asset.text);
            Resources.UnloadAsset(asset);
            return parsed;
        }

        /// <summary>UI language, else English, Vietnamese, Russian, Japanese.</summary>
        public static T Pick<T>(Dictionary<string, T> by) where T : class
        {
            if (by == null) return null;
            if (by.TryGetValue(Loc.Code, out var v) && v != null) return v;
            foreach (var k in new[] { "en", "vi", "ru", "ja" }) if (by.TryGetValue(k, out v) && v != null) return v;
            return null;
        }

        public static List<TenseLesson> Lessons()
        {
            if (_lessons != null) return _lessons;
            _lessons = new List<TenseLesson>();
            var main = Read("tenses_course");
            var extra = Read("tenses_course_en")?.Obj("lessons");
            foreach (var node in CourseQuestionParser.Items(main, "lessons"))
            {
                var l = CourseQuestionParser.Obj(node);
                if (l == null) continue;
                var formula = l.Obj("formula");
                var lesson = new TenseLesson
                {
                    Id = l.Str("id"), TenseId = l.Str("tenseId"), Cefr = l.Str("cefr"), Order = (int)l.Num("order"),
                    Title = CourseText.From(l.ContainsKey("title") ? l["title"] : null),
                    Affirmative = formula?.Str("affirmative"), Negative = formula?.Str("negative"), Question = formula?.Str("question"),
                    SignalWords = CourseQuestionParser.Strings(l, "signalWords"), ExerciseIds = CourseQuestionParser.Strings(l, "exerciseIds"),
                };
                var usage = l.Obj("usage");
                if (usage != null) foreach (var kv in usage) lesson.UsageBy[kv.Key] = CourseQuestionParser.Strings(usage, kv.Key);
                var mistakes = l.Obj("commonMistake");
                if (mistakes != null) foreach (var kv in mistakes) lesson.MistakeBy[kv.Key] = kv.Value as string;
                var en = extra?.Obj(lesson.TenseId);
                if (en != null)
                {
                    lesson.UsageBy["en"] = CourseQuestionParser.Strings(en, "usage");
                    lesson.MistakeBy["en"] = en.Str("commonMistake");
                }
                foreach (var ex in CourseQuestionParser.Items(l, "examples")) lesson.Examples.Add(CourseText.From(ex));
                _lessons.Add(lesson);
            }
            _lessons.Sort((a, b) => a.Order.CompareTo(b.Order));
            return _lessons;
        }

        public static TenseLesson Lesson(string tenseId) => Lessons().Find(l => l.TenseId == tenseId);

        public static List<CourseQuestion> Exercises(string tenseId)
        {
            if (_exercises == null)
            {
                _exercises = new Dictionary<string, List<CourseQuestion>>();
                var f = Read("tense_exercises");
                foreach (var node in CourseQuestionParser.Items(f, "exercises"))
                {
                    var e = CourseQuestionParser.Obj(node);
                    if (e == null) continue;
                    var q = Convert(e);
                    if (!_exercises.TryGetValue(e.Str("tense"), out var list)) _exercises[e.Str("tense")] = list = new List<CourseQuestion>();
                    list.Add(q);
                }
            }
            return _exercises.TryGetValue(tenseId, out var l2) ? l2 : new List<CourseQuestion>();
        }

        public static int ExerciseCount()
        {
            int n = 0;
            foreach (var l in Lessons()) n += Exercises(l.TenseId).Count;
            return n;
        }

        private static CourseQuestion Convert(Dictionary<string, object> e)
        {
            var q = new CourseQuestion
            {
                Id = e.Str("id"), Skill = "grammar", SourceType = e.Str("type"), Target = "en",
                Prompt = CourseText.From(e.ContainsKey("instruction") ? e["instruction"] : null),
                Stem = CourseText.Of(e.Str("promptEn")), Explanation = CourseText.From(e.ContainsKey("explanation") ? e["explanation"] : null),
                Translation = CourseText.Of(e.Str("sentence")),
            };
            switch (e.Str("type"))
            {
                case "MultipleChoice":
                    q.Kind = CourseQuestionKind.Choice;
                    var choices = CourseQuestionParser.Strings(e, "choices");
                    for (int i = 0; i < choices.Count; i++) q.Choices.Add(new CourseChoice { Id = i.ToString(), Text = CourseText.Of(choices[i]) });
                    q.AnswerId = ((int)e.Num("correctChoiceIndex")).ToString();
                    break;
                case "Reorder":
                    q.Kind = CourseQuestionKind.Ordering;
                    q.Tokens = CourseQuestionParser.Strings(e, "tokens");
                    q.AnswerTokens = CourseQuestionParser.Strings(e, "answerTokens");
                    q.IgnoreCase = false;
                    break;
                default:
                    q.Kind = CourseQuestionKind.Typing;
                    q.Accepted = CourseQuestionParser.Strings(e, "acceptedAnswers");
                    break;
            }
            return q;
        }

        // ---- conjugation key ----

        public static List<TenseVerb> Verbs()
        {
            if (_verbs != null) return _verbs;
            _verbs = new List<TenseVerb>();
            var f = Read("verb_conjugation_key");
            foreach (var node in CourseQuestionParser.Items(f, "verbs"))
            {
                var v = CourseQuestionParser.Obj(node);
                if (v == null) continue;
                var forms = v.Obj("forms");
                var verb = new TenseVerb
                {
                    Verb = v.Str("verb"), Irregular = v.Bool("irregular"), Meaning = CourseText.From(v.ContainsKey("meaning") ? v["meaning"] : null),
                    Base = forms?.Str("base"), ThirdPerson = forms?.Str("thirdPerson"), Past = forms?.Str("past"), PastParticiple = forms?.Str("pastParticiple"), Ing = forms?.Str("ing"),
                };
                var tenses = v.Obj("tenses");
                if (tenses != null)
                    foreach (var tk in tenses)
                    {
                        var perSubject = tk.Value as Dictionary<string, object>;
                        if (perSubject == null) continue;
                        var map = new Dictionary<string, string[]>();
                        foreach (var sk in perSubject)
                        {
                            var cell = sk.Value as Dictionary<string, object>;
                            if (cell != null) map[sk.Key] = new[] { cell.Str("affirmative"), cell.Str("negative"), cell.Str("question") };
                        }
                        verb.Forms[tk.Key] = map;
                    }
                _verbs.Add(verb);
            }
            return _verbs;
        }
    }
}