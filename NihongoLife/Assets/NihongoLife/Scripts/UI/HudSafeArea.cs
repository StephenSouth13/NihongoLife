using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// Keeps a full-screen container inside the device safe area (notches, rounded corners, browser
    /// UI bars). Put it on a stretched RectTransform directly under the Canvas and parent the HUD
    /// below it. On desktop the safe area is the whole screen, so nothing changes there.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HudSafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastResolution;

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _lastSafeArea = new Rect(-1f, -1f, 0f, 0f);
            Apply();
        }

        private void Update()
        {
            if (UiScreen.SafeArea != _lastSafeArea || UiScreen.Width != _lastResolution.x || UiScreen.Height != _lastResolution.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rect == null || UiScreen.Width <= 0 || UiScreen.Height <= 0) return;

            Rect safe = UiScreen.SafeArea;
            _lastSafeArea = safe;
            _lastResolution = new Vector2Int(UiScreen.Width, UiScreen.Height);

            _rect.anchorMin = new Vector2(safe.xMin / UiScreen.Width, safe.yMin / UiScreen.Height);
            _rect.anchorMax = new Vector2(safe.xMax / UiScreen.Width, safe.yMax / UiScreen.Height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
