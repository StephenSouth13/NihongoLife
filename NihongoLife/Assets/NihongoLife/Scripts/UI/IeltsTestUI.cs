using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Core;
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
    public sealed partial class IeltsTestUI : MonoBehaviour
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
        private Image _modeChipBg, _timerBg;
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
        private readonly HighlightBook _highlights = new();
        private RectTransform _toolbar;
        private bool _highlightPaper;

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
            _highlights.Clear();
            foreach (var group in (_attempt.highlights ?? new List<IeltsHighlight>()).GroupBy(h => h.key))
                _highlights.Set(group.Key, group.Select(h => new HighlightSpan(h.start, h.end, h.color)).ToList(), notify: false);
            HighlightTool.SetMode(HighlightTool.ToolMode.Off);
            LastResult = null;
            ResetProductive();

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

            RefreshChrome();
            ApplyResponsiveLayout(force: true);
            // The audio controls belong to Listening only; Reading/Writing keep just the part tabs.
            foreach (var audioOnly in new Component[] { _playButton, _progressTrack, _audioTime, _audioStatus })
                if (audioOnly != null) audioOnly.gameObject.SetActive(IsListening);
            RefreshChrome();
            BuildTabs();
            BuildNavigator();
            ShowPart(Mathf.Clamp(_attempt.partIndex, 0, parts - 1));
            // Practice: ShowPart loads the viewed part's audio. Exam: the candidate presses "Bắt đầu nghe".
            if (IsListening && IsExam) StartCoroutine(PrepareAudio(FirstUnfinishedPart(), false));
        }

        private bool IsListening => _test.parts.Any(p => !string.IsNullOrEmpty(p.audio));

        /// <summary>Title, mode chip and the rules note, in the interface language (also after a language switch).</summary>
        private void RefreshChrome()
        {
            if (_test == null) return;
            string local = _test.localOnly ? Loc.T("bản local · không phát hành", "local copy · not published", "ローカル版 · 非公開") : "";
            _title.text = $"<b>{_test.DisplayTitle}</b>  <size=70%><color=#9FB0C4>{local}</color></size>";
            _modeChip.text = IsExam ? Loc.T("CHẾ ĐỘ THI", "EXAM MODE", "本番モード") : Loc.T("LUYỆN TẬP", "PRACTICE", "練習モード");
            if (_modeChipBg != null) _modeChipBg.color = IsExam ? UiKit.Alpha(UiTheme.Bad, 0.2f) : UiKit.Alpha(UiTheme.Teal, 0.2f);
            _modeChip.color = IsExam ? UiTheme.Bad : UiTheme.Teal;
            int review = Mathf.RoundToInt(_test.reviewSeconds / 60f);
            if (IsListening)
                _note.text = IsExam
                    ? Loc.F("Chế độ thi: mỗi phần nghe phát <b>một lần</b>, không dừng, không tua. Sau phần cuối có {0} phút kiểm tra, rồi tự nộp.",
                            "Exam mode: each part plays <b>once</b>, no pausing or seeking. After the last part you have {0} minutes to check, then it submits.",
                            "本番モード：各パートの音声は<b>1回だけ</b>、停止・早送り不可。最後のパートの後に{0}分の確認時間があり、自動提出されます。", review)
                    : Loc.T("Luyện tập: nghe lại, tạm dừng và tua tự do.", "Practice: replay, pause and seek freely.", "練習：自由に再生・停止・移動できます。");
            else
                _note.text = IsExam
                    ? Loc.F("Chế độ thi: <b>{0} phút</b> cho cả bài, hết giờ tự nộp. Không có thời gian chép đáp án riêng.",
                            "Exam mode: <b>{0} minutes</b> for the whole paper; it submits when time is up. No separate transfer time.",
                            "本番モード：全体で<b>{0}分</b>、時間切れで自動提出。解答の転記時間はありません。", _test.timeLimitMinutes)
                    : Loc.F("Luyện tập: không giới hạn thời gian (bài thật {0} phút).", "Practice: no time limit (the real paper is {0} minutes).", "練習：時間制限なし（本番は{0}分）。", _test.timeLimitMinutes);
        }

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
        public int QuestionCount => _test.parts.SelectMany(p => p.groups ?? Array.Empty<IeltsGroup>()).Where(g => IsProductive || g.type != "writing_task" && g.type != "speaking_part").Sum(g => g.to - g.from + 1);

        private void Save()
        {
            if (_attempt == null || IsSubmitted) return;
            _attempt.partIndex = _viewPart;
            if (_audioPart >= 0 && _audio.clip != null) _attempt.audioPositions[_audioPart] = _audio.time;
            _attempt.responses = _answers.Select(kv => new IeltsResponse { number = kv.Key, value = kv.Value }).OrderBy(r => r.number).ToList();
            _attempt.highlights = _highlights.All.SelectMany(kv => kv.Value.Select(h => new IeltsHighlight { key = kv.Key, start = h.start, end = h.end, color = h.color })).ToList();
            IeltsAttemptStore.SaveAttempt(_attempt);
            _saveAt = -1f;
            LastSavedAt = DateTime.Now;
            if (IsProductive && !IsSpeakingPaper) foreach (var refresh in _refreshers.Values.ToList()) refresh(); // "đã lưu lúc …" stamp
        }

        // ─────────── Navigation ───────────

        public void ShowPart(int index)
        {
            _viewPart = Mathf.Clamp(index, 0, _test.parts.Length - 1);
            for (int i = 0; i < _partTabs.Count; i++) UiKit.SetKind(_partTabs[i], i == _viewPart ? UiKit.Kind.Accent : UiKit.Kind.Ghost);
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
            _audioStatus.text = Loc.F("Đang tải audio {0}…", "Loading audio for {0}…", "{0}の音声を読み込み中…", part.title);
            AudioClip clip = null;
            yield return IeltsLibrary.LoadAudio(path, c => clip = c);
            if (_loadingPart != partIndex) yield break; // a newer request replaced this one
            _loadingPart = -1;
            if (clip == null) { _audioStatus.text = Loc.T("Không tải được audio của phần này.", "Could not load this part's audio.", "このパートの音声を読み込めません。"); yield break; }
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
                _playLabel.text = _audio.isPlaying ? Loc.T("Đang phát", "Playing", "再生中") : Loc.T("Bắt đầu nghe", "Start listening", "聴き始める");
                _playButton.interactable = !_audio.isPlaying && !IsSubmitted && _attempt.reviewRemaining < 0f;
                _audioStatus.text = _attempt.reviewRemaining >= 0f ? Loc.T("Đã nghe xong các phần", "All parts heard", "全パートを聴き終えました") : Loc.F("Đang nghe: {0}", "Now playing: {0}", "再生中：{0}", partName);
            }
            else
            {
                _playLabel.text = _audio.isPlaying ? Loc.T("Tạm dừng", "Pause", "一時停止") : Loc.T("Phát", "Play", "再生");
                _playButton.interactable = clip != null;
                _audioStatus.text = Loc.F("Audio: {0}", "Audio: {0}", "音声：{0}", partName);
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
                if (left <= 600f && left + Time.unscaledDeltaTime > 600f) NihongoLife.UI.HudFeed.Post(Loc.T("Còn 10 phút.", "10 minutes left.", "残り10分です。"), NihongoLife.UI.HudFeed.Kind.Warning, 4f);
                if (left <= 60f && left + Time.unscaledDeltaTime > 60f) NihongoLife.UI.HudFeed.Post(Loc.T("Còn 1 phút — bài sẽ tự nộp khi hết giờ.", "1 minute left — the paper submits when time is up.", "残り1分 — 時間切れで自動提出されます。"), NihongoLife.UI.HudFeed.Kind.Warning, 4f);
                _timer.text = $"{Loc.T("Còn lại", "Time left", "残り時間")}: <b>{(left <= 300f ? "<color=#E0644E>" : "")}{Clock(left)}{(left <= 300f ? "</color>" : "")}</b>   ·   {Loc.T("Đã làm", "Answered", "解答")} {AnsweredCount}/{QuestionCount}";
            }
            else if (limit > 0f)
                _timer.text = $"{Loc.T("Thời gian", "Time", "経過")}: <b>{Clock(_attempt.elapsedSeconds)}</b> / {_test.timeLimitMinutes} {Loc.T("phút", "min", "分")}   ·   {Loc.T("Đã làm", "Answered", "解答")} {AnsweredCount}/{QuestionCount}";
            else
                _timer.text = IsExam && _attempt.reviewRemaining >= 0f
                    ? $"{Loc.T("Kiểm tra lại", "Checking time", "見直し")}: <b>{Clock(Mathf.Max(0f, _attempt.reviewRemaining))}</b>"
                    : $"{Loc.T("Thời gian", "Time", "経過")}: <b>{Clock(_attempt.elapsedSeconds)}</b>   ·   {Loc.T("Đã làm", "Answered", "解答")} {AnsweredCount}/{QuestionCount}";

            if (IsProductive) TickProductive();
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
            ShowConfirm(Loc.T("Tạm rời bài thi?", "Leave the test for now?", "試験を中断しますか？"),
                IsExam ? Loc.T("Câu trả lời đã được lưu. Audio dừng lại và phát tiếp từ chỗ này khi bạn quay lại (trong kỳ thi thật thì không dừng được).",
                               "Your answers are saved. The audio stops and continues from here when you come back (in the real test it cannot be paused).",
                               "解答は保存されました。音声は停止し、戻るとここから再開します（本番では停止できません）。")
                       : Loc.T("Câu trả lời và vị trí audio đã được lưu. Mở lại từ Trung tâm luyện thi để làm tiếp.",
                               "Your answers and the audio position are saved. Reopen it from the Exam Center to continue.",
                               "解答と音声の位置は保存されました。試験センターから再開できます。"),
                Loc.T("Tạm rời", "Leave", "中断する"), Close);
        }

        public void Close()
        {
            StopMicIfRecording();
            Save();
            _audio.Stop();
            _audioPlaying = false;
            _confirm.gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
            HighlightTool.SetMode(HighlightTool.ToolMode.Off);
            LockGameplay(false);
        }

        public void RequestSubmit()
        {
            int missing = QuestionCount - AnsweredCount;
            ShowConfirm(Loc.T("Nộp bài?", "Submit the paper?", "提出しますか？"),
                missing > 0 ? Loc.F("Còn <b>{0}</b> câu chưa trả lời. Sau khi nộp sẽ không sửa được nữa.", "<b>{0}</b> questions are unanswered. You cannot change answers after submitting.", "未解答が<b>{0}</b>問あります。提出後は変更できません。", missing)
                            : Loc.T("Bạn đã trả lời tất cả các câu. Sau khi nộp sẽ không sửa được nữa.", "You answered every question. You cannot change answers after submitting.", "すべて解答しました。提出後は変更できません。"),
                Loc.T("Nộp bài", "Submit", "提出"), SubmitNow);
        }

        /// <summary>Grades with the key (loaded only now), records history and shows the review.</summary>
        public void SubmitNow()
        {
            if (IsSubmitted) return;
            if (IsProductive) { SubmitProductive(); return; }
            Save();
            _audio.Stop();
            _audioPlaying = false;
            var key = IeltsLibrary.LoadKey(_package);
            _confirm.gameObject.SetActive(false);
            if (key == null)
            {
                ShowConfirm(Loc.T("Thiếu đáp án", "Answer key missing", "解答キーがありません"),
                    Loc.T("Gói đề này không có key.json hợp lệ nên chưa chấm được. Bài làm vẫn được lưu.", "This package has no valid key.json, so it cannot be marked yet. Your work is saved.", "このパッケージには有効なkey.jsonがないため採点できません。解答は保存されています。"),
                    Loc.T("Đóng", "Close", "閉じる"), () => _confirm.gameObject.SetActive(false));
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

            // Header: title · mode chip · highlight tools · timer pill · close
            _header = Strip(_root, "Header", 0f, 70f, top: true, UiTheme.Ink);
            var line = UiKit.Image(_header, "Line", UiTheme.Line);
            UiKit.Strip(line.rectTransform, false, 0f, 1f);
            _title = Text(_header, "Title", "", 21f, Color.white, TextAlignmentOptions.MidlineLeft);
            Place(_title.rectTransform, 28f, 0f, 1000f, 70f, Vector2.zero);
            _title.overflowMode = TextOverflowModes.Ellipsis;
            _modeChipBg = UiKit.Image(_header, "ModeBg", UiKit.Alpha(UiTheme.Teal, 0.2f), UiKit.Rounded(16));
            _modeChip = Text((RectTransform)_modeChipBg.transform, "Mode", "", 13.5f, UiTheme.Teal, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(_modeChip.rectTransform);
            _timerBg = UiKit.Image(_header, "TimerBg", UiTheme.Sunken, UiKit.Rounded(20));
            _timer = Text((RectTransform)_timerBg.transform, "Timer", "", 16f, new Color(0.88f, 0.92f, 0.97f), TextAlignmentOptions.Center);
            UiKit.Fill(_timer.rectTransform, 14f, 0f, 14f, 0f);
            _timer.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.CloseButton(_header, RequestClose, 13f);
            _toolbar = HighlightToolbar.Create(_header, _font, ClearHighlights, CopyHighlightsToNotebook, 40f);
            _highlights.Changed += () => { if (_attempt != null && !IsSubmitted) _saveAt = Time.unscaledTime + 0.6f; };

            // Audio bar: part tabs · play · progress · time · rules note
            _audioBar = Strip(_root, "AudioBar", 70f, 74f, top: true, new Color(0.075f, 0.098f, 0.135f, 1f));
            _tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            _tabs.SetParent(_audioBar, false);
            Place(_tabs, 28f, 14f, 520f, 46f, Vector2.zero);
            var tabLayout = _tabs.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8f; tabLayout.childForceExpandWidth = false; tabLayout.childControlWidth = true; tabLayout.childControlHeight = true;
            _playButton = UiKit.Button(_audioBar, "Play", null, null, null, TogglePlay, UiKit.Kind.Accent, "play_arrow", 16f, 23);
            _playLabel = UiKit.LabelOf(_playButton);
            Place((RectTransform)_playButton.transform, 570f, 14f, 170f, 46f, Vector2.zero);
            var track = UiKit.Image(_audioBar, "Progress", new Color(1f, 1f, 1f, 0.12f), UiKit.Rounded(6), true);
            _progressTrack = track.rectTransform;
            Place(_progressTrack, 760f, 31f, 520f, 12f, Vector2.zero);
            var trigger = _progressTrack.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(OnTrackClicked);
            trigger.triggers.Add(entry);
            var fill = UiKit.Image(_progressTrack, "Fill", UiTheme.Gold, UiKit.Rounded(6));
            _progressFill = fill.rectTransform;
            _progressFill.anchorMin = Vector2.zero; _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = _progressFill.offsetMax = Vector2.zero;
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

            // Footer: question navigator · previous / next part · submit
            _footer = Strip(_root, "Footer", 0f, 92f, top: false, UiTheme.Ink);
            var topLine = UiKit.Image(_footer, "Line", UiTheme.Line);
            UiKit.Strip(topLine.rectTransform, true, 0f, 1f);
            _navigator = new GameObject("Navigator", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            _navigator.SetParent(_footer, false);
            Place(_navigator, 28f, 14f, 1220f, 64f, Vector2.zero);
            var grid = _navigator.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(28f, 28f); grid.spacing = new Vector2(4f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount; grid.constraintCount = 2;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _prevButton = UiKit.Button(_footer, "Prev", "Phần trước", "Previous part", "前のパート", () => ShowPart(_viewPart - 1), UiKit.Kind.Ghost, "chevron_left", 15f);
            Place((RectTransform)_prevButton.transform, -470f, 21f, 150f, 50f, new Vector2(1f, 0f));
            _nextButton = UiKit.Button(_footer, "Next", "Phần sau", "Next part", "次のパート", () => ShowPart(_viewPart + 1), UiKit.Kind.Secondary, "chevron_right", 15f);
            Place((RectTransform)_nextButton.transform, -310f, 21f, 150f, 50f, new Vector2(1f, 0f));
            _submitButton = UiKit.Button(_footer, "Submit", "Nộp bài", "Submit", "提出", RequestSubmit, UiKit.Kind.Primary, "task_alt", 17f);
            Place((RectTransform)_submitButton.transform, -28f, 21f, 260f, 50f, new Vector2(1f, 0f));

            BuildResults();
            BuildConfirm();
            ApplyResponsiveLayout(force: true);
            _root.gameObject.SetActive(false);
            Loc.Watch(this, () =>
            {
                if (!IsOpen || _test == null) return;
                RefreshChrome();
                RefreshAudioBar();
                if (_results.gameObject.activeSelf) { if (IsProductive) ShowProductiveResults(); else if (LastResult != null) ShowResults(); }
                else ShowPart(_viewPart);
            });
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
            Place((RectTransform)_timerBg.transform, -closeReserve, 13f, timerWidth, 44f, new Vector2(1f, 0f));
            Place((RectTransform)_modeChipBg.transform, -(closeReserve + timerWidth + 12f), 19f, modeWidth, 32f, new Vector2(1f, 0f));
            HighlightToolbar.SetCompact(_toolbar, compact);
            float toolbarWidth = LayoutUtility.GetPreferredWidth(_toolbar);
            Place(_toolbar, -(closeReserve + timerWidth + modeWidth + 24f), 15f, toolbarWidth, 40f, new Vector2(1f, 0f));
            float titleRight = closeReserve + timerWidth + modeWidth + toolbarWidth + 48f;
            Place(_title.rectTransform, 28f, 0f, Mathf.Max(200f, width - titleRight - 28f), 70f, Vector2.zero);

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
                var button = UiKit.Button(tabs, "Part" + (i + 1), null, null, null, () => ShowPart(index), UiKit.Kind.Ghost, null, 15f, 23);
                int number = part.number;
                LocText.Set(UiKit.LabelOf(button), () => Loc.F("Phần {0}", "Part {0}", "パート{0}", number) + range);
                UiKit.Size(button, 132f, 46f);
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
                image.sprite = UiKit.Circle;
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
            _highlightPaper = true; // every passage / question text below can be highlighted

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
                    default: PaperText(sheet, Loc.F("(Dạng câu hỏi chưa hỗ trợ: {0})", "(Question type not supported yet: {0})", "（未対応の問題形式：{0}）", group.type), 16f, FontStyles.Italic).color = Bad; break;
                }
            }
            _highlightPaper = false;
            _questionScroll.verticalNormalizedPosition = 1f;
            if (_passageScroll.gameObject.activeSelf) _passageScroll.verticalNormalizedPosition = 1f;
        }

        // ─────────── Highlights ───────────

        private void ClearHighlights()
        {
            if (_highlights.Count() == 0) { HudFeed.Post(Loc.T("Chưa có đoạn nào được tô sáng.", "Nothing is highlighted yet.", "まだハイライトはありません。"), HudFeed.Kind.Info, 2.5f); return; }
            ShowConfirm(Loc.T("Xoá mọi chỗ tô sáng?", "Clear every highlight?", "ハイライトをすべて消しますか？"),
                Loc.F("Xoá {0} chỗ tô sáng trong cả bài này. Câu trả lời không bị ảnh hưởng.", "Remove {0} highlights from this paper. Your answers are not affected.", "この試験のハイライト{0}件を消します。解答には影響しません。", _highlights.Count()),
                Loc.T("Xoá hết", "Clear all", "すべて消す"),
                () => { _confirm.gameObject.SetActive(false); _highlights.Clear(); });
        }

        /// <summary>Copies what is highlighted in the part on screen into the notebook (one bullet per highlight).</summary>
        private void CopyHighlightsToNotebook()
        {
            var snippets = _highlights.Snippets(k => k.StartsWith("p" + _viewPart + "/"));
            if (snippets.Count == 0) { HudFeed.Post(Loc.T("Tô sáng một đoạn trước, rồi bấm → Sổ để chép vào sổ tay.", "Highlight some text first, then press → Notebook.", "先にテキストをハイライトしてから「→ノート」を押してください。"), HudFeed.Kind.Info, 3f); return; }
            string text = string.Join("\n", snippets.Select(x => "• " + x));
            int left = NihongoLife.Notebook.NotebookService.Append(text, $"{_test.DisplayTitle} · {Loc.F("Phần {0}", "Part {0}", "パート{0}", _test.parts[_viewPart].number)}");
            if (left > 0) HudFeed.Post(Loc.F("Sổ tay hết chỗ — còn {0} ký tự chưa chép. Mua thêm giấy ở Hibari Mart.", "Notebook full — {0} characters not copied. Buy paper at Hibari Mart.", "ノートがいっぱいです — {0}文字コピーできません。ひばりマートで紙を買えます。", left), HudFeed.Kind.Warning, 4f);
            else HudFeed.Post(Loc.F("Đã chép {0} đoạn vào sổ tay (trang {1}).", "Copied {0} highlights to the notebook (page {1}).", "{0}件をノートにコピーしました（{1}ページ）。", snippets.Count, NihongoLife.Notebook.NotebookService.CurrentPage + 1), HudFeed.Kind.Reward, 3f);
            NotebookUI.Show();
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
                if (!System.IO.File.Exists(path)) { PaperText(parent, Loc.F("(Thiếu hình: {0})", "(Missing image: {0})", "（画像がありません：{0}）", relative), 15f, FontStyles.Italic); return; }
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
            PaperText(summary, Loc.T("KẾT QUẢ", "RESULT", "結果"), 15f, FontStyles.Bold).color = Accent;
            PaperText(summary, $"<size=170%><b>{r.Raw}</b></size> / {r.Max}      {Loc.T("Band ước tính", "Estimated band", "推定バンド")} <size=150%><b>{r.Band:0.0}</b></size>", 22f, FontStyles.Normal);
            PaperText(summary, string.Join("      ", r.RawByPart.Select(kv => $"{Loc.F("Phần {0}", "Part {0}", "パート{0}", kv.Key)}: <b>{kv.Value}/{r.MaxByPart[kv.Key]}</b>")), 17f, FontStyles.Normal);
            PaperText(summary, Loc.T("Band là ước tính theo bảng quy đổi điểm thô công khai, không phải kết quả IELTS chính thức.", "The band is an estimate from the public raw-score table, not an official IELTS result.", "バンドは公開されている換算表による推定で、公式のIELTS結果ではありません。"), 14.5f, FontStyles.Italic).color = PaperMuted;

            PaperText(content, Loc.T("Xem lại từng câu", "Question by question", "一問ずつ見直し"), 19f, FontStyles.Bold);
            var header = Row(content, "Head", 10f);
            Cell(header, Loc.T("Câu", "No.", "問"), 60f, FontStyles.Bold); Cell(header, Loc.T("Bạn trả lời", "Your answer", "あなたの答え"), 360f, FontStyles.Bold); Cell(header, Loc.T("Đáp án", "Correct answer", "正解"), 420f, FontStyles.Bold); Cell(header, "", 120f, FontStyles.Bold);
            foreach (var item in r.Items)
            {
                var row = Row(content, "R" + item.Number, 10f);
                row.gameObject.AddComponent<Image>().color = item.IsCorrect ? new Color(0.88f, 0.96f, 0.9f, 1f) : new Color(0.99f, 0.91f, 0.9f, 1f);
                Cell(row, item.Number.ToString(), 60f, FontStyles.Bold);
                Cell(row, string.IsNullOrEmpty(item.Given) ? $"<i>{Loc.T("(bỏ trống)", "(blank)", "（空欄）")}</i>" : item.Given, 360f, FontStyles.Normal);
                Cell(row, IeltsGrader.KeyDisplay(item.Correct) + (item.Correct != null && item.Correct.Length > 1 && item.Correct.All(c => c.Length == 1) ? $"  <i>{Loc.T("(bất kỳ thứ tự)", "(any order)", "（順不同）")}</i>" : ""), 420f, FontStyles.Normal);
                var mark = Cell(row, item.IsCorrect ? Loc.T("✓ Đúng", "✓ Correct", "✓ 正解") : item.OverLimit ? Loc.T("× Quá số từ", "× Too many words", "× 語数オーバー") : Loc.T("× Sai", "× Wrong", "× 不正解"), 120f, FontStyles.Bold);
                mark.color = item.IsCorrect ? Good : Bad;
            }
            var buttons = Row(content, "Buttons", 12f);
            UiKit.Size(UiKit.Button(buttons, "Again", "Làm lại (luyện tập)", "Try again (practice)", "もう一度（練習）", () => Begin(_package, PracticeMode, false), UiKit.Kind.Accent, "replay", 16f), 260f, 50f);
            UiKit.Size(UiKit.Button(buttons, "Exam", "Thi thử lại", "Mock exam again", "もう一度模試", () => Begin(_package, ExamMode, false), UiKit.Kind.Primary, "timer", 16f), 220f, 50f);
            UiKit.Size(UiKit.Button(buttons, "Close", "Đóng", "Close", "閉じる", Close, UiKit.Kind.Secondary, "close", 16f), 160f, 50f);
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
            _confirm.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.66f);
            var box = NLUi.Panel(_confirm, "Box", UiTheme.Surface, new RectOffset(40, 40, 32, 32), 16f);
            box.GetComponent<Image>().sprite = UiKit.Rounded(22);
            NLUi.Anchor(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 0f));
            NLUi.FitContent(box);
            _confirmTitle = NLUi.Label(box, "Title", "", 26f, UiTheme.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            _confirmBody = NLUi.Label(box, "Body", "", 16.5f, UiTheme.TextSoft, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            var row = NLUi.Group(box, "Buttons", false, 14f, TextAnchor.MiddleCenter, false);
            var stay = UiKit.Button(row, "Stay", "Tiếp tục làm bài", "Keep going", "続ける", () => _confirm.gameObject.SetActive(false), UiKit.Kind.Accent, "play_arrow", 16f);
            UiKit.Size(stay, 250f, 50f);
            var yes = UiKit.Button(row, "Yes", null, null, null, () => _confirmYes?.Invoke(), UiKit.Kind.Secondary, null, 16f);
            UiKit.Size(yes, 210f, 50f);
            _confirmYesLabel = UiKit.LabelOf(yes);
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
            if (_highlightPaper && !string.IsNullOrWhiteSpace(value))
                TextHighlighter.Bind(t, _highlights, HighlightBook.KeyFor("p" + _viewPart, value));
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
