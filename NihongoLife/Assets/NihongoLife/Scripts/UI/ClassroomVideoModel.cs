using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.StudyModes.Video;
using UnityEngine;
namespace NihongoLife.UI
{
    [Serializable] public sealed class LessonVideoLocalizedTitle { public string vi,en,ja,ru; public string Resolve() => Loc.IsVietnamese?vi:Loc.IsEnglish?en:Loc.IsJapanese?(ja??en):ru??en??vi; }
    [Serializable] public sealed class LessonVideoBinding
    {
        public string target,kind,id,videoId,youtubeUrl,stepId,source;
        public LessonVideoLocalizedTitle title;
        public string Title => title?.Resolve() ?? id;
    }
    [Serializable] public sealed class LessonVideoIndexFile { public string schema; public LessonVideoBinding[] links; }
    public static class ClassroomVideoIndex
    {
        private static List<LessonVideoBinding> _links;
        public static IReadOnlyList<LessonVideoBinding> All {get{Load();return _links;}}
        private static void Load(){if(_links!=null)return;_links=new List<LessonVideoBinding>();var text=Resources.Load<TextAsset>("Content/Video/lesson_video_index");if(text==null)return;
            var file=JsonUtility.FromJson<LessonVideoIndexFile>(text.text);foreach(var v in file?.links??Array.Empty<LessonVideoBinding>())if(VideoCatalog.IsYouTube(v?.youtubeUrl))_links.Add(v);}
        public static IReadOnlyList<LessonVideoBinding> For(string target,string kind,string id){Load();return _links.Where(v=>v.target==target&&v.kind==kind&&v.id==id).ToList();}
        public static IReadOnlyList<LessonVideoBinding> ForPath(string stepId){Load();return _links.Where(v=>v.stepId==stepId).GroupBy(v=>v.videoId).Select(v=>v.First()).ToList();}
        public static IReadOnlyList<LessonVideoBinding> Collection(string target){Load();return _links.Where(v=>v.target==target&&v.kind=="collection").ToList();}
        public static IReadOnlyList<LessonVideoBinding> Ielts(string id){return For("en","collection","ielts_course_"+id);}
        public static IReadOnlyList<LessonVideoBinding> Patterns(){Load();return _links.Where(v=>v.target=="ja"&&v.kind=="grammar").ToList();}
        public static void ResetCache()=>_links=null;
    }
}
