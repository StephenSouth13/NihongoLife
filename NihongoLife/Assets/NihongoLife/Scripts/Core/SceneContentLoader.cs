using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    /// <summary>
    /// On WebGL, gameplay scenes are not in the first download. Each area is an AssetBundle next to the build
    /// (<c>zones/index.json</c>). Editor, Windows and Android keep loading scenes from the player build.
    /// </summary>
    public sealed class SceneContentLoader : MonoBehaviour
    {
        public const string ZonesFolder = "zones";

        static readonly Dictionary<string, string> BundleNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [WorldLocationCatalog.CityScene] = "nlzone-city",
            [WorldLocationCatalog.StationScene] = "nlzone-station",
            [WorldLocationCatalog.SushiRestaurantScene] = "nlzone-sushi",
            [WorldLocationCatalog.SchoolScene] = "nlzone-school",
            [WorldLocationCatalog.HomeBedroomScene] = "nlzone-home",
            [WorldLocationCatalog.GameCenterScene] = "nlzone-arcade",
            [WorldLocationCatalog.MidoriIslandScene] = "nlzone-island"
        };

        static readonly Dictionary<string, AssetBundle> Loaded = new Dictionary<string, AssetBundle>(StringComparer.Ordinal);
        static Dictionary<string, ZoneEntry> _index;
        static bool _indexTried;
        static bool _warmed;
        static string _activeError;

        [Serializable] class ZoneIndex { public ZoneEntry[] bundles; }
        [Serializable] class ZoneEntry { public string scene; public string file; public string hash; }

        public static string LastError => _activeError;

        public static bool IsOnDemandScene(string sceneName) =>
            !string.IsNullOrEmpty(sceneName) && BundleNames.ContainsKey(sceneName);

        public static void LoadFallback(string sceneName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var host = new GameObject("SceneContentLoader");
            DontDestroyOnLoad(host);
            host.AddComponent<SceneContentLoader>().StartCoroutine(LoadFallbackRoutine(sceneName, host));
#else
            SceneManager.LoadScene(sceneName);
#endif
        }

        static IEnumerator LoadFallbackRoutine(string sceneName, GameObject host)
        {
            yield return EnsureAvailable(sceneName, null);
            if (!string.IsNullOrEmpty(_activeError)) Debug.LogError("[SceneContent] " + _activeError);
            else
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                while (op != null && !op.isDone) yield return null;
            }
            if (host != null) Destroy(host);
        }

        /// <summary>Downloads the area bundle when this WebGL build does not already contain the scene.</summary>
        public static IEnumerator EnsureAvailable(string sceneName, Action<float> progress)
        {
            _activeError = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            yield return WarmShaders(progress);
            if (string.IsNullOrEmpty(sceneName) || !BundleNames.TryGetValue(sceneName, out string bundleName)) yield break;
            if (Application.CanStreamedLevelBeLoaded(sceneName) || Loaded.ContainsKey(bundleName)) yield break;

            if (!_indexTried)
            {
                _indexTried = true;
                yield return LoadIndex();
            }
            ZoneEntry entry = FindEntry(sceneName, bundleName);
            string file = entry != null && !string.IsNullOrWhiteSpace(entry.file) ? entry.file : bundleName;
            string url = BaseUrl + file;
            Hash128 hash = default;
            bool hashed = false;
            if (entry != null && !string.IsNullOrWhiteSpace(entry.hash))
            {
                try
                {
                    hash = Hash128.Parse(entry.hash);
                    hashed = hash.isValid;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[SceneContent] Ignoring bundle hash: " + ex.Message);
                }
            }
            using UnityWebRequest request = hashed
                ? UnityWebRequestAssetBundle.GetAssetBundle(url, hash, 0)
                : UnityWebRequestAssetBundle.GetAssetBundle(url);
            request.SendWebRequest();
            while (!request.isDone)
            {
                progress?.Invoke(request.downloadProgress);
                yield return null;
            }
            if (request.result != UnityWebRequest.Result.Success)
            {
                _activeError = Loc.F(
                    "Không tải được khu vực {0} ({1}). Máy chủ cần có thư mục zones cạnh index.html.",
                    "Could not download area {0} ({1}). The server needs a zones folder next to index.html.",
                    "エリア{0}を取得できません（{1}）。index.htmlの横に zones フォルダが必要です。",
                    sceneName, request.responseCode);
                yield break;
            }
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            if (bundle == null)
            {
                _activeError = Loc.F("Gói khu vực {0} trống.", "Area pack {0} was empty.", "エリア{0}のパックが空です。", sceneName);
                yield break;
            }
            Loaded[bundleName] = bundle;
#else
            yield break;
#endif
        }

        static ZoneEntry FindEntry(string sceneName, string bundleName)
        {
            if (_index == null) return null;
            if (_index.TryGetValue(sceneName, out ZoneEntry byScene)) return byScene;
            _index.TryGetValue(bundleName, out ZoneEntry byFile);
            return byFile;
        }

        static IEnumerator LoadIndex()
        {
            using var request = UnityWebRequest.Get(BaseUrl + "index.json");
            request.timeout = 20;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success || string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                Debug.LogWarning("[SceneContent] zones/index.json is missing; trying fixed bundle names.");
                yield break;
            }
            ZoneIndex index = null;
            try { index = JsonUtility.FromJson<ZoneIndex>(request.downloadHandler.text); }
            catch (Exception ex) { Debug.LogWarning("[SceneContent] index.json: " + ex.Message); }
            if (index?.bundles == null) yield break;
            _index = new Dictionary<string, ZoneEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (ZoneEntry entry in index.bundles)
            {
                if (entry == null || string.IsNullOrEmpty(entry.scene)) continue;
                _index[entry.scene] = entry;
            }
        }

        static IEnumerator WarmShaders(Action<float> progress)
        {
            if (_warmed) yield break;
            _warmed = true;
            ShaderVariantCollection collection = Resources.Load<ShaderVariantCollection>("WebGL/WebGLShaderVariants");
            if (collection != null && !collection.isWarmedUp)
            {
                progress?.Invoke(0.2f);
                collection.WarmUp();
                yield return null;
            }
        }

        public static string BaseUrl
        {
            get
            {
                string abs = Application.absoluteURL;
                if (string.IsNullOrEmpty(abs)) return ZonesFolder + "/";
                int query = abs.IndexOf('?');
                if (query >= 0) abs = abs.Substring(0, query);
                int hash = abs.IndexOf('#');
                if (hash >= 0) abs = abs.Substring(0, hash);
                int slash = abs.LastIndexOf('/');
                if (slash < 0) return ZonesFolder + "/";
                return abs.Substring(0, slash + 1) + ZonesFolder + "/";
            }
        }
    }
}
