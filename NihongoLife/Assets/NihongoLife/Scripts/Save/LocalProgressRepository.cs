using System.IO;
using UnityEngine;
using NihongoLife.Data;
using NihongoLife.Core;

namespace NihongoLife.Save
{
    public class LocalProgressRepository : IProgressRepository
    {
        private PlayerProgressDto _cachedProgress;
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void NL_SyncPersistentData(); // Plugins/WebGL/NihongoLifeWeb.jslib
#endif
        /// <summary>WebGL: files in persistentDataPath live in IndexedDB; flush so they survive a page reload. No-op elsewhere.</summary>
        public static void FlushWebStorage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NL_SyncPersistentData();
#endif
        }

        /// <summary>
        /// One save per account (progress_&lt;account&gt;.json): on a shared machine (a school lab) a second account must
        /// never start from — and merge into the cloud — the previous account's save. The legacy progress.json is no
        /// longer read; the cloud copy is each account's source of truth.
        /// </summary>
        private string SavePath => SavePathFor(_cacheAccount);

        public static string SavePathFor(string account)
        {
            if (string.IsNullOrEmpty(account)) return Path.Combine(Application.persistentDataPath, "progress.json");
            var safe = new System.Text.StringBuilder(account.Length);
            foreach (char c in account) safe.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            return Path.Combine(Application.persistentDataPath, "progress_" + safe + ".json");
        }

        /// <summary>The account the cached progress belongs to; a different signed-in account reloads its own save.</summary>
        private string _cacheAccount = string.Empty;

        public void Initialize()
        {
            LoadFromDisk();
            Debug.Log($"[LocalProgressRepository] Initialized. Save path: {SavePath}");
        }

        public PlayerProgressDto GetProgress()
        {
            PlayerSessionService session = PlayerSessionService.GetOrCreate();
            if (session == null) return _cachedProgress ??= new PlayerProgressDto();
            if (!session.CanPersist)
            {
                return session.GuestProgress;
            }
            if (_cachedProgress == null || _cacheAccount != session.AccountId)
            {
                LoadFromDisk();
            }
            return _cachedProgress;
        }

        public void SaveProgress(PlayerProgressDto progress)
        {
            PlayerSessionService session = PlayerSessionService.GetOrCreate();
            if (session == null) return;
            if (!session.CanPersist)
            {
                session.UpdateGuestProgress(progress);
                return;
            }
            if (_cacheAccount != session.AccountId) _cacheAccount = session.AccountId;
            _cachedProgress = progress;
            SaveToDisk();
        }

        public void ResetProgress()
        {
            PlayerSessionService session = PlayerSessionService.GetOrCreate();
            if (session == null) return;
            if (!session.CanPersist)
            {
                session.BeginGuest();
                return;
            }
            _cachedProgress = new PlayerProgressDto();
            SaveToDisk();
            Debug.Log("[LocalProgressRepository] Progress reset to default.");
        }

        private void LoadFromDisk()
        {
            PlayerSessionService session = PlayerSessionService.GetOrCreate();
            if (session == null)
            {
                _cachedProgress ??= new PlayerProgressDto();
                return;
            }
            if (!session.CanPersist)
            {
                _cachedProgress = session.GuestProgress;
                return;
            }
            _cacheAccount = session.AccountId;
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
                FlushWebStorage();
                Debug.Log("[LocalProgressRepository] Saved progress to disk.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalProgressRepository] Error saving progress: {ex.Message}");
            }
        }
    }
}
