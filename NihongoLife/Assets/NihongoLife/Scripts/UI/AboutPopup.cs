using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Data;

namespace NihongoLife.UI
{
    /// <summary>
    /// "About me" popup: creator profile (name, role, bio, website button, credits) on the left and a
    /// screenshot gallery on the right (fade transitions, arrows, dots, autoplay, click for a full-size
    /// lightbox). Profile text comes from Resources/Menu/menu_about.asset; the gallery shows every
    /// PNG/JPG placed in Resources/MenuGallery, so adding a screenshot is just dropping a file in.
    /// </summary>
    public class AboutPopup : MenuPopupBase
    {
        private const string DefinitionPath = "Menu/menu_about";

        private class GalleryItem
        {
            public string name;
            public Sprite sprite;
        }

        private MenuAboutDefinition _definition;
        private readonly List<GalleryItem> _items = new List<GalleryItem>();
        private bool _galleryLoaded;
        private int _index;
        private float _nextAdvance;
        private Coroutine _fade;

        private TextMeshProUGUI _bioText;
        private TextMeshProUGUI _roleText;
        private TextMeshProUGUI _counterText;
        private TextMeshProUGUI _captionText;
        private TextMeshProUGUI _emptyText;
        private TextMeshProUGUI _websiteLabel;
        private TextMeshProUGUI _creditsText;
        private Image _shot;
        private CanvasGroup _shotGroup;
        private GameObject _frameRoot;
        private GameObject _arrows;
        private RectTransform _dotsRoot;
        private readonly List<Image> _dots = new List<Image>();

        private GameObject _lightbox;
        private Image _lightboxImage;
        private TextMeshProUGUI _lightboxCaption;

        protected override Vector2 CardSize => new Vector2(1180f, 680f);

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "Về tôi";
            en = "About me";
            ja = "作者について";
        }

