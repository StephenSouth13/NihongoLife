using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// Keeps a centered modal inside the parent with a margin, without scaling its children.
    /// Text stays the authored size; a scroll view inside the card handles overflow.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MenuSafeFrame : MonoBehaviour
    {
        [SerializeField] private Vector2 maxSize = new Vector2(1120f, 740f);
        [SerializeField] private Vector2 minSize = new Vector2(640f, 420f);
        [SerializeField] private Vector2 margin = new Vector2(36f, 28f);

        private RectTransform _rect;
        private RectTransform _parent;

        public void Configure(Vector2 maximum, Vector2 minimum, Vector2 edgeMargin)
        {
            maxSize = maximum;
            minSize = minimum;
            margin = edgeMargin;
            Apply();
        }

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _parent = _rect.parent as RectTransform;
            Apply();
        }

        private void LateUpdate()
        {
            if (_parent == null) return;
            Apply();
        }

        private void Apply()
        {
            if (_rect == null || _parent == null) return;
            Vector2 parentSize = _parent.rect.size;
            if (parentSize.x < 2f || parentSize.y < 2f) return;
            float width = Mathf.Clamp(parentSize.x - margin.x * 2f, minSize.x, maxSize.x);
            float height = Mathf.Clamp(parentSize.y - margin.y * 2f, minSize.y, maxSize.y);
            width = Mathf.Min(width, parentSize.x - 16f);
            height = Mathf.Min(height, parentSize.y - 16f);
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = new Vector2(width, height);
        }
    }
}
