using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Health;
using NihongoLife.Player;
using NihongoLife.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The handbook (ハンドブック, key Y): an open book in a teal cloth cover — cream pages, a stitched spine and coloured
    /// ribbon bookmarks for its chapters:
    /// 案内 Introduction (Resources/Guide/handbook.json: the game, controls, body, learning, work, online, tips),
    /// 場所 Places (locations.json, photos), 健康 Health (your condition, every illness with cause / effects / real
    /// treatment / prevention, the medicine shelf, becoming a doctor in Japan), 仕事 Careers (careers.json ladders) and
    /// 音楽 Music (every track: listen, pin one to play everywhere). Left page: the chapter's entries; right page: the
    /// chosen entry. Content is data-driven; the window only lays it out.
    /// </summary>
    public sealed partial class HandbookUI : MonoBehaviour
    {
        public enum Chapter { Intro, Places, Health, Careers, Doctor, Music, Dictionary }

        // Book palette.
        private static readonly Color Cover = new Color(0.13f, 0.26f, 0.25f, 1f);
        private static readonly Color CoverEdge = new Color(0.07f, 0.15f, 0.14f, 1f);
        private static readonly Color Gilt = new Color(0.85f, 0.71f, 0.38f, 1f);
        private static readonly Color Paper = new Color(0.97f, 0.94f, 0.88f, 1f);
        private static readonly Color PaperRight = new Color(0.975f, 0.95f, 0.895f, 1f);
        private static readonly Color Ink = new Color(0.18f, 0.16f, 0.14f, 1f);
        private static readonly Color InkSoft = new Color(0.45f, 0.4f, 0.34f, 1f);
        private static readonly Color Rule = new Color(0.18f, 0.16f, 0.14f, 0.12f);

        private sealed class Entry
        {
            public string id, glyph, title, subtitle, badge;
            public Color color;
            public Action<RectTransform> build;
        }

        [Serializable] private sealed class IntroEntry { public string id, glyph, color, ja, titleVi, titleEn, subtitleVi, subtitleEn, bodyVi, bodyEn; }
        [Serializable] private sealed class DoctorPath { public string titleVi, titleEn, bodyVi, bodyEn; }
        [Serializable] private sealed class HandbookFile { public IntroEntry[] intro = Array.Empty<IntroEntry>(); public DoctorPath doctorPath; }

        private static HandbookUI _instance;
        private static HandbookFile _file;
        private TMP_FontAsset _font;
        private RectTransform _root, _book, _ribbons, _leftHeader, _list, _detail;
        private ScrollRect _listScroll, _detailScroll;
        private TextMeshProUGUI _leftFolio, _rightFolio;
        private PlayerController _player;
        private bool _wasLocked;
        private Chapter _chapter;
        private string _entry;
        private List<Entry> _entries = new List<Entry>();

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeInHierarchy;
        public static Chapter CurrentChapter => _instance != null ? _instance._chapter : Chapter.Intro;
        public static string CurrentEntry => _instance != null ? _instance._entry : null;
        /// <summary>Entry ids of the chapter shown (tests).</summary>
        public static IEnumerable<string> EntryIds => _instance != null ? _instance._entries.Select(e => e.id) : Enumerable.Empty<string>();

        private static HandbookFile File
        {
            get
            {
                if (_file != null) return _file;
                var text = Resources.Load<TextAsset>("Guide/handbook");
                _file = text != null ? JsonUtility.FromJson<HandbookFile>(text.text) ?? new HandbookFile() : new HandbookFile();
                return _file;
            }
        }

        private static HandbookUI Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("HandbookCanvas", 645);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<HandbookUI>();
                _instance._font = NLUi.ResolveFont();
                _instance.Build((RectTransform)canvas.transform);
                Loc.Watch(_instance, () => { if (IsOpen) _instance.Show(_instance._chapter, _instance._entry); });
                UiModalStack.Register(_instance, () => IsOpen, Close, "Handbook");
                PlayerIllness.Changed += () => { if (IsOpen && _instance._chapter == Chapter.Health) _instance.Show(Chapter.Health, _instance._entry); };
                return _instance;
            }
        }

        public static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public static void Open(Chapter chapter = Chapter.Intro, string entry = null)
        {
            var ui = Instance;
            if (!ui._root.gameObject.activeSelf)
            {
                ui._root.gameObject.SetActive(true);
                ui._player = SceneRefs.Find<PlayerController>();
                if (ui._player != null) { ui._wasLocked = ui._player.InputLocked; ui._player.InputLocked = true; }
            }
            ui._root.SetAsLastSibling();
            ui.Show(chapter, entry);
        }

        public static void Close()
        {
            if (_instance == null || _instance._root == null || !_instance._root.gameObject.activeSelf) return;
            _instance._root.gameObject.SetActive(false);
            Soundscape.PreviewKey = null; // a track listened to in the 音楽 chapter stops with the book
            if (_instance._player != null) _instance._player.InputLocked = _instance._wasLocked;
            _instance._player = null;
        }

        // ─────────── The book ───────────

        private void Build(RectTransform canvas)
        {
            _root = new GameObject("Handbook", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _root.SetParent(canvas, false);
            NLUi.Stretch(_root);
            _root.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.68f);

            // Ribbons first: they sit behind the cover and stick out on the right.
            _book = new GameObject("Book", typeof(RectTransform)).GetComponent<RectTransform>();
            _book.SetParent(_root, false);
            _book.anchorMin = _book.anchorMax = _book.pivot = new Vector2(0.5f, 0.5f);
            _book.anchoredPosition = new Vector2(-60f, 0f);
            _book.sizeDelta = new Vector2(1340f, 800f);
            _book.gameObject.AddComponent<HudFitRect>().Configure(0.86f, 0.94f, Vector2.zero, 0.4f);

            _ribbons = new GameObject("Ribbons", typeof(RectTransform)).GetComponent<RectTransform>();
            _ribbons.SetParent(_book, false);
            _ribbons.anchorMin = new Vector2(1f, 1f); _ribbons.anchorMax = new Vector2(1f, 1f); _ribbons.pivot = new Vector2(0f, 1f);
            _ribbons.anchoredPosition = new Vector2(-40f, -70f);
            _ribbons.sizeDelta = new Vector2(220f, 600f);

            var edge = Rounded(_book, "CoverEdge", CoverEdge, 26);
            NLUi.Stretch(edge, -6f);
            var cover = Rounded(_book, "Cover", Cover, 22);
            NLUi.Stretch(cover);
            var gilt = Rounded(cover, "GiltLine", new Color(Gilt.r, Gilt.g, Gilt.b, 0.55f), 16);
            NLUi.Stretch(gilt, 12f);
            var inner = Rounded(cover, "Inner", Cover, 14);
            NLUi.Stretch(inner, 14f);

            var left = Page(cover, "LeftPage", Paper, new Vector2(30f, 30f), new Vector2(-6f, -30f), true);
            var right = Page(cover, "RightPage", PaperRight, new Vector2(6f, 30f), new Vector2(-30f, -30f), false);
            Spine(cover);

            // Left page: chapter header, the entries, folio.
            _leftHeader = NLUi.Group(left, "Header", true, 6f);
            _leftHeader.anchorMin = new Vector2(0f, 1f); _leftHeader.anchorMax = new Vector2(1f, 1f); _leftHeader.pivot = new Vector2(0.5f, 1f);
            _leftHeader.offsetMin = new Vector2(40f, -150f); _leftHeader.offsetMax = new Vector2(-34f, -30f);
            var listRoot = Area(left, "Entries", new Vector2(30f, 54f), new Vector2(-24f, -158f));
            _list = UiKit.Scroll(listRoot, "List", out _listScroll, 6f, new RectOffset(6, 14, 2, 10));
            NLUi.Stretch((RectTransform)_listScroll.transform);
            _leftFolio = Folio(left);

            var detailRoot = Area(right, "Detail", new Vector2(40f, 54f), new Vector2(-30f, -30f));
            _detail = UiKit.Scroll(detailRoot, "Content", out _detailScroll, 12f, new RectOffset(4, 18, 6, 18));
            NLUi.Stretch((RectTransform)_detailScroll.transform);
            _rightFolio = Folio(right);

            var close = NLUi.CloseButton(_book, _font, Close, 46f, -14f);
            close.transform.SetAsLastSibling();
            _root.gameObject.SetActive(false);
        }

        private static RectTransform Rounded(Transform parent, string name, Color color, int radius)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UiKit.Rounded(radius);
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = name != "GiltLine";
            return (RectTransform)go.transform;
        }

        private RectTransform Page(RectTransform cover, string name, Color color, Vector2 offsetMin, Vector2 offsetMax, bool leftSide)
        {
            var page = Rounded(cover, name, color, 10);
            page.anchorMin = leftSide ? new Vector2(0f, 0f) : new Vector2(0.5f, 0f);
            page.anchorMax = leftSide ? new Vector2(0.5f, 1f) : new Vector2(1f, 1f);
            page.offsetMin = offsetMin; page.offsetMax = offsetMax;
            // A faint shade toward the spine, as on a real open book.
            var shade = new GameObject("SpineShade", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            shade.SetParent(page, false);
            shade.anchorMin = leftSide ? new Vector2(1f, 0f) : Vector2.zero;
            shade.anchorMax = leftSide ? Vector2.one : new Vector2(0f, 1f);
            shade.pivot = new Vector2(leftSide ? 1f : 0f, 0.5f);
            shade.sizeDelta = new Vector2(36f, 0f);
            var img = shade.GetComponent<Image>();
            img.sprite = SpineGradient(leftSide);
            img.color = new Color(0.35f, 0.27f, 0.16f, 0.22f);
            img.raycastTarget = false;
            return page;
        }

        private static Sprite SpineGradient(bool leftSide)
        {
            var texture = new Texture2D(32, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 32; x++)
            {
                float t = x / 31f;
                float a = leftSide ? t * t : (1f - t) * (1f - t);
                texture.SetPixel(x, 0, new Color(1f, 1f, 1f, a));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 32, 1), new Vector2(0.5f, 0.5f));
        }

        private static void Spine(RectTransform cover)
        {
            var spine = new GameObject("Spine", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            spine.SetParent(cover, false);
            spine.anchorMin = new Vector2(0.5f, 0f); spine.anchorMax = new Vector2(0.5f, 1f);
            spine.sizeDelta = new Vector2(10f, -40f);
            spine.GetComponent<Image>().color = new Color(0.08f, 0.16f, 0.15f, 1f);
            // Gilt stitches down the spine.
            for (int i = 0; i < 14; i++)
            {
                var stitch = new GameObject("Stitch", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                stitch.SetParent(spine, false);
                stitch.anchorMin = stitch.anchorMax = new Vector2(0.5f, 1f - (i + 0.5f) / 14f);
                stitch.sizeDelta = new Vector2(3f, 18f);
                stitch.GetComponent<Image>().color = new Color(Gilt.r, Gilt.g, Gilt.b, 0.7f);
            }
        }

        private static RectTransform Area(RectTransform page, string name, Vector2 offsetMin, Vector2 offsetMax)
        {
            var area = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(page, false);
            NLUi.Stretch(area);
            area.offsetMin = offsetMin; area.offsetMax = offsetMax;
            return area;
        }

        private TextMeshProUGUI Folio(RectTransform page)
        {
            var folio = NLUi.Label(page, "Folio", "", 14f, InkSoft, _font, FontStyles.Italic, TextAlignmentOptions.Center);
            folio.rectTransform.anchorMin = new Vector2(0f, 0f); folio.rectTransform.anchorMax = new Vector2(1f, 0f);
            folio.rectTransform.pivot = new Vector2(0.5f, 0f);
            folio.rectTransform.anchoredPosition = new Vector2(0f, 16f);
            folio.rectTransform.sizeDelta = new Vector2(0f, 24f);
            return folio;
        }

        // ─────────── Chapters ───────────

        private static string ChapterGlyph(Chapter c) => c switch { Chapter.Intro => "案内", Chapter.Places => "場所", Chapter.Health => "健康", Chapter.Careers => "仕事", Chapter.Doctor => "医師", Chapter.Dictionary => "辞書", _ => "音楽" };

        private static string ChapterName(Chapter c) => c switch
        {
            Chapter.Intro => Loc.T("Giới thiệu", "Introduction", "ゲーム案内"),
            Chapter.Places => Loc.T("Địa điểm", "Places", "場所"),
            Chapter.Health => Loc.T("Sức khoẻ & bệnh", "Health & illness", "健康と病気"),
            Chapter.Careers => Loc.T("Nghề nghiệp", "Careers", "仕事"),
            Chapter.Doctor => Loc.T("Sổ tay bác sĩ", "Doctor's notebook", "医師の手帳"),
            Chapter.Dictionary => Loc.T("Từ điển", "Dictionary", "辞書"),
            _ => Loc.T("Âm nhạc", "Music", "音楽"),
        };

        private static string ChapterLine(Chapter c) => c switch
        {
            Chapter.Intro => Loc.T("NihongoLife là gì, cách chơi, mẹo sống ở Nhật", "What NihongoLife is, how to play, tips for Japan", "ゲームの遊び方"),
            Chapter.Places => Loc.T("Mỗi nơi trong thị trấn: ý nghĩa, cách dùng, câu hay", "Every place in town: meaning, use, phrases", "町の場所"),
            Chapter.Health => Loc.T("Nguyên nhân, triệu chứng, cách chữa đúng như ngoài đời", "Causes, symptoms and real-life treatment", "原因・症状・治し方"),
            Chapter.Careers => Loc.T("Các nghề và nấc thang thăng tiến", "Careers and their ladders", "職業とキャリア"),
            Chapter.Doctor => Loc.T("Ca lâm sàng đã học ở 研修室: chẩn đoán, xử trí, dấu hiệu nguy hiểm", "Cases studied in the 研修室: diagnosis, treatment, red flags", "研修室の症例：診断・治療・注意"),
            Chapter.Dictionary => Loc.T("Tra từ giữa tiếng Việt, Anh, Nhật, Nga", "Look words up between Vietnamese, English, Japanese and Russian", "ベトナム語・英語・日本語・ロシア語の辞書"),
            _ => Loc.T("Nhạc nền của từng nơi — nghe thử, ghim bài yêu thích", "Every place's music — listen, pin a favourite", "BGMを聴く"),
        };

        private static Color ChapterColor(Chapter c) => c switch
        {
            Chapter.Intro => new Color(0.75f, 0.31f, 0.3f),
            Chapter.Places => new Color(0.31f, 0.5f, 0.74f),
            Chapter.Health => new Color(0.24f, 0.62f, 0.49f),
            Chapter.Careers => new Color(0.82f, 0.54f, 0.18f),
            Chapter.Doctor => new Color(0.2f, 0.45f, 0.62f),
            Chapter.Dictionary => new Color(0.18f, 0.5f, 0.5f),
            _ => new Color(0.5f, 0.39f, 0.64f),
        };

        private void Show(Chapter chapter, string entryId)
        {
            if (_chapter != chapter) _entry = null;
            _chapter = chapter;
            _entries = Entries(chapter);
            var entry = _entries.FirstOrDefault(e => e.id == entryId) ?? _entries.FirstOrDefault(e => e.id == _entry) ?? _entries.FirstOrDefault();
            _entry = entry?.id;
            BuildRibbons();
            BuildHeader();
            BuildList();
            BuildDetail(entry);
            int index = Mathf.Max(0, _entries.IndexOf(entry));
            int page = (int)chapter * 20 + index * 2 + 1;
            _leftFolio.text = $"— {page} —";
            _rightFolio.text = $"— {page + 1} —";
        }

        private void BuildRibbons()
        {
            UiKit.Clear(_ribbons);
            int i = 0;
            foreach (Chapter c in Enum.GetValues(typeof(Chapter)))
            {
                bool on = c == _chapter;
                var color = ChapterColor(c);
                var ribbon = Rounded(_ribbons, "Ribbon_" + c, on ? color : Color.Lerp(color, CoverEdge, 0.45f), 12);
                ribbon.anchorMin = ribbon.anchorMax = ribbon.pivot = new Vector2(0f, 1f);
                ribbon.anchoredPosition = new Vector2(0f, -i * 74f);
                ribbon.sizeDelta = new Vector2(on ? 196f : 172f, 64f);
                var button = ribbon.gameObject.AddComponent<Button>();
                button.targetGraphic = ribbon.GetComponent<Image>();
                var chapter = c;
                button.onClick.AddListener(() => Show(chapter, null));
                var glyph = NLUi.Label(ribbon, "Glyph", ChapterGlyph(c), 22f, Color.white, _font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
                glyph.rectTransform.anchorMin = new Vector2(0f, 0f); glyph.rectTransform.anchorMax = new Vector2(1f, 1f);
                glyph.rectTransform.offsetMin = new Vector2(56f, 22f); glyph.rectTransform.offsetMax = new Vector2(-8f, -4f);
                var label = NLUi.Label(ribbon, "Label", ChapterName(c), 13f, new Color(1f, 1f, 1f, 0.85f), _font, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
                label.rectTransform.anchorMin = new Vector2(0f, 0f); label.rectTransform.anchorMax = new Vector2(1f, 0f);
                label.rectTransform.pivot = new Vector2(0.5f, 0f);
                label.rectTransform.offsetMin = new Vector2(56f, 6f); label.rectTransform.offsetMax = new Vector2(-8f, 26f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                i++;
            }
        }

        private void BuildHeader()
        {
            UiKit.Clear(_leftHeader);
            var color = ChapterColor(_chapter);
            var row = NLUi.Group(_leftHeader, "Row", false, 14f, TextAnchor.MiddleLeft, false);
            Badge(row, ChapterGlyph(_chapter), color, 72f, 26f);
            var titles = NLUi.Group(row, "Titles", true, 2f);
            NLUi.Size(titles, 420f);
            NLUi.Label(titles, "Kicker", Loc.T("CUỐN SỔ TAY NIHONGOLIFE", "THE NIHONGOLIFE HANDBOOK", "にほんごライフ ハンドブック"), 12f, InkSoft, _font, FontStyles.Bold);
            NLUi.Label(titles, "Title", ChapterName(_chapter), 30f, Ink, _font, FontStyles.Bold);
            NLUi.Label(titles, "Line", ChapterLine(_chapter), 15f, InkSoft, _font, FontStyles.Italic);
            Divider(_leftHeader, color);
        }

        private void Divider(RectTransform parent, Color color)
        {
            var line = NLUi.Group(parent, "Divider", false, 6f, TextAnchor.MiddleLeft, false);
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bar.SetParent(line, false);
            bar.GetComponent<Image>().color = color;
            NLUi.Size(bar, 120f, 3f);
            var rest = new GameObject("Rest", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rest.SetParent(line, false);
            rest.GetComponent<Image>().color = Rule;
            NLUi.Size(rest, 380f, 1f);
        }

        private RectTransform Badge(RectTransform parent, string glyph, Color color, float size, float fontSize)
        {
            var disc = new GameObject("Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            disc.SetParent(parent, false);
            var image = disc.GetComponent<Image>();
            image.sprite = HudGraphics.Disc;
            image.color = color;
            var layout = disc.GetComponent<LayoutElement>();
            layout.preferredWidth = layout.minWidth = size; layout.preferredHeight = layout.minHeight = size;
            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            ring.SetParent(disc, false);
            NLUi.Stretch(ring, 3f);
            ring.GetComponent<Image>().sprite = UiKit.RingThin;
            ring.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
            ring.GetComponent<Image>().raycastTarget = false;
            var text = NLUi.Label(disc, "Glyph", glyph, fontSize, Color.white, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (text.TryGetComponent(out LayoutElement le)) le.ignoreLayout = true;
            return disc;
        }

        private void BuildList()
        {
            var host = ListHost();
            foreach (var e in _entries)
            {
                bool on = e.id == _entry;
                var row = NLUi.Panel(host, "Entry_" + e.id, on ? new Color(e.color.r, e.color.g, e.color.b, 0.16f) : new Color(1f, 1f, 1f, 0f),
                    new RectOffset(10, 10, 8, 8), 12f, vertical: false);
                var layout = (HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>();
                layout.childForceExpandWidth = false;
                layout.childAlignment = TextAnchor.MiddleLeft;
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row.GetComponent<Image>();
                var colors = button.colors;
                colors.highlightedColor = new Color(0.92f, 0.86f, 0.74f, 1f);
                button.colors = colors;
                string id = e.id;
                button.onClick.AddListener(() => Show(_chapter, id));
                Badge(row, e.glyph, e.color, 46f, e.glyph.Length > 1 ? 15f : 21f);
                var texts = NLUi.Group(row, "Texts", true, 0f);
                NLUi.Size(texts, 380f, -1f, 1f);
                NLUi.Label(texts, "Title", e.title, 17f, Ink, _font, on ? FontStyles.Bold : FontStyles.Normal);
                if (!string.IsNullOrEmpty(e.subtitle)) NLUi.Label(texts, "Subtitle", e.subtitle, 13f, InkSoft, _font);
                if (!string.IsNullOrEmpty(e.badge))
                {
                    var pill = NLUi.Pill(row, "Status", e.badge, _font, e.color, Color.white, 12f);
                    NLUi.Size(pill, -1f, 26f);
                }
            }
            _listScroll.verticalNormalizedPosition = 1f;
        }

        private void BuildDetail(Entry entry)
        {
            UiKit.Clear(_detail);
            if (entry == null) return;
            entry.build(_detail);
            Canvas.ForceUpdateCanvases();
            _detailScroll.verticalNormalizedPosition = 1f;
        }

        // ─────────── Detail page helpers ───────────

        private void Hero(RectTransform parent, string glyph, Color color, string title, string japanese, string tag)
        {
            var row = NLUi.Group(parent, "Hero", false, 18f, TextAnchor.MiddleLeft, false);
            Badge(row, glyph, color, 96f, glyph.Length > 1 ? 30f : 44f);
            var texts = NLUi.Group(row, "Texts", true, 2f);
            NLUi.Size(texts, 420f);
            if (!string.IsNullOrEmpty(tag)) NLUi.Label(texts, "Tag", tag.ToUpperInvariant(), 12f, color, _font, FontStyles.Bold);
            NLUi.Label(texts, "Title", title, 28f, Ink, _font, FontStyles.Bold);
            if (!string.IsNullOrEmpty(japanese)) NLUi.Label(texts, "Japanese", japanese, 18f, InkSoft, _font);
            Divider(parent, color);
        }

        private void Section(RectTransform parent, string heading, string body, Color color)
        {
            if (string.IsNullOrEmpty(body)) return;
            var block = NLUi.Group(parent, "Section", true, 4f);
            var head = NLUi.Group(block, "Head", false, 8f, TextAnchor.MiddleLeft, false);
            var mark = new GameObject("Mark", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            mark.SetParent(head, false);
            mark.GetComponent<Image>().color = color;
            NLUi.Size(mark, 6f, 18f);
            NLUi.Label(head, "Heading", heading, 18f, Ink, _font, FontStyles.Bold);
            NLUi.Label(block, "Body", body, 16f, Ink, _font).lineSpacing = 6f;
        }

        /// <summary>Text with "## heading" lines and "• " bullets laid out as sections.</summary>
        private void Rich(RectTransform parent, string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            string heading = null;
            var body = new List<string>();
            void Flush()
            {
                string joined = string.Join("\n", body).Trim();
                if (heading != null) Section(parent, heading, joined, color);
                else if (joined.Length > 0) NLUi.Label(parent, "Lead", joined, 17f, Ink, _font).lineSpacing = 6f;
                body.Clear();
            }
            foreach (var line in text.Split('\n'))
            {
                if (line.StartsWith("## ")) { Flush(); heading = line.Substring(3); }
                else body.Add(line.StartsWith("• ") ? "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">●</color>  " + line.Substring(2) : line);
            }
            Flush();
        }

        private RectTransform Card(RectTransform parent, Color tint)
        {
            var card = NLUi.Panel(parent, "Card", new Color(tint.r, tint.g, tint.b, 0.12f), new RectOffset(16, 16, 12, 12), 4f);
            return card;
        }

        private Button PageButton(RectTransform parent, string name, string label, Color color, Action onClick)
        {
            var button = NLUi.Button(parent, name, label, _font, onClick, color, 15f, Color.white, 40f);
            ((HorizontalOrVerticalLayoutGroup)button.GetComponent<HorizontalOrVerticalLayoutGroup>()).padding = new RectOffset(16, 16, 8, 8);
            button.GetComponentInChildren<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.FitContent((RectTransform)button.transform, width: true, height: false);
            NLUi.Size(button, -1f, -1f, 0f); // packed to the left, not spread over the row
            return button;
        }

        // ─────────── Entries of each chapter ───────────

        private List<Entry> Entries(Chapter chapter) => chapter switch
        {
            Chapter.Intro => IntroEntries(),
            Chapter.Places => PlaceEntries(),
            Chapter.Health => HealthEntries(),
            Chapter.Careers => CareerEntries(),
            Chapter.Doctor => DoctorEntries(),
            Chapter.Dictionary => DictionaryEntries(),
            _ => MusicEntries(),
        };

        private static Color Hex(string hex, Color fallback) => !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;

        private List<Entry> IntroEntries()
        {
            var list = new List<Entry>();
            foreach (var i in File.intro)
            {
                var item = i;
                var color = Hex(i.color, ChapterColor(Chapter.Intro));
                list.Add(new Entry
                {
                    id = i.id, glyph = i.glyph, color = color, title = Loc.Pick(i.titleVi, i.titleEn, i.ja), subtitle = Loc.Pick(i.subtitleVi, i.subtitleEn, i.ja),
                    build = p => { Hero(p, item.glyph, color, Loc.Pick(item.titleVi, item.titleEn, item.ja), item.ja, ChapterName(Chapter.Intro)); Rich(p, Loc.Pick(item.bodyVi, item.bodyEn, item.bodyEn), color); },
                });
            }
            return list;
        }

        private List<Entry> PlaceEntries()
        {
            var list = new List<Entry>();
            var color = ChapterColor(Chapter.Places);
            foreach (var place in LocationGuideUI.Places)
            {
                var p0 = place;
                string scene = place.type == "MIDORI" ? WorldLocationCatalog.MidoriIslandScene : WorldMapUI.SceneOfType(place.type);
                bool open = scene == null || !LocationUnlocks.IsGated(scene) || LocationUnlocks.IsUnlocked(scene);
                list.Add(new Entry
                {
                    id = place.type, glyph = place.ja.Substring(0, 1), color = color, title = place.Name, subtitle = place.ja,
                    badge = open ? null : Loc.T("khoá", "locked", "未解放"),
                    build = p => BuildPlace(p, p0, scene, color),
                });
            }
            return list;
        }

        /// <summary>
        /// The doctor's notebook: every clinical case of the clinic's training room (Clinic/MedicalTraining). A case
        /// solved there shows its full card — the patient's words, the findings, the diagnosis, the treatment, the red
        /// flags and the source; one not studied yet keeps its answers for the training room.
        /// </summary>
        private List<Entry> DoctorEntries()
        {
            var list = new List<Entry>();
            var color = ChapterColor(Chapter.Doctor);
            foreach (var c in NihongoLife.Clinic.MedicalTraining.Catalog.cases)
            {
                var item = c;
                bool done = NihongoLife.Clinic.MedicalTraining.IsDone(c.id);
                list.Add(new Entry
                {
                    id = c.id, glyph = c.glyph, color = color, title = c.Title, subtitle = c.patientJa,
                    badge = done ? "✓" : Loc.T("chưa học", "to study", "未修"),
                    build = p => BuildCase(p, item, done, color),
                });
            }
            return list;
        }

        private void BuildCase(RectTransform p, NihongoLife.Clinic.TrainingCase c, bool done, Color color)
        {
            Hero(p, c.glyph, color, c.Title, c.titleJa, ChapterName(Chapter.Doctor) + " · " + c.Specialty);
            Section(p, Loc.T("Bệnh nhân nói", "The patient says", "かんじゃさんの ことば"), c.patientJa, color);
            Section(p, Loc.T("Khám", "Findings", "所見"), c.Findings, color);
            if (!done)
            {
                Section(p, Loc.T("Chưa học", "Not studied yet", "未修"), NihongoLife.Clinic.MedicalTraining.IsMedical
                    ? Loc.T("Học ca này ở phòng đào tạo (研修室) của phòng khám Hibari để mở chẩn đoán và cách xử trí.", "Study this case in Hibari Clinic's training room (研修室) to open the diagnosis and treatment.", "研修室で学ぶと、診断と治療が開きます。")
                    : Loc.T("Dành cho nghề Y tế: chọn nghề từ cấp 10, rồi học ở phòng đào tạo (研修室) của phòng khám.", "For the medical career: choose it from level 10, then study in the clinic's training room (研修室).", "医療キャリアの研修室で学べます。"), color);
                return;
            }
            Section(p, Loc.T("Chẩn đoán", "Diagnosis", "診断"), c.RightDiagnosis?.Label + "  ·  " + c.RightDiagnosis?.ja, color);
            Section(p, Loc.T("Xử trí", "Treatment", "治療"), c.RightTreatment?.Label + "  ·  " + c.RightTreatment?.ja, color);
            var wrong = string.Join("\n", c.treatment.Where(t => !t.correct).Select(t => "• " + t.Label + " — " + t.Why));
            Section(p, Loc.T("Không nên", "Do not", "してはいけない"), wrong, color);
            Section(p, Loc.T("Dấu hiệu nguy hiểm", "Red flags", "危険なサイン"), c.RedFlags, new Color(0.75f, 0.25f, 0.22f));
            Section(p, Loc.T("Nguồn", "Source", "出典"), c.sourceVi, color);
        }

        private void BuildPlace(RectTransform p, LocationGuideUI.Place place, string scene, Color color)
        {
            var tex = Resources.Load<Texture2D>("Guide/" + place.type);
            if (tex != null)
            {
                var frame = NLUi.Panel(p, "PhotoFrame", Color.white, new RectOffset(8, 8, 8, 8), 0f);
                var photo = new GameObject("Photo", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement)).GetComponent<RawImage>();
                photo.transform.SetParent(frame, false);
                photo.texture = tex;
                var le = photo.GetComponent<LayoutElement>();
                le.preferredHeight = 290f; le.preferredWidth = 520f;
                frame.localRotation = Quaternion.Euler(0f, 0f, -1.2f); // a photo pasted into the book
            }
            string status = scene == null || !LocationUnlocks.IsGated(scene)
                ? Loc.T("Luôn mở", "Always open", "いつでも行ける")
                : LocationUnlocks.IsUnlocked(scene) ? Loc.T("Đã mở", "Unlocked", "解放済み") : LocationUnlocks.ShortStatus(scene);
            Hero(p, place.ja.Substring(0, 1), color, place.Name, place.ja, status);
            NLUi.Label(p, "About", place.About, 17f, Ink, _font).lineSpacing = 6f;
            string bullet = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">●</color>  ";
            Section(p, Loc.T("Cách tương tác", "How to use it", "使い方"), string.Join("\n", (place.How ?? Array.Empty<string>()).Select(h => bullet + h)), color);
            var phrases = new List<string>();
            for (int i = 0; i < place.phrases.Length; i++)
            {
                string meaning = place.PhraseMeaning(i);
                phrases.Add("<b>" + place.phrases[i][0] + "</b>" + (string.IsNullOrEmpty(meaning) ? "" : "\n<color=#736655>" + meaning + "</color>"));
            }
            Section(p, Loc.T("Câu hữu ích", "Useful phrases", "便利なフレーズ"), string.Join("\n", phrases), color);
        }

        private List<Entry> HealthEntries()
        {
            var list = new List<Entry>();
            var catalog = IllnessCatalog.Load();
            var green = ChapterColor(Chapter.Health);
            int ill = PlayerIllness.Active.Count;
            list.Add(new Entry
            {
                id = "status", glyph = "診", color = green, title = Loc.T("Sức khoẻ của bạn", "Your health", "あなたの体調"),
                subtitle = ill == 0 ? Loc.T("Khoẻ mạnh", "Healthy", "元気") : Loc.F("Đang mắc {0} bệnh", "{0} illness(es) now", "{0}つの病気", ill), badge = ill > 0 ? ill.ToString() : null,
                build = p => BuildStatus(p, green),
            });
            foreach (var def in catalog.illnesses)
            {
                var d = def;
                bool has = PlayerIllness.Has(def.id);
                bool had = PlayerIllness.State?.history.Contains(def.id) == true;
                list.Add(new Entry
                {
                    id = def.id, glyph = def.glyph, color = def.Tint, title = def.Name, subtitle = def.kanji + " · " + def.ja,
                    badge = has ? Loc.T("đang mắc", "now", "今") : had ? Loc.T("đã từng", "had it", "経験") : null,
                    build = p => BuildIllness(p, d),
                });
            }
            list.Add(new Entry
            {
                id = "medicines", glyph = "薬", color = new Color(0.36f, 0.62f, 0.6f), title = Loc.T("Tủ thuốc (くすり)", "The medicine shelf (くすり)", "くすり"),
                subtitle = Loc.T("Thuốc không kê đơn ở Hibari Mart", "Over-the-counter medicine at Hibari Mart", "ひばりマートのくすり"),
                build = p => BuildMedicines(p, new Color(0.36f, 0.62f, 0.6f)),
            });
            var path = File.doctorPath;
            if (path != null)
                list.Add(new Entry
                {
                    id = "doctor_path", glyph = "医", color = new Color(0.25f, 0.45f, 0.7f), title = Loc.Pick(path.titleVi, path.titleEn, path.titleEn),
                    subtitle = Loc.T("Ngoài đời thật và trong game", "Real life and in the game", "医師への道"),
                    build = p =>
                    {
                        var c = new Color(0.25f, 0.45f, 0.7f);
                        Hero(p, "医", c, Loc.Pick(path.titleVi, path.titleEn, path.titleEn), "いしゃに なるには", ChapterName(Chapter.Health));
                        Rich(p, Loc.Pick(path.bodyVi, path.bodyEn, path.bodyEn), c);
                        var go = PageButton(p, "OpenMedicalCareer", Loc.T("Xem nghề Y tế →", "See the Medicine career →", "医療の仕事へ →"), c, () => Show(Chapter.Careers, "medical"));
                        NLUi.Size(go, -1f, 40f, 0f);
                    },
                });
            return list;
        }

        private void BuildStatus(RectTransform p, Color green)
        {
            var status = PlayerStatus.Instance;
            Hero(p, "診", green, Loc.T("Sức khoẻ của bạn", "Your health", "あなたの体調"), "からだの ちょうし", ChapterName(Chapter.Health));
            if (status != null)
                NLUi.Label(p, "Vitals", Loc.F("Máu {0:0}/{1:0} · No {2:0}% · Nước {3:0}% · Buồn ngủ {4:0}%", "Health {0:0}/{1:0} · Food {2:0}% · Water {3:0}% · Sleepiness {4:0}%", "体力 {0:0}/{1:0} · 満腹 {2:0}% · 水分 {3:0}% · 眠気 {4:0}%",
                    status.CurrentHealth, status.MaxHealth, status.Hunger, status.Thirst, status.Sleepiness), 16f, Ink, _font);
            var active = PlayerIllness.Active;
            if (active.Count == 0)
                NLUi.Label(p, "Healthy", Loc.T("Bạn đang khoẻ. Ăn uống đủ, ngủ đủ, tránh mưa và nắng gắt để giữ sức.", "You are well. Eat, drink and sleep enough, and avoid rain and harsh sun.", "元気です。"), 17f, Ink, _font);
            float now = PlayerIllness.Now;
            foreach (var record in active)
            {
                var def = IllnessCatalog.Load().Illness(record.id);
                if (def == null) continue;
                var card = Card(p, def.Tint);
                NLUi.Label(card, "Name", $"<b>{def.Name}</b>  <color=#736655>{def.kanji}</color>", 19f, Ink, _font);
                float left = Mathf.Max(0f, record.until - now);
                string state = record.treated ? Loc.T("đã được bác sĩ điều trị", "treated by the doctor", "診察済み") : record.relieved ? Loc.T("đã uống thuốc", "medicine taken", "服薬済み") : Loc.T("chưa điều trị", "untreated", "未治療");
                NLUi.Label(card, "Left", Loc.F("Còn khoảng {0:0} giờ trong game (~{0:0} phút) · {1}", "About {0:0} game hours left (~{0:0} min) · {1}", "残り約{0:0}時間 · {1}", left, state), 15f, InkSoft, _font);
                if (def.earlyHours > 0f && !record.treated && now - record.since <= def.earlyHours)
                    NLUi.Label(card, "Early", Loc.F("Đi khám ngay: còn {0:0} giờ trong khung {1:0} giờ đầu, thuốc của bác sĩ hiệu quả nhất.", "See the doctor now: {0:0} h left of the first {1:0} h, when treatment works best.", "早めの受診を（あと{0:0}時間）。", def.earlyHours - (now - record.since), def.earlyHours), 15f, def.Tint, _font, FontStyles.Bold);
                var open = PageButton(card, "Open_" + def.id, Loc.T("Cách chữa →", "Treatment →", "治し方 →"), def.Tint, () => Show(Chapter.Health, def.id));
                NLUi.Size(open, -1f, 38f, 0f);
            }
            var lines = new List<string>();
            float flu = PlayerIllness.ImmuneUntil("flu");
            if (flu > now) lines.Add(Loc.F("Đã tiêm phòng cúm — còn ~{0:0} ngày", "Flu shot — about {0:0} days left", "予防接種済み（あと約{0:0}日）", (flu - now) / 24f));
            if (PlayerIllness.IsImmune("mask")) lines.Add(Loc.T("Đang đeo khẩu trang", "Wearing a mask", "マスク着用中"));
            if (lines.Count > 0) Section(p, Loc.T("Phòng ngừa", "Protection", "予防"), string.Join("\n", lines), green);
            var history = PlayerIllness.State?.history;
            if (history != null && history.Count > 0)
                Section(p, Loc.T("Đã từng mắc", "Had before", "これまでの病気"), string.Join(" · ", history.Select(h => IllnessCatalog.Load().Illness(h)?.Name ?? h)), green);
        }

        private void BuildIllness(RectTransform p, IllnessDefinition def)
        {
            Hero(p, def.glyph, def.Tint, def.Name, def.kanji + "（" + def.ja + "）", def.CategoryName);
            NLUi.Label(p, "About", def.About, 17f, Ink, _font).lineSpacing = 6f;
            if (PlayerIllness.Has(def.id))
            {
                var card = Card(p, def.Tint);
                NLUi.Label(card, "Now", Loc.T("Bạn đang mắc bệnh này.", "You have this now.", "今かかっています。"), 17f, def.Tint, _font, FontStyles.Bold);
            }
            Section(p, Loc.T("Nguyên nhân", "Cause", "原因"), def.Cause, def.Tint);
            Section(p, Loc.T("Biểu hiện trong game", "In the game", "症状"), def.Effects, def.Tint);
            Section(p, Loc.T("Cách chữa đúng", "How it is really treated", "治し方"), def.Cure, def.Tint);
            Section(p, Loc.T("Bác sĩ sẽ làm gì", "What the doctor does", "病院では"), def.Clinic, def.Tint);
            Section(p, Loc.T("Phòng ngừa", "Prevention", "予防"), def.Prevent, def.Tint);
            var meds = def.medicines.Select(m => IllnessCatalog.Load().Medicine(m)).Where(m => m != null).ToList();
            if (meds.Count > 0)
                Section(p, Loc.T("Thuốc ở Hibari Mart", "At Hibari Mart", "ひばりマートで"),
                    string.Join("\n", meds.Select(m => $"<b>{m.ja}</b> ({m.kanji}) — {m.Name} · {Loc.Yen(m.price)}")), def.Tint);
            var say = Card(p, def.Tint);
            NLUi.Label(say, "SayHead", Loc.T("Nói với bác sĩ", "Tell the doctor", "先生に言う"), 14f, InkSoft, _font, FontStyles.Bold);
            NLUi.Label(say, "Say", def.sayJa, 22f, Ink, _font, FontStyles.Bold);
        }

        private void BuildMedicines(RectTransform p, Color color)
        {
            Hero(p, "薬", color, Loc.T("Tủ thuốc (くすり)", "The medicine shelf (くすり)", "くすり"), "ドラッグ・くすり", ChapterName(Chapter.Health));
            NLUi.Label(p, "Lead", Loc.T("Thuốc không kê đơn chỉ làm dịu triệu chứng và rút ngắn bệnh một chút — không thay được nghỉ ngơi hay bác sĩ. Đọc kỹ cách dùng; thuốc dị ứng loại cũ gây buồn ngủ. Kháng sinh không chữa được cảm hay cúm.",
                "Over-the-counter medicine eases symptoms and shortens an illness a little — it does not replace rest or a doctor. Read the label; older antiallergy pills make you drowsy. Antibiotics do not cure colds or flu.",
                "市販薬は症状をやわらげるだけです。"), 16f, Ink, _font).lineSpacing = 6f;
            foreach (var m in IllnessCatalog.Load().medicines)
            {
                var card = Card(p, color);
                var picture = ItemIcons.Get(m.id); // the product's own picture (Resources/Items/<id>.png), as on the shelf
                if (picture != null)
                {
                    var icon = new GameObject("MedicineIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
                    icon.transform.SetParent(card, false);
                    icon.sprite = picture; icon.preserveAspect = true; icon.raycastTarget = false;
                    var iconSize = icon.GetComponent<LayoutElement>();
                    iconSize.preferredWidth = 96f; iconSize.preferredHeight = 96f;
                }
                NLUi.Label(card, "Name", $"<b>{m.ja}</b>  <color=#736655>{m.kanji}</color>   {Loc.Yen(m.price)}", 18f, Ink, _font);
                NLUi.Label(card, "Use", m.Name + " — " + m.Use, 15f, InkSoft, _font);
                var helps = IllnessCatalog.Load().illnesses.Where(i => i.medicines.Contains(m.id)).Select(i => i.Name).ToList();
                if (helps.Count > 0) NLUi.Label(card, "Helps", Loc.T("Giúp với: ", "Helps with: ", "効く病気：") + string.Join(", ", helps), 14f, color, _font, FontStyles.Bold);
            }
        }

        private List<Entry> CareerEntries()
        {
            var list = new List<Entry>();
            var color = ChapterColor(Chapter.Careers);
            foreach (var career in CareerService.Catalog.careers)
            {
                var c = career;
                bool current = CareerService.Current?.id == career.id;
                list.Add(new Entry
                {
                    id = career.id, glyph = string.IsNullOrEmpty(career.nameJa) ? "職" : career.nameJa.Substring(0, 1), color = color,
                    title = career.Name, subtitle = career.Workplace, badge = current ? Loc.T("nghề của bạn", "yours", "あなたの仕事") : null,
                    build = p => BuildCareer(p, c, color),
                });
            }
            return list;
        }

        private void BuildCareer(RectTransform p, CareerDefinition career, Color color)
        {
            Hero(p, string.IsNullOrEmpty(career.nameJa) ? "職" : career.nameJa.Substring(0, 1), color, career.Name, career.nameJa, career.Workplace);
            NLUi.Label(p, "About", career.Description, 17f, Ink, _font).lineSpacing = 6f;
            Section(p, Loc.T("Lịch làm", "Schedule", "勤務"), career.Schedule + " · " + Loc.F("mở từ cấp {0}", "opens at level {0}", "レベル{0}から", CareerService.Catalog.unlockLevel), color);
            var ladder = new List<string>();
            int n = 0;
            foreach (var l in career.levels) ladder.Add($"{++n}. <b>{l.Title}</b>  <color=#736655>{l.titleJa} · {Loc.Yen(l.salary)}</color>");
            Section(p, Loc.T("Nấc thang chung", "Shared ladder", "共通のはしご"), string.Join("\n", ladder), color);
            foreach (var b in career.branches)
            {
                var lines = new List<string> { "<i>" + b.Description + "</i>" };
                int k = career.levels.Length;
                foreach (var l in b.levels) lines.Add($"{++k}. <b>{l.Title}</b>  <color=#736655>{l.titleJa} · {Loc.Yen(l.salary)}</color>");
                Section(p, "↘ " + b.Name, string.Join("\n", lines), color);
            }
            var buttons = NLUi.Group(p, "Buttons", false, 10f, TextAnchor.MiddleLeft, false);
            PageButton(buttons, "OpenCareer", Loc.T("Mở cửa sổ nghề nghiệp", "Open the career window", "キャリア画面へ"), color, () => { Close(); CareerUI.Open(); });
            if (career.id == "medical") PageButton(buttons, "OpenDoctorPath", Loc.T("Con đường bác sĩ ngoài đời", "The real doctor path", "医師への道"), new Color(0.25f, 0.45f, 0.7f), () => Show(Chapter.Health, "doctor_path"));
        }

        private List<Entry> MusicEntries()
        {
            var list = new List<Entry>();
            var color = ChapterColor(Chapter.Music);
            foreach (var track in MusicCatalog.Load().tracks)
            {
                var t = track;
                bool pinned = AudioPrefs.PinnedMusic == track.key;
                bool playing = Soundscape.Instance != null && Soundscape.Instance.MusicKey == track.key;
                list.Add(new Entry
                {
                    id = track.key, glyph = "♪", color = color, title = track.Title, subtitle = track.Where,
                    badge = pinned ? Loc.T("ghim", "pinned", "固定") : playing ? Loc.T("đang phát", "playing", "再生中") : null,
                    build = p => BuildTrack(p, t, color),
                });
            }
            return list;
        }

        private void BuildTrack(RectTransform p, MusicTrack track, Color color)
        {
            Hero(p, "♪", color, track.Title, track.ja, ChapterName(Chapter.Music));
            Section(p, Loc.T("Không khí", "Mood", "雰囲気"), track.Mood, color);
            Section(p, Loc.T("Phát ở đâu", "Where it plays", "流れる場所"), track.Where, color);
            Section(p, Loc.T("Nguồn", "Credit", "クレジット"), track.credit, color);
            var buttons = NLUi.Group(p, "Buttons", false, 10f, TextAnchor.MiddleLeft, false);
            // The music room: piano, guitar, melodies, music theory, the gallery (MusicStudioUI).
            PageButton(buttons, "MusicStudio", Loc.T("Phòng nhạc · piano, guitar, nhạc lý", "Music room · piano, guitar, theory", "音楽室 · ピアノ・ギター・理論"), color, () => { Close(); MusicStudioUI.Open(); });
            bool listening = Soundscape.PreviewKey == track.key;
            PageButton(buttons, "Listen", listening ? Loc.T("■ Dừng nghe thử", "■ Stop", "■ 停止") : Loc.T("▶ Nghe thử", "▶ Listen", "▶ 試聴"), color, () =>
            {
                Soundscape.PreviewKey = listening ? null : track.key;
                Show(Chapter.Music, track.key);
            });
            bool pinned = AudioPrefs.PinnedMusic == track.key;
            PageButton(buttons, "Pin", pinned ? Loc.T("Bỏ ghim", "Unpin", "固定を外す") : Loc.T("Ghim phát mọi nơi", "Play everywhere", "どこでも流す"), Color.Lerp(color, Ink, 0.35f), () =>
            {
                AudioPrefs.PinnedMusic = pinned ? "" : track.key;
                Show(Chapter.Music, track.key);
            });
            if (!AudioPrefs.MusicOn) NLUi.Label(p, "MusicOff", Loc.T("Nhạc nền đang tắt (Cài đặt → Âm thanh).", "Music is off (Settings → Sound).", "BGMはオフです。"), 14f, new Color(0.7f, 0.3f, 0.25f), _font, FontStyles.Italic);
            if (!string.IsNullOrEmpty(AudioPrefs.PinnedMusic) && !pinned)
                NLUi.Label(p, "PinnedNote", Loc.F("Đang ghim: {0} — bỏ ghim để mỗi nơi có nhạc riêng.", "Pinned: {0} — unpin to give each place its own music.", "固定中：{0}", MusicCatalog.Load().Track(AudioPrefs.PinnedMusic)?.Title ?? AudioPrefs.PinnedMusic), 14f, InkSoft, _font, FontStyles.Italic);
        }
    }
}
