using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
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
    /// Phone play: the main menu has a switch for the on-screen controls (Auto → On → Off, the same setting as Settings),
    /// and in the town "On" shows the joystick and buttons, "Off" hides them, and the joystick walks the character.
    /// </summary>
    public class PhoneControlsPlayModeTests
    {
        [UnityTest]
        public IEnumerator Menu_SwitchesTouchControls_AndTheJoystickWalks()
        {
            LogAssert.ignoreFailingMessages = true;
            int original = TouchControlPrefs.Mode;
            var input = GameInputService.GetOrCreate();
            try
            {
                TouchControlPrefs.Mode = 0;
                yield return SceneManager.LoadSceneAsync("01_MainMenu");
                yield return new WaitForSecondsRealtime(2f);
                var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "ControlsButton");
                Assert.IsNotNull(button, "a touch-controls switch on the main menu");
                var label = button.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name.IndexOf("Icon", System.StringComparison.OrdinalIgnoreCase) < 0);
                StringAssert.Contains(TouchControlPrefs.ModeVi().Split(':')[1].Trim(), label.text, "shows the current mode");
                button.onClick.Invoke();
                Assert.AreEqual(1, TouchControlPrefs.Mode, "Auto → On");
                StringAssert.Contains(TouchControlPrefs.ModeVi().Split(':')[1].Trim(), label.text, "the label follows");
                var about = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "AboutButton");
                Assert.IsFalse(RectsOverlap((RectTransform)button.transform, (RectTransform)about.transform), "the switch does not cover About us");

                // In the town: On shows the joystick, Off hides it; the joystick walks.
                yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                yield return new WaitForSecondsRealtime(2.5f);
                var dm = DialogueManager.Instance;
                for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
                var overlay = Object.FindFirstObjectByType<TouchControlOverlay>();
                Assert.IsNotNull(overlay, "touch controls in the town");
                yield return new WaitForSecondsRealtime(0.5f);
                var visuals = overlay.GetComponentsInChildren<CanvasGroup>(true).First();
                Assert.Greater(visuals.alpha, 0.1f, "On: the joystick and buttons are shown");
                var player = Object.FindFirstObjectByType<PlayerController>();
                Vector3 start = player.transform.position;
                input.SetMobileMove(new Vector2(0f, 1f));
                yield return new WaitForSecondsRealtime(1f);
                input.SetMobileMove(Vector2.zero);
                Assert.Greater(Vector3.Distance(start, player.transform.position), 1f, "the joystick walks the character");
                TouchControlPrefs.Mode = 2;
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.Less(visuals.alpha, 0.01f, "Off: hidden");
            }
            finally
            {
                input.SetMobileMove(Vector2.zero);
                TouchControlPrefs.Mode = original;
            }
        }

        private static bool RectsOverlap(RectTransform a, RectTransform b)
        {
            var ca = new Vector3[4]; var cb = new Vector3[4];
            a.GetWorldCorners(ca); b.GetWorldCorners(cb);
            var ra = Rect.MinMaxRect(ca[0].x, ca[0].y, ca[2].x, ca[2].y);
            var rb = Rect.MinMaxRect(cb[0].x, cb[0].y, cb[2].x, cb[2].y);
            return ra.Overlaps(rb);
        }
    }
}
