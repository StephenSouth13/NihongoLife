using NihongoLife.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards the medicine shelf prefab (Generated/Konbini/Medicine/Med_Shelf.prefab): its F-to-browse trigger opens
    /// the Medicine section and sits on the interactable layer, so the player's interaction ray can find it.
    /// </summary>
    public class MedicineShelfTests
    {
        private const string ShelfPath = "Assets/NihongoLife/Generated/Konbini/Medicine/Med_Shelf.prefab";
        private const int InteractableLayer = 6; // ZoneBuildKit.InteractableLayer (the editor assembly is not visible from the tests)

        [Test]
        public void ShelfPrefab_HasAMedicineTrigger_OnTheInteractableLayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShelfPath);
            Assert.NotNull(prefab, "run MedicineModelBuilder.Build");
            var shelf = prefab.GetComponentInChildren<KonbiniShelf>(true);
            Assert.NotNull(shelf, "the shelf carries a KonbiniShelf trigger");
            Assert.AreEqual(KonbiniSection.Medicine, shelf.Section);
            Assert.AreEqual(InteractableLayer, shelf.gameObject.layer, "on the interactable layer");
            Assert.IsTrue(shelf.GetComponent<BoxCollider>().isTrigger);
        }
    }
}
