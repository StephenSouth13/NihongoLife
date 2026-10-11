using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The editor restores the developer's real signed-in session, so gameplay tests run as an account and write the
    /// local save. Every PlayMode run keeps the real progress.json: copied before the first test, put back after the last.
    /// </summary>
    [SetUpFixture]
    public class PlayModeSaveGuard
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "progress.json");
        private static string BackupPath => Path.Combine(Application.temporaryCachePath, "progress.before-tests.json");
        private bool _hadSave;

        [OneTimeSetUp]
        public void KeepSave()
        {
            _hadSave = File.Exists(SavePath);
            if (_hadSave) File.Copy(SavePath, BackupPath, true);
        }

        [OneTimeTearDown]
        public void RestoreSave()
        {
            if (_hadSave && File.Exists(BackupPath)) File.Copy(BackupPath, SavePath, true);
            else if (!_hadSave && File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
