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
        // Resolved from profiles / island_showcases (0 / -1 when unknown)
        public int friendLevel;
        public int islandLikes = -1;          // -1: no island published yet
        public string islandUpdatedAt;
        public bool islandChecked;            // the showcase query answered (so "no island" is known, not a network gap)
        public bool HasIsland => islandLikes >= 0;
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

                    ResolveDetails(() =>
                    {
                        OnFriendsUpdated?.Invoke();
                        callback?.Invoke();
                    });
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[FriendService] Refresh failed: {error}");
                    callback?.Invoke();
                });
        }

        /// <summary>Names and levels (profiles) and islands (island_showcases) of everyone listed, two requests in all.</summary>
        private void ResolveDetails(Action done)
        {
            var everyone = new List<FriendEntry>(_friends);
            everyone.AddRange(_pendingRequests);
            if (everyone.Count == 0) { done(); return; }
            var ids = new List<string>();
            foreach (var e in everyone) if (!string.IsNullOrEmpty(e.friendUserId) && !ids.Contains(e.friendUserId)) ids.Add(e.friendUserId);
            string list = string.Join(",", ids);
            int pending = 2;
            void Step() { if (--pending == 0) done(); }

            _client.Get(_client.RestUrl("profiles") + $"?id=in.({list})&select=id,display_name,level", json =>
            {
                var rows = JsonUtility.FromJson<ProfileRows>("{\"items\":" + json + "}");
                foreach (var row in rows?.items ?? Array.Empty<ProfileRow>())
                    foreach (var e in everyone)
                        if (e.friendUserId == row.id)
                        {
                            if (!string.IsNullOrWhiteSpace(row.display_name)) e.friendDisplayName = row.display_name;
                            e.friendLevel = row.level;
                        }
                Step();
            }, (code, error) => { Debug.LogWarning($"[FriendService] Profiles failed: {error}"); Step(); });

            _client.Get(_client.RestUrl("island_showcases") + $"?user_id=in.({list})&select=user_id,likes,updated_at", json =>
            {
                var rows = JsonUtility.FromJson<IslandRows>("{\"items\":" + json + "}");
                foreach (var e in everyone) e.islandChecked = true;
                foreach (var row in rows?.items ?? Array.Empty<IslandRow>())
                    foreach (var e in everyone)
                        if (e.friendUserId == row.user_id) { e.islandLikes = Mathf.Max(0, row.likes); e.islandUpdatedAt = row.updated_at; }
                Step();
            }, (code, error) => { Debug.LogWarning($"[FriendService] Islands failed: {error}"); Step(); });
        }

        /// <summary>Tests: fill the lists without a server.</summary>
        public void SetForTests(IEnumerable<FriendEntry> friends, IEnumerable<FriendEntry> requests)
        {
            _client = null; // offline: refreshes keep these lists instead of asking the server
            _friends.Clear(); _pendingRequests.Clear();
            if (friends != null) _friends.AddRange(friends);
            if (requests != null) _pendingRequests.AddRange(requests);
            OnFriendsUpdated?.Invoke();
        }

        [Serializable] private sealed class ProfileRow { public string id, display_name; public int level; }
        [Serializable] private sealed class ProfileRows { public ProfileRow[] items; }
        [Serializable] private sealed class IslandRow { public string user_id, updated_at; public int likes; }
        [Serializable] private sealed class IslandRows { public IslandRow[] items; }

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
