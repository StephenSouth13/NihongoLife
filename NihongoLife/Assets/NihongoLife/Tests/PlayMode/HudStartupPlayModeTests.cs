using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Entering the city without touching anything: either the status widget is on screen, or a dialogue is open
    /// AND visible (the dock steps aside only for a dialogue the player can actually see and close).
    /// </summary>
    public class HudStartupPlayModeTests
    {
        [UnityTest]
        public IEnumerator City_OnLoad_HudOrVisibleDialogue()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            var dock = Object.FindFirstObjectByType<StatusDock>();
            Assert.NotNull(dock, "StatusDock exists");
            var dm = DialogueManager.Instance;
            bool dialogueOpen = dm != null && dm.IsOpen;
            var view = Object.FindObjectsByType<DialogueView>(FindObjectsSortMode.None).FirstOrDefault();
            bool dialogueVisible = view != null && view.Root != null && view.Root.activeInHierarchy;
            float alpha = dock.StatusWidget.GetComponentInParent<CanvasGroup>().alpha;
            Debug.Log($"[HudStartup] dialogueOpen={dialogueOpen} dialogueVisible={dialogueVisible} dockAlpha={alpha:0.00} immersive={UiModalStack.ImmersiveOpen}");
            Assert.IsTrue(alpha > 0.9f || (dialogueOpen && dialogueVisible),
                $"Neither the HUD nor a dialogue is visible (dialogueOpen={dialogueOpen}, visible={dialogueVisible}, alpha={alpha:0.00})");
            if (dialogueOpen)
            {
                for (int guard = 0; guard < 40 && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.Greater(dock.StatusWidget.GetComponentInParent<CanvasGroup>().alpha, 0.9f, "HUD returns when the dialogue closes");
            }
        }
    }
}
