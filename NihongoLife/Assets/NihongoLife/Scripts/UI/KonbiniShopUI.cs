using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.Shop;

namespace NihongoLife.UI
{
    /// <summary>
    /// Hibari Mart shopping: browse a shelf section, pick a product (name, reading, meaning, price),
    /// choose a quantity, put it in the basket, then pay the clerk at the register. Products tied to the
    /// konbini scenario (onigiri / water / tea) report to the ScenarioManager the same way the old
    /// "press F on the rice ball" items did, so the quest keeps working.
    /// </summary>
    public sealed class KonbiniShopUI : MonoBehaviour
    {
        private static KonbiniShopUI _instance;

        private TMP_FontAsset _font;
        private RectTransform _window;
        private RectTransform _browse;
        private RectTransform _checkout;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _wallet;
        private RectTransform _grid;
        private TextMeshProUGUI _detailGlyph;
        private Image _detailTile;
        private TextMeshProUGUI _detailName;
        private TextMeshProUGUI _detailReading;
        private TextMeshProUGUI _detailMeaning;
        private TextMeshProUGUI _detailPrice;
        private TextMeshProUGUI _detailEffect;
        private TextMeshProUGUI _quantityText;
        private TextMeshProUGUI _basketLine;
        private TextMeshProUGUI _feedback;
        private RectTransform _checkoutLines;
        private TextMeshProUGUI _checkoutTotal;
        private RectTransform _chip;
        private TextMeshProUGUI _chipText;
        private readonly List<(KonbiniProduct product, Image card)> _cards = new List<(KonbiniProduct, Image)>();
        private readonly Dictionary<KonbiniSection, Image> _tabs = new Dictionary<KonbiniSection, Image>();
        private KonbiniSection _section;
        private KonbiniProduct _selected;
        private int _quantity = 1;
        private NPCController _clerk;

        public bool IsOpen => _window != null && _window.gameObject.activeSelf;
        public bool IsCheckout => IsOpen && _checkout.gameObject.activeSelf;
        public KonbiniProduct Selected => _selected;

        public static KonbiniShopUI GetOrCreate()
        {
            if (_instance != null) return _instance;
            var canvas = NLUi.CreateCanvas("KonbiniShopCanvas", 650);
            _instance = canvas.gameObject.AddComponent<KonbiniShopUI>();
            _instance._font = NLUi.ResolveFont();
            _instance.Build(canvas.transform);
            return _instance;
        }

        private void OnEnable() => KonbiniBasket.Changed += RefreshBasket;
        private void OnDisable() => KonbiniBasket.Changed -= RefreshBasket;
        private void OnDestroy() { if (_instance == this) _instance = null; }

        // ─────────── Public API ───────────

        public void OpenSection(KonbiniSection section)
        {
            Show();
            _checkout.gameObject.SetActive(false);
            _browse.gameObject.SetActive(true);
            ShowSection(section);
        }

        public void OpenCheckout(NPCController clerk = null)
        {
            _clerk = clerk;
            Show();
            _browse.gameObject.SetActive(false);
            _checkout.gameObject.SetActive(true);
            RebuildCheckout();
        }

        public void Close()
        {
            if (!IsOpen) return;
            _window.gameObject.SetActive(false);
            if (_clerk != null) { _clerk.StopInteracting(); _clerk = null; }
            Lock(false);
        }

        /// <summary>Selects a product card (also used by tests).</summary>
        public void Select(KonbiniProduct product)
        {
            if (product == null) return;
            _selected = product;
            _quantity = 1;
            foreach (var (p, card) in _cards) card.color = p == product ? new Color(0.3f, 0.24f, 0.09f, 1f) : NLUi.Card;
            _detailTile.color = product.tint;
            _detailGlyph.text = product.glyph;
            _detailName.text = product.japanese;
            _detailReading.text = product.reading;
            _detailMeaning.text = product.vietnamese;
            _detailPrice.text = $"¥{product.price}";
            var effects = new List<string>();
            if (product.food > 0) effects.Add($"No +{product.food:0}");
            if (product.drink > 0) effects.Add($"Nước +{product.drink:0}");
            if (product.energy > 0) effects.Add($"Năng lượng +{product.energy:0}");
            _detailEffect.text = string.Join("  ·  ", effects);
            RefreshQuantity();
            NotifyScenario(product, ScenarioNodeType.InspectItem);
        }

