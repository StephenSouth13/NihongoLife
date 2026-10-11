using System;
using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Exam;
using NihongoLife.Exam.Ielts;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// IELTS Writing and Speaking end to end in the real test screen, with the real AI when it is configured
    /// (NIHONGOLIFE_GEMINI_API_KEY): typing, word count, autosave, submit, AI Examiner on four criteria, AI Coach,
    /// a rewrite (coached, never scored), saved history; offline / daily-limit failures; Speaking with recorded
    /// speech (NL_SPEECH_FIXTURES: WAV files spoken by the Windows TTS voice), transcripts and FC/LR/GRA/P; the real
    /// microphone through MicRecorder and through Agora's recording-device test. Captures: Bao_Cao/ielts-productive.
    /// </summary>
    public class IeltsProductivePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/ielts-productive"));

        private const string Task1 =
            "The table compares the number of adults studying Japanese in four cities in 2015 and 2025, together with the proportion who mainly studied online in 2025. " +
            "Overall, three of the four cities saw growth in learner numbers, with Hanoi showing by far the most dramatic increase, while online study was most common in São Paulo and least common in Tokyo. " +
            "In 2015, Tokyo had the largest number of learners at 32,000, followed by Bangkok with 11,000 and Hanoi with 9,000. São Paulo had the fewest, at 7,000. " +
            "By 2025, the figure for Tokyo had risen to 41,000, but Hanoi had tripled to 27,000, overtaking Bangkok, which only grew slightly to 15,000. In contrast, São Paulo was the only city where numbers fell, dropping a little to 6,000. " +
            "Regarding online learning, just under a fifth of learners in Tokyo (18%) studied mainly online, compared with 39% in Bangkok and 46% in Hanoi. Over half of the learners in São Paulo, 52%, chose this way of studying.";

        private const string Task2 =
            "Nowadays, many people learn foreign languages with games and simulations instead of sitting in a classroom. Some argue that this is more effective, while others believe traditional lessons is still essential. " +
            "In my opinion, both approaches have value, but games work best when they are combined with classroom teaching.\n\n" +
            "On the one hand, games and simulations make learning more enjoyable. When learners play a game set in a Japanese town, for example, they must read signs, buy food and talk with characters, so they use the language in realistic situations. " +
            "This makes them remember vocabulary more easier, because the words are connected with actions and emotions. In addition, games give instant feedback and let students practise as many time as they want without feeling embarrassed.\n\n" +
            "On the other hand, classroom lessons provides things that games cannot easily offer. A teacher can explain grammar clearly, notice the specific mistakes of each student and correct pronunciation. " +
            "Moreover, studying together with classmates create motivation and a fixed schedule, which many adult learners need. Games often focus on a small set of situations, so learners may not develop the accuracy needed for exams or work. " +
            "For instance, a player can become very good at ordering food in a game but still be unable to write a polite email to a company or follow a lecture.\n\n" +
            "In conclusion, I believe that games and simulations are a powerful tool, but they should not replace teachers completely. The most effective method is to use games for practice and motivation, and classroom lessons for explanation and correction.";

        private const string Task2Rewrite =
            "Nowadays, many people learn foreign languages with games and simulations instead of sitting in a classroom. Some argue that this is more effective, while others believe traditional lessons are still essential. " +
            "In my opinion, both approaches have value, but games work best when they are combined with classroom teaching.\n\n" +
            "On the one hand, games and simulations make learning more enjoyable. When learners play a game set in a Japanese town, for example, they must read signs, buy food and talk with characters, so they use the language in realistic situations. " +
            "This makes vocabulary easier to remember, because the words are connected with actions and emotions. In addition, games give instant feedback and let students practise as many times as they want without feeling embarrassed.\n\n" +
            "On the other hand, classroom lessons provide things that games cannot easily offer. A teacher can explain grammar clearly, notice the specific mistakes of each student and correct pronunciation. " +
            "Moreover, studying together with classmates creates motivation and a fixed schedule, which many adult learners need. Games often focus on a small set of situations, so learners may not develop the accuracy needed for exams or work. " +
            "For instance, a player can become very good at ordering food in a game but still be unable to write a polite email to a company or follow a lecture.\n\n" +
            "In conclusion, I believe that games and simulations are a powerful tool, but they should not replace teachers completely. The most effective method is to use games for practice and motivation, and classroom lessons for explanation and correction.";

        [UnityTest]
        public IEnumerator Writing_RealAi_ExaminerCoachRewriteHistory()
        {
            yield return LoadCity();
            if (!GameServices.TryGet(out ExamGradingService grading) || !grading.IsConfigured) Assert.Inconclusive("Gemini is not configured on this machine.");
            var ui = Open("nl_writing_academic_1");
            ui.SetAnswer(1, Task1);
            ui.SetAnswer(2, Task2);
            ui.ShowPart(1);
            yield return new WaitForSecondsRealtime(0.8f);
            var count = Texts(ui).FirstOrDefault(t => t.name == "WordCount_2");
            Assert.NotNull(count, "Word count under Task 2");
            StringAssert.Contains($"<b>{SpeechAudio.CountWords(Task2)}</b>", count.text);
            StringAssert.Contains("đã lưu tự động", count.text, "Autosave stamp");
            Assert.AreEqual(Task2, IeltsAttemptStore.LoadAttempt("nl_writing_academic_1").responses.First(r => r.number == 2).value, "Draft saved to disk");
            Capture("01_writing_task2_editor");

            int historyBefore = IeltsAttemptStore.LoadHistory().entries.Count;
            ui.SubmitNow();
            for (float t = 0f; t < 180f && ui.IsGrading; t += Time.unscaledDeltaTime) yield return null;
            var record = ui.ProductiveRecord;
            Assert.NotNull(record);
            foreach (var task in record.writing)
            {
                Assert.IsTrue(task.examination != null && task.examination.Ok, $"Task {task.task}: {task.examination?.error}");
                Assert.AreEqual(4, task.examination.criteria.Count, $"Task {task.task}: four criteria");
                Assert.IsTrue(task.examination.criteria.All(c => c.band >= 0f && c.band <= 9f && !string.IsNullOrWhiteSpace(c.explanation)), "Bands and explanations");
                Assert.AreEqual(IeltsBands.WritingTask(task.examination.criteria), task.examination.overall, 0.001f, "Task band computed locally");
                Debug.Log($"[IeltsW] Task {task.task}: " + string.Join(", ", task.examination.criteria.Select(c => $"{c.code} {c.band:0.0}")) + $" → {task.examination.overall:0.0}");
            }
            float expected = IeltsBands.WritingOverall(record.writing[0].examination.overall, record.writing[1].examination.overall, true, true);
            Assert.AreEqual(expected, record.overall, 0.001f, "Writing band = (T1 + 2·T2)/3 with IELTS rounding");
            Assert.AreEqual(historyBefore + 1, IeltsAttemptStore.LoadHistory().entries.Count, "Band history gets the original submission");
            yield return null;
            Capture("02_writing_examiner");

            // Coach on the original.
            ui.RequestCoach(2);
            var t2 = record.writing.First(t => t.task == 2);
            for (float t = 0f; t < 120f && (t2.coach == null || t2.coach.overallAdvice == "…"); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(t2.coach.Ok, t2.coach.error);
            Assert.Greater(t2.coach.issues.Count, 0, "The coach finds the planted grammar mistakes");
            Debug.Log("[IeltsW] Coach: " + string.Join(" | ", t2.coach.issues.Take(6).Select(i => $"[{i.sentence}] {i.original} → {i.suggestion}")));
            yield return null;
            Capture("03_writing_coach");

            // Rewrite: coached and saved, never scored.
            float bandBefore = record.overall;
            ui.SubmitRewrite(2, Task2Rewrite);
            var rewrite = t2.rewrites.Last();
            for (float t = 0f; t < 120f && rewrite.coach.overallAdvice == "…" && rewrite.coach.Ok; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(rewrite.coach.Ok, rewrite.coach.error);
            Assert.AreEqual(bandBefore, record.overall, "A rewrite never changes the band");
            Assert.AreEqual(historyBefore + 1, IeltsAttemptStore.LoadHistory().entries.Count, "A rewrite never enters the band history");
            var saved = IeltsProductiveStore.Load(record.id);
            Assert.AreEqual(Task2, saved.writing.First(t => t.task == 2).original, "Original kept");
            Assert.AreEqual(Task2Rewrite, saved.writing.First(t => t.task == 2).rewrites.Last().text, "Rewrite saved");
            yield return null;
            Capture("04_writing_rewrite");

            // Grading the same text again is answered from the cache (no paid request).
            int paid = ExamGradingService.PaidRequestsThisSession;
            IeltsAiExamination again = null;
            grading.ExamineWriting(record.writing[0], r => again = r);
            for (float t = 0f; t < 10f && again == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(again.Ok && again.fromCache, "Cached result");
            Assert.AreEqual(paid, ExamGradingService.PaidRequestsThisSession, "No paid request for a cached answer");
            ui.Close();
        }

        [UnityTest]
        public IEnumerator Writing_OfflineAndDailyLimit_SaveAndExplain()
        {
            yield return LoadCity();
            if (!GameServices.TryGet(out ExamGradingService grading)) Assert.Fail("Grading service");
            string unique = " (draft " + Guid.NewGuid().ToString("N").Substring(0, 6) + ")";
            var ui = Open("nl_writing_academic_1");
            ui.SetAnswer(1, Task1 + unique);
            ui.SetAnswer(2, Task2 + unique);
            ExamGradingService.EndpointOverride = "http://127.0.0.1:9/unreachable";
            try
            {
                ui.SubmitNow();
                for (float t = 0f; t < 120f && ui.IsGrading; t += Time.unscaledDeltaTime) yield return null;
                var record = ui.ProductiveRecord;
                if (grading.IsConfigured)
                {
                    Assert.IsTrue(record.writing.All(w => w.examination != null && !w.examination.Ok), "No band without the AI");
                    StringAssert.Contains("lưu", record.writing[0].examination.error, "The error says the work is saved");
                }
                Assert.AreEqual(0f, record.overall, "No fake band");
                Assert.AreEqual(Task2 + unique, IeltsProductiveStore.Load(record.id).writing[1].original, "The essay is saved");
                Assert.IsTrue(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Any(b => b.name == "Regrade"), "A Regrade button is offered");
                yield return null;
                Capture("05_writing_offline");
            }
            finally { ExamGradingService.EndpointOverride = null; }

            // Daily cost limit: when today's quota is used up, nothing is sent.
            var control = GameServices.TryGet(out GameControlService c) ? c : null;
            int limit = control.Database.aiDailyRequestLimit;
            control.Database.aiDailyRequestLimit = ExamGradingService.RequestsToday;
            try
            {
                int paid = ExamGradingService.PaidRequestsThisSession;
                IeltsAiExamination result = null;
                grading.ExamineWriting(new IeltsWritingTaskRecord { task = 2, prompt = "x", original = Task2 + unique + " limit", minWords = 250 }, r => result = r);
                for (float t = 0f; t < 10f && result == null; t += Time.unscaledDeltaTime) yield return null;
                if (grading.IsConfigured) StringAssert.Contains("lượt chấm AI hôm nay", result.error);
                Assert.AreEqual(paid, ExamGradingService.PaidRequestsThisSession, "Nothing sent over the limit");
            }
            finally { control.Database.aiDailyRequestLimit = limit; }
            ui.Close();
        }

        [UnityTest]
        public IEnumerator Speaking_RecordedSpeech_RealAi_FourCriteria()
        {
            string fixtures = Environment.GetEnvironmentVariable("NL_SPEECH_FIXTURES");
            if (string.IsNullOrEmpty(fixtures) || !Directory.Exists(fixtures)) Assert.Inconclusive("Set NL_SPEECH_FIXTURES to a folder with a1.wav, a2.wav, a5.wav, a6.wav.");
            yield return LoadCity();
            if (!GameServices.TryGet(out ExamGradingService grading) || !grading.IsConfigured) Assert.Inconclusive("Gemini is not configured on this machine.");
            var ui = Open("nl_speaking_1");
            yield return new WaitForSecondsRealtime(0.5f);
            Capture("06_speaking_part1");
            foreach (var (number, file) in new[] { (1, "a1.wav"), (2, "a2.wav"), (5, "a5.wav"), (6, "a6.wav") })
            {
                var samples = SpeechAudio.DecodeWav(File.ReadAllBytes(Path.Combine(fixtures, file)), out int rate);
                Assert.Greater(samples.Length, rate, file);
                ui.StoreTake(number, samples, rate);
            }
            ui.ShowPart(1);
            yield return new WaitForSecondsRealtime(0.5f);
            Capture("07_speaking_part2");
            ui.SubmitNow();
            for (float t = 0f; t < 240f && ui.IsGrading; t += Time.unscaledDeltaTime) yield return null;
            var record = ui.ProductiveRecord;
            var exam = record.speakingExamination;
            Assert.IsTrue(exam != null && exam.Ok, exam?.error);
            foreach (string code in new[] { "FC", "LR", "GRA" })
                Assert.IsTrue(exam.criteria.Any(x => x.code == code && x.assessed), code + " assessed");
            var p = exam.criteria.First(x => x.code == "P");
            Assert.IsFalse(string.IsNullOrWhiteSpace(p.explanation), "Pronunciation verdict is explained (assessed or not)");
            Debug.Log("[IeltsS] " + string.Join(", ", exam.criteria.Select(x => $"{x.code} {(x.assessed ? x.band.ToString("0.0") : "n/a")}")) + $" → {exam.overall:0.0}; P: {p.explanation}");
            var recorded = record.speaking.Where(a => a.audioFile != null).ToList();
            Assert.AreEqual(4, recorded.Count);
            foreach (var a in recorded)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(a.transcript), $"Transcript {a.number}");
                Assert.Greater(a.metrics.speechSeconds, 2f, $"Speech time measured {a.number}");
                Debug.Log($"[IeltsS] {a.number}: {a.metrics.speechSeconds:0.0}s speech, {a.metrics.pauses} pauses, {a.metrics.wordsPerMinute:0} wpm — {a.transcript}");
            }
            StringAssert.Contains("grandmother", recorded.First(a => a.number == 5).transcript.ToLowerInvariant(), "Part 2 transcript matches what was said");
            Assert.AreEqual(IeltsBands.Speaking(exam.criteria), exam.overall, 0.001f, "Speaking band from assessed criteria, IELTS rounding");
            yield return null;
            Capture("08_speaking_result");
            ui.Close();
        }

        /// <summary>The real microphone on this machine: MicRecorder, the Speaking screen, and Agora's device test.</summary>
        [UnityTest]
        public IEnumerator Microphone_Real_MicRecorderSpeakingScreenAndAgora()
        {
            if (!MicRecorder.HasDevice) Assert.Inconclusive("No microphone on this machine.");
            yield return LoadCity();
            Debug.Log("[Mic] devices: " + string.Join(", ", Microphone.devices));
            var recorder = new MicRecorder();
            Assert.IsTrue(recorder.Start(3), recorder.LastError);
            float peakLevel = 0f;
            for (float t = 0f; t < 2f; t += Time.unscaledDeltaTime) { peakLevel = Mathf.Max(peakLevel, recorder.Level); yield return null; }
            var samples = recorder.Stop(out int rate);
            Assert.Greater(samples.Length, rate, "About two seconds captured");
            float peak = samples.Max(Mathf.Abs);
            Debug.Log($"[Mic] {samples.Length / (float)rate:0.0}s at {rate} Hz, peak sample {peak:0.0000}, meter max {peakLevel:0.000}");
            Assert.Greater(peak, 0f, "The device delivers real (non-zero) audio");

            // Through the Speaking screen's own record button.
            var ui = Open("nl_speaking_1");
            yield return new WaitForSecondsRealtime(0.5f);
            var record = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b => b.name == "Record_1");
            Assert.IsTrue(record.interactable, "Record button available");
            record.onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.AreEqual(1, ui.RecordingNumber, "Recording question 1");
            Capture("09_speaking_recording");
            record.onClick.Invoke();
            yield return null;
            StringAssert.StartsWith("rec:", ui.GetAnswer(1), "The take is saved as the answer");
            ui.Close();

            // Agora: engine with the configured App ID, recording-device test with volume callbacks.
            string appId = NihongoLife.Learning.EduMeetingManager.ConfiguredAppId();
            if (string.IsNullOrWhiteSpace(appId)) Assert.Inconclusive("No Agora App ID configured.");
            var engine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
            int volumeCallbacks = 0, maxVolume = 0;
            var handler = new AgoraVolumeHandler(v => { volumeCallbacks++; maxVolume = Mathf.Max(maxVolume, v); });
            try
            {
                int init = engine.Initialize(new Agora.Rtc.RtcEngineContext { appId = appId });
                Assert.AreEqual(0, init, "Agora engine initialised");
                engine.InitEventHandler(handler);
                Assert.AreEqual(0, engine.EnableAudio(), "Agora audio enabled");
                var devices = engine.GetAudioDeviceManager().EnumerateRecordingDevices();
                Debug.Log("[Agora] recording devices: " + string.Join(", ", devices.Select(d => d.deviceName)));
                Assert.Greater(devices.Length, 0, "Agora sees the microphone");
                Assert.AreEqual(0, engine.GetAudioDeviceManager().StartRecordingDeviceTest(200), "Recording device test started");
                for (float t = 0f; t < 3f; t += Time.unscaledDeltaTime) yield return null;
                engine.GetAudioDeviceManager().StopRecordingDeviceTest();
                Debug.Log($"[Agora] volume callbacks {volumeCallbacks}, max volume {maxVolume}");
                Assert.Greater(volumeCallbacks, 0, "Agora reports microphone volume from the real device");
            }
            finally { engine.Dispose(); }
        }

        private sealed class AgoraVolumeHandler : Agora.Rtc.IRtcEngineEventHandler
        {
            private readonly Action<int> _volume;
            public AgoraVolumeHandler(Action<int> volume) => _volume = volume;
            public override void OnAudioVolumeIndication(Agora.Rtc.RtcConnection connection, Agora.Rtc.AudioVolumeInfo[] speakers, uint speakerNumber, int totalVolume) => _volume(totalVolume);
        }

        // ─────────── helpers ───────────

        private static IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return null;
        }

        private static IeltsTestUI Open(string id)
        {
            var package = IeltsLibrary.Discover().FirstOrDefault(p => p.Id == id);
            Assert.NotNull(package, id + " package (StreamingAssets/IELTS)");
            IeltsAttemptStore.ClearAttempt(id);
            return IeltsTestUI.Open(package, IeltsTestUI.PracticeMode, false);
        }

        private static TextMeshProUGUI[] Texts(IeltsTestUI ui) => Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

        private static void Capture(string name)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = cam; canvases[i].planeDistance = cam.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old;
            cam.targetTexture = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
        }
    }
}
