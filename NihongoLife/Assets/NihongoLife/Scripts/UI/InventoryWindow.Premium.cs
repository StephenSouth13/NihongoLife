using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Player;

namespace NihongoLife.UI
{
    /// <summary>
    /// G2: the premium bag. Hover tooltip, right-click menu (use / drop one / drop all / split / to chest), drag and drop to move or
    /// merge stacks, sort, filter chips (non-matching slots are dimmed so slot numbers stay stable), split half.
    /// </summary>
    public sealed partial class InventoryWindow
    {
        public enum BagFilter { All, Food, Drink, Other }

        public BagFilter Filter { get; private set; }
        public int HoverIndex => _hover;
        public bool TooltipVisible => _tip != null && _tip.gameObject.activeSelf;
        public bool MenuOpen => _menu != null && _menu.gameObject.activeSelf;
        public bool Dragging => _dragFrom >= 0;
        public string TooltipText => _tipTitle != null ? _tipTitle.text + "\n" + _tipBody.text : string.Empty;

        private RectTransform _tip, _menu, _ghost;
        private TextMeshProUGUI _tipTitle, _tipBody, _ghostGlyph;
        private Image _ghostTile;
        private int _hover = -1, _dragFrom = -1, _menuIndex = -1;
        private readonly List<(BagFilter filter, Image bg)> _chips = new List<(BagFilter, Image)>();
        private static readonly Color ChipOn = new Color(0.86f, 0.63f, 0.2f, 1f), ChipOff = new Color(1f, 1f, 1f, 0.08f);

        private void BuildToolbar(Transform window)
        {
            var bar = NLUi.Group(window, "Toolbar", false, 8f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)bar.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            void Chip(BagFilter f, string vi, string en, string ja)
            {
                var b = NLUi.Button(bar, "Filter_" + f, "", _font, () => SetFilter(f), ChipOff, 16f, Color.white, 36f);
                LocText.Set(b.GetComponentInChildren<TextMeshProUGUI>(), vi, en, ja);
                _chips.Add((f, b.GetComponent<Image>()));
            }
            Chip(BagFilter.All, "Tất cả", "All", "すべて");
            Chip(BagFilter.Food, "Đồ ăn", "Food", "食べ物");
            Chip(BagFilter.Drink, "Đồ uống", "Drinks", "飲み物");
            Chip(BagFilter.Other, "Khác", "Other", "その他");
            var spacer = new GameObject("Spacer", typeof(RectTransform)); spacer.transform.SetParent(bar, false);
            NLUi.Size(spacer.transform, flexibleWidth: 1f);
            var sort = NLUi.Button(bar, "SortButton", "", _font, SortBag, new Color(0.22f, 0.27f, 0.33f), 16f, Color.white, 36f);
            LocText.Set(sort.GetComponentInChildren<TextMeshProUGUI>(), "Sắp xếp", "Sort", "並べ替え");
            var split = NLUi.Button(bar, "SplitButton", "", _font, SplitSelected, new Color(0.22f, 0.27f, 0.33f), 16f, Color.white, 36f);
            LocText.Set(split.GetComponentInChildren<TextMeshProUGUI>(), "Tách chồng", "Split stack", "分ける");
            RefreshChips();
        }

        private void RefreshChips()
        {
            foreach (var (f, bg) in _chips) if (bg != null) bg.color = f == Filter ? ChipOn : ChipOff;
        }

        private void BuildOverlays(RectTransform root)
        {
            _tip = NLUi.Panel(root, "Tooltip", new Color(0.05f, 0.07f, 0.09f, 0.97f), new RectOffset(14, 14, 10, 12), 4f);
            _tip.anchorMin = _tip.anchorMax = new Vector2(0.5f, 0.5f); _tip.pivot = new Vector2(0f, 1f);
            NLUi.Size(_tip, preferredWidth: 330f);
            NLUi.FitContent(_tip);
            _tipTitle = NLUi.Label(_tip, "Title", "", 20f, NLUi.Gold, _font, FontStyles.Bold);
            _tipBody = NLUi.Label(_tip, "Body", "", 16f, NLUi.Text, _font);
            foreach (var g in _tip.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            _tip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _tip.gameObject.SetActive(false);

            _menu = NLUi.Panel(root, "ContextMenu", new Color(0.08f, 0.1f, 0.13f, 0.99f), new RectOffset(8, 8, 8, 8), 4f);
            _menu.anchorMin = _menu.anchorMax = new Vector2(0.5f, 0.5f); _menu.pivot = new Vector2(0f, 1f);
            NLUi.Size(_menu, preferredWidth: 230f);
            NLUi.FitContent(_menu);
            _menu.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _menu.gameObject.SetActive(false);

            _ghost = NLUi.Panel(root, "DragGhost", Color.white, new RectOffset(0, 0, 0, 0), 0f);
            _ghost.anchorMin = _ghost.anchorMax = new Vector2(0.5f, 0.5f); _ghost.pivot = new Vector2(0.5f, 0.5f);
            _ghost.sizeDelta = new Vector2(84f, 66f);
            _ghost.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _ghostTile = _ghost.GetComponent<Image>(); _ghostTile.raycastTarget = false;
            _ghostGlyph = NLUi.Label(_ghost, "Glyph", "", 40f, new Color(0.15f, 0.12f, 0.1f), _font, FontStyles.Bold, TextAlignmentOptions.Center);
            _ghostGlyph.raycastTarget = false;
            _ghost.gameObject.SetActive(false);
        }

        // ───────── filter / sort / split ─────────

        public void SetFilter(BagFilter f) { Filter = f; RefreshChips(); ApplyFilter(); }

        public static bool Matches(BagFilter f, InventoryEntry e)
        {
            if (e == null) return false;
            return f switch
            {
                BagFilter.All => true,
                BagFilter.Food => e.useType == ItemUseType.Food,
                BagFilter.Drink => e.useType == ItemUseType.Drink,
                _ => e.useType != ItemUseType.Food && e.useType != ItemUseType.Drink,
            };
        }

        private void ApplyFilter()
        {
            var items = PlayerInventory.Instance != null ? PlayerInventory.Instance.Items : null;
            for (int i = 0; i < _slots.Count; i++)
            {
                bool has = items != null && i < items.Count;
                bool dim = has && !Matches(Filter, items[i]);
                var c = _slots[i].tile.color; c.a = dim ? 0.2f : 1f; _slots[i].tile.color = c;
                var g = _slots[i].glyph.color; g.a = dim ? 0.25f : 1f; _slots[i].glyph.color = g;
                var n = _slots[i].name.color; n.a = dim ? 0.25f : 1f; _slots[i].name.color = n;
            }
        }

        public void SortBag()
        {
            PlayerInventory.Instance?.SortItems();
            _selected = 0;
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Pickup, 0.5f);
            Refresh();
        }

