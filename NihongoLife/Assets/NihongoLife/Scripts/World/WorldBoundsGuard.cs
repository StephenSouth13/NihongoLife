using UnityEngine;
using UnityEngine.SceneManagement;
using NihongoLife.Core;

namespace NihongoLife.World
{
    public class WorldBoundsGuard : MonoBehaviour
    {
        [SerializeField] private Vector2 halfExtents = new Vector2(63f, 46f);
        [SerializeField] private bool limitForwardAxis = false;
        [SerializeField] private float fallY = -4f;
        [SerializeField] private Vector3 safeSpawn = new Vector3(0f, 0.08f, -13.5f);

        /// <summary>A zone has no fixed floor height: a drop this far below the last footing means the player left the map.</summary>
        private const float ZoneFallDistance = 5f;
        /// <summary>A jump this long between two footings is a spawn or a door, not a fall.</summary>
        private const float TeleportDistance = 15f;
        private const float FootingAge = 1.2f;

        private CharacterController _controller;
        private Vector3 _footing, _olderFooting;
        private bool _hasFooting;
        private float _nextFootingAt;
        private Scene _footingScene;

        /// <summary>How many times the player was put back in a zone; read by tests.</summary>
        public static int ZoneRescues { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void LateUpdate()
        {
            if (GameServices.TryGet(out SceneFlowController flow) && flow.IsLoading)
            {
                _hasFooting = false;
                return;
            }
            if (SceneManager.GetActiveScene() != gameObject.scene)
            {
                GuardZone();
                return;
            }
            _hasFooting = false;
            Vector3 position = transform.position;
            if (position.y < fallY)
            {
                Place(safeSpawn);
                Debug.Log($"[WorldBoundsGuard] Rescued player to {safeSpawn}.");
                return;
            }
            if (TownEdge.TryClamp(transform, _controller)) return;
            bool outOfBounds = Mathf.Abs(position.x) > halfExtents.x
                || (limitForwardAxis && Mathf.Abs(position.z) > halfExtents.y);
            if (!outOfBounds) return;

            Vector3 rescued = new Vector3(
                Mathf.Clamp(position.x, -halfExtents.x + 2f, halfExtents.x - 2f),
                safeSpawn.y,
                limitForwardAxis ? Mathf.Clamp(position.z, -halfExtents.y + 2f, halfExtents.y - 2f) : position.z
            );

            Place(rescued);
            Debug.Log($"[WorldBoundsGuard] Rescued player to {rescued}.");
        }

        /// <summary>
        /// Zones share the town's player, so the town's height and bounds mean nothing there and they used to have no guard at
        /// all: a step past a doorway fell for ever. The last footing is remembered, and an older one is what the player
        /// returns to, because the newest may be the very edge they fell from.
        /// </summary>
        private void GuardZone()
        {
            if (_controller == null || !_controller.enabled)
            {
                _hasFooting = false;
                return;
            }
            Scene zone = SceneManager.GetActiveScene();
            Vector3 position = transform.position;
            if (_hasFooting && _footingScene != zone) _hasFooting = false;
            if (_hasFooting && HorizontalDistance(position, _footing) > TeleportDistance) _hasFooting = false;
            if (_controller.isGrounded)
            {
                if (!_hasFooting)
                {
                    _footing = _olderFooting = position;
                    _footingScene = zone;
                    _hasFooting = true;
                    _nextFootingAt = Time.time + FootingAge;
                }
                else if (Time.time >= _nextFootingAt)
                {
                    _olderFooting = _footing;
                    _footing = position;
                    _nextFootingAt = Time.time + FootingAge;
                }
                return;
            }
            if (!_hasFooting || _footing.y - position.y < ZoneFallDistance) return;
            ZoneRescues++;
            Place(_olderFooting);
            Debug.Log($"[WorldBoundsGuard] Rescued player in {zone.name} to {_olderFooting}.");
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void Place(Vector3 position)
        {
            if (_controller != null) _controller.enabled = false;
            transform.position = position;
            if (_controller != null) _controller.enabled = true;
        }
    }
}
