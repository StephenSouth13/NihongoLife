using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NihongoLife.StudyModes.Video
{
    /// <summary>
    /// One in-game video. <see cref="youtubeUrl"/> points at our channel (@NihongoLifeGame).
    /// <see cref="playableUrl"/> is a direct mp4/HLS URL (CDN/R2) the Unity VideoPlayer can stream, or a path relative
    /// to StreamingAssets ("Videos/x.mp4"). When it is empty the player shows the YouTube card (thumbnail, title,
    /// transcript, "open on YouTube").
    /// </summary>
    [Serializable]
    public sealed class VideoEntry
    {
        public string id;
        public string module;          // "jp-life", "jlpt", "ielts", "english-basic", "ja-patterns", "kana", ...
        public string youtubeUrl;
        public string playableUrl;
        public string titleVi, titleEn, titleJa;
        public string summaryVi, summaryEn, summaryJa;
        public string transcript;
        public string attribution;    // required for CC-BY third-party clips
        public string license;        // "own", "CC0", "CC-BY-4.0", ...
        public bool shorts;
        public int durationSeconds;
    }

    [Serializable]
    public sealed class VideoCatalogData
    {
        public int version = 1;
        public string channelUrl;
        public List<VideoEntry> videos = new List<VideoEntry>();
    }

    public enum VideoSourceKind { None, LocalFile, DirectUrl, YouTubeOnly }

    public static class VideoCatalog
    {
        public const string ResourcePath = "Video/nihongolife_videos";
        private static VideoCatalogData _loaded;

        private static readonly Regex YouTubeId = new Regex(
            @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|shorts/|embed/|live/)|youtu\.be/)([A-Za-z0-9_-]{11})(?![A-Za-z0-9_-])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>The data-driven list (Resources/Video/nihongolife_videos.json). Never null.</summary>
        public static VideoCatalogData Data
        {
            get
            {
                if (_loaded != null) return _loaded;
                var asset = Resources.Load<TextAsset>(ResourcePath);
                _loaded = Parse(asset != null ? asset.text : null);
                return _loaded;
            }
        }

        public static IReadOnlyList<VideoEntry> All => Data.videos;

        public static VideoEntry Find(string id)
        {
            foreach (var v in All) if (v.id == id) return v;
            return null;
        }

        /// <summary>Parses catalog JSON; invalid JSON or entries without id/title/source are dropped, never throws.</summary>
        public static VideoCatalogData Parse(string json)
        {
            var result = new VideoCatalogData();
            if (string.IsNullOrWhiteSpace(json)) return result;
            VideoCatalogData raw;
            try { raw = JsonUtility.FromJson<VideoCatalogData>(json); }
            catch (Exception) { return result; }
            if (raw == null) return result;
            result.version = raw.version;
            result.channelUrl = raw.channelUrl;
            var seen = new HashSet<string>();
            foreach (var v in raw.videos ?? new List<VideoEntry>())
            {
                if (v == null || string.IsNullOrWhiteSpace(v.id) || !seen.Add(v.id)) continue;
                if (string.IsNullOrWhiteSpace(v.titleVi) && string.IsNullOrWhiteSpace(v.titleEn)) continue;
                if (Classify(v) == VideoSourceKind.None) continue;
                result.videos.Add(v);
            }
            return result;
        }

        public static string ExtractYouTubeId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var m = YouTubeId.Match(url.Trim());
            return m.Success ? m.Groups[1].Value : null;
        }

        public static bool IsYouTube(string url) => ExtractYouTubeId(url) != null;

        public static string ThumbnailUrl(VideoEntry v)
        {
            string id = ExtractYouTubeId(v?.youtubeUrl);
            return id == null ? null : $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";
        }

        /// <summary>What the VideoPlayer can actually play. YouTube pages are not playable by Unity's VideoPlayer.</summary>
        public static VideoSourceKind Classify(VideoEntry v)
        {
            if (v == null) return VideoSourceKind.None;
            string p = v.playableUrl?.Trim();
            if (!string.IsNullOrEmpty(p) && !IsYouTube(p))
            {
                if (p.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || p.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return VideoSourceKind.DirectUrl;
                if (!p.Contains("://") && !p.Contains("..")) return VideoSourceKind.LocalFile;
            }
            return IsYouTube(v.youtubeUrl) || IsYouTube(p) ? VideoSourceKind.YouTubeOnly : VideoSourceKind.None;
        }

        /// <summary>URL for VideoPlayer.url: StreamingAssets path for local files (works on WebGL too).</summary>
        public static string ResolvePlayableUrl(VideoEntry v, string streamingAssetsPath)
        {
            switch (Classify(v))
            {
                case VideoSourceKind.DirectUrl: return v.playableUrl.Trim();
                case VideoSourceKind.LocalFile:
                    string root = (streamingAssetsPath ?? "").TrimEnd('/', '\\');
                    string rel = v.playableUrl.Trim().TrimStart('/', '\\').Replace('\\', '/');
                    return root + "/" + rel;
                default: return null;
            }
        }

        public static string Title(VideoEntry v) => v == null ? "" :
            NihongoLife.Core.Loc.T(First(v.titleVi, v.titleEn), First(v.titleEn, v.titleVi), First(v.titleJa, v.titleEn, v.titleVi));
        public static string Summary(VideoEntry v) => v == null ? "" :
            NihongoLife.Core.Loc.T(First(v.summaryVi, v.summaryEn), First(v.summaryEn, v.summaryVi), First(v.summaryJa, v.summaryEn, v.summaryVi));

        private static string First(params string[] values)
        {
            foreach (var s in values) if (!string.IsNullOrWhiteSpace(s)) return s;
            return "";
        }

        /// <summary>Tests only.</summary>
        public static void ResetCache() => _loaded = null;
    }
}
