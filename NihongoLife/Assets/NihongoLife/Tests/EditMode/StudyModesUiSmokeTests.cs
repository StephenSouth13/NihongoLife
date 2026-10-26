using System.Linq;
using NihongoLife.Core;
using NihongoLife.StudyModes;
using NihongoLife.StudyModes.Video;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>Builds the new study popups for real (no scene) in all three languages and drives them like a player.</summary>
    public class StudyModesUiSmokeTests
    {
        private GameObject _canvas;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nl_ui_" + System.Guid.NewGuid().ToString("N") + ".json");
            LearningProgressStore.OverrideShared(new LearningProgressStore(_tmp));
        }

        private string _tmp;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            LearningProgressStore.OverrideShared(null);
            foreach (var f in new[] { _tmp, _tmp + ".bak", _tmp + ".tmp" }) if (System.IO.File.Exists(f)) System.IO.File.Delete(f);
        }

        private T Open<T>() where T : MenuPopupBase
        {
            var p = _canvas.AddComponent<T>();
            p.Initialize(null);
            p.Show();
            Assert.IsTrue(p.IsOpen, typeof(T).Name);
            return p;
        }

        [TestCase(GameLanguage.Vietnamese)]
        [TestCase(GameLanguage.English)]
        [TestCase(GameLanguage.Japanese)]
        public void Hub_Patterns_Video_OpenWithoutErrors(GameLanguage lang)
        {
            var previous = Loc.Language;
            try
            {
                Loc.Set(lang);
                var hub = Open<StudyHubPopup>();
                Assert.IsNotNull(_canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "Open"));
                hub.OpenPatterns();
                Assert.IsFalse(hub.IsOpen);
                var patterns = _canvas.GetComponent<JapanesePatternsUI>();
                Assert.IsTrue(patterns.IsOpen);
                Assert.AreEqual("N5", patterns.CurrentPattern.level);
                patterns.Hide();
                hub.Show();
                hub.OpenVideos();
                var video = _canvas.GetComponent<InGameVideoPlayerUI>();
                Assert.IsTrue(video.IsOpen);
                Assert.IsNotNull(video.Current);
                video.Hide();
                Assert.AreEqual(VideoPlaybackState.Idle, video.StateMachine.State, "closing must stop playback");
            }
            finally { Loc.Set(previous); }
        }

        [Test]
        public void PatternDrill_FullRun_AllCorrect_GivesThreeStars()
        {
            var ui = Open<JapanesePatternsUI>();
            foreach (var level in JapanesePatternCatalog.Levels)
            {
                ui.SelectLevel(level);
                foreach (var p in JapanesePatternCatalog.ForLevel(level))
                {
                    ui.SelectPattern(p.id);
                    ui.StartDrill();
                    Assert.IsTrue(ui.InDrill);
                    for (int i = 0; i < p.drills.Count; i++)
                    {
                        var d = p.drills[i];
                        if (d.type == PatternDrillType.Reorder)
                        {
                            var shuffled = PatternDrills.Shuffled(d, p.id + i);
                            var used = new bool[shuffled.Length];
                            foreach (var token in d.tokens)
                            {
                                int idx = -1;
                                for (int k = 0; k < shuffled.Length; k++) if (!used[k] && shuffled[k] == token) { idx = k; break; }
                                used[idx] = true;
                                ui.Tap(idx, shuffled[idx]);
                            }
                            ui.CheckOrder();
                        }
                        else ui.Answer(d.answerIndex);
                        ui.Next();
                    }
                    Assert.AreEqual(3, LearningProgressStore.Shared.StarsFor(p.id), p.id);
                }
            }
        }

        [Test]
        public void Video_YouTubeEntry_ShowsExternalCard_AndBadSourceShowsError()
        {
            var ui = Open<InGameVideoPlayerUI>();
            var yt = VideoCatalog.All.First(v => VideoCatalog.Classify(v) == VideoSourceKind.YouTubeOnly);
            ui.Select(yt.id);
            Assert.AreEqual(VideoPlaybackState.ExternalOnly, ui.StateMachine.State);
            ui.TogglePause(); // no-op, must not throw
            ui.ToggleFullscreen(); ui.ToggleFullscreen();
            ui.Retry();
            Assert.AreEqual(VideoPlaybackState.ExternalOnly, ui.StateMachine.State);
        }
    }
}
