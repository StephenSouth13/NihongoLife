using System;
using System.Collections;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;

namespace NihongoLife.Save
{
    /// <summary>
    /// Cloud save implementation backed by Supabase.
    /// Maintains a local cache and syncs with the player_progress table.
    /// Falls back to local-only when offline.
    /// </summary>
    public class SupabaseProgressRepository : IProgressRepository
    {
        private PlayerProgressDto _cachedProgress;
        private readonly LocalProgressRepository _localFallback = new LocalProgressRepository();
        private SupabaseClient _client;
        private IAuthService _authService;
        private bool _cloudSyncPending = false;

        public SupabaseProgressRepository(SupabaseClient client)
        {
            _client = client;
        }

        public void Initialize()
        {
            // Always initialize local fallback first
            _localFallback.Initialize();
            _cachedProgress = _localFallback.GetProgress();

            // Get auth service
            if (GameServices.TryGet(out IAuthService auth))
            {
                _authService = auth;
                _authService.OnAuthStateChanged += HandleAuthStateChanged;

                if (_authService.IsAuthenticated)
                {
                    SyncFromCloud();
                }
            }

            Debug.Log("[SupabaseProgressRepo] Initialized with local fallback.");
        }

        public PlayerProgressDto GetProgress()
        {
            if (!PlayerSessionService.GetOrCreate().CanPersist)
                return PlayerSessionService.Instance.GuestProgress;
            if (_cachedProgress == null)
            {
                _cachedProgress = _localFallback.GetProgress();
            }
            return _cachedProgress;
        }

        public void SaveProgress(PlayerProgressDto progress)
        {
            if (!PlayerSessionService.GetOrCreate().CanPersist)
            {
                PlayerSessionService.Instance.UpdateGuestProgress(progress);
                _cachedProgress = progress;
                return;
            }
            _cachedProgress = progress;

            // Always save locally first
            _localFallback.SaveProgress(progress);

            // Sync to cloud if authenticated
            if (_authService != null && _authService.IsAuthenticated && _client != null && _client.IsConfigured)
            {
                UploadToCloud(progress);
            }
            else
            {
                _cloudSyncPending = true;
            }
        }

        public void ResetProgress()
        {
            _cachedProgress = new PlayerProgressDto();
            _localFallback.ResetProgress();

            if (_authService != null && _authService.IsAuthenticated && _client != null && _client.IsConfigured)
            {
                UploadToCloud(_cachedProgress);
            }
        }

        // ──────────────────────── Cloud Sync ────────────────────────

        private void HandleAuthStateChanged(AuthState state)
        {
            if (state == AuthState.SignedIn || state == AuthState.TokenRefreshed)
            {
                PlayerSessionService.Instance?.BeginAccount(_authService.UserId, _authService.DisplayName);
                if (_cloudSyncPending)
                {
                    UploadToCloud(_cachedProgress);
                    _cloudSyncPending = false;
                }
                else
                {
                    SyncFromCloud();
                }
            }
        }

        private void SyncFromCloud()
        {
            if (_client == null || !_client.IsConfigured || _authService == null || !_authService.IsAuthenticated) return;

            string url = _client.RestUrl("player_progress") + $"?user_id=eq.{_authService.UserId}&select=*";

            _client.Get(url,
                json =>
                {
                    var cloudProgress = ParseCloudProgress(json);
                    if (cloudProgress != null)
                    {
                        _cachedProgress = MergeProgress(_cachedProgress, cloudProgress);
                        _localFallback.SaveProgress(_cachedProgress);
                        Debug.Log("[SupabaseProgressRepo] Synced from cloud.");
                    }
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[SupabaseProgressRepo] Cloud sync failed ({code}): {error}");
                });
        }

        private void UploadToCloud(PlayerProgressDto progress)
        {
            if (_client == null || !_client.IsConfigured || _authService == null || !_authService.IsAuthenticated) return;

            string url = _client.RestUrl("player_progress");

            var cloudData = new CloudProgressData
            {
                user_id = _authService.UserId,
                completed_scenarios = progress.completedScenarios.ToArray(),
                progress_json = JsonUtility.ToJson(progress),
                updated_at = DateTime.UtcNow.ToString("o")
            };

            string body = JsonUtility.ToJson(cloudData);

            _client.Upsert(url, body,
                _ => Debug.Log("[SupabaseProgressRepo] Progress uploaded to cloud."),
                (code, error) => Debug.LogWarning($"[SupabaseProgressRepo] Upload failed ({code}): {error}"));

            // Also update the profile with XP/level
            UploadProfileStats(progress);

            // Also upsert scenario scores
            UploadScenarioScores(progress);
        }

        private void UploadProfileStats(PlayerProgressDto progress)
        {
            if (_client == null || !_client.IsConfigured || _authService == null) return;

            string url = _client.RestUrl("profiles");
            string body = $"{{\"id\":\"{_authService.UserId}\",\"display_name\":\"{EscapeJson(progress.displayName)}\",\"level\":{progress.level},\"xp\":{progress.xp},\"current_chapter\":{progress.currentChapter},\"updated_at\":\"{DateTime.UtcNow:o}\"}}";

            _client.Upsert(url, body,
                _ => { },
                (code, error) => Debug.LogWarning($"[SupabaseProgressRepo] Profile upsert failed ({code}): {error}"));
        }

