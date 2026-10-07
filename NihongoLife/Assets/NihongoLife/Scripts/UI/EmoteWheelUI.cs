using System;
using System.Collections.Generic;
using NihongoLife.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Radial emote picker shown while E is held: eight emotes around a gold-rimmed wheel, the hovered
    /// slice glows and grows, the centre previews it with its Japanese phrase. Selection follows the
    /// mouse (relative movement, so it also works with a locked cursor), the gamepad right stick,
    /// number keys 1–8, or a click/tap on a slot.
    /// </summary>
    public sealed class EmoteWheelUI : MonoBehaviour
    {
        private const float Size = 560f;
        private const float SlotRadius = 188f;
        private const float DeadZone = 38f;

        private RectTransform _root;
        private RectTransform _highlight;
        private Image _centerIcon;
        private TextMeshProUGUI _phrase;
        private readonly List<RectTransform> _slots = new();
        private Vector2 _pointer;
        private int _selected = -1;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public int Selected => _selected;
        public event Action<int> SlotClicked;

        public static EmoteWheelUI GetOrCreate()
        {
            var existing = FindFirstObjectByType<EmoteWheelUI>(FindObjectsInactive.Include);
            if (existing != null) return existing;
            var hud = FindFirstObjectByType<HUDUI>();
            var canvas = NLUi.CreateCanvas("EmoteWheelCanvas", 700, hud != null ? hud.transform : null);
            if (hud == null) DontDestroyOnLoad(canvas.gameObject);
            var wheel = canvas.gameObject.AddComponent<EmoteWheelUI>();
            wheel.Build((RectTransform)canvas.transform);
            return wheel;
        }

        private void Build(RectTransform canvas)
        {
            var font = NLUi.ResolveFont();
            _root = new GameObject("EmoteWheel", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            _root.SetParent(canvas, false);
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(Size, Size);

            Picture(_root, "Frame", EmoteCatalog.Load("Emotes/wheel_frame"), Vector2.zero, Size);
            _highlight = Picture(_root, "Highlight", EmoteCatalog.Load("Emotes/wheel_highlight"), Vector2.zero, Size).rectTransform;

            for (int i = 0; i < EmoteCatalog.All.Count; i++)
            {
                int index = i;
                var emote = EmoteCatalog.All[i];
                float angle = (90f - i * 45f) * Mathf.Deg2Rad;
                var icon = Picture(_root, "Slot_" + emote.Id, EmoteCatalog.Icon(emote.Id), new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SlotRadius, 104f);
                icon.raycastTarget = true;
                var button = icon.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => { Select(index); SlotClicked?.Invoke(index); });
                var key = NLUi.Label(icon.rectTransform, "Key", (i + 1).ToString(), 15f, NLUi.Gold, font, FontStyles.Bold, TextAlignmentOptions.Center);
                key.rectTransform.anchorMin = key.rectTransform.anchorMax = new Vector2(0.86f, 0.12f);
                key.rectTransform.sizeDelta = new Vector2(26f, 22f);
                _slots.Add(icon.rectTransform);
            }

            _centerIcon = Picture(_root, "Center", null, Vector2.zero, 150f);
            _centerIcon.color = new Color(1f, 1f, 1f, 0f);

            var pill = NLUi.Panel(_root, "Phrase", NLUi.Ink, new RectOffset(26, 26, 10, 12), 2f);
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 0f);
            pill.pivot = new Vector2(0.5f, 1f);
            pill.anchoredPosition = new Vector2(0f, -14f);
            pill.sizeDelta = new Vector2(520f, 0f);
            NLUi.FitContent(pill);
            _phrase = NLUi.Label(pill, "Text", "", 24f, NLUi.Text, font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(pill, "Hint", "Rê chuột / 1–8 để chọn  ·  thả E để dùng  ·  Esc huỷ", 15f, NLUi.Muted, font, FontStyles.Normal, TextAlignmentOptions.Center);
            _root.gameObject.SetActive(false);
        }

        private static Image Picture(RectTransform parent, string name, Sprite sprite, Vector2 position, float size)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        public void Open(int preselect)
        {
            _pointer = Vector2.zero;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _root.localScale = Vector3.one * 0.85f;
            Select(preselect);
        }

        public void Close() => _root.gameObject.SetActive(false);

        public void Select(int index)
        {
            _selected = index >= 0 && index < _slots.Count ? index : -1;
            _highlight.gameObject.SetActive(_selected >= 0);
            if (_selected >= 0) _highlight.localRotation = Quaternion.Euler(0f, 0f, -_selected * 45f);
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].localScale = Vector3.one * (i == _selected ? 1.22f : 1f);
            if (_selected >= 0)
            {
                var emote = EmoteCatalog.All[_selected];
                _centerIcon.sprite = EmoteCatalog.Icon(emote.Id);
                _centerIcon.color = Color.white;
                _phrase.text = $"{emote.Ja}  <size=70%><color=#A8B4C4>{emote.Reading} · {emote.Vi}</color></size>";
            }
            else
            {
                _centerIcon.color = new Color(1f, 1f, 1f, 0f);
                _phrase.text = "<color=#A8B4C4>Chọn một biểu cảm</color>";
            }
        }

        private void Update()
        {
            if (!IsOpen) return;
            _root.localScale = Vector3.MoveTowards(_root.localScale, Vector3.one, Time.unscaledDeltaTime * 2.5f);

            var keyboard = Keyboard.current;
            if (keyboard != null)
                for (int i = 0; i < _slots.Count; i++)
                    if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) { Select(i); return; }

            Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.25f) { _pointer = stick * 100f; delta = Vector2.zero; }
            }
            _pointer = Vector2.ClampMagnitude(_pointer + delta, 120f);
            if (_pointer.magnitude < DeadZone) return;
            float angle = Mathf.Atan2(_pointer.y, _pointer.x) * Mathf.Rad2Deg; // 0 = right, 90 = up
            int slot = Mathf.RoundToInt(Mathf.Repeat(90f - angle, 360f) / 45f) % _slots.Count;
            if (slot != _selected) Select(slot);
        }
    }
}
