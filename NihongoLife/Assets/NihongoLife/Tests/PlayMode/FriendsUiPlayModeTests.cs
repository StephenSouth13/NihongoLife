using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    public sealed class FriendsUiPlayModeTests
    {
        private sealed class SignedInAuth : IAuthService
        {
            public bool IsAuthenticated => true;
            public string UserId => "81e76c52-codex-nihongolife-player";
            public string AccessToken => "";
            public string DisplayName => "Haruto";
            public string Email => "haruto@example.test";
            public event Action<AuthState> OnAuthStateChanged { add { } remove { } }
            public void Initialize() { }
            public void SignInAnonymously(Action<bool, string> callback) => callback?.Invoke(true, null);
            public void SignInWithEmail(string email, string password, Action<bool, string> callback) => callback?.Invoke(true, null);
            public void SignUpWithEmail(string email, string password, string displayName, Action<bool, string> callback) => callback?.Invoke(true, null);
            public void SignOut() { }
            public void TryRestoreSession(Action<bool> callback) => callback?.Invoke(true);
        }

        [UnityTest]
        public IEnumerator FriendsWindow_ThreeLanguagesAndResolutions()
        {
            GameServices.Clear();
            GameServices.Register<IAuthService>(new SignedInAuth());
            var settingsGo = new GameObject("FriendsTestSettings");
            var settings = settingsGo.AddComponent<GameSettingsService>();
            settings.Initialize();
            GameServices.Register(settings);

            var serviceGo = new GameObject("FriendsTestService");
            var service = serviceGo.AddComponent<FriendService>();
            service.Initialize();
            var friends = new List<FriendEntry>
            {
                new FriendEntry { id = "f1", requester_id = "local", addressee_id = "aiko", status = "accepted", friendUserId = "aiko-7f31a9", friendDisplayName = "Aiko Tanaka", friendLevel = 7, islandLikes = 12, islandChecked = true },
                new FriendEntry { id = "f2", requester_id = "local", addressee_id = "ren", status = "accepted", friendUserId = "ren-91d2c4", friendDisplayName = "Ren Sato", friendLevel = 3, islandChecked = true },
                new FriendEntry { id = "f3", requester_id = "local", addressee_id = "mai", status = "accepted", friendUserId = "mai-55a0e1", friendDisplayName = "Mai Nguyen", friendLevel = 12, islandLikes = 0, islandChecked = true },
            };
            var requests = new List<FriendEntry> { new FriendEntry { id = "r1", requester_id = "kenji", addressee_id = "local", status = "pending", friendUserId = "kenji-0b77", friendDisplayName = "Kenji" } };
            service.SetForTests(friends, requests);

            var canvasGo = new GameObject("FriendsTestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            var cameraGo = new GameObject("FriendsTestCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.065f, 1f);
            camera.orthographic = true;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var ui = canvasGo.AddComponent<FriendsUI>();
            ui.Initialize(UiKit.Font);
            ui.Show();
            yield return null;

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Bao_Cao", "friends-ui"));
            Directory.CreateDirectory(folder);
            var languages = new[] { GameLanguage.Vietnamese, GameLanguage.English, GameLanguage.Japanese };
            var codes = new[] { "vi", "en", "ja" };
            var sizes = new[] { new Vector2Int(1920, 1080), new Vector2Int(1366, 768), new Vector2Int(1024, 600) };
            for (int language = 0; language < languages.Length; language++)
            {
                Loc.Set(languages[language]);
                yield return null;
                for (int size = 0; size < sizes.Length; size++)
                {
                    Screen.SetResolution(sizes[size].x, sizes[size].y, false);
                    yield return null;
                    string file = Path.Combine(folder, $"friends_{codes[language]}_{sizes[size].x}x{sizes[size].y}.png");
                    Capture(camera, file, sizes[size].x, sizes[size].y);
                    Assert.IsTrue(File.Exists(file), "Screenshot was not written: " + file);
                    Assert.Greater(new FileInfo(file).Length, 1000, "Screenshot is unexpectedly empty: " + file);
                }
            }

            Assert.NotNull(GameObject.Find("FriendsWindow"));
            Assert.AreEqual(3, service.Friends.Count);

            // Rows: island info, a friend without an island cannot be visited, a failed visit says why in the row.
            Loc.Set(GameLanguage.Vietnamese);
            Screen.SetResolution(1920, 1080, false);
            yield return null;
            var aiko = GameObject.Find("Friend_f1").transform;
            var ren = GameObject.Find("Friend_f2").transform;
            StringAssert.Contains("12", aiko.Find("Island").GetComponent<TMPro.TMP_Text>().text);
            Assert.IsFalse(Find(ren, "Visit").interactable, "No island published: nothing to visit.");
            NihongoLife.Island.IslandShowcaseService.FetchOverride = _ => null;
            Find(aiko, "Visit").onClick.Invoke();
            yield return null;
            NihongoLife.Island.IslandShowcaseService.FetchOverride = null;
            var rowStatus = GameObject.Find("Friend_f1").transform.Find("RowStatus");
            Assert.NotNull(rowStatus, "A failed visit is explained in the row.");
            Capture(camera, Path.Combine(folder, "friends_vi_rows_visit_error.png"), 1920, 1080);

            Find(GameObject.Find("FriendsWindow").transform, "Requests").onClick.Invoke();
            yield return null;
            Assert.NotNull(GameObject.Find("Request_r1"), "Requests tab lists the pending request.");
            Capture(camera, Path.Combine(folder, "friends_vi_requests.png"), 1920, 1080);
            Find(GameObject.Find("FriendsWindow").transform, "Add").onClick.Invoke();
            yield return null;
            Assert.NotNull(GameObject.Find("OwnId"), "Add tab shows the own User ID.");
            Capture(camera, Path.Combine(folder, "friends_vi_add.png"), 1920, 1080);
            UnityEngine.Object.Destroy(canvasGo);
            UnityEngine.Object.Destroy(cameraGo);
            UnityEngine.Object.Destroy(serviceGo);
            UnityEngine.Object.Destroy(settingsGo);
            GameServices.Clear();
        }

        private static Button Find(Transform root, string name)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true)) if (b.name == name) return b;
            Assert.Fail("Button " + name + " under " + root.name);
            return null;
        }

        private static void Capture(Camera camera, string file, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(file, texture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.Destroy(texture);
            target.Release();
            UnityEngine.Object.Destroy(target);
        }
    }
}
