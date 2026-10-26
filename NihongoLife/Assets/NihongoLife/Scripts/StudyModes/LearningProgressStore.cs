using System;
using System.Collections.Generic;
using System.IO;
using NihongoLife.Save;
using UnityEngine;

namespace NihongoLife.StudyModes
{
    [Serializable]
    public sealed class LearningProgressEntry
    {
        public string id;
        public int bestCorrect;
        public int total;
        public int stars;
        public int attempts;
        public string lastPlayedUtc;
    }

    [Serializable]
    public sealed class LearningProgressData
    {
        public int version = 1;
        public List<LearningProgressEntry> entries = new List<LearningProgressEntry>();
        public List<string> watchedVideos = new List<string>();
    }

    /// <summary>
    /// Progress for the extra study modes (文型 patterns, English Basic lessons, watched videos). Kept in its own
    /// file (learning_progress.json) so it can never corrupt the main save, written with the same atomic
    /// .tmp → File.Replace(.bak) routine as <see cref="LocalProgressRepository"/> and recovered from .bak.
    /// </summary>
    public sealed class LearningProgressStore
    {
        private static LearningProgressStore _shared;
        public static LearningProgressStore Shared => _shared ??= new LearningProgressStore(Path.Combine(Application.persistentDataPath, "learning_progress.json"));

        /// <summary>Tests: point the shared store at a temp file (null restores the default).</summary>
        public static void OverrideShared(LearningProgressStore store) => _shared = store;

        public string FilePath { get; }
        private LearningProgressData _data;

        public LearningProgressStore(string filePath) { FilePath = filePath; }

        public LearningProgressData Data => _data ??= Load();

        private LearningProgressData Load()
        {
            var data = TryRead(FilePath) ?? TryRead(FilePath + ".bak") ?? new LearningProgressData();
            data.entries ??= new List<LearningProgressEntry>();
            data.watchedVideos ??= new List<string>();
            return data;
        }

        private static LearningProgressData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<LearningProgressData>(json);
            }
            catch (Exception) { return null; }
        }

        public LearningProgressEntry Get(string id)
        {
            foreach (var e in Data.entries) if (e.id == id) return e;
            return null;
        }

        public int StarsFor(string id) => Get(id)?.stars ?? 0;

        /// <summary>Records a finished attempt; keeps the best score. Returns true when this was a new best.</summary>
        public bool RecordResult(string id, int correct, int total)
        {
            if (string.IsNullOrEmpty(id) || total <= 0) return false;
            correct = Mathf.Clamp(correct, 0, total);
            var e = Get(id);
            if (e == null) { e = new LearningProgressEntry { id = id }; Data.entries.Add(e); }
            e.attempts++;
            e.lastPlayedUtc = DateTime.UtcNow.ToString("o");
            bool better = correct > e.bestCorrect || e.total == 0;
            if (better) { e.bestCorrect = correct; e.total = total; e.stars = PatternDrills.Stars(correct, total); }
            Save();
            return better;
        }

        public void MarkVideoWatched(string videoId)
        {
            if (string.IsNullOrEmpty(videoId) || Data.watchedVideos.Contains(videoId)) return;
            Data.watchedVideos.Add(videoId);
            Save();
        }

        public bool IsVideoWatched(string videoId) => Data.watchedVideos.Contains(videoId);

        public int TotalStars(IEnumerable<string> ids)
        {
            int sum = 0;
            foreach (var id in ids) sum += StarsFor(id);
            return sum;
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                LocalProgressRepository.WriteAtomic(FilePath, JsonUtility.ToJson(Data, true));
                LocalProgressRepository.FlushWebStorage();
            }
            catch (Exception e) { Debug.LogWarning($"[LearningProgressStore] Could not save: {e.Message}"); }
        }

        /// <summary>Tests / sign-out: drop the in-memory copy so the next read comes from disk.</summary>
        public void Reload() => _data = null;
    }
}
