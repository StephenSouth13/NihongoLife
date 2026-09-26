using System.Collections;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Player;

#if AGORA_SDK_INSTALLED
using Agora.Rtc;
#endif

namespace NihongoLife.Learning
{
    /// <summary>
    /// Handles connecting the Metaverse to real-world Video calling platforms for 1-on-1 tutoring.
    /// Supports opening Google Meet/Zoom in a browser, or provides hooks for Agora Video SDK (Native 3D Video).
    /// </summary>
    public class EduMeetingManager : MonoBehaviour
    {
        public static EduMeetingManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Opens a scheduled Google Meet/Zoom link directly.
        /// Useful for quick integration without needing the Agora SDK size overhead.
        /// </summary>
        public void JoinExternalMeeting(string meetingUrl)
        {
            if (string.IsNullOrEmpty(meetingUrl))
            {
                Debug.LogWarning("[EduMeeting] No meeting URL provided.");
                return;
            }

            Debug.Log($"[EduMeeting] Opening Virtual Classroom at: {meetingUrl}");
            Application.OpenURL(meetingUrl);

            // Optional: Give the player XP for attending a live class
            if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repo))
            {
                var progress = repo.GetProgress();
                progress.knowledge += 50;
                progress.xp += 100;
                repo.SaveProgress(progress);
                Debug.Log("[EduMeeting] Awarded 50 Knowledge for attending external live class.");
            }
        }

        /// <summary>
        /// Stub for AGORA Video SDK.
        /// To make video appear ON A 3D SCREEN inside the game, you need to import the 'Agora Video SDK for Unity'.
        /// Once imported, you would initialize IRtcEngine here and map the teacher's video frame to a Unity Texture/Material.
        /// </summary>
        public void JoinAgoraClassroom(string channelName, string token = "")
        {
            /* 
             * AGORA INTEGRATION (Đã viết hoàn chỉnh):
             * HƯỚNG DẪN BẬT TÍNH NĂNG:
             * 1. Import "Agora Video SDK for Unity" từ Asset Store.
             * 2. Vào Edit > Project Settings > Player > Other Settings.
             * 3. Ở mục Scripting Define Symbols, thêm vào chữ: AGORA_SDK_INSTALLED
             */

#if AGORA_SDK_INSTALLED
            string encodedAppId = "NDRmN2Q1ZGJmNzA1NGI4Nzg3ZGE5MzEzYmFkNDMwNWE=";
            string encodedCert = "NzgxZjY5YzJiMmJhNDc4ZDk2MmUzYTQyODkyZGUzMzY="; 
            string appId = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(encodedAppId));
            
            if (_rtcEngine == null)
            {
                _rtcEngine = IRtcEngine.GetEngine(appId);
                _rtcEngine.EnableVideo();
                _rtcEngine.EnableAudio();
                _rtcEngine.EnableVideoObserver();
                
                // Đăng ký sự kiện
                _rtcEngine.OnUserJoined = OnTeacherJoined;
                _rtcEngine.OnUserOffline = OnTeacherOffline;
                _rtcEngine.OnJoinChannelSuccess = OnLocalUserJoined;
            }

            Debug.Log($"[EduMeeting] Joining Native 3D Channel: {channelName}");
            _rtcEngine.JoinChannelByKey(token, channelName, "", 0);
#else
            Debug.LogWarning("[EduMeeting] Chưa bật AGORA_SDK_INSTALLED trong Project Settings! Code Native Video tạm ẩn.");
#endif
        }

#if AGORA_SDK_INSTALLED
        private IRtcEngine _rtcEngine;
        private bool _isMuted = false;

        private void OnLocalUserJoined(string channelName, uint uid, int elapsed)
        {
            Debug.Log($"[EduMeeting] Mình đã vào lớp học (UID: {uid})");
            // Hiển thị camera của bản thân lên một góc màn hình (tùy chọn)
            // Cần gán script VideoSurface vào 1 UI RawImage
        }

        private void OnTeacherJoined(uint uid, int elapsed)
        {
            Debug.Log($"[EduMeeting] Giáo viên / Đối tác đã vào (UID: {uid})");
            
            // Tìm màn hình Tivi 3D trong game (phải được setup tag hoặc tìm bằng tên)
            GameObject tvScreen = GameObject.Find("TeacherScreen3D");
            if (tvScreen != null)
            {
                VideoSurface videoSurface = tvScreen.GetComponent<VideoSurface>();
                if (videoSurface == null) videoSurface = tvScreen.AddComponent<VideoSurface>();
                
                videoSurface.SetForUser(uid);
                videoSurface.SetEnable(true);
            }
            else
            {
                Debug.LogWarning("[EduMeeting] Không tìm thấy vật thể 'TeacherScreen3D' trong cảnh để phát video giáo viên!");
            }
        }

        private void OnTeacherOffline(uint uid, USER_OFFLINE_REASON reason)
        {
            Debug.Log($"[EduMeeting] Giáo viên đã ngắt kết nối.");
            GameObject tvScreen = GameObject.Find("TeacherScreen3D");
            if (tvScreen != null)
            {
                VideoSurface videoSurface = tvScreen.GetComponent<VideoSurface>();
                if (videoSurface != null) videoSurface.SetEnable(false);
            }
        }

        /// <summary>
        /// Thoát phòng học
        /// </summary>
        public void LeaveClassroom()
        {
            if (_rtcEngine != null)
            {
                _rtcEngine.LeaveChannel();
                Debug.Log("[EduMeeting] Đã rời phòng học Video.");
            }
        }

        /// <summary>
        /// Bật/Tắt Micro
        /// </summary>
        public void ToggleMicrophone()
        {
            if (_rtcEngine != null)
            {
                _isMuted = !_isMuted;
                _rtcEngine.MuteLocalAudioStream(_isMuted);
                Debug.Log($"[EduMeeting] Micro: {(_isMuted ? "TẮT" : "BẬT")}");
            }
        }

        /// <summary>
        /// Xoay camera (đổi giữa camera trước và sau trên Mobile)
        /// </summary>
        public void SwitchCamera()
        {
            if (_rtcEngine != null)
            {
                _rtcEngine.SwitchCamera();
                Debug.Log("[EduMeeting] Đã đổi Camera trước/sau.");
            }
        }

        private void OnDestroy()
        {
            if (_rtcEngine != null)
            {
                IRtcEngine.Destroy();
                _rtcEngine = null;
            }
        }
#endif

        /// <summary>
        /// Chụp ảnh màn hình khoảnh khắc học tập (hoạt động kể cả chưa có Agora)
        /// </summary>
        public void TakeScreenshot()
        {
            string folder = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            
            string filename = $"EduMeeting_Capture_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = System.IO.Path.Combine(folder, filename);
            
            ScreenCapture.CaptureScreenshot(fullPath);
            Debug.Log($"[EduMeeting] 📸 Đã chụp màn hình và lưu tại: {fullPath}");
        }
    }
}
