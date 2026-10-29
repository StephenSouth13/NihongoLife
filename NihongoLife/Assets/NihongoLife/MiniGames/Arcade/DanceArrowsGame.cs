using System.Collections.Generic;
using System.Linq;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// ダンスアロー: arrows scroll up four lanes towards a glowing line; the player presses the matching arrow key (or
    /// clicks the pad) as an arrow crosses it — the closer to the line, the more points. An arrow that slips past costs
    /// a life. Faster and denser as the round goes on. No learning content: it is the dance machine's own game.
    /// </summary>
    public sealed class DanceArrowsGame : ArcadeGame
    {
        public sealed class Arrow
        {
            public int Lane;
            public RectTransform Rect;
            public float Y;
        }

        private const float Window = 70f;
        private static readonly string[] Icons = { "arrow_back", "arrow_downward", "arrow_upward", "arrow_forward" };
        private static readonly Color[] LaneColors =
        {
            new Color(1f, 0.45f, 0.75f), new Color(0.4f, 0.9f, 1f), new Color(1f, 0.85f, 0.35f), new Color(0.55f, 0.9f, 0.5f),
        };

        public override string GameId => "dance_arrows";
        protected override bool UsesContentSets => false;
        protected override int StartLives => 5;
        protected override Color Accent => new Color(1f, 0.45f, 0.75f);
        protected override (string vi, string en, string ja) HowTo => (
            "Mũi tên chạy lên — bấm đúng phím mũi tên (hoặc A S W D, hoặc bấm ô) khi nó chạm vạch sáng. Càng sát vạch càng nhiều điểm; để lọt là mất một mạng.",
            "Arrows scroll up — press the matching arrow key (or A S W D, or click the pad) as one crosses the glowing line. The closer, the more points; a miss costs a life.",
            "やじるしが のぼってくる！ ひかる線に かさなったら、同じ向きのキー（矢印キー / A S W D / クリック）をおそう。のがすと ライフが へります。");

        private readonly List<Arrow> _arrows = new();
        private RectTransform _lanes;
        private readonly RectTransform[] _pads = new RectTransform[4];
        private float _spawn, _lineY, _height;
        private int _lastLane = -1, _repeat;

        public IReadOnlyList<Arrow> Arrows => _arrows;

        protected override void BuildStage(RectTransform stage)
        {
            var floor = UiKit.Image(stage, "Floor", new Color(0.09f, 0.05f, 0.16f), UiKit.Rounded(18));
            UiKit.Fill(floor.rectTransform);
            _lanes = floor.rectTransform;
            for (int i = 0; i < 4; i++)
            {
                int lane = i;
                var strip = UiKit.Image(floor.transform, "Lane_" + i, UiKit.Alpha(LaneColors[i], 0.1f), UiKit.Rounded(6));
                var s = strip.rectTransform;
                s.anchorMin = new Vector2(0.28f + i * 0.1133f, 0f);
                s.anchorMax = new Vector2(0.28f + i * 0.1133f + 0.1f, 1f);
                s.offsetMin = s.offsetMax = Vector2.zero;
                var pad = UiKit.Card(floor.transform, "Pad_" + i, UiKit.Alpha(LaneColors[i], 0.35f), 16, 0.2f);
                var rect = pad.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.33f + i * 0.1133f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(86f, 86f);
                rect.anchoredPosition = new Vector2(0f, -76f);
                var icon = UiKit.Icon(rect, Icons[i], Color.white, 54f);
                UiKit.Pin(icon.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 54f, 54f);
                var button = pad.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => Press(lane));
                _pads[i] = rect;
            }
        }

        protected override void OnRoundStart()
        {
            _height = _lanes.rect.height;
            _lineY = _height - 76f;
            _spawn = 0.4f;
        }

        private float Difficulty => Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds));

        protected override void OnTick(float dt)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Press(0);
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Press(1);
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Press(2);
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Press(3);
            }
            _spawn -= dt;
            if (_spawn <= 0f)
            {
                Spawn(NextLane());
                _spawn = Mathf.Lerp(0.8f, 0.42f, Difficulty);
            }
            float speed = Mathf.Lerp(250f, 430f, Difficulty);
            foreach (var arrow in _arrows.ToList())
            {
                arrow.Y += speed * dt;
                arrow.Rect.anchoredPosition = new Vector2(0f, arrow.Y);
                if (arrow.Y <= _lineY + Window) continue;
                Remove(arrow);
                Miss("", "", "", _pads[arrow.Lane]);
                if (State != Phase.Playing) return;
            }
        }

        private int NextLane()
        {
            int lane;
            do lane = Random.Range(0, 4); while (lane == _lastLane && _repeat >= 1);
            _repeat = lane == _lastLane ? _repeat + 1 : 0;
            _lastLane = lane;
            return lane;
        }

        /// <summary>Sends an arrow up a lane (tests, and the round itself).</summary>
        public Arrow Spawn(int lane)
        {
            var icon = UiKit.Icon(_lanes, Icons[lane], LaneColors[lane], 70f);
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.33f + lane * 0.1133f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(70f, 70f);
            rect.anchoredPosition = new Vector2(0f, -60f);
            var arrow = new Arrow { Lane = lane, Rect = rect, Y = -60f };
            _arrows.Add(arrow);
            return arrow;
        }

        /// <summary>Presses a lane's arrow (0 ← 1 ↓ 2 ↑ 3 →): hits the arrow closest to the line when it is inside the window, otherwise nothing happens.</summary>
        public void Press(int lane)
        {
            if (State != Phase.Playing || lane < 0 || lane > 3) return;
            var arrow = _arrows.Where(a => a.Lane == lane && Mathf.Abs(a.Y - _lineY) <= Window).OrderBy(a => Mathf.Abs(a.Y - _lineY)).FirstOrDefault();
            StartCoroutine(Bounce(_pads[lane], 0.2f, 0.15f));
            if (arrow == null) return;
            float accuracy = 1f - Mathf.Abs(arrow.Y - _lineY) / Window;
            Hit("", _pads[lane], 60 + Mathf.RoundToInt(accuracy * 90f));
            Remove(arrow);
        }

        private void Remove(Arrow arrow)
        {
            _arrows.Remove(arrow);
            if (arrow.Rect != null) Destroy(arrow.Rect.gameObject);
        }

        /// <summary>Tests: moves the first arrow onto the line.</summary>
        public Arrow PlaceOnLine(Arrow arrow)
        {
            arrow.Y = _lineY;
            arrow.Rect.anchoredPosition = new Vector2(0f, _lineY);
            return arrow;
        }
    }
}
