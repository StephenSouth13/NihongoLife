using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.StudyModes.Video
{
    [Serializable]
    public sealed class VideoLesson
    {
        public string id, title;
        public int seconds;
        /// <summary>A lesson made for the game (NihongoLife Studio): its mp4 and thumbnail under StreamingAssets. It plays inside
        /// the game on every platform; the channel's lessons (no file) are YouTube videos.</summary>
        public string file, thumb;

        public bool IsLocal => !string.IsNullOrEmpty(file);
        public string Url => "https://www.youtube.com/watch?v=" + id;
        public string Length => seconds >= 3600 ? $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}" : $"{seconds / 60}:{seconds % 60:00}";
    }

    [Serializable]
    public sealed class VideoCourseSection
    {
        public string title, titleEn, titleJa;
        public VideoLesson[] lessons = Array.Empty<VideoLesson>();

        public string Title => Loc.T(title, string.IsNullOrEmpty(titleEn) ? title : titleEn, string.IsNullOrEmpty(titleJa) ? title : titleJa);
    }

    [Serializable]
    public sealed class VideoCourse
    {
        public string id, lang, color;
        public string titleVi, titleEn, titleJa, blurbVi, blurbEn, blurbJa;
        public VideoCourseSection[] sections = Array.Empty<VideoCourseSection>();

        public string Title => Loc.T(titleVi, titleEn, titleJa);
        public string Blurb => Loc.T(blurbVi, blurbEn, blurbJa);
        public Color Tint => ColorUtility.TryParseHtmlString(color, out var c) ? c : Color.gray;
        public IEnumerable<VideoLesson> Lessons => sections.SelectMany(s => s.lessons);
        public int LessonCount => Lessons.Count();
        public int TotalSeconds => Lessons.Sum(l => l.seconds);
        public int Watched(LearningProgressStore store) => Lessons.Count(l => store.IsVideoWatched(l.id));
        public VideoLesson NextLesson(LearningProgressStore store) => Lessons.FirstOrDefault(l => !store.IsVideoWatched(l.id)) ?? Lessons.FirstOrDefault();
    }

    [Serializable]
    public sealed class VideoCourseFile
    {
        public string channelUrl;
        public VideoCourse[] courses = Array.Empty<VideoCourse>();
    }

    /// <summary>Courses built from the channel's playlists (Tools/video/build_courses.py → Resources/Video/video_courses.json),
    /// after the game's own lessons that play in the game (Tools/video/studio_lessons.py → Resources/Video/studio_courses.json).</summary>
    public static class VideoCourseCatalog
    {
        public const string Path = "Video/video_courses";
        public const string StudioPath = "Video/studio_courses";
        private static VideoCourseFile _file;

        public static VideoCourseFile File
        {
            get
            {
                if (_file != null) return _file;
                var asset = Resources.Load<TextAsset>(Path);
                _file = asset != null ? JsonUtility.FromJson<VideoCourseFile>(asset.text) ?? new VideoCourseFile() : new VideoCourseFile();
                var studio = Resources.Load<TextAsset>(StudioPath);
                var own = studio != null ? JsonUtility.FromJson<VideoCourseFile>(studio.text) : null;
                if (own != null && own.courses.Length > 0) _file.courses = own.courses.Concat(_file.courses).ToArray();
                return _file;
            }
        }

        public static int LessonCount => File.courses.Sum(c => c.LessonCount);
        public static int Watched(LearningProgressStore store) => File.courses.Sum(c => c.Watched(store));
    }
}
