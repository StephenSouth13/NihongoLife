using System;
using NihongoLife.Core;
using NihongoLife.Island;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Responsive friends window used by the main menu.</summary>
    public sealed class FriendsUI : MonoBehaviour
    {
        private enum Tab { Friends, Requests, Add }

        private GameObject _panel;
        private RectTransform _card, _content;
        private TextMeshProUGUI _title, _subtitle, _status;
        private TMP_InputField _friendIdInput;
        private Button _friendsTab, _requestsTab, _addTab;
        private FriendService _service;
        private Tab _tab;
        private string _confirmRemoveId;
        private int _screenWidth, _screenHeight;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Initialize(TMP_FontAsset font)
        {
            if (font != null) UiKit.Font = font;
            BuildUI();
            Loc.Watch(this, RefreshContent);
        }

        public void Show()
        {
            if (_panel == null) BuildUI();
            _panel.SetActive(true);
            _confirmRemoveId = null;
            UpdateResponsiveLayout(true);
            UiModalStack.Register(this, () => IsOpen, Hide, "Friends");
            RefreshContent();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            _confirmRemoveId = null;
        }

        private void Update()
        {
            if (IsOpen) UpdateResponsiveLayout(false);
        }

        private void BuildUI()
        {
            _panel = new GameObject("FriendsPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(transform, false);
            _panel.layer = 5;
            var overlay = (RectTransform)_panel.transform;
            UiKit.Fill(overlay);
            _panel.GetComponent<Image>().color = UiTheme.Backdrop;

            _card = UiKit.Card(overlay, "FriendsWindow", UiTheme.Ink, 22, 0.65f).rectTransform;
            _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0.5f, 0.5f);
            UiKit.Border(_card, UiTheme.Line, 22);
            UiKit.CloseButton(_card, Hide, 20f);

            var icon = UiKit.Icon(_card, "groups", UiTheme.Gold, 34f);
            UiKit.Place(icon.rectTransform, 28f, 24f, 34f, 34f);
            _title = UiKit.Label(_card, "Title", "", 27f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(_title, 76f, 20f, 700f, 36f);
            _subtitle = UiKit.Label(_card, "Subtitle", "", 14f, UiTheme.Muted);
            UiKit.Place(_subtitle, 76f, 55f, 760f, 24f);

            var tabs = UiKit.Rect(_card, "Tabs");
            UiKit.Place(tabs, 28f, 96f, 760f, 44f);
            UiKit.RowLayout(tabs, 8f);
            _friendsTab = TabButton(tabs, "Friends", "Bạn bè", "Friends", "フレンド", "groups", Tab.Friends);
            _requestsTab = TabButton(tabs, "Requests", "Lời mời", "Requests", "申請", "person_add", Tab.Requests);
            _addTab = TabButton(tabs, "Add", "Thêm bạn", "Add friend", "フレンド追加", "add", Tab.Add);

            var body = UiKit.Card(_card, "Body", UiTheme.Surface, 16).rectTransform;
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(28f, 28f);
            body.offsetMax = new Vector2(-28f, -156f);
            _content = UiKit.Scroll(body, "FriendScroll", out _, 10f, new RectOffset(14, 14, 14, 14));
            UiKit.Fill((RectTransform)_content.parent.parent, 0f, 0f, 0f, 38f);
            _status = UiKit.Label(body, "Status", "", 13.5f, UiTheme.Muted, wrap: true);
            UiKit.Strip(_status.rectTransform, false, 0f, 38f, 16f, 16f);
            _panel.SetActive(false);
        }

        private Button TabButton(Transform parent, string name, string vi, string en, string ja, string icon, Tab tab)
        {
            var button = UiKit.Button(parent, name, vi, en, ja, () => SelectTab(tab), UiKit.Kind.Ghost, icon, 15f);
            UiKit.Size(button, 210f, 44f);
            return button;
        }

        private void SelectTab(Tab tab)
        {
            _tab = tab;
            _confirmRemoveId = null;
            RefreshContent();
        }

        private void RefreshContent()
        {
            if (_panel == null) return;
            _title.text = Loc.T("Bạn bè", "Friends", "フレンド");
            _subtitle.text = Loc.T("Kết nối, nhắn tin và ghé thăm đảo của nhau", "Connect, chat, and visit each other's islands", "つながって、話して、お互いの島を訪ねましょう");
            UiKit.SetKind(_friendsTab, _tab == Tab.Friends ? UiKit.Kind.Primary : UiKit.Kind.Ghost);
            UiKit.SetKind(_requestsTab, _tab == Tab.Requests ? UiKit.Kind.Primary : UiKit.Kind.Ghost);
            UiKit.SetKind(_addTab, _tab == Tab.Add ? UiKit.Kind.Primary : UiKit.Kind.Ghost);
            UiKit.Clear(_content);
            SetStatus(Loc.T("Đang cập nhật…", "Updating…", "更新中…"), UiTheme.Muted);

            if (!GameServices.TryGet(out IAuthService auth) || !OnlineAccess.IsRegistered)
            {
                BuildSignedOutState();
                return;
            }
            _service = FindFirstObjectByType<FriendService>();
            if (_service == null)
            {
                BuildUnavailableState();
                return;
            }
            if (_tab == Tab.Add)
            {
                BuildAddFriend(auth);
                return;
            }
            _service.RefreshFriends(() =>
            {
                if (!IsOpen) return;
                UiKit.Clear(_content);
                if (_tab == Tab.Friends) BuildFriends(); else BuildRequests();
            });
        }

        private void BuildFriends()
        {
            SetStatus(Loc.F("{0} bạn bè", "{0} friends", "フレンド {0}人", _service.Friends.Count), UiTheme.Muted);
            if (_service.Friends.Count == 0)
            {
                EmptyState("group_add", Loc.T("Chưa có bạn bè", "No friends yet", "フレンドはまだいません"),
                    Loc.T("Mở tab Thêm bạn và nhập User ID để gửi lời mời.", "Open Add friend and enter a User ID to send a request.", "「フレンド追加」でユーザーIDを入力して申請できます。"),
                    Loc.T("Thêm bạn", "Add friend", "フレンド追加"), () => SelectTab(Tab.Add));
                return;
            }
            foreach (var friend in _service.Friends) BuildFriendRow(friend);
        }

        private void BuildFriendRow(FriendEntry friend)
        {
            string id = friend.friendUserId;
            string name = string.IsNullOrWhiteSpace(friend.friendDisplayName) ? ShortId(id) : friend.friendDisplayName;
            var card = UiKit.Card(_content, "Friend_" + friend.id, UiTheme.Raised, 14);
            UiKit.Size(card, -1f, 94f, 1f);
            var avatar = UiKit.Avatar(card.transform, "Avatar", name, 52f, new Color(0.22f, 0.42f, 0.5f, 1f), Color.white);
            UiKit.Place(avatar.rectTransform, 16f, 20f, 52f, 52f);
            bool online = IsOnline(id);
            var dot = UiKit.Image(avatar.transform, "Presence", online ? UiTheme.Good : new Color(0.42f, 0.46f, 0.52f, 1f), UiKit.Circle);
            UiKit.Pin(dot.rectTransform, new Vector2(1f, 0f), -2f, 2f, 13f, 13f);
            var nameLabel = UiKit.Label(card.transform, "Name", name + (friend.friendLevel > 0 ? $"  <size=72%><color=#F7BD3D>Lv {friend.friendLevel}</color></size>" : ""), 18f, UiTheme.Text, FontStyles.Bold);
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Place(nameLabel, 82f, 14f, 230f, 28f);
            string island = !friend.islandChecked && !friend.HasIsland ? Loc.F("ID · {0}", "ID · {0}", "ID · {0}", ShortId(id))
                : friend.HasIsland ? Loc.F("♥ {0} · có đảo để thăm", "♥ {0} · island to visit", "♥ {0} · 島あり", friend.islandLikes)
                : Loc.T("Chưa đăng đảo", "No island yet", "島はまだありません");
            UiKit.Place(UiKit.Label(card.transform, "Island", island, 12.5f, friend.HasIsland ? UiTheme.Gold : UiTheme.Muted), 82f, 42f, 230f, 20f);
            UiKit.Place(UiKit.Label(card.transform, "Online", online ? Loc.T("Đang online", "Online now", "オンライン") : Loc.T("Ngoại tuyến", "Offline", "オフライン"), 12f, online ? UiTheme.Good : UiTheme.Muted), 82f, 62f, 230f, 18f);

            var actions = UiKit.Rect(card.transform, "Actions");
            actions.anchorMin = new Vector2(0.34f, 0f);
            actions.anchorMax = Vector2.one;
            actions.offsetMin = new Vector2(0f, 18f);
            actions.offsetMax = new Vector2(-14f, -18f);
            UiKit.RowLayout(actions, 8f, null, TextAnchor.MiddleRight);
            ActionButton(actions, "Message", "Nhắn tin", "Message", "メッセージ", "chat", UiKit.Kind.Secondary, () => OpenDirect(id, name));
            var visit = ActionButton(actions, "Visit", "Thăm đảo", "Visit island", "島を訪ねる", "home", UiKit.Kind.Success, () => VisitIsland(friend, name, card.transform));
            visit.interactable = friend.HasIsland || !friend.islandChecked; // known to have no island: nothing to visit
            bool confirm = _confirmRemoveId == friend.id;
            ActionButton(actions, "Remove", confirm ? "Xác nhận xoá" : "Xoá", confirm ? "Confirm remove" : "Remove", confirm ? "削除を確認" : "削除", "delete", UiKit.Kind.Danger, () => RemoveFriend(friend));
        }

        private void BuildRequests()
        {
            SetStatus(Loc.F("{0} lời mời đang chờ", "{0} pending requests", "保留中の申請 {0}件", _service.PendingRequests.Count), UiTheme.Muted);
            if (_service.PendingRequests.Count == 0)
            {
                EmptyState("mark_chat_unread", Loc.T("Không có lời mời mới", "No new requests", "新しい申請はありません"), Loc.T("Lời mời kết bạn mới sẽ xuất hiện tại đây.", "New friend requests will appear here.", "新しいフレンド申請はここに表示されます。"));
                return;
            }
            foreach (var request in _service.PendingRequests)
            {
                string name = string.IsNullOrWhiteSpace(request.friendDisplayName) ? ShortId(request.friendUserId) : request.friendDisplayName;
                var card = UiKit.Card(_content, "Request_" + request.id, UiTheme.Raised, 14);
                UiKit.Size(card, -1f, 82f, 1f);
                var avatar = UiKit.Avatar(card.transform, "Avatar", name, 48f, new Color(0.35f, 0.28f, 0.48f, 1f), Color.white);
                UiKit.Place(avatar.rectTransform, 16f, 17f, 48f, 48f);
                UiKit.Place(UiKit.Label(card.transform, "Name", name, 17f, UiTheme.Text, FontStyles.Bold), 78f, 15f, 300f, 26f);
                UiKit.Place(UiKit.Label(card.transform, "RequestText", Loc.T("Muốn kết bạn với bạn", "Sent you a friend request", "フレンド申請が届いています"), 13f, UiTheme.Muted), 78f, 42f, 300f, 22f);
                var actions = UiKit.Rect(card.transform, "Actions");
                UiKit.Pin(actions, new Vector2(1f, 0.5f), -14f, 0f, 290f, 48f);
                UiKit.RowLayout(actions, 8f, null, TextAnchor.MiddleRight);
                ActionButton(actions, "Accept", "Chấp nhận", "Accept", "承認", "check_circle", UiKit.Kind.Success, () => _service.AcceptFriendRequest(request.id, _ => RefreshContent()));
                ActionButton(actions, "Decline", "Từ chối", "Decline", "拒否", "close", UiKit.Kind.Ghost, () => _service.RemoveFriend(request.id, _ => RefreshContent()));
            }
        }

        private void BuildAddFriend(IAuthService auth)
        {
            SetStatus(Loc.T("Dùng User ID chính xác của người bạn muốn kết nối.", "Use the exact User ID of the person you want to connect with.", "追加したい相手の正確なユーザーIDを入力してください。"), UiTheme.Muted);
            UiKit.Size(UiKit.Label(_content, "Intro", "Gửi lời mời kết bạn", "Send a friend request", "フレンド申請を送る", 22f, UiTheme.Text, FontStyles.Bold), -1f, 34f, 1f);
            UiKit.Size(UiKit.Label(_content, "Hint", "Nhập User ID. Người kia sẽ thấy lời mời trong tab Lời mời.", "Enter a User ID. They will see it in Requests.", "ユーザーIDを入力すると、相手の「申請」タブに表示されます。", 14f, UiTheme.TextSoft, wrap: true), -1f, 48f, 1f);

            var inputCard = UiKit.Card(_content, "FriendIdCard", UiTheme.Sunken, 12);
            UiKit.Size(inputCard, -1f, 56f, 1f);
            _friendIdInput = CreateInput(inputCard.transform);
            var send = UiKit.Button(inputCard.transform, "Send", "Gửi lời mời", "Send request", "申請する", SendFriendRequest, UiKit.Kind.Primary, "person_add", 14f);
            UiKit.Pin((RectTransform)send.transform, new Vector2(1f, 0.5f), -8f, 0f, 180f, 40f);

            var own = UiKit.Card(_content, "OwnId", new Color(0.12f, 0.2f, 0.24f, 1f), 14);
            UiKit.Size(own, -1f, 92f, 1f);
            UiKit.Place(UiKit.Label(own.transform, "OwnTitle", "User ID của bạn", "Your User ID", "あなたのユーザーID", 13f, UiTheme.Muted, FontStyles.Bold), 18f, 13f, 500f, 22f);
            UiKit.Place(UiKit.Label(own.transform, "OwnValue", auth.UserId, 15f, UiTheme.Text), 18f, 40f, 620f, 30f);
            var copy = UiKit.Button(own.transform, "Copy", "Sao chép ID", "Copy ID", "IDをコピー", () =>
            {
                GUIUtility.systemCopyBuffer = auth.UserId;
                SetStatus(Loc.T("Đã sao chép User ID.", "User ID copied.", "ユーザーIDをコピーしました。"), UiTheme.Good);
            }, UiKit.Kind.Secondary, "checklist", 14f);
            UiKit.Pin((RectTransform)copy.transform, new Vector2(1f, 0.5f), -14f, 0f, 165f, 42f);
        }

        private TMP_InputField CreateInput(Transform parent)
        {
            var root = UiKit.Rect(parent, "Input");
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(14f, 7f); root.offsetMax = new Vector2(-202f, -7f);
            var input = root.gameObject.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine; input.characterLimit = 64;
            var text = UiKit.Label(root, "Text", "", 15f, UiTheme.Text);
            UiKit.Fill(text.rectTransform, 8f, 0f, 8f, 0f);
            var placeholder = UiKit.Label(root, "Placeholder", "User ID…", 15f, UiTheme.Muted);
            UiKit.Fill(placeholder.rectTransform, 8f, 0f, 8f, 0f);
            input.textViewport = root; input.textComponent = text; input.placeholder = placeholder;
            return input;
        }

        private Button ActionButton(Transform parent, string name, string vi, string en, string ja, string icon, UiKit.Kind kind, Action action)
        {
            var button = UiKit.Button(parent, name, vi, en, ja, action, kind, icon, 13f, 10);
            UiKit.Size(button, 128f, 44f);
            return button;
        }

        private void OpenDirect(string id, string name)
        {
            SocialChatWindow.Ensure();
            FindFirstObjectByType<SocialHub>()?.OpenDirect(id, name);
            Hide();
            SocialChatWindow.Open(SocialChatWindow.Tab.Direct, SocialHub.DirectId(id));
        }

        private void VisitIsland(FriendEntry friend, string name, Transform row)
        {
            SetRowError(row, Loc.T("Đang tìm đảo…", "Finding island…", "島を探しています…"), UiTheme.Muted);
            IslandVisit.Visit(friend.friendUserId, name, error =>
            {
                if (string.IsNullOrEmpty(error)) Hide(); else SetRowError(row, error, UiTheme.Bad);
            });
        }

        private void RemoveFriend(FriendEntry friend)
        {
            if (_confirmRemoveId != friend.id)
            {
                _confirmRemoveId = friend.id;
                RefreshContent();
                return;
            }
            _confirmRemoveId = null;
            _service.RemoveFriend(friend.id, success =>
            {
                if (success) RefreshContent(); else SetStatus(Loc.T("Không thể xoá bạn lúc này.", "Could not remove this friend.", "フレンドを削除できませんでした。"), UiTheme.Bad);
            });
        }

        private void SendFriendRequest()
        {
            string id = _friendIdInput != null ? _friendIdInput.text.Trim() : "";
            if (string.IsNullOrEmpty(id))
            {
                SetStatus(Loc.T("Hãy nhập User ID.", "Enter a User ID.", "ユーザーIDを入力してください。"), UiTheme.Warn);
                return;
            }
            _service.SendFriendRequest(id, success =>
            {
                if (success)
                {
                    _friendIdInput.text = "";
                    SetStatus(Loc.T("Đã gửi lời mời kết bạn.", "Friend request sent.", "フレンド申請を送りました。"), UiTheme.Good);
                }
                else SetStatus(Loc.T("Không thể gửi lời mời. Kiểm tra ID và kết nối mạng.", "Could not send the request. Check the ID and connection.", "申請を送れませんでした。IDと接続を確認してください。"), UiTheme.Bad);
            });
        }

        private void BuildSignedOutState()
        {
            SetStatus(Loc.T("Bạn bè cần tài khoản đã đăng ký và đăng nhập. Chế độ Khách chơi ngoại tuyến.", "Friends need a registered account, signed in. Guests play offline.", "フレンドには登録済みアカウントでのログインが必要です。ゲストはオフラインです。"), UiTheme.Warn);
            EmptyState("account_circle", Loc.T("Đăng nhập để gặp bạn bè", "Sign in to meet friends", "ログインしてフレンドと会おう"),
                Loc.T("Sau khi đăng nhập, bạn có thể nhắn tin, nhận lời mời và thăm đảo của bạn bè.", "After signing in, you can chat, receive requests, and visit friends' islands.", "ログインすると、メッセージ、申請、フレンドの島への訪問ができます。"),
                Loc.T("Đăng ký / Đăng nhập", "Register / Sign in", "登録・ログイン"), () => { Hide(); var auth = GetComponent<AuthUI>() ?? FindFirstObjectByType<AuthUI>(); auth?.ShowForOnline(); });
        }

        private void BuildUnavailableState()
        {
            SetStatus(Loc.T("Dịch vụ bạn bè chưa sẵn sàng.", "Friends service is unavailable.", "フレンドサービスを利用できません。"), UiTheme.Bad);
            EmptyState("cloud", Loc.T("Không thể kết nối", "Unable to connect", "接続できません"), Loc.T("Kiểm tra kết nối mạng rồi mở lại cửa sổ này.", "Check your connection and reopen this window.", "接続を確認して、この画面を開き直してください。"));
        }

        private void EmptyState(string iconName, string title, string detail, string action = null, Action onAction = null)
        {
            var card = UiKit.Card(_content, "Empty", UiTheme.Sunken, 16);
            UiKit.Size(card, -1f, action == null ? 210f : 260f, 1f);
            var icon = UiKit.Icon(card.transform, iconName, UiTheme.Gold, 48f);
            UiKit.Pin(icon.rectTransform, new Vector2(0.5f, 1f), 0f, -30f, 48f, 48f);
            UiKit.Place(UiKit.Label(card.transform, "Heading", title, 20f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center), 24f, 92f, 840f, 30f);
            UiKit.Place(UiKit.Label(card.transform, "Detail", detail, 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Top, true), 70f, 128f, 750f, 62f);
            if (action != null)
            {
                var button = UiKit.Button(card.transform, "Action", action, onAction, UiKit.Kind.Primary, "arrow_forward", 15f);
                UiKit.Pin((RectTransform)button.transform, new Vector2(0.5f, 0f), 0f, 22f, 190f, 42f);
            }
        }

        private void SetRowError(Transform row, string message, Color color)
        {
            if (row == null) return;
            var label = row.Find("RowStatus")?.GetComponent<TextMeshProUGUI>() ?? UiKit.Label(row, "RowStatus", "", 12f, color, wrap: true);
            label.color = color; label.text = message ?? "";
            UiKit.Pin(label.rectTransform, new Vector2(0f, 0f), 82f, 5f, 430f, 18f);
        }

        private void SetStatus(string text, Color color)
        {
            if (_status == null) return;
            _status.text = text ?? ""; _status.color = color;
        }

        private void UpdateResponsiveLayout(bool force)
        {
            if (_card == null || (!force && _screenWidth == Screen.width && _screenHeight == Screen.height)) return;
            _screenWidth = Screen.width; _screenHeight = Screen.height;
            var parent = _card.parent as RectTransform;
            Vector2 area = parent != null ? parent.rect.size : new Vector2(1920f, 1080f);
            _card.sizeDelta = new Vector2(Mathf.Min(1040f, Mathf.Max(760f, area.x - 48f)), Mathf.Min(680f, Mathf.Max(520f, area.y - 36f)));
        }

        private static bool IsOnline(string userId)
        {
            var world = FindFirstObjectByType<SupabaseOnlineWorldService>();
            return world != null && !string.IsNullOrEmpty(userId) && world.IsOnline(userId);
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return Loc.T("Người chơi", "Player", "プレイヤー");
            return id.Length <= 12 ? id : id.Substring(0, 8) + "…";
        }
    }
}
