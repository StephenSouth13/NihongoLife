using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// Flick basketball: swipe the ball up towards the hoop. The ball flies on a real arc and shrinks as it goes away, the rim
    /// can bounce it, and a clean shot through the middle scores double. After a few baskets the hoop starts to move.
    /// </summary>
    public sealed class HoopShotGame : ArcadeGame
    {
        private const float Gravity = 1500f, BallSize = 104f, RimHalf = 64f, MaxThrow = 1500f, SwipeFactor = 0.62f;
        private const float RimFromTop = 196f, FloorY = 80f, SwishWidth = 20f;

        private RectTransform _ball, _rim, _net, _board, _shadow;
        private TextMeshProUGUI _hint;
        private Vector2 _ballPos, _velocity, _pressStart;
        private float _pressTime, _hoopX, _hoopDir = 1f, _farScale = 1f;
        private bool _flying, _pressing, _touchedRim, _scored;
        private int _baskets;

        public override string GameId => "hoop_shot";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(1f, 0.55f, 0.2f);
        protected override (string vi, string en, string ja) HowTo => (
            "Vuốt quả bóng lên phía rổ rồi thả tay. Vào giữa vành được gấp đôi điểm. Càng về sau rổ càng di chuyển.",
            "Swipe the ball up towards the hoop and let go. A clean shot scores double. Later the hoop starts to move.",
            "ボールを ゴールへ むけて スワイプ！ まんなかに はいると 2ばい。ゴールは だんだん うごきます。");

        public bool Flying => _flying;
        public int Baskets => _baskets;
        private float Width => Stage.rect.width;
        private float RimY => Stage.rect.height - RimFromTop;

        protected override void BuildStage(RectTransform stage)
        {
            var court = UiKit.Image(stage, "Court", new Color(0.09f, 0.12f, 0.2f));
            UiKit.Fill(court.rectTransform);
            var floor = UiKit.Image(stage, "Floor", new Color(0.55f, 0.34f, 0.18f), UiKit.Rounded(8));
            Bottom(floor.rectTransform, new Vector2(0.5f, 0f), new Vector2(stage.rect.width, FloorY + 40f));
            floor.rectTransform.anchoredPosition = new Vector2(stage.rect.width * 0.5f, 0f);

            _board = Part(stage, "Backboard", new Color(0.93f, 0.95f, 0.98f), new Vector2(230f, 140f), 10);
            var square = UiKit.Image(_board, "Square", new Color(0.9f, 0.25f, 0.2f), UiKit.Rounded(4));
            Bottom(square.rectTransform, new Vector2(0.5f, 0f), new Vector2(92f, 66f));
            square.rectTransform.anchoredPosition = new Vector2(115f, 24f);
            var inner = UiKit.Image(square.transform, "Inner", new Color(0.93f, 0.95f, 0.98f), UiKit.Rounded(3));
            UiKit.Fill(inner.rectTransform, 6f, 6f, 6f, 6f);

            _net = Part(stage, "Net", new Color(1f, 1f, 1f, 0.55f), new Vector2(RimHalf * 1.7f, 58f), 12);
            _rim = Part(stage, "Rim", new Color(0.98f, 0.36f, 0.16f), new Vector2(RimHalf * 2f + 14f, 12f), 6);

            _shadow = Part(stage, "Shadow", new Color(0f, 0f, 0f, 0.35f), new Vector2(BallSize, 22f), 11);
            var ball = UiKit.Image(stage, "Ball", new Color(0.96f, 0.5f, 0.14f), UiKit.Circle);
            _ball = ball.rectTransform;
            Bottom(_ball, new Vector2(0.5f, 0.5f), new Vector2(BallSize, BallSize));
            foreach (float angle in new[] { 0f, 90f })
            {
                var seam = UiKit.Image(_ball, "Seam", new Color(0.3f, 0.14f, 0.06f));
                UiKit.Pin(seam.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, BallSize, 4f);
                seam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            _hint = UiKit.Label(stage, "Hint", "Vuốt bóng lên để ném", "Swipe the ball up to shoot", "ボールを うえに スワイプ", 20f, new Color(1f, 1f, 1f, 0.75f), FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_hint.rectTransform, 0f, 14f, stage.rect.width, 30f);
            _hoopX = stage.rect.width * 0.5f;
            PlaceHoop();
            ResetBall();
        }

        private static void Bottom(RectTransform rect, Vector2 pivot, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = pivot;
            rect.sizeDelta = size;
        }

        private static RectTransform Part(RectTransform parent, string name, Color color, Vector2 size, int round)
        {
            var image = UiKit.Image(parent, name, color, UiKit.Rounded(round));
            Bottom(image.rectTransform, new Vector2(0.5f, 0.5f), size);
            return image.rectTransform;
        }

        private void PlaceHoop()
        {
            _board.anchoredPosition = new Vector2(_hoopX, RimY + 72f);
            _rim.anchoredPosition = new Vector2(_hoopX, RimY);
            _net.anchoredPosition = new Vector2(_hoopX, RimY - 30f);
        }

        private void ResetBall()
        {
            _ballPos = new Vector2(Width * 0.5f, FloorY + BallSize * 0.5f);
            _velocity = Vector2.zero;
            _flying = _pressing = _touchedRim = _scored = false;
            _farScale = 1f;
            DrawBall();
        }

        private void DrawBall()
        {
            _ball.anchoredPosition = _ballPos;
            _ball.localScale = Vector3.one * _farScale;
            _shadow.anchoredPosition = new Vector2(_ballPos.x, FloorY - 6f);
            _shadow.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, Mathf.Clamp01((_ballPos.y - FloorY) / 400f));
            // The net sits in front of a ball that has come down through the hoop.
            if (_flying && _velocity.y < 0f && _ballPos.y < RimY) _net.SetAsLastSibling();
            else _ball.SetAsLastSibling();
        }

        protected override void OnRoundStart()
        {
            _baskets = 0;
            _hoopX = Width * 0.5f;
            ResetBall();
        }

        protected override void OnTick(float dt)
        {
            MoveHoop(dt);
            if (_flying) StepBall(dt);
            else ReadSwipe();
        }

        private void MoveHoop(float dt)
        {
            if (_baskets < 3) return;
            float speed = Mathf.Min(70f + (_baskets - 3) * 22f, 300f);
            float margin = RimHalf + 120f;
            _hoopX += _hoopDir * speed * dt;
            if (_hoopX > Width - margin) { _hoopX = Width - margin; _hoopDir = -1f; }
            if (_hoopX < margin) { _hoopX = margin; _hoopDir = 1f; }
            PlaceHoop();
        }

        private void ReadSwipe()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;
            var canvas = Stage.GetComponentInParent<Canvas>();
            var view = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Stage, pointer.position.ReadValue(), view, out var local)) return;
            Vector2 at = local - Stage.rect.min;
            if (pointer.press.wasPressedThisFrame && Vector2.Distance(at, _ballPos) < BallSize * 1.4f)
            {
                _pressing = true;
                _pressStart = at;
                _pressTime = Time.unscaledTime;
            }
            if (!_pressing) return;
            if (pointer.press.wasReleasedThisFrame)
            {
                _pressing = false;
                float held = Mathf.Max(Time.unscaledTime - _pressTime, 0.06f);
                Vector2 swipe = at - _pressStart;
                if (swipe.y > 24f) Throw(swipe / held * SwipeFactor);
            }
        }

        /// <summary>Throws the ball; also used by tests.</summary>
        public void Throw(Vector2 velocity)
        {
            if (_flying || State != Phase.Playing) return;
            _velocity = Vector2.ClampMagnitude(velocity, MaxThrow);
            _flying = true;
            _touchedRim = _scored = false;
            if (_hint != null) _hint.gameObject.SetActive(false);
        }

        /// <summary>The throw that drops through the middle of the hoop where it is now.</summary>
        public Vector2 PerfectThrow()
        {
            float rise = RimY + 110f - _ballPos.y;
            float up = Mathf.Sqrt(2f * Gravity * rise);
            float fall = Mathf.Sqrt(2f * 110f / Gravity);
            return new Vector2((_hoopX - _ballPos.x) / (up / Gravity + fall), up);
        }

        private void StepBall(float dt)
        {
            Vector2 before = _ballPos;
            _velocity.y -= Gravity * dt;
            _ballPos += _velocity * dt;
            _farScale = Mathf.Lerp(1f, 0.62f, Mathf.Clamp01((_ballPos.y - FloorY) / (RimY - FloorY)));
            float reach = BallSize * 0.5f * _farScale;
            if (!_scored && before.y >= RimY && _ballPos.y < RimY && _velocity.y < 0f)
            {
                float off = _ballPos.x - _hoopX;
                if (Mathf.Abs(off) <= RimHalf - reach * 0.55f) Score_(Mathf.Abs(off) <= SwishWidth && !_touchedRim);
                else if (Mathf.Abs(off) <= RimHalf + reach)
                {
                    _touchedRim = true;
                    _ballPos.y = RimY + 2f;
                    _velocity.y = Mathf.Abs(_velocity.y) * 0.45f;
                    _velocity.x += Mathf.Sign(off) * 150f;
                    Cue(GameAudioCue.Drop);
                }
            }
            DrawBall();
            if (_ballPos.y >= -BallSize && _ballPos.x >= -BallSize && _ballPos.x <= Width + BallSize) return;
            if (!_scored) Miss("skill.hoop_shot", null, null, _rim, costsLife: false, popText: Loc.T("Trượt!", "Missed!", "はずれ！"));
            ResetBall();
        }

        private void Score_(bool swish)
        {
            _scored = true;
            _baskets++;
            Hit("skill.hoop_shot", _rim, swish ? 200 : 100);
            if (swish) Pop(_board, "SWISH!", UiTheme.Gold);
            _velocity.x *= 0.2f;
        }
    }
}
