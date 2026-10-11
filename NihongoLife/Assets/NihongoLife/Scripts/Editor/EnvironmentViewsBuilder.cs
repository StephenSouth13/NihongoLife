#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Outdoors pass, saved into the scenes (no menu item, no setup step at play time):
    /// · Hibari (90_TestSandbox) and Midori get an OutdoorSky: painted day/night sky, sun/moon, ambient and fog that
    ///   keep each scene's daytime look, and night lamps (Midori gets lamps along its paths, station and campfire).
    /// · Interiors (sushi, school, game center, station) get the real Hibari street outside their door: the city
    ///   geometry in front of the matching city entrance is copied (render-only, layer "ExteriorView", own sun), so the
    ///   doorway shows the street the player came from instead of the empty skybox. The station exit becomes a
    ///   stairwell up to that street.
    /// Idempotent: each run replaces the objects it made before.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.EnvironmentViewsBuilder.BuildAll
    /// </summary>
    public static class EnvironmentViewsBuilder
    {
        private const string Scenes = "Assets/NihongoLife/Scenes/";
        private const string SkyMaterialPath = "Assets/NihongoLife/Materials/Sky/NL_SkyDayNight.mat";
        private const string SkyDome = "Assets/ZRNAssets/006441_09646_1_1/Models/doom/skydoom/";
        private const string ExteriorLayerName = "ExteriorView";
        private const int ExteriorLayer = 28;
        /// <summary>URP rendering layer of the copied street (bit 1): only its own sun, fill and lamps light it, and
        /// they light nothing in the room (Forward+ ignores Light.cullingMask, rendering layers are what it uses).</summary>
        private const uint StreetRenderingLayer = 1u << 1;
        private const string ExteriorRoot = "ExteriorView_Street";
        private const string SkyRoot = "OutdoorSky";

        /// <summary>A doorway: floor-level centre and the direction pointing out of the building.</summary>
        private readonly struct Doorway
        {
            public readonly Vector3 Position;
            public readonly Vector3 Outward;
            public Doorway(Vector3 position, Vector3 outward) { Position = position; Outward = outward.normalized; }
        }

        // City entrances (90_TestSandbox): the door plane of each venue and the way out to the street.
        private static readonly Doorway CitySushi = new(new Vector3(-18f, 0f, 0.79f), Vector3.back);
        private static readonly Doorway CitySchool = new(new Vector3(18f, 0f, -20.25f), Vector3.forward);
        private static readonly Doorway CityGameCenter = new(new Vector3(-6f, 0f, -21.13f), Vector3.forward);
        private static readonly Doorway CityStation = new(new Vector3(24f, 0f, 0.83f), Vector3.back);

        private sealed class Piece
        {
            public Mesh Mesh;
            public Material[] Materials;
            public Matrix4x4 Local; // relative to the city doorway frame
            public string Name;
        }

        private sealed class LampPiece
        {
            public Matrix4x4 Local;
            public LightType Type;
            public Color Color;
            public float Intensity, Range, SpotAngle;
        }

        public static void BuildAll()
        {
            try
            {
                EnsureLayer();
                var sky = EnsureSkyMaterial();

                // 1) Hibari: sky + lamps; and capture the street in front of each venue for the interiors.
                var city = EditorSceneManager.OpenScene(Scenes + "90_TestSandbox.unity", OpenSceneMode.Single);
                var streets = new Dictionary<string, (List<Piece>, List<LampPiece>)>
                {
                    ["sushi"] = Capture(CitySushi),
                    ["school"] = Capture(CitySchool),
                    ["gamecenter"] = Capture(CityGameCenter),
                    ["station"] = Capture(CityStation),
                };
                BuildCitySky(sky);
                EditorSceneManager.MarkSceneDirty(city);
                EditorSceneManager.SaveScene(city);

                // 2) Midori.
                var island = EditorSceneManager.OpenScene(Scenes + "60_MidoriIsland.unity", OpenSceneMode.Single);
                BuildIslandSky(sky);
                EditorSceneManager.MarkSceneDirty(island);
                EditorSceneManager.SaveScene(island);

                // 3) Interiors: the street outside each door.
                Interior("30_SushiRestaurant", new Doorway(new Vector3(500f, 0f, -9.0f), Vector3.back), CitySushi, streets["sushi"], sky, null);
                Interior("40_ HIBARICLASS", new Doorway(new Vector3(1000f, 0f, -6.9f), Vector3.back), CitySchool, streets["school"], sky, null);
                Interior("50_GameCenter", new Doorway(new Vector3(300f, 0f, -9.05f), Vector3.back), CityGameCenter, streets["gamecenter"], sky, null);
                Interior("20_StationDistrict", new Doorway(new Vector3(800f, 4.2f, 21.2f), Vector3.forward), CityStation, streets["station"], sky, BuildStationStairwell);

                Debug.Log("[EnvironmentViewsBuilder] Done.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        // ─────────── Sky material and layer ───────────

        private static Material EnsureSkyMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (material == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SkyMaterialPath));
                material = new Material(Shader.Find("NihongoLife/SkyDayNight"));
                AssetDatabase.CreateAsset(material, SkyMaterialPath);
            }
            material.shader = Shader.Find("NihongoLife/SkyDayNight");
            material.SetTexture("_DayTex", AssetDatabase.LoadAssetAtPath<Texture2D>(SkyDome + "skydoom.png"));
            material.SetTexture("_NightTex", AssetDatabase.LoadAssetAtPath<Texture2D>(SkyDome + "skydoom_night_s.png"));
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            var slot = layers.GetArrayElementAtIndex(ExteriorLayer);
            if (slot.stringValue == ExteriorLayerName) return;
            if (!string.IsNullOrEmpty(slot.stringValue)) throw new InvalidOperationException($"Layer {ExteriorLayer} is already '{slot.stringValue}'.");
            slot.stringValue = ExteriorLayerName;
            tagManager.ApplyModifiedProperties();
        }

        // ─────────── Hibari ───────────

        private static void BuildCitySky(Material sky)
        {
            Remove(SkyRoot);
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional && l.gameObject.name == "Directional Light");
            // Street lamps, porch and canopy lights; the konbini's own store lights stay on (they are inside the shop).
            var lamps = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
                .Where(l => l.type != LightType.Directional && !Path(l.transform).Contains("Town_Konbini/Lights") && !Path(l.transform).StartsWith("JobSite"))
                .ToArray();
            var go = new GameObject(SkyRoot);
            go.AddComponent<OutdoorSky>().Configure(sun, sun.transform.eulerAngles.y, sky, true, lamps, null);
            Debug.Log($"[EnvironmentViewsBuilder] Hibari: {lamps.Length} night lamps.");
        }

        // ─────────── Midori ───────────

        private static readonly Vector3[] IslandLampSpots =
        {
            new(-1196f, 0f, -24f), new(-1204f, 0f, -24f),   // from the station down the path
            new(-1196f, 0f, -11f), new(-1204f, 0f, -6f),    // main path / plaza
            new(-1194f, 0f, -14.5f),                         // store
            new(-1209f, 0f, 2f), new(-1188f, 0f, -1.5f),     // farm and animal pen gates
            new(-1203f, 0f, 14f), new(-1197f, 0f, 24f),      // towards the lookout
        };

        private static void BuildIslandSky(Material sky)
        {
            Remove(SkyRoot);
            Remove("Midori_NightLamps");
            var sun = RenderSettings.sun;
            var root = new GameObject("Midori_NightLamps").transform;
            var streetLight = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Roads/street_light.prefab");
            var lamps = new List<Light>();
            for (int i = 0; i < IslandLampSpots.Length; i++)
            {
                Vector3 p = IslandLampSpots[i];
                p.y = GroundHeight(p);
                // Lamp arm faces the nearest path point (the plaza).
                Vector3 toPath = Vector3.ProjectOnPlane(new Vector3(-1200f, 0f, -4f) - p, Vector3.up);
                var rotation = Quaternion.LookRotation(toPath.sqrMagnitude > 0.01f ? -toPath.normalized : Vector3.forward);
                var lamp = (GameObject)PrefabUtility.InstantiatePrefab(streetLight);
                lamp.name = "IslandLamp_" + i;
                lamp.transform.SetParent(root, true);
                lamp.transform.SetPositionAndRotation(p, rotation);
                lamps.Add(AddLampLight(lamp.transform, new Vector3(0f, 2.35f, -1.2f), Quaternion.Euler(62f, 180f, 0f), LightType.Spot, 1.9f, 9f));
            }
            // Under the station canopy and at the campfire.
            foreach (float x in new[] { -1206f, -1194f })
            {
                var canopy = new GameObject("CanopyLamp").transform;
                canopy.SetParent(root, false);
                canopy.position = new Vector3(x, 4.2f, -29.6f);
                lamps.Add(AddLampLight(canopy, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), LightType.Point, 1.6f, 9f));
            }
            var fire = new GameObject("CampfireGlow").transform;
            fire.SetParent(root, false);
            fire.position = new Vector3(-1200f, 0.9f, 30.5f);
            var fireLight = AddLampLight(fire, Vector3.zero, Quaternion.identity, LightType.Point, 2.2f, 8f);
            fireLight.color = new Color(1f, 0.55f, 0.25f);
            lamps.Add(fireLight);

            var go = new GameObject(SkyRoot);
            go.AddComponent<OutdoorSky>().Configure(sun, sun.transform.eulerAngles.y, sky, true, lamps.ToArray(), null);
            Debug.Log($"[EnvironmentViewsBuilder] Midori: {lamps.Count} night lamps.");
        }

        private static float GroundHeight(Vector3 p)
        {
            return Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out var hit, 60f) ? hit.point.y : 0f;
        }

        private static Light AddLampLight(Transform parent, Vector3 localPosition, Quaternion localRotation, LightType type, float intensity, float range)
        {
            var go = new GameObject("WarmRoadLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = new Color(1f, 0.82f, 0.52f);
            light.intensity = intensity;
            light.range = range;
            if (type == LightType.Spot) light.spotAngle = 70f;
            light.shadows = LightShadows.None;
            return light;
        }

        // ─────────── Interiors ───────────

        /// <summary>Everything static in the city in front of <paramref name="door"/>: up to 34 m out and 26 m to each
        /// side, without the entrance decorations of the venue itself, characters or text.</summary>
        private static (List<Piece>, List<LampPiece>) Capture(Doorway door)
        {
            var frame = Matrix4x4.TRS(door.Position, Quaternion.LookRotation(door.Outward), Vector3.one).inverse;
            bool Inside(Vector3 world)
            {
                Vector3 local = frame.MultiplyPoint3x4(world);
                return local.z > 0.4f && local.z < 34f && Mathf.Abs(local.x) < 26f && local.y < 30f;
            }
            string[] roots = { "Environment", "Town_Plaza", "ThirdParty_Integrated_World", "Town_Signposts" };
            var pieces = new List<Piece>();
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.InstanceID))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                string path = Path(renderer.transform);
                if (!roots.Any(r => path.StartsWith(r + "/") || path == r)) continue;
                if (renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                if (!Inside(renderer.bounds.center)) continue;
                if (renderer.bounds.size.magnitude > 120f) continue; // ground planes / backdrops
                pieces.Add(new Piece
                {
                    Mesh = filter.sharedMesh,
                    Materials = renderer.sharedMaterials,
                    Local = frame * renderer.transform.localToWorldMatrix,
                    Name = renderer.name,
                });
            }
            var lamps = new List<LampPiece>();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
            {
                if (light.type == LightType.Directional || !Inside(light.transform.position)) continue;
                lamps.Add(new LampPiece
                {
                    Local = frame * light.transform.localToWorldMatrix,
                    Type = light.type, Color = light.color, Intensity = light.intensity, Range = light.range, SpotAngle = light.spotAngle,
                });
            }
            Debug.Log($"[EnvironmentViewsBuilder] Captured {pieces.Count} meshes, {lamps.Count} lamps in front of {door.Position}.");
            return (pieces, lamps);
        }

        private static void Interior(string sceneName, Doorway door, Doorway cityDoor, (List<Piece> pieces, List<LampPiece> lamps) street, Material sky,
            Action<Transform, Doorway> extra)
        {
            var scene = EditorSceneManager.OpenScene(Scenes + sceneName + ".unity", OpenSceneMode.Single);
            Remove(ExteriorRoot);
            Remove(SkyRoot);
            var root = new GameObject(ExteriorRoot).transform;
            var place = Matrix4x4.TRS(door.Position, Quaternion.LookRotation(door.Outward), Vector3.one);

            // Ground under the copied street (the city ground plane itself is too large to copy).
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "StreetGround";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(root, false);
            ground.transform.position = place.MultiplyPoint3x4(new Vector3(0f, -0.06f, 18f));
            ground.transform.rotation = place.rotation;
            ground.transform.localScale = new Vector3(56f, 0.1f, 36f);
            ground.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/NihongoLife/Materials/City/Town_Gray.mat");
            ground.GetComponent<Renderer>().renderingLayerMask = StreetRenderingLayer;

            foreach (var piece in street.pieces)
            {
                var go = new GameObject(piece.Name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false);
                Matrix4x4 m = place * piece.Local;
                go.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                go.transform.localScale = m.lossyScale;
                go.GetComponent<MeshFilter>().sharedMesh = piece.Mesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterials = piece.Materials;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.renderingLayerMask = StreetRenderingLayer;
                r.receiveShadows = false;
            }
            var lamps = new List<Light>();
            foreach (var lamp in street.lamps)
            {
                var go = new GameObject("WarmRoadLight");
                go.transform.SetParent(root, false);
                Matrix4x4 m = place * lamp.Local;
                go.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                var light = go.AddComponent<Light>();
                light.type = lamp.Type; light.color = lamp.Color; light.intensity = lamp.Intensity; light.range = lamp.Range; light.spotAngle = lamp.SpotAngle;
                light.cullingMask = 1 << ExteriorLayer;
                light.renderingLayerMask = (int)StreetRenderingLayer;
                light.shadows = LightShadows.None;
                lamps.Add(light);
            }

            // Its own sun and a soft fill: they light only the street layer, never the room.
            var sunGo = new GameObject("ExteriorSun");
            sunGo.transform.SetParent(root, false);
            sunGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.93f, 0.78f);
            sun.shadows = LightShadows.None;
            sun.cullingMask = 1 << ExteriorLayer;
            sun.renderingLayerMask = (int)StreetRenderingLayer;
            var fillGo = new GameObject("ExteriorFill");
            fillGo.transform.SetParent(root, false);
            fillGo.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.35f;
            fill.color = new Color(0.75f, 0.82f, 0.95f);
            fill.shadows = LightShadows.None;
            fill.cullingMask = 1 << ExteriorLayer;
            fill.renderingLayerMask = (int)StreetRenderingLayer;

            extra?.Invoke(root, door);

            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ExteriorLayer;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            var skyGo = new GameObject(SkyRoot);
            skyGo.AddComponent<OutdoorSky>().Configure(sun, -35f, sky, false, lamps.ToArray(), null);
            Debug.Log($"[EnvironmentViewsBuilder] {sceneName}: {street.pieces.Count} street meshes, {lamps.Count} lamps.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ─────────── Station: stairwell from the platform up to the street ───────────

        private const string StationWallPath = "StationDistrict_Zone/MetroEnvironment/TunnelNorthWall";

        private static void BuildStationStairwell(Transform root, Doorway top)
        {
            // Opening in the north tunnel wall: the old single wall is disabled and replaced by two pieces and a header.
            var wall = GameObject.Find(StationWallPath);
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/NihongoLife/Materials/City/Town_Stone.mat");
            Vector3 wallCenter = new(800f, 2.9f, 11.5f), wallSize = new(92f, 6.6f, 0.45f);
            if (wall != null)
            {
                var r = wall.GetComponent<Renderer>();
                wallMat = r.sharedMaterial;
                wallCenter = r.bounds.center; wallSize = r.bounds.size;
                wall.SetActive(false);
            }
            var station = GameObject.Find("StationDistrict_Zone").transform;
            var existing = station.Find("ExitStairwell");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var stair = new GameObject("ExitStairwell").transform;
            stair.SetParent(station, false);

            const float opening = 5f;
            float minX = wallCenter.x - wallSize.x / 2f, maxX = wallCenter.x + wallSize.x / 2f;
            float bottom = wallCenter.y - wallSize.y / 2f, topY = wallCenter.y + wallSize.y / 2f;
            Block(stair, "NorthWall_W", new Vector3((minX + 800f - opening / 2f) / 2f, wallCenter.y, wallCenter.z), new Vector3(800f - opening / 2f - minX, wallSize.y, wallSize.z), wallMat, true);
            Block(stair, "NorthWall_E", new Vector3((maxX + 800f + opening / 2f) / 2f, wallCenter.y, wallCenter.z), new Vector3(maxX - 800f - opening / 2f, wallSize.y, wallSize.z), wallMat, true);
            Block(stair, "NorthWall_Header", new Vector3(800f, (3.4f + topY) / 2f, wallCenter.z), new Vector3(opening, topY - 3.4f, wallSize.z), wallMat, true);

            // Steps from the platform (y≈0.3) up to the street (top.Position.y) between z 11.8 and 20.6.
            var stepMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/NihongoLife/Materials/City/Town_Stone.mat");
            var trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/NihongoLife/Materials/City/Town_KonbiniGreen.mat");
            const int steps = 20;
            float z0 = 11.8f, z1 = 20.6f, y0 = 0.3f, y1 = top.Position.y;
            for (int i = 0; i < steps; i++)
            {
                float z = Mathf.Lerp(z0, z1, (i + 0.5f) / steps);
                float h = Mathf.Lerp(y0, y1, (i + 1f) / steps);
                Block(stair, "Step_" + i, new Vector3(800f, (bottom + h) / 2f, z), new Vector3(opening - 0.2f, h - bottom, (z1 - z0) / steps + 0.01f), stepMat, false);
                Block(stair, "Nosing_" + i, new Vector3(800f, h + 0.005f, z - (z1 - z0) / steps / 2f + 0.04f), new Vector3(opening - 0.2f, 0.02f, 0.08f), trimMat, false);
            }
            // Stairwell side walls and a short roof over the lower half (the upper half is open to the sky).
            float wallHeight = y1 + 3f - bottom;
            Block(stair, "SideWall_W", new Vector3(800f - opening / 2f - 0.15f, bottom + wallHeight / 2f, (z0 + z1) / 2f + 0.3f), new Vector3(0.3f, wallHeight, z1 - z0 + 1f), wallMat, true);
            Block(stair, "SideWall_E", new Vector3(800f + opening / 2f + 0.15f, bottom + wallHeight / 2f, (z0 + z1) / 2f + 0.3f), new Vector3(0.3f, wallHeight, z1 - z0 + 1f), wallMat, true);
            Block(stair, "Roof", new Vector3(800f, topY + 0.15f, 14f), new Vector3(opening + 0.6f, 0.3f, 5f), wallMat, false);
            // Handrails.
            foreach (float side in new[] { -1f, 1f })
            {
                var rail = Block(stair, side < 0 ? "Handrail_W" : "Handrail_E", new Vector3(800f + side * (opening / 2f - 0.12f), (y0 + y1) / 2f + 0.95f, (z0 + z1) / 2f),
                    new Vector3(0.06f, 0.06f, Vector3.Distance(new Vector3(0f, y0, z0), new Vector3(0f, y1, z1))), trimMat, false);
                rail.transform.rotation = Quaternion.Euler(-Mathf.Atan2(y1 - y0, z1 - z0) * Mathf.Rad2Deg, 0f, 0f);
            }
            // "出口 · Exit" sign over the opening.
            var sign = Block(stair, "ExitSign", new Vector3(800f, 3.75f, wallCenter.z - 0.26f), new Vector3(2.6f, 0.55f, 0.06f),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/NihongoLife/Materials/City/Town_SignGlow.mat") ?? trimMat, false);
            var text = new GameObject("ExitSign_Text").AddComponent<TMPro.TextMeshPro>();
            text.transform.SetParent(sign.transform.parent, false);
            text.transform.position = sign.transform.position + new Vector3(0f, 0f, -0.04f);
            text.transform.rotation = Quaternion.identity;
            text.rectTransform.sizeDelta = new Vector2(2.5f, 0.5f);
            text.fontSize = 3.2f;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = new Color(0.12f, 0.12f, 0.12f);
            text.text = "出口  <size=70%>Exit · Hibari</size>";
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/NihongoLife/Fonts/NotoSansJP SDF.asset");
            if (font != null) text.font = font;
        }

        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Remove(string rootName)
        {
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == rootName).ToArray())
                Object.DestroyImmediate(go);
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
#endif
