using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using NihongoLife.Player;
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
    /// End-to-end IELTS Listening on the local package "cambridge14_test1" (LocalContent, gitignored). Contains no
    /// exam text: answers are read from the package's key.json at runtime. Screenshots are written to
    /// LocalContent/_captures (gitignored) because they show licensed content. Skipped when the package is absent.
    /// </summary>
    public class IeltsListeningPlayModeTests
    {
        private const string PackageId = "cambridge14_test1_listening";

        [UnityTest]
        public IEnumerator Listening_Cambridge14Test1_EndToEnd()
        {
            var package = IeltsLibrary.Discover().FirstOrDefault(p => p.Id == PackageId);
            if (package == null) { Assert.Ignore("Local package not present on this machine."); yield break; }
            var key = IeltsLibrary.LoadKey(package);
            Assert.NotNull(key, "key.json must load and match the test id.");
            IeltsAttemptStore.ClearAttempt(PackageId);
            int historyBefore = IeltsAttemptStore.LoadHistory().entries.Count(e => e.testId == PackageId);

            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var dialogue = NihongoLife.Dialogue.DialogueManager.Instance;
            while (dialogue != null && dialogue.IsOpen) { dialogue.CancelDialogue(); yield return null; }
            bool lockedBefore = player.InputLocked;

            // 1) Open from the Exam Center card (IELTS tab).
            var center = Object.FindFirstObjectByType<ExamCenterPopup>();
            Assert.NotNull(center, "HUD hosts the exam center.");
            center.Show();
            yield return null;
            Invoke(center, "SelectTab", NihongoLife.Exam.ExamType.Ielts);
            yield return null;
            var card = PackageCard(package);
            Assert.NotNull(card, "The local IELTS package appears in the IELTS tab.");
            Capture("00_exam_center");
            ClickLabel(card, "Luyện tập");
            yield return new WaitForSecondsRealtime(0.5f);
            var ui = IeltsTestUI.Instance;
            Assert.NotNull(ui); Assert.IsTrue(ui.IsOpen);
            Assert.IsTrue(UiModalStack.ImmersiveOpen, "The test hides the HUD and blocks hotkeys.");
            Assert.IsTrue(player.InputLocked, "Movement is locked during the test.");
            Assert.AreEqual(40, ui.QuestionCount);

            // 2) Audio (practice): Part 1 loads, plays, pauses and seeks.
            yield return WaitUntil(() => ui.CurrentClip != null, 20f, "Part 1 audio must load.");
            Assert.AreEqual(523.2f, ui.CurrentClip.length, 1.5f, "Part 1 audio = track S1 (8:43).");
            ui.TogglePlay();
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(ui.AudioPlaying);
            Assert.Greater(ui.AudioTime, 0.5f, "Audio is advancing.");
            ui.Seek(100f);
            yield return null;
            Assert.AreEqual(100f, ui.AudioTime, 1f, "Practice mode can seek.");
            Capture("01_part1_playing");
            ui.TogglePlay();
            Assert.IsFalse(ui.AudioPlaying, "Practice mode can pause.");

            // 3) Hotkeys are blocked; Esc asks before leaving and a second Esc stays in the test.
            yield return Press(GameInputId.Inventory);
            Assert.AreEqual("IELTS test", UiModalStack.TopName, "B must not open the bag during the test.");
            yield return Press(GameInputId.Pause);
            Assert.IsTrue(ui.IsConfirmOpen, "Esc asks before leaving.");
            yield return Press(GameInputId.Pause);
            Assert.IsFalse(ui.IsConfirmOpen); Assert.IsTrue(ui.IsOpen);

            // 4) Answer every question through the real widgets.
            var answers = key.answers.ToDictionary(a => a.number, a => IeltsGrader.Expand(a.accepted[0]).First());
            answers[1] = answers[1] + " citizen";       // over the ONE WORD AND/OR A NUMBER limit → wrong
            answers[4] = "£" + answers[4];               // currency sign is accepted
            // An answer whose key has an optional "(...)" part is given in its long form (must be accepted).
            int optionalQuestion = key.answers.First(a => a.number <= 10 && a.accepted[0].Contains("(") && a.accepted[0].IndexOf('(') > 0 && !char.IsWhiteSpace(a.accepted[0][a.accepted[0].IndexOf('(') - 1])).number;
            answers[optionalQuestion] = IeltsGrader.Expand(key.answers.First(a => a.number == optionalQuestion).accepted[0]).Last();
            answers[10] = answers[10].Replace(" ", "");  // digits without the space are accepted
            for (int part = 0; part < ui.Test.parts.Length; part++)
            {
                ui.ShowPart(part);
                yield return null; yield return null;
                foreach (var group in ui.Test.parts[part].groups)
                    yield return AnswerGroup(group, answers, key);
                yield return new WaitForSecondsRealtime(0.3f);
                Capture($"0{2 + part}_part{part + 1}_answered");
            }
            Assert.AreEqual(40, ui.AnsweredCount, "Every question has an answer.");

            // 5) Leave and resume: answers and position survive.
            ui.RequestClose();
            yield return null;
            ClickLabel(Root(ui), "Tạm rời");
            yield return null;
            Assert.IsFalse(ui.IsOpen);
            Assert.AreEqual(lockedBefore, player.InputLocked, "Leaving restores movement.");
            Assert.IsFalse(UiModalStack.ImmersiveOpen);
            var saved = IeltsAttemptStore.LoadAttempt(PackageId);
            Assert.NotNull(saved); Assert.AreEqual(40, saved.responses.Count);
            center.Show();
            yield return null;
            Invoke(center, "SelectTab", NihongoLife.Exam.ExamType.Ielts);
            yield return null;
            card = PackageCard(package);
            ClickLabel(card, "Tiếp tục");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(ui.IsOpen);
            Assert.AreEqual(answers[5], ui.GetAnswer(5), "Resumed attempt keeps the answers.");
            Assert.AreEqual(40, ui.AnsweredCount);

            // 6) Submit → graded with the original key → review.
            ui.RequestSubmit();
            yield return null;
            Assert.IsTrue(ui.IsConfirmOpen, "Submitting asks for confirmation.");
            ClickLabel(Root(ui), "Nộp bài", inConfirm: true);
            yield return new WaitForSecondsRealtime(0.5f);
            var result = ui.LastResult;
            Assert.NotNull(result, "Submitted and graded.");
            Assert.AreEqual(40, result.Max);
            Assert.AreEqual(39, result.Raw, "Only Q1 (over the word limit) is wrong.");
            Assert.IsTrue(result.Items.Single(i => i.Number == 1).OverLimit);
            Assert.IsTrue(result.Items.Single(i => i.Number == optionalQuestion).IsCorrect, "The long form of an optional key part is accepted.");
            Assert.AreEqual(9f, result.Band);
            Assert.AreEqual(historyBefore + 1, IeltsAttemptStore.LoadHistory().entries.Count(e => e.testId == PackageId));
            Assert.IsNull(IeltsAttemptStore.LoadAttempt(PackageId), "A submitted attempt is cleared.");
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("06_results");
            ui.Close();
            yield return null;

            // 7) Exam mode: no pause / seek, parts follow each other, review time then automatic submission.
            ui = IeltsTestUI.Open(package, IeltsTestUI.ExamMode, false);
            yield return WaitUntil(() => ui.CurrentClip != null, 20f, "Exam audio must load.");
            Assert.IsFalse(ui.AudioPlaying, "Exam audio waits for the candidate to start.");
            ui.TogglePlay();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsTrue(ui.AudioPlaying);
            ui.TogglePlay();
            Assert.IsTrue(ui.AudioPlaying, "Exam mode cannot pause.");
            float before = ui.AudioTime;
            ui.Seek(300f);
            Assert.Less(ui.AudioTime, before + 5f, "Exam mode cannot seek.");
            Capture("07_exam_mode");
            // Jump to the end of Part 1 (test hook): the next part must start by itself.
            var source = (AudioSource)typeof(IeltsTestUI).GetField("_audio", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            source.time = source.clip.length - 0.6f;
            yield return WaitUntil(() => ui.AudioPart == 1 && ui.AudioPlaying, 20f, "Part 2 starts automatically after Part 1.");
            Assert.AreEqual(404.6f, ui.CurrentClip.length, 1.5f, "Part 2 audio = track S2 (6:45).");
            // Finish the remaining parts (test hook) and let the review time run out.
            var attempt = (IeltsAttempt)typeof(IeltsTestUI).GetField("_attempt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            source.Stop();
            for (int i = 0; i < attempt.partsFinished.Length; i++) attempt.partsFinished[i] = true;
            attempt.reviewRemaining = 1.2f;
            yield return WaitUntil(() => ui.IsSubmitted, 6f, "The test submits itself when review time ends.");
            Assert.AreEqual(0, ui.LastResult.Raw, "Nothing was answered in this exam run.");
            ui.Close();
            yield return null;
            Assert.IsFalse(UiModalStack.AnyOpen);
            IeltsAttemptStore.ClearAttempt(PackageId);
        }

        private static IEnumerator AnswerGroup(IeltsGroup group, System.Collections.Generic.Dictionary<int, string> answers, IeltsKey key)
        {
            var root = Root(IeltsTestUI.Instance);
            switch (group.type)
            {
                case "completion":
                    for (int n = group.from; n <= group.to; n++)
                    {
                        var holder = Find(root, "Gap" + n);
                        Assert.NotNull(holder, $"Gap {n} rendered.");
                        holder.GetComponentInChildren<TMP_InputField>().text = answers[n];
                    }
                    break;
                case "mcq_single":
                    foreach (var item in group.items)
                        Click(Find(Find(root, "Q" + item.number), "Opt" + answers[item.number].ToUpperInvariant()));
                    break;
                case "mcq_multi":
                    var block = Find(root, $"Q{group.from}-{group.to}");
                    foreach (string letter in key.answers.First(a => a.number == group.from).accepted)
                        Click(Find(block, "Opt" + letter));
                    break;
                case "matching":
                    foreach (var item in group.items)
                        Click(Find(Find(root, "Q" + item.number), "L" + answers[item.number].ToUpperInvariant()));
                    break;
            }
            yield return null;
        }

        /// <summary>The Exam Center card of this package (there is one card per local package).</summary>
        private static RectTransform PackageCard(IeltsLibrary.Package package) =>
            Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == "IeltsLocalCard" &&
                r.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text.Contains(package.Test.title)));

        private static RectTransform Root(IeltsTestUI ui) => (RectTransform)ui.GetType().GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name && t.gameObject.activeInHierarchy);
        }

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

        private static IEnumerator Press(GameInputId id)
        {
            var input = GameInputService.GetOrCreate();
            yield return new WaitForFixedUpdate();
            input.SetMobileButton(id, true);
            yield return null;
            input.SetMobileButton(id, false);
            yield return null; yield return null;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds, string message)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail(message);
                yield return null;
            }
        }

        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);

        /// <summary>Licensed content is visible on screen, so captures stay in the gitignored LocalContent folder.</summary>
        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            string folder = Path.Combine(IeltsLibrary.Root, "_captures");
            Directory.CreateDirectory(folder);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
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
