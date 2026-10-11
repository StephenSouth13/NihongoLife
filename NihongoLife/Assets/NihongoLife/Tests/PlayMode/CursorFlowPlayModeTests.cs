using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Shop;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The mouse in the default MouseLook scheme: free only while something needs it (a window, a dialogue, Ctrl),
    /// locked again for walking as soon as that ends — through the konbini door, the shelves and the register.
    /// </summary>
    public class CursorFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator Konbini_CursorLocksAgainAfterEveryStep()
        {
            var scheme = ControlSettings.Scheme;
            ControlSettings.Scheme = ControlScheme.MouseLook;
            CursorDirector.AssumeFocusForTests = true;
            try
            {
                yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
                var dm = DialogueManager.Instance;
                for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
                yield return new WaitForSecondsRealtime(0.5f);
                var player = Object.FindFirstObjectByType<PlayerController>();
                yield return Expect(true, "free walking in the city");

                // Through the real konbini door.
                Teleport(player, new Vector3(0f, 0.08f, -0.9f), 0f);
                yield return new WaitForSecondsRealtime(0.3f);
                Object.FindFirstObjectByType<DoorInteractable>().Interact(player.gameObject);
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.Greater(player.transform.position.z, 0.6f, "Inside the store");
                for (int guard = 0; guard < 40 && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Expect(true, "walking inside the store");

                // A shelf opens the shop window (cursor free), Esc closes it (cursor locked again).
                var shelf = Object.FindObjectsByType<KonbiniShelf>(FindObjectsSortMode.None).First(s => s.Section == KonbiniSection.Onigiri);
                Teleport(player, new Vector3(-1.7f, 0.08f, 3.2f), -90f);
                yield return new WaitForSecondsRealtime(0.5f);
                shelf.Interact(player.gameObject);
                yield return null;
                var shop = KonbiniShopUI.GetOrCreate();
                Assert.IsTrue(shop.IsOpen);
                yield return Expect(false, "shop window open");
                shop.Select(KonbiniCatalog.Find("onigiri_sake"));
                shop.AddSelectedToBasket();
                Assert.IsTrue(UiModalStack.CloseTop(), "Esc closes the shop");
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Expect(true, "after closing the shop with Esc");

                // Register with the clerk, pay, thank-you lines, back to walking.
                var clerk = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_cashier");
                Teleport(player, new Vector3(0.1f, 0.08f, 7.4f), 0f);
                yield return new WaitForSecondsRealtime(0.6f);
                clerk.Interact(player.gameObject);
                yield return null;
                for (int guard = 0; guard < 40 && !shop.IsCheckout && dm.IsOpen; guard++) { dm.ContinueDialogue(); yield return new WaitForSecondsRealtime(0.2f); if (!dm.IsOpen && !shop.IsCheckout) { clerk.Interact(player.gameObject); yield return null; } }
                Assert.IsTrue(shop.IsCheckout, "Register open");
                yield return Expect(false, "register open");
                Assert.IsTrue(shop.Pay());
                for (float t = 0f; t < 6f && (shop.IsOpen || dm.IsOpen); t += 0.3f)
                {
                    if (dm.IsOpen) dm.ContinueDialogue();
                    yield return new WaitForSecondsRealtime(0.3f);
                }
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Expect(true, "after paying and the thank-you");
            }
            finally
            {
                CursorDirector.AssumeFocusForTests = false;
                ControlSettings.Scheme = scheme;
            }
        }

        private static IEnumerator Expect(bool look, string when)
        {
            yield return null;
            yield return null;
            var player = Object.FindFirstObjectByType<PlayerController>();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            string state = $"look={CursorDirector.GameplayLook} lock={Cursor.lockState} playerLocked={player?.InputLocked} playerEnabled={player?.enabled} " +
                           $"cameraLocked={camera?.IsLocked} cameraActive={camera?.isActiveAndEnabled} modal={UiModalStack.AnyOpen}:{UiModalStack.TopName} typing={UiModalStack.IsTyping} " +
                           $"dialogue={DialogueManager.Instance?.IsOpen} ctrl={CursorDirector.CursorKeyHeld}";
            Debug.Log($"[CursorFlow] {when}: {state}");
            Assert.AreEqual(look, CursorDirector.GameplayLook, $"Cursor {(look ? "locked for walking" : "free")} {when}: {state}");
        }

        private static void Teleport(PlayerController player, Vector3 position, float yaw)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.SetOrbit(yaw, 16f, 4.2f);
        }
    }
}
