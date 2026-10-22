#if (UNITY_WEBGL && !UNITY_EDITOR) || NL_WEBGL_CHECK
using System;
using System.Globalization;
using Agora.Rtc;
using UnityEngine;

namespace NihongoLife.Learning
{
    /// <summary>Receives Agora Web SDK callbacks (SendMessage, main thread) and forwards them to the active engine handler.</summary>
    public sealed class AgoraWebEvents : MonoBehaviour
    {
        static AgoraWebEvents _host;
        AgoraWebRtcEngine _engine;

        internal static void Ensure(AgoraWebRtcEngine engine)
        {
            if (_host == null)
            {
                var go = new GameObject("NihongoAgoraWeb");
                DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                _host = go.AddComponent<AgoraWebEvents>();
                AgoraWebNative.NL_AgoraSetTarget(go.name);
            }
            _host._engine = engine;
        }

        public void OnAgoraJoinOk(string payload) => With(h =>
        {
            Split(payload, out string channel, out string rest);
            uint.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uid);
            h.OnJoinChannelSuccess(new RtcConnection { channelId = channel, localUid = uid }, 0);
        });

        public void OnAgoraUserJoined(string uidText) => With(h =>
        {
            if (uint.TryParse(uidText, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uid))
                h.OnUserJoined(new RtcConnection(), uid, 0);
        });

        public void OnAgoraUserOffline(string uidText) => With(h =>
        {
            if (uint.TryParse(uidText, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uid))
                h.OnUserOffline(new RtcConnection(), uid, USER_OFFLINE_REASON_TYPE.USER_OFFLINE_QUIT);
        });

        public void OnAgoraRemoteVideo(string payload) => With(h =>
        {
            Split(payload, out string uidText, out string stateText);
            if (!uint.TryParse(uidText, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uid)) return;
            int.TryParse(stateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int state);
            var video = state >= 2 ? REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_DECODING
                : state == 1 ? REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_STARTING
                : REMOTE_VIDEO_STATE.REMOTE_VIDEO_STATE_STOPPED;
            h.OnRemoteVideoStateChanged(new RtcConnection(), uid, video, REMOTE_VIDEO_STATE_REASON.REMOTE_VIDEO_STATE_REASON_INTERNAL, 0);
        });

        public void OnAgoraVolumes(string payload) => With(h =>
        {
            if (string.IsNullOrEmpty(payload))
            {
                h.OnAudioVolumeIndication(new RtcConnection(), Array.Empty<AudioVolumeInfo>(), 0, 0);
                return;
            }
            string[] parts = payload.Split(',');
            var speakers = new AudioVolumeInfo[parts.Length];
            uint total = 0;
            int n = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                int colon = parts[i].IndexOf(':');
                if (colon <= 0) continue;
                if (!uint.TryParse(parts[i].Substring(0, colon), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uid)) continue;
                uint.TryParse(parts[i].Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint volume);
                speakers[n++] = new AudioVolumeInfo { uid = uid, volume = volume };
                if (volume > total) total = volume;
            }
            if (n != speakers.Length) Array.Resize(ref speakers, n);
            h.OnAudioVolumeIndication(new RtcConnection(), speakers, (uint)n, (int)total);
        });

        public void OnAgoraToken(string unused) => With(h => h.OnTokenPrivilegeWillExpire(new RtcConnection(), ""));

        public void OnAgoraDevices(string countText)
        {
            if (int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
                _engine?.NoteDevices(count);
        }

        public void OnAgoraError(string payload)
        {
            Split(payload, out string codeText, out string msg);
            int.TryParse(codeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int code);
            string shown = Explain(code, msg);
            With(h => h.OnError(code, shown));
        }

        void With(Action<IRtcEngineEventHandler> call)
        {
            var handler = _engine != null && !_engine.Disposed ? _engine.Handler : null;
            if (handler == null) return;
            try { call(handler); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        static void Split(string payload, out string left, out string right)
        {
            payload ??= "";
            int bar = payload.IndexOf('|');
            if (bar < 0) { left = payload; right = ""; return; }
            left = payload.Substring(0, bar);
            right = payload.Substring(bar + 1);
        }

        static string Explain(int code, string detail)
        {
            switch (code)
            {
                case 1:
                    return NihongoLife.Core.Loc.T(
                        "Không tải được Agora Web SDK. Trình duyệt cần mở được download.agora.io.",
                        "Could not load the Agora Web SDK. The browser must be allowed to reach download.agora.io.",
                        "Agora Web SDKを読み込めません。download.agora.io への接続を許可してください。");
                case 2:
                    return NihongoLife.Core.Loc.T(
                        "Trình duyệt chưa được cấp quyền micro. Hãy cho phép micro cho trang này, rồi thử lại.",
                        "The browser has no microphone permission. Allow the microphone for this site, then try again.",
                        "ブラウザのマイクが許可されていません。このサイトのマイクを許可して、もう一度試してください。");
                case 4:
                    return NihongoLife.Core.Loc.T(
                        "Không tìm thấy camera trên trình duyệt. Bạn vẫn có thể vào lớp chỉ với micro.",
                        "No camera found in this browser. You can still join with the microphone only.",
                        "ブラウザでカメラが見つかりません。マイクだけでも参加できます。");
                case 5:
                    return NihongoLife.Core.Loc.T(
                        "Agora từ chối vé vào phòng. Kiểm tra App ID, token, và trên Agora Console bật App Certificate đúng với hàm cấp token.",
                        "Agora rejected the room ticket. Check the App ID, the token, and that the Agora Console certificate matches the token function.",
                        "Agoraがチケットを拒否しました。App ID・トークン・コンソールの証明書を確認してください。");
                default:
                    return string.IsNullOrWhiteSpace(detail) ? "Agora " + code : detail;
            }
        }
    }
}
#endif
