using System.IO;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Exam;
using NihongoLife.NPC;
using NihongoLife.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>Regression tests for the AI cache, the atomic save, ungraded AI answers and the scripted NPC dialogue.</summary>
    public class AuditFixesTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "nl_tests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // ── AI cache ──

        [Test]
        public void Cache_InvalidHit_IsDeletedAndMissed()
        {
            string path = Path.Combine(_dir, "a.txt");
            File.WriteAllText(path, "{\"criteria\":[{\"code\":\"TA\""); // truncated reply
            LogAssert.ignoreFailingMessages = true;
            bool hit = ExamGradingService.TryReadCache(path, t => t.TrimEnd().EndsWith("}"), out string text);
            LogAssert.ignoreFailingMessages = false;
            Assert.IsFalse(hit);
            Assert.IsNull(text);
            Assert.IsFalse(File.Exists(path), "a bad cache file is removed so it is fetched again");
        }

        [Test]
        public void Cache_ValidHit_IsReturned()
        {
            string path = Path.Combine(_dir, "b.txt");
            File.WriteAllText(path, "{\"ok\":1}");
            Assert.IsTrue(ExamGradingService.TryReadCache(path, t => t.Contains("ok"), out string text));
            Assert.AreEqual("{\"ok\":1}", text);
            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public void Cache_OnlyCompleteParseableRepliesAreCached()
        {
            Assert.IsTrue(ExamGradingService.ShouldCache("{}", "STOP", t => true));
            Assert.IsFalse(ExamGradingService.ShouldCache("{\"a\":", "MAX_TOKENS", t => true), "truncated reply");
            Assert.IsFalse(ExamGradingService.ShouldCache("{}", null, t => true), "no finish reason");
            Assert.IsFalse(ExamGradingService.ShouldCache("oops", "STOP", t => false), "unparseable reply");
        }

        [Test]
        public void Cache_OldVersionIsPurgedOnce()
        {
            string cache = Path.Combine(_dir, "ai_cache");
            Directory.CreateDirectory(cache);
            File.WriteAllText(Path.Combine(cache, "old.txt"), "truncated");
            ExamGradingService.PurgeCacheIfOutdated(cache);
            Assert.IsFalse(File.Exists(Path.Combine(cache, "old.txt")));
            Assert.AreEqual(ExamGradingService.CacheVersion, File.ReadAllText(Path.Combine(cache, "cache_version.txt")).Trim());

            File.WriteAllText(Path.Combine(cache, "new.txt"), "{}");
            ExamGradingService.PurgeCacheIfOutdated(cache);
            Assert.IsTrue(File.Exists(Path.Combine(cache, "new.txt")), "current-version files are kept");
        }

        // ── Save ──

        [Test]
        public void Save_WriteAtomic_KeepsPreviousAsBackup()
        {
            string path = Path.Combine(_dir, "progress.json");
            LocalProgressRepository.WriteAtomic(path, JsonUtility.ToJson(new PlayerProgressDto { yen = 100 }));
            LocalProgressRepository.WriteAtomic(path, JsonUtility.ToJson(new PlayerProgressDto { yen = 200 }));
            Assert.IsFalse(File.Exists(path + ".tmp"));
            Assert.AreEqual(200, JsonUtility.FromJson<PlayerProgressDto>(File.ReadAllText(path)).yen);
            Assert.AreEqual(100, JsonUtility.FromJson<PlayerProgressDto>(File.ReadAllText(path + ".bak")).yen);
        }

        [Test]
        public void Save_CorruptMain_RecoversFromBackup()
        {
            string path = Path.Combine(_dir, "progress.json");
            File.WriteAllText(path + ".bak", JsonUtility.ToJson(new PlayerProgressDto { yen = 777 }));
            File.WriteAllText(path, "{\"yen\": 12, \"broken");
            LogAssert.ignoreFailingMessages = true;
            var loaded = LocalProgressRepository.ReadWithBackup(path, out bool fromBackup);
            LogAssert.ignoreFailingMessages = false;
            Assert.IsNotNull(loaded);
            Assert.IsTrue(fromBackup);
            Assert.AreEqual(777, loaded.yen);
        }

        [Test]
        public void Save_EmptyMain_RecoversFromBackup()
        {
            string path = Path.Combine(_dir, "progress.json");
            File.WriteAllText(path + ".bak", JsonUtility.ToJson(new PlayerProgressDto { yen = 5 }));
            File.WriteAllText(path, "");
            var loaded = LocalProgressRepository.ReadWithBackup(path, out bool fromBackup);
            Assert.IsTrue(fromBackup);
            Assert.AreEqual(5, loaded.yen);
        }

        // ── Scoring ──

        [Test]
        public void Scoring_NullGrade_CountsAsZeroNotSkipped()
        {
            var ungraded = ExamManager.OrUngraded(null);
            Assert.IsNotNull(ungraded);
            Assert.AreEqual(0f, ungraded.band);
            Assert.IsFalse(string.IsNullOrWhiteSpace(ungraded.feedback));
            float average = (ExamManager.OrUngraded(new ExamAiGrade { band = 8f }).band + ungraded.band) / 2f;
            Assert.AreEqual(4f, average, "an ungraded answer lowers the section, it is not left out");
        }

        [Test]
        public void Timeouts_ClientWaitsLongerThanHttp()
        {
            Assert.Greater(AiGradingTimeouts.ClientWaitSeconds(false), AiGradingTimeouts.HttpSeconds(false) * 2);
            Assert.Greater(AiGradingTimeouts.ClientWaitSeconds(true), AiGradingTimeouts.HttpSeconds(true) * 2);
            Assert.Greater(AiGradingTimeouts.AudioSeconds, AiGradingTimeouts.TextSeconds);
        }

        // ── Scripted NPC dialogue ──

        private static void AssertComplete(string owner, ScriptedNpcLine line)
        {
            Assert.IsNotNull(line, owner);
            Assert.IsFalse(string.IsNullOrWhiteSpace(line.Ja), owner + " ja");
            Assert.IsFalse(string.IsNullOrWhiteSpace(line.Reading), owner + " reading");
            Assert.IsFalse(string.IsNullOrWhiteSpace(line.Romaji), owner + " romaji");
            Assert.IsFalse(string.IsNullOrWhiteSpace(line.Vi), owner + " vi");
            Assert.IsFalse(string.IsNullOrWhiteSpace(line.En), owner + " en");
            Assert.AreNotEqual(line.Vi, line.En, owner + ": the Vietnamese line must not be an English copy");
        }

        [Test]
        public void Npc_EveryLineHasEveryLanguage()
        {
            foreach (var line in ScriptedNpcDialogue.Generic) AssertComplete("generic", line);
            foreach (var pair in ScriptedNpcDialogue.ByNpc)
            {
                Assert.IsNotEmpty(pair.Value, pair.Key);
                foreach (var line in pair.Value) AssertComplete(pair.Key, line);
            }
        }

        [Test]
        public void Npc_AlwaysReturnsALine_InEveryLanguage()
        {
            var before = Loc.Language;
            try
            {
                foreach (string id in new[] { "npc_cashier", "npc_unknown_xyz", "", null })
                    for (int i = 0; i < 5; i++)
                    {
                        var node = ScriptedNpcDialogue.NextNode(id, "Test");
                        Assert.IsFalse(string.IsNullOrWhiteSpace(node.textJa));
                        Loc.Set(GameLanguage.Vietnamese);
                        Assert.AreEqual(node.textVi, node.Gloss);
                        Loc.Set(GameLanguage.English);
                        Assert.AreEqual(node.textEn, node.Gloss);
                        Loc.Set(GameLanguage.Japanese);
                        Assert.AreEqual("", node.Gloss, "Japanese interface shows the Japanese line itself");
                    }
            }
            finally { Loc.Set(before); }
        }
    }
}
