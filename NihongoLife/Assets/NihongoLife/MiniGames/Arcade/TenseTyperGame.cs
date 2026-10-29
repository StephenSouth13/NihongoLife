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
    /// タイムタイパー: an English sentence with a gap and the verb in brackets; the player types the correct tense form
    /// and presses Enter before the clock on the sentence runs out. The sentences are the course's own tense exercises
    /// (12 tenses). A wrong or late answer costs a life and shows the right sentence.
    /// </summary>
    public sealed class TenseTyperGame : ArcadeGame
    {
        public override string GameId => "tense_typer";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(0.45f, 0.7f, 1f);
        protected override (string vi, string en, string ja) HowTo => (
            "Gõ dạng đúng của động từ trong ngoặc để điền vào chỗ trống rồi nhấn Enter. Hết giờ cho câu hoặc gõ sai là mất một mạng. Backspace để xoá.",
            "Type the correct form of the verb in brackets to fill the gap, then press Enter. A wrong answer or running out of time on a sentence costs a life. Backspace deletes.",
            "かっこの中の動詞を正しい形にして入力し、Enterをおそう。まちがい・時間切れで ライフが へります。Backspaceで けす。");

        private List<TenseExerciseBank.Exercise> _deck = new();
        private TenseExerciseBank.Exercise _current;
        private string _typed = "";
        private float _left;
        private int _next;
        private bool _subscribed;
        private RectTransform _card;
        private TextMeshProUGUI _tense, _sentence, _input, _timer, _feedback;

        public TenseExerciseBank.Exercise Current => _current;
        public string Typed => _typed;
        public int DeckSize => _deck.Count;

        protected override void BuildStage(RectTransform stage)
        {
            var back = UiKit.Image(stage, "Back", new Color(0.06f, 0.1f, 0.2f), UiKit.Rounded(18));
            UiKit.Fill(back.rectTransform);
            var card = UiKit.Card(stage, "Sentence", UiTheme.Paper, 22, 0.4f, false);
            UiKit.Pin(card.rectTransform, new Vector2(0.5f, 0.5f), 0f, 70f, 880f, 200f);
            _card = card.rectTransform;
            _tense = UiKit.Label(card.transform, "Tense", "", 18f, UiTheme.Ielts, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_tense.rectTransform, 0f, 14f, 880f, 26f);
            _sentence = UiKit.Label(card.transform, "Text", "", 38f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UiKit.Place(_sentence.rectTransform, 40f, 46f, 800f, 130f);
            var box = UiKit.Card(stage, "Input", UiTheme.Sunken, 18, 0f, false);
            UiKit.Pin(box.rectTransform, new Vector2(0.5f, 0.5f), 0f, -110f, 560f, 70f);
            _input = UiKit.Label(box.transform, "Typed", "", 36f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(_input.rectTransform, 20f, 4f, 20f, 4f);
            _timer = UiKit.Label(stage, "Timer", "", 26f, Accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Pin(_timer.rectTransform, new Vector2(0.5f, 1f), 0f, -20f, 300f, 36f);
            _feedback = UiKit.Label(stage, "Feedback", "", 22f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center, true);
            UiKit.Pin(_feedback.rectTransform, new Vector2(0.5f, 0.5f), 0f, -200f, 880f, 70f);
        }

        protected override void OnRoundStart()
        {
            _deck = TenseExerciseBank.Load().OrderBy(_ => Random.value).ToList();
            _next = 0;
            if (Keyboard.current != null && !_subscribed) { Keyboard.current.onTextInput += OnChar; _subscribed = true; }
            Next();
        }

        protected override void OnDestroy()
        {
            if (_subscribed && Keyboard.current != null) Keyboard.current.onTextInput -= OnChar;
            base.OnDestroy();
        }

        private float Allowed => Mathf.Lerp(22f, 12f, Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds)));

        protected override void OnTick(float dt)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.backspaceKey.wasPressedThisFrame && _typed.Length > 0) SetTyped(_typed.Substring(0, _typed.Length - 1));
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Submit();
            }
            if (State != Phase.Playing || _current == null) return;
            _left -= dt;
            _timer.text = Mathf.CeilToInt(Mathf.Max(0f, _left)).ToString();
            if (_left > 0f) return;
            Fail("");
        }

        private void OnChar(char c)
        {
            if (State != Phase.Playing || char.IsControl(c) || _typed.Length >= 28) return;
            SetTyped(_typed + c);
        }

        /// <summary>Types text (keyboard, or tests).</summary>
        public void Type(string text)
        {
            foreach (char c in text) OnChar(c);
        }

        /// <summary>Checks the typed answer (Enter, or tests).</summary>
        public void Submit()
        {
            if (State != Phase.Playing || _current == null || _typed.Length == 0) return;
            if (_current.Accepts(_typed))
            {
                Hit(_current.id, _card, 100 + Mathf.RoundToInt(_left / Allowed * 60f));
                _feedback.text = "";
                Next();
            }
            else Fail(_typed);
        }

        private void Fail(string typed)
        {
            var exercise = _current;
            Miss(exercise.id, null, exercise.sentence, _card);
            if (State != Phase.Playing) return;
            _feedback.text = $"<color={UiTheme.Hex(UiTheme.Bad)}>✗</color>  {exercise.sentence}";
            StartCoroutine(Shake(_card));
            Next();
        }

        private void Next()
        {
            if (_deck.Count == 0) { Finish(false); return; }
            _current = _deck[_next++ % _deck.Count];
            _left = Allowed;
            _tense.text = _current.TenseName;
            _sentence.text = _current.promptEn.Replace("_____", $"<color={UiTheme.Hex(UiTheme.Ielts)}>_____</color>");
            SetTyped("");
        }

        private void SetTyped(string value)
        {
            _typed = value;
            if (_input != null) _input.text = string.IsNullOrEmpty(_typed) ? $"<color={UiTheme.Hex(UiTheme.Muted)}>{Loc.T("gõ rồi nhấn Enter…", "type, then Enter…", "入力して Enter…")}</color>" : _typed;
        }
    }
}
