using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// Real online world service backed by Supabase Realtime.
    /// Handles player presence (see others on map) and chat via WebSocket.
    /// Falls back gracefully when offline.
    /// </summary>
    public class SupabaseOnlineWorldService : MonoBehaviour, IOnlineWorldService
    {
        private readonly List<OnlinePlayerSnapshot> _visiblePlayers = new List<OnlinePlayerSnapshot>();
        private readonly List<OnlineChatMessage> _chatHistory = new List<OnlineChatMessage>();

        private string _localPlayerId;
        private string _localDisplayName;
        private int _chatHistoryLimit = 80;
        private int _maxVisiblePlayers = 24;
        private SupabaseRealtimeClient _realtimeClient;
        private SupabaseClient _supabaseClient;
        private NihongoLife.Audio.VivoxVoiceManager _vivox;
        private float _presencePublishInterval = 0.25f;
        private float _nextPresencePublish;

        public bool IsConnected { get; private set; }
        public IReadOnlyList<OnlinePlayerSnapshot> VisiblePlayers => _visiblePlayers;
        public IReadOnlyList<OnlineChatMessage> ChatHistory => _chatHistory;

        public event Action<OnlinePlayerSnapshot> OnPlayerJoinedOrUpdated;
        public event Action<string> OnPlayerLeft;
        public event Action<OnlineChatMessage> OnChatMessageReceived;

        private const string PresenceChannel = "nihongolife-world";

        // ──────────────────────── Lifecycle ────────────────────────

        public void Initialize()
        {
            _realtimeClient = GetComponent<SupabaseRealtimeClient>();
            if (_realtimeClient == null)
            {
                _realtimeClient = FindFirstObjectByType<SupabaseRealtimeClient>();
            }

            _supabaseClient = GetComponent<SupabaseClient>();
            if (_supabaseClient == null)
            {
                _supabaseClient = FindFirstObjectByType<SupabaseClient>();
            }

            if (GameServices.TryGet(out GameControlService control) && control.Database != null)
            {
                _chatHistoryLimit = Mathf.Max(10, control.Database.chatHistoryLimit);
                _maxVisiblePlayers = Mathf.Max(4, control.Database.maxVisiblePlayers);
            }

            if (_realtimeClient != null)
            {
                _realtimeClient.OnBroadcastReceived += HandleBroadcast;
                _realtimeClient.OnPresenceJoin += HandlePresenceJoin;
                _realtimeClient.OnPresenceLeave += HandlePresenceLeave;
                _realtimeClient.OnPresenceSync += HandlePresenceSync;
                _realtimeClient.OnDisconnected += HandleDisconnected;
            }

            // Keep the optional voice/text transport visible in the same HUD chat
            // stream when Vivox is configured. Supabase remains the persistence
            // and presence backend; Vivox carries live channel messages.
            _vivox = FindFirstObjectByType<NihongoLife.Audio.VivoxVoiceManager>();
            if (_vivox != null)
            {
                _vivox.OnTextMessageReceived += HandleVivoxMessage;
            }

            Debug.Log("[SupabaseOnlineWorld] Initialized.");
        }

        private void OnDestroy()
        {
            if (_realtimeClient != null)
            {
                _realtimeClient.OnBroadcastReceived -= HandleBroadcast;
                _realtimeClient.OnPresenceJoin -= HandlePresenceJoin;
                _realtimeClient.OnPresenceLeave -= HandlePresenceLeave;
                _realtimeClient.OnPresenceSync -= HandlePresenceSync;
                _realtimeClient.OnDisconnected -= HandleDisconnected;
            }

            if (_vivox != null)
            {
                _vivox.OnTextMessageReceived -= HandleVivoxMessage;
                _vivox = null;
            }
        }

        // ──────────────────────── Public API ────────────────────────

        public void ConnectLocalPlayer(string playerId, string displayName)
        {
            _localPlayerId = string.IsNullOrWhiteSpace(playerId) ? Guid.NewGuid().ToString("N") : playerId;
            _localDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Learner" : displayName;

            // Use auth display name if available
            if (GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated)
            {
                _localPlayerId = auth.UserId;
                if (!string.IsNullOrWhiteSpace(auth.DisplayName))
                {
                    _localDisplayName = auth.DisplayName;
                }
            }

            IsConnected = true;

            // Connect to Supabase Realtime
            if (_realtimeClient != null)
            {
                _realtimeClient.Connect(PresenceChannel);
            }

            // Add local player to visible list
            UpdateLocalPlayerPose(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                Vector3.zero,
                Quaternion.identity);

            SendSystemMessage("town", $"{_localDisplayName} joined the world.");
            Debug.Log($"[SupabaseOnlineWorld] Local player connected: {_localDisplayName} ({_localPlayerId})");
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            string leavingId = _localPlayerId;
            IsConnected = false;

            _visiblePlayers.RemoveAll(p => p.playerId == leavingId);
            OnPlayerLeft?.Invoke(leavingId);

            _realtimeClient?.Disconnect();
            Debug.Log("[SupabaseOnlineWorld] Disconnected.");
        }

        public void UpdateLocalPlayerPose(string sceneName, Vector3 position, Quaternion rotation)
        {
            if (!IsConnected) return;

            // Update local list
            var snapshot = _visiblePlayers.Find(p => p.playerId == _localPlayerId);
            if (snapshot == null)
            {
                snapshot = new OnlinePlayerSnapshot { playerId = _localPlayerId };
                _visiblePlayers.Add(snapshot);
            }

            snapshot.displayName = _localDisplayName;
            snapshot.sceneName = sceneName;
            snapshot.roomId = RoomIdFor(sceneName, _localPlayerId);
            snapshot.position = position;
            snapshot.rotation = rotation;
            snapshot.serverTick = DateTime.UtcNow.Ticks;
            OnPlayerJoinedOrUpdated?.Invoke(snapshot);

            // Throttle presence publishing
            if (Time.unscaledTime >= _nextPresencePublish && _realtimeClient != null && _realtimeClient.IsConnected)
            {
                _nextPresencePublish = Time.unscaledTime + _presencePublishInterval;
                PublishPresence(snapshot);
            }
        }

        public void SendChatMessage(string channelId, string text)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(text)) return;

            var message = new OnlineChatMessage
            {
                messageId = Guid.NewGuid().ToString("N"),
                senderPlayerId = _localPlayerId,
                senderDisplayName = _localDisplayName,
                channelId = string.IsNullOrWhiteSpace(channelId) ? "town" : channelId,
                text = text.Trim(),
                sentAtUtcTicks = DateTime.UtcNow.Ticks
            };

            AddChatMessage(message);

            // Broadcast via Realtime
            if (_realtimeClient != null && _realtimeClient.IsConnected)
            {
                string payload = JsonUtility.ToJson(message);
                _realtimeClient.Broadcast("chat_message", payload);
            }

            _vivox?.SendTextMessage(message.text);

            // Persist to database
            PersistChatMessage(message);
        }

        private void HandleVivoxMessage(string senderName, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            AddChatMessage(new OnlineChatMessage
            {
                messageId = Guid.NewGuid().ToString("N"),
                senderPlayerId = "vivox_remote",
                senderDisplayName = string.IsNullOrWhiteSpace(senderName) ? "Learner" : senderName,
                channelId = "town",
                text = text.Trim(),
                sentAtUtcTicks = DateTime.UtcNow.Ticks
            });
        }

        // ──────────────────────── Presence ────────────────────────

        private void PublishPresence(OnlinePlayerSnapshot snapshot)
        {
            var data = new PresenceData
            {
                playerId = snapshot.playerId,
                displayName = snapshot.displayName,
                sceneName = snapshot.sceneName,
                roomId = snapshot.roomId,
                x = snapshot.position.x,
                y = snapshot.position.y,
                z = snapshot.position.z,
                rotY = snapshot.rotation.eulerAngles.y
            };

            string json = JsonUtility.ToJson(data);
            _realtimeClient.TrackPresence(json);
        }

        private void HandlePresenceSync(string json)
        {
            // Full state sync — rebuild visible players from all presence keys
            // For now, just log — actual parsing depends on Supabase Realtime message format
            Debug.Log("[SupabaseOnlineWorld] Presence sync received.");
        }

        private void HandlePresenceJoin(string key, string payloadJson)
        {
            var data = TryParsePresenceData(payloadJson);
            if (data == null || data.playerId == _localPlayerId) return;

            if (!string.Equals(data.roomId, RoomIdFor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, _localPlayerId), StringComparison.Ordinal)) return;

            if (_visiblePlayers.Count >= _maxVisiblePlayers) return;

            var snapshot = _visiblePlayers.Find(p => p.playerId == data.playerId);
            if (snapshot == null)
            {
                snapshot = new OnlinePlayerSnapshot { playerId = data.playerId };
                _visiblePlayers.Add(snapshot);
            }

            snapshot.displayName = data.displayName;
            snapshot.sceneName = data.sceneName;
            snapshot.roomId = data.roomId;
            snapshot.position = new Vector3(data.x, data.y, data.z);
            snapshot.rotation = Quaternion.Euler(0f, data.rotY, 0f);
            snapshot.serverTick = DateTime.UtcNow.Ticks;

            OnPlayerJoinedOrUpdated?.Invoke(snapshot);
        }

        private void HandlePresenceLeave(string key, string payloadJson)
        {
            var data = TryParsePresenceData(payloadJson);
            if (data == null) return;

            _visiblePlayers.RemoveAll(p => p.playerId == data.playerId);
            OnPlayerLeft?.Invoke(data.playerId);
        }

        // ──────────────────────── Broadcast (Chat) ────────────────────────

        private void HandleBroadcast(string eventType, string payloadJson)
        {
            if (eventType == "chat_message")
            {
                HandleRemoteChatMessage(payloadJson);
            }
        }

        private void HandleRemoteChatMessage(string json)
        {
            try
            {
                var message = JsonUtility.FromJson<OnlineChatMessage>(json);
                if (message == null || message.senderPlayerId == _localPlayerId) return;

                AddChatMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseOnlineWorld] Failed to parse chat message: {ex.Message}");
            }
        }

        private void HandleDisconnected()
        {
            // Remove all remote players
            for (int i = _visiblePlayers.Count - 1; i >= 0; i--)
            {
                if (_visiblePlayers[i].playerId != _localPlayerId)
                {
                    string id = _visiblePlayers[i].playerId;
                    _visiblePlayers.RemoveAt(i);
                    OnPlayerLeft?.Invoke(id);
                }
            }
        }

        // ──────────────────────── Persistence ────────────────────────

        private void PersistChatMessage(OnlineChatMessage message)
        {
            if (_supabaseClient == null || !_supabaseClient.IsConfigured) return;

            string url = _supabaseClient.RestUrl("chat_messages");
            string body = $"{{\"user_id\":\"{EscapeJson(message.senderPlayerId)}\",\"sender_name\":\"{EscapeJson(message.senderDisplayName)}\",\"channel_id\":\"{EscapeJson(message.channelId)}\",\"text\":\"{EscapeJson(message.text)}\"}}";

            _supabaseClient.Post(url, body,
                _ => { },
                (code, error) => Debug.LogWarning($"[SupabaseOnlineWorld] Chat persist failed: {error}"));
        }

        // ──────────────────────── Helpers ────────────────────────

        private void SendSystemMessage(string channelId, string text)
        {
            AddChatMessage(new OnlineChatMessage
            {
                messageId = Guid.NewGuid().ToString("N"),
                senderPlayerId = "system",
                senderDisplayName = "NihongoLife",
                channelId = channelId,
                text = text,
                sentAtUtcTicks = DateTime.UtcNow.Ticks
            });
        }

        private void AddChatMessage(OnlineChatMessage message)
        {
            _chatHistory.Add(message);
            while (_chatHistory.Count > _chatHistoryLimit)
            {
                _chatHistory.RemoveAt(0);
            }
            OnChatMessageReceived?.Invoke(message);
        }

        private static PresenceData TryParsePresenceData(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                // Try to find the inner metas/phx_ref structure and extract our data
                // Supabase presence join/leave wraps data inside metas array
                // Simple approach: find the first object with playerId
                if (json.Contains("\"playerId\""))
                {
                    int start = json.IndexOf('{', json.IndexOf("\"playerId\"") - 30);
                    if (start < 0) start = json.IndexOf('{');
                    int end = FindMatchingBrace(json, start);
                    if (start >= 0 && end > start)
                    {
                        string obj = json.Substring(start, end - start + 1);
                        return JsonUtility.FromJson<PresenceData>(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseOnlineWorld] Presence parse error: {ex.Message}");
            }
            return null;
        }

        private static int FindMatchingBrace(string json, int openIndex)
        {
            if (openIndex < 0 || openIndex >= json.Length) return -1;
            int depth = 0;
            for (int i = openIndex; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;
                if (depth == 0) return i;
            }
            return -1;
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }

        // ──────────────────────── DTOs ────────────────────────

        [Serializable]
        private class PresenceData
        {
            public string playerId;
            public string displayName;
            public string sceneName;
            public string roomId;
            public float x;
            public float y;
            public float z;
            public float rotY;
        }

        private static string RoomIdFor(string sceneName, string playerId)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) && sceneName.IndexOf("Bedroom", StringComparison.OrdinalIgnoreCase) >= 0)
                return "bedroom_" + (string.IsNullOrWhiteSpace(playerId) ? "guest" : playerId);
            return sceneName ?? string.Empty;
        }
    }
}
