using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Dialogue;
using NihongoLife.Learning;

namespace NihongoLife.UI
{
    /// <summary>
    /// The one dialogue box used by every scene (city, konbini, station, sushi, school, bedroom). Built at
    /// runtime with layout groups so the box grows with its text instead of overflowing. Japanese line
    /// types in, reading / romaji / Vietnamese meaning follow the learning mode, choices are numbered
    /// cards (mouse, 1–4, ↑↓ + Enter) and Esc leaves NPC conversations.
    /// </summary>
    public sealed class DialogueView : MonoBehaviour
    {
        public event Action<int> ChoiceSelected;
        public event Action ContinueRequested;
        public event Action LeaveRequested;

        public GameObject Root => _root.gameObject;
        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public bool IsTyping => _typing;
        public int ChoiceCount => _choices.Count;

        private TMP_FontAsset _font;
        private RectTransform _root;
        private RectTransform _box;
        private TextMeshProUGUI _speaker;
        private RectTransform _speakerPill;
        private TextMeshProUGUI _mode;
        private TextMeshProUGUI _japanese;
        private TextMeshProUGUI _reading;
        private TextMeshProUGUI _romaji;
        private TextMeshProUGUI _meaning;
        private RectTransform _choiceRoot;
        private TextMeshProUGUI _hint;
        private readonly List<Image> _choices = new List<Image>();
        private int _selected;
        private bool _typing;
        private float _typed;
        private bool _canLeave;
        private UnityEngine.UI.Button _closeButton;
        private float _openedAt;

        private const float CharsPerSecond = 55f;

        public static DialogueView Create(Transform parent, TMP_FontAsset font)
        {
            var canvas = NLUi.CreateCanvas("DialogueCanvas", 600, parent);
            var view = canvas.gameObject.AddComponent<DialogueView>();
            view._font = font != null ? font : NLUi.ResolveFont();
            view.Build(canvas.transform);
            return view;
        }

        private void Build(Transform canvas)
        {
            var rootGo = new GameObject("DialogueRoot", typeof(RectTransform));
            rootGo.transform.SetParent(canvas, false);
            _root = (RectTransform)rootGo.transform;
            NLUi.Stretch(_root);

            _box = NLUi.Panel(_root, "DialogueBox", NLUi.Ink, new RectOffset(34, 34, 22, 20), 10f);
            NLUi.Anchor(_box, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1320f, 0f));
            NLUi.FitContent(_box);

            var header = NLUi.Group(_box, "Header", false, 12f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)header.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            _speakerPill = NLUi.Pill(header, "Speaker", "", _font, NLUi.Gold, new Color(0.1f, 0.08f, 0.04f), 19f);
            _speaker = _speakerPill.GetComponentInChildren<TextMeshProUGUI>();
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(header, false);
            NLUi.Size(spacer.transform, flexibleWidth: 1f);
            _mode = NLUi.Label(header, "Mode", "", 15f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Right);
            _mode.textWrappingMode = TextWrappingModes.NoWrap;
            var closeSpace = new GameObject("CloseSpace", typeof(RectTransform));
            closeSpace.transform.SetParent(header, false);
            NLUi.Size(closeSpace.transform, 46f, 46f);

            _japanese = NLUi.Label(_box, "Japanese", "", 33f, NLUi.Text, _font, FontStyles.Bold);
            _japanese.lineSpacing = 6f;
            _reading = NLUi.Label(_box, "Reading", "", 19f, NLUi.Muted, _font);
            _romaji = NLUi.Label(_box, "Romaji", "", 17f, new Color(0.6f, 0.66f, 0.74f), _font, FontStyles.Italic);
            _meaning = NLUi.Label(_box, "Meaning", "", 21f, NLUi.Soft, _font);

            _choiceRoot = NLUi.Group(_box, "Choices", true, 8f);
            NLUi.Divider(_box).SetSiblingIndex(_choiceRoot.GetSiblingIndex());
            _hint = NLUi.Label(_box, "Hint", "", 15f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Right);
            _closeButton = NLUi.CloseButton(_box, _font, Leave, 42f, 14f);
            _root.gameObject.SetActive(false);
        }

