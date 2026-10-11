using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The chat and friends window (Enter / U). Tabs: Khu vực (people in your shard), Cộng đồng (everyone online),
    /// Nhóm (your groups + invites), Riêng tư (end-to-end encrypted 1:1) and Bạn bè (online/offline, requests).
    /// Also the player card that F on another player (or a click on a name) opens: message, add friend, invite to a
    /// group, block. All data comes from SocialHub; nothing typed here is stored on a server.
    /// </summary>
    public sealed class SocialChatWindow : MonoBehaviour
    {
        public enum Tab { Area, Community, Groups, Direct, Friends }

        private static SocialChatWindow _instance;
        public static SocialChatWindow Instance => _instance;
        public static bool IsOpen => _instance != null && _instance._window != null && _instance._window.gameObject.activeSelf;
        public static bool CardOpen => _instance != null && _instance._card != null && _instance._card.gameObject.activeSelf;

        private static readonly Color Mine = new Color(0.16f, 0.33f, 0.42f, 1f);
        private static readonly Color Theirs = new Color(0.15f, 0.18f, 0.22f, 1f);
        private static readonly Color SystemTint = new Color(0.2f, 0.17f, 0.1f, 1f);
        private static readonly Color TabOn = new Color(0.86f, 0.63f, 0.2f, 1f);
        private static readonly Color TabOff = new Color(0.14f, 0.17f, 0.21f, 1f);
        private static readonly Color Online = new Color(0.35f, 0.85f, 0.5f, 1f);

        private TMP_FontAsset _font;
        private Canvas _canvas;
        private RectTransform _window, _listContent, _messagesContent, _chatPane, _friendsPane, _friendsContent, _card, _cardBody;
        private ScrollRect _messagesScroll;
        private TextMeshProUGUI _status, _paneTitle, _paneInfo, _listHeader;
        private RectTransform _paneActions, _listFooter;
        private TMP_InputField _input, _groupNameInput, _friendIdInput;
        private RectTransform _launcher;
        private TextMeshProUGUI _launcherText;
        private readonly Dictionary<Tab, TextMeshProUGUI> _tabLabels = new Dictionary<Tab, TextMeshProUGUI>();
        private readonly Dictionary<Tab, Image> _tabImages = new Dictionary<Tab, Image>();

        private SocialHub _hub;
        private Tab _tab = Tab.Area;
        private string _conversationId = SocialHub.AreaId;
        private bool _dirty, _messagesDirty;
        private float _nextRefresh;
        private string _cardPlayerId, _cardPlayerName;

        public Tab CurrentTab => _tab;
        public string CurrentConversationId => _conversationId;
        public int RenderedMessageCount => _messagesContent != null ? _messagesContent.childCount : 0;
        public RectTransform WindowRect => _window;

        // ──────────────────────── Static entry points ────────────────────────

        public static SocialChatWindow Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("SocialChatWindow");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SocialChatWindow>();
            return _instance;
        }

        public static void Open(Tab? tab = null, string conversationId = null)
        {
            var w = Ensure();
            w.Build();
            w._window.gameObject.SetActive(true);
            if (tab.HasValue) w.SelectTab(tab.Value, conversationId);
            else w.SelectTab(w._tab, conversationId ?? w._conversationId);
            if (w._tab != Tab.Friends) w.FocusInput();
        }

        public static void Close()
        {
            if (_instance == null || _instance._window == null) return;
            _instance._input?.DeactivateInputField();
            _instance._window.gameObject.SetActive(false);
            if (_instance._hub != null) _instance._hub.ActiveConversationId = null;
        }

        public static void Toggle(Tab tab)
        {
            if (IsOpen && _instance._tab == tab) Close();
            else Open(tab);
        }

        /// <summary>F on another player, or a click on a name: what can I do with this person?</summary>
        public static void ShowPlayerCard(string playerId, string displayName)
        {
            var w = Ensure();
            w.Build();
            w._cardPlayerId = playerId;
            w._cardPlayerName = displayName;
            w.RebuildCard();
            w._card.gameObject.SetActive(true);
        }

        public static void HidePlayerCard()
        {
            if (_instance != null && _instance._card != null) _instance._card.gameObject.SetActive(false);
        }

        // ──────────────────────── Lifecycle ────────────────────────

        private void Update()
        {
            if (_hub == null && GameServices.TryGet(out _hub))
            {
                _hub.OnConversationsChanged += MarkDirty;
                _hub.OnFriendsChanged += MarkDirty;
                _hub.OnMessage += HandleMessage;
            }
            if (_hub == null) return;
            Build();
            UpdateLauncher();
            if (!IsOpen) return;
            if (_dirty || Time.unscaledTime >= _nextRefresh)
            {
                _dirty = false;
                // The area and friends lists follow people walking in and out; the rest change only on events.
                _nextRefresh = _tab == Tab.Area || _tab == Tab.Friends ? Time.unscaledTime + 1.5f : float.MaxValue;
                RefreshTabs();
                RefreshList();
                RefreshPane();
            }
            if (_messagesDirty)
            {
                _messagesDirty = false;
                RefreshMessages();
            }
        }

        private void OnDestroy()
        {
            if (_hub != null)
            {
                _hub.OnConversationsChanged -= MarkDirty;
                _hub.OnFriendsChanged -= MarkDirty;
                _hub.OnMessage -= HandleMessage;
            }
            if (_instance == this) _instance = null;
        }

        private void MarkDirty() => _dirty = true;

        private void HandleMessage(SocialHub.Conversation c, OnlineChatMessage m)
        {
            _dirty = true;
            if (c.Id == _conversationId) _messagesDirty = true;
        }

        // ──────────────────────── Build ────────────────────────

        private void Build()
        {
            if (_canvas != null) return;
            _font = NLUi.ResolveFont();
            _canvas = NLUi.CreateCanvas("SocialChatCanvas", 560, transform);

            BuildLauncher();

            _window = NLUi.Panel(_canvas.transform, "SocialWindow", NLUi.Ink);
            NLUi.Anchor(_window, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1160f, 700f));
            var root = NLUi.Group(_window, "Root", true, 10f);
            NLUi.Stretch(root, 18f);
            root.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            NLUi.CloseButton(_window, _font, Close, 40f, 10f);

            // Title row
            var top = NLUi.Group(root, "Top", false, 10f, TextAnchor.MiddleLeft);
            var title = NLUi.Label(top, "Title", "Trò chuyện", 26f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Size(title, 220f, 40f);
            _status = NLUi.Label(top, "Status", "", 16f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            NLUi.Size(_status, -1f, 40f, 1f);
            var spacer = new GameObject("Spacer", typeof(RectTransform)); spacer.transform.SetParent(top, false);
            NLUi.Size(spacer.transform, 44f, 40f);

            // Tabs
            var tabs = NLUi.Group(root, "Tabs", false, 8f, TextAnchor.MiddleLeft);
            AddTab(tabs, Tab.Area, "Khu vực");
            AddTab(tabs, Tab.Community, "Cộng đồng");
            AddTab(tabs, Tab.Groups, "Nhóm");
            AddTab(tabs, Tab.Direct, "Riêng tư");
            AddTab(tabs, Tab.Friends, "Bạn bè");

            // Body: list column + chat pane (or the friends pane)
            var body = NLUi.Group(root, "Body", false, 12f, TextAnchor.UpperLeft);
            var bodyLayout = body.GetComponent<HorizontalLayoutGroup>();
            bodyLayout.childForceExpandHeight = true;
            NLUi.Size(body, -1f, 560f, 1f, 560f);

            var listPanel = NLUi.Panel(body, "List", NLUi.Card, new RectOffset(12, 12, 12, 12), 8f);
            NLUi.Size(listPanel, 330f, 560f);
            _listHeader = NLUi.Label(listPanel, "ListHeader", "", 16f, NLUi.Soft, _font, FontStyles.Bold);
            _listContent = Scroll(listPanel, "ListScroll", 380f, out _);
            // Built once (not inside the list that refreshes), so the typed group name survives refreshes.
            _listFooter = NLUi.Panel(listPanel, "CreateGroup", new Color(0.09f, 0.11f, 0.14f, 1f), new RectOffset(10, 10, 8, 10), 6f);
            NLUi.Label(_listFooter, "Label", "Tạo nhóm trò chuyện mới", 15f, NLUi.Muted, _font);
            _groupNameInput = MakeInput(_listFooter, "GroupName", "Tên nhóm (vd. Nhóm ôn N4)", 32);
            NLUi.Size(_groupNameInput, -1f, 40f);
            NLUi.Size(NLUi.Button(_listFooter, "CreateGroupButton", "+ Tạo nhóm", _font, CreateGroupFromInput, NLUi.Gold, 16f, new Color(0.1f, 0.08f, 0.04f), 38f), -1f, 38f);
            _listFooter.gameObject.SetActive(false);

            _chatPane = NLUi.Panel(body, "ChatPane", NLUi.Card, new RectOffset(14, 14, 12, 12), 8f);
            NLUi.Size(_chatPane, -1f, 560f, 1f);
            var head = NLUi.Group(_chatPane, "Head", false, 8f, TextAnchor.MiddleLeft);
            var headText = NLUi.Group(head, "HeadText", true, 2f);
            NLUi.Size(headText, -1f, -1f, 1f);
            _paneTitle = NLUi.Label(headText, "PaneTitle", "", 20f, NLUi.Text, _font, FontStyles.Bold);
            _paneInfo = NLUi.Label(headText, "PaneInfo", "", 14f, NLUi.Muted, _font);
            _paneActions = NLUi.Group(head, "PaneActions", false, 6f, TextAnchor.MiddleRight, false);
            NLUi.Divider(_chatPane);
            _messagesContent = Scroll(_chatPane, "Messages", 410f, out _messagesScroll);
            var inputRow = NLUi.Group(_chatPane, "InputRow", false, 8f, TextAnchor.MiddleLeft);
            _input = MakeInput(inputRow, "ChatInput", "Nhập tin nhắn… (Enter để gửi)", SupabaseOnlineWorldService.MaxChatLength);
            NLUi.Size(_input, -1f, 46f, 1f);
            _input.onSubmit.AddListener(_ => SendCurrent());
            var send = NLUi.Button(inputRow, "Send", "Gửi", _font, SendCurrent, NLUi.Gold, 18f, new Color(0.1f, 0.08f, 0.04f), 46f);
            NLUi.Size(send, 96f, 46f);

            _friendsPane = NLUi.Panel(body, "FriendsPane", NLUi.Card, new RectOffset(14, 14, 12, 12), 8f);
            NLUi.Size(_friendsPane, -1f, 560f, 1f);
            BuildFriendsPane();
            _friendsPane.gameObject.SetActive(false);

            BuildCard();
            _window.gameObject.SetActive(false);
            UiModalStack.Register(this, () => IsOpen, Close, "SocialChat");
            UiModalStack.Register(this, () => CardOpen, HidePlayerCard, "PlayerCard");
        }

        private void AddTab(Transform parent, Tab tab, string label)
        {
            var button = NLUi.Button(parent, "Tab_" + tab, label, _font, () => SelectTab(tab), TabOff, 17f, null, 40f);
            NLUi.Size(button, 150f, 40f);
            _tabLabels[tab] = button.GetComponentInChildren<TextMeshProUGUI>();
            _tabImages[tab] = button.GetComponent<Image>();
        }

        private RectTransform Scroll(Transform parent, string name, float height, out ScrollRect scroll)
        {
            var view = new GameObject(name, typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            view.transform.SetParent(parent, false);
            NLUi.Size(view.transform, -1f, height, 1f, height);
            var content = NLUi.Group(view.transform, "Content", true, 6f);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            NLUi.FitContent(content);
            scroll = view.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = (RectTransform)view.transform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        private TMP_InputField MakeInput(Transform parent, string name, string placeholder, int limit)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UIStyleKit.RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.07f, 0.09f, 0.11f, 1f);
            var area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(go.transform, false);
            NLUi.Stretch((RectTransform)area.transform, 4f);
            ((RectTransform)area.transform).offsetMin = new Vector2(12f, 4f);
            ((RectTransform)area.transform).offsetMax = new Vector2(-12f, -4f);
            var text = NLUi.Label(area.transform, "Text", "", 17f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.Stretch(text.rectTransform);
            var ph = NLUi.Label(area.transform, "Placeholder", placeholder, 17f, new Color(0.6f, 0.66f, 0.72f, 0.7f), _font, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            ph.textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.Stretch(ph.rectTransform);
            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = (RectTransform)area.transform;
            input.textComponent = text;
            input.placeholder = ph;
            input.characterLimit = limit;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;
            input.targetGraphic = image;
            return input;
        }

        private void BuildLauncher()
        {
            _launcher = NLUi.Panel(_canvas.transform, "ChatLauncher", new Color(0.86f, 0.63f, 0.2f, 0.95f), new RectOffset(16, 16, 8, 8), 0f, false);
            NLUi.Anchor(_launcher, new Vector2(0f, 0f), new Vector2(24f, 300f), new Vector2(260f, 44f));
            var button = _launcher.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => Open(_hub != null && _hub.Invites.Count > 0 ? Tab.Groups : _hub != null && _hub.UnreadOf(SocialHub.Kind.Direct) > 0 ? Tab.Direct : Tab.Groups));
            _launcherText = NLUi.Label(_launcher, "Text", "", 17f, new Color(0.1f, 0.07f, 0.02f), _font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            _launcherText.textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.FitContent(_launcher, width: true, height: false);
            _launcher.gameObject.SetActive(false);
        }

        private void UpdateLauncher()
        {
            if (_launcher == null) return;
            int unread = _hub.TotalUnread;
            int invites = _hub.Invites.Count;
            bool show = !IsOpen && (unread > 0 || invites > 0);
            if (_launcher.gameObject.activeSelf != show) _launcher.gameObject.SetActive(show);
            if (!show) return;
            string key = GameInputService.GetOrCreate().GetBindingLabel(GameInputId.Chat);
            _launcherText.text = invites > 0 && unread == 0 ? $"● {invites} lời mời nhóm  ·  {key}" : $"● {unread} tin nhắn mới  ·  {key}";
        }

        // ──────────────────────── Tabs ────────────────────────

        public void SelectTab(Tab tab, string conversationId = null)
        {
            Build();
            _tab = tab;
            switch (tab)
            {
                case Tab.Area: _conversationId = SocialHub.AreaId; break;
                case Tab.Community: _conversationId = SocialHub.CommunityId; break;
                case Tab.Groups:
                    _conversationId = conversationId != null && conversationId.StartsWith("grp:", StringComparison.Ordinal) ? conversationId : FirstOf(SocialHub.Kind.Group);
                    break;
                case Tab.Direct:
                    _conversationId = conversationId != null && conversationId.StartsWith("dm:", StringComparison.Ordinal) ? conversationId : FirstOf(SocialHub.Kind.Direct);
                    break;
                case Tab.Friends:
                    _conversationId = null;
                    _hub?.RefreshFriends();
                    break;
            }
            _chatPane.gameObject.SetActive(tab != Tab.Friends);
            _friendsPane.gameObject.SetActive(tab == Tab.Friends);
            if (_hub != null)
            {
                _hub.ActiveConversationId = _conversationId;
                if (_conversationId != null) _hub.MarkRead(_conversationId);
            }
            _dirty = true;
            _messagesDirty = true;
            _nextRefresh = 0f;
            Update();
        }

        private string FirstOf(SocialHub.Kind kind)
        {
            if (_hub == null) return null;
            var list = _hub.ConversationsOf(kind);
            return list.Count > 0 ? list[0].Id : null;
        }

        private void RefreshTabs()
        {
            foreach (var kv in _tabImages)
            {
                bool on = kv.Key == _tab;
                kv.Value.color = on ? TabOn : TabOff;
                var label = _tabLabels[kv.Key];
                label.color = on ? new Color(0.1f, 0.07f, 0.02f) : NLUi.Text;
                int badge = kv.Key switch
                {
                    Tab.Groups => _hub.UnreadOf(SocialHub.Kind.Group) + _hub.Invites.Count,
                    Tab.Direct => _hub.UnreadOf(SocialHub.Kind.Direct),
                    Tab.Community => _hub.UnreadOf(SocialHub.Kind.Community),
                    Tab.Friends => _hub.Friends.Count(f => f.IncomingRequest),
                    _ => 0
                };
                string name = kv.Key switch { Tab.Area => "Khu vực", Tab.Community => "Cộng đồng", Tab.Groups => "Nhóm", Tab.Direct => "Riêng tư", _ => "Bạn bè" };
                label.text = badge > 0 ? $"{name} <color={(on ? "#7A1E10" : "#FF7A5C")}>●{badge}</color>" : name;
            }

            var online = _hub != null && GameServices.TryGet(out IOnlineWorldService w) ? w as SupabaseOnlineWorldService : null;
            if (online != null && online.IsConnected)
            {
                string room = online.ShardTopic != null ? $"phòng {online.ShardTopic.Substring(online.ShardTopic.LastIndexOf(':') + 1)} ({online.ShardMemberCount}/{SupabaseOnlineWorldService.ShardCapacity})" : "phòng riêng";
                _status.text = $"<color=#5BD98A>●</color> Online · {online.OnlineCount} người đang chơi · {room}  ·  tin nhắn không lưu lại";
            }
            else _status.text = "<color=#9AA5B1>●</color> Ngoại tuyến — chỉ có chat khu vực mô phỏng";
        }

        // ──────────────────────── List column ────────────────────────

        private void RefreshList()
        {
            Clear(_listContent);
            _listFooter.gameObject.SetActive(_tab == Tab.Groups);
            switch (_tab)
            {
                case Tab.Area: ListArea(); break;
                case Tab.Community: ListCommunity(); break;
                case Tab.Groups: ListGroups(); break;
                case Tab.Direct: ListDirect(); break;
                case Tab.Friends: ListFriendsSide(); break;
            }
        }

        private void ListArea()
        {
            int n = 0;
            if (GameServices.TryGet(out IOnlineWorldService world))
            {
                foreach (var p in world.VisiblePlayers)
                {
                    if (p.playerId == world.LocalPlayerId) continue;
                    n++;
                    string id = p.playerId, name = p.displayName;
                    PersonRow(_listContent, name, "đang ở quanh đây", true, () => ShowPlayerCard(id, name));
                }
            }
            _listHeader.text = $"Quanh đây ({n})";
            if (n == 0) Note(_listContent, "Chưa có ai trong khu vực này. Người chơi khác cùng địa điểm sẽ hiện ở đây — bấm vào tên để nhắn riêng, kết bạn hoặc mời nhóm.");
        }

        private void ListCommunity()
        {
            var online = GameServices.TryGet(out IOnlineWorldService w) ? w as SupabaseOnlineWorldService : null;
            _listHeader.text = online != null ? $"Đang online: {online.OnlineCount}" : "Cộng đồng";
            Note(_listContent, "Kênh chung cho tất cả người chơi đang online.\n\n• Tin nhắn chỉ hiện trong lúc bạn đang chơi, không lưu lại.\n• Lịch sự, không chia sẻ thông tin cá nhân.\n• Bấm vào tên người gửi để nhắn riêng, kết bạn hoặc chặn.");
        }

        private void ListGroups()
        {
            _listHeader.text = "Nhóm của bạn";
            foreach (var invite in new List<SocialHub.Invite>(_hub.Invites))
            {
                var row = NLUi.Panel(_listContent, "Invite", new Color(0.26f, 0.2f, 0.08f, 1f), new RectOffset(10, 10, 8, 8), 6f);
                NLUi.Label(row, "Text", $"<b>{invite.FromName}</b> mời bạn vào nhóm “{invite.GroupName}”", 15f, NLUi.Text, _font);
                var buttons = NLUi.Group(row, "Buttons", false, 6f, TextAnchor.MiddleLeft, false);
                var captured = invite;
                NLUi.Size(NLUi.Button(buttons, "Accept", "Vào nhóm", _font, () => { _hub.AcceptInvite(captured); SelectTab(Tab.Groups, SocialHub.GroupConversationId(captured.GroupId)); }, NLUi.Good, 15f, null, 34f), 120f, 34f);
                NLUi.Size(NLUi.Button(buttons, "Decline", "Bỏ qua", _font, () => _hub.DeclineInvite(captured), TabOff, 15f, null, 34f), 100f, 34f);
            }
            foreach (var c in _hub.ConversationsOf(SocialHub.Kind.Group))
            {
                var g = _hub.GetGroup(c.GroupId);
                string id = c.Id;
                ConversationRow(_listContent, c.Title, $"{g?.MemberCount ?? 1} thành viên", c.Unread, id == _conversationId, () => SelectTab(Tab.Groups, id));
            }
            if (!_hub.IsRealOnline) Note(_listContent, "Nhóm cần kết nối online.");
        }

        private void CreateGroupFromInput()
        {
            string name = _groupNameInput != null ? _groupNameInput.text : string.Empty;
            var g = _hub.CreateGroup(name);
            if (g == null) return;
            if (_groupNameInput != null) _groupNameInput.text = string.Empty;
            SelectTab(Tab.Groups, SocialHub.GroupConversationId(g.Id));
        }

        private void ListDirect()
        {
            _listHeader.text = "Tin nhắn riêng";
            var list = _hub.ConversationsOf(SocialHub.Kind.Direct);
            var online = GameServices.TryGet(out IOnlineWorldService w) ? w as SupabaseOnlineWorldService : null;
            foreach (var c in list)
            {
                string id = c.Id;
                bool isOnline = online != null && online.IsOnline(c.PeerId);
                ConversationRow(_listContent, (isOnline ? "<color=#5BD98A>●</color> " : "<color=#6B7580>●</color> ") + c.Title, isOnline ? "online" : "offline", c.Unread, id == _conversationId, () => SelectTab(Tab.Direct, id));
            }
            if (list.Count == 0) Note(_listContent, "Chưa có cuộc trò chuyện riêng.\nMở tab Bạn bè và bấm “Nhắn”, hoặc đến gần ai đó và nhấn F.\n\nTin nhắn riêng được mã hóa đầu-cuối: chỉ hai bạn đọc được.");
        }

        private void ListFriendsSide()
        {
            _listHeader.text = "Bạn của bạn";
            Note(_listContent, "Bạn bè đang online hiện ở trên cùng, kèm nơi họ đang ở.\n\nKết bạn: đến gần người chơi và nhấn F › Kết bạn, hoặc nhập ID của họ bên phải.");
        }

        private void PersonRow(Transform parent, string name, string sub, bool online, Action onClick)
        {
            var row = NLUi.Button(parent, "Person", $"{(online ? "<color=#5BD98A>●</color>" : "<color=#6B7580>●</color>")} <b>{name}</b>\n<size=80%><color=#9AA5B1>{sub}</color></size>", _font, onClick, new Color(0.13f, 0.16f, 0.2f, 1f), 16f, null, 50f);
            row.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void ConversationRow(Transform parent, string title, string sub, int unread, bool selected, Action onClick)
        {
            string badge = unread > 0 ? $"  <color=#FF7A5C>●{unread}</color>" : string.Empty;
            var row = NLUi.Button(parent, "Conversation", $"<b>{title}</b>{badge}\n<size=80%><color=#9AA5B1>{sub}</color></size>", _font, onClick,
                selected ? new Color(0.24f, 0.29f, 0.35f, 1f) : new Color(0.13f, 0.16f, 0.2f, 1f), 16f, null, 50f);
            row.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void Note(Transform parent, string text)
        {
            NLUi.Label(parent, "Note", text, 15f, NLUi.Muted, _font);
        }

        // ──────────────────────── Chat pane ────────────────────────

        private void RefreshPane()
        {
            if (_tab == Tab.Friends) { RefreshFriendsPane(); return; }
            Clear(_paneActions);
            var c = _hub.Get(_conversationId);
            bool canType = c != null;
            _input.interactable = canType;
            if (c == null)
            {
                _paneTitle.text = _tab == Tab.Groups ? "Chưa chọn nhóm" : "Chưa chọn cuộc trò chuyện";
                _paneInfo.text = _tab == Tab.Groups ? "Tạo nhóm mới hoặc chấp nhận lời mời ở cột bên trái." : "Chọn một người ở cột bên trái.";
                return;
            }
            switch (c.Kind)
            {
                case SocialHub.Kind.Area:
                    _paneTitle.text = "Khu vực";
                    _paneInfo.text = "Những người cùng địa điểm (cùng phòng) với bạn sẽ thấy. Không lưu lại.";
                    break;
                case SocialHub.Kind.Community:
                    _paneTitle.text = "Cộng đồng";
                    _paneInfo.text = "Mọi người đang online đều thấy. Không lưu lại.";
                    break;
                case SocialHub.Kind.Group:
                {
                    var g = _hub.GetGroup(c.GroupId);
                    _paneTitle.text = c.Title;
                    var names = new List<string>();
                    if (g != null) foreach (var id in g.MemberIds) names.Add(id == _hub.LocalId ? "Bạn" : g.MemberName(id));
                    _paneInfo.text = $"{names.Count} thành viên: {string.Join(", ", names)} · Tin nhắn chỉ hiện khi đang online.";
                    string gid = c.GroupId;
                    SmallButton(_paneActions, "Mời bạn bè", () => { SelectTab(Tab.Friends); });
                    SmallButton(_paneActions, "Rời nhóm", () => { _hub.LeaveGroup(gid); SelectTab(Tab.Groups); }, NLUi.Bad);
                    break;
                }
                case SocialHub.Kind.Direct:
                {
                    _paneTitle.text = c.Title;
                    string code = _hub.PeerSafetyCode(c.PeerId);
                    _paneInfo.text = $"<color=#5BD98A>Mã hóa đầu-cuối</color> · Mã an toàn của bạn {_hub.SafetyCode} · của {c.Title} {(code ?? "(chưa trao đổi)")}";
                    string peer = c.PeerId, peerName = c.Title;
                    if (!_hub.IsFriend(peer) && _hub.CanUseFriends) SmallButton(_paneActions, "Kết bạn", () => _hub.AddFriend(peer));
                    bool blocked = _hub.IsBlocked(peer);
                    SmallButton(_paneActions, blocked ? "Bỏ chặn" : "Chặn", () => { _hub.SetBlocked(peer, !blocked); _dirty = true; }, blocked ? TabOff : NLUi.Bad);
                    break;
                }
            }
        }

        private void SmallButton(Transform parent, string label, Action onClick, Color? color = null)
        {
            var b = NLUi.Button(parent, label, label, _font, onClick, color ?? new Color(0.22f, 0.27f, 0.33f, 1f), 15f, null, 34f);
            NLUi.Size(b, 116f, 34f);
        }

        private void RefreshMessages()
        {
            if (_messagesContent == null) return;
            Clear(_messagesContent);
            var c = _hub?.Get(_conversationId);
            if (c == null) return;
            if (c.Messages.Count == 0)
                Note(_messagesContent, c.Kind == SocialHub.Kind.Direct ? "Hãy gửi lời chào! Tin nhắn được mã hóa trên máy bạn trước khi gửi đi." : "Chưa có tin nhắn. Hãy bắt đầu cuộc trò chuyện!");
            string me = _hub.LocalId;
            foreach (var m in c.Messages)
            {
                bool mine = m.senderPlayerId == me;
                bool system = m.senderPlayerId == "system";
                var row = NLUi.Group(_messagesContent, "Row", false, 0f, mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft, false);
                var bubble = NLUi.Panel(row, "Bubble", system ? SystemTint : mine ? Mine : Theirs, new RectOffset(12, 12, 6, 8), 2f);
                string time = new DateTime(m.sentAtUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("HH:mm");
                string who = system ? "<color=#E8C46A>NihongoLife</color>" : mine ? "<color=#9FD8F0>Bạn</color>" : $"<color=#F2C75B>{m.senderDisplayName}</color>";
                NLUi.Label(bubble, "Meta", $"<b>{who}</b>  <size=80%><color=#8A96A3>{time}</color></size>", 14f, NLUi.Text, _font);
                var text = NLUi.Label(bubble, "Text", m.text, 17f, system ? NLUi.Soft : NLUi.Text, _font);
                text.richText = system; // players cannot inject tags
                var fitter = bubble.gameObject.AddComponent<LayoutElement>();
                fitter.preferredWidth = Mathf.Min(560f, 60f + m.text.Length * 9.5f);
                if (!mine && !system)
                {
                    var button = bubble.gameObject.AddComponent<Button>();
                    string id = m.senderPlayerId, name = m.senderDisplayName;
                    button.onClick.AddListener(() => ShowPlayerCard(id, name));
                }
            }
            Canvas.ForceUpdateCanvases();
            _messagesScroll.verticalNormalizedPosition = 0f;
        }

        private void SendCurrent()
        {
            if (_hub == null || _input == null || _conversationId == null) return;
            string text = _input.text;
            if (string.IsNullOrWhiteSpace(text)) { FocusInput(); return; }
            _hub.Send(_conversationId, text);
            _input.text = string.Empty;
            FocusInput();
        }

        private void FocusInput()
        {
            if (_input == null || !_input.interactable) return;
            _input.Select();
            _input.ActivateInputField();
        }

        // ──────────────────────── Friends pane ────────────────────────

        private TextMeshProUGUI _friendsInfo;

        private void BuildFriendsPane()
        {
            var head = NLUi.Group(_friendsPane, "Head", false, 8f, TextAnchor.MiddleLeft);
            var t = NLUi.Label(head, "Title", "Bạn bè", 20f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Size(t, -1f, 36f, 1f);
            _friendsInfo = NLUi.Label(_friendsPane, "Info", "", 14f, NLUi.Muted, _font);
            var add = NLUi.Group(_friendsPane, "Add", false, 8f, TextAnchor.MiddleLeft);
            _friendIdInput = MakeInput(add, "FriendId", "Nhập ID người chơi để gửi lời mời kết bạn", 64);
            NLUi.Size(_friendIdInput, -1f, 40f, 1f);
            var send = NLUi.Button(add, "AddFriend", "Gửi lời mời", _font, () =>
            {
                string id = _friendIdInput.text.Trim();
                if (id.Length > 0) { _hub.AddFriend(id); _friendIdInput.text = string.Empty; }
            }, NLUi.Gold, 15f, new Color(0.1f, 0.08f, 0.04f), 40f);
            NLUi.Size(send, 140f, 40f);
            NLUi.Divider(_friendsPane);
            _friendsContent = Scroll(_friendsPane, "FriendList", 400f, out _);
        }

        private void RefreshFriendsPane()
        {
            string id = _hub.LocalId ?? "—";
            _friendsInfo.text = _hub.CanUseFriends
                ? $"ID của bạn: <color=#F2C75B>{id}</color>  ·  Mã an toàn tin nhắn: {_hub.SafetyCode}"
                : "Đăng nhập tài khoản để kết bạn và xem bạn bè online. (Chế độ khách vẫn chat khu vực, cộng đồng, nhóm và nhắn riêng.)";
            Clear(_friendsContent);
            if (_hub.Friends.Count == 0)
            {
                Note(_friendsContent, _hub.CanUseFriends ? "Chưa có bạn bè. Đến gần ai đó và nhấn F › Kết bạn." : "Danh sách bạn bè cần đăng nhập.");
                return;
            }
            foreach (var f in _hub.Friends)
            {
                var row = NLUi.Panel(_friendsContent, "Friend", new Color(0.13f, 0.16f, 0.2f, 1f), new RectOffset(12, 12, 8, 8), 8f, false);
                var text = NLUi.Label(row, "Name",
                    $"{(f.Online ? "<color=#5BD98A>●</color>" : "<color=#6B7580>●</color>")} <b>{f.Name}</b>\n<size=80%><color=#9AA5B1>{(f.IncomingRequest ? "muốn kết bạn với bạn" : f.Online ? "Online" + (string.IsNullOrEmpty(f.Scene) ? "" : " · " + f.Scene) : "Offline")}</color></size>",
                    16f, NLUi.Text, _font);
                NLUi.Size(text, -1f, 44f, 1f);
                var friend = f;
                if (f.IncomingRequest)
                {
                    SmallButton(row, "Đồng ý", () => _hub.AcceptFriend(friend), NLUi.Good);
                    SmallButton(row, "Từ chối", () => _hub.RemoveFriend(friend), TabOff);
                    continue;
                }
                if (f.Online)
                {
                    SmallButton(row, "Nhắn", () => { _hub.OpenDirect(friend.UserId, friend.Name); SelectTab(Tab.Direct, SocialHub.DirectId(friend.UserId)); FocusInput(); });
                    SmallButton(row, "Mời nhóm", () => InviteToMyGroup(friend.UserId, friend.Name));
                }
                SmallButton(row, "Xóa bạn", () => _hub.RemoveFriend(friend), NLUi.Bad);
            }
        }

        /// <summary>Invites to the open group, else the first group, else a new group named after the player.</summary>
        private void InviteToMyGroup(string peerId, string peerName)
        {
            string groupId = null;
            if (_conversationId != null && _conversationId.StartsWith("grp:", StringComparison.Ordinal)) groupId = _conversationId.Substring(4);
            if (groupId == null)
            {
                var groups = _hub.ConversationsOf(SocialHub.Kind.Group);
                if (groups.Count > 0) groupId = groups[0].GroupId;
            }
            if (groupId == null)
            {
                var g = _hub.CreateGroup(string.Empty);
                if (g == null) return;
                groupId = g.Id;
            }
            _hub.InviteToGroup(groupId, peerId);
            HudFeed.Post($"Đã mời {peerName} vào nhóm.", HudFeed.Kind.Info, 3f, "invite");
        }

        // ──────────────────────── Player card ────────────────────────

        private void BuildCard()
        {
            _card = NLUi.Panel(_canvas.transform, "PlayerCard", NLUi.Ink, new RectOffset(22, 22, 20, 20), 10f);
            NLUi.Anchor(_card, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(420f, 10f));
            NLUi.FitContent(_card);
            NLUi.CloseButton(_card, _font, HidePlayerCard, 36f, 8f);
            _cardBody = NLUi.Group(_card, "Body", true, 10f);
            _card.gameObject.SetActive(false);
        }

        private void RebuildCard()
        {
            Clear(_cardBody);
            if (_hub == null) GameServices.TryGet(out _hub);
            string id = _cardPlayerId, name = _cardPlayerName;
            NLUi.Label(_cardBody, "Name", name, 26f, NLUi.Gold, _font, FontStyles.Bold);
            var online = GameServices.TryGet(out IOnlineWorldService w) ? w as SupabaseOnlineWorldService : null;
            bool isFriend = _hub != null && _hub.IsFriend(id);
            bool blocked = _hub != null && _hub.IsBlocked(id);
            string where = online != null && online.TryGetOnline(id, out _, out _, out string scene, out _) && !string.IsNullOrEmpty(scene)
                ? "Đang ở " + WorldLocationCatalog.Get(scene).VietnameseName : "Người chơi";
            NLUi.Label(_cardBody, "Sub", $"{where}{(isFriend ? " · <color=#FFD45C>★ Bạn bè</color>" : "")}{(blocked ? " · <color=#FF7A5C>đã chặn</color>" : "")}", 16f, NLUi.Muted, _font);
            if (_hub == null) return;
            CardButton("Nhắn tin riêng (mã hóa)", () => { HidePlayerCard(); _hub.OpenDirect(id, name); Open(Tab.Direct, SocialHub.DirectId(id)); }, NLUi.Gold, true);
            if (!isFriend)
            {
                if (_hub.CanUseFriends) CardButton("Kết bạn", () => { _hub.AddFriend(id); HidePlayerCard(); }, NLUi.Good);
                else CardButton("Kết bạn (cần đăng nhập)", null, TabOff);
            }
            CardButton("Mời vào nhóm", () => { InviteToMyGroup(id, name); HidePlayerCard(); }, new Color(0.22f, 0.27f, 0.33f, 1f));
            CardButton(blocked ? "Bỏ chặn" : "Chặn (ẩn tin nhắn & lời mời)", () => { _hub.SetBlocked(id, !blocked); RebuildCard(); }, blocked ? TabOff : new Color(0.45f, 0.16f, 0.14f, 1f));
        }

        private void CardButton(string label, Action onClick, Color color, bool dark = false)
        {
            var b = NLUi.Button(_cardBody, label, label, _font, onClick, color, 18f, dark ? new Color(0.1f, 0.08f, 0.04f) : (Color?)null, 46f);
            b.interactable = onClick != null;
        }

        // ──────────────────────── Helpers ────────────────────────

        private static void Clear(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }

    internal static class SocialListExtensions
    {
        public static int Count<T>(this IReadOnlyList<T> list, Func<T, bool> predicate)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++) if (predicate(list[i])) n++;
            return n;
        }
    }
}
