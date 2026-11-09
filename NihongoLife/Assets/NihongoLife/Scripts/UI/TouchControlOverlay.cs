using System;
using System.Collections.Generic;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
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
        TouchLookPad _pad;
        RectTransform _stick;
        MobileJoystick _joystick;
        CanvasGroup _stickGroup;
        readonly List<RectTransform> _arrows = new List<RectTransform>();
        RectTransform _jump;
        RectTransform _interact;
        RectTransform _sprint;
        readonly List<RectTransform> _small = new List<RectTransform>();
        GameObject _restore;
        bool _sessionHidden;
        bool _pinching; // two fingers on a touch screen without the look pad (a touch laptop): see PollTouches
        float _pinchDistance;
        ThirdPersonCameraController _camera;
        readonly List<LabelRef> _labels = new List<LabelRef>();
        readonly GameInputId[] _held =
        {
            GameInputId.Jump, GameInputId.Interact, GameInputId.Sprint, GameInputId.Map,
            GameInputId.Inventory, GameInputId.Chat, GameInputId.PushToTalk, GameInputId.Settings
        };

        public bool Pinching => _pinching || (_pad != null && _pad.Pinching);
        public MobileJoystick Stick => _joystick;

        public static void Attach(Component host)
        {
            if (host == null) return;
            var parent = host.transform;
            var previous = parent.Find("MobileControls");
            if (previous != null) Destroy(previous.gameObject);
            if (parent.Find("TouchControls") != null) return;
            var go = UiBind.Make("TouchControls", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.Need<TouchControlOverlay>();
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
            _pad = _look.gameObject.Need<TouchLookPad>();
            _pad.Bind(this);

            var visuals = UiBind.Make("Visuals", typeof(RectTransform), typeof(CanvasGroup));
            visuals.transform.SetParent(transform, false);
            UiKit.Fill((RectTransform)visuals.transform);
            _visuals = visuals.GetComponent<CanvasGroup>();

            // The stick: dark glass with a white rim and four chevrons (seen on a bright street as on a dark night), a gold knob.
            _stick = Circle(visuals.transform, "Joystick", new Color(0.03f, 0.04f, 0.06f, 0.5f), true);
            _stickGroup = _stick.gameObject.Need<CanvasGroup>();
            Decor(_stick, "Rim", new Color(1f, 1f, 1f, 0.8f), UiKit.RingThin, 0f);
            foreach (var (anchor, angle) in new[] { (new Vector2(0.5f, 0.86f), 180f), (new Vector2(0.5f, 0.14f), 0f), (new Vector2(0.14f, 0.5f), -90f), (new Vector2(0.86f, 0.5f), 90f) })
            {
                var arrow = UiKit.Icon(_stick, "expand_more", new Color(1f, 1f, 1f, 0.7f), 24f);
                arrow.raycastTarget = false;
                var rect = arrow.rectTransform;
                rect.anchorMin = rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localEulerAngles = new Vector3(0f, 0f, angle);
                _arrows.Add(rect);
            }
            var handle = Circle(_stick, "Handle", new Color(0f, 0f, 0f, 0.3f), false); // its shadow, the knob on it
            handle.anchorMin = handle.anchorMax = new Vector2(0.5f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.anchoredPosition = Vector2.zero;
            var knob = Decor(handle, "Knob", UiTheme.Gold, UiKit.Circle, 0.06f);
            knob.rectTransform.offsetMin += new Vector2(0f, 4f);
            knob.rectTransform.offsetMax += new Vector2(0f, 4f);
            Decor(knob.rectTransform, "Shine", new Color(1f, 1f, 1f, 0.3f), UiKit.RingThin, 0.1f);
            _joystick = _stick.gameObject.Need<MobileJoystick>();
            _joystick.Configure(handle, 56f);

            _jump = Action(visuals.transform, "Jump", GameInputId.Jump, "Nhảy", "Jump", "ジャンプ", UiTheme.Gold, UiTheme.OnGold, "keyboard_double_arrow_up");
            _interact = Action(visuals.transform, "Interact", GameInputId.Interact, "F", "F", "F", UiTheme.Gold, UiTheme.OnGold, "touch_app");
            _sprint = Action(visuals.transform, "Sprint", GameInputId.Sprint, "Chạy", "Run", "走る", UiTheme.Teal, Color.white, "sprint");
            var glass = new Color(0.05f, 0.06f, 0.08f, 0.72f);
            _small.Add(Action(visuals.transform, "Map", GameInputId.Map, "Bản đồ", "Map", "地図", glass, Color.white, "map"));
            _small.Add(Action(visuals.transform, "Bag", GameInputId.Inventory, "Túi", "Bag", "荷物", glass, Color.white, "backpack"));
            _small.Add(Action(visuals.transform, "Chat", GameInputId.Chat, "Chat", "Chat", "会話", glass, Color.white, "chat"));
            _small.Add(Action(visuals.transform, "Talk", GameInputId.PushToTalk, "Nói", "Talk", "話す", glass, Color.white, "mic"));
            _small.Add(Action(visuals.transform, "Menu", GameInputId.Settings, "Menu", "Menu", "メニュー", glass, Color.white, "settings"));
            _small.Add(Tap(visuals.transform, "HideTouch", () =>
            {
                _sessionHidden = true;
                ReleaseAll();
            }, "Ẩn", "Hide", "隠す", glass, Color.white, "visibility_off"));

            // Hidden for the session: a gold pill at the top brings them back.
            var pill = UiKit.Image(transform, "ShowTouch", UiTheme.Gold, UiKit.Rounded(20), true);
            pill.gameObject.Need<TouchTap>().Click = () => _sessionHidden = false;
            var pillIcon = UiKit.Icon(pill.transform, "sports_esports", UiTheme.OnGold, 24f);
            pillIcon.raycastTarget = false;
            UiKit.Place(pillIcon.rectTransform, 12f, 8f, 24f, 24f);
            var pillText = UiKit.Label(pill.transform, "Caption", "Hiện nút", 15f, UiTheme.OnGold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            pillText.raycastTarget = false;
            UiKit.Fill(pillText.rectTransform, 42f, 2f, 8f, 2f);
            _labels.Add(new LabelRef { Text = pillText, Vi = "Hiện nút", En = "Controls", Ja = "操作" });
            _restore = pill.gameObject;
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

        /// <summary>Pinch: fingers moving apart (negative) zoom in.</summary>
        public void Zoom(float pixels)
        {
            var camera = FindCamera();
            if (camera != null) camera.AddZoom(pixels * 0.012f);
        }

        /// <summary>A finger landing here moves the stick under it: the lower part of the stick's side of the screen,
        /// below the HUD's top row (the status card and other buttons above the pad still take their own taps).</summary>
        public bool InStickZone(Vector2 screen)
        {
            if (!Shown || _talking || Screen.width <= 0 || Screen.height <= 0) return false;
            float x = screen.x / Screen.width, y = screen.y / Screen.height;
            if (TouchControlPrefs.LeftHanded) x = 1f - x;
            return x < 0.42f && y < 0.74f;
        }

        ThirdPersonCameraController FindCamera()
        {
            if (_camera == null) _camera = SceneRefs.Find<ThirdPersonCameraController>();
            return _camera;
        }

        /// <summary>A touch screen whose touches the look pad does not take (a touch laptop: the pad only catches on a
        /// phone or tablet): one finger looks, two pinch. On a phone the pad does both (TouchLookPad).</summary>
        void PollTouches()
        {
            if (!Shown || _look.raycastTarget)
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
                if (_pinchDistance > 1f) Zoom(_pinchDistance - distance);
                _pinchDistance = distance;
                _pinching = true;
                return;
            }

            _pinching = false;
            _pinchDistance = 0f;
            if (count != 1) return;
            var primary = screen.primaryTouch;
            if (!primary.press.isPressed) return;
            int id = primary.touchId.ReadValue();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id)) return;
            Look(primary.delta.ReadValue());
        }

        bool _wasShown;
        bool _talking;

        void Apply()
        {
            if (!TouchControlPrefs.WantsVisible) _sessionHidden = false;
            bool shown = Shown;
            bool talking = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
            if (talking != _talking)
            {
                // In a conversation only Interact (advance / confirm) stays: the rest would sit on the dialogue box.
                _talking = talking;
                if (talking) { ReleaseAll(); if (_joystick != null) _joystick.Release(); }
                if (_stick != null) _stick.gameObject.SetActive(!talking);
                if (_jump != null) _jump.gameObject.SetActive(!talking);
                if (_sprint != null) _sprint.gameObject.SetActive(!talking);
                foreach (var b in _small) if (b != null) b.gameObject.SetActive(!talking);
            }
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

        /// <summary>
        /// Canvas units the action buttons take from the right (left when left-handed) edge, safe area included, while
        /// they are shown (0 when hidden): the dialogue box and the [F] prompt keep clear of it.
        /// </summary>
        public static float ReservedSide { get; private set; }
        /// <summary>Height of the bottom action row (Jump / Sprint) from the bottom edge while shown, else 0.</summary>
        public static float ReservedBottom { get; private set; }

        void Layout()
        {
            var root = (RectTransform)transform;
            float width = root.rect.width;
            float height = root.rect.height;
            if (width < 2f || height < 2f) return;

            // Safe area (notch, rounded corners) in canvas units, unless a HudSafeArea parent already applied it.
            float insetL = 0f, insetR = 0f, insetB = 0f;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            if (canvas != null && UiScreen.Width > 0 && UiScreen.Height > 0)
            {
                Rect full = ((RectTransform)canvas.transform).rect;
                if (Mathf.Abs(full.width - width) < 1f)
                {
                    float k = full.width / UiScreen.Width;
                    Rect safe = UiScreen.SafeArea;
                    insetL = Mathf.Max(0f, safe.xMin * k);
                    insetR = Mathf.Max(0f, (UiScreen.Width - safe.xMax) * k);
                    insetB = Mathf.Max(0f, safe.yMin * k);
                }
            }

            float scale = TouchControlPrefs.Size;
            bool left = TouchControlPrefs.LeftHanded;
            bool compact = height < 620f; // phones held sideways (canvas ~540 tall)
            float stickD = (compact ? 140f : 150f) * scale;
            float big = (compact ? 80f : 96f) * scale;
            float small = (compact ? 54f : 64f) * scale;
            float gap = 8f * scale;
            const float edge = 16f;
            float stickAnchorX = left ? 1f : 0f;
            float stickSign = left ? -1f : 1f;
            float actionAnchorX = left ? 0f : 1f;
            float actionSign = left ? 1f : -1f;
            float stickEdge = edge + (left ? insetR : insetL);
            float actionEdge = edge + (left ? insetL : insetR);
            float bottom = edge + insetB;

            // Thumb zones at the bottom corners, clear of the HUD at the top (objective, Tools, mailbox bell):
            // Interact above Jump in the corner, Sprint beside Jump, the small buttons in a grid above them.
            Pin(_jump, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2(actionEdge * actionSign, bottom + big * 0.5f), big);
            Pin(_interact, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2(actionEdge * actionSign, bottom + big * 1.5f + gap), big);
            Pin(_sprint, new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2((actionEdge + big + gap) * actionSign, bottom + big * 0.5f), big);
            float gridBase = bottom + (big + gap) * 2f;
            float topReserve = 84f; // the HUD's top row
            int rows = Mathf.Max(1, Mathf.FloorToInt((height - topReserve - gridBase + gap) / (small + gap)));
            int columns = Mathf.Max(2, Mathf.CeilToInt(_small.Count / (float)Mathf.Min(rows, 3)));
            for (int i = 0; i < _small.Count; i++)
            {
                int column = i % columns, row = i / columns;
                float x = actionEdge + (big - small) * 0.5f + column * (small + gap);
                Pin(_small[i], new Vector2(actionAnchorX, 0f), new Vector2(actionAnchorX, 0.5f), new Vector2(x * actionSign, gridBase + small * 0.5f + row * (small + gap)), small);
            }

            float stickY = Mathf.Clamp(height * 0.52f, bottom + stickD * 0.5f, height - topReserve - stickD * 0.5f);
            bool held = _joystick != null && _joystick.Held;
            if (!held) Pin(_stick, new Vector2(stickAnchorX, 0f), new Vector2(stickAnchorX, 0.5f), new Vector2(stickEdge * stickSign, stickY), stickD);
            if (_stickGroup != null) _stickGroup.alpha = held ? 1f : 0.85f;
            foreach (var arrow in _arrows) arrow.sizeDelta = new Vector2(stickD * 0.2f, stickD * 0.2f);

            bool shown = Shown && _visuals != null && _visuals.alpha > 0.01f;
            ReservedSide = shown ? actionEdge + Mathf.Max(big, columns * (small + gap)) + gap : 0f;
            ReservedBottom = shown ? bottom + big + gap : 0f;

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
                chip.sizeDelta = new Vector2(150f, 40f);
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

        RectTransform Action(Transform parent, string name, GameInputId id, string vi, string en, string ja, Color fill, Color ink, string icon = null)
        {
            var rect = Button(parent, name, fill);
            rect.gameObject.Need<MobileActionButton>().Configure(id);
            Face(rect, vi, en, ja, ink, icon);
            return rect;
        }

        RectTransform Tap(Transform parent, string name, Action click, string vi, string en, string ja, Color fill, Color ink, string icon = null)
        {
            var rect = Button(parent, name, fill);
            var tap = rect.gameObject.Need<TouchTap>();
            tap.Click = click;
            Face(rect, vi, en, ja, ink, icon);
            return rect;
        }

        /// <summary>A round button: the root takes the touch (transparent, so the whole button can shrink when pressed),
        /// a soft drop shadow, the coloured disc, a light rim. Only the root takes touches.</summary>
        static RectTransform Button(Transform parent, string name, Color fill)
        {
            var root = Circle(parent, name, new Color(1f, 1f, 1f, 0f), true);
            var shadow = Decor(root, "Shadow", new Color(0f, 0f, 0f, 0.32f), UiKit.Circle, -0.04f);
            shadow.rectTransform.offsetMin += new Vector2(0f, -4f);
            shadow.rectTransform.offsetMax += new Vector2(0f, -4f);
            Decor(root, "Fill", fill, UiKit.Circle, 0f);
            Decor(root, "Rim", new Color(1f, 1f, 1f, fill.a < 0.9f ? 0.32f : 0.5f), UiKit.RingThin, 0f);
            return root;
        }

        /// <summary>A picture-only child filling the parent, inset by a share of its size (negative: larger).</summary>
        static Image Decor(RectTransform parent, string name, Color color, Sprite sprite, float inset)
        {
            var image = UiKit.Image(parent, name, color, sprite, false);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(inset, inset);
            rect.anchorMax = new Vector2(1f - inset, 1f - inset);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return image;
        }

        /// <summary>A button shows its icon, which needs no reading on a small screen; the rest keep their word.</summary>
        void Face(RectTransform rect, string vi, string en, string ja, Color ink, string icon)
        {
            if (string.IsNullOrEmpty(icon))
            {
                AddLabel(rect, vi, en, ja, ink);
                return;
            }
            var glyph = UiKit.Icon(rect, icon, ink, 30f);
            var g = glyph.rectTransform;
            g.anchorMin = new Vector2(0.24f, 0.24f);
            g.anchorMax = new Vector2(0.76f, 0.76f);
            g.offsetMin = g.offsetMax = Vector2.zero;
            glyph.raycastTarget = false;
            glyph.preserveAspect = true;
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

    /// <summary>
    /// The screen behind the controls (a phone or tablet only). A finger landing in the stick zone takes the stick there
    /// (MobileJoystick.PressAt); other fingers look around; two looking fingers pinch to zoom. The finger on the stick is
    /// never part of a pinch, so walking while looking with the other thumb turns the camera instead of zooming.
    /// </summary>
    sealed class TouchLookPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        const int None = int.MinValue;
        TouchControlOverlay _overlay;
        readonly Dictionary<int, Vector2> _looks = new Dictionary<int, Vector2>();
        int _stick = None;

        public bool Pinching => _looks.Count >= 2;
        public void Bind(TouchControlOverlay overlay) => _overlay = overlay;
        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_overlay == null) return;
            var stick = _overlay.Stick;
            if (_stick == None && stick != null && !stick.Held && stick.isActiveAndEnabled && _overlay.InStickZone(eventData.position))
            {
                _stick = eventData.pointerId;
                stick.PressAt(eventData);
                return;
            }
            _looks[eventData.pointerId] = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_overlay == null) return;
            if (eventData.pointerId == _stick)
            {
                if (_overlay.Stick != null) _overlay.Stick.OnDrag(eventData);
                return;
            }
            if (!_looks.TryGetValue(eventData.pointerId, out Vector2 last)) return;
            if (_looks.Count >= 2)
            {
                Vector2 other = last;
                foreach (var kv in _looks) if (kv.Key != eventData.pointerId) { other = kv.Value; break; }
                _overlay.Zoom(Vector2.Distance(last, other) - Vector2.Distance(eventData.position, other));
            }
            else _overlay.Look(eventData.delta);
            _looks[eventData.pointerId] = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _stick)
            {
                _stick = None;
                if (_overlay != null && _overlay.Stick != null) _overlay.Stick.OnPointerUp(eventData);
                return;
            }
            _looks.Remove(eventData.pointerId);
        }

        void OnDisable()
        {
            _looks.Clear();
            _stick = None;
        }
    }

    sealed class TouchTap : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Action Click;
        public void OnPointerClick(PointerEventData eventData) => Click?.Invoke();
        public void OnPointerDown(PointerEventData eventData) => transform.localScale = Vector3.one * 0.92f;
        public void OnPointerUp(PointerEventData eventData) => transform.localScale = Vector3.one;
        void OnDisable() => transform.localScale = Vector3.one;
    }
}
