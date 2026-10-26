using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.English;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    public sealed class EnglishCoursePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/english-course"));

        [UnityTest]
        public IEnumerator CenterAndLesson_RenderGeneralEnglishRoute()
        {
            Directory.CreateDirectory(Folder);
            Loc.Set(GameLanguage.Vietnamese);
            var canvas = NLUi.CreateCanvas("EnglishCourseTestCanvas", 500);
            var center = canvas.gameObject.AddComponent<ExamCenterPopup>();
            center.Initialize(UiKit.Font);
            center.Show();
            typeof(ExamCenterPopup).GetMethod("SelectEnglish", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(center, null);
            yield return null;

            var centerTexts = center.GetComponentsInChildren<TextMeshProUGUI>(false).Select(x => x.text).ToArray();
            Assert.IsTrue(centerTexts.Any(x => x.Contains("Tiếng Anh từ cơ bản")), "English hero is visible");
            Assert.IsTrue(centerTexts.Any(x => x.Contains("Giao tiếp hằng ngày")), "A1 lesson is visible");
            Assert.IsTrue(centerTexts.Any(x => x.Contains("Diễn đạt học thuật")), "C1 lesson is visible");
            yield return Capture("01_english_course_catalog");

            center.Hide();
            var lesson = EnglishCourseCatalog.Find("en_a1_patterns");
            var practice = EnglishPracticeUI.GetOrCreate(canvas.transform);
            practice.Begin(lesson);
            yield return null;
            Assert.IsTrue(practice.IsOpen);
            var buttons = practice.GetComponentsInChildren<Button>(false);
            var answer = buttons.FirstOrDefault(x => x.name == "Choice_0");
            Assert.NotNull(answer, "Answer choices are rendered");
            answer.onClick.Invoke();
            yield return null;
            Assert.IsTrue(practice.GetComponentsInChildren<TextMeshProUGUI>(false).Any(x => x.text.Contains("Chính xác")), "Immediate explanation is rendered");
            yield return Capture("02_a1_sentence_pattern_feedback");
        }

        private static IEnumerator Capture(string name)
        {
            string path = Path.Combine(Folder, name + ".png");
            var camera = Camera.main;
            GameObject cameraObject = null;
            if (camera == null)
            {
                cameraObject = new GameObject("EnglishCaptureCamera", typeof(Camera));
                camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            }
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.gameObject.activeInHierarchy).ToArray();
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f; }
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            RenderTexture.active = old;
            camera.targetTexture = null;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            target.Release();
            Object.Destroy(target);
            Object.Destroy(texture);
            if (cameraObject != null) Object.Destroy(cameraObject);
            Assert.IsTrue(File.Exists(path) && new FileInfo(path).Length > 0, path);
        }
    }
}
