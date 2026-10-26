using System;
using System.Collections.Generic;

namespace NihongoLife.StudyModes
{
    /// <summary>
    /// Plug-in point for study modes shown in the Study Hub. A mode (e.g. Codex's English Basic course) registers
    /// a launcher once; the hub shows its tile and calls it. Unregistered built-in ids show a "coming soon" state.
    /// </summary>
    public static class StudyModeRegistry
    {
        public const string EnglishBasic = "english-basic";
        public const string JapanesePatterns = "ja-patterns";
        public const string VideoLibrary = "video-library";

        private static readonly Dictionary<string, Action> Launchers = new Dictionary<string, Action>();

        public static void Register(string id, Action launch)
        {
            if (string.IsNullOrEmpty(id) || launch == null) return;
            Launchers[id] = launch;
        }

        public static void Unregister(string id) { if (id != null) Launchers.Remove(id); }
        public static bool IsAvailable(string id) => id != null && Launchers.ContainsKey(id);

        /// <summary>Runs the launcher; false (never throws) when the mode is missing or failed to open.</summary>
        public static bool TryLaunch(string id)
        {
            if (id == null || !Launchers.TryGetValue(id, out var launch)) return false;
            try { launch(); return true; }
            catch (Exception e) { UnityEngine.Debug.LogException(e); return false; }
        }
    }
}
