using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.UI;

namespace NihongoLife.Home
{
    /// <summary>
    /// Room-level controller for 45_HomeBedroom. The furniture, lights and interactables are baked into
    /// the scene by HomeBedroomBuilder; this component only owns what must exist at runtime:
    /// the entry title card, the indoor camera mode, the sleep fade, the vocabulary review card used by
    /// the study desk, short toasts, and an interaction prompt for when the zone is played on its own
    /// (no city HUD loaded). When the city HUD is present it already shows prompts and status, so this
    /// component never duplicates them.
    /// </summary>
    public sealed class HomeBedroomRuntime : MonoBehaviour
    {
        public static HomeBedroomRuntime Instance { get; private set; }
        public event Action OnNewDay;

        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private string roomTitleJa = "じぶんの へや";
        [SerializeField] private string roomTitleVi = "Phòng trọ của bạn · Hibari-chō 2-14";

        private static readonly Color Ink = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        private static readonly Color Muted = new Color(0.72f, 0.78f, 0.86f, 1f);
        private static readonly Color Correct = new Color(0.25f, 0.68f, 0.45f, 1f);
        private static readonly Color Wrong = new Color(0.82f, 0.33f, 0.3f, 1f);

        private Canvas _canvas;
        private CanvasGroup _titleCard;
        private RectTransform _promptChip;
        private TextMeshProUGUI _promptText;
        private CanvasGroup _toast;
        private TextMeshProUGUI _toastText;
        private CanvasGroup _sleepFade;
        private TextMeshProUGUI _sleepMessage;
        private Coroutine _toastRoutine;
        private InteractionDetector _detector;
        private bool _hasCityHud;
        private ThirdPersonCameraController _camera;

        // ─────────── Study card ───────────
        private GameObject _studyRoot;
        private TextMeshProUGUI _studyProgress;
        private TextMeshProUGUI _studyWord;
        private TextMeshProUGUI _studyReading;
        private TextMeshProUGUI _studyFeedback;
        private readonly List<Button> _studyButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> _studyLabels = new List<TextMeshProUGUI>();
        private int _studyAnswer = -1;

        public bool IsStudyOpen => _studyRoot != null && _studyRoot.activeSelf;
        public string CurrentStudyJapanese => _studyWord != null ? _studyWord.text : string.Empty;
        public bool IsPromptVisible => _promptChip != null && _promptChip.gameObject.activeInHierarchy;
        public string PromptText => _promptText != null ? _promptText.text : string.Empty;
        public bool IsToastVisible => _toast != null && _toast.alpha > 0.5f;
        public string ToastText => _toastText != null ? _toastText.text : string.Empty;
        public bool IsTitleVisible => _titleCard != null && _titleCard.alpha > 0.5f;

        private void Awake()
        {
            Instance = this;
            gameObject.name = "HomeBedroom_YourRoom";
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            SetIndoorCamera(true);
        }

        private void OnDisable()
        {
            SetIndoorCamera(false);
            if (_canvas != null && _promptChip != null) _promptChip.gameObject.SetActive(false);
        }

        [SerializeField] private float entranceStartYaw = -40f;
        [SerializeField] private float entranceEndYaw = 12f;
        [SerializeField] private float entrancePitch = 24f;
        [SerializeField] private float entranceDistance = 2.9f;
        [SerializeField] private float entranceSeconds = 2.2f;

        private void Start()
        {
            BuildCanvas();
            SetIndoorCamera(true);
            StartCoroutine(EntranceShot());
        }

        /// <summary>Establishing orbit into the room once the zone transition has finished (the scene's
        /// Start runs while the loading screen is still up, before the player is moved to the spawn).</summary>
        private IEnumerator EntranceShot()
        {
            var flow = FindFirstObjectByType<SceneFlowController>();
            while (flow != null && flow.IsLoading) yield return null;
            yield return null;
            StartCoroutine(ShowTitleCard());
            // Arriving home completes "go home" steps (intro: 「ただいま」 must happen in the room, not at the konbini).
            ScenarioManager.Instance?.OnAreaEntered(WorldLocationCatalog.HomeBedroomEntrance);
            if (_camera == null) _camera = FindFirstObjectByType<ThirdPersonCameraController>();
            if (_camera == null) yield break;
            SetIndoorCamera(true);
            var player = FindFirstObjectByType<PlayerController>();
            bool wasLocked = _camera.IsLocked;
            _camera.IsLocked = true;
            if (player != null) player.InputLocked = true;
            for (float t = 0f; t < entranceSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / entranceSeconds);
                float yaw = Mathf.Lerp(entranceStartYaw, entranceEndYaw, k);
                _camera.SetOrbit(yaw, entrancePitch, SafeOrbitDistance(player, yaw));
                yield return null;
            }
            _camera.SetOrbit(entranceEndYaw, entrancePitch, SafeOrbitDistance(player, entranceEndYaw));
            _camera.IsLocked = wasLocked;
            if (player != null) player.InputLocked = false;
        }

