using System;
using UnityEngine;

namespace NihongoLife.Core
{
    [Serializable]
    public class PlayerProfile
    {
        public string id;
        public string display_name;
        public string avatar_url;
        public int level;
        public int xp;
        public int current_chapter;
        public string bio;
    }

    /// <summary>
    /// Profile service — fetch and update player profiles via Supabase REST API.
    /// </summary>
    public class ProfileService : MonoBehaviour, IGameService
    {
        private SupabaseClient _client;
        private PlayerProfile _myProfile;

        public PlayerProfile MyProfile => _myProfile;
        public event Action<PlayerProfile> OnProfileLoaded;

        public void Initialize()
        {
            _client = FindFirstObjectByType<SupabaseClient>();
            Debug.Log("[ProfileService] Initialized.");
        }

        /// <summary>Fetch profile for a specific user.</summary>
        public void GetProfile(string userId, Action<PlayerProfile> callback)
        {
            if (_client == null || !_client.IsConfigured)
            {
                callback?.Invoke(null);
                return;
            }

            string url = _client.RestUrl("profiles") + $"?id=eq.{userId}&select=*";

            _client.Get(url,
                json =>
                {
                    var profile = ParseSingleProfile(json);
                    callback?.Invoke(profile);
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[ProfileService] GetProfile failed: {error}");
                    callback?.Invoke(null);
                });
        }

        /// <summary>Fetch the current user's profile.</summary>
        public void GetMyProfile(Action<PlayerProfile> callback = null)
        {
            if (!GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated)
            {
                callback?.Invoke(null);
                return;
            }

            GetProfile(auth.UserId, profile =>
            {
                _myProfile = profile;
                OnProfileLoaded?.Invoke(profile);
                callback?.Invoke(profile);
            });
        }

        /// <summary>Update display name and bio for current user.</summary>
        public void UpdateProfile(string displayName, string bio, Action<bool> callback = null)
        {
            if (_client == null || !_client.IsConfigured || !GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated)
            {
                callback?.Invoke(false);
                return;
            }

            string url = _client.RestUrl("profiles") + $"?id=eq.{auth.UserId}";
            string body = $"{{\"display_name\":\"{EscapeJson(displayName)}\",\"bio\":\"{EscapeJson(bio)}\",\"updated_at\":\"{DateTime.UtcNow:o}\"}}";

            _client.Patch(url, body,
                _ =>
                {
                    Debug.Log("[ProfileService] Profile updated.");
                    if (_myProfile != null)
                    {
                        _myProfile.display_name = displayName;
                        _myProfile.bio = bio;
                    }
                    callback?.Invoke(true);
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[ProfileService] Update failed: {error}");
                    callback?.Invoke(false);
                });
        }

        private static PlayerProfile ParseSingleProfile(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            string trimmed = json.Trim();

            if (trimmed.StartsWith("["))
            {
                if (trimmed == "[]") return null;
                int s = trimmed.IndexOf('{');
                int e = FindBrace(trimmed, s);
                if (s >= 0 && e > s)
                {
                    trimmed = trimmed.Substring(s, e - s + 1);
                }
            }

            try
            {
                return JsonUtility.FromJson<PlayerProfile>(trimmed);
            }
            catch
            {
                return null;
            }
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

        private static string EscapeJson(string v) => v?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") ?? string.Empty;
    }
}
