using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    public class WorldMapUI : MonoBehaviour
    {
        private readonly Vector2 _worldMin = new Vector2(-66f, -50f);
        private readonly Vector2 _worldMax = new Vector2(66f, 38f);
        private GameObject _overlay;
        private RectTransform _mapArea;
        private RectTransform _playerMarker;
        private TextMeshProUGUI _coordinates;
        private Transform _player;
        private TMP_FontAsset _font;

        public bool IsVisible => _overlay != null && _overlay.activeSelf;
        public event Action<bool> OnVisibilityChanged;

        public void Initialize(TMP_FontAsset font)
        {
            if (_overlay != null) return;
            _font = font;
            _overlay = Panel("WorldMap", transform, new Color(0.015f, 0.022f, 0.026f, 0.97f));
            Stretch(_overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var header = Label("Header", _overlay.transform, "BẢN ĐỒ THỊ TRẤN  |  町の地図", font, 26f, FontStyles.Bold);
            SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(760f, 46f));
            header.alignment = TextAlignmentOptions.Center;
            header.color = new Color(0.96f, 0.78f, 0.30f);

            var close = Panel("Close", _overlay.transform, new Color(0.16f, 0.19f, 0.20f, 1f));
            SetRect(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(44f, 40f));
            var closeButton = close.AddComponent<Button>();
            closeButton.targetGraphic = close.GetComponent<Image>();
            closeButton.onClick.AddListener(() => SetVisible(false));
            var closeLabel = Label("Label", close.transform, "X", font, 19f, FontStyles.Bold);
            Stretch(closeLabel.rectTransform, Vector2.zero, Vector2.zero);
            closeLabel.alignment = TextAlignmentOptions.Center;
            UIStyleKit.StyleButton(closeButton, new Color(0.16f, 0.19f, 0.20f), new Color(0.28f, 0.32f, 0.33f), new Color(0.10f, 0.12f, 0.13f));

            _mapArea = Panel("MapArea", _overlay.transform, new Color(0.09f, 0.16f, 0.16f, 1f)).GetComponent<RectTransform>();
            SetRect(_mapArea, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(820f, 520f));
            AddRoad(new Vector2(0f, 0f), new Vector2(760f, 58f), 0f);
            AddRoad(new Vector2(-120f, 0f), new Vector2(54f, 460f), 0f);
            AddRoad(new Vector2(190f, 35f), new Vector2(48f, 390f), 18f);
            AddRoad(new Vector2(40f, 125f), new Vector2(650f, 38f), -8f);

            AddPlace("Cửa hàng tiện lợi\nコンビニ", new Vector2(0f, -28f), new Color(0.20f, 0.76f, 0.66f));
            AddPlace("Nhà ga\n駅前", new Vector2(260f, 120f), new Color(0.30f, 0.62f, 0.94f));
            AddPlace("Nhà hàng sushi\nすし店", new Vector2(-245f, 132f), new Color(0.94f, 0.42f, 0.36f));
            AddPlace("Khu dân cư\n住宅街", new Vector2(-245f, -150f), new Color(0.62f, 0.76f, 0.38f));
            AddPlace("Công viên\n公園", new Vector2(245f, -145f), new Color(0.42f, 0.72f, 0.40f));

            _playerMarker = Panel("PlayerMarker", _mapArea, new Color(1f, 0.84f, 0.22f)).GetComponent<RectTransform>();
            _playerMarker.sizeDelta = new Vector2(18f, 24f);
            _playerMarker.pivot = new Vector2(0.5f, 0.25f);

            _coordinates = Label("Coordinates", _overlay.transform, "", font, 15f, FontStyles.Normal);
            SetRect(_coordinates.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(620f, 34f));
            _coordinates.alignment = TextAlignmentOptions.Center;
            _coordinates.color = new Color(0.72f, 0.82f, 0.84f);
            _overlay.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (_overlay == null) return;
            _overlay.SetActive(visible);
            OnVisibilityChanged?.Invoke(visible);
            if (visible)
            {
                var found = GameObject.FindWithTag("Player");
                _player = found != null ? found.transform : null;
                RefreshPlayerMarker();
            }
        }

        private void LateUpdate()
        {
            if (IsVisible) RefreshPlayerMarker();
        }

        private void RefreshPlayerMarker()
        {
            if (_player == null || _playerMarker == null) return;
            Vector2 world = new Vector2(_player.position.x, _player.position.z);
            Vector2 normalized = new Vector2(
                Mathf.InverseLerp(_worldMin.x, _worldMax.x, world.x),
                Mathf.InverseLerp(_worldMin.y, _worldMax.y, world.y));
            _playerMarker.anchoredPosition = new Vector2(
                (normalized.x - 0.5f) * (_mapArea.rect.width - 40f),
                (normalized.y - 0.5f) * (_mapArea.rect.height - 40f));
            _playerMarker.localRotation = Quaternion.Euler(0f, 0f, -_player.eulerAngles.y);
            _coordinates.text = $"Vị trí hiện tại  X {_player.position.x:0}  Z {_player.position.z:0}    |    M / Esc: đóng bản đồ";
        }

        private void AddRoad(Vector2 position, Vector2 size, float angle)
        {
            var road = Panel("Road", _mapArea, new Color(0.25f, 0.29f, 0.29f, 1f)).GetComponent<RectTransform>();
            road.anchoredPosition = position;
            road.sizeDelta = size;
            road.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void AddPlace(string value, Vector2 position, Color color)
        {
            var marker = Panel("Place", _mapArea, color).GetComponent<RectTransform>();
            marker.anchoredPosition = position;
            marker.sizeDelta = new Vector2(14f, 14f);
            var label = Label("Label", marker, value, _font, 14f, FontStyles.Bold);
            SetRect(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(180f, 46f));
            label.alignment = TextAlignmentOptions.Top;
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string value, TMP_FontAsset font, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = Color.white;
            label.characterSpacing = 0f;
            return label;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }
    }
}
