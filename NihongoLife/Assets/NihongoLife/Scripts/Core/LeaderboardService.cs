using System;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Core
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string user_id;
        public string display_name;
        public int total_xp;
        public int scenarios_completed;
        public float average_score;
    }

    /// <summary>
    /// Service for fetching and updating the global leaderboard via Supabase REST API.
    /// </summary>
    public class LeaderboardService : MonoBehaviour, IGameService
    {
        private SupabaseClient _client;
        private List<LeaderboardEntry> _cachedEntries = new List<LeaderboardEntry>();
        private int _playerRank = -1;

        public IReadOnlyList<LeaderboardEntry> CachedEntries => _cachedEntries;
        public int PlayerRank => _playerRank;

        public event Action OnLeaderboardUpdated;

        public void Initialize()
        {
            _client = FindFirstObjectByType<SupabaseClient>();
            Debug.Log("[LeaderboardService] Initialized.");
        }

        /// <summary>Fetch top N players from leaderboard.</summary>
        public void FetchTopPlayers(int limit = 50, Action<List<LeaderboardEntry>> callback = null)
        {
            if (_client == null || !_client.IsConfigured)
            {
                callback?.Invoke(_cachedEntries);
                return;
            }

            string url = _client.RestUrl("leaderboard") + $"?order=total_xp.desc&limit={limit}&select=*";

            _client.Get(url,
                json =>
                {
                    _cachedEntries = ParseEntries(json);
                    UpdatePlayerRank();
                    OnLeaderboardUpdated?.Invoke();
                    callback?.Invoke(_cachedEntries);
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[LeaderboardService] Fetch failed ({code}): {error}");
                    callback?.Invoke(_cachedEntries);
                });
        }

        /// <summary>Update the local player's leaderboard entry.</summary>
        public void UpdateMyScore(string displayName, int totalXp, int scenariosCompleted, float averageScore)
        {
            if (_client == null || !_client.IsConfigured) return;
            if (!GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated) return;

            string url = _client.RestUrl("leaderboard");
            string body = $"{{\"user_id\":\"{auth.UserId}\",\"display_name\":\"{EscapeJson(displayName)}\",\"total_xp\":{totalXp},\"scenarios_completed\":{scenariosCompleted},\"average_score\":{averageScore:F1},\"updated_at\":\"{DateTime.UtcNow:o}\"}}";

            _client.Upsert(url, body,
                _ =>
                {
                    Debug.Log("[LeaderboardService] Leaderboard updated.");
                    FetchTopPlayers();
                },
                (code, error) => Debug.LogWarning($"[LeaderboardService] Update failed: {error}"));
        }

        private void UpdatePlayerRank()
        {
            _playerRank = -1;
            if (!GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated) return;

            for (int i = 0; i < _cachedEntries.Count; i++)
            {
                if (_cachedEntries[i].user_id == auth.UserId)
                {
                    _playerRank = i + 1;
                    break;
                }
            }
        }

        private static List<LeaderboardEntry> ParseEntries(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<LeaderboardEntry>();

            // Parse JSON array manually using Unity's JsonUtility (which doesn't support arrays directly)
            var list = new List<LeaderboardEntry>();
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("[")) return list;

            int index = 1; // skip [
            while (index < trimmed.Length)
            {
                int start = trimmed.IndexOf('{', index);
                if (start < 0) break;
                int end = FindMatchingBrace(trimmed, start);
                if (end < 0) break;

                string obj = trimmed.Substring(start, end - start + 1);
                try
                {
                    var entry = JsonUtility.FromJson<LeaderboardEntry>(obj);
                    if (entry != null) list.Add(entry);
                }
                catch { }

                index = end + 1;
            }

            return list;
        }

        private static int FindMatchingBrace(string json, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;
                if (depth == 0) return i;
            }
            return -1;
        }

        private static string EscapeJson(string v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            return v.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
