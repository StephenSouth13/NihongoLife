using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Records real-game footage for the promo videos at a fixed 30 fps (1920×1080 JPG frames + timeline.tsv) into
    /// NL_FRAME_DIR: the main menu, a walk through Hibari-chō with the HUD and the task journal, and a camera tour of
    /// Phú Quốc with its crowds, plus two walks there. Explicit: run on purpose (-testFilter PromoRecording).
    /// </summary>
    public class PromoRecordingTests
    {
        const int W = 1920, H = 1080;
        static string Root => Environment.GetEnvironmentVariable("NL_FRAME_DIR") is { Length: > 0 } d ? d : Path.Combine(Path.GetTempPath(), "NihongoLifePromoFrames");

        RenderTexture _target;
        Texture2D _frame;
        int _index;
        string _dir;
        StringBuilder _timeline;
        PlayerController _player;

        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator PromoRecording_MenuAndTown()
        {
            Begin("town");
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            yield return new WaitForSecondsRealtime(3f);
            var menuCam = Camera.main;
            yield return Clip("menu", 8f, menuCam, true, null);

            yield return LoadPlayable(WorldLocationCatalog.CityScene);
            var cam = Camera.main;
            Vector3 from = new Vector3(0.8f, 0.08f, -16f), to = new Vector3(0.8f, 0.08f, -4f);
            yield return Clip("town_walk", 9f, cam, true, t => Walk(cam, from, to, t, 4.2f, 2.2f));
            Vector3 a2 = new Vector3(-6f, 0.08f, -9f), b2 = new Vector3(6f, 0.08f, -9f);
            yield return Clip("town_walk2", 7f, cam, true, t => Walk(cam, a2, b2, t, 4.6f, 2.4f));
            TaskJournalUI.Open(null);
            yield return Clip("journal", 6f, cam, true, null);
            TaskJournalUI.Close();
            End();
        }

        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator PromoRecording_PhuQuoc()
        {
            Begin("phuquoc");
            yield return LoadPlayable(WorldLocationCatalog.PhuQuocIslandScene);
            var cam = Camera.main;
            Transform r = GameObject.Find("GanhNgon_Restaurant").transform;
            Vector3 O(float x, float y, float z) => r.position + new Vector3(x, y, z);
            Place(O(0f, 0.3f, -12f), 0f);
            var shots = new (string id, float sec, Vector3 p0, Vector3 l0, Vector3 p1, Vector3 l1)[]
            {
                ("overview", 8f, O(-20, 75, -150), O(10, 0, -20), O(20, 58, -118), O(10, 0, -10)),
                ("street", 7f, O(-30, 1.8f, -23.8f), O(10, 1.2f, -12), O(-17, 1.8f, -22), O(20, 1.2f, -10)),
                ("restaurant", 6f, O(7, 4, -14), O(0, 1, 0), O(3, 3, -10), O(0, 1, 0)),
                ("staff", 5f, O(0, 2.2f, -7.5f), O(0, 1, 2), O(1.2f, 2f, -5.5f), O(0, 1, 2)),
                ("beach", 7f, O(-22, 4.5f, -49), O(-6, 0, -73), O(-12, 4, -52), O(4, 0, -75)),
                ("swimmers", 6f, O(-10, 5, -62), O(0, 0, -85), O(-2, 3.5f, -64), O(4, 0, -86)),
                ("promenade", 6f, O(-30, 3, -40), O(0, 1, -48), O(-20, 2.5f, -42), O(6, 1, -48)),
                ("seafood_market", 6f, O(-50, 3.2f, 40), O(-50, 1, 56), O(-46, 2.6f, 46), O(-50, 1, 58)),
                ("hem_alley", 6f, O(44, 1.7f, 39), O(44, 1.6f, 60), O(44, 1.7f, 47), O(44, 1.6f, 66)),
                ("tour_terminal", 5f, O(-12, 3, 36), O(-12, 1, 48), O(-8, 2.6f, 40), O(-12, 1, 50)),
                ("intersection", 6f, O(62, 16, -38), O(44, 0, -15), O(54, 12, -30), O(40, 0, -12)),
                ("hotels", 5f, O(-10, 3, 15), O(5, 4, 30), O(-4, 3, 20), O(8, 4, 34)),
                ("hills", 7f, O(60, 30, 10), O(100, 10, 120), O(70, 34, 30), O(105, 10, 130)),
                ("resort", 7f, O(205, 38, -95), O(205, 0, 0), O(215, 30, -70), O(205, 0, 10)),
                ("resort_pool", 6f, O(190, 5, -2), O(208, 0, 12), O(195, 4, 4), O(210, 0, 14)),
            };
            foreach (var s in shots)
                yield return Clip(s.id, s.sec, cam, false, t =>
                {
                    float k = Mathf.SmoothStep(0f, 1f, t);
                    Vector3 p = Vector3.Lerp(s.p0, s.p1, k), l = Vector3.Lerp(s.l0, s.l1, k);
                    cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(l - p));
                });
            Vector3 w0 = O(-28, 0.3f, -21), w1 = O(-6, 0.3f, -17);
            yield return Clip("walk_street", 8f, cam, true, t => Walk(cam, w0, w1, t, 4.4f, 2.3f));
            Vector3 m0 = O(-50, 0.3f, 32), m1 = O(-50, 0.3f, 46);
            yield return Clip("walk_market", 7f, cam, true, t => Walk(cam, m0, m1, t, 4.4f, 2.3f));
            End();
        }

        // ─────────── helpers ───────────

        void Begin(string name)
        {
            LogAssert.ignoreFailingMessages = true;
            _dir = Path.Combine(Root, name);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);
            _target = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            _frame = new Texture2D(W, H, TextureFormat.RGB24, false);
            _index = 0;
            _timeline = new StringBuilder();
            Time.captureFramerate = 30;
        }

        void End()
        {
            Time.captureFramerate = 0;
            File.WriteAllText(Path.Combine(_dir, "timeline.tsv"), _timeline.ToString());
            UnityEngine.Object.Destroy(_frame);
            _target.Release();
            Debug.Log($"[PromoRecording] {_index} frames → {_dir}");
            Assert.Greater(_index, 30 * 20);
        }

        IEnumerator LoadPlayable(string scene)
        {
            Time.captureFramerate = 0;
            yield return SceneManager.LoadSceneAsync(scene);
            for (float t = 0f; t < 40f; t += Time.unscaledDeltaTime)
            {
                _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                var flow = UnityEngine.Object.FindFirstObjectByType<SceneFlowController>();
                if (_player != null && flow != null && !flow.IsLoading && SceneManager.GetActiveScene().name == scene) break;
                yield return null;
            }
            Assert.NotNull(_player, "player in " + scene);
            _player.InputLocked = true;
            var follow = Camera.main.GetComponent<ThirdPersonCameraController>();
            if (follow != null) follow.enabled = false;
            yield return new WaitForSecondsRealtime(3f);
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            for (int i = 0; i < 8 && dm != null && dm.IsOpen; i++) dm.CancelDialogue();
            Time.captureFramerate = 30;
        }

        void Place(Vector3 p, float yaw)
        {
            if (_player == null) return;
            var cc = _player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            _player.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            if (cc != null) cc.enabled = true;
        }

        void Walk(Camera cam, Vector3 a, Vector3 b, float t, float back, float up)
        {
            Vector3 dir = (b - a).normalized;
            Vector3 p = Vector3.Lerp(a, b, t);
            Place(p, Quaternion.LookRotation(dir).eulerAngles.y);
            _player.GetComponentInChildren<CharacterAnimationController>()?.SetSpeed(1.1f);
            Vector3 c = p - dir * back + Vector3.up * up;
            cam.transform.SetPositionAndRotation(c, Quaternion.LookRotation(p + Vector3.up * 1.3f + dir * 2f - c));
        }

        IEnumerator Clip(string id, float seconds, Camera cam, bool hud, Action<float> pose)
        {
            _timeline.AppendLine($"{_index}\t{id}");
            int frames = Mathf.RoundToInt(seconds * 30f);
            for (int i = 0; i < frames; i++)
            {
                pose?.Invoke(i / (float)(frames - 1));
                yield return null;
                Capture(cam, hud);
            }
        }

        void Capture(Camera camera, bool hud)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.enabled && c.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(c => c.sortingOrder).ToArray();
            if (hud)
                for (int i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = camera.nearClipPlane + 0.05f + 0.002f * (canvases.Length - i);
                }
            Canvas.ForceUpdateCanvases();
            var old = camera.targetTexture;
            camera.targetTexture = _target;
            camera.Render();
            camera.targetTexture = old;
            if (hud) foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
            var active = RenderTexture.active;
            RenderTexture.active = _target;
            _frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            _frame.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(Path.Combine(_dir, $"f{_index++:00000}.jpg"), _frame.EncodeToJPG(90));
        }
    }
}
