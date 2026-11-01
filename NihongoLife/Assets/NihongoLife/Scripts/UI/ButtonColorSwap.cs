using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Swaps a target Image's color on hover/press instead of relying on ColorBlock
    /// multiply, so procedurally-set base colors stay exact and consistent everywhere.</summary>
    public class ButtonColorSwap : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        private Image _image;
        private Color _base;
        private Color _hover;
        private Color _pressed;

        public void Configure(Image image, Color baseColor, Color hoverColor, Color pressedColor)
        {
            _image = image;
            _base = baseColor;
            _hover = hoverColor;
            _pressed = pressedColor;
            if (_image != null) _image.color = _base;
        }

        /// <summary>Lets callers (e.g. a "selected" toggle state) change the resting color
        /// without losing hover/press feedback.</summary>
        public void SetBaseColor(Color baseColor)
        {
            _base = baseColor;
            if (_image != null) _image.color = _base;
        }

        private static float _lastHoverSound;

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_image != null) _image.color = _hover;

            // Soft hover tick; skipped for disabled buttons and rate-limited so sweeping the pointer is not noisy.
            var button = GetComponent<UnityEngine.UI.Button>();
            if (button != null && !button.interactable) return;
            if (Time.unscaledTime - _lastHoverSound < 0.07f) return;
            _lastHoverSound = Time.unscaledTime;
            if (NihongoLife.Core.GameServices.TryGet(out NihongoLife.Audio.IAudioService audio)) audio.PlayCue(NihongoLife.Audio.GameAudioCue.UiHover, 0.35f);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_image != null) _image.color = _base;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_image != null) _image.color = _pressed;
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_image != null) _image.color = _base;
        }
    }
}
