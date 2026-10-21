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
        [Serializable] public sealed class Place
        {
            public string type, ja, vi, en, about, aboutEn, aboutJa; public string[] how, howEn, howJa; public string[][] phrases; public string[] phrasesEn;

            /// <summary>Name, description, how-to lines and phrase meanings in the interface language.</summary>
            public string Name => NihongoLife.Core.Loc.Pick(vi, en, ja);
            public string About => NihongoLife.Core.Loc.Pick(about, aboutEn, aboutJa);
            public string[] How => NihongoLife.Core.Loc.IsJapanese && howJa?.Length > 0 ? howJa : NihongoLife.Core.Loc.IsEnglish && howEn?.Length > 0 ? howEn : how;
            public string PhraseMeaning(int i) => NihongoLife.Core.Loc.IsJapanese ? "" : NihongoLife.Core.Loc.IsEnglish && phrasesEn != null && i < phrasesEn.Length ? phrasesEn[i] : phrases[i][1];
        }

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
            var title = NLUi.Label(card, "Title", "", 28f, NLUi.Gold, _font, FontStyles.Bold);
            LocText.Set(title, () => NihongoLife.Core.Loc.T("Cẩm nang địa điểm", "Place guide", "ガイドブック") + "  <size=70%><color=#A8B4C4>" + NihongoLife.Core.Loc.T("bấm từng nơi để xem", "pick a place to read about it", "場所を選んでください") + "</color></size>");
            NihongoLife.Core.Loc.Watch(this, () => { if (IsOpen) Show(_type); });
            NLUi.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(1000f, 44f));
            title.rectTransform.pivot = new Vector2(0f, 1f);

            _tabs = NLUi.Group(card, "Places", true, 6f);
            NLUi.Anchor(_tabs, new Vector2(0f, 1f), new Vector2(28f, -80f), new Vector2(260f, 610f));
            _tabs.pivot = new Vector2(0f, 1f);
            foreach (var p in _places)
            {
                var place = p;
                var b = NLUi.Button(_tabs, "Tab_" + p.type, p.Name, _font, () => Show(place.type), NLUi.Card, 16f, null, 52f);
                var tabText = b.GetComponentInChildren<TextMeshProUGUI>();
                tabText.alignment = TextAlignmentOptions.MidlineLeft;
                LocText.Set(tabText, () => place.Name);
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
                var place = new Place { type = d.Str("type"), ja = d.Str("ja"), vi = d.Str("vi"), en = d.Str("en"), about = d.Str("about"), aboutEn = d.Str("aboutEn"), aboutJa = d.Str("aboutJa") };
                place.how = Strings(d, "how");
                place.howEn = Strings(d, "howEn");
                place.howJa = Strings(d, "howJa");
                place.phrasesEn = Strings(d, "phrasesEn");
                var phrases = new List<string[]>();
                foreach (var ph in d.Arr("phrases") ?? new List<object>())
                    if (ph is List<object> pair && pair.Count >= 2) phrases.Add(new[] { pair[0] as string, pair[1] as string });
                place.phrases = phrases.ToArray();
                _places.Add(place);
            }
        }

        private static string[] Strings(Dictionary<string, object> d, string key)
        {
            var list = new List<string>();
            foreach (var item in d.Arr(key) ?? new List<object>()) list.Add(item as string);
            return list.ToArray();
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
            string status = scene == null || !LocationUnlocks.IsGated(scene)
                ? "<color=#7BD88F>" + NihongoLife.Core.Loc.T("Luôn mở", "Always open", "いつでも行ける") + "</color>"
                : LocationUnlocks.IsUnlocked(scene) ? "<color=#7BD88F>" + NihongoLife.Core.Loc.T("Đã mở", "Unlocked", "解放済み") + "</color>"
                : "<color=#FF9A80>" + LocationUnlocks.ShortStatus(scene) + "</color>";
            NLUi.Label(_content, "Name", (NihongoLife.Core.Loc.IsJapanese ? $"<b>{place.ja}</b>" : $"<b>{place.ja}</b>  ·  {place.Name}") + $"   <size=70%>{status}</size>", 24f, NLUi.Text, _font);
            NLUi.Label(_content, "About", place.About, 17f, NLUi.Soft, _font);
            var how = new System.Text.StringBuilder("<b>" + NihongoLife.Core.Loc.T("Cách tương tác", "How to use it", "使い方") + "</b>");
            foreach (var h in place.How) how.Append("\n•  ").Append(h);
            NLUi.Label(_content, "How", how.ToString(), 16f, NLUi.Text, _font);
            var phrases = new System.Text.StringBuilder("<b>" + NihongoLife.Core.Loc.T("Câu hữu ích", "Useful phrases", "便利なフレーズ") + "</b>");
            for (int i = 0; i < place.phrases.Length; i++)
            {
                string meaning = place.PhraseMeaning(i);
                phrases.Append("\n").Append(place.phrases[i][0]);
                if (!string.IsNullOrEmpty(meaning)) phrases.Append("  <color=#A8B4C4>— ").Append(meaning).Append("</color>");
            }
            NLUi.Label(_content, "Phrases", phrases.ToString(), 16f, NLUi.Text, _font);

            // Beside the photo: a short summary column.
            var side = _photo.transform.parent.Find("Side") as RectTransform;
            if (side != null) Destroy(side.gameObject);
            side = NLUi.Group(_photo.transform.parent, "Side", true, 6f);
            side.anchorMin = side.anchorMax = side.pivot = new Vector2(0f, 1f);
            side.anchoredPosition = new Vector2(810f, -80f);
            side.sizeDelta = new Vector2(400f, 270f);
            NLUi.Label(side, "Big", place.ja, 40f, NLUi.Gold, _font, FontStyles.Bold);
            if (!NihongoLife.Core.Loc.IsJapanese) NLUi.Label(side, "Vi", place.Name, 20f, NLUi.Text, _font);
            NLUi.Label(side, "Status", status, 16f, NLUi.Text, _font);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
