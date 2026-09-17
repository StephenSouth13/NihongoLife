using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    /// <summary>
    /// Co-op lobby UI — browse available sessions, create new ones, assign roles, and start.
    /// </summary>
    public class CoopLobbyUI : MonoBehaviour
    {
        private GameObject _panel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _sessionListText;
        private TextMeshProUGUI _lobbyInfoText;
        private Button _createButton;
        private Button _refreshButton;
        private Button _startButton;
        private Button _leaveButton;
        private Button _closeButton;
        private TMP_FontAsset _font;
        private readonly List<Button> _sessionButtons = new List<Button>();

        public void Initialize(TMP_FontAsset font)
        {
            _font = font;
            BuildUI();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            SubscribeEvents();
            RefreshView();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void BuildUI()
        {
            _panel = new GameObject("CoopLobbyPanel");
            _panel.transform.SetParent(transform, false);
            var rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.92f);

            var card = new GameObject("Card");
            card.transform.SetParent(_panel.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            Center(cardRect, new Vector2(560f, 520f));
            card.AddComponent<Image>().color = new Color(0.035f, 0.045f, 0.055f, 0.97f);

            _titleText = MakeText(card, "Title", new Vector2(0f, 220f), new Vector2(480f, 38f), 24f);
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = new Color(1f, 0.91f, 0.54f, 1f);

            _sessionListText = MakeText(card, "Sessions", new Vector2(0f, 60f), new Vector2(500f, 240f), 14f);
            _sessionListText.alignment = TextAlignmentOptions.TopLeft;
            _sessionListText.color = new Color(0.88f, 0.93f, 1f, 1f);
            _sessionListText.lineSpacing = 6f;

            _lobbyInfoText = MakeText(card, "LobbyInfo", new Vector2(0f, -100f), new Vector2(500f, 100f), 14f);
            _lobbyInfoText.alignment = TextAlignmentOptions.TopLeft;
            _lobbyInfoText.color = new Color(0.74f, 0.84f, 0.5f, 1f);
            _lobbyInfoText.lineSpacing = 4f;

            // Buttons row
            _createButton = MakeButton(card, "CreateBtn", new Vector2(-140f, -200f), new Vector2(140f, 42f), Text("Tạo phòng", "Create", "作成"));
            _createButton.onClick.AddListener(OnCreateClicked);

            _refreshButton = MakeButton(card, "RefreshBtn", new Vector2(0f, -200f), new Vector2(100f, 42f), Text("Làm mới", "Refresh", "更新"));
            _refreshButton.onClick.AddListener(RefreshView);

            _startButton = MakeButton(card, "StartBtn", new Vector2(120f, -200f), new Vector2(120f, 42f), Text("Bắt đầu", "Start", "開始"));
            _startButton.onClick.AddListener(OnStartClicked);

            _leaveButton = MakeButton(card, "LeaveBtn", new Vector2(120f, -200f), new Vector2(120f, 42f), Text("Rời phòng", "Leave", "退出"));
            _leaveButton.onClick.AddListener(OnLeaveClicked);

            _closeButton = MakeButton(card, "CloseBtn", new Vector2(250f, 220f), new Vector2(40f, 40f), "✕");
            _closeButton.onClick.AddListener(Hide);

            _panel.SetActive(false);
        }

        private void RefreshView()
        {
            _titleText.text = Text("Học nhóm Co-op", "Co-op Learning", "協力学習");

            var coopService = FindFirstObjectByType<CoopSessionService>();
            if (coopService == null)
            {
                _sessionListText.text = Text("Co-op chưa khả dụng.", "Co-op not available.", "協力モードは利用できません。");
                _lobbyInfoText.text = string.Empty;
                ClearSessionButtons();
                SetActive(_createButton, false);
                SetActive(_refreshButton, true);
                SetActive(_startButton, false);
                SetActive(_leaveButton, false);
                return;
            }

            if (!GameServices.TryGet(out IAuthService auth) || !auth.IsAuthenticated)
            {
                _sessionListText.text = Text(
                    "Ban can dang nhap truoc khi tao hoac tham gia phong online.",
                    "Sign in before creating or joining an online room.",
                    "オンラインルームを作成・参加する前にログインしてください。");
                _lobbyInfoText.text = string.Empty;
                ClearSessionButtons();
                SetActive(_createButton, false);
                SetActive(_refreshButton, true);
                SetActive(_startButton, false);
                SetActive(_leaveButton, false);
                return;
            }

            if (coopService.InSession)
            {
                ShowCurrentSession(coopService);
            }
            else
            {
                ShowAvailableSessions(coopService);
            }
        }

        private void ShowCurrentSession(CoopSessionService service)
        {
            ClearSessionButtons();
            _sessionListText.text = string.Empty;
            SetActive(_createButton, false);
            SetActive(_refreshButton, false);
            SetActive(_startButton, service.IsHost && service.CurrentSession.status == "waiting");
            SetActive(_leaveButton, true);

            var sb = new StringBuilder();
            sb.AppendLine($"<b>{Text("Phòng hiện tại", "Current Session", "現在のセッション")}</b>");
            sb.AppendLine($"{Text("Kịch bản", "Scenario", "シナリオ")}: {service.CurrentSession.scenario_id}");
            sb.AppendLine($"{Text("Trạng thái", "Status", "状態")}: {service.CurrentSession.status}");
            sb.AppendLine();
            sb.AppendLine($"<b>{Text("Người chơi", "Players", "プレイヤー")}</b>");

            foreach (var p in service.Participants)
            {
                string role = string.IsNullOrWhiteSpace(p.assigned_speaker) ? p.role : $"{p.role} → {p.assigned_speaker}";
                sb.AppendLine($"• {DisplayParticipantName(p)} ({role})");
            }

            _lobbyInfoText.text = sb.ToString();
        }

        private void ShowAvailableSessions(CoopSessionService service)
        {
            ClearSessionButtons();
            SetActive(_createButton, true);
            SetActive(_refreshButton, true);
            SetActive(_startButton, false);
            SetActive(_leaveButton, false);
            _lobbyInfoText.text = string.Empty;

            _sessionListText.text = Text("Đang tải phòng...", "Loading sessions...", "セッション読み込み中...");

            service.GetAvailableSessions(sessions =>
            {
                if (sessions.Count == 0)
                {
                    _sessionListText.text = Text("Chưa có phòng nào. Hãy tạo phòng mới!", "No sessions available. Create one!", "セッションがありません。作成してください！");
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"<b>{Text("Phòng đang chờ", "Waiting Sessions", "待機中セッション")}</b>\n");
                for (int i = 0; i < sessions.Count; i++)
                {
                    var s = sessions[i];
                    sb.AppendLine($"{i + 1}. {s.scenario_id} | {Text("Tối đa", "Max", "最大")}: {s.max_players}");
                }
                for (int i = 0; i < sessions.Count; i++)
                {
                    CreateSessionJoinButton(i, sessions[i]);
                }
                _sessionListText.text = sb.ToString();
            });
        }

        private void CreateSessionJoinButton(int index, CoopSession session)
        {
            if (_sessionListText == null || _sessionListText.transform.parent == null || session == null) return;

            var parent = _sessionListText.transform.parent.gameObject;
            var button = MakeButton(
                parent,
                $"JoinSessionBtn_{index}",
                new Vector2(190f, 145f - index * 32f),
                new Vector2(104f, 28f),
                Text("Vao phong", "Join", "参加"));
            button.onClick.AddListener(() => OnJoinClicked(session.id));
            _sessionButtons.Add(button);
        }

        private void ClearSessionButtons()
        {
            for (int i = 0; i < _sessionButtons.Count; i++)
            {
                if (_sessionButtons[i] != null)
                {
                    Destroy(_sessionButtons[i].gameObject);
                }
            }

            _sessionButtons.Clear();
        }

        // ──────────────────────── Actions ────────────────────────

        private void OnCreateClicked()
        {
            var coopService = FindFirstObjectByType<CoopSessionService>();
            if (coopService == null) return;

            // Use active scenario from GameControlDatabase
            string scenarioId = "scenario.konbini.buy_onigiri";
            if (GameServices.TryGet(out GameControlService control) && control.Database != null)
            {
                scenarioId = control.Database.activeScenarioId;
            }

            coopService.CreateSession(scenarioId, 2, success => RefreshView());
        }

        private void OnJoinClicked(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;

            var coopService = FindFirstObjectByType<CoopSessionService>();
            if (coopService == null) return;

            _lobbyInfoText.text = Text("Dang vao phong...", "Joining room...", "参加中...");
            coopService.JoinSession(sessionId, success => RefreshView());
        }

        private void OnStartClicked()
        {
            var coopService = FindFirstObjectByType<CoopSessionService>();
            coopService?.StartSession(_ => RefreshView());
        }

        private void OnLeaveClicked()
        {
            var coopService = FindFirstObjectByType<CoopSessionService>();
            coopService?.LeaveSession(() => RefreshView());
        }

        // ──────────────────────── Helpers ────────────────────────

        private void SubscribeEvents()
        {
            var service = FindFirstObjectByType<CoopSessionService>();
            if (service == null) return;

            service.OnSessionUpdated -= HandleSessionUpdated;
            service.OnSessionUpdated += HandleSessionUpdated;
            service.OnSessionEnded -= RefreshView;
            service.OnSessionEnded += RefreshView;
        }

        private void UnsubscribeEvents()
        {
            var service = FindFirstObjectByType<CoopSessionService>();
            if (service == null) return;

            service.OnSessionUpdated -= HandleSessionUpdated;
            service.OnSessionEnded -= RefreshView;
        }

        private void HandleSessionUpdated(CoopSession session)
        {
            RefreshView();
            if (session != null && session.status == "active")
            {
                LoadCoopGameplay(session.scenario_id);
            }
        }

        private static void LoadCoopGameplay(string scenarioId)
        {
            if (!string.IsNullOrWhiteSpace(scenarioId))
            {
                ScenarioSceneInitializer.QueueLaunch(scenarioId);
            }

            if (GameServices.TryGet(out SceneFlowController sceneFlow))
            {
                sceneFlow.LoadScene("90_TestSandbox");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("90_TestSandbox");
            }
        }

        private static string DisplayParticipantName(CoopParticipant participant)
        {
            if (participant == null) return "Player";
            if (!string.IsNullOrWhiteSpace(participant.displayName)) return participant.displayName;
            if (string.IsNullOrWhiteSpace(participant.user_id)) return "Player";
            return participant.user_id.Length <= 8 ? participant.user_id : participant.user_id.Substring(0, 8);
        }

        private static void SetActive(Component c, bool v)
        {
            if (c != null) c.gameObject.SetActive(v);
        }

        private static void Center(RectTransform r, Vector2 s)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = s;
        }

        private TextMeshProUGUI MakeText(GameObject p, string n, Vector2 pos, Vector2 size, float fs)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            var t = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = fs;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.enableAutoSizing = true;
            t.fontSizeMin = fs * 0.6f;
            t.fontSizeMax = fs;
            return t;
        }

        private Button MakeButton(GameObject p, string n, Vector2 pos, Vector2 size, string label)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);
            var btn = go.AddComponent<Button>();
            go.AddComponent<UIHoverScale>();
            var txt = MakeText(go, "Lbl", Vector2.zero, new Vector2(size.x - 10f, size.y - 6f), 15f);
            txt.text = label;
            txt.fontStyle = FontStyles.Bold;
            return btn;
        }

        private static string Text(string vi, string en, string ja)
        {
            return GameServices.TryGet(out GameSettingsService s) ? s.Text(vi, en, ja) : vi;
        }
    }
}
