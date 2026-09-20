using System;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.UI;

namespace NihongoLife.Scenario
{
    /// <summary>
    /// Controls co-op scenario flow: both players see the same scene, dialogue nodes are synced, and the
    /// two players take turns choosing the learner's answer (host first). Each side sees the partner's
    /// choice and its consequences, so both practise the same phrases together.
    /// </summary>
    public class CoopScenarioController : MonoBehaviour
    {
        private CoopSessionService _sessionService;
        private string _myUserId;
        private string _myAssignedSpeaker;
        private string _currentNodeId;
        private ScenarioDefinition _scenario;
        private int _choiceCount;

        private const string TurnNoticeId = "coop.turn";

        public bool IsCoopActive => _sessionService != null && _sessionService.InSession && _sessionService.CurrentSession.status == "active";
        public string MyAssignedSpeaker => _myAssignedSpeaker;

        public event Action<string> OnNodeSyncReceived; // (nodeId) when partner advances
        public event Action<int> OnPartnerChoseOption; // (choiceIndex) when partner selects a dialogue choice

        // ──────────────────────── Setup ────────────────────────

        /// <summary>Creates (or reuses) the controller when a co-op session is running and binds it to the scenario.</summary>
        public static CoopScenarioController EnsureFor(ScenarioDefinition scenario, GameObject host)
        {
            var session = FindFirstObjectByType<CoopSessionService>();
            if (session == null || !session.InSession) return null;

            var controller = FindFirstObjectByType<CoopScenarioController>();
            if (controller == null) controller = host.AddComponent<CoopScenarioController>();
            controller.Initialize(scenario);
            return controller;
        }

        public void Initialize(ScenarioDefinition scenario)
        {
            Detach();
            _choiceCount = 0;
            _scenario = scenario;
            _sessionService = FindFirstObjectByType<CoopSessionService>();

            if (GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated)
            {
                _myUserId = auth.UserId;
            }

            if (_sessionService != null)
            {
                _sessionService.OnCoopBroadcast += HandleCoopBroadcast;
                _sessionService.OnParticipantsUpdated += HandleParticipantsUpdated;
                _sessionService.OnSessionEnded += HandleSessionEnded;

                // Find my assigned speaker
                UpdateMyRole();
            }

            Debug.Log($"[CoopScenario] Initialized for scenario '{scenario?.id}'. Co-op active: {IsCoopActive}");
            PublishTurn();
        }

        private void OnDestroy() => Detach();

        private void Detach()
        {
            if (_sessionService != null)
            {
                _sessionService.OnCoopBroadcast -= HandleCoopBroadcast;
                _sessionService.OnParticipantsUpdated -= HandleParticipantsUpdated;
                _sessionService.OnSessionEnded -= HandleSessionEnded;
            }

            HudNotificationTray.Instance?.Clear(TurnNoticeId);
        }

        // ──────────────────────── Turn Logic ────────────────────────

        /// <summary>Check if the current dialogue node's speaker matches this player's assigned role.</summary>
        public bool IsMyTurnToSpeak(ScenarioNode node)
        {
            if (!IsCoopActive || string.IsNullOrWhiteSpace(_myAssignedSpeaker)) return true; // Solo fallback

            // If node has a speakerId and it matches my assigned speaker, it's my turn
            if (!string.IsNullOrWhiteSpace(node.speakerId))
            {
                return node.speakerId == _myAssignedSpeaker;
            }

            // If node has choices, all players can see but only the assigned speaker can choose
            return string.IsNullOrWhiteSpace(node.speakerName) || true; // Default: allow
        }

        /// <summary>True when it is this player's turn to choose the learner's answer (host answers first, then alternate).</summary>
        public bool IsMyTurnToChoose
        {
            get
            {
                if (!IsCoopActive || _sessionService == null) return true;
                bool hostTurn = _choiceCount % 2 == 0;
                return hostTurn == _sessionService.IsHost;
            }
        }

        /// <summary>Check if this player can select a choice for the current node.</summary>
        public bool CanSelectChoice(ScenarioNode node)
        {
            return IsMyTurnToChoose;
        }

