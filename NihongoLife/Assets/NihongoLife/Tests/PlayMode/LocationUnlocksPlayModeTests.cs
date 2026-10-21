using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Places open with knowledge: a new learner can use Hibari Mart and the school; the station door says what is
    /// missing, the map and the task journal show the locks, and enough knowledge opens the station.
    /// Captures: Bao_Cao/unlock-regression.
    /// </summary>
    public class LocationUnlocksPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/unlock-regression"));
        private int _savedKnowledge = -1;
        private System.Collections.Generic.List<string> _savedCompleted;

        [TearDown]
        public void TearDown()
        {
            LocationUnlocks.Bypass = true;
            LocationUnlocks.ResetVisits();
            if (_savedKnowledge >= 0 && PlayerStatus.Instance != null) PlayerStatus.Instance.DebugSetKnowledge(_savedKnowledge);
            if (_savedCompleted != null && GameServices.TryGet(out NihongoLife.Save.IProgressRepository repo))
            {
                var p = repo.GetProgress(); p.completedScenarios.Clear(); p.completedScenarios.AddRange(_savedCompleted); repo.SaveProgress(p);
            }
            MapUnlockCardUI.Close();
            TaskJournalUI.Close();
        }

        [UnityTest]
        public IEnumerator NewLearner_StationLocked_ThenOpensWithKnowledge()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }

            LocationUnlocks.Bypass = false;
            LocationUnlocks.ResetVisits();
            var status = PlayerStatus.Instance;
            _savedKnowledge = status.Knowledge;
            status.DebugSetKnowledge(10);
            // The roadmap: the opening and the sushi dinner are done; the station still needs 160 knowledge.
            var repo = GameServices.Get<NihongoLife.Save.IProgressRepository>();
            var progress = repo.GetProgress();
            _savedCompleted = new System.Collections.Generic.List<string>(progress.completedScenarios);
            foreach (var id in new[] { "scenario.intro.arrival", "scenario.street.first_talk", "scenario.house1.greeting", "scenario.school.self_intro", "scenario.konbini.buy_onigiri", "scenario.restaurant.sushi_dining" })
                if (!progress.completedScenarios.Contains(id)) progress.completedScenarios.Add(id);
            repo.SaveProgress(progress);

            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.SchoolScene), "School always open");
            Assert.IsFalse(LocationUnlocks.IsUnlocked(WorldLocationCatalog.StationScene), "Station locked at 10 points");
            Assert.AreEqual(150, LocationUnlocks.Missing(WorldLocationCatalog.StationScene));
            Assert.AreEqual(WorldLocationCatalog.SushiRestaurantScene, LocationUnlocks.Next().sceneName, "Sushi Hibari (80) is the next stage.");

            var portal = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).FirstOrDefault(p => p.TargetScene == WorldLocationCatalog.StationScene);
            Assert.NotNull(portal, "Station door in the city");
            StringAssert.Contains("Đã khóa", portal.GetpromptEn());
            var player = Object.FindFirstObjectByType<PlayerController>();
            portal.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(WorldLocationCatalog.CityScene, SceneManager.GetActiveScene().name, "Locked: still in the city");

            // Map and journal show the locks.
            var map = Object.FindFirstObjectByType<WorldMapUI>();
            map.SetVisible(true);
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("map_locked");
            map.SetVisible(false);
            TaskJournalUI.Open();
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("journal_unlocks");
            TaskJournalUI.Close();

            // The guide: photo, meaning, how to use, phrases, lock state.
            LocationGuideUI.Open("KONBINI");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(LocationGuideUI.IsOpen);
            Capture("guide_konbini");
            LocationGuideUI.Open("STATION");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual("STATION", LocationGuideUI.ShownType);
            Capture("guide_station_locked");
            LocationGuideUI.Close();

            // Enough knowledge: the station opens.
            status.AddKnowledge(150);
            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.StationScene));
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("unlocked_toast");
            portal.Interact(player.gameObject);
            for (float t = 0f; t < 20f && SceneManager.GetActiveScene().name != WorldLocationCatalog.StationScene; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(WorldLocationCatalog.StationScene, SceneManager.GetActiveScene().name, "Station entered once unlocked");
            // Lower the score again: a place once entered stays open.
            status.DebugSetKnowledge(0);
            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.StationScene), "Visited places stay open");
        }

        [UnityTest]
        public IEnumerator Wallet_ChangesFlyFromTheChip_AndHistoryLists()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var inventory = PlayerInventory.Instance;
            var feedback = Object.FindFirstObjectByType<WalletFeedback>();
            Assert.NotNull(feedback, "Feedback on the yen chip");
            int before = WalletLedger.Entries.Count;
            inventory.AddYen(600, "Lương làm thêm");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.IsTrue(inventory.SpendYen(160, "Mua ở Hibari Mart"));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Greater(WalletFeedback.Floating, 0, "Amounts rise from the chip");
            Assert.AreEqual("Mua ở Hibari Mart", WalletLedger.Entries[0].reason);
            Assert.AreEqual(-160, WalletLedger.Entries[0].amount);
            Assert.AreEqual(600, WalletLedger.Entries[1].amount);
            Capture("wallet_float");
            feedback.ToggleHistory();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(WalletFeedback.HistoryOpen);
            Capture("wallet_history");
            feedback.ToggleHistory();
            inventory.SpendYen(440, "test");
        }

        private static void Capture(string name)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = cam; canvases[i].planeDistance = cam.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        }
    }
}
