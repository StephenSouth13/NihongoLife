using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Needs with warnings and penalties, weather effects on the needs, the rainbow. Captures: Bao_Cao/hud-regression
    /// (needs_*) and Bao_Cao/environment-regression (rainbow_*).
    /// </summary>
    public class NeedsWeatherPlayModeTests
    {
        private static string Hud => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/hud-regression"));
        private static string Env => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/environment-regression"));

        [UnityTest]
        public IEnumerator Needs_FlashWarnPenaliseAndFaint()
        {
            yield return LoadCity();
            var status = PlayerStatus.Instance;
            var dock = Object.FindFirstObjectByType<StatusDock>();
            var inventory = Object.FindFirstObjectByType<PlayerInventory>();
            WeatherCycle.Instance.Force(WeatherCycle.Kind.Overcast);
            try
            {
                // Hunger falling: amber, then flashing red with the coming penalty spelled out.
                status.DebugSetNeeds(hunger: 25f);
                yield return new WaitForSecondsRealtime(0.4f);
                StringAssert.Contains("khi về 0", dock.WarningText, "Warning before the penalty");
                status.DebugSetNeeds(hunger: 8f);
                var scales = new float[20];
                var ring = dock.StatusWidget.Find("Column/Needs/Need_hunger");
                for (int i = 0; i < scales.Length; i++) { yield return new WaitForSecondsRealtime(0.05f); scales[i] = ring.localScale.x; }
                Assert.Greater(scales.Max() - scales.Min(), 0.02f, "The hunger ring throbs under 15 %");
                Capture("needs_hunger_8");

                // At 0: the penalty applies and the chip says so.
                status.DebugSetNeeds(hunger: 0f);
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.AreEqual(0.8f, status.MoveSpeedMultiplier, 0.001f, "Starving: walk 20 % slower");
                StringAssert.Contains("ĐÓI LẢ", dock.WarningText);
                status.DebugSetNeeds(hunger: 60f, thirst: 0f);
                yield return null;
                Assert.IsFalse(status.CanSprint, "Parched: cannot run");
                status.DebugSetNeeds(thirst: 60f);

                // Health in danger: red screen edges and "about to faint".
                status.DebugSetNeeds(health: 9f);
                yield return new WaitForSecondsRealtime(0.8f);
                StringAssert.Contains("SẮP NGẤT", dock.WarningText);
                Assert.Greater(dock.DangerVignetteAlpha, 0.08f, "Red vignette");
                Capture("needs_health_9");

                // Health 0: faint, clinic, fee, back on your feet.
                inventory.AddYen(2000);
                int yen = inventory.Yen;
                var consequences = status.GetComponent<StatusConsequences>();
                status.DebugSetNeeds(health: 0f);
                for (float t = 0f; t < 2f && !consequences.IsBusy; t += Time.unscaledDeltaTime) yield return null;
                Assert.IsTrue(consequences.IsBusy, "Fainting");
                yield return new WaitForSecondsRealtime(2.2f);
                Capture("needs_fainted");
                for (float t = 0f; t < 10f && consequences.IsBusy; t += Time.unscaledDeltaTime) yield return null;
                Assert.IsFalse(consequences.IsBusy);
                int expectedFee = Mathf.Min(StatusConsequences.MaxClinicFee, Mathf.RoundToInt(yen * 0.1f));
                Assert.AreEqual(expectedFee, consequences.LastFee);
                Assert.AreEqual(yen - expectedFee, inventory.Yen, "Clinic fee paid");
                Assert.GreaterOrEqual(status.CurrentHealth, status.MaxHealth * 0.45f - 0.01f, "Patched up");
                Assert.IsFalse(Object.FindFirstObjectByType<PlayerController>().InputLocked, "Back in control");

                // Falling asleep on your feet: slower, dark edges, countdown.
                status.DebugSetNeeds(sleepiness: 100f);
                yield return new WaitForSecondsRealtime(1f);
                Assert.AreEqual(0.7f, status.MoveSpeedMultiplier, 0.001f, "Nodding off: 30 % slower");
                StringAssert.Contains("GỤC NGÃ SAU", dock.WarningText);
                status.DebugSetNeeds(sleepiness: 10f, hunger: 90f, thirst: 90f, health: 100f);
            }
            finally { WeatherCycle.Instance.Force(null); }
        }

        /// <summary>
        /// Health comes back: by itself while fed and watered, at once with a meal, fully after a night's sleep. A
        /// faint cut short (scene change, disabled player) never leaves the player stuck at 0 health: it faints again
        /// and the clinic patches them up, with control back.
        /// </summary>
        [UnityTest]
        public IEnumerator Health_HealsWhenFed_AndAStuckFaintRecovers()
        {
            yield return LoadCity();
            var status = PlayerStatus.Instance;
            var consequences = status.GetComponent<StatusConsequences>();
            var controller = Object.FindFirstObjectByType<PlayerController>();
            WeatherCycle.Instance.Force(WeatherCycle.Kind.Overcast);
            try
            {
                status.DebugSetNeeds(health: 20f, hunger: 90f, thirst: 90f, sleepiness: 10f, wetness: 0f);
                yield return new WaitForSecondsRealtime(3f);
                Assert.Greater(status.CurrentHealth, 21.5f, "Fed and watered: health heals by itself");

                status.DebugSetNeeds(hunger: 10f, thirst: 10f, health: 30f);
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.LessOrEqual(status.CurrentHealth, 30.01f, "Hungry and thirsty: no healing");

                float before = status.CurrentHealth;
                status.RestoreNeeds(30f, 20f, 0f);
                Assert.GreaterOrEqual(status.CurrentHealth, before + 15f, "A meal and a drink heal at once");

                status.Sleep(8f);
                Assert.AreEqual(status.MaxHealth, status.CurrentHealth, 0.01f, "A full night heals completely");

                // A faint interrupted half way (the component disabled, as when the player object is switched off).
                status.DebugSetNeeds(health: 0f, hunger: 90f, thirst: 90f);
                for (float t = 0f; t < 2f && !consequences.IsBusy; t += Time.unscaledDeltaTime) yield return null;
                Assert.IsTrue(consequences.IsBusy, "Fainting");
                consequences.enabled = false;
                yield return null;
                Assert.IsFalse(consequences.IsBusy, "A cut-short faint is not left running");
                Assert.IsFalse(controller.InputLocked, "Control is given back");
                consequences.enabled = true;
                for (float t = 0f; t < 3f && !consequences.IsBusy; t += Time.unscaledDeltaTime) yield return null;
                Assert.IsTrue(consequences.IsBusy, "Still at 0 health: it faints again instead of staying stuck");
                for (float t = 0f; t < 12f && consequences.IsBusy; t += Time.unscaledDeltaTime) yield return null;
                Assert.GreaterOrEqual(status.CurrentHealth, status.MaxHealth * 0.45f - 0.01f, "Patched up");
                Assert.IsFalse(controller.InputLocked, "Back in control");
            }
            finally
            {
                consequences.enabled = true;
                status.DebugSetNeeds(sleepiness: 10f, hunger: 90f, thirst: 90f, health: 100f);
                WeatherCycle.Instance.Force(null);
            }
        }

        [UnityTest]
        public IEnumerator Weather_WetColdHotSun_ShownAndApplied()
        {
            yield return LoadCity();
            var status = PlayerStatus.Instance;
            var dock = Object.FindFirstObjectByType<StatusDock>();
            var cycle = Object.FindFirstObjectByType<DayNightCycle>();
            try
            {
                cycle.SetHour(10f);
                WeatherCycle.Instance.Force(WeatherCycle.Kind.Rain);
                yield return new WaitForSecondsRealtime(2.5f);
                Assert.IsTrue(WeatherCycle.PlayerInRain);
                Assert.Greater(status.Wetness, 0.02f, "Getting wet");
                StringAssert.Contains("ướt mưa", dock.WeatherEffectText);
                status.DebugSetNeeds(wetness: 0.85f);
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.IsTrue(status.HasCold);
                StringAssert.Contains("cảm lạnh", dock.WeatherEffectText);
                Capture("needs_rain_cold");

                WeatherCycle.Instance.Force(WeatherCycle.Kind.Clear);
                status.DebugSetNeeds(wetness: 0f);
                cycle.SetHour(13f);
                yield return new WaitForSecondsRealtime(1f);
                Assert.IsTrue(status.InHotSun, "Midday sun outdoors");
                StringAssert.Contains("Nắng gắt", dock.WeatherEffectText);
                float before = status.Thirst;
                yield return new WaitForSecondsRealtime(3f);
                Debug.Log($"[Needs] thirst in hot sun: {before:0.000} → {status.Thirst:0.000}");
            }
            finally { WeatherCycle.Instance.Force(null); }
        }

        [UnityTest]
        public IEnumerator Rainbow_AfterTheShower_OppositeTheSun()
        {
            yield return LoadCity();
            var cycle = Object.FindFirstObjectByType<DayNightCycle>();
            cycle.SetHour(15.5f);
            WeatherCycle.Instance.Force(WeatherCycle.Kind.Clear);
            yield return null;
            WeatherCycle.Instance.ForceRainbow(true);
            for (float t = 0f; t < 10f && WeatherCycle.Instance.Rainbow < 0.95f; t += Time.unscaledDeltaTime) yield return null;
            Assert.Greater(RenderSettings.skybox.GetFloat("_Rainbow"), 0.5f, "Rainbow in the sky");
            // Look away from the sun, a little up.
            var sun = RenderSettings.sun.transform.forward;
            var cam = Camera.main;
            var controller = cam.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            controller.enabled = false;
            Vector3 away = Vector3.ProjectOnPlane(sun, Vector3.up).normalized;
            var player = Object.FindFirstObjectByType<PlayerController>();
            cam.transform.position = player.transform.position + Vector3.up * 2f - away * 3f;
            cam.transform.rotation = Quaternion.LookRotation(away + Vector3.up * 0.3f);
            ShootWorld(cam, Path.Combine(Env, "rainbow_15h.jpg"));
            controller.enabled = true;
            WeatherCycle.Instance.ForceRainbow(false);
            WeatherCycle.Instance.Force(null);
        }

        // ─────────── helpers ───────────

        private static IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private static void ShootWorld(Camera cam, string path)
        {
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToJPG(88));
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
        }

        private static void Capture(string name)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = cam; canvases[i].planeDistance = cam.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Canvas.ForceUpdateCanvases();
            ShootWorld(cam, Path.Combine(Hud, name + ".jpg"));
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        }
    }
}