        public void Show(DialogueDisplayData data, bool canLeave)
        {
            _canLeave = canLeave;
            _openedAt = Time.unscaledTime;
            if (_closeButton != null) _closeButton.gameObject.SetActive(canLeave);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();

            string speaker = string.IsNullOrWhiteSpace(data.speakerName) ? "ナレーション · Lời dẫn" : data.speakerName;
            _speaker.text = speaker;
            _mode.text = data.learningMode switch
            {
                LearningMode.GuidedPractice => "Chế độ: Hướng dẫn",
                LearningMode.Practice => "Chế độ: Luyện tập",
                _ => "Chế độ: Kiểm tra"
            };

            _japanese.text = data.textJa ?? string.Empty;
            _japanese.maxVisibleCharacters = 0;
            _typed = 0f;
            _typing = !string.IsNullOrEmpty(_japanese.text);

            bool guided = data.learningMode == LearningMode.GuidedPractice;
            bool assessment = data.learningMode == LearningMode.Assessment;
            SetLine(_reading, assessment ? null : data.textReading);
            SetLine(_romaji, guided ? data.textRomaji : null);
            SetLine(_meaning, assessment ? null : data.textEn);

            foreach (Transform child in _choiceRoot) Destroy(child.gameObject);
            _choices.Clear();
            var choices = data.choices;
            bool hasChoices = choices != null && choices.Count > 0;
            _choiceRoot.gameObject.SetActive(hasChoices);
            if (hasChoices)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    int index = i;
                    var card = NLUi.Panel(_choiceRoot, "Choice_" + (i + 1), NLUi.Card, new RectOffset(16, 18, 10, 10), 14f, vertical: false);
                    var row = (HorizontalLayoutGroup)card.GetComponent<HorizontalOrVerticalLayoutGroup>();
                    row.childForceExpandWidth = false;
                    row.childAlignment = TextAnchor.MiddleLeft;
                    var badge = NLUi.Pill(card, "Number", (i + 1).ToString(), _font, new Color(1f, 1f, 1f, 0.1f), NLUi.Gold, 18f);
                    NLUi.Size(badge, preferredWidth: 38f);
                    var texts = NLUi.Group(card, "Texts", true, 2f);
                    NLUi.Size(texts, flexibleWidth: 1f);
                    NLUi.Label(texts, "Ja", choices[i].textJa, 23f, NLUi.Text, _font);
                    if (!assessment && guided && !string.IsNullOrWhiteSpace(choices[i].textEn))
                        NLUi.Label(texts, "Meaning", choices[i].textEn, 16f, NLUi.Muted, _font);
                    var button = card.gameObject.AddComponent<Button>();
                    button.targetGraphic = card.GetComponent<Image>();
                    button.transition = Selectable.Transition.None;
                    button.onClick.AddListener(() => Pick(index));
                    var hover = card.gameObject.AddComponent<ChoiceHover>();
                    hover.Init(this, index);
                    _choices.Add(card.GetComponent<Image>());
                }
                Select(0);
            }
            RefreshHint(hasChoices);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_box);
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
            _typing = false;
        }

        private void SetLine(TextMeshProUGUI target, string value)
        {
            bool show = !string.IsNullOrWhiteSpace(value);
            target.gameObject.SetActive(show);
            target.text = show ? value : string.Empty;
        }

        private void RefreshHint(bool hasChoices)
        {
            string main = hasChoices ? "<color=#F2B233>1–" + _choices.Count + "</color> chọn  ·  <color=#F2B233>↑↓ Enter</color> xác nhận" : "<color=#F2B233>Enter / Space</color> ▶ tiếp tục";
            string leave = _canLeave ? "   ·   <color=#F2B233>Esc</color> rời cuộc trò chuyện" : string.Empty;
            _hint.text = main + leave;
        }

        internal void Select(int index)
        {
            if (_choices.Count == 0) return;
            _selected = (index % _choices.Count + _choices.Count) % _choices.Count;
            for (int i = 0; i < _choices.Count; i++)
                _choices[i].color = i == _selected ? new Color(0.36f, 0.27f, 0.08f, 1f) : NLUi.Card;
        }

        /// <summary>Public for tests and mouse; ignores input during the first frames so the key that opened
        /// the dialogue (F / Enter) cannot also pick an answer.</summary>
        public void Pick(int index)
        {
            if (!IsOpen || Time.unscaledTime - _openedAt < 0.15f) return;
            if (_typing) { FinishTyping(); return; }
            if (index < 0 || index >= _choices.Count) return;
            ChoiceSelected?.Invoke(index);
        }

        public void Continue()
        {
            if (!IsOpen || Time.unscaledTime - _openedAt < 0.15f) return;
            if (_typing) { FinishTyping(); return; }
            if (_choices.Count > 0) { Pick(_selected); return; }
            ContinueRequested?.Invoke();
        }

        public void Leave()
        {
            if (IsOpen && _canLeave) LeaveRequested?.Invoke();
        }

        private void FinishTyping()
        {
            _typing = false;
            _japanese.maxVisibleCharacters = 99999;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_typing)
            {
                _typed += Time.unscaledDeltaTime * CharsPerSecond;
                _japanese.maxVisibleCharacters = Mathf.FloorToInt(_typed);
                if (_typed >= _japanese.text.Length) FinishTyping();
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { Leave(); return; }
            for (int i = 0; i < Mathf.Min(4, _choices.Count); i++)
            {
                if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame || keyboard[(Key)((int)Key.Numpad1 + i)].wasPressedThisFrame)
                {
                    if (!_typing) Select(i);
                    Pick(i);
                    return;
                }
            }
            if (_choices.Count > 0 && (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)) Select(_selected + 1);
            if (_choices.Count > 0 && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)) Select(_selected - 1);
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Continue();
        }

        private sealed class ChoiceHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
        {
            private DialogueView _view;
            private int _index;
            public void Init(DialogueView view, int index) { _view = view; _index = index; }
            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) => _view.Select(_index);
        }
    }
}
