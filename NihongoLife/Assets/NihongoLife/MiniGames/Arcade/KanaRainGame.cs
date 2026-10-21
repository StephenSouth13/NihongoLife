using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// かなレイン: hiragana or katakana fall like rain; type each one's romaji to burst it before it reaches the
    /// ground (shi/si, chi/ti, tsu/tu, fu/hu, ji/zi all count). Faster and denser as the round goes on; a kana that
    /// lands costs a life. Romaji and kana are the same in every interface language.
    /// </summary>
    public sealed class KanaRainGame : ArcadeGame
    {
        public sealed class Drop
        {
            public KanaPair Pair;
            public RectTransform Rect;
            public float Speed;
        }

        public override string GameId => "kana_rain";
        protected override int StartLives => 5;
        protected override Color Accent => new Color(0.55f, 0.6f, 1f);
        protected override bool SetFits(KanaPairSet set) => base.SetFits(set) && (set.type == KanaPairType.HiraganaRomaji || set.type == KanaPairType.KatakanaRomaji);
        protected override (string vi, string en, string ja) HowTo => (
            "Chữ kana rơi xuống — gõ romaji của chữ (ví dụ か = ka) bằng bàn phím để phá nó trước khi chạm đất. Backspace để xoá.",
            "Kana fall from the sky — type each one's romaji (e.g. か = ka) on the keyboard to burst it before it lands. Backspace deletes.",
            "かなが ふってくる！ ローマ字を入力して（か = ka）、地面につく前に消そう。Backspaceで けす。");

        private readonly List<Drop> _drops = new();
        private float _spawn;
        private string _typed = "";
        private RectTransform _field, _ground;
        private TextMeshProUGUI _input;
        private bool _subscribed;

        public IReadOnlyList<Drop> Drops => _drops;
        public string Typed => _typed;

        protected override void BuildStage(RectTransform stage)
        {
            var sky = UiKit.Image(stage, "Night", new Color(0.07f, 0.08f, 0.2f), UiKit.Rounded(18));
            UiKit.Fill(sky.rectTransform, 0f, 0f, 0f, 90f);
            var fade = UiKit.Image(sky.transform, "Fade", new Color(0.5f, 0.45f, 1f, 0.18f), UiKit.VerticalFade);
            UiKit.Fill(fade.rectTransform);
            _field = sky.rectTransform;
            var ground = UiKit.Image(stage, "Ground", UiKit.Alpha(Accent, 0.35f), UiKit.Rounded(4));
            UiKit.Strip(ground.rectTransform, false, 88f, 6f);
            _ground = ground.rectTransform;
            var box = UiKit.Card(stage, "Input", UiTheme.Sunken, 18, 0f, false);
            UiKit.Pin(box.rectTransform, new Vector2(0.5f, 0f), 0f, 6f, 520f, 70f);
            var key = UiKit.Icon(box.transform, "keyboard", Accent, 30f);
            UiKit.Pin(key.rectTransform, new Vector2(0f, 0.5f), 22f, 0f, 30f, 30f);
            _input = UiKit.Label(box.transform, "Typed", "", 36f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(_input.rectTransform, 64f, 4f, 20f, 4f);
        }

        protected override void OnRoundStart()
        {
            _spawn = 0f;
            if (Keyboard.current != null && !_subscribed) { Keyboard.current.onTextInput += OnChar; _subscribed = true; }
        }

        protected override void OnDestroy()
        {
            if (_subscribed && Keyboard.current != null) Keyboard.current.onTextInput -= OnChar;
            base.OnDestroy();
        }

        private float Difficulty => Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds));

        protected override void OnTick(float dt)
        {
            if (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame && _typed.Length > 0) SetTyped(_typed.Substring(0, _typed.Length - 1));
            _spawn -= dt;
            if (_spawn <= 0f)
            {
                SpawnDrop();
                _spawn = Mathf.Lerp(1.6f, 0.7f, Difficulty);
            }
            float floor = 0f;
            foreach (var d in _drops.ToList())
            {
                d.Rect.anchoredPosition += Vector2.down * d.Speed * dt;
                if (d.Rect.anchoredPosition.y <= floor + 36f)
                {
                    Burst(d, false);
                    Miss(d.Pair.targetId, null, $"{d.Pair.a} = {d.Pair.b}", _ground);
                    if (State != Phase.Playing) return;
                }
            }
        }

        private Drop SpawnDrop(KanaPair forced = null)
        {
            var pair = forced ?? Set.pairs[Random.Range(0, Set.pairs.Count)];
            var tile = UiKit.Card(_field, "Kana_" + pair.a, UiTheme.Paper, 18, 0.25f, false);
            var rect = tile.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(84f, 84f);
            float w = _field.rect.width;
            rect.anchoredPosition = new Vector2(Random.Range(60f, Mathf.Max(80f, w - 60f)), _field.rect.height + 40f);
            var label = UiKit.Label(rect, "Kana", pair.a, 52f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(label.rectTransform);
            var drop = new Drop { Pair = pair, Rect = rect, Speed = Mathf.Lerp(55f, 120f, Difficulty) * Random.Range(0.85f, 1.2f) };
            _drops.Add(drop);
            return drop;
        }

        private void OnChar(char c)
        {
            if (State != Phase.Playing) return;
            if (!char.IsLetter(c)) return;
            SetTyped(_typed + char.ToLowerInvariant(c));
        }

        /// <summary>Types romaji (keyboard, or tests): a full match bursts the lowest matching kana.</summary>
        public void Type(string romaji)
        {
            foreach (char c in romaji) OnChar(c);
        }

        private void SetTyped(string value)
        {
            _typed = value;
            string norm = Normalize(_typed);
            var match = _drops.Where(d => Normalize(d.Pair.b) == norm).OrderBy(d => d.Rect.anchoredPosition.y).FirstOrDefault();
            if (match != null)
            {
                Hit(match.Pair.targetId, match.Rect, 80 + Mathf.RoundToInt(match.Rect.anchoredPosition.y / 6f));
                Burst(match, true);
                _typed = "";
            }
            else if (!_drops.Any(d => Normalize(d.Pair.b).StartsWith(norm)) && _typed.Length >= 3)
            {
                StartCoroutine(Shake((RectTransform)_input.transform.parent));
                _typed = "";
            }
            if (_input != null) _input.text = string.IsNullOrEmpty(_typed) ? $"<color={UiTheme.Hex(UiTheme.Muted)}>{Loc.T("gõ romaji…", "type romaji…", "ローマ字…")}</color>" : _typed;
        }

        /// <summary>Hepburn and Kunrei spellings both count.</summary>
        public static string Normalize(string romaji)
        {
            string r = (romaji ?? "").ToLowerInvariant().Trim();
            return r.Replace("shi", "si").Replace("chi", "ti").Replace("tsu", "tu").Replace("fu", "hu").Replace("ji", "zi");
        }

        private void Burst(Drop d, bool good)
        {
            _drops.Remove(d);
            if (d.Rect == null) return;
            d.Rect.GetComponent<UnityEngine.UI.Image>().color = good ? UiTheme.Good : UiTheme.Bad;
            Destroy(d.Rect.gameObject, 0.12f);
        }

        /// <summary>Tests: makes sure a kana is falling and returns it.</summary>
        public Drop EnsureDrop() => _drops.FirstOrDefault() ?? SpawnDrop();
    }
}