        public void SplitSelected() => SplitAt(_selected);

        public bool SplitAt(int index)
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || index < 0 || index >= inv.Items.Count) return false;
            bool ok = inv.SplitAt(index, inv.Items[index].quantity / 2);
            if (!ok) HudFeed.Post(Loc.T("Không tách được (cần ≥2 món và còn ô trống).", "Can't split (needs 2+ and a free slot).", "分けられません（2個以上と空きが必要）。"), HudFeed.Kind.Warning, 3f);
            Refresh();
            return ok;
        }

        // ───────── hover / menu / drag ─────────

        public void SlotEnter(int i) { _hover = i; }
        public void SlotExit(int i) { if (_hover == i) _hover = -1; if (_tip != null) _tip.gameObject.SetActive(false); }

        private void UpdateTooltip()
        {
            var inv = PlayerInventory.Instance;
            if (_tip == null) return;
            bool show = _hover >= 0 && inv != null && _hover < inv.Items.Count && !MenuOpen && !Dragging;
            if (!show) { if (_tip.gameObject.activeSelf) _tip.gameObject.SetActive(false); return; }
            var e = inv.Items[_hover];
            var sb = new StringBuilder();
            _tipTitle.text = string.IsNullOrEmpty(e.displayNameJa) ? e.Name : e.displayNameJa + (Loc.IsJapanese ? "" : "  <size=75%><color=#9FB0C0>" + e.Name + "</color></size>");
            sb.Append(Loc.F("Số lượng {0}  ·  Giá {1}", "Qty {0}  ·  Price {1}", "個数 {0}  ·  値段 {1}", e.quantity, Loc.Money(e.priceYen)));
            if (e.foodRestore > 0) sb.Append("\n").Append(Loc.T("No", "Food", "満腹")).Append(" +").Append(e.foodRestore.ToString("0"));
            if (e.drinkRestore > 0) sb.Append("\n").Append(Loc.T("Nước", "Drink", "水分")).Append(" +").Append(e.drinkRestore.ToString("0"));
            if (e.energyRestore > 0) sb.Append("\n").Append(Loc.T("Thể lực", "Stamina", "体力")).Append(" +").Append(e.energyRestore.ToString("0"));
            sb.Append("\n<color=#8A96A3>").Append(Loc.T("Chuột phải: menu · Kéo thả: đổi chỗ / gộp", "Right-click: menu · Drag: move / merge", "右クリック：メニュー · ドラッグ：移動／統合")).Append("</color>");
            _tipBody.text = sb.ToString();
            _tip.gameObject.SetActive(true);
            _tip.SetAsLastSibling();
            PlaceAtPointer(_tip, new Vector2(18f, -18f));
        }

        private void PlaceAtPointer(RectTransform r, Vector2 offset)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            PlaceAtScreen(r, mouse.position.ReadValue(), offset);
        }

        private void PlaceAtScreen(RectTransform r, Vector2 screen, Vector2 offset)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local)) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(r);
            Vector2 size = r.rect.size, pos = local + offset;
            Rect area = _root.rect;
            pos.x = Mathf.Clamp(pos.x, area.xMin, area.xMax - size.x);
            pos.y = Mathf.Clamp(pos.y, area.yMin + size.y, area.yMax);
            r.anchoredPosition = pos;
        }

        public void SlotRightClick(int i, Vector2 screen)
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || i < 0 || i >= inv.Items.Count) return;
            Select(i);
            ShowMenu(i, screen);
        }

        public void ShowMenu(int index, Vector2 screen)
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _menu == null || index < 0 || index >= inv.Items.Count) return;
            _menuIndex = index;
            for (int k = _menu.childCount - 1; k >= 0; k--) Destroy(_menu.GetChild(k).gameObject);
            var e = inv.Items[index];
            void Item(string vi, string en, string ja, System.Action act, Color color)
            {
                var b = NLUi.Button(_menu, "M_" + en, "", _font, () => { HideMenu(); act(); }, color, 17f, Color.white, 38f);
                LocText.Set(b.GetComponentInChildren<TextMeshProUGUI>(), vi, en, ja);
            }
            if (e.useType != ItemUseType.None) Item("Dùng / ăn", "Use / eat", "使う／食べる", () => { Select(index); UseSelected(); }, new Color(0.18f, 0.5f, 0.32f));
            if (e.quantity > 1) Item("Bỏ 1", "Drop one", "1個捨てる", () => DropAt(index, 1), new Color(0.35f, 0.16f, 0.15f));
            Item(e.quantity > 1 ? "Bỏ cả chồng" : "Bỏ", e.quantity > 1 ? "Drop stack" : "Drop", e.quantity > 1 ? "全部捨てる" : "捨てる", () => DropAt(index, e.quantity), new Color(0.35f, 0.16f, 0.15f));
            if (e.quantity > 1) Item("Tách đôi", "Split in half", "半分に分ける", () => SplitAt(index), new Color(0.22f, 0.27f, 0.33f));
            if (NihongoLife.Home.StorageChest.Reachable) Item("Cất vào rương", "Store in chest", "チェストに入れる", () => { NihongoLife.Home.StorageChest.Store(inv, index); Refresh(); }, new Color(0.45f, 0.33f, 0.12f));
            Item("Đóng", "Close", "閉じる", () => { }, new Color(0.14f, 0.17f, 0.21f));
            _menu.gameObject.SetActive(true);
            _menu.SetAsLastSibling();
            if (_tip != null) _tip.gameObject.SetActive(false);
            PlaceAtScreen(_menu, screen, new Vector2(4f, -4f));
        }

        public void HideMenu() { if (_menu != null) _menu.gameObject.SetActive(false); _menuIndex = -1; }

        public void DropAt(int index, int quantity)
        {
            var inv = PlayerInventory.Instance;
            if (inv == null) return;
            inv.RemoveAt(index, quantity);
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Drop, 0.7f);
            Refresh();
        }

        public void SlotBeginDrag(int i)
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || i < 0 || i >= inv.Items.Count || _ghost == null) { _dragFrom = -1; return; }
            _dragFrom = i;
            Visual(inv.Items[i], out Color tint, out string glyph);
            _ghostTile.color = new Color(tint.r, tint.g, tint.b, 0.85f);
            _ghostGlyph.text = glyph;
            ItemIcons.Apply(_ghostGlyph, inv.Items[i].itemId, 5f);
            _ghost.gameObject.SetActive(true);
            _ghost.SetAsLastSibling();
            if (_tip != null) _tip.gameObject.SetActive(false);
        }

        public void SlotDrag(Vector2 screen)
        {
            if (_dragFrom < 0 || _ghost == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local)) _ghost.anchoredPosition = local;
        }

        public void SlotEndDrag()
        {
            _dragFrom = -1;
            if (_ghost != null) _ghost.gameObject.SetActive(false);
        }

        /// <summary>Drop on slot <paramref name="to"/>: moves the dragged stack there, or merges when it is the same item.</summary>
        public bool DropOnSlot(int to)
        {
            int from = _dragFrom;
            var inv = PlayerInventory.Instance;
            _dragFrom = -1;
            if (_ghost != null) _ghost.gameObject.SetActive(false);
            if (inv == null || from < 0 || from == to) return false;
            bool ok = inv.MoveItem(from, to);
            if (ok) { _selected = Mathf.Clamp(to, 0, SlotCount - 1); if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Pickup, 0.5f); }
            Refresh();
            return ok;
        }

        /// <summary>Test hook: drag from one slot to another as the pointer would.</summary>
        public bool DragDrop(int from, int to) { SlotBeginDrag(from); return DropOnSlot(to); }
    }

    /// <summary>Pointer events of one bag slot, forwarded to the window.</summary>
    public sealed class BagSlotEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private InventoryWindow _window;
        private int _index;
        public void Init(InventoryWindow window, int index) { _window = window; _index = index; }
        public void OnPointerEnter(PointerEventData e) => _window.SlotEnter(_index);
        public void OnPointerExit(PointerEventData e) => _window.SlotExit(_index);
        public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Right) _window.SlotRightClick(_index, e.position); else _window.HideMenu(); }
        public void OnBeginDrag(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) _window.SlotBeginDrag(_index); }
        public void OnDrag(PointerEventData e) => _window.SlotDrag(e.position);
        public void OnEndDrag(PointerEventData e) => _window.SlotEndDrag();
        public void OnDrop(PointerEventData e) => _window.DropOnSlot(_index);
    }
}