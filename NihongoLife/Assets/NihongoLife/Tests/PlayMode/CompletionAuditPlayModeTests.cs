using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.Scenario;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    public class CompletionAuditPlayModeTests
    {
        [UnityTest]
        public IEnumerator ActualSprint_EmptySave_NarrationCloseAndResume()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var dialogue = DialogueManager.Instance;
            Assert.NotNull(player);
            Assert.NotNull(dialogue);
            if (dialogue.IsOpen) Assert.IsTrue(dialogue.CancelDialogue());
            ScenarioManager.Instance.SetPlayerInputLocked(false);

            // Drive the actual controller through its input service, rather than spending energy in the test.
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = new Vector3(0f, 0.08f, -30f);
            body.enabled = true;
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            camera.SetOrbit(0f, 18f, 4f);
            var status = PlayerStatus.Instance;
            status.RestoreNeeds(100f, 100f, 100f);
            var input = GameInputService.GetOrCreate();
            float energy = status.CurrentEnergy;
            Vector3 start = player.transform.position;
            input.SetMobileMove(Vector2.up);
            input.SetMobileButton(GameInputId.Sprint, true);
            try
            {
                yield return new WaitForSeconds(2f);
                Assert.Greater(Vector3.Distance(start, player.transform.position), 5f, "The player must actually run.");
                Assert.Less(status.CurrentEnergy, energy - 20f, "Running through the controller must drain stamina.");
                Assert.IsTrue(status.IsSprinting);
                Assert.IsTrue(body.isGrounded, "Running must stay on the street collider.");
                Capture("01_actual_sprint");
                status.ConsumeEnergy(1000f);
                Assert.IsTrue(status.IsExhausted, "Exhaustion must start immediately on the last stamina point.");
                Assert.IsFalse(status.CanSprint);
                start = player.transform.position;
                yield return new WaitForSeconds(1f);
                Assert.IsFalse(status.IsSprinting);
                Assert.Less(Vector3.Distance(start, player.transform.position), 5f, "Holding Sprint while empty must walk.");
            }
            finally
            {
                input.SetMobileMove(Vector2.zero);
                input.SetMobileButton(GameInputId.Sprint, false);
            }
            yield return new WaitForSeconds(4f);
            Assert.IsTrue(status.CanSprint, "Standing still must recover enough stamina to run again.");

            // A saved zero is a real depleted need, not a missing/default value.
            var repository = GameServices.Get<IProgressRepository>();
            var saved = JsonUtility.FromJson<PlayerProgressDto>(JsonUtility.ToJson(repository.GetProgress()));
            var depleted = JsonUtility.FromJson<PlayerProgressDto>(JsonUtility.ToJson(saved));
            depleted.energy = depleted.hunger = depleted.thirst = 0f;
            repository.SaveProgress(depleted);
            try
            {
                status.ReloadProgress();
                Assert.AreEqual(0f, status.CurrentEnergy);
                Assert.AreEqual(0f, status.Hunger);
                Assert.AreEqual(0f, status.Thirst);
                Assert.IsFalse(status.CanSprint);
            }
            finally { repository.SaveProgress(saved); status.ReloadProgress(); }

            // Even a system narrator must have a close button, while keeping its story node intact.
            var story = ScriptableObject.CreateInstance<ScenarioDefinition>();
            story.id = "audit.close_resume";
            story.titleJa = "テスト"; story.titleEn = "Close and resume"; story.titleVi = "Đóng và tiếp tục";
            story.repeatable = true;
            var node = new ScenarioNode { id = "narration", speakerId = "system", speakerName = "NihongoLife",
                nodeType = ScenarioNodeType.Dialogue, textJa = "ひばり町へようこそ。", textEn = "Có thể đóng và tiếp tục hội thoại.", nextNodeId = "done" };
            story.nodes.Add(node);
            story.nodes.Add(new ScenarioNode { id = "done", nodeType = ScenarioNodeType.Complete });
            story.startNodeId = node.id;
            var scenario = ScenarioManager.Instance;
            scenario.StartScenario(story);
            yield return new WaitForSeconds(0.3f);
            Assert.AreSame(node, scenario.CurrentNode);
            Assert.IsTrue(dialogue.CanLeave);
            var view = Object.FindFirstObjectByType<DialogueView>();
            var close = view.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text == "×");
            Capture("02_narration_close");
            close.onClick.Invoke();
            yield return null;
            Assert.IsFalse(dialogue.IsOpen);
            Assert.IsFalse(player.InputLocked);
            Assert.AreSame(node, scenario.CurrentNode, "Closing must not advance, complete or abandon the story.");
            Assert.IsTrue(dialogue.IsStoryPaused);
            var resume = view.GetComponentsInChildren<Button>().Single(b => b.name == "ResumeDialogue");
            Assert.IsTrue(resume.gameObject.activeInHierarchy);
            resume.onClick.Invoke();
            yield return null;
            Assert.IsTrue(dialogue.IsOpen);
            Assert.IsTrue(player.InputLocked);
            Assert.AreSame(node, scenario.CurrentNode);
            dialogue.CancelDialogue();
            Object.Destroy(story);
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(c => c.sortingOrder).ToArray();
            var target = new RenderTexture(1600, 900, 24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/completion-audit"));
            Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            Object.Destroy(image); target.Release(); Object.Destroy(target);
        }
    }
}
