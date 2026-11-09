using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>The School's "Video series" tab: uploaded videos of the learning language that belong to no single School unit.</summary>
    public sealed class CourseVideoSeriesView : CourseTabView
    {
        public CourseVideoSeriesView(CoursePopup host) : base(host) { }

        protected override void Draw()
        {
            var list = UiKit.Scroll(Area, "Series", out var _, 12f, new RectOffset(4, 4, 4, 4));
            UiKit.Fill((RectTransform)list.parent.parent);
            var notice = UiKit.Label(list, "Notice",
                "Các bài video đã đăng, theo chủ đề. Chọn đúng bài rồi phát trong game hoặc mở YouTube.",
                "Uploaded classroom video series. Pick the right lesson, then play in game or open YouTube.",
                "公開済みレッスン動画。選んでゲーム内再生、またはYouTubeで開きます。", 18f, UiTheme.Text);
            UiKit.Place(notice.rectTransform, 0f, 0f, 1100f, 62f);
            int i = 0;
            foreach (var link in ClassroomVideoIndex.Collection(Host.Target))
            {
                var video = link;
                var button = UiKit.Button(list, "Series_" + i + "_" + video.videoId, video.Title, () => LessonVideoUi.OpenLink(video), UiKit.Kind.Secondary, "smart_display", 15f);
                UiKit.Place((RectTransform)button.transform, 8f, 78f + 58f * i, 1100f, 50f);
                i++;
            }
            if (i > 0) return;
            var empty = UiKit.Label(list, "Empty", "Video của các bài học sẽ hiện tại đúng bài khi có đường dẫn.",
                "Uploaded videos appear in their lesson when a link is available.", "リンク済みの動画がレッスンに表示されます。", 16f, UiTheme.Muted);
            UiKit.Place(empty.rectTransform, 8f, 90f, 920f, 46f);
        }
    }
}
