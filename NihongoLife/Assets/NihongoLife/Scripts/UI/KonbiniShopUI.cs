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
            _title.text = NihongoLife.Core.Loc.T("Hibari Mart  ·  <color=#F2B233>Quầy thanh toán</color>", "Hibari Mart  ·  <color=#F2B233>Register</color>", "ひばりマート  ·  <color=#F2B233>レジ</color>");
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
            ItemIcons.Apply(_detailGlyph, product.id, 6f);
            _detailName.text = product.japanese;
            _detailReading.text = product.reading;
            _detailMeaning.text = product.Meaning;
            _detailPrice.text = NihongoLife.Core.Loc.Yen(product.price);
            var effects = new List<string>();
            if (product.food > 0) effects.Add(NihongoLife.Core.Loc.T("No", "Food", "満腹") + $" +{product.food:0}");
            if (product.drink > 0) effects.Add(NihongoLife.Core.Loc.T("Nước", "Drink", "水分") + $" +{product.drink:0}");
            if (product.energy > 0) effects.Add(NihongoLife.Core.Loc.T("Năng lượng", "Energy", "元気") + $" +{product.energy:0}");
            if (product.notebookPages > 0) effects.Add(NihongoLife.Core.Loc.F("Sổ tay +{0} trang (đang có {1}/{2})", "Notebook +{0} pages (you have {1}/{2})", "ノート +{0}ページ（今 {1}/{2}）", product.notebookPages, NihongoLife.Notebook.NotebookService.Pages, NihongoLife.Notebook.NotebookService.MaxPages));
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
            // The kid reaches up and takes it from the shelf.
            SceneRefs.Find<NihongoLife.Player.PlayerController>()?.GetComponent<CharacterAnimationController>()?.PlayGesture("Pickup", 1.1f);
            NotifyScenario(_selected, ScenarioNodeType.CollectItem);
        }

        /// <summary>Pays for the basket and hands the goods to the player (also used by tests).</summary>
        public bool Pay()
        {
            var inventory = PlayerInventory.Instance;
            int total = KonbiniBasket.Total;
            if (inventory == null || KonbiniBasket.IsEmpty) return false;
            if (!inventory.SpendYen(total, NihongoLife.Core.Loc.Triple("Mua ở Hibari Mart", "Shopping at Hibari Mart", "ひばりマートで買い物")))
            {
                Feedback(NihongoLife.Core.Loc.F("Không đủ tiền — cần {0}, bạn có {1}.", "Not enough money — you need {0} and have {1}.", "お金が足りません。{0}必要で、所持金は{1}です。", NihongoLife.Core.Loc.Yen(total), NihongoLife.Core.Loc.Yen(inventory.Yen)), NLUi.Bad);
                Cue(GameAudioCue.UiError);
                return false;
            }
            NihongoLife.Audio.Soundscape.Jingle("purchase");
            int refused = 0, sheets = 0;
            foreach (var line in KonbiniBasket.Items.ToList())
            {
                var p = KonbiniCatalog.Find(line.Key);
                for (int i = 0; i < line.Value; i++)
                {
                    if (p.notebookPages > 0)
                    {
                        // Paper goes straight into the notebook; a full binder refunds what does not fit.
                        int added = NihongoLife.Notebook.NotebookService.AddPages(p.notebookPages);
                        sheets += added;
                        if (added < p.notebookPages) refused += Mathf.RoundToInt(p.price * (p.notebookPages - added) / (float)p.notebookPages);
                        continue;
                    }
                    if (!inventory.AddItem(p.id, p.japanese, p.vietnamese, p.price, 1, false, p.useType, p.food, p.drink, p.energy)) refused += p.price;
                }
            }
            if (sheets > 0) HudFeed.Post(NihongoLife.Core.Loc.F("Sổ tay +{0} trang (giờ có {1} trang) — mở bằng phím L.", "Notebook +{0} pages (now {1}) — open it with L.", "ノート +{0}ページ（今{1}ページ）— Lキーで開けます。", sheets, NihongoLife.Notebook.NotebookService.Pages), HudFeed.Kind.Info, 4f);
            if (refused > 0) inventory.AddYen(refused, NihongoLife.Core.Loc.Triple("Hoàn tiền (balo đầy)", "Refund (bag full)", "返金（かばんがいっぱい）"));
            foreach (var line in KonbiniBasket.Items) NihongoLife.Progression.QuestService.Raise("buy", line.Key, line.Value);
            if (total > refused) NihongoLife.Progression.QuestService.Raise("buy", "konbini", 1);
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
            ScenarioNode Line(string id, string ja, string reading, string vi, string en, string next) => new ScenarioNode
            {
                id = id, nodeType = ScenarioNodeType.Dialogue, speakerId = "npc_cashier", speakerName = NihongoLife.Core.Loc.T("Chị Ito", "Ito", "いとうさん"),
                textJa = ja, textReading = reading, textVi = vi, textEn = en, nextNodeId = next, animationCue = "bow"
            };
            var total_ = Line("pay_total", $"ありがとうございます。ぜんぶで {total}えん です。", $"ありがとうございます。ぜんぶで {total}えん です。", $"Cảm ơn quý khách. Tổng cộng {total} yên.", $"Thank you. That's {total} yen in total.", "pay_bag");
            var bag = Line("pay_bag", "ふくろは おつけしますか。", "ふくろは おつけしますか。", "Quý khách có lấy túi không ạ?", "Would you like a bag?", null);
            bag.choices = new List<DialogueChoice>
            {
                new DialogueChoice { textJa = "はい、おねがいします。", textVi = "Vâng, cho tôi xin túi.", textEn = "Yes, please.", nextNodeId = "pay_yes" },
                new DialogueChoice { textJa = "いいえ、だいじょうぶです。", textVi = "Không cần đâu ạ.", textEn = "No, I'm fine.", nextNodeId = "pay_no" },
            };
            var yes = Line("pay_yes", "かしこまりました。ありがとうございました！", "かしこまりました。ありがとうございました！", "Vâng ạ. Cảm ơn quý khách!", "Certainly. Thank you very much!", null);
            var no = Line("pay_no", "ありがとうございました！また おこしください。", "ありがとうございました！また おこしください。", "Cảm ơn quý khách! Hẹn gặp lại.", "Thank you very much! Please come again.", null);
            dm.StartConversation(new[] { total_, bag, yes, no }, "pay_total", ended =>
            {
                if (ended == null || ended == "cancel") return;
                PlayerStatus.Instance?.AddKnowledge(2);
            });
        }

        private void RefreshQuantity()
        {
            if (_selected == null) return;
            _quantityText.text = $"{_quantity}   <size=70%><color=#A8B4C4>= {NihongoLife.Core.Loc.Yen(_selected.price * _quantity)}</color></size>";
        }

        private void RefreshBasket()
        {
            if (_basketLine == null) return;
            int count = KonbiniBasket.Count;
            _basketLine.text = count == 0
                ? NihongoLife.Core.Loc.T("Giỏ: trống", "Basket: empty", "かご：からっぽ")
                : NihongoLife.Core.Loc.F("Giỏ: <color=#F2B233>{0} món · {1}</color>   →   mang ra quầy gặp chị Ito để thanh toán", "Basket: <color=#F2B233>{0} items · {1}</color>   →   take it to the register to pay Ito", "かご：<color=#F2B233>{0}点 · {1}</color>   →   レジで いとうさんに払いましょう", count, NihongoLife.Core.Loc.Yen(KonbiniBasket.Total));
            if (PlayerInventory.Instance != null) _wallet.text = NihongoLife.Core.Loc.Yen(PlayerInventory.Instance.Yen);
            _chip.gameObject.SetActive(count > 0 && !IsOpen);
            _chipText.text = NihongoLife.Core.Loc.T("Giỏ", "Basket", "かご") + $"  {count}  ·  {NihongoLife.Core.Loc.Yen(KonbiniBasket.Total)}\n<size=70%><color=#A8B4C4>" + NihongoLife.Core.Loc.T("Thanh toán ở quầy", "Pay at the register", "レジで払う") + "</color></size>";
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
                NLUi.Size(glyph, 56f, 56f);
                ItemIcons.Apply(glyph.GetComponentInChildren<TextMeshProUGUI>(), p.id, 3f);
                var name = NLUi.Label(row, "Name", p.japanese + (string.IsNullOrEmpty(p.Meaning) ? "" : $"  <size=75%><color=#A8B4C4>{p.Meaning}</color></size>"), 21f, NLUi.Text, _font);
                NLUi.Size(name, flexibleWidth: 1f);
                var qty = NLUi.Label(row, "Qty", $"×{line.Value}   {NihongoLife.Core.Loc.Yen(p.price * line.Value)}", 21f, NLUi.Soft, _font, FontStyles.Bold, TextAlignmentOptions.Right);
                NLUi.Size(qty, preferredWidth: 180f);
                string id = p.id;
                var remove = NLUi.Button(row, "Remove", "×", _font, () => KonbiniBasket.Remove(id), new Color(0.3f, 0.12f, 0.12f), 20f, null, 40f);
                remove.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(4, 4, 4, 4);
                var removeSize = NLUi.Size(remove, preferredWidth: 44f);
                removeSize.flexibleWidth = 0f;
            }
            int wallet = PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;
            _checkoutTotal.text = KonbiniBasket.IsEmpty
                ? NihongoLife.Core.Loc.T("Giỏ hàng trống.", "The basket is empty.", "かごはからっぽです。")
                : NihongoLife.Core.Loc.F("Tổng cộng: <color=#F2B233><b>{0}</b></color>     ·     Bạn có {1} → còn {2}", "Total: <color=#F2B233><b>{0}</b></color>     ·     You have {1} → {2} left", "ごうけい：<color=#F2B233><b>{0}</b></color>     ·     所持金 {1} → 残り {2}", NihongoLife.Core.Loc.Yen(KonbiniBasket.Total), NihongoLife.Core.Loc.Yen(wallet), NihongoLife.Core.Loc.Yen(Mathf.Max(0, wallet - KonbiniBasket.Total)));
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
            var player = SceneRefs.Find<PlayerController>();
            if (player != null) player.InputLocked = locked;
            var camera = SceneRefs.Find<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.IsLocked = locked;
            Cursor.lockState = CursorLockMode.None; // Sims-style: the cursor stays free in gameplay
            Cursor.visible = true;
        }

        private static void Cue(GameAudioCue cue)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, 0.7f);
        }

        // ─────────── Building ───────────

        private void Build(Transform canvas)
        {
            UiModalStack.Register(this, () => IsOpen, Close, "Konbini shop");
            _window = NLUi.Panel(canvas, "ShopWindow", NLUi.Ink, new RectOffset(30, 30, 24, 24), 16f);
            NLUi.Anchor(_window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 0f));
            NLUi.FitContent(_window);

            var header = NLUi.Group(_window, "Header", false, 14f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)header.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            _title = NLUi.Label(header, "Title", "ひばりマート", 30f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Size(_title, flexibleWidth: 1f);
            _wallet = NLUi.Pill(header, "Wallet", "¥0", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Gold, 20f).GetComponentInChildren<TextMeshProUGUI>();
            NLUi.Label(header, "EscHint", "Esc", 15f, NLUi.Muted, _font).textWrappingMode = TextWrappingModes.NoWrap;
            var closeSpace = new GameObject("CloseSpace", typeof(RectTransform));
            closeSpace.transform.SetParent(header, false);
            NLUi.Size(closeSpace.transform, 46f, 46f);
            NLUi.CloseButton(_window, _font, Close, 46f, 18f);

            // Browse page
            _browse = NLUi.Group(_window, "Browse", true, 14f);
            var tabs = NLUi.Group(_browse, "Tabs", false, 10f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)tabs.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            foreach (KonbiniSection s in System.Enum.GetValues(typeof(KonbiniSection)))
            {
                var section = s;
                var tab = NLUi.Button(tabs, "Tab_" + s, "", _font, () => ShowSection(section), NLUi.Card, 18f, null, 44f);
                LocText.Set(tab.GetComponentInChildren<TextMeshProUGUI>(), () => KonbiniCatalog.SectionJa(section) + (Loc.IsJapanese ? "" : $"  <size=75%>{KonbiniCatalog.SectionName(section)}</size>"));
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
            NLUi.Size(tile, preferredHeight: 168f);
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
            NLUi.Button(detail, "AddToBasket", NihongoLife.Core.Loc.T("Thêm vào giỏ", "Add to basket", "かごに いれる"), _font, AddSelectedToBasket, new Color(0.18f, 0.5f, 0.32f), 21f, null, 54f);

            var footer = NLUi.Panel(_browse, "Footer", new Color(1f, 1f, 1f, 0.05f), new RectOffset(18, 18, 10, 10), 6f);
            _basketLine = NLUi.Label(footer, "Basket", "", 19f, NLUi.Text, _font);
            _feedback = NLUi.Label(footer, "Feedback", "", 17f, NLUi.Muted, _font);

            // Checkout page
            _checkout = NLUi.Group(_window, "Checkout", true, 12f);
            LocText.Set(NLUi.Label(_checkout, "Heading", "", 26f, NLUi.Gold, _font, FontStyles.Bold), "Thanh toán với chị Ito", "Pay Ito at the register", "レジで いとうさんに払う");
            _checkoutLines = NLUi.Group(_checkout, "Lines", true, 8f);
            _checkoutTotal = NLUi.Label(_checkout, "Total", "", 22f, NLUi.Text, _font);
            var actions = NLUi.Group(_checkout, "Actions", false, 12f, TextAnchor.MiddleRight);
            ((HorizontalLayoutGroup)actions.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.Button(actions, "Continue", NihongoLife.Core.Loc.T("Mua tiếp", "Keep shopping", "かいものを つづける"), _font, () => OpenSection(_section), NLUi.Card, 19f, null, 52f);
            NLUi.Button(actions, "Pay", NihongoLife.Core.Loc.T("Thanh toán", "Pay", "はらう"), _font, () => Pay(), new Color(0.18f, 0.5f, 0.32f), 21f, null, 52f);
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
            NLUi.Size(tile, preferredHeight: 116f);
            var tileGlyph = NLUi.Label(tile, "Glyph", product.glyph, 42f, new Color(0.15f, 0.12f, 0.1f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            ItemIcons.Apply(tileGlyph, product.id, 4f);
            // One line: long names such as ルーズリーフ（5まい） shrink instead of wrapping onto the meaning.
            var cardName = NLUi.Label(card, "Name", product.japanese, 20f, NLUi.Text, _font, FontStyles.Bold);
            cardName.textWrappingMode = TextWrappingModes.NoWrap;
            cardName.enableAutoSizing = true;
            cardName.fontSizeMin = 13f;
            cardName.fontSizeMax = 20f;
            cardName.overflowMode = TextOverflowModes.Ellipsis;
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
