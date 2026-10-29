using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Content;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>(A) Companion pictures: shown in the quiz feedback and unlocked in the dictionary only after a correct answer. Captures: Bao_Cao/illustrations/.</summary>
    public class IllustrationPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/illustrations"));
        private GameObject _camera;

        [SetUp] public void SetUp() { Directory.CreateDirectory(Out); IllustrationService.ResetForTests(); LearningPath.Clear(); Loc.Set(GameLanguage.Vietnamese); }
        [TearDown] public void TearDown() { IllustrationService.ResetForTests(); LearningPath.Clear(); Loc.Set(GameLanguage.Vietnamese); if (HandbookUI.IsOpen) HandbookUI.Close(); if (_camera != null) Object.Destroy(_camera); }

        private IEnumerator Capture(string name)
        {
            if (_camera == null)
            {
                _camera = new GameObject("IlluCaptureCamera", typeof(Camera));
                var c0 = _camera.GetComponent<Camera>(); c0.clearFlags = CameraClearFlags.SolidColor; c0.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            }
            var camera = _camera.GetComponent<Camera>();
            const int width = 1920, height = 1080;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active; RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            string path = Path.Combine(Out, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = old; camera.targetTexture = null;
            target.Release(); Object.Destroy(target); Object.Destroy(tex);
            foreach (var c in canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Assert.IsTrue(File.Exists(path) && new FileInfo(path).Length > 1000, path);
        }

        [Test]
        public void Service_FindsWordsInEveryLanguage_AndUnlocksOnce()
        {
            Assert.GreaterOrEqual(IllustrationService.Count, 600, "the manifest lists the pictures");
            Assert.AreEqual("w00001", IllustrationService.Find("mother"));
            Assert.AreEqual("w00001", IllustrationService.Find("\u6bcd"));
            Assert.AreEqual("w00001", IllustrationService.Find("the mother"));
            Assert.IsNull(IllustrationService.Find("qzxqzx"));
            Assert.IsNotNull(IllustrationService.GetSprite("w00001"), "the PNG loads");
            Assert.IsFalse(IllustrationService.IsUnlocked("w00001"));
            Assert.IsTrue(IllustrationService.Unlock("w00001"));
            Assert.IsFalse(IllustrationService.Unlock("w00001"), "second unlock is a no-op");
            Assert.IsTrue(IllustrationService.IsUnlocked("w00001"));
            Assert.AreEqual("w00001", IllustrationService.UnlockFor("nonsense", "mother"));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Quiz_CorrectAnswer_ShowsAndUnlocks_Picture_InDictionary()
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "desktop 16:9");
#endif
            yield return null; yield return null;
            var canvas = NLUi.CreateCanvas("IlluTestCanvas", 500);
            var popup = CoursePopup.GetOrCreate(canvas.gameObject, UiKit.Font);
            popup.Show(); yield return null;
            int shown = 0, asked = 0; bool captured = false; string firstId = null, firstEn = null;
            foreach (var target in new[] { "ja", "en" })
            {
                popup.ChooseTarget(target);
                popup.ShowTab(CoursePopup.Tab.Lessons); yield return null;
                foreach (var unit in SchoolCatalog.Units(target).Take(6))
                {
                    popup.LessonsView.Select(unit.Id); popup.LessonsView.StartPractice(); yield return null;
                    var quiz = popup.ActiveView.Quiz; if (quiz == null) continue;
                    while (!quiz.Finished)
                    {
                        quiz.AnswerForTest(true); asked++;
                        if (quiz.LastIllustration != null)
                        {
                            shown++;
                            if (firstId == null) firstId = quiz.LastIllustration;
                            if (!captured) { captured = true; yield return Capture("01_quiz_feedback_unlocked_" + target); }
                        }
                        quiz.Next(); if (asked % 6 == 0) yield return null;
                    }
                    popup.ActiveView.HandleBack(); yield return null;
                    if (shown >= 3) break;
                }
                if (shown >= 3) break;
            }
            File.WriteAllText(Path.Combine(Out, "quiz_matches.txt"), $"asked={asked} withPicture={shown} unlocked={IllustrationService.UnlockedCount} first={firstId}\n");
            popup.Hide();
            Assert.Greater(shown, 0, "at least one correct answer unlocked a picture (asked " + asked + ")");
            Assert.IsTrue(IllustrationService.IsUnlocked(firstId));

            // dictionary: an unlocked word shows its picture, a locked one a silhouette.
            HandbookUI.DictionarySearch("mother", "en", "vi"); yield return null; yield return null;
            var ids = HandbookUI.EntryIds.Where(i => i.StartsWith("h")).ToList();
            Assert.IsNotEmpty(ids);
            HandbookUI.Open(HandbookUI.Chapter.Dictionary, ids[0]); yield return null; yield return null;
            var hb = GameObject.Find("HandbookCanvas");
            Assert.NotNull(hb);
            bool locked = hb.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Silhouette");
            bool open = hb.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Picture");
            Assert.IsTrue(locked ^ open, "the hit shows exactly one of picture/silhouette (locked=" + locked + " open=" + open + ")");
            yield return Capture(locked ? "02_dictionary_locked_silhouette" : "02_dictionary_unlocked_picture");
            Assert.IsTrue(IllustrationService.Unlock("w00001") || IllustrationService.IsUnlocked("w00001"));
            HandbookUI.Open(HandbookUI.Chapter.Dictionary, ids[0]); yield return null; yield return null;
            Assert.IsTrue(GameObject.Find("HandbookCanvas").GetComponentsInChildren<Transform>(true).Any(t => t.name == "Picture"), "unlocked mother shows its picture");
            yield return Capture("03_dictionary_unlocked_picture");
            HandbookUI.Close();
        }
    }
}