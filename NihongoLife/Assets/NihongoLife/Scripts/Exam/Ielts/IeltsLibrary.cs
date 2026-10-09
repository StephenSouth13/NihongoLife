using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NihongoLife.Exam.Ielts
{
    /// <summary>
    /// Finds IELTS test packages on disk and loads them at runtime. Packages built from licensed books live in
    /// "LocalContent/IELTS/&lt;package&gt;/" beside the Unity project's Assets folder (gitignored, never imported
    /// by Unity, never part of a build). A package is a folder with test.json, key.json and its audio files.
    /// Nothing here logs question or answer text.
    /// </summary>
    public static class IeltsLibrary
    {
        public sealed class Package
        {
            public string Folder;
            public IeltsTest Test;
            public string Id => Test?.id;
        }

        /// <summary>LocalContent root: next to Assets in the Editor, next to the *_Data folder in a player build.</summary>
        public static string Root
        {
            get
            {
                string overridePath = Environment.GetEnvironmentVariable("NIHONGOLIFE_LOCAL_CONTENT");
                if (!string.IsNullOrEmpty(overridePath)) return Path.Combine(overridePath, "IELTS");
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LocalContent", "IELTS"));
            }
        }

        public static List<Package> Discover()
        {
            var packages = new List<Package>();
            if (!Directory.Exists(Root)) return packages;
            foreach (string folder in Directory.GetDirectories(Root))
            {
                string path = Path.Combine(folder, "test.json");
                if (!File.Exists(path)) continue;
                try
                {
                    var test = JsonUtility.FromJson<IeltsTest>(File.ReadAllText(path));
                    if (test != null && test.parts != null && test.parts.Length > 0)
                        packages.Add(new Package { Folder = folder, Test = test });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[IeltsLibrary] Skipped package '{Path.GetFileName(folder)}': {exception.GetType().Name}");
                }
            }
            packages.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return packages;
        }

        /// <summary>Loads the answer key only when grading — the test screen never holds it before submission.</summary>
        public static IeltsKey LoadKey(Package package)
        {
            string path = Path.Combine(package.Folder, "key.json");
            if (!File.Exists(path)) return null;
            var key = JsonUtility.FromJson<IeltsKey>(File.ReadAllText(path));
            return key != null && key.testId == package.Id ? key : null;
        }

        public static string AudioPath(Package package, IeltsPart part) =>
            string.IsNullOrEmpty(part?.audio) ? null : Path.GetFullPath(Path.Combine(package.Folder, part.audio));

        /// <summary>
        /// Loads an audio file as a seekable clip. OGG Vorbis stays compressed in memory (recommended for packages:
        /// a 9-minute part is ~4 MB); formats the audio engine cannot keep compressed (MP3) fall back to a decoded
        /// clip. Returns null when the file is missing or unsupported.
        /// </summary>
        public static IEnumerator LoadAudio(string path, Action<AudioClip> done)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { done(null); yield break; }
            AudioType type = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".mp3" => AudioType.MPEG,
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                _ => AudioType.UNKNOWN,
            };
            AudioClip clip = null;
            foreach (bool compressed in type == AudioType.OGGVORBIS ? new[] { true, false } : new[] { false })
            {
                using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type);
                if (request.downloadHandler is DownloadHandlerAudioClip handler) { handler.compressed = compressed; handler.streamAudio = false; }
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) continue;
                try { clip = DownloadHandlerAudioClip.GetContent(request); } catch (Exception) { clip = null; }
                if (clip != null && clip.length > 0f) break;
            }
            if (clip == null) Debug.LogWarning($"[IeltsLibrary] Audio could not be loaded: {Path.GetFileName(path)}");
            else clip.name = Path.GetFileNameWithoutExtension(path);
            done(clip);
        }
    }

    /// <summary>Saves the attempt in progress (answers, part, audio positions, timer) and the score history.</summary>
    public static class IeltsAttemptStore
    {
        private static string Folder => Path.Combine(Application.persistentDataPath, "ielts");
        private static string AttemptPath(string testId) => Path.Combine(Folder, testId + ".attempt.json");
        private static string HistoryPath => Path.Combine(Folder, "history.json");

        public static IeltsAttempt LoadAttempt(string testId)
        {
            string path = AttemptPath(testId);
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<IeltsAttempt>(File.ReadAllText(path)); }
            catch { return null; }
        }

        public static void SaveAttempt(IeltsAttempt attempt)
        {
            if (attempt == null || string.IsNullOrEmpty(attempt.testId)) return;
            Directory.CreateDirectory(Folder);
            string path = AttemptPath(attempt.testId);
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(attempt));
            if (File.Exists(path)) File.Delete(path);
            File.Move(path + ".tmp", path);
        }

        public static void ClearAttempt(string testId)
        {
            string path = AttemptPath(testId);
            if (File.Exists(path)) File.Delete(path);
        }

        public static IeltsHistory LoadHistory()
        {
            try { return File.Exists(HistoryPath) ? JsonUtility.FromJson<IeltsHistory>(File.ReadAllText(HistoryPath)) ?? new IeltsHistory() : new IeltsHistory(); }
            catch { return new IeltsHistory(); }
        }

        public static void AddHistory(IeltsHistoryEntry entry)
        {
            var history = LoadHistory();
            history.entries.Add(entry);
            Directory.CreateDirectory(Folder);
            File.WriteAllText(HistoryPath, JsonUtility.ToJson(history));
        }
    }
}
