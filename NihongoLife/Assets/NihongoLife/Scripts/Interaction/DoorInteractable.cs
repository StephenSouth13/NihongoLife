using System.Collections;
using UnityEngine;
using NihongoLife.Scenario;

namespace NihongoLife.Interaction
{
    public enum DoorType { Swing, Slide }

    [RequireComponent(typeof(Collider))]
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string areaId = "store_entrance";
        [SerializeField] private string promptJa = "ドアを開ける";
        [SerializeField] private string promptVi = "Mở cửa";
        [SerializeField] private Transform doorVisual;
        [SerializeField] private Collider blockingCollider;
        
        [Header("Animation Settings")]
        [SerializeField] private DoorType doorType = DoorType.Slide;
        [SerializeField] private float openDuration = 0.5f;
        
        [Header("Swing Settings")]
        [SerializeField] private float openAngle = 95f;
        
        [Header("Slide Settings")]
        [SerializeField] private Vector3 slideOffset = new Vector3(-1.2f, 0, 0);

        private bool _isOpen;
        private Coroutine _openRoutine;

        public string GetPromptJa() => _isOpen ? "入る" : promptJa;
        public string GetPromptVi() => _isOpen ? "Vào cửa hàng" : promptVi;
        public Transform GetTransform() => transform;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (doorVisual == null)
            {
                doorVisual = transform;
            }
        }

        public void Interact(GameObject player)
        {
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null && !scenarioManager.CanEnterArea(areaId))
            {
                Debug.Log($"[DoorInteractable] Door '{areaId}' is not the active scenario target.");
                return;
            }

            Open();
            scenarioManager?.OnAreaEntered(areaId);
        }

        public void Open()
        {
            if (_isOpen) return;

            _isOpen = true;
            if (blockingCollider != null)
            {
                blockingCollider.enabled = false;
            }

            if (_openRoutine != null)
            {
                StopCoroutine(_openRoutine);
            }

            _openRoutine = StartCoroutine(AnimateOpen());
        }

        private IEnumerator AnimateOpen()
        {
            float elapsed = 0f;
            
            if (doorType == DoorType.Swing)
            {
                Quaternion start = doorVisual.localRotation;
                Quaternion end = start * Quaternion.Euler(0f, openAngle, 0f);

                while (elapsed < openDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / openDuration);
                    // Smooth easing (Ease Out)
                    float easedT = 1f - Mathf.Pow(1f - t, 3f); 
                    doorVisual.localRotation = Quaternion.Slerp(start, end, easedT);
                    yield return null;
                }
                doorVisual.localRotation = end;
            }
            else if (doorType == DoorType.Slide)
            {
                Vector3 start = doorVisual.localPosition;
                Vector3 end = start + slideOffset;

                while (elapsed < openDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / openDuration);
                    // Smooth easing (Ease Out)
                    float easedT = 1f - Mathf.Pow(1f - t, 3f);
                    doorVisual.localPosition = Vector3.Lerp(start, end, easedT);
                    yield return null;
                }
                doorVisual.localPosition = end;
            }
        }
    }
}
