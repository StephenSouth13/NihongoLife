using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// IELTS Reading and every local package (LocalContent, gitignored — no exam text here; answers come from each
    /// package's key.json at runtime). Captures go to LocalContent/IELTS/_captures. Skipped when packages are absent.
    /// </summary>
    public class IeltsReadingPlayModeTests
    {
        private const string ReadingId = "cambridge14_test1_reading";

        [UnityTest]
        public IEnumerator Reading_Cambridge14Test1_TimedExamEndToEnd()
        {
            var package = IeltsLibrary.Discover().FirstOrDefault(p => p.Id == ReadingId);
            if (package == null) { Assert.Ignore("Local package not present on this machine."); yield break; }
            var key = IeltsLibrary.LoadKey(package);
            IeltsAttemptStore.ClearAttempt(ReadingId);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);

            var ui = IeltsTestUI.Open(package, IeltsTestUI.ExamMode, false);
            yield return null; yield return null;
            Assert.IsTrue(ui.IsOpen);
            Assert.AreEqual(40, ui.QuestionCount);
            Assert.AreEqual(60, ui.Test.timeLimitMinutes);
            var timer = Texts(ui).FirstOrDefault(t => t.Contains("Còn lại"));
            Assert.NotNull(timer, "Exam mode counts down the 60 minutes");
            StringAssert.Contains("59:5", timer, "The countdown starts at 60:00");
            Assert.IsTrue(Texts(ui).Any(t => t.Contains("bike-sharing") || t.Contains("children")), "The passage is shown beside the questions");

            // Answer through the real widgets: everything right except Q9 (TRUE → FALSE) and Q36 (wrong word).
            // "IN EITHER ORDER" sets: each box gets a different answer of the set (the same word twice scores once).
            var answers = key.answers.ToDictionary(a => a.number, a =>
                IeltsGrader.Expand(a.accepted[a.set != null && a.set.Length > 0 ? System.Array.IndexOf(a.set, a.number) % a.accepted.Length : 0]).First());
            answers[9] = "FALSE";
            answers[36] = "hotels";
            for (int part = 0; part < ui.Test.parts.Length; part++)
            {
                ui.ShowPart(part);
                yield return null; yield return null;
                foreach (var group in ui.Test.parts[part].groups) AnswerGroup(group, answers, key);
                yield return new WaitForSecondsRealtime(0.2f);
                Capture($"r0{part + 1}_reading_part{part + 1}");
            }
            Assert.AreEqual(40, ui.AnsweredCount);
            ui.RequestSubmit();
            yield return null;
            ClickLabel(Root(ui), "Nộp bài", inConfirm: true);
            yield return new WaitForSecondsRealtime(0.5f);
            var wrong = ui.LastResult.Items.Where(i => !i.IsCorrect).Select(i => $"{i.Number}:'{i.Given}'");
            Assert.AreEqual(38, ui.LastResult.Raw, "Only Q9 and Q36 are wrong; wrong: " + string.Join(", ", wrong));
            Assert.AreEqual(IeltsGrader.AcademicReadingBand(38), ui.LastResult.Band, "Academic Reading band table");
            Capture("r04_reading_results");
            ui.Close();
            yield return null;

            // Time runs out in exam mode: the paper submits itself.
            ui = IeltsTestUI.Open(package, IeltsTestUI.ExamMode, false);
            yield return null;
            var attempt = (IeltsAttempt)typeof(IeltsTestUI).GetField("_attempt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            attempt.elapsedSeconds = 60 * 60 - 1.5f;
            for (float t = 0f; t < 5f && !ui.IsSubmitted; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(ui.IsSubmitted, "Submitted automatically at 60:00");
            ui.Close();
            yield return null;
            IeltsAttemptStore.ClearAttempt(ReadingId);
        }

        [UnityTest]
        public IEnumerator AllLocalPackages_RenderEveryQuestionAndHaveKeys()
        {
            var packages = IeltsLibrary.Discover();
            if (packages.Count == 0) { Assert.Ignore("No local IELTS packages on this machine."); yield break; }
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            var report = new List<string>();
            foreach (var package in packages)
            {
                var key = IeltsLibrary.LoadKey(package);
                Assert.NotNull(key, package.Id + ": key.json");
                var ui = IeltsTestUI.Open(package, IeltsTestUI.PracticeMode, false);
                yield return null;
                int total = ui.QuestionCount;
                var keyed = new HashSet<int>(key.answers.Select(a => a.number));
                var rendered = new HashSet<int>();
                for (int part = 0; part < ui.Test.parts.Length; part++)
                {
                    ui.ShowPart(part);
                    yield return null;
                    var names = Root(ui).GetComponentsInChildren<Transform>(false).Select(t => t.name).ToList();
                    foreach (var group in ui.Test.parts[part].groups)
                        for (int n = group.from; n <= group.to; n++)
                            if (names.Contains("Gap" + n) || names.Contains("Q" + n) || names.Contains($"Q{group.from}-{group.to}")) rendered.Add(n);
                }
                ui.Close();
                yield return null;
                IeltsAttemptStore.ClearAttempt(package.Id);
                report.Add($"{package.Id}: {total} questions, rendered {rendered.Count}, keyed {keyed.Count}");
                Assert.AreEqual(Enumerable.Range(1, total), rendered.OrderBy(n => n), package.Id + ": every question has a widget");
                Assert.IsTrue(keyed.SetEquals(Enumerable.Range(1, total)), package.Id + ": every question has a key");
            }
            Directory.CreateDirectory(Path.Combine(IeltsLibrary.Root, "_captures"));
            File.WriteAllLines(Path.Combine(IeltsLibrary.Root, "_captures", "packages.txt"), report);
        }

        // ─────────── helpers ───────────

        private static IEnumerable<string> Texts(IeltsTestUI ui) => Root(ui).GetComponentsInChildren<TextMeshProUGUI>(false).Select(t => t.text);

        private static void AnswerGroup(IeltsGroup group, Dictionary<int, string> answers, IeltsKey key)
        {
            var root = Root(IeltsTestUI.Instance);
            switch (group.type)
            {
                case "completion":
                    for (int n = group.from; n <= group.to; n++) Find(root, "Gap" + n).GetComponentInChildren<TMP_InputField>().text = answers[n];
                    break;
                case "mcq_multi":
                    var block = Find(root, $"Q{group.from}-{group.to}");
                    foreach (string letter in key.answers.First(a => a.number == group.from).accepted) Click(Find(block, "Opt" + letter));
                    break;
                case "matching":
                case "tfng":
                case "ynng": // letter row per item: L<letter> / LTRUE / LNOT GIVEN
                    foreach (var item in group.items) Click(Find(Find(root, "Q" + item.number), "L" + answers[item.number].ToUpperInvariant()));
                    break;
                default: // mcq_single: option list per item
                    foreach (var item in group.items) Click(Find(Find(root, "Q" + item.number), "Opt" + answers[item.number].ToUpperInvariant()));
                    break;
            }
        }

        private static RectTransform Root(IeltsTestUI ui) => (RectTransform)ui.GetType().GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);

        private static Transform Find(Transform root, string name) =>
            root?.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name && t.gameObject.activeInHierarchy);

        private static void Click(Transform target)
        {
            Assert.NotNull(target, "Widget to click must exist.");
            target.GetComponent<Button>().onClick.Invoke();
        }

        private static void ClickLabel(Transform root, string label, bool inConfirm = false)
        {
            var button = root.GetComponentsInChildren<Button>(false).FirstOrDefault(b =>
                b.GetComponentInChildren<TextMeshProUGUI>()?.text == label && (!inConfirm || b.transform.parent.parent.name == "Box"));
            Assert.NotNull(button, $"Button '{label}' must be visible.");
            button.onClick.Invoke();
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            string folder = Path.Combine(IeltsLibrary.Root, "_captures");
            Directory.CreateDirectory(folder);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(1600, 900, 24);
            var old = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); target.Release(); Object.Destroy(target);
        }
    }
}
