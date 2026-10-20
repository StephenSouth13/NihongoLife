using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Exam;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The redesigned exam screens, photographed in each interface language: exam centre (JLPT / IELTS), JLPT
    /// question + results, the live Speaking room (student waiting, student in the exam, examiner console, marking,
    /// result), real-time toasts and the results mailbox. Also checks that one screen never mixes languages in its
    /// interface text. Captures: Bao_Cao/ui-redesign-v2/*.jpg
    /// </summary>
    public class ExamUiShowcasePlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/ui-redesign-v2"));
        private static string IeltsFolder => Path.Combine(Application.persistentDataPath, "ielts");
        private static string IeltsBackup => Path.Combine(Application.temporaryCachePath, "ielts.before-showcase");

        [UnityTest, Timeout(300000)]
        public IEnumerator ExamScreens_AllLanguages()
        {
            BackupIelts();
            try
            {
                yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
                var dm = NihongoLife.Dialogue.DialogueManager.Instance;
                for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }

                foreach (var language in new[] { GameLanguage.Vietnamese, GameLanguage.English, GameLanguage.Japanese })
                {
                    Loc.Set(language);
                    string code = Loc.Code;
                    yield return null;

                    // Exam centre
                    var center = Object.FindFirstObjectByType<ExamCenterPopup>();
                    Assert.NotNull(center, "Exam centre in the HUD");
                    center.Show();
                    typeof(ExamCenterPopup).GetMethod("SelectTab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(center, new object[] { ExamType.Jlpt });
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Capture($"{code}_01_center_jlpt");
                    typeof(ExamCenterPopup).GetMethod("SelectTab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(center, new object[] { ExamType.Ielts });
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Capture($"{code}_02_center_ielts");
                    AssertOneLanguage(center.transform.Find("ExamCenterPopupPanel") ?? center.transform, language, "exam centre");
                    center.Hide();

                    // JLPT question
                    var exam = Resources.Load<ExamDefinition>("Exams/jlpt_n5_mock_2");
                    var manager = ExamManager.Instance;
                    manager.StartAttempt(exam);
                    var ui = ExamPlayUI.GetOrCreate(NLUi.CreateCanvas("ExamHostCanvas_" + code, 400).transform);
                    ui.Show();
                    manager.SubmitChoice(1);
                    yield return new WaitForSecondsRealtime(0.6f);
                    yield return Capture($"{code}_03_jlpt_question");
                    for (int guard = 0; guard < 6 && manager.IsAttemptActive; guard++)
                    {
                        var section = manager.CurrentSection;
                        for (int q = 0; q < section.questions.Count; q++) { manager.GoToQuestion(q); manager.SubmitChoice(section.questions[q].correctChoiceIndex); }
                        manager.SubmitCurrentSection();
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(1.4f);
                    yield return Capture($"{code}_04_jlpt_results");
                    ui.SendMessage("Hide", SendMessageOptions.DontRequireReceiver);
                    yield return null;

                    // Live Speaking room
                    var meeting = EduMeetingManager.Instance;
                    Assert.NotNull(meeting, "Speaking lounge");
                    meeting.OpenForSpeakingExam("nl_speaking_1", "IELTS Speaking · NL-1");
                    var teacherRoom = new AgoraWaitingRoom { id = "t1", creator_role = "teacher", teacher_name = "Morita", status = "waiting" };
                    meeting.PreviewLobby(false, new[] { teacherRoom });
                    yield return new WaitForSecondsRealtime(0.6f);
                    yield return Capture($"{code}_05_lounge_student");
                    meeting.PreviewLobby(false, new[] { teacherRoom }, null, waiting: true);
                    yield return new WaitForSecondsRealtime(0.6f);
                    yield return Capture($"{code}_05b_lounge_student_waiting");
                    meeting.PreviewLobby(true, new[]
                    {
                        new AgoraWaitingRoom { id = "r1", student_name = "Long Quách", level = 3, xp = 820, status = "waiting", creator_role = "student", bio = "Mình đang ôn IELTS Speaking để du học Nhật. Muốn luyện Part 2 nhiều hơn." },
                        new AgoraWaitingRoom { id = "r2", student_name = "Yui Tanaka", level = 5, xp = 1460, status = "waiting", creator_role = "student" },
                    }, new[] { new AgoraJoinRequest { id = "q1", student_name = "Haruto", level = 2 } });
                    yield return new WaitForSecondsRealtime(0.6f);
                    yield return Capture($"{code}_05c_teacher_lobby");
                    AssertOneLanguage(GameObject.Find("SpeakingLounge").transform, language, "teacher lobby");
                    meeting.EndPreview();

                    meeting.PreviewCall(teacher: false);
                    var hub = meeting.Live;
                    hub.ApplyLocal(new SpeakingLiveState
                    {
                        part = 2, index = 5, phase = "prep", question = "Describe a time when you learned something useful outside school.",
                        cuePoints = new[] { "what you learned", "where and how you learned it", "who helped you", "and explain why it was useful to you." },
                        endsUtc = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0 + 42, asked = 5, total = 9, examinerName = "Morita",
                    });
                    yield return new WaitForSecondsRealtime(0.7f);
                    yield return Capture($"{code}_06_lounge_student_exam");
                    AssertOneLanguage(GameObject.Find("SpeakingLounge").transform, language, "speaking room");

                    LiveToast.Show(LiveToast.Tone.Success, "how_to_reg", "Giáo viên đã nhận lớp", "A teacher accepted your room", "先生が受け入れました",
                        "Morita đang vào phòng — đang nối máy…", "Morita is joining — connecting…", "森田先生が入室中です。接続しています…",
                        "Mở phòng", "Open room", "部屋を開く", () => { }, 8f, "showcase-1");
                    LiveToast.Show(LiveToast.Tone.Live, "record_voice_over", "Học viên chờ thi Speaking", "A student is waiting for a Speaking exam", "スピーキング試験の受験者が待っています",
                        "Long (Level 3) · IELTS Speaking · NL-1", "Long (Level 3) · IELTS Speaking · NL-1", "Long（レベル3）· IELTS Speaking · NL-1", "Xem phòng", "View room", "部屋を見る", () => { }, 8f, "showcase-2");
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return Capture($"{code}_07_live_toasts");
                    Assert.GreaterOrEqual(LiveToast.VisibleCount, 2, "Toasts on screen");

                    hub.ApplyLocal(null, new SpeakingLiveResult { testId = "nl_speaking_1", testTitle = "IELTS Speaking · NL-1", examinerName = "Morita", fc = 6, lr = 7, gra = 6, p = 7, overall = 6.5f,
                        comment = "Clear ideas and good range of vocabulary. Work on longer answers in Part 3 and linking words.", sentUtc = System.DateTime.UtcNow.ToString("o") });
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return Capture($"{code}_08_lounge_student_result");

                    meeting.EndPreview();
                    hub.ResetExam();
                    meeting.PreviewCall(teacher: true);
                    hub.ApplyLocal(new SpeakingLiveState { part = 1, index = 2, phase = "question", question = "What do you like most about your neighbourhood?", asked = 2, total = 9 });
                    yield return new WaitForSecondsRealtime(0.7f);
                    yield return Capture($"{code}_09_examiner_console");
                    UnityEngine.UI.Button mark = null;
                    for (float t = 0f; t < 3f && mark == null; t += Time.unscaledDeltaTime)
                    {
                        mark = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Mark" && b.gameObject.activeInHierarchy);
                        yield return null;
                    }
                    Assert.NotNull(mark, "Examiner can finish and mark");
                    mark.onClick.Invoke();
                    yield return new WaitForSecondsRealtime(0.7f);
                    yield return Capture($"{code}_10_examiner_marking");
                    meeting.EndPreview();
                    meeting.SetOpen(false);
                    meeting.ClearExam();
                    hub.ResetExam();

                    // Mailbox
                    var mailbox = Object.FindFirstObjectByType<ExamMailboxUI>();
                    mailbox.SetOpen(true);
                    yield return new WaitForSecondsRealtime(0.4f);
                    var mail = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name.StartsWith("Mail_"));
                    if (mail != null) mail.onClick.Invoke();
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Capture($"{code}_11_mailbox");
                    mailbox.SetOpen(false);

                    // IELTS papers
                    foreach (var id in new[] { "nl_speaking_1", "nl_writing_academic_1" })
                    {
                        var package = NihongoLife.Exam.Ielts.IeltsLibrary.Discover().First(p => p.Id == id);
                        var paper = IeltsTestUI.Open(package, IeltsTestUI.PracticeMode, resume: false);
                        yield return new WaitForSecondsRealtime(0.8f);
                        yield return Capture($"{code}_12_ielts_{id}");
                        paper.Close();
                    }
                }
            }
            finally
            {
                Loc.Set(GameLanguage.Vietnamese);
                RestoreIelts();
            }
        }

        /// <summary>Interface text of one language only: no Vietnamese letters on an English/Japanese screen, no kana on an English one.</summary>
        private static void AssertOneLanguage(Transform root, GameLanguage language, string where)
        {
            var bad = root.GetComponentsInChildren<TMP_Text>(false).Where(t => t.GetComponentInParent<UiUserContent>() == null).Select(t => TextHighlighter.StripTags(t.text)).Where(text =>
            {
                if (string.IsNullOrWhiteSpace(text)) return false;
                bool vi = text.Any(c => "ăâđêôơưạảấầẩẫậắằẳẵặẹẻẽếềểễệịỉĩọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹ".IndexOf(char.ToLowerInvariant(c)) >= 0);
                return language != GameLanguage.Vietnamese && vi;
            }).ToList();
            Assert.IsEmpty(bad, $"{where} ({language}) shows Vietnamese interface text: " + string.Join(" | ", bad.Take(8)));
        }

        private static void BackupIelts()
        {
            if (Directory.Exists(IeltsBackup)) Directory.Delete(IeltsBackup, true);
            if (Directory.Exists(IeltsFolder)) CopyDir(IeltsFolder, IeltsBackup);
        }

        private static void RestoreIelts()
        {
            if (Directory.Exists(IeltsFolder)) Directory.Delete(IeltsFolder, true);
            if (Directory.Exists(IeltsBackup)) { CopyDir(IeltsBackup, IeltsFolder); Directory.Delete(IeltsBackup, true); }
        }

        private static void CopyDir(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (string file in Directory.GetFiles(from)) File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(from)) CopyDir(dir, Path.Combine(to, Path.GetFileName(dir)));
        }

        /// <summary>Switches the overlay canvases to the camera, lets one frame rebuild layout, then renders 1600×900.</summary>
        private static IEnumerator Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) yield break;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            // World signs use TMP's overlay shader, which draws over a camera-space canvas (never over the real
            // overlay canvas in game): hide them for the capture so full-screen windows look as players see them.
            var signs = Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None).Where(t => t.enabled).ToArray();
            foreach (var sign in signs) sign.enabled = false;
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
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old;
            camera.targetTexture = null;
            target.Release();
            Object.Destroy(target);
            Object.Destroy(tex);
            foreach (var c in canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            foreach (var sign in signs) if (sign != null) sign.enabled = true;
        }
    }
}
