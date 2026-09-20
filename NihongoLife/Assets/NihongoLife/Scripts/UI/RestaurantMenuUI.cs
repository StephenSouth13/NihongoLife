using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    // Shared runtime menu used by every restaurant menu-board interaction.
    /// <summary>
    /// Runtime-built restaurant menu viewer: category tabs, specialty tab, useful phrases and
    /// etiquette, with a detail pane per dish. All content comes from a RestaurantMenuDefinition
    /// asset, so this class never needs to change when the menu is expanded or edited.
    /// </summary>
    public class RestaurantMenuUI : MonoBehaviour
    {
        private enum TabKind { Category, Specialty, Phrases, Etiquette }

        private class TabInfo
        {
            public TabKind kind;
            public string categoryId;
            public string label;
        }

        private class EntryInfo
        {
            public string title;
            public string subtitle;
            public string detail;
        }

        private static RestaurantMenuUI _instance;

        private GameObject _panel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private TextMeshProUGUI _hintText;
        private TextMeshProUGUI _detailText;
        private RectTransform _tabsRoot;
        private RectTransform _listRoot;
        private ScrollRect _detailScroll;
        private ScrollRect _listScroll;
        private TMP_FontAsset _font;
        private RestaurantMenuDefinition _menu;
        private int _tabIndex;
        private readonly List<TabInfo> _tabs = new List<TabInfo>();
        private readonly List<EntryInfo> _entries = new List<EntryInfo>();
        private readonly List<ButtonColorSwap> _rowSwaps = new List<ButtonColorSwap>();

        public static RestaurantMenuUI GetOrCreate()
        {
            if (_instance != null) return _instance;

            var existing = FindFirstObjectByType<RestaurantMenuUI>();
            if (existing != null)
            {
                _instance = existing;
                return existing;
            }

            var host = new GameObject("RestaurantMenuUI");
            _instance = host.AddComponent<RestaurantMenuUI>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            BuildUI();
            _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (_panel != null && _panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Show(RestaurantMenuDefinition menu)
        {
            if (menu == null || _panel == null) return;

            _menu = menu;
            _tabIndex = 0;
            RefreshHeader();
            RebuildTabs();
            SelectTab(0);

            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            UIStyleKit.PlayShowAnimation(_panel);
            ScenarioManager.Instance?.SetPlayerInputLocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (_panel == null || !_panel.activeSelf) return;

            _panel.SetActive(false);
            ScenarioManager.Instance?.SetPlayerInputLocked(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ──────────────────────── Content ────────────────────────

        private void RefreshHeader()
        {
            string reading = string.IsNullOrWhiteSpace(_menu.restaurantNameReading) ? string.Empty : $"  <size=60%><color=#AFC0CF>{_menu.restaurantNameReading}</color></size>";
            _titleText.text = $"{_menu.restaurantNameJa}{reading}";

            string localizedName = Pick(_menu.restaurantNameVi, _menu.restaurantNameEn, _menu.restaurantNameJa);
            string tagline = Pick(_menu.taglineVi, _menu.taglineEn, string.Empty);
            _subtitleText.text = string.IsNullOrWhiteSpace(tagline) ? localizedName : $"{localizedName}  ·  {tagline}";
            _hintText.text = Pick("ESC: đóng thực đơn  |  Chọn mục bên trái để xem chi tiết", "ESC: close menu  |  Pick an entry on the left to see details", "ESC: 閉じる  |  左の項目を選ぶと詳しい説明が見られます");
        }

        private void RebuildTabs()
        {
            _tabs.Clear();

            if (_menu.categories != null)
            {
                foreach (var category in _menu.categories)
                {
                    if (category == null) continue;
                    string local = Pick(category.titleVi, category.titleEn, category.titleReading);
                    _tabs.Add(new TabInfo { kind = TabKind.Category, categoryId = category.id, label = $"{category.titleJa}\n<size=75%>{local}</size>" });
                }
            }

            if (_menu.GetSpecialties().Count > 0)
            {
                _tabs.Add(new TabInfo { kind = TabKind.Specialty, label = $"名物\n<size=75%>{Pick("Đặc sản của quán", "House specialties", "おすすめ")}</size>" });
            }

            if (_menu.phrases != null && _menu.phrases.Count > 0)
            {
                _tabs.Add(new TabInfo { kind = TabKind.Phrases, label = $"会話\n<size=75%>{Pick("Câu nói hữu ích", "Useful phrases", "使える表現")}</size>" });
            }

            if (_menu.etiquette != null && _menu.etiquette.Count > 0)
            {
                _tabs.Add(new TabInfo { kind = TabKind.Etiquette, label = $"マナー\n<size=75%>{Pick("Phép lịch sự", "Etiquette", "マナー")}</size>" });
            }
        }

        private void SelectTab(int index)
        {
            if (_tabs.Count == 0) return;

            _tabIndex = Mathf.Clamp(index, 0, _tabs.Count - 1);
            ClearChildren(_tabsRoot);
            for (int i = 0; i < _tabs.Count; i++)
            {
                int captured = i;
                bool selected = i == _tabIndex;
                var button = CreateRowButton(_tabsRoot, $"Tab_{i}", _tabs[i].label, 62f, selected ? UIStyleKit.AccentGold : UIStyleKit.PanelBase, selected ? UIStyleKit.AccentGoldHover : UIStyleKit.PanelHover, selected ? UIStyleKit.AccentGoldPressed : UIStyleKit.PanelPressed, 20f, selected ? new Color(0.08f, 0.06f, 0.02f, 1f) : Color.white);
                button.onClick.AddListener(() => SelectTab(captured));
            }

            BuildEntries(_tabs[_tabIndex]);
            RebuildList();
            if (_entries.Count > 0)
            {
                SelectEntry(0);
            }
            else
            {
                _detailText.text = Pick("Chưa có nội dung.", "Nothing here yet.", "まだ内容がありません。");
            }
        }

        private void BuildEntries(TabInfo tab)
        {
            _entries.Clear();
            switch (tab.kind)
            {
                case TabKind.Category:
                    foreach (var dish in _menu.GetDishesInCategory(tab.categoryId)) _entries.Add(DishEntry(dish));
                    break;
                case TabKind.Specialty:
                    foreach (var dish in _menu.GetSpecialties()) _entries.Add(DishEntry(dish));
                    break;
                case TabKind.Phrases:
                    foreach (var phrase in _menu.phrases)
                    {
                        if (phrase == null) continue;
                        _entries.Add(PhraseEntry(phrase));
                    }
                    break;
                case TabKind.Etiquette:
                    foreach (var note in _menu.etiquette)
                    {
                        if (note == null) continue;
                        _entries.Add(new EntryInfo
                        {
                            title = string.IsNullOrWhiteSpace(note.titleJa) ? note.titleVi : note.titleJa,
                            subtitle = note.titleVi,
                            detail = $"<size=140%><b>{note.titleJa}</b></size>\n<color=#AFC0CF>{note.titleVi}</color>\n\n{note.bodyVi}"
                        });
                    }
                    break;
            }
        }

        private EntryInfo DishEntry(RestaurantDish dish)
        {
            var sb = new StringBuilder();
            sb.Append("<size=140%><b>").Append(dish.nameJa).Append("</b></size>\n");
            sb.Append("<color=#AFC0CF>").Append(dish.reading);
            if (!string.IsNullOrWhiteSpace(dish.romaji)) sb.Append("  ·  ").Append(dish.romaji);
            sb.Append("</color>\n");
            sb.Append(Pick(dish.nameVi, dish.nameEn, dish.nameJa)).Append("    <color=#F1B83F><b>¥").Append(dish.priceYen.ToString("N0")).Append("</b></color>\n");
            if (dish.isSpecialty)
            {
                sb.Append("<color=#F1B83F>★ ").Append(Pick("Đặc sản của quán", "House specialty", "お店の名物")).Append("</color>\n");
            }

            sb.Append('\n').Append(Pick(dish.descriptionVi, dish.descriptionEn, dish.descriptionJa)).Append("\n\n");
            AppendField(sb, Pick("Nguyên liệu", "Ingredients", "材料"), dish.ingredientsVi != null && dish.ingredientsVi.Count > 0 ? string.Join(", ", dish.ingredientsVi) : "-");
            AppendField(sb, Pick("Lưu ý dị ứng", "Allergens", "アレルギー"), dish.allergensVi != null && dish.allergensVi.Count > 0 ? string.Join(", ", dish.allergensVi) : Pick("Không có", "None", "なし"));
            if (!string.IsNullOrWhiteSpace(dish.eatingTipVi)) AppendField(sb, Pick("Cách ăn", "How to eat", "食べ方"), dish.eatingTipVi);
            if (!string.IsNullOrWhiteSpace(dish.specialtyNoteVi)) AppendField(sb, Pick("Về món này", "About this dish", "この料理について"), dish.specialtyNoteVi);

            return new EntryInfo
            {
                title = $"{dish.nameJa}    <color=#F1B83F>¥{dish.priceYen:N0}</color>",
                subtitle = Pick(dish.nameVi, dish.nameEn, dish.reading),
                detail = sb.ToString()
            };
        }

        private EntryInfo PhraseEntry(RestaurantPhrase phrase)
        {
            var sb = new StringBuilder();
            sb.Append("<size=140%><b>").Append(phrase.ja).Append("</b></size>\n");
            sb.Append("<color=#AFC0CF>").Append(phrase.reading);
            if (!string.IsNullOrWhiteSpace(phrase.romaji)) sb.Append("  ·  ").Append(phrase.romaji);
            sb.Append("</color>\n\n").Append(phrase.vi);
            if (!string.IsNullOrWhiteSpace(phrase.whenVi))
            {
                sb.Append("\n\n");
                AppendField(sb, Pick("Khi nào dùng", "When to use", "使う場面"), phrase.whenVi);
            }

            return new EntryInfo { title = phrase.ja, subtitle = phrase.vi, detail = sb.ToString() };
        }

        private static void AppendField(StringBuilder sb, string label, string value)
        {
            sb.Append("<color=#F1B83F><b>").Append(label).Append(":</b></color> ").Append(value).Append("\n\n");
        }

        private void RebuildList()
        {
            ClearChildren(_listRoot);
            _rowSwaps.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                int captured = i;
                var entry = _entries[i];
                var button = CreateRowButton(_listRoot, $"Entry_{i}", $"{entry.title}\n<size=78%><color=#AFC0CF>{entry.subtitle}</color></size>", 74f, UIStyleKit.PanelBase, UIStyleKit.PanelHover, UIStyleKit.PanelPressed, 20f, Color.white);
                _rowSwaps.Add(button.GetComponent<ButtonColorSwap>());
                button.onClick.AddListener(() => SelectEntry(captured));
            }
        }

        private void SelectEntry(int index)
        {
            if (index < 0 || index >= _entries.Count) return;

            for (int i = 0; i < _rowSwaps.Count; i++)
            {
                if (_rowSwaps[i] != null) _rowSwaps[i].SetBaseColor(i == index ? new Color(0.3f, 0.24f, 0.09f, 0.96f) : UIStyleKit.PanelBase);
            }

            _detailText.text = _entries[index].detail;
            Canvas.ForceUpdateCanvases();
            if (_detailScroll != null) _detailScroll.verticalNormalizedPosition = 1f;
        }

        // ──────────────────────── Localization ────────────────────────

        private static string Pick(string vi, string en, string ja)
        {
            GameLanguage language = GameServices.TryGet(out GameSettingsService settings) ? settings.Language : GameLanguage.Vietnamese;
            string value = language == GameLanguage.English ? en : language == GameLanguage.Japanese ? ja : vi;
            if (string.IsNullOrWhiteSpace(value)) value = !string.IsNullOrWhiteSpace(vi) ? vi : !string.IsNullOrWhiteSpace(en) ? en : ja;
            return value ?? string.Empty;
        }

        // ──────────────────────── UI construction ────────────────────────

        private void BuildUI()
        {
            _font = ResolveFont();
            Canvas canvas = ResolveCanvas();
            _panel = new GameObject("RestaurantMenuPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);
            var panelRect = (RectTransform)_panel.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1180f, 700f);
            UIStyleKit.StylePanel(panelRect, UIStyleKit.PanelBase);

            _titleText = CreateText(panelRect, "Title", string.Empty, new Vector2(28f, -30f), new Vector2(920f, 48f), 32f, FontStyles.Bold);
            _subtitleText = CreateText(panelRect, "Subtitle", string.Empty, new Vector2(28f, -76f), new Vector2(980f, 30f), 19f, FontStyles.Normal);
            _subtitleText.color = new Color(0.69f, 0.75f, 0.81f, 1f);
            _hintText = CreateText(panelRect, "Hint", string.Empty, new Vector2(28f, -660f), new Vector2(1120f, 30f), 16f, FontStyles.Normal);
            _hintText.color = new Color(0.56f, 0.64f, 0.72f, 1f);

            Button close = CreateRowButton(panelRect, "Close", "X", 44f, UIStyleKit.AccentGold, UIStyleKit.AccentGoldHover, UIStyleKit.AccentGoldPressed, 22f, new Color(0.08f, 0.06f, 0.02f, 1f), false);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = Vector2.one;
            closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-24f, -24f);
            closeRect.sizeDelta = new Vector2(52f, 44f);
            close.onClick.AddListener(Close);

            CreateScroll(panelRect, "Tabs", new Vector2(24f, -116f), new Vector2(250f, 530f), out _tabsRoot, new Color(0.025f, 0.033f, 0.04f, 0.6f));
            _listScroll = CreateScroll(panelRect, "List", new Vector2(290f, -116f), new Vector2(390f, 530f), out _listRoot, new Color(0.025f, 0.033f, 0.04f, 0.6f));
            _detailScroll = CreateScroll(panelRect, "Detail", new Vector2(696f, -116f), new Vector2(460f, 530f), out RectTransform detailContent, new Color(0.025f, 0.033f, 0.04f, 0.72f));

            _detailText = CreateText(detailContent, "DetailText", string.Empty, Vector2.zero, Vector2.zero, 20f, FontStyles.Normal);
            _detailText.alignment = TextAlignmentOptions.TopLeft;
            _detailText.richText = true;
            _detailText.lineSpacing = 6f;
        }

        private ScrollRect CreateScroll(Transform parent, string name, Vector2 position, Vector2 size, out RectTransform content, Color background)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            root.transform.SetParent(parent, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = size;
            root.GetComponent<Image>().color = background;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(rootRect, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportRect, false);
            content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            return scroll;
        }

        private Button CreateRowButton(Transform parent, string name, string label, float height, Color baseColor, Color hoverColor, Color pressedColor, float fontSize, Color textColor, bool useLayoutElement = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            if (useLayoutElement)
            {
                go.AddComponent<LayoutElement>().preferredHeight = height;
            }

            var button = go.GetComponent<Button>();
            UIStyleKit.StyleButton(button, baseColor, hoverColor, pressedColor);

            var text = CreateText(go.transform, "Label", label, Vector2.zero, Vector2.zero, fontSize, FontStyles.Bold);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = new Vector2(14f, 4f);
            textRect.offsetMax = new Vector2(-14f, -4f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = textColor;
            text.raycastTarget = false;
            return button;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.richText = true;
            return text;
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private static TMP_FontAsset ResolveFont()
        {
            var anyText = FindFirstObjectByType<TextMeshProUGUI>();
            return anyText != null && anyText.font != null ? anyText.font : TMP_Settings.defaultFontAsset;
        }

        private static Canvas ResolveCanvas()
        {
            var hud = FindFirstObjectByType<HUDUI>();
            Canvas canvas = hud != null ? hud.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas;

            var canvasObject = new GameObject("RestaurantMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }
    }
}
