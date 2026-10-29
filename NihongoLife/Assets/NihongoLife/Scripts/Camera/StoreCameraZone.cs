using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.Cameras
{
    /// <summary>
    /// The konbini's interior volume. Walking through the door used to swap to a second camera (fixed yaw, snapped distance)
    /// and back, which whipped the view and could flicker on the threshold. Now the ONE camera stays, and only eases its
    /// distance/height (ThirdPersonCameraController.SetIndoorMode(smooth)) while keeping the player's own yaw and pitch.
    /// Entering/leaving is debounced: a player has several colliders and the doorway edge can chatter.
    /// </summary>
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

        /// <summary>Distance of the camera behind the player inside the shop.</summary>
        public const float InteriorDistance = 2.6f;
        private const float LeaveDelay = 0.3f;

        private float _leaveAt = -1f;
        private bool _indoorApplied;
        private Transform _player;
        private Collider _volume;
        private float _nextFind;

        public bool IndoorApplied => _indoorApplied;

        private void OnEnable() => Zones.Add(this);
        private void OnDisable() { Zones.Remove(this); if (_indoorApplied) Apply(false); }

        private void Awake() { _volume = GetComponent<Collider>(); _volume.isTrigger = true; }

        /// <summary>
        /// Polled instead of OnTriggerEnter/Exit: the player is also moved by teleports (the shop door) and has more than one
        /// collider, and trigger callbacks then miscount. The volume is widened by a margin once inside (hysteresis), and
        /// leaving needs <see cref="LeaveDelay"/> of being outside, so the doorway edge cannot make the camera chatter.
        /// </summary>
        private void Update()
        {
            if (_player == null)
            {
                if (Time.unscaledTime < _nextFind) return;
                _nextFind = Time.unscaledTime + 0.5f;
                var pc = Object.FindFirstObjectByType<PlayerController>();
                if (pc == null) return;
                _player = pc.transform;
            }
            Bounds b = _volume.bounds;
            if (_indoorApplied) b.Expand(new Vector3(0.5f, 2f, 0.5f));
            bool inside = b.Contains(new Vector3(_player.position.x, b.center.y, _player.position.z));
            if (inside)
            {
                _leaveAt = -1f;
                if (!_indoorApplied) Apply(true);
            }
            else if (_indoorApplied)
            {
                if (_leaveAt < 0f) _leaveAt = Time.unscaledTime + LeaveDelay;
                else if (Time.unscaledTime >= _leaveAt) { _leaveAt = -1f; Apply(false); }
            }
        }

        private static ThirdPersonCameraController FindController()
        {
            var cam = Camera.main;
            var c = cam != null ? cam.GetComponent<ThirdPersonCameraController>() : null;
            return c != null ? c : Object.FindFirstObjectByType<ThirdPersonCameraController>();
        }

        private void Apply(bool indoor)
        {
            var controller = FindController();
            if (controller == null) return;
            // Do not fight another owner (a bedroom/station already put the camera indoors).
            if (indoor && controller.IsIndoor) { _indoorApplied = true; return; }
            _indoorApplied = indoor;
            controller.SetIndoorMode(indoor, true, indoor ? InteriorDistance : -1f);
        }
    }
}