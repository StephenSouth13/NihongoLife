using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.NPC;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Reproduces the station the way it is opened from the Editor: Play pressed in 20_StationDistrict
    /// (StandaloneZoneBootstrap boots through the city, then enters the station). Checks the player stands on
    /// the floor and that pressing F at Kimura through the real input path opens his conversation.
    /// Captures go to Bao_Cao/station-regression.
    /// </summary>
    public class StationStandalonePlayModeTests
    {
        [UnityTest]
        public IEnumerator Station_Standalone_GroundedAndKimuraTalks()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.StationScene);
            PlayerController player = null;
            for (float t = 0f; t < 20f; t += Time.unscaledDeltaTime)
            {
                player = Object.FindFirstObjectByType<PlayerController>();
                var flow = GameServices.TryGet(out SceneFlowController f) ? f : null;
                if (player != null && (flow == null || !flow.IsLoading) && player.gameObject.scene.IsValid() && t > 4f) break;
                yield return null;
            }
            Assert.NotNull(player, "A player must exist after the standalone boot.");
            yield return new WaitForSecondsRealtime(1.5f);

            // 1) Grounding: the capsule bottom must sit on the floor collider below it.
            var body = player.GetComponent<CharacterController>();
            float gap = FloorGap(player, body, out string floorName);
            Debug.Log($"[StationStandalone] player {player.transform.position} grounded={body.isGrounded} floor='{floorName}' gap={gap:0.000} m; " +
                      $"center={body.center} height={body.height} radius={body.radius} skin={body.skinWidth}");
            Capture("01_spawn");
            Assert.Less(Mathf.Abs(gap), 0.02f, $"Player floats {gap:0.000} m above '{floorName}'.");

            // 2) Kimura through the real F path (InteractionDetector → NPCController → StationStaffService).
            var dm = DialogueManager.Instance;
            Assert.NotNull(dm);
            Debug.Log($"[StationStandalone] before F: dialogue open={dm.IsOpen} paused={dm.IsStoryPaused} inputLocked={player.InputLocked}");
            Assert.IsFalse(dm.IsOpen, "No dialogue may stay open (invisibly) after arriving in the station.");
            var kimura = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_station_staff");
            Vector3 front = kimura.transform.position + kimura.transform.forward * 1.4f;
            Teleport(player, new Vector3(front.x, kimura.transform.position.y + 0.05f, front.z), kimura.transform.eulerAngles.y + 180f);
            yield return new WaitForSecondsRealtime(0.6f);
            gap = FloorGap(player, body, out floorName);
            Debug.Log($"[StationStandalone] at Kimura {kimura.transform.position}: gap={gap:0.000} floor='{floorName}' current='{player.GetComponent<NihongoLife.Interaction.InteractionDetector>()?.CurrentInteractable}'");
            Capture("02_at_kimura");
            var input = GameInputService.GetOrCreate();
            // Press F before the next Update reads input (coroutines run after Update; WaitForFixedUpdate runs before it).
            yield return new WaitForFixedUpdate();
            input.SetMobileButton(GameInputId.Interact, true);
            yield return null;
            input.SetMobileButton(GameInputId.Interact, false);
            yield return new WaitForSecondsRealtime(1f);
            Capture("03_after_f");
            Debug.Log($"[StationStandalone] after F: dialogue open={dm.IsOpen} conversation={dm.IsConversation}");
            Assert.IsTrue(dm.IsOpen, "Pressing F at Kimura must open his conversation.");
            Assert.Less(Mathf.Abs(gap), 0.02f, $"Player floats {gap:0.000} m above '{floorName}' at the counter.");
        }

        private static float FloorGap(PlayerController player, CharacterController body, out string floorName)
        {
            Vector3 bottom = player.transform.position; // the model's feet are drawn from the transform
            floorName = "(none)";
            var hits = Physics.RaycastAll(bottom + Vector3.up * 0.5f, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.transform.root != player.transform.root).OrderBy(h => h.distance).ToArray();
            if (hits.Length == 0) return 99f;
            floorName = hits[0].collider.name + " (" + hits[0].collider.GetType().Name + ")";
            return bottom.y - hits[0].point.y;
        }

        private static void Teleport(PlayerController player, Vector3 position, float yaw)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.SetOrbit(yaw, 18f, 3.4f);
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/station-regression"));
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
