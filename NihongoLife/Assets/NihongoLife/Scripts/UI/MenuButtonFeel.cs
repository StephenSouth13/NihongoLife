using System.Collections.Generic;
using NihongoLife.Audio;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Finds every menu button, including ones created when a popup opens, and gives it <see cref="MenuButtonFeel"/>.</summary>
    public sealed class MenuFeelDriver : MonoBehaviour
    {
        readonly HashSet<int> _dressed = new HashSet<int>();
        readonly HashSet<int> _entered = new HashSet<int>();

        private void LateUpdate()
        {
            int order = 0;
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                int id = button.GetInstanceID();
                if (_dressed.Add(id) && button.GetComponent<MenuButtonFeel>() == null)
                    button.gameObject.AddComponent<MenuButtonFeel>();
                if (!button.gameObject.activeInHierarchy || !_entered.Add(id)) continue;
                var feel = button.GetComponent<MenuButtonFeel>();
                if (feel != null) feel.PlayEntrance(order * 0.045f);
                order++;
            }
        }
    }

    /// <summary>
    /// Hover shine, press ripple, icon nudge, and a keyboard focus ring.
    /// Scale squash stays on <see cref="UiHover"/> and <see cref="UIHoverScale"/>. Sound stays on those too when present.
    /// </summary>
    public sealed class MenuButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        RectTransform _rect;
        Image _shine;
        RectTransform _shineRect;
        Image _ripple;
        RectTransform _rippleRect;
        Image _focus;
        Transform _icon;
        Vector3 _iconRest;
        Transform _label;
        Vector3 _labelRest;
        CanvasGroup _group;
        float _shineT = -1f;
        float _rippleT = -1f;
        float _entrance = -1f;
        float _delay;
        bool _over;
        bool _iconRestCaptured;

        public static void Dress(Transform root)
        {
            if (root == null) return;
            if (root.GetComponent<MenuFeelDriver>() == null) root.gameObject.AddComponent<MenuFeelDriver>();
        }

        private void Awake()
        {
            _rect = transform as RectTransform;
            var mask = GetComponent<RectMask2D>();
            if (mask == null) gameObject.AddComponent<RectMask2D>();
            _shine = ChildImage("ShineSweep", new Color(1f, 1f, 1f, 0f), UiKit.Rounded(8));
            _shineRect = _shine.rectTransform;
            _shineRect.anchorMin = _shineRect.anchorMax = new Vector2(0f, 0.5f);
            _shineRect.pivot = new Vector2(0.5f, 0.5f);
            _shineRect.sizeDelta = new Vector2(28f, 180f);
            _shineRect.localRotation = Quaternion.Euler(0f, 0f, 18f);
            _ripple = ChildImage("Ripple", new Color(1f, 0.9f, 0.6f, 0f), UiKit.Circle);
            _rippleRect = _ripple.rectTransform;
            _rippleRect.anchorMin = _rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
            _rippleRect.sizeDelta = new Vector2(36f, 36f);
            _focus = UiKit.Border(_rect, new Color(0.97f, 0.74f, 0.24f, 0.95f), 12);
            _focus.raycastTarget = false;
            _focus.enabled = false;
            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Icon")) continue;
                _icon = child;
                _iconRest = child.localEulerAngles;
                _iconRestCaptured = true;
                break;
            }
            if (_icon != null) return;
            var text = GetComponentInChildren<TextMeshProUGUI>();
            if (text == null || text.transform == transform) return;
            _label = text.transform;
            _labelRest = _label.localScale;
        }

        public void PlayEntrance(float delay)
        {
            _delay = delay;
            _entrance = 0f;
            if (MotionSettings.ReduceMotion)
            {
                if (_group != null) _group.alpha = 1f;
                _entrance = -1f;
                return;
            }
            _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _over = true;
            if (!MotionSettings.ReduceMotion) _shineT = 0f;
            PlayHoverSound();
        }

        public void OnPointerExit(PointerEventData eventData) => _over = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (MotionSettings.ReduceMotion || _rect == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
                _rippleRect.anchoredPosition = local;
            _rippleT = 0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (GetComponent<UIButtonAudio>() != null) return;
            if (!GameServices.TryGet(out IAudioService audio)) return;
            audio.PlayCue(GameAudioCue.UiClick, 0.55f);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_focus == null) return;
            _focus.enabled = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (_focus != null) _focus.enabled = false;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_entrance >= 0f)
            {
                if (_delay > 0f) _delay -= dt;
                else
                {
                    _entrance += dt / 0.28f;
                    float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(_entrance), 3f);
                    if (_group != null) _group.alpha = eased;
                    if (_entrance >= 1f)
                    {
                        if (_group != null) _group.alpha = 1f;
                        _entrance = -1f;
                    }
                }
            }
            if (_shineT >= 0f && _shine != null && _rect != null)
            {
                _shineT += dt / 0.48f;
                float u = Mathf.Clamp01(_shineT);
                float x = Mathf.Lerp(-_rect.rect.width * 0.2f, _rect.rect.width * 1.15f, u);
                _shineRect.anchoredPosition = new Vector2(x, 0f);
                _shine.color = new Color(1f, 1f, 1f, Mathf.Sin(u * Mathf.PI) * 0.38f);
                if (_shineT >= 1f) { _shine.color = new Color(1f, 1f, 1f, 0f); _shineT = -1f; }
            }
            if (_rippleT >= 0f && _ripple != null)
            {
                _rippleT += dt / 0.36f;
                float u = Mathf.Clamp01(_rippleT);
                float size = Mathf.Lerp(24f, Mathf.Max(_rect.rect.width, 80f) * 1.4f, 1f - Mathf.Pow(1f - u, 2f));
                _rippleRect.sizeDelta = new Vector2(size, size);
                _ripple.color = new Color(1f, 0.92f, 0.7f, (1f - u) * 0.4f);
                if (_rippleT >= 1f) _rippleT = -1f;
            }
            if (_icon != null && _iconRestCaptured && !MotionSettings.ReduceMotion && _over)
            {
                float wobble = Mathf.Sin(Time.unscaledTime * 7f) * 7f;
                _icon.localEulerAngles = _iconRest + new Vector3(0f, 0f, wobble);
            }
            else if (_icon != null && _iconRestCaptured)
            {
                _icon.localEulerAngles = Vector3.Lerp(_icon.localEulerAngles, _iconRest, 1f - Mathf.Exp(-12f * dt));
            }
            if (_label != null && !MotionSettings.ReduceMotion && _over)
                _label.localScale = _labelRest * (1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.035f);
            else if (_label != null)
                _label.localScale = Vector3.Lerp(_label.localScale, _labelRest, 1f - Mathf.Exp(-12f * dt));
        }

        private void PlayHoverSound()
        {
            if (GetComponent<UiHover>() != null || GetComponent<ButtonColorSwap>() != null) return;
            if (!GameServices.TryGet(out IAudioService audio)) return;
            audio.PlayCue(GameAudioCue.UiHover, 0.35f);
        }

        private Image ChildImage(string name, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null) { image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = false; }
            return image;
        }
    }
}
