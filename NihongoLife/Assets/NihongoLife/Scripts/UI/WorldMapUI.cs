using System;
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
    public class WorldMapUI : MonoBehaviour
    {
        private GameObject _overlay;
        private RectTransform _mapArea, _playerMarker, _destinationMarker;
        private RawImage _topDownImage;
        private Camera _topDownCamera;
        private RenderTexture _topDownTexture;
        private Button _areaTab, _overviewTab;
        private bool _showTopDown;
        private TextMeshProUGUI _header, _location, _coordinates;
        private Transform _player;
        private TMP_FontAsset _font;
        private Vector2 _worldMin, _worldMax;
        public bool IsVisible => _overlay != null && _overlay.activeSelf;
        public event Action<bool> OnVisibilityChanged;

        public void Initialize(TMP_FontAsset font)
        {
            if (_overlay != null) return;
            _font = font;
            if (GameServices.TryGet(out GameSettingsService languageSettings)) languageSettings.OnLanguageChanged += HandleGlobalLanguageChanged;
            _overlay = Panel("WorldMap", transform, new Color(.012f, .018f, .022f, .98f));
            Stretch(_overlay.GetComponent<RectTransform>());
            _header = Text("Header", _overlay.transform, 27, FontStyles.Bold);
            Rect(_header.rectTransform, new(.5f, 1), new(0, -24), new(820, 42));
            _header.alignment = TextAlignmentOptions.Center;
            _header.color = new(.96f, .79f, .30f);
            _location = Text("Location", _overlay.transform, 15, FontStyles.Normal);
            Rect(_location.rectTransform, new(.5f, 1), new(0, -65), new(820, 28));
            _location.alignment = TextAlignmentOptions.Center;
            _location.color = new(.55f, .82f, .86f);
            GameObject close = Panel("Close", _overlay.transform, new(.16f, .19f, .20f));
            Rect(close.GetComponent<RectTransform>(), Vector2.one, new(-24, -22), new(44, 40));
            Button button = close.AddComponent<Button>();
            button.targetGraphic = close.GetComponent<Image>();
            button.onClick.AddListener(() => SetVisible(false));
            TextMeshProUGUI x = Text("Label", close.transform, 19, FontStyles.Bold, "X");
            Stretch(x.rectTransform); x.alignment = TextAlignmentOptions.Center;
            _mapArea = Panel("MapArea", _overlay.transform, new(.055f, .095f, .105f)).GetComponent<RectTransform>();
            Rect(_mapArea, new(.5f, .5f), new(0, -8), new(940, 530));
            var clickTarget = _mapArea.gameObject.AddComponent<MapClickTarget>();
            clickTarget.Clicked += HandleMapClick;
            BuildTabs();
            _coordinates = Text("Coordinates", _overlay.transform, 15, FontStyles.Normal);
            Rect(_coordinates.rectTransform, new(.5f, 0), new(0, 18), new(900, 32));
            _coordinates.alignment = TextAlignmentOptions.Center;
            _coordinates.color = new(.72f, .82f, .84f);
            _overlay.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameServices.TryGet(out GameSettingsService settings)) settings.OnLanguageChanged -= HandleGlobalLanguageChanged;
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
            _overlay.SetActive(visible); OnVisibilityChanged?.Invoke(visible);
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

        private void BuildTabs()
        {
            _areaTab = Tab("Khu vực hiện tại", new(-112, 0));
            _overviewTab = Tab("Bản đồ tổng", new(112, 0));
            _areaTab.onClick.AddListener(() => SetMapMode(true));
            _overviewTab.onClick.AddListener(() => SetMapMode(false));
            var topDownObject = new GameObject("TopDownMap", typeof(RectTransform), typeof(RawImage));
            topDownObject.transform.SetParent(_mapArea, false);
            _topDownImage = topDownObject.GetComponent<RawImage>();
            _topDownImage.color = Color.white;
            Stretch(_topDownImage.rectTransform);
            _topDownImage.raycastTarget = false;
            SetMapMode(false);
        }

        private Button Tab(string label, Vector2 position)
        {
            GameObject panel = Panel("MapTab_" + label, _overlay.transform, new(.08f, .12f, .15f));
            RectTransform rect = panel.GetComponent<RectTransform>();
            Rect(rect, new Vector2(.5f, 1f), position + new Vector2(0f, -103f), new Vector2(190f, 38f));
            Button button = panel.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            TextMeshProUGUI text = Text("Label", panel.transform, 14, FontStyles.Bold, label);
            Stretch(text.rectTransform); text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private void SetMapMode(bool topDown)
        {
            _showTopDown = topDown;
            _topDownImage.gameObject.SetActive(topDown);
            // Both map tabs are navigable. The top-down camera is only a visual layer;
            // the click target remains active so a click always produces a destination.
            _mapArea.GetComponent<MapClickTarget>().enabled = true;
            if (_areaTab != null) _areaTab.GetComponent<Image>().color = topDown ? new(.95f, .58f, .18f) : new(.08f, .12f, .15f);
            if (_overviewTab != null) _overviewTab.GetComponent<Image>().color = topDown ? new(.08f, .12f, .15f) : new(.95f, .58f, .18f);
            if (topDown) UpdateTopDownCamera();
        }

        private void UpdateTopDownCamera()
        {
            if (!_showTopDown || _player == null) return;
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            if (_topDownTexture == null)
            {
                _topDownTexture = new RenderTexture(1024, 576, 16, RenderTextureFormat.ARGB32);
                _topDownTexture.name = "NihongoLife_TopDownMap";
                _topDownTexture.Create();
            }
            if (_topDownCamera == null)
            {
                var go = new GameObject("TopDownMapCamera");
                _topDownCamera = go.AddComponent<Camera>();
                _topDownCamera.orthographic = true;
                _topDownCamera.orthographicSize = 16f;
                _topDownCamera.clearFlags = CameraClearFlags.SolidColor;
                _topDownCamera.backgroundColor = new Color(.035f, .055f, .065f, 1f);
                _topDownCamera.targetTexture = _topDownTexture;
                _topDownImage.texture = _topDownTexture;
            }
            _topDownCamera.transform.SetPositionAndRotation(_player.position + Vector3.up * 24f, Quaternion.Euler(90f, 0f, 0f));
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
            string scene = SceneManager.GetActiveScene().name;
            _location.text = WorldLocationCatalog.Get(scene).DisplayName;
            if (scene == WorldLocationCatalog.StationScene) StationMap();
            else if (scene == WorldLocationCatalog.SushiRestaurantScene) SushiMap();
            else if (scene == WorldLocationCatalog.ShoppingDistrictScene) ShoppingMap();
            else if (scene == WorldLocationCatalog.LearningCenterScene) LearningMap();
            else CityMap();
            _playerMarker = Panel("YouAreHere", _mapArea, new(1f, .82f, .16f)).GetComponent<RectTransform>();
            _playerMarker.sizeDelta = new(18, 24); _playerMarker.pivot = new(.5f, .25f);
            _destinationMarker = Panel("Destination", _mapArea, new(.95f, .25f, .28f)).GetComponent<RectTransform>();
            _destinationMarker.sizeDelta = new(18, 18);
            _destinationMarker.gameObject.SetActive(false);
            TextMeshProUGUI you = Text("Label", _playerMarker, 12, FontStyles.Bold, L("BẠN", "YOU", "現在地"));
            Rect(you.rectTransform, new(.5f, 0), new(0, -5), new(82, 24)); you.alignment = TextAlignmentOptions.Top;
        }

        private void CityMap()
        {
            _header.text = L("BẢN ĐỒ NIHONGO CITY", "NIHONGO CITY MAP", "日本語シティ地図");
            _worldMin = new(-66, -50); _worldMax = new(66, 38);
            Road(Vector2.zero, new(860, 60)); Road(new(-145, 0), new(56, 470)); Road(new(215, 35), new(50, 410), 18); Road(new(35, 135), new(720, 38), -8);
            Place(L("Cửa hàng tiện lợi", "Convenience store", "コンビニ"), new(0, -32), new(.2f, .76f, .66f), "SHOP");
            Place(L("Khu mua sắm Nihongo", "Nihongo Market", "ショッピング街"), new(160, -32), new(.95f, .48f, .18f), "MARKET");
            Place("Sushi Hibari", new(-285, 145), new(.94f, .42f, .36f), "SUSHI");
            Place(L("Ga Sakura Metro", "Sakura Metro", "さくら駅"), new(300, 135), new(.3f, .62f, .94f), "STATION");
            Place(L("Khu dân cư", "Residential", "住宅街"), new(-285, -160), new(.62f, .76f, .38f), "HOME");
            Place(L("Công viên", "Park", "公園"), new(285, -155), new(.42f, .72f, .4f), "PARK");
            Place(L("Trung tâm học tập", "Learning Center", "学習センター"), new(-120, 210), new(.18f, .55f, .92f), "LEARNING");
        }

        private void StationMap()
        {
            _header.text = L("SƠ ĐỒ GA SAKURA METRO", "SAKURA METRO DIRECTORY", "さくら駅構内図");
            _worldMin = new(742, -20); _worldMax = new(858, 16);
            Road(new(0, 10), new(850, 115)); Road(new(0, -105), new(850, 48));
            Place(L("Quầy vé", "Ticket counter", "きっぷ売り場"), new(-315, 165), new(.22f, .7f, .78f), "TICKETS");
            Place(L("Cổng soát vé", "Ticket gate", "改札"), new(-85, 85), new(.94f, .68f, .25f), "GATE");
            Place(L("Sân ga số 1", "Platform 1", "1番線"), new(185, -65), new(.3f, .62f, .94f), "MIDORI");
            Place(L("Lối ra thành phố", "City exit", "出口"), new(350, 165), new(.45f, .78f, .48f), "EXIT");
        }

        private void SushiMap()
        {
            _header.text = L("SƠ ĐỒ SUSHI HIBARI", "SUSHI HIBARI FLOOR MAP", "ひばり寿司 店内図");
            _worldMin = new(492, -9); _worldMax = new(508, 9);
            Road(Vector2.zero, new(720, 430));
            Place(L("Lễ tân", "Reception", "受付"), new(-275, -150), new(.22f, .7f, .78f), "MENU");
            Place(L("Quầy đầu bếp Ota", "Chef Ota counter", "太田職人"), new(0, 155), new(.94f, .48f, .3f), "ORDER");
            Place(L("Khu bàn ăn", "Dining area", "客席"), new(20, -15), new(.72f, .62f, .36f), "SEATS");
            Place(L("Về thành phố", "Return to city", "町へ戻る"), new(300, -165), new(.45f, .78f, .48f), "EXIT");
        }

        private void ShoppingMap()
        {
            _header.text = L("KHU MUA SẮM NIHONGO", "NIHONGO MARKET", "ショッピング街");
            _worldMin = new(982, -12); _worldMax = new(1018, 12);
            Road(Vector2.zero, new(780, 64));
            Road(new(-260, 0), new(48, 380));
            Road(new(260, 0), new(48, 380));
            Place(L("Cổng chợ", "Market entrance", "市場入口"), new(-300, -150), new(.95f, .48f, .18f), "ENTRANCE");
            Place(L("Quầy hàng", "Market stalls", "屋台"), new(-60, 15), new(.95f, .66f, .18f), "STALLS");
            Place(L("Phố đi bộ", "Pedestrian lane", "歩行者通り"), new(210, 120), new(.3f, .72f, .8f), "LANE");
            Place(L("Về thành phố", "Return to city", "町へ戻る"), new(300, -165), new(.45f, .78f, .48f), "EXIT");
        }

        private void LearningMap()
        {
            _header.text = L("TRUNG TÂM HỌC TẬP", "LEARNING CENTER", "学習センター");
            _worldMin = new(1188, -12); _worldMax = new(1212, 12);
            Road(Vector2.zero, new(760, 58));
            Place(L("Sảnh học tập", "Learning hub", "学習受付"), new(-280, -125), new(.18f, .55f, .92f), "HUB");
            Place("JLPT N5 / N4", new(-120, 100), new(.95f, .66f, .18f), "JLPT");
            Place("IELTS 4 Skills", new(120, 100), new(.18f, .58f, .9f), "IELTS");
            Place(L("Phòng thi thử", "Mock exam", "模擬試験"), new(0, -120), new(.75f, .3f, .9f), "EXAM");
            Place(L("Về thành phố", "Return to city", "町へ戻る"), new(300, -165), new(.45f, .78f, .48f), "EXIT");
        }

        private void LateUpdate() { if (IsVisible) { RefreshMarker(); UpdateTopDownCamera(); } }
        private void RefreshMarker()
        {
            if (_player == null || _playerMarker == null) return;
            Vector2 p = new(_player.position.x, _player.position.z);
            Vector2 n = new(Mathf.InverseLerp(_worldMin.x, _worldMax.x, p.x), Mathf.InverseLerp(_worldMin.y, _worldMax.y, p.y));
            _playerMarker.anchoredPosition = new((n.x - .5f) * (_mapArea.rect.width - 46), (n.y - .5f) * (_mapArea.rect.height - 46));
            _playerMarker.localRotation = Quaternion.Euler(0, 0, -_player.eulerAngles.y);
            _coordinates.text = $"{L("Vị trí", "Position", "位置")}  X {_player.position.x:0.0}  Z {_player.position.z:0.0}    |    M / Esc: {L("đóng", "close", "閉じる")}";
        }

        private void HandleMapClick(Vector2 localPosition)
        {
            if (_player == null || _mapArea == null) return;
            Vector2 normalized = new(
                Mathf.Clamp01(localPosition.x / _mapArea.rect.width + 0.5f),
                Mathf.Clamp01(localPosition.y / _mapArea.rect.height + 0.5f));
            Vector3 destination = new(
                Mathf.Lerp(_worldMin.x, _worldMax.x, normalized.x),
                _player.position.y,
                Mathf.Lerp(_worldMin.y, _worldMax.y, normalized.y));
            if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            {
                destination = navHit.position;
            }
            else if (Physics.Raycast(destination + Vector3.up * 30f, Vector3.down, out RaycastHit ground,
                60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                destination = ground.point;
            }
            else
            {
                return;
            }
            _destinationMarker.anchoredPosition = localPosition;
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

        private void Road(Vector2 p, Vector2 s, float angle = 0) { RectTransform r = Panel("Route", _mapArea, new(.22f, .27f, .29f)).GetComponent<RectTransform>(); r.anchoredPosition = p; r.sizeDelta = s; r.localRotation = Quaternion.Euler(0, 0, angle); }
        private void Place(string name, Vector2 p, Color color, string type) { RectTransform m = Panel("Place_" + type, _mapArea, color).GetComponent<RectTransform>(); m.anchoredPosition = p; m.sizeDelta = new(18, 18); TextMeshProUGUI t = Text("Label", m, 14, FontStyles.Bold, $"<size=75%><color=#AFC2C8>{type}</color></size>\n{name}"); Rect(t.rectTransform, new(.5f, 0), new(0, -7), new(190, 55)); t.alignment = TextAlignmentOptions.Top; }
        private string L(string vi, string en, string ja) => GameServices.TryGet(out GameSettingsService s) ? s.Text(vi, en, ja) : vi;
        private static GameObject Panel(string n, Transform p, Color c) { GameObject g = new(n, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); g.transform.SetParent(p, false); g.GetComponent<Image>().color = c; return g; }
        private TextMeshProUGUI Text(string n, Transform p, float size, FontStyles style, string value = "") { GameObject g = new(n, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); g.transform.SetParent(p, false); TextMeshProUGUI t = g.GetComponent<TextMeshProUGUI>(); if (_font != null) t.font = _font; t.text = value; t.fontSize = size; t.fontStyle = style; t.color = Color.white; t.characterSpacing = 0; t.enableAutoSizing = true; t.fontSizeMin = 11; t.raycastTarget = false; return t; }
        private static void Rect(RectTransform r, Vector2 a, Vector2 p, Vector2 s) { r.anchorMin = r.anchorMax = r.pivot = a; r.anchoredPosition = p; r.sizeDelta = s; }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    }
}
