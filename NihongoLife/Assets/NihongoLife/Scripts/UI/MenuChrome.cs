using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Pins the baked main-menu chrome inside the canvas: language, account and about stay in one
    /// top-right row, and the learner line stays in the left column. Safe to run again.
    /// </summary>
    public static class MenuChrome
    {
        public static void Arrange(RectTransform panel)
        {
            if (panel == null) return;
            var lang = panel.Find("LanguageBar") as RectTransform;
            var login = panel.Find("LoginButton") as RectTransform;
            var about = panel.Find("AboutButton") as RectTransform;
            if (lang == null) return;

            var cluster = panel.Find("TopCluster") as RectTransform;
            if (cluster == null)
            {
                var go = new GameObject("TopCluster", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(MenuTopBar));
                go.layer = panel.gameObject.layer;
                cluster = go.GetComponent<RectTransform>();
                cluster.SetParent(panel, false);
                var row = go.GetComponent<HorizontalLayoutGroup>();
                row.childAlignment = TextAnchor.MiddleRight;
                row.spacing = 12f;
                row.padding = new RectOffset(8, 8, 4, 4);
                row.childControlWidth = true;
                row.childControlHeight = true;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = true;
            }

            if (about != null && about.parent != cluster) about.SetParent(cluster, false);
            if (login != null && login.parent != cluster) login.SetParent(cluster, false);
            if (lang.parent != cluster) lang.SetParent(cluster, false);

            Prefer(about, 196f, 48f, 140f);
            Prefer(login, 188f, 48f, 140f);
            PrepareLanguageBar(lang);
            var langElement = lang.GetComponent<LayoutElement>() ?? lang.gameObject.AddComponent<LayoutElement>();
            langElement.preferredWidth = 460f;
            langElement.minWidth = 280f;
            langElement.preferredHeight = 48f;
            langElement.flexibleWidth = 1f;

            var profile = panel.Find("ProfileText") as RectTransform;
            if (profile != null)
            {
                profile.anchorMin = profile.anchorMax = profile.pivot = new Vector2(0f, 0f);
                profile.anchoredPosition = new Vector2(MainMenuBakerX, 24f);
                profile.sizeDelta = new Vector2(420f, 78f);
                var text = profile.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.textWrappingMode = TextWrappingModes.Normal;
                    text.overflowMode = TextOverflowModes.Truncate;
                }
            }
        }

        private const float MainMenuBakerX = 110f;

        private static void PrepareLanguageBar(RectTransform lang)
        {
            var row = lang.GetComponent<HorizontalLayoutGroup>();
            if (row == null)
            {
                row = lang.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(10, 8, 6, 6);
                row.spacing = 6f;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = true;
                row.childControlHeight = true;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = true;
            }
            foreach (RectTransform child in lang)
            {
                bool button = child.name.IndexOf("Button", System.StringComparison.Ordinal) >= 0;
                bool hidden = child.name == "LanguageLabel";
                var element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                element.ignoreLayout = hidden;
                element.minWidth = button ? 72f : 28f;
                element.preferredWidth = button ? 120f : 28f;
                element.flexibleWidth = button ? 1f : 0f;
                element.preferredHeight = 36f;
                var label = child.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label == null) continue;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.enableAutoSizing = true;
                label.fontSizeMin = 11f;
                label.fontSizeMax = Mathf.Max(label.fontSizeMax, 16f);
            }
        }

        private static void Prefer(RectTransform rect, float width, float height, float minWidth)
        {
            if (rect == null) return;
            var element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = minWidth;
            element.preferredHeight = height;
            element.flexibleWidth = 0f;
            var label = rect.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) return;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            if (label.fontSizeMax < 16f) label.fontSizeMax = 16f;
        }
    }

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
            float left = Mathf.Clamp(width * 0.34f, 520f, 760f);
            if (left > width - 280f) left = Mathf.Max(280f, width * 0.42f);
            _rect.anchorMin = new Vector2(0f, 1f);
            _rect.anchorMax = new Vector2(1f, 1f);
            _rect.pivot = new Vector2(1f, 1f);
            _rect.offsetMin = new Vector2(left, -76f);
            _rect.offsetMax = new Vector2(-20f, -16f);
        }
    }
}
