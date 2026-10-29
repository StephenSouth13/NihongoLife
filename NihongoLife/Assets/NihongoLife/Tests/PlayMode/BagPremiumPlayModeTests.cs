using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Home;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>G2: premium bag (tooltip, context menu, drag and drop, sort, filter, split) and the bedroom chest. Captures: Bao_Cao/bag-premium.</summary>
    public class BagPremiumPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/bag-premium"));

        private static void Capture(string name, int w = 1600, int h = 900)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = cam; canvases[i].planeDistance = cam.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = old; cam.targetTexture = null; rt.Release(); Object.Destroy(rt); Object.Destroy(tex);
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        }

        [UnityTest]
        public IEnumerator Bag_Tooltip_Menu_DragDrop_Sort_Filter_Split_Chest()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || PlayerInventory.Instance == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var inv = PlayerInventory.Instance;
            while (inv.Items.Count > 0) inv.RemoveAt(0, 999);
            Assert.IsTrue(inv.AddItem("zz_pen", "ペン", "Pen", 120, 1, false, ItemUseType.None));
            Assert.IsTrue(inv.AddItem("a_cola", "コーラ", "Cola", 150, 6, false, ItemUseType.Drink, 0, 20, 0));
            Assert.IsTrue(inv.AddItem("b_bun", "パン", "Bun", 180, 4, false, ItemUseType.Food, 25, 0, 5));

            var window = Object.FindObjectsByType<InventoryWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (window == null) window = InventoryWindow.Create(new GameObject("BagHost").transform, null);
            window.Root.SetActive(true);
            yield return null; yield return null;
            window.Refresh();

            // Tooltip on hover.
            window.SlotEnter(1);
            yield return null; yield return null;
            Assert.IsTrue(window.TooltipVisible, "Hover shows the tooltip");
            StringAssert.Contains(inv.Items[1].displayNameJa, window.TooltipText);
            StringAssert.Contains("20", window.TooltipText, "Shows the drink effect");
            Capture("01_tooltip");
            window.SlotExit(1);
            yield return null;
            Assert.IsFalse(window.TooltipVisible, "Leaving hides it");

            // Sort: drinks/foods/other by type.
            window.SortBag();
            var types = inv.Items.Select(e => (int)e.useType).ToArray();
            CollectionAssert.AreEqual(types.OrderBy(x => x).ToArray(), types, "Sorted by use type");

            // Drag and drop moves a stack (slot 0 -> slot 2).
            string first = inv.Items[0].itemId;
            Assert.IsTrue(window.DragDrop(0, 2), "Drop on another slot moves the stack");
            Assert.AreEqual(first, inv.Items[2].itemId, "The dragged stack landed on slot 3");
            Assert.IsFalse(window.Dragging, "Drag ended");

            // Filter dims what does not match; slot numbers stay.
            window.SetFilter(InventoryWindow.BagFilter.Food);
            window.Refresh();
            var tiles = window.GetComponentsInChildren<Image>(true).Where(i => i.name == "Tile" && i.transform.parent.name.StartsWith("Slot_")).ToArray();
            int foodIdx = inv.Items.ToList().FindIndex(e => e.useType == ItemUseType.Food), drinkIdx = inv.Items.ToList().FindIndex(e => e.useType == ItemUseType.Drink);
            Assert.AreEqual(1f, tiles[foodIdx].color.a, 0.01f, "Matching food is bright");
            Assert.Less(tiles[drinkIdx].color.a, 0.5f, "A drink is dimmed under the Food filter");
            Capture("02_filter_food");
            window.SetFilter(InventoryWindow.BagFilter.All);

            // Split in half.
            int idx = inv.Items.ToList().FindIndex(e => e.itemId == "a_cola");
            int total = inv.GetItemQuantity("a_cola"), slotsBefore = inv.Items.Count;
            Assert.IsTrue(window.SplitAt(idx), "Split works with a free slot");
            Assert.AreEqual(slotsBefore + 1, inv.Items.Count);
            Assert.AreEqual(total, inv.Items.Where(e => e.itemId == "a_cola").Sum(e => e.quantity), "Nothing lost");
            Assert.AreEqual(3, inv.Items[idx + 1].quantity); Assert.AreEqual(3, inv.Items[idx].quantity);

            // Dropping one stack onto the other merges them.
            Assert.IsTrue(window.DragDrop(idx + 1, idx), "Same item merges");
            Assert.AreEqual(6, inv.GetItemQuantity("a_cola")); Assert.AreEqual(slotsBefore, inv.Items.Count);

            // Right-click menu.
            idx = inv.Items.ToList().FindIndex(e => e.itemId == "a_cola");
            window.SlotRightClick(idx, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            Assert.IsTrue(window.MenuOpen, "Right-click opens the menu");
            Assert.IsNotNull(window.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "M_Drop one"), "Drop one is offered for a stack");
            Assert.IsNull(window.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "M_Store in chest"), "No chest outside the bedroom");
            Capture("03_context_menu");
            window.GetComponentsInChildren<Button>(true).First(b => b.name == "M_Drop one").onClick.Invoke();
            Assert.AreEqual(5, inv.GetItemQuantity("a_cola"), "Dropped one");
            Assert.IsFalse(window.MenuOpen, "Menu closes after a choice");

            // Bedroom chest: whole stack moves with every detail, and back.
            var chest = new GameObject("TestChest").AddComponent<StorageChest>();
            yield return null;
            Assert.IsTrue(StorageChest.Reachable);
            idx = inv.Items.ToList().FindIndex(e => e.itemId == "b_bun");
            window.SlotRightClick(idx, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            window.GetComponentsInChildren<Button>(true).First(b => b.name == "M_Store in chest").onClick.Invoke();
            Assert.AreEqual(0, inv.GetItemQuantity("b_bun"), "Left the bag");
            Assert.AreEqual(1, chest.StoredItems.Count);
            Assert.AreEqual(4, chest.StoredItems[0].quantity); Assert.AreEqual("パン", chest.StoredItems[0].displayNameJa); Assert.AreEqual(25f, chest.StoredItems[0].foodRestore, 0.01f);
            Assert.IsTrue(chest.Take(0));
            Assert.AreEqual(4, inv.GetItemQuantity("b_bun"), "Back in the bag with its details");
            Assert.AreEqual(ItemUseType.Food, inv.Items.First(e => e.itemId == "b_bun").useType);
            Assert.AreEqual(0, chest.StoredItems.Count);
            Object.Destroy(chest.gameObject);
            window.Root.SetActive(false);
        }
    }
}