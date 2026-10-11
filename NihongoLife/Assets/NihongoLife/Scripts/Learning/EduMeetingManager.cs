using System;
using System.Collections;
using System.Collections.Generic;
using Agora.Rtc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.Learning
{
    public class EduMeetingManager : MonoBehaviour
    {
        public static EduMeetingManager Instance { get; private set; }
        // Agora App IDs are public identifiers, but they are configuration, not code: GameControlDatabase.agoraAppId or the
        // environment variable it names (AGORA_APP_ID). Tokens come from the classroom host; no certificate in the client.
        private IRtcEngine _engine;
        private string _channel = "nihongolife-classroom", _token = "";
        private string _status = "Nhập cùng phòng với giáo viên. Phòng bảo mật cần token còn hạn.";
        private bool _open, _joining, _muted;
        private AgoraClassroomService _classroom;
        private string _accountRole = "student", _waitingRoomId = "";
        private uint _issuedUid;
        private List<AgoraWaitingRoom> _waitingRooms = new List<AgoraWaitingRoom>();
        private AgoraWaitingRoom _selectedRoom;
        private float _nextRoomPoll;
        private bool _roomRequestPending;
        private GameObject _videoRoot;
        private VideoSurface _remote;
        private uint _remoteUid;
        private NihongoLife.Player.PlayerController _player;
        private NihongoLife.Cameras.ThirdPersonCameraController _camera;
        private bool _oldPlayerLock, _oldCameraLock, _oldVisible;
        private CursorLockMode _oldCursor;
        public bool IsInClassroom { get; private set; }
        public string Status => _status;
        public bool IsOpen => _open;
        public bool IsTeacherAccount => IsTeacher;
        public bool IsBusy => _joining || _roomRequestPending;
        public bool IsMuted => _muted;
        public bool HasWaitingRoom => !string.IsNullOrWhiteSpace(_waitingRoomId);
        public bool OnlineClassroomAvailable => _classroom != null && _classroom.IsAvailable;
        public IReadOnlyList<AgoraWaitingRoom> WaitingRooms => _waitingRooms;
        public AgoraWaitingRoom SelectedRoom => _selectedRoom;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (GetComponent<EduMeetingPanelUI>() == null) gameObject.AddComponent<EduMeetingPanelUI>();
            NihongoLife.UI.UiModalStack.Register(this, () => _open, () => SetOpen(false), "Classroom video");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame && !NihongoLife.UI.UiModalStack.BlocksHotkeys) SetOpen(!_open);
            if (_open && !_roomRequestPending && !IsInClassroom && Time.unscaledTime >= _nextRoomPoll)
            {
                _nextRoomPoll = Time.unscaledTime + 4f;
                RefreshClassroomLobby();
            }
        }

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

        private bool IsTeacher => _accountRole == "teacher" || _accountRole == "admin";

        public void RefreshClassroomLobby()
        {
            if (_classroom == null) _classroom = FindFirstObjectByType<AgoraClassroomService>();
            if (_classroom == null || !_classroom.IsAvailable || _roomRequestPending) return;
            _roomRequestPending = true;
            _classroom.GetRole(roleResponse =>
            {
                _accountRole = roleResponse.ok && !string.IsNullOrWhiteSpace(roleResponse.role) ? roleResponse.role : "student";
                if (IsTeacher)
                {
                    _classroom.ListWaitingRooms(response =>
                    {
                        _roomRequestPending = false;
                        if (response.ok) _waitingRooms = new List<AgoraWaitingRoom>(response.rooms ?? Array.Empty<AgoraWaitingRoom>());
                        else _status = response.error;
                    });
                }
                else
                {
                    _classroom.GetMyRoom(response =>
                    {
                        _roomRequestPending = false;
                        if (!response.ok) { _status = response.error; return; }
                        if (response.room == null) { _waitingRoomId = ""; return; }
                        _waitingRoomId = response.room.id;
                        if (response.room.status == "matched") RequestRoomToken(response.room.id);
                        else _status = "Phòng chờ đã tạo. Đang chờ giáo viên nhận lớp...";
                    });
                }
            });
        }

        public void CreateWaitingRoom()
        {
            if (_classroom == null || _roomRequestPending) return;
            _roomRequestPending = true;
            _classroom.CreateRoom(response =>
            {
                _roomRequestPending = false;
                if (!response.ok) { _status = response.error; return; }
                _waitingRoomId = response.room?.id ?? "";
                _status = "Đã tạo phòng chờ. Giáo viên có thể chọn vào phòng.";
            });
        }

        public void ClaimWaitingRoom(string roomId)
        {
            if (_classroom == null || _roomRequestPending) return;
            _roomRequestPending = true;
            _classroom.ClaimRoom(roomId, response =>
            {
                _roomRequestPending = false;
                if (!response.ok) { _status = response.error; return; }
                JoinIssuedRoom(response);
            });
        }

        private void RequestRoomToken(string roomId)
        {
            if (_classroom == null || _roomRequestPending || _joining) return;
            _roomRequestPending = true;
            _classroom.GetToken(roomId, response =>
            {
                _roomRequestPending = false;
                if (!response.ok) { _status = response.error; return; }
                JoinIssuedRoom(response);
            });
        }

        private void JoinIssuedRoom(AgoraClassroomResponse response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.token) || string.IsNullOrWhiteSpace(response.channelName))
            {
                _status = "Máy chủ không cấp đủ channel/token Agora.";
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
                if (response.ok) { _waitingRoomId = ""; _status = "Đã đóng phòng chờ."; }
                else _status = response.error;
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

        public void JoinAgoraClassroom(string channelName, string token = "", uint uid = 0)
        {
            SetOpen(true);
            if (IsInClassroom || _joining) return;
            if (string.IsNullOrWhiteSpace(channelName)) { _status = "Vui lòng nhập tên phòng."; return; }
            _channel = channelName.Trim(); _token = token ?? ""; _issuedUid = uid;
            StartCoroutine(JoinWithPermission());
        }

        private IEnumerator JoinWithPermission()
        {
            _joining = true;
            _status = "Đang xin quyền micro và camera...";
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone | UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone) || !Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                _status = "Chưa được cấp quyền micro/camera. Kiểm tra quyền trong hệ điều hành.";
                _joining = false; yield break;
            }
            try
            {
                if (_engine == null)
                {
                    string appId = ConfiguredAppId();
                    if (string.IsNullOrWhiteSpace(appId)) throw new InvalidOperationException("Chưa cấu hình Agora App ID (biến môi trường AGORA_APP_ID hoặc Control Database).");
                    _engine = RtcEngine.CreateAgoraRtcEngine();
                    Check(_engine.Initialize(new RtcEngineContext { appId = appId }), "Khởi tạo Agora");
                    _engine.InitEventHandler(new Events(this));
                    Check(_engine.EnableAudio(), "Bật micro");
                    Check(_engine.EnableVideo(), "Bật camera");
                }
                var options = new ChannelMediaOptions();
                options.channelProfile.SetValue(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_COMMUNICATION);
                options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
                options.publishCameraTrack.SetValue(true);
                options.publishMicrophoneTrack.SetValue(true);
                options.autoSubscribeAudio.SetValue(true);
                options.autoSubscribeVideo.SetValue(true);
                Check(_engine.JoinChannel(_token, _channel, _issuedUid, options), "Vào phòng");
                _status = "Đang kết nối Agora...";
            }
            catch (Exception ex)
            {
                _status = ex.Message; _joining = false;
                if (_engine != null) { _engine.Dispose(); _engine = null; }
            }
        }

        /// <summary>Agora App ID from the environment variable named in GameControlDatabase, else the database value.</summary>
        public static string ConfiguredAppId()
        {
            string key = "AGORA_APP_ID", fallback = "";
            if (NihongoLife.Core.GameServices.TryGet(out NihongoLife.Core.GameControlService control) && control.Database != null)
            {
                if (!string.IsNullOrWhiteSpace(control.Database.agoraAppIdEnvironmentKey)) key = control.Database.agoraAppIdEnvironmentKey;
                fallback = control.Database.agoraAppId;
            }
            else
            {
                var db = Resources.Load<NihongoLife.Core.GameControlDatabase>("Control/NihongoLifeControlDatabase");
                if (db != null) { if (!string.IsNullOrWhiteSpace(db.agoraAppIdEnvironmentKey)) key = db.agoraAppIdEnvironmentKey; fallback = db.agoraAppId; }
            }
            string fromEnv = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(fromEnv) ? fallback : fromEnv.Trim();
        }

        private static void Check(int code, string action)
        {
            if (code != 0) throw new InvalidOperationException(action + " thất bại (" + code + "). Kiểm tra App ID, token và thiết bị.");
        }

        private void CreateVideo()
        {
            if (_videoRoot != null) return;
            _videoRoot = new GameObject("LiveClassroomVideo", typeof(Canvas));
            _videoRoot.transform.SetParent(transform, false);
            var canvas = _videoRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 150;
            _remote = CreateSurface("Teacher", new Vector2(0.5f, 0.55f), new Vector2(640, 360));
            var local = CreateSurface("MyCamera", new Vector2(0.85f, 0.8f), new Vector2(240, 135));
            local.SetForUser(); local.SetEnable(true);
            _videoRoot.SetActive(_open);
        }

        private VideoSurface CreateSurface(string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(_videoRoot.transform, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
            go.GetComponent<RawImage>().raycastTarget = false;
            var surface = go.AddComponent<VideoSurface>(); surface.SetEnable(false); return surface;
        }

        private sealed class Events : IRtcEngineEventHandler
        {
            private readonly EduMeetingManager _owner;
            public Events(EduMeetingManager owner) { _owner = owner; }
            public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
            {
                _owner._joining = false; _owner.IsInClassroom = true;
                _owner._status = "Đã vào phòng " + connection.channelId + ". Đang chờ người cùng phòng.";
                _owner.CreateVideo();
            }
            public override void OnUserJoined(RtcConnection connection, uint remoteUid, int elapsed)
            {
                _owner.CreateVideo(); _owner._remoteUid = remoteUid;
                _owner._remote.SetForUser(remoteUid, connection.channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
                _owner._remote.SetEnable(true); _owner._status = "Đã kết nối người cùng phòng.";
            }
            public override void OnUserOffline(RtcConnection connection, uint remoteUid, USER_OFFLINE_REASON_TYPE reason)
            {
                if (_owner._remoteUid != remoteUid) return;
                if (_owner._remote != null) _owner._remote.SetEnable(false);
                _owner._status = "Người cùng phòng đã rời cuộc gọi.";
            }
            public override void OnError(int err, string msg)
            {
                _owner._joining = false;
                _owner._status = "Agora lỗi " + err + ": " + msg + ". Kiểm tra token còn hạn và đúng phòng/App ID.";
            }
            public override void OnTokenPrivilegeWillExpire(RtcConnection connection, string token)
            {
                if (_owner._classroom == null || string.IsNullOrWhiteSpace(_owner._waitingRoomId)) return;
                _owner._classroom.GetToken(_owner._waitingRoomId, response =>
                {
                    if (response.ok && !string.IsNullOrWhiteSpace(response.token) && _owner._engine != null)
                    {
                        int code = _owner._engine.RenewToken(response.token);
                        _owner._status = code == 0 ? "Đã gia hạn phiên lớp học." : "Không gia hạn được token Agora: " + code;
                    }
                    else _owner._status = response.error ?? "Không gia hạn được token Agora.";
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
            _videoRoot = null; _remote = null; _status = "Đã rời phòng.";
            if (_classroom != null && !string.IsNullOrWhiteSpace(_waitingRoomId)) CloseWaitingRoom();
        }
        public void ToggleMicrophone()
        {
            if (_engine == null || !IsInClassroom) return;
            int code = _engine.MuteLocalAudioStream(!_muted);
            if (code == 0) _muted = !_muted;
            else _status = "Không đổi được micro: " + code;
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
            SetOpen(false); LeaveClassroom();
            if (_engine != null) { _engine.Dispose(); _engine = null; }
            Instance = null;
        }
    }
}
