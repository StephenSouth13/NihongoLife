using System;
using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.Progression;
using NihongoLife.StudyModes.Russian;
using NihongoLife.StudyModes.Video;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace NihongoLife.Tests
{
    /// <summary>Not just a URL count: the actual classroom School surface exposes each mapped video and hands the exact ID to its player.</summary>
    public class ClassroomVideoPlayModeTests
    {
        [UnityTest]
        public IEnumerator Classroom_School_VideoLinks_AreReachable()
        {
            var ui=ReferenceUi.Open<StudyHubPopup>();ui.Open();yield return null;
            ui.OpenSchool();var school=ui.GetComponent<CoursePopup>();
            Assert.NotNull(school,"Classroom Study hub must open the defined School component.");
            Assert.IsTrue(school.IsOpen);Assert.AreEqual("ru",school.Target,"Russian path keeps its selected Russian content, not default Japanese.");
            int tests=0;
            foreach(var target in new[]{"ru","en","ja","vi"}){
                school.ChooseTarget(target);
                foreach(var u in SchoolCatalog.Units(target)){
                    var v=ClassroomVideoIndex.For(target,"unit",u.Id);if(v.Count==0)continue;
                    school.OpenUnit(u.Id);yield return null;yield return Check(school,v,$"{target}/unit/{u.Id}");tests++;
                }
                foreach(var id in SchoolCatalog.AlphabetIds(target)){
                    var a=SchoolCatalog.Alphabet(id);school.OpenAlphabet(id);
                    foreach(var lesson in a.Lessons){
                        var v=ClassroomVideoIndex.For(target,"alphabet",lesson.Id);if(v.Count==0)continue;
                        school.AlphabetView.SelectLesson(lesson.Id);yield return null;yield return Check(school,v,$"{target}/alphabet/{lesson.Id}");tests++;
                    }
                }
                foreach(var id in SchoolCatalog.CertificateIds(target)){
                    var v=ClassroomVideoIndex.For(target,"certificate",id);if(v.Count==0)continue;
                    school.OpenCertificate(id);yield return null;yield return Check(school,v,$"{target}/certificate/{id}");tests++;
                }
            }
            var rus=RussianCourse.Path.Steps.First(s=>s.kind=="unit");ui.OpenRussianPath();
            var path=ui.GetComponent<RussianPathPopup>();path.SelectStep(rus.id);path.OpenSchool();yield return null;
            yield return Check(school,ClassroomVideoIndex.ForPath(rus.id),rus.id);tests++;
            var alpha=RussianCourse.Path.Steps.First(s=>s.id=="ru_L_4a");path.SelectStep(alpha.id);path.OpenSchool();yield return null;
            Assert.AreEqual("ru_ar_consonants_2",school.AlphabetView.SelectedLessonId,"Split Cyrillic video group selects the exact alphabet lesson.");
            yield return Check(school,ClassroomVideoIndex.ForPath(alpha.id),alpha.id);tests++;
            var cert=RussianCourse.Path.Steps.First(s=>s.kind=="cert");path.SelectStep(cert.id);path.OpenSchool();yield return null;
            Assert.AreEqual("cert_"+cert.@ref,school.CertificatesView.SelectedId);tests++;
            // Multiple sections in Cyrillic 4 and TORFL stacks must retain all parts, not just the first URL.
            Assert.Greater(ClassroomVideoIndex.For("ru","certificate","cert_torfl_a1").Count,1);
            school.ChooseTarget("en");school.ShowTab(CoursePopup.Tab.VideoSeries);yield return null;
            Assert.GreaterOrEqual(ClassroomVideoIndex.Collection("en").Count,34);yield return null;
            Assert.NotNull(school.ActiveView.GetType());
            Assert.Greater(tests,45);Debug.Log($"[ClassroomVideo] verified {tests} classroom lesson surfaces / all linked video parts");
            GameObject.Find("ReferenceCanvas");
            var video=ReferenceUi.Open<VideoCoursesUI>();video.Hide();
        }
        private static IEnumerator Check(CoursePopup school, System.Collections.Generic.IReadOnlyList<LessonVideoBinding> expected,string label)
        {
            Assert.Greater(expected.Count,0,label);var oldLang=Loc.Language;
            var button=school.ActiveView.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="LessonVideos");
            Assert.NotNull(button,"Missing visible video button inside selected lesson: "+label);
            button.onClick.Invoke();var popup=Object.FindFirstObjectByType<VideoCoursesUI>();
            Assert.NotNull(popup,label);Assert.IsTrue(popup.IsOpen,label);
            Assert.AreEqual(NihongoLife.StudyModes.Video.VideoCatalog.ExtractYouTubeId(expected[0].youtubeUrl),popup.Lesson.id,label+" exact selection");
            var external=popup.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="OpenYouTubeCompact");Assert.NotNull(external,label+" compact YouTube");
            foreach(var v in expected){popup.SelectLessonVideo(v);Assert.NotNull(popup.SelectedBinding,label+"/"+v.videoId);Assert.AreEqual(v.videoId,popup.Lesson.id);
                Assert.IsTrue(VideoCatalog.IsYouTube(v.youtubeUrl));Assert.IsNotEmpty(v.Title);}
            popup.Hide();yield return null;
            Loc.Set(oldLang);
        }
    }
}
