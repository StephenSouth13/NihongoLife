#if UNITY_EDITOR
using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Progression;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Part-time job work spots, persisted in the scenes (no runtime setup):
    /// · 90_TestSandbox — root "JobSite_Konbini" placed in the konbini's local space: job board at the entrance,
    ///   stock crate in the back, 3 shelves to restock, 2 customers, the register;
    /// · 30_SushiRestaurant — root "JobSite_Sushi": job board, order/serve spots at both dining tables (seated guests),
    ///   the kitchen pass.
    /// Each root is separate from the scene's own builder roots, so re-running CityTownBuilder never deletes it and
    /// re-running this builder replaces only its own root. Midori Island's farm manager is placed by IslandBuilder.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.JobSiteBuilder.Build
    /// </summary>
    public static class JobSiteBuilder
    {
        private const string CityPath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";
        private const string SushiPath = "Assets/NihongoLife/Scenes/30_SushiRestaurant.unity";
        private const string MatDir = "Assets/NihongoLife/Materials/Jobs";
        private const string NeighborPrefab = "Assets/NihongoLife/Resources/Characters/NL_Neighbor.prefab";
        private const string GuidePrefab = "Assets/NihongoLife/Resources/Characters/NL_Guide.prefab";
        private static Material _board, _post, _crate, _tray;

        public static void Build()
        {
            try
            {
                Materials();
                BuildKonbini();
                BuildSushi();
                RemoveLegacyJobPoints();
                AssetDatabase.SaveAssets();
                Debug.Log("[JobSiteBuilder] Job sites built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static void Materials()
        {
            if (_board != null) return;
            _board = Lit(MatDir, "Job_Board", new Color(0.13f, 0.2f, 0.27f), 0.3f);
            _post = Lit(MatDir, "Job_Post", new Color(0.55f, 0.42f, 0.28f), 0.2f);
            _crate = Lit(MatDir, "Job_Crate", new Color(0.72f, 0.55f, 0.34f), 0.15f);
            _tray = Lit(MatDir, "Job_Tray", new Color(0.18f, 0.1f, 0.06f), 0.5f);
        }

        // ─────────── Konbini (city) ───────────

        private static void BuildKonbini()
        {
            var scene = EditorSceneManager.OpenScene(CityPath, OpenSceneMode.Single);
            var konbini = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Town_Konbini");
            if (konbini == null) throw new InvalidOperationException("Town_Konbini not found in " + CityPath);
            var stale = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "JobSite_Konbini");
            if (stale != null) Object.DestroyImmediate(stale);

            // The register sign faced the staff side, so customers read it mirrored: turn it toward the shop floor.
            var registerSign = konbini.GetComponentsInChildren<TextMeshPro>(true).FirstOrDefault(t => t.name == "RegisterPrompt");
            if (registerSign != null && Vector3.Dot(registerSign.transform.forward, konbini.transform.forward) < 0f)
                registerSign.transform.Rotate(0f, 180f, 0f, Space.World);

            var site = new GameObject("JobSite_Konbini");
            site.transform.SetPositionAndRotation(konbini.transform.position, konbini.transform.rotation);
            site.AddComponent<SceneZoneVisibility>();
            const string quest = "job_konbini_shift";

            Board(site.transform, new Vector3(3.9f, 0f, 1.6f), 90f, quest, "アルバイト ぼしゅう", "Tuyển làm thêm · ¥600/ca");

            // Back room stock: a stack of cardboard boxes behind the drinks corner.
            var stock = Group(site.transform, "StockCrates");
            stock.localPosition = new Vector3(3.75f, 0f, 8.9f);
            Block(stock, "Box_A", new Vector3(0f, 0.25f, 0f), new Vector3(0.7f, 0.5f, 0.55f), _crate, true);
            Block(stock, "Box_B", new Vector3(0.05f, 0.72f, 0.02f), new Vector3(0.6f, 0.44f, 0.5f), _crate, false);
            Block(stock, "Box_C", new Vector3(-0.62f, 0.22f, 0.1f), new Vector3(0.5f, 0.44f, 0.5f), _crate, true);
            Text(stock, "Label", "ざいこ · Kho hàng", new Vector3(0f, 1.3f, -0.4f), 0f, 0.09f, new Color(0.95f, 0.85f, 0.3f), 1.2f);
            Station(site.transform, "Job_StockCrate", new Vector3(3.6f, 1f, 8.2f), new Vector3(1.6f, 2f, 1.2f), quest, JobStationKind.StockCrate, "stock", "ざいこ", "Kho hàng");

            Station(site.transform, "Job_Shelf_Onigiri", new Vector3(-1.95f, 1f, 3.2f), new Vector3(1.2f, 2f, 1.4f), quest, JobStationKind.Shelf, "shelf_onigiri", "たなに ならべる", "Kệ cơm nắm");
            Station(site.transform, "Job_Shelf_Snacks", new Vector3(-1.95f, 1f, 5.9f), new Vector3(1.2f, 2f, 1.4f), quest, JobStationKind.Shelf, "shelf_snacks", "たなに ならべる", "Kệ bánh kẹo");
            Station(site.transform, "Job_Shelf_Drinks", new Vector3(2.2f, 1f, 4.6f), new Vector3(1.4f, 2f, 3f), quest, JobStationKind.Shelf, "shelf_drinks", "たなに ならべる", "Tủ đồ uống");

            Customer(site.transform, "Job_Customer_A", new Vector3(0.5f, 0f, 3.4f), -60f, quest, "customer_a");
            Customer(site.transform, "Job_Customer_B", new Vector3(-0.6f, 0f, 6.6f), 120f, quest, "customer_b");

            var registerGuest = Person(site.transform, "RegisterGuest", new Vector3(0.95f, 0.06f, 7.45f), 0f);
            Station(site.transform, "Job_Register", new Vector3(0.3f, 1f, 7.6f), new Vector3(1.8f, 2f, 1.2f), quest, JobStationKind.Register, "register", "レジ", "Quầy thu ngân", registerGuest);

            Finish(site);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + CityPath);
        }

        // ─────────── Sushi restaurant ───────────

        private static void BuildSushi()
        {
            var scene = EditorSceneManager.OpenScene(SushiPath, OpenSceneMode.Single);
            var stale = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "JobSite_Sushi");
            if (stale != null) Object.DestroyImmediate(stale);
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
            var tableA = all.FirstOrDefault(t => t.name == "DiningTable_A");
            var tableB = all.FirstOrDefault(t => t.name == "DiningTable_B");
            var spawn = all.FirstOrDefault(t => t.name == "Spawn_" + WorldLocationCatalog.SushiEntrance);
            if (tableA == null || tableB == null || spawn == null) throw new InvalidOperationException("Dining tables or entrance spawn missing in " + SushiPath);

            var site = new GameObject("JobSite_Sushi");
            const string quest = "job_sushi_shift";
            Board(site.transform, spawn.position + new Vector3(-2.2f, 0f, 1.4f), 0f, quest, "アルバイト ぼしゅう", "Tuyển phục vụ · ¥700/ca");

            foreach (var (table, id, vi) in new[] { (tableA, "table_a", "bàn A"), (tableB, "table_b", "bàn B") })
            {
                Bounds b = BoundsOf(table.gameObject);
                // The guest sits on the far side of the table; the work spot is the table itself.
                var guest = Person(site.transform, "Guest_" + id, new Vector3(b.center.x, b.min.y, b.max.z + 0.45f), 180f);
                Station(site.transform, "Job_Order_" + id, new Vector3(b.center.x, b.min.y + 1f, b.min.z - 0.2f), new Vector3(Mathf.Max(1.4f, b.size.x), 2f, 1.4f), quest, JobStationKind.OrderTable, id, "ちゅうもん", vi, guest);
            }

            // Kitchen pass in front of Chef Ota's prep counter (SushiRestaurantRuntime centre x = 500).
            var pass = Group(site.transform, "KitchenPass");
            pass.position = new Vector3(500f, 0f, 6.25f);
            Block(pass, "PassTray", new Vector3(0f, 1.18f, 0.35f), new Vector3(1.4f, 0.04f, 0.4f), _tray, false);
            Text(pass, "Label", "できあがり · Món đã xong", new Vector3(0f, 1.6f, 0.2f), 0f, 0.09f, new Color(0.95f, 0.85f, 0.3f), 1.6f);
            Station(site.transform, "Job_DishPass", new Vector3(500f, 1f, 5.9f), new Vector3(1.8f, 2f, 1.2f), quest, JobStationKind.DishPass, "pass", "できあがり", "Quầy bếp");

            Finish(site);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + SushiPath);
        }

        /// <summary>The old one-click "JobPoint_*" spots (JobInteractable, removed) paid without any work: delete them.</summary>
        private static void RemoveLegacyJobPoints()
        {
            foreach (var path in new[] { "Assets/NihongoLife/Scenes/20_StationDistrict.unity", SushiPath })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var stale = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Where(t => t.name.StartsWith("JobPoint_")).Select(t => t.gameObject).ToList();
                foreach (var go in stale) Object.DestroyImmediate(go);
                foreach (var go in scene.GetRootGameObjects())
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                foreach (var t in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                Debug.Log($"[JobSiteBuilder] Removed {stale.Count} legacy job point(s) from {path}.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + path);
            }
        }

        // ─────────── Pieces ───────────

        public static void Board(Transform parent, Vector3 localPosition, float yaw, string quest, string titleJa, string titleVi)
        {
            var group = Group(parent, "JobBoard_" + quest);
            group.localPosition = localPosition;
            group.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Block(group, "PostL", new Vector3(-0.55f, 0.75f, 0f), new Vector3(0.08f, 1.5f, 0.08f), _post, true);
            Block(group, "PostR", new Vector3(0.55f, 0.75f, 0f), new Vector3(0.08f, 1.5f, 0.08f), _post, true);
            Block(group, "Board", new Vector3(0f, 1.45f, 0f), new Vector3(1.3f, 0.8f, 0.05f), _board, false);
            Text(group, "Title", titleJa, new Vector3(0f, 1.6f, -0.04f), 0f, 0.17f, new Color(0.98f, 0.85f, 0.4f), 1.2f);
            Text(group, "Sub", titleVi + "\n<size=80%>[F] xem việc · N sổ nhiệm vụ</size>", new Vector3(0f, 1.32f, -0.04f), 0f, 0.075f, Color.white, 1.2f);
            Station(group, "Interaction", new Vector3(0f, 1f, -0.6f), new Vector3(1.4f, 2f, 1.2f), quest, JobStationKind.Board, "board", "アルバイト", "Bảng tuyển làm thêm", local: true);
        }

        private static void Customer(Transform parent, string name, Vector3 position, float yaw, string quest, string id)
        {
            var person = Person(parent, name + "_Visual", position, yaw);
            Station(parent, name, position + new Vector3(0f, 1f, 0f), new Vector3(1.4f, 2f, 1.4f), quest, JobStationKind.Customer, id, "おきゃくさん", "Khách hàng", person);
        }

        public static GameObject Person(Transform parent, string name, Vector3 localPosition, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NeighborPrefab) ?? AssetDatabase.LoadAssetAtPath<GameObject>(GuidePrefab);
            if (prefab == null) throw new InvalidOperationException("Missing customer prefab " + NeighborPrefab);
            var person = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            person.name = name;
            person.transform.SetParent(parent, false);
            person.transform.localPosition = localPosition;
            person.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var c in person.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var mb in person.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null && mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("NihongoLife")) Object.DestroyImmediate(mb);
            person.SetActive(false); // shown by its JobStation only while the shift runs
            return person;
        }

        private static void Station(Transform parent, string name, Vector3 position, Vector3 size, string quest, JobStationKind kind, string id, string ja, string vi, GameObject visual = null, bool local = true)
        {
            var go = Trigger(parent, name, position, size);
            go.AddComponent<JobStation>().Configure(quest, kind, id, ja, vi, visual);
        }

        private static void Finish(GameObject site)
        {
            foreach (var r in site.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
        }
    }
}
#endif
