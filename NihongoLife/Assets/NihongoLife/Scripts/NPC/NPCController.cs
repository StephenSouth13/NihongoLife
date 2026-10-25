using UnityEngine;
using UnityEngine.AI;
using NihongoLife.Core;
using NihongoLife.Dialogue;
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
        [SerializeField] private string promptJa = "話す";

        [UnityEngine.Serialization.FormerlySerializedAs("promptVi")]
        [SerializeField] private string promptEn = "Talk";

        [SerializeField] private string scenarioAreaIdOnInteract;

        [Header("Fallback Conversation")]
        [SerializeField] private string fallbackJa = "こんにちは。今日はいい天気ですね。";
        [SerializeField] private string fallbackReading = "こんにちは。きょうはいいてんきですね。";
        [SerializeField] private string fallbackEn = "Hello. Nice weather today.";
        [SerializeField] private string fallbackRomaji = "Konnichiwa. Kyou wa ii tenki desu ne.";

        private NavMeshAgent _navAgent;
        private NPCStreetPatrol _patrol;
        private CharacterAnimationController _animation;
        private Transform _lookTarget;
        private bool _isInteracting;

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public string Role => role;
        public bool IsInteracting => _isInteracting;

        private void Awake()
        {
            if (GetComponent<NihongoLife.Player.FootGrounder>() == null) gameObject.AddComponent<NihongoLife.Player.FootGrounder>();
            _navAgent = GetComponent<NavMeshAgent>();
            _patrol = GetComponent<NPCStreetPatrol>();
            _animation = GetComponent<CharacterAnimationController>();
            if (_animation == null)
                Debug.LogWarning($"[NPCController] '{name}' has no CharacterAnimationController; its visual may remain in T-pose.", this);
        }

        private void Update()
        {
            if (!_isInteracting || _lookTarget == null) return;

            Vector3 lookDirection = _lookTarget.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude <= 0.01f) return;

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
        }

        public string GetPromptJa() => promptJa;
        public string GetpromptEn() => promptEn;
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            Debug.Log($"[NPCController] Player interacted with NPC: {npcId} ({displayName})");
            // Reporting to an employer ("talk" objectives) is just talking to them.
            NihongoLife.Progression.QuestService.Raise("talk", npcId);

            _lookTarget = player.transform;
            _isInteracting = true;
            SetMovementFrozen(true);
            _animation?.SetLookTarget(player.transform);

            if (ScenarioManager.Instance == null)
            {
                ScenarioManager.Instance?.SetPlayerInputLocked(true);
                StartFallbackDialogue();
                return;
            }

            if (!string.IsNullOrEmpty(scenarioAreaIdOnInteract)
                && ScenarioManager.Instance.CanEnterArea(scenarioAreaIdOnInteract))
            {
                ScenarioManager.Instance.OnAreaEntered(scenarioAreaIdOnInteract);
            }

            // Service NPCs that drive real gameplay (e.g. the station clerk selling tickets) run before the
            // scenario and report progress to it themselves, so a narrated scenario chain cannot hide the service.
            if (TryGetComponent(out IPriorityNpcService priority) && priority.HandleInteract(player)) return;

            bool handledByScenario = ScenarioManager.Instance.OnNPCInteracted(npcId, this);
            if (!handledByScenario && TryGetComponent(out INpcService service) && service.HandleInteract(player))
            {
                return;
            }
            if (!handledByScenario)
            {
                ScenarioManager.Instance.SetPlayerInputLocked(true);
                StartSmartFallbackDialogue();
            }
        }

        /// <summary>NPC small talk is pre-written and offline (ScriptedNpcDialogue): the NPC always answers, with no
        /// network or AI call. GeminiConversationService is no longer used here.</summary>
        private void StartSmartFallbackDialogue() => StartFallbackDialogue();

        private void StartFallbackDialogue()
        {
            if (DialogueManager.Instance == null) return;
            DialogueManager.Instance.StartDialogue(ScriptedNpcDialogue.NextNode(npcId, string.IsNullOrEmpty(displayName) ? gameObject.name : displayName));
        }

        public void StopInteracting()
        {
            _isInteracting = false;
            _lookTarget = null;
            _animation?.SetLookTarget(null);
            SetMovementFrozen(false);
        }

        public void ReactToAttack(Transform attacker)
        {
            StopInteracting();
            if (attacker != null)
            {
                Vector3 away = transform.position - attacker.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(-away.normalized);
            }
            _animation?.SetSpeed(0f);
            Debug.LogWarning($"[NPCController] {displayName} was attacked. Civic conduct penalty reported.", this);
        }

        private void SetMovementFrozen(bool frozen)
        {
            if (_patrol != null)
            {
                _patrol.enabled = !frozen;
            }

            if (_navAgent != null && _navAgent.enabled)
            {
                _navAgent.isStopped = frozen;
                if (frozen) _navAgent.ResetPath();
            }

            _animation?.SetSpeed(0f);
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

    /// <summary>Optional job an NPC performs when the scenario does not own the conversation
    /// (e.g. the konbini clerk rings up the basket). Return true when the interaction was handled.</summary>
    public interface INpcService
    {
        bool HandleInteract(GameObject player);
    }

    /// <summary>An <see cref="INpcService"/> that runs before the scenario gets the interaction.</summary>
    public interface IPriorityNpcService : INpcService
    {
    }
}
