using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    public class ShopUI : MonoBehaviour
    {
        private static ShopUI _instance;

        private GameObject _panel;
        private RectTransform _rowsRoot;
        private TextMeshProUGUI _balanceText;
        private TextMeshProUGUI _feedbackText;
        private GameObject _player;
        private readonly List<InteractiveItem> _items = new List<InteractiveItem>();

        public static ShopUI GetOrCreate()
        {
            if (_instance != null) return _instance;

            var existing = FindFirstObjectByType<ShopUI>();
            if (existing != null)
            {
                _instance = existing;
                return existing;
            }

            var host = new GameObject("ShopUI");
            _instance = host.AddComponent<ShopUI>();
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

        public void Show(IReadOnlyList<InteractiveItem> items, GameObject player)
        {
            _player = player;
            _items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item != null && item.gameObject.activeInHierarchy)
                    {
                        _items.Add(item);
                    }
                }
            }

            RebuildRows();
            SetFeedback(string.Empty, Color.white);
            RefreshBalance();
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

        private void Buy(InteractiveItem item)
        {
            var inventory = PlayerInventory.Instance;
            if (item == null || !item.gameObject.activeInHierarchy)
            {
                SetFeedback("Món này hiện đã hết hàng.", new Color(1f, 0.52f, 0.42f));
                RebuildRows();
                return;
            }

            if (inventory == null)
            {
                SetFeedback("Không tìm thấy túi đồ của người chơi.", new Color(1f, 0.52f, 0.42f));
                return;
            }

            int price = Mathf.Max(0, item.PriceYen);
            if (!inventory.SpendYen(price))
            {
                SetFeedback("Không đủ tiền để mua món này.", new Color(1f, 0.72f, 0.3f));
                RefreshBalance();
                return;
            }

            int quantityBefore = inventory.GetItemQuantity(item.ItemId);
            item.Interact(_player);
            int quantityAfter = inventory.GetItemQuantity(item.ItemId);
            if (quantityAfter <= quantityBefore)
            {
                inventory.AddYen(price);
                SetFeedback("Không thể mua món này lúc này. Tiền đã được hoàn lại.", new Color(1f, 0.52f, 0.42f));
                RefreshBalance();
                return;
            }

            SetFeedback($"Đã mua {DisplayName(item)}.", new Color(0.45f, 0.86f, 0.55f));
            RefreshBalance();
            RebuildRows();
        }

        private void BuildUI()
        {
            Canvas canvas = ResolveCanvas();
            _panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);
            var panelRect = (RectTransform)_panel.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 560f);
            UIStyleKit.StylePanel(panelRect, UIStyleKit.PanelBase);
            _panel.AddComponent<HudFitRect>().Configure(1f, 0.96f, new Vector2(32f, 0f), 0.4f);

            CreateText(panelRect, "Title", "CỬA HÀNG / お店", new Vector2(28f, -30f), new Vector2(560f, 48f), 30f, FontStyles.Bold);
            _balanceText = CreateText(panelRect, "Balance", string.Empty, new Vector2(28f, -82f), new Vector2(420f, 36f), 20f, FontStyles.Normal);

            Button close = CreateButton(panelRect, "Close", "X", new Vector2(-28f, -28f), new Vector2(48f, 44f));
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = Vector2.one;
            closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            close.onClick.AddListener(Close);

            var viewport = new GameObject("ItemsViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(panelRect, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = new Vector2(0f, 1f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.pivot = new Vector2(0.5f, 1f);
            viewportRect.anchoredPosition = new Vector2(0f, -126f);
            viewportRect.sizeDelta = new Vector2(-48f, 342f);
            viewport.GetComponent<Image>().color = new Color(0.025f, 0.033f, 0.04f, 0.72f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;

            var rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            rows.transform.SetParent(viewportRect, false);
            _rowsRoot = (RectTransform)rows.transform;
            _rowsRoot.anchorMin = new Vector2(0f, 1f);
            _rowsRoot.anchorMax = new Vector2(1f, 1f);
            _rowsRoot.pivot = new Vector2(0.5f, 1f);
            _rowsRoot.sizeDelta = Vector2.zero;
            var layout = rows.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            rows.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _feedbackText = CreateText(panelRect, "Feedback", string.Empty, new Vector2(28f, -486f), new Vector2(664f, 42f), 18f, FontStyles.Normal);
        }

        private void RebuildRows()
        {
            if (_rowsRoot == null) return;

            for (int i = _rowsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_rowsRoot.GetChild(i).gameObject);
            }

            foreach (var item in _items)
            {
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                CreateItemRow(item);
            }

            if (_rowsRoot.childCount == 0)
            {
                var empty = CreateText(_rowsRoot, "Empty", "Hết hàng / 売り切れ", Vector2.zero, new Vector2(0f, 64f), 20f, FontStyles.Normal);
                empty.alignment = TextAlignmentOptions.Center;
            }
        }

        private void CreateItemRow(InteractiveItem item)
        {
            var row = new GameObject($"Item_{item.ItemId}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(_rowsRoot, false);
            row.GetComponent<Image>().color = new Color(0.11f, 0.13f, 0.15f, 0.96f);
            row.GetComponent<LayoutElement>().preferredHeight = 78f;

            string reading = string.IsNullOrWhiteSpace(item.DisplayNameReading) ? string.Empty : $"  ({item.DisplayNameReading})";
            var name = CreateText(row.transform, "Name", $"{item.DisplayNameJa}{reading}\n<color=#AFC0CF>{item.DisplayNameEn}</color>", new Vector2(18f, -10f), new Vector2(420f, 58f), 19f, FontStyles.Normal);
            name.richText = true;

            var price = CreateText(row.transform, "Price", $"¥{item.PriceYen:N0}", new Vector2(444f, -20f), new Vector2(100f, 36f), 20f, FontStyles.Bold);
            price.alignment = TextAlignmentOptions.MidlineRight;

            Button buy = CreateButton(row.transform, "Buy", "Mua", new Vector2(-16f, -15f), new Vector2(92f, 48f));
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = new Vector2(1f, 1f);
            buyRect.anchorMax = new Vector2(1f, 1f);
            buyRect.pivot = new Vector2(1f, 1f);
            buy.onClick.AddListener(() => Buy(item));
        }

        private void RefreshBalance()
        {
            if (_balanceText == null) return;
            int yen = PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;
            _balanceText.text = $"Số dư: <color=#F1B83F>¥{yen:N0}</color>";
        }

        private void SetFeedback(string message, Color color)
        {
            if (_feedbackText == null) return;
            _feedbackText.text = message;
            _feedbackText.color = color;
        }

        private static string DisplayName(InteractiveItem item)
        {
            return !string.IsNullOrWhiteSpace(item.DisplayNameEn) ? item.DisplayNameEn : item.DisplayNameJa;
        }

        private static Canvas ResolveCanvas()
        {
            var hud = FindFirstObjectByType<HUDUI>();
            Canvas canvas = hud != null ? hud.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas;

            var canvasObject = new GameObject("ShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, FontStyles style)
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
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var button = go.GetComponent<Button>();
            UIStyleKit.StyleButton(button, UIStyleKit.AccentGold, UIStyleKit.AccentGoldHover, UIStyleKit.AccentGoldPressed);
            var text = CreateText(go.transform, "Label", label, Vector2.zero, size, 19f, FontStyles.Bold);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.08f, 0.09f, 0.1f, 1f);
            text.raycastTarget = false;
            return button;
        }
    }
}