        private void UploadScenarioScores(PlayerProgressDto progress)
        {
            if (progress.bestScores == null || progress.bestScores.Count == 0) return;

            foreach (var score in progress.bestScores)
            {
                string url = _client.RestUrl("scenario_scores");
                string body = $"{{\"user_id\":\"{_authService.UserId}\",\"scenario_id\":\"{EscapeJson(score.scenarioId)}\",\"overall_score\":{score.bestScore},\"success\":true,\"completed_at\":\"{DateTime.UtcNow:o}\"}}";

                _client.Upsert(url, body,
                    _ => { },
                    (code, error) => Debug.LogWarning($"[SupabaseProgressRepo] Score upsert failed: {error}"));
            }
        }

        // ──────────────────────── Parse / Merge ────────────────────────

        private static PlayerProgressDto ParseCloudProgress(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            // Response is an array — take first element
            string trimmed = json.Trim();
            if (trimmed.StartsWith("["))
            {
                if (trimmed == "[]") return null;
                // Extract first object
                int start = trimmed.IndexOf('{');
                int end = FindMatchingBrace(trimmed, start);
                if (start >= 0 && end > start)
                {
                    string obj = trimmed.Substring(start, end - start + 1);
                    return ParseSingleCloudProgress(obj);
                }
            }

            return ParseSingleCloudProgress(trimmed);
        }

        private static PlayerProgressDto ParseSingleCloudProgress(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<CloudProgressData>(json);
                if (wrapper != null && !string.IsNullOrWhiteSpace(wrapper.progress_json))
                {
                    return JsonUtility.FromJson<PlayerProgressDto>(wrapper.progress_json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseProgressRepo] Parse error: {ex.Message}");
            }
            return null;
        }

        private static PlayerProgressDto MergeProgress(PlayerProgressDto local, PlayerProgressDto cloud)
        {
            if (local == null) return cloud ?? new PlayerProgressDto();
            if (cloud == null) return local;

            // Cloud wins for primary stats if cloud has more progress
            var merged = new PlayerProgressDto
            {
                playerId = cloud.playerId,
                displayName = !string.IsNullOrWhiteSpace(cloud.displayName) ? cloud.displayName : local.displayName,
                xp = Mathf.Max(local.xp, cloud.xp),
                level = Mathf.Max(local.level, cloud.level),
                currentChapter = Mathf.Max(local.currentChapter, cloud.currentChapter),
                health = cloud.health,
                energy = cloud.energy,
                hunger = cloud.hunger,
                thirst = cloud.thirst,
                knowledge = Mathf.Max(local.knowledge, cloud.knowledge),
                activeJobRole = !string.IsNullOrWhiteSpace(cloud.activeJobRole) ? cloud.activeJobRole : local.activeJobRole,
                completedScenarios = new System.Collections.Generic.List<string>(cloud.completedScenarios),
                bestScores = new System.Collections.Generic.List<ScenarioScoreRecord>(cloud.bestScores),
                masteryLevels = new System.Collections.Generic.List<MasteryRecord>(cloud.masteryLevels),
                careers = cloud.careers != null
                    ? new System.Collections.Generic.List<CareerRecord>(cloud.careers)
                    : new System.Collections.Generic.List<CareerRecord>()
            };

            // Merge completed scenarios (union)
            foreach (var s in local.completedScenarios)
            {
                if (!merged.completedScenarios.Contains(s))
                {
                    merged.completedScenarios.Add(s);
                }
            }

            // Merge best scores (keep highest)
            foreach (var localScore in local.bestScores)
            {
                var existing = merged.bestScores.Find(s => s.scenarioId == localScore.scenarioId);
                if (existing == null)
                {
                    merged.bestScores.Add(localScore);
                }
                else if (localScore.bestScore > existing.bestScore)
                {
                    existing.bestScore = localScore.bestScore;
                    existing.completedAt = localScore.completedAt;
                }
            }

            foreach (var localCareer in local.careers ?? new System.Collections.Generic.List<CareerRecord>())
            {
                var existingCareer = merged.careers.Find(c => c.roleId == localCareer.roleId);
                if (existingCareer == null)
                {
                    merged.careers.Add(localCareer);
                    continue;
                }

                existingCareer.rank = Mathf.Max(existingCareer.rank, localCareer.rank);
                existingCareer.completedShifts = Mathf.Max(existingCareer.completedShifts, localCareer.completedShifts);
                existingCareer.reputation = Mathf.Max(existingCareer.reputation, localCareer.reputation);
            }

            return merged;
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
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        // ──────────────────────── DTOs ────────────────────────

        [Serializable]
        private class CloudProgressData
        {
            public string user_id;
            public string[] completed_scenarios;
            public string progress_json;
            public string updated_at;
        }
    }
}
