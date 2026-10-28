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
            return _cachedProgress ??= new PlayerProgressDto();
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
            if (progress == null) { Debug.LogWarning("[LocalProgressRepository] Ignored a null progress save."); return; }
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
            if (File.Exists(SavePath) || File.Exists(SavePath + ".bak"))
            {
                _cachedProgress = ReadWithBackup(SavePath, out bool fromBackup);
                if (_cachedProgress == null)
                {
                    // Neither the save nor its backup is readable: keep a copy of the broken file before starting over.
                    string kept = PreserveCorrupt(SavePath);
                    Debug.LogError($"[LocalProgressRepository] Save file could not be read; kept a copy at {kept} and started new progress.");
                    _cachedProgress = new PlayerProgressDto();
                }
                else if (fromBackup)
                {
                    string kept = PreserveCorrupt(SavePath);
                    Debug.LogWarning($"[LocalProgressRepository] Save file was unreadable; restored the previous save (.bak). Broken file kept at {kept}.");
                    SaveToDisk();
                }
                else Debug.Log("[LocalProgressRepository] Loaded progress from disk.");
            }
            else
            {
                Debug.Log("[LocalProgressRepository] No save file found, creating new progress.");
                _cachedProgress = new PlayerProgressDto();
                SaveToDisk();
            }
        }

        /// <summary>Reads the save, falling back to <c>path.bak</c> when the save is missing, empty or not valid JSON.
        /// Returns null only when neither can be read.</summary>
        public static PlayerProgressDto ReadWithBackup(string path, out bool fromBackup)
        {
            fromBackup = false;
            var main = TryRead(path);
            if (main != null) return main;
            var backup = TryRead(path + ".bak");
            fromBackup = backup != null;
            return backup;
        }

        private static PlayerProgressDto TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<PlayerProgressDto>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LocalProgressRepository] Could not read {Path.GetFileName(path)}: {ex.Message}");
                return null;
            }
        }

        /// <summary>Copies an unreadable save aside (never deleted) and returns where; null when there was nothing to keep.</summary>
        private static string PreserveCorrupt(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string kept = path + ".corrupt-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(path, kept, true);
                return kept;
            }
            catch (System.Exception ex) { Debug.LogWarning("[LocalProgressRepository] Could not keep the broken save: " + ex.Message); return null; }
        }

        /// <summary>Writes to <c>path.tmp</c> first, then swaps it in, keeping the previous save as <c>path.bak</c>,
        /// so a crash mid-write never leaves a half-written save.</summary>
        public static void WriteAtomic(string path, string json)
        {
            string tmp = path + ".tmp";
            string bak = path + ".bak";
            File.WriteAllText(tmp, json);
            if (!File.Exists(path)) { File.Move(tmp, path); return; }
            try { File.Replace(tmp, path, bak); }
            catch (System.Exception) // platforms/file systems without File.Replace (e.g. WebGL IndexedDB)
            {
                File.Copy(path, bak, true);
                File.Copy(tmp, path, true);
                File.Delete(tmp);
            }
        }

        private void SaveToDisk()
        {
            try
            {
                string json = JsonUtility.ToJson(_cachedProgress ??= new PlayerProgressDto(), true);
                WriteAtomic(SavePath, json);
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
