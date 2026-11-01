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
            var s = root.gameObject.Need<VideoPlayerStatus>();
            var ring = UiKit.Image(root, "Spinner", UiTheme.Teal, ArcSprite());
            ring.type = Image.Type.Simple;
            ring.preserveAspect = true;
            ring.raycastTarget = false;
            s._spinner = ring.rectTransform;
            UiKit.Pin(s._spinner, new Vector2(0.5f, 1f), 0f, 0f, 56f, 56f);
            var track = UiKit.Image(root, "Track", new Color(1f, 1f, 1f, 0.15f), UiKit.Rounded(3));
            UiKit.Pin(track.rectTransform, new Vector2(0.5f, 0f), 0f, 4f, 320f, 6f);
            var fill = UiKit.Image(track.rectTransform, "Fill", UiTheme.Teal, UiKit.Rounded(3));
            s._fill = fill.rectTransform;
            s._fill.anchorMin = Vector2.zero; s._fill.anchorMax = new Vector2(0f, 1f); s._fill.pivot = new Vector2(0f, 0.5f);
            s._fill.offsetMin = s._fill.offsetMax = Vector2.zero;
            s.Set(false, 0f);
            return s;
        }

        private static Sprite _arc;

        /// <summary>Thin anti-aliased ring with a 270° arc whose tail fades out (a real spinner, not a filled disc).</summary>
        public static Sprite ArcSprite()
        {
            if (_arc != null) return _arc;
            const int size = 128;
            const float outer = 62f, thickness = 9f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "VideoSpinnerArc" };
            var px = new Color32[size * size];
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = Mathf.Clamp01(Mathf.Min(outer - r, r - (outer - thickness)) + 0.5f);
                    float ang = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - 90f, 360f) / 360f; // 0 at top, clockwise sweep
                    float t = 1f - ang;
                    float a = t > 0.75f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.75f) * 1.15f);
                    byte v = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(edge * a));
                    px[y * size + x] = new Color32(255, 255, 255, v);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _arc = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _arc.name = "VideoSpinnerArc";
            return _arc;
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
