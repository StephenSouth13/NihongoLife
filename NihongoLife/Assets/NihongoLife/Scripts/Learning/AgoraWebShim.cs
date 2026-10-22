#if (UNITY_WEBGL && !UNITY_EDITOR) || NL_WEBGL_CHECK
// WebGL stand-in for the native Agora Unity SDK (excluded from WebGL via AgoraRtcSDK.asmdef).
// VoiceChatService and EduMeetingManager keep calling Agora.Rtc. The browser engine below talks to
// NihongoAgora.jslib, which loads Agora's Web SDK the first time a call or voice room is joined.
// NL_WEBGL_CHECK compiles this file in the editor only when the native Agora reference is removed.
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Agora.Rtc
{
    internal enum CHANNEL_PROFILE_TYPE { CHANNEL_PROFILE_COMMUNICATION = 0, CHANNEL_PROFILE_LIVE_BROADCASTING = 1 }
    internal enum CLIENT_ROLE_TYPE { CLIENT_ROLE_BROADCASTER = 1, CLIENT_ROLE_AUDIENCE = 2 }
    internal enum AUDIO_SCENARIO_TYPE { AUDIO_SCENARIO_DEFAULT = 0, AUDIO_SCENARIO_CHATROOM = 5 }
    internal enum AUDIO_PROFILE_TYPE { AUDIO_PROFILE_DEFAULT = 0, AUDIO_PROFILE_SPEECH_STANDARD = 1 }
    internal enum AUDIO_AINS_MODE { AINS_MODE_BALANCED = 0 }
    internal enum VIDEO_SOURCE_TYPE { VIDEO_SOURCE_CAMERA_PRIMARY = 0, VIDEO_SOURCE_REMOTE = 9 }
    internal enum REMOTE_VIDEO_STATE { REMOTE_VIDEO_STATE_STOPPED = 0, REMOTE_VIDEO_STATE_STARTING = 1, REMOTE_VIDEO_STATE_DECODING = 2 }
    internal enum REMOTE_VIDEO_STATE_REASON { REMOTE_VIDEO_STATE_REASON_INTERNAL = 0 }
    internal enum USER_OFFLINE_REASON_TYPE { USER_OFFLINE_QUIT = 0 }

    internal sealed class Optional<T>
    {
        public T Value { get; private set; }
        public bool HasValue { get; private set; }
        public void SetValue(T value) { Value = value; HasValue = true; }
    }

    internal sealed class ChannelMediaOptions
    {
        public Optional<CHANNEL_PROFILE_TYPE> channelProfile = new Optional<CHANNEL_PROFILE_TYPE>();
        public Optional<CLIENT_ROLE_TYPE> clientRoleType = new Optional<CLIENT_ROLE_TYPE>();
        public Optional<bool> publishMicrophoneTrack = new Optional<bool>();
        public Optional<bool> publishCameraTrack = new Optional<bool>();
        public Optional<bool> autoSubscribeAudio = new Optional<bool>();
        public Optional<bool> autoSubscribeVideo = new Optional<bool>();
    }

    internal sealed class RtcEngineContext
    {
        public string appId;
        public CHANNEL_PROFILE_TYPE channelProfile;
        public AUDIO_SCENARIO_TYPE audioScenario;
    }

    internal sealed class RtcConnection { public string channelId = ""; public uint localUid; }
    internal sealed class AudioVolumeInfo { public uint uid; public uint volume; }
    internal sealed class RemoteVoicePositionInfo { public float[] position; public float[] forward; }
    internal sealed class DeviceInfo { public string deviceName = ""; public string deviceId = ""; }

    internal abstract class IRtcEngineEventHandler
    {
        public virtual void OnJoinChannelSuccess(RtcConnection connection, int elapsed) { }
        public virtual void OnUserJoined(RtcConnection connection, uint remoteUid, int elapsed) { }
        public virtual void OnUserOffline(RtcConnection connection, uint remoteUid, USER_OFFLINE_REASON_TYPE reason) { }
        public virtual void OnAudioVolumeIndication(RtcConnection connection, AudioVolumeInfo[] speakers, uint speakerNumber, int totalVolume) { }
        public virtual void OnRemoteVideoStateChanged(RtcConnection connection, uint remoteUid, REMOTE_VIDEO_STATE state, REMOTE_VIDEO_STATE_REASON reason, int elapsed) { }
        public virtual void OnError(int err, string msg) { }
        public virtual void OnTokenPrivilegeWillExpire(RtcConnection connection, string token) { }
    }

    internal interface IVideoDeviceManager { DeviceInfo[] EnumerateVideoDevices(); }

    internal interface ILocalSpatialAudioEngine : IDisposable
    {
        int Initialize();
        int ClearRemotePositions();
        int SetAudioRecvRange(float range);
        int SetMaxAudioRecvCount(int maxCount);
        int SetDistanceUnit(float unit);
        int UpdateSelfPosition(float[] position, float[] axisForward, float[] axisRight, float[] axisUp);
        int UpdateRemotePosition(uint uid, RemoteVoicePositionInfo posInfo);
        int RemoveRemotePosition(uint uid);
    }

    internal interface IRtcEngine : IDisposable
    {
        int Initialize(RtcEngineContext context);
        int InitEventHandler(IRtcEngineEventHandler handler);
        int EnableAudio();
        int EnableVideo();
        int DisableVideo();
        int SetAudioProfile(AUDIO_PROFILE_TYPE profile);
        int SetAudioProfile(AUDIO_PROFILE_TYPE profile, AUDIO_SCENARIO_TYPE scenario);
        int SetAINSMode(bool enabled, AUDIO_AINS_MODE mode);
        int EnableAudioVolumeIndication(int interval, int smooth, bool reportVad);
        int EnableSpatialAudio(bool enabled);
        ILocalSpatialAudioEngine GetLocalSpatialAudioEngine();
        IVideoDeviceManager GetVideoDeviceManager();
        int JoinChannel(string token, string channelId, uint uid, ChannelMediaOptions options);
        int UpdateChannelMediaOptions(ChannelMediaOptions options);
        int LeaveChannel();
        int RenewToken(string token);
        int StartPreview();
        int StopPreview();
        int MuteLocalAudioStream(bool mute);
        int MuteLocalVideoStream(bool mute);
        int SwitchCamera();
    }

    internal static class AgoraWebNative
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] internal static extern void NL_AgoraSetTarget(string gameObjectName);
        [DllImport("__Internal")] internal static extern int NL_AgoraInitialize(string appId);
        [DllImport("__Internal")] internal static extern int NL_AgoraJoin(string token, string channel, string uid, int profile, int role, int pubMic, int pubCam, int subAudio, int subVideo);
        [DllImport("__Internal")] internal static extern int NL_AgoraUpdateOptions(int role, int pubMic, int pubCam);
        [DllImport("__Internal")] internal static extern void NL_AgoraLeave();
        [DllImport("__Internal")] internal static extern int NL_AgoraRenewToken(string token);
        [DllImport("__Internal")] internal static extern int NL_AgoraStartPreview();
        [DllImport("__Internal")] internal static extern void NL_AgoraStopPreview();
        [DllImport("__Internal")] internal static extern void NL_AgoraMuteLocalAudio(int mute);
        [DllImport("__Internal")] internal static extern void NL_AgoraMuteLocalVideo(int mute);
        [DllImport("__Internal")] internal static extern void NL_AgoraSwitchCamera();
        [DllImport("__Internal")] internal static extern void NL_AgoraEnableSpatial(int enabled);
        [DllImport("__Internal")] internal static extern void NL_AgoraSetSpatialLimits(float range, int maxCount);
        [DllImport("__Internal")] internal static extern void NL_AgoraClearRemotes();
        [DllImport("__Internal")] internal static extern void NL_AgoraRemoveRemote(string uid);
        [DllImport("__Internal")] internal static extern void NL_AgoraUpdateSelf(float x, float y, float z);
        [DllImport("__Internal")] internal static extern void NL_AgoraUpdateRemote(string uid, float x, float y, float z);
        [DllImport("__Internal")] internal static extern void NL_AgoraSetVideoRect(string id, float x, float y, float w, float h, float screenW, float screenH);
        [DllImport("__Internal")] internal static extern void NL_AgoraSetVideoVisible(string id, int visible);
        [DllImport("__Internal")] internal static extern void NL_AgoraDispose();
