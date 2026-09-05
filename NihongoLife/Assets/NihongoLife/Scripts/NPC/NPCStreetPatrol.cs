using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.NPC
{
    public class NPCStreetPatrol : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float walkSpeed = 1.15f;
        [SerializeField] private float turnSpeed = 7f;
        [SerializeField] private float waitSeconds = 1.2f;
        [SerializeField] private bool loop = true;

        private NPCController _npc;
        private CharacterAnimationController _animation;
        private CharacterController _characterController;
        private int _currentIndex;
        private float _waitTimer;
        private int _direction = 1;
        private Vector3 _smoothVelocity;

        private void Awake()
        {
            _npc = GetComponent<NPCController>();
            _animation = GetComponent<CharacterAnimationController>();
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null)
            {
                _characterController = gameObject.AddComponent<CharacterController>();
                _characterController.center = new Vector3(0f, 0.95f, 0f);
                _characterController.height = 1.85f;
                _characterController.radius = 0.32f;
                _characterController.stepOffset = 0.22f;
            }
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                _animation?.SetSpeed(0f);
                return;
            }

            if (_npc != null && _npc.IsInteracting)
            {
                _animation?.SetSpeed(0f);
                return;
            }

            if (_waitTimer > 0f)
            {
                _waitTimer -= Time.deltaTime;
                _animation?.SetSpeed(0f);
                return;
            }

            Transform target = waypoints[Mathf.Clamp(_currentIndex, 0, waypoints.Length - 1)];
            Vector3 targetPosition = target.position;
            targetPosition.y = transform.position.y;

            Vector3 offset = targetPosition - transform.position;
            if (offset.sqrMagnitude < 0.08f)
            {
                AdvanceWaypoint();
                _waitTimer = waitSeconds;
                _animation?.SetSpeed(0f);
                return;
            }

            Vector3 direction = offset.normalized;
            Vector3 desiredVelocity = direction * walkSpeed;
            _smoothVelocity = Vector3.Lerp(_smoothVelocity, desiredVelocity, 5f * Time.deltaTime);
            _characterController.Move(_smoothVelocity * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
            _animation?.SetSpeed(_smoothVelocity.magnitude);
        }

        private void AdvanceWaypoint()
        {
            if (loop)
            {
                _currentIndex = (_currentIndex + 1) % waypoints.Length;
                return;
            }

            if (_currentIndex == waypoints.Length - 1)
            {
                _direction = -1;
            }
            else if (_currentIndex == 0)
            {
                _direction = 1;
            }

            _currentIndex += _direction;
        }
    }
}
