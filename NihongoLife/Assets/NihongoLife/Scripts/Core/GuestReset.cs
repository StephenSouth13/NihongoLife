using System.IO;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// Resets an offline guest on this device (for a shop / classroom / contest booth handing the game to the next
    /// player): deletes guest_progress.json (and its web/IndexedDB copy is flushed by the normal save path on next
    /// save), clears the guest's look/intro prefs. Never touches an account's progress_&lt;userId&gt;.json
    /// (AGENTS.md: each account keeps its own save). Callers: a "Chơi mới (khách)" button in the main menu with a
    /// confirm dialog — see Docs/Expansion/edu-v1/GUEST_RESET_HOOK.md.
    /// </summary>
    public static class GuestReset
    {
        public static readonly string[] GuestPrefKeys = { "NihongoLife.CharacterLook" };

        /// <summary>True when a guest save exists on this device.</summary>
        public static bool HasGuestSave => File.Exists(PlayerSessionService.GuestSavePath);

        /// <summary>Deletes the guest save. Returns false (and does nothing) while an account is signed in.</summary>
        public static bool ResetGuest()
        {
            var session = PlayerSessionService.Instance;
            if (session != null && session.Mode != PlayerSessionMode.Guest) return false;
            if (session != null) session.ClearGuestProgress(); // the in-memory copy too (existing API)
            string path = PlayerSessionService.GuestSavePath;
            if (File.Exists(path)) File.Delete(path);
            string bak = path + ".bak";
            if (File.Exists(bak)) File.Delete(bak);
            foreach (var k in GuestPrefKeys) PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
            Debug.Log("[GuestReset] Guest progress cleared.");
            return true;
        }
    }
}
