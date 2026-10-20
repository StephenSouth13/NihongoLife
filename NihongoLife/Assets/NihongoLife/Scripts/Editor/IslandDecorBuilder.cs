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

        private static GameObject DecorPrefab(IslandDecorDef def)
        {
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
            float wide = Mathf.Max(b.size.x, b.size.z) * scale;
            if (wide > def.footprint * 1.8f) scale *= def.footprint * 1.8f / wide;
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
