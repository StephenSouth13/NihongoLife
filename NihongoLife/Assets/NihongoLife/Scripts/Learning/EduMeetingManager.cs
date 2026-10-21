using System;
using System.Collections;
using System.Collections.Generic;
using Agora.Rtc;
using NihongoLife.Core;
using NihongoLife.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.Learning
{
    /// <summary>
    /// The Speaking lounge (F8, or "Live exam" in the exam centre): students open a waiting room — optionally for a
    /// Speaking test — teachers accept it, and both meet in a 1-to-1 Agora call where the teacher examines live.
    /// The classroom server (agora-classroom edge function) owns rooms, roles and tokens; <see cref="SpeakingLiveHub"/>
    /// announces every change in real time (the 4-second poll remains only as a safety net) and carries the live exam.
    /// </summary>
    public class EduMeetingManager : MonoBehaviour
    {
        public static EduMeetingManager Instance { get; private set; }
        // Agora App IDs are public identifiers, but they are configuration, not code: GameControlDatabase.agoraAppId or the
        // environment variable it names (AGORA_APP_ID). Tokens come from the classroom host; no certificate in the client.
        private IRtcEngine _engine;
        private string _channel = "nihongolife-classroom", _token = "";
        private string _statusVi = "Vào cùng phòng với giáo viên. Phòng bảo mật cần token còn hạn.",
                       _statusEn = "Meet your teacher in the same room. Secure rooms need a valid token.",
                       _statusJa = "先生と同じ部屋に入ります。安全な部屋には有効なトークンが必要です。";
        private bool _open, _joining, _muted;
        private AgoraClassroomService _classroom;
        private string _accountRole = "student", _waitingRoomId = "";
        private uint _issuedUid;
        private List<AgoraWaitingRoom> _waitingRooms = new List<AgoraWaitingRoom>();
        private List<AgoraJoinRequest> _joinRequests = new List<AgoraJoinRequest>();
        private AgoraWaitingRoom _selectedRoom;
        private float _nextRoomPoll;
        private bool _roomRequestPending;
        private GameObject _videoRoot;
        private VideoSurface _remote;
        private VideoSurface _local;
        private RawImage _localView, _remoteView;
        private uint _remoteUid;
        private bool _remoteVideo, _cameraOn = true, _previewing;
        private NihongoLife.Player.PlayerController _player;
        private NihongoLife.Cameras.ThirdPersonCameraController _camera;
        private bool _oldPlayerLock, _oldCameraLock, _oldVisible;
        private CursorLockMode _oldCursor;
        private SpeakingLiveHub _hub;
        // Agora callbacks may arrive off the main thread: they only set these, Update acts on them.
        private volatile bool _pendingEnterRoom, _pendingPeerJoined, _pendingPeerLeft;

        public bool IsInClassroom { get; private set; }
        public string Status => Loc.T(_statusVi, _statusEn, _statusJa);
        public bool IsOpen => _open;
        public bool IsTeacherAccount => IsTeacher;
        public bool IsBusy => _joining || _roomRequestPending;
        public bool IsMuted => _muted;
        public bool HasWaitingRoom => !string.IsNullOrWhiteSpace(_waitingRoomId);
        public bool OnlineClassroomAvailable => _previewCall || _classroom != null && _classroom.IsAvailable;
        public IReadOnlyList<AgoraWaitingRoom> WaitingRooms => _waitingRooms;
        public IReadOnlyList<AgoraJoinRequest> JoinRequests => _joinRequests;
        public AgoraWaitingRoom SelectedRoom => _selectedRoom;
        /// <summary>Own camera shown (in a call, or the pre-call camera check).</summary>
        public bool IsCameraPreviewing => _previewing;
        public bool IsCameraOn => _cameraOn;
        /// <summary>The other person in the call is sending video (otherwise the panel shows a placeholder).</summary>
        public bool HasRemoteVideo => _remoteUid != 0 && _remoteVideo;
        public uint RemoteUid => _remoteUid;
        public int CameraDeviceCount { get; private set; } = -1;
        public RawImage LocalView => _localView;
        public SpeakingLiveHub Live => _hub;
        /// <summary>The Speaking test the student asked to be examined on (empty = free conversation).</summary>
        public string ExamTestId { get; private set; } = "";
        public string ExamTestTitle { get; private set; } = "";
        public string WaitingRoomId => _waitingRoomId;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _hub = SpeakingLiveHub.GetOrCreate();
            _hub.OnRoomOpened += HandleRoomOpened;
            _hub.OnRoomClosed += HandleRoomClosed;
            _hub.OnRoomClaimed += HandleRoomClaimed;
            _hub.OnJoinRequested += HandleJoinRequested;
            _hub.OnRequestApproved += HandleRequestApproved;
            _hub.OnPeerEntered += HandlePeerEntered;
            _hub.OnPeerLeft += HandlePeerLeft;
            _hub.OnResult += HandleResult;
            if (GetComponent<EduMeetingPanelUI>() == null) gameObject.AddComponent<EduMeetingPanelUI>();
            UiModalStack.Register(this, () => _open, () => SetOpen(false), "Classroom video");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame && !UiModalStack.BlocksHotkeys) SetOpen(!_open);
            if (_open && !_roomRequestPending && !IsInClassroom && Time.unscaledTime >= _nextRoomPoll)
            {
                _nextRoomPoll = Time.unscaledTime + 4f;
                RefreshClassroomLobby();
            }
            if (_pendingEnterRoom) { _pendingEnterRoom = false; if (HasWaitingRoom) _hub.EnterRoom(_waitingRoomId); }
            if (_pendingPeerJoined) { _pendingPeerJoined = false; CreateVideo(); }
            if (_pendingPeerLeft)
            {
                _pendingPeerLeft = false;
                LiveToast.Show(LiveToast.Tone.Warning, "call_end", "Người cùng phòng đã rời cuộc gọi", "The other person left the call", "相手が通話から退出しました",
                    "Bạn có thể chờ họ quay lại hoặc rời phòng.", "You can wait for them to come back or leave the room.", "戻るのを待つか、退出できます。", key: "speaking-peer");
            }
        }

        private void SetStatus(string vi, string en, string ja) { _statusVi = vi; _statusEn = en; _statusJa = ja; }
        /// <summary>A message from the server or the SDK, shown as is.</summary>
        private void SetStatusRaw(string text) { _statusVi = _statusEn = _statusJa = text ?? ""; }

        public void SetOpen(bool open)
        {
            if (_open == open) return;
            if (open)
            {
                _player = FindFirstObjectByType<NihongoLife.Player.PlayerController>();
                _camera = FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
                _oldPlayerLock = _player != null && _player.InputLocked;
                _oldCameraLock = _camera != null && _camera.IsLocked;
                _oldCursor = Cursor.lockState; _oldVisible = Cursor.visible;
            }
            else
            {
                if (_player != null) _player.InputLocked = _oldPlayerLock;
                if (_camera != null) _camera.IsLocked = _oldCameraLock;
                Cursor.lockState = _oldCursor; Cursor.visible = _oldVisible;
            }
            _open = open;
            if (_videoRoot != null) _videoRoot.SetActive(open);
            if (open)
            {
                if (_classroom == null) _classroom = FindFirstObjectByType<AgoraClassroomService>();
                _nextRoomPoll = 0f;
                RefreshClassroomLobby();
            }
            LateUpdate();
        }

        /// <summary>Opens the lounge to be examined on a Speaking test by a teacher, live.</summary>
        public void OpenForSpeakingExam(string testId, string testTitle)
        {
            ExamTestId = testId ?? "";
            ExamTestTitle = testTitle ?? "";
            SetOpen(true);
            SetStatus($"Phòng thi Speaking: {ExamTestTitle}. Tạo phòng chờ để giáo viên nhận làm giám khảo.",
                      $"Speaking exam room: {ExamTestTitle}. Open a waiting room so a teacher can examine you.",
                      $"スピーキング試験室：{ExamTestTitle}。待合室を作ると先生が試験官として入ります。");
        }

        public void ClearExam() { ExamTestId = ""; ExamTestTitle = ""; }

        private bool IsTeacher => _accountRole == "teacher" || _accountRole == "admin";
        /// <summary>A preview call (captures/tests): no server polling, the role set by <see cref="PreviewCall"/> stays.</summary>
        private bool _previewCall;

        public void RefreshClassroomLobby()
        {
            if (_previewCall) return;
            if (_classroom == null) _classroom = FindFirstObjectByType<AgoraClassroomService>();
            if (_classroom == null || !_classroom.IsAvailable || _roomRequestPending) return;
            _roomRequestPending = true;
            _classroom.GetRole(roleResponse =>
            {
                _accountRole = roleResponse.ok && !string.IsNullOrWhiteSpace(roleResponse.role) ? roleResponse.role : "student";
                _hub.IsTeacher = IsTeacher;
                _hub.Connect();
                _classroom.GetMyRoom(mine =>
                {
                    if (mine.ok && mine.room != null)
                    {
                        _waitingRoomId = mine.room.id;
                        if (mine.room.status == "matched") { _roomRequestPending = false; RequestRoomToken(mine.room.id); return; }
                    }
                    else _waitingRoomId = "";
                    _classroom.ListWaitingRooms(response =>
                    {
                        if (!response.ok) { _roomRequestPending = false; SetStatusRaw(response.error); return; }
                        _waitingRooms = new List<AgoraWaitingRoom>(response.rooms ?? Array.Empty<AgoraWaitingRoom>());
                        if (IsTeacher && !string.IsNullOrWhiteSpace(_waitingRoomId))
                            _classroom.ListJoinRequests(requests => { _roomRequestPending = false; _joinRequests = new List<AgoraJoinRequest>(requests.requests ?? Array.Empty<AgoraJoinRequest>()); });
                        else _roomRequestPending = false;
                    });
                });
            });
        }

        public void CreateWaitingRoom()
        {
            if (_classroom == null || _roomRequestPending) return;
            _roomRequestPending = true;
            _classroom.CreateRoom(response =>
            {
                _roomRequestPending = false;
                if (!response.ok) { SetStatusRaw(response.error); return; }
                _waitingRoomId = response.room?.id ?? "";
                int level = response.room != null ? Mathf.Max(1, response.room.level) : 1;
                _hub.AnnounceRoomOpen(_waitingRoomId, ExamTestId, ExamTestTitle, level);
                bool exam = !string.IsNullOrEmpty(ExamTestId);
                SetStatus(exam ? "Đã mở phòng thi. Giáo viên trực tuyến vừa nhận được thông báo." : "Đã tạo phòng chờ. Giáo viên trực tuyến vừa nhận được thông báo.",
                          exam ? "Exam room open. Teachers online were notified just now." : "Waiting room open. Teachers online were notified just now.",
                          exam ? "試験室を開きました。オンラインの先生に通知しました。" : "待合室を作りました。オンラインの先生に通知しました。");
                LiveToast.Show(LiveToast.Tone.Live, "campaign",
                    exam ? "Phòng thi Speaking đã mở" : "Phòng chờ đã mở", exam ? "Speaking exam room is open" : "Waiting room is open", exam ? "スピーキング試験室を開きました" : "待合室を開きました",
                    "Đang chờ giáo viên nhận lớp — bạn sẽ được nối máy ngay khi có người nhận.", "Waiting for a teacher — you will be connected the moment one accepts.", "先生を待っています。受け入れられるとすぐにつながります。",
                    key: "speaking-room");
            });
        }

        public void ClaimWaitingRoom(string roomId)
        {
            if (_classroom == null || _roomRequestPending) return;
            string studentId = _waitingRooms.Find(r => r.id == roomId)?.student_id;
            _roomRequestPending = true;
            _classroom.ClaimRoom(roomId, response =>
            {
                _roomRequestPending = false;
                if (!response.ok) { SetStatusRaw(response.error); return; }
                _hub.AnnounceClaimed(roomId, studentId ?? response.room?.student_id);
                JoinIssuedRoom(response);
            });
        }

        private void RequestRoomToken(string roomId)
        {
            if (_classroom == null || _roomRequestPending || _joining || IsInClassroom) return;
            _roomRequestPending = true;
            _classroom.GetToken(roomId, response =>
            {
                _roomRequestPending = false;
                if (!response.ok) { SetStatusRaw(response.error); return; }
                JoinIssuedRoom(response);
            });
        }

        private void JoinIssuedRoom(AgoraClassroomResponse response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.token) || string.IsNullOrWhiteSpace(response.channelName))
            {
                SetStatus("Máy chủ không cấp đủ channel/token Agora.", "The server did not issue an Agora channel/token.", "サーバーからAgoraのチャンネル/トークンが発行されませんでした。");
                return;
            }
            _waitingRoomId = response.room?.id ?? _waitingRoomId;
            JoinAgoraClassroom(response.channelName, response.token, response.uid);
        }

        public void CloseWaitingRoom()
        {
            if (_classroom == null || string.IsNullOrWhiteSpace(_waitingRoomId)) return;
            string id = _waitingRoomId;
            _classroom.CloseRoom(id, response =>
            {
                if (response.ok)
                {
                    _hub.AnnounceRoomClosed(id);
                    _waitingRoomId = "";
                    SetStatus("Đã đóng phòng chờ.", "Waiting room closed.", "待合室を閉じました。");
                    LiveToast.Hide("speaking-room");
                }
                else SetStatusRaw(response.error);
            });
        }

        private void LateUpdate()
        {
            if (!_open) return;
            if (_player != null) _player.InputLocked = true;
            if (_camera != null) _camera.IsLocked = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        public void SelectWaitingRoom(AgoraWaitingRoom room) => _selectedRoom = room;

        public void RequestJoinRoom(string roomId)
        {
            if (_classroom == null || _roomRequestPending) return;
            string teacherId = _waitingRooms.Find(r => r.id == roomId)?.teacher_id;
            _roomRequestPending = true;
            _classroom.RequestJoin(roomId, response =>
            {
                _roomRequestPending = false;
                if (!response.ok) { SetStatusRaw(response.error); return; }
                _hub.AnnounceJoinRequest(roomId, teacherId, 1);
                SetStatus("Đã gửi yêu cầu. Giáo viên vừa nhận được thông báo.", "Request sent. The teacher was notified just now.", "リクエストを送りました。先生に通知しました。");
            });
        }

        public void ApproveJoinRequest(string requestId)
        {
            if (_classroom == null || _roomRequestPending) return;
            string studentId = _joinRequests.Find(r => r.id == requestId)?.student_id;
            _roomRequestPending = true;
            _classroom.ApproveRequest(requestId, response =>
            {
                _roomRequestPending = false;
                if (!response.ok) { SetStatusRaw(response.error); return; }
                _hub.AnnounceApproved(response.room?.id ?? _waitingRoomId, studentId);
                JoinIssuedRoom(response);
            });
        }

        // ─────────── Real-time hints ───────────

        private void HandleRoomOpened(string roomId, string student, string test, int level)
        {
            bool exam = !string.IsNullOrEmpty(test);
            LiveToast.Show(LiveToast.Tone.Live, exam ? "record_voice_over" : "person_add",
                exam ? "Học viên chờ thi Speaking" : "Học viên đang chờ lớp", exam ? "A student is waiting for a Speaking exam" : "A student is waiting for class", exam ? "スピーキング試験の受験者が待っています" : "生徒が授業を待っています",
                exam ? $"{student} (Level {level}) · {test}" : $"{student} (Level {level}) vừa mở phòng chờ.",
                exam ? $"{student} (Level {level}) · {test}" : $"{student} (Level {level}) just opened a waiting room.",
                exam ? $"{student}（レベル{level}）· {test}" : $"{student}（レベル{level}）が待合室を開きました。",
                "Xem phòng", "View room", "部屋を見る", () => { SetOpen(true); RefreshNow(); }, 10f, "room-" + roomId);
            RefreshNow();
        }

        private void HandleRoomClosed(string roomId)
        {
            LiveToast.Hide("room-" + roomId);
            _waitingRooms.RemoveAll(r => r.id == roomId);
            if (_selectedRoom != null && _selectedRoom.id == roomId) _selectedRoom = null;
            RefreshNow();
        }

        private void HandleRoomClaimed(string roomId, string teacher)
        {
            if (roomId != _waitingRoomId && HasWaitingRoom) return;
            _waitingRoomId = roomId;
            LiveToast.Show(LiveToast.Tone.Success, "how_to_reg",
                "Giáo viên đã nhận lớp", "A teacher accepted your room", "先生が受け入れました",
                $"{teacher} đang vào phòng — đang nối máy…", $"{teacher} is joining — connecting…", $"{teacher}先生が入室中です。接続しています…",
                "Mở phòng", "Open room", "部屋を開く", () => SetOpen(true), 8f, "speaking-room");
            SetOpen(true);
            _roomRequestPending = false;
            RequestRoomToken(roomId);
        }

        private void HandleJoinRequested(string roomId, string student, int level)
        {
            LiveToast.Show(LiveToast.Tone.Live, "person_add",
                "Có học viên xin vào lớp", "A student asks to join", "参加リクエストがあります",
                $"{student} (Level {level}) đang chờ bạn duyệt.", $"{student} (Level {level}) is waiting for your approval.", $"{student}（レベル{level}）が承認を待っています。",
                "Duyệt ngay", "Review", "確認する", () => { SetOpen(true); RefreshNow(); }, 10f, "request-" + roomId + student);
            RefreshNow();
        }

        private void HandleRequestApproved(string roomId, string teacher)
        {
            _waitingRoomId = roomId;
            LiveToast.Show(LiveToast.Tone.Success, "how_to_reg",
                "Yêu cầu đã được duyệt", "Your request was approved", "リクエストが承認されました",
                $"{teacher} cho bạn vào lớp — đang nối máy…", $"{teacher} let you in — connecting…", $"{teacher}先生が承認しました。接続しています…",
                seconds: 6f, key: "speaking-room");
            SetOpen(true);
            _roomRequestPending = false;
            RequestRoomToken(roomId);
        }

        private void HandlePeerEntered(string name, string role)
        {
            bool teacher = role == "teacher";
            LiveToast.Show(LiveToast.Tone.Success, teacher ? "co_present" : "person",
                teacher ? "Giám khảo đã vào phòng" : "Học viên đã vào phòng", teacher ? "The examiner is in the room" : "The student is in the room", teacher ? "試験官が入室しました" : "生徒が入室しました",
                $"{name} đã sẵn sàng.", $"{name} is ready.", $"{name}さんの準備ができました。", seconds: 5f, key: "speaking-peer");
        }

        private void HandlePeerLeft(string name) => _pendingPeerLeft = true;

        private void HandleResult(SpeakingLiveResult result)
        {
            var record = result.ToRecord();
            NihongoLife.Exam.Ielts.IeltsProductiveStore.Save(record);
            if (!record.countedInHistory && record.overall > 0f)
            {
                record.countedInHistory = true;
                NihongoLife.Exam.Ielts.IeltsAttemptStore.AddHistory(new NihongoLife.Exam.Ielts.IeltsHistoryEntry { testId = record.testId, finishedUtc = record.finishedUtc, mode = "live", band = record.overall });
                NihongoLife.Exam.Ielts.IeltsProductiveStore.Save(record);
            }
            ExamMailboxUI.NotifyGraded(record);
            LiveToast.Show(LiveToast.Tone.Success, "workspace_premium",
                $"Kết quả Speaking: Band {result.overall:0.0}", $"Speaking result: Band {result.overall:0.0}", $"スピーキング結果：バンド {result.overall:0.0}",
                $"Giám khảo {result.examinerName} đã chấm. Bài được lưu trong hộp thư (phím I).",
                $"Marked by {result.examinerName}. Saved in your exam mailbox (key I).",
                $"{result.examinerName}先生が採点しました。試験ボックス（Iキー）に保存しました。",
                "Xem chi tiết", "See details", "詳細を見る", () => ExamMailboxUI.Open(record.id), 12f, "speaking-result");
        }

        private void RefreshNow()
        {
            _nextRoomPoll = 0f;
            if (!_roomRequestPending) RefreshClassroomLobby();
        }

        // ─────────── Joining the call ───────────

        public void JoinAgoraClassroom(string channelName, string token = "", uint uid = 0)
        {
            SetOpen(true);
            if (IsInClassroom || _joining) return;
            if (string.IsNullOrWhiteSpace(channelName)) { SetStatus("Vui lòng nhập tên phòng.", "Please enter a room name.", "部屋の名前を入力してください。"); return; }
            _channel = channelName.Trim(); _token = token ?? ""; _issuedUid = uid;
            StartCoroutine(JoinWithPermission());
        }

        private IEnumerator JoinWithPermission()
        {
            _joining = true;
            SetStatus("Đang xin quyền micro và camera...", "Asking for microphone and camera permission…", "マイクとカメラの許可を求めています…");
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone | UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone) || !Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                SetStatus("Chưa được cấp quyền micro/camera. Kiểm tra quyền trong hệ điều hành.", "No microphone/camera permission. Check the system privacy settings.", "マイク/カメラの許可がありません。OSの設定を確認してください。");
                _joining = false; yield break;
            }
            try
            {
                EnsureEngine();
                var options = new ChannelMediaOptions();
                options.channelProfile.SetValue(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_COMMUNICATION);
                options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
                options.publishCameraTrack.SetValue(true);
                options.publishMicrophoneTrack.SetValue(true);
                options.autoSubscribeAudio.SetValue(true);
                options.autoSubscribeVideo.SetValue(true);
                Check(_engine.JoinChannel(_token, _channel, _issuedUid, options), "join");
                SetStatus("Đang kết nối Agora...", "Connecting…", "接続しています…");
            }
            catch (Exception ex)
            {
                SetStatusRaw(ex.Message); _joining = false;
                if (_engine != null) { _engine.Dispose(); _engine = null; }
            }
        }

        /// <summary>Agora App ID from the environment variable named in GameControlDatabase, else the database value.</summary>
        public static string ConfiguredAppId()
        {
            string key = "AGORA_APP_ID", fallback = "";
            if (GameServices.TryGet(out GameControlService control) && control.Database != null)
            {
                if (!string.IsNullOrWhiteSpace(control.Database.agoraAppIdEnvironmentKey)) key = control.Database.agoraAppIdEnvironmentKey;
                fallback = control.Database.agoraAppId;
            }
            else
            {
                var db = Resources.Load<GameControlDatabase>("Control/NihongoLifeControlDatabase");
                if (db != null) { if (!string.IsNullOrWhiteSpace(db.agoraAppIdEnvironmentKey)) key = db.agoraAppIdEnvironmentKey; fallback = db.agoraAppId; }
            }
            string fromEnv = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(fromEnv) ? fallback : fromEnv.Trim();
        }

        /// <summary>Creates the Agora engine once (audio + video), for the camera check and for calls.</summary>
        private void EnsureEngine()
        {
            if (_engine != null) return;
            string appId = ConfiguredAppId();
            if (string.IsNullOrWhiteSpace(appId))
                throw new InvalidOperationException(Loc.T("Chưa cấu hình Agora App ID (biến môi trường AGORA_APP_ID hoặc Control Database).",
                    "Agora App ID is not configured (AGORA_APP_ID or the Control Database).", "Agora App IDが設定されていません（AGORA_APP_ID または Control Database）。"));
            _engine = RtcEngine.CreateAgoraRtcEngine();
            Check(_engine.Initialize(new RtcEngineContext { appId = appId }), "init");
            _engine.InitEventHandler(new Events(this));
            Check(_engine.EnableAudio(), "audio");
            Check(_engine.EnableVideo(), "video");
            var devices = _engine.GetVideoDeviceManager()?.EnumerateVideoDevices();
            CameraDeviceCount = devices?.Length ?? 0;
        }

        // ─────────── Video views (owned by EduMeetingPanelUI, fed by Agora) ───────────

        /// <summary>The lounge panel gives the two pictures the call is drawn into (no separate canvas behind the panel).</summary>
        public void BindVideoViews(RawImage local, RawImage remote)
        {
            _localView = local;
            _remoteView = remote;
            _local = Surface(local, _local);
            _remote = Surface(remote, _remote);
            if (_previewing || IsInClassroom) { _local.SetForUser(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA_PRIMARY); _local.SetEnable(_cameraOn); }
            if (_remoteUid != 0) { _remote.SetForUser(_remoteUid, _channel, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE); _remote.SetEnable(true); }
        }

        private static VideoSurface Surface(RawImage view, VideoSurface old)
        {
            if (view == null) return old;
            var surface = view.GetComponent<VideoSurface>() ?? view.gameObject.AddComponent<VideoSurface>();
            surface.SetEnable(false);
            return surface;
        }

        /// <summary>"Check camera": the local camera shows before any call, to check framing, light and permission.</summary>
        public void StartCameraCheck()
        {
            if (_previewing || IsInClassroom) return;
            StartCoroutine(CameraCheckRoutine());
        }

        private IEnumerator CameraCheckRoutine()
        {
            SetStatus("Đang mở camera...", "Opening the camera…", "カメラを起動しています…");
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam | UserAuthorization.Microphone);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                SetStatus("Chưa được cấp quyền camera. Hãy cho phép trong cài đặt quyền riêng tư của hệ điều hành.", "No camera permission. Allow it in the system privacy settings.", "カメラの許可がありません。OSのプライバシー設定で許可してください。");
                yield break;
            }
            try
            {
                EnsureEngine();
                if (CameraDeviceCount == 0)
                {
                    SetStatus("Không tìm thấy camera trên máy này. Bạn vẫn có thể vào lớp chỉ với micro.", "No camera found. You can still join with the microphone only.", "カメラが見つかりません。マイクだけでも参加できます。");
                    yield break;
                }
                Check(_engine.StartPreview(), "preview");
                _previewing = true;
                _cameraOn = true;
                if (_local != null) { _local.SetForUser(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA_PRIMARY); _local.SetEnable(true); }
                SetStatus("Camera đang bật — kiểm tra khung hình và ánh sáng trước khi vào lớp.", "Camera on — check framing and light before class.", "カメラがオンです。授業の前に映りと明るさを確認してください。");
            }
            catch (Exception ex) { SetStatusRaw(ex.Message); _previewing = false; }
        }

        public void StopCameraCheck()
        {
            if (!_previewing || IsInClassroom) return;
            if (_engine != null) _engine.StopPreview();
            if (_local != null) _local.SetEnable(false);
            _previewing = false;
            SetStatus("Đã tắt camera.", "Camera off.", "カメラをオフにしました。");
        }

        /// <summary>Camera on/off in a call (others see a placeholder) or during the check.</summary>
        public void ToggleCamera()
        {
            if (_engine == null) return;
            bool on = !_cameraOn;
            int code = IsInClassroom ? _engine.MuteLocalVideoStream(!on) : 0;
            if (code != 0) { SetStatusRaw(Loc.T("Không đổi được camera: ", "Could not switch the camera: ", "カメラを切り替えられません：") + code); return; }
            if (on) _engine.StartPreview(); else _engine.StopPreview();
            _cameraOn = on;
            if (_local != null) _local.SetEnable(on);
            if (on) SetStatus("Camera đã bật.", "Camera on.", "カメラをオンにしました。");
            else SetStatus("Camera đã tắt — người cùng phòng chỉ nghe thấy bạn.", "Camera off — the other person only hears you.", "カメラをオフにしました。相手には声だけが届きます。");
        }

        private static void Check(int code, string action)
        {
            if (code != 0) throw new InvalidOperationException(Loc.T("Agora thất bại", "Agora failed", "Agoraの処理に失敗") + $" ({action}, {code}). " +
                Loc.T("Kiểm tra App ID, token và thiết bị.", "Check the App ID, token and devices.", "App ID・トークン・デバイスを確認してください。"));
        }

        /// <summary>A call started: the own camera goes into the panel's small picture.</summary>
        private void CreateVideo()
        {
            _previewing = true;
            if (_local != null) { _local.SetForUser(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA_PRIMARY); _local.SetEnable(_cameraOn); }
        }

        private sealed class Events : IRtcEngineEventHandler
        {
            private readonly EduMeetingManager _owner;
            public Events(EduMeetingManager owner) { _owner = owner; }
            public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
            {
                _owner._joining = false; _owner.IsInClassroom = true;
                _owner.SetStatus("Đã vào phòng. Đang chờ người cùng phòng.", "In the room. Waiting for the other person.", "入室しました。相手を待っています。");
                _owner._pendingEnterRoom = true;
                _owner._pendingPeerJoined = true;
            }
            public override void OnUserJoined(RtcConnection connection, uint remoteUid, int elapsed)
            {
                _owner._remoteUid = remoteUid; _owner._remoteVideo = true;
                if (_owner._remote != null)
                {
                    _owner._remote.SetForUser(remoteUid, connection.channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
                    _owner._remote.SetEnable(true);
                }
                _owner._pendingPeerJoined = true;
                _owner.SetStatus("Đã kết nối người cùng phòng.", "Connected with the other person.", "相手とつながりました。");
            }
            public override void OnRemoteVideoStateChanged(RtcConnection connection, uint remoteUid, REMOTE_VIDEO_STATE state, REMOTE_VIDEO_STATE_REASON reason, int elapsed)
            {
                if (remoteUid != _owner._remoteUid) return;
                _owner._remoteVideo = state == REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_DECODING || state == REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_STARTING;
            }
            public override void OnUserOffline(RtcConnection connection, uint remoteUid, USER_OFFLINE_REASON_TYPE reason)
            {
                if (_owner._remoteUid != remoteUid) return;
                if (_owner._remote != null) _owner._remote.SetEnable(false);
                _owner._remoteUid = 0; _owner._remoteVideo = false;
                _owner.SetStatus("Người cùng phòng đã rời cuộc gọi.", "The other person left the call.", "相手が通話から退出しました。");
                _owner._pendingPeerLeft = true;
            }
            public override void OnError(int err, string msg)
            {
                _owner._joining = false;
                _owner.SetStatusRaw($"Agora {err}: {msg}. " + Loc.T("Kiểm tra token còn hạn và đúng phòng/App ID.", "Check the token, the room and the App ID.", "トークン・部屋・App IDを確認してください。"));
            }
            public override void OnTokenPrivilegeWillExpire(RtcConnection connection, string token)
            {
                if (_owner._classroom == null || string.IsNullOrWhiteSpace(_owner._waitingRoomId)) return;
                _owner._classroom.GetToken(_owner._waitingRoomId, response =>
                {
                    if (response.ok && !string.IsNullOrWhiteSpace(response.token) && _owner._engine != null)
                    {
                        int code = _owner._engine.RenewToken(response.token);
                        if (code == 0) _owner.SetStatus("Đã gia hạn phiên lớp học.", "Class session renewed.", "セッションを更新しました。");
                        else _owner.SetStatusRaw("RenewToken " + code);
                    }
                    else _owner.SetStatusRaw(response.error ?? "RenewToken");
                });
            }
        }

        public void LeaveClassroom()
        {
            StopAllCoroutines(); _joining = false;
            if (_engine != null) { _engine.LeaveChannel(); _engine.StopPreview(); }
            IsInClassroom = false; _muted = false; _remoteUid = 0;
            if (_engine != null) _engine.MuteLocalAudioStream(false);
            if (_videoRoot != null) Destroy(_videoRoot);
            _videoRoot = null; _remoteVideo = false; _previewing = false;
            if (_remote != null) _remote.SetEnable(false);
            if (_local != null) _local.SetEnable(false);
            _hub?.LeaveRoom();
            bool preview = _previewCall;
            _previewCall = false;
            SetStatus("Đã rời phòng.", "You left the room.", "退出しました。");
            if (preview) { _waitingRoomId = ""; return; }
            if (_classroom != null && !string.IsNullOrWhiteSpace(_waitingRoomId)) CloseWaitingRoom();
        }

        /// <summary>Captures and tests: shows the lobby as a signed-in student or teacher, with these rooms (no server).</summary>
        public void PreviewLobby(bool teacher, AgoraWaitingRoom[] rooms, AgoraJoinRequest[] requests = null, bool waiting = false)
        {
            _previewCall = true;
            _accountRole = teacher ? "teacher" : "student";
            _hub.IsTeacher = teacher;
            _waitingRooms = new List<AgoraWaitingRoom>(rooms ?? Array.Empty<AgoraWaitingRoom>());
            _joinRequests = new List<AgoraJoinRequest>(requests ?? Array.Empty<AgoraJoinRequest>());
            _waitingRoomId = waiting ? "preview" : "";
            _selectedRoom = teacher && _waitingRooms.Count > 0 ? _waitingRooms[0] : null;
            SetOpen(true);
        }

        /// <summary>Ends a preview (lobby or call) without touching the server.</summary>
        public void EndPreview()
        {
            if (IsInClassroom) LeaveClassroom();
            _previewCall = false;
            _waitingRoomId = "";
            _waitingRooms.Clear();
            _joinRequests.Clear();
            _selectedRoom = null;
            _accountRole = "student";
        }

        /// <summary>Captures and tests: shows the in-call layout without Agora (no media, no server).</summary>
        public void PreviewCall(bool teacher, string roomId = "preview")
        {
            _previewCall = true;
            _accountRole = teacher ? "teacher" : "student";
            _hub.IsTeacher = teacher;
            _waitingRoomId = roomId;
            IsInClassroom = true;
            SetOpen(true);
        }

        public void ToggleMicrophone()
        {
            if (_engine == null || !IsInClassroom) return;
            int code = _engine.MuteLocalAudioStream(!_muted);
            if (code == 0) _muted = !_muted;
            else SetStatusRaw(Loc.T("Không đổi được micro: ", "Could not switch the microphone: ", "マイクを切り替えられません：") + code);
        }

        public void SwitchCamera() { if (_engine != null) _engine.SwitchCamera(); }

        public void JoinExternalMeeting(string meetingUrl)
        {
            if (Uri.TryCreate(meetingUrl, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http")) Application.OpenURL(meetingUrl);
        }

        public void TakeScreenshot()
        {
            string folder = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
            System.IO.Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "EduMeeting_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"));
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (_hub != null)
            {
                _hub.OnRoomOpened -= HandleRoomOpened;
                _hub.OnRoomClosed -= HandleRoomClosed;
                _hub.OnRoomClaimed -= HandleRoomClaimed;
                _hub.OnJoinRequested -= HandleJoinRequested;
                _hub.OnRequestApproved -= HandleRequestApproved;
                _hub.OnPeerEntered -= HandlePeerEntered;
                _hub.OnPeerLeft -= HandlePeerLeft;
                _hub.OnResult -= HandleResult;
            }
            SetOpen(false); LeaveClassroom();
            if (_engine != null) { _engine.Dispose(); _engine = null; }
            Instance = null;
        }
    }
}
