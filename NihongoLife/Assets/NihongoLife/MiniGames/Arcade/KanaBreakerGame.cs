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
    /// かなブレイク: a breakout field. Twelve bricks carry kana; the banner shows a romaji reading and only the brick
    /// with that kana breaks (the others bounce the ball back). A broken brick comes back with a new kana. Dropping the
    /// ball costs a life. Hiragana and katakana sets only.
    /// </summary>
    public sealed class KanaBreakerGame : ArcadeGame
    {
        public sealed class Brick
        {
            public KanaPair Pair;
            public RectTransform Rect;
            public Image Card;
            public TextMeshProUGUI Label;
        }

        private const float BallRadius = 12f, PaddleWidth = 150f, PaddleHeight = 18f, PaddleY = 38f;

        public override string GameId => "kana_breaker";
        protected override Color Accent => new Color(0.95f, 0.5f, 0.35f);
        protected override bool SetFits(KanaPairSet set) => set != null && set.pairs.Count >= 12 && set.AvailableInCurrentLanguage
            && (set.type == KanaPairType.HiraganaRomaji || set.type == KanaPairType.KatakanaRomaji);
        protected override (string vi, string en, string ja) HowTo => (
            "Di chuột (hoặc phím ← →) để đỡ bóng và đập vỡ viên gạch có chữ kana đọc đúng như gợi ý ở trên. Gạch sai chỉ làm bóng nảy lại; để rơi bóng mất một mạng. Space để phóng bóng.",
            "Move the mouse (or ← →) to bounce the ball and break the brick whose kana reads like the clue on top. Other bricks only bounce it back; dropping the ball costs a life. Space launches.",
            "マウス（または ← →）でボールを うちかえして、上のよみ方と同じかなの ブロックをこわそう。ちがうブロックは はねかえるだけ。おとすと ライフが へります。スペースで はっしゃ。");

        private readonly List<Brick> _bricks = new();
        private List<KanaPair> _deck = new();
        private KanaPair _target;
        private RectTransform _field, _paddle, _ball;
        private TextMeshProUGUI _clue;
        private Vector2 _ballPos, _ballVel, _brickSize;
        private float _paddleX, _width, _height, _stuck;

        public KanaPair Target => _target;
        public IReadOnlyList<Brick> Bricks => _bricks;
        public Vector2 BallPosition => _ballPos;
        public bool BallLaunched => _stuck <= 0f;

        protected override void BuildStage(RectTransform stage)
        {
            var back = UiKit.Image(stage, "Back", new Color(0.12f, 0.07f, 0.1f), UiKit.Rounded(18));
            UiKit.Fill(back.rectTransform);
            _field = back.rectTransform;
            var banner = UiKit.Card(stage, "Clue", UiKit.Alpha(UiTheme.Ink, 0.92f), 16, 0.3f, false);
            UiKit.Pin(banner.rectTransform, new Vector2(0.5f, 1f), 0f, -8f, 420f, 58f);
            UiKit.Place(UiKit.Label(banner.transform, "Ask", "Phá viên đọc là", "Break the one read as", "これを こわそう", 13f, Accent, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 4f, 420f, 18f);
            _clue = UiKit.Label(banner.transform, "Text", "", 30f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_clue.rectTransform, 0f, 20f, 420f, 36f);
            _paddle = Corner(UiKit.Card(_field, "Paddle", UiTheme.Gold, 9, 0.2f, false).rectTransform, new Vector2(PaddleWidth, PaddleHeight));
            _ball = Corner(UiKit.Image(_field, "Ball", Color.white, UiKit.Circle).rectTransform, Vector2.one * BallRadius * 2f);
        }

        private static RectTransform Corner(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        protected override void OnRoundStart()
        {
            _deck = Set.pairs.Where(p => !string.IsNullOrEmpty(p.b)).ToList();
            _width = _field.rect.width > 10f ? _field.rect.width : 960f;
            _height = _field.rect.height > 10f ? _field.rect.height : 540f;
            _brickSize = new Vector2((_width - 80f) / 6f - 8f, 54f);
            var shuffled = _deck.OrderBy(_ => Random.value).Take(12).ToList();
            for (int i = 0; i < shuffled.Count; i++)
            {
                int col = i % 6, row = i / 6;
                var card = UiKit.Card(_field, "Brick_" + i, Color.white, 10, 0.15f, false);
                var rect = Corner(card.rectTransform, _brickSize);
                rect.anchoredPosition = new Vector2(40f + (col + 0.5f) * (_brickSize.x + 8f), _height - 130f - row * (_brickSize.y + 10f));
                var label = UiKit.Label(rect, "Kana", "", 34f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Fill(label.rectTransform);
                var brick = new Brick { Rect = rect, Card = card, Label = label };
                SetBrick(brick, shuffled[i]);
                _bricks.Add(brick);
            }
            _paddleX = _width * 0.5f;
            PickTarget();
            Reset();
        }

        private float Difficulty => Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds));

        private void SetBrick(Brick brick, KanaPair pair)
        {
            brick.Pair = pair;
            brick.Label.text = pair.a;
            brick.Card.color = Color.HSVToRGB(Mathf.Abs(pair.a.GetHashCode() % 100) / 100f, 0.28f, 1f);
        }

        private void Reset()
        {
            _stuck = 0.8f;
            _ballVel = Vector2.zero;
            _ballPos = new Vector2(_paddleX, PaddleY + PaddleHeight * 0.5f + BallRadius);
            Draw();
        }

        /// <summary>Launches the ball from the paddle at once.</summary>
        public void Launch()
        {
            if (_stuck <= 0f) return;
            _stuck = 0f;
            float speed = Mathf.Lerp(330f, 520f, Difficulty);
            _ballVel = new Vector2(Random.Range(-0.5f, 0.5f), 1f).normalized * speed;
        }

        protected override void OnTick(float dt)
        {
            MovePaddle(dt);
            if (_stuck > 0f)
            {
                _stuck -= dt;
                _ballPos = new Vector2(_paddleX, PaddleY + PaddleHeight * 0.5f + BallRadius);
                if (_stuck <= 0f) { _stuck = 0.0001f; Launch(); }
                Draw();
                return;
            }
            float speed = Mathf.Lerp(330f, 520f, Difficulty);
            _ballVel = _ballVel.normalized * speed;
            _ballPos += _ballVel * dt;
            if (_ballPos.x < BallRadius) { _ballPos.x = BallRadius; _ballVel.x = Mathf.Abs(_ballVel.x); }
            if (_ballPos.x > _width - BallRadius) { _ballPos.x = _width - BallRadius; _ballVel.x = -Mathf.Abs(_ballVel.x); }
            if (_ballPos.y > _height - BallRadius) { _ballPos.y = _height - BallRadius; _ballVel.y = -Mathf.Abs(_ballVel.y); }
            float top = PaddleY + PaddleHeight * 0.5f;
            if (_ballVel.y < 0f && _ballPos.y - BallRadius <= top && _ballPos.y > PaddleY - PaddleHeight && Mathf.Abs(_ballPos.x - _paddleX) <= PaddleWidth * 0.5f + BallRadius)
            {
                _ballPos.y = top + BallRadius;
                float offset = Mathf.Clamp((_ballPos.x - _paddleX) / (PaddleWidth * 0.5f), -1f, 1f);
                _ballVel = new Vector2(offset * 0.85f, 1f).normalized * speed;
                Cue(NihongoLife.Audio.GameAudioCue.UiTick);
            }
            if (_ballPos.y < -BallRadius)
            {
                LoseBall();
                return;
            }
            Collide();
            Draw();
        }

        /// <summary>The ball got past the paddle: costs a life and a new ball waits on the paddle (the round does this at the bottom edge; tests call it directly).</summary>
        public void LoseBall()
        {
            if (State != Phase.Playing) return;
            Miss(Id(_target), null, Note(_target), _paddle);
            if (State == Phase.Playing) Reset();
        }

        private void MovePaddle(float dt)
        {
            if (ArcadePointer.TryGet(_field, out Vector2 pointer) && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f) _paddleX = pointer.x;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float dir = (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed ? 1f : 0f);
                _paddleX += dir * 620f * dt;
                if (keyboard.spaceKey.wasPressedThisFrame) Launch();
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) Launch();
            _paddleX = Mathf.Clamp(_paddleX, PaddleWidth * 0.5f, _width - PaddleWidth * 0.5f);
        }

        private void Collide()
        {
            foreach (var brick in _bricks)
            {
                Vector2 centre = brick.Rect.anchoredPosition, half = _brickSize * 0.5f;
                Vector2 delta = _ballPos - centre;
                Vector2 closest = new Vector2(Mathf.Clamp(delta.x, -half.x, half.x), Mathf.Clamp(delta.y, -half.y, half.y));
                if ((delta - closest).sqrMagnitude >= BallRadius * BallRadius) continue;
                if (Mathf.Abs(delta.x) / half.x > Mathf.Abs(delta.y) / half.y)
                {
                    _ballVel.x = Mathf.Abs(_ballVel.x) * Mathf.Sign(delta.x);
                    _ballPos.x = centre.x + Mathf.Sign(delta.x) * (half.x + BallRadius);
                }
                else
                {
                    _ballVel.y = Mathf.Abs(_ballVel.y) * Mathf.Sign(delta.y);
                    _ballPos.y = centre.y + Mathf.Sign(delta.y) * (half.y + BallRadius);
                }
                Break(brick);
                return;
            }
        }

        private void Break(Brick brick)
        {
            if (brick.Pair != _target)
            {
                StartCoroutine(Bounce(brick.Rect, 0.08f, 0.15f));
                return;
            }
            Hit(Id(brick.Pair), brick.Rect, 110);
            var fresh = _deck.Where(p => _bricks.All(b => b.Pair != p)).ToList();
            if (fresh.Count > 0) SetBrick(brick, fresh[Random.Range(0, fresh.Count)]);
            PickTarget();
        }

        /// <summary>Tests: breaks the target brick as if the ball had hit it.</summary>
        public bool BreakTarget()
        {
            var brick = _bricks.FirstOrDefault(b => b.Pair == _target);
            if (brick == null) return false;
            Break(brick);
            return true;
        }

        private void PickTarget()
        {
            if (_bricks.Count == 0) return;
            _target = _bricks[Random.Range(0, _bricks.Count)].Pair;
            _clue.text = _target.b;
        }

        private void Draw()
        {
            _paddle.anchoredPosition = new Vector2(_paddleX, PaddleY);
            _ball.anchoredPosition = _ballPos;
        }

        private static string Id(KanaPair pair) => pair?.targetId;

        private static string Note(KanaPair pair) => pair == null ? null : $"{pair.a} = {pair.b}";
    }
}
