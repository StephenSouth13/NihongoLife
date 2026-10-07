using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Shop;

namespace NihongoLife.UI
{
    /// <summary>
    /// The bag (B): a 4×4 slot grid with kanji/kana tiles, a detail card with Use / Drop, the wallet and the
    /// four vitals. Replaces the old text-list panel whose rows overlapped. Root is assigned to HUDUI's
    /// inventoryPanel so the HUD's existing open/close and input-lock logic keeps working.
    /// </summary>
    public sealed class InventoryWindow : MonoBehaviour
    {
        public GameObject Root => _root.gameObject;
        public int SelectedIndex => _selected;

        private TMP_FontAsset _font;
        private RectTransform _root;
        private TextMeshProUGUI _slotsText;
        private TextMeshProUGUI _walletText;
        private readonly List<(Image bg, Image tile, TextMeshProUGUI glyph, TextMeshProUGUI qty, TextMeshProUGUI name)> _slots = new List<(Image, Image, TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI)>();
        private Image _detailTile;
        private TextMeshProUGUI _detailGlyph;
        private TextMeshProUGUI _detailName;
        private TextMeshProUGUI _detailInfo;
        private TextMeshProUGUI _detailEffect;
        private Button _useButton;
        private TextMeshProUGUI _useLabel;
        private Button _dropButton;
        private readonly Dictionary<string, (Image fill, TextMeshProUGUI value)> _bars = new Dictionary<string, (Image, TextMeshProUGUI)>();
        private int _selected;
        private PlayerInventory _boundInventory;
        private PlayerStatus _boundStatus;

        private const int Columns = 4;
        private const int SlotCount = 16;

        public static InventoryWindow Create(Transform parent, TMP_FontAsset font)
        {
            var canvas = NLUi.CreateCanvas("InventoryCanvas", 550, parent);
            var window = canvas.gameObject.AddComponent<InventoryWindow>();
            window._font = font != null ? font : NLUi.ResolveFont();
            window.Build(canvas.transform);
            return window;
        }

        private void Update()
        {
            if (!_root.gameObject.activeInHierarchy) return;
            Bind();
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            for (int i = 0; i < 9; i++)
                if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) Select(i);
            if (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) UseSelected();
            if (keyboard.deleteKey.wasPressedThisFrame) DropSelected();
            if (keyboard.rightArrowKey.wasPressedThisFrame) Select(_selected + 1);
            if (keyboard.leftArrowKey.wasPressedThisFrame) Select(_selected - 1);
            if (keyboard.downArrowKey.wasPressedThisFrame) Select(_selected + Columns);
            if (keyboard.upArrowKey.wasPressedThisFrame) Select(_selected - Columns);
        }

        private void Bind()
        {
            if (_boundInventory != PlayerInventory.Instance)
            {
                if (_boundInventory != null) _boundInventory.OnInventoryChanged -= Refresh;
                _boundInventory = PlayerInventory.Instance;
                if (_boundInventory != null) _boundInventory.OnInventoryChanged += Refresh;
                Refresh();
            }
            if (_boundStatus != PlayerStatus.Instance)
            {
                if (_boundStatus != null) _boundStatus.OnStatusChanged -= Refresh;
                _boundStatus = PlayerStatus.Instance;
                if (_boundStatus != null) _boundStatus.OnStatusChanged += Refresh;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (_boundInventory != null) _boundInventory.OnInventoryChanged -= Refresh;
            if (_boundStatus != null) _boundStatus.OnStatusChanged -= Refresh;
        }

        public void Refresh()
        {
            var inventory = PlayerInventory.Instance;
            var items = inventory != null ? inventory.Items : null;
            int used = items != null ? items.Count : 0;
            _slotsText.text = $"{used}/{(inventory != null ? inventory.MaxSlots : SlotCount)} ô";
            _walletText.text = inventory != null ? $"¥{inventory.Yen:N0}" : "¥--";
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                bool has = items != null && i < items.Count;
                slot.tile.gameObject.SetActive(has);
                slot.qty.gameObject.SetActive(has && items[i].quantity > 1);
                slot.name.text = has ? items[i].displayNameJa : string.Empty;
                slot.bg.color = i == _selected ? new Color(0.36f, 0.27f, 0.08f, 1f) : (has ? NLUi.Card : new Color(1f, 1f, 1f, 0.04f));
                if (!has) continue;
                Visual(items[i], out Color tint, out string glyph);
                slot.tile.color = tint;
                slot.glyph.text = glyph;
                ItemIcons.Apply(slot.glyph, items[i].itemId, 5f);
                slot.qty.text = "×" + items[i].quantity;
            }
            RefreshDetail(items != null && _selected < items.Count ? items[_selected] : null);
            RefreshBars();
        }

        public void Select(int index)
        {
            _selected = Mathf.Clamp(index, 0, SlotCount - 1);
            Refresh();
        }

