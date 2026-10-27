using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Clinic;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Hibari Clinic in the real game: walking in from the town and through to the consultation room; a visit as a
    /// patient (reception, questionnaire, doctor, bill, medicine — the first-visit task completes, health is full and
    /// the cold is gone); the clinic shift (check-in, triage, medicine, report — paid); the health check and the
    /// vaccination tasks; fainting wakes the player on the clinic's recovery bed. Captures: Bao_Cao/clinic.
    /// </summary>
    public class ClinicPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/clinic"));
        private static readonly Vector3 O = new Vector3(650f, 0f, 0f);

        [UnityTest, Timeout(300000)]
        public IEnumerator Clinic_VisitShiftChecksAndFaint()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var status = PlayerStatus.Instance;
            var inventory = PlayerInventory.Instance;
            inventory.AddYen(5000);
            ClinicVisit.Reset();

            // The entrance on Sakura-dōri.
            var portal = GameObject.Find("AdditiveZonePortals/ClinicPortal");
            Assert.NotNull(portal, "Clinic portal in the town");
            Teleport(player, new Vector3(portal.transform.position.x, 0.1f, portal.transform.position.z + 4.5f), 180f); // on the pavement, facing the entrance
            Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>()?.SetOrbit(180f, 14f, 7f);
            yield return new WaitForSecondsRealtime(0.8f);
            Capture("01_entrance");
            portal.GetComponent<ScenePortal>().Interact(player.gameObject);
            yield return null;
            for (float t = 0f; t < 30f && flow.IsLoading; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(WorldLocationCatalog.ClinicScene, SceneManager.GetActiveScene().name, "Inside the clinic");
            Capture("02_waiting_room");

            // Walk from the door round the counter and along the corridor into the consultation room.
            yield return Walk(player, O + new Vector3(0f, 0.1f, -6.6f), O + new Vector3(3.05f, 0f, -6.6f), 2f);
            yield return Walk(player, player.transform.position, O + new Vector3(3.05f, 0f, 0.2f), 4f);
            yield return Walk(player, player.transform.position, O + new Vector3(-3.8f, 0f, 0.2f), 4f);
            yield return Walk(player, player.transform.position, O + new Vector3(-3.8f, 0f, 3.2f), 2.5f);
            Assert.Greater(player.transform.position.z, O.z + 2.5f, "Walked into the consultation room: " + player.transform.position);
            Assert.Less(player.transform.position.x, O.x - 2f);

            // A patient visit: soaked (a cold) and weak.
            Assert.IsNull(QuestService.Accept(ClinicVisit.FirstVisitQuest), "First-visit task");
            status.DebugSetNeeds(health: 35f, wetness: 0.9f);
            Assert.IsTrue(status.HasCold);
            Teleport(player, O + new Vector3(-3f, 0.1f, -4.6f), 0f);
            yield return null;
            ClinicVisit.Use(ClinicSpotKind.Reception);
            Assert.IsTrue(JobQuizCard.IsOpen, "Reception asks for the insurance card");
            Capture("03_reception");
            AnswerWrongThenRight();
            yield return null;
            Assert.IsTrue(JobQuizCard.IsOpen, "The questionnaire");
            Capture("04_questionnaire");
            AnswerRight();
            yield return null;
            Assert.AreEqual(ClinicVisit.Stage.CheckedIn, ClinicVisit.Current);
            Assert.IsTrue(ClinicVisit.Symptom.cold, "Soaked: a cold symptom");

            Teleport(player, O + new Vector3(-5.6f, 0.1f, 4.2f), 270f);
            yield return new WaitForSecondsRealtime(0.4f);
            ClinicVisit.Use(ClinicSpotKind.Doctor);
            Capture("05_doctor");
            AnswerRight();
            yield return null;
            Assert.AreEqual(status.MaxHealth, status.CurrentHealth, 0.01f, "The doctor heals fully");
            Assert.IsFalse(status.HasCold, "The cold is cured");

            int yen = inventory.Yen;
            ClinicVisit.Use(ClinicSpotKind.Reception);
            AnswerRight();
            yield return null;
            Assert.AreEqual(yen - ClinicCatalog.Load().visitFeeYen, inventory.Yen, "The bill (30 % with insurance)");
            ClinicVisit.Use(ClinicSpotKind.Pharmacy);
            Capture("06_medicine");
            AnswerRight();
            yield return null;
            Assert.AreEqual("completed", QuestService.State(ClinicVisit.FirstVisitQuest).status, "First visit done");

            // The clinic shift: patients appear, check-in, triage, medicine, report to Dr Mori.
            Assert.IsNull(QuestService.Accept("job_clinic_shift"), "Clinic shift");
            yield return null;
            var stations = Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).Where(s => s.QuestId == "job_clinic_shift" && s.Kind != JobStationKind.Board).ToList();
            Assert.AreEqual(5, stations.Count, "Five patients");
            yield return null;
            Capture("07_shift");
            yen = inventory.Yen;
            foreach (var station in stations.OrderBy(s => s.Kind))
            {
                Assert.IsTrue(station.IsInteractionAvailable, station.name);
                station.Interact(player.gameObject);
                AnswerWrongThenRight();
                yield return null;
            }
            Assert.IsTrue(QuestService.ReadyToReport("job_clinic_shift"));
            ClinicVisit.Use(ClinicSpotKind.Doctor);
            yield return null;
            Assert.AreEqual("completed", QuestService.State("job_clinic_shift").status);
            Assert.AreEqual(yen + 750, inventory.Yen, "Shift pay");

            // Health check: reception, height, three eye rounds, the doctor's result.
            Assert.IsNull(QuestService.Accept(ClinicVisit.HealthCheckQuest));
            ClinicVisit.Use(ClinicSpotKind.Reception); AnswerRight(); yield return null; AnswerRight(); yield return null;
            ClinicVisit.Use(ClinicSpotKind.Scale); AnswerRight(); yield return null;
            for (int i = 0; i < 3; i++)
            {
                ClinicVisit.Use(ClinicSpotKind.EyeChart);
                if (i == 0) Capture("08_eye_test");
                AnswerRight();
                yield return null;
            }
            ClinicVisit.Use(ClinicSpotKind.Doctor);
            yield return null;
            Assert.AreEqual("completed", QuestService.State(ClinicVisit.HealthCheckQuest).status, "Health check done");

            // Vaccination: reception, the doctor, the bill.
            Assert.IsNull(QuestService.Accept(ClinicVisit.VaccineQuest));
            ClinicVisit.Use(ClinicSpotKind.Reception); AnswerRight(); yield return null; AnswerRight(); yield return null;
            ClinicVisit.Use(ClinicSpotKind.Doctor); AnswerRight(); yield return null;
            ClinicVisit.Use(ClinicSpotKind.Reception); AnswerRight(); yield return null;
            Assert.AreEqual("completed", QuestService.State(ClinicVisit.VaccineQuest).status, "Vaccination done");

            // Back to the town; fainting there wakes the player on the clinic's recovery bed.
            GameObject.Find("HibariClinic_Zone/ExitToCity").GetComponent<ScenePortal>().Interact(player.gameObject);
            yield return null;
            for (float t = 0f; t < 30f && flow.IsLoading; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(WorldLocationCatalog.CityScene, SceneManager.GetActiveScene().name);
            var consequences = status.GetComponent<StatusConsequences>();
            status.DebugSetNeeds(health: 0f);
            for (float t = 0f; t < 3f && !consequences.IsBusy; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(consequences.IsBusy, "Fainting");
            for (float t = 0f; t < 40f && consequences.IsBusy; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(WorldLocationCatalog.ClinicScene, SceneManager.GetActiveScene().name, "Woke up in the clinic");
            var bed = Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None).First(s => s.Id == WorldLocationCatalog.ClinicBed);
            Assert.Less(Vector3.Distance(player.transform.position, bed.transform.position), 1.5f, "On the recovery bed");
            Assert.IsFalse(player.InputLocked, "Back in control");
            yield return new WaitForSecondsRealtime(0.5f);
            Capture("09_woke_in_clinic");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Clinic_TownspeopleComeForACheckUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            GameObject.Find("AdditiveZonePortals/ClinicPortal").GetComponent<ScenePortal>().Interact(player.gameObject);
            yield return null;
            for (float t = 0f; t < 30f && flow.IsLoading; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(WorldLocationCatalog.ClinicScene, SceneManager.GetActiveScene().name);

            var patients = Object.FindFirstObjectByType<ClinicPatientFlow>();
            Assert.NotNull(patients, "The clinic has its townspeople");
            Assert.GreaterOrEqual(patients.Count, 2, "People already wait when you walk in");
            var people = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Townsperson_")).ToArray();
            Assert.IsTrue(people.All(p => p.GetComponentsInChildren<Collider>().Length == 0), "Townspeople never block the player");
            Teleport(player, O + new Vector3(1.2f, 0.1f, -6.4f), 50f);
            Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>()?.SetOrbit(40f, 18f, 6.5f);
            yield return new WaitForSecondsRealtime(1f);
            Capture("10_townspeople_waiting");

            // Faster clock: the first person is called, sees Dr Mori, takes the medicine and goes home.
            float scale = Time.timeScale;
            try
            {
                Time.timeScale = 4f;
                for (float t = 0f; t < 40f && patients.Called < 1; t += Time.unscaledDeltaTime) yield return null;
                Assert.GreaterOrEqual(patients.Called, 1, "A number is called");
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(0.4f);
                Capture("11_number_called");
                Time.timeScale = 4f;
                for (float t = 0f; t < 25f && !Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None).Any(x => x.name == "Speech" && x.gameObject.activeInHierarchy && x.transform.parent.name == "Doctor_Mori"); t += Time.unscaledDeltaTime) yield return null;
                Time.timeScale = 1f;
                Teleport(player, O + new Vector3(-3.6f, 0.1f, 2.6f), 240f);
                Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>()?.SetOrbit(240f, 14f, 4.5f);
                yield return new WaitForSecondsRealtime(0.8f);
                Capture("12_consultation");
                Time.timeScale = 4f;
                for (float t = 0f; t < 60f && patients.Treated < 1; t += Time.unscaledDeltaTime) yield return null;
                Assert.GreaterOrEqual(patients.Treated, 1, "Someone got their medicine and left");
            }
            finally
            {
                Time.timeScale = scale;
            }

            // Elders look their age: a beard / lines on the head bone, grey hair.
            var elder = GameObject.FindObjectsByType<NihongoLife.Player.CharacterLook>(FindObjectsSortMode.None).FirstOrDefault(l => l.name.StartsWith("Townsperson_") && l.Age >= 60);
            if (elder != null) Assert.Less(elder.Walk, 1f, "An elder walks slower");
        }

        private static void AnswerRight() => JobQuizCard.Answer(JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct));

        private static void AnswerWrongThenRight()
        {
            int wrong = JobQuizCard.CurrentChoices.ToList().FindIndex(c => !c.Correct);
            if (wrong >= 0) { JobQuizCard.Answer(wrong); Assert.IsTrue(JobQuizCard.IsOpen, "A wrong answer is explained, not accepted"); }
            AnswerRight();
        }

        private static void Teleport(PlayerController player, Vector3 position, float yaw)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            cc.enabled = true;
        }

        /// <summary>Walks with the real CharacterController towards <paramref name="to"/> at walking speed.</summary>
        private static IEnumerator Walk(PlayerController player, Vector3 from, Vector3 to, float seconds)
        {
            var cc = player.GetComponent<CharacterController>();
            if ((player.transform.position - from).sqrMagnitude > 0.01f) Teleport(player, from, 0f);
            yield return null;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Vector3 d = to - player.transform.position; d.y = 0f;
                if (d.magnitude < 0.15f) break;
                cc.Move(d.normalized * 2.4f * Time.deltaTime + Vector3.down * 0.5f * Time.deltaTime);
                yield return null;
            }
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
