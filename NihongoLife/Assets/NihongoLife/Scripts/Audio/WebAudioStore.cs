using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace NihongoLife.Audio
{
    /// <summary>
    /// Audio by Resources path ("Audio/Music/city_day", "Voice/ja/&lt;quest&gt;/&lt;node&gt;"). Editor, Windows and Android load
    /// it from Resources at once. The web build leaves music and story voices out of the first download (they were two
    /// thirds of it): ReleaseBuild puts them next to the page (StreamingAssets/Audio, index.json) and this class
    /// fetches each one the first time it is wanted, keeps it, and tells callers it is still on the way (IsPending),
    /// so music starts a moment later instead of the whole game loading a minute longer.
    /// </summary>
    public sealed class WebAudioStore : MonoBehaviour
    {
        [Serializable] private sealed class Index { public Entry[] files = Array.Empty<Entry>(); }
        [Serializable] private sealed class Entry { public string path, file; }

        private static WebAudioStore _host;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private static readonly HashSet<string> Pending = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> Missing = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Queue<string> Queued = new Queue<string>();
        private static Dictionary<string, string> _files; // Resources path → file under StreamingAssets/Audio
        private static bool _indexLoading;
        private const int Parallel = 3;
        private static int _running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _host = null; _files = null; _indexLoading = false; _running = 0;
            Cache.Clear(); Pending.Clear(); Missing.Clear(); Queued.Clear();
        }

        /// <summary>True when this build fetches music and voices on demand (the web build).</summary>
        public static bool OnDemand => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>The clip if it is here now; on the web, starts fetching it and returns null meanwhile.</summary>
        public static AudioClip Get(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            if (Cache.TryGetValue(resourcePath, out var cached) && cached != null) return cached;
            var local = Resources.Load<AudioClip>(resourcePath);
            if (local != null || !OnDemand) return local;
            Request(resourcePath);
            return null;
        }

        /// <summary>Still downloading (or waiting for the list of files): ask again in a moment.</summary>
        public static bool IsPending(string resourcePath) => OnDemand && !string.IsNullOrEmpty(resourcePath) && Pending.Contains(resourcePath);

        /// <summary>Starts fetching every file under a folder (a quest's voices when the quest starts).</summary>
        public static void PrefetchFolder(string folder)
        {
            if (!OnDemand || string.IsNullOrEmpty(folder)) return;
            if (_files == null) { EnsureHost(); _host.StartCoroutine(AfterIndex(() => PrefetchFolder(folder))); return; }
            string prefix = folder.TrimEnd('/') + "/";
            foreach (var path in _files.Keys)
                if (path.StartsWith(prefix, StringComparison.Ordinal) && !Cache.ContainsKey(path)) Request(path);
        }

        /// <summary>Waits (up to <paramref name="seconds"/>) for a clip, then hands it over (null when it never came).</summary>
        public static IEnumerator WaitFor(string resourcePath, float seconds, Action<AudioClip> done)
        {
            var clip = Get(resourcePath);
            for (float t = 0f; clip == null && IsPending(resourcePath) && t < seconds; t += Time.unscaledDeltaTime)
            {
                yield return null;
                Cache.TryGetValue(resourcePath, out clip);
            }
            done?.Invoke(clip);
        }

        private static void Request(string path)
        {
            if (Missing.Contains(path) || Pending.Contains(path) || Cache.ContainsKey(path)) return;
            EnsureHost();
            Pending.Add(path);
            if (_files == null) { _host.StartCoroutine(AfterIndex(() => Enqueue(path))); return; }
            Enqueue(path);
        }

        private static void Enqueue(string path)
        {
            if (!_files.ContainsKey(path)) { Pending.Remove(path); Missing.Add(path); return; }
            Queued.Enqueue(path);
            Pump();
        }

        private static void Pump()
        {
            while (_running < Parallel && Queued.Count > 0) _host.StartCoroutine(Download(Queued.Dequeue()));
        }

        private static IEnumerator Download(string path)
        {
            _running++;
            string file = _files[path];
            string url = Application.streamingAssetsPath + "/Audio/" + file;
            var type = file.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ? AudioType.OGGVORBIS : AudioType.MPEG;
            using (var request = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                yield return request.SendWebRequest();
                AudioClip clip = request.result == UnityWebRequest.Result.Success ? DownloadHandlerAudioClip.GetContent(request) : null;
                if (clip != null)
                {
                    clip.name = path.Substring(path.LastIndexOf('/') + 1); // callers compare clip names (Soundscape)
                    Cache[path] = clip;
                }
                else
                {
                    Missing.Add(path);
                    Debug.LogWarning("[WebAudioStore] Could not load " + url + ": " + request.error);
                }
            }
            Pending.Remove(path);
            _running--;
            Pump();
        }

        private static IEnumerator AfterIndex(Action then)
        {
            if (_files == null && !_indexLoading)
            {
                _indexLoading = true;
                using (var request = UnityWebRequest.Get(Application.streamingAssetsPath + "/Audio/index.json"))
                {
                    yield return request.SendWebRequest();
                    var files = new Dictionary<string, string>(StringComparer.Ordinal);
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Index index = null;
                        try { index = JsonUtility.FromJson<Index>(request.downloadHandler.text); }
                        catch (ArgumentException) { index = null; }
                        if (index?.files != null) foreach (var e in index.files) if (e != null && !string.IsNullOrEmpty(e.path)) files[e.path] = e.file;
                    }
                    else Debug.LogWarning("[WebAudioStore] No audio index: " + request.error);
                    _files = files;
                }
            }
            while (_files == null) yield return null;
            then();
        }

        private static void EnsureHost()
        {
            if (_host != null) return;
            var go = new GameObject("WebAudioStore");
            DontDestroyOnLoad(go);
            _host = go.AddComponent<WebAudioStore>();
        }
    }
}
