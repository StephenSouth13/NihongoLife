using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Island;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Fishing and hand-held tools for Midori Island (part of IslandBuilder.Build): prefabs for the tools the player holds
    /// while working (hoe, shovel, rod with a "Tip" marker), the float and each catalog fish, plus the wooden pier on the
    /// north shore with its FishingSpot. The shore wall is opened only where the pier starts and the pier has its own rails.
    /// </summary>
    public static partial class IslandBuilder
    {
        private const string FishPack = "Assets/ThirdParty/Mixamo/fish and tool/FBX-20261010T144531Z-1-001/FBX/";
        private const float PierX = -14f, PierLength = 12f, PierLandOverlap = 2.5f, PierHalfWidth = 2.3f;

        // ─────────── prefabs ───────────

        private static List<IslandModelLibrary.Entry> BuildFishingPrefabs()
        {
            EnsureFolder(GenDir + "/Fishing");
            var entries = new List<IslandModelLibrary.Entry>
            {
                Held("tool_hoe", $"{Survival}Tool Hoe/toolHoe.obj", 1.15f, 0.3f, handleAtWideEnd: false),
                Held("tool_shovel", $"{Survival}Shovel/toolShovel.obj", 1.05f, 0.32f, handleAtWideEnd: false),
                Held(IslandFishing.RodId, FishPack + "FishingRod_Lvl2.fbx", 1.9f, 0.12f, handleAtWideEnd: true),
                Simple("Bobber", FishPack + "Lure_1.fbx", 0.16f),
            };
            foreach (var fish in IslandCatalog.Load().fish ?? Array.Empty<IslandFish>())
            {
                var entry = Simple("Fish_" + fish.id, FishPack + fish.model + ".fbx", fish.id == "fish_maguro" ? 0.8f : 0.45f);
                entries.Add(entry);
                RenderIcon(entry.prefab, fish.id, Quaternion.Euler(0f, 90f, 0f));
            }
            RenderIcon(entries.First(e => e.name == "Held_" + IslandFishing.RodId).prefab, IslandFishing.RodId, Quaternion.Euler(0f, 0f, -45f));
            AssetDatabase.Refresh();
            return entries;
        }

        /// <summary>Bag / shop picture (Resources/Items/&lt;itemId&gt;.png, transparent) rendered from the real model.</summary>
        private static void RenderIcon(GameObject prefab, string itemId, Quaternion pose)
        {
            const int size = 256;
            var stage = new GameObject("IconStage") { transform = { position = new Vector3(0f, -500f, 0f) } };
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.transform);
            model.transform.localRotation = pose;
            var light = new GameObject("IconLight").AddComponent<Light>();
            light.transform.SetParent(stage.transform, false);
            light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            var cam = new GameObject("IconCamera").AddComponent<Camera>();
            cam.transform.SetParent(stage.transform, false);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 24f; cam.nearClipPlane = 0.01f;
            Bounds b = BoundsOf(model);
            float distance = b.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
            cam.transform.position = b.center + Quaternion.Euler(18f, -25f, 0f) * Vector3.back * distance;
            cam.transform.LookAt(b.center);
            var target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = target;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            cam.targetTexture = null;
            string path = $"Assets/NihongoLife/Resources/Items/{itemId}.png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); target.Release(); Object.DestroyImmediate(stage);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 256;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        /// <summary>A tool to hold: shaft along +Y, handle down, origin at the grip, a "Tip" child at the far end.</summary>
        private static IslandModelLibrary.Entry Held(string id, string path, float length, float gripFromHandle, bool handleAtWideEnd)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new InvalidOperationException("Missing model " + path);
            var root = new GameObject("Held_" + id);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.transform.SetParent(root.transform, false);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            // Longest axis → Y.
            Vector3 size = BoundsOf(model).size;
            if (size.x >= size.y && size.x >= size.z) model.transform.Rotate(0f, 0f, 90f, Space.World);
            else if (size.z >= size.y && size.z >= size.x) model.transform.Rotate(90f, 0f, 0f, Space.World);
            Bounds b = BoundsOf(model);
            model.transform.localScale *= length / Mathf.Max(0.001f, b.size.y);
            b = BoundsOf(model);

            // The head (hoe blade, shovel spade) or the rod's reel end is the wider end; put the handle at the bottom.
            bool wideTop = EndSpread(model, b, top: true) > EndSpread(model, b, top: false);
            if (handleAtWideEnd == wideTop) { model.transform.Rotate(180f, 0f, 0f, Space.World); b = BoundsOf(model); }
            float gripY = b.min.y + b.size.y * gripFromHandle;
            model.transform.position -= new Vector3(b.center.x, gripY, b.center.z);
            var tip = new GameObject("Tip").transform;
            tip.SetParent(root.transform, false);
            tip.localPosition = new Vector3(0f, b.size.y * (1f - gripFromHandle), 0f);

            ConvertMaterials(root);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{GenDir}/Fishing/Held_{id}.prefab");
            Object.DestroyImmediate(root);
            return new IslandModelLibrary.Entry { name = "Held_" + id, prefab = prefab };
        }

        /// <summary>Largest distance from the shaft axis among the vertices in the top (or bottom) fifth of the model.</summary>
        private static float EndSpread(GameObject model, Bounds b, bool top)
        {
            float best = 0f;
            float limit = top ? b.max.y - b.size.y * 0.2f : b.min.y + b.size.y * 0.2f;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    Vector3 w = mf.transform.TransformPoint(v);
                    if (top ? w.y < limit : w.y > limit) continue;
                    best = Mathf.Max(best, new Vector2(w.x - b.center.x, w.z - b.center.z).magnitude);
                }
            }
            return best;
        }

        /// <summary>A model scaled so its longest side is <paramref name="longest"/> metres, centred on the origin.</summary>
        private static IslandModelLibrary.Entry Simple(string name, string path, float longest)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new InvalidOperationException("Missing model " + path);
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.transform.SetParent(root.transform, false);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var a in model.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            Bounds b = BoundsOf(model);
            model.transform.localScale *= longest / Mathf.Max(0.001f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            b = BoundsOf(model);
            model.transform.position -= b.center;
            ConvertMaterials(root);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{GenDir}/Fishing/{name}.prefab");
            Object.DestroyImmediate(root);
            return new IslandModelLibrary.Entry { name = name, prefab = prefab };
        }

        // ─────────── the pier ───────────

        /// <summary>z of the shore wall line on the north side at x.</summary>
        private static float ShoreWallZ(float x) => (IslandRZ - 3.5f) * Mathf.Sqrt(Mathf.Max(0f, 1f - x * x / ((IslandRX - 3.5f) * (IslandRX - 3.5f))));

        private static void BuildPier(Transform parent)
        {
            var pier = Group(parent, "FishingPier");
            float startZ = ShoreWallZ(PierX) - PierLandOverlap;
            float endZ = startZ + PierLength;

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(FishPack + "Dock_Long_NoRope.fbx") ?? throw new InvalidOperationException("Missing Dock_Long_NoRope");
            var holder = Group(pier, "Dock");
            var dock = (GameObject)PrefabUtility.InstantiatePrefab(source);
            dock.transform.SetParent(holder, false);
            foreach (var c in dock.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            Bounds b = BoundsOf(dock);
            dock.transform.localScale *= PierLength / Mathf.Max(0.01f, b.size.z);
            b = BoundsOf(dock);

            // Deck height: ray down the centre line against temporary mesh colliders.
            var temp = dock.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Select(f => { var mc = f.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = f.sharedMesh; return mc; }).ToList();
            Physics.SyncTransforms();
            var hits = new List<float>();
            foreach (float k in new[] { 0.3f, 0.5f, 0.7f })
            {
                var origin = new Vector3(b.center.x, b.max.y + 5f, Mathf.Lerp(b.min.z, b.max.z, k));
                // Only the dock's own colliders (the island ground is also under the ray).
                float top = float.NegativeInfinity;
                foreach (var mc in temp)
                    if (mc.Raycast(new Ray(origin, Vector3.down), out var hit, b.size.y + 10f)) top = Mathf.Max(top, hit.point.y);
                if (!float.IsNegativeInfinity(top)) hits.Add(top);
            }
            foreach (var mc in temp) Object.DestroyImmediate(mc);
            float deckY = hits.Count > 0 ? hits.OrderBy(h => h).ElementAt(hits.Count / 2) : b.max.y;
            Debug.Log($"[IslandBuilder] pier bounds={b.center}/{b.size} colliders={temp.Count} deck hits=[{string.Join(", ", hits.Select(h => h.ToString("0.00")))}] → deckY={deckY:0.00}");
            // Deck top at y = 0.04 (just above the grass), the pier running north from the shore.
            Vector3 worldTarget = holder.TransformPoint(new Vector3(PierX, 0f, (startZ + endZ) * 0.5f));
            dock.transform.position += new Vector3(worldTarget.x - b.center.x, holder.position.y + 0.04f - deckY, worldTarget.z - b.center.z);
            ConvertMaterials(dock);
            foreach (var r in dock.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }

            // Walkable deck and rails (the open sea is never reachable).
            Collider(pier, "DeckCollider", new Vector3(PierX, -0.11f, (startZ + endZ) * 0.5f), new Vector3(PierHalfWidth * 2f, 0.3f, PierLength));
            float railZ0 = startZ + PierLandOverlap - 0.6f;
            foreach (float side in new[] { -1f, 1f })
                Collider(pier, side < 0 ? "RailWest" : "RailEast", new Vector3(PierX + side * (PierHalfWidth + 0.2f), 0.6f, (railZ0 + endZ) * 0.5f), new Vector3(0.4f, 1.2f, endZ - railZ0));
            Collider(pier, "RailEnd", new Vector3(PierX, 0.6f, endZ + 0.2f), new Vector3(PierHalfWidth * 2f + 0.8f, 1.2f, 0.4f));
            // Rails are waist-high (enough to keep the player on the pier) so the follow camera can look over them.

            // Fishing spot at the end, facing the open sea.
            var spotGo = Trigger(pier, "FishingSpot", new Vector3(PierX, 1f, endZ - 1.3f), new Vector3(PierHalfWidth * 2f, 2f, 2.4f));
            var spot = spotGo.AddComponent<FishingSpot>();
            var target = new GameObject("CastTarget").transform;
            target.SetParent(pier, false);
            target.localPosition = new Vector3(PierX + 0.6f, -0.5f, endZ + 6.5f);
            spot.castTarget = target;
        }

        private static void Collider(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>Shore wall pieces of a segment outside the pier corridor (north side only).</summary>
        private static IEnumerable<(Vector3 a, Vector3 b)> ShoreOutsidePier(Vector3 p0, Vector3 p1)
        {
            float xa = PierX - PierHalfWidth - 0.2f, xb = PierX + PierHalfWidth + 0.2f;
            if (p0.z < 0f || (Mathf.Max(p0.x, p1.x) <= xa) || (Mathf.Min(p0.x, p1.x) >= xb)) { yield return (p0, p1); yield break; }
            Vector3 At(float x) => Vector3.Lerp(p0, p1, Mathf.InverseLerp(p0.x, p1.x, x));
            Vector3 lo = p0.x < p1.x ? p0 : p1, hi = p0.x < p1.x ? p1 : p0;
            if (lo.x < xa) yield return (lo, At(xa));
            if (hi.x > xb) yield return (At(xb), hi);
        }
    }
}
