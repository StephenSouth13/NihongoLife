using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Cẩm nang địa điểm — opened from the map. Each place has a photo taken in the game
    /// (Resources/Guide/{TYPE}.jpg, shot by GuidePhotosPlayModeTests), what it means in Japanese daily life, how to
    /// use it in the game, a few phrases and whether it is unlocked yet. Text lives in Resources/Guide/locations.json.
    /// </summary>
    public sealed class LocationGuideUI : MonoBehaviour
    {
        [Serializable] private sealed class GuideFile { public Place[] places; }
        [Serializable] public sealed class Place { public string type, ja, vi, about; public string[] how; public string[][] phrases; }

        private static LocationGuideUI _instance;
        public static bool IsOpen => _instance != null && _instance._window != null && _instance._window.gameObject.activeSelf;
        public static string ShownType => _instance != null ? _instance._type : null;

        private readonly List<Place> _places = new List<Place>();
        private TMP_FontAsset _font;
        private RectTransform _window, _tabs, _content;
        private RawImage _photo;
        private string _type;

        public static void Open(string type = null)
        {
            if (_instance == null)
            {
                var go = new GameObject("LocationGuide");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<LocationGuideUI>();
                _instance.Build();
            }
            _instance._window.gameObject.SetActive(true);
            _instance.Show(type ?? _instance._type ?? "KONBINI");
        }

        public static void Close()
        {
            if (_instance != null && _instance._window != null) _instance._window.gameObject.SetActive(false);
        }

        private void Build()
        {
            Load();
            _font = NLUi.ResolveFont();
            var canvas = NLUi.CreateCanvas("LocationGuideCanvas", 640, transform);
            var dim = NLUi.Panel(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            NLUi.Stretch(dim);
            _window = dim;
            var card = NLUi.Panel(dim, "Card", NLUi.Ink);
            NLUi.Anchor(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 720f));
            NLUi.CloseButton(card, _font, Close, 44f, 12f);
            var title = NLUi.Label(card, "Title", "Cẩm nang địa điểm  <size=70%><color=#A8B4C4>ガイドブック · bấm từng nơi để xem</color></size>", 28f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(1000f, 44f));
            title.rectTransform.pivot = new Vector2(0f, 1f);

            _tabs = NLUi.Group(card, "Places", true, 6f);
            NLUi.Anchor(_tabs, new Vector2(0f, 1f), new Vector2(28f, -80f), new Vector2(260f, 610f));
            _tabs.pivot = new Vector2(0f, 1f);
            foreach (var p in _places)
            {
                var place = p;
                var b = NLUi.Button(_tabs, "Tab_" + p.type, p.vi, _font, () => Show(place.type), NLUi.Card, 16f, null, 52f);
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            }

            var photoGo = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
            photoGo.transform.SetParent(card, false);
            _photo = photoGo.GetComponent<RawImage>();
            var photoRect = (RectTransform)photoGo.transform;
            photoRect.anchorMin = photoRect.anchorMax = photoRect.pivot = new Vector2(0f, 1f);
            photoRect.anchoredPosition = new Vector2(310f, -80f);
            photoRect.sizeDelta = new Vector2(480f, 270f);

            _content = NLUi.Group(card, "Content", true, 10f);
            _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0f, 1f);
            _content.anchoredPosition = new Vector2(310f, -364f);
            _content.sizeDelta = new Vector2(900f, 330f);

            UiModalStack.Register(this, () => IsOpen, Close, "LocationGuide");
        }

        private void Load()
        {
            var asset = Resources.Load<TextAsset>("Guide/locations");
            if (asset == null) return;
            // JsonUtility cannot read string[][]: read with MiniJson.
            var root = MiniJson.ParseObject(asset.text);
            foreach (var o in root.Arr("places") ?? new List<object>())
            {
                if (!(o is Dictionary<string, object> d)) continue;
                var place = new Place { type = d.Str("type"), ja = d.Str("ja"), vi = d.Str("vi"), about = d.Str("about") };
                var how = new List<string>();
                foreach (var h in d.Arr("how") ?? new List<object>()) how.Add(h as string);
                place.how = how.ToArray();
                var phrases = new List<string[]>();
                foreach (var ph in d.Arr("phrases") ?? new List<object>())
                    if (ph is List<object> pair && pair.Count >= 2) phrases.Add(new[] { pair[0] as string, pair[1] as string });
                place.phrases = phrases.ToArray();
                _places.Add(place);
            }
        }

        private void Show(string type)
        {
            var place = _places.Find(p => p.type == type) ?? (_places.Count > 0 ? _places[0] : null);
            if (place == null) return;
            _type = place.type;
            foreach (Transform t in _tabs)
            {
                bool on = t.name == "Tab_" + place.type;
                t.GetComponent<Image>().color = on ? NLUi.Gold : NLUi.Card;
                t.GetComponentInChildren<TextMeshProUGUI>().color = on ? NLUi.Ink : NLUi.Text;
            }
            var tex = Resources.Load<Texture2D>("Guide/" + place.type);
            _photo.texture = tex;
            _photo.color = tex != null ? Color.white : new Color(0.15f, 0.18f, 0.22f);

            for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);
            string scene = place.type == "MIDORI" ? WorldLocationCatalog.MidoriIslandScene : WorldMapUI.SceneOfType(place.type);
            string status = scene == null || LocationUnlocks.Required(scene) == 0
                ? "<color=#7BD88F>Luôn mở</color>"
                : LocationUnlocks.IsUnlocked(scene) ? "<color=#7BD88F>Đã mở</color>"
                : $"<color=#FF9A80>Đã khóa — cần {LocationUnlocks.Required(scene)} điểm kiến thức (thiếu {LocationUnlocks.Missing(scene)})</color>";
            NLUi.Label(_content, "Name", $"<b>{place.ja}</b>  ·  {place.vi}   <size=70%>{status}</size>", 24f, NLUi.Text, _font);
            NLUi.Label(_content, "About", place.about, 17f, NLUi.Soft, _font);
            var how = new System.Text.StringBuilder("<b>Cách tương tác</b>");
            foreach (var h in place.how) how.Append("\n•  ").Append(h);
            NLUi.Label(_content, "How", how.ToString(), 16f, NLUi.Text, _font);
            var phrases = new System.Text.StringBuilder("<b>Câu hữu ích</b>");
            foreach (var ph in place.phrases) phrases.Append("\n").Append(ph[0]).Append("  <color=#A8B4C4>— ").Append(ph[1]).Append("</color>");
            NLUi.Label(_content, "Phrases", phrases.ToString(), 16f, NLUi.Text, _font);

            // Beside the photo: a short summary column.
            var side = _photo.transform.parent.Find("Side") as RectTransform;
            if (side != null) Destroy(side.gameObject);
            side = NLUi.Group(_photo.transform.parent, "Side", true, 6f);
            side.anchorMin = side.anchorMax = side.pivot = new Vector2(0f, 1f);
            side.anchoredPosition = new Vector2(810f, -80f);
            side.sizeDelta = new Vector2(400f, 270f);
            NLUi.Label(side, "Big", place.ja, 40f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(side, "Vi", place.vi, 20f, NLUi.Text, _font);
            NLUi.Label(side, "Status", status, 16f, NLUi.Text, _font);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
