using System.Collections;
using UnityEngine;
using NihongoLife.Scenario;

namespace NihongoLife.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string areaId = "store_entrance";
        [SerializeField] private string promptJa = "ドアを開ける";
        [SerializeField] private string promptVi = "Mở cửa";
        [SerializeField] private Transform doorVisual;
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private float openAngle = 95f;
        [SerializeField] private float openDuration = 0.35f;

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
            Quaternion start = doorVisual.localRotation;
            Quaternion end = Quaternion.Euler(0f, openAngle, 0f);
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                doorVisual.localRotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }

            doorVisual.localRotation = end;
        }
    }
}
