#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using static NihongoLife.EditorTools.ZoneBuildKit;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Town pass for 90_TestSandbox. Removes the duplicated orphan roots left by earlier builders, wraps
    /// the konbini merchandise in a real building (ひばりマート) with its cashier, turns the four coloured
    /// portal slabs into real storefront/gate entrances (station, sushi, school, home), and adds bilingual
    /// signposts at the main crossroad. Headless: Unity -batchmode -executeMethod
    /// NihongoLife.EditorTools.CityTownBuilder.Build (no MenuItem). Idempotent: everything it creates
    /// lives under Town_* roots that are deleted and rebuilt on every run.
    /// </summary>
    public static class CityTownBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";
        private const string MatDir = "Assets/NihongoLife/Materials/City";
        private const string CashierPrefab = "Assets/NihongoLife/Prefabs/Characters/NL_Cashier.prefab";

        // Konbini footprint (interior), shared with the map.
        public const float KonbiniHalfWidth = 4.6f, KonbiniFront = 0.6f, KonbiniBack = 9.9f, KonbiniHeight = 3.4f;

        private static readonly HashSet<string> OrphanRootNames = new HashSet<string>
        {
            "DoorMat", "FacadeTrim", "WarmWindow_L", "WarmWindow_R", "Collision_Footprint", "WarmRoadLight",
            "Collision_LeftWall", "Collision_RightWall", "Collision_BackWall", "Visual"
        };

        private static Material _white, _wallGray, _green, _blue, _glass, _signGlow, _tile, _dark, _wood, _navyCloth, _lantern, _stone, _metal, _ceilingPanel, _blueSign, _red;
        private static readonly StringBuilder Log = new StringBuilder();

        public static void Build()
        {
            try
            {
                Log.Clear();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                CreateMaterials();
                RemoveOrphansAndLegacy(scene);

                var konbini = NewRoot(scene, "Town_Konbini");
                BuildKonbini(konbini);
                ClearKonbiniOverlaps();
                ArrangeStoreProps(konbini);
                FixLillyMaterials();
                EnsureRiggedCashierPrefabs();
                BuildCashier(konbini);

                BuildPlaza(NewRoot(scene, "Town_Plaza"));

                var destinations = NewRoot(scene, "Town_Destinations");
                BuildDestinations(destinations);

                var signs = NewRoot(scene, "Town_Signposts");
                BuildSignposts(signs);

                Validate(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Scene save failed.");
                AssetDatabase.SaveAssets();
                Debug.Log("[CityTownBuilder] Done.\n" + Log);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        // ─────────── Cleanup ───────────

        private static void RemoveOrphansAndLegacy(Scene scene)
        {
            int orphans = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                bool deadCamera = root.name == "Main Camera" && !root.activeSelf && root.GetComponent("ThirdPersonCameraController") == null;
                bool previousPass = root.name.StartsWith("Town_", StringComparison.Ordinal);
                if (OrphanRootNames.Contains(root.name) || deadCamera || previousPass)
                {
                    if (!previousPass) orphans++;
                    Object.DestroyImmediate(root);
                }
            }
            Log.AppendLine($"Removed {orphans} orphan/duplicate root objects.");

            var environment = GameObject.Find("Environment").transform;
            foreach (string legacy in new[] { "KonbiniSign", "Shelf_Food", "Shelf_Drinks", "CashierNPC" })
            {
                var t = environment.Find(legacy);
                if (t != null) { Object.DestroyImmediate(t.gameObject); Log.AppendLine("Removed legacy Environment/" + legacy); }
            }
            var storeVisuals = GameObject.Find("ThirdParty_Integrated_World/Store_ThirdParty_Visuals");
            var oldSign = storeVisuals != null ? storeVisuals.transform.Find("StoreSignVisual") : null;
            if (oldSign != null) Object.DestroyImmediate(oldSign.gameObject);
        }

        private static Transform NewRoot(Scene scene, string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            var visibility = go.AddComponent<SceneZoneVisibility>();
            var so = new SerializedObject(visibility);
            var prop = so.FindProperty("hideWhenZoneChanges");
            if (prop != null) { prop.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
            return go.transform;
        }

        // ─────────── Konbini building ───────────

        private static void BuildKonbini(Transform root)
        {
            float w = KonbiniHalfWidth, z0 = KonbiniFront, z1 = KonbiniBack, h = KonbiniHeight;
            float depth = z1 - z0, cz = (z0 + z1) / 2f;
            var shell = Group(root, "Shell");
            Block(shell, "Floor", new Vector3(0f, 0.03f, cz), new Vector3(2 * w, 0.06f, depth), _tile, true);
            Block(shell, "Wall_L", new Vector3(-w - 0.15f, h / 2f, cz), new Vector3(0.3f, h, depth + 0.3f), _white, true);
            Block(shell, "Wall_R", new Vector3(w + 0.15f, h / 2f, cz), new Vector3(0.3f, h, depth + 0.3f), _white, true);
            Block(shell, "Wall_Back", new Vector3(0f, h / 2f, z1 + 0.15f), new Vector3(2 * w + 0.6f, h, 0.3f), _white, true);
            Block(shell, "Roof", new Vector3(0f, h + 0.1f, cz), new Vector3(2 * w + 0.8f, 0.2f, depth + 0.8f), _wallGray, true);
            Block(shell, "Ceiling", new Vector3(0f, h - 0.02f, cz), new Vector3(2 * w, 0.04f, depth), _white, false);
            Block(shell, "RoofUnit_A", new Vector3(-2.2f, h + 0.5f, z1 - 2f), new Vector3(1.2f, 0.6f, 0.9f), _metal, false);
            Block(shell, "RoofUnit_B", new Vector3(1.6f, h + 0.45f, z1 - 1.6f), new Vector3(0.9f, 0.5f, 0.9f), _metal, false);

            // Glass shop front either side of the existing sliding door (x ∈ [-1.95, 1.95]).
            foreach (float side in new[] { -1f, 1f })
            {
                float inner = 2.0f * side, outer = w * side, mid = (inner + outer) / 2f, span = Mathf.Abs(outer - inner);
                string s = side < 0 ? "L" : "R";
                Block(shell, "FrontBase_" + s, new Vector3(mid, 0.18f, z0), new Vector3(span, 0.36f, 0.2f), _white, true);
                Block(shell, "FrontGlass_" + s, new Vector3(mid, 1.45f, z0), new Vector3(span, 2.2f, 0.05f), _glass, true);
                Block(shell, "Mullion_" + s, new Vector3(mid, 1.45f, z0 - 0.03f), new Vector3(0.06f, 2.2f, 0.08f), _dark, false);
                Block(shell, "Corner_" + s, new Vector3(outer, 1.45f, z0), new Vector3(0.12f, 2.2f, 0.12f), _dark, false);
                // Posters in the window — opening hours and today's deal.
                Text(shell, "WindowPoster_" + s, side < 0 ? "<b>24じかん</b>\nOPEN 24h" : "<b>おにぎり</b>\n¥150〜", new Vector3(mid + 0.6f * side, 1.95f, z0 - 0.05f), 0f, 0.13f, side < 0 ? new Color(0.1f, 0.45f, 0.3f) : new Color(0.85f, 0.25f, 0.2f), 1.2f);
            }
            // Fascia band with the konbini stripes and the lit sign box.
            Block(shell, "Fascia", new Vector3(0f, 2.98f, z0), new Vector3(2 * w + 0.6f, 0.84f, 0.24f), _white, true);
            Block(shell, "Stripe_Green", new Vector3(0f, 2.68f, z0 - 0.13f), new Vector3(2 * w + 0.6f, 0.14f, 0.02f), _green, false);
            Block(shell, "Stripe_Blue", new Vector3(0f, 2.86f, z0 - 0.13f), new Vector3(2 * w + 0.6f, 0.1f, 0.02f), _blue, false);
            Block(shell, "SignBox", new Vector3(0f, 3.95f, z0 + 0.1f), new Vector3(4.2f, 0.9f, 0.3f), _signGlow, false);
            Text(shell, "SignText_JA", "ひばりマート", new Vector3(0f, 4.05f, z0 - 0.06f), 0f, 0.42f, new Color(0.08f, 0.42f, 0.28f), 4f);
            Text(shell, "SignText_EN", "HIBARI MART  ·  Cửa hàng tiện lợi", new Vector3(0f, 3.68f, z0 - 0.06f), 0f, 0.15f, new Color(0.12f, 0.24f, 0.5f), 4f);
            Block(shell, "Canopy", new Vector3(0f, 2.62f, z0 - 0.55f), new Vector3(4.6f, 0.08f, 1.1f), _green, false);
            Block(shell, "EntranceMat", new Vector3(0f, 0.065f, z0 - 0.7f), new Vector3(2.6f, 0.02f, 1.0f), _dark, false);

            // Interior lighting: ceiling panels + lights (no shadows, cheap).
            var lights = Group(root, "Lights");
            for (int i = 0; i < 3; i++)
            {
                float z = z0 + 1.8f + i * 3f;
                Block(lights, "CeilingPanel_L" + i, new Vector3(-2f, h - 0.06f, z), new Vector3(1.6f, 0.04f, 0.45f), _ceilingPanel, false);
                Block(lights, "CeilingPanel_R" + i, new Vector3(2f, h - 0.06f, z), new Vector3(1.6f, 0.04f, 0.45f), _ceilingPanel, false);
                PointLight(lights, "StoreLight_" + i, new Vector3(0f, h - 0.4f, z), 7.5f, 1.25f, new Color(0.98f, 0.98f, 1f));
            }
            PointLight(lights, "SignLight", new Vector3(0f, 3.5f, z0 - 1.2f), 5f, 0.9f, new Color(1f, 0.96f, 0.88f));

            // Aisle signs hanging from the ceiling (bilingual) — tells the player where each quest item is.
            var aisle = Group(root, "AisleSigns");
            HangingSign(aisle, "Sign_Food", "おにぎり・おかし\nCơm nắm · Đồ ăn", new Vector3(-3.0f, 2.55f, 4.4f));
            HangingSign(aisle, "Sign_Drinks", "のみもの\nĐồ uống", new Vector3(3.0f, 2.55f, 4.4f));
            HangingSign(aisle, "Sign_Register", "レジ\nThanh toán", new Vector3(0f, 2.6f, 7.4f));
        }

        private static void HangingSign(Transform parent, string name, string text, Vector3 position)
        {
            Block(parent, name + "_Board", position, new Vector3(1.6f, 0.48f, 0.05f), _blueSign, false);
            Block(parent, name + "_Wire", position + new Vector3(0f, 0.5f, 0f), new Vector3(0.02f, 0.55f, 0.02f), _dark, false);
            Text(parent, name + "_Front", text, position + new Vector3(0f, 0f, -0.035f), 0f, 0.13f, Color.white, 1.5f);
            Text(parent, name + "_Back", text, position + new Vector3(0f, 0f, 0.035f), 180f, 0.13f, Color.white, 1.5f);
        }

        /// <summary>The house slot west of the store (StreetBuilding_N_4) is a single 19 m mesh that covers
        /// the whole store lot, and the old KonbiniStoreAsset carried a stray fascia bar plus three invisible
        /// collision walls in the middle of the aisles. Both are superseded by the new building.</summary>
        private static void ClearKonbiniOverlaps()
        {
            foreach (string path in new[] { "Environment/CityTile_0/StreetBuilding_N_4", "Environment/CityTile_0/KonbiniStoreAsset" })
            {
                var go = GameObject.Find(path);
                if (go == null) continue;
                go.SetActive(false);
                Log.AppendLine("Disabled superseded " + path);
            }
            var oldCounter = GameObject.Find("Environment/CashierCounter");
            if (oldCounter != null) { Object.DestroyImmediate(oldCounter); Log.AppendLine("Removed duplicate Environment/CashierCounter"); }
        }

        private static void ArrangeStoreProps(Transform konbini)
        {
            var tile = GameObject.Find("Environment/CityTile_0").transform;
            // Vending machine stands outside, right of the entrance, facing the street.
            MoveTo(tile, "OutdoorDrinkFridge", new Vector3(5.55f, 0f, -0.15f), 180f);
            // Coffee machine becomes the in-store coffee corner by the window.
            MoveTo(tile, "OutdoorCoffeeMachine", new Vector3(-3.75f, 0.6f, 1.35f), 0f);
            // Delivery boxes stacked tidily along the outside wall.
            for (int i = 0; i < 4; i++) MoveTo(tile, "DeliveryBox_" + i, new Vector3(-5.35f, 0f, 2.2f + i * 0.85f), 0f);
            MoveTo(tile, "StorefrontPlant_R", new Vector3(2.6f, 0f, -0.6f), 0f);
            // The bus-stop bench was sunk into the store's east wall; it belongs on the sidewalk.
            var bench = tile.Find("Bench_Stop_North");
            if (bench != null)
            {
                // Its mesh sits far from its pivot, so move by the rendered centre, not the transform.
                Bounds bb = BoundsOf(bench.gameObject);
                bench.position += new Vector3(6.6f - bb.center.x, 0f, -2.7f - bb.center.z);
            }
            foreach (string basket in new[] { "FoodShelf_A", "FoodShelf_B" })
            {
                var t = GameObject.Find("ThirdParty_Integrated_World/Store_ThirdParty_Visuals/" + basket);
                if (t != null) t.SetActive(false); // replaced by the gondola shelving
            }

            BuildGondola(konbini, "Gondola_A", 2.55f, 3.8f);
            BuildGondola(konbini, "Gondola_B", 5.25f, 6.5f);
            BuildCounter(konbini);

            // Quest items sit on real fixtures instead of floating at shoulder height.
            var env = GameObject.Find("Environment").transform;
            var cold = GameObject.Find("ThirdParty_Integrated_World/Store_ThirdParty_Visuals/ColdDisplay");
            var onigiri = env.Find("Onigiri");
            if (onigiri != null) onigiri.position = new Vector3(-2.72f, GondolaLevels[2] + 0.08f, 6.2f);
            PlaceItem(env, "Water", cold, new Vector3(-0.32f, 0f, -0.15f));
            PlaceItem(env, "Tea", cold, new Vector3(-0.32f, 0f, 0.2f));
            var labels = Group(konbini, "PriceTags");
            foreach (var (item, label) in new[] { ("Onigiri", "おにぎり ¥150"), ("Water", "みず ¥120"), ("Tea", "おちゃ ¥150") })
            {
                var t = env.Find(item);
                if (t == null) continue;
                Text(labels, "Tag_" + item, label, t.position + new Vector3(0f, 0.28f, -0.05f), 0f, 0.07f, new Color(0.95f, 0.85f, 0.3f), 0.8f);
            }
        }

        private static readonly float[] GondolaLevels = { 0.64f, 1.14f, 1.59f };

        /// <summary>White supermarket shelf unit along the west aisle; shelf tops match the heights the
        /// existing stock items were authored at, so nothing floats any more.</summary>
        private static void BuildGondola(Transform root, string name, float z0, float z1)
        {
            var g = Group(root, name);
            float x0 = -3.5f, x1 = -2.55f, cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f, len = z1 - z0;
            Block(g, "Plinth", new Vector3(cx, 0.08f, cz), new Vector3(x1 - x0, 0.16f, len), _white, true);
            Block(g, "BackPanel", new Vector3(x0 - 0.03f, 1.0f, cz), new Vector3(0.06f, 2.0f, len), _white, true);
            Block(g, "Side_A", new Vector3(cx, 1.0f, z0), new Vector3(x1 - x0, 2.0f, 0.04f), _white, false);
            Block(g, "Side_B", new Vector3(cx, 1.0f, z1), new Vector3(x1 - x0, 2.0f, 0.04f), _white, false);
            foreach (float level in GondolaLevels)
            {
                Block(g, "Shelf_" + level.ToString("0.00"), new Vector3(cx, level - 0.02f, cz), new Vector3(x1 - x0, 0.04f, len), _white, false);
                Block(g, "PriceRail_" + level.ToString("0.00"), new Vector3(x1 + 0.01f, level - 0.05f, cz), new Vector3(0.02f, 0.05f, len), _green, false);
            }
            Block(g, "Header", new Vector3(cx + 0.2f, 2.08f, cz), new Vector3(0.5f, 0.16f, len), _green, false);
        }

        private static void BuildCounter(Transform root)
        {
            var c = Group(root, "Register");
            // One collision volume for the two checkout counter models.
            var blocker = new GameObject("CounterCollision");
            blocker.transform.SetParent(c, false);
            blocker.transform.position = new Vector3(-0.05f, 0.45f, 8.55f);
            blocker.AddComponent<BoxCollider>().size = new Vector3(4.2f, 0.9f, 0.8f);
            Block(c, "Till", new Vector3(0.95f, 0.95f, 8.6f), new Vector3(0.42f, 0.18f, 0.36f), _dark, false);
            Block(c, "TillScreen", new Vector3(0.95f, 1.17f, 8.68f), new Vector3(0.36f, 0.24f, 0.03f), _blueSign, false);
            Block(c, "BagStand", new Vector3(-0.35f, 0.9f, 8.6f), new Vector3(0.3f, 0.12f, 0.25f), _white, false);
            Text(c, "RegisterPrompt", "レジ · Thanh toán ở đây", new Vector3(0f, 0.6f, 8.13f), 0f, 0.07f, new Color(0.95f, 0.85f, 0.3f), 1.6f);
        }

        private static void MoveTo(Transform parent, string name, Vector3 position, float yaw)
        {
            var t = parent.Find(name);
            if (t == null) { Log.AppendLine("Prop not found: " + name); return; }
            t.position = position;
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void PlaceItem(Transform env, string name, GameObject fixture, Vector3 offset)
        {
            var item = env.Find(name);
            if (item == null) { Log.AppendLine("Quest item missing: " + name); return; }
            if (fixture == null) { Log.AppendLine("Fixture missing for " + name); return; }
            Bounds f = BoundsOf(fixture);
            item.position = new Vector3(f.center.x + offset.x, f.max.y + 0.12f, f.center.z + offset.z);
        }

        // ─────────── Cashier ───────────

        /// <summary>The Lilly character (guide + clerk) rendered plain white: six of its URP material
        /// copies were created without their textures. Re-link each to its &lt;name&gt;_Diffuse / _Normal file.</summary>
        private static void FixLillyMaterials()
        {
            const string textures = "Assets/ThirdParty/Mixamo/Characters/lilly/textures/";
            foreach (string guid in AssetDatabase.FindAssets("NL_Guide__ t:Material", new[] { "Assets/NihongoLife/Materials/Characters" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                int start = file.IndexOf("Lilly_0_", StringComparison.Ordinal);
                if (mat == null || start < 0) continue;
                string source = file.Substring(start + "Lilly_0_".Length).Replace("_URP", "");
                var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(textures + source + "_Diffuse.png");
                if (diffuse == null) { Log.AppendLine("No diffuse for " + source); continue; }
                bool changed = mat.GetTexture("_BaseMap") != diffuse;
                mat.SetTexture("_BaseMap", diffuse);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diffuse);
                mat.SetColor("_BaseColor", Color.white);
                string normalPath = textures + source + "_Normal.png";
                if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter)
                {
                    if (normalImporter.textureType != TextureImporterType.NormalMap)
                    {
                        normalImporter.textureType = TextureImporterType.NormalMap;
                        normalImporter.SaveAndReimport();
                    }
                    mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                    mat.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(mat);
                if (changed) Log.AppendLine("Linked texture for " + file);
            }
        }

        private const string GuidePrefab = "Assets/NihongoLife/Prefabs/Characters/NL_Guide.prefab";
        private static readonly string[] CashierPrefabs = { CashierPrefab, "Assets/NihongoLife/Resources/Characters/NL_Cashier.prefab" };

        /// <summary>The Elizabeth model behind NL_Cashier has no skeleton (static T-pose mesh), so every
        /// shop clerk using it stood in a T-pose. Rebuild both NL_Cashier prefabs (same GUIDs, so every
        /// scene reference picks it up) from the rigged Lilly character plus a green konbini apron.</summary>
        private static void EnsureRiggedCashierPrefabs()
        {
            foreach (string path in CashierPrefabs)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var existingAnimator = existing != null ? existing.GetComponentInChildren<Animator>(true) : null;
                if (existingAnimator != null && existingAnimator.avatar != null && existingAnimator.avatar.isHuman && existing.transform.Find("ClerkUniformMarker") != null)
                    continue;
                var contents = PrefabUtility.LoadPrefabContents(GuidePrefab);
                try
                {
                    var animator = contents.GetComponentInChildren<Animator>(true);
                    Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                    Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                    Transform chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
                    float bottom = hips.position.y - 0.32f, top = chest.position.y + 0.08f;
                    var apron = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    apron.name = "UniformApron";
                    Object.DestroyImmediate(apron.GetComponent<Collider>());
                    apron.GetComponent<Renderer>().sharedMaterial = _green;
                    apron.transform.SetParent(spine, true);
                    apron.transform.position = new Vector3(hips.position.x, (bottom + top) / 2f, hips.position.z + 0.13f);
                    apron.transform.rotation = contents.transform.rotation;
                    apron.transform.localScale = Vector3.one;
                    var lossy = apron.transform.lossyScale;
                    apron.transform.localScale = new Vector3(0.36f / lossy.x, (top - bottom) / lossy.y, 0.02f / lossy.z);
                    // Marker so the check above can see the prefab is already the rigged clerk.
                    new GameObject("ClerkUniformMarker").transform.SetParent(contents.transform, false);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    Log.AppendLine("Rebuilt rigged clerk prefab " + path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        private static void BuildCashier(Transform konbini)
        {
            var npc = new GameObject("CashierNPC");
            npc.layer = InteractableLayer;
            npc.transform.SetParent(konbini, false);
            npc.transform.SetPositionAndRotation(new Vector3(0f, 0.06f, 9.15f), Quaternion.Euler(0f, 180f, 0f));
            var collider = npc.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.95f, 0f);
            collider.size = new Vector3(0.9f, 1.9f, 0.9f);
            collider.isTrigger = true;
            var animation = npc.AddComponent<CharacterAnimationController>();
            npc.AddComponent<NPCAmbientTalker>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CashierPrefab);
            if (prefab == null) throw new InvalidOperationException("Missing " + CashierPrefab);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            visual.name = "Visual";
            visual.transform.SetParent(npc.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            animation.SetAnimator(visual.GetComponentInChildren<Animator>(true));

            var so = new SerializedObject(npc.AddComponent<NPCController>());
            so.FindProperty("npcId").stringValue = "npc_cashier";
            so.FindProperty("displayName").stringValue = "Ito";
            so.FindProperty("role").stringValue = "Cashier";
            so.FindProperty("promptJa").stringValue = "かいけいする";
            so.FindProperty("promptEn").stringValue = "Thanh toán với chị Ito";
            so.FindProperty("scenarioAreaIdOnInteract").stringValue = "cashier";
            so.FindProperty("fallbackJa").stringValue = "いらっしゃいませ！";
            so.FindProperty("fallbackReading").stringValue = "いらっしゃいませ！";
            so.FindProperty("fallbackEn").stringValue = "Xin mời quý khách!";
            so.FindProperty("fallbackRomaji").stringValue = "Irasshaimase!";
            so.ApplyModifiedPropertiesWithoutUndo();
            Text(konbini, "CashierNameplate", "いとう · Ito", new Vector3(0f, 2.05f, 9.15f), 0f, 0.09f, Color.white, 0.8f);
        }

        // ─────────── Neighbourhood plaza on the freed lot west of the store ───────────

        private static void BuildPlaza(Transform root)
        {
            Block(root, "Paving", new Vector3(-8.1f, 0.03f, 4.6f), new Vector3(6.0f, 0.06f, 8.4f), _stone, true);
            // Community notice board: garbage days (house3_garbage) and the summer festival.
            foreach (float x in new[] { -9.4f, -6.8f })
                Block(root, "BoardPost_" + x.ToString("0.0"), new Vector3(x, 0.9f, 1.3f), new Vector3(0.1f, 1.8f, 0.1f), _wood, true);
            Block(root, "NoticeBoard", new Vector3(-8.1f, 1.45f, 1.3f), new Vector3(2.7f, 1.0f, 0.08f), _wood, false);
            Block(root, "NoticeRoof", new Vector3(-8.1f, 2.05f, 1.3f), new Vector3(2.9f, 0.08f, 0.4f), _wallGray, false);
            Text(root, "Notice_Title", "<b>ひばりちょう けいじばん</b>  ·  Bảng tin khu phố", new Vector3(-8.1f, 1.82f, 1.24f), 0f, 0.09f, new Color(1f, 0.95f, 0.85f), 2.6f);
            Block(root, "Notice_Paper_A", new Vector3(-8.85f, 1.38f, 1.25f), new Vector3(1.1f, 0.62f, 0.01f), _white, false);
            Text(root, "Notice_A", "<b>ゴミの日</b>\nもえるゴミ: げつ・もく\nしげんゴミ: すい\n<size=80%>Lịch đổ rác</size>", new Vector3(-8.85f, 1.38f, 1.24f), 0f, 0.07f, new Color(0.2f, 0.22f, 0.28f), 1.05f);
            Block(root, "Notice_Paper_B", new Vector3(-7.4f, 1.38f, 1.25f), new Vector3(1.1f, 0.62f, 0.01f), _red, false);
            Text(root, "Notice_B", "<b>なつまつり</b>\n8がつ30にち\nひばりこうえん\n<size=80%>Lễ hội mùa hè</size>", new Vector3(-7.4f, 1.38f, 1.24f), 0f, 0.07f, Color.white, 1.05f);
            // Bicycle parking.
            for (int i = 0; i < 5; i++)
                Block(root, "BikeRack_" + i, new Vector3(-10.4f, 0.4f, 3.4f + i * 0.9f), new Vector3(0.06f, 0.8f, 0.6f), _metal, true);
            Block(root, "BikeRackRail", new Vector3(-10.4f, 0.78f, 5.2f), new Vector3(0.06f, 0.05f, 4.2f), _metal, false);
            // Benches facing the plaza centre.
            foreach (float z in new[] { 4.2f, 6.6f })
            {
                Block(root, "BenchSeat_" + z, new Vector3(-6.4f, 0.45f, z), new Vector3(0.5f, 0.08f, 1.6f), _wood, true);
                Block(root, "BenchBack_" + z, new Vector3(-6.15f, 0.75f, z), new Vector3(0.08f, 0.5f, 1.6f), _wood, false);
                Block(root, "BenchLegs_" + z, new Vector3(-6.4f, 0.22f, z), new Vector3(0.4f, 0.44f, 1.4f), _metal, false);
            }
            var tree = GameObject.Find("Environment/CityTile_0/Tree_N_0");
            if (tree != null)
                foreach (var p in new[] { new Vector3(-9.6f, 0f, 8.2f), new Vector3(-6.6f, 0f, 8.4f) })
                {
                    var copy = Object.Instantiate(tree, root);
                    copy.name = "PlazaTree";
                    copy.transform.position = p;
                }
            PointLight(root, "PlazaLight", new Vector3(-8.1f, 3f, 4.5f), 8f, 0.6f, new Color(1f, 0.88f, 0.7f));
        }

        // ─────────── Destinations (portals become real entrances) ───────────

        private sealed class Destination
        {
            public string Portal, Spawn, Building, Ja, Vi, PromptJa, PromptEn;
            public bool FacesNorth;
        }

        private static readonly Destination[] Destinations =
        {
            new Destination { Portal = "HomeBedroomPortal", Spawn = "Spawn_city_home_return", Building = "StreetBuilding_N_2", Ja = "ひばりハイツ 2-14", Vi = "Nhà trọ của bạn", PromptJa = "うちに かえる", PromptEn = "Về phòng trọ" },
            new Destination { Portal = "SushiPortal", Spawn = "Spawn_city_sushi_return", Building = "StreetBuilding_N_3", Ja = "ひばり寿司", Vi = "Nhà hàng Sushi Hibari", PromptJa = "みせに はいる", PromptEn = "Vào quán sushi" },
            new Destination { Portal = "StationPortal", Spawn = "Spawn_city_station_return", Building = "StreetBuilding_N_7", Ja = "ひばり駅", Vi = "Ga Hibari · Tàu điện", PromptJa = "えきに はいる", PromptEn = "Vào ga tàu" },
            new Destination { Portal = "SchoolPortal", Spawn = "Spawn_city_school_return", Building = "StreetBuilding_S_5", Ja = "ひばり日本語学院", Vi = "Trường Nhật ngữ Hibari", PromptJa = "がっこうに はいる", PromptEn = "Vào trường", FacesNorth = true },
        };

        /// <summary>World position (on the sidewalk) of each entrance, filled during the build and used by the signposts.</summary>
        private static readonly Dictionary<string, Vector3> EntrancePoints = new Dictionary<string, Vector3>();

        private static void BuildDestinations(Transform root)
        {
            EntrancePoints.Clear();
            var portals = GameObject.Find("AdditiveZonePortals").transform;
            foreach (var d in Destinations)
            {
                var building = GameObject.Find("Environment/CityTile_0/" + d.Building);
                if (building == null) throw new InvalidOperationException("Missing building " + d.Building);
                Bounds b = BoundsOf(building);
                float frontZ = d.FacesNorth ? b.max.z : b.min.z;
                float outward = d.FacesNorth ? 1f : -1f; // direction from the facade toward the street
                float yaw = d.FacesNorth ? 180f : 0f;     // text readable from the street
                var group = Group(root, "Entrance_" + d.Portal.Replace("Portal", ""));
                group.position = new Vector3(building.transform.position.x, 0f, frontZ);
                group.rotation = Quaternion.Euler(0f, d.FacesNorth ? 180f : 0f, 0f);

                switch (d.Portal)
                {
                    case "StationPortal": StationGate(group, d); break;
                    case "SushiPortal": SushiFront(group, d); break;
                    case "SchoolPortal": SchoolGate(group, d); break;
                    default: ApartmentDoor(group, d); break;
                }

                // Re-home the portal trigger in front of the new entrance; hide the old coloured slab.
                var portal = portals.Find(d.Portal);
                if (portal == null) throw new InvalidOperationException("Missing portal " + d.Portal);
                foreach (Transform child in portal.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                var renderer = portal.GetComponent<MeshRenderer>();
                if (renderer != null) Object.DestroyImmediate(renderer);
                var filter = portal.GetComponent<MeshFilter>();
                if (filter != null) Object.DestroyImmediate(filter);
                portal.position = new Vector3(group.position.x, 1.1f, frontZ + outward * 0.6f);
                portal.rotation = Quaternion.identity;
                portal.localScale = Vector3.one;
                var box = portal.GetComponent<BoxCollider>();
                box.center = Vector3.zero;
                box.size = new Vector3(2.6f, 2.2f, 1.2f);
                box.isTrigger = true;
                portal.gameObject.layer = InteractableLayer;
                var ps = new SerializedObject(portal.GetComponent<ScenePortal>());
                ps.FindProperty("promptJa").stringValue = d.PromptJa;
                ps.FindProperty("promptEn").stringValue = d.PromptEn;
                ps.ApplyModifiedPropertiesWithoutUndo();

                var spawn = portals.Find(d.Spawn);
                if (spawn == null) throw new InvalidOperationException("Missing spawn " + d.Spawn);
                spawn.SetPositionAndRotation(new Vector3(group.position.x, 0.08f, frontZ + outward * 2.4f), Quaternion.Euler(0f, d.FacesNorth ? 0f : 180f, 0f));
                EntrancePoints[d.Portal] = new Vector3(group.position.x, 0f, frontZ + outward * 1.2f);
                Log.AppendLine($"{d.Portal} → {portal.position} (spawn {spawn.position})");
            }
        }

        // All entrance pieces are authored in the group's local space: +X along the facade, −Z toward the street.

        private static void StationGate(Transform g, Destination d)
        {
            Block(g, "Canopy", new Vector3(0f, 3.2f, -1.4f), new Vector3(7.2f, 0.22f, 3.0f), _wallGray, false);
            Block(g, "CanopyEdge", new Vector3(0f, 3.05f, -2.85f), new Vector3(7.2f, 0.3f, 0.1f), _blue, false);
            foreach (float x in new[] { -3.3f, 3.3f })
                Block(g, "Pillar_" + (x < 0 ? "L" : "R"), new Vector3(x, 1.55f, -2.6f), new Vector3(0.3f, 3.1f, 0.3f), _metal, true);
            Block(g, "SignBoard", new Vector3(0f, 3.75f, -2.75f), new Vector3(4.6f, 0.9f, 0.15f), _blueSign, false);
            Text(g, "Sign_JA", "ひばり駅", new Vector3(0f, 3.88f, -2.84f), 0f, 0.42f, Color.white, 4.4f);
            Text(g, "Sign_VI", "HIBARI STATION · Ga tàu điện", new Vector3(0f, 3.52f, -2.84f), 0f, 0.14f, new Color(0.85f, 0.92f, 1f), 4.4f);
            Block(g, "EntranceDark", new Vector3(0f, 1.3f, -0.02f), new Vector3(4.2f, 2.6f, 0.06f), _dark, false);
            for (int i = 0; i < 3; i++)
            {
                float x = -1.3f + i * 1.3f;
                Block(g, "Gate_" + i, new Vector3(x, 0.5f, -0.6f), new Vector3(0.25f, 1.0f, 1.1f), _metal, true);
                Block(g, "GateLight_" + i, new Vector3(x, 1.02f, -0.95f), new Vector3(0.18f, 0.04f, 0.2f), _green, false);
            }
            Block(g, "RouteMap", new Vector3(-2.7f, 1.5f, -0.1f), new Vector3(1.2f, 0.8f, 0.05f), _white, false);
            Text(g, "RouteMap_Text", "<b>ろせんず</b>\nひばり → さくら → みどり", new Vector3(-2.7f, 1.5f, -0.14f), 0f, 0.07f, new Color(0.12f, 0.24f, 0.5f), 1.1f);
            PointLight(g, "CanopyLight", new Vector3(0f, 2.8f, -1.6f), 6f, 1.0f, new Color(0.92f, 0.96f, 1f));
        }

        private static void SushiFront(Transform g, Destination d)
        {
            Block(g, "WoodFrame_Top", new Vector3(0f, 2.55f, -0.12f), new Vector3(3.2f, 0.18f, 0.2f), _wood, false);
            foreach (float x in new[] { -1.5f, 1.5f })
                Block(g, "WoodPost_" + (x < 0 ? "L" : "R"), new Vector3(x, 1.27f, -0.12f), new Vector3(0.16f, 2.55f, 0.18f), _wood, true);
            Block(g, "SlidingDoor", new Vector3(0f, 1.05f, -0.06f), new Vector3(2.8f, 2.1f, 0.05f), _dark, false);
            for (int i = 0; i < 3; i++)
            {
                float x = -0.95f + i * 0.95f;
                Block(g, "Noren_" + i, new Vector3(x, 2.1f, -0.24f), new Vector3(0.9f, 0.8f, 0.02f), _navyCloth, false);
            }
            Text(g, "Noren_Text", "す　し", new Vector3(0f, 2.12f, -0.26f), 0f, 0.32f, Color.white, 3f);
            foreach (float x in new[] { -1.95f, 1.95f })
            {
                var lantern = Cylinder(g, "Chochin_" + (x < 0 ? "L" : "R"), new Vector3(x, 2.05f, -0.35f), new Vector3(0.38f, 0.28f, 0.38f), _lantern, false);
                PointLight(g, "ChochinLight_" + (x < 0 ? "L" : "R"), new Vector3(x, 2.05f, -0.7f), 3f, 0.8f, new Color(1f, 0.45f, 0.3f));
            }
            Block(g, "Signboard", new Vector3(0f, 3.05f, -0.15f), new Vector3(2.8f, 0.6f, 0.08f), _wood, false);
            Text(g, "Sign_JA", "ひばり寿司", new Vector3(0f, 3.1f, -0.2f), 0f, 0.3f, new Color(1f, 0.93f, 0.8f), 2.8f);
            Text(g, "Sign_VI", "Sushi Hibari", new Vector3(0f, 2.86f, -0.2f), 0f, 0.1f, new Color(1f, 0.85f, 0.6f), 2.8f);
            Block(g, "MenuStand", new Vector3(2.4f, 0.55f, -0.8f), new Vector3(0.6f, 1.1f, 0.08f), _dark, true);
            Text(g, "MenuStand_Text", "<b>おすすめ</b>\nまぐろ ¥280\nサーモン ¥250", new Vector3(2.4f, 0.65f, -0.85f), 0f, 0.06f, Color.white, 0.6f);
        }

        private static void SchoolGate(Transform g, Destination d)
        {
            foreach (float x in new[] { -1.9f, 1.9f })
                Block(g, "GatePillar_" + (x < 0 ? "L" : "R"), new Vector3(x, 0.95f, -0.5f), new Vector3(0.6f, 1.9f, 0.6f), _stone, true);
            foreach (float x in new[] { -3.6f, 3.6f })
                Block(g, "LowWall_" + (x < 0 ? "L" : "R"), new Vector3(x, 0.45f, -0.5f), new Vector3(2.8f, 0.9f, 0.35f), _stone, true);
            Block(g, "Plaque", new Vector3(-1.9f, 1.3f, -0.82f), new Vector3(0.42f, 1.0f, 0.04f), _wood, false);
            Text(g, "Plaque_Text", "ひ\nば\nり\n日\n本\n語\n学\n院", new Vector3(-1.9f, 1.3f, -0.85f), 0f, 0.1f, new Color(1f, 0.95f, 0.85f), 0.4f);
            Block(g, "Banner", new Vector3(0f, 2.45f, -0.5f), new Vector3(3.4f, 0.5f, 0.06f), _green, false);
            Text(g, "Banner_Text", "ひばり日本語学院 · Trường Nhật ngữ Hibari", new Vector3(0f, 2.45f, -0.55f), 0f, 0.13f, Color.white, 3.3f);
            foreach (float x in new[] { -1.9f, 1.9f })
                Block(g, "BannerPole_" + (x < 0 ? "L" : "R"), new Vector3(x, 2.25f, -0.5f), new Vector3(0.06f, 0.9f, 0.06f), _metal, false);
            Block(g, "Path", new Vector3(0f, 0.07f, -0.4f), new Vector3(3.0f, 0.02f, 1.6f), _stone, false);
            PointLight(g, "GateLight", new Vector3(0f, 2.2f, -1.4f), 5f, 0.7f, new Color(1f, 0.92f, 0.8f));
        }

        private static void ApartmentDoor(Transform g, Destination d)
        {
            Block(g, "DoorFrame", new Vector3(0f, 1.1f, -0.06f), new Vector3(1.3f, 2.25f, 0.1f), _white, false);
            Block(g, "Door", new Vector3(0f, 1.05f, -0.12f), new Vector3(1.0f, 2.05f, 0.06f), _wood, false);
            Block(g, "Step", new Vector3(0f, 0.08f, -0.5f), new Vector3(1.8f, 0.16f, 0.8f), _stone, true);
            Block(g, "Awning", new Vector3(0f, 2.45f, -0.45f), new Vector3(1.8f, 0.08f, 0.9f), _wallGray, false);
            Block(g, "Mailbox", new Vector3(1.05f, 1.15f, -0.18f), new Vector3(0.35f, 0.45f, 0.22f), _metal, false);
            Block(g, "NamePlate", new Vector3(0f, 2.25f, -0.17f), new Vector3(0.9f, 0.18f, 0.02f), _white, false);
            Text(g, "NamePlate_Text", d.Ja, new Vector3(0f, 2.25f, -0.19f), 0f, 0.09f, new Color(0.2f, 0.22f, 0.28f), 0.9f);
            Block(g, "SignBoard", new Vector3(-1.45f, 1.6f, -0.18f), new Vector3(0.9f, 0.55f, 0.04f), _white, false);
            Text(g, "Sign_Text", "<b>ひばりハイツ</b>\nNhà trọ của bạn", new Vector3(-1.45f, 1.6f, -0.21f), 0f, 0.09f, new Color(0.15f, 0.35f, 0.25f), 0.9f);
            PointLight(g, "PorchLight", new Vector3(0f, 2.3f, -0.8f), 3.5f, 0.8f, new Color(1f, 0.85f, 0.6f));
        }

        // ─────────── Signposts ───────────

        private static void BuildSignposts(Transform root)
        {
            Signpost(root, "Signpost_Crossroad_NE", new Vector3(5.4f, 0f, -3.7f));
            Signpost(root, "Signpost_Spawn", new Vector3(4.6f, 0f, -16.4f));
        }

        private static void Signpost(Transform root, string name, Vector3 position)
        {
            var post = Group(root, name);
            post.position = position;
            Cylinder(post, "Pole", new Vector3(-1.55f, 1.3f, 0f), new Vector3(0.12f, 1.3f, 0.12f), _metal, true);
            var targets = new List<(string label, Vector3 point)>
            {
                ("コンビニ  ひばりマート", new Vector3(0f, 0f, KonbiniFront - 0.8f)),
            };
            foreach (var d in Destinations)
                if (EntrancePoints.TryGetValue(d.Portal, out var p)) targets.Add(($"{d.Ja}  ·  {d.Vi}", p));
            int i = 0;
            foreach (var (label, point) in targets)
            {
                Vector3 delta = point - position;
                float meters = new Vector2(delta.x, delta.z).magnitude;
                float y = 2.35f - i * 0.36f;
                Block(post, "Board_" + i, new Vector3(0f, y, 0f), new Vector3(3.0f, 0.3f, 0.06f), i == 0 ? _green : _blueSign, false);
                // Front face: read by someone walking north. Back face: read by someone walking south.
                Text(post, "Board_" + i + "_N", $"{Arrow(delta, 0f)}  {label}  <size=75%>{meters:0}m</size>", new Vector3(0f, y, -0.04f), 0f, 0.1f, Color.white, 2.9f);
                Text(post, "Board_" + i + "_S", $"{Arrow(delta, 180f)}  {label}  <size=75%>{meters:0}m</size>", new Vector3(0f, y, 0.04f), 180f, 0.1f, Color.white, 2.9f);
                i++;
            }
        }

        /// <summary>Arrow for a viewer whose forward is viewerYaw (0 = north/+Z, 180 = south).</summary>
        private static string Arrow(Vector3 delta, float viewerYaw)
        {
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg - viewerYaw; // 0 = straight ahead, 90 = to the right
            string[] arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };
            int index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % 8;
            return arrows[index];
        }

        // ─────────── Validation ───────────

        private static void Validate(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (OrphanRootNames.Contains(root.name)) throw new InvalidOperationException("Orphan root survived: " + root.name);
            var cashier = Object.FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(n => n.NpcId == "npc_cashier").ToArray();
            if (cashier.Length != 1) throw new InvalidOperationException("Expected exactly one npc_cashier, found " + cashier.Length);
            foreach (var d in Destinations)
            {
                var portal = GameObject.Find("AdditiveZonePortals/" + d.Portal);
                if (portal == null || portal.GetComponent<ScenePortal>() == null) throw new InvalidOperationException("Portal broken: " + d.Portal);
            }
            foreach (string item in new[] { "Onigiri", "Water", "Tea" })
            {
                var t = GameObject.Find("Environment/" + item);
                if (t == null || t.GetComponent<InteractiveItem>() == null) throw new InvalidOperationException("Quest item missing: " + item);
                Vector3 p = t.transform.position;
                if (Mathf.Abs(p.x) > KonbiniHalfWidth || p.z < KonbiniFront || p.z > KonbiniBack) throw new InvalidOperationException($"{item} is outside the store: {p}");
            }
            if (GameObject.Find("Environment/StoreDoor")?.GetComponent<DoorInteractable>() == null) throw new InvalidOperationException("Store door missing.");
        }

        private static void CreateMaterials()
        {
            _white = Lit(MatDir, "Town_White", new Color(0.95f, 0.95f, 0.93f), 0.2f);
            _wallGray = Lit(MatDir, "Town_Gray", new Color(0.55f, 0.58f, 0.62f), 0.2f);
            _green = Lit(MatDir, "Town_KonbiniGreen", new Color(0.12f, 0.58f, 0.36f), 0.35f);
            _blue = Lit(MatDir, "Town_KonbiniBlue", new Color(0.16f, 0.36f, 0.72f), 0.35f);
            _glass = Lit(MatDir, "Town_ShopGlass", new Color(0.7f, 0.86f, 0.95f, 0.3f), 0.92f, transparent: true);
            _signGlow = Lit(MatDir, "Town_SignGlow", new Color(0.98f, 0.98f, 0.95f), 0.3f, new Color(0.9f, 0.9f, 0.85f) * 1.2f);
            _tile = Lit(MatDir, "Town_StoreTile", new Color(0.86f, 0.87f, 0.88f), 0.55f);
            _dark = Lit(MatDir, "Town_Dark", new Color(0.12f, 0.13f, 0.15f), 0.4f);
            _wood = Lit(MatDir, "Town_Wood", new Color(0.52f, 0.34f, 0.2f), 0.3f);
            _navyCloth = Lit(MatDir, "Town_Noren", new Color(0.1f, 0.16f, 0.32f), 0.05f);
            _lantern = Lit(MatDir, "Town_Lantern", new Color(0.85f, 0.18f, 0.14f), 0.2f, new Color(1f, 0.3f, 0.2f) * 1.5f);
            _stone = Lit(MatDir, "Town_Stone", new Color(0.66f, 0.65f, 0.62f), 0.15f);
            _metal = Lit(MatDir, "Town_Metal", new Color(0.62f, 0.65f, 0.7f), 0.6f);
            _ceilingPanel = Lit(MatDir, "Town_CeilingPanel", Color.white, 0.2f, Color.white * 1.4f);
            _blueSign = Lit(MatDir, "Town_SignBlue", new Color(0.13f, 0.3f, 0.6f), 0.3f);
            _red = Lit(MatDir, "Town_Red", new Color(0.8f, 0.22f, 0.2f), 0.3f);
        }
    }
}
#endif
