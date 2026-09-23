using System.Collections.Generic;
using NihongoLife.NPC;
using NihongoLife.Scenario;
using UnityEngine;

namespace NihongoLife.World
{
    public class RuntimeCollisionRepair : MonoBehaviour
    {
        [SerializeField] private bool repairOnStart = true;
        [SerializeField] private Vector3 defaultBuildingSize = new Vector3(8.6f, 4.6f, 7.2f);
        private readonly List<NPCController> _controlledNpcs = new List<NPCController>();

        private void Start()
        {
            if (repairOnStart)
            {
                RepairBuildings();
                BuildStoreInterior();
                CacheControlledNpcs();
                ConfigureNpcPopulation(ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentScenario : null);
            }

            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioStarted += ConfigureNpcPopulation;
            }
        }

        private void OnDestroy()
        {
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioStarted -= ConfigureNpcPopulation;
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

        private void CacheControlledNpcs()
        {
            _controlledNpcs.Clear();
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (npc != null) _controlledNpcs.Add(npc);
            }
        }

        private void ConfigureNpcPopulation(ScenarioDefinition scenario)
        {
            // Keep authored NPCs visible. Scenario filtering here used to hide
            // most of the scene when the active scenario was empty or loading.
        }

        public static void BuildStoreInterior()
        {
            if (GameObject.Find("StoreDoor") == null) return;

            var existingShell = GameObject.Find("StoreInteriorShell");
            if (existingShell != null)
            {
                // Keep one authored store only. The old runtime shell duplicated
                // walls and furniture over the real store asset.
                Object.Destroy(existingShell);
            }

            // Store geometry and merchandise are authored in the scene. Do not
            // inject a second house/store at runtime.
            return;

        }

        private static void DisableLegacyStoreHouse()
        {
            foreach (var transform in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform == null || transform.name != "KonbiniStoreAsset") continue;
                foreach (var renderer in transform.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                foreach (var collider in transform.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                transform.name = "LegacyKonbiniStoreAsset_Disabled";
            }
        }

        private static GameObject CreateStorePiece(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.position = position;
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            return piece;
        }

        private static void CreateStoreLight(Transform parent, Vector3 position)
        {
            var go = new GameObject("CeilingLight");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.93f, 0.78f, 1f);
            light.intensity = 2.1f;
            light.range = 8f;
            light.shadows = LightShadows.None;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            return new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = name,
                color = color
            };
        }
    }
}
