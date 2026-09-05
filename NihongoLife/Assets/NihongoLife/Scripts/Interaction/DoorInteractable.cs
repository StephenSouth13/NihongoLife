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
        [SerializeField] private string promptEn = "Mở cửa";
        [SerializeField] private Transform doorVisual;
        [SerializeField] private Transform leftDoorPanel;
        [SerializeField] private Transform rightDoorPanel;
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private bool enterAfterOpening = true;
        [SerializeField] private Vector3 interiorOffset = new Vector3(0f, -1.1f, 2.9f);

        [Header("Animation Settings")]
        [SerializeField] private DoorType doorType = DoorType.Slide;
        [SerializeField] private float openDuration = 0.45f;

        [Header("Swing Settings")]
        [SerializeField] private float openAngle = 95f;

        [Header("Slide Settings")]
        [SerializeField] private Vector3 slideOffset = new Vector3(-1.2f, 0f, 0f);
        [SerializeField] private Vector3 leftSlideOffset = new Vector3(-0.95f, 0f, 0f);
        [SerializeField] private Vector3 rightSlideOffset = new Vector3(0.95f, 0f, 0f);

        private bool _isOpen;
        private Coroutine _openRoutine;

        public string GetPromptJa() => _isOpen ? "入る" : promptJa;
        public string GetpromptEn() => _isOpen ? "Vào cửa hàng" : promptEn;
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
            Open();
            if (scenarioManager != null && scenarioManager.CanEnterArea(areaId))
            {
                scenarioManager.OnAreaEntered(areaId);
            }

            if (enterAfterOpening && player != null)
            {
                StartCoroutine(MovePlayerInsideAfterDoorOpens(player));
            }
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
            if (doorType == DoorType.Swing)
            {
                yield return AnimateSwing();
                yield break;
            }

            if (leftDoorPanel != null && rightDoorPanel != null)
            {
                yield return AnimateSlidingPair();
                yield break;
            }

            yield return AnimateSingleSlide();
        }

        private IEnumerator AnimateSwing()
        {
            float elapsed = 0f;
            Quaternion start = doorVisual.localRotation;
            Quaternion end = start * Quaternion.Euler(0f, openAngle, 0f);

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutCubic(elapsed / openDuration);
                doorVisual.localRotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }

            doorVisual.localRotation = end;
        }

        private IEnumerator AnimateSingleSlide()
        {
            float elapsed = 0f;
            Vector3 start = doorVisual.localPosition;
            Vector3 end = start + slideOffset;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutCubic(elapsed / openDuration);
                doorVisual.localPosition = Vector3.Lerp(start, end, t);
                yield return null;
            }

            doorVisual.localPosition = end;
        }

        private IEnumerator AnimateSlidingPair()
        {
            float elapsed = 0f;
            Vector3 leftStart = leftDoorPanel.localPosition;
            Vector3 rightStart = rightDoorPanel.localPosition;
            Vector3 leftEnd = leftStart + leftSlideOffset;
            Vector3 rightEnd = rightStart + rightSlideOffset;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutCubic(elapsed / openDuration);
                leftDoorPanel.localPosition = Vector3.Lerp(leftStart, leftEnd, t);
                rightDoorPanel.localPosition = Vector3.Lerp(rightStart, rightEnd, t);
                yield return null;
            }

            leftDoorPanel.localPosition = leftEnd;
            rightDoorPanel.localPosition = rightEnd;
        }

        private IEnumerator MovePlayerInsideAfterDoorOpens(GameObject player)
        {
            yield return new WaitForSeconds(openDuration + 0.05f);
            if (player == null) yield break;

            Vector3 destination = transform.TransformPoint(interiorOffset);
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.transform.position = destination;

            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        private static float EaseOutCubic(float value)
        {
            float t = Mathf.Clamp01(value);
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
