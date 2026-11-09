using NihongoLife.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NihongoLife.UI
{
    /// <summary>
    /// The move stick. Input is measured from the stick's centre (not its pivot, which sits on the screen edge: measured
    /// from there a touch in the middle walked the character sideways). One finger owns it until it lifts; no drag
    /// threshold, so a small thumb movement already walks. <see cref="PressAt"/> moves the stick under a finger that
    /// landed anywhere in the stick zone (the touch overlay's look pad forwards it); <see cref="Held"/> tells the layout
    /// not to pull it back home while held.
    /// </summary>
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        private const int None = int.MinValue;
        [SerializeField] private RectTransform handle;
        [SerializeField, Min(20f)] private float radius = 72f;
        private RectTransform _area;
        private GameInputService _input;
        private int _pointer = None;

        public bool Held => _pointer != None;
        public Vector2 Value { get; private set; }

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

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Held) return;
            _pointer = eventData.pointerId;
            UpdateStick(eventData);
        }

        /// <summary>A finger landed in the stick zone, off the stick: the stick comes under it, then follows it.</summary>
        public void PressAt(PointerEventData eventData)
        {
            if (Held || _area == null) return;
            var parent = _area.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                Rect box = parent.rect;
                Vector2 half = _area.rect.size * 0.5f;
                local.x = Mathf.Clamp(local.x, box.xMin + half.x, box.xMax - half.x);
                local.y = Mathf.Clamp(local.y, box.yMin + half.y, box.yMax - half.y);
                Vector3 centre = _area.TransformPoint(_area.rect.center);
                _area.position += parent.TransformPoint(local) - centre;
            }
            _pointer = eventData.pointerId;
            UpdateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointer) UpdateStick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointer) Release();
        }

        public void Release()
        {
            _pointer = None;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (_input != null) _input.SetMobileMove(Vector2.zero); // Unity null: no ?.
        }

        private void OnDisable()
        {
            if (Held) Release();
        }

        private void UpdateStick(PointerEventData eventData)
        {
            if (_area == null || handle == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
            Vector2 normalized = Vector2.ClampMagnitude((point - _area.rect.center) / radius, 1f);
            if (normalized.magnitude < 0.12f) normalized = Vector2.zero; // a resting thumb does not creep
            Value = normalized;
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
        public void OnPointerDown(PointerEventData eventData) { _input.SetMobileButton(action, true); transform.localScale = Vector3.one * 0.9f; }
        public void OnPointerUp(PointerEventData eventData) { _input.SetMobileButton(action, false); transform.localScale = Vector3.one; }
        private void OnDisable() { transform.localScale = Vector3.one; if (_input != null) _input.SetMobileButton(action, false); } // Unity null: no ?.
    }
}
