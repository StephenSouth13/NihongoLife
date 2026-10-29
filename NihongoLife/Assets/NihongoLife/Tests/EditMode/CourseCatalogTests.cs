using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>The school curriculum (40 units x 4 languages, alphabets, placement, certificates, routes) and the 12-tense course are data; these tests parse all of it.</summary>
    public class CourseCatalogTests
    {
        private GameLanguage _language;
        private string _tmp;

        [SetUp]
        public void Setup()
        {
            _language = Loc.Language;
            SchoolCatalog.ClearCache();
            TensesCatalog.ClearCache();
            _tmp = Path.Combine(Path.GetTempPath(), "nl_course_" + System.Guid.NewGuid().ToString("N") + ".json");
            LearningProgressStore.OverrideShared(new LearningProgressStore(_tmp));
            PlayerPrefs.DeleteKey("nl.course.path");
        }

        [TearDown]
        public void Teardown()
        {
            Loc.Set(_language);
            LearningProgressStore.OverrideShared(null);
            PlayerPrefs.DeleteKey("nl.course.path");
            if (File.Exists(_tmp)) File.Delete(_tmp);
        }

        private static IEnumerable<CourseQuestion> AllQuestions(string target)
        {
            foreach (var u in SchoolCatalog.Units(target)) foreach (var e in u.Exercises) foreach (var q in e.Questions) yield return q;
            foreach (var id in SchoolCatalog.AlphabetIds(target)) foreach (var q in SchoolCatalog.Alphabet(id).Quiz) yield return q;
            foreach (var q in SchoolCatalog.Placement(target).Questions) yield return q;
            foreach (var id in SchoolCatalog.CertificateIds(target)) foreach (var q in SchoolCatalog.Certificate(id).Questions) yield return q;
        }

        private static void AssertGradable(CourseQuestion q)
        {
            Assert.IsFalse(string.IsNullOrEmpty(q.Id), "question id");
            Assert.IsFalse(q.Stem.IsEmpty && q.Pairs.Count == 0 && q.Tokens.Count == 0 && q.Prompt.IsEmpty, q.Id + " has something to show");
            switch (q.Kind)
            {
                case CourseQuestionKind.Choice:
                    Assert.GreaterOrEqual(q.Choices.Count, 2, q.Id + " choices");
                    Assert.IsTrue(q.Choices.Any(c => c.Id == q.AnswerId), q.Id + " answer is one of the choices");
                    Assert.IsTrue(CourseGrader.Choice(q, q.AnswerId));
                    Assert.IsFalse(CourseGrader.Choice(q, q.Choices.First(c => c.Id != q.AnswerId).Id));
                    break;
                case CourseQuestionKind.Typing:
                    Assert.GreaterOrEqual(q.Accepted.Count, 1, q.Id + " accepted answers");
                    Assert.IsTrue(CourseGrader.Typing(q, q.Accepted[0]), q.Id + " accepts its own answer");
                    Assert.IsFalse(CourseGrader.Typing(q, "zzzz qqqq"), q.Id);
                    break;
                case CourseQuestionKind.Ordering:
                    Assert.GreaterOrEqual(q.AnswerTokens.Count, 2, q.Id + " tokens");
                    Assert.IsTrue(CourseGrader.Ordering(q, q.AnswerTokens), q.Id + " accepts its own order");
                    break;
                case CourseQuestionKind.Matching:
                    Assert.GreaterOrEqual(q.Pairs.Count, 2, q.Id + " pairs");
                    break;
                case CourseQuestionKind.TrueFalse:
                    Assert.IsTrue(CourseGrader.TrueFalse(q, q.AnswerBool));
                    break;
            }
        }

        [Test]
        public void Units_FortyPerLanguage_WithWordsDialogueAndExercises()
        {
            foreach (var target in SchoolCatalog.Targets)
            {
                var units = SchoolCatalog.Units(target);
                Assert.AreEqual(40, units.Count, target + " units");
                for (int i = 0; i < units.Count; i++)
                {
                    var u = units[i];
                    Assert.AreEqual(i + 1, u.No, u.Id);
                    Assert.IsTrue(u.Id.StartsWith(target), u.Id);
                    Assert.IsFalse(u.Title.IsEmpty, u.Id + " title");
                    Assert.Greater(u.Words.Count, 0, u.Id + " words");
                    Assert.Greater(u.Dialogue.Count, 0, u.Id + " dialogue");
                    Assert.Greater(u.Exercises.Count, 0, u.Id + " exercises");
                    Assert.Greater(u.QuestionCount, 0, u.Id + " questions");
                    Assert.IsNotNull(SchoolCatalog.GrammarPoint(target, u.GrammarId), u.Id + " grammar " + u.GrammarId);
                }
                Assert.AreEqual(units.Count, SchoolCatalog.Grammar(target).Count, target + " grammar points");
            }
        }

        [Test]
        public void EveryQuestionOfEveryTarget_IsGradableAgainstItsOwnAnswer()
        {
            int total = 0;
            foreach (var target in SchoolCatalog.Targets)
                foreach (var q in AllQuestions(target)) { AssertGradable(q); total++; }
            Assert.Greater(total, 3000, "the curriculum has thousands of questions");
            Debug.Log("[course] graded questions: " + total);
        }

        [Test]
        public void Alphabets_HaveGlyphsLessonsAndQuiz()
        {
            foreach (var target in SchoolCatalog.Targets)
                foreach (var id in SchoolCatalog.AlphabetIds(target))
                {
                    var a = SchoolCatalog.Alphabet(id);
                    Assert.IsNotNull(a, id);
                    Assert.AreEqual(target, a.Target);
                    Assert.Greater(a.Glyphs.Count, 10, id + " glyphs");
                    Assert.Greater(a.Lessons.Count, 0, id + " lessons");
                    Assert.Greater(a.Quiz.Count, 0, id + " quiz");
                    foreach (var g in a.Glyphs) { Assert.IsFalse(string.IsNullOrEmpty(g.Char), id + " glyph char " + g.Id); Assert.IsFalse(string.IsNullOrEmpty(g.Sound), id + " sound " + g.Id); }
                    foreach (var l in a.Lessons) foreach (var gid in l.ItemIds) Assert.IsNotNull(a.Glyph(gid), id + " lesson item " + gid);
                }
        }

        [Test]
        public void Placement_HasFiveLevelsAndAMonotonicScorer()
        {
            foreach (var target in SchoolCatalog.Targets)
            {
                var p = SchoolCatalog.Placement(target);
                Assert.AreEqual(20, p.Questions.Count, target);
                Assert.AreEqual(5, p.Levels.Count, target);
                foreach (var l in p.Levels)
                {
                    Assert.IsNotNull(SchoolCatalog.Unit(l.StartUnitId), target + " start unit " + l.StartUnitId);
                    Assert.IsNotNull(SchoolCatalog.Certificate(l.CertificateId), target + " certificate " + l.CertificateId);
                }
            }
            var levels = SchoolCatalog.Levels;
            Assert.AreEqual(-1, CourseGrader.PlacementLevelIndex(levels, new Dictionary<string, int>(), 3), "nothing passed = below A1, the path starts at A1");
            Assert.AreEqual(1, CourseGrader.PlacementLevelIndex(levels, new Dictionary<string, int> { { "A1", 4 }, { "A2", 3 }, { "B1", 1 } }, 3), "A1 and A2 passed, B1 failed");
            Assert.AreEqual(4, CourseGrader.PlacementLevelIndex(levels, new Dictionary<string, int> { { "A1", 4 }, { "A2", 4 }, { "B1", 4 }, { "B2", 3 }, { "C1", 3 } }, 3));
        }

        [Test]
        public void Certificates_AllTargets_HaveSectionsQuestionsAndHonestDisclaimer()
        {
            int count = 0;
            foreach (var target in SchoolCatalog.Targets)
                foreach (var id in SchoolCatalog.CertificateIds(target))
                {
                    var c = SchoolCatalog.Certificate(id);
                    Assert.IsNotNull(c, id);
                    Assert.AreEqual(id, c.Id);
                    Assert.Greater(c.Sections.Count, 0, id + " sections");
                    Assert.GreaterOrEqual(c.Questions.Count, 12, id + " questions");
                    Assert.Greater(c.TimeLimitMinutes, 0, id + " time limit");
                    Assert.IsFalse(c.Label.IsEmpty, id + " label");
                    foreach (var s in c.Sections) Assert.Greater(s.Questions.Count, 0, id + " section " + s.Id);
                    count++;
                }
            Assert.AreEqual(21, count);
            var ielts = SchoolCatalog.Certificate(SchoolCatalog.IeltsStyleId);
            Assert.GreaterOrEqual(ielts.Sections.Count, 2, "ielts-style reading and listening");
            Assert.GreaterOrEqual(ielts.Prompts.Count, 5, "writing tasks and speaking cards");
            Assert.IsTrue(ielts.Prompts.Any(p => p.Kind == "part2"), "speaking part 2 cue card");
            Assert.IsFalse(string.IsNullOrEmpty(ielts.Disclaimer), "ielts-style says it is not official");
            StringAssert.Contains("not", ielts.Disclaimer.ToLowerInvariant());
        }

        [Test]
        public void Routes_AllOneHundredTwenty_ResolveToExistingContent()
        {
            int routes = 0;
            foreach (var target in SchoolCatalog.Targets)
                foreach (var goal in LearningPath.Goals())
                    foreach (var level in SchoolCatalog.Levels)
                    {
                        var r = LearningPath.Route(target, goal.Id, level);
                        Assert.IsNotNull(r, target + "/" + goal.Id + "/" + level);
                        Assert.Greater(r.Steps.Count, 3, r.Id);
                        foreach (var s in r.Steps)
                        {
                            if (s.Type == "unit") Assert.IsNotNull(SchoolCatalog.Unit(s.Id), r.Id + " unit " + s.Id);
                            else if (s.Type == "alphabet") Assert.IsNotNull(SchoolCatalog.Alphabet(s.Id), r.Id + " alphabet " + s.Id);
                            else if (s.Type == "certificate") Assert.IsNotNull(SchoolCatalog.Certificate(s.Id), r.Id + " certificate " + s.Id);
                            else Assert.Fail(r.Id + " unknown step type " + s.Type);
                            Assert.IsFalse(string.IsNullOrEmpty(LearningPath.StepTitle(s)));
                        }
                        routes++;
                    }
            Assert.AreEqual(120, routes);
            Assert.AreEqual(6, LearningPath.Goals().Count);
        }

        [Test]
        public void LearningPath_SavesTheChoiceAndFindsTheNextStep()
        {
            Assert.IsFalse(LearningPath.Current.IsSet);
            Assert.AreEqual("en", LearningPath.Target);
            LearningPath.Save(new LearningPath.Choice { Target = "ja", Goal = "travel", StartLevel = "A1" });
            Assert.AreEqual("ja", LearningPath.Current.Target);
            var route = LearningPath.CurrentRoute();
            Assert.IsNotNull(route);
            var first = LearningPath.NextStep(route);
            Assert.IsNotNull(first);
            Assert.AreEqual(route.Steps[0].Id, first.Id);
            Assert.AreEqual(0, LearningPath.DoneCount(route));
            LearningProgressStore.Shared.RecordResult(LearningPath.StepKey(first), 8, 8);
            Assert.AreEqual(1, LearningPath.DoneCount(route));
            Assert.AreNotEqual(first.Id, LearningPath.NextStep(route).Id);
            LearningPath.Clear();
            Assert.IsFalse(LearningPath.Current.IsSet);
        }

        [Test]
        public void CertificatePass_UsesThePassRatio()
        {
            var cert = SchoolCatalog.Certificate("cert_jlpt_n5");
            int total = cert.Questions.Count;
            LearningProgressStore.Shared.RecordResult(LearningPath.CertificateKey(cert.Id), (int)(total * 0.4), total);
            Assert.IsFalse(LearningPath.CertificatePassed(cert.Id));
            LearningProgressStore.Shared.RecordResult(LearningPath.CertificateKey(cert.Id), (int)System.Math.Ceiling(total * cert.PassRatio), total);
            Assert.IsTrue(LearningPath.CertificatePassed(cert.Id));
        }

        [Test]
        public void Tenses_TwelveLessons_120Exercises_AllGradable_AllLanguagesHaveUsage()
        {
            var lessons = TensesCatalog.Lessons();
            Assert.AreEqual(12, lessons.Count);
            Assert.AreEqual(120, TensesCatalog.ExerciseCount());
            foreach (var l in lessons)
            {
                var ex = TensesCatalog.Exercises(l.TenseId);
                Assert.AreEqual(10, ex.Count, l.Id);
                foreach (var e in ex) AssertGradable(e);
                Assert.AreEqual(l.ExerciseIds.Count, ex.Count, l.Id + " exercise ids");
                Assert.AreEqual(l.ExerciseIds.OrderBy(x => x), ex.Select(e => e.Id).OrderBy(x => x), l.Id + " ids match");
                foreach (var code in new[] { "vi", "en", "ja", "ru" })
                {
                    Assert.Greater(l.UsageBy[code].Count, 0, l.Id + " usage " + code);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(l.MistakeBy[code]), l.Id + " mistake " + code);
                    Assert.IsFalse(string.IsNullOrEmpty(l.Title.In(code)), l.Id + " title " + code);
                }
                Assert.Greater(l.Examples.Count, 0);
                Assert.IsFalse(string.IsNullOrEmpty(l.Affirmative));
            }
            Assert.AreEqual(32, TensesCatalog.Verbs().Count);
            Assert.IsTrue(TensesCatalog.Verbs().All(v => v.Forms.Count == 12));
        }

        [Test]
        public void CourseText_FallsBackAndGlossesOnlyWhenTheLanguageDiffers()
        {
            var t = CourseText.From(MiniJson.ParseObject("{\"x\":{\"vi\":\"xin chào\",\"en\":\"hello\"}}")["x"]);
            Loc.Set(GameLanguage.Vietnamese); Assert.AreEqual("xin chào", t.Resolve());
            Loc.Set(GameLanguage.Japanese); Assert.AreEqual("hello", t.Resolve(), "falls back to English");
            Loc.Set(GameLanguage.Russian); Assert.AreEqual("hello", t.Resolve(), "Russian falls back to English");
            Loc.Set(GameLanguage.English); Assert.IsNull(t.Gloss("en"), "no gloss in the language being learned");
            Assert.AreEqual("hello", t.Gloss("vi"));
            Assert.AreEqual("a b", CourseText.Of("a b").Resolve());
            Assert.AreEqual("", CourseText.Empty.Resolve());
        }

        [Test]
        public void Grader_NormalizesCaseAccentsAndPunctuationPerQuestion()
        {
            Assert.AreEqual("khong", CourseGrader.Normalize("Không!", true, true, true));
            Assert.AreEqual("duong", CourseGrader.Normalize("Đường", true, true, true));
            Assert.AreEqual("không", CourseGrader.Normalize("Không", true, true, false));
            var q = new CourseQuestion { Kind = CourseQuestionKind.Typing, Accepted = new List<string> { "Good morning" } };
            Assert.IsTrue(CourseGrader.Typing(q, "good morning."));
            Assert.IsFalse(CourseGrader.Typing(q, "good evening"));
        }
    }
}