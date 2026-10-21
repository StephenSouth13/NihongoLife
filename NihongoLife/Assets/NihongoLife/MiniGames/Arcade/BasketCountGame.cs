using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// かずバスケ: a number is called in Japanese (kana reading; the Japanese interface shows it in kanji, e.g. 二十四),
    /// three hoops with digits slide across the court, and the player throws the ball into the hoop with the right
    /// number. Numbers grow from 1–10 to tens and hundreds as the streak builds; close distractors (14 vs 40,
    /// 300 vs 3) keep it a listening/reading test, not a guess.
    /// </summary>
    public sealed class BasketCountGame : ArcadeGame
    {
        public sealed class Hoop
        {
            public int Value;
            public RectTransform Rect;
            public float Speed;
        }

        public override string GameId => "basket_count";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(1f, 0.55f, 0.2f);
        protected override (string vi, string en, string ja) HowTo => (
            "Đọc số được gọi bằng tiếng Nhật, rồi bấm vào rổ ghi đúng chữ số để ném bóng. Ném nhầm mất một mạng. Số lớn dần: 1–10, hàng chục, hàng trăm.",
            "Read the number called in Japanese, then click the hoop with the matching digits to shoot. A wrong shot costs a life. Numbers grow: 1–10, tens, hundreds.",
            "日本語の数を読んで、同じ数字のゴールをクリックしてシュート！まちがえるとライフがへります。");

        private readonly List<Hoop> _hoops = new();
        private RectTransform _court, _ball, _callCard;
        private TextMeshProUGUI _call, _sub;
        private int _number;
        private bool _throwing;

        public int Number => _number;
        public IReadOnlyList<Hoop> Hoops => _hoops;
        public bool Throwing => _throwing;

        protected override void BuildStage(RectTransform stage)
        {
            var court = UiKit.Image(stage, "Court", new Color(0.55f, 0.32f, 0.18f), UiKit.Rounded(18));
            UiKit.Fill(court.rectTransform);
            var lines = UiKit.Image(court.transform, "Key", new Color(1f, 1f, 1f, 0.08f), UiKit.Rounded(80));
            UiKit.Pin(lines.rectTransform, new Vector2(0.5f, 0f), 0f, -60f, 520f, 300f);
            _court = court.rectTransform;

            var call = UiKit.Card(stage, "Call", UiKit.Alpha(UiTheme.Ink, 0.94f), 20, 0.3f, false);
            UiKit.Pin(call.rectTransform, new Vector2(0.5f, 0f), 0f, 20f, 520f, 120f);
            _callCard = call.rectTransform;
            _call = UiKit.Label(call.transform, "Number", "", 46f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_call.rectTransform, 0f, 10f, 520f, 64f);
            _sub = UiKit.Label(call.transform, "Sub", "", 17f, UiTheme.TextSoft, FontStyles.Italic, TextAlignmentOptions.Center);
            UiKit.Place(_sub.rectTransform, 0f, 78f, 520f, 28f);

            var ball = UiKit.Image(stage, "Ball", new Color(1f, 0.55f, 0.15f), UiKit.Circle);
            _ball = ball.rectTransform;
            _ball.anchorMin = _ball.anchorMax = new Vector2(0.5f, 0f);
            _ball.sizeDelta = new Vector2(56f, 56f);
            ResetBall();
        }

        private void ResetBall() => _ball.anchoredPosition = new Vector2(330f, 80f);

        protected override void OnRoundStart() => NextNumber();

        private int Level => CorrectCount < 5 ? 0 : CorrectCount < 12 ? 1 : 2;

        private void NextNumber()
        {
            int previous = _number;
            for (int i = 0; i < 8 && (_number == previous); i++)
                _number = Level == 0 ? Random.Range(1, 11) : Level == 1 ? Random.Range(11, 100) : Random.Range(100, 1000);
            // Japanese interface: the kanji numeral (reading practice); others: the kana reading with romaji below.
            _call.text = Loc.IsJapanese ? JapaneseNumber.ToKanji(_number) : JapaneseNumber.ToReading(_number);
            _sub.text = Loc.IsJapanese ? JapaneseNumber.ToReading(_number) : Loc.T("Rổ nào là số này?", "Which hoop is this number?", "");
            BuildHoops();
            StartCoroutine(Bounce(_callCard, 0.06f));
        }

        private IEnumerable<int> Distractors(int n)
        {
            var list = new List<int>();
            if (n < 10) list.AddRange(new[] { n + 1, n - 1, n + 2, 10 - n, n * 10 });
            else if (n < 100) list.AddRange(new[] { (n % 10) * 10 + n / 10, n + 10, n - 10, n + 1, n - 1 });
            else list.AddRange(new[] { n + 100, n - 100, n + 10, n - 10, (n % 10) * 100 + (n / 10 % 10) * 10 + n / 100 });
            return list.Where(v => v > 0 && v != n && v < 1000).Distinct().OrderBy(_ => Random.value).Take(2);
        }

        private void BuildHoops()
        {
            foreach (var h in _hoops) if (h.Rect != null) Destroy(h.Rect.gameObject);
            _hoops.Clear();
            var values = Distractors(_number).Append(_number).OrderBy(_ => Random.value).ToList();
            while (values.Count < 3) values.Add(_number + values.Count * 7);
            for (int i = 0; i < values.Count; i++)
            {
                var board = UiKit.Card(_court, "Hoop_" + values[i], UiTheme.Paper, 14, 0.3f);
                var rect = board.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(180f, 150f);
                rect.anchoredPosition = new Vector2(180f + i * 380f, -30f - (i % 2) * 40f);
                var digits = UiKit.Label(rect, "Digits", values[i].ToString(), 46f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(digits.rectTransform, 0f, 14f, 180f, 60f);
                // Backboard square, orange rim and a net hanging below the board.
                var square = UiKit.Image(rect, "Square", new Color(0.95f, 0.35f, 0.2f, 0.9f), UiKit.Stroke(6));
                UiKit.Pin(square.rectTransform, new Vector2(0.5f, 0f), 0f, 26f, 70f, 46f);
                var rim = UiKit.Image(rect, "Rim", new Color(0.97f, 0.45f, 0.12f), UiKit.Rounded(4));
                UiKit.Pin(rim.rectTransform, new Vector2(0.5f, 0f), 0f, -2f, 120f, 9f);
                for (int n = 0; n < 5; n++)
                {
                    var strand = UiKit.Image(rect, "Net" + n, new Color(1f, 1f, 1f, 0.75f), null);
                    var sr = strand.rectTransform;
                    sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0f); sr.pivot = new Vector2(0.5f, 1f);
                    sr.sizeDelta = new Vector2(2f, 34f);
                    sr.anchoredPosition = new Vector2(-48f + n * 24f, -4f);
                    sr.localRotation = Quaternion.Euler(0f, 0f, (n - 2) * -7f);
                }
                var hoop = new Hoop { Value = values[i], Rect = rect, Speed = Random.Range(90f, 160f) * (Random.value < 0.5f ? -1f : 1f) * (1f + Level * 0.35f) };
                var button = board.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => Shoot(hoop));
                _hoops.Add(hoop);
            }
        }

        protected override void OnTick(float dt)
        {
            float width = _court.rect.width;
            foreach (var h in _hoops)
            {
                var p = h.Rect.anchoredPosition;
                p.x += h.Speed * dt;
                if (p.x < 100f) { p.x = 100f; h.Speed = Mathf.Abs(h.Speed); }
                if (p.x > width - 100f) { p.x = width - 100f; h.Speed = -Mathf.Abs(h.Speed); }
                h.Rect.anchoredPosition = p;
            }
        }

        /// <summary>Throws at a hoop (click, or tests).</summary>
        public void Shoot(Hoop hoop)
        {
            if (State != Phase.Playing || _throwing || hoop == null) return;
            StartCoroutine(Throw(hoop));
        }

        /// <summary>Tests: the hoop with the called number.</summary>
        public Hoop RightHoop => _hoops.First(h => h.Value == _number);
        public Hoop WrongHoop => _hoops.First(h => h.Value != _number);

        private IEnumerator Throw(Hoop hoop)
        {
            _throwing = true;
            Vector3 from = _ball.position;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
            {
                if (hoop.Rect == null) break;
                Vector3 to = hoop.Rect.position + Vector3.down * 110f;
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y += Mathf.Sin(t * Mathf.PI) * 180f;
                _ball.position = p;
                _ball.localRotation = Quaternion.Euler(0f, 0f, -t * 540f);
                yield return null;
            }
            string target = "number." + _number;
            if (hoop.Value == _number)
            {
                Hit(target, hoop.Rect, 100 + Level * 40);
                ResetBall();
                _throwing = false;
                if (State == Phase.Playing) NextNumber();
            }
            else
            {
                Miss(target, "number." + hoop.Value, $"{JapaneseNumber.ToReading(_number)} = {_number}  " + Loc.F("(không phải {0})", "(not {0})", "（{0}ではない）", hoop.Value), hoop.Rect);
                StartCoroutine(Shake(hoop.Rect));
                ResetBall();
                _throwing = false;
            }
        }
    }
}
