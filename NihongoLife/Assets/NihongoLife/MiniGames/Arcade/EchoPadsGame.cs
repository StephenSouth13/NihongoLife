using System.Collections.Generic;
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
    /// エコーパッド: four glowing pads named after the notes ド・レ・ミ・ソ play a tune; the player repeats it. Every
    /// clean repeat adds one more note. A wrong pad costs a life and the same tune is played again. No learning content —
    /// it trains the ear and the memory (and the note names every musician in town uses).
    /// </summary>
    public sealed class EchoPadsGame : ArcadeGame
    {
        private enum Mode { Show, Repeat }

        public override string GameId => "echo_pads";
        protected override bool UsesContentSets => false;
        protected override Color Accent => new Color(1f, 0.75f, 0.3f);
        protected override (string vi, string en, string ja) HowTo => (
            "Xem các ô sáng lên theo thứ tự, rồi bấm lại đúng thứ tự đó (hoặc phím 1-4 / A S D F). Mỗi lượt đúng thêm một nốt. Bấm sai mất một mạng.",
            "Watch the pads light up in order, then repeat the tune (click, keys 1-4 or A S D F). Every clean round adds a note. A wrong pad costs a life.",
            "ひかる順番を見て、同じ順番でパッドをおそう（クリック、キー1〜4、A S D F）。せいかいするたびに 音が1つふえます。");

        private static readonly string[] NoteNames = { "ド", "レ", "ミ", "ソ" };
        private static readonly Color[] PadColors =
        {
            new Color(0.95f, 0.38f, 0.4f), new Color(1f, 0.78f, 0.3f), new Color(0.35f, 0.82f, 0.5f), new Color(0.4f, 0.65f, 1f),
        };

        private readonly List<int> _sequence = new();
        private readonly Image[] _pads = new Image[4];
        private Mode _mode;
        private int _showIndex, _lit = -1, _inputIndex, _flashPad = -1;
        private float _timer, _flash;
        private TextMeshProUGUI _status;

        public IReadOnlyList<int> Sequence => _sequence;
        public bool IsRepeating => State == Phase.Playing && _mode == Mode.Repeat;

        protected override void BuildStage(RectTransform stage)
        {
            var back = UiKit.Image(stage, "Back", new Color(0.1f, 0.07f, 0.16f), UiKit.Rounded(18));
            UiKit.Fill(back.rectTransform);
            _status = UiKit.Label(stage, "Status", "", 30f, Accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Pin(_status.rectTransform, new Vector2(0.5f, 1f), 0f, -20f, 700f, 48f);
            for (int i = 0; i < 4; i++)
            {
                int pad = i;
                var card = UiKit.Card(stage, "Pad_" + NoteNames[i], Dim(PadColors[i]), 28, 0.3f);
                var rect = card.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.14f + i * 0.24f, 0.42f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(170f, 230f);
                rect.anchoredPosition = Vector2.zero;
                var name = UiKit.Label(rect, "Note", NoteNames[i], 84f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(name.rectTransform, 0f, 40f, 170f, 110f);
                var key = UiKit.Label(rect, "Key", $"{i + 1}  ·  {"ASDF"[i]}", 22f, UiKit.Alpha(Color.white, 0.8f), FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(key.rectTransform, 0f, 176f, 170f, 30f);
                var button = card.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => Press(pad));
                _pads[i] = card;
            }
        }

        private static Color Dim(Color c) => Color.Lerp(c, new Color(0.08f, 0.06f, 0.12f), 0.62f);

        protected override void OnRoundStart()
        {
            _sequence.Clear();
            AddNote();
            StartShow();
        }

        private float Difficulty => Mathf.Clamp01(Elapsed / Mathf.Max(30f, Definition.timeLimitSeconds));

        private void AddNote() => _sequence.Add(Random.Range(0, 4));

        private void StartShow()
        {
            _mode = Mode.Show;
            _showIndex = -1;
            _lit = -1;
            _timer = 0.7f;
            _status.text = Loc.T("Nghe và nhìn…", "Watch and listen…", "よく見て…");
        }

        protected override void OnTick(float dt)
        {
            if (_flashPad >= 0 && (_flash -= dt) <= 0f)
            {
                Light(_flashPad, false);
                _flashPad = -1;
            }
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Press(0);
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Press(1);
                if (keyboard.digit3Key.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Press(2);
                if (keyboard.digit4Key.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame) Press(3);
            }
            if (_mode != Mode.Show) return;
            _timer -= dt;
            if (_timer > 0f) return;
            if (_lit >= 0)
            {
                Light(_lit, false);
                _lit = -1;
                _timer = 0.18f;
                return;
            }
            _showIndex++;
            if (_showIndex >= _sequence.Count)
            {
                _mode = Mode.Repeat;
                _inputIndex = 0;
                _status.text = Loc.T($"Tới lượt bạn! ({_sequence.Count} nốt)", $"Your turn! ({_sequence.Count} notes)", $"あなたの番！（{_sequence.Count}こ）");
                return;
            }
            _lit = _sequence[_showIndex];
            Light(_lit, true);
            Cue(GameAudioCue.UiTick);
            _timer = Mathf.Lerp(0.5f, 0.3f, Difficulty);
        }

        /// <summary>Presses a pad (0-3: ド レ ミ ソ). Only counts while it is the player's turn.</summary>
        public void Press(int pad)
        {
            if (State != Phase.Playing || _mode != Mode.Repeat || pad < 0 || pad > 3) return;
            Light(pad, true);
            _flashPad = pad;
            _flash = 0.16f;
            Cue(GameAudioCue.UiTick);
            if (pad == _sequence[_inputIndex])
            {
                _inputIndex++;
                if (_inputIndex < _sequence.Count) return;
                Hit("", _pads[pad].rectTransform, 40 + _sequence.Count * 30);
                if (State != Phase.Playing) return;
                AddNote();
                StartShow();
            }
            else
            {
                Miss("", "", $"{NoteNames[_sequence[_inputIndex]]} ≠ {NoteNames[pad]}", _pads[pad].rectTransform);
                if (State == Phase.Playing) StartShow();
            }
        }

        private void Light(int pad, bool on) => _pads[pad].color = on ? Color.Lerp(PadColors[pad], Color.white, 0.25f) : Dim(PadColors[pad]);
    }
}
