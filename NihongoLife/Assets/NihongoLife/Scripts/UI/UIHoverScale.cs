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
            if (transform.localScale != targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = originalScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = originalScale * pressScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = eventData.pointerEnter == gameObject ? originalScale * hoverScale : originalScale;
        }
    }
}
