using System.Collections.Generic;
using System.Linq;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using NihongoLife.Core;
using NihongoLife.StudyModes;
using NihongoLife.StudyModes.Video;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The video library as a course site: a catalogue of courses filtered by language, then a course page with the player, the
    /// lesson's actions and the curriculum by section, with progress kept per lesson. In the browser a lesson plays right there
    /// (an embedded YouTube frame over the player area); a desktop build opens it in the browser, because Unity cannot play
    /// YouTube pages.
    /// </summary>
    public sealed class VideoCoursesUI : MenuPopupBase
    {
        private const float W = 1420f, H = 860f, CardW = 430f, CardH = 324f, PlayerW = 900f, PlayerH = 506f;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void NL_VideoShow(string videoId, float x, float y, float width, float height);
        [DllImport("__Internal")] private static extern void NL_VideoHide();
#else
        private static void NL_VideoShow(string videoId, float x, float y, float width, float height) { }
        private static void NL_VideoHide() { }
#endif

        private RectTransform _body, _player;
        private string _language = "all";

        public VideoCourse Course { get; private set; }
        public VideoLesson Lesson { get; private set; }
        public bool Playing { get; private set; }

        private static LearningProgressStore Store => LearningProgressStore.Shared;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);
        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Khóa học video"; en = "Video courses"; ja = "ビデオコース"; }

        protected override void Build(RectTransform card)
        {
            _body = UiKit.Rect(card, "Body");
            UiKit.Place(_body, 32f, 96f, W - 64f, H - 124f);
        }

        protected override void OnOpened()
        {
            Course = null;
            Rebuild();
        }

        protected override void OnClosed() => StopPlayback();
        protected override void OnLanguageApplied() { if (IsOpen) Rebuild(); }

        private void Rebuild()
        {
            StopPlayback();
            UiKit.Clear(_body);
            if (Course == null) BuildCatalogue();
            else BuildCoursePage();
        }

        private void StopPlayback()
        {
            Playing = false;
            NL_VideoHide();
        }

        // ─────────── Catalogue ───────────

        private void BuildCatalogue()
        {
            var filters = new[] { ("all", Loc.T("Tất cả", "All", "すべて")), ("ru", Loc.T("Tiếng Nga", "Russian", "ロシア語")), ("en", Loc.T("Tiếng Anh", "English", "英語")), ("ja", Loc.T("Tiếng Nhật", "Japanese", "日本語")) };
            float x = 0f;
            foreach (var (id, label) in filters)
            {
                string code = id;
                var chip = UiKit.Button(_body, "Filter_" + id, label, () => { _language = code; Rebuild(); }, _language == id ? UiKit.Kind.Primary : UiKit.Kind.Quiet, null, 16f);
                UiKit.Place((RectTransform)chip.transform, x, 0f, 150f, 46f);
                x += 160f;
            }
            var stats = UiKit.Label(_body, "Stats", Loc.F("{0} bài học · đã xem {1}", "{0} lessons · {1} watched", "{0}レッスン・視聴済み {1}", VideoCourseCatalog.LessonCount, VideoCourseCatalog.Watched(Store)), 16f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            UiKit.Place(stats.rectTransform, W - 464f, 8f, 400f, 30f);

            var shown = VideoCourseCatalog.File.courses.Where(c => _language == "all" || c.lang == _language || c.lang == "all").ToList();
            for (int i = 0; i < shown.Count; i++) CourseCard(shown[i], (i % 3) * (CardW + 24f), 62f + (i / 3) * (CardH + 16f));
        }

        private void CourseCard(VideoCourse course, float x, float y)
        {
            var card = UiKit.Card(_body, "Course_" + course.id, UiTheme.Surface, 16, 0.25f);
            UiKit.Place(card.rectTransform, x, y, CardW, CardH);
            var cover = UiKit.Image(card.transform, "Cover", course.Tint, UiKit.Rounded(14));
            UiKit.Place(cover.rectTransform, 0f, 0f, CardW, 162f);
            var picture = new GameObject("Picture", typeof(RectTransform)).AddComponent<RawImage>();
            picture.transform.SetParent(cover.transform, false);
            picture.color = new Color(1f, 1f, 1f, 0f);
            UiKit.Fill(picture.rectTransform);
            picture.raycastTarget = false;
            picture.uvRect = new Rect(0f, 0.17f, 1f, 0.66f); // the wide cover shows the middle of the 16:9 picture
            StartCoroutine(VideoThumbnails.Load(course.Lessons.FirstOrDefault()?.id, picture));

            UiKit.Place(UiKit.Label(card.transform, "Title", course.Title, 21f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft), 18f, 170f, CardW - 36f, 30f);
            UiKit.Place(UiKit.Label(card.transform, "Blurb", course.Blurb, 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true), 18f, 200f, CardW - 36f, 40f);
            int watched = course.Watched(Store), total = course.LessonCount;
            UiKit.Place(UiKit.Label(card.transform, "Meta", Loc.F("{0} bài · {1}", "{0} lessons · {1}", "{0}レッスン・{1}", total, Hours(course.TotalSeconds)), 14f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.TopLeft), 18f, 244f, CardW - 180f, 22f);
            var bar = UiKit.ProgressBar(card.transform, "Progress", course.Tint, new Color(1f, 1f, 1f, 0.1f));
            UiKit.Place((RectTransform)bar.parent, 18f, 272f, CardW - 180f, 10f);
            UiKit.SetProgress(bar, total > 0 ? watched / (float)total : 0f);
            UiKit.Place(UiKit.Label(card.transform, "Percent", Loc.F("Đã học {0}/{1}", "{0}/{1} done", "{0}/{1} 完了", watched, total), 13f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft), 18f, 288f, CardW - 180f, 20f);
            var go = UiKit.Button(card.transform, "Open", watched > 0 ? "Học tiếp" : "Bắt đầu", watched > 0 ? "Continue" : "Start", watched > 0 ? "続きから" : "はじめる", () => OpenCourse(course), UiKit.Kind.Primary, "play_arrow", 15f);
            UiKit.Place((RectTransform)go.transform, CardW - 150f, 258f, 132f, 48f);
        }

        private static string Hours(int seconds)
        {
            int minutes = seconds / 60;
            return minutes >= 60 ? Loc.F("{0} giờ {1} phút", "{0} h {1} min", "{0}時間{1}分", minutes / 60, minutes % 60) : Loc.F("{0} phút", "{0} min", "{0}分", minutes);
        }

        // ─────────── Course page ───────────

        public void OpenCourse(VideoCourse course)
        {
            Course = course;
            Lesson = course.NextLesson(Store);
            Rebuild();
        }

        public void BackToCatalogue()
        {
            Course = null;
            Rebuild();
        }

        private void BuildCoursePage()
        {
            var back = UiKit.Button(_body, "Back", "Tất cả khóa học", "All courses", "コース一覧", BackToCatalogue, UiKit.Kind.Quiet, "arrow_back", 15f);
            UiKit.Place((RectTransform)back.transform, 0f, 0f, 210f, 44f);
            UiKit.Place(UiKit.Label(_body, "CourseTitle", Course.Title, 24f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft), 226f, 0f, PlayerW - 226f, 44f);

            var stage = UiKit.Image(_body, "Player", Course.Tint, UiKit.Rounded(12), true);
            _player = stage.rectTransform;
            UiKit.Place(_player, 0f, 56f, PlayerW, PlayerH);
            var picture = new GameObject("Picture", typeof(RectTransform)).AddComponent<RawImage>();
            picture.transform.SetParent(_player, false);
            picture.color = new Color(1f, 1f, 1f, 0f);
            UiKit.Fill(picture.rectTransform);
            picture.raycastTarget = false;
            StartCoroutine(VideoThumbnails.Load(Lesson?.id, picture));
            var play = UiKit.Button(_player, "Play", "Phát bài học", "Play lesson", "再生する", Play, UiKit.Kind.Primary, "play_arrow", 20f);
            UiKit.Pin((RectTransform)play.transform, new Vector2(0.5f, 0.5f), 0f, 0f, 260f, 72f);

            UiKit.Place(UiKit.Label(_body, "LessonTitle", Lesson?.title ?? "", 19f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft, true), 0f, 572f, PlayerW, 56f);
            bool done = Lesson != null && Store.IsVideoWatched(Lesson.id);
            var finished = UiKit.Button(_body, "Done", done ? "Đã học xong" : "Đánh dấu đã học", done ? "Completed" : "Mark as done", done ? "完了済み" : "完了にする", MarkDone, done ? UiKit.Kind.Quiet : UiKit.Kind.Secondary, "check_circle", 15f);
            UiKit.Place((RectTransform)finished.transform, 0f, 638f, 240f, 48f);
            var next = UiKit.Button(_body, "Next", "Bài tiếp theo", "Next lesson", "次のレッスン", NextLesson, UiKit.Kind.Secondary, "chevron_right", 15f);
            UiKit.Place((RectTransform)next.transform, 252f, 638f, 200f, 48f);
            var site = UiKit.Button(_body, "OnYouTube", "Mở trên YouTube", "Open on YouTube", "YouTubeで開く", OpenOnYouTube, UiKit.Kind.Quiet, "public", 15f);
            UiKit.Place((RectTransform)site.transform, 464f, 638f, 220f, 48f);

            var side = UiKit.Rect(_body, "Curriculum");
            UiKit.Place(side, PlayerW + 24f, 0f, W - 64f - PlayerW - 24f, H - 124f);
            var list = UiKit.Scroll(side, "Scroll", out _, 6f, new RectOffset(2, 12, 2, 12));
            UiKit.Fill((RectTransform)side.GetChild(0));
            foreach (var section in Course.sections) SectionRows(list, section);
        }

        private void SectionRows(RectTransform list, VideoCourseSection section)
        {
            int done = section.lessons.Count(l => Store.IsVideoWatched(l.id));
            var head = UiKit.Card(list, "Section", UiTheme.Raised, 10, 0f, false);
            head.gameObject.Need<LayoutElement>().preferredHeight = 52f;
            UiKit.Place(UiKit.Label(head.transform, "Title", section.Title, 16f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft), 14f, 0f, 300f, 52f);
            UiKit.Place(UiKit.Label(head.transform, "Count", $"{done}/{section.lessons.Length}", 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineRight), 310f, 0f, 62f, 52f);
            foreach (var lesson in section.lessons)
            {
                var item = lesson;
                bool current = Lesson == lesson;
                var row = UiKit.Card(list, "Lesson_" + lesson.id, current ? UiKit.Alpha(Course.Tint, 0.35f) : UiTheme.Surface, 8, 0f);
                row.gameObject.Need<LayoutElement>().preferredHeight = 58f;
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                button.onClick.AddListener(() => SelectLesson(item));
                var check = UiKit.Icon(row.transform, Store.IsVideoWatched(lesson.id) ? "check_circle" : "play_circle", Store.IsVideoWatched(lesson.id) ? UiTheme.Good : UiTheme.TextSoft, 22f);
                UiKit.Place(check.rectTransform, 12f, 18f, 22f, 22f);
                var title = UiKit.Label(row.transform, "Title", lesson.title, 13.5f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true);
                title.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Place(title.rectTransform, 44f, 4f, 270f, 50f);
                UiKit.Place(UiKit.Label(row.transform, "Length", lesson.Length, 12.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineRight), 318f, 0f, 54f, 58f);
            }
        }

        public void SelectLesson(VideoLesson lesson)
        {
            Lesson = lesson;
            Rebuild();
        }

        public void NextLesson()
        {
            var all = Course.Lessons.ToList();
            int i = all.IndexOf(Lesson);
            if (i >= 0 && i + 1 < all.Count) SelectLesson(all[i + 1]);
        }

        public void MarkDone()
        {
            if (Lesson == null) return;
            Store.MarkVideoWatched(Lesson.id);
            Rebuild();
        }

        /// <summary>Plays the lesson: over the player area in the browser, in the browser itself on a desktop build.</summary>
        public void Play()
        {
            if (Lesson == null) return;
            Store.MarkVideoWatched(Lesson.id);
#if UNITY_WEBGL && !UNITY_EDITOR
            var corners = new Vector3[4];
            _player.GetWorldCorners(corners);
            var canvas = _player.GetComponentInParent<Canvas>().rootCanvas;
            var view = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 low = RectTransformUtility.WorldToScreenPoint(view, corners[0]), high = RectTransformUtility.WorldToScreenPoint(view, corners[2]);
            NL_VideoShow(Lesson.id, low.x / Screen.width, 1f - high.y / Screen.height, (high.x - low.x) / Screen.width, (high.y - low.y) / Screen.height);
            Playing = true;
#else
            OpenOnYouTube();
#endif
        }

        public void OpenOnYouTube()
        {
            if (Lesson != null) Application.OpenURL(Lesson.Url);
        }
    }
}
