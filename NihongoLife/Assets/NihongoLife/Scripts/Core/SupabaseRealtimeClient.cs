using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// WebSocket client for Supabase Realtime (Phoenix protocol, vsn 1.0.0). One socket carries any number of
    /// channels (topics): the world shard, the community room, group rooms, a private inbox, the friends presence
    /// room. Each <see cref="Channel"/> has Broadcast (fire-and-forget messages, nothing stored) and Presence (who is
    /// in the room, with a small meta object each).
    ///
    /// · Sends go through one queue — ClientWebSocket allows a single SendAsync at a time.
    /// · Incoming frames are parsed with <see cref="MiniJson"/> on the main thread.
    /// · A dropped socket reconnects with backoff (2 → 30 s) and rejoins every channel, re-tracking presence.
    ///
    /// The single-channel API (Connect / Broadcast / TrackPresence and the On* events) is kept for
    /// CoopSessionService; it is a thin layer over the "legacy" channel.
    /// </summary>
    public class SupabaseRealtimeClient : MonoBehaviour
    {
        // ──────────────────────── Channel ────────────────────────

        public sealed class Channel
        {
            internal readonly SupabaseRealtimeClient Owner;
            public string Topic { get; }
            public string PresenceKey { get; }
            public bool ReceiveOwnBroadcasts { get; }
            public bool Joined { get; internal set; }
            internal string JoinRef;
            internal string TrackJson;
            internal bool Closed;
            internal float RejoinAt = -1f;
            internal readonly List<string> PendingFrames = new List<string>();
            // presence key → metas (each with its phx_ref)
            internal readonly Dictionary<string, List<Dictionary<string, object>>> PresenceMetas = new Dictionary<string, List<Dictionary<string, object>>>();

            /// <summary>(event, payload) — payload is the parsed object (may be null).</summary>
            public event Action<string, Dictionary<string, object>> OnBroadcast;
            /// <summary>(presence key, latest meta) — fired for a new member and when a member re-tracks.</summary>
            public event Action<string, Dictionary<string, object>> OnPresenceJoin;
            /// <summary>(presence key) — the member's last connection left.</summary>
            public event Action<string> OnPresenceLeave;
            /// <summary>The full member list was (re)received after join.</summary>
            public event Action OnPresenceSync;
            public event Action OnJoined;
            /// <summary>The server said this channel sends too much ("Too many messages per second").</summary>
            public event Action OnRateLimited;

            internal Channel(SupabaseRealtimeClient owner, string topic, string presenceKey, bool self)
            {
                Owner = owner;
                Topic = topic;
                PresenceKey = presenceKey ?? string.Empty;
                ReceiveOwnBroadcasts = self;
            }

            public string FullTopic => "realtime:" + Topic;
            public int MemberCount => PresenceMetas.Count;
            public IEnumerable<string> Members => PresenceMetas.Keys;

            public Dictionary<string, object> MetaOf(string key)
            {
                return PresenceMetas.TryGetValue(key, out var metas) && metas.Count > 0 ? metas[metas.Count - 1] : null;
            }

            /// <summary>Broadcasts to everyone in the channel. Payload: a dictionary/list (serialised) or a raw JSON string.</summary>
            public void Send(string eventName, object payload)
            {
                string payloadJson = payload is string raw ? raw : MiniJson.Serialize(payload);
                string inner = "{\"type\":\"broadcast\",\"event\":" + MiniJson.Quote(eventName) + ",\"payload\":" + (string.IsNullOrEmpty(payloadJson) ? "{}" : payloadJson) + "}";
                Owner.SendOnChannel(this, "broadcast", inner);
            }

            /// <summary>Publishes this client's presence meta (kept and re-sent after reconnect).</summary>
            public void Track(object meta)
            {
                TrackJson = meta is string raw ? raw : MiniJson.Serialize(meta);
                if (Joined) Owner.SendOnChannel(this, "presence", "{\"type\":\"presence\",\"event\":\"track\",\"payload\":" + TrackJson + "}");
            }

            public void Untrack()
            {
                TrackJson = null;
                if (Joined) Owner.SendOnChannel(this, "presence", "{\"type\":\"presence\",\"event\":\"untrack\"}");
            }

            public void Leave() => Owner.Leave(this);

            internal void RaiseBroadcast(string evt, Dictionary<string, object> payload) => OnBroadcast?.Invoke(evt, payload);
            internal void RaiseJoin(string key, Dictionary<string, object> meta) => OnPresenceJoin?.Invoke(key, meta);
            internal void RaiseLeave(string key) => OnPresenceLeave?.Invoke(key);
            internal void RaiseSync() => OnPresenceSync?.Invoke();
            internal void RaiseJoined() => OnJoined?.Invoke();
            internal void RaiseRateLimited() => OnRateLimited?.Invoke();
        }

        // ──────────────────────── State ────────────────────────

        /// <summary>Socket open and the send/receive threads running (frames can go out).</summary>
        public bool IsConnected
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return _ready && _webSocketOpen;
#else
                return _ready && _socket != null && _socket.State == WebSocketState.Open;
#endif
            }
        }
        private volatile bool _ready;
        public int ReconnectAttempts { get; private set; }
        public int SentFrames { get; private set; }
        public int ReceivedFrames { get; private set; }
        public IReadOnlyCollection<Channel> Channels => _channels.Values;

        public event Action OnConnected;
        public event Action OnDisconnected;

        // Legacy single-channel events (CoopSessionService).
        public event Action<string, string> OnBroadcastReceived; // (eventType, payloadJson)
        public event Action<string> OnPresenceSync;              // (presenceStateJson)
        public event Action<string, string> OnPresenceJoin;      // (key, metaJson)
        public event Action<string, string> OnPresenceLeave;     // (key, "{}")

        private SupabaseClient _supabaseClient;
        private string _urlOverride, _tokenOverride;
        private ClientWebSocket _socket;
        private CancellationTokenSource _cancellation;
        private readonly Dictionary<string, Channel> _channels = new Dictionary<string, Channel>();
        private Channel _legacy;

        private const float HeartbeatInterval = 25f;
        private float _nextHeartbeat;
        private string _pendingHeartbeatRef;
        private bool _wantConnected, _connecting;
        private float _reconnectAt = -1f;
        private float _reconnectDelay = 2f;
        private volatile bool _socketDropped;
        private volatile string _closeReason;
        private int _messageRef;

        private readonly Queue<string> _incoming = new Queue<string>();
        private readonly object _incomingLock = new object();
        private readonly Queue<byte[]> _outgoing = new Queue<byte[]>();
        private readonly object _outgoingLock = new object();
        private SemaphoreSlim _outgoingSignal;

        private const int ReceiveBufferSize = 16384;

        /// <summary>Logs every connect, join and close (tests, debugging a live session).</summary>
        public static bool Verbose;

        static SupabaseRealtimeClient()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            // Mono's ClientWebSocket goes through ServicePoint, which allows 2 connections per host by default;
            // a websocket holds its connection for life, so a third socket (tests, bots) would wait forever.
            if (System.Net.ServicePointManager.DefaultConnectionLimit < 64) System.Net.ServicePointManager.DefaultConnectionLimit = 64;
            // ConnectAsync (TLS handshake) still runs on the pool; give it room so the first connect is not slow either.
            ThreadPool.GetMinThreads(out int worker, out int io);
            if (worker < 32 || io < 32) ThreadPool.SetMinThreads(Math.Max(worker, 32), Math.Max(io, 32));
