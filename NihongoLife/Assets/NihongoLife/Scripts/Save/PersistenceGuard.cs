using System;
using NihongoLife.Core;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.Save
{
    /// <summary>
    /// Keeps the player's progress safe offline. Saves on a timer and the moment the page/app is hidden or closed
    /// (browser visibilitychange / pagehide / beforeunload, app pause, focus loss, quit), and coalesces the slow IndexedDB
    /// sync (WebGL) so a burst of milestone saves costs one flush, not one each.
    /// </summary>
    public sealed class PersistenceGuard : MonoBehaviour
    {
        public const string ObjectName = "NLPersistence";
        public static float AutosaveSeconds = 45f;
        public static float FlushDelaySeconds = 1.5f;

        private static PersistenceGuard _instance;
        public static int SaveCount { get; private set; }
        public static int FlushCount { get; private set; }
        public static string LastReason { get; private set; } = "";
        public static bool AutosaveEnabled = true;

        private float _nextAutosave;
        private bool _flushPending;
        private float _flushAt;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void NL_RegisterPersistHooks(string gameObjectName);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void NL_SyncPersistentData();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; SaveCount = 0; FlushCount = 0; LastReason = ""; AutosaveEnabled = true; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot() => Ensure();

        public static PersistenceGuard Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject(ObjectName);
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PersistenceGuard>();
            _instance._nextAutosave = Time.unscaledTime + AutosaveSeconds;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { NL_RegisterPersistHooks(ObjectName); } catch (Exception e) { Debug.LogWarning("[Persistence] hooks: " + e.Message); }
#endif
            return _instance;
        }

        /// <summary>Writes the live game state into the progress and saves it now (then schedules the web flush).</summary>
        public static void SaveNow(string reason)
        {
            LastReason = reason ?? "";
            try
            {
                var status = SceneRefs.Find<PlayerStatus>();
                if (status != null) { status.SaveNow(); SaveCount++; }
            }
            catch (Exception e) { Debug.LogWarning("[Persistence] save failed: " + e.Message); }
            if (Ensure() != null) _instance.FlushNowOrSoon(true);
        }

        /// <summary>A save was written into the browser's file system: sync it to IndexedDB soon (coalesced).</summary>
        public static void RequestFlush() { if (Ensure() != null) _instance.FlushNowOrSoon(false); }

        private void FlushNowOrSoon(bool now)
        {
            if (now) { DoFlush(); return; }
            if (!_flushPending) { _flushPending = true; _flushAt = Time.unscaledTime + FlushDelaySeconds; }
        }

        private void DoFlush()
        {
            _flushPending = false;
            FlushCount++;
#if UNITY_WEBGL && !UNITY_EDITOR
            NL_SyncPersistentData();
#endif
        }

        private void Update()
        {
            if (_flushPending && Time.unscaledTime >= _flushAt) DoFlush();
            if (AutosaveEnabled && Time.unscaledTime >= _nextAutosave)
            {
                _nextAutosave = Time.unscaledTime + AutosaveSeconds;
                SaveNow("autosave");
            }
        }

        // Called from the browser (NihongoLifeWeb.jslib) when the tab is hidden or about to close.
        public void OnBrowserHide(string reason) => SaveNow("browser-" + reason);
        private void OnApplicationPause(bool paused) { if (paused) SaveNow("pause"); }
        private void OnApplicationFocus(bool focused) { if (!focused) SaveNow("focus-lost"); }
        private void OnApplicationQuit() => SaveNow("quit");
    }
}