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
            var player = SceneRefs.Find<PlayerController>();
            var camera = SceneRefs.Find<NihongoLife.Cameras.ThirdPersonCameraController>();
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
