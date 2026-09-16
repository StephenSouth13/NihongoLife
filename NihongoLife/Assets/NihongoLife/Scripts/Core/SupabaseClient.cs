using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NihongoLife.Core
{
    /// <summary>
    /// Central client for all Supabase REST API communication.
    /// Attach to the AppRoot GameObject — lives for the entire session.
    /// </summary>
    public class SupabaseClient : MonoBehaviour
    {
        public string ProjectUrl { get; private set; } = string.Empty;
        public string AnonKey { get; private set; } = string.Empty;
        public string AccessToken { get; private set; } = string.Empty;
        public string RefreshToken { get; private set; } = string.Empty;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ProjectUrl) && !string.IsNullOrWhiteSpace(AnonKey);

        // ──────────────────────── Init ────────────────────────

        public void Initialize(GameControlDatabase database)
        {
            if (database == null) return;
            ProjectUrl = (database.supabaseProjectUrl ?? string.Empty).TrimEnd('/');
            AnonKey = database.supabaseAnonKey ?? string.Empty;

            if (!IsConfigured)
            {
                Debug.LogWarning("[SupabaseClient] Project URL or Anon Key is empty. Online features will be unavailable.");
            }
            else
            {
                Debug.Log($"[SupabaseClient] Configured for {ProjectUrl}");
            }
        }

        public void SetSession(string accessToken, string refreshToken)
        {
            AccessToken = accessToken ?? string.Empty;
            RefreshToken = refreshToken ?? string.Empty;
        }

        public void ClearSession()
        {
            AccessToken = string.Empty;
            RefreshToken = string.Empty;
        }

        // ──────────────────────── URL builders ────────────────────────

        public string RestUrl(string table) => $"{ProjectUrl}/rest/v1/{table}";
        public string AuthUrl(string path) => $"{ProjectUrl}/auth/v1/{path}";
        public string StorageUrl(string path) => $"{ProjectUrl}/storage/v1/{path}";
        public string RealtimeUrl => ProjectUrl.Replace("https://", "wss://").Replace("http://", "ws://") + "/realtime/v1/websocket?apikey=" + AnonKey + "&vsn=1.0.0";

        // ──────────────────────── REST helpers ────────────────────────

        /// <summary>GET request to Supabase REST API.</summary>
        public Coroutine Get(string url, Action<string> onSuccess, Action<long, string> onError = null)
        {
            return StartCoroutine(RequestCoroutine("GET", url, null, onSuccess, onError));
        }

        /// <summary>POST request with JSON body.</summary>
        public Coroutine Post(string url, string jsonBody, Action<string> onSuccess, Action<long, string> onError = null)
        {
            return StartCoroutine(RequestCoroutine("POST", url, jsonBody, onSuccess, onError));
        }

        /// <summary>PATCH request with JSON body.</summary>
        public Coroutine Patch(string url, string jsonBody, Action<string> onSuccess, Action<long, string> onError = null)
        {
            return StartCoroutine(RequestCoroutine("PATCH", url, jsonBody, onSuccess, onError));
        }

        /// <summary>DELETE request.</summary>
        public Coroutine Delete(string url, Action<string> onSuccess, Action<long, string> onError = null)
        {
            return StartCoroutine(RequestCoroutine("DELETE", url, null, onSuccess, onError));
        }

        /// <summary>
        /// UPSERT — POST with Prefer: resolution=merge-duplicates.
        /// Supabase will insert or update based on unique constraints.
        /// </summary>
        public Coroutine Upsert(string url, string jsonBody, Action<string> onSuccess, Action<long, string> onError = null)
        {
            return StartCoroutine(RequestCoroutine("POST", url, jsonBody, onSuccess, onError, upsert: true));
        }

        // ──────────────────────── Auth helpers ────────────────────────

        /// <summary>POST to Supabase Auth endpoint (no Bearer token, only apikey).</summary>
        public Coroutine AuthPost(string path, string jsonBody, Action<string> onSuccess, Action<long, string> onError = null)
        {
            return StartCoroutine(AuthRequestCoroutine(path, jsonBody, onSuccess, onError));
        }

        // ──────────────────────── Core request implementation ────────────────────────

        private IEnumerator RequestCoroutine(string method, string url, string jsonBody, Action<string> onSuccess, Action<long, string> onError, bool upsert = false)
        {
            using (var request = new UnityWebRequest(url, method))
            {
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                }
                request.downloadHandler = new DownloadHandlerBuffer();

                // Standard headers
                request.SetRequestHeader("apikey", AnonKey);
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Prefer", upsert ? "resolution=merge-duplicates,return=representation" : "return=representation");

                // Auth header — use access token if available, otherwise anon key
                string bearer = !string.IsNullOrWhiteSpace(AccessToken) ? AccessToken : AnonKey;
                request.SetRequestHeader("Authorization", "Bearer " + bearer);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                }
                else
                {
                    string errorDetail = request.downloadHandler != null ? request.downloadHandler.text : request.error;
                    Debug.LogWarning($"[SupabaseClient] {method} {url} failed ({request.responseCode}): {errorDetail}");
                    onError?.Invoke(request.responseCode, errorDetail);
                }
            }
        }

        private IEnumerator AuthRequestCoroutine(string path, string jsonBody, Action<string> onSuccess, Action<long, string> onError)
        {
            string url = AuthUrl(path);
            using (var request = new UnityWebRequest(url, "POST"))
            {
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                }
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("apikey", AnonKey);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                }
                else
                {
                    string errorDetail = request.downloadHandler != null ? request.downloadHandler.text : request.error;
                    Debug.LogWarning($"[SupabaseClient] AUTH {path} failed ({request.responseCode}): {errorDetail}");
                    onError?.Invoke(request.responseCode, errorDetail);
                }
            }
        }

        // ──────────────────────── Token Refresh ────────────────────────

        /// <summary>Refresh the access token using the stored refresh token.</summary>
        public Coroutine RefreshSession(Action<bool> callback)
        {
            return StartCoroutine(RefreshSessionCoroutine(callback));
        }

        private IEnumerator RefreshSessionCoroutine(Action<bool> callback)
        {
            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                callback?.Invoke(false);
                yield break;
            }

            string body = $"{{\"refresh_token\":\"{RefreshToken}\"}}";
            bool success = false;

            yield return AuthPost("token?grant_type=refresh_token", body,
                json =>
                {
                    var session = JsonUtility.FromJson<SupabaseSessionResponse>(json);
                    if (session != null && !string.IsNullOrWhiteSpace(session.access_token))
                    {
                        SetSession(session.access_token, session.refresh_token);
                        success = true;
                        Debug.Log("[SupabaseClient] Session refreshed successfully.");
                    }
                },
                (code, error) =>
                {
                    Debug.LogWarning($"[SupabaseClient] Token refresh failed: {error}");
                });

            callback?.Invoke(success);
        }

        // ──────────────────────── JSON helpers ────────────────────────

        [Serializable]
        public class SupabaseSessionResponse
        {
            public string access_token;
            public string token_type;
            public int expires_in;
            public string refresh_token;
            public SupabaseUser user;
        }

        [Serializable]
        public class SupabaseUser
        {
            public string id;
            public string email;
            public string role;
            public SupabaseUserMetadata user_metadata;
        }

        [Serializable]
        public class SupabaseUserMetadata
        {
            public string display_name;
        }

        [Serializable]
        public class SupabaseErrorResponse
        {
            public string error;
            public string error_description;
            public string msg;
            public int code;
        }
    }
}
