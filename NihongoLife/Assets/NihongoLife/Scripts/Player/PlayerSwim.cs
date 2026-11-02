using NihongoLife.Core;
using NihongoLife.Gestures;
using NihongoLife.UI;
using NihongoLife.World;
using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// Swimming: past waist depth the player floats at the surface and strokes along. Reaching land climbs out by itself,
    /// and too far from the shore a current turns them back. PlayerController reads <see cref="Swimming"/> for speed and height.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public sealed class PlayerSwim : MonoBehaviour
    {
        private const float EnterDepth = 0.5f, FloatDepth = 0.4f, ClimbReach = 0.9f, ClimbHeight = 1.3f;
        private const float WarnDistance = 45f, TurnBackDistance = 60f;
        private const string Stroke = "swim_crawl", Tread = "swim_tread";

        private CharacterController _body;
        private CharacterAnimationController _animation;
        private FootGrounder _feet;
        private PostureStabilizer _posture;
        private Transform _visual;
        private WaterSurface _water;
        private Vector3 _shore;
        private bool _hasShore, _warned;
        private float _pitch, _nextClimbCheck;

        public bool Swimming { get; private set; }

        /// <summary>Where the transform floats while swimming.</summary>
        public float FloatY => _water != null ? _water.Y - FloatDepth : transform.position.y;

        private void Awake()
        {
            _body = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_body == null || !_body.enabled)
            {
                if (Swimming) End();
                return;
            }
            Vector3 position = transform.position;
            if (!Swimming)
            {
                if (_body.isGrounded) { _shore = position; _hasShore = true; }
                var water = WaterSurface.At(position);
                if (water != null && position.y < water.Y - EnterDepth) Begin(water);
                return;
            }
            if (_water == null || !_water.isActiveAndEnabled)
            {
                End();
                return;
            }
            if (Physics.Raycast(position + Vector3.up * 1.2f, Vector3.down, out RaycastHit floor, 2.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && floor.point.y > _water.Y - EnterDepth)
            {
                End();
                return;
            }
            if (Time.time >= _nextClimbCheck)
            {
                _nextClimbCheck = Time.time + 0.15f;
                if (ClimbOut()) return;
            }
            Vector3 flat = position - _shore;
            flat.y = 0f;
            if (_hasShore && flat.magnitude > TurnBackDistance)
            {
                Place(_shore + (position - _shore).normalized * (WarnDistance * 0.5f) + Vector3.down * (position.y - FloatY));
                HudFeed.Post(Loc.T("Dòng nước đẩy bạn về gần bờ.", "The current carries you back towards the shore.", "流れに押されて岸の近くへ戻りました。"), HudFeed.Kind.Warning);
            }
            else if (_hasShore && flat.magnitude > WarnDistance && !_warned)
            {
                _warned = true;
                HudFeed.Post(Loc.T("Ra xa quá rồi, hãy bơi vào bờ.", "You are far out. Swim back to the shore.", "沖に出すぎました。岸へ戻りましょう。"), HudFeed.Kind.Warning);
            }
            else if (flat.magnitude < WarnDistance * 0.8f) _warned = false;
            Animate();
        }

        /// <summary>The shore stands above the water at many places: land just ahead, within reach, is climbed onto.</summary>
        private bool ClimbOut()
        {
            Vector3 ahead = transform.position + transform.forward * ClimbReach;
            ahead.y = _water.Y + ClimbHeight;
            if (!Physics.SphereCast(ahead, 0.2f, Vector3.down, out RaycastHit land, ClimbHeight + 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            if (land.normal.y < 0.7f || land.point.y < _water.Y - EnterDepth) return false;
            End();
            Place(land.point + Vector3.up * 0.08f);
            return true;
        }

        private void Begin(WaterSurface water)
        {
            _water = water;
            Swimming = true;
            _warned = false;
            _animation = GetComponent<CharacterAnimationController>();
            if (_animation == null) _animation = GetComponentInChildren<CharacterAnimationController>();
            _feet = GetComponentInChildren<FootGrounder>();
            _posture = GetComponentInChildren<PostureStabilizer>();
            if (_feet != null) _feet.enabled = false;
            if (_posture != null) _posture.enabled = false;
            var animator = GetComponentInChildren<Animator>();
            _visual = animator != null ? animator.transform : null;
            GestureLayer.On(gameObject).Play(Tread);
        }

        private void Animate()
        {
            Vector3 velocity = _body.velocity;
            velocity.y = 0f;
            bool stroking = velocity.sqrMagnitude > 0.15f;
            var gestures = GestureLayer.On(gameObject);
            string wanted = stroking ? Stroke : Tread;
            if (gestures.Current != wanted) gestures.Play(wanted);
            _pitch = Mathf.Lerp(_pitch, stroking ? 62f : 12f, 5f * Time.deltaTime);
            // Only the model leans: the player's own transform carries the camera and the capsule.
            if (_visual != null && _visual != transform) _visual.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void End()
        {
            Swimming = false;
            _water = null;
            _pitch = 0f;
            if (_visual != null && _visual != transform) _visual.localRotation = Quaternion.identity;
            if (_feet != null) _feet.enabled = true;
            if (_posture != null) _posture.enabled = true;
            var gestures = GestureLayer.On(gameObject);
            if (gestures.Current == Stroke || gestures.Current == Tread) gestures.Stop(0.15f);
        }

        private void Place(Vector3 at)
        {
            _body.enabled = false;
            transform.position = at;
            _body.enabled = true;
        }

        private void OnDisable()
        {
            if (Swimming) End();
        }
    }
}