        /// <summary>Puts the selected product in the basket (also used by tests).</summary>
        public void AddSelectedToBasket()
        {
            if (_selected == null) return;
            KonbiniBasket.Add(_selected.id, _quantity);
            Feedback($"かごに いれました: {_selected.japanese} ×{_quantity}", NLUi.Good);
            Cue(GameAudioCue.Pickup);
            NotifyScenario(_selected, ScenarioNodeType.CollectItem);
        }

        /// <summary>Pays for the basket and hands the goods to the player (also used by tests).</summary>
        public bool Pay()
        {
            var inventory = PlayerInventory.Instance;
            int total = KonbiniBasket.Total;
            if (inventory == null || KonbiniBasket.IsEmpty) return false;
            if (!inventory.SpendYen(total))
            {
                Feedback($"Không đủ tiền — cần ¥{total}, bạn có ¥{inventory.Yen}.", NLUi.Bad);
                Cue(GameAudioCue.UiError);
                return false;
            }
            int refused = 0;
            foreach (var line in KonbiniBasket.Items.ToList())
            {
                var p = KonbiniCatalog.Find(line.Key);
                for (int i = 0; i < line.Value; i++)
                    if (!inventory.AddItem(p.id, p.japanese, p.vietnamese, p.price, 1, false, p.useType, p.food, p.drink, p.energy)) refused += p.price;
            }
            if (refused > 0) inventory.AddYen(refused);
            KonbiniBasket.Clear();
            Cue(GameAudioCue.UiConfirm);
            _clerk = null; // the thank-you conversation releases the clerk when it ends
            Close();
            ThankYouConversation(total);
            return true;
        }

        // ─────────── Behaviour ───────────

        private void Show()
        {
            _window.gameObject.SetActive(true);
            _window.SetAsLastSibling();
            UIStyleKit.PlayShowAnimation(_window.gameObject);
            Feedback(string.Empty, NLUi.Muted);
            RefreshBasket();
            Lock(true);
        }

        private void ShowSection(KonbiniSection section)
        {
            _section = section;
            _title.text = $"ひばりマート  ·  <color=#F2B233>{KonbiniCatalog.SectionJa(section)}</color>  <size=70%><color=#A8B4C4>{KonbiniCatalog.SectionVi(section)}</color></size>";
            foreach (var tab in _tabs) tab.Value.color = tab.Key == section ? NLUi.Gold : NLUi.Card;
            foreach (Transform child in _grid) Destroy(child.gameObject);
            _cards.Clear();
            foreach (var product in KonbiniCatalog.InSection(section)) _cards.Add((product, ProductCard(product)));
            Select(KonbiniCatalog.InSection(section).First());
        }

        private void NotifyScenario(KonbiniProduct product, ScenarioNodeType kind)
        {
            var scenario = ScenarioManager.Instance;
            var node = scenario != null ? scenario.CurrentNode : null;
            if (node == null || string.IsNullOrEmpty(product.scenarioItemId)) return;
            if (node.nodeType != kind || node.targetItemId != product.scenarioItemId) return;
            var worldItem = FindObjectsByType<InteractiveItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(i => i.ItemId == product.scenarioItemId);
            if (worldItem != null) scenario.OnItemInteracted(product.scenarioItemId, worldItem);
        }

