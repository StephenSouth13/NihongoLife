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

        private float _nextCheckTime;
        private IInteractable _currentInteractable;

        public event Action<IInteractable> OnInteractableChanged;

        public IInteractable CurrentInteractable => _currentInteractable;

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
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, interactableLayers);
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
                    float dist = Vector3.Distance(transform.position, interactable.GetTransform().position);
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
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
