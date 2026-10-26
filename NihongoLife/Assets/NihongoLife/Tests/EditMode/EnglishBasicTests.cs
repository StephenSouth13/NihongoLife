using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.StudyModes;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>English Basic content integrity: every lesson is complete, translated and has a valid answer key.</summary>
    public class EnglishBasicCatalogTests
    {
        [Test]
        public void Catalog_HasBeginnerTopics_WithUniqueIds()
        {
            var all = EnglishBasicCatalog.All;
            Assert.GreaterOrEqual(all.Count, 8);
            Assert.AreEqual(all.Count, all.Select(l => l.id).Distinct().Count());
            foreach (var id in new[] { "eb_greetings", "eb_numbers", "eb_daily", "eb_shopping", "eb_directions" })
                Assert.IsNotNull(EnglishBasicCatalog.Find(id), id);
            Assert.IsTrue(all.All(l => l.id.StartsWith("eb_")), "progress ids must not clash with 文型 ids");
            Assert.IsNull(EnglishBasicCatalog.Find("nope"));
        }

        [Test]
        public void EveryLesson_IsFullyTranslated_AndHasVocabExamplesQuiz()
        {
            foreach (var l in EnglishBasicCatalog.All)
            {
                foreach (var s in new[] { l.titleVi, l.titleEn, l.titleJa, l.goalVi, l.goalEn, l.goalJa, l.icon })
                    Assert.IsFalse(string.IsNullOrWhiteSpace(s), l.id);
                Assert.GreaterOrEqual(l.vocab.Count, 5, l.id);
                Assert.GreaterOrEqual(l.examples.Count, 3, l.id);
                Assert.GreaterOrEqual(l.quiz.Count, 4, l.id);
                foreach (var v in l.vocab) Assert.IsTrue(new[] { v.en, v.vi, v.ja }.All(x => !string.IsNullOrWhiteSpace(x)), l.id + " vocab " + v.en);
                foreach (var e in l.examples) Assert.IsTrue(new[] { e.en, e.vi, e.ja }.All(x => !string.IsNullOrWhiteSpace(x)), l.id + " example " + e.en);
                Assert.IsTrue(l.quiz.Any(q => q.type == EnglishQuizType.Reorder), l.id + " needs a word-order question");
            }
        }

        [Test]
        public void EveryQuiz_HasValidAnswerKey()
        {
            foreach (var l in EnglishBasicCatalog.All)
            foreach (var q in l.quiz)
            {
                string tag = l.id + ": " + q.prompt;
                Assert.IsTrue(new[] { q.hintVi, q.hintEn, q.hintJa }.All(x => !string.IsNullOrWhiteSpace(x)), tag);
                if (q.type == EnglishQuizType.Reorder)
                {
                    Assert.GreaterOrEqual(q.tokens.Length, 3, tag);
                    Assert.IsTrue(EnglishQuizzes.IsCorrectOrder(q, q.tokens), tag);
                    Assert.AreEqual(q.hintEn, q.CorrectAnswer, tag + " reorder answer should equal the English hint");
                    continue;
                }
                Assert.GreaterOrEqual(q.choices.Length, 3, tag);
                Assert.That(q.answerIndex, Is.InRange(0, q.choices.Length - 1), tag);
                Assert.AreEqual(q.choices.Length, q.choices.Distinct().Count(), tag + " duplicate choices");
                if (q.type == EnglishQuizType.FillIn)
                {
                    Assert.IsTrue(q.prompt.Contains(EnglishQuiz.Blank), tag);
                    Assert.IsFalse(q.CorrectAnswer.Contains(EnglishQuiz.Blank), tag);
                    Assert.AreEqual(q.hintEn, q.CorrectAnswer, tag + " fill-in answer should equal the English hint");
                }
                Assert.IsTrue(EnglishQuizzes.IsCorrect(q, q.answerIndex), tag);
                Assert.IsFalse(EnglishQuizzes.IsCorrect(q, (q.answerIndex + 1) % q.choices.Length), tag);
            }
        }

        [Test]
        public void Reorder_ShuffleIsDeterministic_AndNotTheAnswer()
        {
            foreach (var l in EnglishBasicCatalog.All)
                for (int i = 0; i < l.quiz.Count; i++)
                {
                    var q = l.quiz[i];
                    if (q.type != EnglishQuizType.Reorder) continue;
                    var a = EnglishQuizzes.Shuffled(q, l.id + i);
                    CollectionAssert.AreEqual(a, EnglishQuizzes.Shuffled(q, l.id + i));
                    CollectionAssert.AreEquivalent(q.tokens, a);
                    CollectionAssert.AreNotEqual(q.tokens, a, l.id);
                    Assert.IsFalse(EnglishQuizzes.IsCorrectOrder(q, a), l.id);
                    Assert.IsFalse(EnglishQuizzes.IsCorrectOrder(q, q.tokens.Take(q.tokens.Length - 1).ToList()));
                }
        }

        [Test]
        public void Checkers_RejectNullAndWrongType()
        {
            var meaning = EnglishBasicCatalog.All[0].quiz.First(q => q.type != EnglishQuizType.Reorder);
            var order = EnglishBasicCatalog.All[0].quiz.First(q => q.type == EnglishQuizType.Reorder);
            Assert.IsFalse(EnglishQuizzes.IsCorrect(null, 0));
            Assert.IsFalse(EnglishQuizzes.IsCorrect(order, 0));
            Assert.IsFalse(EnglishQuizzes.IsCorrectOrder(meaning, new List<string>()));
            Assert.IsFalse(EnglishQuizzes.IsCorrectOrder(order, null));
        }
    }

    /// <summary>Builds the English Basic popup for real (no scene) and drives it like a player, via the Study Hub.</summary>
    public class EnglishBasicUiTests
    {
        private GameObject _canvas;
        private string _tmp;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nl_eb_" + System.Guid.NewGuid().ToString("N") + ".json");
            LearningProgressStore.OverrideShared(new LearningProgressStore(_tmp));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            StudyModeRegistry.Unregister(StudyModeRegistry.EnglishBasic);
            LearningProgressStore.OverrideShared(null);
            foreach (var f in new[] { _tmp, _tmp + ".bak", _tmp + ".tmp" }) if (System.IO.File.Exists(f)) System.IO.File.Delete(f);
        }

        [TestCase(GameLanguage.Vietnamese)]
        [TestCase(GameLanguage.English)]
        [TestCase(GameLanguage.Japanese)]
        public void Hub_RegistersEnglishBasic_AndOpensIt(GameLanguage lang)
        {
            var previous = Loc.Language;
            try
            {
                Loc.Set(lang);
                var hub = _canvas.AddComponent<StudyHubPopup>();
                hub.Initialize(null);
                hub.Show();
                Assert.IsTrue(StudyModeRegistry.IsAvailable(StudyModeRegistry.EnglishBasic), "hub must register English Basic");
                Assert.IsTrue(StudyModeRegistry.TryLaunch(StudyModeRegistry.EnglishBasic));
                var ui = _canvas.GetComponent<EnglishBasicUI>();
                Assert.IsNotNull(ui);
                Assert.IsTrue(ui.IsOpen);
                Assert.AreEqual(EnglishBasicCatalog.All[0], ui.CurrentLesson);
                foreach (var l in EnglishBasicCatalog.All) ui.SelectLesson(l.id); // draws every lesson in this language
                ui.Hide();
            }
            finally { Loc.Set(previous); }
        }

        private static void AnswerCorrectly(EnglishBasicUI ui, EnglishBasicLesson l, int i)
        {
            var q = l.quiz[i];
            if (q.type != EnglishQuizType.Reorder) { ui.Answer(q.answerIndex); return; }
            var shuffled = EnglishQuizzes.Shuffled(q, l.id + i);
            var used = new bool[shuffled.Length];
            foreach (var token in q.tokens)
            {
                int idx = -1;
                for (int k = 0; k < shuffled.Length; k++) if (!used[k] && shuffled[k] == token) { idx = k; break; }
                used[idx] = true;
                ui.Tap(idx, shuffled[idx]);
            }
            ui.CheckOrder();
        }

        [Test]
        public void FullQuiz_AllCorrect_GivesThreeStars_ForEveryLesson()
        {
            var ui = _canvas.AddComponent<EnglishBasicUI>();
            ui.Initialize(null);
            ui.Show();
            foreach (var l in EnglishBasicCatalog.All)
            {
                ui.SelectLesson(l.id);
                ui.StartQuiz();
                Assert.IsTrue(ui.InQuiz);
                for (int i = 0; i < l.quiz.Count; i++)
                {
                    AnswerCorrectly(ui, l, i);
                    Assert.IsTrue(ui.LastAnswerCorrect, l.id + " q" + i);
                    ui.Next();
                }
                var e = LearningProgressStore.Shared.Get(l.id);
                Assert.AreEqual(3, e.stars, l.id);
                Assert.AreEqual(l.quiz.Count, e.bestCorrect, l.id);
            }
        }

        [Test]
        public void WrongAnswers_ScoreLower_AndDoNotOverwriteBest()
        {
            var ui = _canvas.AddComponent<EnglishBasicUI>();
            ui.Initialize(null);
            ui.Show();
            var l = EnglishBasicCatalog.Find("eb_shopping");
            ui.SelectLesson(l.id);

            ui.StartQuiz();
            for (int i = 0; i < l.quiz.Count; i++) { AnswerCorrectly(ui, l, i); ui.Next(); }
            Assert.AreEqual(3, LearningProgressStore.Shared.StarsFor(l.id));

            ui.StartQuiz();
            for (int i = 0; i < l.quiz.Count; i++)
            {
                var q = l.quiz[i];
                if (q.type == EnglishQuizType.Reorder)
                {
                    var shuffled = EnglishQuizzes.Shuffled(q, l.id + i);
                    for (int k = 0; k < shuffled.Length; k++) ui.Tap(k, shuffled[k]);
                    ui.CheckOrder();
                }
                else ui.Answer((q.answerIndex + 1) % q.choices.Length);
                Assert.IsFalse(ui.LastAnswerCorrect);
                ui.Answer(q.type == EnglishQuizType.Reorder ? 0 : q.answerIndex); // second answer is ignored
                Assert.IsFalse(ui.LastAnswerCorrect);
                ui.Next();
            }
            var e = LearningProgressStore.Shared.Get(l.id);
            Assert.AreEqual(3, e.stars, "best score is kept");
            Assert.AreEqual(2, e.attempts);
        }
    }
}