        private void ThankYouConversation(int total)
        {
            var dm = DialogueManager.Instance;
            if (dm == null) return;
            ScenarioNode Line(string id, string ja, string reading, string vi, string next) => new ScenarioNode
            {
                id = id, nodeType = ScenarioNodeType.Dialogue, speakerId = "npc_cashier", speakerName = "いとう · Ito",
                textJa = ja, textReading = reading, textEn = vi, nextNodeId = next, animationCue = "bow"
            };
            var total_ = Line("pay_total", $"ありがとうございます。ぜんぶで {total}えん です。", $"ありがとうございます。ぜんぶで {total}えん です。", $"Cảm ơn quý khách. Tổng cộng {total} yên.", "pay_bag");
            var bag = Line("pay_bag", "ふくろは おつけしますか。", "ふくろは おつけしますか。", "Quý khách có lấy túi không ạ?", null);
            bag.choices = new List<DialogueChoice>
            {
                new DialogueChoice { textJa = "はい、おねがいします。", textEn = "Vâng, cho tôi xin túi.", nextNodeId = "pay_yes" },
                new DialogueChoice { textJa = "いいえ、だいじょうぶです。", textEn = "Không cần đâu ạ.", nextNodeId = "pay_no" },
            };
            var yes = Line("pay_yes", "かしこまりました。ありがとうございました！", "かしこまりました。ありがとうございました！", "Vâng ạ. Cảm ơn quý khách!", null);
            var no = Line("pay_no", "ありがとうございました！また おこしください。", "ありがとうございました！また おこしください。", "Cảm ơn quý khách! Hẹn gặp lại.", null);
            dm.StartConversation(new[] { total_, bag, yes, no }, "pay_total", ended =>
            {
                if (ended == null || ended == "cancel") return;
                PlayerStatus.Instance?.AddKnowledge(2);
            });
        }

        private void RefreshQuantity()
        {
            if (_selected == null) return;
            _quantityText.text = $"{_quantity}   <size=70%><color=#A8B4C4>= ¥{_selected.price * _quantity}</color></size>";
        }

        private void RefreshBasket()
        {
            if (_basketLine == null) return;
            int count = KonbiniBasket.Count;
            _basketLine.text = count == 0
                ? "かご (giỏ): trống"
                : $"かご (giỏ): <color=#F2B233>{count} món · ¥{KonbiniBasket.Total}</color>   →   mang ra <b>レジ</b> gặp chị Ito để thanh toán";
            if (PlayerInventory.Instance != null) _wallet.text = $"¥{PlayerInventory.Instance.Yen:N0}";
            _chip.gameObject.SetActive(count > 0 && !IsOpen);
            _chipText.text = $"かご  {count}  ·  ¥{KonbiniBasket.Total}\n<size=70%><color=#A8B4C4>Thanh toán ở レジ</color></size>";
            if (IsCheckout) RebuildCheckout();
        }

