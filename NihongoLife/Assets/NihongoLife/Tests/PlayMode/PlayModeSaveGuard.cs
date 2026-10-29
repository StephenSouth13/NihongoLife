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
            NihongoLife.Core.IntroTrailer.Bypass = true; // starting a game in a test never plays the intro trailer
            NihongoLife.Health.PlayerIllness.Triggers = false; // nobody falls ill by chance in other tests (IllnessPlayModeTests turns it on)
            NihongoLife.Player.CharacterLook.Persist = false; // the look panel never writes the device's look in tests
            NihongoLife.Progression.CareerClock.PromptEnabled = false; // the level-10 career card only where a test checks it
            NihongoLife.Scenario.StoryDirector.Bypass = true; // StoryFlowPlayModeTests switches the story back on
            NihongoLife.Core.PlayerSessionService.PersistGuest = false; // tests never read or write the real guest save
            NihongoLife.Core.PlayerSessionService.GuestFullAccess = false; // tests run guest sessions but check the campaign gates
            _ledger = PlayerPrefs.GetString("NL.WalletLedger.v1", "");
            // Tests read Vietnamese text; the player's own interface language is put back afterwards.
            _language = PlayerPrefs.GetInt(NihongoLife.Core.Loc.PrefsKey, 0);
            NihongoLife.Core.Loc.Set(NihongoLife.Core.GameLanguage.Vietnamese);
            _accountSaves = new System.Collections.Generic.HashSet<string>(Directory.GetFiles(Application.persistentDataPath, "progress_*.json"));
            _hadSave = File.Exists(SavePath);
            if (_hadSave) File.Copy(SavePath, BackupPath, true);
        }

        private System.Collections.Generic.HashSet<string> _accountSaves;

        [OneTimeTearDown]
        public void RestoreSave()
        {
            NihongoLife.Core.LocationUnlocks.Bypass = false;
            NihongoLife.Core.IntroTrailer.Bypass = false;
            NihongoLife.Progression.CareerClock.PromptEnabled = true;
            NihongoLife.Player.CharacterLook.Persist = true;
            NihongoLife.Scenario.StoryDirector.Bypass = false;
            NihongoLife.Core.Loc.Set((NihongoLife.Core.GameLanguage)Mathf.Clamp(_language, 0, 3));
            PlayerPrefs.SetString("NL.WalletLedger.v1", _ledger ?? ""); // tests' purchases do not stay in the player's wallet history
            if (_hadSave && File.Exists(BackupPath)) File.Copy(BackupPath, SavePath, true);
            else if (!_hadSave && File.Exists(SavePath)) File.Delete(SavePath);
            // Per-account saves made by tests' fake accounts go; the player's own account saves stay untouched.
            foreach (var file in Directory.GetFiles(Application.persistentDataPath, "progress_*.json"))
                if (_accountSaves == null || !_accountSaves.Contains(file)) File.Delete(file);
            NihongoLife.Core.PlayerSessionService.GuestFullAccess = true;
            NihongoLife.Core.PlayerSessionService.PersistGuest = true;
        }
    }
}
