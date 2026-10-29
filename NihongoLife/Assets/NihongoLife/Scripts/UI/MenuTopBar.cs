using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Stretches the top cluster between the brand column and the right edge.</summary>
    public sealed class MenuTopBar : MonoBehaviour
    {
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
            bool narrow = width < 980f;
            if (narrow)
            {
                _rect.anchorMin = new Vector2(0f, 0f);
                _rect.anchorMax = new Vector2(1f, 0f);
                _rect.pivot = new Vector2(0.5f, 0f);
                _rect.offsetMin = new Vector2(12f, 10f);
                _rect.offsetMax = new Vector2(-12f, 62f);
                Tighten(true);
                return;
            }
            Tighten(false);
            float left = Mathf.Clamp(width * 0.34f, 520f, 760f);
            if (left > width - 280f) left = Mathf.Max(280f, width * 0.42f);
            _rect.anchorMin = new Vector2(0f, 1f);
            _rect.anchorMax = new Vector2(1f, 1f);
            _rect.pivot = new Vector2(1f, 1f);
            _rect.offsetMin = new Vector2(left, -76f);
            _rect.offsetMax = new Vector2(-20f, -16f);
        }

        private void Tighten(bool narrow)
        {
            foreach (RectTransform child in _rect)
            {
                var element = child.GetComponent<LayoutElement>();
                if (element == null) continue;
                bool language = child.name == "LanguageBar";
                element.minWidth = narrow ? (language ? 120f : 72f) : (language ? 280f : 140f);
            }
        }
    }
}
