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
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float runSpeed = 6.0f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float rotationSpeed = 10f;

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
            // Simple grounding check. Fallback if groundCheck transform isn't assigned
            if (groundCheck != null)
            {
                _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            }
            else
            {
                _isGrounded = _characterController.isGrounded;
            }

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Slight downward force to keep grounded
            }
        }

        private void HandleMovement()
        {
            // Read new Input System inputs directly
            Vector2 moveInput = Vector2.zero;
            bool isRunning = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
                if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
                if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
                if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
                
                isRunning = Keyboard.current.shiftKey.isPressed;
            }

            // Calculate camera relative direction
            Vector3 forward = _mainCamera.transform.forward;
            Vector3 right = _mainCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            _characterController.Move(moveDirection * (currentSpeed * Time.deltaTime));
            if (_animationController != null)
            {
                _animationController.SetSpeed(moveDirection.magnitude * currentSpeed);
            }

            // Rotate Player in movement direction
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Apply gravity
            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
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
