using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Music;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The easel of the Hibiki music shop: paint by numbers after a gallery masterpiece (Music/MusicCatalog.Artworks).
    /// The picture is reduced to a grid and a palette of six colours (k-means on the cells); every cell shows the number
    /// of its colour. Pick a colour, click a cell: the right colour fills it and every touching cell of the same number,
    /// a wrong one is counted. A finished painting raises "paint" (the painter's commission, job_art_commission).
    /// </summary>
    public sealed class PaintingUI : MonoBehaviour
    {
        private const int Columns = 12, PaletteSize = 6;
        private const string DoneKey = "NL.PaintingsDone";
        private static readonly Color Blank = new Color(0.94f, 0.93f, 0.9f);

        private static PaintingUI _instance;
        private TMP_FontAsset _font;
        private RectTransform _root, _grid, _side;
        private PlayerController _player;
        private bool _wasLocked;
        private Artwork _art;
        private int _rows, _selected, _mistakes, _artIndex = -1;
        private int[] _target;
        private bool[] _painted;
        private Color[] _palette = new Color[0];
        private Image[] _cells = new Image[0];
        private TextMeshProUGUI[] _numbers = new TextMeshProUGUI[0];
        private TextMeshProUGUI _status;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeInHierarchy;
        public static Artwork Current => _instance != null ? _instance._art : null;
        public static int CellCount => _instance != null && _instance._target != null ? _instance._target.Length : 0;
        public static int Mistakes => _instance != null ? _instance._mistakes : 0;
        public static int PaintedCount => _instance != null && _instance._painted != null ? _instance._painted.Count(p => p) : 0;
        public static bool Done => CellCount > 0 && PaintedCount == CellCount;
        public static int PaintingsDone => PlayerPrefs.GetInt(DoneKey, 0);
        /// <summary>The palette number (0-based) a cell must be painted with.</summary>
        public static int Target(int cell) => _instance._target[cell];

        private static PaintingUI Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("PaintingCanvas", 128);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<PaintingUI>();
                _instance._font = NLUi.ResolveFont();
                _instance.Build((RectTransform)canvas.transform);
                Loc.Watch(_instance, () => { if (IsOpen) _instance.Redraw(); });
                UiModalStack.Register(_instance, () => IsOpen, Close, "Painting");
                return _instance;
            }
        }

        /// <summary>Opens the easel with a painting (the next one in the gallery when none is given).</summary>
        public static void Open(string artworkId = null)
        {
            var ui = Instance;
            var all = MusicCatalog.Artworks;
            if (all.Length == 0) return;
            int index = artworkId != null ? System.Array.FindIndex(all, a => a.id == artworkId) : -1;
            if (index < 0) index = (ui._artIndex + 1) % all.Length;
            ui._artIndex = index;
            if (!ui._root.gameObject.activeSelf)
            {
                ui._root.gameObject.SetActive(true);
                ui._player = SceneRefs.Find<PlayerController>();
                if (ui._player != null) { ui._wasLocked = ui._player.InputLocked; ui._player.InputLocked = true; }
            }
            ui._root.SetAsLastSibling();
            ui.Load(all[index]);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            _instance._root.gameObject.SetActive(false);
            if (_instance._player != null) _instance._player.InputLocked = _instance._wasLocked;
            _instance._player = null;
        }

        public static void Select(int colour) { if (_instance != null) { _instance._selected = Mathf.Clamp(colour, 0, _instance._palette.Length - 1); _instance.Redraw(); } }

        /// <summary>Paints a cell with the selected colour: right → it and its same-number neighbours fill; wrong → a mistake.</summary>
        public static bool Fill(int cell) => _instance != null && _instance.Paint(cell, _instance._selected);

        // ─────────── Picture → grid ───────────

        private void Load(Artwork art)
        {
            _art = art;
            _mistakes = 0;
            _selected = 0;
            var tex = art.Picture;
            float aspect = tex != null ? tex.width / (float)tex.height : 1f;
            _rows = Mathf.Clamp(Mathf.RoundToInt(Columns / aspect), 6, 15);
            var colours = CellColours(tex, Columns, _rows);
            _palette = Palette(colours, PaletteSize);
            _target = colours.Select(c => Nearest(_palette, c)).ToArray();
            _painted = new bool[_target.Length];
            BuildGrid();
            Redraw();
        }

        /// <summary>Average colour of every cell, top row first. The texture need not be readable: it is drawn into a
        /// small render texture (4×4 samples per cell) and read back.</summary>
        private static Color[] CellColours(Texture2D tex, int cols, int rows)
        {
            var result = new Color[cols * rows];
            if (tex == null) { for (int i = 0; i < result.Length; i++) result[i] = Color.gray; return result; }
            const int k = 4;
            int w = cols * k, h = rows * k;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(w, h, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            var pixels = read.GetPixels();
            Destroy(read);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Color sum = Color.clear;
                    int y0 = (rows - 1 - r) * k; // texture rows start at the bottom
                    for (int y = 0; y < k; y++)
                        for (int x = 0; x < k; x++) sum += pixels[(y0 + y) * w + c * k + x];
                    var avg = sum / (k * k);
                    avg.a = 1f;
                    result[r * cols + c] = avg;
                }
            return result;
        }

        /// <summary>A few k-means rounds from luminance quantiles; the palette is ordered dark → light (number 1 darkest).</summary>
        private static Color[] Palette(Color[] colours, int size)
        {
            var sorted = colours.OrderBy(c => c.grayscale).ToArray();
            var centres = Enumerable.Range(0, size).Select(i => sorted[Mathf.Min(sorted.Length - 1, (int)((i + 0.5f) * sorted.Length / size))]).ToArray();
            for (int round = 0; round < 10; round++)
            {
                var sum = new Color[size];
                var count = new int[size];
                foreach (var c in colours) { int n = Nearest(centres, c); sum[n] += c; count[n]++; }
                for (int i = 0; i < size; i++) if (count[i] > 0) { centres[i] = sum[i] / count[i]; centres[i].a = 1f; }
            }
            var used = centres.Where(c => colours.Any(x => Nearest(centres, x) == System.Array.IndexOf(centres, c))).Distinct().OrderBy(c => c.grayscale).ToArray();
            return used.Length > 0 ? used : centres;
        }

        private static int Nearest(Color[] palette, Color c)
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < palette.Length; i++)
            {
                float dr = palette[i].r - c.r, dg = palette[i].g - c.g, db = palette[i].b - c.b;
                float d = dr * dr * 0.3f + dg * dg * 0.59f + db * db * 0.11f;
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return best;
        }

        // ─────────── Painting ───────────

        private bool Paint(int cell, int colour)
        {
            if (_target == null || cell < 0 || cell >= _target.Length || _painted[cell]) return false;
            if (_target[cell] != colour)
            {
                _mistakes++;
                _status.text = Loc.F("Ô này là số {0}, không phải số {1}.", "This cell is number {0}, not {1}.", "このマスは {0} 番です（{1} ではありません）。", _target[cell] + 1, colour + 1);
                return false;
            }
            // Flood: the cell and every touching cell of the same number.
            var stack = new Stack<int>();
            stack.Push(cell);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (_painted[i] || _target[i] != colour) continue;
                _painted[i] = true;
                int r = i / Columns, c = i % Columns;
                if (c > 0) stack.Push(i - 1);
                if (c < Columns - 1) stack.Push(i + 1);
                if (r > 0) stack.Push(i - Columns);
                if (r < _rows - 1) stack.Push(i + Columns);
            }
            if (Done) Complete();
            Redraw();
            return true;
        }

        private void Complete()
        {
            PlayerPrefs.SetInt(DoneKey, PaintingsDone + 1);
            NihongoLife.Progression.QuestService.Raise("paint", "art_easel");
            HudFeed.Post(Loc.F("Hoàn thành bức tranh theo «{0}»!", "Painting after “{0}” finished!", "「{0}」の模写が できました！", _art.title), HudFeed.Kind.Reward, 4f, "painting");
        }

        // ─────────── Layout ───────────

        private void Build(RectTransform canvas)
        {
            _root = new GameObject("Painting", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _root.SetParent(canvas, false);
            NLUi.Stretch(_root);
            _root.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.72f);
            var card = NLUi.Panel(_root, "Card", new Color(0.09f, 0.08f, 0.12f, 0.98f));
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(1180f, 720f);
            card.gameObject.AddComponent<HudFitRect>().Configure(0.96f, 0.94f, Vector2.zero, 0.4f);
            _grid = new GameObject("Canvas", typeof(RectTransform)).GetComponent<RectTransform>();
            _grid.SetParent(card, false);
            _grid.anchorMin = _grid.anchorMax = _grid.pivot = new Vector2(0f, 1f);
            _grid.anchoredPosition = new Vector2(30f, -30f);
            _grid.sizeDelta = new Vector2(700f, 660f);
            _side = NLUi.Group(card, "Side", true, 10f, TextAnchor.UpperLeft, true);
            _side.anchorMin = _side.anchorMax = _side.pivot = new Vector2(0f, 1f);
            _side.anchoredPosition = new Vector2(760f, -30f);
            _side.sizeDelta = new Vector2(380f, 660f);
            NLUi.CloseButton(card, _font, Close, 46f, 16f);
            _root.gameObject.SetActive(false);
        }

        private void BuildGrid()
        {
            UiKit.Clear(_grid);
            float size = Mathf.Floor(Mathf.Min(700f / Columns, 660f / _rows));
            _cells = new Image[_target.Length];
            _numbers = new TextMeshProUGUI[_target.Length];
            for (int i = 0; i < _target.Length; i++)
            {
                int cell = i;
                var rect = new GameObject("Cell_" + i, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
                rect.SetParent(_grid, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2((i % Columns) * size, -(i / Columns) * size);
                rect.sizeDelta = new Vector2(size - 1f, size - 1f);
                _cells[i] = rect.GetComponent<Image>();
                rect.GetComponent<Button>().onClick.AddListener(() => Fill(cell));
                _numbers[i] = NLUi.Label(rect, "Number", (_target[i] + 1).ToString(), Mathf.Clamp(size * 0.36f, 11f, 20f), new Color(0.35f, 0.33f, 0.3f), _font, FontStyles.Normal, TextAlignmentOptions.Center);
                NLUi.Stretch(_numbers[i].rectTransform);
                _numbers[i].raycastTarget = false;
            }
        }

        private void Redraw()
        {
            if (_target == null) return;
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].color = _painted[i] ? _palette[_target[i]] : _target[i] == _selected ? Color.Lerp(Blank, _palette[_selected], 0.22f) : Blank;
                _numbers[i].gameObject.SetActive(!_painted[i]);
            }
            UiKit.Clear(_side);
            NLUi.Label(_side, "Title", Loc.T("Giá vẽ — tô tranh theo số", "The easel — paint by numbers", "イーゼル — 数字でぬり絵"), 22f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(_side, "Art", $"<b>{_art.title}</b>\n<size=85%>{_art.artist} · {_art.date}</size>", 16f, NLUi.Text, _font);
            var thumb = new GameObject("Original", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement)).GetComponent<RawImage>();
            thumb.transform.SetParent(_side, false);
            thumb.texture = _art.Picture;
            float aspect = _art.Picture != null ? _art.Picture.width / (float)_art.Picture.height : 1f;
            var element = thumb.GetComponent<LayoutElement>();
            element.preferredHeight = Mathf.Min(170f, 300f / aspect);
            element.preferredWidth = element.preferredHeight * aspect;
            element.flexibleWidth = 0f;
            var swatches = NLUi.Group(_side, "Palette", false, 6f, TextAnchor.MiddleLeft, false);
            for (int i = 0; i < _palette.Length; i++)
            {
                int colour = i;
                int left = Enumerable.Range(0, _target.Length).Count(c => _target[c] == i && !_painted[c]);
                var b = NLUi.Button(swatches, "Colour_" + (i + 1), left == 0 ? "✓" : (i + 1).ToString(), _font, () => Select(colour), _palette[i], 18f,
                    _palette[i].grayscale > 0.55f ? NLUi.Ink : Color.white, 52f);
                NLUi.Size(b, 52f, i == _selected ? 64f : 52f);
            }
            _status = NLUi.Label(_side, "Status", Done
                ? Loc.F("Xong! {0} ô, {1} lần tô sai. Tranh đã vẽ: {2}.", "Done! {0} cells, {1} slips. Paintings finished: {2}.", "完成！{0}マス、ミス{1}。完成した絵：{2}", _target.Length, _mistakes, PaintingsDone)
                : Loc.F("Đã tô {0}/{1} ô · màu đang chọn: số {2} · tô sai: {3}", "Painted {0}/{1} · colour {2} selected · slips: {3}", "{0}/{1}マス · 色{2} · ミス{3}", PaintedCount, _target.Length, _selected + 1, _mistakes),
                16f, Done ? NLUi.Good : NLUi.Text, _font, Done ? FontStyles.Bold : FontStyles.Normal);
            NLUi.Label(_side, "Help", Loc.T("Chọn một màu rồi bấm vào ô có số đó: cả vùng cùng số liền nhau sẽ được tô.", "Pick a colour, then click a cell with its number: the whole touching area of that number fills.", "色を選んで、同じ番号のマスをクリック。つながったマスがぬられます。"), 14f, NLUi.Muted, _font);
            NLUi.Button(_side, "Next", Loc.T("Tranh khác →", "Another painting →", "ほかの絵 →"), _font, () => Open(), NLUi.CardHover, 16f, NLUi.Text, 44f);
            NLUi.Label(_side, "Licence", _art.licence, 11f, NLUi.Muted, _font);
        }
    }
}
