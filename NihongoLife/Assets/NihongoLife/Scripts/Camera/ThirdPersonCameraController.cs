using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.Camera
{
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.6f, 0); // Aim at player head height

        [Header("Distance Settings")]
        [SerializeField] private float defaultDistance = 3.5f;
        [SerializeField] private float minDistance = 1.0f;
        [SerializeField] private float maxDistance = 10.0f;

        [Header("Orbit Sensitivity")]
        [SerializeField] private float sensitivityX = 0.15f;
        [SerializeField] private float sensitivityY = 0.15f;
        [SerializeField] private float minYAngle = -30f;
        [SerializeField] private float maxYAngle = 60f;

        [Header("Obstacle Collision")]
        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float cameraRadius = 0.2f;

        private float _rotationX = 0f;
        private float _rotationY = 20f;
        private float _currentDistance;
        private bool _isLocked = false;

        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }

        private void Start()
        {
            _currentDistance = defaultDistance;
            
            // Auto-find player target if not set
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            // Lock and hide cursor for third-person control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Handle lock state (e.g. during menus/dialogue)
            if (!_isLocked && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                _rotationX += mouseDelta.x * sensitivityX;
                _rotationY -= mouseDelta.y * sensitivityY;
                _rotationY = Mathf.Clamp(_rotationY, minYAngle, maxYAngle);
            }

            // Determine target position
            Vector3 targetPosition = target.position + targetOffset;

            // Calculate rotation and position
            Quaternion rotation = Quaternion.Euler(_rotationY, _rotationX, 0);
            Vector3 desiredDirection = rotation * Vector3.back;
            Vector3 desiredPosition = targetPosition + desiredDirection * defaultDistance;

            // Simple camera collision detection
            Vector3 targetToDesired = desiredPosition - targetPosition;
            if (Physics.SphereCast(targetPosition, cameraRadius, targetToDesired.normalized, out RaycastHit hit, defaultDistance, collisionLayers))
            {
                _currentDistance = Mathf.Clamp(hit.distance, minDistance, maxDistance);
            }
            else
            {
                _currentDistance = defaultDistance;
            }

            // Final position
            Vector3 finalPosition = targetPosition + desiredDirection * _currentDistance;

            transform.position = finalPosition;
            transform.LookAt(targetPosition);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
