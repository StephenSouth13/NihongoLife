using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.StudyModes;
using NihongoLife.StudyModes.Video;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>The course library opens from the Study Hub, filters by language, and keeps progress lesson by lesson.</summary>
    public class VideoCoursesPlayModeTests
    {
        [Test]
        public void Catalog_HoldsTheChannelsCourses()
        {
            var file = VideoCourseCatalog.File;
            Assert.GreaterOrEqual(file.courses.Length, 4);
            var russian = file.courses.First(c => c.id == "russian");
            Assert.GreaterOrEqual(russian.LessonCount, 80, "The Russian playlist");
            Assert.Greater(russian.TotalSeconds, 3600);
            foreach (var lesson in file.courses.SelectMany(c => c.Lessons))
            {
                Assert.AreEqual(11, lesson.id.Length, lesson.title);
                Assert.IsNotEmpty(lesson.title, lesson.id);
            }
        }

        private static void Capture(MenuPopupBase popup, string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/video-courses"));
            Directory.CreateDirectory(folder);
            var camera = Camera.main;
            var canvas = popup.GetComponentInParent<Canvas>().rootCanvas;
            var mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.06f;
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1600, 900, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            canvas.renderMode = mode;
            Object.Destroy(rt);
            Object.Destroy(tex);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator Library_FiltersOpensACourseAndKeepsProgress()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            var hub = Object.FindFirstObjectByType<StudyHubPopup>(FindObjectsInactive.Include);
            Assert.NotNull(hub);
            hub.OpenVideos();
            yield return null;
            var library = Object.FindFirstObjectByType<VideoCoursesUI>(FindObjectsInactive.Include);
            Assert.NotNull(library, "The Study Hub opens the library");
            Assert.IsTrue(library.IsOpen);
            yield return null;
            Assert.IsTrue(library.GetComponentsInChildren<TMP_Text>(false).Any(t => t.text.Contains("Tiếng Nga")), "The Russian course is listed");
            yield return new WaitForSecondsRealtime(2.5f); // the covers come from the network
            Capture(library, "01_catalogue");

            var course = VideoCourseCatalog.File.courses.First(c => c.id == "russian");
            library.OpenCourse(course);
            yield return null;
            Assert.AreEqual(course, library.Course);
            Assert.NotNull(library.Lesson);
            yield return new WaitForSecondsRealtime(1.5f);
            Capture(library, "02_course_page");

            var first = library.Lesson;
            int before = course.Watched(LearningProgressStore.Shared);
            library.MarkDone();
            yield return null;
            Assert.IsTrue(LearningProgressStore.Shared.IsVideoWatched(first.id));
            Assert.AreEqual(before + 1, course.Watched(LearningProgressStore.Shared), "Progress counts the lesson");
            library.NextLesson();
            yield return null;
            Assert.AreNotEqual(first, library.Lesson, "The next lesson is on the page");
            library.BackToCatalogue();
            yield return null;
            Assert.IsNull(library.Course);
            library.Hide();
        }
    }
}
