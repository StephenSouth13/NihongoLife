using System;
using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>F4: hire, train, dismiss and collect from workers; the company screen (Z). Captures: Bao_Cao/hire-workers.</summary>
    public class BusinessWorkersPlayModeTests
    {
        private BusinessSystem _sys;

        [TearDown]
        public void TearDown()
        {
            BusinessWorkers.NowTicks = () => DateTime.UtcNow.Ticks;
            Loc.Set(GameLanguage.Vietnamese);
            if (_sys != null && _sys.Business != null)
            {
                var b = _sys.Business; b.companyName = null; b.capitalYen = 0; b.employeeCount = 0; b.reputation = 0; b.workers.Clear(); b.totalEarnedYen = 0; b.lastCollectTicks = 0;
                _sys.Persist();
            }
        }

        [UnityTest]
        public IEnumerator Workers_HireEarnTrainDismiss_AndTheScreen()
        {
            LogAssert.ignoreFailingMessages = true;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/hire-workers")); Directory.CreateDirectory(folder);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (BusinessSystem.Instance == null || UnityEngine.Object.FindFirstObjectByType<BusinessDashboardUI>() == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
            _sys = BusinessSystem.Instance;
            Assert.NotNull(_sys, "BusinessSystem present");
            var ui = UnityEngine.Object.FindFirstObjectByType<BusinessDashboardUI>();
            Assert.NotNull(ui, "The company screen is created with the HUD (key Z)");

            long now = DateTime.UtcNow.Ticks; BusinessWorkers.NowTicks = () => now;
            var b = _sys.Business;
            b.companyName = "Test Co"; b.capitalYen = 200000; b.employeeCount = 0; b.workers.Clear(); b.lastCollectTicks = 0; b.reputation = 20;

            Assert.IsNull(BusinessWorkers.Hire(_sys, "cook", 1000), "Below the living wage nobody is hired");
            Assert.AreEqual(0, b.workers.Count);
            var cook = BusinessWorkers.Hire(_sys, "cook", 25000);
            var cashier = BusinessWorkers.Hire(_sys, "cashier", 26000);
            Assert.NotNull(cook); Assert.NotNull(cashier);
            Assert.AreEqual(2, b.workers.Count); Assert.AreEqual(2, b.employeeCount);
            Assert.AreNotEqual(cook.name, cashier.name);

            now += TimeSpan.TicksPerHour * 5;
            BusinessWorkers.Pending(_sys, out int gross, out int wages, out int net);
            Assert.AreEqual((220 + 180) * 5, gross, 2, "Gross = revenue per hour x 5 hours");
            Assert.Greater(wages, 0); Assert.AreEqual(gross - wages, net); Assert.Greater(net, 1000);
            int cap0 = b.capitalYen;
            Assert.AreEqual(net, BusinessWorkers.Collect(_sys));
            Assert.AreEqual(cap0 + net, b.capitalYen, "Collected into capital");
            BusinessWorkers.Pending(_sys, out int again, out _, out _); Assert.AreEqual(0, again, "Nothing waiting right after collecting");

            now += TimeSpan.TicksPerHour * 100;
            BusinessWorkers.Pending(_sys, out int capped, out _, out _);
            Assert.AreEqual((220 + 180) * BusinessWorkers.MaxHours, capped, 2, "Earnings stop piling up after 8 hours away");

            int cap1 = b.capitalYen;
            Assert.IsTrue(BusinessWorkers.Train(_sys, cook.id)); Assert.AreEqual(2, cook.level);
            Assert.AreEqual(cap1 - 5000, b.capitalYen); Assert.AreEqual(264, BusinessWorkers.RevenuePerHour(cook));

            // Persisted in the save.
            if (GameServices.TryGet(out IProgressRepository repo) && repo.GetProgress() != null)
                Assert.AreEqual(2, repo.GetProgress().business.workers.Count, "Workers are in the save");

            // The screen: lists them, follows the language, Hire / Collect / Dismiss work.
            ui.Show(); yield return null; yield return null;
            Assert.IsTrue(ui.IsOpen);
            Assert.IsTrue(ui.WorkersTextValue.Contains(cook.name) && ui.WorkersTextValue.Contains("Lv2"), ui.WorkersTextValue);
            Loc.Set(GameLanguage.English); ui.Hide(); ui.Show(); yield return null;
            Assert.IsTrue(ui.WorkersTextValue.Contains("Cook") && ui.WorkersTextValue.Contains("Cashier"), "English roles: " + ui.WorkersTextValue);
            Assert.IsFalse(ui.WorkersTextValue.Contains("Phụ bếp") || ui.WorkersTextValue.Contains("Thu ngân"), "No Vietnamese in English");
            Capture(ui, Path.Combine(folder, "01_company_screen_en.png"));
            Loc.Set(GameLanguage.Russian); ui.Hide(); ui.Show(); yield return null;
            Assert.IsFalse(ui.WorkersTextValue.Contains("Cook") || ui.WorkersTextValue.Contains("Cashier"), "Russian roles: " + ui.WorkersTextValue);
            Loc.Set(GameLanguage.English);

            ui.RoleButton.onClick.Invoke();
            int before = b.workers.Count; ui.HireButton.onClick.Invoke(); yield return null;
            Assert.AreEqual(before + 1, b.workers.Count, "Hire button hires");
            ui.CollectButton.onClick.Invoke(); // may be nothing to collect (just collected by Pending cap): must not throw
            ui.FireButton.onClick.Invoke(); yield return null;
            Assert.AreEqual(before, b.workers.Count, "Dismiss button removes the latest hire");
            ui.Hide();

            // Dismiss through the API.
            Assert.IsTrue(BusinessWorkers.Fire(_sys, cashier.id)); Assert.AreEqual(1, b.workers.Count); Assert.AreEqual(1, b.employeeCount);
        }

        private static void Capture(MenuPopupBase popup, string path)
        {
            var cam = Camera.main; var c = popup.GetComponentInParent<Canvas>().rootCanvas; var mode = c.renderMode;
            c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.06f; Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = old; cam.targetTexture = null; rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(tex);
            c.renderMode = mode; c.worldCamera = null;
        }
    }
}