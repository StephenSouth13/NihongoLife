#if UNITY_EDITOR
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Builds the main menu into 01_MainMenu and saves it, so the Editor shows exactly what Play shows: a dark glass
    /// column on the left (title, tagline, the menu list with icons), the account button and the language switch at the
    /// top right, the learner line at the bottom. Sprites and icons are assets (Resources/UI), texts are the Vietnamese
    /// defaults — at runtime MainMenuUI only translates the texts and wires the buttons (it never moves or restyles).
    /// The orbit camera's framing is saved too (it used to be set at runtime).
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.MainMenuBaker.Bake
    /// </summary>
    public static class MainMenuBaker
    {
        private const string Art = "Assets/NihongoLife/Resources/UI/Art/";
        private const string Icons = "Assets/NihongoLife/Resources/UI/Icons/";

        // Layout (1920×1080 reference): every runtime element is here and only here.
        public const float ColumnX = 110f, ColumnW = 440f, ListTop = 380f, RowH = 54f, RowGap = 10f;

        public static readonly (string name, string icon, string vi, bool primary)[] Menu =
        {
            ("StartButton", "play_arrow", "Bắt đầu", true),
            ("OnlineBtn", "public", "Chơi online", false),
            ("ProfileBtn", "person", "Hồ sơ", false),
            ("FriendsBtn", "groups", "Bạn bè", false),
            ("LeaderboardBtn", "leaderboard", "Xếp hạng", false),
            ("GuideButton", "help", "Cách chơi", false),
            ("SettingsButton", "settings", "Cài đặt", false),
            ("QuitButton", "logout", "Thoát", false),
        };

        public static void Bake()
        {
            try
            {
                BakeScene();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>Builds the menu into the saved scene (also called by SceneBuilder after it rebuilds 01_MainMenu).</summary>
        public static void BakeScene()
        {
            {
                var scene = EditorSceneManager.OpenScene(MenuSyncCheck.ScenePath, OpenSceneMode.Single);
                var panel = GameObject.Find("Canvas/MainMenuPanel");
                if (panel == null) throw new System.Exception("Canvas/MainMenuPanel not found");
                var menu = panel.GetComponent<MainMenuUI>();
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/NihongoLife/Fonts/NotoSansJP SDF.asset");

                // Start clean: the old buttons were positioned / restyled at runtime.
                for (int i = panel.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);
                var panelImage = panel.GetComponent<Image>();
                if (panelImage != null) { panelImage.color = new Color(0f, 0f, 0f, 0f); panelImage.raycastTarget = false; }

                // Left glass column with a soft edge into the town.
                var shade = Img(panel.transform, "ColumnShade", new Color(0.03f, 0.045f, 0.07f, 0.95f), Load(Art + "fade_h.png"));
                shade.rectTransform.anchorMin = new Vector2(0f, 0f); shade.rectTransform.anchorMax = new Vector2(0f, 1f);
                shade.rectTransform.pivot = new Vector2(0f, 0.5f); shade.rectTransform.sizeDelta = new Vector2(1180f, 0f); shade.rectTransform.anchoredPosition = Vector2.zero;

                // Brand
                var accent = Img(panel.transform, "TopAccent", UiTheme.Gold, Load(Art + "round_8.png"));
                TopLeft(accent.rectTransform, ColumnX, 132f, 64f, 6f);
                var title = Text(panel.transform, "TitleText", "NIHONGO LIFE", font, 66f, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
                TopLeft(title.rectTransform, ColumnX, 150f, 760f, 96f);
                title.characterSpacing = 4f;
                var kana = Text(panel.transform, "BrandKana", "日本語ライフ", font, 24f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
                TopLeft(kana.rectTransform, ColumnX + 4f, 246f, 400f, 36f);
                kana.characterSpacing = 6f;
                var subtitle = Text(panel.transform, "SubtitleText", "Sống một ngày ở Nhật — học tiếng Nhật qua từng việc nhỏ", font, 20f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft);
                subtitle.textWrappingMode = TextWrappingModes.Normal;
                TopLeft(subtitle.rectTransform, ColumnX + 2f, 296f, 520f, 64f);

                // Menu list
                float y = ListTop;
                foreach (var (name, icon, vi, primary) in Menu)
                {
                    float h = primary ? 68f : RowH;
                    var b = MenuButton(panel.transform, name, icon, vi, font, primary, h);
                    TopLeft((RectTransform)b.transform, ColumnX, y, primary ? ColumnW : ColumnW, h);
                    y += h + RowGap + (primary ? 6f : 0f);
                }

                // About link + learner line
                var about = Pill(panel.transform, "AboutButton", "Về tác giả", font, false, "info", 0.78f);
                TopRight((RectTransform)about.transform, -696f, 40f, 196f, 52f);
                var profile = Text(panel.transform, "ProfileText", "", font, 15f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.BottomLeft);
                profile.textWrappingMode = TextWrappingModes.Normal;
                BottomLeft(profile.rectTransform, ColumnX, 36f, 760f, 90f);

                // Top right: language switch + account
                var langBar = Img(panel.transform, "LanguageBar", new Color(0.035f, 0.05f, 0.075f, 0.78f), Load(Art + "round_22.png"));
                TopRight(langBar.rectTransform, -48f, 40f, 414f, 52f);
                var langIcon = Img(langBar.transform, "Icon", UiTheme.TextSoft, Load(Icons + "ic_translate.png"));
                langIcon.preserveAspect = true;
                TopLeft(langIcon.rectTransform, 14f, 14f, 24f, 24f);
                var label = Text(langBar.transform, "LanguageLabel", "Ngôn ngữ", font, 1f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
                label.gameObject.SetActive(false); // kept for old references; the icon says "language"
                float lx = 48f;
                foreach (var (name, text) in new[] { ("LanguageVietnameseButton", "Tiếng Việt"), ("LanguageEnglishButton", "English"), ("LanguageJapaneseButton", "日本語") })
                {
                    bool on = name == "LanguageVietnameseButton";
                    var lb = Pill(langBar.transform, name, text, font, on);
                    TopLeft((RectTransform)lb.transform, lx, 6f, 116f, 40f);
                    lx += 120f;
                }
                var login = Pill(panel.transform, "LoginButton", "Đăng nhập", font, false, "account_circle", 0.78f);
                TopRight((RectTransform)login.transform, -480f, 40f, 200f, 52f);

                // Menu references
                var so = new SerializedObject(menu);
                SetRef(so, "startButton", panel.transform.Find("StartButton").GetComponent<Button>());
                SetRef(so, "quitButton", panel.transform.Find("QuitButton").GetComponent<Button>());
                SetRef(so, "vietnameseButton", langBar.transform.Find("LanguageVietnameseButton").GetComponent<Button>());
                SetRef(so, "englishButton", langBar.transform.Find("LanguageEnglishButton").GetComponent<Button>());
                SetRef(so, "japaneseButton", langBar.transform.Find("LanguageJapaneseButton").GetComponent<Button>());
                SetRef(so, "guideButton", panel.transform.Find("GuideButton").GetComponent<Button>());
                SetRef(so, "aboutButton", about);
                SetRef(so, "titleText", title);
                SetRef(so, "subtitleText", subtitle);
                SetRef(so, "profileText", profile);
                SetRef(so, "startButtonText", panel.transform.Find("StartButton/Text").GetComponent<TextMeshProUGUI>());
                SetRef(so, "quitButtonText", panel.transform.Find("QuitButton/Text").GetComponent<TextMeshProUGUI>());
                SetRef(so, "languageLabelText", label);
                so.ApplyModifiedPropertiesWithoutUndo();

                // Camera framing that MainMenuUI used to set at runtime.
                var orbit = Object.FindFirstObjectByType<MenuCameraOrbit>();
                if (orbit != null)
                {
                    var oso = new SerializedObject(orbit);
                    oso.FindProperty("radius").floatValue = 44f;
                    oso.FindProperty("height").floatValue = 15.5f;
                    oso.FindProperty("orbitSpeed").floatValue = 2.15f;
                    oso.FindProperty("startAngle").floatValue = 205f;
                    oso.FindProperty("lookHeight").floatValue = 3.1f;
                    oso.ApplyModifiedPropertiesWithoutUndo();
                    var target = oso.FindProperty("target").objectReferenceValue as Transform;
                    if (target != null)
                    {
                        float r = 205f * Mathf.Deg2Rad;
                        orbit.transform.position = target.position + new Vector3(Mathf.Sin(r) * 44f, 15.5f, Mathf.Cos(r) * 44f);
                        orbit.transform.LookAt(target.position + Vector3.up * 3.1f);
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[MainMenuBaker] 01_MainMenu baked: " + string.Join(", ", panel.transform.Cast<Transform>().Select(t => t.name)));
            }
        }

        // ─────────── Builders ───────────

        private static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new System.Exception("Missing sprite " + path);

        private static Image Img(Transform parent, string name, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null) { image.sprite = sprite; image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple; }
            return image;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(8f, size * 0.7f);
            return t;
        }

        private static Button MenuButton(Transform parent, string name, string icon, string label, TMP_FontAsset font, bool primary, float height)
        {
            Color bg = primary ? UiTheme.Gold : new Color(1f, 1f, 1f, 0.07f);
            Color fg = primary ? UiTheme.OnGold : UiTheme.Text;
            var image = Img(parent, name, bg, Load(Art + "round_16.png"));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = colors.highlightedColor = colors.pressedColor = colors.selectedColor = Color.white; button.colors = colors;
            if (!primary)
            {
                var border = Img(image.transform, "Border", new Color(1f, 1f, 1f, 0.12f), Load(Art + "stroke_16.png"));
                Stretch(border.rectTransform, 0f, 0f, 0f, 0f);
            }
            var ic = Img(image.transform, "Icon", fg, Load(Icons + "ic_" + icon + ".png"));
            ic.preserveAspect = true;
            var r = ic.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f); r.anchoredPosition = new Vector2(22f, 0f); r.sizeDelta = new Vector2(primary ? 28f : 24f, primary ? 28f : 24f);
            var text = Text(image.transform, "Text", label, font, primary ? 22f : 18f, fg, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform, 64f, 0f, 16f, 0f);
            return button;
        }

        private static Button LinkButton(Transform parent, string name, string icon, string label, TMP_FontAsset font)
        {
            var image = Img(parent, name, new Color(0f, 0f, 0f, 0f), null);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var ic = Img(image.transform, "Icon", UiTheme.Muted, Load(Icons + "ic_" + icon + ".png"));
            ic.preserveAspect = true;
            var r = ic.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f); r.anchoredPosition = new Vector2(0f, 0f); r.sizeDelta = new Vector2(20f, 20f);
            var text = Text(image.transform, "Text", label, font, 15f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform, 28f, 0f, 0f, 0f);
            return button;
        }

        private static Button Pill(Transform parent, string name, string label, TMP_FontAsset font, bool selected, string icon = null, float glass = 0f)
        {
            Color bg = selected ? UiTheme.Gold : glass > 0f ? new Color(0.035f, 0.05f, 0.075f, glass) : new Color(1f, 1f, 1f, 0f);
            Color fg = selected ? UiTheme.OnGold : UiTheme.Text;
            var image = Img(parent, name, bg, Load(Art + "round_22.png"));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = colors.highlightedColor = colors.pressedColor = colors.selectedColor = Color.white; button.colors = colors;
            float left = 0f;
            if (icon != null)
            {
                var ic = Img(image.transform, "Icon", fg, Load(Icons + "ic_" + icon + ".png"));
                ic.preserveAspect = true;
                var r = ic.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f); r.anchoredPosition = new Vector2(16f, 0f); r.sizeDelta = new Vector2(24f, 24f);
                left = 46f;
            }
            var text = Text(image.transform, "Text", label, font, 16f, fg, FontStyles.Bold, icon != null ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center);
            Stretch(text.rectTransform, icon != null ? left : 8f, 0f, icon != null ? 14f : 8f, 0f);
            return button;
        }

        // ─────────── Placement (1920×1080 reference) ───────────

        private static void TopLeft(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void TopRight(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void BottomLeft(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void Stretch(RectTransform r, float left, float top, float? right, float bottom)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(left, bottom);
            r.offsetMax = new Vector2(-(right ?? 0f), -top);
        }

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
        }
    }
}
#endif
