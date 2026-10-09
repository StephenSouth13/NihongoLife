using System;
using System.Collections;
using Agora.Rtc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.Learning
{
    public class EduMeetingManager : MonoBehaviour
    {
        public static EduMeetingManager Instance { get; private set; }
        // Agora App IDs are public identifiers. Tokens come from the classroom host; no certificate belongs in the client.
        [SerializeField] private string appId = "44f7d5dbf7054b8787da9313bad4305a";
        private IRtcEngine _engine;
        private string _channel = "nihongolife-classroom", _token = "";
        private string _status = "Nhập cùng phòng với giáo viên. Phòng bảo mật cần token còn hạn.";
        private bool _open, _joining, _muted;
        private GameObject _videoRoot;
        private VideoSurface _remote;
        private uint _remoteUid;
        private NihongoLife.Player.PlayerController _player;
        private NihongoLife.Cameras.ThirdPersonCameraController _camera;
        private bool _oldPlayerLock, _oldCameraLock, _oldVisible;
        private CursorLockMode _oldCursor;
        public bool IsInClassroom { get; private set; }
        public string Status => _status;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            NihongoLife.UI.UiModalStack.Register(this, () => _open, () => SetOpen(false), "Classroom video");
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f8Key.wasPressedThisFrame && !NihongoLife.UI.UiModalStack.BlocksHotkeys) SetOpen(!_open);
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
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (!_open) return;
            if (_player != null) _player.InputLocked = true;
            if (_camera != null) _camera.IsLocked = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        public void JoinAgoraClassroom(string channelName, string token = "")
        {
            SetOpen(true);
            if (IsInClassroom || _joining) return;
            if (string.IsNullOrWhiteSpace(channelName)) { _status = "Vui lòng nhập tên phòng."; return; }
            _channel = channelName.Trim(); _token = token ?? "";
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
                    string configuredId = Environment.GetEnvironmentVariable("AGORA_APP_ID");
                    _engine = RtcEngine.CreateAgoraRtcEngine();
                    Check(_engine.Initialize(new RtcEngineContext { appId = string.IsNullOrWhiteSpace(configuredId) ? appId : configuredId }), "Khởi tạo Agora");
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
                Check(_engine.JoinChannel(_token, _channel, 0, options), "Vào phòng");
                _status = "Đang kết nối Agora...";
            }
            catch (Exception ex)
            {
                _status = ex.Message; _joining = false;
                if (_engine != null) { _engine.Dispose(); _engine = null; }
            }
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
        }

        public void LeaveClassroom()
        {
            StopAllCoroutines(); _joining = false;
            if (_engine != null) { _engine.LeaveChannel(); _engine.StopPreview(); }
            IsInClassroom = false; _muted = false; _remoteUid = 0;
            if (_engine != null) _engine.MuteLocalAudioStream(false);
            if (_videoRoot != null) Destroy(_videoRoot);
            _videoRoot = null; _remote = null; _status = "Đã rời phòng.";
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
        private void OnGUI()
        {
            if (!_open) { GUI.Label(new Rect(24, Screen.height - 82, 400, 25), "[F8] Gọi video / lớp học trực tiếp"); return; }
            GUILayout.BeginArea(new Rect(20, 20, Mathf.Min(620, Screen.width - 40), 240), GUI.skin.box);
            GUILayout.Label("LỚP HỌC TRỰC TIẾP · AGORA");
            GUILayout.Label(_status);
            GUI.enabled = !_joining && !IsInClassroom;
            GUILayout.Label("Tên phòng"); _channel = GUILayout.TextField(_channel);
            GUILayout.Label("Token do người tổ chức cung cấp (nếu phòng bảo mật)"); _token = GUILayout.PasswordField(_token, '*');
            if (GUILayout.Button("Vào phòng · bật camera và micro")) JoinAgoraClassroom(_channel, _token);
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            if (IsInClassroom && GUILayout.Button(_muted ? "Bật micro" : "Tắt micro")) ToggleMicrophone();
            if ((IsInClassroom || _joining) && GUILayout.Button("Rời cuộc gọi")) LeaveClassroom();
            if (GUILayout.Button("Đóng · F8 / Esc")) SetOpen(false);
            GUILayout.EndHorizontal(); GUILayout.EndArea();
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
