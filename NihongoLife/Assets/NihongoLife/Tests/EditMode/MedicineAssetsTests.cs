using System.Linq;
using NihongoLife.Health;
using NihongoLife.Shop;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards the Hibari Mart medicine shelf art: every medicine in illnesses.json has a picture icon
    /// (Resources/Items/&lt;id&gt;.png, loaded by ItemIcons) and a 3D prefab built by MedicineModelBuilder, and the
    /// medicine shelf prefab opens the Medicine section.
    /// </summary>
    public class MedicineAssetsTests
    {
        // Same folder MedicineModelBuilder (Editor assembly) writes to.
        private const string Dir = "Assets/NihongoLife/Generated/Konbini/Medicine";
        private const string ShelfPath = Dir + "/Med_Shelf.prefab";
        private static string PrefabPath(string id) => Dir + "/Med_" + id + ".prefab";

        private static string[] Ids => IllnessCatalog.Load().medicines.Select(m => m.id).ToArray();

        [Test]
        public void EveryMedicine_HasAPictureIcon()
        {
            Assert.GreaterOrEqual(Ids.Length, 7);
            foreach (string id in Ids)
            {
                var texture = Resources.Load<Texture2D>("Items/" + id);
                Assert.IsNotNull(texture, "Missing Resources/Items/" + id + ".png");
                Assert.IsNotNull(ItemIcons.Get(id), "ItemIcons has no sprite for " + id);
                Assert.AreEqual(512, texture.width, id);
                Assert.AreEqual(512, texture.height, id);
            }
        }

        [Test]
        public void EveryMedicine_HasA3DPrefab_WithoutColliders()
        {
            foreach (string id in Ids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(id));
                Assert.IsNotNull(prefab, "Missing 3D prefab for " + id + " (run MedicineModelBuilder.Build)");
                Assert.Greater(prefab.GetComponentsInChildren<Renderer>(true).Length, 0, id);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(true).Length, id + ": products must not block the player");
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab), id);
            }
        }

        [Test]
        public void MedicineShelf_OpensTheMedicineSection_AndStocksEveryMedicine()
        {
            var shelf = AssetDatabase.LoadAssetAtPath<GameObject>(ShelfPath);
            Assert.IsNotNull(shelf, "Missing Med_Shelf prefab");
            var trigger = shelf.GetComponentInChildren<KonbiniShelf>(true);
            Assert.IsNotNull(trigger, "The shelf needs a KonbiniShelf trigger");
            Assert.AreEqual(KonbiniSection.Medicine, trigger.Section);
            Assert.IsTrue(trigger.GetComponent<Collider>().isTrigger);
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(shelf));
            foreach (string id in Ids)
                Assert.IsTrue(shelf.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Med_" + id), "The shelf does not stock " + id);
        }

        [Test]
        public void KonbiniCatalog_ListsEveryMedicineInTheMedicineSection()
        {
            var listed = KonbiniCatalog.InSection(KonbiniSection.Medicine).Select(p => p.id).ToArray();
            CollectionAssert.AreEquivalent(Ids, listed);
        }
    }
}
