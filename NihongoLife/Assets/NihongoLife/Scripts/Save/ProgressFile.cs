using System;
using System.IO;
using NihongoLife.Data;
using UnityEngine;

namespace NihongoLife.Save
{
    /// <summary>
    /// The versioned on-disk form of the player's progress (guest and account saves alike). Every write is atomic with a
    /// previous-save backup (path.bak); a read that fails falls back to the backup and keeps a copy of the broken file.
    /// <c>saveVersion</c> lets later builds migrate old saves: fields a save does not have simply keep their defaults.
    /// </summary>
    public static class ProgressFile
    {
        /// <summary>1 = before versioning, 2 = adds saveVersion/savedAtUtc (this change).</summary>
        public const int CurrentVersion = 2;

        public static string Serialize(PlayerProgressDto progress, bool pretty = false)
        {
            progress.saveVersion = CurrentVersion;
            progress.savedAtUtc = DateTime.UtcNow.ToString("o");
            return JsonUtility.ToJson(progress, pretty);
        }

        /// <summary>Atomic write: tmp file, swap in, previous save kept as .bak.</summary>
        public static void Write(string path, PlayerProgressDto progress, bool pretty = false)
        {
            if (progress == null) return;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            LocalProgressRepository.WriteAtomic(path, Serialize(progress, pretty));
        }

        /// <summary>The save, else its backup, else null (a broken main file is kept next to it as .corrupt-*).</summary>
        public static PlayerProgressDto Read(string path, out bool fromBackup)
        {
            fromBackup = false;
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return null;
            var progress = LocalProgressRepository.ReadWithBackup(path, out fromBackup);
            if (progress == null || fromBackup) LocalProgressRepository.PreserveCorrupt(path);
            if (progress != null) Migrate(progress);
            return progress;
        }

        /// <summary>Brings an older save up to <see cref="CurrentVersion"/> (null lists from hand-edited saves are repaired).</summary>
        public static void Migrate(PlayerProgressDto p)
        {
            if (p == null) return;
            p.completedScenarios ??= new System.Collections.Generic.List<string>();
            p.storyFlags ??= new System.Collections.Generic.List<string>();
            p.inventory ??= new System.Collections.Generic.List<NihongoLife.Player.InventoryEntry>();
            p.homeStorage ??= new System.Collections.Generic.List<NihongoLife.Player.InventoryEntry>();
            p.island ??= new NihongoLife.Island.IslandRecord();
            p.quests ??= new System.Collections.Generic.List<NihongoLife.Progression.QuestStateRecord>();
            p.business ??= new BusinessRecord();
            if (p.saveVersion < 2) p.saveVersion = 2;
        }
    }
}