using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;

namespace NihongoLife.Editor
{
    /// <summary>
    /// Keeps music and story voices out of the web build's first download (they were ~65 MB of ~117 MB): while the
    /// WebGL player is built, those Resources folders are parked outside Resources (AssetDatabase moves — GUIDs and
    /// references are kept) and put back right after; then every clip is written next to the page as MP3
    /// (StreamingAssets/Audio/…, with index.json) for Audio/WebAudioStore to fetch the first time it is needed.
    /// An interrupted build is undone the next time the editor loads. Called by ReleaseBuild.BuildWebGL only.
    /// </summary>
    public static class WebAudioOnDemand
    {
        private const string Parking = "Assets/NihongoLife/WebOnDemand";
        /// <summary>(folder under Resources, its Resources path prefix)</summary>
        private static readonly (string folder, string prefix)[] Folders =
        {
            ("Assets/NihongoLife/Resources/Voice", "Voice"),
            ("Assets/NihongoLife/Resources/Audio/Music", "Audio/Music"),
        };

        private static string Parked(string prefix) => Parking + "/" + prefix.Replace('/', '_');

        public static void Park()
        {
            if (!AssetDatabase.IsValidFolder(Parking)) AssetDatabase.CreateFolder("Assets/NihongoLife", "WebOnDemand");
            foreach (var (folder, prefix) in Folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                string error = AssetDatabase.MoveAsset(folder, Parked(prefix));
                if (!string.IsNullOrEmpty(error)) throw new BuildFailedException("Could not park " + folder + ": " + error);
            }
            AssetDatabase.Refresh();
        }

        public static void Restore()
        {
            foreach (var (folder, prefix) in Folders)
            {
                string parked = Parked(prefix);
                if (!AssetDatabase.IsValidFolder(parked) || AssetDatabase.IsValidFolder(folder)) continue;
                string error = AssetDatabase.MoveAsset(parked, folder);
                if (!string.IsNullOrEmpty(error)) Debug.LogError("[WebAudio] Could not restore " + folder + ": " + error);
            }
            if (AssetDatabase.IsValidFolder(Parking) && !Directory.EnumerateFileSystemEntries(Path.GetFullPath(Parking)).Any(p => !p.EndsWith(".meta")))
                AssetDatabase.DeleteAsset(Parking);
            AssetDatabase.Refresh();
        }

        [InitializeOnLoadMethod]
        private static void RestoreInterruptedBuild()
        {
            if (WebGLZoneBundleBuilder.ReleaseBuildInProgress || !Directory.Exists(Path.GetFullPath(Parking))) return;
            EditorApplication.delayCall += () => { Restore(); Debug.Log("[WebAudio] Put the music and voices back into Resources after an unfinished web build."); };
        }

        /// <summary>Writes the clips next to the built page (StreamingAssets/Audio) as MP3 with an index.</summary>
        public static void Publish(string playerOutput)
        {
            string root = Directory.Exists(playerOutput) ? playerOutput : Path.GetDirectoryName(playerOutput);
            string dest = Path.Combine(root, "StreamingAssets", "Audio");
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            Directory.CreateDirectory(dest);
            string ffmpeg = FindFfmpeg();
            var index = new StringBuilder("{\"files\":[");
            int count = 0;
            long bytes = 0;
            foreach (var (folder, prefix) in Folders)
            {
                string full = Path.GetFullPath(folder);
                if (!Directory.Exists(full)) continue;
                foreach (string source in Directory.GetFiles(full, "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(source).ToLowerInvariant();
                    if (ext != ".mp3" && ext != ".ogg" && ext != ".wav") continue;
                    string relative = source.Substring(full.Length + 1).Replace('\\', '/');
                    string resourcePath = prefix + "/" + relative.Substring(0, relative.Length - ext.Length);
                    string file = resourcePath + (ext == ".mp3" || ffmpeg != null ? ".mp3" : ext);
                    string target = Path.Combine(dest, file.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    if (ext == ".mp3") File.Copy(source, target, true);
                    else if (ffmpeg == null || !Transcode(ffmpeg, source, target, prefix.StartsWith("Voice") ? "64k" : "112k")) { file = resourcePath + ext; target = Path.Combine(dest, file.Replace('/', Path.DirectorySeparatorChar)); File.Copy(source, target, true); }
                    bytes += new FileInfo(target).Length;
                    if (count++ > 0) index.Append(',');
                    index.Append("{\"path\":\"").Append(resourcePath).Append("\",\"file\":\"").Append(file).Append("\"}");
                }
            }
            index.Append("]}");
            File.WriteAllText(Path.Combine(dest, "index.json"), index.ToString());
            Debug.Log($"[WebAudio] {count} clips ({bytes / (1024f * 1024f):F1} MB) next to the page in StreamingAssets/Audio.");
        }

        private static bool Transcode(string ffmpeg, string source, string target, string bitrate)
        {
            try
            {
                var start = new ProcessStartInfo(ffmpeg, $"-y -loglevel error -i \"{source}\" -codec:a libmp3lame -b:a {bitrate} \"{target}\"")
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
                };
                using (var process = Process.Start(start))
                {
                    process.WaitForExit(120000);
                    return process.ExitCode == 0 && File.Exists(target);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WebAudio] ffmpeg failed on " + source + ": " + e.Message);
                return false;
            }
        }

        private static string FindFfmpeg()
        {
            var candidates = new List<string> { Environment.GetEnvironmentVariable("NIHONGO_FFMPEG") };
            foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                if (!string.IsNullOrWhiteSpace(dir)) candidates.Add(Path.Combine(dir.Trim(), "ffmpeg.exe"));
            string winget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages");
            if (Directory.Exists(winget))
                candidates.AddRange(Directory.GetDirectories(winget, "Gyan.FFmpeg*").SelectMany(d => Directory.GetFiles(d, "ffmpeg.exe", SearchOption.AllDirectories)));
            return candidates.FirstOrDefault(c => !string.IsNullOrEmpty(c) && File.Exists(c));
        }
    }
}