#endif
        }
        private const int MaxPendingFramesPerChannel = 64;

        // ──────────────────────── Setup ────────────────────────

        public void Initialize(SupabaseClient client)
        {
            _supabaseClient = client;
        }

        /// <summary>Tests and tools: talk to a realtime endpoint without a SupabaseClient.</summary>
        public void InitializeRaw(string websocketUrl, string accessToken)
        {
            _urlOverride = websocketUrl;
            _tokenOverride = accessToken;
        }

        private bool Configured => !string.IsNullOrEmpty(_urlOverride) || (_supabaseClient != null && _supabaseClient.IsConfigured);
        private string Url => !string.IsNullOrEmpty(_urlOverride) ? _urlOverride : _supabaseClient.RealtimeUrl;
        private string Token => !string.IsNullOrEmpty(_tokenOverride) ? _tokenOverride
            : !string.IsNullOrWhiteSpace(_supabaseClient.AccessToken) ? _supabaseClient.AccessToken : _supabaseClient.AnonKey;

        // ──────────────────────── Multi-channel API ────────────────────────

        /// <summary>
        /// Joins a channel (opening the socket if needed). Joining the same topic twice returns the same channel.
        /// presenceKey identifies this client in the channel's presence (use the player id).
        /// </summary>
        public Channel Join(string topic, string presenceKey = null, bool receiveOwnBroadcasts = false)
        {
            if (string.IsNullOrEmpty(topic)) throw new ArgumentException("topic");
            if (_channels.TryGetValue(topic, out var existing)) return existing;
            var channel = new Channel(this, topic, presenceKey, receiveOwnBroadcasts);
            _channels[topic] = channel;
            EnsureSocket();
            if (IsConnected) SendJoin(channel);
            return channel;
        }

        public Channel Get(string topic) => topic != null && _channels.TryGetValue(topic, out var c) ? c : null;

        public void Leave(Channel channel)
        {
            if (channel == null || channel.Closed) return;
            channel.Closed = true;
            // Also when the join is still pending, so the server does not keep a half-open member.
            if (channel.JoinRef != null && IsConnected) Enqueue(Frame(channel.FullTopic, "phx_leave", "{}", NextRef(), channel.JoinRef));
            channel.Joined = false;
            foreach (var key in new List<string>(channel.PresenceMetas.Keys)) channel.RaiseLeave(key);
            channel.PresenceMetas.Clear();
            _channels.Remove(channel.Topic);
            if (channel == _legacy) _legacy = null;
            if (_channels.Count == 0) CloseSocket(); // nothing left to listen to
        }

        public void LeaveAll()
        {
            foreach (var c in new List<Channel>(_channels.Values)) Leave(c);
        }

        /// <summary>
        /// Broadcasts to a topic without joining it (REST). Used for someone else's private inbox: one HTTP call,
        /// and we never receive the traffic of a channel that is not ours.
        /// </summary>
        public void PostBroadcast(string topic, string eventName, object payload, Action<bool> done = null)
        {
            if (!Configured) { done?.Invoke(false); return; }
            string payloadJson = payload is string raw ? raw : MiniJson.Serialize(payload);
            string body = "{\"messages\":[{\"topic\":" + MiniJson.Quote(topic) + ",\"event\":" + MiniJson.Quote(eventName) +
                          ",\"payload\":" + payloadJson + ",\"private\":false}]}";
            SentFrames++;
            StartCoroutine(PostBroadcastCoroutine(body, done));
        }

        private IEnumerator PostBroadcastCoroutine(string body, Action<bool> done)
        {
            string wsUrl = Url;
            int cut = wsUrl.IndexOf("/realtime/v1/", StringComparison.Ordinal);
            string restUrl = wsUrl.Substring(0, cut).Replace("wss://", "https://").Replace("ws://", "http://") + "/realtime/v1/api/broadcast";
            string apiKey = _supabaseClient != null && _supabaseClient.IsConfigured ? _supabaseClient.AnonKey : QueryValue(wsUrl, "apikey");
            using (var request = new UnityEngine.Networking.UnityWebRequest(restUrl, "POST"))
            {
                request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                request.SetRequestHeader("apikey", apiKey);
                request.SetRequestHeader("Authorization", "Bearer " + Token);
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;
                yield return request.SendWebRequest();
                bool ok = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success;
                if (!ok) Debug.LogWarning($"[SupabaseRealtime] Broadcast POST failed: {request.responseCode} {request.error} {request.downloadHandler?.text}");
                else if (Verbose) Debug.Log($"[SupabaseRealtime] {name}: POST broadcast {request.responseCode} ({body.Length} bytes)");
                done?.Invoke(ok);
            }
        }

        private static string QueryValue(string url, string key)
        {
            int q = url.IndexOf('?');
            if (q < 0) return string.Empty;
            foreach (var part in url.Substring(q + 1).Split('&'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0 && part.Substring(0, eq) == key) return Uri.UnescapeDataString(part.Substring(eq + 1));
            }
            return string.Empty;
        }

        // ──────────────────────── Legacy single-channel API ────────────────────────

        /// <summary>Connects and joins one channel; Broadcast/TrackPresence and the On* events refer to it.</summary>
        public void Connect(string channelTopic)
        {
            if (!Configured)
            {
                Debug.LogWarning("[SupabaseRealtime] SupabaseClient is not configured.");
                return;
            }
            if (_legacy != null && _legacy.Topic == channelTopic) return;
            if (_legacy != null) Leave(_legacy);
            _legacy = Join(channelTopic, string.Empty, true);
            _legacy.OnBroadcast += (evt, payload) => OnBroadcastReceived?.Invoke(evt, MiniJson.Serialize(payload ?? new Dictionary<string, object>()));
            _legacy.OnPresenceJoin += (key, meta) => OnPresenceJoin?.Invoke(key, MiniJson.Serialize(meta));
            _legacy.OnPresenceLeave += key => OnPresenceLeave?.Invoke(key, "{}");
            _legacy.OnPresenceSync += () => OnPresenceSync?.Invoke(MiniJson.Serialize(new List<string>(_legacy.Members)));
        }

        /// <summary>Leaves every channel and closes the socket.</summary>
        public void Disconnect()
        {
            LeaveAll();
            CloseSocket();
        }

        public void Broadcast(string eventType, string payloadJson) => _legacy?.Send(eventType, payloadJson);

        public void TrackPresence(string presencePayloadJson) => _legacy?.Track(presencePayloadJson);

        // ──────────────────────── Socket ────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
        private bool _webSocketOpen;

        [DllImport("__Internal")] private static extern void NihongoRealtimeConnect(string url, string gameObjectName);
        [DllImport("__Internal")] private static extern void NihongoRealtimeSend(string message);
        [DllImport("__Internal")] private static extern void NihongoRealtimeClose();
