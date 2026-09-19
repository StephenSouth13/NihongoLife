using System;
using System.Collections;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// Supabase Auth implementation.
    /// Supports anonymous login, email/password sign-in and sign-up.
    /// Persists session to PlayerPrefs for auto-restore on next launch.
    /// </summary>
    public class SupabaseAuthService : MonoBehaviour, IAuthService
    {
        public bool IsAuthenticated { get; private set; }
        public string UserId { get; private set; } = string.Empty;
        public string AccessToken { get; private set; } = string.Empty;
        public string DisplayName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;

        public event Action<AuthState> OnAuthStateChanged;

        private SupabaseClient _client;
        private Coroutine _refreshCoroutine;

        private const string PrefKeyAccessToken = "NL.Auth.AccessToken";
        private const string PrefKeyRefreshToken = "NL.Auth.RefreshToken";
        private const string PrefKeyUserId = "NL.Auth.UserId";
        private const string PrefKeyDisplayName = "NL.Auth.DisplayName";
        private const string PrefKeyEmail = "NL.Auth.Email";
        private const float TokenRefreshIntervalSeconds = 3300f; // Refresh every 55 minutes (tokens last 60 min)

        // ──────────────────────── Lifecycle ────────────────────────

        public void Initialize()
        {
            // Find the SupabaseClient on the same or parent GameObject
            _client = GetComponent<SupabaseClient>();
            if (_client == null)
            {
                _client = FindFirstObjectByType<SupabaseClient>();
            }

            if (_client == null || !_client.IsConfigured)
            {
                Debug.LogWarning("[SupabaseAuth] SupabaseClient not found or not configured. Auth will not function.");
            }

            Debug.Log("[SupabaseAuth] Initialized.");
        }

        private void OnDestroy()
        {
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
            }
        }

        // ──────────────────────── Sign In Anonymously ────────────────────────

        public void SignInAnonymously(Action<bool, string> callback)
        {
            if (_client == null || !_client.IsConfigured)
            {
                // Offline fallback — create a local-only session
                SetLocalSession("local_" + Guid.NewGuid().ToString("N").Substring(0, 8), "Guest", string.Empty);
                callback?.Invoke(true, string.Empty);
                return;
            }

            StartCoroutine(SignInAnonymouslyCoroutine(callback));
        }

        private IEnumerator SignInAnonymouslyCoroutine(Action<bool, string> callback)
        {
            string body = "{\"data\":{}}";
            bool done = false;
            bool success = false;
            string errorMessage = string.Empty;

            _client.AuthPost("signup", body,
                json =>
                {
                    success = HandleAuthResponse(json);
                    if (!success) errorMessage = "Failed to parse auth response.";
                    done = true;
                },
                (code, error) =>
                {
                    errorMessage = ParseErrorMessage(error);
                    done = true;
                });

            while (!done) yield return null;

            callback?.Invoke(success, errorMessage);
        }

        // ──────────────────────── Sign In with Email ────────────────────────

        public void SignInWithEmail(string email, string password, Action<bool, string> callback)
        {
            if (_client == null || !_client.IsConfigured)
            {
                callback?.Invoke(false, "Supabase is not configured.");
                return;
            }

            StartCoroutine(SignInWithEmailCoroutine(email, password, callback));
        }

        private IEnumerator SignInWithEmailCoroutine(string email, string password, Action<bool, string> callback)
        {
            string body = $"{{\"email\":\"{EscapeJson(email)}\",\"password\":\"{EscapeJson(password)}\"}}";
            bool done = false;
            bool success = false;
            string errorMessage = string.Empty;

            _client.AuthPost("token?grant_type=password", body,
                json =>
                {
                    success = HandleAuthResponse(json);
                    if (!success) errorMessage = "Failed to parse auth response.";
                    done = true;
                },
                (code, error) =>
                {
                    errorMessage = ParseErrorMessage(error);
                    done = true;
                });

            while (!done) yield return null;

            callback?.Invoke(success, errorMessage);
        }

        // ──────────────────────── Sign Up with Email ────────────────────────

        public void SignUpWithEmail(string email, string password, string displayName, Action<bool, string> callback)
        {
            if (_client == null || !_client.IsConfigured)
            {
                callback?.Invoke(false, "Supabase is not configured.");
                return;
            }

            StartCoroutine(SignUpWithEmailCoroutine(email, password, displayName, callback));
        }

        private IEnumerator SignUpWithEmailCoroutine(string email, string password, string displayName, Action<bool, string> callback)
        {
            string safeName = EscapeJson(string.IsNullOrWhiteSpace(displayName) ? "Gakusei" : displayName);
            string body = $"{{\"email\":\"{EscapeJson(email)}\",\"password\":\"{EscapeJson(password)}\",\"data\":{{\"display_name\":\"{safeName}\"}}}}";

            bool done = false;
            bool success = false;
            string errorMessage = string.Empty;

            _client.AuthPost("signup", body,
                json =>
                {
                    success = HandleAuthResponse(json);
                    if (!success && IsEmailConfirmationPending(json))
                    {
                        success = true;
                        errorMessage = "EMAIL_CONFIRMATION_REQUIRED";
                    }
                    if (success && !string.IsNullOrWhiteSpace(displayName))
                    {
                        DisplayName = displayName;
                        PlayerPrefs.SetString(PrefKeyDisplayName, DisplayName);
                    }
                    if (!success) errorMessage = "Supabase did not return a valid account.";
                    done = true;
                },
                (code, error) =>
                {
                    errorMessage = ParseErrorMessage(error);
                    done = true;
                });

            while (!done) yield return null;

            callback?.Invoke(success, errorMessage);
        }

        private static bool IsEmailConfirmationPending(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var response = JsonUtility.FromJson<SupabaseClient.SupabaseSessionResponse>(json);
                return response != null && response.user != null &&
                       !string.IsNullOrWhiteSpace(response.user.id) &&
                       string.IsNullOrWhiteSpace(response.access_token);
            }
            catch
            {
                return false;
            }
        }

        // ──────────────────────── Sign Out ────────────────────────

        public void SignOut()
        {
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
                _refreshCoroutine = null;
            }

            IsAuthenticated = false;
            UserId = string.Empty;
            AccessToken = string.Empty;
            DisplayName = string.Empty;
            Email = string.Empty;

            _client?.ClearSession();

            PlayerPrefs.DeleteKey(PrefKeyAccessToken);
            PlayerPrefs.DeleteKey(PrefKeyRefreshToken);
            PlayerPrefs.DeleteKey(PrefKeyUserId);
            PlayerPrefs.DeleteKey(PrefKeyDisplayName);
            PlayerPrefs.DeleteKey(PrefKeyEmail);
            PlayerPrefs.Save();

            OnAuthStateChanged?.Invoke(AuthState.SignedOut);
            Debug.Log("[SupabaseAuth] Signed out.");
        }

        // ──────────────────────── Restore Session ────────────────────────

        public void TryRestoreSession(Action<bool> callback)
        {
            string savedAccessToken = PlayerPrefs.GetString(PrefKeyAccessToken, string.Empty);
            string savedRefreshToken = PlayerPrefs.GetString(PrefKeyRefreshToken, string.Empty);

            if (string.IsNullOrWhiteSpace(savedRefreshToken))
            {
                Debug.Log("[SupabaseAuth] No saved session to restore.");
                callback?.Invoke(false);
                return;
            }

            if (_client == null || !_client.IsConfigured)
            {
                // Offline — restore locally
                UserId = PlayerPrefs.GetString(PrefKeyUserId, string.Empty);
                DisplayName = PlayerPrefs.GetString(PrefKeyDisplayName, "Guest");
                Email = PlayerPrefs.GetString(PrefKeyEmail, string.Empty);
                AccessToken = savedAccessToken;

                if (!string.IsNullOrWhiteSpace(UserId))
                {
                    IsAuthenticated = true;
                    OnAuthStateChanged?.Invoke(AuthState.SignedIn);
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
                return;
            }

            // Try to refresh the token
            _client.SetSession(savedAccessToken, savedRefreshToken);
            StartCoroutine(RestoreSessionCoroutine(callback));
        }

        private IEnumerator RestoreSessionCoroutine(Action<bool> callback)
        {
            bool done = false;
            bool success = false;

            _client.RefreshSession(result =>
            {
                if (result)
                {
                    UserId = PlayerPrefs.GetString(PrefKeyUserId, string.Empty);
                    DisplayName = PlayerPrefs.GetString(PrefKeyDisplayName, "Gakusei");
                    Email = PlayerPrefs.GetString(PrefKeyEmail, string.Empty);
                    AccessToken = _client.AccessToken;

                    // Save refreshed tokens
                    PlayerPrefs.SetString(PrefKeyAccessToken, _client.AccessToken);
                    PlayerPrefs.SetString(PrefKeyRefreshToken, _client.RefreshToken);
                    PlayerPrefs.Save();

                    IsAuthenticated = true;
                    success = true;

                    OnAuthStateChanged?.Invoke(AuthState.TokenRefreshed);
                    StartTokenRefreshLoop();
                    Debug.Log($"[SupabaseAuth] Session restored for {DisplayName}.");
                }
                else
                {
                    Debug.Log("[SupabaseAuth] Failed to restore session — token expired.");
                    SignOut();
                }

                done = true;
            });

            while (!done) yield return null;
            callback?.Invoke(success);
        }

        // ──────────────────────── Internal ────────────────────────

        private bool HandleAuthResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var session = JsonUtility.FromJson<SupabaseClient.SupabaseSessionResponse>(json);
                if (session == null || string.IsNullOrWhiteSpace(session.access_token)) return false;

                AccessToken = session.access_token;
                _client.SetSession(session.access_token, session.refresh_token);

                if (session.user != null)
                {
                    UserId = session.user.id ?? string.Empty;
                    Email = session.user.email ?? string.Empty;
                    DisplayName = session.user.user_metadata?.display_name ?? "Gakusei";
                }

                IsAuthenticated = true;

                // Persist
                PlayerPrefs.SetString(PrefKeyAccessToken, session.access_token);
                PlayerPrefs.SetString(PrefKeyRefreshToken, session.refresh_token ?? string.Empty);
                PlayerPrefs.SetString(PrefKeyUserId, UserId);
                PlayerPrefs.SetString(PrefKeyDisplayName, DisplayName);
                PlayerPrefs.SetString(PrefKeyEmail, Email);
                PlayerPrefs.Save();

                OnAuthStateChanged?.Invoke(AuthState.SignedIn);
                StartTokenRefreshLoop();

                Debug.Log($"[SupabaseAuth] Authenticated as {DisplayName} ({UserId})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseAuth] Failed to parse auth response: {ex.Message}");
                return false;
            }
        }

        private void SetLocalSession(string playerId, string displayName, string email)
        {
            UserId = playerId;
            DisplayName = displayName;
            Email = email;
            AccessToken = string.Empty;
            IsAuthenticated = true;

            PlayerPrefs.SetString(PrefKeyUserId, UserId);
            PlayerPrefs.SetString(PrefKeyDisplayName, DisplayName);
            PlayerPrefs.SetString(PrefKeyEmail, Email);
            PlayerPrefs.Save();

            OnAuthStateChanged?.Invoke(AuthState.SignedIn);
            Debug.Log($"[SupabaseAuth] Local session created for {DisplayName}.");
        }

        private void StartTokenRefreshLoop()
        {
            if (_refreshCoroutine != null) StopCoroutine(_refreshCoroutine);
            _refreshCoroutine = StartCoroutine(TokenRefreshLoop());
        }

        private IEnumerator TokenRefreshLoop()
        {
            while (IsAuthenticated && _client != null && _client.IsConfigured)
            {
                yield return new WaitForSecondsRealtime(TokenRefreshIntervalSeconds);

                if (!IsAuthenticated) yield break;

                bool done = false;
                _client.RefreshSession(success =>
                {
                    if (success)
                    {
                        AccessToken = _client.AccessToken;
                        PlayerPrefs.SetString(PrefKeyAccessToken, _client.AccessToken);
                        PlayerPrefs.SetString(PrefKeyRefreshToken, _client.RefreshToken);
                        PlayerPrefs.Save();
                        OnAuthStateChanged?.Invoke(AuthState.TokenRefreshed);
                    }
                    else
                    {
                        Debug.LogWarning("[SupabaseAuth] Token refresh failed. Session may expire.");
                        OnAuthStateChanged?.Invoke(AuthState.SessionExpired);
                    }
                    done = true;
                });

                while (!done) yield return null;
            }
        }

        private static string ParseErrorMessage(string errorJson)
        {
            if (string.IsNullOrWhiteSpace(errorJson)) return "Unknown error";

            try
            {
                var err = JsonUtility.FromJson<SupabaseClient.SupabaseErrorResponse>(errorJson);
                if (err != null)
                {
                    if (!string.IsNullOrWhiteSpace(err.msg)) return err.msg;
                    if (!string.IsNullOrWhiteSpace(err.error_description)) return err.error_description;
                    if (!string.IsNullOrWhiteSpace(err.error)) return err.error;
                }
            }
            catch { }

            return errorJson.Length > 200 ? errorJson.Substring(0, 200) : errorJson;
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
