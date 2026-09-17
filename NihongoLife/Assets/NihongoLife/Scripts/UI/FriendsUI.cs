using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>
    /// Friends panel — shows friends list, pending requests, and add friend functionality.
    /// </summary>
    public class FriendsUI : MonoBehaviour
    {
        private GameObject _panel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _friendsListText;
        private TextMeshProUGUI _pendingText;
        private TMP_InputField _addFriendInput;
        private Button _addButton;
        private Button _closeButton;
        private TMP_FontAsset _font;

        public void Initialize(TMP_FontAsset font)
        {
            _font = font;
            BuildUI();
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
            _panel = new GameObject("FriendsPanel");
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
            Center(cardRect, new Vector2(480f, 520f));
            card.AddComponent<Image>().color = new Color(0.035f, 0.045f, 0.055f, 0.97f);

            _titleText = MakeText(card, "Title", new Vector2(0f, 220f), new Vector2(420f, 38f), 24f);
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = new Color(1f, 0.91f, 0.54f, 1f);

            _friendsListText = MakeText(card, "FriendsList", new Vector2(0f, 60f), new Vector2(420f, 240f), 15f);
            _friendsListText.alignment = TextAlignmentOptions.TopLeft;
            _friendsListText.color = new Color(0.88f, 0.93f, 1f, 1f);
            _friendsListText.lineSpacing = 6f;

            _pendingText = MakeText(card, "Pending", new Vector2(0f, -100f), new Vector2(420f, 80f), 13f);
            _pendingText.alignment = TextAlignmentOptions.TopLeft;
            _pendingText.color = new Color(0.95f, 0.72f, 0.25f, 1f);

            // Add friend input
            var inputGo = new GameObject("AddInput");
            inputGo.transform.SetParent(card.transform, false);
            var inputRect = inputGo.AddComponent<RectTransform>();
            Center(inputRect, new Vector2(300f, 40f));
            inputRect.anchoredPosition = new Vector2(-40f, -190f);
            inputGo.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.13f, 1f);

            _addFriendInput = inputGo.AddComponent<TMP_InputField>();
            _addFriendInput.lineType = TMP_InputField.LineType.SingleLine;

            var inputText = MakeText(inputGo, "Text", Vector2.zero, new Vector2(280f, 32f), 14f);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            inputText.margin = new Vector4(10, 0, 10, 0);
            _addFriendInput.textComponent = inputText;
            _addFriendInput.textViewport = inputRect;

            var ph = MakeText(inputGo, "Placeholder", Vector2.zero, new Vector2(280f, 32f), 14f);
            ph.text = "User ID...";
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            ph.margin = new Vector4(10, 0, 10, 0);
            ph.color = new Color(0.5f, 0.55f, 0.6f, 0.7f);
            _addFriendInput.placeholder = ph;

            _addButton = MakeButton(card, "AddBtn", new Vector2(140f, -190f), new Vector2(80f, 40f), Text("Thêm", "Add", "追加"));
            _addButton.onClick.AddListener(OnAddFriend);

            _closeButton = MakeButton(card, "CloseBtn", new Vector2(210f, 220f), new Vector2(40f, 40f), "✕");
            _closeButton.onClick.AddListener(Hide);

            _panel.SetActive(false);
        }

        private void RefreshContent()
        {
            _titleText.text = Text("Bạn bè", "Friends", "フレンド");

            var friendService = FindFirstObjectByType<FriendService>();
            if (friendService == null)
            {
                _friendsListText.text = Text("Chưa kết nối.", "Not connected.", "接続されていません。");
                _pendingText.text = string.Empty;
                return;
            }

            friendService.RefreshFriends(() =>
            {
                // Friends list
                if (friendService.Friends.Count == 0)
                {
                    _friendsListText.text = Text("Chưa có bạn bè. Thêm bạn bằng User ID!", "No friends yet. Add someone by User ID!", "まだフレンドがいません。IDで追加しましょう！");
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"<b>{Text("Danh sách bạn bè", "Friends List", "フレンドリスト")}</b>\n");
                    foreach (var f in friendService.Friends)
                    {
                        string name = !string.IsNullOrWhiteSpace(f.friendDisplayName) ? f.friendDisplayName : ShortId(f.friendUserId);
                        sb.AppendLine($"• <color=#74d680>{name}</color>");
                    }
                    _friendsListText.text = sb.ToString();
                }

                // Pending requests
                if (friendService.PendingRequests.Count == 0)
                {
                    _pendingText.text = string.Empty;
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"<b>{Text("Lời mời kết bạn", "Pending Requests", "保留中のリクエスト")}</b>");
                    foreach (var p in friendService.PendingRequests)
                    {
                        sb.AppendLine($"• {ShortId(p.requester_id)}...");
                    }
                    _pendingText.text = sb.ToString();
                }
            });
        }

        private void OnAddFriend()
        {
            string userId = _addFriendInput.text.Trim();
            if (string.IsNullOrWhiteSpace(userId)) return;

            var friendService = FindFirstObjectByType<FriendService>();
            if (friendService == null) return;

            friendService.SendFriendRequest(userId, success =>
            {
                _addFriendInput.text = string.Empty;
                RefreshContent();
            });
        }

        // ──────────────────────── Helpers ────────────────────────

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
            var txt = MakeText(go, "Lbl", Vector2.zero, new Vector2(size.x - 8f, size.y - 6f), 15f);
            txt.text = label;
            txt.fontStyle = FontStyles.Bold;
            return btn;
        }

        private static string Text(string vi, string en, string ja)
        {
            return GameServices.TryGet(out GameSettingsService s) ? s.Text(vi, en, ja) : vi;
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "Player";
            return id.Length <= 8 ? id : id.Substring(0, 8);
        }
    }
}
