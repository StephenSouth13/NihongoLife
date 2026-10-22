using NihongoLife.Core;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>Slow gold wash across the menu title. A colour lerp only — no extra materials.</summary>
    public sealed class MenuTitleShimmer : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private Color _base;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            if (_text != null) _base = _text.color;
        }

        private void Update()
        {
            if (_text == null) return;
            if (MotionSettings.ReduceMotion)
            {
                _text.color = _base;
                return;
            }
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.15f);
            _text.color = Color.Lerp(_base, new Color(1f, 0.9f, 0.62f, _base.a), wave * 0.35f);
        }
    }
}
