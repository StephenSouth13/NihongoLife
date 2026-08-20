using UnityEngine;
using UnityEngine.AI;
using NihongoLife.Interaction;
using NihongoLife.Scenario;

namespace NihongoLife.NPC
{
    [RequireComponent(typeof(Collider))]
    public class NPCController : MonoBehaviour, IInteractable
    {
        [Header("NPC Profile")]
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;
        [SerializeField] private string role;

        [Header("Interaction Settings")]
        [SerializeField] private float lookAtSpeed = 5.0f;
        [SerializeField] private string promptJa = "話す"; // Talk
        [SerializeField] private string promptVi = "Nói chuyện";

        private NavMeshAgent _navAgent;
        private Transform _lookTarget;
        private bool _isInteracting = false;

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public string Role => role;

        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (_isInteracting && _lookTarget != null)
            {
                // Smoothly rotate to look at the player
                Vector3 lookDirection = _lookTarget.position - transform.position;
                lookDirection.y = 0; // Keep horizontal rotation only
                if (lookDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
                }
            }
        }

        public string GetPromptJa() => promptJa;
        public string GetPromptVi() => promptVi;
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            Debug.Log($"[NPCController] Player interacted with NPC: {npcId} ({displayName})");

            _lookTarget = player.transform;
            _isInteracting = true;

            // Notify ScenarioManager about interaction
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnNPCInteracted(npcId, this);
            }
        }

        public void StopInteracting()
        {
            _isInteracting = false;
            _lookTarget = null;
        }

        public void MoveToDestination(Vector3 destination, System.Action onReached = null)
        {
            if (_navAgent == null)
            {
                Debug.LogWarning($"[NPCController] MoveToDestination called on {gameObject.name} but no NavMeshAgent is attached.");
                return;
            }

            _navAgent.isStopped = false;
            _navAgent.SetDestination(destination);
            
            // We can start a coroutine or update check to trigger the onReached callback
            StartCoroutine(CheckReachedDestination(onReached));
        }

        private System.Collections.IEnumerator CheckReachedDestination(System.Action onReached)
        {
            while (_navAgent != null && !_navAgent.pathPending && _navAgent.remainingDistance > _navAgent.stoppingDistance)
            {
                yield return new WaitForSeconds(0.2f);
            }
            onReached?.Invoke();
        }
    }
}
