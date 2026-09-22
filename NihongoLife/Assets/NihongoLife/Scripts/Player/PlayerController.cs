using UnityEngine;
using UnityEngine.InputSystem;
using NihongoLife.Interaction;
using NihongoLife.Core;
using NihongoLife.Cameras;
using NihongoLife.Audio;
using NihongoLife.World;

namespace NihongoLife.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 2.8f;
        [SerializeField] private float runSpeed = 5.2f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 16f;
        [SerializeField] private float maxFallSpeed = -20f;
        [SerializeField] private float runEnergyCostPerSecond = 4.5f;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float walkFootstepInterval = 0.52f;
        [SerializeField] private float runFootstepInterval = 0.34f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        private CharacterController _characterController;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _inputLocked = false;
        private UnityEngine.Camera _mainCamera;
        private CharacterAnimationController _animationController;
        private Vector3 _smoothedMoveDirection;
        private GameInputService _input;
        private float _footstepTimer;
        private Vector3 _clickDestination;
        private bool _hasClickDestination;

        public void SetClickDestination(Vector3 destination)
        {
            _clickDestination = destination;
            _hasClickDestination = true;
        }

        public bool InputLocked
        {
            get => _inputLocked;
            set
            {
                _inputLocked = value;
                if (_inputLocked)
                {
                    _velocity = Vector3.zero;
                    if (_animationController != null) _animationController.SetSpeed(0f);
                }
            }
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _mainCamera = ResolveGameplayCamera();
            _animationController = GetComponent<CharacterAnimationController>();
            if (GetComponent<ClickToMoveController>() == null)
            {
                gameObject.AddComponent<ClickToMoveController>();
            }
            PlayableCharacterCatalog.ApplySelectedVisual(gameObject);
            _input = GameInputService.GetOrCreate();
            if (GetComponent<PlayerWorldActionController>() == null)
            {
                gameObject.AddComponent<PlayerWorldActionController>();
            }
            if (GetComponent<EmploymentSystem>() == null) gameObject.AddComponent<EmploymentSystem>();
            if (GetComponent<BusinessSystem>() == null) gameObject.AddComponent<BusinessSystem>();
        }

        private void Update()
        {
            if (_inputLocked) return;

            HandleGroundCheck();
            HandleMovement();
            HandleInteractionInput();
            HandleActionInput();
        }

        private void HandleGroundCheck()
        {
            bool controllerGrounded = _characterController.isGrounded;
            if (groundCheck != null)
            {
                _isGrounded = controllerGrounded || Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                _isGrounded = controllerGrounded;
            }

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            Vector2 moveInput = _input != null ? _input.Move : Vector2.zero;
            bool isRunning = _input != null && _input.IsPressed(GameInputId.Sprint);

            if (isRunning && moveInput.sqrMagnitude > 0.01f && PlayerStatus.Instance != null)
            {
                isRunning = PlayerStatus.Instance.ConsumeEnergy(runEnergyCostPerSecond * Time.deltaTime);
            }

            if (!IsGameplayCamera(_mainCamera))
            {
                _mainCamera = ResolveGameplayCamera();
            }

            Vector3 forward = _mainCamera != null ? _mainCamera.transform.forward : transform.forward;
            Vector3 right = _mainCamera != null ? _mainCamera.transform.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            if (_hasClickDestination && moveInput.sqrMagnitude < 0.01f)
            {
                Vector3 toDestination = _clickDestination - transform.position;
                toDestination.y = 0f;
                if (toDestination.sqrMagnitude <= 0.16f)
                {
                    _hasClickDestination = false;
                    moveDirection = Vector3.zero;
                }
                else
                {
                    moveDirection = toDestination.normalized;
                }
            }
            else if (moveInput.sqrMagnitude > 0.01f)
            {
                _hasClickDestination = false;
            }

            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            float smoothing = moveDirection.sqrMagnitude > 0.001f ? acceleration : deceleration;
            _smoothedMoveDirection = Vector3.MoveTowards(_smoothedMoveDirection, moveDirection, smoothing * Time.deltaTime);
            if (_animationController != null)
            {
                _animationController.SetSpeed(_smoothedMoveDirection.magnitude * currentSpeed);
            }

            // Rotate Player in movement direction
            if (_smoothedMoveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_smoothedMoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Apply gravity
            if (_isGrounded && _input != null && _input.WasPressed(GameInputId.Jump))
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _velocity.y = Mathf.Max(_velocity.y + gravity * Time.deltaTime, maxFallSpeed);
            Vector3 finalMove = _smoothedMoveDirection * currentSpeed;
            finalMove.y = _velocity.y;
            CollisionFlags flags = _characterController.Move(finalMove * Time.deltaTime);
            if (_hasClickDestination && (flags & CollisionFlags.Sides) != 0 && finalMove.magnitude > 0.25f)
            {
                _hasClickDestination = false;
                _smoothedMoveDirection = Vector3.zero;
            }
            if ((flags & CollisionFlags.Below) != 0 && _velocity.y < 0f)
            {
                _velocity.y = -2f;
                _isGrounded = true;
            }
            HandleFootsteps(moveDirection.sqrMagnitude > 0.01f, isRunning);
        }

        private void HandleFootsteps(bool moving, bool running)
        {
            if (!moving || !_isGrounded)
            {
                _footstepTimer = 0f;
                return;
            }

            _footstepTimer += Time.deltaTime;
            if (_footstepTimer < (running ? runFootstepInterval : walkFootstepInterval)) return;
            _footstepTimer = 0f;
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(ResolveFootstepCue(), 0.45f);
        }

        private GameAudioCue ResolveFootstepCue()
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.25f, Vector3.down, out RaycastHit hit,
                    1.5f, ~0, QueryTriggerInteraction.Ignore)) return GameAudioCue.FootstepConcrete;

            string surface = hit.collider.name.ToLowerInvariant();
            if (surface.Contains("wood") || surface.Contains("floor")) return GameAudioCue.FootstepWood;
            if (surface.Contains("carpet") || surface.Contains("rug")) return GameAudioCue.FootstepCarpet;
            return GameAudioCue.FootstepConcrete;
        }

        private void HandleInteractionInput()
        {
            if (_input != null && _input.WasPressed(GameInputId.Interact))
            {
                var detector = GetComponent<InteractionDetector>();
                if (detector != null)
                {
                    detector.TriggerInteraction();
                }
            }
        }

        private void HandleActionInput()
        {
            var actions = GetComponent<PlayerWorldActionController>();
            if (actions == null) return;
            if (_input.WasPressed(GameInputId.Attack)) actions.TryAttack();
            if (_input.WasPressed(GameInputId.DropItem)) actions.TryDropLastItem();
            if (_input.WasPressed(GameInputId.UseItem) && PlayerInventory.Instance != null &&
                PlayerInventory.Instance.TryConsumeLastConsumable() && GameServices.TryGet(out IAudioService audio))
            {
                audio.PlayCue(GameAudioCue.UiConfirm, 0.7f);
            }
        }

        private static bool IsGameplayCamera(UnityEngine.Camera camera)
        {
            return camera != null
                && camera.isActiveAndEnabled
                && camera.GetComponent<ThirdPersonCameraController>() != null;
        }

        private static UnityEngine.Camera ResolveGameplayCamera()
        {
            UnityEngine.Camera taggedCamera = UnityEngine.Camera.main;
            if (IsGameplayCamera(taggedCamera)) return taggedCamera;

            var controller = FindFirstObjectByType<ThirdPersonCameraController>();
            return controller != null ? controller.GetComponent<UnityEngine.Camera>() : null;
        }
    }
}
