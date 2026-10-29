#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Renders the art of the travel screen (UI/LoadingCard) and the web loader:
    /// · Resources/UI/Portraits/&lt;character id&gt;.png — a head-and-shoulders portrait of each playable character;
    /// · Resources/UI/Loading/&lt;scene&gt;.jpg — a wide view of each area, from its preview camera or its arrival point;
    /// · Assets/WebGLTemplates/NihongoLife/TemplateData/art/ — the same pictures, small, for the browser loader.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.LoadingArtBuilder.Build
    /// </summary>
    public static class LoadingArtBuilder
    {
        private const string PortraitDir = "Assets/NihongoLife/Resources/UI/Portraits";
        private const string PictureDir = "Assets/NihongoLife/Resources/UI/Loading";
        private const string WebDir = "Assets/WebGLTemplates/NihongoLife/TemplateData/art";
        private const int PortraitSize = 256, Width = 1280, Height = 720;

        /// <summary>Scene, arrival spawn, and the camera's offset from it (behind and above), when no preview camera exists.</summary>
        /// <summary>Scene, arrival spawn, the camera's offset from it and where it looks (spawn space), field of view.
        /// Rooms are shot from the doorway itself at head height with a wide lens; outdoor areas from behind and above.</summary>
        private static readonly (string scene, string spawn, Vector3 offset, Vector3 look, float fov)[] Areas =
        {
            (WorldLocationCatalog.CityScene, WorldLocationCatalog.CityStationReturn, new Vector3(0f, 9f, -16f), new Vector3(0f, 1.5f, 14f), 52f),
            (WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, new Vector3(0f, 2.4f, 0.2f), new Vector3(0f, 1.4f, 10f), 70f),
            (WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance, new Vector3(0f, 2.3f, 0f), new Vector3(0f, 1.0f, 6f), 72f),
            (WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance, new Vector3(0f, 2.4f, 0f), new Vector3(0f, 1.1f, 6f), 72f),
            (WorldLocationCatalog.HomeBedroomScene, WorldLocationCatalog.HomeBedroomEntrance, new Vector3(0f, 2.1f, -0.2f), new Vector3(0f, 0.9f, 4f), 75f),
            (WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.GameCenterEntrance, new Vector3(0f, 2.4f, 0f), new Vector3(0f, 1.2f, 7f), 72f),
            (WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation, new Vector3(0f, 12f, -18f), new Vector3(0f, 0f, 18f), 52f),
            (WorldLocationCatalog.ClinicScene, WorldLocationCatalog.ClinicEntrance, new Vector3(0f, 2.3f, 0f), new Vector3(0f, 1.1f, 6f), 72f),
            (WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.MusicShopEntrance, new Vector3(0f, 2.3f, 0f), new Vector3(0f, 1.1f, 6f), 72f),
            (WorldLocationCatalog.PhuQuocIslandScene, WorldLocationCatalog.PhuQuocArrival, new Vector3(-6f, 10f, -16f), new Vector3(0f, 1f, 16f), 52f),
        };

        public static void Build()
        {
            try
            {
                Directory.CreateDirectory(PortraitDir);
                Directory.CreateDirectory(PictureDir);
                Directory.CreateDirectory(WebDir);
                int portraits = Portraits();
                int pictures = 0;
                foreach (var area in Areas) if (Picture(area.scene, area.spawn, area.offset, area.look, area.fov)) pictures++;
                AssetDatabase.Refresh();
                Import(PortraitDir, PortraitSize, true);
                Import(PictureDir, 1280, false);
                Debug.Log($"[LoadingArtBuilder] {portraits} portraits, {pictures} area pictures.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static int Portraits()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("PortraitCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.2f, 0.27f, 1f);
            camera.fieldOfView = 18f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 30f;
            Light(new Vector3(35f, 150f, 0f), 1.3f, new Color(1f, 0.96f, 0.88f));
            Light(new Vector3(15f, 220f, 0f), 0.55f, new Color(0.8f, 0.88f, 1f));
            Light(new Vector3(20f, 20f, 0f), 0.7f, new Color(1f, 0.8f, 0.55f)); // warm rim from behind
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.64f);
            int written = 0;
            for (int i = 0; i < PlayableCharacterCatalog.Count; i++)
            {
                var character = PlayableCharacterCatalog.Get(i);
                var prefab = PlayableCharacterCatalog.LoadPrefab(character);
                if (prefab == null) continue;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // facing the camera (−Z)
                Pose(instance.GetComponentsInChildren<Animator>(true));
                Bounds bounds = BoundsOf(instance);
                // Chibi proportions: the head is the top ~40 % of the body. Frame the head and the top of the shoulders.
                float height = bounds.size.y;
                Vector3 face = new Vector3(bounds.center.x, bounds.max.y - height * 0.25f, bounds.center.z);
                float view = height * 0.6f;
                float distance = view * 0.5f / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                camera.transform.position = face + new Vector3(distance * 0.12f, height * 0.02f, -distance);
                camera.transform.LookAt(face);
                Save(camera, PortraitSize, PortraitSize, Path.Combine(PortraitDir, character.Id + ".png"), png: true);
                Save(camera, 160, 160, Path.Combine(WebDir, character.Id.Replace('_', '-') + ".png"), png: true);
                Object.DestroyImmediate(instance);
                written++;
            }
            return written;
        }

        private static bool Picture(string sceneName, string spawnId, Vector3 offset, Vector3 look, float fov)
        {
            string path = EditorBuildSettings.scenes.Select(s => s.path).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == sceneName)
                          ?? AssetDatabase.FindAssets("t:Scene " + sceneName).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == sceneName);
            if (path == null) { Debug.LogWarning("[LoadingArtBuilder] No scene " + sceneName); return false; }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var spawn = roots.SelectMany(r => r.GetComponentsInChildren<SceneSpawnPoint>(true)).FirstOrDefault(s => s.Id == spawnId)
                        ?? roots.SelectMany(r => r.GetComponentsInChildren<SceneSpawnPoint>(true)).FirstOrDefault();
            var preview = roots.SelectMany(r => r.GetComponentsInChildren<NihongoLife.Cameras.ZonePreviewCamera>(true)).FirstOrDefault();
            foreach (var other in roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true))) other.enabled = false;
            var camera = new GameObject("LoadingArtCamera").AddComponent<Camera>();
            camera.fieldOfView = fov;
            camera.nearClipPlane = 0.1f;
            Pose(roots.SelectMany(r => r.GetComponentsInChildren<Animator>(true)));
            camera.farClipPlane = 2500f;
            if (spawn != null)
            {
                var basis = spawn.transform;
                camera.transform.position = basis.TransformPoint(offset);
                camera.transform.LookAt(basis.TransformPoint(look));
            }
            else if (preview != null) camera.transform.SetPositionAndRotation(preview.transform.position, preview.transform.rotation);
            if (RenderSettings.skybox == null) { camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.55f, 0.72f, 0.88f); }
            Save(camera, Width, Height, Path.Combine(PictureDir, sceneName + ".jpg"), png: false);
            Save(camera, 640, 360, Path.Combine(WebDir, Safe(sceneName) + ".jpg"), png: false);
            return true;
        }

        /// <summary>Edit mode does not run animators: evaluate the default state (Idle) so nobody stands in a T-pose.</summary>
        private static void Pose(System.Collections.Generic.IEnumerable<Animator> animators)
        {
            foreach (var animator in animators)
            {
                if (animator == null || animator.runtimeAnimatorController == null) continue;
                animator.Rebind();
                animator.Update(0f);
                animator.Update(0.4f);
            }
        }

        private static string Safe(string scene) => scene.Replace(" ", "").Replace("_", "-").ToLowerInvariant();

        private static void Save(Camera camera, int width, int height, string file, bool png)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var read = new Texture2D(width, height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            read.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            File.WriteAllBytes(file, png ? read.EncodeToPNG() : read.EncodeToJPG(84));
            Object.DestroyImmediate(read);
            target.Release();
            Object.DestroyImmediate(target);
        }

        private static void Import(string folder, int maxSize, bool alpha)
        {
            foreach (var file in Directory.GetFiles(folder).Where(f => f.EndsWith(".png") || f.EndsWith(".jpg")))
            {
                if (AssetImporter.GetAtPath(file.Replace('\\', '/')) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = alpha;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = maxSize;
                importer.wrapMode = TextureWrapMode.Clamp;
                // They sit in Resources, so in the web build's first download: crunch them (≈0.3 MB instead of 2.4 MB).
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.crunchedCompression = true;
                importer.compressionQuality = 60;
                importer.SaveAndReimport();
            }
        }

        private static void Light(Vector3 euler, float intensity, Color color)
        {
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(euler);
            light.intensity = intensity;
            light.color = color;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
#endif
