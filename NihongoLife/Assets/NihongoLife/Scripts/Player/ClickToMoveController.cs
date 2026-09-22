using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.AI;

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
            _camera = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (_camera == null) _camera = Camera.main;
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
                }
                break;
            }
        }
    }
}
