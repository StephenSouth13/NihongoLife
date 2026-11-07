using System.Collections.Generic;
using NihongoLife.Core;
using UnityEngine;
namespace NihongoLife.UI
{
    /// <summary>Compact lesson controls connect School/path selections to the existing classroom video player.</summary>
    public static class LessonVideoUi
    {
        public static float AddStrip(Transform parent,string title,IReadOnlyList<LessonVideoBinding> videos,float y,float width)
        {
            if(videos==null||videos.Count==0)return y;
            string label=Loc.F("VIDEO BÀI HỌC ({0})","LESSON VIDEO ({0})","レッスン動画（{0}）",videos.Count);
            var b=UiKit.Button(parent,"LessonVideos",label,()=>Open(title,videos),UiKit.Kind.Secondary,"smart_display",15f);
            UiKit.Place((RectTransform)b.transform,0f,y,width,48f);return y+58f;
        }
        public static void AddHeaderButton(Transform parent,string title,IReadOnlyList<LessonVideoBinding> videos,float x,float y)
        {
            if(videos==null||videos.Count==0)return;
            var b=UiKit.Button(parent,"LessonVideos",Loc.F("VIDEO ({0})","VIDEO ({0})","動画（{0}）",videos.Count),()=>Open(title,videos),UiKit.Kind.Secondary,"smart_display",14f);
            UiKit.Place((RectTransform)b.transform,x,y,176f,42f);
        }
        public static VideoCoursesUI Open(string title,IReadOnlyList<LessonVideoBinding> videos)
        {
            if(videos==null||videos.Count==0)throw new System.ArgumentException("No links for "+title);
            var popup=ReferenceUi.Open<VideoCoursesUI>();popup.OpenLessonBindings(title,videos);return popup;
        }
        public static void OpenLink(LessonVideoBinding b)=>Open(b?.Title,new[]{b});
    }
}
