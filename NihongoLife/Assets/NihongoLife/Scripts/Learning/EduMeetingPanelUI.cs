using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Learning
{
    /// <summary>
    /// The Speaking lounge window. Before a call: students choose a Speaking test and open an exam room (or a free
    /// conversation room), teachers see waiting students with their profile and accept them. In a call: the video
    /// (the other person large, own camera in the corner) beside the exam board — the examiner's console (questions,
    /// cue card, preparation / speaking timers, marking the four criteria) or the student's view of the same board,
    /// kept in step in real time by <see cref="SpeakingLiveHub"/>.
    /// </summary>
    public sealed class EduMeetingPanelUI : MonoBehaviour
    {
        private const float CardW = 1260f, CardH = 780f;

        private EduMeetingManager _manager;
        private GameObject _root;
        private RectTransform _card, _content;
        private TextMeshProUGUI _title, _subtitle;
        private float _nextRefresh;
        private string _signature;
        private RectTransform _stage, _remoteFrame, _localFrame;
        private RawImage _remoteView, _localView;
        private TextMeshProUGUI _remotePlaceholder, _localPlaceholder;
        private bool _boundViews;

        // Live exam (rebuilt panels keep these)
        private List<SpeakingLiveTest> _tests;
        private int _testIndex;
        private int _questionIndex;
        private bool _grading;
        private float _fc = 6f, _lr = 6f, _gra = 6f, _p = 6f;
        private string _comment = "";
        private TextMeshProUGUI _timerLabel;
        private Image _timerRing;
        private SpeakingLiveState _shownState;

        private void Awake()
        {
            _manager = GetComponent<EduMeetingManager>();
            BuildShell();
            Loc.Watch(this, () => { _signature = null; });
        }

        private void Start()
        {
            if (_manager.Live != null)
            {
                _manager.Live.OnStateChanged += _ => _signature = null;
                _manager.Live.OnResult += _ => _signature = null;
                _manager.Live.OnPeerEntered += (_, __) => _signature = null;
            }
        }

        private List<SpeakingLiveTest> Tests => _tests ??= SpeakingLiveTest.All();

        private void Update()
        {
            if (_root == null || _manager == null) return;
            if (_root.activeSelf != _manager.IsOpen) _root.SetActive(_manager.IsOpen);
            if (!_boundViews) { _manager.BindVideoViews(_localView, _remoteView); _boundViews = true; }
            LayoutStage();
            if (!_manager.IsOpen) return;
            TickTimer();
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.3f;
            var live = _manager.Live;
            string signature = string.Join("|", _manager.Status, _manager.IsTeacherAccount, _manager.IsBusy,
                _manager.IsInClassroom, _manager.IsMuted, _manager.HasWaitingRoom, _manager.ExamTestId,
                _manager.SelectedRoom?.id, string.Join(",", _manager.WaitingRooms.Select(r => r.id)),
                _manager.IsCameraPreviewing, _manager.IsCameraOn, _manager.HasRemoteVideo, _manager.CameraDeviceCount,
                string.Join(",", _manager.JoinRequests.Select(r => r.id)), Loc.Code,
                live?.State?.phase, live?.State?.index, live?.State?.endsUtc, live?.LastResult?.sentUtc, live?.PeerName, _grading, _testIndex, _questionIndex);
            if (signature != _signature) { _signature = signature; Rebuild(); }
        }

        // ─────────── Shell ───────────

        private void BuildShell()
        {
            var canvas = NLUi.CreateCanvas("SpeakingLoungeCanvas", 180, transform);
            _root = new GameObject("SpeakingLounge", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(canvas.transform, false);
            NLUi.Stretch((RectTransform)_root.transform);
            _root.GetComponent<Image>().color = UiTheme.Backdrop;

            var card = UiKit.Card(_root.transform, "LoungeCard", UiTheme.Ink, 24, 0.6f);
            _card = card.rectTransform;
            UiKit.Pin(_card, new Vector2(0.5f, 0.5f), 0f, 0f, CardW, CardH);
            _card.gameObject.AddComponent<HudFitRect>().Configure(0.97f, 0.95f, Vector2.zero, 0.4f);
            var glow = UiKit.Image(_card, "Glow", UiKit.Alpha(UiTheme.Live, 0.1f), UiKit.VerticalFade);
            UiKit.Strip(glow.rectTransform, true, 0f, 120f, 24f, 24f);

            var live = UiKit.Chip(_card, "LiveChip", "LIVE", UiTheme.Live, Color.white, "radio_button_checked", 12f);
            UiKit.Place(live, 40f, 34f, 80f, 26f);
            _title = UiKit.Label(_card, "Title", (Func<string>)TitleText, 32f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(_title.rectTransform, 40f, 64f, 900f, 46f);
            _subtitle = UiKit.Label(_card, "Subtitle", (Func<string>)SubtitleText, 16f, UiTheme.Muted);
            UiKit.Place(_subtitle.rectTransform, 40f, 110f, 1000f, 26f);
            UiKit.CloseButton(_card, () => _manager.SetOpen(false), 26f);

            _content = UiKit.Rect(_card, "DynamicContent");
            UiKit.Place(_content, 40f, 152f, CardW - 80f, CardH - 184f);
            BuildVideoStage();
            _root.SetActive(false);
        }

        private string TitleText()
        {
            if (_manager == null) return "";
            if (_manager.IsInClassroom) return IsExamRunning ? Loc.T("Phòng thi Speaking", "Speaking exam room", "スピーキング試験室") : Loc.T("Đang trong lớp", "In class", "授業中");
            if (_manager.IsTeacherAccount) return Loc.T("Bàn giáo viên · Phòng chờ", "Teacher desk · Waiting rooms", "先生用 · 待合室");
            return string.IsNullOrEmpty(_manager.ExamTestId)
                ? Loc.T("Phòng học trực tuyến", "Live classroom", "オンライン教室")
                : Loc.T("Thi Speaking với giám khảo", "Speaking exam with an examiner", "試験官とスピーキング試験");
        }

        private string SubtitleText() => Loc.T(
            "Gặp giáo viên 1–1 qua video · thông báo tức thời · camera và micro bảo mật bởi Agora",
            "Meet a teacher 1-to-1 on video · instant notifications · camera and microphone secured by Agora",
            "先生と1対1のビデオ授業 · 即時通知 · カメラとマイクはAgoraで保護");

        private bool IsExamRunning
        {
            get
            {
                var s = _manager.Live?.State;
                return s != null && s.part > 0 || _manager.Live?.LastResult != null || (_manager.IsTeacherAccount && _manager.IsInClassroom);
            }
        }

        // ─────────── Video stage (persistent: Agora draws into these pictures) ───────────

        private void BuildVideoStage()
        {
            _stage = UiKit.Rect(_card, "VideoStage");
            UiKit.Place(_stage, 40f, 236f, 700f, 394f);
            _remoteFrame = Frame(_stage, "RemoteVideo", out _remoteView, out _remotePlaceholder);
            UiKit.Fill(_remoteFrame);
            _localFrame = Frame(_stage, "MyCamera", out _localView, out _localPlaceholder);
            _stage.gameObject.SetActive(false);
        }

        private RectTransform Frame(RectTransform parent, string name, out RawImage view, out TextMeshProUGUI placeholder)
        {
            var frame = UiKit.Card(parent, name, new Color(0.02f, 0.03f, 0.045f, 1f), 18, 0f, false).rectTransform;
            var avatar = UiKit.Icon(frame, "videocam_off", UiTheme.Muted, 42f);
            UiKit.Pin(avatar.rectTransform, new Vector2(0.5f, 0.5f), 0f, 22f, 42f, 42f);
            placeholder = UiKit.Label(frame, "Placeholder", "", 15f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center);
            UiKit.Pin(placeholder.rectTransform, new Vector2(0.5f, 0.5f), 0f, -22f, 600f, 30f);
            view = new GameObject("View", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            view.transform.SetParent(frame, false);
            UiKit.Fill(view.rectTransform, 2f, 2f, 2f, 2f);
            view.raycastTarget = false;
            view.color = Color.white;
            // Agora frames arrive upside down on RawImage: flip vertically.
            view.uvRect = new Rect(0f, 1f, 1f, -1f);
            UiKit.Outline(frame, new Color(1f, 1f, 1f, 0.1f));
            return frame;
        }

        /// <summary>In a call: the other person large, own camera as a corner picture. Camera check: own camera large.</summary>
        private void LayoutStage()
        {
            bool show = _manager.IsOpen && (_manager.IsInClassroom || _manager.IsCameraPreviewing);
            if (_stage.gameObject.activeSelf != show) _stage.gameObject.SetActive(show);
            if (!show) return;
            bool checkOnly = !_manager.IsInClassroom;
            UiKit.Place(_stage, 40f, checkOnly ? 236f : 196f, checkOnly ? 700f : 740f, checkOnly ? 394f : 404f);
            _remoteFrame.gameObject.SetActive(!checkOnly);
            var local = _localFrame;
            if (checkOnly) UiKit.Fill(local);
            else
            {
                UiKit.Pin(local, new Vector2(1f, 0f), -14f, 14f, 224f, 126f);
                local.SetAsLastSibling();
            }
            // A RawImage without a frame yet would be a white box: show the placeholder until Agora draws.
            _remoteView.enabled = _manager.HasRemoteVideo && _remoteView.texture != null;
            _remotePlaceholder.text = _manager.RemoteUid == 0
                ? Loc.T("Đang chờ người cùng phòng vào lớp…", "Waiting for the other person to join…", "相手の入室を待っています…")
                : Loc.T("Người cùng phòng đang tắt camera", "The other person's camera is off", "相手のカメラはオフです");
            _localView.enabled = _manager.IsCameraOn && _manager.CameraDeviceCount != 0 && _localView.texture != null;
            _localPlaceholder.text = _manager.CameraDeviceCount == 0 ? Loc.T("Không có camera", "No camera", "カメラなし") : Loc.T("Camera đang tắt", "Camera off", "カメラオフ");
        }

        // ─────────── Rebuild ───────────

        private void Rebuild()
        {
            _title.text = TitleText();
            UiKit.Clear(_content);
            _timerLabel = null; _timerRing = null;
            BuildStatusBar();
            if (_manager.IsInClassroom) { BuildInCall(); return; }
            // Out of the call: the next call starts at the first question, not in the marking form.
            _grading = false;
            _questionIndex = 0;
            if (_manager.IsCameraPreviewing) { BuildCameraCheck(); return; }
            if (!_manager.OnlineClassroomAvailable) BuildOffline();
            else if (_manager.IsTeacherAccount) BuildTeacher();
            else BuildStudent();
            // The camera check works without signing in: it only opens the local camera.
            var check = UiKit.Button(_content, "CameraCheck", "Kiểm tra camera & micro", "Check camera & mic", "カメラとマイクを確認", () => _manager.StartCameraCheck(), UiKit.Kind.Ghost, "videocam", 15f);
            UiKit.Place((RectTransform)check.transform, 0f, CardH - 184f - 50f, 300f, 46f);
        }

        private void BuildStatusBar()
        {
            bool inCall = _manager.IsInClassroom;
            string vi = inCall ? "ĐANG TRONG LỚP" : _manager.IsBusy ? "ĐANG KẾT NỐI" : _manager.HasWaitingRoom ? "ĐANG CHỜ GIÁO VIÊN" : "SẴN SÀNG";
            string en = inCall ? "IN CLASS" : _manager.IsBusy ? "CONNECTING" : _manager.HasWaitingRoom ? "WAITING FOR A TEACHER" : "READY";
            string ja = inCall ? "授業中" : _manager.IsBusy ? "接続中" : _manager.HasWaitingRoom ? "先生を待っています" : "準備完了";
            Color c = inCall ? UiTheme.Good : _manager.HasWaitingRoom ? UiTheme.Live : _manager.IsBusy ? UiTheme.Warn : UiTheme.Teal;
            var pill = UiKit.Chip(_content, "State", vi, en, ja, UiKit.Alpha(c, 0.18f), c, "radio_button_checked", 12.5f);
            UiKit.Place(pill, 0f, 0f, 200f, 28f);
            pill.gameObject.AddComponent<UiPulse>().Configure(_manager.HasWaitingRoom || _manager.IsBusy ? 1.4f : 0f);
            var msg = UiKit.Label(_content, "Message", _manager.Status, 14.5f, UiTheme.TextSoft);
            UiKit.Place(msg.rectTransform, 260f, 0f, CardW - 80f - 260f, 28f);
        }

        // ─────────── Not signed in ───────────

        private void BuildOffline()
        {
            var box = UiKit.Card(_content, "LoginRequired", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(box, 0f, 48f, CardW - 80f, 460f);
            var disc = UiKit.Image(box, "Disc", UiKit.Alpha(UiTheme.Live, 0.16f), UiKit.Circle);
            UiKit.Pin(disc.rectTransform, new Vector2(0.5f, 1f), 0f, -50f, 108f, 108f);
            var ic = UiKit.Icon(disc.transform, "co_present", UiTheme.Live, 56f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 56f, 56f);
            var title = UiKit.Label(box, "Title", "Đăng nhập để vào phòng học trực tuyến", "Sign in to use the live classroom", "オンライン教室にはログインが必要です", 26f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Pin(title.rectTransform, new Vector2(0.5f, 1f), 0f, -184f, 900f, 40f);
            var text = UiKit.Label(box, "Text",
                "Học viên mở phòng chờ hoặc phòng thi Speaking; giáo viên nhận được thông báo ngay lập tức và vào phòng.\nToken Agora được cấp tự động — không cần nhập mã phòng. Bạn vẫn có thể kiểm tra camera và micro ngay bây giờ.",
                "Students open a waiting room or a Speaking exam room; teachers are notified instantly and join.\nAgora tokens are issued automatically — no room codes. You can still check your camera and microphone now.",
                "生徒が待合室またはスピーキング試験室を開くと、先生に即時通知されます。\nAgoraのトークンは自動発行され、部屋コードは不要です。今すぐカメラとマイクを確認できます。",
                16.5f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Top, wrap: true);
            UiKit.Pin(text.rectTransform, new Vector2(0.5f, 1f), 0f, -236f, 860f, 120f);
        }

        // ─────────── Student ───────────

        private void BuildStudent()
        {
            float w = CardW - 80f;
            var hero = UiKit.Card(_content, "StudentHero", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(hero, 0f, 48f, 700f, 470f);
            bool exam = !string.IsNullOrEmpty(_manager.ExamTestId);
            bool waiting = _manager.HasWaitingRoom;

            var icon = UiKit.Image(hero, "Disc", UiKit.Alpha(exam ? UiTheme.Ielts : UiTheme.Live, 0.16f), UiKit.Circle);
            UiKit.Place(icon.rectTransform, 32f, 32f, 64f, 64f);
            var ic = UiKit.Icon(icon.transform, exam ? "record_voice_over" : "co_present", exam ? UiTheme.Ielts : UiTheme.Live, 34f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 34f, 34f);
            var title = UiKit.Label(hero, "Title", (Func<string>)(() => waiting
                    ? (exam ? Loc.T("Phòng thi đã mở — đang chờ giám khảo", "Exam room open — waiting for an examiner", "試験室を開きました — 試験官を待っています")
                            : Loc.T("Phòng chờ đã sẵn sàng", "Your waiting room is ready", "待合室の準備ができました"))
                    : (exam ? Loc.T("Thi Speaking trực tiếp với giám khảo", "Take the Speaking test live with an examiner", "試験官とライブでスピーキング試験")
                            : Loc.T("Luyện nói trực tiếp cùng giáo viên", "Practise speaking live with a teacher", "先生とライブで会話練習"))),
                24f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, wrap: true);
            UiKit.Place(title.rectTransform, 112f, 30f, 560f, 70f);

            // Test chooser (Speaking sets the examiner will use).
            var label = UiKit.Label(hero, "TestLabel", "Đề thi Speaking", "Speaking test", "スピーキング試験", 13f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(label.rectTransform, 32f, 120f, 300f, 20f);
            float y = 146f;
            int shown = 0;
            foreach (var test in Tests.Take(3))
            {
                var t = test;
                bool selected = _manager.ExamTestId == t.Id;
                var b = UiKit.Button(hero, "Test_" + t.Id, t.Title, () => _manager.OpenForSpeakingExam(t.Id, t.Title), selected ? UiKit.Kind.Accent : UiKit.Kind.Ghost, selected ? "check_circle" : "record_voice_over", 14.5f);
                UiKit.Place((RectTransform)b.transform, 32f, y, 636f, 44f);
                b.interactable = !waiting;
                y += 52f; shown++;
            }
            var free = UiKit.Button(hero, "FreeTalk", "Không thi — chỉ trò chuyện với giáo viên", "No test — just talk with a teacher", "試験なし — 先生と会話だけ",
                () => _manager.ClearExam(), !exam ? UiKit.Kind.Accent : UiKit.Kind.Ghost, !exam ? "check_circle" : "forum", 14.5f);
            UiKit.Place((RectTransform)free.transform, 32f, y, 636f, 44f);
            free.interactable = !waiting;

            if (waiting)
            {
                var cancel = UiKit.Button(hero, "Cancel", "Hủy phòng chờ", "Cancel the room", "待合室を閉じる", () => _manager.CloseWaitingRoom(), UiKit.Kind.Danger, "close", 16f);
                UiKit.Place((RectTransform)cancel.transform, 32f, 392f, 260f, 52f);
                var dots = UiKit.Label(hero, "Waiting", "", 15f, UiTheme.Live, FontStyles.Bold);
                UiKit.Place(dots.rectTransform, 312f, 392f, 360f, 52f);
                dots.gameObject.AddComponent<UiWaitingDots>().Configure(() => Loc.T("Đang chờ giáo viên", "Waiting for a teacher", "先生を待っています"));
            }
            else
            {
                var create = UiKit.Button(hero, "Create", exam ? "Mở phòng thi Speaking" : "Mở phòng chờ", exam ? "Open the exam room" : "Open a waiting room", exam ? "試験室を開く" : "待合室を開く",
                    () => _manager.CreateWaitingRoom(), UiKit.Kind.Primary, "campaign", 17f);
                UiKit.Place((RectTransform)create.transform, 32f, 392f, 340f, 56f);
            }

            // Classes opened by teachers: ask to join (the teacher approves, in real time).
            var open = _manager.WaitingRooms.Where(r => r.creator_role == "teacher" && r.status != "closed").Take(2).ToList();
            var side = UiKit.Card(_content, "HowItWorks", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(side, 720f, 48f, w - 720f, 470f);
            var head = UiKit.Label(side, "Head", "Cách hoạt động", "How it works", "しくみ", 15f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 26f, 22f, 400f, 24f);
            Step(side, 0, "campaign", "Mở phòng", "Open a room", "部屋を開く", "Giáo viên trực tuyến nhận thông báo ngay.", "Online teachers are notified at once.", "オンラインの先生にすぐ通知されます。");
            Step(side, 1, "how_to_reg", "Giáo viên nhận lớp", "A teacher accepts", "先生が受け入れる", "Bạn được nối máy tự động — không cần mã.", "You are connected automatically — no codes.", "自動で接続されます。コード不要。");
            Step(side, 2, "record_voice_over", "Thi & nhận kết quả", "Exam & result", "試験と結果", "Câu hỏi, đồng hồ và band hiện realtime; bài lưu vào hộp thư.", "Questions, timers and bands appear live; saved to your mailbox.", "問題・タイマー・バンドがリアルタイムに表示され、ボックスに保存。");
            float jy = 330f;
            foreach (var room in open)
            {
                var r = room;
                string teacher = string.IsNullOrWhiteSpace(r.teacher_name) ? Loc.T("giáo viên", "teacher", "先生") : r.teacher_name;
                var join = UiKit.Button(side, "Join_" + r.id, $"Xin vào lớp của {teacher}", $"Ask to join {teacher}'s class", $"{teacher}のクラスに参加リクエスト",
                    () => _manager.RequestJoinRoom(r.id), UiKit.Kind.Success, "person_add", 14f);
                UiKit.Place((RectTransform)join.transform, 26f, jy, w - 720f - 52f, 42f);
                jy += 50f;
            }
        }

        private void Step(RectTransform parent, int index, string icon, string tVi, string tEn, string tJa, string bVi, string bEn, string bJa)
        {
            float y = 64f + index * 84f;
            var disc = UiKit.Image(parent, "StepDisc" + index, UiKit.Alpha(UiTheme.Gold, 0.14f), UiKit.Circle);
            UiKit.Place(disc.rectTransform, 26f, y, 44f, 44f);
            var ic = UiKit.Icon(disc.transform, icon, UiTheme.Gold, 22f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 22f, 22f);
            float w = parent.sizeDelta.x - 110f;
            var t = UiKit.Label(parent, "StepTitle" + index, tVi, tEn, tJa, 16f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(t.rectTransform, 84f, y - 2f, w, 24f);
            var b = UiKit.Label(parent, "StepBody" + index, bVi, bEn, bJa, 13.5f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(b.rectTransform, 84f, y + 24f, w, 44f);
        }

        // ─────────── Teacher ───────────

        private void BuildTeacher()
        {
            float w = CardW - 80f;
            var list = UiKit.Card(_content, "WaitingList", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(list, 0f, 48f, 560f, 470f);
            var head = UiKit.Label(list, "Head", "Học viên đang chờ", "Students waiting", "待っている生徒", 15f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 24f, 20f, 360f, 24f);
            var refresh = UiKit.Button(list, "Refresh", null, null, null, () => _manager.RefreshClassroomLobby(), UiKit.Kind.Ghost, "refresh", 14f);
            UiKit.Pin((RectTransform)refresh.transform, new Vector2(1f, 1f), -18f, -14f, 44f, 36f);
            int index = 0;
            foreach (var room in _manager.WaitingRooms.Where(r => r.creator_role != "teacher").Take(5))
            {
                var r = room;
                bool selected = _manager.SelectedRoom?.id == r.id;
                var row = UiKit.Button(list, "Student_" + r.id, null, null, null, () => _manager.SelectWaitingRoom(r), selected ? UiKit.Kind.Accent : UiKit.Kind.Ghost, null, 15f, 14);
                UiKit.Place((RectTransform)row.transform, 20f, 62f + index * 72f, 520f, 62f);
                UiKit.LabelOf(row).gameObject.SetActive(false);
                var av = UiKit.Avatar(row.transform, "Avatar", r.student_name, 42f, UiKit.Alpha(UiTheme.Gold, 0.2f), UiTheme.Gold);
                UiKit.Place(av.rectTransform, 12f, 10f, 42f, 42f);
                var name = UiUserContent.Mark(UiKit.Label(row.transform, "Name", r.student_name, 16f, UiTheme.Text, FontStyles.Bold));
                UiKit.Place(name.rectTransform, 68f, 9f, 300f, 24f);
                var meta = UiKit.Label(row.transform, "Meta", $"Level {Mathf.Max(1, r.level)} · {Mathf.Max(0, r.xp)} XP", 13f, UiTheme.Muted);
                UiKit.Place(meta.rectTransform, 68f, 33f, 300f, 20f);
                var chip = UiKit.Chip(row.transform, "Kind", "LIVE", selected ? Color.white : UiKit.Alpha(UiTheme.Live, 0.2f), UiTheme.Live, "radio_button_checked", 11f);
                UiKit.Pin(chip, new Vector2(1f, 0.5f), -14f, 0f, 60f, 24f);
                index++;
            }
            foreach (var request in _manager.JoinRequests.Take(Mathf.Max(0, 5 - index)))
            {
                var q = request;
                var label = UiKit.Label(list, "Request_" + q.id, (Func<string>)(() => $"{q.student_name}  <size=80%><color=#9FB2C5>{Loc.T("xin vào lớp", "asks to join", "参加希望")} · Level {Mathf.Max(1, q.level)}</color></size>"), 15.5f, UiTheme.Text);
                UiKit.Place(label.rectTransform, 24f, 62f + index * 72f, 340f, 62f);
                var approve = UiKit.Button(list, "Approve_" + q.id, "Duyệt", "Approve", "承認", () => _manager.ApproveJoinRequest(q.id), UiKit.Kind.Success, "done", 15f);
                UiKit.Place((RectTransform)approve.transform, 380f, 70f + index * 72f, 160f, 46f);
                index++;
            }
            if (index == 0)
            {
                var empty = UiKit.Label(list, "Empty", "Chưa có học viên nào đang chờ.\nBạn sẽ nhận thông báo ngay khi có người mở phòng.",
                    "No student is waiting yet.\nYou will be notified the moment someone opens a room.", "まだ待っている生徒はいません。\n部屋が開かれるとすぐに通知されます。",
                    15.5f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center, wrap: true);
                UiKit.Place(empty.rectTransform, 40f, 170f, 480f, 90f);
            }

            var profile = UiKit.Card(_content, "Profile", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(profile, 580f, 48f, w - 580f, 470f);
            var selectedRoom = _manager.SelectedRoom;
            if (selectedRoom == null)
            {
                var pick = UiKit.Label(profile, "Select", "Chọn một học viên để xem hồ sơ trước khi nhận lớp", "Pick a student to see their profile before accepting", "受け入れる前に生徒を選んでプロフィールを確認",
                    19f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center, wrap: true);
                UiKit.Place(pick.rectTransform, 60f, 180f, w - 580f - 120f, 90f);
                return;
            }
            var avatar = UiKit.Avatar(profile, "Avatar", selectedRoom.student_name, 92f, UiKit.Alpha(UiTheme.Gold, 0.2f), UiTheme.Gold);
            UiKit.Place(avatar.rectTransform, 28f, 28f, 92f, 92f);
            var nm = UiKit.Label(profile, "Name", selectedRoom.student_name, 26f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(nm.rectTransform, 140f, 34f, 420f, 38f);
            var stats = UiKit.Label(profile, "Stats", $"Level {Mathf.Max(1, selectedRoom.level)}  ·  {Mathf.Max(0, selectedRoom.xp)} XP", 15f, UiTheme.Good);
            UiKit.Place(stats.rectTransform, 140f, 76f, 420f, 26f);
            var bio = UiKit.Label(profile, "Bio", string.IsNullOrWhiteSpace(selectedRoom.bio) ? Loc.T("Học viên chưa viết phần giới thiệu.", "No introduction yet.", "自己紹介はまだありません。") : selectedRoom.bio,
                15.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(bio.rectTransform, 28f, 144f, w - 580f - 56f, 120f);
            UiUserContent.Mark(bio);
            UiUserContent.Mark(nm);
            var accept = UiKit.Button(profile, "Accept", "Nhận lớp & làm giám khảo", "Accept & examine", "受け入れて試験官になる", () => _manager.ClaimWaitingRoom(selectedRoom.id), UiKit.Kind.Primary, "how_to_reg", 17f);
            UiKit.Place((RectTransform)accept.transform, 28f, 390f, w - 580f - 56f, 56f);
        }

        // ─────────── Camera check ───────────

        private void BuildCameraCheck()
        {
            var panel = UiKit.Card(_content, "CallControls", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(panel, 720f, 84f, CardW - 80f - 720f, 394f);
            var head = UiKit.Label(panel, "Head", "Kiểm tra camera & micro", "Camera & microphone check", "カメラとマイクの確認", 18f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 24f, 22f, 400f, 28f);
            var tip = UiKit.Label(panel, "Tip", "Xem khung hình, ánh sáng và quyền camera trước khi vào lớp.", "Check framing, light and camera permission before class.", "授業の前に映り・明るさ・許可を確認しましょう。",
                14.5f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(tip.rectTransform, 24f, 56f, CardW - 80f - 720f - 48f, 48f);
            float y = 120f;
            var cam = UiKit.Button(panel, "Camera", _manager.IsCameraOn ? "Tắt camera" : "Bật camera", _manager.IsCameraOn ? "Turn camera off" : "Turn camera on", _manager.IsCameraOn ? "カメラをオフ" : "カメラをオン",
                () => _manager.ToggleCamera(), UiKit.Kind.Secondary, _manager.IsCameraOn ? "videocam_off" : "videocam", 16f);
            UiKit.Place((RectTransform)cam.transform, 24f, y, CardW - 80f - 720f - 48f, 50f); y += 62f;
            if (_manager.CameraDeviceCount > 1)
            {
                var sw = UiKit.Button(panel, "SwitchCamera", "Đổi camera", "Switch camera", "カメラ切替", () => _manager.SwitchCamera(), UiKit.Kind.Secondary, "refresh", 16f);
                UiKit.Place((RectTransform)sw.transform, 24f, y, CardW - 80f - 720f - 48f, 50f); y += 62f;
            }
            var stop = UiKit.Button(panel, "StopCheck", "Xong — tắt camera", "Done — close camera", "完了 — カメラを閉じる", () => _manager.StopCameraCheck(), UiKit.Kind.Ghost, "done", 16f);
            UiKit.Place((RectTransform)stop.transform, 24f, y, CardW - 80f - 720f - 48f, 50f);
        }

        // ─────────── In the call ───────────

        private void BuildInCall()
        {
            float colX = 760f, colW = CardW - 80f - 760f;
            // Bottom control bar under the video.
            var bar = UiKit.Rect(_content, "CallControls");
            UiKit.Place(bar, 0f, 462f, 740f, 92f);
            float x = 0f;
            Round(bar, "Mic", _manager.IsMuted ? "mic_off" : "mic", _manager.IsMuted ? UiTheme.Bad : UiTheme.Raised, () => _manager.ToggleMicrophone(), ref x,
                _manager.IsMuted ? "Bật micro" : "Tắt micro", _manager.IsMuted ? "Unmute" : "Mute", _manager.IsMuted ? "ミュート解除" : "ミュート");
            Round(bar, "Camera", _manager.IsCameraOn ? "videocam" : "videocam_off", _manager.IsCameraOn ? UiTheme.Raised : UiTheme.Bad, () => _manager.ToggleCamera(), ref x,
                _manager.IsCameraOn ? "Tắt camera" : "Bật camera", _manager.IsCameraOn ? "Camera off" : "Camera on", _manager.IsCameraOn ? "カメラをオフ" : "カメラをオン");
            if (_manager.CameraDeviceCount > 1) Round(bar, "SwitchCamera", "refresh", UiTheme.Raised, () => _manager.SwitchCamera(), ref x, "Đổi camera", "Switch camera", "カメラ切替");
            Round(bar, "Leave", "call_end", UiTheme.Bad, () => _manager.LeaveClassroom(), ref x, "Rời cuộc gọi", "Leave", "退出");
            var peer = UiKit.Label(bar, "Peer", (Func<string>)(() => string.IsNullOrEmpty(_manager.Live?.PeerName)
                ? Loc.T("Đang chờ người cùng phòng…", "Waiting for the other person…", "相手を待っています…")
                : Loc.T("Cùng phòng: ", "With: ", "相手：") + _manager.Live.PeerName), 14.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            UiKit.Place(peer.rectTransform, x + 10f, 12f, 740f - x - 10f, 48f);

            var board = UiKit.Card(_content, "ExamBoard", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(board, colX, 44f, colW, 540f);
            if (_manager.IsTeacherAccount) BuildExaminerConsole(board, colW);
            else BuildStudentBoard(board, colW);
        }

        private void Round(RectTransform parent, string name, string icon, Color color, Action click, ref float x, string vi, string en, string ja)
        {
            var b = UiKit.Button(parent, name, null, null, null, click, UiKit.Kind.Secondary, icon, 22f, 30);
            UiKit.SetKind(b, UiKit.Kind.Secondary);
            if (b.targetGraphic is Image img) { img.color = color; b.GetComponent<ButtonColorSwap>()?.Configure(img, color, UiKit.Lighten(color, 0.12f), UiKit.Darken(color, 0.15f)); }
            UiKit.LabelOf(b).gameObject.SetActive(false);
            UiKit.Place((RectTransform)b.transform, x, 8f, 60f, 60f);
            var tip = UiKit.Label(parent, name + "Tip", vi, en, ja, 11.5f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center);
            UiKit.Place(tip.rectTransform, x - 20f, 68f, 100f, 18f);
            x += 84f;
        }

        // Student view of the live exam
        private void BuildStudentBoard(RectTransform board, float w)
        {
            var live = _manager.Live;
            var state = live?.State;
            var result = live?.LastResult;
            var head = UiKit.Label(board, "Head", "BÀI THI SPEAKING", "SPEAKING TEST", "スピーキング試験", 13f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 24f, 20f, w - 48f, 20f);
            if (result != null) { BuildResultCard(board, w, result); return; }
            if (state == null || state.part == 0)
            {
                var title = UiKit.Label(board, "Waiting", "Chờ giám khảo bắt đầu", "Waiting for the examiner to start", "試験官の開始を待っています", 22f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft, wrap: true);
                UiKit.Place(title.rectTransform, 24f, 50f, w - 48f, 64f);
                var tip = UiKit.Label(board, "Tip",
                    "Mẹo: nói rõ, trả lời 2–4 câu ở Part 1; Part 2 có 1 phút chuẩn bị rồi nói tối đa 2 phút. Câu hỏi sẽ hiện ở đây ngay khi giám khảo hỏi.",
                    "Tip: speak clearly, answer in 2–4 sentences in Part 1; Part 2 gives one minute to prepare, then up to two minutes to talk. Questions appear here the moment the examiner asks.",
                    "ヒント：はっきり話しましょう。Part 1は2〜4文で答えます。Part 2は1分準備して最大2分話します。質問は試験官が出すとすぐここに表示されます。",
                    14.5f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
                UiKit.Place(tip.rectTransform, 24f, 124f, w - 48f, 120f);
                var dots = UiKit.Label(board, "Dots", "", 15f, UiTheme.Live, FontStyles.Bold);
                UiKit.Place(dots.rectTransform, 24f, 260f, w - 48f, 30f);
                dots.gameObject.AddComponent<UiWaitingDots>().Configure(() => Loc.T("Đang chờ", "Waiting", "待機中"));
                return;
            }
            BuildQuestionCard(board, w, state, examiner: false);
        }

        private void BuildQuestionCard(RectTransform board, float w, SpeakingLiveState state, bool examiner)
        {
            var part = UiKit.Chip(board, "Part", $"PART {state.part}", UiKit.Alpha(UiTheme.Ielts, 0.2f), UiTheme.Ielts, "record_voice_over", 12.5f);
            UiKit.Place(part, 24f, 48f, 110f, 26f);
            string phaseVi = state.phase == "prep" ? "Chuẩn bị" : state.phase == "talk" ? "Đang nói" : state.phase == "grading" ? "Đang chấm" : "Câu hỏi";
            string phaseEn = state.phase == "prep" ? "Preparing" : state.phase == "talk" ? "Speaking" : state.phase == "grading" ? "Marking" : "Question";
            string phaseJa = state.phase == "prep" ? "準備中" : state.phase == "talk" ? "スピーチ中" : state.phase == "grading" ? "採点中" : "質問";
            Color pc = state.phase == "talk" ? UiTheme.Live : state.phase == "prep" ? UiTheme.Warn : state.phase == "grading" ? UiTheme.Gold : UiTheme.Teal;
            var phase = UiKit.Chip(board, "Phase", phaseVi, phaseEn, phaseJa, UiKit.Alpha(pc, 0.2f), pc, state.phase == "talk" ? "mic" : state.phase == "prep" ? "hourglass_top" : "help", 12.5f);
            UiKit.Place(phase, 146f, 48f, 130f, 26f);
            var count = UiKit.Label(board, "Count", (Func<string>)(() => Loc.F("Câu {0}/{1}", "Question {0} of {1}", "{0}/{1}問", Mathf.Max(1, state.asked), Mathf.Max(1, state.total))), 13f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            UiKit.Pin(count.rectTransform, new Vector2(1f, 1f), -24f, -48f, 200f, 26f);

            // Paper card with the question / cue card.
            var paper = UiKit.Card(board, "QuestionPaper", UiTheme.Paper, 16, 0f, false).rectTransform;
            UiKit.Place(paper, 24f, 88f, w - 48f, examiner ? 190f : 270f);
            var q = UiKit.Label(paper, "Question", state.question ?? "", state.part == 2 ? 19f : 20f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Fill(q.rectTransform, 22f, 18f, 22f, 18f);
            if (state.cuePoints != null && state.cuePoints.Length > 0)
                q.text = $"{state.question}\n<size=80%><color=#5C6676><b>{NihongoLife.Core.Loc.T("Bạn nên nói:", "You should say:", "話すポイント:")}</b>\n• {string.Join("\n• ", state.cuePoints)}</color></size>";

            // Timer ring.
            float ty = examiner ? 290f : 372f;
            _timerRing = UiKit.ProgressRing(board, "TimerRing", 92f, pc, new Color(1f, 1f, 1f, 0.08f));
            UiKit.Place((RectTransform)_timerRing.transform.parent, 24f, ty, 92f, 92f);
            _timerLabel = UiKit.Label(_timerRing.transform.parent, "TimerText", "", 22f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(_timerLabel.rectTransform);
            var timerCaption = UiKit.Label(board, "TimerCaption", (Func<string>)(() => state.endsUtc > 0
                    ? (state.phase == "prep" ? Loc.T("Thời gian chuẩn bị — ghi chú ý chính", "Preparation time — note your ideas", "準備時間 — メモを取りましょう")
                                             : Loc.T("Thời gian nói — nói liên tục đến hết giờ", "Speaking time — keep talking until time is up", "スピーチ時間 — 時間いっぱい話しましょう"))
                    : Loc.T("Trả lời tự nhiên, 2–4 câu", "Answer naturally, 2–4 sentences", "自然に2〜4文で答えましょう")),
                14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, wrap: true);
            UiKit.Place(timerCaption.rectTransform, 134f, ty + 18f, w - 160f, 56f);
            _shownState = state;
            TickTimer();
        }

        private void TickTimer()
        {
            if (_timerLabel == null || _shownState == null) return;
            if (_shownState.endsUtc <= 0)
            {
                _timerLabel.text = "—";
                if (_timerRing != null) _timerRing.fillAmount = 1f;
                return;
            }
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            double left = Math.Max(0, _shownState.endsUtc - now);
            float total = _shownState.phase == "prep" ? 60f : 120f;
            _timerLabel.text = $"{(int)left / 60}:{(int)left % 60:00}";
            if (_timerRing != null) _timerRing.fillAmount = Mathf.Clamp01((float)(left / total));
        }

        private void BuildResultCard(RectTransform board, float w, SpeakingLiveResult result)
        {
            var ring = UiKit.ProgressRing(board, "BandRing", 132f, UiTheme.Gold, new Color(1f, 1f, 1f, 0.08f));
            UiKit.Place((RectTransform)ring.transform.parent, 24f, 54f, 132f, 132f);
            ring.fillAmount = Mathf.Clamp01(result.overall / 9f);
            var band = UiKit.Label(ring.transform.parent, "Band", $"{result.overall:0.0}", 40f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(band.rectTransform, 0f, 0f, 0f, 14f);
            var cap = UiKit.Label(ring.transform.parent, "BandCap", "Band", "Band", "バンド", 12f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(cap.rectTransform, 0f, 78f, 0f, 0f);
            var who = UiKit.Label(board, "Examiner", (Func<string>)(() => Loc.F("Giám khảo {0} đã chấm", "Marked by {0}", "{0}先生が採点", result.examinerName)), 16f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(who.rectTransform, 172f, 60f, w - 196f, 50f);
            var saved = UiKit.Label(board, "Saved", "Đã lưu vào hộp thư bài thi (phím I).", "Saved in your exam mailbox (key I).", "試験ボックスに保存しました（Iキー）。", 12.5f, UiTheme.Good, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(saved.rectTransform, 172f, 116f, w - 196f, 52f);
            float y = 206f;
            Criterion(board, w, ref y, "FC", "Trôi chảy & mạch lạc", "Fluency & coherence", "流暢さと一貫性", result.fc);
            Criterion(board, w, ref y, "LR", "Vốn từ", "Lexical resource", "語彙", result.lr);
            Criterion(board, w, ref y, "GRA", "Ngữ pháp", "Grammar", "文法", result.gra);
            Criterion(board, w, ref y, "P", "Phát âm", "Pronunciation", "発音", result.p);
            if (!string.IsNullOrWhiteSpace(result.comment))
            {
                var c = UiUserContent.Mark(UiKit.Label(board, "Comment", "<noparse>" + result.comment + "</noparse>", 14.5f, UiTheme.TextSoft, FontStyles.Italic, TextAlignmentOptions.TopLeft, wrap: true));
                UiKit.Place(c.rectTransform, 24f, y + 6f, w - 48f, 520f - y - 20f);
            }
        }

        private void Criterion(RectTransform board, float w, ref float y, string code, string vi, string en, string ja, float band)
        {
            var label = UiKit.Label(board, "Crit_" + code, vi, en, ja, 14f, UiTheme.TextSoft);
            UiKit.Place(label.rectTransform, 24f, y, 220f, 26f);
            var fill = UiKit.ProgressBar(board, "Bar_" + code, UiTheme.Gold, new Color(1f, 1f, 1f, 0.07f));
            UiKit.Place((RectTransform)fill.parent, 250f, y + 9f, w - 330f, 10f);
            UiKit.SetProgress(fill, band / 9f);
            var value = UiKit.Label(board, "Val_" + code, band.ToString("0.0"), 15f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Pin(value.rectTransform, new Vector2(1f, 1f), -24f, -y, 60f, 26f);
            y += 36f;
        }

        // Examiner console
        private void BuildExaminerConsole(RectTransform board, float w)
        {
            var live = _manager.Live;
            var state = live?.State ?? new SpeakingLiveState();
            if (Tests.Count == 0)
            {
                var none = UiKit.Label(board, "NoTests", "Không có đề Speaking nào trên máy này.", "No Speaking test on this machine.", "このPCにスピーキング試験がありません。", 16f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center, wrap: true);
                UiKit.Fill(none.rectTransform, 24f, 200f, 24f, 200f);
                return;
            }
            _testIndex = Mathf.Clamp(_testIndex, 0, Tests.Count - 1);
            var test = Tests[_testIndex];
            _questionIndex = Mathf.Clamp(_questionIndex, 0, test.Questions.Count - 1);
            var question = test.Questions[_questionIndex];

            var head = UiKit.Label(board, "Head", "BẢNG GIÁM KHẢO", "EXAMINER CONSOLE", "試験官パネル", 13f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 24f, 20f, 250f, 20f);
            if (state.part == 0 && live?.LastResult == null)
            {
                // Choose the test before starting.
                var prevT = UiKit.Button(board, "PrevTest", "‹", () => { _testIndex = (_testIndex - 1 + Tests.Count) % Tests.Count; _questionIndex = 0; }, UiKit.Kind.Ghost, null, 18f);
                UiKit.Place((RectTransform)prevT.transform, 24f, 48f, 40f, 36f);
                var name = UiKit.Label(board, "TestName", test.Title, 15f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(name.rectTransform, 70f, 48f, w - 140f, 36f);
                var nextT = UiKit.Button(board, "NextTest", "›", () => { _testIndex = (_testIndex + 1) % Tests.Count; _questionIndex = 0; }, UiKit.Kind.Ghost, null, 18f);
                UiKit.Pin((RectTransform)nextT.transform, new Vector2(1f, 1f), -24f, -48f, 40f, 36f);
            }

            if (_grading) { BuildMarkingForm(board, w, test); return; }
            if (live?.LastResult != null && state.phase == "done")
            {
                BuildResultCard(board, w, live.LastResult);
                return;
            }

            var preview = new SpeakingLiveState
            {
                part = question.Part, index = question.Number, question = question.Text, cuePoints = question.CuePoints,
                phase = state.index == question.Number ? state.phase : "question", endsUtc = state.index == question.Number ? state.endsUtc : 0,
                asked = _questionIndex + 1, total = test.Questions.Count,
            };
            BuildQuestionCard(board, w, preview, examiner: true);

            float y = 400f;
            var prev = UiKit.Button(board, "PrevQ", null, null, null, () => _questionIndex--, UiKit.Kind.Ghost, "chevron_left", 15f);
            UiKit.Place((RectTransform)prev.transform, 24f, y, 48f, 44f);
            prev.interactable = _questionIndex > 0;
            bool asked = state.index == question.Number;
            if (question.Part == 2)
            {
                var prep = UiKit.Button(board, "Prep", "Cue card + 1′ chuẩn bị", "Cue card + 1′ prep", "カード + 準備1分", () => Send(test, question, "prep", 60), UiKit.Kind.Accent, "hourglass_top", 14f);
                UiKit.Place((RectTransform)prep.transform, 80f, y, 200f, 44f);
                var talk = UiKit.Button(board, "Talk", "Bắt đầu nói 2′", "Start 2′ talk", "スピーチ開始 2分", () => Send(test, question, "talk", 120), UiKit.Kind.Secondary, "mic", 14f);
                UiKit.Place((RectTransform)talk.transform, 288f, y, 170f, 44f);
            }
            else
            {
                var ask = UiKit.Button(board, "Ask", asked ? "Đã hỏi — hỏi lại" : "Hỏi câu này", asked ? "Asked — ask again" : "Ask this question", asked ? "出題済み — もう一度" : "この質問をする",
                    () => Send(test, question, "question", 0), UiKit.Kind.Accent, "campaign", 14.5f);
                UiKit.Place((RectTransform)ask.transform, 80f, y, 378f, 44f);
            }
            var next = UiKit.Button(board, "NextQ", null, null, null, () => _questionIndex++, UiKit.Kind.Ghost, "chevron_right", 15f);
            UiKit.Place((RectTransform)next.transform, 466f, y, 48f, 44f);
            next.interactable = _questionIndex < test.Questions.Count - 1;
            var mark = UiKit.Button(board, "Mark", "Kết thúc & chấm điểm", "Finish & mark", "終了して採点", () =>
            {
                _grading = true;
                var s = live.State ?? new SpeakingLiveState();
                live.SendState(new SpeakingLiveState { testId = test.Id, testTitle = test.Title, part = Mathf.Max(1, s.part), index = s.index, question = s.question, phase = "grading", asked = s.asked, total = test.Questions.Count });
            }, UiKit.Kind.Primary, "workspace_premium", 15f);
            UiKit.Place((RectTransform)mark.transform, 24f, y + 56f, w - 48f, 46f);
        }

        private void Send(SpeakingLiveTest test, SpeakingLiveQuestion question, string phase, int seconds)
        {
            double ends = seconds > 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0 + seconds : 0;
            _manager.Live?.SendState(new SpeakingLiveState
            {
                testId = test.Id, testTitle = test.Title, part = question.Part, index = question.Number, question = question.Text,
                cuePoints = question.CuePoints ?? Array.Empty<string>(), phase = phase, endsUtc = ends, asked = _questionIndex + 1, total = test.Questions.Count,
            });
        }

        private void BuildMarkingForm(RectTransform board, float w, SpeakingLiveTest test)
        {
            var title = UiKit.Label(board, "MarkTitle", "Chấm theo 4 tiêu chí IELTS", "Mark the four IELTS criteria", "IELTSの4項目で採点", 19f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 24f, 92f, w - 48f, 30f);
            float y = 134f;
            Stepper(board, w, ref y, "FC", "Trôi chảy & mạch lạc", "Fluency & coherence", "流暢さと一貫性", () => _fc, v => _fc = v);
            Stepper(board, w, ref y, "LR", "Vốn từ", "Lexical resource", "語彙", () => _lr, v => _lr = v);
            Stepper(board, w, ref y, "GRA", "Ngữ pháp", "Grammar", "文法", () => _gra, v => _gra = v);
            Stepper(board, w, ref y, "P", "Phát âm", "Pronunciation", "発音", () => _p, v => _p = v);
            var overall = UiKit.Label(board, "Overall", (Func<string>)(() => Loc.F("Band tổng (làm tròn IELTS): {0:0.0}", "Overall band (IELTS rounding): {0:0.0}", "総合バンド（IELTS丸め）：{0:0.0}", SpeakingLiveResult.Overall(_fc, _lr, _gra, _p))), 16f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(overall.rectTransform, 24f, y + 2f, w - 48f, 26f);
            y += 36f;
            var input = NewInput(board, w - 48f, 78f);
            UiKit.Place((RectTransform)input.transform, 24f, y, w - 48f, 78f);
            input.text = _comment;
            input.onValueChanged.AddListener(v => _comment = v);
            y += 90f;
            var back = UiKit.Button(board, "BackToQuestions", "Quay lại câu hỏi", "Back to questions", "質問に戻る", () => _grading = false, UiKit.Kind.Ghost, "arrow_back", 14f);
            UiKit.Place((RectTransform)back.transform, 24f, y, 190f, 44f);
            var send = UiKit.Button(board, "SendResult", "Gửi kết quả", "Send result", "結果を送信", () =>
            {
                _manager.Live?.SendResult(new SpeakingLiveResult { testId = test.Id, testTitle = test.Title, fc = _fc, lr = _lr, gra = _gra, p = _p, comment = _comment });
                _grading = false;
                LiveToast.Show(LiveToast.Tone.Success, "send", "Đã gửi kết quả", "Result sent", "結果を送信しました",
                    "Học viên vừa nhận được band và nhận xét của bạn.", "The student has just received your band and comment.", "生徒にバンドとコメントが届きました。", seconds: 5f, key: "speaking-result-sent");
            }, UiKit.Kind.Primary, "send", 15f);
            UiKit.Place((RectTransform)send.transform, 226f, y, w - 250f, 44f);
        }

        private void Stepper(RectTransform board, float w, ref float y, string code, string vi, string en, string ja, Func<float> get, Action<float> set)
        {
            var label = UiKit.Label(board, "Step_" + code, vi, en, ja, 14.5f, UiTheme.TextSoft);
            UiKit.Place(label.rectTransform, 24f, y, w - 220f, 36f);
            var minus = UiKit.Button(board, "Minus_" + code, null, null, null, () => { set(Mathf.Max(0f, get() - 1f)); _signature = null; }, UiKit.Kind.Ghost, "remove", 14f);
            UiKit.Pin((RectTransform)minus.transform, new Vector2(1f, 1f), -150f, -y, 40f, 36f);
            var value = UiKit.Label(board, "Value_" + code, get().ToString("0"), 18f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Pin(value.rectTransform, new Vector2(1f, 1f), -86f, -y, 56f, 36f);
            var plus = UiKit.Button(board, "Plus_" + code, null, null, null, () => { set(Mathf.Min(9f, get() + 1f)); _signature = null; }, UiKit.Kind.Ghost, "add", 14f);
            UiKit.Pin((RectTransform)plus.transform, new Vector2(1f, 1f), -24f, -y, 40f, 36f);
            y += 44f;
        }

        private TMP_InputField NewInput(RectTransform parent, float w, float h)
        {
            var bg = UiKit.Card(parent, "CommentInput", UiTheme.Sunken, 12, 0f, true);
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            var area = UiKit.Rect(bg.transform, "TextArea");
            UiKit.Fill(area, 12f, 8f, 12f, 8f);
            area.gameObject.AddComponent<RectMask2D>();
            var text = UiKit.Label(area, "Text", "", 14.5f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Fill(text.rectTransform);
            var ph = UiKit.Label(area, "Placeholder", "Nhận xét cho học viên…", "Comment for the student…", "生徒へのコメント…", 14.5f, UiTheme.Muted, FontStyles.Italic, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Fill(ph.rectTransform);
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = ph;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.characterLimit = 600;
            input.fontAsset = UiKit.Font;
            input.pointSize = 14.5f;
            return input;
        }
    }

    /// <summary>Gentle breathing scale for a "live / waiting" pill.</summary>
    public sealed class UiPulse : MonoBehaviour
    {
        private float _speed;
        public void Configure(float speed) => _speed = speed;
        private void Update()
        {
            if (_speed <= 0f) { transform.localScale = Vector3.one; return; }
            float s = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * Mathf.PI * _speed);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>"Waiting . . ." with animated dots, in the interface language.</summary>
    public sealed class UiWaitingDots : MonoBehaviour
    {
        private Func<string> _text;
        private TextMeshProUGUI _label;
        public void Configure(Func<string> text) { _text = text; _label = GetComponent<TextMeshProUGUI>(); }
        private void Update()
        {
            if (_label == null || _text == null) return;
            int n = (int)(Time.unscaledTime * 2.5f) % 4;
            _label.text = _text() + new string('.', n) + "<color=#00000000>" + new string('.', 3 - n) + "</color>";
        }
    }
}
