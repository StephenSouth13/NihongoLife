using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.StudyModes.Video;
using UnityEngine;

namespace NihongoLife.UI
{
    [Serializable]
    public sealed class LessonVideoLocalizedTitle
    {
        public string vi, en, ja, ru;

        public string Resolve() => Loc.IsVietnamese ? vi : Loc.IsEnglish ? en : Loc.IsJapanese ? ja ?? en : ru ?? en ?? vi;
    }

    /// <summary>One uploaded YouTube video tied to the exact School unit, alphabet lesson, certificate or Russian path step it teaches.</summary>
    [Serializable]
    public sealed class LessonVideoBinding
    {
        public string target, kind, id, videoId, youtubeUrl, stepId, source;
        public LessonVideoLocalizedTitle title;

        public string Title => title?.Resolve() ?? id;
    }

    [Serializable]
    public sealed class LessonVideoIndexFile
    {
        public string schema;
        public LessonVideoBinding[] links;
    }

    /// <summary>Resources/Content/Video/lesson_video_index.json (built by Tools/classroom/build_lesson_videos.py): only real YouTube links are kept.</summary>
    public static class ClassroomVideoIndex
    {
        private static List<LessonVideoBinding> _links;

        private static List<LessonVideoBinding> Links
        {
            get
            {
                if (_links != null) return _links;
                _links = new List<LessonVideoBinding>();
                var text = Resources.Load<TextAsset>("Content/Video/lesson_video_index");
                if (text == null) return _links;
                var file = JsonUtility.FromJson<LessonVideoIndexFile>(text.text);
                foreach (var v in file?.links ?? Array.Empty<LessonVideoBinding>())
                    if (VideoCatalog.IsYouTube(v?.youtubeUrl)) _links.Add(v);
                return _links;
            }
        }

        public static IReadOnlyList<LessonVideoBinding> For(string target, string kind, string id) =>
            Links.Where(v => v.target == target && v.kind == kind && v.id == id).ToList();

        /// <summary>The videos of a Russian path step, each video once.</summary>
        public static IReadOnlyList<LessonVideoBinding> ForPath(string stepId) =>
            Links.Where(v => v.stepId == stepId).GroupBy(v => v.videoId).Select(g => g.First()).ToList();

        public static IReadOnlyList<LessonVideoBinding> Collection(string target) =>
            Links.Where(v => v.target == target && v.kind == "collection").ToList();
    }
}
