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

        public static bool IsTouchLayout(bool forced) => forced || Application.isMobilePlatform;

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            _lastResolution = Vector2Int.zero;
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _lastResolution.x || Screen.height != _lastResolution.y || !Mathf.Approximately(Screen.dpi, _lastDpi))
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_scaler == null || Screen.width <= 0 || Screen.height <= 0) return;

            _lastResolution = new Vector2Int(Screen.width, Screen.height);
            _lastDpi = Screen.dpi;

            float referenceHeight = desktopReferenceHeight;
            if (IsTouchLayout(forceTouchLayout))
            {
                // Screen.dpi is 96 x device pixel ratio in browsers, so this approximates CSS pixels.
                float dpiScale = Screen.dpi > 0f ? Mathf.Clamp(Screen.dpi / 96f, 1f, 4f) : 1f;
                float cssHeight = Screen.height / dpiScale;
                referenceHeight = Mathf.Clamp(cssHeight, touchMinReferenceHeight, touchMaxReferenceHeight);
            }

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _scaler.referenceResolution = new Vector2(referenceHeight * 16f / 9f, referenceHeight);
        }
    }
}
