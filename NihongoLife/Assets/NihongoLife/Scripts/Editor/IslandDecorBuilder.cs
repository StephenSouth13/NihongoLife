using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Island;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Island decorations (part of IslandBuilder.Build, or incrementally with
    /// <c>-executeMethod NihongoLife.EditorTools.IslandBuilder.BuildDecor</c>): one prefab per catalog decoration
    /// (Generated/Island/Decor/Decor_&lt;id&gt;.prefab — URP materials, pivot at the ground centre, scaled to its height,
    /// a trunk or body collider when solid), its shop picture (Resources/Items/decor_&lt;id&gt;.png), the model library
    /// entries "decor:&lt;id&gt;" and the IslandDecorRuntime on 60_MidoriIsland. The incremental path only touches the
    /// library and the "Decor" group, so the rest of the scene stays as it is.
    /// </summary>
    public static partial class IslandBuilder
    {
        public static void BuildDecor()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var entries = BuildDecorPrefabs();
                var library = Object.FindFirstObjectByType<IslandModelLibrary>() ?? throw new InvalidOperationException("IslandModelLibrary missing in " + ScenePath);
                foreach (var e in entries) library.Put(e.name, e.prefab);
                EditorUtility.SetDirty(library);
                EnsureDecorRuntime(library.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[IslandBuilder] Decorations built: {entries.Count} prefabs.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void EnsureDecorRuntime(Transform zoneRoot)
        {
            var existing = Object.FindObjectsByType<IslandDecorRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var extra in existing.Skip(1)) Object.DestroyImmediate(extra.gameObject);
            var runtime = existing.FirstOrDefault();
            if (runtime == null)
            {
                var group = new GameObject("Decor");
                group.transform.SetParent(zoneRoot, false);
                group.transform.localPosition = Vector3.zero;
                runtime = group.AddComponent<IslandDecorRuntime>();
            }
            runtime.Configure(O, IslandRX, IslandRZ);
            EditorUtility.SetDirty(runtime);
        }

        private static List<IslandModelLibrary.Entry> BuildDecorPrefabs()
        {
            EnsureFolder(GenDir + "/Decor");
            EnsureFolder(GenDir + "/Materials");
            var entries = new List<IslandModelLibrary.Entry>();
            foreach (var def in IslandCatalog.Load().decor ?? Array.Empty<IslandDecorDef>())
            {
                var prefab = DecorPrefab(def);
                entries.Add(new IslandModelLibrary.Entry { name = "decor:" + def.id, prefab = prefab });
                RenderIcon(prefab, def.ItemId, Quaternion.Euler(0f, def.category == "structure" ? 30f : 0f, 0f));
            }
            AssetDatabase.Refresh();
            return entries;
        }

        /// <summary>
        /// No pond model in the packs, so it is put together: a round pool of water (URP Lit, glossy teal) sunk a little
        /// into the grass, a ring of rocks around it and two koi on the surface. Walk-through (trigger) like flowers.
        /// </summary>
        private static GameObject KoiPond(IslandDecorDef def)
        {
            var root = new GameObject("Decor_" + def.id);
            float r = def.footprint * 0.5f;

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Water";
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.SetParent(root.transform, false);
            water.transform.localScale = new Vector3(r * 1.7f, 0.03f, r * 1.7f);
            water.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            string matPath = GenDir + "/Materials/KoiPondWater.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            mat.SetColor("_BaseColor", new Color(0.18f, 0.52f, 0.62f, 1f));
            mat.SetFloat("_Smoothness", 0.92f);
            mat.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(mat);
            water.GetComponent<Renderer>().sharedMaterial = mat;

            var rock = AssetDatabase.LoadAssetAtPath<GameObject>(Survival + "Rock/rockA.obj") ?? throw new InvalidOperationException("Missing rockA.obj");
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                var stone = (GameObject)PrefabUtility.InstantiatePrefab(rock);
                PrefabUtility.UnpackPrefabInstance(stone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                stone.name = "Stone" + i;
                stone.transform.SetParent(root.transform, false);
                foreach (var c in stone.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                Bounds sb = BoundsOf(stone);
                float s = (0.22f + 0.1f * Mathf.Abs(Mathf.Sin(i * 2.3f))) / Mathf.Max(0.01f, sb.size.y);
                stone.transform.localScale *= s;
                stone.transform.localRotation = Quaternion.Euler(0f, i * 47f, 0f);
                stone.transform.localPosition = new Vector3(Mathf.Cos(a) * r * 0.92f, 0f, Mathf.Sin(a) * r * 0.95f);
                sb = BoundsOf(stone);
                stone.transform.position += Vector3.up * (-sb.min.y - 0.05f);
            }

            var koi = AssetDatabase.LoadAssetAtPath<GameObject>(FishPack + "Koi.fbx") ?? throw new InvalidOperationException("Missing Koi.fbx");
            for (int i = 0; i < 2; i++)
            {
                var fish = (GameObject)PrefabUtility.InstantiatePrefab(koi);
                PrefabUtility.UnpackPrefabInstance(fish, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                fish.name = "Koi" + i;
                fish.transform.SetParent(root.transform, false);
                foreach (var c in fish.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                foreach (var an in fish.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(an);
                Bounds fb = BoundsOf(fish);
                fish.transform.localScale *= Mathf.Min(0.62f / Mathf.Max(0.01f, Mathf.Max(fb.size.x, fb.size.z)), 0.2f / Mathf.Max(0.01f, fb.size.y)); // long and flat on the water
                fish.transform.localRotation = Quaternion.Euler(0f, i == 0 ? 35f : 215f, 0f);
                fish.transform.localPosition = new Vector3(i == 0 ? -0.3f : 0.35f, 0f, i == 0 ? 0.2f : -0.25f);
                fb = BoundsOf(fish);
                fish.transform.position += Vector3.up * (0.02f - fb.min.y); // floating on the surface
            }

            ConvertMaterials(root);
            foreach (var rd in root.GetComponentsInChildren<Renderer>(true)) { rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; rd.receiveShadows = true; }
            water.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var pick = root.AddComponent<BoxCollider>();
            pick.isTrigger = true;
            pick.center = new Vector3(0f, 0.25f, 0f);
            pick.size = new Vector3(def.footprint, 0.5f, def.footprint);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{GenDir}/Decor/Decor_{def.id}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject DecorPrefab(IslandDecorDef def)
        {
            if (def.source == "composite:koi_pond") return KoiPond(def);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(def.source) ?? throw new InvalidOperationException("Missing decor model " + def.source);
            var root = new GameObject("Decor_" + def.id);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.transform.SetParent(root.transform, false);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var a in model.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            foreach (var l in model.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);
            Bounds b = BoundsOf(model);
            float scale = def.height / Mathf.Max(0.001f, b.size.y);
            // Very wide models (fence, log, flat rock) keep to about their footprint.
            // (Trees keep their height: their canopy is meant to spread past the trunk's footprint.)
            float wide = Mathf.Max(b.size.x, b.size.z) * scale;
            bool tree = def.category == "plant" && def.height > 2f;
            if (!tree && wide > def.footprint * 1.8f) scale *= def.footprint * 1.8f / wide;
            model.transform.localScale *= scale;
            b = BoundsOf(model);
            model.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            ConvertMaterials(root);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            if (def.solid)
            {
                b = BoundsOf(model);
                var box = root.AddComponent<BoxCollider>();
                bool trunk = def.category == "plant" && def.height > 2f;
                if (trunk)
                {
                    box.center = new Vector3(0f, 1.2f, 0f);
                    box.size = new Vector3(0.6f, 2.4f, 0.6f);
                }
                else
                {
                    box.center = b.center;
                    box.size = new Vector3(Mathf.Max(0.3f, b.size.x), Mathf.Max(0.3f, b.size.y), Mathf.Max(0.3f, b.size.z));
                }
            }
            else
            {
                // Walk-through pieces still need a pick-up target for the decorating click (a trigger the player ignores).
                b = BoundsOf(model);
                var pick = root.AddComponent<BoxCollider>();
                pick.isTrigger = true;
                pick.center = b.center;
                pick.size = new Vector3(Mathf.Max(0.4f, b.size.x), Mathf.Max(0.4f, b.size.y), Mathf.Max(0.4f, b.size.z));
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{GenDir}/Decor/Decor_{def.id}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
