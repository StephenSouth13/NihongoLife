using System.Collections;
using UnityEngine;
using NihongoLife.Player;

namespace NihongoLife.Cameras
{
    /// <summary>
    /// Brief establishing camera sweep when the player spawns into a zone — reuses the player's own
    /// ThirdPersonCameraController (SetOrbit/IsLocked, both already public) instead of a second camera,
    /// so there is never more than one Camera/AudioListener active. Player movement is paused for the
    /// duration so the shot does not fight the player already walking out of frame.
    /// </summary>
    public class ZoneEntrancePan : MonoBehaviour
    {
        [SerializeField] private float panDuration = 2.4f;
        [SerializeField] private float startYaw = -55f;
        [SerializeField] private float endYaw = 55f;
        [SerializeField] private float pitch = 18f;
        [SerializeField] private float distance = 4.2f;

        private void Start()
        {
            StartCoroutine(PanRoutine());
        }

        private IEnumerator PanRoutine()
        {
            var cameraController = FindFirstObjectByType<ThirdPersonCameraController>();
            if (cameraController == null) yield break;

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.enabled = false;

            bool wasLocked = cameraController.IsLocked;
            cameraController.IsLocked = true;

            float elapsed = 0f;
            while (elapsed < panDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / panDuration));
                cameraController.SetOrbit(Mathf.Lerp(startYaw, endYaw, t), pitch, distance);
                yield return null;
            }

            cameraController.IsLocked = wasLocked;
            if (player != null) player.enabled = true;
        }
    }
}
