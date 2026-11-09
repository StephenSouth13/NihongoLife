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
                if (lesson.IsLocal) continue; // the game's own lessons: checked below
                Assert.AreEqual(11, lesson.id.Length, lesson.title);
                Assert.IsNotEmpty(lesson.title, lesson.id);
            }
            var studio = file.courses.First(c => c.id == "studio");
            Assert.AreEqual(file.courses[0], studio, "the game's own course comes first");
            Assert.GreaterOrEqual(studio.LessonCount, 15);
            foreach (var lesson in studio.Lessons)
            {
                Assert.IsTrue(lesson.IsLocal, lesson.id);
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, lesson.file)), lesson.file);
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, lesson.thumb)), lesson.thumb);
                Assert.Greater(lesson.seconds, 20, lesson.id);
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

        /// <summary>The game's own lesson plays inside the game (no browser, no YouTube): the picture moves and the sound
        /// track is there. Captures: Bao_Cao/video-courses/03_studio_*.png.</summary>
        [UnityTest, Timeout(90000)]
        public IEnumerator StudioLesson_PlaysInsideTheGame()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            var hub = Object.FindFirstObjectByType<StudyHubPopup>(FindObjectsInactive.Include);
            hub.OpenVideos();
            yield return null;
            var library = Object.FindFirstObjectByType<VideoCoursesUI>(FindObjectsInactive.Include);
            var studio = VideoCourseCatalog.File.courses.First(c => c.id == "studio");
            library.OpenCourse(studio);
            library.SelectLesson(studio.Lessons.First());
            yield return new WaitForSecondsRealtime(0.5f);
            Capture(library, "03_studio_course");
            Assert.IsFalse(library.transform.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b => b.name == "OnYouTube" && b.gameObject.activeInHierarchy), "not a YouTube lesson");
            library.Play();
            var video = library.LessonVideo;
            Assert.NotNull(video, "the lesson plays in the game");
            for (float t = 0f; t < 10f && !(video.isPlaying && video.time > 6.0); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(video.isPlaying, "playing");
            Assert.Greater(video.time, 6.0, "the picture moves on");
            Assert.Greater(video.length, 20.0);
            Assert.GreaterOrEqual(video.audioTrackCount, 1, "with its voice track");
            Assert.NotNull(video.targetTexture);
            Capture(library, "04_studio_playing");
            library.TogglePause();
            yield return null;
            Assert.IsFalse(video.isPlaying, "a tap pauses");
            library.Hide();
            Assert.IsFalse(video.isPlaying, "closing stops it");
        }
    }
}
