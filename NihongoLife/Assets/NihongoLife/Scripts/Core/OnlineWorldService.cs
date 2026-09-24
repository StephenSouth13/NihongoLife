using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    [Serializable]
    public class OnlinePlayerSnapshot
    {
        public string playerId;
        public string displayName;
        public string sceneName;
        public string roomId;
        public Vector3 position;
        public Quaternion rotation;
        public long serverTick;
    }

    [Serializable]
    public class OnlineChatMessage
    {
        public string messageId;
        public string senderPlayerId;
        public string senderDisplayName;
        public string channelId;
        public string text;
        public long sentAtUtcTicks;
    }

    public interface IOnlineWorldService : IGameService
    {
        bool IsConnected { get; }
        IReadOnlyList<OnlinePlayerSnapshot> VisiblePlayers { get; }
        IReadOnlyList<OnlineChatMessage> ChatHistory { get; }

        event Action<OnlinePlayerSnapshot> OnPlayerJoinedOrUpdated;
        event Action<string> OnPlayerLeft;
        event Action<OnlineChatMessage> OnChatMessageReceived;

        void ConnectLocalPlayer(string playerId, string displayName);
        void Disconnect();
        void UpdateLocalPlayerPose(string sceneName, Vector3 position, Quaternion rotation);
        void SendChatMessage(string channelId, string text);
    }

    public class LocalOnlineWorldService : IOnlineWorldService
    {
        private readonly List<OnlinePlayerSnapshot> _visiblePlayers = new List<OnlinePlayerSnapshot>();
        private readonly List<OnlineChatMessage> _chatHistory = new List<OnlineChatMessage>();

        private string _localPlayerId;
        private string _localDisplayName;
        private int _chatHistoryLimit = 80;

        public bool IsConnected { get; private set; }
        public IReadOnlyList<OnlinePlayerSnapshot> VisiblePlayers => _visiblePlayers;
        public IReadOnlyList<OnlineChatMessage> ChatHistory => _chatHistory;

        public event Action<OnlinePlayerSnapshot> OnPlayerJoinedOrUpdated;
        public event Action<string> OnPlayerLeft;
        public event Action<OnlineChatMessage> OnChatMessageReceived;

        public void Initialize()
        {
            if (GameServices.TryGet(out GameControlService control) && control.Database != null)
            {
                _chatHistoryLimit = Mathf.Max(10, control.Database.chatHistoryLimit);
            }
        }

        public void ConnectLocalPlayer(string playerId, string displayName)
        {
            _localPlayerId = string.IsNullOrWhiteSpace(playerId) ? Guid.NewGuid().ToString("N") : playerId;
            _localDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Learner" : displayName;
            IsConnected = true;

            UpdateLocalPlayerPose(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, Vector3.zero, Quaternion.identity);
            SendSystemMessage("town", $"{_localDisplayName} joined the local simulation.");
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            string leavingId = _localPlayerId;
            IsConnected = false;
            _visiblePlayers.RemoveAll(p => p.playerId == leavingId);
            OnPlayerLeft?.Invoke(leavingId);
        }

        public void UpdateLocalPlayerPose(string sceneName, Vector3 position, Quaternion rotation)
        {
            if (!IsConnected) return;

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
        }

        private static string RoomIdFor(string sceneName, string playerId)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) && sceneName.IndexOf("Bedroom", StringComparison.OrdinalIgnoreCase) >= 0)
                return "bedroom_" + (string.IsNullOrWhiteSpace(playerId) ? "guest" : playerId);
            return sceneName ?? string.Empty;
        }

        public void SendChatMessage(string channelId, string text)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(text)) return;

            AddChatMessage(new OnlineChatMessage
            {
                messageId = Guid.NewGuid().ToString("N"),
                senderPlayerId = _localPlayerId,
                senderDisplayName = _localDisplayName,
                channelId = string.IsNullOrWhiteSpace(channelId) ? "town" : channelId,
                text = text.Trim(),
                sentAtUtcTicks = DateTime.UtcNow.Ticks
            });
        }

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
    }
}
