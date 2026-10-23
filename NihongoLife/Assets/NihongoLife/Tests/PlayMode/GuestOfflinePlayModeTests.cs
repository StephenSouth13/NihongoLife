using System;
using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Save;
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
    /// Guests play the whole game offline with a save kept on this device; online (the online world, friends, chat,
    /// co-op, voice, ranking) is for a registered account that is signed in. "Play online" and "Friends" on the
    /// menu open the account panel with the reason for a guest, and go through for a registered account. An
    /// anonymous session (no email) counts as a guest. Uses a fake auth service, so no network.
    /// Captures: Bao_Cao/guest-online.
    /// </summary>
    public class GuestOfflinePlayModeTests
    {
        private sealed class FakeAuth : IAuthService
        {
            public bool IsAuthenticated { get; set; }
            public string UserId => IsAuthenticated ? "guest-online-test" : "";
            public string AccessToken => "";
            public string DisplayName => "Long Quách";
            public string Email { get; set; } = "";
            public event Action<AuthState> OnAuthStateChanged;
            public void Initialize() { }
            public void SignInAnonymously(Action<bool, string> callback) => callback(false, "n/a");
            public void SignInWithEmail(string email, string password, Action<bool, string> callback) => callback(false, "n/a");
            public void SignUpWithEmail(string email, string password, string displayName, Action<bool, string> callback) => callback(false, "n/a");
            public void SignOut() { IsAuthenticated = false; OnAuthStateChanged?.Invoke(AuthState.SignedOut); }
            public void TryRestoreSession(Action<bool> callback) => callback(false);
        }

        [UnityTest]
        public IEnumerator Guest_PlaysOfflineWithSave_OnlineNeedsRegisteredAccount()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            for (float t = 0f; t < 15f && Object.FindFirstObjectByType<MainMenuUI>() == null; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            GameServices.TryGet(out IAuthService original);
            var fake = new FakeAuth();
            GameServices.Register<IAuthService>(fake);
            var session = PlayerSessionService.GetOrCreate();
            string backup = File.Exists(PlayerSessionService.GuestSavePath) ? File.ReadAllText(PlayerSessionService.GuestSavePath) : null;
            try
            {
                var authUi = Object.FindFirstObjectByType<AuthUI>();
                var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var online = buttons.First(b => b.name == "OnlineBtn");
                var friends = buttons.First(b => b.name == "FriendsBtn" || b.name == "FriendsButton");

                // 1. Guest: online and friends open the account panel with the reason.
                Assert.IsFalse(OnlineAccess.IsRegistered);
                online.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.IsTrue(authUi.IsOpen, "Play online asks a guest to register / sign in.");
                string status = authUi.GetComponentsInChildren<TextMeshProUGUI>(false).First(t => t.name == "StatusText").text;
                StringAssert.Contains("Khách", status);
                StringAssert.Contains("Đăng ký", status);
                Capture("01_guest_online_prompt");
                authUi.Hide();
                friends.onClick.Invoke();
                yield return null;
                Assert.IsTrue(authUi.IsOpen, "Friends asks a guest to register / sign in.");
                authUi.Hide();

                // 1b. Offline guest: the whole town is open (no knowledge gates, no forced story), however the town was
                // started; an account session plays the roadmap.
                PlayerSessionService.GuestFullAccess = true;
                LocationUnlocks.Bypass = false;
                try
                {
                    session.BeginGuest();
                    Assert.IsTrue(PlayerSessionService.OpenWorld);
                    Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.SushiRestaurantScene), "Guests have the whole map.");
                    Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.MidoriIslandScene), "Guests have the whole map.");
                    Assert.IsFalse(NihongoLife.Scenario.StoryDirector.GatesFreePlay, "Guests are not held in the opening story.");
                    session.BeginAccount("open-world-test", "Long");
                    Assert.IsFalse(PlayerSessionService.OpenWorld, "An account plays the campaign.");
                    PlayerStatus.Instance?.DebugSetKnowledge(0);
                    Assert.IsFalse(LocationUnlocks.IsUnlocked(WorldLocationCatalog.MidoriIslandScene), "An account opens the map by knowledge and story.");
                }
                finally
                {
                    session.BeginGuest();
                    PlayerSessionService.GuestFullAccess = false;
                    LocationUnlocks.Bypass = true;
                }

                // 2. An anonymous session (no email) is still a guest.
                fake.IsAuthenticated = true;
                Assert.IsFalse(OnlineAccess.IsRegistered, "An anonymous session does not go online.");

                // 3. Registered and signed in: online goes through (character, then the lobby), no account panel.
                fake.Email = "long@example.test";
                Assert.IsTrue(OnlineAccess.IsRegistered);
                online.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.IsFalse(authUi.IsOpen, "A registered account is not stopped.");
                Capture("02_registered_online");
                UiModalStack.CloseTop();
                UiModalStack.CloseTop();

                // 4. A guest's progress is kept on the device and is ready to move into a new account.
                fake.IsAuthenticated = false; fake.Email = "";
                PlayerSessionService.PersistGuest = true;
                if (File.Exists(PlayerSessionService.GuestSavePath)) File.Delete(PlayerSessionService.GuestSavePath);
                session.BeginGuest();
                Assert.IsNull(session.GuestProgressToCarry(), "A fresh guest has nothing to carry.");
                var repo = GameServices.Get<IProgressRepository>();
                var progress = repo.GetProgress();
                progress.knowledge = 120;
                progress.completedScenarios.Add("scenario.intro.arrival");
                progress.storyFlags.Add("map." + WorldLocationCatalog.HomeBedroomScene);
                repo.SaveProgress(progress);
                Assert.IsTrue(File.Exists(PlayerSessionService.GuestSavePath), "The guest save is written to the device.");
                session.BeginGuest(); // next launch
                var reloaded = repo.GetProgress();
                Assert.AreEqual(120, reloaded.knowledge, "Guest knowledge survives a restart.");
                CollectionAssert.Contains(reloaded.completedScenarios, "scenario.intro.arrival");
                Assert.NotNull(session.GuestProgressToCarry(), "Played guest progress can move into a new account.");
                session.ClearGuestProgress();
                Assert.IsFalse(File.Exists(PlayerSessionService.GuestSavePath));
                Assert.AreEqual(0, repo.GetProgress().knowledge, "After moving into an account the next guest starts fresh.");
            }
            finally
            {
                PlayerSessionService.PersistGuest = true;
                if (backup != null) File.WriteAllText(PlayerSessionService.GuestSavePath, backup);
                else if (File.Exists(PlayerSessionService.GuestSavePath)) File.Delete(PlayerSessionService.GuestSavePath);
                PlayerSessionService.PersistGuest = false;
                session.BeginGuest();
                if (original != null) GameServices.Register(original); else GameServices.Unregister<IAuthService>();
            }
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/guest-online"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); rt.Release();
        }
    }
}
