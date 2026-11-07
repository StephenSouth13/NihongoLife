using System.Linq;
using NihongoLife.Core;
using UnityEngine;
namespace NihongoLife.UI
{
    /// <summary>Supplementary uploads of this learning language stay reachable from School even without a School unit ID.</summary>
    public sealed class CourseVideoSeriesView : CourseTabView
    {
        public CourseVideoSeriesView(CoursePopup host):base(host){}
        protected override void Draw()
        {
            var list=UiKit.Scroll(Area,"Series",out var _,12f,new RectOffset(4,4,4,4));UiKit.Fill((RectTransform)list.parent.parent);
            UiKit.Place(UiKit.Label(list,"Notice","Các bài video đã đăng, theo chủ đề. Chọn đúng bài rồi phát trong game hoặc mở YouTube.","Uploaded classroom video series. Pick the right lesson, then play in game or open YouTube.","公開済みレッスン動画。選んでゲーム内再生、またはYouTubeで開きます。",18f,UiTheme.Text).rectTransform,0f,0f,1100f,62f);
            int i=0;
            foreach(var link in ClassroomVideoIndex.Collection(Host.Target)){var v=link;string title=v.Title;
                var b=UiKit.Button(list,"Series_"+i+"_"+v.videoId,title,()=>LessonVideoUi.OpenLink(v),UiKit.Kind.Secondary,"smart_display",15f);UiKit.Place((RectTransform)b.transform,8f,78f+58f*i,1100f,50f);i++;
            }
            if(i==0)UiKit.Place(UiKit.Label(list,"Empty","Video của các bài học sẽ hiện tại đúng bài khi có đường dẫn.","Uploaded videos appear in their lesson when a link is available.","リンク済みの動画がレッスンに表示されます。",16f,UiTheme.Muted).rectTransform,8f,90f,920f,46f);
        }
    }
}
