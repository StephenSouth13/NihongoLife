#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NihongoLife.EditorTools
{
    public static class EkimaePilotBuilder
    {
        private const string GroundModel = "Assets/ZRNAssets/005030_06196_1_1/Models/Fukuoka_Ground.fbx";
        private const string PropModel = "Assets/ZRNAssets/005030_06196_1_1/Models/Fukuoka_Prop.fbx";
        private const string MaterialFolder = "Assets/NihongoLife/Materials/Zenrin";
        private const string PrefabFolder = "Assets/NihongoLife/Prefabs/World/Ekimae";
        private const string StationScene = "Assets/NihongoLife/Scenes/20_StationDistrict.unity";
        private const string PilotRootName = "Ekimae_FukuokaPilot";
        private const float PilotScale = 6f;

        public static void AuditSource()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(GroundModel);
            if (source == null) throw new System.InvalidOperationException($"Missing source model: {GroundModel}");

            AuditModel(source, GroundModel, "block_");

            GameObject propSource = AssetDatabase.LoadAssetAtPath<GameObject>(PropModel);
            if (propSource == null) throw new System.InvalidOperationException($"Missing source model: {PropModel}");
            AuditModel(propSource, PropModel, "prop_");
        }

        public static void BuildPilot()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            GameObject blockB = BuildWrapper('B');
            GameObject blockH = BuildWrapper('H');
            IntegratePilot(blockB, blockH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidatePilot();
            Debug.Log("[EkimaePilot] Build complete. Fukuoka blocks B and H integrated without modifying vendor assets.");
        }

        public static void ValidatePilot()
        {
            Scene scene = EditorSceneManager.OpenScene(StationScene, OpenSceneMode.Single);
            GameObject pilot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == PilotRootName);
            if (pilot == null) throw new System.InvalidOperationException("Ekimae pilot root is missing.");
            if (pilot.transform.childCount != 2) throw new System.InvalidOperationException("Ekimae pilot must contain exactly two blocks.");
            if (GameObject.Find("Spawn_station_entrance") == null) throw new System.InvalidOperationException("Station entrance spawn was lost.");
            if (GameObject.Find("ExitToCity") == null) throw new System.InvalidOperationException("Station exit portal was lost.");

            Renderer[] renderers = pilot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length < 8) throw new System.InvalidOperationException("Ekimae pilot renderer set is incomplete.");
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                        throw new System.InvalidOperationException($"Invalid material on {renderer.name}.");
                    string path = AssetDatabase.GetAssetPath(material);
                    if (!path.StartsWith(MaterialFolder, System.StringComparison.Ordinal))
                        throw new System.InvalidOperationException($"Renderer {renderer.name} still uses vendor material {path}.");
                }
            }

            Bounds bounds = CalculateBounds(renderers);
            if (bounds.min.y < -0.08f || bounds.max.y > 20f)
                throw new System.InvalidOperationException($"Pilot vertical bounds are invalid: {bounds}");
            Debug.Log($"[EkimaePilot] Validation passed. renderers={renderers.Length}, bounds={bounds}, scene={scene.name}");
        }

        public static void OpenPilotForReview()
        {
            EditorSceneManager.OpenScene(StationScene, OpenSceneMode.Single);
            GameObject pilot = GameObject.Find(PilotRootName);
            Selection.activeGameObject = pilot;
            EditorApplication.delayCall += () =>
            {
                SceneView view = SceneView.lastActiveSceneView;
                if (view == null || pilot == null) return;
                Bounds bounds = CalculateBounds(pilot.GetComponentsInChildren<Renderer>(true));
                view.LookAt(bounds.center + new Vector3(0f, 1.5f, 0f), Quaternion.Euler(24f, 205f, 0f), bounds.extents.magnitude * 1.05f);
                view.Repaint();
            };
        }

        public static void CaptureGameView()
        {
            EditorSceneManager.OpenScene(StationScene, OpenSceneMode.Single);
            Camera camera = GameObject.Find("StationSceneCamera")?.GetComponent<Camera>();
            if (camera == null) throw new System.InvalidOperationException("StationSceneCamera is missing.");

            const int width = 1920;
            const int height = 1080;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ekimae-gameview.png"));
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log($"[EkimaePilot] Captured StationSceneCamera to {path}");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(target);
            }
        }

        public static void CaptureOverview()
        {
            EditorSceneManager.OpenScene(StationScene, OpenSceneMode.Single);
            Camera camera = GameObject.Find("StationSceneCamera")?.GetComponent<Camera>();
            if (camera == null) throw new System.InvalidOperationException("StationSceneCamera is missing.");
            camera.transform.position = new Vector3(800f, 38f, -58f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(800f, 1.5f, 0f) - camera.transform.position);
            camera.fieldOfView = 58f;

            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, 1920, 1080), 0, 0);
                texture.Apply();
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ekimae-overview.png"));
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log($"[EkimaePilot] Captured overview to {path}");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(target);
            }
        }

        private static GameObject BuildWrapper(char suffix)
        {
            GameObject groundSource = AssetDatabase.LoadAssetAtPath<GameObject>(GroundModel);
            GameObject propSource = AssetDatabase.LoadAssetAtPath<GameObject>(PropModel);
            if (groundSource == null || propSource == null) throw new System.InvalidOperationException("Fukuoka source models are missing.");

            GameObject groundInstance = Object.Instantiate(groundSource);
            GameObject propInstance = Object.Instantiate(propSource);
            var wrapper = new GameObject($"NL_Ekimae_Block_{suffix}");
            try
            {
                Transform groundBlock = FindDeep(groundInstance.transform, "block_" + suffix);
                Transform propBlock = FindDeep(propInstance.transform, "block_" + suffix);
                if (groundBlock == null || propBlock == null)
                    throw new System.InvalidOperationException($"Fukuoka block {suffix} hierarchy is incomplete.");

                GameObject ground = Object.Instantiate(groundBlock.gameObject, wrapper.transform);
                ground.name = "GroundAndBuildings";
                GameObject props = Object.Instantiate(propBlock.gameObject, wrapper.transform);
                props.name = "StreetProps";

                Renderer[] renderers = wrapper.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = CalculateBounds(renderers);
                foreach (Transform child in wrapper.transform)
                    child.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

                ConvertRendererMaterials(renderers);
                ConfigureStaticGeometry(wrapper);
                string prefabPath = $"{PrefabFolder}/NL_Ekimae_Block_{suffix}.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
                if (prefab == null) throw new System.InvalidOperationException($"Could not save {prefabPath}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(wrapper);
                Object.DestroyImmediate(groundInstance);
                Object.DestroyImmediate(propInstance);
            }
        }

        private static void ConvertRendererMaterials(IEnumerable<Renderer> renderers)
        {
            var converted = new Dictionary<Material, Material>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null) continue;
                    if (!converted.TryGetValue(source, out Material target))
                    {
                        target = ConvertMaterial(source);
                        converted[source] = target;
                    }
                    materials[i] = target;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material ConvertMaterial(Material source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string guid = AssetDatabase.AssetPathToGUID(sourcePath);
            string safeName = string.Concat(source.name.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
            string targetPath = $"{MaterialFolder}/NL_Zenrin_{safeName}_{guid.Substring(0, 8)}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("URP Lit shader is unavailable.");
            var target = new Material(shader) { name = "NL_Zenrin_" + safeName };

            Texture mainTexture = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
            Color color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            target.SetTexture("_BaseMap", mainTexture);
            target.SetColor("_BaseColor", color);
            target.SetFloat("_Smoothness", 0.18f);

            bool transparent = source.name.IndexOf("TransP", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                               source.renderQueue >= (int)RenderQueue.Transparent || color.a < 0.999f;
            if (transparent)
            {
                target.SetFloat("_Surface", 1f);
                target.SetFloat("_Blend", 0f);
                target.SetFloat("_ZWrite", 0f);
                target.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                target.renderQueue = (int)RenderQueue.Transparent;
            }

            if (source.HasProperty("_EmissionColor"))
            {
                Color emission = source.GetColor("_EmissionColor");
                if (emission.maxColorComponent > 0.01f)
                {
                    target.SetColor("_EmissionColor", emission);
                    if (source.HasProperty("_EmissionMap")) target.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
                    target.EnableKeyword("_EMISSION");
                }
            }

            AssetDatabase.CreateAsset(target, targetPath);
            return target;
        }

        private static void ConfigureStaticGeometry(GameObject wrapper)
        {
            foreach (Transform item in wrapper.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(item.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

            foreach (MeshRenderer renderer in wrapper.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool building = renderer.gameObject.name.StartsWith("_build_");
                renderer.shadowCastingMode = building ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = building;

                bool collisionSurface = building || renderer.gameObject.name.StartsWith("_road_");
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (collisionSurface && filter != null && filter.sharedMesh != null && renderer.GetComponent<Collider>() == null)
                {
                    var collider = renderer.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                }
            }
        }

        private static void IntegratePilot(GameObject blockB, GameObject blockH)
        {
            Scene scene = EditorSceneManager.OpenScene(StationScene, OpenSceneMode.Single);
            GameObject existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == PilotRootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(PilotRootName);
            GameObject left = (GameObject)PrefabUtility.InstantiatePrefab(blockB, scene);
            left.name = "NL_Ekimae_Block_B_West";
            left.transform.SetParent(root.transform);
            left.transform.SetPositionAndRotation(new Vector3(766f, 0f, -5f), Quaternion.identity);
            left.transform.localScale = Vector3.one * PilotScale;

            GameObject right = (GameObject)PrefabUtility.InstantiatePrefab(blockH, scene);
            right.name = "NL_Ekimae_Block_H_East";
            right.transform.SetParent(root.transform);
            right.transform.SetPositionAndRotation(new Vector3(834f, 0f, -5f), Quaternion.identity);
            right.transform.localScale = Vector3.one * PilotScale;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StationScene);
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        private static void AuditModel(GameObject source, string sourcePath, string labelPrefix)
        {
            GameObject instance = Object.Instantiate(source);
            try
            {
                Debug.Log($"[EkimaeAudit] Source={sourcePath} root={source.name} scale={source.transform.localScale}");
                for (char suffix = 'A'; suffix <= 'I'; suffix++)
                {
                    Transform block = FindDeep(instance.transform, "block_" + suffix);
                    if (block == null) block = FindDeep(instance.transform, "prop_" + suffix);
                    if (block == null) block = FindDeep(instance.transform, "Prop_" + suffix);
                    if (block == null)
                    {
                        IEnumerable<Transform> matches = instance.GetComponentsInChildren<Transform>(true)
                            .Where(item => item.name.EndsWith("_" + suffix) &&
                                           (item.name.StartsWith("_propA") || item.name.StartsWith("_propB")));
                        Renderer[] looseRenderers = matches.SelectMany(item => item.GetComponentsInChildren<Renderer>(true)).ToArray();
                        if (looseRenderers.Length > 0)
                        {
                            Bounds looseBounds = CalculateBounds(looseRenderers);
                            Debug.Log($"[EkimaeAudit] {labelPrefix}{suffix} looseParts={looseRenderers.Length} bounds={looseBounds.size} center={looseBounds.center}");
                            continue;
                        }
                        Debug.LogWarning($"[EkimaeAudit] {labelPrefix}{suffix} missing");
                        continue;
                    }

                    Renderer[] renderers = block.GetComponentsInChildren<Renderer>(true);
                    Bounds bounds = CalculateBounds(renderers);
                    string materials = string.Join(", ", renderers
                        .SelectMany(renderer => renderer.sharedMaterials)
                        .Where(material => material != null)
                        .Select(material => material.name)
                        .Distinct()
                        .OrderBy(name => name));
                    string children = string.Join(", ", Enumerable.Range(0, block.childCount)
                        .Select(index => block.GetChild(index).name));

                    Debug.Log($"[EkimaeAudit] block_{suffix} bounds={bounds.size} center={bounds.center} " +
                              $"renderers={renderers.Length} children=[{children}] materials=[{materials}]");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Transform FindDeep(Transform root, string targetName)
        {
            if (root.name == targetName) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private static Bounds CalculateBounds(IReadOnlyList<Renderer> renderers)
        {
            if (renderers.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
#endif
