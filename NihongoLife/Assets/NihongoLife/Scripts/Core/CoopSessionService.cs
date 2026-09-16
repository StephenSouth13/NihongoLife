using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    [Serializable]
    public class CoopSession
    {
        public string id;
        public string host_user_id;
        public string scenario_id;
        public string status; // "waiting", "active", "completed"
        public int max_players;
        // Client-side resolved
        public string hostDisplayName;
    }

    [Serializable]
    public class CoopParticipant
    {
        public string id;
        public string session_id;
        public string user_id;
        public string role; // "host", "participant"
        public string assigned_speaker;
        public string displayName; // resolved client-side
    }

    /// <summary>
    /// Manages co-op learning sessions via Supabase.
    /// Create/join/leave sessions, assign speaker roles, and sync scenario state via Realtime.
    /// </summary>
    public class CoopSessionService : MonoBehaviour, IGameService
    {
        private SupabaseClient _client;
        private SupabaseRealtimeClient _realtimeClient;
        private CoopSession _currentSession;
        private List<CoopParticipant> _participants = new List<CoopParticipant>();

        public CoopSession CurrentSession => _currentSession;
        public IReadOnlyList<CoopParticipant> Participants => _participants;
        public bool InSession => _currentSession != null;
        public bool IsHost => InSession && GameServices.TryGet(out IAuthService auth) && _currentSession.host_user_id == auth.UserId;

        public event Action<CoopSession> OnSessionUpdated;
        public event Action<List<CoopParticipant>> OnParticipantsUpdated;
        public event Action<string> OnCoopBroadcast; // (payloadJson) for scenario state sync
        public event Action OnSessionEnded;

        public void Initialize()
        {
            _client = FindFirstObjectByType<SupabaseClient>();
            _realtimeClient = FindFirstObjectByType<SupabaseRealtimeClient>();
            Debug.Log("[CoopSessionService] Initialized.");
        }

        // ──────────────────────── Create ────────────────────────

        /// <summary>Create a new co-op session for a scenario.</summary>
        public void CreateSession(string scenarioId, int maxPlayers = 2, Action<bool> callback = null)
        {
            if (_client == null || !_client.IsConfigured || !GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated)
            {
                callback?.Invoke(false);
                return;
            }

            string url = _client.RestUrl("coop_sessions");
            string body = $"{{\"host_user_id\":\"{auth.UserId}\",\"scenario_id\":\"{EscapeJson(scenarioId)}\",\"max_players\":{maxPlayers},\"status\":\"waiting\"}}";

            _client.Post(url, body,
                json =>
                {
                    var session = ParseSession(json);
                    if (session != null)
                    {
                        _currentSession = session;

                        // Also add host as participant
                        JoinSessionInternal(session.id, "host", auth.UserId, callback);
                    }
                    else
                    {
                        callback?.Invoke(false);
                    }
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[CoopSession] Create failed: {error}");
                    callback?.Invoke(false);
                });
        }

        // ──────────────────────── Join ────────────────────────

        /// <summary>Join an existing co-op session.</summary>
        public void JoinSession(string sessionId, Action<bool> callback = null)
        {
            if (_client == null || !GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated)
            {
                callback?.Invoke(false);
                return;
            }

            // Fetch session info first
            string url = _client.RestUrl("coop_sessions") + $"?id=eq.{sessionId}&select=*";
            _client.Get(url,
                json =>
                {
                    var session = ParseSession(json);
                    if (session == null || session.status != "waiting")
                    {
                        callback?.Invoke(false);
                        return;
                    }

                    _currentSession = session;
                    JoinSessionInternal(sessionId, "participant", auth.UserId, callback);
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[CoopSession] Join failed: {error}");
                    callback?.Invoke(false);
                });
        }

        private void JoinSessionInternal(string sessionId, string role, string userId, Action<bool> callback)
        {
            string url = _client.RestUrl("coop_participants");
            string body = $"{{\"session_id\":\"{sessionId}\",\"user_id\":\"{userId}\",\"role\":\"{role}\"}}";

            _client.Upsert(url, body,
                _ =>
                {
                    Debug.Log($"[CoopSession] Joined session {sessionId} as {role}.");

                    // Subscribe to co-op Realtime channel
                    SubscribeToCoopChannel(sessionId);

                    // Refresh participants
                    RefreshParticipants(sessionId);
                    OnSessionUpdated?.Invoke(_currentSession);
                    callback?.Invoke(true);
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[CoopSession] Join internal failed: {error}");
                    callback?.Invoke(false);
                });
        }

        // ──────────────────────── Leave ────────────────────────

        public void LeaveSession(Action callback = null)
        {
            if (_currentSession == null || _client == null || !GameServices.TryGet(out IAuthService auth))
            {
                callback?.Invoke();
                return;
            }

            string url = _client.RestUrl("coop_participants") + $"?session_id=eq.{_currentSession.id}&user_id=eq.{auth.UserId}";

            _client.Delete(url,
                _ =>
                {
                    Debug.Log("[CoopSession] Left session.");

                    // If host leaves, end session
                    if (IsHost)
                    {
                        EndSession();
                    }

                    _currentSession = null;
                    _participants.Clear();
                    OnSessionEnded?.Invoke();
                    callback?.Invoke();
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[CoopSession] Leave failed: {error}");
                    callback?.Invoke();
                });
        }

        // ──────────────────────── Start / End ────────────────────────

        /// <summary>Host starts the session — changes status to 'active'.</summary>
        public void StartSession(Action<bool> callback = null)
        {
            if (!IsHost || _currentSession == null)
            {
                callback?.Invoke(false);
                return;
            }

            string url = _client.RestUrl("coop_sessions") + $"?id=eq.{_currentSession.id}";
            string body = "{\"status\":\"active\"}";

            _client.Patch(url, body,
                _ =>
                {
                    _currentSession.status = "active";
                    OnSessionUpdated?.Invoke(_currentSession);

                    // Broadcast start event
                    BroadcastCoopEvent("session_start", $"{{\"scenario_id\":\"{_currentSession.scenario_id}\"}}");

                    Debug.Log("[CoopSession] Session started!");
                    callback?.Invoke(true);
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[CoopSession] Start failed: {error}");
                    callback?.Invoke(false);
                });
        }

        private void EndSession()
        {
            if (_currentSession == null) return;

            string url = _client.RestUrl("coop_sessions") + $"?id=eq.{_currentSession.id}";
            string body = "{\"status\":\"completed\"}";

            _client.Patch(url, body,
                _ => Debug.Log("[CoopSession] Session ended."),
                (code, error) => Debug.LogWarning($"[CoopSession] End failed: {error}"));
        }

        // ──────────────────────── Speaker Assignment ────────────────────────

        /// <summary>Assign a speaker role to a participant (host only).</summary>
        public void AssignSpeaker(string participantId, string speakerRole, Action<bool> callback = null)
        {
            if (!IsHost || _client == null)
            {
                callback?.Invoke(false);
                return;
            }

            string url = _client.RestUrl("coop_participants") + $"?id=eq.{participantId}";
            string body = $"{{\"assigned_speaker\":\"{EscapeJson(speakerRole)}\"}}";

            _client.Patch(url, body,
                _ =>
                {
                    Debug.Log($"[CoopSession] Assigned speaker '{speakerRole}' to {participantId}.");
                    if (_currentSession != null) RefreshParticipants(_currentSession.id);
                    callback?.Invoke(true);
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[CoopSession] Assign failed: {error}");
                    callback?.Invoke(false);
                });
        }

        // ──────────────────────── Available Sessions ────────────────────────

        /// <summary>Fetch sessions that are waiting for players.</summary>
        public void GetAvailableSessions(Action<List<CoopSession>> callback)
        {
            if (_client == null || !_client.IsConfigured)
            {
                callback?.Invoke(new List<CoopSession>());
                return;
            }

            string url = _client.RestUrl("coop_sessions") + "?status=eq.waiting&order=created_at.desc&limit=20&select=*";

            _client.Get(url,
                json => callback?.Invoke(ParseSessions(json)),
                (code, error) =>
                {
                    Debug.LogWarning($"[CoopSession] Fetch available failed: {error}");
                    callback?.Invoke(new List<CoopSession>());
                });
        }

        // ──────────────────────── Realtime ────────────────────────

        private void SubscribeToCoopChannel(string sessionId)
        {
            if (_realtimeClient == null) return;

            // We reuse the existing realtime client but listen for coop-specific broadcasts
            _realtimeClient.OnBroadcastReceived += HandleCoopBroadcast;
        }

        private void HandleCoopBroadcast(string eventType, string payloadJson)
        {
            if (eventType.StartsWith("coop_"))
            {
                OnCoopBroadcast?.Invoke(payloadJson);

                if (eventType == "coop_session_start" && _currentSession != null)
                {
                    _currentSession.status = "active";
                    OnSessionUpdated?.Invoke(_currentSession);
                }
            }
        }

        /// <summary>Broadcast a co-op event to all participants.</summary>
        public void BroadcastCoopEvent(string eventType, string payloadJson)
        {
            if (_realtimeClient == null || !_realtimeClient.IsConnected) return;
            _realtimeClient.Broadcast($"coop_{eventType}", payloadJson);
        }

        // ──────────────────────── Participants ────────────────────────

        private void RefreshParticipants(string sessionId)
        {
            string url = _client.RestUrl("coop_participants") + $"?session_id=eq.{sessionId}&select=*";

            _client.Get(url,
                json =>
                {
                    _participants = ParseParticipants(json);
                    OnParticipantsUpdated?.Invoke(_participants);
                },
                (code, error) => Debug.LogWarning($"[CoopSession] Refresh participants failed: {error}"));
        }

        // ──────────────────────── Parsing ────────────────────────

        private static CoopSession ParseSession(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            string trimmed = json.Trim();
            if (trimmed.StartsWith("["))
            {
                if (trimmed == "[]") return null;
                int s = trimmed.IndexOf('{');
                int e = FindBrace(trimmed, s);
                if (s >= 0 && e > s) trimmed = trimmed.Substring(s, e - s + 1);
            }
            try { return JsonUtility.FromJson<CoopSession>(trimmed); }
            catch { return null; }
        }

        private static List<CoopSession> ParseSessions(string json)
        {
            return ParseArray<CoopSession>(json);
        }

        private static List<CoopParticipant> ParseParticipants(string json)
        {
            return ParseArray<CoopParticipant>(json);
        }

        private static List<T> ParseArray<T>(string json)
        {
            var list = new List<T>();
            if (string.IsNullOrWhiteSpace(json)) return list;
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("[")) return list;

            int idx = 1;
            while (idx < trimmed.Length)
            {
                int s = trimmed.IndexOf('{', idx);
                if (s < 0) break;
                int e = FindBrace(trimmed, s);
                if (e < 0) break;
                try
                {
                    var item = JsonUtility.FromJson<T>(trimmed.Substring(s, e - s + 1));
                    if (item != null) list.Add(item);
                }
                catch { }
                idx = e + 1;
            }
            return list;
        }

        private static int FindBrace(string s, int i)
        {
            int d = 0;
            for (; i < s.Length; i++)
            {
                if (s[i] == '{') d++;
                else if (s[i] == '}') d--;
                if (d == 0) return i;
            }
            return -1;
        }

        private static string EscapeJson(string v) => v?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty;
    }
}