#else
        internal static void NL_AgoraSetTarget(string gameObjectName) { }
        internal static int NL_AgoraInitialize(string appId) => 0;
        internal static int NL_AgoraJoin(string token, string channel, string uid, int profile, int role, int pubMic, int pubCam, int subAudio, int subVideo) => 0;
        internal static int NL_AgoraUpdateOptions(int role, int pubMic, int pubCam) => 0;
        internal static void NL_AgoraLeave() { }
        internal static int NL_AgoraRenewToken(string token) => 0;
        internal static int NL_AgoraStartPreview() => 0;
        internal static void NL_AgoraStopPreview() { }
        internal static void NL_AgoraMuteLocalAudio(int mute) { }
        internal static void NL_AgoraMuteLocalVideo(int mute) { }
        internal static void NL_AgoraSwitchCamera() { }
        internal static void NL_AgoraEnableSpatial(int enabled) { }
        internal static void NL_AgoraSetSpatialLimits(float range, int maxCount) { }
        internal static void NL_AgoraClearRemotes() { }
        internal static void NL_AgoraRemoveRemote(string uid) { }
        internal static void NL_AgoraUpdateSelf(float x, float y, float z) { }
        internal static void NL_AgoraUpdateRemote(string uid, float x, float y, float z) { }
        internal static void NL_AgoraSetVideoRect(string id, float x, float y, float w, float h, float screenW, float screenH) { }
        internal static void NL_AgoraSetVideoVisible(string id, int visible) { }
        internal static void NL_AgoraDispose() { }
