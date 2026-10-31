using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Spinner + progress strip for the in-game video player (preparing / buffering). Spins in Play Mode only.</summary>
    public sealed class VideoPlayerStatus : MonoBehaviour
    {
        public const float DegreesPerSecond = 270f;
        private RectTransform _spinner, _fill;
        public bool Visible { get; private set; }
        public float Progress { get; private set; }

        public static VideoPlayerStatus Create(RectTransform parent)
        {
            var root = UiKit.Rect(parent, "LoadingStatus");
            UiKit.Pin(root, new Vector2(0.5f, 0.5f), 0f, 40f, 360f, 120f);
            var s = root.gameObject.AddComponent<VideoPlayerStatus>();
            var ring = UiKit.Image(root, "Spinner", UiTheme.Teal, UiKit.Rounded(32));
            ring.fillCenter = false;
            ring.type = Image.Type.Sliced;
            s._spinner = ring.rectTransform;
            UiKit.Pin(s._spinner, new Vector2(0.5f, 1f), 0f, 0f, 56f, 56f);
            var gap = UiKit.Image(s._spinner, "Gap", Color.black);
            UiKit.Pin(gap.rectTransform, new Vector2(1f, 1f), 0f, 0f, 30f, 30f);
            var track = UiKit.Image(root, "Track", new Color(1f, 1f, 1f, 0.15f), UiKit.Rounded(3));
            UiKit.Pin(track.rectTransform, new Vector2(0.5f, 0f), 0f, 4f, 320f, 6f);
            var fill = UiKit.Image(track.rectTransform, "Fill", UiTheme.Teal, UiKit.Rounded(3));
            s._fill = fill.rectTransform;
            s._fill.anchorMin = Vector2.zero; s._fill.anchorMax = new Vector2(0f, 1f); s._fill.pivot = new Vector2(0f, 0.5f);
            s._fill.offsetMin = s._fill.offsetMax = Vector2.zero;
            s.Set(false, 0f);
            return s;
        }

        /// <summary>Shows/hides the status; progress &lt; 0 hides the strip (indeterminate, e.g. buffering).</summary>
        public void Set(bool visible, float progress)
        {
            Visible = visible;
            Progress = progress;
            gameObject.SetActive(visible);
            if (_fill == null) return;
            _fill.parent.gameObject.SetActive(progress >= 0f);
            _fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        private void Update()
        {
            if (Application.isPlaying && _spinner != null) _spinner.Rotate(0f, 0f, -DegreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
