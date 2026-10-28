using System;
using NihongoLife.Core;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The travel / loading screen (SceneFlowController): a picture of the place the player is heading to, slowly
    /// zooming; its name in the interface language and in Japanese; the player's own character riding the progress
    /// bar; and a Japanese phrase to learn while waiting (Resources/UI/loading_tips.json), with a game tip.
    /// Pictures: Resources/UI/Loading/&lt;scene&gt;; portraits: Resources/UI/Portraits/&lt;character id&gt; (LoadingArtCapture
    /// renders both). Everything is optional — a missing picture falls back to a calm gradient.
    /// </summary>
    public sealed class LoadingCard : MonoBehaviour
    {
        [Serializable] private sealed class Word { public string ja, romaji, vi, en; }
        [Serializable] private sealed class Tip { public string vi, en, ja; }
        [Serializable] private sealed class Tips { public Word[] words = Array.Empty<Word>(); public Tip[] tips = Array.Empty<Tip>(); }

        private static Tips _tips;
        private static int _wordIndex = -1, _tipIndex = -1;

        private RawImage _picture;
        private RectTransform _pictureRect, _track, _avatar;
        private RawImage _avatarImage;
        private TextMeshProUGUI _kicker, _japanese, _percent, _wordJa, _wordRomaji, _wordMeaning, _tip;
        private float _shownAt, _progress, _shown;
        private int _percentShown = -1;
        private CanvasGroup _group;

        public TextMeshProUGUI PlaceName { get; private set; }
        public TextMeshProUGUI Status { get; private set; }
        public Image Fill { get; private set; }

        public void Build(TMP_FontAsset font)
        {
            var root = (RectTransform)transform;
            var back = Img(root, "Background", new Color(0.03f, 0.045f, 0.06f, 1f));
            Stretch(back.rectTransform);
            _picture = new GameObject("Picture", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _picture.transform.SetParent(root, false);
            _pictureRect = _picture.rectTransform;
            Stretch(_pictureRect);
            _picture.raycastTarget = false;
            var shade = new GameObject("Shade", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            shade.transform.SetParent(root, false);
            Stretch(shade.rectTransform);
            shade.texture = Gradient();
            shade.raycastTarget = false;

            var brand = Text(root, "Brand", "NIHONGO LIFE  <color=#F2C14E>日本語ライフ</color>", font, 20f, new Color(1f, 1f, 1f, 0.8f), FontStyles.Bold);
            Pin(brand.rectTransform, new Vector2(0f, 1f), new Vector2(64f, -48f), new Vector2(700f, 40f));
            brand.characterSpacing = 6f;

            _kicker = Text(root, "Kicker", "", font, 22f, new Color(0.95f, 0.76f, 0.3f, 1f), FontStyles.Bold);
            Pin(_kicker.rectTransform, Vector2.zero, new Vector2(64f, 330f), new Vector2(1000f, 36f));
            _kicker.characterSpacing = 4f;
            PlaceName = Text(root, "PlaceName", "", font, 72f, Color.white, FontStyles.Bold);
            Pin(PlaceName.rectTransform, Vector2.zero, new Vector2(64f, 240f), new Vector2(1100f, 96f));
            PlaceName.enableAutoSizing = true; PlaceName.fontSizeMin = 30f; PlaceName.fontSizeMax = 72f;
            _japanese = Text(root, "Japanese", "", font, 32f, new Color(0.86f, 0.9f, 0.95f, 0.9f), FontStyles.Normal);
            Pin(_japanese.rectTransform, Vector2.zero, new Vector2(64f, 190f), new Vector2(1000f, 46f));

            // Progress: a full-width track, the player's portrait riding its end.
            _track = Img(root, "ProgressTrack", new Color(1f, 1f, 1f, 0.14f)).rectTransform;
            _track.anchorMin = new Vector2(0f, 0f); _track.anchorMax = new Vector2(1f, 0f); _track.pivot = new Vector2(0.5f, 0.5f);
            _track.offsetMin = new Vector2(64f, 124f); _track.offsetMax = new Vector2(-64f, 132f);
            Fill = Img(_track, "Fill", new Color(0.96f, 0.72f, 0.24f, 1f));
            Stretch(Fill.rectTransform);
            Fill.type = Image.Type.Filled; Fill.fillMethod = Image.FillMethod.Horizontal; Fill.fillOrigin = 0;
            Fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            _avatar = Img(_track, "Avatar", new Color(0.96f, 0.72f, 0.24f, 1f)).rectTransform;
            _avatar.GetComponent<Image>().sprite = UIStyleKit.RoundedSprite();
            _avatar.GetComponent<Image>().type = Image.Type.Sliced;
            _avatar.anchorMin = _avatar.anchorMax = new Vector2(0f, 0.5f);
            _avatar.sizeDelta = new Vector2(92f, 92f);
            var mask = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
            mask.SetParent(_avatar, false);
            Stretch(mask); mask.offsetMin = new Vector2(4f, 4f); mask.offsetMax = new Vector2(-4f, -4f);
            mask.GetComponent<Image>().sprite = UIStyleKit.RoundedSprite();
            mask.GetComponent<Image>().type = Image.Type.Sliced;
            mask.GetComponent<Mask>().showMaskGraphic = true;
            mask.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f, 1f);
            _avatarImage = new GameObject("Portrait", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _avatarImage.transform.SetParent(mask, false);
            Stretch(_avatarImage.rectTransform);
            _avatarImage.raycastTarget = false;

            Status = Text(root, "Status", "", font, 20f, new Color(0.8f, 0.85f, 0.9f, 1f), FontStyles.Normal);
            Pin(Status.rectTransform, Vector2.zero, new Vector2(64f, 70f), new Vector2(900f, 34f));
            _percent = Text(root, "Percent", "", font, 22f, new Color(0.96f, 0.8f, 0.4f, 1f), FontStyles.Bold, TextAlignmentOptions.Right);
            Pin(_percent.rectTransform, new Vector2(1f, 0f), new Vector2(-64f, 70f), new Vector2(200f, 34f));

            // "Learn while you wait": a Japanese phrase and a tip, on a glass card at the right.
            var card = Img(root, "WordCard", new Color(0.04f, 0.06f, 0.08f, 0.72f)).rectTransform;
            card.GetComponent<Image>().sprite = UIStyleKit.RoundedSprite();
            card.GetComponent<Image>().type = Image.Type.Sliced;
            Pin(card, new Vector2(1f, 0f), new Vector2(-64f, 190f), new Vector2(620f, 250f));
            var cardTitle = Text(card, "Title", "", font, 18f, new Color(0.95f, 0.76f, 0.3f, 1f), FontStyles.Bold);
            Pin(cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(560f, 28f));
            LocText.Set(cardTitle, "HỌC TRONG LÚC CHỜ · 今日のことば", "LEARN WHILE YOU WAIT · 今日のことば", "待つあいだに · 今日のことば");
            _wordJa = Text(card, "Japanese", "", font, 44f, Color.white, FontStyles.Bold);
            Pin(_wordJa.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -56f), new Vector2(570f, 60f));
            _wordJa.enableAutoSizing = true; _wordJa.fontSizeMin = 24f; _wordJa.fontSizeMax = 44f;
            _wordRomaji = Text(card, "Romaji", "", font, 19f, new Color(0.75f, 0.82f, 0.9f, 1f), FontStyles.Italic);
            Pin(_wordRomaji.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -118f), new Vector2(570f, 28f));
            _wordMeaning = Text(card, "Meaning", "", font, 22f, new Color(1f, 0.92f, 0.72f, 1f), FontStyles.Normal);
            Pin(_wordMeaning.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -148f), new Vector2(570f, 32f));
            _tip = Text(card, "Tip", "", font, 15.5f, new Color(0.72f, 0.78f, 0.85f, 1f), FontStyles.Normal);
            Pin(_tip.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -186f), new Vector2(570f, 56f));
            _tip.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>A new trip: the destination's picture and names, the player's portrait, the next phrase.</summary>
        public void Show(string sceneName, string displayName)
        {
            _shownAt = Time.unscaledTime;
            _shown = 0f;
            _progress = 0f;
            PlaceName.text = displayName ?? "";
            var texture = string.IsNullOrEmpty(sceneName) ? null : Resources.Load<Texture2D>("UI/Loading/" + sceneName);
            if (texture == null) texture = Resources.Load<Texture2D>("UI/Loading/default");
            _picture.texture = texture;
            _picture.color = texture != null ? Color.white : new Color(0f, 0f, 0f, 0f);
            Cover(texture);
            string japanese = WorldLocationCatalog.Has(sceneName) ? WorldLocationCatalog.Get(sceneName).JapaneseName : "";
            _japanese.text = japanese != displayName ? japanese : "";
            _kicker.text = Loc.T("ĐANG ĐẾN", "NEXT STOP", "つぎは");
            var portrait = Resources.Load<Texture2D>("UI/Portraits/" + PlayableCharacterCatalog.GetSelected().Id);
            _avatarImage.texture = portrait;
            _avatarImage.color = portrait != null ? Color.white : new Color(0f, 0f, 0f, 0f);
            NextWord();
        }

        public void SetProgress(float value) => _progress = Mathf.Clamp01(value);

        private void Update()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group != null && _group.alpha <= 0f) return; // hidden between trips
            float t = Time.unscaledTime - _shownAt;
            // A slow push-in on the picture (Ken Burns); still with Reduce motion.
            float zoom = MotionSettings.ReduceMotion ? 1.04f : 1.04f + Mathf.Min(t, 12f) * 0.006f;
            _pictureRect.localScale = new Vector3(zoom, zoom, 1f);
            _shown = Mathf.MoveTowards(_shown, _progress, Time.unscaledDeltaTime * (_progress >= 1f ? 5f : 1.6f)); // catch up once arrived
            Fill.fillAmount = _shown;
            int percent = Mathf.RoundToInt(_shown * 100f);
            if (percent != _percentShown) { _percentShown = percent; _percent.text = percent + "%"; }
            float width = _track.rect.width;
            float bob = MotionSettings.ReduceMotion ? 0f : Mathf.Abs(Mathf.Sin(t * 7f)) * 8f;
            _avatar.anchoredPosition = new Vector2(Mathf.Clamp(width * _shown, 46f, Mathf.Max(46f, width - 46f)), 52f + bob);
            if (t > 5f && Mathf.FloorToInt(t / 5f) != Mathf.FloorToInt((t - Time.unscaledDeltaTime) / 5f)) NextWord(); // a long load shows another phrase
        }

        private void NextWord()
        {
            _tips ??= Load();
            if (_tips.words.Length > 0)
            {
                _wordIndex = (_wordIndex + 1 + UnityEngine.Random.Range(0, Mathf.Max(1, _tips.words.Length - 1))) % _tips.words.Length;
                var w = _tips.words[_wordIndex];
                _wordJa.text = w.ja;
                _wordRomaji.text = w.romaji;
                _wordMeaning.text = Loc.T(w.vi, w.en, w.en);
            }
            if (_tips.tips.Length > 0)
            {
                _tipIndex = (_tipIndex + 1) % _tips.tips.Length;
                var tip = _tips.tips[_tipIndex];
                _tip.text = Loc.T(tip.vi, tip.en, tip.ja);
            }
        }

        private static Tips Load()
        {
            var text = Resources.Load<TextAsset>("UI/loading_tips");
            Tips tips = null;
            if (text != null) { try { tips = JsonUtility.FromJson<Tips>(text.text); } catch (ArgumentException) { tips = null; } }
            tips ??= new Tips();
            tips.words ??= Array.Empty<Word>();
            tips.tips ??= Array.Empty<Tip>();
            return tips;
        }

        /// <summary>Fills the screen with the picture without stretching it (crop the overflow).</summary>
        private void Cover(Texture texture)
        {
            if (texture == null) { _picture.uvRect = new Rect(0f, 0f, 1f, 1f); return; }
            float screen = Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9f;
            float image = texture.width / (float)Mathf.Max(1, texture.height);
            if (image > screen) { float w = screen / image; _picture.uvRect = new Rect((1f - w) * 0.5f, 0f, w, 1f); }
            else { float h = image / screen; _picture.uvRect = new Rect(0f, (1f - h) * 0.5f, 1f, h); }
        }

        private static Texture2D _gradient;

        /// <summary>Transparent at the top, dark at the bottom (the text sits on the dark part).</summary>
        private static Texture2D Gradient()
        {
            if (_gradient != null) return _gradient;
            _gradient = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "LoadingShade" };
            for (int y = 0; y < 64; y++)
            {
                float k = y / 63f; // 0 bottom … 1 top
                float a = Mathf.Lerp(0.94f, 0.12f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k * 1.5f)));
                _gradient.SetPixel(0, y, new Color(0.02f, 0.03f, 0.04f, a));
            }
            _gradient.Apply();
            return _gradient;
        }

        private static Image Img(Transform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, TMP_FontAsset font, float size, Color color, FontStyles style, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
