using UnityEngine;

namespace NihongoLife.World
{
    public static class StoreLayoutStabilizer
    {
        private static bool _applied;

        public static void Apply()
        {
            if (_applied) return;
            _applied = true;

            // Imported stock was authored against a different shelf scale and currently
            // floats in the aisle. Keep the usable fixtures, but hide this decoration.
            var integratedStore = GameObject.Find("Store_ThirdParty_Visuals");
            if (integratedStore != null)
            {
                foreach (Transform child in integratedStore.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.StartsWith("Stock_") || child.name.StartsWith("ColdStock_"))
                        child.gameObject.SetActive(false);
                }
            }

            StabilizeShelf("Shelf_Food", new Vector3(-4.35f, 1.1f, 5.8f), 90f);
            StabilizeShelf("Shelf_Drinks", new Vector3(4.35f, 1.1f, 5.8f), -90f);
            RestoreInteractiveItem("Onigiri");
            RestoreInteractiveItem("Water");
            RestoreInteractiveItem("Tea");
        }

        private static void StabilizeShelf(string name, Vector3 position, float yaw)
        {
            var shelf = GameObject.Find(name);
            if (shelf == null) return;
            shelf.SetActive(true);
            shelf.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        private static void RestoreInteractiveItem(string name)
        {
            var item = GameObject.Find(name);
            if (item == null) return;

            var replacement = item.transform.Find("ThirdPartyVisual");
            if (replacement != null) replacement.gameObject.SetActive(false);

            foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                if (replacement == null || !renderer.transform.IsChildOf(replacement)) renderer.enabled = true;
            }
        }
    }
}
