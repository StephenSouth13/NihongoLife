using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Exam.Ielts;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The Speaking Lounge (F8) with this machine's real webcam through Agora: the camera check shows a live picture
    /// inside the lounge card (not hidden behind it), camera on/off works; and the exam mailbox: a graded paper opens
    /// from the mail into the full results, Esc closes the mailbox. Captures: Bao_Cao/ui-redesign (webcam shots only in
    /// LocalContent/_captures, never in the repository).
    /// </summary>
    public class SpeakingLoungeCameraPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/ui-redesign"));
        /// <summary>Shots with the real webcam (a person's face) stay in the gitignored LocalContent folder.</summary>
        private static string PrivateFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "../LocalContent/_captures"));

        [UnityTest]
        public IEnumerator Lounge_CameraCheck_ShowsRealWebcamInsideTheCard()
        {
            if (WebCamTexture.devices.Length == 0) Assert.Inconclusive("No webcam on this machine.");
            yield return LoadCity();
            var meeting = Object.FindFirstObjectByType<EduMeetingManager>();
            Assert.NotNull(meeting, "Speaking lounge");
            meeting.SetOpen(true);
            yield return new WaitForSecondsRealtime(0.6f);
            var check = Buttons().FirstOrDefault(b => b.name == "CameraCheck");
            Assert.NotNull(check, "Camera check button in the lounge");
            // Agora needs an App ID from LocalSecrets/secrets.json (or AGORA_APP_ID); without it this machine cannot run the check.
            if (string.IsNullOrWhiteSpace(NihongoLife.Learning.EduMeetingManager.ConfiguredAppId()))
            {
                meeting.SetOpen(false); // never leave the lounge open for the next test
                Assert.Inconclusive("No Agora App ID on this machine (LocalSecrets/secrets.json or AGORA_APP_ID).");
            }
            check.onClick.Invoke();
            for (float t = 0f; t < 8f && !meeting.IsCameraPreviewing; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(meeting.IsCameraPreviewing, "Camera check started: " + meeting.Status);
            Assert.Greater(meeting.CameraDeviceCount, 0, "Agora sees the webcam");
            float variance = 0f;
            for (float t = 0f; t < 8f && variance < 0.0005f; t += 0.5f)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                variance = Variance(meeting.LocalView != null ? meeting.LocalView.texture : null);
            }
            var view = meeting.LocalView;
            Debug.Log($"[Lounge] camera texture {view.texture?.width}x{view.texture?.height}, variance {variance:0.00000}, status: {meeting.Status}");
            Assert.NotNull(view.texture, "Agora draws the camera into the lounge picture");
            Assert.Greater(variance, 0.0005f, "A real image (not a black frame)");
            Assert.IsTrue(view.isActiveAndEnabled, "The picture is visible");
            // The picture is on top of the lounge card (same canvas, later sibling), so nothing covers it.
            Assert.AreEqual(view.canvas.rootCanvas, Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).First(r => r.name == "LoungeCard").GetComponentInParent<Canvas>().rootCanvas);
            Capture("lounge_camera_check", PrivateFolder);

            Buttons().First(b => b.name == "Camera").onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(meeting.IsCameraOn);
            Assert.IsFalse(view.enabled, "Camera off: placeholder instead of the picture");
            Capture("lounge_camera_off", PrivateFolder);
            Buttons().First(b => b.name == "Camera").onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(meeting.IsCameraOn);
            Buttons().First(b => b.name == "StopCheck").onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(meeting.IsCameraPreviewing, "Camera closed");
            meeting.SetOpen(false);
        }

        [UnityTest]
        public IEnumerator Mailbox_OpensFullResult_EscCloses()
        {
            yield return LoadCity();
            var record = IeltsProductiveStore.All(null).LastOrDefault(r => r.skill == "writing" && r.writing.Any(w => w.examination != null && w.examination.Ok));
            bool created = false;
            if (record == null)
            {
                record = new IeltsProductiveRecord
                {
                    id = IeltsProductiveStore.NewId("nl_writing_academic_1"), testId = "nl_writing_academic_1", testTitle = "IELTS Writing (test mail)", skill = "writing",
                    mode = "practice", finishedUtc = DateTime.UtcNow.ToString("o"), overall = 6.5f,
                };
                record.writing.Add(new IeltsWritingTaskRecord
                {
                    number = 2, task = 2, prompt = "Discuss both views.", original = "A short test essay.", words = 4, minWords = 250,
                    examination = new IeltsAiExamination { overall = 6.5f, summary = "Test summary.", criteria = new List<IeltsCriterionScore> { new IeltsCriterionScore { code = "TR", name = "Task Response", band = 6.5f, explanation = "x" } } },
                });
                IeltsProductiveStore.Save(record);
                created = true;
            }
            try
            {
                var mailbox = Object.FindFirstObjectByType<ExamMailboxUI>();
                Assert.NotNull(mailbox, "Exam mailbox");
                Buttons(includeInactive: true).First(b => b.name == "NotificationBell").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                var mail = Buttons().FirstOrDefault(b => b.name == "Mail_" + record.id) ?? Buttons().First(b => b.name.StartsWith("Mail_"));
                string mailId = mail.name.Substring(5);
                mail.onClick.Invoke();
                yield return null;
                var open = Buttons().FirstOrDefault(b => b.name == "OpenFullResult");
                Assert.IsTrue(open != null && open.gameObject.activeInHierarchy, "\"Xem bài đầy đủ\" appears for the selected mail");
                Capture("mailbox_detail");
                Assert.IsTrue(UiModalStack.CloseTop(), "Esc closes the mailbox");
                yield return null;
                Assert.IsFalse(Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Any(r => r.name == "MailboxWindow" && r.gameObject.activeInHierarchy));

                Buttons(includeInactive: true).First(b => b.name == "NotificationBell").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.3f);
                Buttons().First(b => b.name == "Mail_" + mailId).onClick.Invoke();
                yield return null;
                Buttons().First(b => b.name == "OpenFullResult").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.6f);
                var ui = IeltsTestUI.Instance;
                Assert.IsTrue(ui != null && ui.IsOpen, "The paper opens in the test screen");
                Assert.AreEqual(mailId, ui.ProductiveRecord?.id, "With that mail's results");
                Assert.IsTrue(Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Any(t => t.name == "OverallBand"), "Full results shown");
                Capture("mailbox_full_result");
                ui.Close();
            }
            finally
            {
                if (created) File.Delete(Path.Combine(IeltsProductiveStore.Folder, record.id + ".json"));
            }
        }

        // ─────────── helpers ───────────

        private static IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private static Button[] Buttons(bool includeInactive = false) =>
            Object.FindObjectsByType<Button>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        /// <summary>Brightness variance of a (possibly non-readable) texture, through a small render target.</summary>
        private static float Variance(Texture texture)
        {
            if (texture == null || texture.width < 8) return 0f;
            var rt = RenderTexture.GetTemporary(64, 36, 0);
            Graphics.Blit(texture, rt);
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(64, 36, TextureFormat.RGB24, false);
            read.ReadPixels(new Rect(0, 0, 64, 36), 0, 0);
            read.Apply();
            RenderTexture.active = old;
            RenderTexture.ReleaseTemporary(rt);
            var px = read.GetPixels();
            Object.Destroy(read);
            float mean = px.Average(c => c.grayscale);
            return px.Average(c => (c.grayscale - mean) * (c.grayscale - mean));
        }

        private static void Capture(string name, string folder = null)
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
            Directory.CreateDirectory(folder ?? Folder);
            File.WriteAllBytes(Path.Combine(folder ?? Folder, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old;
            cam.targetTexture = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
        }
    }
}
