using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// WebSocket client for Supabase Realtime.
    /// Handles Presence (see other players), Broadcast (chat messages), and Postgres Changes.
    /// </summary>
    public class SupabaseRealtimeClient : MonoBehaviour
    {
        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string, string> OnBroadcastReceived; // (eventType, payloadJson)
        public event Action<string> OnPresenceSync; // (presenceStateJson)
        public event Action<string, string> OnPresenceJoin; // (key, payloadJson)
        public event Action<string, string> OnPresenceLeave; // (key, payloadJson)

        private ClientWebSocket _socket;
        private CancellationTokenSource _cancellation;
        private SupabaseClient _supabaseClient;
        private string _channelTopic;
        private float _heartbeatInterval = 30f;
        private float _nextHeartbeat;
        private int _messageRef = 0;
        private bool _joined = false;
        private readonly Queue<string> _incomingMessages = new Queue<string>();
        private readonly object _lock = new object();

        private const int ReceiveBufferSize = 8192;

        // ──────────────────────── Public API ────────────────────────

        public void Initialize(SupabaseClient client)
        {
            _supabaseClient = client;
        }

        /// <summary>Connect to Supabase Realtime and join a channel.</summary>
        public void Connect(string channelTopic)
        {
            if (!_supabaseClient.IsConfigured)
            {
                Debug.LogWarning("[SupabaseRealtime] SupabaseClient is not configured.");
                return;
            }

            _channelTopic = channelTopic;
            StartCoroutine(ConnectCoroutine());
        }

        public void Disconnect()
        {
            _joined = false;
            _cancellation?.Cancel();

            if (_socket != null && _socket.State == WebSocketState.Open)
            {
                try
                {
                    _ = _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SupabaseRealtime] Close error: {ex.Message}");
                }
            }

            _socket = null;
            OnDisconnected?.Invoke();
            Debug.Log("[SupabaseRealtime] Disconnected.");
        }

        /// <summary>Send a Broadcast event to the channel.</summary>
        public void Broadcast(string eventType, string payloadJson)
        {
            if (!IsConnected || !_joined) return;

            string message = BuildMessage("realtime:broadcast", "broadcast", $"{{\"type\":\"broadcast\",\"event\":\"{eventType}\",\"payload\":{payloadJson}}}");
            SendMessage(message);
        }

        /// <summary>Track presence state (e.g., player position).</summary>
        public void TrackPresence(string presencePayloadJson)
        {
            if (!IsConnected || !_joined) return;

            string message = BuildMessage($"realtime:{_channelTopic}", "presence", $"{{\"type\":\"presence\",\"event\":\"track\",\"payload\":{presencePayloadJson}}}");
            SendMessage(message);
        }

        // ──────────────────────── Connection ────────────────────────

        private IEnumerator ConnectCoroutine()
        {
            _cancellation?.Cancel();
            _cancellation = new CancellationTokenSource();

            string url = _supabaseClient.RealtimeUrl;
            Debug.Log($"[SupabaseRealtime] Connecting to {url}");

            _socket = new ClientWebSocket();

            var connectTask = _socket.ConnectAsync(new Uri(url), _cancellation.Token);
            while (!connectTask.IsCompleted)
            {
                yield return null;
            }

            if (connectTask.IsFaulted || _socket.State != WebSocketState.Open)
            {
                Debug.LogError($"[SupabaseRealtime] Connection failed: {connectTask.Exception?.GetBaseException()?.Message}");
                yield break;
            }

            Debug.Log("[SupabaseRealtime] WebSocket connected.");
            OnConnected?.Invoke();

            // Start receive loop on background thread
            StartReceiveLoop();

            // Join the channel
            JoinChannel();
        }

        private void JoinChannel()
        {
            string token = !string.IsNullOrWhiteSpace(_supabaseClient.AccessToken)
                ? _supabaseClient.AccessToken
                : _supabaseClient.AnonKey;

            string joinPayload = $"{{" +
                $"\"config\":{{" +
                    $"\"broadcast\":{{\"self\":true}}," +
                    $"\"presence\":{{\"key\":\"\"}}," +
                    $"\"postgres_changes\":[]" +
                $"}}," +
                $"\"access_token\":\"{token}\"" +
            $"}}";

            string message = $"{{\"topic\":\"realtime:{_channelTopic}\",\"event\":\"phx_join\",\"payload\":{joinPayload},\"ref\":\"{NextRef()}\"}}";
            SendMessage(message);
            _joined = true;
            _nextHeartbeat = Time.unscaledTime + _heartbeatInterval;
            Debug.Log($"[SupabaseRealtime] Joined channel: {_channelTopic}");
        }

        // ──────────────────────── Update loop ────────────────────────

        private void Update()
        {
            // Process incoming messages on main thread
            lock (_lock)
            {
                while (_incomingMessages.Count > 0)
                {
                    string msg = _incomingMessages.Dequeue();
                    ProcessMessage(msg);
                }
            }

            // Heartbeat
            if (IsConnected && Time.unscaledTime >= _nextHeartbeat)
            {
                _nextHeartbeat = Time.unscaledTime + _heartbeatInterval;
                SendHeartbeat();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        // ──────────────────────── Receive loop ────────────────────────

        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {
                var buffer = new byte[ReceiveBufferSize];
                var builder = new StringBuilder();

                try
                {
                    while (_socket != null && _socket.State == WebSocketState.Open && !_cancellation.IsCancellationRequested)
                    {
                        builder.Clear();
                        WebSocketReceiveResult result;

                        do
                        {
                            result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellation.Token);
                            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

                        string message = builder.ToString();
                        lock (_lock)
                        {
                            _incomingMessages.Enqueue(message);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (WebSocketException ex)
                {
                    Debug.LogWarning($"[SupabaseRealtime] WebSocket error: {ex.Message}");
                }
            });
        }

        // ──────────────────────── Message processing ────────────────────────

        private void ProcessMessage(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<RealtimeMessage>(json);
                if (msg == null) return;

                switch (msg.@event)
                {
                    case "phx_reply":
                        // Acknowledgement — ignore
                        break;

                    case "phx_error":
                        Debug.LogWarning($"[SupabaseRealtime] Channel error: {json}");
                        break;

                    case "broadcast":
                        HandleBroadcast(json);
                        break;

                    case "presence_state":
                        OnPresenceSync?.Invoke(json);
                        break;

                    case "presence_diff":
                        HandlePresenceDiff(json);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseRealtime] Failed to process message: {ex.Message}");
            }
        }

        private void HandleBroadcast(string json)
        {
            // Extract event type and payload from broadcast message
            // Format: {"topic":"...","event":"broadcast","payload":{"type":"broadcast","event":"chat_message","payload":{...}}}
            try
            {
                // Simple extraction — find the inner event and payload
                int eventIndex = json.IndexOf("\"event\":", json.IndexOf("\"payload\""), StringComparison.Ordinal);
                if (eventIndex < 0) return;

                string eventType = ExtractStringValue(json, eventIndex);
                string payload = ExtractPayloadObject(json, eventIndex);

                OnBroadcastReceived?.Invoke(eventType, payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseRealtime] Broadcast parse error: {ex.Message}");
            }
        }

        private void HandlePresenceDiff(string json)
        {
            // Presence diff contains joins and leaves
            // For simplicity, fire generic events — consumers parse the JSON
            try
            {
                if (json.Contains("\"joins\""))
                {
                    int joinsIndex = json.IndexOf("\"joins\"", StringComparison.Ordinal);
                    if (joinsIndex >= 0)
                    {
                        string joinsSection = ExtractObjectAfterKey(json, joinsIndex);
                        OnPresenceJoin?.Invoke("joins", joinsSection);
                    }
                }

                if (json.Contains("\"leaves\""))
                {
                    int leavesIndex = json.IndexOf("\"leaves\"", StringComparison.Ordinal);
                    if (leavesIndex >= 0)
                    {
                        string leavesSection = ExtractObjectAfterKey(json, leavesIndex);
                        OnPresenceLeave?.Invoke("leaves", leavesSection);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseRealtime] Presence diff parse error: {ex.Message}");
            }
        }

        // ──────────────────────── Send helpers ────────────────────────

        private void SendMessage(string json)
        {
            if (_socket == null || _socket.State != WebSocketState.Open) return;

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                _ = _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellation.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseRealtime] Send error: {ex.Message}");
            }
        }

        private void SendHeartbeat()
        {
            string heartbeat = $"{{\"topic\":\"phoenix\",\"event\":\"heartbeat\",\"payload\":{{}},\"ref\":\"{NextRef()}\"}}";
            SendMessage(heartbeat);
        }

        private string BuildMessage(string topic, string eventName, string payload)
        {
            return $"{{\"topic\":\"{topic}\",\"event\":\"{eventName}\",\"payload\":{payload},\"ref\":\"{NextRef()}\"}}";
        }

        private string NextRef() => (++_messageRef).ToString();

        // ──────────────────────── Simple JSON helpers ────────────────────────

        private static string ExtractStringValue(string json, int startIndex)
        {
            int firstQuote = json.IndexOf('"', json.IndexOf(':', startIndex));
            if (firstQuote < 0) return string.Empty;
            int secondQuote = json.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0) return string.Empty;
            return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }

        private static string ExtractPayloadObject(string json, int afterEventIndex)
        {
            int payloadKey = json.IndexOf("\"payload\":", afterEventIndex, StringComparison.Ordinal);
            if (payloadKey < 0) return "{}";
            return ExtractObjectAfterKey(json, payloadKey);
        }

        private static string ExtractObjectAfterKey(string json, int keyIndex)
        {
            int colonIndex = json.IndexOf(':', keyIndex);
            if (colonIndex < 0) return "{}";

            int braceStart = json.IndexOf('{', colonIndex);
            if (braceStart < 0) return "{}";

            int depth = 0;
            for (int i = braceStart; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;

                if (depth == 0)
                {
                    return json.Substring(braceStart, i - braceStart + 1);
                }
            }

            return "{}";
        }

        // ──────────────────────── Serialization DTOs ────────────────────────

        [Serializable]
        private class RealtimeMessage
        {
            public string topic;
            // 'event' is a C# keyword, we use @event but JsonUtility maps "event" field
            [SerializeField] private string _event;
            public string @event
            {
                get
                {
                    // JsonUtility doesn't support @event, so we try a workaround
                    return _event;
                }
                set => _event = value;
            }
            public string @ref;
        }
    }
}
