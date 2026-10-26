using System;
using System.Collections.Generic;
using NihongoLife.Cameras;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// On-screen move, look, and action buttons for touch play.
    /// Keyboard and mouse stay active. The look pad only swallows pointer events on a real touch device.
    /// </summary>
    public sealed class TouchControlOverlay : MonoBehaviour
    {
        struct LabelRef
        {
            public TextMeshProUGUI Text;
            public string Vi, En, Ja;
        }

        CanvasGroup _visuals;
        Image _look;
        RectTransform _stick;
        RectTransform _jump;
        RectTransform _interact;
        RectTransform _sprint;
        readonly List<RectTransform> _small = new List<RectTransform>();
        GameObject _restore;
        bool _sessionHidden;
        bool _pinching;
        float _pinchDistance;
        ThirdPersonCameraController _camera;
        readonly List<LabelRef> _labels = new List<LabelRef>();
        readonly GameInputId[] _held =
        {
            GameInputId.Jump, GameInputId.Interact, GameInputId.Sprint, GameInputId.Map,
            GameInputId.Inventory, GameInputId.Chat, GameInputId.PushToTalk, GameInputId.Settings
        };

        public bool Pinching => _pinching;

        public static void Attach(Component host)
        {
            if (host == null) return;
            var parent = host.transform;
            var previous = parent.Find("MobileControls");
            if (previous != null) Destroy(previous.gameObject);
            if (parent.Find("TouchControls") != null) return;
            var go = new GameObject("TouchControls", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<TouchControlOverlay>();
        }

        private void Awake()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            _look = UiKit.Image(transform, "LookPad", new Color(1f, 1f, 1f, 0f), null, false);
            UiKit.Fill(_look.rectTransform);
            _look.gameObject.AddComponent<TouchLookPad>().Bind(this);

            var visuals = new GameObject("Visuals", typeof(RectTransform), typeof(CanvasGroup));
            visuals.transform.SetParent(transform, false);
            UiKit.Fill((RectTransform)visuals.transform);
            _visuals = visuals.GetComponent<CanvasGroup>();

            _stick = Circle(visuals.transform, "Joystick", new Color(1f, 1f, 1f, 0.18f), true);
            var handle = Circle(_stick, "Handle", new Color(1f, 1f, 1f, 0.94f), false);
            handle.anchorMin = handle.anchorMax = new Vector2(0.5f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.anchoredPosition = Vector2.zero;
            _stick.gameObject.AddComponent<MobileJoystick>().Configure(handle, 56f);

            _jump = Action(visuals.transform, "Jump", GameInputId.Jump, "Nhảy", "Jump", "ジャンプ", UiTheme.Gold, UiTheme.OnGold);
            _interact = Action(visuals.transform, "Interact", GameInputId.Interact, "F", "F", "F", UiTheme.Gold, UiTheme.OnGold);
            _sprint = Action(visuals.transform, "Sprint", GameInputId.Sprint, "Chạy", "Run", "走る", UiTheme.Teal, UiTheme.OnGold);
            _small.Add(Action(visuals.transform, "Map", GameInputId.Map, "Bản đồ", "Map", "地図", UiTheme.Raised, UiTheme.Text));
            _small.Add(Action(visuals.transform, "Bag", GameInputId.Inventory, "Túi", "Bag", "荷物", UiTheme.Raised, UiTheme.Text));
            _small.Add(Action(visuals.transform, "Chat", GameInputId.Chat, "Chat", "Chat", "会話", UiTheme.Raised, UiTheme.Text));
            _small.Add(Action(visuals.transform, "Talk", GameInputId.PushToTalk, "Nói", "Talk", "話す", UiTheme.Raised, UiTheme.Text));
            _small.Add(Action(visuals.transform, "Menu", GameInputId.Settings, "Menu", "Menu", "メニュー", UiTheme.Raised, UiTheme.Text));
            _small.Add(Tap(visuals.transform, "HideTouch", () =>
            {
                _sessionHidden = true;
                ReleaseAll();
            }, "Ẩn", "Hide", "隠す", new Color(0.07f, 0.08f, 0.1f, 0.92f), UiTheme.Text));

            _restore = Tap(transform, "ShowTouch", () => _sessionHidden = false, "Nút", "Controls", "操作", UiTheme.Gold, UiTheme.OnGold).gameObject;
            Loc.Watch(this, RefreshLabels);
            RefreshLabels();
        }

        private void OnEnable() => TouchControlPrefs.Changed += Apply;
        private void OnDisable()
        {
            TouchControlPrefs.Changed -= Apply;
            ReleaseAll();
        }

        private void Update() => PollTouches();
        private void LateUpdate() => Apply();

        public void Look(Vector2 delta)
        {
            if (!Shown || delta.sqrMagnitude < 0.01f) return;
            var camera = FindCamera();
            if (camera == null) return;
            float sensitivity = Mathf.Max(0.2f, ControlSettings.MouseSensitivity);
            float invert = ControlSettings.InvertY ? -1f : 1f;
            camera.AddLookDegrees(delta.x * 0.12f * sensitivity, -delta.y * 0.12f * sensitivity * invert);
        }

        bool Shown => TouchControlPrefs.WantsVisible && !_sessionHidden;

        ThirdPersonCameraController FindCamera()
        {
            if (_camera == null) _camera = SceneRefs.Find<ThirdPersonCameraController>();
            return _camera;
        }

        void PollTouches()
        {
            if (!Shown)
            {
                _pinching = false;
                _pinchDistance = 0f;
                return;
            }
            var screen = Touchscreen.current;
            if (screen == null)
            {
                _pinching = false;
                _pinchDistance = 0f;
                return;
            }

            Vector2 first = default, second = default;
            int count = 0;
            foreach (var touch in screen.touches)
            {
                if (!touch.press.isPressed) continue;
                if (count == 0) first = touch.position.ReadValue();
                else if (count == 1) second = touch.position.ReadValue();
                count++;
                if (count >= 2) break;
            }

            if (count >= 2)
            {
                float distance = Vector2.Distance(first, second);
                if (_pinchDistance > 1f) FindCamera()?.AddZoom((_pinchDistance - distance) * 0.012f);
                _pinchDistance = distance;
                _pinching = true;
                return;
            }

            _pinching = false;
            _pinchDistance = 0f;
            if (count != 1 || _look.raycastTarget) return;
            var primary = screen.primaryTouch;
            if (!primary.press.isPressed) return;
            int id = primary.touchId.ReadValue();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id)) return;
            Look(primary.delta.ReadValue());
        }

        bool _wasShown;

        void Apply()
        {
            if (!TouchControlPrefs.WantsVisible) _sessionHidden = false;
            bool shown = Shown;
            if (_visuals != null)
            {
                _visuals.alpha = shown ? TouchControlPrefs.Opacity : 0f;
                _visuals.blocksRaycasts = shown;
                _visuals.interactable = shown;
            }
            bool touchDevice = UnityEngine.Device.Application.isMobilePlatform
                || UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld
                || WebGLPointer.Coarse;
            if (_look != null) _look.raycastTarget = shown && touchDevice;
            if (_restore != null) _restore.SetActive(TouchControlPrefs.WantsVisible && _sessionHidden);
            // Release what the overlay held once, when it hides — not every frame while hidden: that wiped any other
            // source of the mobile move / buttons (the legacy on-screen controls, tests) every frame.
            if (!shown && _wasShown) ReleaseAll();
            _wasShown = shown;
            Layout();
        }

        void Layout()
        {
            var root = (RectTransform)transform;
            float width = root.rect.width;
            float height = root.rect.height;
            if (width < 2f || height < 2f) return;

            float scale = TouchControlPrefs.Size;
            bool left = TouchControlPrefs.LeftHanded;
            bool cramped = height < 520f;
            float stickD = (cramped ? 108f : 150f) * scale;
            float big = (cramped ? 70f : 96f) * scale;
            float small = (cramped ? 54f : 64f) * scale;
            float gap = 8f * scale;
            float edge = 16f;
            float stickAnchorX = left ? 1f : 0f;
            float stickSign = left ? -1f : 1f;
            float actionAnchorX = left ? 0f : 1f;
            float actionSign = left ? 1f : -1f;

            if (cramped)
            {
                float y = height - edge - Mathf.Max(stickD, big) * 0.5f;
                var diameters = new[] { big, big, big, small, small, small, small, small, small };
                float used = stickD + edge * 2f;
                for (int i = 0; i < diameters.Length; i++) used += diameters[i] + gap;
                float fit = used > width ? Mathf.Clamp(width / used, 0.62f, 1f) : 1f;
                stickD *= fit;
                big *= fit;
                small *= fit;
                gap *= fit;
                y = height - edge - Mathf.Max(stickD, big) * 0.5f;
                Pin(_stick, new Vector2(stickAnchorX, 0f), new Vector2(stickAnchorX, 0.5f), new Vector2(edge * stickSign, y), stickD);
                float cursor = edge;
                Pin(_jump, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2((cursor + big * 0.5f) * actionSign, y), big);
                cursor += big + gap;
                Pin(_interact, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2((cursor + big * 0.5f) * actionSign, y), big);
                cursor += big + gap;
                Pin(_sprint, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2((cursor + big * 0.5f) * actionSign, y), big);
                cursor += big + gap;
                for (int i = 0; i < _small.Count; i++)
                {
                    Pin(_small[i], new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2((cursor + small * 0.5f) * actionSign, y), small);
                    cursor += small + gap;
                }
            }
            else
            {
                float stickY = Mathf.Clamp(height * 0.34f, 280f, 460f);
                Pin(_stick, new Vector2(stickAnchorX, 0f), new Vector2(stickAnchorX, 0.5f), new Vector2(edge * stickSign, stickY), stickD);
                float baseY = Mathf.Max(300f, 270f + big * 0.5f);
                Pin(_jump, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2(edge * actionSign, baseY), big);
                Pin(_interact, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2(edge * actionSign, baseY + big + gap), big);
                Pin(_sprint, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2((edge + big + gap) * actionSign, baseY + (big + gap) * 0.5f), big);
                float x = edge;
                float y = baseY + (big + gap) * 2f;
                for (int i = 0; i < _small.Count; i++)
                {
                    Pin(_small[i], new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2((x + small * 0.5f) * actionSign, y), small);
                    x += small + gap;
                }
            }

            var handle = _stick.Find("Handle") as RectTransform;
            if (handle != null)
            {
                handle.sizeDelta = new Vector2(stickD * 0.42f, stickD * 0.42f);
                var joystick = _stick.GetComponent<MobileJoystick>();
                if (joystick != null) joystick.Configure(handle, stickD * 0.36f);
            }

            if (_restore != null)
            {
                var chip = (RectTransform)_restore.transform;
                chip.anchorMin = chip.anchorMax = new Vector2(0.5f, 1f);
                chip.pivot = new Vector2(0.5f, 1f);
                chip.anchoredPosition = new Vector2(0f, -12f);
                chip.sizeDelta = new Vector2(148f, 40f);
            }
        }

        void ReleaseAll()
        {
            // Called from OnDisable too: on scene close the service may already be gone, and creating one here
            // would leave a stray "GameInputService" object behind ("Some objects were not cleaned up").
            var input = GameInputService.Instance;
            if (input == null) return;
            input.SetMobileMove(Vector2.zero);
            for (int i = 0; i < _held.Length; i++) input.SetMobileButton(_held[i], false);
        }

        RectTransform Action(Transform parent, string name, GameInputId id, string vi, string en, string ja, Color fill, Color ink)
        {
            var rect = Circle(parent, name, fill, true);
            rect.gameObject.AddComponent<MobileActionButton>().Configure(id);
            AddLabel(rect, vi, en, ja, ink);
            return rect;
        }

        RectTransform Tap(Transform parent, string name, Action click, string vi, string en, string ja, Color fill, Color ink)
        {
            var rect = Circle(parent, name, fill, true);
            var tap = rect.gameObject.AddComponent<TouchTap>();
            tap.Click = click;
            AddLabel(rect, vi, en, ja, ink);
            return rect;
        }

        void AddLabel(RectTransform rect, string vi, string en, string ja, Color ink)
        {
            var label = UiKit.Label(rect, "Caption", vi, 16f, ink, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(label.rectTransform, 4f, 4f, 4f, 4f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = 16f;
            _labels.Add(new LabelRef { Text = label, Vi = vi, En = en, Ja = ja });
        }

        void RefreshLabels()
        {
            for (int i = 0; i < _labels.Count; i++)
            {
                var entry = _labels[i];
                if (entry.Text != null) entry.Text.text = Loc.T(entry.Vi, entry.En, entry.Ja);
            }
        }

        static RectTransform Circle(Transform parent, string name, Color color, bool raycast)
        {
            var image = UiKit.Image(parent, name, color, UiKit.Circle, raycast);
            return image.rectTransform;
        }

        static void Pin(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, float size)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
        }
    }

    sealed class TouchLookPad : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        TouchControlOverlay _overlay;
        public void Bind(TouchControlOverlay overlay) => _overlay = overlay;
        public void OnPointerDown(PointerEventData eventData) { }
        public void OnDrag(PointerEventData eventData)
        {
            if (_overlay == null || _overlay.Pinching) return;
            _overlay.Look(eventData.delta);
        }
    }

    sealed class TouchTap : MonoBehaviour, IPointerClickHandler
    {
        public Action Click;
        public void OnPointerClick(PointerEventData eventData) => Click?.Invoke();
    }
}
