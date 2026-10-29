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
using System.Collections;

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
            /// <summary>A small marker without a label (a crowded map labels only its main places).</summary>
            public bool Quiet;
            public bool HasWorld => !float.IsNaN(World.x);
            /// <summary>Scene behind this place (for unlocks and the guide), null for spots inside a scene.</summary>
            public string Scene => SceneOfType(Type);
            public bool Locked => Scene != null && !LocationUnlocks.IsUnlocked(Scene);
        }

        public static string SceneOfType(string type)
        {
            switch (type)
            {
                case "STATION": return WorldLocationCatalog.StationScene;
                case "SUSHI": return WorldLocationCatalog.SushiRestaurantScene;
                case "ARCADE": return WorldLocationCatalog.GameCenterScene;
                case "SCHOOL": return WorldLocationCatalog.SchoolScene;
                case "HOME": return WorldLocationCatalog.HomeBedroomScene;
                case "CLINIC": return WorldLocationCatalog.ClinicScene;
                case "MUSIC": return WorldLocationCatalog.MusicShopScene;
                default: return null;
            }
        }

        private const float MapW = 960f, MapH = 560f, Inset = 40f;

        private GameObject _overlay;
        private RectTransform _mapArea, _playerMarker, _destinationMarker, _sidebarList;
        private RawImage _topDownImage;
        private Camera _topDownCamera;
        private RenderTexture _topDownTexture;
        private Button _areaTab, _overviewTab, _guideButton;
        private TextMeshProUGUI _sidebarTitle;
        private bool _showTopDown;
        private TextMeshProUGUI _header, _location, _coordinates;
        private Transform _player;
        private TMP_FontAsset _font;
        private Vector2 _worldMin, _worldMax;
        /// <summary>Real metres per world unit on this map (Phú Quốc is built at 1 unit = 100 m).</summary>
        private float _metresPerUnit = 1f;
        private readonly List<MapPlace> _places = new List<MapPlace>();
        private readonly List<Rect> _labelRects = new List<Rect>();
        private readonly List<(MapPlace place, TextMeshProUGUI distance)> _rows = new List<(MapPlace, TextMeshProUGUI)>();
        private CanvasGroup _group;
        private RectTransform _window, _legend;
        private Coroutine _anim;
        private bool _closing, _isCity;
        public bool IsVisible => _overlay != null && _overlay.activeSelf && !_closing;
        /// <summary>True while the open/close animation is running (tests, captures).</summary>
        public bool IsAnimating => _anim != null;
        public float WindowAlpha => _group != null ? _group.alpha : 0f;
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

            _group = _overlay.AddComponent<CanvasGroup>();
            var window = NLUi.Panel(_overlay.transform, "MapWindow", NLUi.Ink);
            _window = window;
            NLUi.Anchor(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 800f));

            _header = NLUi.Label(window, "Header", "", 30f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Anchor(_header.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -26f), new Vector2(700f, 44f));
            _location = NLUi.Label(window, "Location", "", 18f, NLUi.Muted, _font);
            NLUi.Anchor(_location.rectTransform, new Vector2(0f, 1f), new Vector2(38f, -70f), new Vector2(700f, 28f));

            var guide = NLUi.Button(window, "GuideButton", GuideLabel(), _font, () => LocationGuideUI.Open(), NLUi.Gold, 17f, NLUi.Ink, 44f);
            _guideButton = guide;
            NLUi.Anchor((RectTransform)guide.transform, new Vector2(1f, 0f), new Vector2(-30f, 22f), new Vector2(230f, 44f));
            ((RectTransform)guide.transform).pivot = new Vector2(1f, 0f);

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
            MapPremium.PolishWindow(window, mapFrame);

            BuildTabs(window);

            var sidebar = NLUi.Panel(window, "Places", NLUi.Card, new RectOffset(18, 18, 16, 16), 8f);
            NLUi.Anchor(sidebar, new Vector2(1f, 1f), new Vector2(-30f, -112f), new Vector2(362f, MapH + 12f));
            var layout = sidebar.GetComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            _sidebarTitle = NLUi.Label(sidebar, "Title", SidebarTitle(), 21f, NLUi.Text, _font, FontStyles.Bold);
            // Many places (Phú Quốc has 31): the list scrolls inside the panel.
            var listRoot = new GameObject("ListScroll", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            listRoot.SetParent(sidebar, false);
            var listSize = listRoot.GetComponent<LayoutElement>();
            listSize.flexibleHeight = 1f; listSize.preferredHeight = MapH - 60f;
            _sidebarList = UiKit.Scroll(listRoot, "List", out var listScroll, 6f, new RectOffset(0, 10, 0, 4));
            NLUi.Stretch((RectTransform)listScroll.transform);

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

        private static string GuideLabel() => NihongoLife.Core.Loc.T("Sổ tay · địa điểm", "Handbook · places", "ハンドブック・場所");

        private static string SidebarTitle() => "ちめい  <size=75%><color=#A8B4C4>" + NihongoLife.Core.Loc.T("Địa điểm · bấm để đi tới", "Places · click to travel", "場所・クリックで移動") + "</color></size>";

        private void HandleGlobalLanguageChanged(GameLanguage _)
        {
            if (_guideButton != null) _guideButton.GetComponentInChildren<TextMeshProUGUI>().text = GuideLabel();
            if (_sidebarTitle != null) _sidebarTitle.text = SidebarTitle();
            if (_areaTab != null) _areaTab.GetComponentInChildren<TextMeshProUGUI>().text = L("Khu vực hiện tại", "Current area", "現在のエリア");
            if (_overviewTab != null) _overviewTab.GetComponentInChildren<TextMeshProUGUI>().text = L("Bản đồ tổng", "Overview map", "全体マップ");
            if (IsVisible) { BuildMap(); RefreshMarker(); }
        }

        public void SetVisible(bool visible)
        {
            if (_overlay == null) return;
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
            bool animate = isActiveAndEnabled;
            if (visible)
            {
                _closing = false;
                _group.blocksRaycasts = true;
                _group.interactable = true;
                _overlay.SetActive(true);
                _overlay.transform.SetAsLastSibling();
            }
            else
            {
                bool wasOpen = _overlay.activeSelf && !_closing;
                _closing = wasOpen && animate;
                if (!_closing) _overlay.SetActive(false);
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }
            OnVisibilityChanged?.Invoke(visible);
            if (!visible)
            {
                if (_topDownCamera != null) _topDownCamera.enabled = false;
                if (_closing) _anim = StartCoroutine(Animate(false));
                return;
            }
            GameObject found = GameObject.FindWithTag("Player");
            _player = found != null ? found.transform : null;
            BuildMap(); RefreshMarker();
            UpdateTopDownCamera();
            if (animate) _anim = StartCoroutine(Animate(true)); else Apply(1f);
        }

        private void Apply(float k)
        {
            _group.alpha = k;
            if (_window != null) _window.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, k);
        }

        private IEnumerator Animate(bool show)
        {
            float duration = show ? 0.22f : 0.15f;
            float t = 0f;
            Apply(show ? 0f : 1f);
            while (t < duration)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                Apply(show ? 1f - Mathf.Pow(1f - k, 3f) : 1f - k * k);
            }
            Apply(show ? 1f : 0f);
            if (!show) { _overlay.SetActive(false); _closing = false; }
            _anim = null;
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
                if (child.name == "Route" || child.name == "Grid" || child.name.StartsWith("Place_") || child.name.StartsWith("District_")) child.gameObject.SetActive(!topDown);
            if (_legend != null) _legend.gameObject.SetActive(!topDown);
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
            _metresPerUnit = 1f;
            _isCity = false;
            if (scene == WorldLocationCatalog.StationScene) StationMap();
            else if (scene == WorldLocationCatalog.SushiRestaurantScene) SushiMap();
            else if (scene == WorldLocationCatalog.SchoolScene) SchoolMap();
            else if (scene == WorldLocationCatalog.HomeBedroomScene) BedroomMap();
            else if (scene == WorldLocationCatalog.GameCenterScene) GameCenterMap();
            else if (scene == WorldLocationCatalog.ClinicScene) ClinicMap();
            else if (scene == WorldLocationCatalog.MusicShopScene) MusicShopMap();
            else if (scene == WorldLocationCatalog.PhuQuocIslandScene) PhuQuocIslandMap();
            else CityMap();
            DrawGrid();
            DrawDistricts();
            _labelRects.Clear();
            foreach (var place in _places) DrawPlace(place);
            BuildSidebar();
            DrawPlayerMarker();
            DrawDecor();
            SetMapMode(_showTopDown);
        }

        // ─────────── Map definitions ───────────

        private void CityMap()
        {
            _isCity = true;
            _header.text = L("BẢN ĐỒ HIBARI-CHŌ", "HIBARI-CHŌ MAP", "ひばり町 地図");
            // Streets as CityStreetBuilder lays them: Sakura-dōri (with its sidewalks), the side lane north–south,
            // the back alley west behind the north row, and the park behind it.
            _worldMin = new(-50, -36); _worldMax = new(58, 48);
            Road(MapPos(4f, -10f), MapSize(108f, 14f));
            Road(MapPos(3.5f, -26f), MapSize(5.6f, 20f));
            Road(MapPos(3.5f, 29f), MapSize(5.6f, 37f));
            Road(MapPos(-24.6f, 14.4f), MapSize(51f, 3.2f));
            Road(MapPos(-46f, 21f), MapSize(2f, 10f));
            AddWorld(L("Siêu thị Hibari Mart", "Hibari Mart", "ひばりマート"), "KONBINI", "店", 0f, 4f, new(.2f, .76f, .66f), "ひばりマート");
            AddWorld(L("Bảng tin khu phố", "Notice board", "けいじばん"), "PLAZA", "掲", -8f, 1.5f, new(.85f, .7f, .4f), "けいじばん");
            AddWorld(L("Sushi Hibari", "Sushi Hibari", "ひばり寿司"), "SUSHI", "寿", -18f, 0.3f, new(.94f, .42f, .36f), "すし");
            AddWorld(L("Nhà trọ Hibari Heights", "Hibari Heights (home)", "ひばりハイツ"), "HOME", "家", -30f, 1.3f, new(.62f, .76f, .38f), "いえ");
            AddWorld(L("Ga Hibari", "Hibari Station", "ひばり駅"), "STATION", "駅", 24f, 0.3f, new(.3f, .62f, .94f), "えき");
            AddWorld(L("Trường Nhật ngữ Hibari", "Hibari Japanese School", "ひばり日本語学院"), "SCHOOL", "学", 18f, -20f, new(.2f, .55f, .32f), "がっこう");
            AddWorld(L("Công viên Hibari", "Hibari Park", "ひばり公園"), "PARK", "園", -40f, 40f, new(.42f, .72f, .4f), "こうえん");
            AddWorld(L("Game Center Hibari", "Game Center Hibari", "ゲームセンター"), "ARCADE", "遊", -6f, -20.5f, new(.92f, .36f, .72f), "ゲーセン");
            AddWorld(L("Phòng khám Hibari", "Hibari Clinic", "ひばりクリニック"), "CLINIC", "医", -18f, -20.5f, new(.36f, .72f, .64f), "クリニック");
            AddWorld(L("Tiệm nhạc & tranh Hibiki", "Hibiki Music & Art", "ひびき楽器店"), "MUSIC", "音", -30f, -20.5f, new(.66f, .45f, .88f), "がっき");
        }

        /// <summary>
        /// Phú Quốc from the real map data (Resources/PhuQuoc/phu_quoc_map.json, OpenStreetMap): the coastline, the
        /// places coloured by kind (beaches, towns, markets, nature…), the player where they stand.
        /// </summary>
        private void PhuQuocIslandMap()
        {
            _header.text = L("BẢN ĐỒ ĐẢO PHÚ QUỐC", "PHÚ QUỐC ISLAND MAP", "フーコック島 地図");
            var map = NihongoLife.PhuQuoc.PhuQuocMap.Load();
            var rootObject = GameObject.Find("PhuQuocIsland");
            Vector3 origin = rootObject != null ? rootObject.transform.position : new Vector3(4000f, 0f, 0f);
            var outline = map.OutlineWorld();
            if (outline.Length < 3) { CityMap(); return; }
            _metresPerUnit = map.metersPerUnit;
            Vector2 min = outline[0], max = outline[0];
            foreach (var p in outline) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            // Keep the island's real proportions: widen the shorter side to the map's aspect.
            Vector2 centre = (min + max) * 0.5f, size = (max - min) * 1.08f;
            float aspect = (MapW - Inset) / (MapH - Inset);
            if (size.x / size.y < aspect) size.x = size.y * aspect; else size.y = size.x / aspect;
            _worldMin = new Vector2(origin.x, origin.z) + centre - size * 0.5f;
            _worldMax = new Vector2(origin.x, origin.z) + centre + size * 0.5f;

            var sea = NLUi.Panel(_mapArea, "Sea", new Color(0.12f, 0.36f, 0.5f, 1f));
            sea.GetComponent<Image>().raycastTarget = false;
            NLUi.Anchor(sea, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MapW - Inset * 0.5f, MapH - Inset * 0.5f));
            var land = new GameObject("Coast", typeof(RectTransform), typeof(UiPolygon)).GetComponent<UiPolygon>();
            land.transform.SetParent(_mapArea, false);
            NLUi.Anchor(land.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            land.color = new Color(0.36f, 0.6f, 0.36f, 1f);
            land.raycastTarget = false;
            var points = new List<Vector2>();
            foreach (var p in outline) points.Add(MapPos(origin.x + p.x, origin.z + p.y));
            land.SetPoints(points);
            foreach (var place in map.places)
            {
                var world = origin + map.ToWorld(place);
                var (glyph, color) = PhuQuocStyle(place.category, place.id);
                AddWorld(place.Name, "PQ_" + place.category.ToUpperInvariant(), glyph, world.x, world.z, color, place.nameJa);
                // Label only the main places (towns, markets, the restaurant, ports and the airport); the rest are dots.
                _places[_places.Count - 1].Quiet = place.id != "ganh_ngon" && place.category != "town" && place.category != "market" && place.category != "transport";
            }
        }

        private static (string glyph, Color color) PhuQuocStyle(string category, string id)
        {
            if (id == "ganh_ngon") return ("食", new Color(0.96f, 0.6f, 0.2f));
            if (id.Contains("airport")) return ("空", new Color(0.3f, 0.62f, 0.94f));
            switch (category)
            {
                case "beach": return ("浜", new Color(0.98f, 0.86f, 0.52f));
                case "town": return ("町", new Color(0.94f, 0.42f, 0.36f));
                case "market": return ("市", new Color(0.94f, 0.68f, 0.25f));
                case "restaurant": return ("食", new Color(0.96f, 0.6f, 0.2f));
                case "transport": return ("港", new Color(0.3f, 0.62f, 0.94f));
                case "nature": return ("森", new Color(0.3f, 0.7f, 0.42f));
                case "farm": return ("農", new Color(0.62f, 0.76f, 0.38f));
                case "village": return ("村", new Color(0.72f, 0.62f, 0.5f));
                case "history": return ("史", new Color(0.75f, 0.35f, 0.55f));
                case "landmark": return ("名", new Color(0.85f, 0.7f, 0.4f));
                case "factory": return ("工", new Color(0.6f, 0.62f, 0.66f));
                case "entertainment": return ("遊", new Color(0.92f, 0.36f, 0.72f));
                case "island": return ("島", new Color(0.36f, 0.72f, 0.64f));
                default: return ("・", NLUi.Muted);
            }
        }

        private void ClinicMap()
        {
            _header.text = L("SƠ ĐỒ PHÒNG KHÁM HIBARI", "HIBARI CLINIC FLOOR MAP", "ひばりクリニック 案内図");
            _worldMin = new(640, -8); _worldMax = new(660, 8);
            Road(Vector2.zero, new(MapW - 120, MapH - 80));
            AddPixel(L("Quầy tiếp nhận (受付)", "Reception (受付)", "受付"), "RECEPTION", "受", new(-140, -60), new(.36f, .72f, .64f));
            AddPixel(L("Nhận thuốc (お薬)", "Medicine (お薬)", "お薬"), "MEDICINE", "薬", new(90, -60), new(.3f, .62f, .94f));
            AddPixel(L("Phòng khám bác sĩ (診察室)", "Consultation room (診察室)", "診察室"), "DOCTOR", "診", new(-260, 140), new(.94f, .68f, .25f));
            AddPixel(L("Khám sức khoẻ · giường nghỉ", "Health check · recovery beds", "健康診断"), "CHECK", "健", new(260, 140), new(.45f, .78f, .48f));
            AddPixel(L("Phòng chờ", "Waiting room", "待合室"), "WAIT", "待", new(260, -140), new(.62f, .76f, .38f));
            AddPixel(L("Về thành phố", "Return to city", "出口"), "EXIT", "出", new(0, -200), new(.45f, .78f, .48f));
        }

        private void MusicShopMap()
        {
            _header.text = L("SƠ ĐỒ TIỆM NHẠC & PHÒNG TRANH HIBIKI", "HIBIKI MUSIC & ART FLOOR MAP", "ひびき楽器店 案内図");
            _worldMin = new(1288, -8); _worldMax = new(1312, 24);
            Road(Vector2.zero, new(MapW - 120, MapH - 80));
            AddPixel(L("Phòng tranh (39 kiệt tác)", "Gallery (39 masterpieces)", "ギャラリー"), "GALLERY", "画", new(0, 150), new(.86f, .66f, .3f));
            AddPixel(L("Sân khấu · bàn DJ", "Stage · DJ booth", "ステージ・DJ"), "STAGE", "舞", new(220, 20), new(.92f, .36f, .72f));
            AddPixel(L("Đàn piano", "Pianos", "ピアノ"), "PIANO", "鍵", new(-200, 0), new(.3f, .62f, .94f));
            AddPixel(L("Tường guitar", "Guitar wall", "ギター"), "GUITAR", "弦", new(-160, 60), new(.94f, .68f, .25f));
            AddPixel(L("Giá vẽ", "Easel", "アトリエ"), "EASEL", "絵", new(-280, -90), new(.45f, .78f, .48f));
            AddPixel(L("Sách nhạc lý", "Theory books", "音楽理論"), "THEORY", "譜", new(280, -60), new(.62f, .76f, .38f));
            AddPixel(L("Quầy · chú Hibiki", "Counter · Mr Hibiki", "おかいけい"), "COUNTER", "会", new(140, -150), new(.66f, .45f, .88f));
            AddPixel(L("Về thành phố", "Return to city", "出口"), "EXIT", "出", new(0, -200), new(.45f, .78f, .48f));
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
            AddPixel(L("Sân ga số 2 · đi Sân bay (qua Midori)", "Platform 2 · to the Airport (via Midori)", "2番線 空港行き"), "PLATFORM", "線", new(190, -70), new(.3f, .62f, .94f));
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

        private void DrawDistricts()
        {
            if (_mapArea == null) return;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == WorldLocationCatalog.PhuQuocIslandScene && !_isCity)
            {
                MapPremium.SeaLabel(_mapArea, _font, MapPremium.N4("Vịnh Thái Lan", "Gulf of Thailand", "タイ湾", "Тайский залив"), new Vector2(-MapW * 0.36f, -MapH * 0.12f));
                return;
            }
            if (!_isCity) return;
            int index = 1;
            void Zone(float x0, float z0, float x1, float z1, string vi, string en, string ja, string ru, Color tint)
            {
                Vector2 a = MapPos(x0, z0), b = MapPos(x1, z1);
                MapPremium.District(_mapArea, _font, MapPremium.N4(vi, en, ja, ru), (a + b) * 0.5f, new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)), tint, index++);
            }
            Zone(-50f, -17f, 58f, -3f, "Phố Sakura", "Sakura-dori", "桜通り", "Улица Сакура", new Color(.2f, .76f, .66f));
            Zone(-36f, -2f, -14f, 9f, "Khu dân cư", "Residential", "住宅街", "Жилой квартал", new Color(.62f, .76f, .38f));
            Zone(-12f, -2f, 12f, 9f, "Trung tâm phố", "Town centre", "商店街", "Центр", new Color(.85f, .7f, .4f));
            Zone(14f, -2f, 36f, 9f, "Khu ga", "Station quarter", "駅前", "Район вокзала", new Color(.3f, .62f, .94f));
            Zone(-22f, -27f, 28f, -18f, "Học đường & dịch vụ", "Campus & services", "学園エリア", "Кампус и сервисы", new Color(.75f, .45f, .9f));
            Zone(-50f, 30f, -26f, 48f, "Công viên", "Park", "公園エリア", "Парк", new Color(.42f, .72f, .4f));
        }

        private void DrawDecor()
        {
            MapPremium.Compass(_mapArea, _font, MapW, MapH);
            if (_places.Exists(x => x.HasWorld))
                MapPremium.ScaleBar(_mapArea, _font, MapW, MapH, _metresPerUnit * (_worldMax.x - _worldMin.x) / (MapW - Inset));
            var items = new List<(string key, string name, Color color, string glyph)>();
            items.Add(("YOU", MapPremium.YouLabel, NLUi.Gold, ""));
            if (_places.Exists(x => x.Locked)) items.Add(("LOCKED", MapPremium.LockedLabel, new Color(0.42f, 0.45f, 0.5f), "x"));
            var seen = new HashSet<string>();
            foreach (var place in _places)
                if (items.Count < 14 && MapPremium.HasCategory(place.Type) && seen.Add(place.Type))
                    items.Add((place.Type, MapPremium.CategoryName(place.Type), place.Color, place.Glyph));
            _legend = MapPremium.Legend(_window, _font, items);
            _legend.gameObject.SetActive(!_showTopDown);
        }

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
            var edge = road.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.1f, 0.14f, 0.16f, 0.9f);
            edge.effectDistance = new Vector2(2f, 2f);
            NLUi.Anchor(road, new Vector2(0.5f, 0.5f), p, s);
            road.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private void DrawPlace(MapPlace place)
        {
            bool locked = place.Locked;
            var marker = NLUi.Panel(_mapArea, "Place_" + place.Type, locked ? new Color(0.42f, 0.45f, 0.5f) : place.Color);
            float size = place.Quiet ? 26f : 46f;
            NLUi.Anchor(marker, new Vector2(0.5f, 0.5f), place.Pixel, new Vector2(size, size));
            marker.GetComponent<Image>().raycastTarget = false;
            var drop = marker.gameObject.AddComponent<Shadow>();
            drop.effectColor = new Color(0f, 0f, 0f, 0.5f);
            drop.effectDistance = new Vector2(0f, -3f);
            NLUi.Stretch(NLUi.Label(marker, "Glyph", place.Glyph, size * 0.56f, new Color(0.08f, 0.1f, 0.12f), _font, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform);
            if (place.Quiet) return;
            string text = string.IsNullOrEmpty(place.Short) ? place.Name : place.Short;
            if (locked) text += NihongoLife.Core.Loc.T(" · khóa", " · locked", " · 未開放");
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
                string rowName = place.Locked
                    ? $"{place.Name}\n<size=80%><color=#FF9A80>" + LocationUnlocks.ShortStatus(place.Scene) + "</color></size>"
                    : place.Name;
                var name = NLUi.Label(row, "Name", rowName, 17f, place.Locked ? NLUi.Muted : NLUi.Text, _font);
                NLUi.Size(name, flexibleWidth: 1f);
                var distance = NLUi.Label(row, "Distance", "", 15f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Right);
                distance.textWrappingMode = TextWrappingModes.NoWrap;
                NLUi.Size(distance, preferredWidth: 58f);
                // "?" opens this place in the guide (photo, what it is, how to use it, phrases).
                var info = NLUi.Button(row, "Guide_" + place.Type, "?", _font, () => LocationGuideUI.Open(p.Type), NLUi.Card, 16f, NLUi.Gold, 30f);
                NLUi.Size(info, 34f, 30f, 0f);
                if (place.HasWorld)
                {
                    var button = row.gameObject.AddComponent<Button>();
                    button.targetGraphic = row.GetComponent<Image>();
                    button.onClick.AddListener(() =>
                    {
                        WaypointGuide.Show(p.Name, new Vector3(p.World.x, 0f, p.World.y));
                        WalkTo(new Vector3(p.World.x, _player != null ? _player.position.y : 0f, p.World.y), p.Pixel);
                    });
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
            var ring = _playerMarker.Find("Ring");
            if (ring != null)
            {
                float k = (Mathf.Sin(Time.unscaledTime * 3.2f) + 1f) * 0.5f;
                ring.localScale = Vector3.one * (1f + 0.4f * k);
                ring.GetComponent<Image>().color = new Color(1f, 0.82f, 0.2f, 0.4f - 0.3f * k);
            }
            foreach (var (place, distance) in _rows)
            {
                float metres = place.HasWorld ? Vector2.Distance(p, place.World) * _metresPerUnit : 0f;
                distance.text = !place.HasWorld ? string.Empty : metres >= 1000f ? $"{metres / 1000f:0.0}km" : $"{metres:0}m";
            }
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
            WaypointGuide.Show(L("Điểm đã chọn", "Picked spot", "目的地"), destination);
            WalkTo(destination, localPosition);
        }

        /// <summary>Guide (and walk) to a listed place by its marker type, e.g. "ARCADE" (tests, quests).</summary>
        public bool GuideTo(string type)
        {
            var place = _places.Find(x => x.Type == type);
            if (place == null || !place.HasWorld) return false;
            WaypointGuide.Show(place.Name, new Vector3(place.World.x, 0f, place.World.y));
            return true;
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
