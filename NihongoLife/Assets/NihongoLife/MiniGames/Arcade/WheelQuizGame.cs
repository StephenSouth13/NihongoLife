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
    /// ルーレットクイズ: spin the prize wheel (click or Space), then answer a quick question about a Japanese word —
    /// the wheel's multiplier is what the answer is worth. A wrong answer costs a life. Content: any word set.
    /// </summary>
    public sealed class WheelQuizGame : ArcadeGame
    {
        private enum Mode { Ready, Spinning, Question }

        public static readonly int[] Segments = { 1, 2, 1, 3, 1, 2, 1, 5 };

        public override string GameId => "wheel_quiz";
        protected override Color Accent => new Color(1f, 0.6f, 0.3f);
        protected override (string vi, string en, string ja) HowTo => (
            "Bấm quay (hoặc Space) để quay bánh xe, rồi trả lời câu hỏi: chọn từ tiếng Nhật khớp với gợi ý. Số nhân trên bánh xe là điểm thưởng; sai là mất một mạng.",
            "Spin the wheel (click or Space), then answer: pick the Japanese word that matches the clue. The multiplier the wheel lands on is what the answer is worth; a wrong answer costs a life.",
            "ホイールを まわして（クリック / スペース）、ヒントに合うことばを えらぼう。とまった倍率が 点数に！まちがえると ライフが へります。");

        private readonly List<Button> _options = new();
        private readonly List<KanaPair> _choices = new();
        private List<KanaPair> _deck = new();
        private Mode _mode;
        private KanaPair _target;
        private RectTransform _wheel, _question;
        private Button _spin;
        private TextMeshProUGUI _clue, _multiplier;
        private float _angle, _speed;
        private int _lastMultiplier = 1;

        public int Multiplier => _lastMultiplier;
        public KanaPair Target => _target;
        public IReadOnlyList<KanaPair> Choices => _choices;
        public bool IsAsking => State == Phase.Playing && _mode == Mode.Question;

        /// <summary>Needs at least four words that have a clue in the interface language (picture sets have none in Japanese).</summary>
        protected override bool SetFits(KanaPairSet set) => base.SetFits(set) && set.pairs.Count(p => !string.IsNullOrEmpty(p.Clue(set.type))) >= 4;

        protected override void BuildStage(RectTransform stage)
        {
            var back = UiKit.Image(stage, "Back", new Color(0.16f, 0.08f, 0.06f), UiKit.Rounded(18));
            UiKit.Fill(back.rectTransform);
            var wheel = UiKit.Rect(stage, "Wheel");
            wheel.anchorMin = wheel.anchorMax = new Vector2(0.27f, 0.5f);
            wheel.pivot = new Vector2(0.5f, 0.5f);
            wheel.sizeDelta = new Vector2(330f, 330f);
            wheel.anchoredPosition = Vector2.zero;
            _wheel = wheel;
            var rim = UiKit.Image(wheel, "Rim", new Color(0.95f, 0.8f, 0.4f), UiKit.Circle);
            UiKit.Fill(rim.rectTransform, -10f, -10f, -10f, -10f);
            var colors = new[] { new Color(0.9f, 0.35f, 0.4f), new Color(0.35f, 0.7f, 0.95f), new Color(0.95f, 0.8f, 0.3f), new Color(0.5f, 0.85f, 0.5f) };
            for (int i = 0; i < Segments.Length; i++)
            {
                var slice = UiKit.Image(wheel, "Slice_" + i, colors[i % colors.Length], UiKit.Circle);
                UiKit.Fill(slice.rectTransform);
                slice.type = Image.Type.Filled;
                slice.fillMethod = Image.FillMethod.Radial360;
                slice.fillOrigin = (int)Image.Origin360.Top;
                slice.fillClockwise = true;
                slice.fillAmount = 1f / Segments.Length;
                slice.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -i * 360f / Segments.Length);
                float mid = (i + 0.5f) * 360f / Segments.Length * Mathf.Deg2Rad;
                var label = UiKit.Label(wheel, "Value_" + i, "×" + Segments[i], 44f, Segments[i] >= 3 ? Color.white : UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(90f, 60f);
                label.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(mid), Mathf.Cos(mid)) * 112f;
                label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -(i + 0.5f) * 360f / Segments.Length);
            }
            var hub = UiKit.Image(wheel, "Hub", new Color(0.2f, 0.12f, 0.08f), UiKit.Circle);
            UiKit.Pin(hub.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 60f, 60f);
            var pointer = UiKit.Icon(stage, "expand_more", UiTheme.Gold, 70f);
            pointer.rectTransform.anchorMin = pointer.rectTransform.anchorMax = new Vector2(0.27f, 0.5f);
            pointer.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pointer.rectTransform.sizeDelta = new Vector2(70f, 70f);
            pointer.rectTransform.anchoredPosition = new Vector2(0f, 190f);

            _multiplier = UiKit.Label(stage, "Multiplier", "", 40f, Accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Pin(_multiplier.rectTransform, new Vector2(0.27f, 0f), 0f, 14f, 330f, 56f);
            _spin = UiKit.Button(stage, "Spin", "Quay! (Space)", "Spin! (Space)", "まわす！（スペース）", Spin, UiKit.Kind.Primary, "refresh", 22f, 18);
            UiKit.Pin(_spin.GetComponent<RectTransform>(), new Vector2(0.72f, 0.5f), 0f, 0f, 320f, 76f);

            var card = UiKit.Card(stage, "Question", UiKit.Alpha(UiTheme.Ink, 0.94f), 20, 0.3f, false);
            UiKit.Pin(card.rectTransform, new Vector2(0.72f, 0.5f), 0f, 0f, 470f, 400f);
            _question = card.rectTransform;
            UiKit.Place(UiKit.Label(card.transform, "Ask", "Từ nào có nghĩa là", "Which word is", "どれ？", 15f, Accent, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 12f, 470f, 22f);
            _clue = UiKit.Label(card.transform, "Clue", "", 32f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_clue.rectTransform, 20f, 38f, 430f, 50f);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var button = UiKit.Button(card.transform, "Option_" + i, "", () => Answer(index), UiKit.Kind.Secondary, null, 28f, 14);
                UiKit.Place(button.GetComponent<RectTransform>(), 30f, 100f + i * 68f, 410f, 58f);
                _options.Add(button);
            }
            _question.gameObject.SetActive(false);
        }

        protected override void OnRoundStart()
        {
            _deck = Set.pairs.Where(p => !string.IsNullOrEmpty(p.Clue(Set.type))).ToList();
            ShowReady();
        }

        private void ShowReady()
        {
            _mode = Mode.Ready;
            _question.gameObject.SetActive(false);
            _spin.gameObject.SetActive(true);
            _multiplier.text = Loc.T("Quay để chọn số nhân!", "Spin for the multiplier!", "まわして倍率をきめよう！");
        }

        protected override void OnTick(float dt)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Spin();
            if (_mode != Mode.Spinning) return;
            _angle += _speed * dt;
            _speed = Mathf.Max(0f, _speed - 260f * dt);
            _wheel.localRotation = Quaternion.Euler(0f, 0f, _angle);
            if (_speed > 0f) return;
            _lastMultiplier = Segments[SegmentUnderPointer()];
            _multiplier.text = $"×{_lastMultiplier}";
            Ask();
        }

        /// <summary>Starts the wheel (button, Space, or tests).</summary>
        public void Spin() => Spin(Random.Range(620f, 900f));

        /// <summary>Starts the wheel at a given angular speed in degrees per second (a short spin for tests).</summary>
        public void Spin(float degreesPerSecond)
        {
            if (State != Phase.Playing || _mode != Mode.Ready) return;
            _mode = Mode.Spinning;
            _spin.gameObject.SetActive(false);
            _speed = degreesPerSecond;
            _multiplier.text = "…";
        }

        private int SegmentUnderPointer()
        {
            float a = ((_angle % 360f) + 360f) % 360f;
            return Mathf.FloorToInt(a / (360f / Segments.Length)) % Segments.Length;
        }

        private void Ask()
        {
            if (_deck.Count < 4) { Finish(false); return; }
            _mode = Mode.Question;
            _target = _deck[Random.Range(0, _deck.Count)];
            _choices.Clear();
            _choices.Add(_target);
            foreach (var p in _deck.OrderBy(_ => Random.value))
                if (_choices.Count < 4 && p != _target && p.a != _target.a) _choices.Add(p);
            var order = _choices.OrderBy(_ => Random.value).ToList();
            _choices.Clear();
            _choices.AddRange(order);
            _clue.text = _target.Clue(Set.type);
            for (int i = 0; i < _options.Count; i++) UiKit.SetLabel(_options[i], _choices[i].a);
            _question.gameObject.SetActive(true);
        }

        /// <summary>Answers with option <paramref name="index"/> (click, or tests).</summary>
        public void Answer(int index)
        {
            if (State != Phase.Playing || _mode != Mode.Question || index < 0 || index >= _choices.Count) return;
            var chosen = _choices[index];
            var at = _options[index].GetComponent<RectTransform>();
            if (chosen == _target) Hit(_target.targetId, at, 100 * _lastMultiplier);
            else Miss(_target.targetId, chosen.targetId, $"{_target.a} = {_target.Clue(Set.type)}  ≠  {chosen.a}", at);
            if (State == Phase.Playing) ShowReady();
        }

        /// <summary>Tests: the index of the right option while a question is open.</summary>
        public int CorrectIndex => _choices.IndexOf(_target);
    }
}
