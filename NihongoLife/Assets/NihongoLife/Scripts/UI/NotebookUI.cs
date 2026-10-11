using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Notebook;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The pocket notebook (L, the HUD "Sổ tay" button, or the exam toolbar). A small spiral notebook docked at the
    /// right edge — draggable by its cover — with ruled cream pages: a title line, the page body, page chips to jump
    /// between sheets, a character meter and an eraser. Pages are limited (see <see cref="NotebookService"/>); when
    /// every sheet is written on, it points the player to Hibari Mart's stationery shelf for more paper. It sits
    /// above every game window, exams included, so notes can be taken during a test.
    /// </summary>
    public sealed class NotebookUI : MonoBehaviour
    {
        private static readonly Color Cover = new Color(0.16f, 0.27f, 0.25f, 1f);
        private static readonly Color CoverEdge = new Color(0.09f, 0.16f, 0.15f, 1f);
        private static readonly Color PageColor = new Color(0.988f, 0.968f, 0.91f, 1f);
        private static readonly Color Rule = new Color(0.55f, 0.69f, 0.86f, 0.42f);
        private static readonly Color Margin = new Color(0.89f, 0.42f, 0.42f, 0.55f);
        private static readonly Color Ink = new Color(0.13f, 0.17f, 0.27f, 1f);
        private static readonly Color InkMuted = new Color(0.43f, 0.46f, 0.52f, 1f);
        private static readonly Color Gold = new Color(0.96f, 0.78f, 0.36f, 1f);

        private const float Width = 500f, Height = 680f;

        private static NotebookUI _instance;

        private TMP_FontAsset _font;
        private Canvas _canvas;
        private RectTransform _window, _chips, _rules;
        private TMP_InputField _title, _body;
        private TextMeshProUGUI _usage, _pageLabel, _meter, _stamp, _hint, _eraseLabel;
        private Button _prev, _next, _erase;
        private readonly List<Image> _ruleLines = new();
        private bool _applying;
        private float _saveAt = -1f;
        private float _eraseArmedUntil = -1f;
        private int _shownPage = -1;
        private Vector2 _lastViewport;
        private float _lastTextOffset = float.NaN;

        public static bool IsOpen => _instance != null && _instance._window != null && _instance._window.gameObject.activeSelf;
        public static NotebookUI Instance => _instance;
        public TMP_InputField TitleField => _title;
        public TMP_InputField BodyField => _body;
        public RectTransform Window => _window;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var host = new GameObject("NotebookUI");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<NotebookUI>();
        }

        public static NotebookUI GetOrCreate()
        {
            if (_instance == null) Boot();
            return _instance;
        }

        public static void Toggle()
        {
            var ui = GetOrCreate();
            if (IsOpen) ui.Close(); else ui.Open();
        }

        public static void Show() => GetOrCreate().Open();

        private void OnDestroy()
        {
            NotebookService.Changed -= OnNotebookChanged;
            if (_instance == this) _instance = null;
        }

        public void Open()
        {
            if (_window == null) Build();
            _window.gameObject.SetActive(true);
            _canvas.transform.SetAsLastSibling();
            _shownPage = -1;
            Refresh(force: true);
            if (GameServices.TryGet(out NihongoLife.Audio.IAudioService audio)) audio.PlayCue(NihongoLife.Audio.GameAudioCue.UiOpen, 0.6f);
        }

        public void Close()
        {
            if (_window == null || !_window.gameObject.activeSelf) return;
            Commit();
            NotebookService.Save();
            _saveAt = -1f;
            if (EventSystem.current != null && (EventSystem.current.currentSelectedGameObject == _title?.gameObject || EventSystem.current.currentSelectedGameObject == _body?.gameObject))
                EventSystem.current.SetSelectedGameObject(null);
            _window.gameObject.SetActive(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame && !UiModalStack.IsTyping && CanUseHere())
                Toggle();

            if (!IsOpen) return;
            if (_saveAt > 0f && Time.unscaledTime >= _saveAt) { NotebookService.Save(); _saveAt = -1f; }
            if (_eraseArmedUntil > 0f && Time.unscaledTime > _eraseArmedUntil) DisarmErase();
            var viewport = _body.textViewport.rect.size;
            float offset = _body.textComponent.rectTransform.anchoredPosition.y;
            if (viewport != _lastViewport || !Mathf.Approximately(offset, _lastTextOffset)) LayoutRules();
        }

        /// <summary>The notebook belongs to the game world (and its exams), not to the title or login screens.</summary>
        private static bool CanUseHere() =>
            NihongoLife.Player.PlayerInventory.Instance != null || UiModalStack.ImmersiveOpen || IsOpen;

        // ─────────── Data ↔ UI ───────────

        private void OnNotebookChanged()
        {
            if (_applying || !IsOpen) return;
            Refresh(force: NotebookService.CurrentPage != _shownPage);
        }

        private void Refresh(bool force)
        {
            int page = NotebookService.CurrentPage;
            var data = NotebookService.Page(page);
            _applying = true;
            if (force || page != _shownPage)
            {
                _title.SetTextWithoutNotify(data.title);
                _body.SetTextWithoutNotify(data.body);
                _shownPage = page;
                DisarmErase();
            }
            _applying = false;
            int pages = NotebookService.Pages, used = NotebookService.UsedPages;
            _usage.text = $"Đã viết <b>{used}</b>/{pages} trang";
            _pageLabel.text = $"Trang <b>{page + 1}</b> / {pages}";
            _prev.interactable = page > 0;
            _next.interactable = page < pages - 1;
            _stamp.text = data.updatedUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(data.updatedUnix).ToLocalTime().ToString("dd/MM  HH:mm") : "";
            UpdateMeter();
            _hint.gameObject.SetActive(used >= pages);
            RebuildChips();
            LayoutRules();
        }

        private void Commit()
        {
            if (_shownPage < 0 || _title == null) return;
            _applying = true;
            NotebookService.Write(_shownPage, _title.text, _body.text);
            _applying = false;
        }

        private void OnEdited()
        {
            if (_applying) return;
            Commit();
            _saveAt = Time.unscaledTime + 1.2f;
            UpdateMeter();
            _stamp.text = DateTimeOffset.Now.ToString("dd/MM  HH:mm");
            int used = NotebookService.UsedPages;
            _usage.text = $"Đã viết <b>{used}</b>/{NotebookService.Pages} trang";
            _hint.gameObject.SetActive(used >= NotebookService.Pages);
            RefreshChipStates();
            LayoutRules();
        }

        private void UpdateMeter()
        {
            int length = _body.text.Length;
            float fill = length / (float)NotebookService.PageChars;
            string color = fill >= 1f ? "#C0473F" : fill >= 0.85f ? "#C98A1E" : "#6F7684";
            _meter.text = $"<color={color}>{length} / {NotebookService.PageChars}</color> ký tự";
        }

        private void GoTo(int page)
        {
            Commit();
            NotebookService.Save();
            NotebookService.SetCurrentPage(page);
            Refresh(force: true);
        }

        private void EraseClicked()
        {
            if (NotebookService.Page(_shownPage).IsBlank && string.IsNullOrEmpty(_body.text) && string.IsNullOrEmpty(_title.text)) return;
            if (_eraseArmedUntil < 0f)
            {
                _eraseArmedUntil = Time.unscaledTime + 3f;
                _eraseLabel.text = "Bấm lại để xoá";
                _erase.GetComponent<Image>().color = new Color(0.78f, 0.28f, 0.24f, 1f);
                _eraseLabel.color = Color.white;
                return;
            }
            DisarmErase();
            NotebookService.Erase(_shownPage);
            Refresh(force: true);
        }

        private void DisarmErase()
        {
            _eraseArmedUntil = -1f;
            if (_erase == null) return;
            _eraseLabel.text = "Tẩy trang";
            _erase.GetComponent<Image>().color = new Color(0.13f, 0.17f, 0.27f, 0.08f);
            _eraseLabel.color = Ink;
        }

        // ─────────── Page chips ───────────

        private readonly List<(Image bg, TextMeshProUGUI label, int page)> _chipViews = new();

        private void RebuildChips()
        {
            int pages = NotebookService.Pages;
            if (_chipViews.Count != pages)
            {
                foreach (Transform child in _chips) Destroy(child.gameObject);
                _chipViews.Clear();
                for (int i = 0; i < pages; i++)
                {
                    int page = i;
                    var go = new GameObject("Page" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                    go.transform.SetParent(_chips, false);
                    var bg = go.GetComponent<Image>();
                    bg.sprite = UIStyleKit.RoundedSprite();
                    bg.type = Image.Type.Sliced;
                    go.GetComponent<Button>().onClick.AddListener(() => GoTo(page));
                    var element = go.GetComponent<LayoutElement>();
                    element.preferredWidth = element.minWidth = 30f;
                    element.preferredHeight = element.minHeight = 30f;
                    var label = NLUi.Label(go.transform, "N", (i + 1).ToString(), 13f, Color.white, _font, FontStyles.Bold, TextAlignmentOptions.Center);
                    NLUi.Stretch(label.rectTransform);
                    _chipViews.Add((bg, label, page));
                }
            }
            RefreshChipStates();
        }

        private void RefreshChipStates()
        {
            int current = NotebookService.CurrentPage;
            foreach (var (bg, label, page) in _chipViews)
            {
                bool written = page == _shownPage ? !(string.IsNullOrWhiteSpace(_title.text) && string.IsNullOrWhiteSpace(_body.text)) : !NotebookService.Page(page).IsBlank;
                if (page == current) { bg.color = Gold; label.color = new Color(0.2f, 0.14f, 0.04f); }
                else if (written) { bg.color = new Color(1f, 1f, 1f, 0.9f); label.color = Ink; }
                else { bg.color = new Color(1f, 1f, 1f, 0.12f); label.color = new Color(1f, 1f, 1f, 0.75f); }
            }
        }

        // ─────────── Ruled lines that follow the text ───────────

        private void LayoutRules()
        {
            if (_body == null) return;
            var text = _body.textComponent;
            TMP_Text measure = !string.IsNullOrEmpty(_body.text) ? text : (TMP_Text)_body.placeholder;
            measure.ForceMeshUpdate();
            var info = measure.textInfo;
            float pitch = info.lineCount > 0 && info.lineInfo[0].lineHeight > 1f ? info.lineInfo[0].lineHeight : 34f;
            float firstBaseline = info.lineCount > 0 ? info.lineInfo[0].baseline : -26f;
            var viewport = _body.textViewport.rect;
            float offset = text.rectTransform.anchoredPosition.y;
            _lastViewport = viewport.size;
            _lastTextOffset = offset;
            // Lines live under the text rect, so they scroll together with what is written on them.
            int needed = Mathf.CeilToInt((viewport.height + Mathf.Abs(offset)) / pitch) + 2;
            while (_ruleLines.Count < needed)
            {
                var go = new GameObject("Rule", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(_rules, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                var image = go.GetComponent<Image>();
                image.color = Rule;
                image.raycastTarget = false;
                _ruleLines.Add(image);
            }
            for (int i = 0; i < _ruleLines.Count; i++)
            {
                var rect = _ruleLines[i].rectTransform;
                bool on = i < needed;
                _ruleLines[i].gameObject.SetActive(on);
                if (!on) continue;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                float topToBaseline = firstBaseline - (text.rectTransform.rect.yMax);
                rect.anchoredPosition = new Vector2(0f, topToBaseline - 7f - i * pitch);
                rect.sizeDelta = new Vector2(30f, 1.6f);
            }
        }

        // ─────────── Build ───────────

        private void Build()
        {
            _font = NLUi.ResolveFont();
            _canvas = NLUi.CreateCanvas("NotebookCanvas", 900, transform);
            _window = new GameObject("Notebook", typeof(RectTransform), typeof(Image), typeof(Shadow)).GetComponent<RectTransform>();
            _window.SetParent(_canvas.transform, false);
            NLUi.Anchor(_window, new Vector2(1f, 0.5f), new Vector2(-36f, 10f), new Vector2(Width, Height));
            var cover = _window.GetComponent<Image>();
            cover.sprite = UIStyleKit.RoundedSprite();
            cover.type = Image.Type.Sliced;
            cover.color = Cover;
            var shadow = _window.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(6f, -8f);

            // Cover band (drag handle) with the title, usage and close button.
            var band = new GameObject("CoverBand", typeof(RectTransform), typeof(Image), typeof(NotebookDragHandle)).GetComponent<RectTransform>();
            band.SetParent(_window, false);
            band.anchorMin = new Vector2(0f, 1f); band.anchorMax = new Vector2(1f, 1f); band.pivot = new Vector2(0.5f, 1f);
            band.anchoredPosition = Vector2.zero; band.sizeDelta = new Vector2(0f, 64f);
            band.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
            band.GetComponent<NotebookDragHandle>().Target = _window;
            var heading = NLUi.Label(band, "Heading", "<color=#F5C75C>ノート</color>  Sổ tay", 22f, Color.white, _font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Place(heading.rectTransform, 22f, 0f, 220f, 64f, top: true);
            _usage = NLUi.Label(band, "Usage", "", 14f, new Color(0.82f, 0.88f, 0.86f), _font, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            Place(_usage.rectTransform, -64f, 0f, 170f, 64f, top: true, right: true);
            NLUi.CloseButton(band, _font, Close, 36f, 14f);

            // Page chips on the cover.
            var chipsHost = new GameObject("Chips", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            chipsHost.SetParent(_window, false);
            Place(chipsHost, 22f, 66f, Width - 44f, 34f, top: true);
            var scroll = chipsHost.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 18f;
            _chips = NLUi.Group(chipsHost, "Row", false, 6f, TextAnchor.MiddleLeft, false);
            _chips.anchorMin = new Vector2(0f, 0f); _chips.anchorMax = new Vector2(0f, 1f); _chips.pivot = new Vector2(0f, 0.5f);
            _chips.anchoredPosition = Vector2.zero;
            NLUi.FitContent(_chips, width: true, height: false);
            scroll.content = _chips;
            scroll.viewport = chipsHost;

            // The paper sheet with its spiral binding.
            var paper = new GameObject("Paper", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            paper.SetParent(_window, false);
            paper.anchorMin = Vector2.zero; paper.anchorMax = Vector2.one;
            paper.offsetMin = new Vector2(34f, 16f); paper.offsetMax = new Vector2(-16f, -110f);
            var paperImage = paper.GetComponent<Image>();
            paperImage.sprite = UIStyleKit.RoundedSprite();
            paperImage.type = Image.Type.Sliced;
            paperImage.color = PageColor;
            BuildSpiral(paper);

            var margin = new GameObject("Margin", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            margin.SetParent(paper, false);
            margin.anchorMin = new Vector2(0f, 0f); margin.anchorMax = new Vector2(0f, 1f);
            margin.pivot = new Vector2(0f, 0.5f);
            margin.anchoredPosition = new Vector2(40f, 0f); margin.sizeDelta = new Vector2(2f, -24f);
            var marginImage = margin.GetComponent<Image>();
            marginImage.color = Margin; marginImage.raycastTarget = false;

            _title = Input(paper, "TitleField", "Tiêu đề trang…", 21f, FontStyles.Bold, multiline: false, NotebookService.TitleChars);
            Place((RectTransform)_title.transform, 50f, 14f, -64f, 40f, top: true, stretch: true);
            var titleRule = new GameObject("TitleRule", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            titleRule.SetParent(paper, false);
            Place(titleRule, 50f, 54f, -64f, 2f, top: true, stretch: true);
            titleRule.GetComponent<Image>().color = new Color(0.13f, 0.17f, 0.27f, 0.35f);
            _stamp = NLUi.Label(paper, "Stamp", "", 12.5f, InkMuted, _font, FontStyles.Italic, TextAlignmentOptions.MidlineRight);
            Place(_stamp.rectTransform, -16f, 56f, 160f, 22f, top: true, right: true);

            _body = Input(paper, "BodyField", "Viết ghi chú… (từ mới, mẹo làm bài, đáp án cần xem lại)", 18.5f, FontStyles.Normal, multiline: true, NotebookService.PageChars);
            var bodyRect = (RectTransform)_body.transform;
            bodyRect.anchorMin = Vector2.zero; bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(50f, 92f); bodyRect.offsetMax = new Vector2(-14f, -82f);
            _body.textComponent.lineSpacing = 16f;
            ((TMP_Text)_body.placeholder).lineSpacing = 16f;
            _rules = new GameObject("Rules", typeof(RectTransform)).GetComponent<RectTransform>();
            _rules.SetParent(_body.textComponent.rectTransform, false);
            NLUi.Stretch(_rules);
            _rules.SetAsFirstSibling();
            // Ruled lines span the whole sheet width, not only the writing area.
            _rules.offsetMin = new Vector2(-46f, 0f);
            _rules.offsetMax = new Vector2(10f, 0f);

            _title.onValueChanged.AddListener(_ => OnEdited());
            _body.onValueChanged.AddListener(_ => OnEdited());

            // Footer of the sheet.
            _hint = NLUi.Label(paper, "Hint", "Hết giấy rồi! Mua thêm ở <b>Hibari Mart</b> → quầy <b>Văn phòng phẩm</b>, hoặc tẩy một trang cũ.", 13f,
                new Color(0.62f, 0.33f, 0.18f), _font, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            Place(_hint.rectTransform, 50f, 52f, -64f, 36f, top: false, stretch: true);
            _meter = NLUi.Label(paper, "Meter", "", 12.5f, InkMuted, _font, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Place(_meter.rectTransform, 50f, 12f, 118f, 34f, top: false);
            _prev = SmallButton(paper, "Prev", "‹", () => GoTo(NotebookService.CurrentPage - 1), 34f);
            Place((RectTransform)_prev.transform, 170f, 12f, 34f, 34f, top: false);
            _pageLabel = NLUi.Label(paper, "PageLabel", "", 14f, Ink, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            _pageLabel.textWrappingMode = TextWrappingModes.NoWrap;
            Place(_pageLabel.rectTransform, 206f, 12f, 96f, 34f, top: false);
            _next = SmallButton(paper, "Next", "›", () => GoTo(NotebookService.CurrentPage + 1), 34f);
            Place((RectTransform)_next.transform, 304f, 12f, 34f, 34f, top: false);
            _erase = SmallButton(paper, "Erase", "Tẩy trang", EraseClicked, 34f);
            _eraseLabel = _erase.GetComponentInChildren<TextMeshProUGUI>();
            _eraseLabel.fontSize = 13f;
            _eraseLabel.textWrappingMode = TextWrappingModes.NoWrap;
            Place((RectTransform)_erase.transform, -12f, 12f, 92f, 34f, top: false, right: true);
            DisarmErase();

            UiModalStack.Register(this, () => IsOpen, Close, "Notebook");
            NotebookService.Changed += OnNotebookChanged;
            _window.gameObject.SetActive(false);
        }

        private void BuildSpiral(RectTransform paper)
        {
            const int rings = 13;
            for (int i = 0; i < rings; i++)
            {
                float y = -34f - i * ((Height - 126f - 68f) / (rings - 1));
                var hole = new GameObject("Hole", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                hole.SetParent(paper, false);
                hole.anchorMin = hole.anchorMax = new Vector2(0f, 1f);
                hole.pivot = new Vector2(0.5f, 0.5f);
                hole.anchoredPosition = new Vector2(12f, y);
                hole.sizeDelta = new Vector2(10f, 10f);
                var holeImage = hole.GetComponent<Image>();
                holeImage.sprite = UIStyleKit.RoundedSprite(); holeImage.type = Image.Type.Sliced;
                holeImage.color = CoverEdge; holeImage.raycastTarget = false;

                var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                ring.SetParent(paper, false);
                ring.anchorMin = ring.anchorMax = new Vector2(0f, 1f);
                ring.pivot = new Vector2(1f, 0.5f);
                ring.anchoredPosition = new Vector2(14f, y);
                ring.sizeDelta = new Vector2(30f, 7f);
                var ringImage = ring.GetComponent<Image>();
                ringImage.sprite = UIStyleKit.RoundedSprite(); ringImage.type = Image.Type.Sliced;
                ringImage.color = new Color(0.78f, 0.8f, 0.84f, 1f); ringImage.raycastTarget = false;
            }
        }

        private TMP_InputField Input(RectTransform parent, string name, string placeholder, float size, FontStyles style, bool multiline, int limit)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var input = go.GetComponent<TMP_InputField>();
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            input.characterLimit = limit;
            input.richText = false;
            input.caretColor = Ink;
            input.customCaretColor = true;
            input.caretWidth = 2;
            input.selectionColor = new Color(0.98f, 0.82f, 0.32f, 0.45f);
            input.scrollSensitivity = 12f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(go.transform, false);
            NLUi.Stretch(viewport);
            input.textViewport = viewport;

            TextMeshProUGUI Make(string childName, Color color, FontStyles childStyle)
            {
                var t = new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                t.transform.SetParent(viewport, false);
                NLUi.Stretch(t.rectTransform);
                if (_font != null) t.font = _font;
                t.fontSize = size;
                t.color = color;
                t.fontStyle = childStyle;
                t.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
                t.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                t.richText = false;
                return t;
            }
            input.textComponent = Make("Text", Ink, style);
            var ph = Make("Placeholder", new Color(0.43f, 0.46f, 0.52f, 0.55f), style | FontStyles.Italic);
            ph.text = placeholder;
            input.placeholder = ph;
            return input;
        }

        private Button SmallButton(RectTransform parent, string name, string label, Action onClick, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UIStyleKit.RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.13f, 0.17f, 0.27f, 0.08f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());
            var text = NLUi.Label(go.transform, "Label", label, label.Length > 1 ? 13f : 22f, Ink, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(text.rectTransform);
            return button;
        }

        /// <summary>Positions <paramref name="rect"/> from the top/bottom-left (or right) corner of its parent.
        /// With <paramref name="stretch"/>, width is a right inset (negative) instead of a width.</summary>
        private static void Place(RectTransform rect, float x, float y, float width, float height, bool top, bool right = false, bool stretch = false)
        {
            float v = top ? 1f : 0f;
            if (stretch)
            {
                rect.anchorMin = new Vector2(0f, v); rect.anchorMax = new Vector2(1f, v); rect.pivot = new Vector2(0.5f, v);
                rect.offsetMin = new Vector2(x, top ? -y - height : y);
                rect.offsetMax = new Vector2(width, top ? -y : y + height);
                return;
            }
            float h = right ? 1f : 0f;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(h, v);
            rect.anchoredPosition = new Vector2(x, top ? -y : y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }

    /// <summary>Drags the notebook by its cover, kept inside the screen.</summary>
    public sealed class NotebookDragHandle : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        public RectTransform Target;
        private Vector2 _grab;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Target.parent, eventData.position, eventData.pressEventCamera, out var local);
            _grab = Target.anchoredPosition - local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            var parent = (RectTransform)Target.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var local);
            var position = local + _grab;
            // Keep at least the cover band on screen (anchor: right-middle, pivot: right-middle).
            var size = Target.rect.size;
            var area = parent.rect;
            position.x = Mathf.Clamp(position.x, -area.width + size.x * 0.25f, size.x * 0.75f);
            position.y = Mathf.Clamp(position.y, -area.height * 0.5f + 60f, area.height * 0.5f - 30f);
            Target.anchoredPosition = position;
        }
    }
}
