using System;
using System.Collections.Generic;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The conversation card used by part-time jobs: a customer line in Japanese (reading and Vietnamese meaning on
    /// demand), a task question and 3–4 answers. A wrong answer is explained and that answer is disabled — the player
    /// tries again, and nothing is counted until the right answer. Closing the card (× / Esc) cancels without credit.
    /// </summary>
    public sealed class JobQuizCard : MonoBehaviour
    {
        public sealed class Choice
        {
            public string Id;
            /// <summary>Item icon (Resources/Items/&lt;id&gt;) shown on the answer, when one exists.</summary>
            public string IconItemId;
            public string Label;
            public string Detail;
            public bool Correct;
            public string WrongHint;
        }

        private static JobQuizCard _instance;
        private TMP_FontAsset _font;
        private RectTransform _card;
        private Action<Choice> _onCorrect;
        private PlayerController _player;
        private bool _wasLocked;

        public static bool IsOpen => _instance != null && _instance._card != null;
        /// <summary>Test hook: the answer labels in display order.</summary>
        public static IReadOnlyList<Choice> CurrentChoices { get; private set; } = Array.Empty<Choice>();

        public static void Show(string kicker, string speaker, string lineJa, string reading, string meaningVi, string question, IList<Choice> choices, Action onCorrect, string headerIcon = null)
            => Show(kicker, speaker, lineJa, reading, meaningVi, question, choices, _ => onCorrect?.Invoke(), headerIcon);

        public static void Show(string kicker, string speaker, string lineJa, string reading, string meaningVi, string question, IList<Choice> choices, Action<Choice> onCorrect, string headerIcon = null)
        {
            if (_instance == null)
            {
                var canvas = NLUi.CreateCanvas("JobQuizCanvas", 160);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<JobQuizCard>();
                _instance._font = NLUi.ResolveFont();
                UiModalStack.Register(_instance, () => IsOpen, () => Close(), "Job conversation");
            }
            var ui = _instance;
            if (ui._card != null) Destroy(ui._card.gameObject);
            ui._onCorrect = onCorrect;
            CurrentChoices = new List<Choice>(choices);
            if (ui._player == null)
            {
                ui._player = FindFirstObjectByType<PlayerController>();
                if (ui._player != null) { ui._wasLocked = ui._player.InputLocked; ui._player.InputLocked = true; }
            }

            ui._card = NLUi.Panel(ui.transform, "JobQuiz", NLUi.Ink, new RectOffset(28, 28, 20, 22), 10f);
            NLUi.Anchor(ui._card, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(680f, 0f));
            NLUi.FitContent(ui._card);
            NLUi.Label(ui._card, "Kicker", kicker, 14f, NLUi.Gold, ui._font, FontStyles.Bold);
            NLUi.Label(ui._card, "Speaker", speaker, 16f, NLUi.Muted, ui._font);
            var lineRow = NLUi.Group(ui._card, "LineRow", false, 14f, TextAnchor.MiddleLeft, false);
            var headerSprite = ItemIcons.Get(headerIcon);
            if (headerSprite != null) Icon(lineRow, headerSprite, 64f);
            var line = NLUi.Label(lineRow, "LineJa", lineJa, 30f, NLUi.Text, ui._font, FontStyles.Bold);
            NLUi.Size(line, flexibleWidth: 1f);
            line.textWrappingMode = TextWrappingModes.Normal;
            if (!string.IsNullOrEmpty(reading)) NLUi.Label(ui._card, "Reading", reading, 16f, NLUi.Muted, ui._font, FontStyles.Italic);
            var meaningRow = NLUi.Group(ui._card, "MeaningRow", false, 10f, TextAnchor.MiddleLeft, false);
            var meaning = NLUi.Label(meaningRow, "Meaning", "", 16f, NLUi.Soft, ui._font);
            var reveal = NLUi.Button(meaningRow, "RevealMeaning", "Nghĩa?", ui._font, null, new Color(1f, 1f, 1f, 0.08f), 14f, NLUi.Text, 32f);
            NLUi.Size(reveal, 96f, 32f);
            reveal.onClick.AddListener(() => { meaning.text = meaningVi; reveal.gameObject.SetActive(false); });
            NLUi.Divider(ui._card);
            NLUi.Label(ui._card, "Question", question, 18f, NLUi.Text, ui._font).textWrappingMode = TextWrappingModes.Normal;
            var feedback = NLUi.Label(ui._card, "Feedback", "", 16f, new Color(0.95f, 0.5f, 0.45f), ui._font, FontStyles.Bold);
            var grid = NLUi.Group(ui._card, "Choices", true, 8f);
            feedback.transform.SetAsLastSibling();
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                string text = string.IsNullOrEmpty(choice.Detail) ? choice.Label : $"{choice.Label}   <size=75%><color=#A8B4C4>{choice.Detail}</color></size>";
                var button = NLUi.Button(grid, "Choice_" + i, text, ui._font, null, new Color(0.12f, 0.18f, 0.24f, 1f), 19f, NLUi.Text, 50f);
                var choiceSprite = ItemIcons.Get(choice.IconItemId);
                if (choiceSprite != null)
                {
                    var icon = Icon((RectTransform)button.transform, choiceSprite, 40f);
                    icon.GetComponent<LayoutElement>().ignoreLayout = true;
                    var r = icon.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f); r.anchoredPosition = new Vector2(12f, 0f); r.sizeDelta = new Vector2(40f, 40f);
                }
                button.onClick.AddListener(() =>
                {
                    if (choice.Correct) { Close(success: true, chosen: choice); return; }
                    feedback.text = string.IsNullOrEmpty(choice.WrongHint) ? "Chưa đúng — thử lại nhé." : choice.WrongHint;
                    button.interactable = false;
                });
            }
            NLUi.CloseButton(ui._card, ui._font, () => Close(), 42f, 14f);
            ui._card.SetAsLastSibling();
        }

        private static Image Icon(RectTransform parent, Sprite sprite, float size)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var e = go.GetComponent<LayoutElement>(); e.preferredWidth = e.minWidth = size; e.preferredHeight = e.minHeight = size;
            var image = go.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        /// <summary>Test hook: answers with the choice at <paramref name="index"/>.</summary>
        public static void Answer(int index)
        {
            if (!IsOpen) return;
            var buttons = _instance._card.GetComponentsInChildren<Button>();
            foreach (var b in buttons) if (b.name == "Choice_" + index) { b.onClick.Invoke(); return; }
        }

        public static void Close(bool success = false, Choice chosen = null)
        {
            if (_instance == null || _instance._card == null) return;
            _instance._card.gameObject.SetActive(false);
            Destroy(_instance._card.gameObject);
            _instance._card = null;
            CurrentChoices = Array.Empty<Choice>();
            if (_instance._player != null) _instance._player.InputLocked = _instance._wasLocked;
            _instance._player = null;
            var done = _instance._onCorrect;
            _instance._onCorrect = null;
            if (success) done?.Invoke(chosen);
        }
    }
}
