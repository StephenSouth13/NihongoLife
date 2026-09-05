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
        private int _currentIndex;
        private float _waitTimer;
        private int _direction = 1;

        private void Awake()
        {
            _npc = GetComponent<NPCController>();
            _animation = GetComponent<CharacterAnimationController>();
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
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, walkSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
            _animation?.SetSpeed(walkSpeed);
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
