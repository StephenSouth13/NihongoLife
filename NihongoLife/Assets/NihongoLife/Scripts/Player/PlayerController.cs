using UnityEngine;
using UnityEngine.InputSystem;
using NihongoLife.Interaction;
using NihongoLife.Core;

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
            _mainCamera = UnityEngine.Camera.main;
            _animationController = GetComponent<CharacterAnimationController>();
        }

        private void Update()
        {
            if (_inputLocked) return;

            HandleGroundCheck();
            HandleMovement();
            HandleInteractionInput();
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
            // Read new Input System inputs directly
            Vector2 moveInput = Vector2.zero;
            bool isRunning = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1f;
                
                isRunning = Keyboard.current.shiftKey.isPressed;
            }

            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main;
            }

            Vector3 forward = _mainCamera != null ? _mainCamera.transform.forward : transform.forward;
            Vector3 right = _mainCamera != null ? _mainCamera.transform.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

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
            _velocity.y = Mathf.Max(_velocity.y + gravity * Time.deltaTime, maxFallSpeed);
            Vector3 finalMove = _smoothedMoveDirection * currentSpeed;
            finalMove.y = _velocity.y;
            CollisionFlags flags = _characterController.Move(finalMove * Time.deltaTime);
            if ((flags & CollisionFlags.Below) != 0 && _velocity.y < 0f)
            {
                _velocity.y = -2f;
                _isGrounded = true;
            }
        }

        private void HandleInteractionInput()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                var detector = GetComponent<InteractionDetector>();
                if (detector != null)
                {
                    detector.TriggerInteraction();
                }
            }
        }
    }
}
