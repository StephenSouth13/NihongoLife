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
            string scenarioId = scenario != null ? scenario.id : string.Empty;
            foreach (var npc in _controlledNpcs)
            {
                if (npc == null) continue;

                string name = npc.gameObject.name;
                bool visible = name == "GuideNPC"
                    || (name == "CashierNPC" && scenarioId.Contains("konbini"))
                    || (name == "Neighbor_1" && scenarioId.Contains("house1"))
                    || (name == "Neighbor_2" && scenarioId.Contains("house2"))
                    || (name == "Neighbor_3" && scenarioId.Contains("house3"));
                npc.gameObject.SetActive(visible);
            }
        }

        public static void BuildStoreInterior()
        {
            if (GameObject.Find("StoreDoor") == null) return;

            var existingShell = GameObject.Find("StoreInteriorShell");
            if (existingShell != null) return;

            DisableLegacyStoreHouse();

            var shell = new GameObject("StoreInteriorShell");
            Material floor = CreateMaterial("StoreFloor", new Color(0.14f, 0.18f, 0.2f, 1f));
            Material wall = CreateMaterial("StoreWall", new Color(0.9f, 0.88f, 0.8f, 1f));
            Material trim = CreateMaterial("StoreTrim", new Color(0.02f, 0.38f, 0.32f, 1f));
            Material accent = CreateMaterial("StoreAccent", new Color(0.96f, 0.53f, 0.14f, 1f));

            CreateStorePiece(shell.transform, "Floor", new Vector3(0f, -0.12f, 6.4f), new Vector3(14f, 0.2f, 13f), floor);
            CreateStorePiece(shell.transform, "LeftWall", new Vector3(-7f, 2.7f, 6.4f), new Vector3(0.25f, 5.6f, 13f), wall);
            CreateStorePiece(shell.transform, "RightWall", new Vector3(7f, 2.7f, 6.4f), new Vector3(0.25f, 5.6f, 13f), wall);
            CreateStorePiece(shell.transform, "BackWall", new Vector3(0f, 2.7f, 12.8f), new Vector3(14f, 5.6f, 0.25f), wall);
            CreateStorePiece(shell.transform, "FrontLeft", new Vector3(-4.6f, 2.7f, 0.05f), new Vector3(4.8f, 5.6f, 0.25f), wall);
            CreateStorePiece(shell.transform, "FrontRight", new Vector3(4.6f, 2.7f, 0.05f), new Vector3(4.8f, 5.6f, 0.25f), wall);
            CreateStorePiece(shell.transform, "Ceiling", new Vector3(0f, 5.45f, 6.4f), new Vector3(14f, 0.2f, 13f), wall);
            CreateStorePiece(shell.transform, "CounterBackdrop", new Vector3(0f, 2.2f, 12.55f), new Vector3(8f, 3.2f, 0.18f), trim);
            CreateStorePiece(shell.transform, "BrandStripe", new Vector3(0f, 4.15f, -0.1f), new Vector3(14f, 0.22f, 0.32f), accent);

            var sign = CreateStorePiece(shell.transform, "SushiStoreSign", new Vector3(0f, 4.75f, -0.12f), new Vector3(6.8f, 0.92f, 0.24f), trim);
            var label = new GameObject("Label");
            label.transform.SetParent(shell.transform, false);
            label.transform.position = sign.transform.position + new Vector3(0f, 0f, -0.13f);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.transform.localScale = Vector3.one;
            var text = label.AddComponent<TextMesh>();
            text.text = "NIHONGO MART";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 54;
            text.characterSize = 0.12f;
            text.color = new Color(1f, 0.9f, 0.55f, 1f);

            CreateStoreLight(shell.transform, new Vector3(-4f, 4.9f, 4f));
            CreateStoreLight(shell.transform, new Vector3(0f, 4.9f, 7f));
            CreateStoreLight(shell.transform, new Vector3(4f, 4.9f, 10f));

            var cameraZone = GameObject.Find("StoreInteriorCameraZone");
            if (cameraZone != null)
            {
                cameraZone.transform.position = new Vector3(0f, 2.3f, 6.4f);
                var zoneCollider = cameraZone.GetComponent<BoxCollider>();
                if (zoneCollider != null) zoneCollider.size = new Vector3(13.2f, 5f, 11.8f);
            }

            Transform cashier = GameObject.Find("CashierNPC")?.transform;
            if (cashier != null)
            {
                cashier.position = new Vector3(0f, 0.05f, 11.65f);
                cashier.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            Transform counter = GameObject.Find("CashierCounter")?.transform;
            if (counter != null) counter.position = new Vector3(0f, 0.55f, 10.45f);
        }

        private static void DisableLegacyStoreHouse()
        {
            var legacy = GameObject.Find("KonbiniStoreAsset") ?? GameObject.Find("LegacyKonbiniStoreAsset_Disabled");
            if (legacy == null) return;
            foreach (var renderer in legacy.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var collider in legacy.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            legacy.name = "LegacyKonbiniStoreAsset_Disabled";
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
