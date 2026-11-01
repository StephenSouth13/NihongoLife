using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Hover / press feedback that keeps an element's saved resting colour (baked scenes look the same in the Editor
    /// and in Play): lighter on hover, darker on press, a slight scale. <see cref="SetBase"/> changes the resting colour.
    /// </summary>
    public sealed class UiHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        private Graphic _graphic;
        private Color _base;
        private bool _hasBase, _over;
        private Vector3 _scale = Vector3.one;
        private static float _lastHoverSound;

        private void Awake() { _graphic = GetComponent<Button>()?.targetGraphic ?? GetComponent<Graphic>(); Capture(); _scale = transform.localScale; }
        private void Capture() { if (_graphic != null && !_hasBase) { _base = _graphic.color; _hasBase = true; } }

        public void SetBase(Color color)
        {
            if (_graphic == null) _graphic = GetComponent<Button>()?.targetGraphic ?? GetComponent<Graphic>();
            _base = color; _hasBase = true;
            if (_graphic != null) _graphic.color = _over ? Hover(color) : color;
        }

        private static Color Hover(Color c) => c.a < 0.2f ? new Color(1f, 1f, 1f, c.a + 0.1f) : UiKit.Lighten(c, 0.12f);
        private bool Interactable => GetComponent<Button>() == null || GetComponent<Button>().interactable;

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
        {
            _over = true;
            if (_graphic == null || !Interactable) return;
            _graphic.color = Hover(_base);
            if (!NihongoLife.Core.MotionSettings.ReduceMotion) transform.localScale = _scale * 1.035f;
            if (Time.unscaledTime - _lastHoverSound < 0.07f) return;
            _lastHoverSound = Time.unscaledTime;
            if (NihongoLife.Core.GameServices.TryGet(out NihongoLife.Audio.IAudioService audio)) audio.PlayCue(NihongoLife.Audio.GameAudioCue.UiHover, 0.35f);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
        {
            _over = false;
            if (_graphic != null) _graphic.color = _base;
            transform.localScale = _scale;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
        {
            if (_graphic == null || !Interactable) return;
            _graphic.color = UiKit.Darken(_base.a < 0.2f ? new Color(1f, 1f, 1f, _base.a + 0.06f) : _base, 0.12f);
            if (!NihongoLife.Core.MotionSettings.ReduceMotion)
                transform.localScale = new Vector3(_scale.x * 1.06f, _scale.y * 0.9f, _scale.z);
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)
        {
            if (_graphic != null) _graphic.color = _over ? Hover(_base) : _base;
            transform.localScale = _over && !NihongoLife.Core.MotionSettings.ReduceMotion ? _scale * 1.02f : _scale;
        }

        private void OnDisable() { _over = false; if (_graphic != null && _hasBase) _graphic.color = _base; transform.localScale = _scale; }
    }
}
