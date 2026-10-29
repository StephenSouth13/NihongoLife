using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// エアホッケー: a match against the robot. The puck flies over the table; the player's mallet (mouse, or arrow keys /
    /// WASD) knocks it into the robot's goal (the red mouth at the top); the player's own goal is the green mouth at the
    /// bottom. First to <see cref="WinGoals"/> goals wins; conceding as many goals as there are hearts (3) loses; when the
    /// time runs out the one ahead wins (level = a draw). The score is on the table. The robot gets quicker as the round
    /// goes on. No learning content — it is a pure reflex game.
    /// </summary>
    public sealed class AirHockeyGame : ArcadeGame
    {
        private const float PuckRadius = 20f, MalletRadius = 34f, GoalHalf = 110f, MaxPuckSpeed = 900f;
        public const int WinGoals = 5;

        public enum Outcome { Playing, Won, Lost, Draw }
        public int PlayerGoals => CorrectCount;
        public int RobotGoals => WrongCount;
        public Outcome Result => State != Phase.Finished ? Outcome.Playing
            : PlayerGoals >= WinGoals || (Lives > 0 && PlayerGoals > RobotGoals) ? Outcome.Won
            : Lives <= 0 || RobotGoals > PlayerGoals ? Outcome.Lost : Outcome.Draw;

        protected override string OverText => Result switch
        {
            Outcome.Won => "かち！\n<size=40%>" + Loc.F("Bạn thắng {0} – {1}", "You win {0} – {1}", "{0} – {1} で かち", PlayerGoals, RobotGoals) + "</size>",
            Outcome.Lost => "まけ…\n<size=40%>" + Loc.F("Robot thắng {1} – {0}", "The robot wins {1} – {0}", "{1} – {0} で まけ", PlayerGoals, RobotGoals) + "</size>",
            _ => "ひきわけ\n<size=40%>" + Loc.F("Hòa {0} – {1}", "A draw {0} – {1}", "{0} – {1} ひきわけ", PlayerGoals, RobotGoals) + "</size>",
        };

        public override string GameId => "air_hockey";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(0.4f, 0.85f, 1f);
        protected override (string vi, string en, string ja) HowTo => (
            "Chuột / mũi tên / WASD điều khiển vợt xanh. Đánh đĩa vào khung đỏ của robot. Ghi 5 bàn trước là THẮNG; lọt khung xanh 3 lần (hết 3 tim) là THUA. Hết giờ: ai nhiều bàn hơn thắng.",
            "Mouse / arrows / WASD steer the green mallet. Hit the puck into the robot's red goal. First to 5 goals WINS; let 3 into your green goal (3 hearts) and you LOSE. Time up: more goals wins.",
            "マウス / 矢印 / WASD で みどりの マレット。赤い ゴールに 5点 先に 入れたら かち。みどりの ゴールに 3回 入れられたら まけ。");

        private RectTransform _table, _puck, _player, _robot;
        private Vector2 _puckPos, _puckVel, _playerPos, _playerPrev, _robotPos;
        private float _width, _height, _serve;

        public Vector2 PuckPosition => _puckPos;
        public Vector2 TableSize => new Vector2(_width, _height);

        protected override void BuildStage(RectTransform stage)
        {
            var table = UiKit.Image(stage, "Table", new Color(0.07f, 0.18f, 0.3f), UiKit.Rounded(24));
            UiKit.Fill(table.rectTransform);
            _table = table.rectTransform;
            var glow = UiKit.Image(table.transform, "Glow", new Color(0.4f, 0.85f, 1f, 0.16f), UiKit.VerticalFade);
            UiKit.Fill(glow.rectTransform);
            var centre = UiKit.Image(table.transform, "Centre", UiKit.Alpha(Color.white, 0.25f), UiKit.Rounded(2));
            centre.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            centre.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            centre.rectTransform.offsetMin = new Vector2(14f, -2f);
            centre.rectTransform.offsetMax = new Vector2(-14f, 2f);
            var ring = UiKit.Image(table.transform, "Ring", UiKit.Alpha(Color.white, 0.18f), UiKit.Ring);
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ring.rectTransform.sizeDelta = Vector2.one * 150f;
            ring.rectTransform.anchoredPosition = Vector2.zero;
            // The two goal mouths (the puck scores only between them) and the score, so the match reads at a glance.
            GoalMouth(table.rectTransform, "RobotGoal", true, new Color(0.95f, 0.4f, 0.45f), () => "ROBOT  ゴール");
            GoalMouth(table.rectTransform, "PlayerGoal", false, new Color(0.45f, 0.85f, 0.55f), () => Loc.T("KHUNG CỦA BẠN", "YOUR GOAL", "あなたの ゴール"));
            _scoreboard = UiKit.Label(table.transform, "Scoreboard", ScoreText, 30f, UiKit.Alpha(Color.white, 0.85f), TMPro.FontStyles.Bold, TMPro.TextAlignmentOptions.MidlineLeft);
            _scoreboard.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _scoreboard.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _scoreboard.rectTransform.pivot = new Vector2(0f, 0.5f);
            _scoreboard.rectTransform.sizeDelta = new Vector2(330f, 110f);
            _scoreboard.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            _scoreboard.raycastTarget = false;
            _robot = Disc(table.rectTransform, "Robot", new Color(0.95f, 0.4f, 0.45f), MalletRadius);
            _player = Disc(table.rectTransform, "Player", new Color(0.45f, 0.85f, 0.55f), MalletRadius);
            _puck = Disc(table.rectTransform, "Puck", Color.white, PuckRadius);
        }

        private TMPro.TextMeshProUGUI _scoreboard;

        private string ScoreText() =>
            $"<color=#7CE0A0>{Loc.T("BẠN", "YOU", "あなた")} {PlayerGoals}</color>  –  <color=#FF8A90>{RobotGoals} ROBOT</color>\n<size=55%>" +
            Loc.F("Ghi {0} bàn trước là thắng", "First to {0} goals wins", "{0}点 先取で かち", WinGoals) + "</size>";

        private void RefreshScore() { if (_scoreboard != null) _scoreboard.text = ScoreText(); }

        private void GoalMouth(RectTransform table, string name, bool top, Color color, System.Func<string> label)
        {
            var mouth = UiKit.Image(table, name, color, UiKit.Rounded(4)).rectTransform;
            mouth.anchorMin = mouth.anchorMax = new Vector2(0.5f, top ? 1f : 0f);
            mouth.pivot = new Vector2(0.5f, 0.5f);
            mouth.sizeDelta = new Vector2(GoalHalf * 2f, 14f);
            mouth.anchoredPosition = Vector2.zero;
            var text = UiKit.Label(table, name + "Label", label, 15f, UiKit.Alpha(color, 0.9f), TMPro.FontStyles.Bold, TMPro.TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, top ? 1f : 0f);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.sizeDelta = new Vector2(GoalHalf * 2f + 120f, 24f);
            text.rectTransform.anchoredPosition = new Vector2(0f, top ? -24f : 24f);
            text.raycastTarget = false;
        }

        private static RectTransform Disc(RectTransform parent, string name, Color color, float radius)
        {
            var rect = UiKit.Image(parent, name, color, UiKit.Circle).rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * radius * 2f;
            return rect;
        }

        protected override void OnRoundStart()
        {
            _width = _table.rect.width > 10f ? _table.rect.width : 760f;
            _height = _table.rect.height > 10f ? _table.rect.height : 540f;
            _playerPos = _playerPrev = new Vector2(_width * 0.5f, _height * 0.18f);
            _robotPos = new Vector2(_width * 0.5f, _height * 0.82f);
            RefreshScore();
            Serve(-1f);
        }

        private float Difficulty => Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds));

        private void Serve(float direction)
        {
            _puckPos = new Vector2(_width * 0.5f, _height * 0.5f);
            _puckVel = Vector2.zero;
            _serve = 0.9f;
            _lastDirection = direction;
            Draw();
        }

        private float _lastDirection;

        protected override void OnTick(float dt)
        {
            MovePlayer(dt);
            MoveRobot(dt);
            if (_serve > 0f)
            {
                _serve -= dt;
                if (_serve <= 0f) _puckVel = new Vector2(Random.Range(-160f, 160f), _lastDirection * 300f);
                Draw();
                return;
            }
            _puckPos += _puckVel * dt;
            _puckVel *= 1f - 0.12f * dt;
            if (_puckPos.x < PuckRadius) { _puckPos.x = PuckRadius; _puckVel.x = Mathf.Abs(_puckVel.x); }
            if (_puckPos.x > _width - PuckRadius) { _puckPos.x = _width - PuckRadius; _puckVel.x = -Mathf.Abs(_puckVel.x); }
            BounceMallet(_playerPos, (_playerPos - _playerPrev) / Mathf.Max(dt, 0.001f));
            BounceMallet(_robotPos, Vector2.zero);
            bool inGoal = Mathf.Abs(_puckPos.x - _width * 0.5f) <= GoalHalf;
            if (_puckPos.y > _height - PuckRadius)
            {
                if (inGoal) { Goal(true); return; }
                _puckPos.y = _height - PuckRadius;
                _puckVel.y = -Mathf.Abs(_puckVel.y);
            }
            if (_puckPos.y < PuckRadius)
            {
                if (inGoal) { Goal(false); return; }
                _puckPos.y = PuckRadius;
                _puckVel.y = Mathf.Abs(_puckVel.y);
            }
            Draw();
        }

        private void Goal(bool player)
        {
            if (player) Hit("", _robot, 150);
            else Miss("", "", "", _player, true, "ゴールされた！");
            RefreshScore();
            if (State == Phase.Playing && PlayerGoals >= WinGoals) { Finish(true); return; } // first to five
            if (State == Phase.Playing) Serve(player ? 1f : -1f);
        }

        private void BounceMallet(Vector2 mallet, Vector2 malletVelocity)
        {
            Vector2 delta = _puckPos - mallet;
            float min = PuckRadius + MalletRadius;
            if (delta.sqrMagnitude >= min * min || delta.sqrMagnitude < 0.0001f) return;
            Vector2 normal = delta.normalized;
            _puckPos = mallet + normal * min;
            float speed = Mathf.Clamp(Mathf.Max(_puckVel.magnitude, 380f) + malletVelocity.magnitude * 0.6f, 380f, MaxPuckSpeed);
            _puckVel = normal * speed;
            Cue(GameAudioCue.UiTick);
        }

        private void MovePlayer(float dt)
        {
            _playerPrev = _playerPos;
            if (ArcadePointer.TryGet(_table, out Vector2 pointer) && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f) _playerPos = pointer;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 move = new Vector2(
                    (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed ? 1f : 0f),
                    (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed ? 1f : 0f));
                _playerPos += move * 520f * dt;
            }
            SetPlayer(_playerPos);
        }

        /// <summary>Puts the player's mallet somewhere on its half of the table (mouse, keys, tests).</summary>
        public void SetPlayer(Vector2 position)
        {
            _playerPos = new Vector2(Mathf.Clamp(position.x, MalletRadius, _width - MalletRadius), Mathf.Clamp(position.y, MalletRadius, _height * 0.5f - MalletRadius));
        }

        private void MoveRobot(float dt)
        {
            float speed = Mathf.Lerp(190f, 420f, Difficulty);
            Vector2 goal = _puckPos.y > _height * 0.55f || _puckVel.y > 0f
                ? new Vector2(_puckPos.x, Mathf.Clamp(_puckPos.y + 24f, _height * 0.55f, _height - MalletRadius))
                : new Vector2(_width * 0.5f, _height * 0.82f);
            _robotPos = Vector2.MoveTowards(_robotPos, goal, speed * dt);
        }

        private void Draw()
        {
            _puck.anchoredPosition = _puckPos;
            _player.anchoredPosition = _playerPos;
            _robot.anchoredPosition = _robotPos;
        }

        /// <summary>Tests: puts the puck in front of the player's own goal, heading straight in, with the mallet out of the way.</summary>
        public void ShootAtPlayerGoal()
        {
            _serve = 0f;
            SetPlayer(new Vector2(_width * 0.9f, _height * 0.18f));
            _puckPos = new Vector2(_width * 0.5f, _height * 0.2f);
            _puckVel = new Vector2(0f, -600f);
        }

        /// <summary>Tests: puts the puck a short way in front of the robot's goal, heading straight in, and moves the robot away.</summary>
        public void ShootAtRobotGoal()
        {
            _serve = 0f;
            _robotPos = new Vector2(_width * 0.15f, _height * 0.82f);
            _puckPos = new Vector2(_width * 0.5f, _height * 0.8f);
            _puckVel = new Vector2(0f, 600f);
        }
    }
}
