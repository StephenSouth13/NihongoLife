using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// Shrinks a fixed-size HUD panel so it always fits inside its parent (the safe-area container or
    /// canvas). The authored size is untouched on large screens, so the Scene view shows exactly what
    /// desktop Play Mode shows; on phones and small windows the panel scales down about its pivot.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HudFitRect : MonoBehaviour
    {
        [SerializeField, Range(0.2f, 1f)] private float maxWidthFraction = 1f;
        [SerializeField, Range(0.2f, 1f)] private float maxHeightFraction = 1f;
        [Tooltip("Total space kept free horizontally / vertically (both sides together), in canvas units.")]
        [SerializeField] private Vector2 margin = new Vector2(48f, 48f);
        [SerializeField, Range(0.3f, 1f)] private float minScale = 0.45f;

        private RectTransform _rect;
        private RectTransform _parent;
        private Vector2 _authoredSize;
        private Vector2 _lastParentSize;
        private float _fitScale = 1f;

        /// <summary>Scale currently needed to fit the parent (1 when the panel already fits).</summary>
        public float FitScale => _fitScale;

        public void Configure(float widthFraction, float heightFraction, Vector2 freeMargin, float lowestScale)
        {
            maxWidthFraction = widthFraction;
            maxHeightFraction = heightFraction;
            margin = freeMargin;
            minScale = lowestScale;
            Capture();
            Apply();
        }

        private void OnEnable()
        {
            Capture();
            Apply();
        }

        private void LateUpdate()
        {
            if (_parent == null) return;
            if (_parent.rect.size != _lastParentSize) Apply();
        }

        private void Capture()
        {
            _rect = (RectTransform)transform;
            _parent = _rect.parent as RectTransform;
            _rect.localScale = Vector3.one;
            _authoredSize = _rect.rect.size;
            _lastParentSize = Vector2.negativeInfinity;
        }

        private void Apply()
        {
            if (_rect == null || _parent == null || _authoredSize.x <= 0f || _authoredSize.y <= 0f) return;

            Vector2 parentSize = _parent.rect.size;
            _lastParentSize = parentSize;

            float availableWidth = Mathf.Max(1f, parentSize.x * maxWidthFraction - margin.x);
            float availableHeight = Mathf.Max(1f, parentSize.y * maxHeightFraction - margin.y);
            float scale = Mathf.Min(1f, availableWidth / _authoredSize.x, availableHeight / _authoredSize.y);
            scale = Mathf.Max(minScale, scale);
            _fitScale = scale;
            _rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
