using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Health;
using NihongoLife.Player;
using NihongoLife.Shop;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The medicine shelf in Hibari Mart and the medicine pictures in the handbook, as a player meets them:
    /// the shelf CityTownBuilder placed stands inside the store with a solid body, F opens the Medicine section, and
    /// the handbook's medicine page shows one picture per medicine.
    /// Captures: Bao_Cao/medicine-shelf and Bao_Cao/handbook.
    /// </summary>
    public class MedicineShelfPlayModeTests
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao"));

        private static IEnumerator Town()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
        }

        [UnityTest]
        public IEnumerator MedicineShelf_StandsInTheStore_AndOpensTheMedicineSection()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Town();
            var shelves = Object.FindObjectsByType<KonbiniShelf>(FindObjectsSortMode.None).Where(s => s.Section == KonbiniSection.Medicine).ToArray();
            Assert.AreEqual(1, shelves.Length, "CityTownBuilder placed exactly one medicine shelf");
            var shelf = shelves[0];
            var p = shelf.transform.position;
            Assert.That(p.x, Is.InRange(-4.6f, 4.6f), "inside the store (x)");
            Assert.That(p.z, Is.InRange(0.6f, 9.9f), "inside the store (z)");
            Assert.AreEqual(6, shelf.gameObject.layer, "on the interactable layer");
            var root = shelf.transform.root.gameObject.scene.GetRootGameObjects().First(r => r.name == "Town_Konbini").transform;
            Assert.NotNull(root.Find("MedicineCorner/Med_Shelf_Body"), "a solid body");
            Assert.Greater(root.Find("MedicineCorner/Med_Shelf").GetComponentsInChildren<Renderer>().Length, 30, "the shelf is stocked with the medicine models");

            // Look at it from the aisle: the player stands in front of it and the game's own camera orbits behind.
            var shelfPos = root.Find("MedicineCorner/Med_Shelf").position;
            var playerController = Object.FindFirstObjectByType<PlayerController>();
            var body = playerController.GetComponent<CharacterController>();
            body.enabled = false;
            playerController.transform.SetPositionAndRotation(new Vector3(shelfPos.x, 0.08f, shelfPos.z - 2.2f), Quaternion.identity);
            body.enabled = true;
            Physics.SyncTransforms();
            var orbit = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (orbit != null) orbit.enabled = false;
            // The camera stands in the aisle a little to the side, looking at the stocked shelf (the player waits aside).
            var look = new Vector3(shelfPos.x, 1.0f, shelfPos.z);
            var eye = new Vector3(shelfPos.x - 0.9f, 1.7f, shelfPos.z - 2.9f);
            var shot = new GameObject("ShelfShotCamera").AddComponent<Camera>();
            shot.CopyFrom(Camera.main);
            shot.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
            shot.fieldOfView = 55f;
            yield return new WaitForSecondsRealtime(0.5f);
            var hit = Physics.Raycast(eye, (look - eye).normalized, out var ray, 6f, ~0, QueryTriggerInteraction.Ignore);
            Debug.Log("[MedShelfCam] first hit " + (hit ? ray.collider.name + " at " + ray.distance.ToString("0.00") : "none") + " shelf at " + shelfPos);            Capture(Path.Combine(Root, "medicine-shelf"), "01_medicine_shelf_in_store", shot);
            Debug.Log("[MedShelfCam] camera " + shot.transform.position + " main " + Camera.main.transform.position);
            Object.Destroy(shot.gameObject);

            // F opens the Medicine section.
            var player = Object.FindFirstObjectByType<PlayerController>().gameObject;
            shelf.Interact(player);
            yield return null;
            var shop = KonbiniShopUI.GetOrCreate();
            Assert.IsTrue(shop.IsOpen, "the product browser opens");
            Capture(Path.Combine(Root, "medicine-shelf"), "02_medicine_section");
            shop.Close();
        }

        [UnityTest]
        public IEnumerator Handbook_MedicinePage_ShowsEachMedicinesPicture()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Town();
            HandbookUI.Open(HandbookUI.Chapter.Health, "medicines");
            yield return null;
            yield return null;
            Assert.IsTrue(HandbookUI.IsOpen);
            var icons = Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(i => i.name == "MedicineIcon").ToArray();
            var medicines = IllnessCatalog.Load().medicines;
            Assert.AreEqual(medicines.Length, icons.Length, "one picture per medicine");
            foreach (var icon in icons) Assert.NotNull(icon.sprite, "the picture is loaded");
            Assert.AreEqual(medicines.Length, icons.Select(i => i.sprite.name).Distinct().Count(), "each medicine has its own picture");
            Capture(Path.Combine(Root, "handbook"), "01_medicine_icons");
            HandbookUI.Close();
        }

        private static void Capture(string folder, string name, Camera camera = null)
        {
            camera = camera != null ? camera : Camera.main;
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
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Canvas.ForceUpdateCanvases();
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
