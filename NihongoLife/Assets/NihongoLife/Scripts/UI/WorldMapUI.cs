using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using NihongoLife.Player;
using UnityEngine.AI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Map window (M). Overview: real-coordinate roads, kanji place markers and a heading arrow for the
    /// player; sidebar lists every place with its distance — click a place or anywhere on the map to walk
    /// there. "Current area" shows a live top-down camera of the zone.
    /// </summary>
    public class WorldMapUI : MonoBehaviour
    {
        private sealed class MapPlace
        {
            public string Name, Type, Glyph, Short;
            public Vector2 World;     // world X/Z, or NaN when only a pixel position is known
            public Vector2 Pixel;
            public Color Color;
            public bool HasWorld => !float.IsNaN(World.x);
        }

        private const float MapW = 960f, MapH = 560f, Inset = 40f;

        private GameObject _overlay;
        private RectTransform _mapArea, _playerMarker, _destinationMarker, _sidebarList;
        private RawImage _topDownImage;
        private Camera _topDownCamera;
        private RenderTexture _topDownTexture;
        private Button _areaTab, _overviewTab;
        private bool _showTopDown;
        private TextMeshProUGUI _header, _location, _coordinates;
        private Transform _player;
        private TMP_FontAsset _font;
        private Vector2 _worldMin, _worldMax;
        private readonly List<MapPlace> _places = new List<MapPlace>();
        private readonly List<Rect> _labelRects = new List<Rect>();
        private readonly List<(MapPlace place, TextMeshProUGUI distance)> _rows = new List<(MapPlace, TextMeshProUGUI)>();
        public bool IsVisible => _overlay != null && _overlay.activeSelf;
        public event Action<bool> OnVisibilityChanged;

        public void Initialize(TMP_FontAsset font)
        {
            if (_overlay != null) return;
            _font = font != null ? font : NLUi.ResolveFont();
            if (GameServices.TryGet(out GameSettingsService languageSettings)) languageSettings.OnLanguageChanged += HandleGlobalLanguageChanged;

            var canvas = GetComponentInParent<Canvas>();
            _overlay = new GameObject("WorldMap", typeof(RectTransform), typeof(Image));
            _overlay.transform.SetParent(canvas != null ? canvas.transform : transform, false);
            NLUi.Stretch((RectTransform)_overlay.transform);
            _overlay.GetComponent<Image>().color = new Color(0f, 0.01f, 0.02f, 0.72f);
            _overlay.transform.SetAsLastSibling();

            var window = NLUi.Panel(_overlay.transform, "MapWindow", NLUi.Ink);
            NLUi.Anchor(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 800f));

            _header = NLUi.Label(window, "Header", "", 30f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Anchor(_header.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -26f), new Vector2(700f, 44f));
            _location = NLUi.Label(window, "Location", "", 18f, NLUi.Muted, _font);
            NLUi.Anchor(_location.rectTransform, new Vector2(0f, 1f), new Vector2(38f, -70f), new Vector2(700f, 28f));

            var close = NLUi.Button(window, "Close", "×", _font, () => SetVisible(false), NLUi.Card, 26f, null, 48f);
            NLUi.Anchor((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(56f, 52f));

            var mapFrame = NLUi.Panel(window, "MapFrame", new Color(0.075f, 0.12f, 0.14f, 1f));
            NLUi.Anchor(mapFrame, new Vector2(0f, 1f), new Vector2(30f, -112f), new Vector2(MapW + 12f, MapH + 12f));
            var areaGo = new GameObject("MapArea", typeof(RectTransform), typeof(Image), typeof(Mask));
            areaGo.transform.SetParent(mapFrame, false);
            _mapArea = (RectTransform)areaGo.transform;
            NLUi.Anchor(_mapArea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MapW, MapH));
            var areaImage = areaGo.GetComponent<Image>();
            areaImage.sprite = UIStyleKit.RoundedSprite();
            areaImage.type = Image.Type.Sliced;
            areaImage.color = new Color(0.1f, 0.16f, 0.18f, 1f);
            areaGo.GetComponent<Mask>().showMaskGraphic = true;
            var clickTarget = _mapArea.gameObject.AddComponent<MapClickTarget>();
            clickTarget.Clicked += HandleMapClick;

            BuildTabs(window);

            var sidebar = NLUi.Panel(window, "Places", NLUi.Card, new RectOffset(18, 18, 16, 16), 8f);
            NLUi.Anchor(sidebar, new Vector2(1f, 1f), new Vector2(-30f, -112f), new Vector2(362f, MapH + 12f));
            var layout = sidebar.GetComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            NLUi.Label(sidebar, "Title", "ちめい  <size=75%><color=#A8B4C4>Địa điểm · bấm để đi tới</color></size>", 21f, NLUi.Text, _font, FontStyles.Bold);
            _sidebarList = NLUi.Group(sidebar, "List", true, 6f);

            _coordinates = NLUi.Label(window, "Coordinates", "", 16f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Left);
            NLUi.Anchor(_coordinates.rectTransform, new Vector2(0f, 0f), new Vector2(36f, 22f), new Vector2(1320f, 30f));
            _overlay.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameServices.TryGet(out GameSettingsService settings)) settings.OnLanguageChanged -= HandleGlobalLanguageChanged;
            if (_topDownCamera != null) Destroy(_topDownCamera.gameObject);
            if (_topDownTexture != null) { _topDownTexture.Release(); Destroy(_topDownTexture); }
        }

        private void HandleGlobalLanguageChanged(GameLanguage _)
        {
            if (_areaTab != null) _areaTab.GetComponentInChildren<TextMeshProUGUI>().text = L("Khu vực hiện tại", "Current area", "現在のエリア");
            if (_overviewTab != null) _overviewTab.GetComponentInChildren<TextMeshProUGUI>().text = L("Bản đồ tổng", "Overview map", "全体マップ");
            if (IsVisible) { BuildMap(); RefreshMarker(); }
        }

        public void SetVisible(bool visible)
        {
            if (_overlay == null) return;
            _overlay.SetActive(visible);
            if (visible) _overlay.transform.SetAsLastSibling();
            OnVisibilityChanged?.Invoke(visible);
            if (!visible)
            {
                if (_topDownCamera != null) _topDownCamera.enabled = false;
                return;
            }
            GameObject found = GameObject.FindWithTag("Player");
            _player = found != null ? found.transform : null;
            BuildMap(); RefreshMarker();
            UpdateTopDownCamera();
        }

        private void BuildTabs(Transform window)
        {
            var tabs = NLUi.Group(window, "Tabs", false, 10f, TextAnchor.MiddleRight, false);
            NLUi.Anchor(tabs, new Vector2(1f, 1f), new Vector2(-96f, -30f), new Vector2(460f, 46f));
            ((HorizontalLayoutGroup)tabs.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            _areaTab = NLUi.Button(tabs, "MapTab_Area", L("Khu vực hiện tại", "Current area", "現在のエリア"), _font, () => SetMapMode(true), NLUi.Card, 17f, null, 44f);
            _overviewTab = NLUi.Button(tabs, "MapTab_Overview", L("Bản đồ tổng", "Overview map", "全体マップ"), _font, () => SetMapMode(false), NLUi.Card, 17f, null, 44f);
            var topDownObject = new GameObject("TopDownMap", typeof(RectTransform), typeof(RawImage));
            topDownObject.transform.SetParent(_mapArea, false);
            _topDownImage = topDownObject.GetComponent<RawImage>();
            _topDownImage.color = Color.white;
            NLUi.Stretch(_topDownImage.rectTransform);
            _topDownImage.raycastTarget = false;
            SetMapMode(false);
        }

        private void SetMapMode(bool topDown)
        {
            _showTopDown = topDown;
            _topDownImage.gameObject.SetActive(topDown);
            foreach (Transform child in _mapArea)
                if (child.name == "Route" || child.name == "Grid" || child.name.StartsWith("Place_")) child.gameObject.SetActive(!topDown);
            _mapArea.GetComponent<MapClickTarget>().enabled = true;
            if (_areaTab != null) _areaTab.GetComponent<Image>().color = topDown ? NLUi.Gold : NLUi.Card;
            if (_overviewTab != null) _overviewTab.GetComponent<Image>().color = topDown ? NLUi.Card : NLUi.Gold;
            if (_areaTab != null) _areaTab.GetComponentInChildren<TextMeshProUGUI>().color = topDown ? NLUi.Ink : NLUi.Text;
            if (_overviewTab != null) _overviewTab.GetComponentInChildren<TextMeshProUGUI>().color = topDown ? NLUi.Text : NLUi.Ink;
            if (_topDownCamera != null) _topDownCamera.enabled = topDown && IsVisible;
            if (topDown) UpdateTopDownCamera();
        }

        private void UpdateTopDownCamera()
        {
            if (!_showTopDown || _player == null) return;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            if (_topDownTexture == null)
            {
                _topDownTexture = new RenderTexture(1024, 600, 16, RenderTextureFormat.ARGB32);
                _topDownTexture.name = "NihongoLife_TopDownMap";
                _topDownTexture.Create();
            }
            if (_topDownCamera == null)
            {
                var go = new GameObject("TopDownMapCamera");
                _topDownCamera = go.AddComponent<Camera>();
                _topDownCamera.orthographic = true;
                _topDownCamera.cullingMask = ~(LayerMask.GetMask("MapRoof") | (1 << 5));
                _topDownCamera.clearFlags = CameraClearFlags.SolidColor;
                _topDownCamera.backgroundColor = new Color(.035f, .055f, .065f, 1f);
                _topDownCamera.targetTexture = _topDownTexture;
                _topDownImage.texture = _topDownTexture;
            }
            Vector2 center = (_worldMin + _worldMax) * .5f;
            Vector2 size = _worldMax - _worldMin;
            _topDownCamera.orthographicSize = size.y * .5f;
            _topDownCamera.aspect = size.x / size.y;
            _topDownCamera.transform.SetPositionAndRotation(new Vector3(center.x, _player.position.y + 80f, center.y), Quaternion.Euler(90f, 0f, 0f));
            _topDownCamera.enabled = true;
        }

        private void BuildMap()
        {
            for (int i = _mapArea.childCount - 1; i >= 0; i--)
            {
                Transform child = _mapArea.GetChild(i);
                if (_topDownImage != null && child == _topDownImage.transform) continue;
                Destroy(child.gameObject);
            }
            foreach (Transform child in _sidebarList) Destroy(child.gameObject);
            _places.Clear();
            _rows.Clear();
            string scene = SceneManager.GetActiveScene().name;
            _location.text = L("Bạn đang ở: ", "You are in: ", "現在地: ") + WorldLocationCatalog.Get(scene).DisplayName;
            if (scene == WorldLocationCatalog.StationScene) StationMap();
            else if (scene == WorldLocationCatalog.SushiRestaurantScene) SushiMap();
            else if (scene == WorldLocationCatalog.SchoolScene) SchoolMap();
            else if (scene == WorldLocationCatalog.HomeBedroomScene) BedroomMap();
            else if (scene == WorldLocationCatalog.GameCenterScene) GameCenterMap();
            else CityMap();
            DrawGrid();
            _labelRects.Clear();
            foreach (var place in _places) DrawPlace(place);
            BuildSidebar();
            DrawPlayerMarker();
            SetMapMode(_showTopDown);
        }

        // ─────────── Map definitions ───────────

        private void CityMap()
        {
            _header.text = L("BẢN ĐỒ HIBARI-CHŌ", "HIBARI-CHŌ MAP", "ひばり町 地図");
            _worldMin = new(-42, -32); _worldMax = new(58, 20);
            Road(MapPos(8f, -10f), MapSize(100f, 9f));
            Road(MapPos(0f, -6f), MapSize(9f, 52f));
            Road(MapPos(-12f, -6f), MapSize(5f, 52f));
            Road(MapPos(12f, -6f), MapSize(5f, 52f));
            AddWorld(L("Siêu thị Hibari Mart", "Hibari Mart", "ひばりマート"), "KONBINI", "店", 0f, 4f, new(.2f, .76f, .66f), "ひばりマート");
            AddWorld(L("Bảng tin khu phố", "Notice board", "けいじばん"), "PLAZA", "掲", -8f, 1.5f, new(.85f, .7f, .4f), "けいじばん");
            AddWorld(L("Sushi Hibari", "Sushi Hibari", "ひばり寿司"), "SUSHI", "寿", -18f, 0.3f, new(.94f, .42f, .36f), "すし");
            AddWorld(L("Nhà trọ Hibari Heights", "Hibari Heights (home)", "ひばりハイツ"), "HOME", "家", -30f, 1.3f, new(.62f, .76f, .38f), "いえ");
            AddWorld(L("Ga Hibari", "Hibari Station", "ひばり駅"), "STATION", "駅", 24f, 0.3f, new(.3f, .62f, .94f), "えき");
            AddWorld(L("Trường Nhật ngữ Hibari", "Hibari Japanese School", "ひばり日本語学院"), "SCHOOL", "学", 18f, -20f, new(.2f, .55f, .32f), "がっこう");
            AddWorld(L("Công viên", "Park", "こうえん"), "PARK", "園", 48f, -4f, new(.42f, .72f, .4f), "こうえん");
            AddWorld(L("Game Center Hibari", "Game Center Hibari", "ゲームセンター"), "ARCADE", "遊", -6f, -20.5f, new(.92f, .36f, .72f), "ゲーセン");
        }

        private void GameCenterMap()
        {
            _header.text = L("SƠ ĐỒ GAME CENTER", "GAME CENTER FLOOR MAP", "ゲームセンター 案内図");
            _worldMin = new(288, -10); _worldMax = new(312, 10);
            Road(Vector2.zero, new(MapW - 120, MapH - 80));
            AddPixel(L("Kana Match · chơi ngay", "Kana Match · play", "かなマッチ"), "KANA", "か", new(-140, 40), new(.92f, .36f, .72f));
            AddPixel(L("Word Shooter · sắp mở", "Word Shooter · soon", "ワードシューター"), "SHOOT", "撃", new(0, 40), new(.3f, .62f, .94f));
            AddPixel(L("Order Rush · sắp mở", "Order Rush · soon", "オーダーラッシュ"), "ORDER", "注", new(140, 40), new(.94f, .68f, .25f));
            AddPixel(L("Quầy đổi quà", "Prize counter", "景品カウンター"), "PRIZE", "賞", new(300, -150), new(.45f, .78f, .48f));
            AddPixel(L("Về thành phố", "Return to city", "出口"), "EXIT", "出", new(0, -200), new(.45f, .78f, .48f));
        }

        private void StationMap()
        {
            _header.text = L("SƠ ĐỒ GA HIBARI", "HIBARI STATION MAP", "ひばり駅 構内図");
            _worldMin = new(742, -20); _worldMax = new(858, 16);
            Road(new(0, 10), new(MapW - 60, 120)); Road(new(0, -110), new(MapW - 60, 52));
            AddPixel(L("Máy bán vé", "Ticket machine", "券売機"), "TICKETS", "券", new(-330, 170), new(.22f, .7f, .78f));
            AddPixel(L("Cổng soát vé", "Ticket gate", "改札"), "GATE", "改", new(-90, 90), new(.94f, .68f, .25f));
            AddPixel(L("Sân ga số 1 · đi Midori", "Platform 1 · to Midori", "1番線 みどり行き"), "PLATFORM", "線", new(190, -70), new(.3f, .62f, .94f));
            AddPixel(L("Lối ra thành phố", "City exit", "出口"), "EXIT", "出", new(360, 170), new(.45f, .78f, .48f));
        }

        private void SushiMap()
        {
            _header.text = L("SƠ ĐỒ SUSHI HIBARI", "SUSHI HIBARI FLOOR MAP", "ひばり寿司 店内図");
            _worldMin = new(492, -9); _worldMax = new(508, 9);
            Road(Vector2.zero, new(MapW - 120, MapH - 80));
            AddPixel(L("Lễ tân", "Reception", "受付"), "MENU", "受", new(-300, -170), new(.22f, .7f, .78f));
            AddPixel(L("Quầy đầu bếp Ota", "Chef Ota counter", "板前"), "ORDER", "板", new(0, 170), new(.94f, .48f, .3f));
            AddPixel(L("Khu bàn ăn", "Dining area", "客席"), "SEATS", "席", new(20, -15), new(.72f, .62f, .36f));
            AddPixel(L("Về thành phố", "Return to city", "出口"), "EXIT", "出", new(320, -180), new(.45f, .78f, .48f));
        }

        private void SchoolMap()
        {
            _header.text = L("SƠ ĐỒ TRƯỜNG HIBARI", "HIBARI SCHOOL FLOOR MAP", "ひばり日本語学院 見取り図");
            _worldMin = new(990, -10); _worldMax = new(1010, 10);
            Road(Vector2.zero, new(MapW - 120, MapH - 80));
            AddPixel(L("Bảng đen / Cô Morita", "Blackboard / Teacher Morita", "黒板・森田先生"), "TEACHER", "先", new(0, 170), new(.75f, .35f, .55f));
            AddPixel(L("Bàn học sinh", "Student desks", "机"), "DESKS", "机", new(-60, -20), new(.94f, .78f, .3f));
            AddPixel(L("Về thành phố", "Return to city", "出口"), "EXIT", "出", new(0, -190), new(.45f, .78f, .48f));
        }

        private void BedroomMap()
        {
            _header.text = L("PHÒNG TRỌ CỦA BẠN", "YOUR ROOM", "じぶんの へや");
            _worldMin = new(-4.2f, -3.4f); _worldMax = new(4.2f, 3.4f);
            Road(MapPos(0f, 0f), MapSize(7.2f, 6f));
            AddWorld(L("Giường · ngủ", "Bed · sleep", "ベッド"), "REST", "寝", -2.5f, 1.9f, new(.3f, .62f, .94f));
            AddWorld(L("Bàn học · ôn từ", "Desk · study", "つくえ"), "STUDY", "机", 1.6f, 2.4f, new(.2f, .72f, .64f));
            AddWorld(L("Bếp & tủ lạnh", "Kitchen & fridge", "だいどころ"), "KITCHEN", "台", -3.0f, -1.0f, new(.94f, .68f, .25f));
            AddWorld(L("Cửa ra phố", "Door to the street", "げんかん"), "EXIT", "出", -2.4f, -2.6f, new(.45f, .78f, .48f));
        }

        private void AddWorld(string name, string type, string glyph, float x, float z, Color color, string shortLabel = null) =>
            _places.Add(new MapPlace { Name = name, Type = type, Glyph = glyph, Short = shortLabel, World = new Vector2(x, z), Pixel = MapPos(x, z), Color = color });

        private void AddPixel(string name, string type, string glyph, Vector2 pixel, Color color) =>
            _places.Add(new MapPlace { Name = name, Type = type, Glyph = glyph, World = new Vector2(float.NaN, float.NaN), Pixel = pixel, Color = color });

        private Vector2 MapPos(float x, float z)
        {
            Vector2 n = new(Mathf.InverseLerp(_worldMin.x, _worldMax.x, x), Mathf.InverseLerp(_worldMin.y, _worldMax.y, z));
            return new((n.x - .5f) * (MapW - Inset), (n.y - .5f) * (MapH - Inset));
        }

        private Vector2 MapSize(float width, float depth) =>
            new(width / (_worldMax.x - _worldMin.x) * (MapW - Inset), depth / (_worldMax.y - _worldMin.y) * (MapH - Inset));

        // ─────────── Drawing ───────────

        private void DrawGrid()
        {
            var grid = new GameObject("Grid", typeof(RectTransform));
            grid.transform.SetParent(_mapArea, false);
            grid.transform.SetAsFirstSibling();
            NLUi.Stretch((RectTransform)grid.transform);
            for (int i = 1; i < 12; i++)
            {
                Line(grid.transform, new Vector2(-MapW / 2f + i * MapW / 12f, 0f), new Vector2(1f, MapH));
                if (i < 7) Line(grid.transform, new Vector2(0f, -MapH / 2f + i * MapH / 7f), new Vector2(MapW, 1f));
            }
        }

        private static void Line(Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject("GridLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.035f);
            go.GetComponent<Image>().raycastTarget = false;
            NLUi.Anchor((RectTransform)go.transform, new Vector2(0.5f, 0.5f), position, size);
        }

        private void Road(Vector2 p, Vector2 s, float angle = 0)
        {
            var road = NLUi.Panel(_mapArea, "Route", new Color(0.26f, 0.32f, 0.35f, 1f));
            road.GetComponent<Image>().raycastTarget = false;
            NLUi.Anchor(road, new Vector2(0.5f, 0.5f), p, s);
            road.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private void DrawPlace(MapPlace place)
        {
            var marker = NLUi.Panel(_mapArea, "Place_" + place.Type, place.Color);
            NLUi.Anchor(marker, new Vector2(0.5f, 0.5f), place.Pixel, new Vector2(46f, 46f));
            marker.GetComponent<Image>().raycastTarget = false;
            NLUi.Stretch(NLUi.Label(marker, "Glyph", place.Glyph, 26f, new Color(0.08f, 0.1f, 0.12f), _font, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform);
            string text = string.IsNullOrEmpty(place.Short) ? place.Name : place.Short;
            var label = NLUi.Pill(marker, "Label", text, _font, new Color(0.05f, 0.07f, 0.09f, 0.88f), NLUi.Text, 15f);
            // Below the marker by default; flip above when that would cover a neighbour's label.
            float width = text.Length * 16f + 34f;
            var below = new Rect(place.Pixel.x - width / 2f, place.Pixel.y - 23f - 30f, width, 30f);
            bool above = _labelRects.Exists(r => r.Overlaps(below));
            var chosen = above ? new Rect(below.x, place.Pixel.y + 23f, width, 30f) : below;
            _labelRects.Add(chosen);
            label.anchorMin = label.anchorMax = new Vector2(0.5f, above ? 1f : 0f);
            label.pivot = new Vector2(0.5f, above ? 0f : 1f);
            label.anchoredPosition = new Vector2(0f, above ? 6f : -6f);
        }

        private void BuildSidebar()
        {
            foreach (var place in _places)
            {
                var p = place;
                var row = NLUi.Panel(_sidebarList, "Row_" + place.Type, new Color(1f, 1f, 1f, 0.04f), new RectOffset(10, 12, 7, 7), 10f, vertical: false);
                ((HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                var dot = NLUi.Pill(row, "Glyph", place.Glyph, _font, place.Color, new Color(0.08f, 0.1f, 0.12f), 18f);
                NLUi.Size(dot, preferredWidth: 38f);
                var name = NLUi.Label(row, "Name", place.Name, 17f, NLUi.Text, _font);
                NLUi.Size(name, flexibleWidth: 1f);
                var distance = NLUi.Label(row, "Distance", "", 15f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Right);
                distance.textWrappingMode = TextWrappingModes.NoWrap;
                NLUi.Size(distance, preferredWidth: 58f);
                if (place.HasWorld)
                {
                    var button = row.gameObject.AddComponent<Button>();
                    button.targetGraphic = row.GetComponent<Image>();
                    button.onClick.AddListener(() => WalkTo(new Vector3(p.World.x, _player != null ? _player.position.y : 0f, p.World.y), p.Pixel));
                }
                _rows.Add((place, distance));
            }
        }

        private void DrawPlayerMarker()
        {
            _destinationMarker = NLUi.Panel(_mapArea, "Destination", NLUi.Bad);
            NLUi.Anchor(_destinationMarker, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
            _destinationMarker.gameObject.SetActive(false);
            _playerMarker = new GameObject("YouAreHere", typeof(RectTransform)).GetComponent<RectTransform>();
            _playerMarker.SetParent(_mapArea, false);
            NLUi.Anchor(_playerMarker, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            var ring = NLUi.Panel(_playerMarker, "Ring", new Color(1f, 0.82f, 0.2f, 0.25f));
            NLUi.Stretch(ring, -10f);
            var arrow = NLUi.Panel(_playerMarker, "Arrow", NLUi.Gold);
            NLUi.Anchor(arrow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
            arrow.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var tip = NLUi.Panel(_playerMarker, "Tip", Color.white);
            NLUi.Anchor(tip, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(8f, 12f));
            var you = NLUi.Pill(_playerMarker, "Label", L("BẠN", "YOU", "現在地"), _font, NLUi.Gold, NLUi.Ink, 13f);
            you.anchorMin = you.anchorMax = new Vector2(0.5f, 1f);
            you.pivot = new Vector2(0.5f, 0f);
            you.anchoredPosition = new Vector2(0f, 14f);
        }

        private void LateUpdate() { if (IsVisible) { RefreshMarker(); UpdateTopDownCamera(); } }

        private void RefreshMarker()
        {
            if (_player == null || _playerMarker == null) return;
            Vector2 p = new(_player.position.x, _player.position.z);
            _playerMarker.anchoredPosition = MapPos(p.x, p.y);
            _playerMarker.localRotation = Quaternion.Euler(0f, 0f, -_player.eulerAngles.y);
            var label = _playerMarker.Find("Label");
            if (label != null) label.rotation = Quaternion.identity;
            foreach (var (place, distance) in _rows)
                distance.text = place.HasWorld ? $"{Vector2.Distance(p, place.World):0}m" : string.Empty;
            _coordinates.text = $"{L("Vị trí", "Position", "位置")}  X {_player.position.x:0.0}  Z {_player.position.z:0.0}     ·     " +
                                L("Bấm vào bản đồ hoặc danh sách để tự đi tới", "Click the map or a place to walk there", "地図か場所をクリックすると歩いて行きます") +
                                $"     ·     M / Esc: {L("đóng", "close", "閉じる")}";
        }

        private void HandleMapClick(Vector2 localPosition)
        {
            if (_player == null || _mapArea == null) return;
            Vector2 normalized = new(
                Mathf.Clamp01(localPosition.x / (MapW - Inset) + 0.5f),
                Mathf.Clamp01(localPosition.y / (MapH - Inset) + 0.5f));
            Vector3 destination = new(
                Mathf.Lerp(_worldMin.x, _worldMax.x, normalized.x),
                _player.position.y,
                Mathf.Lerp(_worldMin.y, _worldMax.y, normalized.y));
            WalkTo(destination, localPosition);
        }

        private void WalkTo(Vector3 destination, Vector2 markerPosition)
        {
            if (_player == null) return;
            if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            {
                destination = navHit.position;
            }
            else if (Physics.Raycast(destination + Vector3.up * 30f, Vector3.down, out RaycastHit ground,
                60f, Physics.DefaultRaycastLayers & ~LayerMask.GetMask("MapRoof"), QueryTriggerInteraction.Ignore))
            {
                destination = ground.point;
            }
            else
            {
                return;
            }
            _destinationMarker.anchoredPosition = markerPosition;
            _destinationMarker.gameObject.SetActive(true);
            _player.GetComponent<PlayerController>()?.SetClickDestination(destination);
            SetVisible(false);
        }

        private sealed class MapClickTarget : MonoBehaviour, IPointerClickHandler
        {
            public event Action<Vector2> Clicked;
            public void OnPointerClick(PointerEventData eventData)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        (RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 local))
                    Clicked?.Invoke(local);
            }
        }

        private string L(string vi, string en, string ja) => GameServices.TryGet(out GameSettingsService s) ? s.Text(vi, en, ja) : vi;
    }
}
