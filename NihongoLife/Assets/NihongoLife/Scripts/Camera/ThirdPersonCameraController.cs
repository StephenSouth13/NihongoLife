using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.Cameras
{
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.6f, 0); // Aim at player head height

        [Header("Distance Settings")]
        [SerializeField] private float defaultDistance = 3.5f;
        [SerializeField] private float outdoorDistance = 7.2f;
        [SerializeField] private float indoorDistance = 3.2f;
        [SerializeField] private float minDistance = 1.0f;
        [SerializeField] private float maxDistance = 10.0f;
        [SerializeField] private float zoomSpeed = 0.012f;
        [SerializeField] private float distanceSmoothTime = 0.08f;
        [SerializeField] private float positionSmoothTime = 0.055f;

        [Header("Orbit Sensitivity")]
        [SerializeField] private float sensitivityX = 0.15f;
        [SerializeField] private float sensitivityY = 0.15f;
        [SerializeField] private float minYAngle = -30f;
        [SerializeField] private float maxYAngle = 60f;

        [Header("Obstacle Collision")]
        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float cameraRadius = 0.2f;
        [SerializeField] private float collisionPadding = 0.12f;

        [Header("Conversation Framing")]
        [SerializeField] private float conversationShoulderDistance = 1.45f;
        [SerializeField] private float conversationShoulderOffset = 0.65f;
        [SerializeField] private float conversationHeightOffset = 0.18f;
        [SerializeField] private float conversationPositionSmoothTime = 0.28f;
        [SerializeField] private float conversationRotationSharpness = 7f;

        private float _rotationX = 0f;
        private float _rotationY = 20f;
        private float _currentDistance;
        private float _distanceVelocity;
        private Vector3 _followVelocity;
        private bool _isLocked = false;
        private bool _isIndoor = false;
        private Transform _conversationTarget;
        private Vector3 _conversationVelocity;
        private float _conversationSide = 1f;
        private float _preConversationYaw;
        private float _preConversationPitch;
        private float _preConversationDistance;
        private bool _returningFromConversation;

        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }

        private void Start()
        {
            if (collisionLayers.value == 0)
            {
                collisionLayers = Physics.DefaultRaycastLayers;
            }
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

            if (_conversationTarget != null)
            {
                UpdateConversationCamera();
                return;
            }

            // Handle lock state (e.g. during menus/dialogue)
            if (!_isLocked && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                _rotationX += mouseDelta.x * sensitivityX;
                _rotationY -= mouseDelta.y * sensitivityY;
                _rotationY = Mathf.Clamp(_rotationY, minYAngle, maxYAngle);

                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float zoomed = Mathf.Clamp(defaultDistance - scroll * zoomSpeed, minDistance, maxDistance);
                    defaultDistance = zoomed;
                    if (!_isIndoor) outdoorDistance = zoomed;
                    else indoorDistance = zoomed;
                }
            }

            // Determine target position
            Vector3 targetPosition = target.position + targetOffset;

            // Calculate rotation and position
            Quaternion rotation = Quaternion.Euler(_rotationY, _rotationX, 0);
            Vector3 desiredDirection = rotation * Vector3.back;
            Vector3 desiredPosition = targetPosition + desiredDirection * defaultDistance;

            // Simple camera collision detection
            Vector3 targetToDesired = desiredPosition - targetPosition;
            float targetDistance = defaultDistance;
            if (Physics.SphereCast(targetPosition, cameraRadius, targetToDesired.normalized, out RaycastHit hit, defaultDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                targetDistance = Mathf.Clamp(hit.distance - collisionPadding, minDistance, defaultDistance);
            }

            _currentDistance = Mathf.SmoothDamp(_currentDistance, targetDistance, ref _distanceVelocity, distanceSmoothTime);

            // Final position
            Vector3 finalPosition = targetPosition + desiredDirection * _currentDistance;

            if (_returningFromConversation)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    finalPosition,
                    ref _conversationVelocity,
                    conversationPositionSmoothTime);
                Quaternion desiredRotation = Quaternion.LookRotation(targetPosition - transform.position, Vector3.up);
                float rotationT = 1f - Mathf.Exp(-conversationRotationSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);

                if ((transform.position - finalPosition).sqrMagnitude < 0.0025f)
                {
                    _returningFromConversation = false;
                    _conversationVelocity = Vector3.zero;
                }
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref _followVelocity, positionSmoothTime);
                Vector3 lookDirection = targetPosition - transform.position;
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    float rotationT = 1f - Mathf.Exp(-18f * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection, Vector3.up), rotationT);
                }
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetOrbit(float yaw, float pitch, float distance)
        {
            _rotationX = yaw;
            _rotationY = Mathf.Clamp(pitch, minYAngle, maxYAngle);
            defaultDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            outdoorDistance = defaultDistance;
            _currentDistance = defaultDistance;
            _distanceVelocity = 0f;
        }

        public void SetIndoorMode(bool indoor)
        {
            _isIndoor = indoor;
            defaultDistance = Mathf.Clamp(indoor ? indoorDistance : outdoorDistance, minDistance, maxDistance);
            _rotationY = Mathf.Clamp(indoor ? 22f : 16f, minYAngle, maxYAngle);
            targetOffset = indoor ? new Vector3(0f, 1.45f, 0f) : new Vector3(0f, 1.6f, 0f);
        }

        public void SetConversationTarget(Transform focusTarget)
        {
            if (focusTarget == null || target == null) return;

            _preConversationYaw = _rotationX;
            _preConversationPitch = _rotationY;
            _preConversationDistance = defaultDistance;
            _returningFromConversation = false;
            _conversationTarget = focusTarget;
            Vector3 playerToNpc = Vector3.ProjectOnPlane(focusTarget.position - target.position, Vector3.up);
            if (playerToNpc.sqrMagnitude > 0.01f)
            {
                Vector3 conversationRight = Vector3.Cross(Vector3.up, playerToNpc.normalized);
                _conversationSide = Vector3.Dot(transform.position - target.position, conversationRight) >= 0f ? 1f : -1f;
            }
            IsLocked = true;
        }

        public void ClearConversationTarget()
        {
            if (_conversationTarget == null) return;

            _conversationTarget = null;
            _conversationVelocity = Vector3.zero;
            _rotationX = _preConversationYaw;
            _rotationY = _preConversationPitch;
            defaultDistance = Mathf.Clamp(_preConversationDistance, minDistance, maxDistance);
            _currentDistance = defaultDistance;
            _distanceVelocity = 0f;
            _returningFromConversation = true;
        }

        private void UpdateConversationCamera()
        {
            Vector3 playerFace = target.position + targetOffset;
            Vector3 npcFace = _conversationTarget.position + Vector3.up * 1.55f;
            Vector3 playerToNpc = Vector3.ProjectOnPlane(npcFace - playerFace, Vector3.up);
            Vector3 forward = playerToNpc.sqrMagnitude > 0.01f ? playerToNpc.normalized : target.forward;
            Vector3 side = Vector3.Cross(Vector3.up, forward) * _conversationSide;
            Vector3 desiredPosition = playerFace
                - forward * conversationShoulderDistance
                + side * conversationShoulderOffset
                + Vector3.up * conversationHeightOffset;
            Vector3 lookTarget = Vector3.Lerp(playerFace, npcFace, 0.72f);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _conversationVelocity,
                conversationPositionSmoothTime);

            Vector3 lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                float rotationT = 1f - Mathf.Exp(-conversationRotationSharpness * Time.deltaTime);
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
            }
        }

    }
}
