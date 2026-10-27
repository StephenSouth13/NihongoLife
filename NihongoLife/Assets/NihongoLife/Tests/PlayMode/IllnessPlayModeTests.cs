using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Health;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Illnesses in the real town: caught from what the player does, felt in the body, treated as in real life
    /// (medicine eases, the doctor treats — flu antivirals in the first 48 h, a drip for heatstroke —, sleep heals),
    /// and the handbook (Y) that explains it all. Captures: Bao_Cao/health, Bao_Cao/handbook.
    /// </summary>
    public class IllnessPlayModeTests
    {
        private static string Folder(string topic) => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + topic));

        private static IEnumerator LoadCity()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
        }

        [UnityTest]
        public IEnumerator Illness_CaughtFeltTreatedAndHealed()
        {
            yield return LoadCity();
            var status = PlayerStatus.Instance;
            Assert.IsNotNull(PlayerIllness.Instance, "The player carries PlayerIllness");
            PlayerIllness.CureAll();
            var fresh = PlayerIllness.State;
            fresh.immunities.Clear(); fresh.workDay = -1; fresh.shiftsToday = 0;
            float scale = Time.timeScale;
            try
            {
                PlayerIllness.Triggers = true;

                // Soaked for a while → a cold (the game clock runs 5× to keep the test short).
                Time.timeScale = 5f;
                for (float t = 0f; t < 12f && !PlayerIllness.Has("cold"); t += Time.unscaledDeltaTime)
                {
                    status.DebugSetNeeds(wetness: 0.95f);
                    yield return null;
                }
                Time.timeScale = 1f;
                Assert.IsTrue(PlayerIllness.Has("cold"), "Soaked → a cold");
                Assert.Less(PlayerIllness.Instance.Energy, 1f, "A cold slows recovery");
                Assert.IsTrue(PlayerIllness.Instance.BlocksHealing);
                status.DebugSetNeeds(wetness: 0f);

                // Cold medicine relieves, it does not cure.
                float sleepinessBefore = PlayerIllness.Instance.Sleepiness;
                string said = PlayerIllness.TakeMedicine("kazegusuri");
                StringAssert.Contains("Vẫn cần nghỉ ngơi", said);
                Assert.IsTrue(PlayerIllness.Has("cold"), "Medicine eases, the cold stays");
                yield return null;
                Assert.Less(PlayerIllness.Instance.Sleepiness, sleepinessBefore, "Symptoms eased");
                // A night's sleep ends it.
                status.Sleep(8f);
                Assert.IsFalse(PlayerIllness.Has("cold"), "Sleep cures a cold");

                // Flu: fever on screen, slower, works worse; seen early, the doctor's antivirals shorten it a lot.
                var flu = PlayerIllness.Catch("flu");
                Assert.IsNotNull(flu);
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.Less(status.MoveSpeedMultiplier, 1f, "Flu slows you down");
                Assert.Less(PlayerIllness.WorkFactor, 0.7f, "Flu: you work worse");
                Assert.Greater(IllnessOverlay.Alpha, 0.05f, "The fever glow shows");
                Assert.AreEqual("flu", PlayerIllness.Main.id);
                StringAssert.Contains("Cúm", Object.FindFirstObjectByType<StatusDock>().WeatherEffectText ?? "", "The HUD names the illness");
                Capture("health", "01_flu_fever");
                float before = flu.until - PlayerIllness.Now;
                status.TreatAtClinic();
                float after = flu.until - PlayerIllness.Now;
                Assert.Less(after, before * 0.25f, "Seen within 48 h: antivirals cut it short");
                Assert.IsTrue(flu.treated);

                // Working with flu costs reputation; three shifts in one day exhaust.
                int notoriety = SocialStanding.Notoriety;
                PlayerIllness.ShiftWorked();
                Assert.Greater(SocialStanding.Notoriety, notoriety, "Working with flu: reputation down");
                PlayerIllness.CureAll();
                PlayerIllness.ShiftWorked();
                Assert.IsTrue(PlayerIllness.Has("muscle_ache"), "Two shifts in a day: aching muscles");
                PlayerIllness.ShiftWorked();
                Assert.IsTrue(PlayerIllness.Has("overwork"), "Three shifts: overwork");

                // Heatstroke: the clinic's drip cures it at once.
                PlayerIllness.CureAll();
                PlayerIllness.Catch("heatstroke");
                status.TreatAtClinic();
                Assert.IsFalse(PlayerIllness.Has("heatstroke"), "A drip cures heatstroke");

                // Eating when full upsets the stomach; stomach medicine bought at the konbini helps.
                status.DebugSetNeeds(hunger: 96f);
                status.RestoreNeeds(20f, 0f, 0f);
                Assert.IsTrue(PlayerIllness.Has("stomach"), "Overeating → indigestion");
                var inventory = PlayerInventory.Instance;
                var product = NihongoLife.Shop.KonbiniCatalog.Find("igusuri");
                Assert.IsTrue(inventory.AddItem(product.id, product.japanese, product.vietnamese, product.price, 1, false, product.useType, product.food, product.drink, product.energy));
                Assert.IsTrue(inventory.TryConsumeLastConsumable(), "Medicine is used from the bag");
                Assert.IsTrue(PlayerIllness.State.illnesses.First(r => r.id == "stomach").relieved);

                // The konbini's くすり shelf.
                var shop = KonbiniShopUI.GetOrCreate();
                shop.OpenSection(NihongoLife.Shop.KonbiniSection.Medicine);
                yield return null;
                Assert.IsTrue(shop.IsOpen);
                Capture("health", "04_konbini_medicine");
                shop.Close();
                yield return null;

                // A flu shot protects; a mask is worn for a day.
                PlayerIllness.CureAll();
                PlayerIllness.Immunize("flu", 720f);
                Assert.IsNull(PlayerIllness.Catch("flu"), "Vaccinated: no flu");
                PlayerIllness.TakeMedicine("mask");
                Assert.IsTrue(PlayerIllness.IsImmune("mask"));

                // The handbook's health page while ill.
                PlayerIllness.Catch("hay_fever");
                HandbookUI.Open(HandbookUI.Chapter.Health, "status");
                yield return null;
                Capture("health", "02_handbook_status");
                HandbookUI.Open(HandbookUI.Chapter.Health, "flu");
                yield return null;
                Capture("health", "03_handbook_flu");
                HandbookUI.Close();
            }
            finally
            {
                Time.timeScale = scale;
                PlayerIllness.Triggers = false;
                PlayerIllness.CureAll();
                var state = PlayerIllness.State;
                if (state != null) { state.immunities.Clear(); state.shiftsToday = 0; }
                HandbookUI.Close();
            }
        }

        [UnityTest]
        public IEnumerator Handbook_EveryChapter_AndTheMusicPlayer()
        {
            yield return LoadCity();
            string pinned = AudioPrefs.PinnedMusic;
            try
            {
                HandbookUI.Open();
                yield return null;
                Assert.IsTrue(HandbookUI.IsOpen);
                Assert.GreaterOrEqual(HandbookUI.EntryIds.Count(), 6, "Introduction pages");
                Capture("handbook", "01_intro");
                Button("Entry_controls").onClick.Invoke();
                yield return null;
                Assert.AreEqual("controls", HandbookUI.CurrentEntry);
                Capture("handbook", "02_controls");

                Button("Ribbon_Places").onClick.Invoke();
                yield return null;
                Assert.AreEqual(HandbookUI.Chapter.Places, HandbookUI.CurrentChapter);
                Capture("handbook", "03_places");
                LocationGuideUI.Open("STATION");
                yield return null;
                Assert.IsTrue(LocationGuideUI.IsOpen, "The map's place guide is the handbook's places chapter");
                Assert.AreEqual("STATION", LocationGuideUI.ShownType);

                Button("Ribbon_Health").onClick.Invoke();
                yield return null;
                foreach (var id in new[] { "status", "cold", "flu", "heatstroke", "medicines", "doctor_path" })
                    Assert.Contains(id, HandbookUI.EntryIds.ToList(), "Health page " + id);
                Button("Entry_heatstroke").onClick.Invoke();
                yield return null;
                Capture("handbook", "04_heatstroke");
                Button("Entry_doctor_path").onClick.Invoke();
                yield return null;
                Capture("handbook", "05_doctor_path");
                Button("Entry_medicines").onClick.Invoke();
                yield return null;
                Capture("handbook", "06_medicines");

                Button("Ribbon_Careers").onClick.Invoke();
                yield return null;
                Assert.AreEqual(CareerService.Catalog.careers.Length, HandbookUI.EntryIds.Count(), "Every career");
                Button("Entry_medical").onClick.Invoke();
                yield return null;
                Capture("handbook", "07_medical_career");

                Button("Ribbon_Music").onClick.Invoke();
                yield return null;
                Assert.GreaterOrEqual(HandbookUI.EntryIds.Count(), 15, "Every track");
                Button("Entry_clinic").onClick.Invoke();
                yield return null;
                Button("Listen").onClick.Invoke();
                yield return null;
                Assert.AreEqual("clinic", Soundscape.PreviewKey);
                for (float t = 0f; t < 8f && Soundscape.Instance.MusicKey != "clinic"; t += Time.unscaledDeltaTime) yield return null;
                Assert.AreEqual("clinic", Soundscape.Instance.MusicKey, "Listening plays the track");
                Capture("handbook", "08_music");
                Button("Pin").onClick.Invoke();
                yield return null;
                Assert.AreEqual("clinic", AudioPrefs.PinnedMusic);
                HandbookUI.Close();
                Assert.IsNull(Soundscape.PreviewKey, "Closing the book stops the preview");
            }
            finally
            {
                AudioPrefs.PinnedMusic = pinned;
                HandbookUI.Close();
            }
        }

        private static Button Button(string name) =>
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name && b.gameObject.activeInHierarchy);

        private static void Capture(string topic, string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Canvas.ForceUpdateCanvases();
            Directory.CreateDirectory(Folder(topic));
            File.WriteAllBytes(Path.Combine(Folder(topic), name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }
    }
}
