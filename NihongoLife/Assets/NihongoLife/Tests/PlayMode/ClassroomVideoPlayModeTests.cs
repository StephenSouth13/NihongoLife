using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes.Russian;
using NihongoLife.StudyModes.Video;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>Not just a URL count: the School pages opened from the classroom show each linked video, and its button hands the exact
    /// YouTube id to the player. The Russian path opens the exact unit, alphabet lesson and certificate.</summary>
    public class ClassroomVideoPlayModeTests
    {
        [UnityTest]
        public IEnumerator Classroom_School_VideoLinks_AreReachable()
        {
            var hub = ReferenceUi.Open<StudyHubPopup>();
            yield return null;
            hub.ChooseLanguage("ru");
            hub.OpenSchool("ru");
            var school = hub.GetComponent<CoursePopup>();
            Assert.NotNull(school, "The classroom opens the School");
            Assert.IsTrue(school.IsOpen);
            Assert.AreEqual("ru", school.Target, "The School opens on the language picked in the classroom");

            int surfaces = 0;
            foreach (var target in new[] { "ru", "en", "ja", "vi" })
            {
                school.ChooseTarget(target);
                foreach (var unit in SchoolCatalog.Units(target))
                {
                    var videos = ClassroomVideoIndex.For(target, "unit", unit.Id);
                    if (videos.Count == 0) continue;
                    school.OpenUnit(unit.Id);
                    yield return null;
                    yield return Check(school, videos, $"{target}/unit/{unit.Id}");
                    surfaces++;
                }
                foreach (var id in SchoolCatalog.AlphabetIds(target))
                {
                    school.OpenAlphabet(id);
                    foreach (var lesson in SchoolCatalog.Alphabet(id).Lessons)
                    {
                        var videos = ClassroomVideoIndex.For(target, "alphabet", lesson.Id);
                        if (videos.Count == 0) continue;
                        school.AlphabetView.SelectLesson(lesson.Id);
                        yield return null;
                        yield return Check(school, videos, $"{target}/alphabet/{lesson.Id}");
                        surfaces++;
                    }
                }
                foreach (var id in SchoolCatalog.CertificateIds(target))
                {
                    var videos = ClassroomVideoIndex.For(target, "certificate", id);
                    if (videos.Count == 0) continue;
                    school.OpenCertificate(id);
                    yield return null;
                    yield return Check(school, videos, $"{target}/certificate/{id}");
                    surfaces++;
                }
            }

            // From the Russian path: the exact unit, alphabet lesson and certificate (a certificate has no video yet and must not throw).
            hub.OpenRussianPath();
            var path = hub.GetComponent<RussianPathPopup>();
            var unitStep = RussianCourse.Path.Steps.First(s => s.kind == "unit");
            path.SelectStep(unitStep.id);
            path.OpenSchool();
            yield return null;
            yield return Check(school, ClassroomVideoIndex.ForPath(unitStep.id), unitStep.id);
            var alphabetStep = RussianCourse.Path.Steps.First(s => s.kind == "alphabet" && ClassroomVideoIndex.ForPath(s.id).Count > 0);
            path.SelectStep(alphabetStep.id);
            path.OpenSchool();
            yield return null;
            Assert.AreEqual(alphabetStep.@ref, school.AlphabetView.SelectedLessonId, "The path's alphabet step selects its exact lesson");
            yield return Check(school, ClassroomVideoIndex.ForPath(alphabetStep.id), alphabetStep.id);
            var certificateStep = RussianCourse.Path.Steps.First(s => s.kind == "cert");
            path.SelectStep(certificateStep.id);
            path.OpenSchool();
            yield return null;
            Assert.AreEqual(certificateStep.@ref, school.CertificatesView.SelectedId, "The path's certificate step opens that certificate");
            surfaces += 2;

            // Videos that belong to no single unit: the School's video series tab lists them.
            school.ChooseTarget("en");
            school.ShowTab(CoursePopup.Tab.VideoSeries);
            yield return null;
            Assert.GreaterOrEqual(ClassroomVideoIndex.Collection("en").Count, 34);
            Assert.IsTrue(school.GetComponentsInChildren<Button>(true).Any(b => b.name.StartsWith("Series_")), "The video series tab lists the uploads");

            Assert.GreaterOrEqual(surfaces, 40, "Every linked lesson, alphabet and path surface");
            Debug.Log($"[ClassroomVideo] verified {surfaces} classroom surfaces and every linked video part");
            ReferenceUi.Open<VideoCoursesUI>().Hide();
        }

        private static IEnumerator Check(CoursePopup school, IReadOnlyList<LessonVideoBinding> expected, string label)
        {
            Assert.Greater(expected.Count, 0, label);
            var button = school.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "LessonVideos");
            Assert.NotNull(button, "The open page shows its video button: " + label);
            button.onClick.Invoke();
            var popup = Object.FindFirstObjectByType<VideoCoursesUI>();
            Assert.NotNull(popup, label);
            Assert.IsTrue(popup.IsOpen, label);
            Assert.AreEqual(VideoCatalog.ExtractYouTubeId(expected[0].youtubeUrl), popup.Lesson.id, label + ": the exact video is selected");
            Assert.NotNull(popup.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "OpenYouTubeCompact"), label + ": compact YouTube button");
            foreach (var video in expected)
            {
                popup.SelectLessonVideo(video);
                Assert.AreEqual(video.videoId, popup.SelectedBinding?.videoId, label + "/" + video.videoId);
                Assert.AreEqual(video.videoId, popup.Lesson.id);
                Assert.IsTrue(VideoCatalog.IsYouTube(video.youtubeUrl));
                Assert.IsNotEmpty(video.Title);
            }
            popup.Hide();
            yield return null;
        }
    }
}
