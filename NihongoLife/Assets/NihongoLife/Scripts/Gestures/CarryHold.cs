using NihongoLife.Core;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.Gestures
{
    /// <summary>
    /// Keeps something visibly carried by the character between actions: a dish on a tray or a box during a shift
    /// (<see cref="Job"/>), the player's own guitar on the back (<see cref="Back"/>). Both are gestures of the
    /// <see cref="GestureLayer"/> ("carry_…" ids survive walking), so the props sit in the hands or on the body of any rig.
    /// Another gesture (serving, strumming, an emote) simply takes over; when the layer is idle again the carry comes back.
    /// While the player is locked (asleep, seated at an instrument, a timed action) the guitar on the back is put away.
    /// Checked on a timer, not every frame.
    /// </summary>
    public sealed class CarryHold : MonoBehaviour
    {
        private const float CheckEvery = 0.25f;

        private GestureLayer _layer;
        private PlayerController _player;
        private float _next;

        /// <summary>Gesture of what a shift has the character carry (null when nothing).</summary>
        public string Job { get; private set; }
        /// <summary>Gesture of what the character wears on the back (null when nothing).</summary>
        public string Back { get; private set; }

        public static CarryHold On(GameObject who)
        {
            if (who == null) return null;
            return who.GetComponent<CarryHold>().OrNull() ?? who.AddComponent<CarryHold>();
        }

        public void SetJob(string gestureId)
        {
            string old = Job;
            Job = string.IsNullOrEmpty(gestureId) ? null : gestureId;
            Layer();
            if (_layer != null && old != null && _layer.Current == old && old != Job) _layer.Stop(0.25f);
            _next = 0f;
            Tick();
        }

        public void SetBack(string gestureId)
        {
            string old = Back;
            Back = string.IsNullOrEmpty(gestureId) ? null : gestureId;
            Layer();
            if (_layer != null && old != null && _layer.Current == old && old != Back) _layer.Stop(0.25f);
            _next = 0f;
            Tick();
        }

        private GestureLayer Layer()
        {
            if (_layer == null) _layer = GestureLayer.On(gameObject);
            if (_player == null) _player = GetComponentInParent<PlayerController>();
            return _layer;
        }

        private void Update()
        {
            if (Time.time < _next) return;
            Tick();
        }

        private void Tick()
        {
            _next = Time.time + CheckEvery;
            if (Job == null && Back == null) return;
            var layer = Layer();
            if (layer == null) return;
            bool locked = _player != null && _player.InputLocked;
            string want = Job ?? (locked ? null : Back);
            string current = layer.Current;
            if (current != null && current == Back && want != Back) { layer.Stop(0.2f); return; }
            if (want == null || current == want) return;
            if (current == null) layer.Play(want);
        }
    }
}
