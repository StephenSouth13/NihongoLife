using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using NihongoLife.Cameras;

namespace NihongoLife.Player
{
    public sealed class ClickToMoveController : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private float rayDistance = 300f;

        private PlayerController _player;
        private Camera _camera;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _camera = ResolveGameplayCamera();
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (!IsGameplayCamera(_camera)) _camera = ResolveGameplayCamera();
            if (_camera == null || _player == null) return;

            Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, groundLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.normal.y < 0.55f) continue;
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
                {
                    _player.SetClickDestination(navHit.position);
                    return;
                }
            }
        }

        private static bool IsGameplayCamera(Camera camera)
        {
            return camera != null
                && camera.isActiveAndEnabled
                && camera.GetComponent<ThirdPersonCameraController>() != null;
        }

        private static Camera ResolveGameplayCamera()
        {
            Camera taggedCamera = Camera.main;
            if (IsGameplayCamera(taggedCamera)) return taggedCamera;

            var controller = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            return controller != null ? controller.GetComponent<Camera>() : null;
        }
    }
}
