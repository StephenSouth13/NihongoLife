using TMPro;
using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Player
{
    /// <summary>
    /// Visual representation of a remote player in the world.
    /// Shows a capsule with nametag and smoothly interpolates position/rotation.
    /// </summary>
    public class RemotePlayerAvatar : MonoBehaviour
    {
        public string PlayerId { get; private set; }
        public string DisplayName { get; private set; }
        public string SceneName { get; private set; }

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _smoothSpeed = 8f;
        private TextMeshPro _nameTag;
        private CharacterAnimationController _animController;
        private Vector3 _lastPosition;
        private static readonly Color NameTagColor = new Color(0.88f, 0.93f, 1f, 1f);
        private static readonly Color AvatarColor = new Color(0.35f, 0.6f, 0.85f, 0.92f);

        // ──────────────────────── Setup ────────────────────────

        public void Setup(OnlinePlayerSnapshot snapshot)
        {
            PlayerId = snapshot.playerId;
            UpdateFromSnapshot(snapshot);
            CreateVisuals();

            // Snap to position immediately on first setup
            transform.position = _targetPosition;
            transform.rotation = _targetRotation;
        }

        public void UpdateFromSnapshot(OnlinePlayerSnapshot snapshot)
        {
            DisplayName = snapshot.displayName;
            SceneName = snapshot.sceneName;
            _targetPosition = snapshot.position;
            _targetRotation = snapshot.rotation;

            if (_nameTag != null)
            {
                _nameTag.text = DisplayName;
            }
        }

        // ──────────────────────── Update ────────────────────────

        private void Update()
        {
            // Smooth interpolation
            transform.position = Vector3.Lerp(transform.position, _targetPosition, _smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, _smoothSpeed * Time.deltaTime);

            // Face nametag to camera
            if (_nameTag != null && Camera.main != null)
            {
                _nameTag.transform.rotation = Quaternion.LookRotation(
                    _nameTag.transform.position - Camera.main.transform.position);
            }

            // Simple speed-based animation
            if (_animController != null)
            {
                float speed = (transform.position - _lastPosition).magnitude / Time.deltaTime;
                _animController.SetSpeed(speed);
            }
            _lastPosition = transform.position;
        }

        // ──────────────────────── Visuals ────────────────────────

        private void CreateVisuals()
        {
            // Check if visuals already exist
            if (transform.Find("AvatarBody") != null) return;

            // Capsule body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "AvatarBody";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Remove collider to avoid physics interference
            var collider = body.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set material color
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.material.color = AvatarColor;
            }

            // Head sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "AvatarHead";
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            var headCollider = head.GetComponent<Collider>();
            if (headCollider != null) Destroy(headCollider);

            var headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null)
            {
                headRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                headRenderer.material.color = new Color(0.92f, 0.78f, 0.65f, 1f);
            }

            // Nametag (TextMeshPro 3D)
            var nameTagGo = new GameObject("NameTag");
            nameTagGo.transform.SetParent(transform, false);
            nameTagGo.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            nameTagGo.transform.localScale = Vector3.one * 0.08f;

            _nameTag = nameTagGo.AddComponent<TextMeshPro>();
            _nameTag.text = DisplayName;
            _nameTag.fontSize = 36f;
            _nameTag.alignment = TextAlignmentOptions.Center;
            _nameTag.color = NameTagColor;
            _nameTag.outlineWidth = 0.2f;
            _nameTag.outlineColor = new Color32(0, 0, 0, 180);
            _nameTag.enableAutoSizing = false;

            // Try to get animation controller if there's a proper model
            _animController = GetComponent<CharacterAnimationController>();
        }
    }
}
