using NihongoLife.Audio;
using NihongoLife.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// エアホッケー: the puck flies over the table; the player's mallet (mouse, or arrow keys / WASD) hits it into the
    /// robot's goal at the top. Every goal scores; a goal against costs a life. The robot gets quicker as the round goes
    /// on. No learning content — it is a pure reflex game.
    /// </summary>
    public sealed class AirHockeyGame : ArcadeGame
    {
        private const float PuckRadius = 20f, MalletRadius = 34f, GoalHalf = 110f, MaxPuckSpeed = 900f;

        public override string GameId => "air_hockey";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(0.4f, 0.85f, 1f);
        protected override (string vi, string en, string ja) HowTo => (
            "Di chuột (hoặc phím mũi tên / WASD) để điều khiển cây vợt ở nửa dưới, đánh đĩa vào khung thành của robot ở trên. Ghi bàn được điểm; để lọt bàn là mất một mạng.",
            "Move the mouse (or arrow keys / WASD) to steer your mallet on the lower half and knock the puck into the robot's goal at the top. A goal scores; conceding one costs a life.",
            "マウス（または矢印キー / WASD）で下がわのマレットを うごかして、パックを 上のゴールに入れよう。ゴールで 得点、入れられると ライフが へります。");

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
            _robot = Disc(table.rectTransform, "Robot", new Color(0.95f, 0.4f, 0.45f), MalletRadius);
            _player = Disc(table.rectTransform, "Player", new Color(0.45f, 0.85f, 0.55f), MalletRadius);
            _puck = Disc(table.rectTransform, "Puck", Color.white, PuckRadius);
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
            else Miss("", "", "", _player);
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
