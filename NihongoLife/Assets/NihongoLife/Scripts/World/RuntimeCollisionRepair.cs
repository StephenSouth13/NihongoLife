using UnityEngine;

namespace NihongoLife.World
{
    public class RuntimeCollisionRepair : MonoBehaviour
    {
        [SerializeField] private bool repairOnStart = true;
        [SerializeField] private Vector3 defaultBuildingSize = new Vector3(8.6f, 4.6f, 7.2f);

        private void Start()
        {
            if (repairOnStart)
            {
                RepairBuildings();
            }
        }

        public void RepairBuildings()
        {
            foreach (Transform transform in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (transform == null) continue;

                string objectName = transform.gameObject.name;
                if (objectName.StartsWith("StreetBuilding_", System.StringComparison.Ordinal))
                {
                    EnsureSolidCollider(transform.gameObject, defaultBuildingSize);
                }
                else if (objectName == "KonbiniStoreAsset")
                {
                    EnsureKonbiniSideColliders(transform);
                }
            }
        }

        private static void EnsureSolidCollider(GameObject target, Vector3 size)
        {
            var collider = target.GetComponent<BoxCollider>();
            if (collider == null) collider = target.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.center = new Vector3(0f, size.y * 0.5f, 0f);
            collider.size = size;
        }

        private static void EnsureKonbiniSideColliders(Transform root)
        {
            EnsureChildCollider(root, "Collision_LeftWall", new Vector3(-3.9f, 2.1f, 0.2f), new Vector3(0.45f, 4.2f, 6.5f));
            EnsureChildCollider(root, "Collision_RightWall", new Vector3(3.9f, 2.1f, 0.2f), new Vector3(0.45f, 4.2f, 6.5f));
            EnsureChildCollider(root, "Collision_BackWall", new Vector3(0f, 2.1f, 3.1f), new Vector3(8.2f, 4.2f, 0.45f));
        }

        private static void EnsureChildCollider(Transform root, string name, Vector3 localPosition, Vector3 size)
        {
            Transform child = root.Find(name);
            GameObject go = child != null ? child.gameObject : new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var collider = go.GetComponent<BoxCollider>();
            if (collider == null) collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.center = Vector3.zero;
            collider.size = size;
        }
    }
}
