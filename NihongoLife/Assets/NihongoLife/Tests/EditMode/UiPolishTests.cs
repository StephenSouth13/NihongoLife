using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NihongoLife.StudyModes.Video;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>Shared frame/type scale, one transition curve, async-only scene loads, video loading/buffering states.</summary>
    public class UiPolishTests
    {
        private GameObject _go;
        [TearDown] public void TearDown() { if (_go != null) Object.DestroyImmediate(_go); }

        [Test]
        public void Frame_IsAppliedOnceWithBorderBodyAndCorners()
        {
            _go = new GameObject("Card", typeof(RectTransform), typeof(Image));
            var card = (RectTransform)_go.transform;
            card.sizeDelta = new Vector2(400, 300);
            _go.GetComponent<Image>().color = UiTheme.Ink;
            var child = new GameObject("Content", typeof(RectTransform)); child.transform.SetParent(card, false);
            UiFrame.Apply(card); UiFrame.Apply(card, UiFrame.Tone.Teal);
            Assert.AreEqual(1, card.Cast<Transform>().Count(t => t.name == "UiFrame"), "idempotent");
            var frame = card.Find("UiFrame");
            Assert.AreEqual(0, frame.GetSiblingIndex(), "behind the content");
            Assert.AreEqual(4, frame.Cast<Transform>().Count(t => t.name.StartsWith("Edge")), "four hairline edges");
            Assert.IsNull(frame.Find("Body"), "no filled wash behind the card");
            Assert.AreEqual(8, frame.Cast<Transform>().Count(t => t.name.StartsWith("Corner")));
            Assert.Greater(UiFrame.Title, UiFrame.Heading); Assert.Greater(UiFrame.Heading, UiFrame.Body); Assert.Greater(UiFrame.Body, UiFrame.Caption);
        }

        [Test]
        public void Transition_CurveIsSmoothMonotonicAndWipesBackToRest()
        {
            Assert.AreEqual(0f, SceneTransition.Alpha(0f, true), 1e-4); Assert.AreEqual(1f, SceneTransition.Alpha(SceneTransition.FadeInSeconds, true), 1e-4);
            Assert.AreEqual(1f, SceneTransition.Alpha(0f, false), 1e-4); Assert.AreEqual(0f, SceneTransition.Alpha(5f, false), 1e-4);
            float prev = -1f;
            for (float t = 0; t <= SceneTransition.FadeInSeconds; t += 0.02f) { float a = SceneTransition.Alpha(t, true); Assert.GreaterOrEqual(a, prev); prev = a; }
            Assert.AreEqual(0f, SceneTransition.WipeOffset(1f, true), 1e-4);
            Assert.Less(SceneTransition.WipeOffset(0f, true), 0f); Assert.Greater(SceneTransition.WipeOffset(0f, false), 0f);
            Assert.AreEqual(1f, SceneTransition.DisplayProgress(0.3f, 0.9f), 1e-4);
            Assert.AreEqual(0.5f, SceneTransition.DisplayProgress(0.5f, 0.1f), 1e-4, "never goes backwards");
        }

        [Test]
        public void Runtime_NeverLoadsScenesSynchronously()
        {
            var root = Path.Combine(Application.dataPath, "NihongoLife", "Scripts");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/Editor/"))
                .Where(f => Regex.IsMatch(File.ReadAllText(f), @"SceneManager\.LoadScene\s*\(")).Select(Path.GetFileName).ToList();
            CollectionAssert.IsEmpty(offenders, "use LoadSceneAsync (SceneFlowController) so the loading screen can show progress");
        }

        [Test]
        public void Video_PrepareProgressBufferingAndStallTimeout()
        {
            var sm = new VideoPlaybackStateMachine { PrepareTimeout = 10f, StallTimeout = 3f };
            var entry = new VideoEntry { id = "t", playableUrl = "https://example.com/v.mp4" };
            sm.Load(entry);
            Assert.AreEqual(VideoPlaybackState.Preparing, sm.State);
            sm.Tick(2.5f);
            Assert.AreEqual(0.25f, sm.PrepareProgress, 1e-4);
            Assert.IsTrue(sm.OnPrepared(sm.Generation));
            Assert.IsFalse(sm.TickPlayback(1, 0.1f)); Assert.IsFalse(sm.Buffering);
            sm.TickPlayback(1, 0.5f); Assert.IsFalse(sm.Buffering);
            sm.TickPlayback(1, 0.5f); Assert.IsTrue(sm.Buffering, "no new frame for 1s -> buffering");
            sm.TickPlayback(2, 0.1f); Assert.IsFalse(sm.Buffering, "a new frame clears it");
            Assert.IsFalse(sm.TickPlayback(2, 2f));
            Assert.IsTrue(sm.TickPlayback(2, 1.5f), "stall timeout fails");
            Assert.AreEqual(VideoPlaybackState.Error, sm.State); Assert.AreEqual(VideoErrorKind.Timeout, sm.Error); Assert.IsFalse(sm.Buffering);
            sm.Load(entry); Assert.AreEqual(VideoPlaybackState.Preparing, sm.State, "retry reloads");
        }

        [Test]
        public void Video_StatusShowsDeterminateAndIndeterminate()
        {
            _go = new GameObject("Stage", typeof(RectTransform));
            var s = VideoPlayerStatus.Create((RectTransform)_go.transform);
            s.Set(true, 0.4f);
            Assert.IsTrue(s.gameObject.activeSelf); Assert.IsTrue(s.transform.Find("Track").gameObject.activeSelf);
            s.Set(true, -1f);
            Assert.IsFalse(s.transform.Find("Track").gameObject.activeSelf, "buffering: spinner only");
            s.Set(false, 0f); Assert.IsFalse(s.gameObject.activeSelf);
        }
    }
}
