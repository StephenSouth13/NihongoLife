using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>
    /// Leaderboard panel — shows top players with rank, name, XP, scenarios completed, and average score.
    /// Built entirely in code, accessible from MainMenu and HUD.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        private GameObject _panel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _contentText;
        private TextMeshProUGUI _playerRankText;
        private Button _closeButton;
        private Button _refreshButton;
        private TMP_FontAsset _font;

        public void Initialize(TMP_FontAsset font)
        {
            if (GameServices.TryGet(out GameSettingsService languageSettings)) languageSettings.OnLanguageChanged += HandleGlobalLanguageChanged;

            _font = font;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (GameServices.TryGet(out GameSettingsService settings)) settings.OnLanguageChanged -= HandleGlobalLanguageChanged;
        }

        private void HandleGlobalLanguageChanged(GameLanguage _)
        {
            if (_panel != null && _panel.activeSelf) RefreshContent();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            RefreshContent();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void BuildUI()
        {
            _panel = new GameObject("LeaderboardPanel");
            _panel.transform.SetParent(transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.015f, 0.02f, 0.92f);
            bg.raycastTarget = true;

            // Card
            var card = CreateChild(_panel, "Card");
            var cardRect = card.AddComponent<RectTransform>();
            Center(cardRect, new Vector2(600f, 560f));
            card.AddComponent<Image>().color = new Color(0.035f, 0.045f, 0.055f, 0.97f);

            // Title
            _titleText = CreateText(card, "Title", new Vector2(0f, 240f), new Vector2(540f, 42f), 26f);
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = new Color(1f, 0.91f, 0.54f, 1f);

            // Header row
            var header = CreateText(card, "Header", new Vector2(0f, 198f), new Vector2(540f, 28f), 14f);
            header.alignment = TextAlignmentOptions.Left;
            header.color = new Color(0.58f, 0.64f, 0.7f, 1f);

            // Content
            _contentText = CreateText(card, "Content", new Vector2(0f, 20f), new Vector2(540f, 340f), 15f);
            _contentText.alignment = TextAlignmentOptions.TopLeft;
            _contentText.color = new Color(0.88f, 0.93f, 1f, 1f);
            _contentText.lineSpacing = 6f;

            // Player rank
            _playerRankText = CreateText(card, "PlayerRank", new Vector2(0f, -192f), new Vector2(540f, 30f), 15f);
            _playerRankText.color = new Color(0.45f, 0.84f, 0.5f, 1f);
            _playerRankText.fontStyle = FontStyles.Bold;

            // Buttons
            _closeButton = CreateSimpleButton(card, "Close", new Vector2(250f, 240f), new Vector2(40f, 40f), "X");
            _closeButton.onClick.AddListener(Hide);

            _refreshButton = CreateSimpleButton(card, "Refresh", new Vector2(0f, -234f), new Vector2(160f, 42f), Text("Làm mới", "Refresh", "更新"));
            _refreshButton.onClick.AddListener(RefreshContent);

            _panel.SetActive(false);
        }

        private void RefreshContent()
        {
            _titleText.text = Text("Bảng xếp hạng", "Leaderboard", "ランキング");

            var leaderboard = FindFirstObjectByType<LeaderboardService>();
            if (leaderboard == null)
            {
                _contentText.text = Text("Chưa kết nối online.", "Not connected online.", "オンラインに接続されていません。");
                _playerRankText.text = string.Empty;
                return;
            }

            _contentText.text = Text("Đang tải...", "Loading...", "読み込み中...");

            leaderboard.FetchTopPlayers(30, entries =>
            {
                if (entries == null || entries.Count == 0)
                {
                    _contentText.text = Text("Chưa có dữ liệu.", "No data yet.", "データがありません。");
                    _playerRankText.text = string.Empty;
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"<color=#8fa3b8>{"#",-4} {"Name",-18} {"XP",-8} {"Done",-6} {"Avg"}</color>");
                sb.AppendLine();

                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    bool isMe = GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated && e.user_id == auth.UserId;
                    string color = isMe ? "#f1c75b" : "#e8ecf0";
                    string name = e.display_name.Length > 16 ? e.display_name.Substring(0, 16) + "…" : e.display_name;

                    sb.AppendLine($"<color={color}>{(i + 1),-4} {name,-18} {e.total_xp,-8} {e.scenarios_completed,-6} {e.average_score:F0}</color>");
                }

                _contentText.text = sb.ToString();

                int rank = leaderboard.PlayerRank;
                _playerRankText.text = rank > 0
                    ? Text($"Xếp hạng của bạn: #{rank}", $"Your rank: #{rank}", $"あなたの順位: #{rank}")
                    : Text("Bạn chưa có trên bảng xếp hạng", "You're not on the leaderboard yet", "まだランキングに載っていません");
            });
        }

        // ──────────────────────── UI Helpers ────────────────────────

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private TextMeshProUGUI CreateText(GameObject parent, string name, Vector2 pos, Vector2 size, float fontSize)
        {
            var go = CreateChild(parent, name);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.6f;
            text.fontSizeMax = fontSize;
            return text;
        }

        private Button CreateSimpleButton(GameObject parent, string name, Vector2 pos, Vector2 size, string label)
        {
            var go = CreateChild(parent, name);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            go.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);
            var btn = go.AddComponent<Button>();
            go.AddComponent<UIHoverScale>();

            var txt = CreateText(go, "Label", Vector2.zero, new Vector2(size.x - 10f, size.y - 6f), 16f);
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
