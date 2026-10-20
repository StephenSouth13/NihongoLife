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
        private string _ledger;
        private int _language;

        [OneTimeSetUp]
        public void KeepSave()
        {
            // Most tests walk through every place; LocationUnlocksPlayModeTests switches the gate back on.
            NihongoLife.Core.LocationUnlocks.Bypass = true;
            _ledger = PlayerPrefs.GetString("NL.WalletLedger.v1", "");
            // Tests read Vietnamese text; the player's own interface language is put back afterwards.
            _language = PlayerPrefs.GetInt(NihongoLife.Core.Loc.PrefsKey, 0);
            NihongoLife.Core.Loc.Set(NihongoLife.Core.GameLanguage.Vietnamese);
            _hadSave = File.Exists(SavePath);
            if (_hadSave) File.Copy(SavePath, BackupPath, true);
        }

        [OneTimeTearDown]
        public void RestoreSave()
        {
            NihongoLife.Core.LocationUnlocks.Bypass = false;
            NihongoLife.Core.Loc.Set((NihongoLife.Core.GameLanguage)Mathf.Clamp(_language, 0, 2));
            PlayerPrefs.SetString("NL.WalletLedger.v1", _ledger ?? ""); // tests' purchases do not stay in the player's wallet history
            if (_hadSave && File.Exists(BackupPath)) File.Copy(BackupPath, SavePath, true);
            else if (!_hadSave && File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
