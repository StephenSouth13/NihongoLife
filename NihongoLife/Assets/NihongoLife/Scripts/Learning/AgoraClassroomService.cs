using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using NihongoLife.Core;

namespace NihongoLife.Learning
{
    [Serializable]
    public class AgoraWaitingRoom
    {
        public string id;
        public string student_id;
        public string student_name;
        public string teacher_id;
        public string teacher_name;
        public string channel_name;
        public string status;
        public string created_at;
        public string avatar_url;
        public string bio;
        public int level;
        public int xp;
        public string creator_role;
        public string created_by;
    }

    [Serializable]
    public class AgoraJoinRequest
    {
        public string id, room_id, student_id, student_name, avatar_url, bio, status, created_at;
        public int level, xp;
    }

    [Serializable]
    public class AgoraClassroomResponse
    {
        public bool ok;
        public string error;
        public string role;
        public AgoraWaitingRoom room;
        public AgoraWaitingRoom[] rooms;
        public AgoraJoinRequest[] requests;
        public string token;
        public string channelName;
        public uint uid;
        public int expiresIn;
    }

    /// <summary>Authenticated waiting-room and Agora-token client. The App Certificate never enters Unity.</summary>
    public class AgoraClassroomService : MonoBehaviour
    {
        private SupabaseClient _client;
        private IAuthService _auth;
        private string _endpoint;

        public bool IsAvailable => _client != null && _client.IsConfigured && _auth != null && _auth.IsAuthenticated;

        private void Awake()
        {
            _client = FindFirstObjectByType<SupabaseClient>();
            GameServices.TryGet(out _auth);
            var db = Resources.Load<GameControlDatabase>("Control/NihongoLifeControlDatabase");
            if (db != null && !string.IsNullOrWhiteSpace(db.agoraClassroomApiUrl))
                _endpoint = db.agoraClassroomApiUrl.Trim();
            else if (_client != null && _client.IsConfigured)
                _endpoint = _client.ProjectUrl.TrimEnd('/') + "/functions/v1/agora-classroom";
        }

        public void GetRole(Action<AgoraClassroomResponse> done) => Call("{\"action\":\"role\"}", done);
        public void CreateRoom(Action<AgoraClassroomResponse> done) => Call("{\"action\":\"create\"}", done);
        public void ListWaitingRooms(Action<AgoraClassroomResponse> done) => Call("{\"action\":\"list\"}", done);
        public void GetMyRoom(Action<AgoraClassroomResponse> done) => Call("{\"action\":\"mine\"}", done);
        public void ClaimRoom(string roomId, Action<AgoraClassroomResponse> done) =>
            Call($"{{\"action\":\"claim\",\"roomId\":\"{Escape(roomId)}\"}}", done);
        public void RequestJoin(string roomId, Action<AgoraClassroomResponse> done) =>
            Call($"{{\"action\":\"request_join\",\"roomId\":\"{Escape(roomId)}\"}}", done);
        public void ListJoinRequests(Action<AgoraClassroomResponse> done) => Call("{\"action\":\"requests\"}", done);
        public void ApproveRequest(string requestId, Action<AgoraClassroomResponse> done) =>
            Call($"{{\"action\":\"approve\",\"requestId\":\"{Escape(requestId)}\"}}", done);
        public void GetToken(string roomId, Action<AgoraClassroomResponse> done) =>
            Call($"{{\"action\":\"token\",\"roomId\":\"{Escape(roomId)}\"}}", done);
        public void CloseRoom(string roomId, Action<AgoraClassroomResponse> done) =>
            Call($"{{\"action\":\"close\",\"roomId\":\"{Escape(roomId)}\"}}", done);

        private void Call(string json, Action<AgoraClassroomResponse> done)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(_endpoint))
            {
                done?.Invoke(new AgoraClassroomResponse { ok = false, error = NihongoLife.Core.Loc.T("Cần đăng nhập và cấu hình Supabase để dùng phòng Speaking.", "Sign in and configure Supabase to use the Speaking room.", "スピーキングルームにはログインとSupabaseの設定が必要です。") });
                return;
            }
            StartCoroutine(Request(json, done));
        }

        private IEnumerator Request(string json, Action<AgoraClassroomResponse> done)
        {
            using (var request = new UnityWebRequest(_endpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", _client.AnonKey);
                request.SetRequestHeader("Authorization", "Bearer " + _auth.AccessToken);
                yield return request.SendWebRequest();

                AgoraClassroomResponse response = null;
                try { response = JsonUtility.FromJson<AgoraClassroomResponse>(request.downloadHandler.text); }
                catch (Exception ex) { Debug.LogWarning("[AgoraClassroom] Invalid response: " + ex.Message); }
                if (response == null) response = new AgoraClassroomResponse { ok = false, error = request.error ?? NihongoLife.Core.Loc.T("Phản hồi máy chủ không hợp lệ.", "Invalid server response.", "サーバーの応答が不正です。") };
                if (request.result != UnityWebRequest.Result.Success && string.IsNullOrWhiteSpace(response.error))
                    response.error = NihongoLife.Core.Loc.F("Máy chủ phòng học trả lỗi {0}.", "The classroom server returned error {0}.", "教室サーバーのエラー {0}。", request.responseCode);
                done?.Invoke(response);
            }
        }

        private static string Escape(string value) => (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
