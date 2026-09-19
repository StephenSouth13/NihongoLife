using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NihongoLife.UI
{
    public class CharacterPreviewDragRotate : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private float degreesPerPixel = 0.45f;
        private Action<float> _onYawChanged;
        private float _yaw;

        public void Configure(Action<float> onYawChanged)
        {
            _onYawChanged = onYawChanged;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _yaw = 0f;
            _onYawChanged?.Invoke(_yaw);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _yaw = Mathf.Clamp(_yaw - eventData.delta.x * degreesPerPixel, -90f, 90f);
            _onYawChanged?.Invoke(_yaw);
        }
    }
}
