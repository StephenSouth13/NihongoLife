using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Exam.Ielts;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Full-screen IELTS test (Listening validated first; Reading passages, Writing tasks and Speaking prompts use
    /// the same screen). Two explicit modes:
    /// · Practice — free play / pause / seek on every part, no time pressure.
    /// · Exam — each part's recording plays once, in order, with no pause or seeking; after Part 4 there are
    ///   reviewSeconds to check answers, then the test is submitted automatically.
    /// Answers save after every change and the attempt resumes where it was left. The answer key is loaded only
    /// when the test is submitted. Esc / × ask before leaving an unfinished attempt (UiModalStack, immersive).
    /// </summary>
    public sealed class IeltsTestUI : MonoBehaviour
    {
        public const string PracticeMode = "practice";
        public const string ExamMode = "exam";

        private static readonly Color Bg = new Color(0.05f, 0.065f, 0.085f, 0.985f);
        private static readonly Color Paper = new Color(0.97f, 0.965f, 0.95f, 1f);
        private static readonly Color PaperInk = new Color(0.11f, 0.13f, 0.17f, 1f);
        private static readonly Color PaperMuted = new Color(0.36f, 0.4f, 0.47f, 1f);
        private static readonly Color PaperLine = new Color(0.82f, 0.81f, 0.78f, 1f);
        private static readonly Color Accent = new Color(0.16f, 0.45f, 0.72f, 1f);
        private static readonly Color AnsweredColor = new Color(0.18f, 0.55f, 0.45f, 1f);
        private static readonly Color Good = new Color(0.2f, 0.62f, 0.4f, 1f);
        private static readonly Color Bad = new Color(0.82f, 0.3f, 0.28f, 1f);

        public static IeltsTestUI Instance { get; private set; }

        private IeltsLibrary.Package _package;
        private IeltsTest _test;
        private IeltsAttempt _attempt;
        private readonly Dictionary<int, string> _answers = new();
        private TMP_FontAsset _font;

        private Canvas _canvas;
        private RectTransform _root, _header, _audioBar, _footer, _tabs;
        private TextMeshProUGUI _title, _modeChip, _timer, _audioStatus, _audioTime, _note;
        private readonly List<Button> _partTabs = new();
        private Button _playButton;
        private TextMeshProUGUI _playLabel;
        private RectTransform _progressTrack, _progressFill;
        private RectTransform _bodyArea, _questionContent, _passageContent;
        private ScrollRect _questionScroll, _passageScroll;
        private RectTransform _navigator;
        private readonly Dictionary<int, Image> _navButtons = new();
        private readonly Dictionary<int, RectTransform> _anchors = new();
        private readonly Dictionary<int, Action> _refreshers = new();
        private Button _prevButton, _nextButton, _submitButton;
        private RectTransform _confirm;
        private TextMeshProUGUI _confirmTitle, _confirmBody, _confirmYesLabel;
        private Action _confirmYes;
        private RectTransform _results;

        private AudioSource _audio;
        private int _viewPart;
        private int _audioPart = -1;
        private int _loadingPart = -1;
        private bool _audioPlaying;
        private float _saveAt = -1f;
        private bool _playerWasLocked, _cameraWasLocked;
        private float _layoutWidth = -1f;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public bool IsSubmitted => _attempt != null && _attempt.submitted;
        public bool IsConfirmOpen => _confirm != null && _confirm.gameObject.activeSelf;
        public bool IsExam => _attempt != null && _attempt.mode == ExamMode;
        public int ViewPart => _viewPart;
        public int AudioPart => _audioPart;
        public AudioClip CurrentClip => _audio != null ? _audio.clip : null;
        public bool AudioPlaying => _audio != null && _audio.isPlaying;
        public float AudioTime => _audio != null && _audio.clip != null ? _audio.time : 0f;
        public IeltsGrader.Result LastResult { get; private set; }
        public IeltsTest Test => _test;

        // ─────────── Opening ───────────

        public static IeltsTestUI Open(IeltsLibrary.Package package, string mode, bool resume)
        {
            if (Instance == null)
            {
                var go = new GameObject("IeltsTestUI");
                Instance = go.AddComponent<IeltsTestUI>();
            }
            Instance.Begin(package, mode, resume);
            return Instance;
        }

        private void Awake()
        {
            _font = NLUi.ResolveFont();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            Build();
            UiModalStack.Register(this, () => IsOpen, HandleEscape, "IELTS test", immersive: true);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private void Begin(IeltsLibrary.Package package, string mode, bool resume)
        {
            _package = package;
            _test = package.Test;
            var saved = resume ? IeltsAttemptStore.LoadAttempt(_test.id) : null;
            if (saved != null && !saved.submitted) _attempt = saved;
            else
            {
                _attempt = new IeltsAttempt { testId = _test.id, mode = mode, startedUtc = DateTime.UtcNow.ToString("o") };
                IeltsAttemptStore.ClearAttempt(_test.id);
            }
            int parts = _test.parts.Length;
            if (_attempt.audioPositions == null || _attempt.audioPositions.Length != parts) _attempt.audioPositions = new float[parts];
            if (_attempt.partsFinished == null || _attempt.partsFinished.Length != parts) _attempt.partsFinished = new bool[parts];
            _answers.Clear();
            foreach (var r in _attempt.responses) _answers[r.number] = r.value;
            LastResult = null;

            bool wasOpen = IsOpen;
            _root.gameObject.SetActive(true);
            _canvas.transform.SetAsLastSibling();
            _results.gameObject.SetActive(false);
            _confirm.gameObject.SetActive(false);
            _bodyArea.gameObject.SetActive(true);
            if (!wasOpen) LockGameplay(true);
            _submitButton.interactable = true;
            _audioPart = -1;
            _audioPlaying = false;
            _audio.Stop();

            _title.text = $"<b>{_test.title}</b>  <size=70%><color=#9FB0C4>{(_test.localOnly ? "bản local · không phát hành" : "")}</color></size>";
            _modeChip.text = IsExam ? "CHẾ ĐỘ THI" : "LUYỆN TẬP";
            ApplyResponsiveLayout(force: true);
            // The audio controls belong to Listening only; Reading/Writing keep just the part tabs.
            foreach (var audioOnly in new Component[] { _playButton, _progressTrack, _audioTime, _audioStatus })
                if (audioOnly != null) audioOnly.gameObject.SetActive(IsListening);
            if (IsListening)
                _note.text = IsExam
                    ? "Chế độ thi: mỗi phần nghe phát <b>một lần</b>, không dừng, không tua. Sau phần cuối có " + Mathf.RoundToInt(_test.reviewSeconds / 60f) + " phút kiểm tra, rồi tự nộp."
                    : "Luyện tập: nghe lại, tạm dừng và tua tự do.";
            else
                _note.text = IsExam
                    ? $"Chế độ thi: <b>{_test.timeLimitMinutes} phút</b> cho cả bài, hết giờ tự nộp. Không có thời gian chép đáp án riêng."
                    : $"Luyện tập: không giới hạn thời gian (bài thật {_test.timeLimitMinutes} phút).";
            BuildTabs();
            BuildNavigator();
            ShowPart(Mathf.Clamp(_attempt.partIndex, 0, parts - 1));
            // Practice: ShowPart loads the viewed part's audio. Exam: the candidate presses "Bắt đầu nghe".
            if (IsListening && IsExam) StartCoroutine(PrepareAudio(FirstUnfinishedPart(), false));
        }

        private bool IsListening => _test.parts.Any(p => !string.IsNullOrEmpty(p.audio));

        private int FirstUnfinishedPart()
        {
            for (int i = 0; i < _attempt.partsFinished.Length; i++) if (!_attempt.partsFinished[i]) return i;
            return _attempt.partsFinished.Length - 1;
        }

        private void LockGameplay(bool locked)
        {
            var player = FindFirstObjectByType<PlayerController>();
            var camera = FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (locked)
            {
                _playerWasLocked = player != null && player.InputLocked;
                _cameraWasLocked = camera != null && camera.IsLocked;
                if (player != null) player.InputLocked = true;
                if (camera != null) camera.IsLocked = true;
            }
            else
            {
                if (player != null) player.InputLocked = _playerWasLocked;
                if (camera != null) camera.IsLocked = _cameraWasLocked;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ─────────── Answers ───────────

        public string GetAnswer(int number) => _answers.TryGetValue(number, out string v) ? v : string.Empty;

        public void SetAnswer(int number, string value)
        {
            if (IsSubmitted) return;
            value = value ?? string.Empty;
            if (value.Length == 0) _answers.Remove(number); else _answers[number] = value;
            if (_navButtons.TryGetValue(number, out var nav)) nav.color = value.Length > 0 ? AnsweredColor : new Color(1f, 1f, 1f, 0.08f);
            if (_refreshers.TryGetValue(number, out var refresh)) refresh();
            _saveAt = Time.unscaledTime + 0.4f;
        }

        public int AnsweredCount => _answers.Count(kv => kv.Value.Length > 0);
        public int QuestionCount => _test.parts.SelectMany(p => p.groups ?? Array.Empty<IeltsGroup>()).Where(g => g.type != "writing_task" && g.type != "speaking_part").Sum(g => g.to - g.from + 1);

        private void Save()
        {
            if (_attempt == null || IsSubmitted) return;
            _attempt.partIndex = _viewPart;
            if (_audioPart >= 0 && _audio.clip != null) _attempt.audioPositions[_audioPart] = _audio.time;
            _attempt.responses = _answers.Select(kv => new IeltsResponse { number = kv.Key, value = kv.Value }).OrderBy(r => r.number).ToList();
            IeltsAttemptStore.SaveAttempt(_attempt);
            _saveAt = -1f;
        }

        // ─────────── Navigation ───────────

        public void ShowPart(int index)
        {
            _viewPart = Mathf.Clamp(index, 0, _test.parts.Length - 1);
            for (int i = 0; i < _partTabs.Count; i++)
            {
                bool on = i == _viewPart;
                _partTabs[i].GetComponent<Image>().color = on ? Accent : new Color(1f, 1f, 1f, 0.08f);
            }
            RenderPart(_test.parts[_viewPart]);
            _prevButton.interactable = _viewPart > 0;
            _nextButton.interactable = _viewPart < _test.parts.Length - 1;
            if (!IsExam && IsListening && _audioPart != _viewPart && _loadingPart != _viewPart) StartCoroutine(PrepareAudio(_viewPart, false));
            _saveAt = Time.unscaledTime + 0.4f;
        }

        public void GoToQuestion(int number)
        {
            int part = Array.FindIndex(_test.parts, p => (p.groups ?? Array.Empty<IeltsGroup>()).Any(g => g.Covers(number)));
            if (part < 0) return;
            if (part != _viewPart) ShowPart(part);
            StartCoroutine(ScrollToAnchor(number));
        }

        private IEnumerator ScrollToAnchor(int number)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (!_anchors.TryGetValue(number, out var anchor) || anchor == null) yield break;
            float contentHeight = _questionContent.rect.height;
            float viewport = ((RectTransform)_questionScroll.viewport).rect.height;
            if (contentHeight <= viewport) yield break;
            Vector3 local = _questionContent.InverseTransformPoint(anchor.position);
            float y = -local.y - 60f;
            _questionScroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(y / (contentHeight - viewport));
        }

        // ─────────── Audio ───────────

        private IEnumerator PrepareAudio(int partIndex, bool autoplay)
        {
            var part = _test.parts[partIndex];
            string path = IeltsLibrary.AudioPath(_package, part);
            if (path == null) yield break;
            _loadingPart = partIndex;
            _audio.Stop();
            _audioStatus.text = $"Đang tải audio {part.title}…";
            AudioClip clip = null;
            yield return IeltsLibrary.LoadAudio(path, c => clip = c);
            if (_loadingPart != partIndex) yield break; // a newer request replaced this one
            _loadingPart = -1;
            if (clip == null) { _audioStatus.text = "Không tải được audio của phần này."; yield break; }
            if (_audio.clip != null && _audio.clip != clip) Destroy(_audio.clip);
            _audio.clip = clip;
            _audioPart = partIndex;
            _audio.time = Mathf.Clamp(_attempt.audioPositions[partIndex], 0f, Mathf.Max(0f, clip.length - 0.05f));
            if (autoplay) { _audio.Play(); _audioPlaying = true; }
            RefreshAudioBar();
        }

        public void TogglePlay()
        {
            if (_audio.clip == null || IsSubmitted) return;
            if (IsExam)
            {
                // The recording cannot be paused in exam conditions; starting it is allowed.
                if (!_audio.isPlaying) { _audio.Play(); _audioPlaying = true; }
                return;
            }
            if (_audio.isPlaying) { _audio.Pause(); _audioPlaying = false; }
            else { _audio.Play(); _audioPlaying = true; }
            RefreshAudioBar();
        }

        public void Seek(float seconds)
        {
            if (IsExam || _audio.clip == null) return;
            _audio.time = Mathf.Clamp(seconds, 0f, _audio.clip.length - 0.05f);
            RefreshAudioBar();
        }

        private void OnTrackClicked(BaseEventData data)
        {
            if (IsExam || _audio.clip == null || !(data is PointerEventData pointer)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_progressTrack, pointer.position, pointer.pressEventCamera, out Vector2 local);
            float t = Mathf.InverseLerp(_progressTrack.rect.xMin, _progressTrack.rect.xMax, local.x);
            Seek(t * _audio.clip.length);
        }

        private void RefreshAudioBar()
        {
            if (!IsListening) return;
            var clip = _audio.clip;
            float length = clip != null ? clip.length : 0f;
            float time = clip != null ? _audio.time : 0f;
            _progressFill.anchorMax = new Vector2(length > 0f ? time / length : 0f, 1f);
            _audioTime.text = $"{Clock(time)} / {Clock(length)}";
            string partName = _audioPart >= 0 ? _test.parts[_audioPart].title : "—";
            if (IsExam)
            {
                _playLabel.text = _audio.isPlaying ? "Đang phát" : "▶  Bắt đầu nghe";
                _playButton.interactable = !_audio.isPlaying && !IsSubmitted && _attempt.reviewRemaining < 0f;
                _audioStatus.text = _attempt.reviewRemaining >= 0f ? "Đã nghe xong 4 phần" : $"Đang nghe: {partName}";
            }
            else
            {
                _playLabel.text = _audio.isPlaying ? "❚❚  Tạm dừng" : "▶  Phát";
                _playButton.interactable = clip != null;
                _audioStatus.text = $"Audio: {partName}";
            }
        }

        private void Update()
        {
            if (!IsOpen || _attempt == null) return;
            ApplyResponsiveLayout();
            if (!IsSubmitted) _attempt.elapsedSeconds += Time.unscaledDeltaTime;

            if (IsListening)
            {
                // Exam mode: when a part's recording ends, move straight on to the next one.
                if (IsExam && _audioPlaying && !_audio.isPlaying && _audio.clip != null && _loadingPart < 0)
                {
                    if (_audio.time <= 0.01f || _audio.time >= _audio.clip.length - 0.25f)
                    {
                        _audioPlaying = false;
                        _attempt.partsFinished[_audioPart] = true;
                        _attempt.audioPositions[_audioPart] = _audio.clip.length;
                        int next = FirstUnfinishedPart();
                        if (!_attempt.partsFinished[next]) { StartCoroutine(PrepareAudio(next, true)); if (_viewPart != next) ShowPart(next); }
                        else if (_attempt.reviewRemaining < 0f) _attempt.reviewRemaining = _test.reviewSeconds;
                        _saveAt = Time.unscaledTime;
                    }
                }
                if (!IsExam && _audioPlaying && !_audio.isPlaying && _audio.clip != null && _audio.time <= 0.01f) _audioPlaying = false;
                RefreshAudioBar();
            }

            if (IsExam && !IsSubmitted && _attempt.reviewRemaining >= 0f)
            {
                _attempt.reviewRemaining -= Time.unscaledDeltaTime;
                if (_attempt.reviewRemaining <= 0f) { SubmitNow(); return; }
            }

            // Reading / Writing: one time limit for the whole paper (exam mode submits when it runs out).
            float limit = !IsListening && _test.timeLimitMinutes > 0 ? _test.timeLimitMinutes * 60f : 0f;
            if (limit > 0f && IsExam && !IsSubmitted)
            {
                float left = limit - _attempt.elapsedSeconds;
                if (left <= 0f) { SubmitNow(); return; }
                if (left <= 600f && left + Time.unscaledDeltaTime > 600f) NihongoLife.UI.HudFeed.Post("Còn 10 phút.", NihongoLife.UI.HudFeed.Kind.Warning, 4f);
                if (left <= 60f && left + Time.unscaledDeltaTime > 60f) NihongoLife.UI.HudFeed.Post("Còn 1 phút — bài sẽ tự nộp khi hết giờ.", NihongoLife.UI.HudFeed.Kind.Warning, 4f);
                _timer.text = $"Còn lại: <b>{(left <= 300f ? "<color=#E0644E>" : "")}{Clock(left)}{(left <= 300f ? "</color>" : "")}</b>   ·   Đã làm {AnsweredCount}/{QuestionCount}";
            }
            else if (limit > 0f)
                _timer.text = $"Thời gian: <b>{Clock(_attempt.elapsedSeconds)}</b> / {_test.timeLimitMinutes} phút   ·   Đã làm {AnsweredCount}/{QuestionCount}";
            else
                _timer.text = IsExam && _attempt.reviewRemaining >= 0f
                    ? $"Kiểm tra lại: <b>{Clock(Mathf.Max(0f, _attempt.reviewRemaining))}</b>"
                    : $"Thời gian: <b>{Clock(_attempt.elapsedSeconds)}</b>   ·   Đã làm {AnsweredCount}/{QuestionCount}";

            if (_saveAt > 0f && Time.unscaledTime >= _saveAt) Save();
            if (_audioPlaying && Time.frameCount % 120 == 0) Save();
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }

        // ─────────── Leaving / submitting ───────────

        private void HandleEscape()
        {
            if (IsConfirmOpen) { _confirm.gameObject.SetActive(false); return; }
            RequestClose();
        }

        public void RequestClose()
        {
            if (IsSubmitted) { Close(); return; }
            ShowConfirm("Tạm rời bài thi?",
                IsExam ? "Câu trả lời đã được lưu. Audio dừng lại và phát tiếp từ chỗ này khi bạn quay lại (trong kỳ thi thật thì không dừng được)."
                       : "Câu trả lời và vị trí audio đã được lưu. Mở lại từ Trung tâm luyện thi để làm tiếp.",
                "Tạm rời", Close);
        }

        public void Close()
        {
            Save();
            _audio.Stop();
            _audioPlaying = false;
            _confirm.gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
            LockGameplay(false);
        }

        public void RequestSubmit()
        {
            int missing = QuestionCount - AnsweredCount;
            ShowConfirm("Nộp bài?", missing > 0 ? $"Còn <b>{missing}</b> câu chưa trả lời. Sau khi nộp sẽ không sửa được nữa." : "Bạn đã trả lời tất cả các câu. Sau khi nộp sẽ không sửa được nữa.",
                "Nộp bài", SubmitNow);
        }

        /// <summary>Grades with the key (loaded only now), records history and shows the review.</summary>
        public void SubmitNow()
        {
            if (IsSubmitted) return;
            Save();
            _audio.Stop();
            _audioPlaying = false;
            var key = IeltsLibrary.LoadKey(_package);
            _confirm.gameObject.SetActive(false);
            if (key == null)
            {
                ShowConfirm("Thiếu đáp án", "Gói đề này không có key.json hợp lệ nên chưa chấm được. Bài làm vẫn được lưu.", "Đóng", () => _confirm.gameObject.SetActive(false));
                return;
            }
            LastResult = IeltsGrader.Grade(_test, key, _answers);
            _attempt.submitted = true;
            _attempt.rawScore = LastResult.Raw;
            _attempt.band = LastResult.Band;
            IeltsAttemptStore.ClearAttempt(_test.id);
            IeltsAttemptStore.AddHistory(new IeltsHistoryEntry
            {
                testId = _test.id, finishedUtc = DateTime.UtcNow.ToString("o"), mode = _attempt.mode,
                rawScore = LastResult.Raw, maxScore = LastResult.Max, band = LastResult.Band,
            });
            ShowResults();
        }

        private void ShowConfirm(string title, string body, string yes, Action onYes)
        {
            _confirmTitle.text = title;
            _confirmBody.text = body;
            _confirmYesLabel.text = yes;
            _confirmYes = onYes;
            _confirm.gameObject.SetActive(true);
            _confirm.SetAsLastSibling();
        }

        // ─────────── Building the screen ───────────

        private void Build()
        {
            _canvas = NLUi.CreateCanvas("IeltsTestCanvas", 760);
            _root = new GameObject("IeltsRoot", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _root.SetParent(_canvas.transform, false);
            NLUi.Stretch(_root);
            _root.GetComponent<Image>().color = Bg;

            // Header
            _header = Strip(_root, "Header", 0f, 70f, top: true, new Color(0.08f, 0.1f, 0.13f, 1f));
            _title = Text(_header, "Title", "", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            Place(_title.rectTransform, 28f, 0f, 1000f, 70f, Vector2.zero);
            _title.overflowMode = TextOverflowModes.Ellipsis;
            _modeChip = Text(_header, "Mode", "", 15f, NLUi.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            Place(_modeChip.rectTransform, -560f, 0f, 160f, 70f, new Vector2(1f, 0f));
            _timer = Text(_header, "Timer", "", 17f, new Color(0.85f, 0.9f, 0.96f), TextAlignmentOptions.MidlineRight);
            Place(_timer.rectTransform, -96f, 0f, 460f, 70f, new Vector2(1f, 0f));
            _timer.overflowMode = TextOverflowModes.Ellipsis;
            NLUi.CloseButton(_header, _font, RequestClose, 46f, 12f);

            // Audio bar
            _audioBar = Strip(_root, "AudioBar", 70f, 74f, top: true, new Color(0.065f, 0.085f, 0.11f, 1f));
            _tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            _tabs.SetParent(_audioBar, false);
            Place(_tabs, 28f, 14f, 520f, 46f, Vector2.zero);
            var tabLayout = _tabs.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8f; tabLayout.childForceExpandWidth = false; tabLayout.childControlWidth = true; tabLayout.childControlHeight = true;
            _playButton = NLUi.Button(_audioBar, "Play", "▶  Phát", _font, TogglePlay, Accent, 17f, Color.white, 46f);
            _playLabel = _playButton.GetComponentInChildren<TextMeshProUGUI>();
            Place((RectTransform)_playButton.transform, 570f, 14f, 170f, 46f, Vector2.zero);
            _progressTrack = new GameObject("Progress", typeof(RectTransform), typeof(Image), typeof(EventTrigger)).GetComponent<RectTransform>();
            _progressTrack.SetParent(_audioBar, false);
            Place(_progressTrack, 760f, 31f, 520f, 12f, Vector2.zero);
            _progressTrack.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var trigger = _progressTrack.GetComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(OnTrackClicked);
            trigger.triggers.Add(entry);
            _progressFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _progressFill.SetParent(_progressTrack, false);
            _progressFill.anchorMin = Vector2.zero; _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = _progressFill.offsetMax = Vector2.zero;
            _progressFill.GetComponent<Image>().color = NLUi.Gold;
            _progressFill.GetComponent<Image>().raycastTarget = false;
            _audioTime = Text(_audioBar, "Time", "00:00 / 00:00", 16f, Color.white, TextAlignmentOptions.MidlineLeft);
            Place(_audioTime.rectTransform, 1296f, 14f, 150f, 46f, Vector2.zero);
            _audioStatus = Text(_audioBar, "Status", "", 14f, NLUi.Muted, TextAlignmentOptions.MidlineLeft);
            Place(_audioStatus.rectTransform, 760f, 44f, 520f, 26f, Vector2.zero);
            _note = Text(_audioBar, "Note", "", 13.5f, NLUi.Soft, TextAlignmentOptions.MidlineRight);
            _note.textWrappingMode = TextWrappingModes.Normal;
            Place(_note.rectTransform, -28f, 6f, 430f, 62f, new Vector2(1f, 0f));

            // Body (questions; passage column added for reading parts)
            _bodyArea = new GameObject("Body", typeof(RectTransform)).GetComponent<RectTransform>();
            _bodyArea.SetParent(_root, false);
            _bodyArea.anchorMin = Vector2.zero; _bodyArea.anchorMax = Vector2.one;
            _bodyArea.offsetMin = new Vector2(0f, 92f); _bodyArea.offsetMax = new Vector2(0f, -144f);
            _passageScroll = MakeScroll(_bodyArea, "PassageScroll", out _passageContent);
            _questionScroll = MakeScroll(_bodyArea, "QuestionScroll", out _questionContent);

            // Footer
            _footer = Strip(_root, "Footer", 0f, 92f, top: false, new Color(0.08f, 0.1f, 0.13f, 1f));
            _navigator = new GameObject("Navigator", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            _navigator.SetParent(_footer, false);
            Place(_navigator, 28f, 14f, 1220f, 64f, Vector2.zero);
            var grid = _navigator.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(27f, 28f); grid.spacing = new Vector2(3.5f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount; grid.constraintCount = 2;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _prevButton = NLUi.Button(_footer, "Prev", "‹  Phần trước", _font, () => ShowPart(_viewPart - 1), new Color(1f, 1f, 1f, 0.08f), 16f, Color.white, 50f);
            Place((RectTransform)_prevButton.transform, -470f, 21f, 150f, 50f, new Vector2(1f, 0f));
            _nextButton = NLUi.Button(_footer, "Next", "Phần sau  ›", _font, () => ShowPart(_viewPart + 1), new Color(1f, 1f, 1f, 0.08f), 16f, Color.white, 50f);
            Place((RectTransform)_nextButton.transform, -310f, 21f, 150f, 50f, new Vector2(1f, 0f));
            _submitButton = NLUi.Button(_footer, "Submit", "Nộp bài", _font, RequestSubmit, NLUi.Gold, 18f, new Color(0.1f, 0.08f, 0.04f), 50f);
            Place((RectTransform)_submitButton.transform, -28f, 21f, 260f, 50f, new Vector2(1f, 0f));

            BuildResults();
            BuildConfirm();
            ApplyResponsiveLayout(force: true);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Keeps the header, audio controls and footer readable on 4:3 and narrow game views.</summary>
        private void ApplyResponsiveLayout(bool force = false)
        {
            if (_root == null || _header == null) return;
            float width = _root.rect.width;
            if (width <= 0f || (!force && Mathf.Abs(width - _layoutWidth) < 0.5f)) return;
            _layoutWidth = width;

            bool compact = width < 1540f;
            float closeReserve = 74f;
            float timerWidth = compact ? 330f : 410f;
            float modeWidth = compact ? 116f : 140f;
            Place(_timer.rectTransform, -closeReserve, 0f, timerWidth, 70f, new Vector2(1f, 0f));
            Place(_modeChip.rectTransform, -(closeReserve + timerWidth + 12f), 0f, modeWidth, 70f, new Vector2(1f, 0f));
            float titleRight = closeReserve + timerWidth + modeWidth + 40f;
            Place(_title.rectTransform, 28f, 0f, Mathf.Max(240f, width - titleRight - 28f), 70f, Vector2.zero);

            float tabsWidth = Mathf.Clamp(width * (compact ? 0.34f : 0.28f), 360f, 520f);
            Place(_tabs, 28f, 14f, tabsWidth, 46f, Vector2.zero);
            float playX = 28f + tabsWidth + 16f;
            float noteWidth = compact ? 300f : 410f;
            float playWidth = compact ? 138f : 170f;
            Place((RectTransform)_playButton.transform, playX, 14f, playWidth, 46f, Vector2.zero);
            float progressX = playX + playWidth + 18f;
            float audioTimeWidth = compact ? 126f : 150f;
            float noteLeft = width - noteWidth - 28f;
            float progressWidth = Mathf.Max(150f, noteLeft - audioTimeWidth - 24f - progressX);
            Place(_progressTrack, progressX, 31f, progressWidth, 12f, Vector2.zero);
            Place(_audioStatus.rectTransform, progressX, 44f, progressWidth, 26f, Vector2.zero);
            Place(_audioTime.rectTransform, progressX + progressWidth + 12f, 14f, audioTimeWidth, 46f, Vector2.zero);
            Place(_note.rectTransform, -28f, 6f, noteWidth, 62f, new Vector2(1f, 0f));
            _audioStatus.gameObject.SetActive(_test != null && IsListening && !compact);

            float submitWidth = compact ? 190f : 230f;
            float navWidth = Mathf.Max(300f, width - (compact ? 560f : 620f));
            Place(_navigator, 28f, 14f, navWidth, 64f, Vector2.zero);
            Place((RectTransform)_submitButton.transform, -28f, 21f, submitWidth, 50f, new Vector2(1f, 0f));
            Place((RectTransform)_nextButton.transform, -(40f + submitWidth), 21f, 140f, 50f, new Vector2(1f, 0f));
            Place((RectTransform)_prevButton.transform, -(192f + submitWidth), 21f, 140f, 50f, new Vector2(1f, 0f));
        }

        private void BuildTabs()
        {
            var tabs = _root.Find("AudioBar/Tabs");
            foreach (Transform child in tabs) Destroy(child.gameObject);
            _partTabs.Clear();
            for (int i = 0; i < _test.parts.Length; i++)
            {
                int index = i;
                var part = _test.parts[i];
                var groups = part.groups ?? Array.Empty<IeltsGroup>();
                string range = groups.Length > 0 ? $"<size=75%>  {groups.Min(g => g.from)}–{groups.Max(g => g.to)}</size>" : "";
                var button = NLUi.Button(tabs, "Part" + (i + 1), $"Phần {part.number}{range}", _font, () => ShowPart(index), new Color(1f, 1f, 1f, 0.08f), 16f, Color.white, 46f);
                NLUi.Size(button, 122f, 46f);
                _partTabs.Add(button);
            }
        }

        private void BuildNavigator()
        {
            foreach (Transform child in _navigator) Destroy(child.gameObject);
            _navButtons.Clear();
            var numbers = _test.parts.SelectMany(p => p.groups ?? Array.Empty<IeltsGroup>())
                .Where(g => g.type != "writing_task" && g.type != "speaking_part")
                .SelectMany(g => Enumerable.Range(g.from, g.to - g.from + 1)).Distinct().OrderBy(n => n).ToList();
            foreach (int n in numbers)
            {
                int number = n;
                var cell = new GameObject("Nav" + n, typeof(RectTransform), typeof(Image), typeof(Button));
                cell.transform.SetParent(_navigator, false);
                var image = cell.GetComponent<Image>();
                image.color = GetAnswer(n).Length > 0 ? AnsweredColor : new Color(1f, 1f, 1f, 0.08f);
                cell.GetComponent<Button>().onClick.AddListener(() => GoToQuestion(number));
                var label = Text((RectTransform)cell.transform, "N", n.ToString(), 12.5f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
                NLUi.Stretch(label.rectTransform);
                label.raycastTarget = false;
                _navButtons[n] = image;
            }
        }

        // ─────────── Rendering a part ───────────

        private void RenderPart(IeltsPart part)
        {
            foreach (Transform child in _questionContent) Destroy(child.gameObject);
            foreach (Transform child in _passageContent) Destroy(child.gameObject);
            _anchors.Clear();
            _refreshers.Clear();

            bool reading = !string.IsNullOrWhiteSpace(part.passage);
            _passageScroll.gameObject.SetActive(reading);
            var passageRect = (RectTransform)_passageScroll.transform;
            var questionRect = (RectTransform)_questionScroll.transform;
            if (reading)
            {
                passageRect.anchorMin = new Vector2(0f, 0f); passageRect.anchorMax = new Vector2(0.5f, 1f);
                questionRect.anchorMin = new Vector2(0.5f, 0f); questionRect.anchorMax = new Vector2(1f, 1f);
                passageRect.offsetMin = new Vector2(24f, 12f); passageRect.offsetMax = new Vector2(-8f, -12f);
                questionRect.offsetMin = new Vector2(8f, 12f); questionRect.offsetMax = new Vector2(-24f, -12f);
                if (!string.IsNullOrWhiteSpace(part.passageTitle)) PaperText(_passageContent, part.passageTitle, 24f, FontStyles.Bold);
                foreach (string paragraph in part.passage.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
                    PaperText(_passageContent, paragraph.Trim(), 18f, FontStyles.Normal).lineSpacing = 6f;
            }
            else
            {
                questionRect.anchorMin = Vector2.zero; questionRect.anchorMax = Vector2.one;
                questionRect.offsetMin = new Vector2(0f, 12f); questionRect.offsetMax = new Vector2(0f, -12f);
            }

            var sheet = _questionContent;
            var title = PaperText(sheet, $"{part.title.ToUpperInvariant()}", 20f, FontStyles.Bold);
            title.color = Accent;
            foreach (var group in part.groups ?? Array.Empty<IeltsGroup>())
            {
                switch (group.type)
                {
                    case "completion": RenderCompletion(sheet, group); break;
                    case "mcq_single": RenderSingleChoice(sheet, group); break;
                    case "mcq_multi": RenderMultiChoice(sheet, group); break;
                    case "matching": RenderMatching(sheet, group); break;
                    case "tfng": RenderFixedChoice(sheet, group, "TRUE", "FALSE", "NOT GIVEN"); break;
                    case "ynng": RenderFixedChoice(sheet, group, "YES", "NO", "NOT GIVEN"); break;
                    case "writing_task": RenderWriting(sheet, group); break;
                    case "speaking_part": RenderSpeaking(sheet, group); break;
                    default: PaperText(sheet, $"(Dạng câu hỏi chưa hỗ trợ: {group.type})", 16f, FontStyles.Italic).color = Bad; break;
                }
            }
            _questionScroll.verticalNormalizedPosition = 1f;
            if (_passageScroll.gameObject.activeSelf) _passageScroll.verticalNormalizedPosition = 1f;
        }

        private void GroupHeader(RectTransform parent, IeltsGroup group)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = 6f;
            string range = group.from == group.to ? $"Question {group.from}" : $"Questions {group.from}–{group.to}";
            PaperText(parent, range, 18f, FontStyles.Bold | FontStyles.Italic);
            if (!string.IsNullOrWhiteSpace(group.instruction)) PaperText(parent, Emphasise(group.instruction), 17f, FontStyles.Italic);
            string limit = LimitText(group.limit);
            if (limit != null) PaperText(parent, limit, 17f, FontStyles.Italic);
            if (!string.IsNullOrWhiteSpace(group.image)) GroupImage(parent, group.image);
        }

        private readonly Dictionary<string, Texture2D> _images = new();

        /// <summary>A plan / map / diagram that belongs to the questions (Listening "label the plan", Reading diagrams).
        /// Read from the package folder at runtime — never imported into the project.</summary>
        private void GroupImage(RectTransform parent, string relative)
        {
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(_package.Folder, relative));
            if (!_images.TryGetValue(path, out var texture))
            {
                if (!System.IO.File.Exists(path)) { PaperText(parent, $"(Thiếu hình: {relative})", 15f, FontStyles.Italic); return; }
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(System.IO.File.ReadAllBytes(path));
                _images[path] = texture;
            }
            const float width = 600f;
            float height = width * texture.height / Mathf.Max(1f, texture.width);
            var holder = new GameObject("GroupImage", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            holder.SetParent(parent, false);
            holder.GetComponent<LayoutElement>().preferredHeight = height + 8f;
            var image = new GameObject("Picture", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(holder, false);
            image.texture = texture;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static string LimitText(string limit) => limit switch
        {
            "ONE_WORD" => "Write <b>ONE WORD ONLY</b> for each answer.",
            "ONE_WORD_AND_OR_NUMBER" => "Write <b>ONE WORD AND/OR A NUMBER</b> for each answer.",
            "TWO_WORDS" => "Write <b>NO MORE THAN TWO WORDS</b> for each answer.",
            "NO_MORE_THAN_TWO_WORDS_AND_OR_A_NUMBER" => "Write <b>NO MORE THAN TWO WORDS AND/OR A NUMBER</b> for each answer.",
            "NO_MORE_THAN_THREE_WORDS" => "Write <b>NO MORE THAN THREE WORDS</b> for each answer.",
            "NO_MORE_THAN_THREE_WORDS_AND_OR_A_NUMBER" => "Write <b>NO MORE THAN THREE WORDS AND/OR A NUMBER</b> for each answer.",
            _ => null,
        };

        private static string Emphasise(string text) =>
            Regex.Replace(text, @"\b(TWO|THREE|FIVE|ONE|NOT GIVEN|TRUE|FALSE|[A-G](?=[,–\-]| or))\b", "<b>$1</b>");

        private void RenderCompletion(RectTransform sheet, IeltsGroup group)
        {
            GroupHeader(sheet, group);
            var box = Box(sheet, "Form");
            if (!string.IsNullOrWhiteSpace(group.heading))
            {
                var heading = PaperText(box, group.heading, 21f, FontStyles.Bold);
                heading.alignment = TextAlignmentOptions.Center;
            }
            foreach (var line in group.lines ?? Array.Empty<IeltsLine>())
            {
                if (line.style == "subheading")
                {
                    PaperText(box, line.label, 17.5f, FontStyles.Bold).margin = new Vector4(0f, 8f, 0f, 0f);
                    continue;
                }
                var row = Row(box, "Line", 10f);
                if (!string.IsNullOrEmpty(line.label) || HasLabels(group))
                {
                    var label = PaperText(row, line.label ?? "", 17f, line.style == "example" ? FontStyles.Italic : FontStyles.Normal);
                    NLUi.Size(label, 290f);
                }
                foreach (string segment in Regex.Split(line.text ?? "", @"(\{\d+\})"))
                {
                    if (segment.Length == 0) continue;
                    var m = Regex.Match(segment, @"^\{(\d+)\}$");
                    if (m.Success) GapInput(row, int.Parse(m.Groups[1].Value), group.limit);
                    else
                    {
                        var t = PaperText(row, segment, 17f, line.style == "example" ? FontStyles.Italic : FontStyles.Normal);
                        t.textWrappingMode = TextWrappingModes.Normal;
                        if (line.style == "example") t.color = PaperMuted;
                        var size = NLUi.Size(t, preferredWidth: Mathf.Min(480f, t.GetPreferredValues(segment, 480f, 0f).x + 4f), flexibleWidth: 1f);
                        size.minWidth = 32f;
                    }
                }
                var filler = new GameObject("Fill", typeof(RectTransform), typeof(LayoutElement));
                filler.transform.SetParent(row, false);
                filler.GetComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }

        private static bool HasLabels(IeltsGroup group) => group.lines != null && group.lines.Any(l => !string.IsNullOrEmpty(l.label) && l.style != "subheading");

        private void GapInput(RectTransform row, int number, string limit)
        {
            var holder = Row(row, "Gap" + number, 6f);
            NLUi.Size(holder, 244f);
            _anchors[number] = holder;
            var chip = NLUi.Pill(holder, "No", number.ToString(), _font, Accent, Color.white, 14f);
            NLUi.Size(chip, 34f, 30f);
            var input = MakeInput(holder, 200f, 34f);
            input.text = GetAnswer(number);
            input.onValueChanged.AddListener(v => SetAnswer(number, v.Trim()));
            input.characterLimit = 40;
        }

        private void RenderSingleChoice(RectTransform sheet, IeltsGroup group)
        {
            GroupHeader(sheet, group);
            if (!string.IsNullOrWhiteSpace(group.heading)) PaperText(sheet, group.heading, 21f, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
            foreach (var item in group.items ?? Array.Empty<IeltsItem>())
            {
                var block = Column(sheet, "Q" + item.number, 6f);
                _anchors[item.number] = block;
                PaperText(block, $"<b>{item.number}</b>   {item.text}", 17.5f, FontStyles.Normal);
                ChoiceList(block, item.number, item.options, multi: false, null);
            }
        }

        private void RenderFixedChoice(RectTransform sheet, IeltsGroup group, params string[] labels)
        {
            GroupHeader(sheet, group);
            var options = labels.Select(l => new IeltsOption { letter = l, text = "" }).ToArray();
            foreach (var item in group.items ?? Array.Empty<IeltsItem>())
            {
                var block = Column(sheet, "Q" + item.number, 6f);
                _anchors[item.number] = block;
                PaperText(block, $"<b>{item.number}</b>   {item.text}", 17.5f, FontStyles.Normal);
                LetterRow(block, item.number, options);
            }
        }

        private void RenderMultiChoice(RectTransform sheet, IeltsGroup group)
        {
            GroupHeader(sheet, group);
            if (!string.IsNullOrWhiteSpace(group.heading)) PaperText(sheet, group.heading, 21f, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
            var block = Column(sheet, $"Q{group.from}-{group.to}", 6f);
            for (int n = group.from; n <= group.to; n++) _anchors[n] = block;
            PaperText(block, group.stem, 17.5f, FontStyles.Normal);
            ChoiceList(block, group.from, group.options, multi: true, group);
        }

        /// <summary>Option rows; for "choose N" the chosen letters are stored in order across from..to.</summary>
        private void ChoiceList(RectTransform parent, int number, IeltsOption[] options, bool multi, IeltsGroup group)
        {
            var rows = new List<(Image bg, string letter)>();
            foreach (var option in options ?? Array.Empty<IeltsOption>())
            {
                string letter = option.letter;
                var button = NLUi.Button(parent, "Opt" + letter, $"<b>{letter}</b>     {option.text}", _font, null, Color.white, 17f, PaperInk, 40f);
                button.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                var bg = button.GetComponent<Image>();
                rows.Add((bg, letter));
                button.onClick.AddListener(() =>
                {
                    if (IsSubmitted) return;
                    if (!multi) { SetAnswer(number, GetAnswer(number) == letter ? "" : letter); return; }
                    var chosen = Enumerable.Range(group.from, group.to - group.from + 1).Select(GetAnswer).Where(v => v.Length > 0).ToList();
                    if (chosen.Contains(letter)) chosen.Remove(letter);
                    else if (chosen.Count < Mathf.Max(1, group.choose)) chosen.Add(letter);
                    chosen.Sort(string.CompareOrdinal);
                    for (int n = group.from; n <= group.to; n++) SetAnswer(n, n - group.from < chosen.Count ? chosen[n - group.from] : "");
                });
            }
            void Refresh()
            {
                var selected = multi
                    ? new HashSet<string>(Enumerable.Range(group.from, group.to - group.from + 1).Select(GetAnswer))
                    : new HashSet<string> { GetAnswer(number) };
                foreach (var (bg, letter) in rows)
                    bg.color = selected.Contains(letter) ? new Color(0.78f, 0.88f, 0.98f, 1f) : new Color(1f, 1f, 1f, 0.95f);
            }
            if (multi) for (int n = group.from; n <= group.to; n++) _refreshers[n] = Refresh;
            else _refreshers[number] = Refresh;
            Refresh();
        }

        private void RenderMatching(RectTransform sheet, IeltsGroup group)
        {
            GroupHeader(sheet, group);
            var box = Box(sheet, "Options");
            if (!string.IsNullOrWhiteSpace(group.optionsTitle)) PaperText(box, group.optionsTitle, 18f, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
            foreach (var option in group.options ?? Array.Empty<IeltsOption>())
                PaperText(box, $"<b>{option.letter}</b>     {option.text}", 17f, FontStyles.Normal);
            if (!string.IsNullOrWhiteSpace(group.heading)) PaperText(sheet, group.heading, 18f, FontStyles.Bold);
            // A summary completed from the box: show its text with numbered gaps; the letters are picked per gap below.
            foreach (var line in group.lines ?? Array.Empty<IeltsLine>())
                PaperText(sheet, Regex.Replace(line.text ?? "", @"\{(\d+)\}", "<b>$1</b> ________"), 17f, FontStyles.Normal);
            foreach (var item in group.items ?? Array.Empty<IeltsItem>())
            {
                var row = Row(sheet, "Q" + item.number, 10f);
                _anchors[item.number] = row;
                var label = PaperText(row, $"<b>{item.number}</b>   {item.text}", 17f, FontStyles.Normal);
                NLUi.Size(label, 360f);
                LetterRow(row, item.number, group.options);
                var filler = new GameObject("Fill", typeof(RectTransform), typeof(LayoutElement));
                filler.transform.SetParent(row, false);
                filler.GetComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }

        private void LetterRow(RectTransform parent, int number, IeltsOption[] options)
        {
            var row = Row(parent, "Letters", 6f);
            var cells = new List<(Image bg, TextMeshProUGUI label, string letter)>();
            foreach (var option in options ?? Array.Empty<IeltsOption>())
            {
                string letter = option.letter;
                var button = NLUi.Button(row, "L" + letter, letter, _font, () => { if (!IsSubmitted) SetAnswer(number, GetAnswer(number) == letter ? "" : letter); },
                    Color.white, 15f, PaperInk, 36f);
                NLUi.Size(button, letter.Length > 1 ? 118f : 42f, 36f);
                cells.Add((button.GetComponent<Image>(), button.GetComponentInChildren<TextMeshProUGUI>(), letter));
            }
            void Refresh()
            {
                string chosen = GetAnswer(number);
                foreach (var (bg, label, letter) in cells)
                {
                    bool on = chosen == letter;
                    bg.color = on ? Accent : new Color(0.93f, 0.93f, 0.91f, 1f);
                    label.color = on ? Color.white : PaperInk;
                }
            }
            _refreshers[number] = Refresh;
            Refresh();
        }

        private void RenderWriting(RectTransform sheet, IeltsGroup group)
        {
            PaperText(sheet, group.heading ?? "Writing task", 20f, FontStyles.Bold);
            if (group.minutes > 0) PaperText(sheet, $"You should spend about {group.minutes} minutes on this task.", 16.5f, FontStyles.Italic);
            var prompt = Box(sheet, "Prompt");
            PaperText(prompt, group.stem ?? "", 17.5f, FontStyles.Normal);
            if (group.minWords > 0) PaperText(sheet, $"Write at least {group.minWords} words.", 16.5f, FontStyles.Italic);
            int number = group.from;
            _anchors[number] = sheet;
            var input = MakeInput(sheet, 1060f, 420f, multiline: true);
            input.text = GetAnswer(number);
            var count = PaperText(sheet, "", 15f, FontStyles.Normal);
            void Count(string v)
            {
                int words = Regex.Matches(v ?? "", @"[\p{L}\p{N}'’-]+").Count;
                count.text = $"Số từ: <b>{words}</b>" + (group.minWords > 0 ? $"  /  tối thiểu {group.minWords}" : "");
                count.color = group.minWords > 0 && words < group.minWords ? Bad : Good;
            }
            input.onValueChanged.AddListener(v => { SetAnswer(number, v); Count(v); });
            Count(input.text);
        }

        private void RenderSpeaking(RectTransform sheet, IeltsGroup group)
        {
            PaperText(sheet, group.heading ?? "Speaking", 20f, FontStyles.Bold);
            var prompt = Box(sheet, "Prompt");
            PaperText(prompt, group.stem ?? "", 17.5f, FontStyles.Normal);
#if UNITY_WEBGL && !UNITY_EDITOR
            bool hasMic = false;
#else
            bool hasMic = Microphone.devices.Length > 0;
#endif
            PaperText(sheet, hasMic
                ? "Ghi âm câu trả lời bằng nút Luyện phát âm (V). Phần chấm Speaking tự động chưa có — chỉ dùng để luyện."
                : "Không tìm thấy micro: phần Speaking chỉ hiển thị đề để luyện nói.", 15f, FontStyles.Italic).color = PaperMuted;
        }

        // ─────────── Results ───────────

        private void BuildResults()
        {
            _results = new GameObject("Results", typeof(RectTransform)).GetComponent<RectTransform>();
            _results.SetParent(_root, false);
            _results.anchorMin = Vector2.zero; _results.anchorMax = Vector2.one;
            _results.offsetMin = new Vector2(0f, 92f); _results.offsetMax = new Vector2(0f, -144f);
            _results.gameObject.SetActive(false);
        }

        private void ShowResults()
        {
            _bodyArea.gameObject.SetActive(false);
            foreach (Transform child in _results) Destroy(child.gameObject);
            _results.gameObject.SetActive(true);
            var scroll = MakeScroll(_results, "ResultScroll", out var content);
            var rect = (RectTransform)scroll.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(0f, 12f); rect.offsetMax = new Vector2(0f, -12f);

            var r = LastResult;
            var summary = Box(content, "Summary");
            PaperText(summary, "KẾT QUẢ · RESULT", 15f, FontStyles.Bold).color = Accent;
            PaperText(summary, $"<size=170%><b>{r.Raw}</b></size> / {r.Max}      Band ước tính <size=150%><b>{r.Band:0.0}</b></size>", 22f, FontStyles.Normal);
            PaperText(summary, string.Join("      ", r.RawByPart.Select(kv => $"Phần {kv.Key}: <b>{kv.Value}/{r.MaxByPart[kv.Key]}</b>")), 17f, FontStyles.Normal);
            PaperText(summary, "Band là ước tính theo bảng quy đổi điểm thô công khai, không phải kết quả IELTS chính thức.", 14.5f, FontStyles.Italic).color = PaperMuted;

            PaperText(content, "Xem lại từng câu", 19f, FontStyles.Bold);
            var header = Row(content, "Head", 10f);
            Cell(header, "Câu", 60f, FontStyles.Bold); Cell(header, "Bạn trả lời", 360f, FontStyles.Bold); Cell(header, "Đáp án", 420f, FontStyles.Bold); Cell(header, "", 120f, FontStyles.Bold);
            foreach (var item in r.Items)
            {
                var row = Row(content, "R" + item.Number, 10f);
                row.gameObject.AddComponent<Image>().color = item.IsCorrect ? new Color(0.88f, 0.96f, 0.9f, 1f) : new Color(0.99f, 0.91f, 0.9f, 1f);
                Cell(row, item.Number.ToString(), 60f, FontStyles.Bold);
                Cell(row, string.IsNullOrEmpty(item.Given) ? "<i>(bỏ trống)</i>" : item.Given, 360f, FontStyles.Normal);
                Cell(row, IeltsGrader.KeyDisplay(item.Correct) + (item.Correct != null && item.Correct.Length > 1 && item.Correct.All(c => c.Length == 1) ? "  <i>(bất kỳ thứ tự)</i>" : ""), 420f, FontStyles.Normal);
                var mark = Cell(row, item.IsCorrect ? "✓ Đúng" : item.OverLimit ? "× Quá số từ" : "× Sai", 120f, FontStyles.Bold);
                mark.color = item.IsCorrect ? Good : Bad;
            }
            var buttons = Row(content, "Buttons", 12f);
            NLUi.Size(NLUi.Button(buttons, "Again", "Làm lại (luyện tập)", _font, () => Begin(_package, PracticeMode, false), Accent, 17f, Color.white, 50f), 240f, 50f);
            NLUi.Size(NLUi.Button(buttons, "Exam", "Thi thử lại", _font, () => Begin(_package, ExamMode, false), NLUi.Gold, 17f, new Color(0.1f, 0.08f, 0.04f), 50f), 200f, 50f);
            NLUi.Size(NLUi.Button(buttons, "Close", "Đóng", _font, Close, new Color(0.3f, 0.34f, 0.4f), 17f, Color.white, 50f), 140f, 50f);
            _submitButton.interactable = false;
            RefreshAudioBar();
        }

        private TextMeshProUGUI Cell(RectTransform row, string text, float width, FontStyles style)
        {
            var t = PaperText(row, text, 16.5f, style);
            NLUi.Size(t, width);
            return t;
        }

        // ─────────── Confirm dialog ───────────

        private void BuildConfirm()
        {
            _confirm = new GameObject("Confirm", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _confirm.SetParent(_root, false);
            NLUi.Stretch(_confirm);
            _confirm.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var box = NLUi.Panel(_confirm, "Box", new Color(0.09f, 0.12f, 0.16f, 1f), new RectOffset(34, 34, 28, 28), 14f);
            NLUi.Anchor(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 0f));
            NLUi.FitContent(box);
            _confirmTitle = NLUi.Label(box, "Title", "", 26f, NLUi.Gold, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            _confirmBody = NLUi.Label(box, "Body", "", 17f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            var row = NLUi.Group(box, "Buttons", false, 14f, TextAnchor.MiddleCenter, false);
            var stay = NLUi.Button(row, "Stay", "Tiếp tục làm bài", _font, () => _confirm.gameObject.SetActive(false), Accent, 17f, Color.white, 50f);
            NLUi.Size(stay, 240f, 50f);
            var yes = NLUi.Button(row, "Yes", "", _font, () => _confirmYes?.Invoke(), new Color(0.32f, 0.36f, 0.42f), 17f, Color.white, 50f);
            NLUi.Size(yes, 200f, 50f);
            _confirmYesLabel = yes.GetComponentInChildren<TextMeshProUGUI>();
            _confirm.gameObject.SetActive(false);
        }

        // ─────────── Small builders ───────────

        private RectTransform Strip(RectTransform parent, string name, float offset, float height, bool top, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchoredPosition = new Vector2(0f, top ? -offset : offset);
            rect.sizeDelta = new Vector2(0f, height);
            rect.GetComponent<Image>().color = color;
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private TextMeshProUGUI Text(RectTransform parent, string name, string value, float size, Color color, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            t.transform.SetParent(parent, false);
            t.font = _font; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.text = value; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        private TextMeshProUGUI PaperText(RectTransform parent, string value, float size, FontStyles style)
        {
            var t = NLUi.Label(parent, "T", value, size, PaperInk, _font, style);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            return t;
        }

        private RectTransform Row(RectTransform parent, string name, float spacing)
        {
            var row = NLUi.Group(parent, name, false, spacing, TextAnchor.MiddleLeft, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = false;
            layout.childControlWidth = true;
            NLUi.FitContent(row, width: false, height: true);
            return row;
        }

        private RectTransform Column(RectTransform parent, string name, float spacing) => NLUi.Group(parent, name, true, spacing);

        private RectTransform Box(RectTransform parent, string name)
        {
            var box = NLUi.Panel(parent, name, new Color(1f, 1f, 1f, 1f), new RectOffset(26, 26, 18, 20), 8f);
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = PaperLine;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return box;
        }

        private ScrollRect MakeScroll(RectTransform parent, string name, out RectTransform content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            viewport.SetParent(rect, false);
            NLUi.Stretch(viewport);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var paper = new GameObject("Paper", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            paper.SetParent(viewport, false);
            paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 1f);
            paper.pivot = new Vector2(0.5f, 1f);
            paper.sizeDelta = new Vector2(1180f, 0f);
            paper.GetComponent<Image>().color = Paper;
            var layout = paper.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 34, 44);
            layout.spacing = 10f;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            paper.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = paper;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38f;
            content = paper;
            // Narrow the paper to its column when the passage is shown beside it.
            go.AddComponent<IeltsPaperWidth>().Configure(paper, rect);
            return scroll;
        }

        private TMP_InputField MakeInput(RectTransform parent, float width, float height, bool multiline = false)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = width; element.minWidth = width; element.preferredHeight = height; element.minHeight = height;
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 0.97f, 1f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.6f, 0.68f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            var area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            area.SetParent(go.transform, false);
            NLUi.Stretch(area);
            area.offsetMin = new Vector2(10f, 4f); area.offsetMax = new Vector2(-10f, -4f);
            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(area, false);
            NLUi.Stretch(text.rectTransform);
            text.font = _font; text.fontSize = 17f; text.color = PaperInk;
            text.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.targetGraphic = image;
            input.fontAsset = _font;
            input.pointSize = 17f;
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            input.richText = false;
            return input;
        }
    }

    /// <summary>Keeps the question / passage paper at a comfortable reading width inside its scroll column.</summary>
    internal sealed class IeltsPaperWidth : MonoBehaviour
    {
        private RectTransform _paper, _column;
        public void Configure(RectTransform paper, RectTransform column) { _paper = paper; _column = column; }

        private void LateUpdate()
        {
            if (_paper == null || _column == null) return;
            float width = Mathf.Min(1180f, _column.rect.width - 32f);
            if (Mathf.Abs(_paper.sizeDelta.x - width) > 0.5f) _paper.sizeDelta = new Vector2(width, _paper.sizeDelta.y);
        }
    }
}
