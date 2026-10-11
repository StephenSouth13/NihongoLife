using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Agora.Rtc;
using NihongoLife.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace NihongoLife.Learning
{
    /// <summary>
    /// Voice between players over Agora, built not to turn a crowded street into noise:
    /// · <b>Khu vực</b>: one voice room per world shard (≤ 40 people) with spatial audio — you hear people near you,
    ///   louder the closer they are, nobody beyond <see cref="HearingRange"/> m, and at most <see cref="MaxAudible"/>
    ///   voices at once (the nearest).
    /// · <b>Nhóm</b> / <b>Gọi riêng</b>: a room for a chat group, or for two people.
    /// · <b>Push-to-talk</b> (hold T) by default: everyone listens as audience and only becomes a speaker while the key
    ///   is held, so open microphones (fans, TV, keyboards) never reach the room. AI noise suppression is on.
    ///
    /// Tokens come from the "agora-voice" Edge Function (the App Certificate stays on the server). The Agora engine is
    /// shared with the Speaking Lounge (EduMeetingManager): voice steps aside while the lounge panel is open.
    /// </summary>
    public sealed class VoiceChatService : MonoBehaviour
    {
        public enum Mode { Off, Area, Group, Call }

        public const float HearingRange = 18f;
        public const int MaxAudible = 6;
        private const float PushToTalkTail = 0.35f;

        public static VoiceChatService Instance { get; private set; }

        public Mode Current { get; private set; } = Mode.Off;
        public string Channel { get; private set; }
        public string Status { get; private set; } = "Voice đang tắt.";
        public bool Joined { get; private set; }
        public bool Talking { get; private set; }
        public bool OpenMic { get; set; }
        public bool Busy { get; private set; }
        public IReadOnlyCollection<uint> Speaking => _speaking;

        private IRtcEngine _engine;
        private ILocalSpatialAudioEngine _spatial;
        private string _token;
        private uint _uid;
        private float _releaseAt = -1f;
        private readonly HashSet<uint> _speaking = new HashSet<uint>();
        private readonly HashSet<uint> _remotes = new HashSet<uint>();
        private float _nextPositions, _nextAreaCheck;

        public event Action OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        // ──────────────────────── Public API ────────────────────────

        public void JoinArea()
        {
            if (!(GameServices.TryGet(out IOnlineWorldService w) && w is SupabaseOnlineWorldService online) || online.ShardTopic == null)
            {
                Fail("Voice khu vực cần kết nối online và đang ở nơi công cộng.");
                return;
            }
            Join(Mode.Area, "nlv_" + Sanitize(online.ShardTopic.Substring(2)), null);
        }

        public void JoinGroup(string groupId) => Join(Mode.Group, "nlg_" + Sanitize(groupId), groupId);

        /// <summary>Private call: both people join the same room, named from both ids.</summary>
        public void Call(string myId, string peerId)
        {
            string a = string.CompareOrdinal(myId, peerId) < 0 ? myId : peerId;
            string b = a == myId ? peerId : myId;
            Join(Mode.Call, "nld_" + AgoraUid(a + "|" + b).ToString("x8") + AgoraUid(b + "|" + a).ToString("x8"), peerId);
        }

        public void Leave()
        {
            if (_engine != null && Joined) _engine.LeaveChannel();
            Joined = false;
            Talking = false;
            Current = Mode.Off;
            Channel = null;
            _speaking.Clear();
            _remotes.Clear();
            SetStatus("Voice đang tắt.");
        }

        public static uint AgoraUid(string userId)
        {
            // Same FNV-1a as the Edge Functions, so a player id maps to the uid we hear them on.
            uint hash = 2166136261;
            foreach (char c in userId) hash = (hash ^ c) * 16777619;
            return hash == 0 ? 1u : hash;
        }

        // ──────────────────────── Join ────────────────────────

        private void Join(Mode mode, string channel, string context)
        {
            if (Busy) return;
            if (Channel == channel && Joined) return;
            if (NihongoLife.Learning.EduMeetingManager.Instance != null && NihongoLife.Learning.EduMeetingManager.Instance.IsOpen)
            {
                Fail("Đang mở phòng Speaking — đóng phòng đó để dùng voice.");
                return;
            }
            if (!GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated)
            {
                Fail("Voice cần đăng nhập (tài khoản hoặc khách ẩn danh) để máy chủ cấp quyền vào phòng.");
                return;
            }
            Leave();
            StartCoroutine(JoinRoutine(mode, channel, auth));
        }

        private IEnumerator JoinRoutine(Mode mode, string channel, IAuthService auth)
        {
            Busy = true;
            SetStatus("Đang xin quyền micro…");
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Busy = false;
                Fail("Chưa được cấp quyền micro. Bật micro cho game trong cài đặt hệ điều hành.");
                yield break;
            }

            SetStatus("Đang lấy vé vào phòng voice…");
            string token = null, error = null;
            uint uid = 0;
            yield return FetchToken(channel, auth, (t, u, e) => { token = t; uid = u; error = e; });
            if (token == null)
            {
                Busy = false;
                Fail(error ?? "Không lấy được vé vào phòng voice.");
                yield break;
            }

            try
            {
                EnsureEngine();
                bool spatial = mode == Mode.Area;
                var options = new ChannelMediaOptions();
                options.channelProfile.SetValue(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING);
                // Everyone starts as audience: nobody's microphone is in the room until they press T.
                options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_AUDIENCE);
                options.publishMicrophoneTrack.SetValue(false);
                options.publishCameraTrack.SetValue(false);
                options.autoSubscribeAudio.SetValue(true);
                options.autoSubscribeVideo.SetValue(false);
                if (_spatial != null)
                {
                    _spatial.ClearRemotePositions();
                    _spatial.SetAudioRecvRange(spatial ? HearingRange : 10000f);
                    _spatial.SetMaxAudioRecvCount(spatial ? MaxAudible : 16);
                }
                _engine.EnableSpatialAudio(spatial);
                int code = _engine.JoinChannel(token, channel, uid, options);
                if (code != 0) throw new InvalidOperationException("Vào phòng voice thất bại (" + code + ").");
                _token = token;
                _uid = uid;
                Channel = channel;
                Current = mode;
                SetStatus("Đang kết nối voice…");
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
                Current = Mode.Off;
                ReleaseEngine();
            }
            Busy = false;
        }

        private IEnumerator FetchToken(string channel, IAuthService auth, Action<string, uint, string> done)
        {
            var client = FindFirstObjectByType<SupabaseClient>();
            if (client == null || !client.IsConfigured) { done(null, 0, "Chưa cấu hình Supabase."); yield break; }
            string url = client.ProjectUrl.TrimEnd('/') + "/functions/v1/agora-voice";
            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{\"channel\":" + MiniJson.Quote(channel) + "}"));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", client.AnonKey);
            request.SetRequestHeader("Authorization", "Bearer " + auth.AccessToken);
            request.timeout = 12;
            yield return request.SendWebRequest();
            var body = MiniJson.ParseObject(request.downloadHandler?.text);
            if (request.responseCode == 404)
            {
                done(null, 0, "Máy chủ chưa triển khai hàm agora-voice (supabase functions deploy agora-voice).");
                yield break;
            }
            if (request.result != UnityWebRequest.Result.Success || body == null || !body.Bool("ok"))
            {
                done(null, 0, body.Str("error") ?? $"Máy chủ voice trả lỗi {request.responseCode}.");
                yield break;
            }
            done(body.Str("token"), (uint)body.Num("uid"), null);
        }

        private void EnsureEngine()
        {
            if (_engine != null) return;
            string appId = EduMeetingManager.ConfiguredAppId();
            if (string.IsNullOrWhiteSpace(appId)) throw new InvalidOperationException("Chưa cấu hình Agora App ID.");
            _engine = RtcEngine.CreateAgoraRtcEngine();
            int code = _engine.Initialize(new RtcEngineContext { appId = appId, channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING, audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_CHATROOM });
            if (code != 0) { _engine = null; throw new InvalidOperationException("Khởi tạo Agora thất bại (" + code + ")."); }
            _engine.InitEventHandler(new Events(this));
            _engine.EnableAudio();
            _engine.DisableVideo();
            _engine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_SPEECH_STANDARD, AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_CHATROOM);
            _engine.SetAINSMode(true, AUDIO_AINS_MODE.AINS_MODE_BALANCED); // fans, keyboards, street noise
            _engine.EnableAudioVolumeIndication(250, 3, true);
            _spatial = _engine.GetLocalSpatialAudioEngine();
            if (_spatial != null && _spatial.Initialize() != 0) _spatial = null;
            _spatial?.SetDistanceUnit(1f);
        }

        private void ReleaseEngine()
        {
            if (_engine == null) return;
            try
            {
                if (Joined) _engine.LeaveChannel();
                _spatial?.Dispose();
                _engine.InitEventHandler(null);
                _engine.Dispose();
            }
            catch (Exception ex) { Debug.LogWarning("[Voice] Release: " + ex.Message); }
            _engine = null;
            _spatial = null;
            Joined = false;
        }

        // ──────────────────────── Update ────────────────────────

        private void Update()
        {
            // The Speaking Lounge needs the (single) Agora engine: step aside while its panel is open.
            if (_engine != null && EduMeetingManager.Instance != null && EduMeetingManager.Instance.IsOpen)
            {
                Leave();
                ReleaseEngine();
                SetStatus("Voice tạm dừng trong phòng Speaking.");
                return;
            }
            if (!Joined || _engine == null) return;

            // Area voice follows the player: another place (or shard) means another voice room.
            if (Current == Mode.Area && Time.unscaledTime >= _nextAreaCheck)
            {
                _nextAreaCheck = Time.unscaledTime + 2f;
                string shard = GameServices.TryGet(out IOnlineWorldService w) && w is SupabaseOnlineWorldService o ? o.ShardTopic : null;
                if (shard == null) { Leave(); SetStatus("Voice khu vực tắt trong phòng riêng."); return; }
                if ("nlv_" + Sanitize(shard.Substring(2)) != Channel) { JoinArea(); return; }
            }

            // Push-to-talk: speaker only while T is held (plus a short tail so the last word is not cut).
            var input = GameInputService.Instance;
            bool held = OpenMic || (input != null && !NihongoLife.UI.UiModalStack.IsTyping && input.IsPressed(GameInputId.PushToTalk));
            if (held) _releaseAt = Time.unscaledTime + PushToTalkTail;
            bool talk = held || Time.unscaledTime < _releaseAt;
            if (talk != Talking)
            {
                Talking = talk;
                var options = new ChannelMediaOptions();
                options.clientRoleType.SetValue(talk ? CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER : CLIENT_ROLE_TYPE.CLIENT_ROLE_AUDIENCE);
                options.publishMicrophoneTrack.SetValue(talk);
                _engine.UpdateChannelMediaOptions(options);
                OnStateChanged?.Invoke();
            }

            if (Current == Mode.Area && _spatial != null && Time.unscaledTime >= _nextPositions)
            {
                _nextPositions = Time.unscaledTime + 0.1f;
                UpdatePositions();
            }
        }

        private void UpdatePositions()
        {
            var cam = Camera.main;
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;
            Transform ear = cam != null ? cam.transform : player.transform;
            Vector3 p = player.transform.position + Vector3.up * 1.5f;
            _spatial.UpdateSelfPosition(V(p), V(ear.forward), V(ear.right), V(ear.up));
            var manager = FindFirstObjectByType<NihongoLife.Player.RemotePlayerManager>();
            if (manager == null) return;
            foreach (var kv in manager.Avatars)
            {
                if (kv.Value == null) continue;
                uint uid = AgoraUid(kv.Key);
                if (!_remotes.Contains(uid)) continue;
                var t = kv.Value.transform;
                _spatial.UpdateRemotePosition(uid, new RemoteVoicePositionInfo { position = V(t.position + Vector3.up * 1.5f), forward = V(t.forward) });
            }
        }

        // Agora axes: [forward(z), right(x), up(y)].
        private static float[] V(Vector3 v) => new[] { v.z, v.x, v.y };

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.Length > 56 ? sb.ToString(0, 56) : sb.ToString();
        }

        private void SetStatus(string status)
        {
            Status = status;
            OnStateChanged?.Invoke();
        }

        /// <summary>A failure while nothing is joined has no voice bar to show it: tell the player in the feed.</summary>
        private void Fail(string message)
        {
            SetStatus(message);
            NihongoLife.UI.HudFeed.Post(message, NihongoLife.UI.HudFeed.Kind.Warning, 6f, "voice");
        }

        private void OnDestroy()
        {
            ReleaseEngine();
            if (Instance == this) Instance = null;
        }

        // ──────────────────────── Agora events (worker thread → main thread) ────────────────────────

        private readonly Queue<Action> _main = new Queue<Action>();
        private void Post(Action a) { lock (_main) _main.Enqueue(a); }

        private void LateUpdate()
        {
            while (true)
            {
                Action a;
                lock (_main) { if (_main.Count == 0) break; a = _main.Dequeue(); }
                a();
            }
        }

        private sealed class Events : IRtcEngineEventHandler
        {
            private readonly VoiceChatService _owner;
            public Events(VoiceChatService owner) { _owner = owner; }

            public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed) => _owner.Post(() =>
            {
                _owner.Joined = true;
                string key = GameInputService.GetOrCreate().GetBindingLabel(GameInputId.PushToTalk);
                _owner.SetStatus(_owner.Current switch
                {
                    Mode.Area => $"Voice khu vực · giữ {key} để nói · nghe người trong {HearingRange:0} m",
                    Mode.Group => $"Voice nhóm · giữ {key} để nói",
                    _ => $"Đang gọi · giữ {key} để nói"
                });
            });

            public override void OnUserJoined(RtcConnection connection, uint remoteUid, int elapsed) => _owner.Post(() => _owner._remotes.Add(remoteUid));
            public override void OnUserOffline(RtcConnection connection, uint remoteUid, USER_OFFLINE_REASON_TYPE reason) => _owner.Post(() =>
            {
                _owner._remotes.Remove(remoteUid);
                _owner._speaking.Remove(remoteUid);
                _owner._spatial?.RemoveRemotePosition(remoteUid);
            });

            public override void OnAudioVolumeIndication(RtcConnection connection, AudioVolumeInfo[] speakers, uint speakerNumber, int totalVolume) => _owner.Post(() =>
            {
                _owner._speaking.Clear();
                if (speakers != null) foreach (var s in speakers) if (s.uid != 0 && s.volume > 12) _owner._speaking.Add(s.uid);
                _owner.OnStateChanged?.Invoke();
            });

            public override void OnError(int err, string msg) => _owner.Post(() => _owner.SetStatus($"Voice lỗi {err}: {msg}"));

            public override void OnTokenPrivilegeWillExpire(RtcConnection connection, string token) => _owner.Post(() =>
            {
                if (!GameServices.TryGet(out IAuthService auth) || _owner.Channel == null) return;
                _owner.StartCoroutine(_owner.FetchToken(_owner.Channel, auth, (t, u, e) => { if (t != null) _owner._engine?.RenewToken(t); }));
            });
        }
    }
}
