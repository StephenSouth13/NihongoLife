using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Keeps a shadow image aligned with its card (same rect grown by the blur, dropped a little).</summary>
    [ExecuteAlways]
    public sealed class UiFollowRect : MonoBehaviour
    {
        private RectTransform _target, _self;
        private float _grow, _drop;

        public void Configure(RectTransform target, float grow, float drop)
        {
            _target = target; _grow = grow; _drop = drop;
            _self = (RectTransform)transform;
            Sync();
        }

        private void OnEnable() { Canvas.willRenderCanvases += Sync; Sync(); }
        private void OnDisable() => Canvas.willRenderCanvases -= Sync;
        private void LateUpdate() => Sync();

        private void Sync()
        {
            if (this == null) { Canvas.willRenderCanvases -= Sync; return; }
            if (_self == null) return;
            if (_target == null) { if (Application.isPlaying) Destroy(gameObject); else gameObject.SetActive(false); return; }
            // Follow the card in its parent's space, even when a layout group moved it after Configure.
            if (_target.parent != _self.parent)
            {
                _self.position = _target.position + new Vector3(0f, -_drop * _target.lossyScale.y, 0f);
                _self.sizeDelta = _target.rect.size + new Vector2(_grow * 2f, _grow * 2f);
                return;
            }
            _self.anchorMin = _target.anchorMin;
            _self.anchorMax = _target.anchorMax;
            _self.pivot = _target.pivot;
            _self.anchoredPosition = _target.anchoredPosition + new Vector2(0f, -_drop);
            _self.sizeDelta = _target.sizeDelta + new Vector2(_grow * 2f, _grow * 2f);
            _self.localScale = _target.localScale;
        }
    }
}
