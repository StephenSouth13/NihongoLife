using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    // Shared runtime menu used by every restaurant menu-board interaction.
    /// <summary>
    /// Runtime-built restaurant menu: category tabs, specialty tab, useful phrases and etiquette,
    /// with a detail pane per dish. When opened from a RestaurantTable it also becomes an order
    /// screen (quantity steppers, cart tab, total vs wallet, "place order") and doubles as the
    /// subtitle bar for the staff's Japanese lines. All content comes from a
    /// RestaurantMenuDefinition asset, so this class never needs to change when the menu is edited.
    /// </summary>
    public class RestaurantMenuUI : MonoBehaviour
    {
        private enum TabKind { Category, Specialty, Phrases, Etiquette, Order }

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
            public string dishId;
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

        // Ordering
        private RestaurantTable _table;
        private readonly List<KeyValuePair<string, int>> _cart = new List<KeyValuePair<string, int>>();
        private readonly Dictionary<string, TextMeshProUGUI> _qtyLabels = new Dictionary<string, TextMeshProUGUI>();
        private GameObject _footer;
        private TextMeshProUGUI _cartText;
        private Button _orderButton;
        private TextMeshProUGUI _orderButtonLabel;

        // Staff subtitle bar
        private GameObject _staffBar;
        private TextMeshProUGUI _staffSpeakerText;
        private TextMeshProUGUI _staffLineText;
        private float _staffHideTime;

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

            if (_staffBar != null && _staffBar.activeSelf && Time.unscaledTime >= _staffHideTime)
            {
                _staffBar.SetActive(false);
            }
        }

        /// <summary>Subtitle bar for the staff: Japanese, reading and translation, auto-hides.</summary>
        public void ShowStaffLine(string speaker, string ja, string reading, string translation, float seconds)
        {
            if (_staffBar == null) return;

            _staffSpeakerText.text = speaker;
            var sb = new StringBuilder();
            sb.Append("<size=125%><b>").Append(ja).Append("</b></size>");
            if (!string.IsNullOrWhiteSpace(reading) && reading != ja) sb.Append("\n<size=80%><color=#AFC0CF>").Append(reading).Append("</color></size>");
            if (!string.IsNullOrWhiteSpace(translation)) sb.Append("\n<size=85%><color=#F1B83F>").Append(translation).Append("</color></size>");
            _staffLineText.text = sb.ToString();
            _staffBar.SetActive(true);
            _staffBar.transform.SetAsLastSibling();
            _staffHideTime = Time.unscaledTime + Mathf.Max(2f, seconds);
        }

        public void Show(RestaurantMenuDefinition menu, RestaurantTable table = null)
        {
            if (menu == null || _panel == null) return;

            _menu = menu;
            _table = table;
            _cart.Clear();
            if (_footer != null) _footer.SetActive(_table != null);
            RefreshCartSummary();
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
            SetDefaultHint();
        }

        private void SetDefaultHint()
        {
            _hintText.text = _table != null
                ? Pick("ESC: đóng  |  Dùng nút - / + để chọn số lượng, rồi bấm 注文する để gọi món", "ESC: close  |  Use - / + to pick quantities, then press 注文する to order", "ESC: 閉じる  |  - / + で数を選んで、注文するを押します")
                : Pick("ESC: đóng thực đơn  |  Muốn gọi món? Hãy đến bàn ăn và bấm E", "ESC: close menu  |  Want to order? Go to a table and press E", "ESC: 閉じる  |  注文はテーブルでできます");
        }

        public static string Localize(string vi, string en, string ja) => Pick(vi, en, ja);

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

            if (_table != null)
            {
                _tabs.Add(new TabInfo { kind = TabKind.Order, label = $"注文\n<size=75%>{Pick("Đơn của bạn", "Your order", "ご注文")} ({CartQuantity()})</size>" });
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
                _detailText.text = _tabs[_tabIndex].kind == TabKind.Order ? BuildOrderSummary() : Pick("Chưa có nội dung.", "Nothing here yet.", "まだ内容がありません。");
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
                case TabKind.Order:
                    foreach (var pair in _cart)
                    {
                        var dish = _menu.FindDish(pair.Key);
                        if (dish == null) continue;
                        _entries.Add(new EntryInfo
                        {
                            dishId = dish.id,
                            title = $"{dish.nameJa}    <color=#F1B83F>¥{dish.priceYen:N0}</color>",
                            subtitle = Pick(dish.nameVi, dish.nameEn, dish.reading),
                            detail = string.Empty
                        });
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
                dishId = dish.id,
                title = $"{dish.nameJa}    <color=#F1B83F>¥{dish.priceYen:N0}</color>",
                subtitle = string.IsNullOrWhiteSpace(dish.servingVi) ? Pick(dish.nameVi, dish.nameEn, dish.reading) : $"{Pick(dish.nameVi, dish.nameEn, dish.reading)}  ·  {dish.servingVi}",
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
            _qtyLabels.Clear();
            bool ordering = _table != null;
            for (int i = 0; i < _entries.Count; i++)
            {
                int captured = i;
                var entry = _entries[i];
                bool stepper = ordering && !string.IsNullOrEmpty(entry.dishId);
                Transform rowParent = _listRoot;
                if (stepper)
                {
                    var row = new GameObject($"Row_{i}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                    row.transform.SetParent(_listRoot, false);
                    row.GetComponent<LayoutElement>().preferredHeight = 74f;
                    var layout = row.GetComponent<HorizontalLayoutGroup>();
                    layout.spacing = 4f;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = true;
                    rowParent = row.transform;
                }

                var button = CreateRowButton(rowParent, $"Entry_{i}", $"{entry.title}\n<size=78%><color=#AFC0CF>{entry.subtitle}</color></size>", 74f, UIStyleKit.PanelBase, UIStyleKit.PanelHover, UIStyleKit.PanelPressed, 20f, Color.white);
                _rowSwaps.Add(button.GetComponent<ButtonColorSwap>());
                button.onClick.AddListener(() => SelectEntry(captured));

                if (stepper)
                {
                    button.GetComponent<LayoutElement>().flexibleWidth = 1f;
                    string dishId = entry.dishId;
                    var minus = CreateRowButton(rowParent, "Minus", "-", 74f, UIStyleKit.PanelBase, UIStyleKit.PanelHover, UIStyleKit.PanelPressed, 28f, Color.white);
                    SetFixedWidth(minus, 44f);
                    minus.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    minus.onClick.AddListener(() => ChangeQuantity(dishId, -1));

                    var qtyGo = new GameObject("Qty", typeof(RectTransform), typeof(LayoutElement));
                    qtyGo.transform.SetParent(rowParent, false);
                    qtyGo.GetComponent<LayoutElement>().preferredWidth = 40f;
                    var qty = CreateText(qtyGo.transform, "QtyText", QuantityOf(dishId).ToString(), Vector2.zero, Vector2.zero, 26f, FontStyles.Bold);
                    var qtyRect = qty.rectTransform;
                    qtyRect.anchorMin = Vector2.zero;
                    qtyRect.anchorMax = Vector2.one;
                    qtyRect.offsetMin = Vector2.zero;
                    qtyRect.offsetMax = Vector2.zero;
                    qty.alignment = TextAlignmentOptions.Center;
                    qty.color = UIStyleKit.AccentGold;
                    qty.raycastTarget = false;
                    _qtyLabels[dishId] = qty;

                    var plus = CreateRowButton(rowParent, "Plus", "+", 74f, UIStyleKit.AccentGold, UIStyleKit.AccentGoldHover, UIStyleKit.AccentGoldPressed, 28f, new Color(0.08f, 0.06f, 0.02f, 1f));
                    SetFixedWidth(plus, 44f);
                    plus.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                    plus.onClick.AddListener(() => ChangeQuantity(dishId, +1));
                }
            }
        }

        private static void SetFixedWidth(Button button, float width)
        {
            var element = button.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;
            element.flexibleWidth = 0f;
        }

        // ──────────────────────── Cart ────────────────────────

        private int QuantityOf(string dishId)
        {
            foreach (var pair in _cart)
            {
                if (pair.Key == dishId) return pair.Value;
            }

            return 0;
        }

        private int CartQuantity()
        {
            int total = 0;
            foreach (var pair in _cart) total += pair.Value;
            return total;
        }

        private int CartTotalYen()
        {
            int total = 0;
            foreach (var pair in _cart)
            {
                var dish = _menu != null ? _menu.FindDish(pair.Key) : null;
                if (dish != null) total += dish.priceYen * pair.Value;
            }

            return total;
        }

        private void ChangeQuantity(string dishId, int delta)
        {
            int index = _cart.FindIndex(pair => pair.Key == dishId);
            int current = index >= 0 ? _cart[index].Value : 0;
            int next = Mathf.Clamp(current + delta, 0, RestaurantTable.MaxPerDish);

            if (delta > 0)
            {
                if (index < 0 && _cart.Count >= RestaurantTable.MaxDistinctDishes)
                {
                    _hintText.text = Pick($"Mỗi bàn tối đa {RestaurantTable.MaxDistinctDishes} món khác nhau.", $"Up to {RestaurantTable.MaxDistinctDishes} different dishes per table.", $"1テーブルにつき最大{RestaurantTable.MaxDistinctDishes}種類までです。");
                    return;
                }

                if (CartQuantity() >= RestaurantTable.MaxTotalQuantity)
                {
                    _hintText.text = Pick($"Mỗi bàn tối đa {RestaurantTable.MaxTotalQuantity} phần.", $"Up to {RestaurantTable.MaxTotalQuantity} servings per table.", $"1テーブルにつき最大{RestaurantTable.MaxTotalQuantity}個までです。");
                    return;
                }
            }

            if (next == current) return;

            if (next == 0)
            {
                _cart.RemoveAt(index);
            }
            else if (index >= 0)
            {
                _cart[index] = new KeyValuePair<string, int>(dishId, next);
            }
            else
            {
                _cart.Add(new KeyValuePair<string, int>(dishId, next));
            }

            SetDefaultHint();
            if (_qtyLabels.TryGetValue(dishId, out var label) && label != null) label.text = next.ToString();
            RefreshCartSummary();

            if (_tabs.Count > 0 && _tabs[_tabIndex].kind == TabKind.Order)
            {
                // The order tab lists only what is in the cart, so it is rebuilt when a line disappears.
                if (next == 0) SelectTab(_tabIndex);
                else RefreshOrderDetail();
            }

            UpdateOrderTabLabel();
        }

        private void UpdateOrderTabLabel()
        {
            // Tab buttons are rebuilt only on selection; refresh the counter without losing list scroll.
            int orderIndex = _tabs.FindIndex(tab => tab.kind == TabKind.Order);
            if (orderIndex < 0) return;
            _tabs[orderIndex].label = $"注文\n<size=75%>{Pick("Đơn của bạn", "Your order", "ご注文")} ({CartQuantity()})</size>";
            var button = _tabsRoot.Find($"Tab_{orderIndex}");
            var text = button != null ? button.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (text != null) text.text = _tabs[orderIndex].label;
        }

        private void RefreshCartSummary()
        {
            if (_cartText == null || _menu == null) return;

            int wallet = PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;
            int total = CartTotalYen();
            int quantity = CartQuantity();
            bool enough = PlayerInventory.Instance == null || wallet >= total;
            string totalColor = enough ? "#F1B83F" : "#FF7A6B";
            _cartText.text = $"{Pick("Đã chọn", "Selected", "選択")}: <b>{quantity}</b>   ·   {Pick("Tổng", "Total", "合計")}: <color={totalColor}><b>¥{total:N0}</b></color>   ·   {Pick("Ví", "Wallet", "所持金")}: ¥{wallet:N0}";
            if (_orderButton != null) _orderButton.interactable = quantity > 0;
            if (_orderButtonLabel != null) _orderButtonLabel.color = quantity > 0 ? new Color(0.08f, 0.06f, 0.02f, 1f) : new Color(0.08f, 0.06f, 0.02f, 0.45f);
        }

        private static string StripParentheses(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(value, "[（(][^）)]*[）)]", string.Empty).Trim();
        }

        private string BuildOrderSummary()
        {
            var sb = new StringBuilder();
            sb.Append("<size=140%><b>").Append(Pick("Đơn của bạn", "Your order", "ご注文")).Append("</b></size>\n\n");
            if (_cart.Count == 0)
            {
                sb.Append(Pick("Chưa có món nào. Chọn món ở các tab bên trái rồi dùng nút + để thêm.", "Nothing yet. Pick dishes in the other tabs and use + to add them.", "まだ何もありません。他のタブで料理を選んで + を押してください。"));
                return sb.ToString();
            }

            var ja = new StringBuilder();
            var reading = new StringBuilder();
            for (int i = 0; i < _cart.Count; i++)
            {
                var dish = _menu.FindDish(_cart[i].Key);
                if (dish == null) continue;
                int qty = _cart[i].Value;
                sb.Append(dish.nameJa).Append("  x").Append(qty).Append("   <color=#F1B83F>¥").Append((dish.priceYen * qty).ToString("N0")).Append("</color>\n");
                sb.Append("<size=80%><color=#AFC0CF>").Append(Pick(dish.nameVi, dish.nameEn, dish.reading)).Append("  (").Append(dish.priceYen.ToString("N0")).Append(" x ").Append(qty).Append(")</color></size>\n");

                ja.Append(ja.Length == 0 ? string.Empty : "、").Append(StripParentheses(dish.nameJa)).Append("を").Append(JapaneseNumber.CountKanji(qty));
                reading.Append(reading.Length == 0 ? string.Empty : "、").Append(StripParentheses(string.IsNullOrWhiteSpace(dish.reading) ? dish.nameJa : dish.reading)).Append("を").Append(JapaneseNumber.CountReading(qty));
            }

            int total = CartTotalYen();
            sb.Append("\n<size=125%><b>").Append(Pick("Tổng", "Total", "合計")).Append(": <color=#F1B83F>¥").Append(total.ToString("N0")).Append("</color></b></size>\n");
            sb.Append("<size=85%><color=#AFC0CF>").Append(JapaneseNumber.ToKanji(total)).Append("円  (").Append(JapaneseNumber.ToReading(total)).Append("えん)</color></size>\n\n");

            sb.Append("<color=#F1B83F><b>").Append(Pick("Bạn nói với nhân viên:", "You tell the staff:", "店員さんに言うこと:")).Append("</b></color>\n");
            sb.Append("<size=130%><b>すみません、").Append(ja).Append("お願いします。</b></size>\n");
            sb.Append("<color=#AFC0CF>すみません、").Append(reading).Append("おねがいします。</color>\n");
            sb.Append(Pick("Xin lỗi, cho tôi các món trên.", "Excuse me, I would like the items above, please.", string.Empty)).Append("\n\n");
            sb.Append("<size=85%><color=#AFC0CF>").Append(Pick("Mẹo: số lượng đếm bằng ひとつ、ふたつ、みっつ... (1 đến 10). Nhân viên sẽ đọc lại đơn để xác nhận; thanh toán khi ăn xong (お会計).", "Tip: count with ひとつ、ふたつ、みっつ... (1 to 10). The staff repeats your order to confirm; you pay after eating (お会計).", "ヒント: ひとつ、ふたつ、みっつ…と数えます。食べたあとでお会計です。")).Append("</color></size>");
            return sb.ToString();
        }

        private void RefreshOrderDetail()
        {
            _detailText.text = BuildOrderSummary();
        }

        private void PlaceOrder()
        {
            if (_table == null || _cart.Count == 0) return;

            if (_table.TryPlaceOrder(_menu, _cart, out string error))
            {
                Close();
            }
            else
            {
                _hintText.text = $"<color=#FF7A6B>{error}</color>";
            }
        }

        private void SelectEntry(int index)
        {
            if (index < 0 || index >= _entries.Count) return;

            for (int i = 0; i < _rowSwaps.Count; i++)
            {
                if (_rowSwaps[i] != null) _rowSwaps[i].SetBaseColor(i == index ? new Color(0.3f, 0.24f, 0.09f, 0.96f) : UIStyleKit.PanelBase);
            }

            _detailText.text = _tabs[_tabIndex].kind == TabKind.Order ? BuildOrderSummary() : _entries[index].detail;
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

            CreateScroll(panelRect, "Tabs", new Vector2(24f, -116f), new Vector2(250f, 466f), out _tabsRoot, new Color(0.025f, 0.033f, 0.04f, 0.6f));
            _listScroll = CreateScroll(panelRect, "List", new Vector2(290f, -116f), new Vector2(390f, 466f), out _listRoot, new Color(0.025f, 0.033f, 0.04f, 0.6f));
            _detailScroll = CreateScroll(panelRect, "Detail", new Vector2(696f, -116f), new Vector2(460f, 466f), out RectTransform detailContent, new Color(0.025f, 0.033f, 0.04f, 0.72f));

            _detailText = CreateText(detailContent, "DetailText", string.Empty, Vector2.zero, Vector2.zero, 20f, FontStyles.Normal);
            _detailText.alignment = TextAlignmentOptions.TopLeft;
            _detailText.richText = true;
            _detailText.lineSpacing = 6f;

            BuildFooter(panelRect);
            BuildStaffBar(canvas.transform);
        }

        private void BuildFooter(RectTransform panelRect)
        {
            _footer = new GameObject("OrderFooter", typeof(RectTransform), typeof(Image));
            _footer.transform.SetParent(panelRect, false);
            var rect = (RectTransform)_footer.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -590f);
            rect.sizeDelta = new Vector2(1132f, 60f);
            _footer.GetComponent<Image>().color = new Color(0.025f, 0.033f, 0.04f, 0.72f);

            _cartText = CreateText(rect, "CartText", string.Empty, new Vector2(18f, -8f), new Vector2(800f, 44f), 22f, FontStyles.Normal);
            _cartText.alignment = TextAlignmentOptions.MidlineLeft;

            _orderButton = CreateRowButton(rect, "OrderButton", "注文する", 44f, UIStyleKit.AccentGold, UIStyleKit.AccentGoldHover, UIStyleKit.AccentGoldPressed, 24f, new Color(0.08f, 0.06f, 0.02f, 1f), false);
            var buttonRect = (RectTransform)_orderButton.transform;
            buttonRect.anchorMin = new Vector2(1f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-10f, 0f);
            buttonRect.sizeDelta = new Vector2(250f, 46f);
            _orderButtonLabel = _orderButton.GetComponentInChildren<TextMeshProUGUI>();
            _orderButtonLabel.alignment = TextAlignmentOptions.Center;
            _orderButton.onClick.AddListener(PlaceOrder);
            _footer.SetActive(false);
        }

        private void BuildStaffBar(Transform canvasTransform)
        {
            _staffBar = new GameObject("RestaurantStaffBar", typeof(RectTransform), typeof(Image));
            _staffBar.transform.SetParent(canvasTransform, false);
            var rect = (RectTransform)_staffBar.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 150f);
            rect.sizeDelta = new Vector2(1000f, 150f);
            UIStyleKit.StylePanel(rect, new Color(0.05f, 0.06f, 0.075f, 0.94f));
            _staffBar.GetComponent<Image>().raycastTarget = false;

            _staffSpeakerText = CreateText(rect, "Speaker", string.Empty, new Vector2(26f, -14f), new Vector2(600f, 28f), 20f, FontStyles.Bold);
            _staffSpeakerText.color = UIStyleKit.AccentGold;
            _staffSpeakerText.raycastTarget = false;
            _staffLineText = CreateText(rect, "Line", string.Empty, new Vector2(26f, -44f), new Vector2(950f, 100f), 24f, FontStyles.Normal);
            _staffLineText.alignment = TextAlignmentOptions.TopLeft;
            _staffLineText.raycastTarget = false;
            _staffBar.SetActive(false);
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
