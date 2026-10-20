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
        [SerializeField] private float minYAngle = -10f;
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

        [SerializeField] private float orbitDragMultiplier = 1.6f;

        [Header("Overview (Sims-style, after FreeSims' WorldCamera)")]
        [Tooltip("High three-quarter view: four 45° diagonals, three zoom levels; toggled with H, rotated with , and .")]
        [SerializeField] private float overviewPitch = 52f;
        [SerializeField] private float[] overviewDistances = { 9f, 15f, 24f };
        [SerializeField] private float overviewTurnSpeed = 260f;
        private bool _overview;
        private int _overviewQuadrant;
        private int _overviewZoom = 1;
        private float _preOverviewYaw, _preOverviewPitch, _preOverviewDistance;

        /// <summary>The Sims-style overview camera is active (cursor free, snap rotation, zoom levels).</summary>
        public bool OverviewMode => _overview;
        public int OverviewQuadrant => _overviewQuadrant;
        public int OverviewZoom => _overviewZoom;
        public float Yaw => _rotationX;

        /// <summary>Turns the overview camera on or off (refused inside buildings, where it would see through the roof).</summary>
        public bool SetOverview(bool on)
        {
            if (on == _overview) return true;
            if (on && _isIndoor) return false;
            if (on)
            {
                _preOverviewYaw = _rotationX; _preOverviewPitch = _rotationY; _preOverviewDistance = defaultDistance;
                _overviewQuadrant = Mathf.RoundToInt((_rotationX - 45f) / 90f);
            }
            else
            {
                _rotationX = _preOverviewYaw; _rotationY = _preOverviewPitch;
                defaultDistance = Mathf.Clamp(_preOverviewDistance, minDistance, maxDistance);
            }
            _overview = on;
            return true;
        }

        public void RotateOverview(int steps) { if (_overview) _overviewQuadrant += steps; }
        public void ZoomOverview(int steps) { if (_overview) _overviewZoom = Mathf.Clamp(_overviewZoom + steps, 0, overviewDistances.Length - 1); }

        private void UpdateOverviewInput()
        {
            var input = NihongoLife.Core.GameInputService.Instance;
            bool blocked = NihongoLife.UI.UiModalStack.BlocksHotkeys;
            if (input != null && !blocked && input.WasPressed(NihongoLife.Core.GameInputId.CameraMode))
            {
                bool want = !_overview;
                if (!SetOverview(want)) NihongoLife.UI.HudFeed.Post(NihongoLife.Core.Loc.T("Góc nhìn trên cao chỉ dùng ngoài trời.", "The overhead view only works outdoors.", "上からの視点は屋外だけで使えます。"), NihongoLife.UI.HudFeed.Kind.Info, 3f, "overview");
                else if (want) NihongoLife.UI.HudFeed.Post(NihongoLife.Core.Loc.T("Góc nhìn trên cao: , và . để xoay · cuộn chuột để phóng to/thu nhỏ · H để trở lại.", "Overhead view: , and . to rotate · scroll to zoom · H to go back.", "上からの視点：「,」「.」で回転 · ホイールでズーム · Hで戻る。"), NihongoLife.UI.HudFeed.Kind.Info, 5f, "overview");
            }
            if (!_overview || input == null || blocked) return;
            if (input.WasPressed(NihongoLife.Core.GameInputId.CameraRotateLeft)) RotateOverview(-1);
            if (input.WasPressed(NihongoLife.Core.GameInputId.CameraRotateRight)) RotateOverview(1);
        }

        /// <summary>Glides to the current diagonal, pitch and zoom level (no free orbit, no wall collision pull-in).</summary>
        private void UpdateOverviewCamera()
        {
            float targetYaw = _overviewQuadrant * 90f + 45f;
            _rotationX = Mathf.MoveTowardsAngle(_rotationX, targetYaw, overviewTurnSpeed * Time.deltaTime);
            _rotationY = Mathf.MoveTowards(_rotationY, overviewPitch, 90f * Time.deltaTime);
            float targetDistance = overviewDistances[Mathf.Clamp(_overviewZoom, 0, overviewDistances.Length - 1)];
            defaultDistance = Mathf.MoveTowards(defaultDistance, targetDistance, 30f * Time.deltaTime);
            if (Mouse.current != null && !NihongoLife.UI.UiModalStack.AnyOpen)
            {
                bool overUi = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
                float scroll = overUi ? 0f : Mouse.current.scroll.ReadValue().y;
                if (scroll > 0.01f) ZoomOverview(-1);
                else if (scroll < -0.01f) ZoomOverview(1);
            }
            Vector3 targetPosition = target.position + targetOffset;
            Vector3 finalPosition = targetPosition + Quaternion.Euler(_rotationY, _rotationX, 0f) * Vector3.back * defaultDistance;
            _currentDistance = defaultDistance;
            transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref _followVelocity, positionSmoothTime * 2f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position, Vector3.up), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
        private bool _orbiting;
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

            // The cursor itself is owned by UI.CursorDirector (locked mouse-look by default, free in menus / Ctrl).
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (_conversationTarget != null)
            {
                UpdateConversationCamera();
                return;
            }

            UpdateOverviewInput();
            if (_overview && !_isLocked)
            {
                UpdateOverviewCamera();
                return;
            }

            // Handle lock state (e.g. during menus/dialogue)
            if (!_isLocked && Mouse.current != null && !NihongoLife.UI.UiModalStack.AnyOpen)
            {
                var mouse = Mouse.current;
                bool overUi = !NihongoLife.UI.CursorDirector.GameplayLook && UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
                float sensitivity = NihongoLife.Core.ControlSettings.MouseSensitivity;
                float invert = NihongoLife.Core.ControlSettings.InvertY ? -1f : 1f;
                if (NihongoLife.UI.CursorDirector.GameplayLook && Cursor.lockState == CursorLockMode.Locked)
                {
                    // Gameplay mode: the locked mouse turns the camera directly. The first frames after the lock
                    // are skipped so leaving a menu or releasing Ctrl never snaps the view.
                    _orbiting = false;
                    if (Time.frameCount > NihongoLife.UI.CursorDirector.LookStartedFrame + 1)
                    {
                        Vector2 mouseDelta = mouse.delta.ReadValue();
                        _rotationX += mouseDelta.x * sensitivityX * sensitivity;
                        _rotationY -= mouseDelta.y * sensitivityY * sensitivity * invert;
                        _rotationY = Mathf.Clamp(_rotationY, minYAngle, maxYAngle);
                    }
                }
                else if (!NihongoLife.UI.CursorDirector.CursorKeyHeld)
                {
                    // Click-to-move scheme (and the WebGL fallback before pointer lock is granted): right/middle drag
                    // orbits. Holding Ctrl pauses the camera so the cursor can be used on the HUD.
                    if ((mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame) && !overUi) _orbiting = true;
                    if (!mouse.rightButton.isPressed && !mouse.middleButton.isPressed) _orbiting = false;
                    if (_orbiting)
                    {
                        Vector2 mouseDelta = mouse.delta.ReadValue();
                        _rotationX += mouseDelta.x * sensitivityX * orbitDragMultiplier * sensitivity;
                        _rotationY -= mouseDelta.y * sensitivityY * orbitDragMultiplier * sensitivity * invert;
                        _rotationY = Mathf.Clamp(_rotationY, minYAngle, maxYAngle);
                    }
                }
                else _orbiting = false;

                float scroll = overUi ? 0f : mouse.scroll.ReadValue().y;
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
            if (indoor) SetOverview(false);
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
