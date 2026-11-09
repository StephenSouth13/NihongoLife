using System.Collections.Generic;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>The one video button of a School page: it opens that lesson's videos in the classroom player.</summary>
    public static class LessonVideoUi
    {
        /// <summary>A full-width button in a page's body; returns the next free y.</summary>
        public static float AddStrip(Transform parent, string title, IReadOnlyList<LessonVideoBinding> videos, float y, float width)
        {
            if (videos == null || videos.Count == 0) return y;
            string label = Loc.F("VIDEO BÀI HỌC ({0})", "LESSON VIDEO ({0})", "レッスン動画（{0}）", videos.Count);
            var button = UiKit.Button(parent, "LessonVideos", label, () => Open(title, videos), UiKit.Kind.Secondary, "smart_display", 15f);
            UiKit.Place((RectTransform)button.transform, 0f, y, width, 48f);
            return y + 58f;
        }

        /// <summary>A small button in a page's header.</summary>
        public static void AddHeaderButton(Transform parent, string title, IReadOnlyList<LessonVideoBinding> videos, float x, float y)
        {
            if (videos == null || videos.Count == 0) return;
            string label = Loc.F("VIDEO ({0})", "VIDEO ({0})", "動画（{0}）", videos.Count);
            var button = UiKit.Button(parent, "LessonVideos", label, () => Open(title, videos), UiKit.Kind.Secondary, "smart_display", 14f);
            UiKit.Place((RectTransform)button.transform, x, y, 176f, 36f);
        }

        public static VideoCoursesUI Open(string title, IReadOnlyList<LessonVideoBinding> videos)
        {
            if (videos == null || videos.Count == 0) throw new System.ArgumentException("No links for " + title);
            var popup = ReferenceUi.Open<VideoCoursesUI>();
            popup.OpenLessonBindings(title, videos);
            return popup;
        }

        public static void OpenLink(LessonVideoBinding binding) => Open(binding?.Title, new[] { binding });
    }
}
