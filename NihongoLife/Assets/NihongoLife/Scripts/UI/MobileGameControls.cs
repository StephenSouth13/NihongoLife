using NihongoLife.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NihongoLife.UI
{
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField, Min(20f)] private float radius = 72f;
        private RectTransform _area;
        private GameInputService _input;

        private void Awake()
        {
            _area = transform as RectTransform;
            _input = GameInputService.GetOrCreate();
        }

        public void Configure(RectTransform joystickHandle, float movementRadius)
        {
            handle = joystickHandle;
            radius = Mathf.Max(20f, movementRadius);
        }

        public void OnPointerDown(PointerEventData eventData) => UpdateStick(eventData);
        public void OnDrag(PointerEventData eventData) => UpdateStick(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            _input.SetMobileMove(Vector2.zero);
        }

        private void UpdateStick(PointerEventData eventData)
        {
            if (_area == null || handle == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
            Vector2 normalized = Vector2.ClampMagnitude(point / radius, 1f);
            handle.anchoredPosition = normalized * radius;
            _input.SetMobileMove(normalized);
        }
    }

    public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private GameInputId action = GameInputId.Jump;
        private GameInputService _input;

        private void Awake() => _input = GameInputService.GetOrCreate();
        public void Configure(GameInputId inputAction) => action = inputAction;
        public void OnPointerDown(PointerEventData eventData) => _input.SetMobileButton(action, true);
        public void OnPointerUp(PointerEventData eventData) => _input.SetMobileButton(action, false);
        private void OnDisable() => _input?.SetMobileButton(action, false);
    }
}
