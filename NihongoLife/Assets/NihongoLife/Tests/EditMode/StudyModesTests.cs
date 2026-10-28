using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.StudyModes;
using NihongoLife.StudyModes.Video;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    public class JapanesePatternCatalogTests
    {
        [Test]
        public void Catalog_CoversN5ToN3_WithUniqueIds()
        {
            var all = JapanesePatternCatalog.All;
            Assert.GreaterOrEqual(all.Count, 15);
            foreach (var level in JapanesePatternCatalog.Levels)
                Assert.GreaterOrEqual(JapanesePatternCatalog.ForLevel(level).Count(), 4, level);
            Assert.AreEqual(all.Count, all.Select(p => p.id).Distinct().Count());
            Assert.IsTrue(all.All(p => JapanesePatternCatalog.Levels.Contains(p.level)));
        }

        [Test]
        public void EveryPattern_HasCompleteTrilingualContent()
        {
            foreach (var p in JapanesePatternCatalog.All)
            {
                foreach (var s in new[] { p.pattern, p.structure, p.meaningVi, p.meaningEn, p.meaningJa, p.noteVi, p.noteEn, p.noteJa })
                    Assert.IsFalse(string.IsNullOrWhiteSpace(s), p.id);
                Assert.GreaterOrEqual(p.examples.Count, 2, p.id);
                foreach (var e in p.examples)
                {
                    foreach (var s in new[] { e.ja, e.kana, e.romaji, e.vi, e.en }) Assert.IsFalse(string.IsNullOrWhiteSpace(s), p.id);
                    Assert.IsTrue(e.kana.All(c => c < 0x4E00 || c > 0x9FFF), $"{p.id}: kana line must not contain kanji: {e.kana}");
                    Assert.IsTrue(e.romaji.All(c => c < 0x3000), $"{p.id}: romaji must be latin: {e.romaji}");
                }
            }
        }

        [Test]
        public void EveryDrill_HasAValidAnswerKey()
        {
            foreach (var p in JapanesePatternCatalog.All)
            {
                Assert.GreaterOrEqual(p.drills.Count, 3, p.id);
                Assert.IsTrue(p.drills.Any(d => d.type == PatternDrillType.Reorder), p.id + " needs a reorder drill");
                foreach (var d in p.drills)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(d.hintVi) || string.IsNullOrWhiteSpace(d.hintEn), p.id);
                    if (d.type == PatternDrillType.Reorder)
                    {
                        Assert.GreaterOrEqual(d.tokens.Length, 3, p.id);
                        Assert.IsTrue(d.tokens.All(t => !string.IsNullOrWhiteSpace(t)), p.id);
                        Assert.IsTrue(PatternDrills.IsCorrectOrder(d, d.tokens), p.id);
                        continue;
                    }
                    Assert.GreaterOrEqual(d.choices.Length, 3, p.id);
                    Assert.That(d.answerIndex, Is.InRange(0, d.choices.Length - 1), p.id);
                    Assert.AreEqual(d.choices.Length, d.choices.Distinct().Count(), p.id + " duplicate choices");
                    if (d.type == PatternDrillType.FillIn)
                        Assert.AreEqual(1, CountOf(d.promptJa, "＿＿"), p.id + " fill-in needs exactly one blank");
                    Assert.IsTrue(PatternDrills.IsCorrect(d, d.answerIndex));
                    for (int i = 0; i < d.choices.Length; i++) if (i != d.answerIndex) Assert.IsFalse(PatternDrills.IsCorrect(d, i));
                }
            }
        }

        [Test]
        public void FillIn_CorrectSentenceReplacesBlank()
        {
            var d = JapanesePatternCatalog.Find("n5_wa_desu").drills[0];
            Assert.AreEqual("わたしはベトナム人です。", d.CorrectSentence);
        }

        [Test]
        public void Shuffle_IsDeterministic_AndNeverTheAnswer()
        {
            foreach (var p in JapanesePatternCatalog.All)
                foreach (var d in p.drills.Where(x => x.type == PatternDrillType.Reorder))
                {
                    var a = PatternDrills.Shuffled(d, p.id);
                    CollectionAssert.AreEqual(a, PatternDrills.Shuffled(d, p.id));
                    CollectionAssert.AreEquivalent(d.tokens, a);
                    Assert.AreNotEqual(string.Concat(d.tokens), string.Concat(a), p.id);
                    Assert.IsFalse(PatternDrills.IsCorrectOrder(d, a));
                }
        }

        [Test]
        public void Stars_Thresholds()
        {
            Assert.AreEqual(3, PatternDrills.Stars(3, 3));
            Assert.AreEqual(2, PatternDrills.Stars(2, 3));
            Assert.AreEqual(1, PatternDrills.Stars(1, 3));
            Assert.AreEqual(0, PatternDrills.Stars(0, 3));
            Assert.AreEqual(0, PatternDrills.Stars(1, 0));
        }

        private static int CountOf(string s, string sub) { int n = 0, i = 0; while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; } return n; }
    }

    public class LearningProgressStoreTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = Path.Combine(Path.GetTempPath(), "nl_learn_" + System.Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_dir); }
        [TearDown] public void TearDown() { try { Directory.Delete(_dir, true); } catch { } }

        [Test]
        public void RecordResult_KeepsBest_AndPersists()
        {
            string path = Path.Combine(_dir, "learning_progress.json");
            var store = new LearningProgressStore(path);
            Assert.IsTrue(store.RecordResult("n5_tai", 1, 3));
            Assert.IsTrue(store.RecordResult("n5_tai", 3, 3));
            Assert.IsFalse(store.RecordResult("n5_tai", 2, 3));
            store.MarkVideoWatched("kana-match");
            store.MarkVideoWatched("kana-match");
            var reloaded = new LearningProgressStore(path);
            var e = reloaded.Get("n5_tai");
            Assert.AreEqual(3, e.bestCorrect); Assert.AreEqual(3, e.stars); Assert.AreEqual(3, e.attempts);
            Assert.IsTrue(reloaded.IsVideoWatched("kana-match"));
            Assert.AreEqual(1, reloaded.Data.watchedVideos.Count);
            Assert.AreEqual(3, reloaded.TotalStars(new[] { "n5_tai", "missing" }));
        }

        [Test]
        public void CorruptFile_RecoversFromBackup()
        {
            string path = Path.Combine(_dir, "learning_progress.json");
            var store = new LearningProgressStore(path);
            store.RecordResult("a", 2, 3);
            store.RecordResult("b", 3, 3); // second write leaves the first as .bak
            File.WriteAllText(path, "{ not json");
            var recovered = new LearningProgressStore(path);
            Assert.IsNotNull(recovered.Get("a"));
        }

        [Test]
        public void MissingOrGarbage_StartsEmpty_AndIgnoresBadInput()
        {
            string path = Path.Combine(_dir, "sub", "learning_progress.json");
            var store = new LearningProgressStore(path);
            Assert.AreEqual(0, store.Data.entries.Count);
            Assert.IsFalse(store.RecordResult("", 1, 1));
            Assert.IsFalse(store.RecordResult("x", 1, 0));
            store.RecordResult("x", 99, 3);
            Assert.AreEqual(3, store.Get("x").bestCorrect);
            Assert.IsTrue(File.Exists(path));
        }
    }

    public class StudyModeRegistryTests
    {
        [TearDown] public void TearDown() => StudyModeRegistry.Unregister("test-mode");

        [Test]
        public void Register_Launch_Unregister()
        {
            int calls = 0;
            Assert.IsFalse(StudyModeRegistry.TryLaunch("test-mode"));
            StudyModeRegistry.Register("test-mode", () => calls++);
            Assert.IsTrue(StudyModeRegistry.IsAvailable("test-mode"));
            Assert.IsTrue(StudyModeRegistry.TryLaunch("test-mode"));
            Assert.AreEqual(1, calls);
            StudyModeRegistry.Unregister("test-mode");
            Assert.IsFalse(StudyModeRegistry.IsAvailable("test-mode"));
            Assert.IsFalse(StudyModeRegistry.TryLaunch(null));
        }

        [Test]
        public void ThrowingLauncher_ReturnsFalse()
        {
            StudyModeRegistry.Register("test-mode", () => throw new System.InvalidOperationException("boom"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));
            Assert.IsFalse(StudyModeRegistry.TryLaunch("test-mode"));
        }
    }

    public class VideoCatalogTests
    {
        [TestCase("https://www.youtube.com/watch?v=SjFq0BmgRS4", "SjFq0BmgRS4")]
        [TestCase("https://youtu.be/eqdwetRt8tY?si=abc", "eqdwetRt8tY")]
        [TestCase("https://www.youtube.com/shorts/ZyGLBL-Ep9g", "ZyGLBL-Ep9g")]
        [TestCase("https://m.youtube.com/watch?feature=share&v=89bm458dzM0", "89bm458dzM0")]
        [TestCase("https://www.youtube.com/embed/rMlRcLSQHWY", "rMlRcLSQHWY")]
        [TestCase("https://cdn.example.com/v.mp4", null)]
        [TestCase("not a url", null)]
        [TestCase("", null)]
        public void ExtractYouTubeId(string url, string expected) => Assert.AreEqual(expected, VideoCatalog.ExtractYouTubeId(url));

        [Test]
        public void Classify_Sources()
        {
            Assert.AreEqual(VideoSourceKind.DirectUrl, VideoCatalog.Classify(new VideoEntry { playableUrl = "https://cdn.x/v.mp4" }));
            Assert.AreEqual(VideoSourceKind.LocalFile, VideoCatalog.Classify(new VideoEntry { playableUrl = "Videos/a.mp4" }));
            Assert.AreEqual(VideoSourceKind.YouTubeOnly, VideoCatalog.Classify(new VideoEntry { youtubeUrl = "https://youtu.be/SjFq0BmgRS4" }));
            Assert.AreEqual(VideoSourceKind.YouTubeOnly, VideoCatalog.Classify(new VideoEntry { playableUrl = "https://youtu.be/SjFq0BmgRS4" }));
            Assert.AreEqual(VideoSourceKind.None, VideoCatalog.Classify(new VideoEntry { playableUrl = "../../secret.mp4" }));
            Assert.AreEqual(VideoSourceKind.None, VideoCatalog.Classify(new VideoEntry()));
            Assert.AreEqual(VideoSourceKind.None, VideoCatalog.Classify(null));
            Assert.AreEqual("file:///sa/Videos/a.mp4", VideoCatalog.ResolvePlayableUrl(new VideoEntry { playableUrl = "/Videos\\a.mp4" }, "file:///sa/"));
        }

        [Test]
        public void Parse_DropsInvalidEntries_AndNeverThrows()
        {
            Assert.AreEqual(0, VideoCatalog.Parse(null).videos.Count);
            Assert.AreEqual(0, VideoCatalog.Parse("{oops").videos.Count);
            var data = VideoCatalog.Parse("{\"videos\":[{\"id\":\"a\",\"titleEn\":\"A\",\"youtubeUrl\":\"https://youtu.be/SjFq0BmgRS4\"}," +
                                          "{\"id\":\"a\",\"titleEn\":\"dup\",\"youtubeUrl\":\"https://youtu.be/SjFq0BmgRS4\"}," +
                                          "{\"id\":\"b\",\"titleEn\":\"no source\"},{\"id\":\"\",\"titleEn\":\"x\",\"playableUrl\":\"https://x/v.mp4\"}]}");
            Assert.AreEqual(1, data.videos.Count);
        }

        [Test]
        public void ShippedCatalog_IsCompleteAndPointsAtOurChannel()
        {
            VideoCatalog.ResetCache();
            var data = VideoCatalog.Data;
            Assert.AreEqual("https://www.youtube.com/@NihongoLifeGame", data.channelUrl);
            Assert.GreaterOrEqual(data.videos.Count, 10);
            var expected = new[] { "SjFq0BmgRS4", "eqdwetRt8tY", "44hg0WslsSo", "pgDTu0frRj4", "lfEIoUaOvN4", "ZyGLBL-Ep9g", "89bm458dzM0", "7byPFzoSBJw", "uPPEUCvh9Xw", "rMlRcLSQHWY" };
            CollectionAssert.IsSubsetOf(expected, data.videos.Select(v => VideoCatalog.ExtractYouTubeId(v.youtubeUrl)).ToList());
            foreach (var v in data.videos)
            {
                foreach (var s in new[] { v.titleVi, v.titleEn, v.titleJa, v.summaryVi, v.summaryEn, v.summaryJa, v.module, v.license })
                    Assert.IsFalse(string.IsNullOrWhiteSpace(s), v.id);
                if (v.license != "own") Assert.IsFalse(string.IsNullOrWhiteSpace(v.attribution), v.id + " third-party video needs attribution");
                if (VideoCatalog.Classify(v) == VideoSourceKind.LocalFile)
                    Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, v.playableUrl)), v.id + " local file missing");
            }
        }
    }

    public class VideoPlaybackStateMachineTests
    {
        private static VideoEntry Direct => new VideoEntry { id = "d", titleEn = "d", playableUrl = "https://cdn.example.com/v.mp4" };

        [Test]
        public void HappyPath_PreparePlayPauseEndReplay()
        {
            var sm = new VideoPlaybackStateMachine();
            var states = new List<VideoPlaybackState>();
            sm.Changed += states.Add;
            Assert.AreEqual(VideoSourceKind.DirectUrl, sm.Load(Direct));
            Assert.AreEqual(VideoPlaybackState.Preparing, sm.State);
            Assert.IsFalse(sm.CanSeek);
            Assert.IsTrue(sm.OnPrepared(sm.Generation));
            Assert.IsTrue(sm.TogglePause()); Assert.AreEqual(VideoPlaybackState.Paused, sm.State);
            Assert.IsTrue(sm.TogglePause()); Assert.AreEqual(VideoPlaybackState.Playing, sm.State);
            Assert.IsTrue(sm.OnEnded(sm.Generation)); Assert.AreEqual(VideoPlaybackState.Ended, sm.State);
            Assert.IsTrue(sm.CanSeek);
            Assert.IsTrue(sm.TogglePause()); Assert.AreEqual(VideoPlaybackState.Playing, sm.State);
            CollectionAssert.AreEqual(new[] { VideoPlaybackState.Preparing, VideoPlaybackState.Playing, VideoPlaybackState.Paused, VideoPlaybackState.Playing, VideoPlaybackState.Ended, VideoPlaybackState.Playing }, states);
        }

        [Test]
        public void YouTube_GoesExternalOnly()
        {
            var sm = new VideoPlaybackStateMachine();
            sm.Load(new VideoEntry { id = "y", titleEn = "y", youtubeUrl = "https://youtu.be/SjFq0BmgRS4" });
            Assert.AreEqual(VideoPlaybackState.ExternalOnly, sm.State);
            Assert.IsFalse(sm.TogglePause());
            Assert.IsFalse(sm.OnPrepared(sm.Generation));
        }

        [Test]
        public void Errors_Offline_BadUrl_NoSource_Timeout_PlayerError()
        {
            var sm = new VideoPlaybackStateMachine();
            sm.Load(Direct, networkReachable: false);
            Assert.AreEqual(VideoErrorKind.Offline, sm.Error);
            sm.Load(new VideoEntry { playableUrl = "https://bad url with spaces/v.mp4" });
            Assert.AreEqual(VideoErrorKind.BadUrl, sm.Error);
            sm.Load(new VideoEntry());
            Assert.AreEqual(VideoErrorKind.NoSource, sm.Error);
            Assert.AreEqual(VideoPlaybackState.Error, sm.State);

            sm.Load(Direct);
            sm.PrepareTimeout = 5f;
            Assert.IsFalse(sm.Tick(4f));
            Assert.IsTrue(sm.Tick(2f));
            Assert.AreEqual(VideoErrorKind.Timeout, sm.Error);

            sm.Load(Direct);
            Assert.IsTrue(sm.OnPlayerError(sm.Generation, "404"));
            Assert.AreEqual(VideoErrorKind.PrepareFailed, sm.Error);
            Assert.IsFalse(sm.OnPlayerError(sm.Generation, "again"));

            sm.Load(Direct); sm.OnPrepared(sm.Generation);
            Assert.IsTrue(sm.OnPlayerError(sm.Generation, "decode"));
            Assert.AreEqual(VideoErrorKind.Decode, sm.Error);
        }

        [Test]
        public void StaleCallbacks_AfterCloseOrReload_AreIgnored()
        {
            var sm = new VideoPlaybackStateMachine();
            sm.Load(Direct);
            int old = sm.Generation;
            sm.Close();
            Assert.AreEqual(VideoPlaybackState.Idle, sm.State);
            Assert.IsFalse(sm.OnPrepared(old));
            Assert.IsFalse(sm.OnPlayerError(old, "late"));
            Assert.IsFalse(sm.OnEnded(old));
            sm.Load(Direct);
            Assert.IsFalse(sm.OnPrepared(old));
            Assert.AreEqual(VideoPlaybackState.Preparing, sm.State);
        }

        [Test]
        public void Helpers_ClampAndFormat()
        {
            Assert.AreEqual(0f, VideoPlaybackStateMachine.ClampVolume(-2f));
            Assert.AreEqual(1f, VideoPlaybackStateMachine.ClampVolume(9f));
            Assert.AreEqual(1f, VideoPlaybackStateMachine.ClampVolume(float.NaN));
            Assert.AreEqual(50.0, VideoPlaybackStateMachine.SeekTime(0.5f, 100), 1e-6);
            Assert.AreEqual(0.0, VideoPlaybackStateMachine.SeekTime(0.5f, 0));
            Assert.AreEqual(100.0, VideoPlaybackStateMachine.SeekTime(3f, 100), 1e-6);
            Assert.AreEqual("0:00", VideoPlaybackStateMachine.FormatTime(double.NaN));
            Assert.AreEqual("4:45", VideoPlaybackStateMachine.FormatTime(285));
            Assert.AreEqual("1:01:01", VideoPlaybackStateMachine.FormatTime(3661));
        }
    }
}
