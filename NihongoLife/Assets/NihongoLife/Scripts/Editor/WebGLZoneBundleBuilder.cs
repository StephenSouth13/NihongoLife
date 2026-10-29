using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace NihongoLife.Editor
{
    /// <summary>
    /// Builds one AssetBundle per gameplay area for WebGL and writes zones/index.json next to the player.
    /// ReleaseBuild calls this directly. The build-window path uses <see cref="WebGLPlayerBuildProcessor"/>.
    /// No menu item: a WebGL player build is the only step.
    /// </summary>
    public static class WebGLZoneBundleBuilder
    {
        public const string StagingFolderName = "WebGLZoneBundles";
        public static bool ReleaseBuildInProgress { get; set; }
        public static bool BundlesReady { get; private set; }

        public static readonly (string path, string bundle)[] Zones =
        {
            ("Assets/NihongoLife/Scenes/90_TestSandbox.unity", "nlzone-city"),
            ("Assets/NihongoLife/Scenes/20_StationDistrict.unity", "nlzone-station"),
            ("Assets/NihongoLife/Scenes/30_SushiRestaurant.unity", "nlzone-sushi"),
            ("Assets/NihongoLife/Scenes/40_ HIBARICLASS.unity", "nlzone-school"),
            ("Assets/NihongoLife/Scenes/45_HomeBedroom.unity", "nlzone-home"),
            ("Assets/NihongoLife/Scenes/50_GameCenter.unity", "nlzone-arcade"),
            ("Assets/NihongoLife/Scenes/60_MidoriIsland.unity", "nlzone-island"),
            ("Assets/NihongoLife/Scenes/35_HibariClinic.unity", "nlzone-clinic"),
            ("Assets/NihongoLife/Scenes/55_MusicShop.unity", "nlzone-music"),
            ("Assets/NihongoLife/Scenes/65_PhuQuocIsland.unity", "nlzone-phuquoc")
        };

        public static readonly string[] PlayerScenes =
        {
            "Assets/NihongoLife/Scenes/01_MainMenu.unity",
            "Assets/NihongoLife/Scenes/00_Bootstrap.unity"
        };

        public static string StagingPath() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", StagingFolderName));

        public static void BuildBundles()
        {
            if (BundlesReady) return;
            foreach (var zone in Zones)
            {
                if (!File.Exists(zone.path))
                    throw new BuildFailedException("Missing zone scene: " + zone.path);
            }
            string staging = StagingPath();
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);
            var builds = new AssetBundleBuild[Zones.Length];
            for (int i = 0; i < Zones.Length; i++)
            {
                builds[i] = new AssetBundleBuild
                {
                    assetBundleName = Zones[i].bundle,
                    assetNames = new[] { Zones[i].path }
                };
            }
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                staging,
                builds,
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.AppendHashToAssetBundleName,
                BuildTarget.WebGL);
            if (manifest == null) throw new BuildFailedException("WebGL zone bundle build failed.");
            BundlesReady = true;
            Debug.Log("[WebGL] Zone bundles ready in " + staging);
        }

        public static void Publish(string playerOutput)
        {
            if (string.IsNullOrEmpty(playerOutput)) return;
            string root = Directory.Exists(playerOutput) ? playerOutput : Path.GetDirectoryName(playerOutput);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
            string staging = StagingPath();
            if (!Directory.Exists(staging))
            {
                Debug.LogWarning("[WebGL] No zone bundles to copy. WebGL area loads will fail until a bundle build runs.");
                return;
            }
            string dest = Path.Combine(root, "zones");
            Directory.CreateDirectory(dest);
            var index = new StringBuilder();
            index.Append("{\"bundles\":[");
            bool first = true;
            foreach (var zone in Zones)
            {
                string match = Directory.GetFiles(staging).FirstOrDefault(file =>
                {
                    string name = Path.GetFileName(file);
                    return name.StartsWith(zone.bundle + "_", StringComparison.OrdinalIgnoreCase) && !name.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase);
                });
                if (match == null) throw new BuildFailedException("Zone bundle was not produced: " + zone.bundle);
                string fileName = Path.GetFileName(match);
                File.Copy(match, Path.Combine(dest, fileName), true);
                if (!first) index.Append(',');
                first = false;
                string scene = Path.GetFileNameWithoutExtension(zone.path);
                index.Append("{\"scene\":").Append(Quote(scene))
                    .Append(",\"file\":").Append(Quote(fileName))
                    .Append('}');
            }
            index.Append("]}");
            File.WriteAllText(Path.Combine(dest, "index.json"), index.ToString());
            Debug.Log("[WebGL] Published " + Zones.Length + " zone bundles to " + dest);
        }

        static string Quote(string value) => "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public static class WebGLShaderWarmupBuilder
    {
        public const string CollectionPath = "Assets/NihongoLife/Resources/WebGL/WebGLShaderVariants.shadervariants";

        public static void BuildCollection()
        {
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/NihongoLife/Resources/WebGL"))
                    AssetDatabase.CreateFolder("Assets/NihongoLife/Resources", "WebGL");
                var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(CollectionPath);
                if (collection == null)
                {
                    collection = new ShaderVariantCollection();
                    AssetDatabase.CreateAsset(collection, CollectionPath);
                }
                collection.Clear();
                var seen = new HashSet<Shader>();
                int added = 0;
                foreach (string guid in AssetDatabase.FindAssets("t:Material"))
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (material == null || material.shader == null || !seen.Add(material.shader)) continue;
                    if (TryAdd(collection, material.shader, PassType.ScriptableRenderPipeline)) added++;
                    if (TryAdd(collection, material.shader, PassType.Normal)) added++;
                }
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
                Debug.Log($"[WebGL] Shader warmup collection: {seen.Count} shaders, {added} variants.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WebGL] Shader warmup collection was not rebuilt: " + ex.Message);
            }
        }

        static bool TryAdd(ShaderVariantCollection collection, Shader shader, PassType pass)
        {
            try
            {
                collection.Add(new ShaderVariantCollection.ShaderVariant(shader, pass));
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    public sealed class WebGLPlayerBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string SceneBackup = "Temp/nl-webgl-build-scenes.txt";
        static EditorBuildSettingsScene[] _saved;
        public int callbackOrder => 50;

        [InitializeOnLoadMethod]
        static void RestoreInterruptedSceneList()
        {
            if (!File.Exists(SceneBackup) || WebGLZoneBundleBuilder.ReleaseBuildInProgress) return;
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string line in File.ReadAllLines(SceneBackup))
            {
                int bar = line.IndexOf('|');
                if (bar <= 0) continue;
                scenes.Add(new EditorBuildSettingsScene(line.Substring(bar + 1), line.Substring(0, bar) == "1"));
            }
            if (scenes.Count == 0) return;
            EditorBuildSettings.scenes = scenes.ToArray();
            File.Delete(SceneBackup);
            Debug.Log("[WebGL] Restored Editor build scenes after an unfinished WebGL build.");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            WebGLShaderWarmupBuilder.BuildCollection();
            if (WebGLZoneBundleBuilder.ReleaseBuildInProgress) return;
            _saved = EditorBuildSettings.scenes;
            WriteBackup(_saved);
            var keep = new HashSet<string>(WebGLZoneBundleBuilder.PlayerScenes, StringComparer.OrdinalIgnoreCase);
            EditorBuildSettings.scenes = _saved
                .Select(scene => new EditorBuildSettingsScene(scene.path, scene.enabled && keep.Contains(scene.path)))
                .ToArray();
            WebGLZoneBundleBuilder.BuildBundles();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            try
            {
                WebGLZoneBundleBuilder.Publish(report.summary.outputPath);
                WriteWebConfig(report.summary.outputPath);
                WriteSizeReport(report);
            }
            finally
            {
                if (_saved != null)
                {
                    EditorBuildSettings.scenes = _saved;
                    _saved = null;
                }
                if (File.Exists(SceneBackup)) File.Delete(SceneBackup);
            }
        }

        /// <summary>
        /// nl-config.js beside index.html: the public online settings (Supabase URL + anon key, Agora App ID) from
        /// LocalSecrets/secrets.json or the environment. A browser cannot read that file, and without these the web build
        /// says "server not configured" and every online button is dead. Build output is gitignored; the Gemini key and
        /// any service key are never written.
        /// </summary>
        internal static void WriteWebConfig(string outputPath)
        {
            string root = Directory.Exists(outputPath) ? outputPath : Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(root)) return;
            var fields = new List<string>();
            foreach (string key in NihongoLife.Core.LocalSecrets.PublicWebKeys)
            {
                string value = NihongoLife.Core.LocalSecrets.Resolve(key, null);
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (IsSecretKey(value))
                    throw new BuildFailedException(key + " holds a secret key; the web build only takes the public anon key.");
                fields.Add("\"" + key + "\": \"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
            }
            string body = fields.Count == 0 ? "null" : "{ " + string.Join(", ", fields) + " }";
            File.WriteAllText(Path.Combine(root, "nl-config.js"),
                "// Written by the WebGL build (WebGLZoneBundleBuilder.WriteWebConfig). Public client settings only.\n" +
                "window.NIHONGOLIFE_CONFIG = " + body + ";\n", new System.Text.UTF8Encoding(false));
            if (fields.Count < 2)
                Debug.LogWarning("[WebGL] nl-config.js has no Supabase URL / anon key: the web build stays offline. Fill LocalSecrets/secrets.json.");
            else Debug.Log("[WebGL] nl-config.js written (" + fields.Count + " public settings).");
        }

        /// <summary>A new-style secret key, or a legacy JWT whose payload says role service_role.</summary>
        internal static bool IsSecretKey(string value)
        {
            if (value.StartsWith("sb_secret_", StringComparison.Ordinal)) return true;
            string[] parts = value.Split('.');
            if (parts.Length != 3) return false;
            try
            {
                string payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                return Encoding.UTF8.GetString(Convert.FromBase64String(payload)).Contains("service_role");
            }
            catch (FormatException) { return false; }
        }

        static void WriteBackup(EditorBuildSettingsScene[] scenes)
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllLines(SceneBackup, scenes.Select(scene => (scene.enabled ? "1" : "0") + "|" + scene.path));
        }

        static void WriteSizeReport(BuildReport report)
        {
            string root = Directory.Exists(report.summary.outputPath) ? report.summary.outputPath : Path.GetDirectoryName(report.summary.outputPath);
            if (string.IsNullOrEmpty(root)) return;
            var rows = new List<(ulong size, string path)>();
            foreach (var packed in report.packedAssets)
            {
                if (packed.contents == null) continue;
                foreach (var content in packed.contents)
                    rows.Add((content.packedSize, content.sourceAssetPath));
            }
            var text = new StringBuilder();
            text.AppendLine("totalBytes " + report.summary.totalSize);
            text.AppendLine("output " + report.summary.outputPath);
            foreach (var row in rows.OrderByDescending(row => row.size).Take(60))
                text.AppendLine(row.size.ToString().PadLeft(12) + "  " + row.path);
            File.WriteAllText(Path.Combine(root, "WebGLBuildSizeReport.txt"), text.ToString());
        }
    }
}