        private void RebuildCheckout()
        {
            foreach (Transform child in _checkoutLines) Destroy(child.gameObject);
            foreach (var line in KonbiniBasket.Items.ToList())
            {
                var p = KonbiniCatalog.Find(line.Key);
                var row = NLUi.Panel(_checkoutLines, "Line_" + p.id, NLUi.Card, new RectOffset(16, 12, 8, 8), 14f, vertical: false);
                ((HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                var glyph = NLUi.Pill(row, "Glyph", p.glyph, _font, p.tint, new Color(0.15f, 0.12f, 0.1f), 22f);
                var name = NLUi.Label(row, "Name", $"{p.japanese}  <size=75%><color=#A8B4C4>{p.vietnamese}</color></size>", 21f, NLUi.Text, _font);
                NLUi.Size(name, flexibleWidth: 1f);
                var qty = NLUi.Label(row, "Qty", $"×{line.Value}   ¥{p.price * line.Value}", 21f, NLUi.Soft, _font, FontStyles.Bold, TextAlignmentOptions.Right);
                NLUi.Size(qty, preferredWidth: 180f);
                string id = p.id;
                var remove = NLUi.Button(row, "Remove", "×", _font, () => KonbiniBasket.Remove(id), new Color(0.3f, 0.12f, 0.12f), 20f, null, 40f);
                NLUi.Size(remove, preferredWidth: 48f);
            }
            int wallet = PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;
            _checkoutTotal.text = KonbiniBasket.IsEmpty
                ? "Giỏ hàng trống."
                : $"ごうけい (Tổng cộng): <color=#F2B233><b>¥{KonbiniBasket.Total}</b></color>     ·     Bạn có ¥{wallet:N0} → còn ¥{Mathf.Max(0, wallet - KonbiniBasket.Total):N0}";
        }

        private void Feedback(string message, Color color)
        {
            if (_feedback == null) return;
            _feedback.text = message;
            _feedback.color = color;
        }

        private static void Lock(bool locked)
        {
            ScenarioManager.Instance?.SetPlayerInputLocked(locked);
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.InputLocked = locked;
            var camera = FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.IsLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        private static void Cue(GameAudioCue cue)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, 0.7f);
        }

        private void Update()
        {
            if (!IsOpen || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        // ─────────── Building ───────────

        private void Build(Transform canvas)
        {
            _window = NLUi.Panel(canvas, "ShopWindow", NLUi.Ink, new RectOffset(30, 30, 24, 24), 16f);
            NLUi.Anchor(_window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 0f));
            NLUi.FitContent(_window);

            var header = NLUi.Group(_window, "Header", false, 14f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)header.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            _title = NLUi.Label(header, "Title", "ひばりマート", 30f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Size(_title, flexibleWidth: 1f);
            _wallet = NLUi.Pill(header, "Wallet", "¥0", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Gold, 20f).GetComponentInChildren<TextMeshProUGUI>();
            NLUi.Button(header, "Close", "とじる · Esc", _font, Close, NLUi.Card, 17f, NLUi.Muted, 44f);

            // Browse page
            _browse = NLUi.Group(_window, "Browse", true, 14f);
            var tabs = NLUi.Group(_browse, "Tabs", false, 10f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)tabs.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            foreach (KonbiniSection s in System.Enum.GetValues(typeof(KonbiniSection)))
            {
                var section = s;
                var tab = NLUi.Button(tabs, "Tab_" + s, $"{KonbiniCatalog.SectionJa(s)}  <size=75%>{KonbiniCatalog.SectionVi(s)}</size>", _font, () => ShowSection(section), NLUi.Card, 18f, null, 44f);
                _tabs[s] = tab.GetComponent<Image>();
            }

            var body = NLUi.Group(_browse, "Body", false, 18f, TextAnchor.UpperLeft);
            ((HorizontalLayoutGroup)body.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            var gridGo = new GameObject("Products", typeof(RectTransform));
            gridGo.transform.SetParent(body, false);
            _grid = (RectTransform)gridGo.transform;
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(232f, 196f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            NLUi.Size(_grid, preferredWidth: 720f, preferredHeight: 404f);

            var detail = NLUi.Panel(body, "Detail", NLUi.Card, new RectOffset(22, 22, 20, 20), 8f);
            NLUi.Size(detail, preferredWidth: 420f, flexibleWidth: 1f);
            var tile = NLUi.Panel(detail, "Tile", Color.white, new RectOffset(0, 0, 6, 6), 0f);
            _detailTile = tile.GetComponent<Image>();
            NLUi.Size(tile, preferredHeight: 110f);
            _detailGlyph = NLUi.Label(tile, "Glyph", "", 70f, new Color(0.15f, 0.12f, 0.1f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            _detailName = NLUi.Label(detail, "Name", "", 30f, NLUi.Text, _font, FontStyles.Bold);
            _detailReading = NLUi.Label(detail, "Reading", "", 18f, NLUi.Muted, _font);
            _detailMeaning = NLUi.Label(detail, "Meaning", "", 21f, NLUi.Soft, _font);
            _detailPrice = NLUi.Label(detail, "Price", "", 28f, NLUi.Gold, _font, FontStyles.Bold);
            _detailEffect = NLUi.Label(detail, "Effect", "", 16f, NLUi.Muted, _font);
            var qtyRow = NLUi.Group(detail, "Quantity", false, 10f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)qtyRow.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.Size(NLUi.Button(qtyRow, "Minus", "−", _font, () => { _quantity = Mathf.Max(1, _quantity - 1); RefreshQuantity(); }, NLUi.CardHover, 26f, null, 46f), preferredWidth: 56f);
            _quantityText = NLUi.Label(qtyRow, "Value", "1", 26f, NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Size(_quantityText, preferredWidth: 150f);
            NLUi.Size(NLUi.Button(qtyRow, "Plus", "+", _font, () => { _quantity = Mathf.Min(9, _quantity + 1); RefreshQuantity(); }, NLUi.CardHover, 26f, null, 46f), preferredWidth: 56f);
            NLUi.Button(detail, "AddToBasket", "かごに いれる  ·  Thêm vào giỏ", _font, AddSelectedToBasket, new Color(0.18f, 0.5f, 0.32f), 21f, null, 54f);

            var footer = NLUi.Panel(_browse, "Footer", new Color(1f, 1f, 1f, 0.05f), new RectOffset(18, 18, 10, 10), 6f);
            _basketLine = NLUi.Label(footer, "Basket", "", 19f, NLUi.Text, _font);
            _feedback = NLUi.Label(footer, "Feedback", "", 17f, NLUi.Muted, _font);

            // Checkout page
            _checkout = NLUi.Group(_window, "Checkout", true, 12f);
            NLUi.Label(_checkout, "Heading", "レジ  ·  <size=75%><color=#A8B4C4>Thanh toán với chị Ito</color></size>", 26f, NLUi.Gold, _font, FontStyles.Bold);
            _checkoutLines = NLUi.Group(_checkout, "Lines", true, 8f);
            _checkoutTotal = NLUi.Label(_checkout, "Total", "", 22f, NLUi.Text, _font);
            var actions = NLUi.Group(_checkout, "Actions", false, 12f, TextAnchor.MiddleRight);
            ((HorizontalLayoutGroup)actions.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.Button(actions, "Continue", "かいものを つづける · Mua tiếp", _font, () => OpenSection(_section), NLUi.Card, 19f, null, 52f);
            NLUi.Button(actions, "Pay", "はらう · Thanh toán", _font, () => Pay(), new Color(0.18f, 0.5f, 0.32f), 21f, null, 52f);
            _checkout.gameObject.SetActive(false);
            _window.gameObject.SetActive(false);

            // Basket chip (top-right) — visible while shopping around the store.
            _chip = NLUi.Panel(canvas, "BasketChip", NLUi.Ink, new RectOffset(18, 18, 10, 10), 0f);
            NLUi.Anchor(_chip, new Vector2(1f, 1f), new Vector2(-28f, -96f), new Vector2(290f, 0f));
            NLUi.FitContent(_chip);
            _chipText = NLUi.Label(_chip, "Text", "", 19f, NLUi.Text, _font, FontStyles.Bold);
            _chip.gameObject.SetActive(false);
        }

        private Image ProductCard(KonbiniProduct product)
        {
            var card = NLUi.Panel(_grid, "Card_" + product.id, NLUi.Card, new RectOffset(12, 12, 12, 10), 4f);
            var tile = NLUi.Panel(card, "Tile", product.tint, new RectOffset(0, 0, 2, 2), 0f);
            NLUi.Size(tile, preferredHeight: 70f);
            NLUi.Label(tile, "Glyph", product.glyph, 42f, new Color(0.15f, 0.12f, 0.1f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(card, "Name", product.japanese, 20f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Label(card, "Meaning", product.vietnamese, 15f, NLUi.Muted, _font);
            NLUi.Label(card, "Price", $"¥{product.price}", 19f, NLUi.Gold, _font, FontStyles.Bold);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Select(product));
            return card.GetComponent<Image>();
        }
    }
}