        public void UseSelected()
        {
            var inventory = PlayerInventory.Instance;
            var status = PlayerStatus.Instance;
            if (inventory == null || status == null || _selected >= inventory.Items.Count) return;
            var entry = inventory.Items[_selected];
            if (entry.useType == ItemUseType.None) return;
            string id = entry.itemId;
            float food = entry.foodRestore, drink = entry.drinkRestore, energy = entry.energyRestore;
            if (!inventory.RemoveItem(id)) return;
            status.RestoreNeeds(food, drink, energy);
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Pickup, 0.7f);
            Refresh();
        }

        public void DropSelected()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null || _selected >= inventory.Items.Count) return;
            inventory.DropItem(inventory.Items[_selected].itemId);
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Drop, 0.7f);
            Refresh();
        }

        private void RefreshDetail(InventoryEntry entry)
        {
            bool has = entry != null;
            _detailTile.gameObject.SetActive(has);
            _useButton.gameObject.SetActive(has && entry.useType != ItemUseType.None);
            _dropButton.gameObject.SetActive(has);
            if (!has)
            {
                _detailName.text = "Chọn một ô đồ";
                _detailInfo.text = "Phím 1–9 hoặc bấm chuột để chọn · E dùng · Delete bỏ";
                _detailEffect.text = string.Empty;
                return;
            }
            Visual(entry, out Color tint, out string glyph);
            _detailTile.color = tint;
            _detailGlyph.text = glyph;
            ItemIcons.Apply(_detailGlyph, entry.itemId, 6f);
            _detailName.text = entry.displayNameJa;
            _detailInfo.text = $"{entry.displayNameEn}\nSố lượng: {entry.quantity}   ·   Giá: ¥{entry.priceYen}";
            var effects = new List<string>();
            if (entry.foodRestore > 0) effects.Add($"No +{entry.foodRestore:0}");
            if (entry.drinkRestore > 0) effects.Add($"Nước +{entry.drinkRestore:0}");
            if (entry.energyRestore > 0) effects.Add($"Thể lực +{entry.energyRestore:0}");
            _detailEffect.text = effects.Count > 0 ? string.Join("  ·  ", effects) : "Không dùng được — giữ trong balo.";
            _useLabel.text = entry.useType == ItemUseType.Drink ? "のむ  ·  Uống (E)" : "たべる  ·  Ăn (E)";
        }

        private void RefreshBars()
        {
            var s = PlayerStatus.Instance;
            if (s == null) return;
            SetBar("food", s.Hunger, 100f);
            SetBar("drink", s.Thirst, 100f);
            SetBar("energy", s.CurrentEnergy, s.MaxEnergy);
            SetBar("rest", s.Restfulness, 100f);
        }

        private void SetBar(string key, float value, float max)
        {
            if (!_bars.TryGetValue(key, out var bar)) return;
            float t = max <= 0f ? 0f : Mathf.Clamp01(value / max);
            bar.fill.rectTransform.anchorMax = new Vector2(t, 1f);
            bar.fill.color = Color.Lerp(NLUi.Bad, NLUi.Good, t);
            bar.value.text = $"{value:0}";
        }

        private static void Visual(InventoryEntry entry, out Color tint, out string glyph)
        {
            var product = KonbiniCatalog.Find(entry.itemId);
            if (product != null) { tint = product.tint; glyph = product.glyph; return; }
            tint = entry.useType == ItemUseType.Drink ? new Color(0.62f, 0.82f, 0.96f) : entry.useType == ItemUseType.Food ? new Color(0.86f, 0.9f, 0.82f) : new Color(0.82f, 0.8f, 0.92f);
            glyph = string.IsNullOrEmpty(entry.displayNameJa) ? "?" : entry.displayNameJa.Substring(0, 1);
        }

        // ─────────── Building ───────────

        private void Build(Transform canvas)
        {
            var rootGo = new GameObject("InventoryRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(canvas, false);
            _root = (RectTransform)rootGo.transform;
            NLUi.Stretch(_root);
            rootGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            var window = NLUi.Panel(_root, "BagWindow", NLUi.Ink, new RectOffset(30, 30, 24, 26), 16f);
            NLUi.Anchor(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 0f));
            NLUi.FitContent(window);

            var header = NLUi.Group(window, "Header", false, 12f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)header.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            var title = NLUi.Label(header, "Title", "バッグ  <size=70%><color=#A8B4C4>Balo của bạn</color></size>", 30f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Size(title, flexibleWidth: 1f);
            _slotsText = NLUi.Pill(header, "Slots", "0/16 ô", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Muted, 18f).GetComponentInChildren<TextMeshProUGUI>();
            _walletText = NLUi.Pill(header, "Wallet", "¥0", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Gold, 20f).GetComponentInChildren<TextMeshProUGUI>();
            NLUi.Label(header, "Hint", "B / Esc", 15f, NLUi.Muted, _font).textWrappingMode = TextWrappingModes.NoWrap;
            var closeSpace = new GameObject("CloseSpace", typeof(RectTransform));
            closeSpace.transform.SetParent(header, false);
            NLUi.Size(closeSpace.transform, 46f, 46f);
            NLUi.CloseButton(window, _font, () => _root.gameObject.SetActive(false), 46f, 18f);

            var body = NLUi.Group(window, "Body", false, 20f, TextAnchor.UpperLeft);
            ((HorizontalLayoutGroup)body.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            var gridGo = new GameObject("Slots", typeof(RectTransform));
            gridGo.transform.SetParent(body, false);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(126f, 126f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            NLUi.Size(gridGo.transform, preferredWidth: Columns * 136f - 10f, preferredHeight: 4 * 136f - 10f);
            for (int i = 0; i < SlotCount; i++) BuildSlot(gridGo.transform, i);

            var detail = NLUi.Panel(body, "Detail", NLUi.Card, new RectOffset(22, 22, 20, 20), 10f);
            NLUi.Size(detail, preferredWidth: 470f, flexibleWidth: 1f);
            var tile = NLUi.Panel(detail, "Tile", Color.white, new RectOffset(0, 0, 4, 4), 0f);
            NLUi.Size(tile, preferredHeight: 160f);
            _detailTile = tile.GetComponent<Image>();
            _detailGlyph = NLUi.Label(tile, "Glyph", "", 64f, new Color(0.15f, 0.12f, 0.1f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            _detailName = NLUi.Label(detail, "Name", "", 28f, NLUi.Text, _font, FontStyles.Bold);
            _detailInfo = NLUi.Label(detail, "Info", "", 18f, NLUi.Muted, _font);
            _detailEffect = NLUi.Label(detail, "Effect", "", 18f, NLUi.Soft, _font);
            var actions = NLUi.Group(detail, "Actions", false, 10f, TextAnchor.MiddleLeft);
            _useButton = NLUi.Button(actions, "Use", "たべる  ·  Ăn (E)", _font, UseSelected, new Color(0.18f, 0.5f, 0.32f), 19f, null, 50f);
            _useLabel = _useButton.GetComponentInChildren<TextMeshProUGUI>();
            _dropButton = NLUi.Button(actions, "Drop", "すてる  ·  Bỏ (Del)", _font, DropSelected, new Color(0.35f, 0.16f, 0.15f), 19f, null, 50f);

            NLUi.Divider(detail);
            NLUi.Label(detail, "VitalsTitle", "からだ  <size=75%><color=#A8B4C4>Thể trạng</color></size>", 20f, NLUi.Text, _font, FontStyles.Bold);
            Bar(detail, "food", "おなか · No");
            Bar(detail, "drink", "のど · Nước");
            Bar(detail, "energy", "げんき · Thể lực");
            Bar(detail, "rest", "ねむけ · Tỉnh táo");
            _root.gameObject.SetActive(false);
        }

        private void BuildSlot(Transform grid, int index)
        {
            var slot = NLUi.Panel(grid, "Slot_" + (index + 1), new Color(1f, 1f, 1f, 0.04f));
            var tile = NLUi.Panel(slot, "Tile", Color.white);
            NLUi.Anchor(tile, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(84f, 66f));
            var glyph = NLUi.Label(tile, "Glyph", "", 40f, new Color(0.15f, 0.12f, 0.1f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(glyph.rectTransform);
            var qty = NLUi.Label(slot, "Qty", "", 16f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.TopRight);
            NLUi.Anchor(qty.rectTransform, new Vector2(1f, 1f), new Vector2(-6f, -4f), new Vector2(50f, 22f));
            var name = NLUi.Label(slot, "Name", "", 14f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            NLUi.Anchor(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(116f, 22f));
            var label = NLUi.Label(slot, "Number", (index + 1) <= 9 ? (index + 1).ToString() : string.Empty, 12f, NLUi.Muted, _font);
            NLUi.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(7f, -4f), new Vector2(20f, 18f));
            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slot.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            int i = index;
            button.onClick.AddListener(() => Select(i));
            _slots.Add((slot.GetComponent<Image>(), tile.GetComponent<Image>(), glyph, qty, name));
        }

        private void Bar(Transform parent, string key, string label)
        {
            var row = NLUi.Group(parent, "Bar_" + key, false, 10f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            var name = NLUi.Label(row, "Label", label, 16f, NLUi.Muted, _font);
            NLUi.Size(name, preferredWidth: 170f);
            var track = NLUi.Panel(row, "Track", new Color(1f, 1f, 1f, 0.08f));
            NLUi.Size(track, preferredWidth: 200f, preferredHeight: 14f, flexibleWidth: 1f);
            var fill = NLUi.Panel(track, "Fill", NLUi.Good);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.5f, 1f); fill.offsetMin = fill.offsetMax = Vector2.zero;
            var value = NLUi.Label(row, "Value", "0", 16f, NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Right);
            NLUi.Size(value, preferredWidth: 48f);
            _bars[key] = (fill.GetComponent<Image>(), value);
        }
    }
}
