#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>Isolated model thumbnails. No scene asset, source material, runtime catalog, or existing icon edits.</summary>
    public static class AssetPipelineIconBatch
    {
        private const string OutputRoot = "Assets/NihongoLife/Generated/AssetPipeline/Icons/";
        [Serializable] public sealed class Entry { public string id, assetPath, guid, iconOutputPath; }
        [Serializable] public sealed class Jobs { public int schemaVersion, resolution; public float margin; public Entry[] entries; }
        [Serializable] public sealed class Result { public string id, assetPath, iconOutputPath, status, detail; public int width, height; }
        [Serializable] public sealed class Report { public string fatalError; public Result[] entries; }

        public static void Render()
        {
            int exit = 0;
            string fatalError = null;
            var results = new List<Result>();
            string reportPath = "Docs/AssetPipeline/Reports/render_results.json";
            try
            {
                var args = Environment.GetCommandLineArgs();
                string manifest = Argument(args, "-iconManifest") ?? "Docs/AssetPipeline/Reports/icon_jobs.json";
                var jobs = JsonUtility.FromJson<Jobs>(File.ReadAllText(manifest));
                if (jobs == null || jobs.schemaVersion != 1 || jobs.entries == null ||
                    (jobs.resolution != 256 && jobs.resolution != 512) || jobs.margin < 0.05f || jobs.margin > 0.3f)
                    throw new InvalidDataException("Invalid icon job manifest.");
                if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                    throw new InvalidOperationException("An active URP pipeline is required.");
                int limit = int.TryParse(Argument(args, "-iconLimit"), out var count) ? count : int.MaxValue;
                if (limit < 1) throw new InvalidDataException("iconLimit must be positive.");
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in jobs.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id) || entry.id.Any(c => !((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')) ||
                        !ids.Add(entry.id) || !paths.Add(entry.iconOutputPath ?? "") ||
                        entry.iconOutputPath != OutputRoot + entry.id + ".png" ||
                        string.IsNullOrEmpty(entry.assetPath) || !entry.assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                        entry.assetPath.Contains("..") || entry.assetPath.Contains('\\'))
                        throw new InvalidDataException("Invalid/duplicate icon job or unsafe path.");
                }
                foreach (var entry in jobs.entries.OrderBy(e => e.id, StringComparer.Ordinal).Take(limit))
                {
                    var result = new Result { id = entry.id, assetPath = entry.assetPath, iconOutputPath = entry.iconOutputPath };
                    results.Add(result);
                    try
                    {
                        if (!string.IsNullOrEmpty(entry.guid) && AssetDatabase.AssetPathToGUID(entry.assetPath) != entry.guid)
                            throw new InvalidDataException("Asset GUID changed; regenerate/review audit.");
                        RenderOne(entry, jobs.resolution, jobs.margin);
                        result.status = "rendered";
                        result.width = result.height = jobs.resolution;
                    }
                    catch (Exception exception)
                    {
                        result.status = "failed";
                        result.detail = exception.Message;
                        exit = 1;
                        Debug.LogWarning($"[AssetPipeline] {entry.id}: {exception.Message}");
                    }
                }
                AssetDatabase.Refresh();
                foreach (var result in results.Where(r => r.status == "rendered"))
                {
                    var importer = AssetImporter.GetAtPath(result.iconOutputPath) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("Generated icon importer unavailable.");
                    // Match ItemIcons.Get's Texture2D loading; do not create a second runtime icon system.
                    importer.textureType = TextureImporterType.Default;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.maxTextureSize = jobs.resolution;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            catch (Exception exception) { Debug.LogException(exception); fatalError = exception.Message; exit = 1; }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, JsonUtility.ToJson(new Report { fatalError = fatalError, entries = results.ToArray() }, true) + "\n");
            }
            Debug.Log($"[AssetPipeline] {results.Count(r => r.status == "rendered")} rendered; {results.Count(r => r.status == "failed")} failed.");
            if (Application.isBatchMode) EditorApplication.Exit(exit);
        }

        private static string Argument(string[] args, string key)
        {
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private static void RenderOne(Entry entry, int size, float margin)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(entry.assetPath);
            if (source == null) throw new InvalidDataException("No imported GameObject geometry at source path.");
            // PreviewRenderUtility owns a temporary unsaved preview workspace; no gameplay scene is opened or saved.
            var preview = new PreviewRenderUtility();
            GameObject instance = null;
            RenderTexture target = null;
            Texture2D pixels = null;
            var materialCopies = new List<Material>();
            var previousTarget = RenderTexture.active;
            bool previewOpened = false;
            try
            {
                preview.BeginPreview(new Rect(0, 0, size, size), GUIStyle.none);
                previewOpened = true;
                // BeginPreview scopes lighting to the preview workspace and restores it in EndPreview.
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);
                // Instantiate disabled to prevent ExecuteAlways scripts from running during icon generation.
                var holder = new GameObject("Icon geometry") { hideFlags = HideFlags.HideAndDontSave };
                holder.SetActive(false);
                preview.AddSingleGO(holder);
                instance = Object.Instantiate(source, holder.transform, false);
                foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                    Object.DestroyImmediate(behaviour);
                foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                foreach (var light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
                foreach (var cam in instance.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
                instance.SetActive(true);
                holder.SetActive(true);
                var renderers = instance.GetComponentsInChildren<Renderer>().Where(r => r.enabled &&
                    (r is MeshRenderer || r is SkinnedMeshRenderer)).ToArray();
                if (renderers.Length == 0) throw new InvalidDataException("No active mesh renderers.");
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    if (materials.Length == 0 || materials.Any(m => m == null))
                        throw new InvalidDataException("Missing imported materials; fix mapping before rendering.");
                    renderer.sharedMaterials = materials.Select(m => CopyMaterial(m, materialCopies)).ToArray();
                }
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.sqrMagnitude < 0.00000001f || float.IsNaN(bounds.size.sqrMagnitude) || float.IsInfinity(bounds.size.sqrMagnitude))
                    throw new InvalidDataException("Empty or non-finite model bounds.");
                var camera = preview.camera;
                camera.cameraType = CameraType.Preview;
                camera.orthographic = true;
                camera.aspect = 1f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.transform.rotation = Quaternion.Euler(24f, -32f, 0f);
                // Fit all eight world bounds corners in camera coordinates, including long/irregular models.
                var inverse = Quaternion.Inverse(camera.transform.rotation);
                float halfX = 0, halfY = 0, halfZ = 0;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var corner = inverse * Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    halfX = Mathf.Max(halfX, Mathf.Abs(corner.x));
                    halfY = Mathf.Max(halfY, Mathf.Abs(corner.y));
                    halfZ = Mathf.Max(halfZ, Mathf.Abs(corner.z));
                }
                camera.orthographicSize = Mathf.Max(halfX, halfY) / (1 - 2 * margin);
                float distance = Mathf.Max(bounds.size.magnitude * 2, 1);
                camera.transform.position = bounds.center - camera.transform.forward * distance;
                camera.nearClipPlane = Mathf.Max(0.001f, distance - halfZ - bounds.size.magnitude);
                camera.farClipPlane = distance + halfZ + bounds.size.magnitude;
                var additional = camera.GetUniversalAdditionalCameraData();
                additional.renderPostProcessing = false;
                additional.antialiasing = AntialiasingMode.None;
                additional.renderShadows = false;
                preview.ambientColor = new Color(0.55f, 0.55f, 0.58f);
                preview.lights[0].transform.rotation = Quaternion.Euler(45, -35, 0);
                preview.lights[0].intensity = 1.25f;
                preview.lights[0].color = new Color(1, 0.97f, 0.9f);
                preview.lights[1].transform.rotation = Quaternion.Euler(20, 140, 0);
                preview.lights[1].intensity = 0.65f;
                preview.lights[1].color = new Color(0.75f, 0.85f, 1);
                foreach (var light in preview.lights) light.enabled = true;
                target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
                target.Create();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("Active URP renderer does not support single-camera requests.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                pixels = new Texture2D(size, size, TextureFormat.RGBA32, false);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                pixels.Apply();
                var colors = pixels.GetPixels32();
                int visible = 0;
                for (int i = 0; i < colors.Length; i++)
                {
                    if (colors[i].a <= 8) continue;
                    visible++;
                    int x = i % size, y = i / size;
                    if (x <= 1 || y <= 1 || x >= size - 2 || y >= size - 2)
                        throw new InvalidDataException("Geometry touches image edge; review framing.");
                    if (colors[i].r > 240 && colors[i].b > 240 && colors[i].g < 25)
                        throw new InvalidDataException("Magenta error shader detected.");
                }
                if (visible == 0 || visible == colors.Length)
                    throw new InvalidDataException("Empty icon or opaque background; render rejected.");
                Directory.CreateDirectory(Path.GetDirectoryName(entry.iconOutputPath));
                File.WriteAllBytes(entry.iconOutputPath, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (pixels != null) Object.DestroyImmediate(pixels);
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                if (previewOpened) preview.EndPreview();
                preview.Cleanup();
                RenderTexture.active = previousTarget;
                foreach (var material in materialCopies) Object.DestroyImmediate(material);
            }
        }

        private static Material CopyMaterial(Material source, List<Material> copies)
        {
            var copy = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
            copies.Add(copy);
            if (source.shader == null) throw new InvalidDataException("Missing shader.");
            if (source.shader.name == "Standard" || source.shader.name == "Standard (Specular setup)" || source.shader.name == "Legacy Shaders/Diffuse")
            {
                if (source.HasProperty("_Mode") && source.GetFloat("_Mode") != 0)
                    throw new InvalidDataException("Transparent Standard material requires reviewed URP conversion.");
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidDataException("URP Lit shader missing.");
                copy.shader = shader;
                copy.SetColor("_BaseColor", source.GetColor("_Color"));
                copy.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                copy.SetTextureScale("_BaseMap", source.GetTextureScale("_MainTex"));
                copy.SetTextureOffset("_BaseMap", source.GetTextureOffset("_MainTex"));
                copy.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : 0);
                copy.SetFloat("_WorkflowMode", source.shader.name == "Standard (Specular setup)" ? 0 : 1);
                if (source.HasProperty("_MetallicGlossMap") && source.GetTexture("_MetallicGlossMap") != null ||
                    source.HasProperty("_SpecGlossMap") && source.GetTexture("_SpecGlossMap") != null)
                    copy.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else if (!source.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
                throw new InvalidDataException("Custom/non-URP shader requires reviewed conversion: " + source.shader.name);
            return copy;
        }
    }
}
#endif
