using System.IO;
using UnityEngine;
using NihongoLife.Data;

namespace NihongoLife.Save
{
    public class LocalProgressRepository : IProgressRepository
    {
        private PlayerProgressDto _cachedProgress;
        private string SavePath => Path.Combine(Application.persistentDataPath, "progress.json");

        public void Initialize()
        {
            LoadFromDisk();
            Debug.Log($"[LocalProgressRepository] Initialized. Save path: {SavePath}");
        }

        public PlayerProgressDto GetProgress()
        {
            if (_cachedProgress == null)
            {
                LoadFromDisk();
            }
            return _cachedProgress;
        }

        public void SaveProgress(PlayerProgressDto progress)
        {
            _cachedProgress = progress;
            SaveToDisk();
        }

        public void ResetProgress()
        {
            _cachedProgress = new PlayerProgressDto();
            SaveToDisk();
            Debug.Log("[LocalProgressRepository] Progress reset to default.");
        }

        private void LoadFromDisk()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    _cachedProgress = JsonUtility.FromJson<PlayerProgressDto>(json);
                    Debug.Log("[LocalProgressRepository] Loaded progress from disk.");
                }
                else
                {
                    Debug.Log("[LocalProgressRepository] No save file found, creating new progress.");
                    _cachedProgress = new PlayerProgressDto();
                    SaveToDisk();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalProgressRepository] Error loading progress: {ex.Message}. Resetting to default.");
                _cachedProgress = new PlayerProgressDto();
            }
        }

        private void SaveToDisk()
        {
            try
            {
                string json = JsonUtility.ToJson(_cachedProgress, true);
                File.WriteAllText(SavePath, json);
                Debug.Log("[LocalProgressRepository] Saved progress to disk.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalProgressRepository] Error saving progress: {ex.Message}");
            }
        }
    }
}
