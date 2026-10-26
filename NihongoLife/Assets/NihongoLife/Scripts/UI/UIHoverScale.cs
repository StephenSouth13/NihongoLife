using UnityEngine;
using UnityEngine.EventSystems;

namespace NihongoLife.UI
{
    public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 originalScale;
        private Vector3 targetScale;
        private float scaleSpeed = 15f;

        public float hoverScale = 1.05f;
        public float pressScale = 0.95f;

        private void Start()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;
        }

        private void Update()
        {
            if (NihongoLife.Core.MotionSettings.ReduceMotion)
            {
                targetScale = originalScale;
                if (originalScale.sqrMagnitude > 0.001f) transform.localScale = originalScale;
                return;
            }
            if (transform.localScale != targetScale)
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (NihongoLife.Core.MotionSettings.ReduceMotion) return;
            targetScale = originalScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (NihongoLife.Core.MotionSettings.ReduceMotion) return;
            targetScale = new Vector3(originalScale.x * 1.06f, originalScale.y * 0.9f, originalScale.z);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (NihongoLife.Core.MotionSettings.ReduceMotion) { targetScale = originalScale; return; }
            targetScale = eventData.pointerEnter == gameObject ? originalScale * hoverScale : originalScale;
        }
    }
}
