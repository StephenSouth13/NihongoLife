using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// かなヘビ: a snake glides over a board that holds four kana tiles. The banner shows a romaji reading — steer the
    /// snake onto the kana that reads that way. The right tile makes it longer; a wrong tile, a wall or its own tail
    /// costs a life (and the snake starts again, short). Hiragana and katakana sets only: the reading is the same in
    /// every interface language.
    /// </summary>
    public sealed class KanaSnakeGame : ArcadeGame
    {
        public sealed class Tile
        {
            public Vector2Int Cell;
            public KanaPair Pair;
            public RectTransform Rect;
        }

        public const int Columns = 16, Rows = 9;

        public override string GameId => "kana_snake";
        protected override Color Accent => new Color(0.4f, 0.9f, 0.55f);
        protected override bool SetFits(KanaPairSet set) => base.SetFits(set) && (set.type == KanaPairType.HiraganaRomaji || set.type == KanaPairType.KatakanaRomaji);
        protected override (string vi, string en, string ja) HowTo => (
            "Điều khiển rắn bằng phím mũi tên (hoặc WASD) tới ô kana có cách đọc đúng với gợi ý ở trên. Ăn đúng thì dài ra; ăn sai, đâm tường hay cắn đuôi mất một mạng.",
            "Steer the snake with the arrow keys (or WASD) onto the kana that reads like the clue on top. The right tile makes it longer; a wrong tile, a wall or its own tail costs a life.",
            "矢印キー（またはWASD）でヘビを動かして、上のよみ方と同じかなを食べよう。正かいで長くなる。まちがい・かべ・しっぽは ライフが へります。");

        private readonly List<Vector2Int> _snake = new();
        private readonly List<Image> _body = new();
        private readonly List<Tile> _tiles = new();
        private List<KanaPair> _deck = new();
        private Vector2Int _dir = Vector2Int.right, _queued = Vector2Int.right;
        private KanaPair _target;
        private RectTransform _board, _grid;
        private TextMeshProUGUI _clue;
        private float _cell, _step;

        public KanaPair Target => _target;
        public IReadOnlyList<Tile> Tiles => _tiles;
        public IReadOnlyList<Vector2Int> Snake => _snake;
        public Vector2Int Direction => _dir;

        protected override void BuildStage(RectTransform stage)
        {
            var back = UiKit.Image(stage, "Back", new Color(0.05f, 0.12f, 0.09f), UiKit.Rounded(18));
            UiKit.Fill(back.rectTransform);
            _board = back.rectTransform;
            var banner = UiKit.Card(stage, "Clue", UiKit.Alpha(UiTheme.Ink, 0.92f), 16, 0.3f, false);
            UiKit.Pin(banner.rectTransform, new Vector2(0.5f, 1f), 0f, -8f, 420f, 58f);
            UiKit.Place(UiKit.Label(banner.transform, "Ask", "Ăn chữ đọc là", "Eat the kana read as", "これを 食べよう", 13f, Accent, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 4f, 420f, 18f);
            _clue = UiKit.Label(banner.transform, "Text", "", 30f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_clue.rectTransform, 0f, 20f, 420f, 36f);
        }

        protected override void OnRoundStart()
        {
            _deck = Set.pairs.Where(p => !string.IsNullOrEmpty(p.b)).ToList();
            float w = _board.rect.width > 10f ? _board.rect.width : 960f, h = _board.rect.height > 10f ? _board.rect.height - 70f : 480f;
            _cell = Mathf.Floor(Mathf.Min(w / Columns, h / Rows));
            var grid = UiKit.Image(_board, "Grid", new Color(0.08f, 0.2f, 0.14f), UiKit.Rounded(8));
            _grid = grid.rectTransform;
            _grid.anchorMin = _grid.anchorMax = new Vector2(0.5f, 0f);
            _grid.pivot = new Vector2(0.5f, 0f);
            _grid.sizeDelta = new Vector2(_cell * Columns, _cell * Rows);
            _grid.anchoredPosition = new Vector2(0f, Mathf.Max(6f, (h - _cell * Rows) * 0.5f + 6f));
            _grid.SetSiblingIndex(1);
            ResetSnake();
            for (int i = 0; i < 4; i++) AddTile();
            PickTarget();
        }

        private float Difficulty => Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds));

        protected override void OnTick(float dt)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Steer(Vector2Int.up);
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Steer(Vector2Int.down);
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Steer(Vector2Int.left);
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Steer(Vector2Int.right);
            }
            _step -= dt;
            if (_step > 0f) return;
            Advance();
            _step = Mathf.Lerp(0.4f, 0.22f, Difficulty);
        }

        /// <summary>Turns the snake (not straight back on itself).</summary>
        public void Steer(Vector2Int direction)
        {
            if (direction + _dir == Vector2Int.zero) return;
            _queued = direction;
        }

        /// <summary>Moves the snake one cell (the round does this on a timer; tests call it directly).</summary>
        public void Advance()
        {
            if (State != Phase.Playing) return;
            _dir = _queued;
            var next = _snake[0] + _dir;
            if (next.x < 0 || next.y < 0 || next.x >= Columns || next.y >= Rows || _snake.Contains(next))
            {
                Miss(Id(_target), null, Note(_target), _clue.rectTransform);
                if (State == Phase.Playing) ResetSnake();
                return;
            }
            _snake.Insert(0, next);
            var tile = _tiles.FirstOrDefault(t => t.Cell == next);
            bool grow = false;
            if (tile != null)
            {
                _tiles.Remove(tile);
                Destroy(tile.Rect.gameObject);
                if (tile.Pair == _target)
                {
                    Hit(Id(tile.Pair), _clue.rectTransform, 100);
                    grow = true;
                    AddTile();
                    PickTarget();
                }
                else
                {
                    Miss(Id(_target), Id(tile.Pair), $"{tile.Pair.a} = {tile.Pair.b}  ≠  {_target.b}", _clue.rectTransform);
                    AddTile();
                    if (State != Phase.Playing) return;
                }
            }
            if (!grow) _snake.RemoveAt(_snake.Count - 1);
            Draw();
        }

        private void ResetSnake()
        {
            _snake.Clear();
            var start = new Vector2Int(2, Rows / 2);
            for (int i = 0; i < 3; i++) _snake.Add(start + Vector2Int.left * i);
            _dir = _queued = Vector2Int.right;
            _step = 0.6f;
            Draw();
        }

        private void Draw()
        {
            while (_body.Count < _snake.Count)
            {
                var piece = UiKit.Card(_grid, "Body", UiTheme.Good, 10, 0f, false);
                _body.Add(piece);
            }
            for (int i = 0; i < _body.Count; i++)
            {
                bool used = i < _snake.Count;
                _body[i].gameObject.SetActive(used);
                if (!used) continue;
                Place(_body[i].rectTransform, _snake[i], 0.86f);
                _body[i].color = i == 0 ? UiTheme.Gold : Color.Lerp(UiTheme.Good, new Color(0.1f, 0.45f, 0.3f), Mathf.Clamp01(i / 14f));
            }
        }

        private void Place(RectTransform rect, Vector2Int cell, float scale)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * _cell * scale;
            rect.anchoredPosition = new Vector2((cell.x + 0.5f) * _cell, (cell.y + 0.5f) * _cell);
        }

        private Tile AddTile()
        {
            var free = new List<Vector2Int>();
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!_snake.Contains(c) && _tiles.All(t => t.Cell != c) && Vector2Int.Distance(c, _snake[0]) > 2.5f) free.Add(c);
                }
            var used = _tiles.Select(t => t.Pair).ToList();
            var choices = _deck.Where(p => !used.Contains(p)).ToList();
            if (free.Count == 0 || choices.Count == 0) return null;
            var pair = choices[Random.Range(0, choices.Count)];
            var card = UiKit.Card(_grid, "Kana_" + pair.a, UiTheme.Paper, 12, 0.15f, false);
            var label = UiKit.Label(card.transform, "Kana", pair.a, _cell * 0.62f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(label.rectTransform);
            var tile = new Tile { Cell = free[Random.Range(0, free.Count)], Pair = pair, Rect = card.rectTransform };
            Place(tile.Rect, tile.Cell, 0.92f);
            _tiles.Add(tile);
            return tile;
        }

        private void PickTarget()
        {
            if (_tiles.Count == 0) return;
            _target = _tiles[Random.Range(0, _tiles.Count)].Pair;
            _clue.text = _target.b;
        }

        private static string Id(KanaPair pair) => pair?.targetId;

        private static string Note(KanaPair pair) => pair == null ? null : $"{pair.a} = {pair.b}";
    }
}
