using System;
using NihongoLife.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NihongoLife.UI
{
    /// <summary>Drag (mouse or touch) spins the character preview through a full turn. Idle spin is optional.</summary>
    public class CharacterPreviewDragRotate : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float degreesPerPixel = 0.45f;
        [SerializeField] private float spinDegreesPerSecond = 12f;

        private Action<float> _onYawChanged;
        private float _holdSpinUntil;

        public float Yaw { get; private set; }
        public bool Dragging { get; private set; }
        public bool AutoSpin { get; set; } = true;

        public void Configure(Action<float> onYawChanged) => _onYawChanged = onYawChanged;

        public void SetYaw(float yaw)
        {
            Yaw = yaw;
            _onYawChanged?.Invoke(Yaw);
        }

        public void AddYaw(float degrees)
        {
            _holdSpinUntil = Time.unscaledTime + 1.4f;
            SetYaw(Yaw + degrees);
        }

        public void OnBeginDrag(PointerEventData eventData) => Dragging = true;

        public void OnDrag(PointerEventData eventData)
        {
            Dragging = true;
            SetYaw(Yaw - eventData.delta.x * degreesPerPixel);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Dragging = false;
            _holdSpinUntil = Time.unscaledTime + 0.8f;
        }

        private void Update()
        {
            if (!AutoSpin || Dragging || MotionSettings.ReduceMotion) return;
            if (!isActiveAndEnabled) return;
            if (Time.unscaledTime < _holdSpinUntil) return;
            SetYaw(Yaw + spinDegreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