#endif

        private void EnsureSocket()
        {
            if (!Configured) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            _wantConnected = true;
            if (_webSocketOpen || _connecting || _reconnectAt > 0f) return;
            _connecting = true;
            NihongoRealtimeConnect(Url, gameObject.name);
            return;
#endif
            _wantConnected = true;
            if (IsConnected || _connecting || _reconnectAt > 0f) return;
            _reconnectDelay = 2f;
            StartCoroutine(ConnectCoroutine());
        }

        private void CloseSocket()
        {
            bool wasOpen = IsConnected;
            _ready = false;
            _wantConnected = false;
            _reconnectAt = -1f;
#if UNITY_WEBGL && !UNITY_EDITOR
            NihongoRealtimeClose();
            _webSocketOpen = false;
            _connecting = false;
#endif
            _cancellation?.Cancel();
            var socket = _socket;
            _socket = null;
            if (socket != null && socket.State == WebSocketState.Open)
            {
                try { _ = socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None); }
                catch (Exception ex) { Debug.LogWarning($"[SupabaseRealtime] Close error: {ex.Message}"); }
            }
            foreach (var c in _channels.Values) c.Joined = false;
            lock (_outgoingLock) _outgoing.Clear();
            if (wasOpen) OnDisconnected?.Invoke();
        }

        private IEnumerator ConnectCoroutine()
        {
            _connecting = true;
            _cancellation?.Cancel();
            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            _ready = false;
            var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            _socket = socket;

            if (Verbose) Debug.Log($"[SupabaseRealtime] {name}: connecting ({_channels.Count} channels)");
            float connectStarted = Time.realtimeSinceStartup;
            Task connectTask;
            // Off the main thread: Mono resolves DNS and starts TLS synchronously inside ConnectAsync (a 1–2 s hitch).
            var uri = new Uri(Url);
            connectTask = Task.Run(() => socket.ConnectAsync(uri, cancellation.Token));
            while (!connectTask.IsCompleted) yield return null;
            _connecting = false;

            if (connectTask.IsCanceled || cancellation.IsCancellationRequested || !_wantConnected || _socket != socket) yield break; // closed on purpose
            if (connectTask.IsFaulted || socket.State != WebSocketState.Open)
            {
                Debug.LogWarning($"[SupabaseRealtime] Connection failed: {connectTask.Exception?.GetBaseException()?.Message} — retrying in {_reconnectDelay:0} s");
                _socket = null;
                ScheduleReconnect();
                yield break;
            }

            if (Verbose) Debug.Log($"[SupabaseRealtime] {name}: connected in {Time.realtimeSinceStartup - connectStarted:0.00} s");
            _reconnectDelay = 2f;
            _socketDropped = false;
            _closeReason = null;
            _pendingHeartbeatRef = null;
            _nextHeartbeat = Time.unscaledTime + HeartbeatInterval;
            _outgoingSignal = new SemaphoreSlim(0);
            StartSendLoop(socket, cancellation.Token, _outgoingSignal);
            StartReceiveLoop(socket, cancellation.Token);
            _ready = true;
            OnConnected?.Invoke();

            foreach (var c in _channels.Values) SendJoin(c);
        }

        private void ScheduleReconnect()
        {
            if (!_wantConnected || _channels.Count == 0) return;
            _reconnectAt = Time.unscaledTime + _reconnectDelay;
            _reconnectDelay = Mathf.Min(30f, _reconnectDelay * 2f);
        }

        private void SendJoin(Channel channel)
        {
            channel.Joined = false;
            channel.JoinRef = NextRef();
            string payload = "{\"config\":{\"broadcast\":{\"self\":" + (channel.ReceiveOwnBroadcasts ? "true" : "false") + ",\"ack\":false}," +
                             "\"presence\":{\"key\":" + MiniJson.Quote(channel.PresenceKey) + ",\"enabled\":true},\"postgres_changes\":[],\"private\":false}," +
                             "\"access_token\":" + MiniJson.Quote(Token) + "}";
            Enqueue(Frame(channel.FullTopic, "phx_join", payload, channel.JoinRef, channel.JoinRef));
        }

        internal void SendOnChannel(Channel channel, string eventName, string payloadJson)
        {
            if (channel.Closed) return;
            string frame = Frame(channel.FullTopic, eventName, payloadJson, NextRef(), channel.JoinRef);
            if (channel.Joined && IsConnected) { Enqueue(frame); return; }
            // Joining: hold a few frames and send them once the server confirms (presence is re-sent separately).
            if (eventName == "presence") return;
            if (channel.PendingFrames.Count < MaxPendingFramesPerChannel) channel.PendingFrames.Add(frame);
        }

        // ──────────────────────── Update ────────────────────────

        private readonly List<string> _drain = new List<string>();

        private void Update()
        {
            lock (_incomingLock)
            {
                while (_incoming.Count > 0) _drain.Add(_incoming.Dequeue());
            }
            foreach (var json in _drain) ProcessFrame(json);
            _drain.Clear();

#if UNITY_WEBGL && !UNITY_EDITOR
            if (_wantConnected && !_connecting && _socketDropped)
#else
            if (_wantConnected && !_connecting && _socket != null && (_socketDropped || _socket.State != WebSocketState.Open))
#endif
            {
                _ready = false;
                Debug.LogWarning($"[SupabaseRealtime] {name}: connection lost ({_closeReason ?? _socket.State.ToString()}) — reconnecting.");
                _cancellation?.Cancel();
                _socket = null;
#if UNITY_WEBGL && !UNITY_EDITOR
                _webSocketOpen = false;
#endif
                foreach (var c in _channels.Values) c.Joined = false;
                OnDisconnected?.Invoke();
                ScheduleReconnect();
            }
            if (_reconnectAt > 0f && Time.unscaledTime >= _reconnectAt)
            {
                _reconnectAt = -1f;
                ReconnectAttempts++;
#if UNITY_WEBGL && !UNITY_EDITOR
                _connecting = true;
                NihongoRealtimeConnect(Url, gameObject.name);
#else
                StartCoroutine(ConnectCoroutine());
#endif
            }

            if (IsConnected && Time.unscaledTime >= _nextHeartbeat)
            {
                if (_pendingHeartbeatRef != null)
                {
                    // The previous heartbeat never came back: the link is dead even if the socket says Open.
                    _socketDropped = true;
                    return;
                }
                _nextHeartbeat = Time.unscaledTime + HeartbeatInterval;
                _pendingHeartbeatRef = NextRef();
                Enqueue(Frame("phoenix", "heartbeat", "{}", _pendingHeartbeatRef));
            }

            // Channels the server closed (phx_error / phx_close) come back after a short wait.
            foreach (var c in _channels.Values)
            {
                if (c.RejoinAt > 0f && Time.unscaledTime >= c.RejoinAt && IsConnected)
                {
                    c.RejoinAt = -1f;
                    SendJoin(c);
                }
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Called by the browser WebSocket plugin on Unity's main thread.
        public void OnWebGLRealtimeOpen(string unused)
        {
            _connecting = false;
            _webSocketOpen = true;
            _socketDropped = false;
            _closeReason = null;
            _reconnectDelay = 2f;
            _pendingHeartbeatRef = null;
            _nextHeartbeat = Time.unscaledTime + HeartbeatInterval;
            _ready = true;
            OnConnected?.Invoke();
            foreach (var channel in _channels.Values) SendJoin(channel);
        }

        public void OnWebGLRealtimeMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            lock (_incomingLock) _incoming.Enqueue(message);
        }

        public void OnWebGLRealtimeClose(string reason)
        {
            _connecting = false;
            _closeReason = string.IsNullOrEmpty(reason) ? "browser socket closed" : reason;
            if (_wantConnected) _socketDropped = true;
        }

        public void OnWebGLRealtimeError(string reason)
        {
            _connecting = false;
            _closeReason = string.IsNullOrEmpty(reason) ? "browser socket error" : reason;
            if (_wantConnected) _socketDropped = true;
        }
#endif

        // ──────────────────────── Frames ────────────────────────

        private void ProcessFrame(string json)
        {
            ReceivedFrames++;
            Dictionary<string, object> msg;
            try { msg = MiniJson.Parse(json) as Dictionary<string, object>; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseRealtime] Unreadable frame ({ex.Message}).");
                return;
            }
            if (msg == null) return;

            string topic = msg.Str("topic");
            string evt = msg.Str("event");
            var payload = msg.Obj("payload");
            string msgRef = msg.Str("ref");

            if (topic == "phoenix")
            {
                if (evt == "phx_reply" && msgRef == _pendingHeartbeatRef) _pendingHeartbeatRef = null;
                return;
            }
            if (topic == null || !topic.StartsWith("realtime:", StringComparison.Ordinal)) return;
            if (!_channels.TryGetValue(topic.Substring(9), out var channel)) return;

            try
            {
                switch (evt)
                {
                    case "phx_reply":
                        if (msgRef != null && msgRef != channel.JoinRef && payload.Str("status") != "ok")
                            Debug.LogWarning($"[SupabaseRealtime] {name}: {channel.Topic} refused a message: {MiniJson.Serialize(payload)}");
                        if (msgRef != null && msgRef == channel.JoinRef)
                        {
                            string status = payload.Str("status");
                            if (status == "ok") { HandleJoined(channel); if (Verbose) Debug.Log($"[SupabaseRealtime] {name}: joined {channel.Topic}"); }
                            else
                            {
                                Debug.LogWarning($"[SupabaseRealtime] Join {channel.Topic} refused: {MiniJson.Serialize(payload)}");
                                channel.RejoinAt = Time.unscaledTime + 10f;
                            }
                        }
                        break;
                    case "phx_error":
                    case "phx_close":
                        // A close/error for an older join of this topic (rejoin, quick leave + join) is not about us.
                        if (msg.Str("join_ref") != null && msg.Str("join_ref") != channel.JoinRef) break;
                        if (Verbose || evt == "phx_error") Debug.LogWarning($"[SupabaseRealtime] {name}: {evt} on {channel.Topic} {MiniJson.Serialize(payload)}");
                        if (channel.Joined && !channel.Closed)
                        {
                            channel.Joined = false;
                            channel.RejoinAt = Time.unscaledTime + 3f;
                        }
                        break;
                    case "system":
                        if ((payload.Str("message") ?? "").IndexOf("Too many", StringComparison.OrdinalIgnoreCase) >= 0) channel.RaiseRateLimited();
                        if (payload.Str("status") != "ok" || Verbose) Debug.LogWarning($"[SupabaseRealtime] {name}: system on {channel.Topic}: {MiniJson.Serialize(payload)}");
                        break;
                    case "broadcast":
                        channel.RaiseBroadcast(payload.Str("event"), payload.Obj("payload"));
                        break;
                    case "presence_state":
                        HandlePresenceState(channel, payload);
                        break;
                    case "presence_diff":
                        HandlePresenceDiff(channel, payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                // A consumer threw: keep the socket alive, report the cause.
                Debug.LogException(ex);
            }
        }

        private void HandleJoined(Channel channel)
        {
            channel.Joined = true;
            channel.RejoinAt = -1f;
            if (channel.TrackJson != null) channel.Track(channel.TrackJson);
            foreach (var frame in channel.PendingFrames) Enqueue(frame);
            channel.PendingFrames.Clear();
            channel.RaiseJoined();
        }

        private static void HandlePresenceState(Channel channel, Dictionary<string, object> state)
        {
            var stale = new HashSet<string>(channel.PresenceMetas.Keys);
            if (state != null)
            {
                foreach (var kv in state)
                {
                    var metas = (kv.Value as Dictionary<string, object>).Arr("metas");
                    if (metas == null || metas.Count == 0) continue;
                    var list = new List<Dictionary<string, object>>();
                    foreach (var m in metas) if (m is Dictionary<string, object> d) list.Add(d);
                    channel.PresenceMetas[kv.Key] = list;
                    stale.Remove(kv.Key);
                    channel.RaiseJoin(kv.Key, list[list.Count - 1]);
                }
            }
            foreach (var key in stale)
            {
                channel.PresenceMetas.Remove(key);
                channel.RaiseLeave(key);
            }
            channel.RaiseSync();
        }

        private static void HandlePresenceDiff(Channel channel, Dictionary<string, object> diff)
        {
            // A re-track arrives as join(new ref) + leave(old ref) for the same key: apply joins first.
            var joins = diff.Obj("joins");
            if (joins != null)
            {
                foreach (var kv in joins)
                {
                    var metas = (kv.Value as Dictionary<string, object>).Arr("metas");
                    if (metas == null) continue;
                    if (!channel.PresenceMetas.TryGetValue(kv.Key, out var list))
                        channel.PresenceMetas[kv.Key] = list = new List<Dictionary<string, object>>();
                    foreach (var m in metas) if (m is Dictionary<string, object> d) list.Add(d);
                    if (list.Count > 0) channel.RaiseJoin(kv.Key, list[list.Count - 1]);
                }
            }
            var leaves = diff.Obj("leaves");
            if (leaves != null)
            {
                foreach (var kv in leaves)
                {
                    if (!channel.PresenceMetas.TryGetValue(kv.Key, out var list)) continue;
                    var metas = (kv.Value as Dictionary<string, object>).Arr("metas");
                    if (metas != null)
                    {
                        foreach (var m in metas)
                        {
                            string phxRef = (m as Dictionary<string, object>).Str("phx_ref");
                            list.RemoveAll(x => x.Str("phx_ref") == phxRef);
                        }
                    }
                    if (list.Count == 0)
                    {
                        channel.PresenceMetas.Remove(kv.Key);
                        channel.RaiseLeave(kv.Key);
                    }
                }
            }
        }

        private static string Frame(string topic, string evt, string payloadJson, string msgRef, string joinRef = null)
        {
            var sb = new StringBuilder(96 + (payloadJson?.Length ?? 0));
            sb.Append("{\"topic\":").Append(MiniJson.Quote(topic))
              .Append(",\"event\":").Append(MiniJson.Quote(evt))
              .Append(",\"payload\":").Append(string.IsNullOrEmpty(payloadJson) ? "{}" : payloadJson)
              .Append(",\"ref\":").Append(MiniJson.Quote(msgRef));
            if (joinRef != null) sb.Append(",\"join_ref\":").Append(MiniJson.Quote(joinRef));
            return sb.Append('}').ToString();
        }

        private string NextRef() => (++_messageRef).ToString();

        // ──────────────────────── Transport threads ────────────────────────

        private void Enqueue(string frame)
        {
            if (!IsConnected) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            NihongoRealtimeSend(frame);
            SentFrames++;
#else
            lock (_outgoingLock) _outgoing.Enqueue(Encoding.UTF8.GetBytes(frame));
            SentFrames++;
            _outgoingSignal?.Release();
#endif
        }

        private static void StartThread(string name, Action body)
        {
            var thread = new Thread(() => body()) { IsBackground = true, Name = name };
            thread.Start();
        }

        private void StartSendLoop(ClientWebSocket socket, CancellationToken token, SemaphoreSlim signal)
        {
            // Dedicated threads, not the thread pool: Mono's ClientWebSocket blocks a thread per read/write, and a few
            // sockets were enough to starve the pool (join replies arrived many seconds late).
            StartThread("RealtimeSend", () =>
            {
                try
                {
                    while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
                    {
                        signal.Wait(token);
                        // Drain everything queued: the signal count and the queue length need not match.
                        while (true)
                        {
                            byte[] bytes;
                            lock (_outgoingLock)
                            {
                                if (_outgoing.Count == 0) break;
                                bytes = _outgoing.Dequeue();
                            }
                            socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token).GetAwaiter().GetResult();
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Debug.LogWarning($"[SupabaseRealtime] Send failed: {ex.Message}");
                        _socketDropped = true;
                    }
                }
            });
        }

        private void StartReceiveLoop(ClientWebSocket socket, CancellationToken token)
        {
            StartThread("RealtimeReceive", () =>
            {
                var buffer = new byte[ReceiveBufferSize];
                var bytes = new List<byte>(ReceiveBufferSize);
                try
                {
                    while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
                    {
                        bytes.Clear();
                        WebSocketReceiveResult result;
                        do
                        {
                            result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).GetAwaiter().GetResult();
                            for (int i = 0; i < result.Count; i++) bytes.Add(buffer[i]);
                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _closeReason = $"server closed: {socket.CloseStatus} {socket.CloseStatusDescription}";
                            break;
                        }
                        // Decode whole frames only: a UTF-8 character can straddle two reads.
                        string message = Encoding.UTF8.GetString(bytes.ToArray());
                        lock (_incomingLock) _incoming.Enqueue(message);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested) { _closeReason = "receive failed: " + ex.Message; Debug.LogWarning($"[SupabaseRealtime] Receive failed: {ex.Message}"); }
                }
                if (!token.IsCancellationRequested) _socketDropped = true;
            });
        }
    }
}
