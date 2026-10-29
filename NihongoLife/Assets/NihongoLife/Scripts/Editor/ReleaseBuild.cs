using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NihongoLife.Editor
{
    /// <summary>
    /// Non-interactive release entry points. Invoke with Unity -executeMethod; no editor menu or setup step.
    /// Environment variables: NIHONGO_VERSION, NIHONGO_ANDROID_CODE, NIHONGO_APP_ID, NIHONGO_BUILD_ROOT.
    /// </summary>
    public static class ReleaseBuild
    {
        private const string DefaultAppId = "com.vtcacademy.nihongolife";

        private const string CityScenePath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";

        private static readonly string[] ProductionScenes =
        {
            "Assets/NihongoLife/Scenes/00_Bootstrap.unity",
            "Assets/NihongoLife/Scenes/01_MainMenu.unity",
            // The town is the game's hub (WorldLocationCatalog.CityScene); "TestSandbox" is only its historical name.
            CityScenePath,
            "Assets/NihongoLife/Scenes/20_StationDistrict.unity",
            "Assets/NihongoLife/Scenes/30_SushiRestaurant.unity",
            "Assets/NihongoLife/Scenes/40_ HIBARICLASS.unity",
            "Assets/NihongoLife/Scenes/45_HomeBedroom.unity",
            "Assets/NihongoLife/Scenes/50_GameCenter.unity",
            "Assets/NihongoLife/Scenes/60_MidoriIsland.unity",
            "Assets/NihongoLife/Scenes/35_HibariClinic.unity",
            "Assets/NihongoLife/Scenes/55_MusicShop.unity",
            "Assets/NihongoLife/Scenes/65_PhuQuocIsland.unity"
        };

        public static void BuildWebGL()
        {
            ConfigureCommon();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true; // loads on any host (itch.io, GitHub Pages), not only servers that send Content-Encoding: br
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            // A 32 MB heap grows repeatedly while the first world is loading. Each WebAssembly growth copies memory
            // and presents as a long hitch in the browser. Start at a realistic footprint and retain bounded growth.
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            // Explicitly thrown exceptions stay on: voice, Agora and Supabase use try/catch. "None" would skip those catches.
            // High stripping is the release default. Minimal stripping drops reflection JsonUtility and the URP render-scale setter need.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);

            string output = Path.Combine(BuildRoot(), "WebGL");
            WebGLZoneBundleBuilder.ReleaseBuildInProgress = true;
            try
            {
                // Area scenes are downloaded from zones/ after the menu. The player itself stays the menu plus bootstrap.
                WebGLZoneBundleBuilder.BuildBundles();
                // Music and story voices stay out of the first download; they are fetched on demand (WebAudioOnDemand).
                WebAudioOnDemand.Park();
                try { Build(BuildTarget.WebGL, output, BuildOptions.None, WebGLZoneBundleBuilder.PlayerScenes); }
                finally { WebAudioOnDemand.Restore(); }
                WebAudioOnDemand.Publish(output);
            }
            finally
            {
                WebGLZoneBundleBuilder.ReleaseBuildInProgress = false;
            }
        }

        public static void BuildAndroid()
        {
            ConfigureCommon();
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId());
            PlayerSettings.Android.bundleVersionCode = ReadPositiveInt("NIHONGO_ANDROID_CODE", 1);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);

            Build(BuildTarget.Android, Path.Combine(BuildRoot(), "Android", "NihongoLife.aab"), BuildOptions.None);
        }

        public static void ValidateRelease()
        {
            ConfigureCommon();
            ValidateScenes();
            if (AppId().Contains("UnityTechnologies", StringComparison.OrdinalIgnoreCase) ||
                AppId().Contains("template", StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Release application identifier is still a Unity template identifier.");
            Debug.Log($"[ReleaseBuild] Validation passed: {PlayerSettings.productName} {PlayerSettings.bundleVersion} ({AppId()}).");
        }

        private static void ConfigureCommon()
        {
            PlayerSettings.companyName = "VTC Academy";
            PlayerSettings.productName = "NihongoLife";
            PlayerSettings.bundleVersion = Environment.GetEnvironmentVariable("NIHONGO_VERSION") ?? "0.1.0";
            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId());
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId());
        }

        private static void Build(BuildTarget target, string output, BuildOptions options, string[] scenes = null)
        {
            ValidateRelease();
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes ?? ProductionScenes,
                target = target,
                locationPathName = output,
                options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"{target} build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            if (target == BuildTarget.WebGL)
            {
                WebGLZoneBundleBuilder.Publish(output);
                WriteWebHostingFiles(output);
            }
            Debug.Log($"[ReleaseBuild] {target} ready at {output}; {report.summary.totalSize / (1024f * 1024f):F1} MB.");
        }

        private static void WriteWebHostingFiles(string output)
        {
            // Netlify/Cloudflare Pages read _headers. Apache reads .htaccess. Hashed data/code can be cached forever;
            // index/loader stay revalidated so a deployment never mixes old and new build files.
            // Decompression fallback is on, so do NOT set Content-Encoding: br. Unity's loader
            // inflates the .unityweb payload itself. Content-Encoding would make the browser inflate
            // it first and the loader would fail. Set types so wasm still compiles.
            // Zone bundle filenames include a hash; index.json must stay revalidated.
            File.WriteAllText(Path.Combine(output, "_headers"),
                "/Build/*\n  Cache-Control: public, max-age=31536000, immutable\n" +
                "/Build/*.wasm\n  Content-Type: application/wasm\n" +
                "/Build/*.js\n  Content-Type: application/javascript\n" +
                "/zones/index.json\n  Cache-Control: no-cache\n" +
                "/zones/*\n  Cache-Control: public, max-age=31536000, immutable\n" +
                "/index.html\n  Cache-Control: no-cache\n");
            File.WriteAllText(Path.Combine(output, ".htaccess"),
                "AddType application/wasm .wasm\n" +
                "AddType application/javascript .js\n" +
                "<IfModule mod_headers.c>\n" +
                "  <FilesMatch \\\"\\.(data|wasm|js|unityweb)$\\\">\n" +
                "    Header set Cache-Control \\\"public, max-age=31536000, immutable\\\"\n" +
                "  </FilesMatch>\n" +
                "  <FilesMatch \\\"^(index\\.html|.*loader.*\\.js|index\\.json)$\\\">\n" +
                "    Header set Cache-Control \\\"no-cache\\\"\n" +
                "  </FilesMatch>\n" +
                "</IfModule>\n" +
                "# Do not add \"Header set Content-Encoding br\" while WebGL decompression fallback is enabled.\n");
            // Vercel reads vercel.json from the published folder. Same rule: do not set Content-Encoding.
            File.WriteAllText(Path.Combine(output, "vercel.json"),
                "{\n" +
                "  \"headers\": [\n" +
                "    { \"source\": \"/Build/(.*).wasm\", \"headers\": [\n" +
                "      { \"key\": \"Content-Type\", \"value\": \"application/wasm\" },\n" +
                "      { \"key\": \"Cache-Control\", \"value\": \"public, max-age=31536000, immutable\" }\n" +
                "    ]},\n" +
                "    { \"source\": \"/Build/(.*).js\", \"headers\": [\n" +
                "      { \"key\": \"Content-Type\", \"value\": \"application/javascript\" },\n" +
                "      { \"key\": \"Cache-Control\", \"value\": \"public, max-age=31536000, immutable\" }\n" +
                "    ]},\n" +
                "    { \"source\": \"/Build/(.*)\", \"headers\": [\n" +
                "      { \"key\": \"Cache-Control\", \"value\": \"public, max-age=31536000, immutable\" }\n" +
                "    ]},\n" +
                "    { \"source\": \"/zones/index.json\", \"headers\": [\n" +
                "      { \"key\": \"Cache-Control\", \"value\": \"no-cache\" }\n" +
                "    ]},\n" +
                "    { \"source\": \"/zones/(.*)\", \"headers\": [\n" +
                "      { \"key\": \"Cache-Control\", \"value\": \"public, max-age=31536000, immutable\" }\n" +
                "    ]},\n" +
                "    { \"source\": \"/index.html\", \"headers\": [\n" +
                "      { \"key\": \"Cache-Control\", \"value\": \"no-cache\" }\n" +
                "    ]}\n" +
                "  ]\n" +
                "}\n");
        }

        private static void ValidateScenes()
        {
            var missing = ProductionScenes.Where(scene => !File.Exists(scene)).ToArray();
            if (missing.Length > 0)
                throw new BuildFailedException("Missing production scenes: " + string.Join(", ", missing));
            if (ProductionScenes.Any(scene => scene != CityScenePath && scene.Contains("Test", StringComparison.OrdinalIgnoreCase)))
                throw new BuildFailedException("A test scene is included in the production scene list.");
        }

        private static string AppId() => Environment.GetEnvironmentVariable("NIHONGO_APP_ID") ?? DefaultAppId;

        private static string BuildRoot()
        {
            string configured = Environment.GetEnvironmentVariable("NIHONGO_BUILD_ROOT");
            return string.IsNullOrWhiteSpace(configured) ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Build", "Release")) : Path.GetFullPath(configured);
        }

        private static int ReadPositiveInt(string name, int fallback)
        {
            string raw = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!int.TryParse(raw, out int value) || value < 1)
                throw new BuildFailedException($"{name} must be a positive integer.");
            return value;
        }
    }
}