        private void Update()
        {
            UpdateStandalonePrompt();
            if (IsStudyOpen) HandleStudyKeys();
        }

        public void ResetDailyActivities() => OnNewDay?.Invoke();

        /// <summary>Ceiling/kitchen lights follow the wall switch; lamps marked as night lights stay on.</summary>
        public void SetRoomLights(bool lit, bool includeInactive)
        {
            var roomSwitch = GetComponentInChildren<RoomLightSwitch>(includeInactive);
            if (roomSwitch != null) roomSwitch.SetLit(lit, false);
            else foreach (var light in GetComponentsInChildren<Light>(includeInactive)) light.enabled = lit;
        }

        // ─────────── Sleep fade (used by BedroomRestInteractable) ───────────

        public IEnumerator FadeToNight(string title, string subtitle)
        {
            BuildCanvas();
            _sleepMessage.text = $"<size=140%>{title}</size>\n<color=#B8C4D6>{subtitle}</color>\n\n<size=160%>Z z z</size>";
            yield return Fade(_sleepFade, 1f, 1.1f);
            yield return new WaitForSeconds(1.6f);
        }

        public IEnumerator FadeToMorning(string title, string subtitle, string summary)
        {
            BuildCanvas();
            _sleepMessage.text = $"<size=140%>{title}</size>\n<color=#F2B233>{subtitle}</color>\n\n<size=80%><color=#B8C4D6>{summary}</color></size>";
            yield return new WaitForSeconds(2.2f);
            yield return Fade(_sleepFade, 0f, 0.9f);
        }

        // ─────────── Toast ───────────

        public void ShowToast(string japanese, string vietnamese)
        {
            BuildCanvas();
            _toastText.text = string.IsNullOrEmpty(japanese) ? vietnamese : $"<color=#F2B233>{japanese}</color>   {vietnamese}";
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            yield return Fade(_toast, 1f, 0.2f);
            yield return new WaitForSeconds(2.8f);
            yield return Fade(_toast, 0f, 0.35f);
        }

        // ─────────── Study card (used by StudyDeskInteractable) ───────────

        /// <summary>Runs a multiple-choice review. onFinished receives the number of correct answers.</summary>
        public IEnumerator RunStudySession(IReadOnlyList<StudyWord> questions, Action<int> onFinished)
        {
            BuildCanvas();
            SetGameplayLocked(true);
            _studyRoot.SetActive(true);
            UIStyleKit.PlayShowAnimation(_studyRoot);
            int correct = 0;
            for (int q = 0; q < questions.Count; q++)
            {
                StudyWord word = questions[q];
                var options = new List<string> { word.meaning, word.distractorA, word.distractorB };
                Shuffle(options);
                int answerIndex = options.IndexOf(word.meaning);
                _studyProgress.text = $"Câu {q + 1}/{questions.Count}   ·   Đúng {correct}";
                _studyWord.text = word.japanese;
                _studyReading.text = word.reading;
                _studyFeedback.text = "Chọn nghĩa đúng (chuột hoặc phím 1 · 2 · 3)";
                _studyFeedback.color = Muted;
                for (int i = 0; i < _studyButtons.Count; i++)
                {
                    _studyLabels[i].text = $"{i + 1}.  {options[i]}";
                    _studyButtons[i].interactable = true;
                    _studyButtons[i].GetComponent<Image>().color = UIStyleKit.PanelHover;
                }

                _studyAnswer = -1;
                while (_studyAnswer < 0) yield return null;

                bool ok = _studyAnswer == answerIndex;
                if (ok) correct++;
                foreach (var b in _studyButtons) b.interactable = false;
                _studyButtons[answerIndex].GetComponent<Image>().color = Correct;
                if (!ok) _studyButtons[_studyAnswer].GetComponent<Image>().color = Wrong;
                _studyFeedback.text = ok
                    ? $"せいかい！ Đúng rồi — {word.example}"
                    : $"ざんねん… “{word.japanese}” nghĩa là “{word.meaning}”. {word.example}";
                _studyFeedback.color = ok ? Correct : Wrong;
                yield return new WaitForSeconds(ok ? 1.3f : 2.4f);
            }

            _studyRoot.SetActive(false);
            SetGameplayLocked(false);
            onFinished?.Invoke(correct);
        }