        protected override void Build(RectTransform card)
        {
            _definition = Resources.Load<MenuAboutDefinition>(DefinitionPath);
            string displayName = _definition != null && !string.IsNullOrWhiteSpace(_definition.displayName) ? _definition.displayName : "quachthanhlong.com";
            string monogram = _definition != null && !string.IsNullOrWhiteSpace(_definition.avatarText) ? _definition.avatarText : "QL";

            // ── Left column: creator ──
            AddRounded(card, "CreatorCard", 36f, 100f, 404f, 552f, Surface);
            AddCircle(card, "AvatarRing", 56f, 120f, 96f, Gold);
            AddCircle(card, "Avatar", 60f, 124f, 88f, new Color(0.10f, 0.13f, 0.16f, 1f));
            AddText(card, monogram, 34f, 60f, 124f, 88f, 88f, TextAlignmentOptions.Center, FontStyles.Bold, false, Gold);

            AddText(card, displayName, 24f, 166f, 128f, 264f, 40f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);
            _roleText = AddText(card, string.Empty, 16f, 166f, 170f, 264f, 28f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false, Muted);

            _bioText = AddText(card, string.Empty, 16f, 58f, 240f, 360f, 190f, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);

            string url = _definition != null && !string.IsNullOrWhiteSpace(_definition.websiteUrl) ? _definition.websiteUrl : "https://quachthanhlong.com";
            var website = AddButton(card, string.Empty, 56f, 446f, 364f, 50f, true, 19f);
            _websiteLabel = website.GetComponentInChildren<TextMeshProUGUI>();
            website.onClick.AddListener(() => Application.OpenURL(url));

            float linkY = 506f;
            if (_definition != null && _definition.links != null)
            {
                int shown = 0;
                foreach (var link in _definition.links)
                {
                    if (link == null || string.IsNullOrWhiteSpace(link.url) || shown >= 2) continue;
                    string linkUrl = link.url;
                    var button = AddButton(card, string.IsNullOrWhiteSpace(link.label) ? linkUrl : link.label, 56f + shown * 186f, linkY, 178f, 38f, false, 15f);
                    button.onClick.AddListener(() => Application.OpenURL(linkUrl));
                    shown++;
                }
            }

            AddLocalizedText(card, "Nguồn và công nghệ", "Credits and tech", "クレジット", 13f, 58f, 552f, 360f, 20f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);
            _creditsText = AddText(card, string.Empty, 12f, 58f, 574f, 360f, 70f, TextAlignmentOptions.TopLeft, FontStyles.Normal, true, Muted);
            _creditsText.lineSpacing = 2f;

            // ── Right column: screenshot gallery ──
            AddLocalizedText(card, "Ảnh chụp màn hình", "Screenshots", "スクリーンショット", 22f, 464f, 100f, 400f, 34f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            _counterText = AddText(card, string.Empty, 17f, 1030f, 104f, 114f, 28f, TextAlignmentOptions.MidlineRight, FontStyles.Bold, false, Gold);

            var frame = AddRounded(card, "Frame", 464f, 144f, 680f, 382f, new Color(0.03f, 0.04f, 0.05f, 1f));
            _frameRoot = frame.gameObject;

            _shot = AddImage(card, "Shot", 468f, 148f, 672f, 374f, Color.white);
            _shot.preserveAspect = true;
            _shot.raycastTarget = true;
            _shotGroup = _shot.gameObject.AddComponent<CanvasGroup>();
            var shotButton = _shot.gameObject.AddComponent<Button>();
            shotButton.transition = Selectable.Transition.None;
            shotButton.targetGraphic = _shot;
            shotButton.onClick.AddListener(OpenLightbox);

            _emptyText = AddText(card, string.Empty, 40f, 468f, 148f, 672f, 374f, TextAlignmentOptions.Center, FontStyles.Bold, true, new Color(0.95f, 0.72f, 0.25f, 0.85f));

            _arrows = new GameObject("Arrows", typeof(RectTransform));
            _arrows.transform.SetParent(card, false);
            Place((RectTransform)_arrows.transform, 0f, 0f, CardSize.x, CardSize.y);
            var previous = AddButton(_arrows.transform, "<", 476f, 312f, 48f, 54f, false, 26f);
            previous.onClick.AddListener(() => Step(-1, true));
            var next = AddButton(_arrows.transform, ">", 1080f, 312f, 48f, 54f, false, 26f);
            next.onClick.AddListener(() => Step(1, true));

            _captionText = AddText(card, string.Empty, 18f, 464f, 540f, 680f, 44f, TextAlignmentOptions.Center, FontStyles.Normal, true);

            var dots = new GameObject("Dots", typeof(RectTransform));
            dots.transform.SetParent(card, false);
            _dotsRoot = (RectTransform)dots.transform;
            Place(_dotsRoot, 464f, 604f, 680f, 16f);

            BuildLightbox();
        }

        private void BuildLightbox()
        {
            _lightbox = new GameObject("Lightbox", typeof(RectTransform), typeof(Image), typeof(Button));
            _lightbox.transform.SetParent(PanelObject.transform, false);
            var rect = (RectTransform)_lightbox.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var background = _lightbox.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.94f);
            var button = _lightbox.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            button.onClick.AddListener(CloseLightbox);

            var imageGo = new GameObject("Image", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(_lightbox.transform, false);
            var imageRect = (RectTransform)imageGo.transform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(48f, 96f);
            imageRect.offsetMax = new Vector2(-48f, -48f);
            _lightboxImage = imageGo.GetComponent<Image>();
            _lightboxImage.preserveAspect = true;
            _lightboxImage.raycastTarget = false;

            var captionGo = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
            captionGo.transform.SetParent(_lightbox.transform, false);
            var captionRect = (RectTransform)captionGo.transform;
            captionRect.anchorMin = new Vector2(0f, 0f);
            captionRect.anchorMax = new Vector2(1f, 0f);
            captionRect.pivot = new Vector2(0.5f, 0f);
            captionRect.anchoredPosition = new Vector2(0f, 28f);
            captionRect.sizeDelta = new Vector2(-96f, 48f);
            _lightboxCaption = captionGo.GetComponent<TextMeshProUGUI>();
            if (Font != null) _lightboxCaption.font = Font;
            _lightboxCaption.fontSize = 22f;
            _lightboxCaption.alignment = TextAlignmentOptions.Center;
            _lightboxCaption.color = Color.white;
            _lightboxCaption.raycastTarget = false;

            _lightbox.SetActive(false);
        }

        // ──────────────────────── Lifecycle ────────────────────────

        protected override void OnOpened()
        {
            LoadGallery();
            if (_lightbox != null) _lightbox.SetActive(false);
            _index = 0;
            ShowItem(0, false);
            ScheduleAdvance();
        }

        protected override void OnLanguageApplied()
        {
            if (_roleText != null) _roleText.text = _definition != null ? Pick(_definition.roleVi, _definition.roleEn, _definition.roleJa) : "Creator of Nihongo Life";
            if (_bioText != null) _bioText.text = BioText();
            if (_websiteLabel != null) _websiteLabel.text = (_definition != null ? Pick(_definition.websiteButtonVi, _definition.websiteButtonEn, _definition.websiteButtonJa) : "Visit website");
            if (_creditsText != null) _creditsText.text = CreditsText();
            if (_emptyText != null) _emptyText.text = "日本語ライフ\n<size=42%><color=#A9B7C4>" + Pick("Ảnh chụp màn hình sẽ sớm có ở đây", "Screenshots are coming soon", "スクリーンショットは近日公開") + "</color></size>";
            if (_items.Count > 0) UpdateCaption();
        }

        protected override bool HandleEscape()
        {
            if (_lightbox != null && _lightbox.activeSelf)
            {
                CloseLightbox();
                return true;
            }

            return false;
        }

        protected override void OnOpenUpdate()
        {
            if (_items.Count < 2) return;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame) Step(-1, true);
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame) Step(1, true);
            }

            bool lightboxOpen = _lightbox != null && _lightbox.activeSelf;
            if (!lightboxOpen && _definition != null && _definition.autoAdvanceSeconds > 0f && Time.unscaledTime >= _nextAdvance)
            {
                Step(1, false);
            }
        }

        // ──────────────────────── Gallery ────────────────────────

        private void LoadGallery()
        {
            if (_galleryLoaded) return;
            _galleryLoaded = true;

            string folder = _definition != null && !string.IsNullOrWhiteSpace(_definition.galleryResourceFolder) ? _definition.galleryResourceFolder : "MenuGallery";
            Texture2D[] textures;
            try
            {
                textures = Resources.LoadAll<Texture2D>(folder);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return;
            }

            Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var texture in textures)
            {
                if (texture == null || texture.width < 8) continue;
                var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                _items.Add(new GalleryItem { name = texture.name, sprite = sprite });
            }

            BuildDots();
        }

        private void BuildDots()
        {
            foreach (var dot in _dots)
            {
                if (dot != null) Destroy(dot.gameObject);
            }

            _dots.Clear();
            if (_items.Count < 2 || _dotsRoot == null) return;

            float total = _items.Count * 22f;
            float start = (680f - total) * 0.5f;
            for (int i = 0; i < _items.Count; i++)
            {
                var dot = AddCircle(_dotsRoot, "Dot", start + i * 22f, 0f, 12f, new Color(0.3f, 0.35f, 0.4f, 1f));
                var button = dot.gameObject.AddComponent<Button>();
                dot.raycastTarget = true;
                button.transition = Selectable.Transition.None;
                button.targetGraphic = dot;
                int captured = i;
                button.onClick.AddListener(() => GoTo(captured, true));
                _dots.Add(dot);
            }
        }

        private void Step(int direction, bool manual)
        {
            if (_items.Count < 2) return;
            GoTo((_index + direction + _items.Count) % _items.Count, manual);
        }

        private void GoTo(int index, bool manual)
        {
            if (_items.Count == 0 || index == _index) return;
            _index = index;
            ShowItem(index, true);
            ScheduleAdvance(manual ? 1.6f : 0f);
        }

        private void ScheduleAdvance(float extraDelay = 0f)
        {
            float interval = _definition != null ? _definition.autoAdvanceSeconds : 5f;
            _nextAdvance = Time.unscaledTime + interval + extraDelay;
        }

        private void ShowItem(int index, bool animate)
        {
            bool has = _items.Count > 0;
            if (_emptyText != null) _emptyText.gameObject.SetActive(!has);
            if (_shot != null) _shot.gameObject.SetActive(has);
            if (_arrows != null) _arrows.SetActive(_items.Count > 1);
            if (_counterText != null) _counterText.text = has ? $"{index + 1} / {_items.Count}" : string.Empty;
            if (!has) return;

            if (_fade != null) StopCoroutine(_fade);
            if (animate && isActiveAndEnabled) _fade = StartCoroutine(FadeTo(index));
            else Apply(index);
        }

        private IEnumerator FadeTo(int index)
        {
            const float half = 0.12f;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                _shotGroup.alpha = 1f - t / half;
                yield return null;
            }

            Apply(index);
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                _shotGroup.alpha = t / half;
                yield return null;
            }

            _shotGroup.alpha = 1f;
            _fade = null;
        }

        private void Apply(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            _shot.sprite = _items[index].sprite;
            _shotGroup.alpha = 1f;
            if (_counterText != null) _counterText.text = $"{index + 1} / {_items.Count}";
            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dots[i] != null) _dots[i].color = i == index ? Gold : new Color(0.3f, 0.35f, 0.4f, 1f);
            }

            UpdateCaption();
        }

        private string CaptionFor(GalleryItem item)
        {
            var caption = _definition != null ? _definition.FindCaption(item.name) : null;
            if (caption != null)
            {
                string localized = Pick(caption.captionVi, caption.captionEn, caption.captionJa);
                if (!string.IsNullOrWhiteSpace(localized)) return localized;
            }

            // "03_Sushi restaurant.png" -> "Sushi restaurant"
            string pretty = System.Text.RegularExpressions.Regex.Replace(item.name, "^[0-9]+[ _.-]*", string.Empty).Replace('_', ' ').Trim();
            return string.IsNullOrEmpty(pretty) ? item.name : pretty;
        }

        private void UpdateCaption()
        {
            if (_items.Count == 0 || _index >= _items.Count) return;
            if (_captionText != null) _captionText.text = CaptionFor(_items[_index]);
        }

        private void OpenLightbox()
        {
            if (_items.Count == 0 || _lightbox == null) return;
            _lightboxImage.sprite = _items[_index].sprite;
            _lightboxCaption.text = CaptionFor(_items[_index]);
            _lightbox.SetActive(true);
            _lightbox.transform.SetAsLastSibling();
        }

        private void CloseLightbox()
        {
            if (_lightbox != null) _lightbox.SetActive(false);
            ScheduleAdvance(1.6f);
        }

        // ──────────────────────── Text ────────────────────────

        private string BioText()
        {
            if (_definition != null)
            {
                string bio = Pick(_definition.bioVi, _definition.bioEn, _definition.bioJa);
                if (!string.IsNullOrWhiteSpace(bio)) return bio;
            }

            return Pick(
                "Nihongo Life là game mô phỏng cuộc sống ở Nhật để học tiếng Nhật qua tình huống thật: hội thoại, mua sắm, nhà hàng, trường học và cộng đồng online.",
                "Nihongo Life is a Japan life-sim for learning Japanese through real situations: conversations, shopping, restaurants, school and an online community.",
                "Nihongo Life は、会話・買い物・レストラン・学校・オンライン交流を通して日本語を学ぶ生活シミュレーションです。");
        }

        private string CreditsText()
        {
            if (_definition != null && _definition.credits != null && _definition.credits.Count > 0)
            {
                return string.Join("  ·  ", _definition.credits);
            }

            return "Unity 6  ·  Supabase  ·  Sushi Restaurant Kit  ·  Kenney  ·  KayKit  ·  Mixamo";
        }
    }
}