#endif
    }

    internal static class RtcEngine
    {
        static AgoraWebRtcEngine _instance;

        public static IRtcEngine CreateAgoraRtcEngine()
        {
            if (_instance == null || _instance.Disposed) _instance = new AgoraWebRtcEngine();
            return _instance;
        }

        internal static void Clear(AgoraWebRtcEngine engine)
        {
            if (_instance == engine) _instance = null;
        }
    }

    internal sealed class AgoraWebRtcEngine : IRtcEngine, IVideoDeviceManager
    {
        public bool Disposed { get; private set; }
        IRtcEngineEventHandler _handler;
        CHANNEL_PROFILE_TYPE _profile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING;
        bool _video = true;
        int _deviceCount = 1;
        readonly Spatial _spatial;

        public AgoraWebRtcEngine()
        {
            _spatial = new Spatial();
            AgoraWebEvents.Ensure(this);
        }

        internal void SetHandler(IRtcEngineEventHandler handler) => _handler = handler;
        internal IRtcEngineEventHandler Handler => _handler;

        internal void NoteDevices(int count) => _deviceCount = count;

        public int Initialize(RtcEngineContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.appId)) return -2;
            _profile = context.channelProfile;
            AgoraWebEvents.Ensure(this);
            return AgoraWebNative.NL_AgoraInitialize(context.appId);
        }

        public int InitEventHandler(IRtcEngineEventHandler handler)
        {
            _handler = handler;
            return 0;
        }

        public int EnableAudio() => 0;
        public int EnableVideo() { _video = true; return 0; }
        public int DisableVideo() { _video = false; return 0; }
        public int SetAudioProfile(AUDIO_PROFILE_TYPE profile) => 0;
        public int SetAudioProfile(AUDIO_PROFILE_TYPE profile, AUDIO_SCENARIO_TYPE scenario) => SetAudioProfile(profile);
        public int SetAINSMode(bool enabled, AUDIO_AINS_MODE mode) => 0;
        public int EnableAudioVolumeIndication(int interval, int smooth, bool reportVad) => 0;
        public int EnableSpatialAudio(bool enabled)
        {
            AgoraWebNative.NL_AgoraEnableSpatial(enabled ? 1 : 0);
            return 0;
        }

        public ILocalSpatialAudioEngine GetLocalSpatialAudioEngine() => _spatial;
        public IVideoDeviceManager GetVideoDeviceManager() => this;
        public DeviceInfo[] EnumerateVideoDevices()
        {
            if (_deviceCount <= 0) return Array.Empty<DeviceInfo>();
            var list = new DeviceInfo[Mathf.Max(1, _deviceCount)];
            for (int i = 0; i < list.Length; i++) list[i] = new DeviceInfo { deviceName = "browser-" + i, deviceId = i.ToString(CultureInfo.InvariantCulture) };
            return list;
        }

        public int JoinChannel(string token, string channelId, uint uid, ChannelMediaOptions options)
        {
            int profile = options != null && options.channelProfile.HasValue ? (int)options.channelProfile.Value : (int)_profile;
            int role = options != null && options.clientRoleType.HasValue && options.clientRoleType.Value == CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER ? 1 : 2;
            int pubMic = options != null && options.publishMicrophoneTrack.HasValue && options.publishMicrophoneTrack.Value ? 1 : 0;
            int pubCam = _video && options != null && options.publishCameraTrack.HasValue && options.publishCameraTrack.Value ? 1 : 0;
            int subAudio = options == null || !options.autoSubscribeAudio.HasValue || options.autoSubscribeAudio.Value ? 1 : 0;
            int subVideo = _video && options != null && options.autoSubscribeVideo.HasValue && options.autoSubscribeVideo.Value ? 1 : 0;
            return AgoraWebNative.NL_AgoraJoin(token ?? "", channelId ?? "", uid.ToString(CultureInfo.InvariantCulture), profile, role, pubMic, pubCam, subAudio, subVideo);
        }

        public int UpdateChannelMediaOptions(ChannelMediaOptions options)
        {
            int role = options != null && options.clientRoleType.HasValue && options.clientRoleType.Value == CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER ? 1 : 2;
            int pubMic = options != null && options.publishMicrophoneTrack.HasValue && options.publishMicrophoneTrack.Value ? 1 : 0;
            int pubCam = options != null && options.publishCameraTrack.HasValue && options.publishCameraTrack.Value ? 1 : 0;
            return AgoraWebNative.NL_AgoraUpdateOptions(role, pubMic, pubCam);
        }

        public int LeaveChannel() { AgoraWebNative.NL_AgoraLeave(); return 0; }
        public int RenewToken(string token) => AgoraWebNative.NL_AgoraRenewToken(token ?? "");
        public int StartPreview() => AgoraWebNative.NL_AgoraStartPreview();
        public int StopPreview() { AgoraWebNative.NL_AgoraStopPreview(); return 0; }
        public int MuteLocalAudioStream(bool mute) { AgoraWebNative.NL_AgoraMuteLocalAudio(mute ? 1 : 0); return 0; }
        public int MuteLocalVideoStream(bool mute) { AgoraWebNative.NL_AgoraMuteLocalVideo(mute ? 1 : 0); return 0; }
        public int SwitchCamera() { AgoraWebNative.NL_AgoraSwitchCamera(); return 0; }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            AgoraWebNative.NL_AgoraDispose();
            _handler = null;
            RtcEngine.Clear(this);
        }

        sealed class Spatial : ILocalSpatialAudioEngine
        {
            float _range = 18f;
            int _max = 6;

            public int Initialize() => 0;
            public int ClearRemotePositions() { AgoraWebNative.NL_AgoraClearRemotes(); return 0; }
            public int SetAudioRecvRange(float range)
            {
                _range = range;
                AgoraWebNative.NL_AgoraSetSpatialLimits(_range, _max);
                return 0;
            }
            public int SetMaxAudioRecvCount(int maxCount)
            {
                _max = maxCount;
                AgoraWebNative.NL_AgoraSetSpatialLimits(_range, _max);
                return 0;
            }
            public int SetDistanceUnit(float unit) => 0;
            public int UpdateSelfPosition(float[] position, float[] axisForward, float[] axisRight, float[] axisUp)
            {
                if (position == null || position.Length < 3) return -1;
                AgoraWebNative.NL_AgoraUpdateSelf(position[0], position[1], position[2]);
                return 0;
            }
            public int UpdateRemotePosition(uint uid, RemoteVoicePositionInfo posInfo)
            {
                if (posInfo?.position == null || posInfo.position.Length < 3) return -1;
                AgoraWebNative.NL_AgoraUpdateRemote(uid.ToString(CultureInfo.InvariantCulture), posInfo.position[0], posInfo.position[1], posInfo.position[2]);
                return 0;
            }
            public int RemoveRemotePosition(uint uid)
            {
                AgoraWebNative.NL_AgoraRemoveRemote(uid.ToString(CultureInfo.InvariantCulture));
                return 0;
            }
            public void Dispose() { }
        }
    }
}
#endif
