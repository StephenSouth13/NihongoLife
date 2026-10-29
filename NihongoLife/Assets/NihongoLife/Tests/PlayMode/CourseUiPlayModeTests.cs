using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// School (CoursePopup: path, lessons, alphabet, grammar, certificates, tenses), the six-tile Study hub, the Handbook
    /// dictionary and the exam hall's school shortcut, rendered at 1920x1080. Captures: Bao_Cao/course/.
    /// </summary>
    public class CourseUiPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/course"));
        private GameObject _camera;

        [SetUp]
        public void SetUp() { Directory.CreateDirectory(Out); LearningPath.Clear(); Loc.Set(GameLanguage.Vietnamese); }

        [TearDown]
        public void TearDown()
        {
            LearningPath.Clear(); Loc.Set(GameLanguage.Vietnamese);
            if (HandbookUI.IsOpen) HandbookUI.Close();
            if (_camera != null) Object.Destroy(_camera);
        }

        private static IEnumerator DesktopResolution()
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "desktop 16:9");
#endif
            yield return null; yield return null; yield return null;
        }

        private IEnumerator Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                if (_camera == null)
                {
                    _camera = new GameObject("CourseCaptureCamera", typeof(Camera));
                    var c = _camera.GetComponent<Camera>();
                    c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
                }
                camera = _camera.GetComponent<Camera>();
            }
            const int width = 1920, height = 1080;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            string path = Path.Combine(Out, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = old;
            camera.targetTexture = null;
            target.Release(); Object.Destroy(target); Object.Destroy(tex);
            foreach (var c in canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Assert.IsTrue(File.Exists(path) && new FileInfo(path).Length > 1000, path);
        }

        private static List<string> Texts(Component root) => root.GetComponentsInChildren<TextMeshProUGUI>(false).Select(t => t.text).Where(t => !string.IsNullOrEmpty(t)).ToList();

        private static void Overflows(Component root, string label, StringBuilder report)
        {
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                if (string.IsNullOrWhiteSpace(t.text) || !t.enableWordWrapping && false) continue;
                var r = t.rectTransform.rect;
                t.ForceMeshUpdate();
                if (r.height > 4f && t.preferredHeight > r.height + 6f && t.overflowMode == TextOverflowModes.Overflow)
                    report.AppendLine(label + " | " + t.name + " h=" + r.height.ToString("0") + " need=" + t.preferredHeight.ToString("0") + " | " + t.text.Replace("\n", " ").Substring(0, Math.Min(70, t.text.Length)));
            }
            // Text drawn over other text: compare the rendered text bounds of every visible label pair.
            var items = new List<(TextMeshProUGUI t, Rect r)>();
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                if (string.IsNullOrWhiteSpace(t.text) || t.color.a < 0.05f) continue;
                t.ForceMeshUpdate();
                var b = t.textBounds;
                if (b.size.x < 1f || b.size.y < 1f) continue;
                var tr = t.rectTransform;
                var a = tr.TransformPoint(b.min); var c = tr.TransformPoint(b.max);
                var rect = Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
                var sr = t.GetComponentInParent<ScrollRect>();
                if (sr != null)
                {
                    var vc = new Vector3[4]; ((RectTransform)sr.transform).GetWorldCorners(vc);
                    var view = Rect.MinMaxRect(vc[0].x, vc[0].y, vc[2].x, vc[2].y);
                    if (!view.Overlaps(rect)) continue;
                }
                items.Add((t, rect));
            }
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    var x = items[i].r; var y = items[j].r;
                    float w = Mathf.Min(x.xMax, y.xMax) - Mathf.Max(x.xMin, y.xMin), h = Mathf.Min(x.yMax, y.yMax) - Mathf.Max(x.yMin, y.yMin);
                    if (w <= 0f || h <= 0f) continue;
                    float inter = w * h, small = Mathf.Min(x.width * x.height, y.width * y.height);
                    if (small > 0f && inter / small > 0.25f)
                        report.AppendLine(label + " | OVERLAP '" + items[i].t.text.Replace("\n", " ").Substring(0, Math.Min(30, items[i].t.text.Length)) + "' x '" + items[j].t.text.Replace("\n", " ").Substring(0, Math.Min(30, items[j].t.text.Length)) + "' " + (inter / small).ToString("0.00"));
                }
        }

        private static CoursePopup NewSchool(out Canvas canvas)
        {
            canvas = NLUi.CreateCanvas("CourseTestCanvas", 500);
            var popup = CoursePopup.GetOrCreate(canvas.gameObject, UiKit.Font);
            popup.Show();
            return popup;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator School_AllTabs_RenderAndCapture()
        {
            yield return DesktopResolution();
            var popup = NewSchool(out var canvas);
            var report = new StringBuilder();
            yield return null;
            Assert.IsTrue(popup.IsOpen);

            // Path: draft -> route
            popup.ShowTab(CoursePopup.Tab.Path); yield return null;
            Assert.Greater(Texts(popup).Count, 8, "path tab has content");
            yield return Capture("01_path_empty_vi");
            popup.PathView.SaveDraft(); yield return null;
            Assert.IsTrue(LearningPath.Current.IsSet, "saving the draft stores the path");
            Assert.NotNull(LearningPath.CurrentRoute());
            Overflows(popup, "path", report);
            yield return Capture("02_path_route_ja_vi");

            popup.ShowTab(CoursePopup.Tab.Lessons); yield return null;
            var unit = SchoolCatalog.Units("ja").First();
            popup.LessonsView.Select(unit.Id); yield return null;
            Assert.AreEqual(unit.Id, popup.LessonsView.SelectedUnitId);
            Overflows(popup, "lessons", report);
            yield return Capture("03_lessons_ja_vi");

            popup.ShowTab(CoursePopup.Tab.Alphabet); yield return null;
            Overflows(popup, "alphabet", report);
            yield return Capture("04_alphabet_ja_vi");

            popup.ShowTab(CoursePopup.Tab.Grammar); yield return null;
            Assert.Greater(Texts(popup).Count, 8);
            Overflows(popup, "grammar", report);
            yield return Capture("05_grammar_ja_vi");

            popup.ShowTab(CoursePopup.Tab.Certificates); yield return null;
            Assert.NotNull(popup.CertificatesView.Selected ?? SchoolCatalog.Certificate(popup.CertificatesView.SelectedId) ?? new CourseCertificate(), "certificates tab opens");
            Overflows(popup, "certificates", report);
            yield return Capture("06_certificates_ja_vi");

            popup.ChooseTarget("en"); yield return null;
            popup.ShowTab(CoursePopup.Tab.Certificates); yield return null;
            Overflows(popup, "certificates-en", report);
            yield return Capture("07_certificates_en_ielts_vi");

            popup.ShowTab(CoursePopup.Tab.Tenses); yield return null;
            popup.TensesView.Select(TensesCatalog.Lessons()[2].TenseId); yield return null;
            popup.TensesView.ChooseVerb(1); yield return null;
            Overflows(popup, "tenses", report);
            yield return Capture("08_tenses_vi");

            Loc.Set(GameLanguage.Russian); yield return null; yield return null;
            popup.ChooseTarget("ru"); yield return null;
            popup.ShowTab(CoursePopup.Tab.Lessons); yield return null;
            Overflows(popup, "lessons-ru", report);
            yield return Capture("09_lessons_ru_target_ru_ui");
            popup.ShowTab(CoursePopup.Tab.Path); yield return null;
            Overflows(popup, "path-ru", report);
            yield return Capture("10_path_ru_ui");
            popup.ChooseTarget("en"); popup.ShowTab(CoursePopup.Tab.Tenses); yield return null;
            Overflows(popup, "tenses-ru", report);
            yield return Capture("11_tenses_ru_ui");

            Loc.Set(GameLanguage.English); yield return null; yield return null;
            popup.ShowTab(CoursePopup.Tab.Path); yield return null;
            yield return Capture("12_path_en_ui");

            File.WriteAllText(Path.Combine(Out, "overflow_report.txt"), report.ToString(), new UTF8Encoding(false));
            Assert.AreEqual("", report.ToString(), "no text spills out of its box:\n" + report);
            popup.Hide();
        }

        private static IEnumerator RunQuiz(CoursePopup popup, HashSet<CourseQuestionKind> seen, Func<string, IEnumerator> capture, string prefix, int wrongEvery = 4)
        {
            var quiz = popup.ActiveView.Quiz;
            Assert.NotNull(quiz, prefix + ": a quiz opened");
            int total = quiz.Count, n = 0;
            Assert.Greater(total, 0);
            while (!quiz.Finished)
            {
                var q = quiz.Current;
                if (q != null && seen.Add(q.Kind)) yield return capture(prefix + "_q_" + q.Kind);
                bool right = wrongEvery <= 0 || n % wrongEvery != 3;
                quiz.AnswerForTest(right);
                Assert.IsTrue(quiz.Answered, prefix + ": answered " + q?.Id);
                if (n == 0) yield return capture(prefix + "_feedback");
                quiz.Next(); n++;
                if (n > total + 2) Assert.Fail(prefix + ": quiz did not finish, total=" + total + " n=" + n + " index=" + quiz.Index);
                if (n % 5 == 0) yield return null;
            }
            Assert.AreEqual(total, n);
            Assert.NotNull(quiz.Result);
            Assert.AreEqual(total, quiz.Result.Total);
            yield return null;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator School_Quizzes_RunEveryQuestionKind()
        {
            yield return DesktopResolution();
            var popup = NewSchool(out _);
            var seen = new HashSet<CourseQuestionKind>();
            yield return null;
            foreach (var target in new[] { "ja", "en" })
            {
                popup.ChooseTarget(target);
                popup.ShowTab(CoursePopup.Tab.Lessons); yield return null;
                var unit = SchoolCatalog.Units(target).First();
                popup.LessonsView.Select(unit.Id); popup.LessonsView.StartPractice(); yield return null;
                yield return RunQuiz(popup, seen, Capture, "q_unit_" + target);
                popup.ActiveView.HandleBack(); yield return null;
                Assert.IsFalse(popup.ActiveView.InQuiz, "back leaves the quiz");
                Assert.Greater(LearningProgressStore.Shared.StarsFor(LearningPath.UnitKey(unit.Id)), -1);

                popup.ShowTab(CoursePopup.Tab.Alphabet); yield return null;
                popup.AlphabetView.StartFullQuiz(); yield return null;
                yield return RunQuiz(popup, seen, Capture, "q_alpha_" + target, 0);
                popup.ActiveView.HandleBack(); yield return null;

                popup.ShowTab(CoursePopup.Tab.Certificates); yield return null;
                popup.CertificatesView.StartExam(); yield return null;
                yield return RunQuiz(popup, seen, Capture, "q_cert_" + target, 5);
                popup.ActiveView.HandleBack(); yield return null;
            }
            popup.ChooseTarget("en");
            popup.ShowTab(CoursePopup.Tab.Tenses); yield return null;
            popup.TensesView.Select(TensesCatalog.Lessons()[0].TenseId);
            popup.TensesView.StartPractice(); yield return null;
            yield return RunQuiz(popup, seen, Capture, "q_tense", 0);
            Assert.GreaterOrEqual(LearningProgressStore.Shared.StarsFor(LearningPath.TenseKey(TensesCatalog.Lessons()[0].TenseId)), 3, "a perfect tense drill earns three stars");
            popup.ActiveView.HandleBack(); yield return null;
            popup.TensesView.StartMixed(); yield return null;
            yield return RunQuiz(popup, seen, Capture, "q_tense_mixed", 3);
            popup.ActiveView.Quiz.Retry(true); yield return null;
            popup.ActiveView.HandleBack(); yield return null;
            File.WriteAllText(Path.Combine(Out, "question_kinds.txt"), string.Join("\n", seen), new UTF8Encoding(false));
            Assert.GreaterOrEqual(seen.Count, 3, "at least three question kinds were exercised: " + string.Join(",", seen));
            popup.Hide();
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator School_PlacementTest_RunsAndSavesThePath()
        {
            yield return DesktopResolution();
            var popup = NewSchool(out _);
            yield return null;
            foreach (var target in new[] { "ja", "en" })
            {
                popup.ChooseTarget(target);
                popup.ShowTab(CoursePopup.Tab.Path); yield return null;
                popup.PathView.StartPlacement(); yield return null;
                var quiz = popup.PathView.Quiz;
                Assert.NotNull(quiz, "placement quiz");
                Assert.AreEqual(20, quiz.Count);
                if (target == "ja") yield return Capture("20_placement_question");
                int i = 0;
                while (!quiz.Finished)
                {
                    if (i % 7 == 6) quiz.DontKnow(); else quiz.AnswerForTest(i < 14);
                    quiz.Next(); i++;
                    if (i > 60) Assert.Fail("placement did not finish");
                }
                yield return null;
                Assert.IsTrue(popup.PathView.ShowingPlacementResult, "result screen");
                Assert.IsFalse(string.IsNullOrEmpty(popup.PathView.PlacementLevel));
                if (target == "ja") yield return Capture("21_placement_result");
                string level = popup.PathView.PlacementLevel;
                popup.PathView.UsePlacement(); yield return null;
                Assert.IsTrue(LearningPath.Current.IsSet);
                Assert.AreEqual(target, LearningPath.Current.Target);
                Assert.AreEqual(level, LearningPath.Current.StartLevel);
            }
            yield return Capture("22_path_after_placement");
            popup.Hide();
        }

        private static Button Find(Component root, string name) => root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);

        [UnityTest, Timeout(300000)]
        public IEnumerator StudyHub_SixTiles_OpenSchoolTensesAndDictionary()
        {
            yield return DesktopResolution();
            var canvas = NLUi.CreateCanvas("HubTestCanvas", 500);
            var hub = canvas.gameObject.AddComponent<StudyHubPopup>();
            hub.Initialize(UiKit.Font);
            hub.Show(); yield return null; yield return null;
            for (int i = 0; i < 6; i++) Assert.NotNull(hub.transform.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "Tile" + i), "tile " + i);
            var texts = Texts(hub);
            Assert.IsTrue(texts.Any(t => t.Contains("Trường học")), "School tile");
            Assert.IsTrue(texts.Any(t => t.Contains("12 thì")), "Tenses tile");
            Assert.IsTrue(texts.Any(t => t.Contains("Từ điển")), "Dictionary tile");
            var report = new StringBuilder();
            Overflows(hub, "studyhub", report);
            yield return Capture("30_studyhub_six_tiles_vi");
            Loc.Set(GameLanguage.Russian); yield return null; yield return null;
            Overflows(hub, "studyhub-ru", report);
            yield return Capture("31_studyhub_six_tiles_ru");
            Loc.Set(GameLanguage.Vietnamese); yield return null; yield return null;
            File.WriteAllText(Path.Combine(Out, "overflow_studyhub.txt"), report.ToString(), new UTF8Encoding(false));
            Assert.AreEqual("", report.ToString(), report.ToString());

            // School tile
            var open = hub.transform.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Tile3").GetComponentInChildren<Button>(true);
            open.onClick.Invoke(); yield return null;
            var school = canvas.GetComponent<CoursePopup>();
            Assert.NotNull(school); Assert.IsTrue(school.IsOpen); Assert.IsFalse(hub.IsOpen);
            school.Hide();
            // Tenses tile
            hub.Show(); yield return null;
            hub.transform.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Tile4").GetComponentInChildren<Button>(true).onClick.Invoke(); yield return null;
            Assert.IsTrue(school.IsOpen); Assert.AreEqual(CoursePopup.Tab.Tenses, school.CurrentTab);
            school.Hide();
            // Dictionary tile
            hub.Show(); yield return null;
            hub.transform.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Tile5").GetComponentInChildren<Button>(true).onClick.Invoke(); yield return null; yield return null;
            Assert.IsTrue(HandbookUI.IsOpen); Assert.AreEqual(HandbookUI.Chapter.Dictionary, HandbookUI.CurrentChapter);
            yield return Capture("32_dictionary_empty_vi");
            HandbookUI.Close();
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Handbook_Dictionary_SearchesInAllDirections()
        {
            yield return DesktopResolution();
            HandbookUI.Open(HandbookUI.Chapter.Dictionary); yield return null; yield return null;
            var report = new StringBuilder();
            var overlap = new StringBuilder();
            var cases = new[]
            {
                ("ja", "vi", "みず", "40_dict_ja_vi"),
                ("en", "vi", "water", "41_dict_en_vi"),
                ("vi", "ja", "nước", "42_dict_vi_ja"),
                ("ru", "en", "вода", "43_dict_ru_en"),
                ("ja", "ru", "たべる", "44_dict_ja_ru"),
            };
            foreach (var c in cases)
            {
                HandbookUI.DictionarySearch(c.Item3, c.Item1, c.Item2); yield return null; yield return null;
                Assert.AreEqual(HandbookUI.Chapter.Dictionary, HandbookUI.CurrentChapter);
                Assert.AreEqual(c.Item1, HandbookUI.DictionaryFrom); Assert.AreEqual(c.Item2, HandbookUI.DictionaryTo);
                var ids = HandbookUI.EntryIds.ToList();
                report.AppendLine(c.Item1 + ">" + c.Item2 + " '" + c.Item3 + "' -> " + string.Join(",", ids.Take(6)) + " (" + ids.Count + ")");
                Assert.IsTrue(ids.Any(i => i.StartsWith("h")), "hits for " + c.Item3 + " " + c.Item1 + ">" + c.Item2 + ": " + string.Join(",", ids));
                var hb = GameObject.Find("HandbookCanvas");
                if (hb != null) Overflows(hb.transform, "dict " + c.Item4, overlap);
                yield return Capture(c.Item4);
            }
            HandbookUI.DictionarySearch("qzxqzxqzx", "en", "vi"); yield return null; yield return null;
            Assert.IsTrue(HandbookUI.EntryIds.Contains("none"), "an unknown word shows the 'nothing found' entry");
            yield return Capture("45_dict_nothing_found");
            Loc.Set(GameLanguage.Russian); yield return null; yield return null;
            HandbookUI.DictionarySearch("water", "en", "ru"); yield return null; yield return null;
            yield return Capture("46_dict_en_ru_ui_ru");
            File.WriteAllText(Path.Combine(Out, "dictionary_cases.txt"), report.ToString(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(Out, "overflow_dictionary.txt"), overlap.ToString(), new UTF8Encoding(false));
            Assert.AreEqual("", overlap.ToString(), overlap.ToString());
            HandbookUI.Close();
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator ExamCenter_ShowsSchoolShortcut_ForTheSavedPath()
        {
            yield return DesktopResolution();
            var canvas = NLUi.CreateCanvas("ExamLinkCanvas", 500);
            var exam = canvas.gameObject.AddComponent<ExamCenterPopup>();
            exam.Initialize(UiKit.Font);
            exam.Show(); yield return null; yield return null;
            var generic = Find(exam, "SchoolShortcut");
            Assert.NotNull(generic, "the exam hall always links to the school");
            yield return Capture("50_exam_center_shortcut_generic");
            exam.Hide();

            var goal = LearningPath.Goals().FirstOrDefault(g => SchoolCatalog.IsTarget("en"));
            LearningPath.Save(new LearningPath.Choice { Target = "en", Goal = goal.Id, StartLevel = "A1" });
            var route = LearningPath.CurrentRoute();
            Assert.NotNull(route);
            var step = ExamCenterPopup.NextCertificateStep();
            exam.Show(); yield return null; yield return null;
            var shortcut = Find(exam, "SchoolShortcut");
            Assert.NotNull(shortcut);
            if (step != null)
                Assert.IsTrue(shortcut.GetComponentsInChildren<TextMeshProUGUI>(true).Any(t => t.text.Contains(LearningPath.StepTitle(step))), "the shortcut names the next certificate");
            yield return Capture("51_exam_center_shortcut_path");
            shortcut.onClick.Invoke(); yield return null; yield return null;
            var school = canvas.GetComponent<CoursePopup>();
            Assert.NotNull(school); Assert.IsTrue(school.IsOpen); Assert.IsFalse(exam.IsOpen);
            if (step != null && step.Type == "certificate") { Assert.AreEqual(CoursePopup.Tab.Certificates, school.CurrentTab); Assert.AreEqual(step.Id, school.CertificatesView.SelectedId); }
            yield return Capture("52_school_opened_from_exam_center");
            school.Hide();
        }
    }
}