using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Stretches the top cluster between the brand column and the right edge.</summary>
    public sealed class MenuTopBar : MonoBehaviour
    {
        /// <summary>Gap under the bottom bar on narrow screens (canvas units).</summary>
        public const float BottomLift = 58f;
        private RectTransform _rect;
        private RectTransform _parent;

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _parent = _rect.parent as RectTransform;
            Apply();
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (_rect == null || _parent == null) return;
            float width = _parent.rect.width;
            if (width < 2f) return;
            float left = Mathf.Clamp(width * 0.34f, 520f, 760f);
            if (left > width - 280f) left = Mathf.Max(280f, width * 0.42f);
            // Drop to a bottom bar whenever the buttons would not fit beside the brand column (wide-but-short phone browsers too).
            bool narrow = width < 980f || width - left - 20f < WideNeed();
            if (narrow)
            {
                _rect.anchorMin = new Vector2(0f, 0f);
                _rect.anchorMax = new Vector2(1f, 0f);
                _rect.pivot = new Vector2(0.5f, 0f);
                // Lifted clear of the browser's "Fullscreen" pill, which phone pages show at the bottom centre (WebGL template).
                _rect.offsetMin = new Vector2(12f, BottomLift);
                _rect.offsetMax = new Vector2(-12f, BottomLift + 52f);
                Tighten(true);
                return;
            }
            Tighten(false);
            _rect.anchorMin = new Vector2(0f, 1f);
            _rect.anchorMax = new Vector2(1f, 1f);
            _rect.pivot = new Vector2(1f, 1f);
            _rect.offsetMin = new Vector2(left, -76f);
            _rect.offsetMax = new Vector2(-20f, -16f);
        }

        private float WideNeed()
        {
            float need = 0f; int count = 0;
            foreach (RectTransform child in _rect)
            {
                if (!child.gameObject.activeSelf || child.GetComponent<LayoutElement>() == null) continue;
                need += child.name == "LanguageBar" ? 280f : 140f; count++;
            }
            var group = _rect.GetComponent<HorizontalLayoutGroup>();
            if (group != null) need += group.spacing * Mathf.Max(0, count - 1) + group.padding.horizontal;
            return need;
        }

        private void Tighten(bool narrow)
        {
            foreach (RectTransform child in _rect)
            {
                var element = child.GetComponent<LayoutElement>();
                if (element == null) continue;
                bool language = child.name == "LanguageBar";
                element.minWidth = narrow ? (language ? 120f : 72f) : (language ? 280f : 140f);
                // Long labels ("????????? ??????????: ????.", "Ti?ng Vi?t" on a narrow bar) shrink, then wrap to two lines, never "?".
                foreach (var label in child.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                {
                    if (label.name.IndexOf("Icon", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (label.textWrappingMode == TMPro.TextWrappingModes.Normal && label.fontSizeMin <= 9f) continue;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 9f;
                    label.fontSizeMax = Mathf.Max(label.fontSizeMax, 16f);
                    label.textWrappingMode = TMPro.TextWrappingModes.Normal;
                    label.overflowMode = TMPro.TextOverflowModes.Truncate;
                }
            }
        }
    }
}
