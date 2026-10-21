#if (UNITY_WEBGL && !UNITY_EDITOR) || NL_WEBGL_CHECK
// The Agora Unity SDK is native only (Windows/macOS/Android/iOS): its assembly is excluded from WebGL builds
// (AgoraRtcSDK.asmdef excludePlatforms). This stands in for the part of Agora.Rtc that VoiceChatService and
// EduMeetingManager use, so the web build compiles without them changing. Creating the engine throws, and their
// existing error handling tells the player that voice / the video classroom need the desktop version.
// NL_WEBGL_CHECK: only to compile this file in the editor (with the AgoraRtcSDK reference removed) as a check.
using System;
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

    internal static class RtcEngine
    {
        public static IRtcEngine CreateAgoraRtcEngine() =>
            throw new NotSupportedException(NihongoLife.Core.Loc.T(
                "Voice và lớp học video chỉ có trên bản PC/Mac, bản web chưa hỗ trợ.",
                "Voice and the video classroom are in the desktop version; the web version does not support them yet.",
                "ボイスとビデオ教室はPC/Mac版のみです（Web版は未対応）。"));
    }
}
#endif
