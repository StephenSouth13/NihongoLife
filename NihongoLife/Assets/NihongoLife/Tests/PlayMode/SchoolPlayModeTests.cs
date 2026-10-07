using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Exam;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.School;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The Hibari classroom reacts to the loaded exams: city → school portal → welcome banner → walk to the
    /// exam desk → F opens the exam centre → start JLPT N5 (lights dim, spotlight) → answer + submit →
    /// result banner, Morita's comment and the best score on the blackboard. Captures: Bao_Cao/school-regression.
    /// </summary>
    public class SchoolPlayModeTests
    {
        [UnityTest]
        public IEnumerator Classroom_ExamDeskRunsAnExamWithFeedback()
        {
            PlayerPrefs.DeleteKey("NL.Classroom.BestScore");
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }

            GameObject.Find("AdditiveZonePortals/SchoolPortal").GetComponent<ScenePortal>().Interact(player.gameObject);
            yield return null;
            yield return WaitUntil(() => !flow.IsLoading, 30f, "School transition timed out.");
            Assert.AreEqual(WorldLocationCatalog.SchoolScene, SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(1.2f);
            var room = ClassroomRuntime.Instance;
            Assert.NotNull(room, "The classroom must have its runtime.");
            Assert.NotNull(GameObject.Find("ClassroomFX/Banner"), "Walking in shows the welcome banner.");
            Capture("01_welcome");

            var desk = Object.FindFirstObjectByType<ClassroomExamDesk>();
            Assert.NotNull(desk);
            Vector3 front = desk.transform.position + Vector3.back * 0.6f; front.y = 0.08f;
            Teleport(player, front, 0f);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreSame(desk, player.GetComponent<InteractionDetector>().CurrentInteractable as ClassroomExamDesk, "F at the exam desk must offer the exam.");
            Capture("02_exam_desk");
            desk.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.6f);
            var center = Object.FindFirstObjectByType<ExamCenterPopup>();
            Assert.IsTrue(center.IsOpen, "The desk opens the exam centre.");
            Capture("03_exam_centre");

            var exam = Resources.LoadAll<ExamDefinition>("Exams").First(e => e.examType == ExamType.Jlpt);
            typeof(ExamCenterPopup).GetMethod("StartExam", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(center, new object[] { exam });
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(ExamManager.Instance.IsAttemptActive);
            Assert.IsTrue(room.ExamCeremonyActive, "Starting an exam dims the room and lights the desk.");
            Capture("04_exam_started");

            var manager = ExamManager.Instance;
            for (int guard = 0; guard < 20 && manager.IsAttemptActive; guard++)
            {
                var section = manager.CurrentSection;
                for (int q = 0; q < section.questions.Count; q++)
                {
                    manager.GoToQuestion(q);
                    if (section.questions[q].choices.Count > 0) manager.SubmitChoice(0);
                }
                manager.SubmitCurrentSection();
                yield return null;
            }
            yield return WaitUntil(() => room.ResultsShown > 0, 20f, "Submitting must show the classroom result.");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(room.ExamCeremonyActive, "Lights come back after the exam.");
            Capture("05_result");
            yield return WaitUntil(() => dm.IsOpen, 5f, "Morita comments on the result.");
            Capture("06_teacher_comment");
            for (int guard = 0; guard < 10 && dm.IsOpen; guard++) { dm.CancelDialogue(); yield return null; }
            var board = GameObject.Find("Chalkboard/BestScore").GetComponent<TMPro.TextMeshPro>();
            StringAssert.Contains("さいこうてん", board.text, "The blackboard shows the best score.");
            foreach (var ui in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(m => m.GetType().Name == "ExamPlayUI"))
                ui.SendMessage("Hide", SendMessageOptions.DontRequireReceiver);

            var orbit = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            Teleport(player, new Vector3(1000f, 0.08f, -4.6f), 0f);
            orbit?.SetOrbit(0f, 14f, 4.5f);
            yield return new WaitForSecondsRealtime(1.5f);
            Capture("07_classroom");
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds, string message)
        {
            float timeout = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(condition(), message);
        }

        private static void Teleport(PlayerController player, Vector3 position, float yaw)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            Object.FindFirstObjectByType<ThirdPersonCameraController>()?.SetOrbit(yaw, 18f, 3.4f);
        }

        private static void Capture(string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/school-regression"));
            Directory.CreateDirectory(folder);
            var camera = Camera.main;
            if (camera == null) return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(x => x.isRootCanvas && x.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(x => x.sortingOrder).ToArray();
            var target = new RenderTexture(1600, 900, 24);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
            Canvas.ForceUpdateCanvases();
            var oldTarget = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            var oldActive = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            Object.Destroy(texture);
            target.Release();
            Object.Destroy(target);
        }
    }
}
