using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Exam;
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
    /// The full JLPT N5 mock through the real exam UI (captures: Bao_Cao/jlpt-regression): listening scripts stay hidden
    /// while answering, the AI-voiced audio plays once, scoring uses the N5 divisions (0–120 + 0–60, minima 38/19,
    /// pass 80) and the result screen gives the per-もんだい analysis.
    /// </summary>
    public class JlptMockPlayModeTests
    {
        [UnityTest]
        public IEnumerator N5Mock2_ListeningScoringAndAnalysis()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || ExamManager.Instance == null || t < 2f); t += Time.unscaledDeltaTime) yield return null;
            var exam = Resources.Load<ExamDefinition>("Exams/jlpt_n5_mock_2");
            Assert.NotNull(exam);
            var manager = ExamManager.Instance;
            Assert.NotNull(manager, "Exam service");
            ExamAttemptResult result = null;
            manager.OnExamFinished += r => result = r;
            manager.StartAttempt(exam);
            // Hosted on a screen canvas, as the Exam Center does in the game.
            var ui = ExamPlayUI.GetOrCreate(NLUi.CreateCanvas("ExamHostCanvas", 400).transform);
            ui.Show();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("01_vocab_question");

            // Planned answers: vocab wrong in もんだい2 (1) and もんだい4 (all 3); grammar wrong in もんだい2 (all 4);
            // listening wrong in もんだい3 and もんだい4 (11). Raw: 言語知識 35/43, 聴解 13/24.
            int Answer(ExamSection section, ExamQuestion q, int index)
            {
                bool wrong = section.type == ExamSectionType.JlptVocabulary ? q.mondai == "m4" || q.id == "n5m2_v08"
                           : section.type == ExamSectionType.JlptGrammarReading ? q.mondai == "m2"
                           : q.mondai == "m3" || q.mondai == "m4";
                return wrong ? (q.correctChoiceIndex + 1) % q.choices.Count : q.correctChoiceIndex;
            }

            for (int guard = 0; guard < 6 && manager.IsAttemptActive; guard++)
            {
                var section = manager.CurrentSection;
                if (section.type == ExamSectionType.JlptListening)
                {
                    manager.GoToQuestion(0);
                    yield return new WaitForSecondsRealtime(0.3f);
                    var passage = section.FindPassage(section.questions[0].passageId);
                    var texts = ui.GetComponentsInChildren<TextMeshProUGUI>(false).Select(t => t.text).ToList();
                    Assert.IsTrue(texts.Any(t => t.Contains("聴解") || t.Contains("Nghe")), "The listening screen is visible");
                    Assert.IsFalse(texts.Any(t => t.Contains("しりょう")), "The script is hidden while answering");
                    var play = ui.GetComponentsInChildren<Button>(false).First(b => b.GetComponentInChildren<TextMeshProUGUI>()?.text.StartsWith("Nghe") == true || b.GetComponentInChildren<TextMeshProUGUI>()?.text.StartsWith("Play") == true || b.GetComponentInChildren<TextMeshProUGUI>()?.text.StartsWith("再生") == true);
                    Assert.IsTrue(play.interactable, "Audio available");
                    play.onClick.Invoke();
                    yield return new WaitForSecondsRealtime(0.4f);
                    var audio = ui.GetComponent<AudioSource>();
                    Assert.IsTrue(audio != null && audio.isPlaying && audio.clip == passage.audioClip, "The question's audio plays");
                    Assert.AreEqual(0, manager.RemainingPlays(passage), "Played once — no replays, as in the real test");
                    yield return Capture("02_listening_playing");
                    audio.Stop();
                }
                for (int q = 0; q < section.questions.Count; q++)
                {
                    manager.GoToQuestion(q);
                    manager.SubmitChoice(Answer(section, section.questions[q], q));
                }
                manager.SubmitCurrentSection();
                yield return null;
            }

            for (float t = 0f; t < 20f && result == null; t += Time.unscaledDeltaTime) yield return null;
            if (result == null)
            {
                // The last SubmitCurrentSection may already have finished the attempt before we subscribed.
                var progress = GameServices.TryGet(out NihongoLife.Save.IProgressRepository repo) ? repo.GetProgress() : null;
                var last = progress?.examAttempts?.LastOrDefault(a => a.examId == exam.id);
                Assert.NotNull(last, "The attempt was saved");
                result = new ExamAttemptResult { exam = exam, record = last };
            }
            var record = result.record;
            var gengo = record.groups.First(g => g.groupId == "gengo");
            var choukai = record.groups.First(g => g.groupId == "choukai");
            Assert.AreEqual(35, gengo.rawCorrect); Assert.AreEqual(43, gengo.rawMax);
            Assert.AreEqual(Mathf.RoundToInt(35f / 43f * 120f), gengo.scored);
            Assert.AreEqual(13, choukai.rawCorrect); Assert.AreEqual(24, choukai.rawMax);
            Assert.AreEqual(Mathf.RoundToInt(13f / 24f * 60f), choukai.scored);
            Assert.AreEqual(gengo.scored + choukai.scored, record.totalScore);
            Assert.AreEqual(180, record.totalMaxScore);
            Assert.IsTrue(record.passed, "Above 80 with both divisions over their minimum");

            var report = ExamAnalysis.Analyze(exam, record);
            Assert.IsTrue(report.advice.Any(a => a.Contains("文の組み立て")), "Weakest 言語知識 もんだい (★ ordering, 0/4) is named");
            Assert.IsTrue(report.advice.Any(a => a.Contains("即時応答")), "Weakest 聴解 もんだい (0/6, most wrong) is named");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("03_results_analysis");
            var texts2 = ui.GetComponentsInChildren<TextMeshProUGUI>(false).Select(t => t.text).ToList();
            Assert.IsTrue(texts2.Any(t => t.Contains("しりょう")), "The review shows the listening script");
            Assert.IsTrue(texts2.Any(t => t.Contains("言語知識（文字・語彙・文法）・読解")), "Division scores are shown");
            File.WriteAllLines(Path.Combine(FolderPath(), "result.txt"), new[] { $"gengo {gengo.scored}/120 choukai {choukai.scored}/60 total {record.totalScore} passed {record.passed}", report.status }.Concat(report.advice));
            ui.SendMessage("Hide", SendMessageOptions.DontRequireReceiver);
        }

        private static string FolderPath()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/jlpt-regression"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>Switches the overlay canvases to the camera, lets one frame rebuild layout and masks, then renders.</summary>
        private static IEnumerator Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) yield break;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            yield return null;
            yield return null;
            var old = RenderTexture.active;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(FolderPath(), name + ".jpg"), tex.EncodeToJPG(88));
            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); target.Release(); Object.Destroy(target);
        }
    }
}
