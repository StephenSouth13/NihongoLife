using UnityEngine;

namespace NihongoLife.Cameras
{
    [RequireComponent(typeof(Collider))]
    public class StoreCameraZone : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetIndoorMode(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetIndoorMode(false);
        }

        private static void SetIndoorMode(bool indoor)
        {
            var camera = Camera.main;
            if (camera == null) return;

            var controller = camera.GetComponent<ThirdPersonCameraController>();
            if (controller != null)
            {
                controller.SetIndoorMode(indoor);
            }
        }
    }
}
