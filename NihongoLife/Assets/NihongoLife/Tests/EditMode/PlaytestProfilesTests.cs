using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Exam;
using NihongoLife.Exam.Ielts;
using NihongoLife.Music;
using NihongoLife.Progression;
using NihongoLife.Save;
using NihongoLife.StudyModes;
using NihongoLife.StudyModes.Russian;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Scripted playthroughs for five clearly-labelled local test profiles (no online accounts): new game, study hub,
    /// English Basic, IELTS model answers + self-assessment, the Russian path / TORFL mock, music, a career ladder,
    /// the JLPT N5 mock, then save → reload of each profile's own file. Saves go to a temp folder, never the real ones.
    /// </summary>
    public class PlaytestProfilesTests
    {
        public sealed class Profile
        {
            public string id, name, career; public GameLanguage lang;
            public override string ToString() => id;
        }

        public static IEnumerable<Profile> Profiles => new[]
        {
            new Profile { id = "playtest-01-an",   name = "Test An",   lang = GameLanguage.Vietnamese, career = "medical" },
            new Profile { id = "playtest-02-binh", name = "Test Binh", lang = GameLanguage.English,    career = "food" },
            new Profile { id = "playtest-03-chi",  name = "Test Chi",  lang = GameLanguage.Japanese,   career = "hospitality" },
            new Profile { id = "playtest-04-dung", name = "Test Dung", lang = GameLanguage.Russian,    career = "musician" },
            new Profile { id = "playtest-05-em",   name = "Test Em",   lang = GameLanguage.Vietnamese, career = "chef" },
        };

        private string _dir;
        private GameObject _canvas;
        private GameLanguage _lang;

        [SetUp]
        public void SetUp()
        {
            _lang = Loc.Language;
            _dir = Path.Combine(Path.GetTempPath(), "nl_playtest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _canvas = new GameObject("PlaytestCanvas", typeof(Canvas), typeof(CanvasScaler));
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
            try { Directory.Delete(_dir, true); } catch { }
        }

        private string SaveFile(Profile p) => Path.Combine(_dir, Path.GetFileName(LocalProgressRepository.SavePathFor(p.id)));

        [Test, TestCaseSource(nameof(Profiles))]
        public void FullPlaythrough(Profile p)
        {
            Loc.Set(p.lang);

            // New game
            var save = new PlayerProgressDto { playerId = p.id, displayName = p.name };
            Assert.AreEqual(1, save.level); Assert.AreEqual(1200, save.yen);

            // Study hub opens model answers and the Russian path
            var hub = _canvas.AddComponent<StudyHubPopup>();
            hub.Initialize(null); hub.Show();
            Assert.IsTrue(hub.IsOpen, p.id + " study hub");
            hub.OpenModelAnswers();
            Assert.IsTrue(_canvas.GetComponentsInChildren<ModelAnswersPopup>(true).Any(x => x.IsOpen), p.id + " model answers");
            hub.Show(); hub.OpenRussianPath();
            Assert.IsTrue(_canvas.GetComponentsInChildren<RussianPathPopup>(true).Any(x => x.IsOpen), p.id + " russian path");

            // English Basic: answer every quiz of the first two lessons correctly
            int answered = 0;
            foreach (var l in EnglishBasicCatalog.All.Take(2))
            foreach (var q in l.quiz)
            {
                bool ok = q.type == EnglishQuizType.Reorder ? EnglishQuizzes.IsCorrectOrder(q, q.tokens) : EnglishQuizzes.IsCorrect(q, q.answerIndex);
                Assert.IsTrue(ok, p.id + " " + l.id); answered++;
            }
            Assert.Greater(answered, 0);

            // Model answers + self-assessment (computed only; never touches a graded band)
            Assert.Greater(IeltsModelAnswers.All.Count, 0);
            var sa = new IeltsSelfAssessment();
            foreach (var c in new[] { "TR", "CC", "LR", "GRA" }) sa.Set(c, 6 + (p.id.Length % 3));
            Assert.IsTrue(sa.Complete); Assert.Greater(sa.Compute("writing"), 0f);

            // Russian path + a TORFL mock answered perfectly
            RussianCourse.ResetCache();
            Assert.Greater(RussianCourse.Path.Steps.Count(), 0);
            var mock = RussianCourse.Mock(RussianCourse.MockIds.First());
            var answers = mock.Questions.ToDictionary(q => q.id, q => q.answer);
            Assert.IsTrue(mock.Passed(mock.Score(answers)), p.id + " torfl");

            // Music: a song exists for the profile's culture
            string region = p.lang == GameLanguage.Russian ? "russia" : p.lang == GameLanguage.Vietnamese ? "vietnam" : "japan";
            Assert.IsNotEmpty(MusicCatalog.SongsIn(region).ToArray(), p.id + " music " + region);

            // Career: climb the ladder through the branch to the top
            var career = CareerCatalog.Load().careers.FirstOrDefault(c => c.id == p.career);
            Assert.IsNotNull(career, p.career);
            string branch = career.branches[p.id.Length % career.branches.Length].id;
            save.career.careerId = career.id; save.career.branchId = branch;
            for (int lv = 1; lv <= career.MaxLevel; lv++)
            {
                var def = career.Level(lv, branch);
                Assert.IsNotNull(def, $"{p.career} level {lv}");
                if (lv > 1) Assert.IsNotNull(career.Interview(lv, branch), $"{p.career} interview to {lv}");
                save.career.level = lv; save.career.totalSalary += def.salary; save.career.shifts += 5;
            }

            // JLPT N5 mock: all right answers → record a passing attempt
            var exam = Resources.Load<ExamDefinition>("Exams/jlpt_n5_mock_2");
            Assert.IsNotNull(exam, "jlpt mock");
            int total = exam.sections.SelectMany(s => s.questions).Count();
            Assert.Greater(total, 0);
            save.examAttempts.Add(new ExamAttemptRecord { examId = exam.name, totalScore = 180, totalMaxScore = 180, passed = true, completedAtUtcTicks = System.DateTime.UtcNow.Ticks });
            save.xp += 100; save.knowledge += answered;

            // Save → load: this profile's own file round-trips
            string path = SaveFile(p);
            StringAssert.Contains(p.id, path);
            ProgressFile.Write(path, save);
            var back = ProgressFile.Read(path, out bool fromBackup);
            Assert.IsFalse(fromBackup);
            Assert.AreEqual(p.name, back.displayName);
            Assert.AreEqual(career.MaxLevel, back.career.level);
            Assert.AreEqual(branch, back.career.branchId);
            Assert.AreEqual(1, back.examAttempts.Count);
            Assert.IsTrue(back.examAttempts[0].passed);
        }

        [Test]
        public void FiveProfiles_KeepSeparateSaves()
        {
            var all = Profiles.ToList();
            Assert.AreEqual(all.Count, all.Select(p => LocalProgressRepository.SavePathFor(p.id)).Distinct().Count());
            int i = 0;
            foreach (var p in all) ProgressFile.Write(SaveFile(p), new PlayerProgressDto { playerId = p.id, displayName = p.name, yen = 1000 + ++i });
            i = 0;
            foreach (var p in all)
            {
                var back = ProgressFile.Read(SaveFile(p), out _);
                Assert.AreEqual(p.name, back.displayName);
                Assert.AreEqual(1000 + ++i, back.yen, p.id + " did not get another profile's save");
            }
        }
    }
}
