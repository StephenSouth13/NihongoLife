using System;
using UnityEngine;

namespace NihongoLife.Interaction
{
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRadius = 2.0f;
        [SerializeField] private LayerMask interactableLayers;
        [SerializeField] private float checkInterval = 0.1f;
        [SerializeField] private bool fallbackToAnyLayer = true;

        private float _nextCheckTime;
        private IInteractable _currentInteractable;

        public event Action<IInteractable> OnInteractableChanged;

        public IInteractable CurrentInteractable => _currentInteractable;

        private void Awake()
        {
            detectionRadius = Mathf.Max(detectionRadius, 3.6f);
        }

        private void Update()
        {
            if (Time.time >= _nextCheckTime)
            {
                _nextCheckTime = Time.time + checkInterval;
                DetectInteractables();
            }
        }

        private void DetectInteractables()
        {
            Vector3 detectionOrigin = transform.position + Vector3.up * 0.9f;
            Collider[] colliders = Physics.OverlapSphere(detectionOrigin, detectionRadius, interactableLayers);
            if (colliders.Length == 0 && fallbackToAnyLayer)
            {
                colliders = Physics.OverlapSphere(detectionOrigin, detectionRadius);
            }

            IInteractable closestInteractable = null;
            float minDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                var interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null)
                {
                    interactable = col.GetComponent<IInteractable>();
                }

                if (interactable != null)
                {
                    float dist = Vector3.Distance(detectionOrigin, interactable.GetTransform().position + Vector3.up * 0.9f);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestInteractable = interactable;
                    }
                }
            }

            if (closestInteractable != _currentInteractable)
            {
                _currentInteractable = closestInteractable;
                OnInteractableChanged?.Invoke(_currentInteractable);
            }
        }

        public void TriggerInteraction()
        {
            if (_currentInteractable != null)
            {
                _currentInteractable.Interact(gameObject);
                return;
            }

            DetectInteractables();
            if (_currentInteractable != null)
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