        private void PublishTurn()
        {
            var tray = HudNotificationTray.Instance;
            if (tray == null) return;

            if (!IsCoopActive)
            {
                tray.Clear(TurnNoticeId);
                return;
            }

            bool mine = IsMyTurnToChoose;
            tray.Post(TurnNoticeId,
                "Co-op",
                mine ? Pick("Đến lượt bạn chọn câu trả lời", "Your turn to answer", "あなたの番です") : Pick("Đang chờ bạn cùng chơi chọn...", "Waiting for your partner...", "相手の番です..."),
                null,
                mine ? HudNoticeTone.Warning : HudNoticeTone.Info,
                mine);
        }

        private static string Pick(string vi, string en, string ja)
        {
            GameLanguage language = GameServices.TryGet(out GameSettingsService settings) ? settings.Language : GameLanguage.Vietnamese;
            return language == GameLanguage.English ? en : language == GameLanguage.Japanese ? ja : vi;
        }

        // ──────────────────────── Broadcast Events ────────────────────────

        /// <summary>Broadcast that this player has advanced to a new node.</summary>
        public void BroadcastNodeAdvance(string nodeId)
        {
            if (!IsCoopActive) return;
            _currentNodeId = nodeId;

            string payload = $"{{\"type\":\"node_advance\",\"node_id\":\"{nodeId}\",\"sender\":\"{_myUserId}\"}}";
            _sessionService.BroadcastCoopEvent("dialogue", payload);
        }

        /// <summary>Broadcast that this player has selected a dialogue choice.</summary>
        public void BroadcastChoiceSelected(int choiceIndex, string nextNodeId)
        {
            if (!IsCoopActive) return;
            _choiceCount++;
            PublishTurn();

            string payload = $"{{\"type\":\"choice_selected\",\"choice_index\":{choiceIndex},\"next_node_id\":\"{nextNodeId}\",\"sender\":\"{_myUserId}\"}}";
            _sessionService.BroadcastCoopEvent("dialogue", payload);
        }

        /// <summary>Broadcast that the scenario is complete.</summary>
        public void BroadcastScenarioComplete(int score)
        {
            if (!IsCoopActive) return;

            string payload = $"{{\"type\":\"scenario_complete\",\"score\":{score},\"sender\":\"{_myUserId}\"}}";
            _sessionService.BroadcastCoopEvent("dialogue", payload);
        }

        // ──────────────────────── Handle Incoming ────────────────────────

        private void HandleCoopBroadcast(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return;

            try
            {
                var msg = JsonUtility.FromJson<CoopDialogueMessage>(payloadJson);
                if (msg == null || msg.sender == _myUserId) return; // Ignore own messages

                switch (msg.type)
                {
                    case "node_advance":
                        _currentNodeId = msg.node_id;
                        OnNodeSyncReceived?.Invoke(msg.node_id);
                        break;

                    case "choice_selected":
                        _choiceCount++;
                        OnPartnerChoseOption?.Invoke(msg.choice_index);
                        PublishTurn();
                        break;

                    case "scenario_complete":
                        Debug.Log($"[CoopScenario] Partner completed scenario with score: {msg.score}");
                        HudNotificationTray.Instance?.Clear(TurnNoticeId);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CoopScenario] Broadcast parse error: {ex.Message}");
            }
        }

        private void HandleParticipantsUpdated(List<CoopParticipant> participants)
        {
            UpdateMyRole();
        }

        private void HandleSessionEnded()
        {
            _myAssignedSpeaker = null;
            HudNotificationTray.Instance?.Clear(TurnNoticeId);
            Debug.Log("[CoopScenario] Session ended — reverting to solo mode.");
        }

        private void UpdateMyRole()
        {
            if (_sessionService == null || string.IsNullOrWhiteSpace(_myUserId)) return;

            foreach (var p in _sessionService.Participants)
            {
                if (p.user_id == _myUserId)
                {
                    _myAssignedSpeaker = p.assigned_speaker;
                    Debug.Log($"[CoopScenario] My role: {_myAssignedSpeaker ?? "unassigned"}");
                    return;
                }
            }
        }

        // ──────────────────────── DTO ────────────────────────

        [Serializable]
        private class CoopDialogueMessage
        {
            public string type;
            public string node_id;
            public int choice_index;
            public string next_node_id;
            public int score;
            public string sender;
        }
    }
}
