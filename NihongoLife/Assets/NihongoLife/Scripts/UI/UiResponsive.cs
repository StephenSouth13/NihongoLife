using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Picks the canvas match so portrait phones, 4:3, 16:9, and ultrawide all keep the menu on screen.</summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvasFit : MonoBehaviour
    {
        CanvasScaler _scaler;
        Vector2 _lastRef;
        float _lastMatch;
        int _lastW, _lastH;

        public static void Ensure(Transform any)
        {
            var canvas = any != null ? any.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.GetComponent<ResponsiveCanvasFit>() != null) return;
            canvas.gameObject.AddComponent<ResponsiveCanvasFit>();
        }

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _lastW || Screen.height != _lastH) Apply();
        }

        private void Apply()
        {
            if (_scaler == null || Screen.width < 2 || Screen.height < 2) return;
            _lastW = Screen.width;
            _lastH = Screen.height;
            float aspect = (float)Screen.width / Screen.height;
            int shortSide = Mathf.Min(Screen.width, Screen.height);
            Vector2 reference;
            float match;
            if (shortSide <= 500)
            {
                // Phones stay near real pixels so a button is about a finger tall.
                // A landscape phone is often wider than 2:1 and must not use the ultrawide reference.
                if (aspect < 1f)
                {
                    reference = new Vector2(Mathf.Clamp(Screen.width, 360, 480), Mathf.Clamp(Screen.height, 640, 960));
                    match = 0f;
                }
                else
                {
                    reference = new Vector2(Mathf.Clamp(Screen.width, 640, 960), Mathf.Clamp(Screen.height, 320, 480));
                    match = 1f;
                }
            }
            else if (aspect < 0.92f)
            {
                reference = new Vector2(720f, 1280f);
                match = 0f;
            }
            else if (aspect > 2.05f && shortSide >= 900)
            {
                reference = new Vector2(2560f, 1080f);
                match = 1f;
            }
            else if (aspect < 1.45f)
            {
                reference = new Vector2(1440f, 1080f);
                match = 0.5f;
            }
            else
            {
                reference = new Vector2(1920f, 1080f);
                match = 0.5f;
            }
            if (reference == _lastRef && Mathf.Approximately(match, _lastMatch) && _scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                return;
            _lastRef = reference;
            _lastMatch = match;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.referenceResolution = reference;
            _scaler.matchWidthOrHeight = match;
        }
    }

    /// <summary>Insets a stretched rect by the device safe area. A parent <see cref="HudSafeArea"/> already did this.</summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaInset : MonoBehaviour
    {
        RectTransform _rect;
        Rect _last;
        Vector2Int _pixels;

        public static void Ensure(Transform target)
        {
            if (target == null || target.GetComponent<SafeAreaInset>() != null) return;
            if (target.GetComponentInParent<HudSafeArea>() != null) return;
            target.gameObject.AddComponent<SafeAreaInset>();
        }

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _last = new Rect(-1f, -1f, 0f, 0f);
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _last || Screen.width != _pixels.x || Screen.height != _pixels.y) Apply();
        }

        private void Apply()
        {
            if (_rect == null || Screen.width < 2 || Screen.height < 2) return;
            if (_rect.anchorMin != Vector2.zero || _rect.anchorMax != Vector2.one) return;
            _last = Screen.safeArea;
            _pixels = new Vector2Int(Screen.width, Screen.height);
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
            float left = _last.xMin / scale;
            float bottom = _last.yMin / scale;
            float right = (Screen.width - _last.xMax) / scale;
            float top = (Screen.height - _last.yMax) / scale;
            _rect.offsetMin = new Vector2(left, bottom);
            _rect.offsetMax = new Vector2(-right, -top);
        }
    }

    /// <summary>
    /// Repacks the baked main-menu column when the canvas is shorter or narrower than the 1920×1080 design.
    /// Desktop 16:9 keeps the saved positions, which the menu sync test checks.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class MenuResponsive : MonoBehaviour
    {
        struct Saved
        {
            public RectTransform Rect;
            public Vector2 AnchorMin, AnchorMax, Pivot, Position, Size;
            public Vector3 Scale;
            public bool Active;
            public float FontSize;
            public bool HasFont;
        }

        readonly List<Saved> _saved = new List<Saved>();
        bool _captured;
        bool _packed;
        static readonly string[] Names =
        {
            "ColumnShade", "TopAccent", "TitleText", "BrandKana", "SubtitleText",
            "StartButton", "OnlineBtn", "ProfileBtn", "FriendsBtn", "LeaderboardBtn", "GuideButton", "SettingsButton", "QuitButton",
            "StoryCard", "ProfileText"
        };

        public static void Ensure(RectTransform panel)
        {
            if (panel != null && panel.GetComponent<MenuResponsive>() == null) panel.gameObject.AddComponent<MenuResponsive>();
        }

        private void LateUpdate()
        {
            var panel = (RectTransform)transform;
            if (panel.rect.width < 2f || panel.rect.height < 2f) return;
            if (!_captured) Capture();
            bool compact = panel.rect.width < 900f || panel.rect.height < 1000f
                || Mathf.Min(Screen.width, Screen.height) <= 500;
            if (compact) Pack(panel);
            else if (_packed) Restore();
        }

        private void Capture()
        {
            _captured = true;
            foreach (var name in Names)
            {
                var rect = transform.Find(name) as RectTransform;
                if (rect == null) continue;
                var text = rect.GetComponent<TextMeshProUGUI>();
                _saved.Add(new Saved
                {
                    Rect = rect,
                    AnchorMin = rect.anchorMin,
                    AnchorMax = rect.anchorMax,
                    Pivot = rect.pivot,
                    Position = rect.anchoredPosition,
                    Size = rect.sizeDelta,
                    Scale = rect.localScale,
                    Active = rect.gameObject.activeSelf,
                    FontSize = text != null ? text.fontSize : 0f,
                    HasFont = text != null
                });
            }
        }

        private void Pack(RectTransform panel)
        {
            _packed = true;
            float w = panel.rect.width;
            float h = panel.rect.height;
            float margin = 16f;
            bool split = h < 620f && w >= 680f;
            float colW = split ? (w - margin * 2f - 12f) * 0.5f : Mathf.Min(440f, w - margin * 2f);
            bool shortH = h < 820f;
            float bottomReserve = w < 980f ? 76f : 16f;
            float y = shortH ? 6f : 18f;
            Place("ColumnShade", 0f, 0f, w, h, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
            Place("TopAccent", margin, y, 48f, 4f);
            y += 8f;
            float titleW = split ? w - margin * 2f : colW;
            Place("TitleText", margin, y, titleW, shortH ? 34f : 56f);
            SetFont("TitleText", shortH ? 28f : 44f);
            y += shortH ? 36f : 60f;
            Place("BrandKana", margin, y, titleW, 22f);
            SetFont("BrandKana", 15f);
            y += 24f;
            if (h > 760f)
            {
                Place("SubtitleText", margin, y, titleW, 36f);
                y += 40f;
            }
            else Hide("SubtitleText");

            var names = new[] { "StartButton", "OnlineBtn", "ProfileBtn", "FriendsBtn", "LeaderboardBtn", "GuideButton", "SettingsButton", "QuitButton" };
            float available = Mathf.Max(36f, h - bottomReserve - y);
            int rows = split ? 1 + 4 : names.Length;
            float row = Mathf.Clamp((available - 6f * rows) / rows, 36f, shortH ? 48f : 54f);
            if (!split)
            {
                foreach (var name in names)
                {
                    float bh = name == "StartButton" && !shortH ? row + 6f : row;
                    Place(name, margin, y, colW, bh);
                    y += bh + 6f;
                }
            }
            else
            {
                Place("StartButton", margin, y, w - margin * 2f, row);
                y += row + 6f;
                var rest = new[] { "OnlineBtn", "ProfileBtn", "FriendsBtn", "LeaderboardBtn", "GuideButton", "SettingsButton", "QuitButton" };
                for (int i = 0; i < rest.Length; i++)
                {
                    float x = margin + (i % 2) * (colW + 12f);
                    Place(rest[i], x, y, colW, row);
                    if (i % 2 == 1 || i == rest.Length - 1) y += row + 6f;
                }
            }

            bool storyFits = !split && w > colW + 300f && h > 700f;
            if (storyFits) Place("StoryCard", margin + colW + 16f, 20f, Mathf.Min(380f, w - colW - margin * 2f - 16f), 150f);
            else Hide("StoryCard");
            var profile = transform.Find("ProfileText") as RectTransform;
            if (profile == null) return;
            if (split || y > h - bottomReserve)
            {
                profile.gameObject.SetActive(false);
                return;
            }
            profile.gameObject.SetActive(true);
            profile.anchorMin = profile.anchorMax = profile.pivot = new Vector2(0f, 0f);
            profile.anchoredPosition = new Vector2(margin, w < 980f ? 78f : 16f);
            profile.sizeDelta = new Vector2(colW, 40f);
        }

        private void Place(string name, float x, float y, float width, float height)
            => Place(name, x, y, width, height, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        private void Place(string name, float x, float y, float width, float height, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var rect = transform.Find(name) as RectTransform;
            if (rect == null) return;
            rect.gameObject.SetActive(true);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pivot.y > 0.9f ? new Vector2(x, -y) : new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void Hide(string name)
        {
            var rect = transform.Find(name) as RectTransform;
            if (rect != null) rect.gameObject.SetActive(false);
        }

        private void SetFont(string name, float size)
        {
            var text = transform.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Max(12f, size * 0.6f);
            text.fontSize = size;
        }

        private void Restore()
        {
            _packed = false;
            foreach (var saved in _saved)
            {
                if (saved.Rect == null) continue;
                saved.Rect.gameObject.SetActive(saved.Active);
                saved.Rect.anchorMin = saved.AnchorMin;
                saved.Rect.anchorMax = saved.AnchorMax;
                saved.Rect.pivot = saved.Pivot;
                saved.Rect.anchoredPosition = saved.Position;
                saved.Rect.sizeDelta = saved.Size;
                saved.Rect.localScale = saved.Scale;
                if (!saved.HasFont) continue;
                var text = saved.Rect.GetComponent<TextMeshProUGUI>();
                if (text == null) continue;
                text.fontSize = saved.FontSize;
                text.fontSizeMax = saved.FontSize;
            }
        }
    }

    /// <summary>Gives a gameplay HUD a safe-area inset and the shared canvas fitter when the scene did not.</summary>
    public static class HudResponsiveBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
            Apply();
        }

        static void OnScene(Scene scene, LoadSceneMode mode) => Apply();

        static void Apply()
        {
            var huds = Object.FindObjectsByType<HUDUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < huds.Length; i++)
            {
                var hud = huds[i];
                if (hud == null) continue;
                var canvas = hud.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.GetComponent<HudCanvasFitter>() == null)
                    canvas.gameObject.AddComponent<HudCanvasFitter>();
                SafeAreaInset.Ensure(hud.transform);
            }
        }
    }
}
