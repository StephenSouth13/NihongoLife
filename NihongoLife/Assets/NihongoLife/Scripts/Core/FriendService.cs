using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    [Serializable]
    public class FriendEntry
    {
        public string id;
        public string requester_id;
        public string addressee_id;
        public string status; // "pending", "accepted", "blocked"
        // Resolved display names (populated client-side)
        public string friendDisplayName;
        public string friendUserId;
    }

    /// <summary>
    /// Friend system service — send/accept/remove friend requests via Supabase.
    /// </summary>
    public class FriendService : MonoBehaviour, IGameService
    {
        private SupabaseClient _client;
        private List<FriendEntry> _friends = new List<FriendEntry>();
        private List<FriendEntry> _pendingRequests = new List<FriendEntry>();

        public IReadOnlyList<FriendEntry> Friends => _friends;
        public IReadOnlyList<FriendEntry> PendingRequests => _pendingRequests;

        public event Action OnFriendsUpdated;

        public void Initialize()
        {
            _client = FindFirstObjectByType<SupabaseClient>();
            Debug.Log("[FriendService] Initialized.");
        }

        /// <summary>Send a friend request to another user by their ID.</summary>
        public void SendFriendRequest(string targetUserId, Action<bool> callback = null)
        {
            if (_client == null || !_client.IsConfigured || !GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated)
            {
                callback?.Invoke(false);
                return;
            }

            string url = _client.RestUrl("friendships");
            string body = $"{{\"requester_id\":\"{auth.UserId}\",\"addressee_id\":\"{EscapeJson(targetUserId)}\",\"status\":\"pending\"}}";

            _client.Post(url, body,
                _ =>
                {
                    Debug.Log($"[FriendService] Friend request sent to {targetUserId}");
                    callback?.Invoke(true);
                    RefreshFriends();
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[FriendService] Request failed: {error}");
                    callback?.Invoke(false);
                });
        }

        /// <summary>Accept a pending friend request.</summary>
        public void AcceptFriendRequest(string friendshipId, Action<bool> callback = null)
        {
            if (_client == null || !_client.IsConfigured)
            {
                callback?.Invoke(false);
                return;
            }

            string url = _client.RestUrl("friendships") + $"?id=eq.{friendshipId}";
            string body = "{\"status\":\"accepted\"}";

            _client.Patch(url, body,
                _ =>
                {
                    Debug.Log("[FriendService] Friend request accepted.");
                    callback?.Invoke(true);
                    RefreshFriends();
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[FriendService] Accept failed: {error}");
                    callback?.Invoke(false);
                });
        }

        /// <summary>Remove a friend or decline a request.</summary>
        public void RemoveFriend(string friendshipId, Action<bool> callback = null)
        {
            if (_client == null || !_client.IsConfigured)
            {
                callback?.Invoke(false);
                return;
            }

            string url = _client.RestUrl("friendships") + $"?id=eq.{friendshipId}";

            _client.Delete(url,
                _ =>
                {
                    Debug.Log("[FriendService] Friend removed.");
                    callback?.Invoke(true);
                    RefreshFriends();
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[FriendService] Remove failed: {error}");
                    callback?.Invoke(false);
                });
        }

        /// <summary>Refresh the full friends list and pending requests from Supabase.</summary>
        public void RefreshFriends(Action callback = null)
        {
            if (_client == null || !_client.IsConfigured || !GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated)
            {
                callback?.Invoke();
                return;
            }

            // Fetch all friendships involving the current user
            string userId = auth.UserId;
            string url = _client.RestUrl("friendships") + $"?or=(requester_id.eq.{userId},addressee_id.eq.{userId})&select=*";

            _client.Get(url,
                json =>
                {
                    var all = ParseFriendEntries(json);
                    _friends.Clear();
                    _pendingRequests.Clear();

                    foreach (var entry in all)
                    {
                        // Resolve which user is the "friend"
                        entry.friendUserId = entry.requester_id == userId ? entry.addressee_id : entry.requester_id;

                        if (entry.status == "accepted")
                        {
                            _friends.Add(entry);
                        }
                        else if (entry.status == "pending" && entry.addressee_id == userId)
                        {
                            _pendingRequests.Add(entry);
                        }
                    }

                    OnFriendsUpdated?.Invoke();
                    callback?.Invoke();
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[FriendService] Refresh failed: {error}");
                    callback?.Invoke();
                });
        }

        private static List<FriendEntry> ParseFriendEntries(string json)
        {
            var list = new List<FriendEntry>();
            if (string.IsNullOrWhiteSpace(json)) return list;

            string trimmed = json.Trim();
            if (!trimmed.StartsWith("[")) return list;

            int index = 1;
            while (index < trimmed.Length)
            {
                int start = trimmed.IndexOf('{', index);
                if (start < 0) break;
                int end = FindBrace(trimmed, start);
                if (end < 0) break;

                try
                {
                    var entry = JsonUtility.FromJson<FriendEntry>(trimmed.Substring(start, end - start + 1));
                    if (entry != null) list.Add(entry);
                }
                catch { }
                index = end + 1;
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
