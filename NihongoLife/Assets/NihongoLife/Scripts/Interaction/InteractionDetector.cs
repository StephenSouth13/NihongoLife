using System;
using System.Collections.Generic;
using NihongoLife.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NihongoLife.Interaction
{
    /// <summary>
    /// Picks what F talks to. In a crowd the choice has to be obvious and controllable:
    /// · score = distance + how far it is from where the player faces and from the middle of the screen − door/priority
    ///   bonus; the current target keeps a bonus (hysteresis) so it does not flicker between two people;
    /// · Q cycles through everyone in reach (nearest first), a left click on someone selects them;
    ///   a manual choice sticks until they leave reach or 6 s pass;
    /// · InteractionHighlight marks the target in the world (arrow + "[F] name", "Q 2/4" when several);
    /// · distance is measured to the nearest collider surface (a long counter is as near as its nearest edge), and
    ///   something behind a wall loses to what the player can see; clicking the selected target again uses it.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRadius = 2.0f;
        [SerializeField] private LayerMask interactableLayers;
        [SerializeField] private float checkInterval = 0.05f; // 20 Hz: the target follows the player within a few frames
        [SerializeField] private bool fallbackToAnyLayer = true;

        private const float StickyBonus = 0.3f; // enough to stop flicker, small enough that walking up to something new wins
        private const float ManualHoldSeconds = 6f;

        private float _nextCheckTime;
        private IInteractable _currentInteractable;
        private IInteractable _manual;
        private float _manualUntil;
        private float _selectedAt = -10f;
        private readonly Collider[] _overlapBuffer = new Collider[48];
        private readonly List<(IInteractable item, float score)> _candidates = new List<(IInteractable, float)>();
        private readonly HashSet<IInteractable> _seen = new HashSet<IInteractable>();
        private readonly Dictionary<IInteractable, (float distance, Collider collider)> _nearest = new Dictionary<IInteractable, (float, Collider)>();
        private readonly RaycastHit[] _sightHits = new RaycastHit[8];
        private const float HiddenPenalty = 2.5f; // out of sight: only chosen when nothing visible is near

        public event Action<IInteractable> OnInteractableChanged;

        public IInteractable CurrentInteractable => _currentInteractable;
        /// <summary>How many things are in reach (the HUD shows "Q: đổi (i/n)" when more than one).</summary>
        public int CandidateCount => _candidates.Count;
        public int CurrentIndex => _currentInteractable == null ? -1 : _candidates.FindIndex(c => c.item == _currentInteractable);
        public IReadOnlyList<(IInteractable item, float score)> Candidates => _candidates;

        private void Awake()
        {
            detectionRadius = Mathf.Max(detectionRadius, 3.6f);
            if (GetComponent<InteractionHighlight>() == null) gameObject.AddComponent<InteractionHighlight>();
        }

        private void Update()
        {
            var input = GameInputService.Instance;
            if (!NihongoLife.UI.UiModalStack.BlocksHotkeys)
            {
                if (input != null && input.WasPressed(GameInputId.CycleTarget)) CycleTarget();
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TrySelectUnderCursor();
            }

            if (Time.time >= _nextCheckTime)
            {
                _nextCheckTime = Time.time + checkInterval;
                DetectInteractables();
            }
        }

        private void DetectInteractables()
        {
            Vector3 detectionOrigin = transform.position + Vector3.up * 0.9f;
            // Other players (RemotePlayerAvatar) live on Default: always include it, or a nearby NPC on the
            // interactable layer would hide everyone else.
            int colliderCount = Physics.OverlapSphereNonAlloc(detectionOrigin, detectionRadius, _overlapBuffer, interactableLayers | 1, QueryTriggerInteraction.Collide);
            if (colliderCount == 0 && fallbackToAnyLayer)
                colliderCount = Physics.OverlapSphereNonAlloc(detectionOrigin, detectionRadius, _overlapBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);

            var cam = Camera.main;
            Vector3 camForward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : transform.forward;
            _candidates.Clear();
            _seen.Clear();
            _nearest.Clear();
            for (int i = 0; i < colliderCount; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null) continue;
                var interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;
                float d = SurfaceDistance(col, detectionOrigin);
                if (_seen.Add(interactable))
                {
                    if (interactable is UnityEngine.Object unityObject && unityObject == null) { _seen.Remove(interactable); continue; }
                    if (interactable is IConditionalInteractable conditional && !conditional.IsInteractionAvailable) continue;
                    if (interactable is Component component && component.gameObject == gameObject) continue;
                    _nearest[interactable] = (d, col);
                }
                else if (_nearest.TryGetValue(interactable, out var known) && d < known.distance) _nearest[interactable] = (d, col);
            }
            foreach (var pair in _nearest)
                _candidates.Add((pair.Key, Score(pair.Key, pair.Value.distance, pair.Value.collider, detectionOrigin, camForward)));
            _candidates.Sort((a, b) => a.score.CompareTo(b.score));

            IInteractable best = null;
            if (_manual != null && Time.time < _manualUntil && _candidates.Exists(c => c.item == _manual)) best = _manual;
            else
            {
                _manual = null;
                float bestScore = float.MaxValue;
                foreach (var (item, score) in _candidates)
                {
                    float s = item == _currentInteractable ? score - StickyBonus : score;
                    if (s < bestScore) { bestScore = s; best = item; }
                }
            }
            SetCurrent(best);
        }

        /// <summary>Lower is better.</summary>
        private float Score(IInteractable interactable, float distance, Collider collider, Vector3 origin, Vector3 camForward)
        {
            Vector3 target = interactable.GetTransform().position;
            Vector3 flat = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            float horizontal = flat.magnitude;
            Vector3 dir = horizontal > 0.001f ? flat / horizontal : transform.forward;
            float facing = Vector3.Dot(transform.forward, dir);   // where the character looks
            float view = Vector3.Dot(camForward, dir);             // where the camera (screen centre) looks
            float priority = interactable is IInteractionPriority p ? p.InteractionPriority : 0f;
            float hidden = Visible(collider, origin) ? 0f : HiddenPenalty;
            // Things behind the character are much less likely to be meant.
            return distance + (1f - facing) * 0.75f + (1f - view) * 0.45f - priority + hidden;
        }

        /// <summary>Distance to the nearest point of the collider (its pivot can be metres away on a counter or a door).</summary>
        private static float SurfaceDistance(Collider col, Vector3 origin)
        {
            bool closestWorks = col is BoxCollider || col is SphereCollider || col is CapsuleCollider || (col is MeshCollider mesh && mesh.convex);
            Vector3 point = closestWorks ? col.ClosestPoint(origin) : col.bounds.ClosestPoint(origin);
            return Vector3.ProjectOnPlane(point - origin, Vector3.up).magnitude;
        }

        /// <summary>No solid wall between the player's chest and the target (triggers, people and the target itself do not block).</summary>
        private bool Visible(Collider target, Vector3 origin)
        {
            Vector3 aim = target.bounds.center;
            aim.y = Mathf.Clamp(aim.y, origin.y - 0.6f, origin.y + 0.6f);
            Vector3 ray = aim - origin;
            float length = ray.magnitude;
            if (length < 0.3f) return true;
            int count = Physics.RaycastNonAlloc(origin, ray / length, _sightHits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = _sightHits[i].collider;
                if (hit == null || hit == target || hit.transform.IsChildOf(transform)) continue;
                if (hit is CharacterController || hit.attachedRigidbody != null) continue; // people and moving things never "hide" (no flicker)
                if (hit.GetComponentInParent<IInteractable>() != null) continue;  // the target itself, a counter, another person
                return false;
            }
            return true;
        }

        private void SetCurrent(IInteractable next)
        {
            if (next == _currentInteractable) return;
            _currentInteractable = next;
            OnInteractableChanged?.Invoke(_currentInteractable);
        }

        /// <summary>Q: next thing in reach (nearest first), wrapping round.</summary>
        public void CycleTarget()
        {
            DetectInteractables();
            if (_candidates.Count < 2) return;
            int index = CurrentIndex;
            var next = _candidates[(index + 1) % _candidates.Count].item;
            Select(next);
        }

        /// <summary>Makes something the target (click or cycle); it stays the target for a few seconds.</summary>
        public void Select(IInteractable interactable)
        {
            if (interactable == null) return;
            _manual = interactable;
            _manualUntil = Time.time + ManualHoldSeconds;
            SetCurrent(interactable);
        }

        private void TrySelectUnderCursor()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 60f, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;
                if (interactable is Component c && c.gameObject == gameObject) continue;
                if (Vector3.Distance(interactable.GetTransform().position, transform.position) > detectionRadius * 1.5f) return;
                DetectInteractables();
                if (!_candidates.Exists(x => x.item == interactable)) return;
                // A second click on the chosen person / thing uses it (mouse and touch players need no F).
                if (interactable == _currentInteractable && _manual == interactable && Time.time - _selectedAt < 4f) { TriggerInteraction(); return; }
                Select(interactable);
                _selectedAt = Time.time;
                return;
            }
        }

        public void TriggerInteraction()
        {
            if (_currentInteractable == null) DetectInteractables();
            if (_currentInteractable != null && !(_currentInteractable is UnityEngine.Object o && o == null))
            {
                _currentInteractable.Interact(gameObject);
                return;
            }
            Debug.Log("[InteractionDetector] No interactable nearby. Move closer to an NPC, item, or door.");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, detectionRadius);
        }
    }
}
