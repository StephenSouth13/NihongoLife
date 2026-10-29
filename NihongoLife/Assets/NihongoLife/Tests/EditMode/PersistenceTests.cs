using System.IO;
using NihongoLife.Data;
using NihongoLife.Save;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests.EditMode
{
    /// <summary>G1: versioned, atomic offline saves with a backup slot, and the guard that flushes them.</summary>
    public class PersistenceTests
    {
        private string _dir;

        [SetUp] public void SetUp() { _dir = Path.Combine(Path.GetTempPath(), "nl_persist_" + System.Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_dir); }
        [TearDown] public void TearDown() { try { Directory.Delete(_dir, true); } catch { } }

        private static PlayerProgressDto Sample(int yen) => new PlayerProgressDto { displayName = "Test", yen = yen, xp = 42 };

        [Test]
        public void Save_IsVersioned_AndRoundTrips()
        {
            string path = Path.Combine(_dir, "guest.json");
            ProgressFile.Write(path, Sample(777));
            var back = ProgressFile.Read(path, out bool fromBackup);
            Assert.IsFalse(fromBackup);
            Assert.AreEqual(777, back.yen);
            Assert.AreEqual(ProgressFile.CurrentVersion, back.saveVersion);
            Assert.IsNotEmpty(back.savedAtUtc);
            Assert.IsFalse(File.Exists(path + ".tmp"), "No half-written temp file is left behind.");
        }

        [Test]
        public void SecondWrite_KeepsThePreviousSave_AsBackup()
        {
            string path = Path.Combine(_dir, "guest.json");
            ProgressFile.Write(path, Sample(100));
            ProgressFile.Write(path, Sample(200));
            Assert.IsTrue(File.Exists(path + ".bak"));
            Assert.AreEqual(100, JsonUtility.FromJson<PlayerProgressDto>(File.ReadAllText(path + ".bak")).yen);
            Assert.AreEqual(200, ProgressFile.Read(path, out _).yen);
        }

        [Test]
        public void CorruptSave_RestoresTheBackup_AndKeepsTheBrokenFile()
        {
            string path = Path.Combine(_dir, "guest.json");
            ProgressFile.Write(path, Sample(100));
            ProgressFile.Write(path, Sample(200));
            File.WriteAllText(path, "{ this is not json");          // a crash / bad write
            var back = ProgressFile.Read(path, out bool fromBackup);
            Assert.IsTrue(fromBackup, "Falls back to the backup slot.");
            Assert.AreEqual(100, back.yen);
            Assert.IsNotEmpty(Directory.GetFiles(_dir, "guest.json.corrupt-*"), "The broken file is kept, never deleted.");
        }

        [Test]
        public void OldUnversionedSave_IsMigrated_AndNullListsRepaired()
        {
            string path = Path.Combine(_dir, "old.json");
            File.WriteAllText(path, "{\"playerId\":\"guest\",\"yen\":55,\"completedScenarios\":null}");
            var back = ProgressFile.Read(path, out _);
            Assert.AreEqual(55, back.yen);
            Assert.AreEqual(ProgressFile.CurrentVersion, back.saveVersion);
            Assert.NotNull(back.completedScenarios);
            Assert.NotNull(back.inventory);
        }

        [Test]
        public void MissingSave_IsNull_NotAnError()
        {
            Assert.IsNull(ProgressFile.Read(Path.Combine(_dir, "none.json"), out bool fromBackup));
            Assert.IsFalse(fromBackup);
        }
    }
}