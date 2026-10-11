using System;
using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The account panel on the main menu: drawn above the menu buttons, a way out (×, Esc, Continue) and it
    /// closes by itself after signing in — the player is never left stuck on "Signed in as". Uses a fake auth
    /// service, so no network. Capture: Bao_Cao/ui-regression/10_account_signed_in.png.
    /// </summary>
    public class AuthPanelPlayModeTests
    {
        private sealed class FakeAuth : IAuthService
        {
            public bool IsAuthenticated { get; private set; }
            public string UserId => IsAuthenticated ? "a8a57662-4400-test" : "";
            public string AccessToken => "";
            public string DisplayName => "Long Quách";
            public string Email => "test@example.com";
            public event Action<AuthState> OnAuthStateChanged;
            public void Initialize() { }
            public void SignInAnonymously(Action<bool, string> callback) => callback(false, "n/a");
            public void SignInWithEmail(string email, string password, Action<bool, string> callback)
            {
                IsAuthenticated = true;
                OnAuthStateChanged?.Invoke(AuthState.SignedIn);
                callback(true, null);
            }
            public void SignUpWithEmail(string email, string password, string displayName, Action<bool, string> callback) => callback(false, "n/a");
            public void SignOut() { IsAuthenticated = false; OnAuthStateChanged?.Invoke(AuthState.SignedOut); }
            public void TryRestoreSession(Action<bool> callback) => callback(false);
        }

        [UnityTest]
        public IEnumerator Account_SignInClosesAndPanelHasWayOut()
        {
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            for (float t = 0f; t < 15f && Object.FindFirstObjectByType<AuthUI>() == null; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            GameServices.TryGet(out IAuthService original);
            string originalName = PlayerPrefs.GetString(PlayableCharacterCatalog.PlayerNamePrefsKey, PlayableCharacterCatalog.DefaultPlayerName);
            var originalMode = PlayerSessionService.Instance != null ? PlayerSessionService.Instance.Mode : PlayerSessionMode.Guest;
            var fake = new FakeAuth();
            GameServices.Register<IAuthService>(fake);
            try
            {
                var auth = Object.FindFirstObjectByType<AuthUI>();
                Assert.NotNull(auth, "AuthUI on the main menu");
                auth.Show();
                yield return null;
                var panel = auth.transform.Find("AuthPanel");
                Assert.AreEqual(panel.parent.childCount - 1, panel.GetSiblingIndex(), "The account panel draws above every menu button");
                Assert.IsTrue(panel.GetComponentsInChildren<Button>(false).Any(b => b.name == "CloseX"), "× button");
                Assert.IsTrue(UiModalStack.CloseTop(), "Esc closes the panel");
                Assert.IsFalse(auth.IsOpen);

                auth.Show();
                yield return null;
                panel.GetComponentsInChildren<TMP_InputField>(false).First(i => i.name == "EmailInput").text = "test@example.com";
                panel.GetComponentsInChildren<TMP_InputField>(false).First(i => i.name == "PasswordInput").text = "secret";
                panel.GetComponentsInChildren<Button>(false).First(b => b.name == "SignInButton").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsTrue(auth.IsOpen, "The success message is shown first");
                var buttons = panel.GetComponentsInChildren<Button>(false).Select(b => b.name).ToList();
                CollectionAssert.IsSubsetOf(new[] { "ContinueButton", "LogoutButton", "CloseX" }, buttons);
                Capture("10_account_signed_in");
                yield return new WaitForSecondsRealtime(1.3f);
                Assert.IsFalse(auth.IsOpen, "The panel closes by itself after signing in");

                auth.Show();
                yield return null;
                panel.GetComponentsInChildren<Button>(false).First(b => b.name == "ContinueButton").onClick.Invoke();
                Assert.IsFalse(auth.IsOpen, "Continue returns to the menu");
            }
            finally
            {
                if (original != null) GameServices.Register(original);
                // Signing in switched the session to the fake account and copied its name: put both back.
                PlayerPrefs.SetString(PlayableCharacterCatalog.PlayerNamePrefsKey, originalName);
                PlayerPrefs.Save();
                if (originalMode == PlayerSessionMode.Guest) PlayerSessionService.Instance?.BeginGuest();
            }
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var target = new RenderTexture(1600, 900, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var shot = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            shot.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/ui-regression"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), shot.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(shot);
            target.Release();
            Object.Destroy(target);
        }
    }
}
