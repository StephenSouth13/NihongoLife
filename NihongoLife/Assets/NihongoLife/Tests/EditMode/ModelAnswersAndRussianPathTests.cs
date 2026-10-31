using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using NihongoLife.StudyModes.Russian;
using NihongoLife.StudyModes.Video;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>IELTS model answers + self-assessment rubric, the Russian path / TORFL mocks, the video categories, and the three new popups.</summary>
    public class ModelAnswersAndRussianPathTests
    {
        private GameObject _canvas;
        private GameLanguage _lang;
        private static readonly string[] Langs = { "vi", "en", "ja", "ru" };

        [SetUp]
        public void SetUp()
        {
            _lang = Loc.Language;
            _canvas = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            IeltsTestUI.AutoOpenModelAnswers = false;
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Set(_lang);
            Object.DestroyImmediate(_canvas);
            var host = GameObject.Find(ReferenceUi.HostName);
            if (host != null) Object.DestroyImmediate(host);
            IeltsTestUI.AutoOpenModelAnswers = true;
            VideoCatalog.ResetCache();
        }

        private static void Complete(IeltsText t, string what)
        {
            Assert.IsNotNull(t, what);
            foreach (var l in Langs) Assert.IsFalse(string.IsNullOrWhiteSpace(t.Get(l)), $"{what} [{l}]");
        }

        // ── model answers ──

        [Test]
        public void ModelAnswers_CoverWritingAndSpeakingAtBands5To9()
        {
            var all = IeltsModelAnswers.All;
            Assert.AreEqual(7, all.Count);
            Assert.AreEqual(2, IeltsModelAnswers.For("writing", 1).Count());
            Assert.AreEqual(2, IeltsModelAnswers.For("writing", 2).Count());
            for (int part = 1; part <= 3; part++) Assert.AreEqual(1, IeltsModelAnswers.For("speaking", part).Count(), "part " + part);
            Assert.AreEqual(35, all.Sum(s => s.samples.Count));
            foreach (var set in all)
            {
                Complete(set.title, set.id + " title");
                Assert.IsFalse(string.IsNullOrWhiteSpace(set.prompt), set.id);
                CollectionAssert.AreEqual(IeltsModelAnswers.Bands, set.samples.Select(s => s.band).ToArray(), set.id);
            }
        }

        [Test]
        public void ModelAnswers_AnalysisIsConsistent()
        {
            foreach (var set in IeltsModelAnswers.All)
            foreach (var s in set.samples)
            {
                string at = $"{set.id}@{s.band}";
                var codes = s.criteria.Select(c => c.code).ToArray();
                CollectionAssert.AreEqual(set.skill == "speaking" ? new[] { "FC", "LR", "GRA", "P" } : new[] { set.task == 1 ? "TA" : "TR", "CC", "LR", "GRA" }, codes, at);
                var scores = s.criteria.Select(c => new IeltsCriterionScore { code = c.code, band = c.band }).ToList();
                Assert.AreEqual(s.band, set.skill == "speaking" ? IeltsBands.Speaking(scores) : IeltsBands.WritingTask(scores), 0.001f, at + " band = criteria");
                Complete(s.summary, at + " summary");
                Complete(s.nextBand, at + " nextBand");
                Assert.IsTrue(s.highlights.Count >= 2, at);
                foreach (var h in s.highlights)
                {
                    StringAssert.Contains(h.phrase, s.text, at);
                    Complete(h.note, at + " highlight");
                }
                if (set.skill == "writing" && s.band >= 6f)
                    Assert.GreaterOrEqual(Regex.Matches(s.text, @"\S+").Count, set.task == 1 ? 150 : 250, at + " words");
                var marked = ReferenceUi.Highlight(s.text, s.highlights);
                Assert.AreEqual(s.highlights.Count, Regex.Matches(marked, "<link=").Count, at + " markup");
            }
        }

        [Test]
        public void Rubric_HasFourCriteriaWithBands4To9AndChecklists()
        {
            var r = IeltsRubric.Loaded;
            Complete(r.source, "source");
            foreach (var (skill, task, codes) in new[] { ("writing", 1, "TA CC LR GRA"), ("writing", 2, "TR CC LR GRA"), ("speaking", 0, "FC LR GRA P") })
            {
                var list = r.For(skill, task);
                CollectionAssert.AreEqual(codes.Split(' '), list.Select(c => c.code).ToArray(), skill + task);
                foreach (var c in list)
                {
                    Complete(c.name, c.code); Complete(c.help, c.code);
                    CollectionAssert.AreEquivalent(new[] { 4, 5, 6, 7, 8, 9 }, c.bands.Select(b => b.band).ToArray(), c.code);
                    foreach (var b in c.bands) Complete(b.text, c.code + b.band);
                    Assert.GreaterOrEqual(c.checklist.Count, 3, c.code);
                    foreach (var x in c.checklist) Complete(x, c.code + " check");
                }
            }
        }

        [Test]
        public void SelfAssessment_UsesOfficialRoundingAndNeverTouchesTheGradedBand()
        {
            var a = new IeltsSelfAssessment();
            a.Set("TR", 7); a.Set("CC", 7); a.Set("LR", 6);
            Assert.IsFalse(a.Complete);
            Assert.AreEqual(0f, a.Compute("writing"));
            a.Set("GRA", 6);
            Assert.AreEqual(6.5f, a.Compute("writing"));
            a.Set("CC", 6);                                 // 6.25 → writing task floors to 6.0, speaking rounds up to 6.5
            Assert.AreEqual(6f, a.Compute("writing"));
            Assert.AreEqual(6.5f, a.Compute("speaking"));
            Assert.AreEqual(IeltsSelfAssessment.Source, a.source);

            var t1 = new IeltsSelfAssessment(); foreach (var c in new[] { "TA", "CC", "LR", "GRA" }) t1.Set(c, 6);
            var t2 = new IeltsSelfAssessment(); foreach (var c in new[] { "TR", "CC", "LR", "GRA" }) t2.Set(c, 7);
            var record = new IeltsProductiveRecord { id = "selftest_" + System.Guid.NewGuid().ToString("N"), skill = "writing", overall = 5f };
            record.writing.Add(new IeltsWritingTaskRecord { number = 1, task = 1, selfAssessment = t1 });
            record.writing.Add(new IeltsWritingTaskRecord { number = 2, task = 2, selfAssessment = t2 });
            Assert.AreEqual(6.5f, IeltsSelfAssessmentService.Overall(record));   // (6 + 2·7)/3 = 6.67 → 6.5
            try
            {
                IeltsSelfAssessmentService.Save(record);
                var back = IeltsProductiveStore.Load(record.id);
                Assert.IsNotNull(back);
                Assert.AreEqual(6.5f, back.selfAssessedOverall);
                Assert.AreEqual(5f, back.overall, "graded band untouched");
                Assert.AreEqual(7f, back.writing[1].selfAssessment.Get("TR"));
                Assert.AreEqual("self-assessed", back.writing[1].selfAssessment.source);
            }
            finally
            {
                var f = System.IO.Path.Combine(IeltsProductiveStore.Folder, record.id + ".json");
                if (System.IO.File.Exists(f)) System.IO.File.Delete(f);
            }
        }

        // ── Russian ──

        [Test]
        public void RussianPath_StepsResolveAndHaveVideoSlots()
        {
            RussianCourse.ResetCache();
            var path = RussianCourse.Path;
            CollectionAssert.AreEqual(new[] { "alphabet", "pronunciation", "a1", "a2", "b1", "b2" }, path.stages.Select(s => s.id).ToArray());
            var alphabet = Resources.Load<TextAsset>("Content/School/alphabets/ru_cyrillic").text;
            var units = Resources.Load<TextAsset>("Content/School/units/units_ru").text;
            var ids = new HashSet<string>();
            var slots = new HashSet<string>();
            foreach (var st in path.stages) Complete(st.label, st.id);
            foreach (var step in path.Steps)
            {
                Assert.IsTrue(ids.Add(step.id), "unique " + step.id);
                Complete(step.title, step.id);
                Assert.IsNotNull(step.video, step.id);
                StringAssert.StartsWith("ru-path-", step.video.id);
                Assert.IsTrue(slots.Add(step.video.id), "unique slot " + step.video.id);
                Assert.IsTrue(string.IsNullOrEmpty(step.video.youtubeUrl) || VideoCatalog.IsYouTube(step.video.youtubeUrl), step.id);
                string idPattern = "\"id\"\\s*:\\s*\"" + Regex.Escape(step.@ref) + "\"";
                switch (step.kind)
                {
                    case "alphabet": Assert.IsTrue(Regex.IsMatch(alphabet, idPattern), step.@ref); break;
                    case "unit": Assert.IsTrue(Regex.IsMatch(units, idPattern), step.@ref); break;
                    case "cert": Assert.IsNotNull(Resources.Load<TextAsset>("Content/School/certificates/" + step.@ref), step.@ref); break;
                    case "pronunciation": Assert.IsNotNull(path.Pronunciation(step.@ref), step.@ref); break;
                    case "mock": Assert.IsNotNull(RussianCourse.Mock(step.@ref), step.@ref); break;
                    default: Assert.Fail("kind " + step.kind); break;
                }
            }
            Assert.AreEqual(4, path.pronunciation.Count);
            foreach (var p in path.pronunciation)
            {
                Complete(p.tip, p.id);
                Assert.IsNotEmpty(p.examples, p.id);
                foreach (var q in p.quiz) Assert.That(q.answer, Is.InRange(0, q.choices.Length - 1), p.id);
            }
        }

        [Test]
        public void TorflMocks_HaveTwentyKeyedItemsWritingAndSpeaking()
        {
            var answers = new List<int>();
            foreach (var id in RussianCourse.MockIds)
            {
                var m = RussianCourse.Mock(id);
                Assert.IsNotNull(m, id);
                Complete(m.title, id); Complete(m.disclaimer, id);
                var qs = m.Questions.ToList();
                Assert.AreEqual(20, qs.Count, id);
                Assert.AreEqual(qs.Count, qs.Select(q => q.id).Distinct().Count(), id);
                foreach (var q in qs)
                {
                    Assert.AreEqual(4, q.choices.Distinct().Count(), q.id);
                    Assert.That(q.answer, Is.InRange(0, 3), q.id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(q.explanation?.en), q.id);
                    answers.Add(q.answer);
                }
                Assert.GreaterOrEqual(Regex.Matches(m.writing.model, @"\S+").Count, m.writing.minWords, id + " model length");
                Assert.GreaterOrEqual(m.speaking.Count, 2, id);
                var all = qs.ToDictionary(q => q.id, q => q.answer);
                Assert.AreEqual(20, m.Score(all));
                Assert.IsTrue(m.Passed(m.Score(all)));
                Assert.IsFalse(m.Passed(0));
            }
            Assert.Greater(answers.Distinct().Count(), 2, "keys are not all the same letter");
        }

        [Test]
        public void VideoCatalog_MergesAndCategorises()
        {
            VideoCatalog.ResetCache();
            int added = VideoCatalog.Merge(new[] { new VideoEntry { id = "t-ru-1", module = "ru-path", titleEn = "Test", youtubeUrl = "https://youtu.be/dQw4w9WgXcQ" } });
            Assert.AreEqual(1, added);
            Assert.AreEqual(0, VideoCatalog.Merge(new[] { new VideoEntry { id = "t-ru-1", module = "ru-path" } }), "no duplicates");
            Assert.AreEqual("ru", VideoCatalog.Category(VideoCatalog.Find("t-ru-1")));
            Assert.AreEqual("en", VideoCatalog.Category(new VideoEntry { module = "ielts" }));
            Assert.AreEqual("ja", VideoCatalog.Category(new VideoEntry { module = "jlpt" }));
            Assert.IsTrue(VideoCatalog.ByCategory("ru").Any(v => v.id == "t-ru-1"));
            Assert.AreEqual(0, RussianCourse.RegisterVideos(), "slots are empty until videos are published");
        }

        // ── UI ──

        private T Open<T>() where T : MenuPopupBase
        {
            var p = _canvas.AddComponent<T>();
            p.Initialize(null);
            p.Show();
            Assert.IsTrue(p.IsOpen, typeof(T).Name);
            return p;
        }

        private static Transform Find(Component root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        [TestCase(GameLanguage.Vietnamese)]
        [TestCase(GameLanguage.English)]
        [TestCase(GameLanguage.Japanese)]
        [TestCase(GameLanguage.Russian)]
        public void ModelAnswersPopup_BandTabsCompareAndTooltips(GameLanguage lang)
        {
            Loc.Set(lang);
            var p = Open<ModelAnswersPopup>();
            foreach (var set in IeltsModelAnswers.All)
            {
                p.Select(set);
                Assert.AreSame(set, p.CurrentSet);
                foreach (var b in IeltsModelAnswers.Bands) { p.SelectBand(b); Assert.AreEqual(b, p.Band); }
            }
            p.Select(IeltsModelAnswers.For("writing", 2).First());
            p.SelectBand(6f);
            Assert.IsNotEmpty(p.Tooltips);
            var tip = p.Tooltips[0];
            tip.Show(0, Vector2.zero);
            Assert.AreEqual(0, tip.Shown);
            Assert.IsTrue(tip.Bubble.gameObject.activeSelf);
            Assert.IsFalse(string.IsNullOrWhiteSpace(tip.BubbleText.text));
            tip.Show(-1, Vector2.zero);
            Assert.IsFalse(tip.Bubble.gameObject.activeSelf);
            p.ToggleCompare();
            Assert.IsTrue(p.Comparing);
            Assert.AreNotEqual(p.Band, p.CompareBand);
            Assert.GreaterOrEqual(p.Tooltips.Count, 2, "two columns");
            p.Hide();
        }

        [TestCase(GameLanguage.Vietnamese)]
        [TestCase(GameLanguage.Russian)]
        public void SelfAssessPopup_PicksBandsPerCriterion(GameLanguage lang)
        {
            Loc.Set(lang);
            var record = new IeltsProductiveRecord { id = "ui_selftest", skill = "writing" };
            record.writing.Add(new IeltsWritingTaskRecord { number = 1, task = 1, original = "x" });
            record.writing.Add(new IeltsWritingTaskRecord { number = 2, task = 2, original = "y" });
            var p = _canvas.AddComponent<SelfAssessPopup>();
            p.Initialize(null);
            p.Bind(record, "writing");
            p.Show();
            Assert.IsTrue(p.IsOpen);
            p.ShowTask(2);
            foreach (var c in p.Criteria) p.SetBand(c.code, 7);
            p.ToggleCheck("TR", 0);
            Assert.AreEqual(7f, p.Current.Compute("writing"));
            p.ShowTask(1);
            Assert.AreEqual("TA", p.Criteria[0].code);
            Assert.IsFalse(p.Current.Complete);
            Assert.IsTrue(record.writing[1].selfAssessment.Complete, "kept on the task record");
            p.Hide();
        }

        [TestCase(GameLanguage.Vietnamese)]
        [TestCase(GameLanguage.English)]
        [TestCase(GameLanguage.Japanese)]
        [TestCase(GameLanguage.Russian)]
        public void RussianPathPopup_StagesLessonsAndMock(GameLanguage lang)
        {
            Loc.Set(lang);
            var p = Open<RussianPathPopup>();
            Assert.IsNotNull(p.Step);
            foreach (var st in RussianCourse.Path.stages) { p.SelectStage(st.id); Assert.AreSame(st, p.Stage); }
            p.SelectStep("ru_pr1");
            Assert.IsFalse(p.PlayVideo(), "empty slot does not play");
            p.SelectStep("torfl_b1_mock_1");
            var mock = RussianCourse.Mock("torfl_b1_mock_1");
            foreach (var q in mock.Questions) p.Answer(q.id, q.answer);
            Assert.AreEqual(20, p.Submit());
            Assert.IsNotNull(Find(p, "Key_" + mock.Questions.First().id), "key shown after submit");
            p.Reveal("w");
            Assert.IsNotNull(Find(p, "Model"));
            p.Retry();
            Assert.AreEqual(-1, p.LastScore);
            p.Hide();
        }

        [Test]
        public void StudyHub_OpensModelAnswersAndRussianPath()
        {
            var hub = Open<StudyHubPopup>();
            Assert.IsNotNull(Find(hub, "Tile6"), "tile 6");
            hub.OpenModelAnswers();
            Assert.IsTrue(_canvas.GetComponentsInChildren<ModelAnswersPopup>(true).Any(x => x.IsOpen));
            hub.Show();
            hub.OpenRussianPath();
            Assert.IsTrue(_canvas.GetComponentsInChildren<RussianPathPopup>(true).Any(x => x.IsOpen));
        }
    }
}
