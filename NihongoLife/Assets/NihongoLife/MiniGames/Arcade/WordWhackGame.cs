using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// ことばモグラ: nine burrows, Japanese words pop out of them for a moment. The banner names what to find
    /// (meaning in the interface language, or the reading) and the player whacks the matching word before it ducks
    /// back. A wrong word or a target that escapes costs a life; the burrows get quicker as the round goes on.
    /// </summary>
    public sealed class WordWhackGame : ArcadeGame
    {
        public sealed class Hole
        {
            public RectTransform Rect;
            public Image Card;
            public TextMeshProUGUI Label;
            public KanaPair Pair;
            public float Age, Life;
        }

        public override string GameId => "word_whack";
        protected override Color Accent => new Color(0.5f, 0.85f, 0.4f);
        protected override (string vi, string en, string ja) HowTo => (
            "Từ tiếng Nhật chui ra khỏi hang — bấm đúng từ khớp với gợi ý ở trên trước khi nó thụt vào. Sai hoặc để nó chạy mất là mất một mạng.",
            "Japanese words pop out of the burrows — click the one that matches the clue on top before it ducks back. A wrong word or an escape costs a life.",
            "あなから ことばが でてくる！ 上のヒントに合うものを、もぐる前にクリック。まちがえると ライフが へります。");

        private readonly List<Hole> _holes = new();
        private List<KanaPair> _deck = new();
        private KanaPair _target;
        private float _spawn;
        private int _sinceTarget;
        private RectTransform _banner;
        private TextMeshProUGUI _clue;

        public KanaPair Target => _target;
        public IReadOnlyList<Hole> Holes => _holes;

        /// <summary>Needs at least four words that have a clue in the interface language (picture sets have none in Japanese).</summary>
        protected override bool SetFits(KanaPairSet set) => base.SetFits(set) && set.pairs.Count(p => !string.IsNullOrEmpty(p.Clue(set.type))) >= 4;

        protected override void BuildStage(RectTransform stage)
        {
            var meadow = UiKit.Image(stage, "Meadow", new Color(0.17f, 0.3f, 0.16f), UiKit.Rounded(18));
            UiKit.Fill(meadow.rectTransform);
            var fade = UiKit.Image(meadow.transform, "Fade", new Color(0.7f, 1f, 0.55f, 0.16f), UiKit.VerticalFade);
            UiKit.Fill(fade.rectTransform);

            var banner = UiKit.Card(stage, "Clue", UiKit.Alpha(UiTheme.Ink, 0.92f), 18, 0.3f, false);
            UiKit.Pin(banner.rectTransform, new Vector2(0.5f, 1f), 0f, -14f, 560f, 84f);
            _banner = banner.rectTransform;
            UiKit.Place(UiKit.Label(banner.transform, "Ask", "Tìm từ", "Find the word", "さがせ！", 15f, Accent, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 6f, 560f, 22f);
            _clue = UiKit.Label(banner.transform, "Text", "", 34f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_clue.rectTransform, 20f, 28f, 520f, 52f);

            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    var card = UiKit.Card(stage, $"Burrow_{row}{col}", new Color(0.09f, 0.06f, 0.04f), 36, 0.2f);
                    var rect = card.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.2f + col * 0.3f, 0.12f + row * 0.2f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(230f, 84f);
                    rect.anchoredPosition = Vector2.zero;
                    var label = UiKit.Label(rect, "Word", "", 36f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center);
                    UiKit.Fill(label.rectTransform, 8f, 2f, 8f, 2f);
                    var hole = new Hole { Rect = rect, Card = card, Label = label };
                    var button = card.gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.None;
                    button.onClick.AddListener(() => Whack(hole));
                    _holes.Add(hole);
                }
        }

        protected override void OnRoundStart()
        {
            _deck = Set.pairs.Where(p => !string.IsNullOrEmpty(p.Clue(Set.type))).ToList();
            NextTarget();
            _spawn = 0.3f;
        }

        private float Difficulty => Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds));

        protected override void OnTick(float dt)
        {
            _spawn -= dt;
            if (_spawn <= 0f)
            {
                Raise(null);
                _spawn = Mathf.Lerp(0.95f, 0.55f, Difficulty);
            }
            foreach (var hole in _holes)
            {
                if (hole.Pair == null) continue;
                hole.Age += dt;
                hole.Rect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(hole.Age / 0.12f));
                if (hole.Age < hole.Life) continue;
                var gone = hole.Pair;
                Empty(hole);
                if (gone == _target)
                {
                    Miss(Id(gone), null, Note(gone, null), hole.Rect);
                    if (State == Phase.Playing) NextTarget();
                    return;
                }
            }
        }

        /// <summary>Raises a word from a free burrow (the target when it is not out yet and is due).</summary>
        public Hole Raise(KanaPair forced)
        {
            var free = _holes.Where(h => h.Pair == null).ToList();
            if (free.Count == 0 || _deck.Count == 0) return null;
            bool targetOut = _holes.Any(h => h.Pair == _target);
            var pair = forced ?? (!targetOut && (_sinceTarget >= 2 || Random.value < 0.4f) ? _target : _deck[Random.Range(0, _deck.Count)]);
            _sinceTarget = pair == _target ? 0 : _sinceTarget + 1;
            var hole = free[Random.Range(0, free.Count)];
            hole.Pair = pair;
            hole.Age = 0f;
            hole.Life = Mathf.Lerp(2.1f, 1.2f, Difficulty);
            hole.Label.text = pair.a;
            hole.Card.color = UiTheme.Paper;
            return hole;
        }

        /// <summary>Whacks a burrow (mouse click, or tests).</summary>
        public void Whack(Hole hole)
        {
            if (State != Phase.Playing || hole == null || hole.Pair == null) return;
            var pair = hole.Pair;
            if (pair == _target)
            {
                Hit(Id(pair), hole.Rect, 100 + Mathf.RoundToInt((1f - hole.Age / hole.Life) * 60f));
                Empty(hole);
                NextTarget();
            }
            else
            {
                Miss(Id(_target), Id(pair), Note(_target, pair), hole.Rect);
                Empty(hole);
            }
        }

        /// <summary>Tests: whacks the target if a burrow shows it (raising it first when needed).</summary>
        public bool WhackTarget()
        {
            var hole = _holes.FirstOrDefault(h => h.Pair == _target) ?? Raise(_target);
            if (hole == null) return false;
            Whack(hole);
            return true;
        }

        private static void Empty(Hole hole)
        {
            hole.Pair = null;
            hole.Label.text = "";
            hole.Card.color = new Color(0.09f, 0.06f, 0.04f);
            hole.Rect.localScale = Vector3.one;
        }

        private void NextTarget()
        {
            if (_deck.Count == 0) return;
            KanaPair next;
            do next = _deck[Random.Range(0, _deck.Count)]; while (next == _target && _deck.Count > 1);
            _target = next;
            _clue.text = _target.Clue(Set.type);
        }

        private static string Id(KanaPair pair) => pair?.targetId;

        private string Note(KanaPair expected, KanaPair chosen) =>
            chosen == null ? $"{expected.a} = {expected.Clue(Set.type)}" : $"{expected.a} = {expected.Clue(Set.type)}  ≠  {chosen.a}";
    }
}
