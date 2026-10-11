using System;
using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The name on the HUD is the character's name, never the "Guest" placeholder of a guest save: signed in with a
    /// profile name, that name shows; a name chosen in character select wins over it. Fake auth, no network.
    /// </summary>
    public class HudPlayerNamePlayModeTests
    {
        private sealed class SignedIn : IAuthService
        {
            public bool IsAuthenticated => true;
            public string UserId => "a8a57662-test";
            public string AccessToken => "";
            public string DisplayName => "Long Quách";
            public string Email => "";
#pragma warning disable 67
            public event Action<AuthState> OnAuthStateChanged;
#pragma warning restore 67
            public void Initialize() { }
            public void SignInAnonymously(Action<bool, string> callback) { }
            public void SignInWithEmail(string email, string password, Action<bool, string> callback) { }
            public void SignUpWithEmail(string email, string password, string displayName, Action<bool, string> callback) { }
            public void SignOut() { }
            public void TryRestoreSession(Action<bool> callback) { }
        }

        [UnityTest]
        public IEnumerator Hud_SignedIn_ShowsProfileNameNotGuest() => Run("Guest", "Long Quách");

        [UnityTest]
        public IEnumerator Hud_ChosenCharacterName_Wins() => Run("Mai", "Mai");

        private static IEnumerator Run(string savedPrefsName, string expected)
        {
            string originalName = PlayerPrefs.GetString(PlayableCharacterCatalog.PlayerNamePrefsKey, PlayableCharacterCatalog.DefaultPlayerName);
            GameServices.TryGet(out IAuthService originalAuth);
            var originalMode = PlayerSessionService.Instance != null ? PlayerSessionService.Instance.Mode : PlayerSessionMode.Guest;
            try
            {
                // The state behind the bug: a guest session ("Guest" save) although an account is signed in, and the
                // stored name polluted with "Guest".
                PlayerPrefs.SetString(PlayableCharacterCatalog.PlayerNamePrefsKey, savedPrefsName);
                GameServices.Register<IAuthService>(new SignedIn());
                PlayerSessionService.GetOrCreate()?.BeginGuest();
                yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || PlayerStatus.Instance == null || t < 2f); t += Time.unscaledDeltaTime) yield return null;
                GameServices.Register<IAuthService>(new SignedIn()); // the scene's bootstrap may have registered its own
                // The save carries the "Guest" placeholder (what guest play writes into progress.json).
                string savedDisplayName = null;
                if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repository) && repository.GetProgress() != null)
                {
                    savedDisplayName = repository.GetProgress().displayName;
                    repository.GetProgress().displayName = "Guest";
                }
                PlayerStatus.Instance.ReloadProgress();
                var dm = DialogueManager.Instance;
                for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
                yield return new WaitForSecondsRealtime(0.8f);
                var dock = Object.FindFirstObjectByType<StatusDock>();
                var label = dock.StatusWidget.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "Name");
                Debug.Log($"[HudName] prefs='{savedPrefsName}' → HUD '{label.text}', PlayerStatus '{PlayerStatus.Instance.PlayerName}'");
                Assert.AreEqual(expected, PlayerStatus.Instance.PlayerName);
                Assert.AreEqual(expected, label.text, "HUD name");
                Assert.AreNotEqual("Guest", label.text);
                if (repository != null && repository.GetProgress() != null) repository.GetProgress().displayName = savedDisplayName;
            }
            finally
            {
                PlayerPrefs.SetString(PlayableCharacterCatalog.PlayerNamePrefsKey, originalName);
                PlayerPrefs.Save();
                if (originalAuth != null) GameServices.Register(originalAuth);
                // Leave the session as it was: an account session left behind would make later tests write to the save.
                if (originalMode == PlayerSessionMode.Guest) PlayerSessionService.Instance?.BeginGuest();
            }
        }
    }
}
