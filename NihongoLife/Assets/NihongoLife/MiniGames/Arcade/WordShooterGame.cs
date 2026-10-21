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
    /// ワードシューター: Japanese words float across the sky in bubbles; the banner shows the clue (meaning in the
    /// interface language, the reading, or a picture) and the player shoots the bubble that matches before it drifts
    /// away. Wrong shots and escaped targets cost a life; bubbles speed up as the round goes on.
    /// </summary>
    public sealed class WordShooterGame : ArcadeGame
    {
        public sealed class Bubble
        {
            public KanaPair Pair;
            public RectTransform Rect;
            public float Speed;
            public bool Gone;
        }

        public override string GameId => "word_shooter";
        protected override Color Accent => new Color(0.35f, 0.85f, 1f);
        protected override (string vi, string en, string ja) HowTo => (
            "Nhìn gợi ý ở trên rồi bấm vào bong bóng có từ tiếng Nhật đúng trước khi nó bay mất. Sai hoặc để lọt mất một mạng.",
            "Read the clue at the top, then click the bubble with the matching Japanese word before it floats away. A wrong shot or a miss costs a life.",
            "上のヒントを見て、合うことばの風船を、にげる前にクリック！まちがえると ライフが へります。");

        private readonly List<Bubble> _bubbles = new();
        private List<KanaPair> _deck = new();
        private KanaPair _target;
        private float _spawnTimer;
        private int _sinceTarget;
        private RectTransform _sky, _banner;
        private TextMeshProUGUI _clue;
        private Image _cluePicture;

        public KanaPair Target => _target;
        public IReadOnlyList<Bubble> Bubbles => _bubbles;

        protected override void BuildStage(RectTransform stage)
        {
            var sky = UiKit.Image(stage, "Sky", new Color(0.09f, 0.18f, 0.33f), UiKit.Rounded(18));
            UiKit.Fill(sky.rectTransform);
            var glow = UiKit.Image(sky.transform, "Glow", new Color(0.45f, 0.75f, 1f, 0.25f), UiKit.VerticalFade);
            UiKit.Fill(glow.rectTransform);
            _sky = sky.rectTransform;

            var banner = UiKit.Card(stage, "Clue", UiKit.Alpha(UiTheme.Ink, 0.92f), 18, 0.3f, false);
            UiKit.Pin(banner.rectTransform, new Vector2(0.5f, 1f), 0f, -14f, 640f, 96f);
            _banner = banner.rectTransform;
            UiKit.Place(UiKit.Label(banner.transform, "Ask", "Bắn từ có nghĩa là", "Shoot the word for", "これを うて！", 15f, Accent, FontStyles.Bold, TextAlignmentOptions.Center), 0f, 8f, 640f, 22f);
            _cluePicture = UiKit.Image(banner.transform, "Picture", Color.white);
            _cluePicture.preserveAspect = true;
            UiKit.Pin(_cluePicture.rectTransform, new Vector2(0f, 0.5f), 24f, -10f, 60f, 60f);
            _clue = UiKit.Label(banner.transform, "Text", "", 36f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_clue.rectTransform, 90f, 30f, 460f, 58f);
        }

        protected override void OnRoundStart()
        {
            _deck = Set.pairs.Where(p => !string.IsNullOrEmpty(p.Clue(Set.type)) || !string.IsNullOrEmpty(p.imageB)).ToList();
            NextTarget();
            _spawnTimer = 0.2f;
        }

        private float Difficulty => Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds));

        protected override void OnTick(float dt)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0f)
            {
                Spawn();
                _spawnTimer = Mathf.Lerp(1.25f, 0.65f, Difficulty);
            }
            float width = _sky.rect.width;
            foreach (var b in _bubbles.ToList())
            {
                if (b.Gone) continue;
                b.Rect.anchoredPosition += Vector2.left * b.Speed * dt;
                b.Rect.anchoredPosition += Vector2.up * Mathf.Sin((Elapsed + b.Speed) * 2.2f) * 12f * dt;
                if (b.Rect.anchoredPosition.x < -90f)
                {
                    Remove(b);
                    if (b.Pair == _target)
                    {
                        Miss(Id(_target), null, Note(_target, null), _banner);
                        if (State == Phase.Playing) NextTarget();
                    }
                }
            }
        }

        private Bubble Spawn(KanaPair forced = null)
        {
            if (_deck.Count == 0) return null;
            bool targetOnScreen = _bubbles.Any(b => !b.Gone && b.Pair == _target);
            KanaPair pair = forced ?? (!targetOnScreen && (_sinceTarget >= 2 || Random.value < 0.4f) ? _target : _deck[Random.Range(0, _deck.Count)]);
            _sinceTarget = pair == _target ? 0 : _sinceTarget + 1;
            var bubble = UiKit.Card(_sky, "Bubble", UiKit.Alpha(Color.white, 0.92f), 40, 0.25f);
            float size = Mathf.Clamp(64f + pair.a.Length * 22f, 110f, 220f);
            var rect = bubble.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, 78f);
            float height = _sky.rect.height;
            rect.anchoredPosition = new Vector2(_sky.rect.width + size * 0.5f, Random.Range(70f, height - 170f));
            var text = UiKit.Label(rect, "Word", pair.a, 34f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(text.rectTransform, 10f, 4f, 10f, 4f);
            var data = new Bubble { Pair = pair, Rect = rect, Speed = Mathf.Lerp(150f, 290f, Difficulty) * Random.Range(0.85f, 1.15f) };
            var button = bubble.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Shoot(data));
            _bubbles.Add(data);
            return data;
        }

        /// <summary>Shoots a bubble (mouse click, or tests).</summary>
        public void Shoot(Bubble bubble)
        {
            if (State != Phase.Playing || bubble == null || bubble.Gone) return;
            if (bubble.Pair == _target)
            {
                Hit(Id(_target), bubble.Rect, 100);
                Remove(bubble);
                NextTarget();
            }
            else
            {
                Miss(Id(_target), Id(bubble.Pair), Note(_target, bubble.Pair), bubble.Rect);
                StartCoroutine(Shake(bubble.Rect));
            }
        }

        /// <summary>Tests: shoots the target if it is on screen (spawning it when needed).</summary>
        public void ShootTarget()
        {
            var b = _bubbles.FirstOrDefault(x => !x.Gone && x.Pair == _target);
            if (b == null) b = Spawn(_target);
            Shoot(b);
        }

        /// <summary>Tests: shoots a wrong bubble.</summary>
        public void ShootWrong()
        {
            var b = _bubbles.FirstOrDefault(x => !x.Gone && x.Pair != _target);
            if (b == null) b = Spawn(_deck.First(p => p != _target));
            Shoot(b);
        }

        private void NextTarget()
        {
            if (_deck.Count == 0) return;
            var previous = _target;
            for (int i = 0; i < 6 && (_target == previous || _target == null); i++) _target = _deck[Random.Range(0, _deck.Count)];
            string clue = _target.Clue(Set.type);
            var picture = Set.type == KanaPairType.WordImage ? Picture(_target.imageB) : null;
            _cluePicture.sprite = picture;
            _cluePicture.gameObject.SetActive(picture != null);
            _clue.text = string.IsNullOrEmpty(clue) ? "?" : clue;
            StartCoroutine(Bounce(_banner, 0.08f));
        }

        private void Remove(Bubble b)
        {
            b.Gone = true;
            _bubbles.Remove(b);
            if (b.Rect != null) Destroy(b.Rect.gameObject);
        }

        private static string Id(KanaPair p) => p?.targetId;

        private string Note(KanaPair expected, KanaPair chosen)
        {
            string meaning = expected.Clue(Set.type);
            string line = string.IsNullOrEmpty(meaning) ? expected.a : $"{expected.a} = {meaning}";
            return chosen == null ? line : line + Loc.F("  (không phải {0})", "  (not {0})", "（{0}ではない）", chosen.a);
        }
    }
}
