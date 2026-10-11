using UnityEngine;

namespace NihongoLife.Cameras
{
    [RequireComponent(typeof(Collider))]
    public class StoreCameraZone : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<StoreCameraZone> Zones = new();

        /// <summary>True when <paramref name="position"/> is inside a shop's indoor zone (sheltered from the weather).</summary>
        public static bool Contains(Vector3 position)
        {
            foreach (var zone in Zones)
                if (zone != null && zone.isActiveAndEnabled && zone.GetComponent<Collider>().bounds.Contains(position)) return true;
            return false;
        }

        private void OnEnable() => Zones.Add(this);
        private void OnDisable() => Zones.Remove(this);

        private Camera _outdoorCamera;
        private Camera _indoorCamera;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            CreateIndoorCamera();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetIndoorMode(true, other.transform);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetIndoorMode(false, other.transform);
        }

        private void CreateIndoorCamera()
        {
            _outdoorCamera = Camera.main;
            if (_outdoorCamera == null) return;

            var go = new GameObject("Store Interior Camera");
            go.SetActive(false);
            _indoorCamera = go.AddComponent<Camera>();
            _indoorCamera.clearFlags = _outdoorCamera.clearFlags;
            _indoorCamera.backgroundColor = _outdoorCamera.backgroundColor;
            _indoorCamera.fieldOfView = 56f;
            _indoorCamera.nearClipPlane = 0.06f;
            _indoorCamera.farClipPlane = _outdoorCamera.farClipPlane;
            go.AddComponent<AudioListener>();
            var controller = go.AddComponent<ThirdPersonCameraController>();
            controller.SetOrbit(180f, 18f, 2.45f);
            controller.SetIndoorMode(true);
        }

        private void SetIndoorMode(bool indoor, Transform player)
        {
            if (_outdoorCamera == null || _indoorCamera == null) return;

            if (indoor)
            {
                var controller = _indoorCamera.GetComponent<ThirdPersonCameraController>();
                controller.SetTarget(player);
                _outdoorCamera.gameObject.tag = "Untagged";
                _outdoorCamera.gameObject.SetActive(false);
                _indoorCamera.gameObject.tag = "MainCamera";
                _indoorCamera.gameObject.SetActive(true);
                return;
            }

            _indoorCamera.gameObject.tag = "Untagged";
            _indoorCamera.gameObject.SetActive(false);
            _outdoorCamera.gameObject.tag = "MainCamera";
            _outdoorCamera.gameObject.SetActive(true);
        }
    }
}
