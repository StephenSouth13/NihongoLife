using NihongoLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Chooses the canvas reference resolution for the current device so the HUD stays readable and
    /// compact everywhere (desktop browsers, tablets, phones in either orientation).
    /// Desktop keeps the 1080p design space. Touch devices use a smaller reference height (roughly
    /// the height in CSS pixels), which makes every element physically larger on small screens.
    /// Expand mode guarantees the whole design area is always visible on any aspect ratio.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public class HudCanvasFitter : MonoBehaviour
    {
        [SerializeField] private float desktopReferenceHeight = 1080f;
        [SerializeField] private float touchMinReferenceHeight = 540f;
        [SerializeField] private float touchMaxReferenceHeight = 720f;
        [Tooltip("Editor testing: behave like a phone or tablet even when running on desktop.")]
        [SerializeField] private bool forceTouchLayout;

        private CanvasScaler _scaler;
        private Vector2Int _lastResolution;
        private float _lastDpi;

        public static bool IsTouchLayout(bool forced) =>
            forced || (UiScreen.PhoneOverride ?? (Application.isMobilePlatform || UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld
            || UnityEngine.Device.Application.isMobilePlatform || WebGLPointer.Coarse));

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            _lastResolution = Vector2Int.zero;
            Apply();
        }

        private void Update()
        {
            if (UiScreen.Width != _lastResolution.x || UiScreen.Height != _lastResolution.y || !Mathf.Approximately(UiScreen.Dpi, _lastDpi))
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_scaler == null || UiScreen.Width <= 0 || UiScreen.Height <= 0) return;

            _lastResolution = new Vector2Int(UiScreen.Width, UiScreen.Height);
            _lastDpi = UiScreen.Dpi;

            float referenceHeight = desktopReferenceHeight;
            if (IsTouchLayout(forceTouchLayout))
            {
                // UiScreen.Dpi is 96 x device pixel ratio in browsers, so this approximates CSS pixels.
                float dpiScale = UiScreen.Dpi > 0f ? Mathf.Clamp(UiScreen.Dpi / 96f, 1f, 4f) : 1f;
                float cssHeight = UiScreen.Height / dpiScale;
                referenceHeight = Mathf.Clamp(cssHeight, touchMinReferenceHeight, touchMaxReferenceHeight);
            }

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _scaler.referenceResolution = new Vector2(referenceHeight * 16f / 9f, referenceHeight);
        }
    }
}