        private void HandleStudyKeys()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || _studyAnswer >= 0) return;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) PickAnswer(0);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) PickAnswer(1);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) PickAnswer(2);
        }

        /// <summary>Also used by tests to answer without input devices.</summary>
        public void PickAnswer(int index)
        {
            if (!IsStudyOpen || _studyAnswer >= 0 || index < 0 || index >= _studyButtons.Count) return;
            if (!_studyButtons[index].interactable) return;
            _studyAnswer = index;
        }

        /// <summary>Index of the option that holds the given meaning on the current question (tests).</summary>
        public int FindOption(string meaning)
        {
            for (int i = 0; i < _studyLabels.Count; i++)
                if (_studyLabels[i].text.EndsWith(meaning, StringComparison.Ordinal)) return i;
            return -1;
        }

        private void SetGameplayLocked(bool locked)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.InputLocked = locked;
            if (_camera == null) _camera = FindFirstObjectByType<ThirdPersonCameraController>();
            if (_camera != null) _camera.IsLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        // ─────────── Prompt when the zone runs without the city HUD ───────────

        private void UpdateStandalonePrompt()
        {
            if (_promptChip == null) return;
            if (_hasCityHud || IsStudyOpen || _sleepFade.alpha > 0.01f)
            {
                _promptChip.gameObject.SetActive(false);
                return;
            }

            if (_detector == null) _detector = FindFirstObjectByType<InteractionDetector>();
            IInteractable current = _detector != null ? _detector.CurrentInteractable : null;
            bool show = current != null && (current as MonoBehaviour) != null;
            _promptChip.gameObject.SetActive(show);
            if (!show) return;
            string key = GameInputService.GetOrCreate().GetBindingLabel(GameInputId.Interact);
            _promptText.text = $"<color=#F2B233>[{key}]</color>  {current.GetPromptJa()}  <color=#B8C4D6>· {current.GetpromptEn()}</color>";
        }

        // ─────────── Camera ───────────

        /// <summary>SetOrbit snaps the camera distance, bypassing the controller's wall collision, so the
        /// establishing shot clamps the distance against the room walls itself.</summary>
        private float SafeOrbitDistance(PlayerController player, float yaw)
        {
            if (player == null) return entranceDistance;
            Vector3 pivot = player.transform.position + Vector3.up * 1.45f;
            Vector3 direction = Quaternion.Euler(entrancePitch, yaw, 0f) * Vector3.back;
            if (Physics.SphereCast(pivot, 0.25f, direction, out RaycastHit hit, entranceDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return Mathf.Max(0.6f, hit.distance - 0.3f);
            return entranceDistance;
        }

        private void SetIndoorCamera(bool indoor)
        {
            if (_camera == null) _camera = FindFirstObjectByType<ThirdPersonCameraController>();
            if (_camera != null) _camera.SetIndoorMode(indoor);
        }

        // ─────────── Building ───────────

        private void BuildCanvas()
        {
            if (_canvas != null) return;
            if (font == null) font = TMP_Settings.defaultFontAsset;
            _hasCityHud = FindFirstObjectByType<HUDUI>() != null;

            var root = new GameObject("HomeRoomCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 900; // above the city HUD so the sleep fade covers it
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Entry title card — top centre, fades out after a few seconds.
            var title = Panel(root.transform, "RoomTitle", new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(560f, 112f), Ink);
            _titleCard = title.gameObject.AddComponent<CanvasGroup>();
            _titleCard.alpha = 0f;
            var titleJa = Label(title, roomTitleJa, 34, Color.white, TextAlignmentOptions.Center);
            Place(titleJa.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(520f, 56f));
            var titleVi = Label(title, roomTitleVi, 19, new Color(0.95f, 0.72f, 0.25f), TextAlignmentOptions.Center);
            Place(titleVi.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(520f, 32f));

            // Interaction prompt (standalone only) — bottom centre.
            _promptChip = Panel(root.transform, "RoomPrompt", new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(620f, 58f), Ink);
            _promptText = Label(_promptChip, string.Empty, 22, Color.white, TextAlignmentOptions.Center);
            Stretch(_promptText.rectTransform, 12f);
            _promptChip.gameObject.SetActive(false);

            // Toast — above the prompt.
            var toast = Panel(root.transform, "RoomToast", new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(760f, 60f), Ink);
            _toast = toast.gameObject.AddComponent<CanvasGroup>();
            _toast.alpha = 0f;
            _toast.blocksRaycasts = false;
            _toastText = Label(toast, string.Empty, 21, Color.white, TextAlignmentOptions.Center);
            Stretch(_toastText.rectTransform, 14f);

            BuildStudyCard(root.transform);

            // Sleep fade — full screen, above everything else in the room canvas.
            var night = new GameObject("SleepFade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            night.transform.SetParent(root.transform, false);
            Stretch((RectTransform)night.transform, 0f);
            night.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 1f);
            _sleepFade = night.GetComponent<CanvasGroup>();
            _sleepFade.alpha = 0f;
            _sleepFade.blocksRaycasts = false;
            _sleepMessage = Label((RectTransform)night.transform, string.Empty, 34, Color.white, TextAlignmentOptions.Center);
            Place(_sleepMessage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 420f));
            _sleepFade.transform.SetAsLastSibling();
        }

        private void BuildStudyCard(Transform root)
        {
            var card = Panel(root, "StudyCard", new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(760f, 560f), UIStyleKit.PanelBase);
            _studyRoot = card.gameObject;
            var header = Label(card, "勉強する  ·  Ôn từ vựng N5", 28, new Color(0.95f, 0.72f, 0.25f), TextAlignmentOptions.Left);
            Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(680f, 40f));
            _studyProgress = Label(card, string.Empty, 18, Muted, TextAlignmentOptions.Right);
            Place(_studyProgress.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(680f, 36f));
            _studyWord = Label(card, string.Empty, 64, Color.white, TextAlignmentOptions.Center);
            Place(_studyWord.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(680f, 96f));
            _studyReading = Label(card, string.Empty, 22, Muted, TextAlignmentOptions.Center);
            Place(_studyReading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(680f, 34f));

            for (int i = 0; i < 3; i++)
            {
                var buttonRect = Panel(card, "Answer_" + (i + 1), new Vector2(0.5f, 1f), new Vector2(0f, -258f - i * 70f), new Vector2(640f, 58f), UIStyleKit.PanelHover);
                var button = buttonRect.gameObject.AddComponent<Button>();
                button.targetGraphic = buttonRect.GetComponent<Image>();
                int index = i;
                button.onClick.AddListener(() => PickAnswer(index));
                var label = Label(buttonRect, string.Empty, 22, Color.white, TextAlignmentOptions.Left);
                Stretch(label.rectTransform, 22f);
                _studyButtons.Add(button);
                _studyLabels.Add(label);
            }

            _studyFeedback = Label(card, string.Empty, 18, Muted, TextAlignmentOptions.Center);
            Place(_studyFeedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(680f, 56f));
            _studyFeedback.textWrappingMode = TextWrappingModes.Normal;
            _studyRoot.SetActive(false);
        }

        private IEnumerator ShowTitleCard()
        {
            if (_titleCard == null) yield break;
            yield return new WaitForSeconds(0.4f);
            yield return Fade(_titleCard, 1f, 0.45f);
            yield return new WaitForSeconds(3f);
            yield return Fade(_titleCard, 0f, 0.6f);
        }

        private static IEnumerator Fade(CanvasGroup group, float target, float seconds)
        {
            if (group == null) yield break;
            float start = group.alpha;
            group.blocksRaycasts = target > 0f && group.gameObject.name == "SleepFade";
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            group.alpha = target;
        }

        private RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Place(rect, anchor, position, size);
            var image = go.GetComponent<Image>();
            image.sprite = UIStyleKit.RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            return rect;
        }

        private TextMeshProUGUI Label(Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, 0f);
            rect.offsetMax = new Vector2(-padding, 0f);
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
