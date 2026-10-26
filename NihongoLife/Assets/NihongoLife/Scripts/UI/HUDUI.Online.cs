using System.Linq;
using NihongoLife.Save;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    /// <summary>Partial of HUDUI: inventory, character, and chat visibility plus online status. The MonoBehaviour entry point stays HUDUI.cs.</summary>
    public partial class HUDUI : MonoBehaviour
    {
        private void SetInventoryVisible(bool visible)
        {
            if (inventoryPanel == null) return;
            inventoryPanel.SetActive(visible);
            if (visible)
            {
                _inventoryWindow?.Refresh();
                _worldMap?.SetVisible(false);
                SetCharacterVisible(false);
                SetChatVisible(false);
                RefreshPlayerPanels();
            }
            UpdateOverlayInputLock();
        }

        private void SetCharacterVisible(bool visible)
        {
            if (characterPanel == null) return;
            characterPanel.SetActive(visible);
            if (visible)
            {
                _statusDock?.Refresh();
                _worldMap?.SetVisible(false);
                SetInventoryVisible(false);
                SetChatVisible(false);
                RefreshPlayerPanels();
            }
            UpdateOverlayInputLock();
        }

        /// <summary>Enter opens the full chat window (SocialChatWindow: area, community, groups, private, friends);
        /// the old single-channel panel stays hidden.</summary>
        private void SetChatVisible(bool visible)
        {
            if (chatPanel != null && chatPanel.activeSelf) chatPanel.SetActive(false);
            if (visible)
            {
                _worldMap?.SetVisible(false);
                SetInventoryVisible(false);
                SetCharacterVisible(false);
                SocialChatWindow.Open();
            }
            else if (SocialChatWindow.IsOpen)
            {
                SocialChatWindow.Close();
            }
            UpdateOverlayInputLock();
        }

        private bool _socialWasOpen;

        private void UpdateOverlayInputLock()
        {
            bool overlayOpen = (inventoryPanel != null && inventoryPanel.activeSelf) ||
                               (characterPanel != null && characterPanel.activeSelf) ||
                               SocialChatWindow.IsOpen || SocialChatWindow.CardOpen ||
                               (_worldMap != null && _worldMap.IsVisible);
            bool dialogueOpen = dialoguePanel != null && dialoguePanel.activeSelf;
            ScenarioManager.Instance?.SetPlayerInputLocked(overlayOpen || dialogueOpen);
            Cursor.lockState = CursorLockMode.None; // Sims-style: the cursor stays free in gameplay
            Cursor.visible = true;
        }

        private void SendChatMessage(string message)
        {
            if (_onlineWorld == null || string.IsNullOrWhiteSpace(message)) return;

            _onlineWorld.SendChatMessage("town", message);
            if (chatInputField != null)
            {
                chatInputField.text = string.Empty;
                chatInputField.ActivateInputField();
            }
        }

        private void HandleChatMessageReceived(OnlineChatMessage message)
        {
            RefreshChatHistory();
        }

        private void HandlePlayerPresenceChanged(OnlinePlayerSnapshot player)
        {
            RefreshOnlineStatus();
        }

        private void HandlePlayerLeft(string playerId)
        {
            RefreshOnlineStatus();
        }

        private void RefreshOnlineStatus()
        {
            if (onlineStatusText == null) return;

            int onlineCount = _onlineWorld != null && _onlineWorld.IsConnected ? _onlineWorld.VisiblePlayers.Count : 0;
            bool isRealOnline = _onlineWorld is SupabaseOnlineWorldService;
            string modeLabel = isRealOnline ? "Online" : Text("Mô phỏng", "Simulation", "模擬");

            string status = _onlineWorld != null && _onlineWorld.IsConnected
                ? Text($"{modeLabel}: {onlineCount} người chơi  |  Enter: chat", $"{modeLabel}: {onlineCount} player(s)  |  Enter: chat", $"{modeLabel}: {onlineCount}人  |  Enter: チャット")
                : Text($"{modeLabel}: chưa kết nối", $"{modeLabel}: offline", $"{modeLabel}: オフライン");
            onlineStatusText.text = status;
            // The chat window (Enter) shows who is online and where; this corner line overlapped the city signs.
            onlineStatusText.gameObject.SetActive(false);
        }

        private void RefreshChatHistory()
        {
            if (chatHistoryText == null) return;

            if (_onlineWorld == null || _onlineWorld.ChatHistory.Count == 0)
            {
                chatHistoryText.text = Text("Chưa có tin nhắn.", "No messages yet.", "メッセージはまだありません。");
                return;
            }

            var builder = new StringBuilder();
            int start = Mathf.Max(0, _onlineWorld.ChatHistory.Count - 8);
            for (int i = start; i < _onlineWorld.ChatHistory.Count; i++)
            {
                OnlineChatMessage message = _onlineWorld.ChatHistory[i];
                builder.Append("<color=#f1c75b>")
                    .Append(message.senderDisplayName)
                    .Append("</color>: ")
                    .Append(message.text)
                    .AppendLine();
            }

            chatHistoryText.text = builder.ToString();
        }
    }
}
