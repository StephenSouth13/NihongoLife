using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// Everything social on top of the shared world (SupabaseOnlineWorldService):
    ///
    /// · <b>Khu vực</b> — the people in your shard (area chat lives in the world service).
    /// · <b>Cộng đồng</b> — one room for everyone online ("nl:community").
    /// · <b>Nhóm</b> — private rooms "grp:{id}" you create and invite people to; members are the room's presence.
    /// · <b>Riêng tư</b> — 1:1 messages, end-to-end encrypted (ChatCrypto) and delivered to the other person's
    ///   inbox channel ("inbox:{id}") over the REST broadcast endpoint, so nobody else receives them.
    /// · <b>Bạn bè</b> — the friendships table (FriendService) joined with the lobby for online/offline and place.
    ///
    /// Nothing here is stored on the server: chat is live and disappears when the game closes (as asked). A DM can
    /// only be delivered while both people are online.
    /// </summary>
    public sealed class SocialHub : MonoBehaviour, IGameService
    {
        public enum Kind { Area, Community, Group, Direct }

        public const string AreaId = "area";
        public const string CommunityId = "community";
        public const string CommunityTopic = "nl:community";
        public const int MaxGroupMembers = 20;
        private const int MessagesPerConversation = 120;
        private const string PinsKey = "NL.Chat.Pins.v1";
        private const string BlockedKey = "NL.Chat.Blocked.v1";

        public sealed class Conversation
        {
            public string Id;
            public Kind Kind;
            public string Title;
            public string PeerId;
            public string GroupId;
            public readonly List<OnlineChatMessage> Messages = new List<OnlineChatMessage>();
            public int Unread;
            public DateTime LastActivity = DateTime.UtcNow;
            public bool Encrypted => Kind == Kind.Direct;
        }

        public sealed class Group
        {
            public string Id;
            public string Name;
            public string OwnerId;
            internal SupabaseRealtimeClient.Channel Channel;
            public int MemberCount => Channel?.MemberCount ?? 1;
            public IEnumerable<string> MemberIds => Channel != null ? Channel.Members : Array.Empty<string>();
            public string MemberName(string id) => Channel?.MetaOf(id).Str("n", "Learner") ?? "Learner";
        }

        public sealed class Invite
        {
            public string GroupId, GroupName, FromId, FromName;
            public DateTime At = DateTime.UtcNow;
        }

        public sealed class Friend
        {
            public string UserId, Name, FriendshipId, Scene;
            public bool Online, IncomingRequest, OutgoingRequest;
        }

        private readonly Dictionary<string, Conversation> _conversations = new Dictionary<string, Conversation>();
        private readonly Dictionary<string, Group> _groups = new Dictionary<string, Group>();
        private readonly List<Invite> _invites = new List<Invite>();
        private readonly List<Friend> _friends = new List<Friend>();
        private readonly Dictionary<string, string> _pins = new Dictionary<string, string>();
        private readonly HashSet<string> _blocked = new HashSet<string>();
        private readonly Dictionary<string, List<OnlineChatMessage>> _outbox = new Dictionary<string, List<OnlineChatMessage>>();
        private readonly Dictionary<string, float> _keyRequestedAt = new Dictionary<string, float>();
        private readonly Dictionary<string, string> _names = new Dictionary<string, string>();

        private IOnlineWorldService _world;
        private SupabaseOnlineWorldService _online;
        private SupabaseRealtimeClient _realtime;
        private SupabaseRealtimeClient.Channel _community, _inbox;
        private FriendService _friendService;
        private readonly ChatCrypto _crypto = new ChatCrypto();
        private bool _started, _outboxKicked;
        private float _lastCommunityAt = -10f, _lastGroupAt = -10f, _lastDmAt = -10f;
        private float _friendsDirtyAt = -1f;

        public IReadOnlyList<Invite> Invites => _invites;
        public IReadOnlyList<Friend> Friends => _friends;
        public IEnumerable<Group> Groups => _groups.Values;
        public bool IsRealOnline => _online != null && _online.IsConnected;
        public bool CanUseFriends => _friendService != null && GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated;
        public string LocalId => _world?.LocalPlayerId;
        public string SafetyCode => _crypto.SafetyCode;
        public bool CryptoReady => _crypto.Ready;
        /// <summary>The conversation the chat window shows right now (its messages do not count as unread).</summary>
        public string ActiveConversationId { get; set; }

        public event Action OnConversationsChanged;
        public event Action<Conversation, OnlineChatMessage> OnMessage;
        public event Action OnFriendsChanged;
        public event Action<Invite> OnInvite;

        // ──────────────────────── Lifecycle ────────────────────────

        public void Initialize()
        {
            LoadPins();
            LoadBlocked();
            GetConversation(AreaId, Kind.Area, NihongoLife.Core.Loc.T("Khu vực", "Area", "エリア"));
            GetConversation(CommunityId, Kind.Community, NihongoLife.Core.Loc.T("Cộng đồng", "Community", "コミュニティ"));
            _friendService = GetComponent<FriendService>() ?? FindFirstObjectByType<FriendService>();
            if (_friendService == null) { _friendService = gameObject.AddComponent<FriendService>(); _friendService.Initialize(); }
            _friendService.OnFriendsUpdated += HandleFriendsUpdated;
            _crypto.EnsureKeys(_keySuffix); // first launch makes a key pair in the background, long before the first DM
        }

        /// <summary>Tests: a hub bound to its own world service / realtime client.</summary>
        public void InitializeWith(SupabaseOnlineWorldService online, string keySuffix)
        {
            _keySuffix = keySuffix;
            _world = online;
            _online = online;
            GetConversation(AreaId, Kind.Area, NihongoLife.Core.Loc.T("Khu vực", "Area", "エリア"));
            GetConversation(CommunityId, Kind.Community, NihongoLife.Core.Loc.T("Cộng đồng", "Community", "コミュニティ"));
        }

        private string _keySuffix = string.Empty;
        /// <summary>Tests use a private community room so they never post into the real one.</summary>
        [NonSerialized] public string CommunityTopicOverride;

        private void OnDestroy()
        {
            if (_friendService != null) _friendService.OnFriendsUpdated -= HandleFriendsUpdated;
            if (_world != null) _world.OnChatMessageReceived -= HandleAreaMessage;
            if (_online != null) _online.OnLobbyChanged -= HandleLobbyChanged;
        }

        private void Update()
        {
            _crypto.Pump();
            if (_world == null && GameServices.TryGet(out IOnlineWorldService world)) _world = world;
            if (_online == null) _online = _world as SupabaseOnlineWorldService;
            if (_started && _world != null && _world.IsConnected && _world.LocalPlayerId != _startedFor) StopSocial(); // signed in/out
            if (!_started && _world != null && _world.IsConnected && !string.IsNullOrEmpty(_world.LocalPlayerId)) StartSocial();

            // Messages written while our key pair was still being made.
            if (_crypto.Ready && _outbox.Count > 0 && !_outboxKicked)
            {
                _outboxKicked = true;
                foreach (var peer in new List<string>(_outbox.Keys))
                {
                    if (_pins.ContainsKey(peer)) FlushOutbox(peer);
                    else if (!_keyRequestSent.Contains(peer)) RequestKey(peer);
                }
            }
            if (!_crypto.Ready) _outboxKicked = false;

            // DMs waiting for a key that never came: the other person is offline.
            if (_keyRequestedAt.Count > 0)
            {
                foreach (var peer in new List<string>(_keyRequestedAt.Keys))
                {
                    if (Time.unscaledTime - _keyRequestedAt[peer] < 6f) continue;
                    _keyRequestedAt.Remove(peer);
                    if (_outbox.TryGetValue(peer, out var waiting) && waiting.Count > 0)
                    {
                        _outbox.Remove(peer);
                        AddSystem(DirectId(peer), Tri("{0} không phản hồi (có thể đã offline). Tin nhắn riêng chỉ gửi được khi cả hai cùng online — không lưu trên máy chủ.", "{0} is not responding (maybe offline). Private messages only go through while you are both online — nothing is stored on the server.", "{0}さんから応答がありません（オフラインかもしれません）。個人メッセージは二人ともオンラインのときだけ届き、サーバーには保存されません。", NameOf(peer)));
                    }
                }
            }
            if (_friendsDirtyAt > 0f && Time.unscaledTime >= _friendsDirtyAt)
            {
                _friendsDirtyAt = -1f;
                RebuildFriends();
            }
        }

        private string _startedFor;

        private void StopSocial()
        {
            _started = false;
            _world.OnChatMessageReceived -= HandleAreaMessage;
            if (_online != null) _online.OnLobbyChanged -= HandleLobbyChanged;
            foreach (var id in new List<string>(_groups.Keys)) LeaveGroup(id);
            _community?.Leave();
            _inbox?.Leave();
            _community = _inbox = null;
            _invites.Clear();
            _outbox.Clear();
            _keyRequestedAt.Clear();
            foreach (var id in new List<string>(_conversations.Keys)) if (id.StartsWith("dm:", StringComparison.Ordinal)) _conversations.Remove(id);
            OnConversationsChanged?.Invoke();
        }

        private void StartSocial()
        {
            _started = true;
            _startedFor = _world.LocalPlayerId;
            _world.OnChatMessageReceived += HandleAreaMessage;
            foreach (var m in _world.ChatHistory) HandleAreaMessage(m);
            _crypto.EnsureKeys(_keySuffix);
            if (_online == null) return; // local simulation: area chat only

            _realtime = _online.Realtime;
            _online.OnLobbyChanged += HandleLobbyChanged;
            _online.PreferredShard = PreferredShardFor;
            _community = _realtime.Join(CommunityTopicOverride ?? CommunityTopic, string.Empty);
            _community.OnBroadcast += HandleCommunity;
            _inbox = _realtime.Join("inbox:" + LocalId, string.Empty);
            _inbox.OnBroadcast += HandleInbox;
            if (CanUseFriends) _friendService.RefreshFriends();
        }

        // ──────────────────────── Conversations ────────────────────────

        public IEnumerable<Conversation> Conversations => _conversations.Values;

        public List<Conversation> ConversationsOf(Kind kind)
        {
            var list = new List<Conversation>();
            foreach (var c in _conversations.Values) if (c.Kind == kind) list.Add(c);
            list.Sort((a, b) => b.LastActivity.CompareTo(a.LastActivity));
            return list;
        }

        public Conversation Get(string id) => id != null && _conversations.TryGetValue(id, out var c) ? c : null;

        public int TotalUnread
        {
            get { int n = 0; foreach (var c in _conversations.Values) if (c.Kind != Kind.Area && c.Kind != Kind.Community) n += c.Unread; return n; }
        }

        public int UnreadOf(Kind kind)
        {
            int n = 0;
            foreach (var c in _conversations.Values) if (c.Kind == kind) n += c.Unread;
            return n;
        }

        public void MarkRead(string id)
        {
            var c = Get(id);
            if (c == null || c.Unread == 0) return;
            c.Unread = 0;
            OnConversationsChanged?.Invoke();
        }

        private Conversation GetConversation(string id, Kind kind, string title)
        {
            if (_conversations.TryGetValue(id, out var c)) return c;
            c = new Conversation { Id = id, Kind = kind, Title = title };
            _conversations[id] = c;
            return c;
        }

        public static string DirectId(string peerId) => "dm:" + peerId;
        public static string GroupConversationId(string groupId) => "grp:" + groupId;

        private void Append(Conversation c, OnlineChatMessage m, bool countUnread)
        {
            c.Messages.Add(m);
            while (c.Messages.Count > MessagesPerConversation) c.Messages.RemoveAt(0);
            c.LastActivity = DateTime.UtcNow;
            if (countUnread && ActiveConversationId != c.Id && m.senderPlayerId != LocalId) c.Unread++;
            OnMessage?.Invoke(c, m);
            OnConversationsChanged?.Invoke();
        }

        private OnlineChatMessage Make(string conversationId, string senderId, string senderName, string text, string id = null) => new OnlineChatMessage
        {
            messageId = id ?? Guid.NewGuid().ToString("N"),
            senderPlayerId = senderId,
            senderDisplayName = senderName,
            channelId = conversationId,
            text = text,
            sentAtUtcTicks = DateTime.UtcNow.Ticks
        };

        /// <summary>A system line stored in all three languages, so it follows later language changes.</summary>
        private static string Tri(string vi, string en, string ja, params object[] args) =>
            NihongoLife.Core.Loc.Triple(string.Format(vi, args), string.Format(en, args), string.Format(ja, args));

        private void AddSystem(string conversationId, string text)
        {
            var c = Get(conversationId);
            if (c == null) return;
            Append(c, Make(conversationId, "system", "NihongoLife", text), false);
        }

        private void HandleAreaMessage(OnlineChatMessage m)
        {
            if (m == null || IsBlocked(m.senderPlayerId)) return;
            var c = Get(AreaId);
            if (c.Messages.Exists(x => x.messageId == m.messageId)) return;
            Remember(m.senderPlayerId, m.senderDisplayName);
            Append(c, m, false);
        }

        // ──────────────────────── Sending ────────────────────────

        /// <summary>Sends text to a conversation ("area", "community", "grp:{id}", "dm:{peer}"). Returns false when refused.</summary>
        public bool Send(string conversationId, string text)
        {
            text = SupabaseOnlineWorldService.Clean(text);
            if (text.Length == 0 || _world == null) return false;
            if (conversationId == AreaId || conversationId == "town") { _world.SendChatMessage(AreaId, text); return true; }
            if (_online == null || _realtime == null)
            {
                AddSystem(conversationId, NihongoLife.Core.Loc.Triple("Cần kết nối online để dùng kênh này.", "You need to be online to use this channel.", "このチャンネルを使うにはオンライン接続が必要です。"));
                return false;
            }
            if (conversationId == CommunityId) return SendCommunity(text);
            if (conversationId.StartsWith("grp:", StringComparison.Ordinal)) return SendGroup(conversationId.Substring(4), text);
            if (conversationId.StartsWith("dm:", StringComparison.Ordinal)) return SendDirect(conversationId.Substring(3), text);
            return false;
        }

        private bool TooFast(ref float last, float gap, string conversationId)
        {
            if (Time.unscaledTime - last < gap)
            {
                AddSystem(conversationId, NihongoLife.Core.Loc.Triple("Bạn gửi hơi nhanh — chờ một chút nhé.", "You're sending too fast — wait a moment.", "送信が速すぎます。少し待ってください。"));
                return true;
            }
            last = Time.unscaledTime;
            return false;
        }

        private bool SendCommunity(string text)
        {
            if (TooFast(ref _lastCommunityAt, 2f, CommunityId)) return false;
            var m = Make(CommunityId, LocalId, _online.LocalDisplayName, text);
            Append(Get(CommunityId), m, false);
            _community.Send("c", new Dictionary<string, object> { ["i"] = LocalId, ["n"] = m.senderDisplayName, ["t"] = text, ["id"] = m.messageId });
            return true;
        }

        private void HandleCommunity(string evt, Dictionary<string, object> p)
        {
            if (evt != "c") return;
            string id = p.Str("i");
            if (string.IsNullOrEmpty(id) || id == LocalId || IsBlocked(id)) return;
            string text = SupabaseOnlineWorldService.Clean(p.Str("t", ""));
            if (text.Length == 0) return;
            string name = SupabaseOnlineWorldService.Clean(p.Str("n", "Learner"));
            Remember(id, name);
            Append(Get(CommunityId), Make(CommunityId, id, name, text, p.Str("id")), true);
        }

        // ──────────────────────── Groups ────────────────────────

        public Group CreateGroup(string name)
        {
            if (_online == null) return null;
            name = SupabaseOnlineWorldService.Clean(name);
            if (name.Length == 0) name = _online.LocalDisplayName + "'s group";
            if (name.Length > 32) name = name.Substring(0, 32);
            string id = Guid.NewGuid().ToString("N").Substring(0, 10);
            var g = JoinGroup(id, name, LocalId);
            AddSystem(GroupConversationId(id), Tri("Đã tạo nhóm “{0}”. Mời bạn bè từ tab Bạn bè hoặc khi đứng gần ai đó (F).", "Group “{0}” created. Invite friends from the Friends tab or when standing next to someone (F).", "グループ「{0}」を作りました。友だちタブから、または近くの人に（F）招待できます。", name));
            return g;
        }

        private Group JoinGroup(string id, string name, string ownerId)
        {
            if (_groups.TryGetValue(id, out var existing)) return existing;
            var g = new Group { Id = id, Name = name, OwnerId = ownerId };
            g.Channel = _realtime.Join("grp:" + id, LocalId);
            g.Channel.Track(new Dictionary<string, object> { ["n"] = _online.LocalDisplayName, ["gn"] = name });
            g.Channel.OnBroadcast += (evt, p) => HandleGroup(g, evt, p);
            g.Channel.OnPresenceJoin += (key, meta) =>
            {
                Remember(key, meta.Str("n", "Learner"));
                if (key != LocalId && !_announced.Contains(g.Id + key)) { _announced.Add(g.Id + key); AddSystem(GroupConversationId(id), Tri("{0} đã vào nhóm.", "{0} joined the group.", "{0}さんがグループに入りました。", meta.Str("n", "Learner"))); }
                OnConversationsChanged?.Invoke();
            };
            g.Channel.OnPresenceLeave += key =>
            {
                _announced.Remove(g.Id + key);
                if (key != LocalId) AddSystem(GroupConversationId(id), Tri("{0} đã rời nhóm.", "{0} left the group.", "{0}さんがグループを抜けました。", NameOf(key)));
            };
            _groups[id] = g;
            GetConversation(GroupConversationId(id), Kind.Group, name).GroupId = id;
            OnConversationsChanged?.Invoke();
            return g;
        }

        private readonly HashSet<string> _announced = new HashSet<string>();

        public Group GetGroup(string id) => id != null && _groups.TryGetValue(id, out var g) ? g : null;

        public void LeaveGroup(string id)
        {
            if (!_groups.TryGetValue(id, out var g)) return;
            g.Channel?.Leave();
            _groups.Remove(id);
            _conversations.Remove(GroupConversationId(id));
            if (ActiveConversationId == GroupConversationId(id)) ActiveConversationId = null;
            OnConversationsChanged?.Invoke();
        }

        public bool InviteToGroup(string groupId, string peerId)
        {
            var g = GetGroup(groupId);
            if (g == null || string.IsNullOrEmpty(peerId) || peerId == LocalId) return false;
            if (g.MemberCount >= MaxGroupMembers) { AddSystem(GroupConversationId(groupId), Tri("Nhóm đã đủ {0} người.", "The group is full ({0} people).", "グループは{0}人でいっぱいです。", MaxGroupMembers)); return false; }
            SendInbox(peerId, "inv", new Dictionary<string, object> { ["g"] = g.Id, ["gn"] = g.Name });
            AddSystem(GroupConversationId(groupId), Tri("Đã gửi lời mời tới {0}.", "Invitation sent to {0}.", "{0}さんに招待を送りました。", NameOf(peerId)));
            return true;
        }

        public void AcceptInvite(Invite invite)
        {
            if (invite == null || _online == null) return;
            _invites.Remove(invite);
            JoinGroup(invite.GroupId, invite.GroupName, invite.FromId);
            ActiveConversationId = GroupConversationId(invite.GroupId);
            OnConversationsChanged?.Invoke();
        }

        public void DeclineInvite(Invite invite)
        {
            _invites.Remove(invite);
            OnConversationsChanged?.Invoke();
        }

        private bool SendGroup(string groupId, string text)
        {
            var g = GetGroup(groupId);
            if (g == null) return false;
            if (TooFast(ref _lastGroupAt, 0.6f, GroupConversationId(groupId))) return false;
            var m = Make(GroupConversationId(groupId), LocalId, _online.LocalDisplayName, text);
            Append(Get(m.channelId), m, false);
            g.Channel.Send("c", new Dictionary<string, object> { ["i"] = LocalId, ["n"] = m.senderDisplayName, ["t"] = text, ["id"] = m.messageId });
            return true;
        }

        private void HandleGroup(Group g, string evt, Dictionary<string, object> p)
        {
            if (evt != "c") return;
            string id = p.Str("i");
            if (string.IsNullOrEmpty(id) || id == LocalId || IsBlocked(id)) return;
            string text = SupabaseOnlineWorldService.Clean(p.Str("t", ""));
            if (text.Length == 0) return;
            string name = SupabaseOnlineWorldService.Clean(p.Str("n", "Learner"));
            Remember(id, name);
            Append(Get(GroupConversationId(g.Id)), Make(GroupConversationId(g.Id), id, name, text, p.Str("id")), true);
        }

        /// <summary>Keeps groups together: the shard of a group mate already in this place.</summary>
        private int PreferredShardFor(string scene)
        {
            if (_online == null) return -1;
            string prefix = "w:" + SupabaseOnlineWorldService.SceneKey(scene) + ":";
            foreach (var g in _groups.Values)
                foreach (var id in g.MemberIds)
                {
                    if (id == LocalId) continue;
                    if (_online.TryGetOnline(id, out _, out _, out _, out string shard) && shard.StartsWith(prefix, StringComparison.Ordinal)
                        && int.TryParse(shard.Substring(prefix.Length), out int n)) return n;
                }
            return -1;
        }

        // ──────────────────────── Direct (E2E) ────────────────────────

        public Conversation OpenDirect(string peerId, string peerName)
        {
            if (string.IsNullOrEmpty(peerId) || peerId == LocalId) return null;
            Remember(peerId, peerName);
            var c = GetConversation(DirectId(peerId), Kind.Direct, NameOf(peerId));
            c.PeerId = peerId;
            c.Title = NameOf(peerId);
            OnConversationsChanged?.Invoke();
            return c;
        }

        private readonly Dictionary<string, float> _incomingCalls = new Dictionary<string, float>();

        /// <summary>The peer rang us in the last minute.</summary>
        public bool HasIncomingCall(string peerId) => _incomingCalls.TryGetValue(peerId, out float at) && Time.unscaledTime - at < 60f;

        public void ClearIncomingCall(string peerId) => _incomingCalls.Remove(peerId);

        /// <summary>Rings the other person (they join the same private voice room when they press “Nghe”).</summary>
        public void NotifyCall(string peerId)
        {
            if (_online == null || string.IsNullOrEmpty(peerId)) return;
            SendInbox(peerId, "call", new Dictionary<string, object>());
            AddSystem(DirectId(peerId), Tri("Đang gọi {0}…", "Calling {0}…", "{0}さんに発信中…", NameOf(peerId)));
        }

        public string PeerSafetyCode(string peerId) => _pins.TryGetValue(peerId, out var pk) ? ChatCrypto.SafetyCodeOf(pk) : null;

        private bool SendDirect(string peerId, string text)
        {
            if (IsBlocked(peerId)) { AddSystem(DirectId(peerId), NihongoLife.Core.Loc.Triple("Bạn đã chặn người này. Bỏ chặn để nhắn tin.", "You blocked this person. Unblock them to send messages.", "この人をブロックしています。メッセージを送るにはブロックを解除してください。")); return false; }
            if (TooFast(ref _lastDmAt, 0.5f, DirectId(peerId))) return false;
            var c = OpenDirect(peerId, NameOf(peerId));
            var m = Make(c.Id, LocalId, _online.LocalDisplayName, text);
            Append(c, m, false);
            if (!_online.IsOnline(peerId))
            {
                AddSystem(c.Id, Tri("{0} đang offline — tin nhắn riêng chỉ gửi được khi cả hai cùng online.", "{0} is offline — private messages only go through while you are both online.", "{0}さんはオフラインです。個人メッセージは二人ともオンラインのときだけ届きます。", NameOf(peerId)));
                return false;
            }
            if (!_crypto.Ready)
            {
                _outboxKicked = false;
                QueueDirect(peerId, m);
                return true;
            }
            if (_pins.TryGetValue(peerId, out var pk)) DeliverDirect(peerId, pk, m);
            else QueueDirect(peerId, m);
            return true;
        }

        private void QueueDirect(string peerId, OnlineChatMessage m)
        {
            if (!_outbox.TryGetValue(peerId, out var list)) _outbox[peerId] = list = new List<OnlineChatMessage>();
            list.Add(m);
            if (!_keyRequestedAt.ContainsKey(peerId)) RequestKey(peerId);
        }

        private void RequestKey(string peerId)
        {
            _keyRequestedAt[peerId] = Time.unscaledTime;
            if (!_crypto.Ready) return; // sent from Update once the key pair exists
            _keyRequestSent.Add(peerId);
            SendInbox(peerId, "kq", new Dictionary<string, object> { ["pk"] = _crypto.PublicKey });
        }

        private void DeliverDirect(string peerId, string peerKey, OnlineChatMessage m)
        {
            Dictionary<string, object> envelope;
            try { envelope = _crypto.Seal(m.text, peerKey); }
            catch (Exception ex) { AddSystem(DirectId(peerId), Tri("Không mã hóa được tin nhắn: {0}", "Could not encrypt the message: {0}", "メッセージを暗号化できませんでした：{0}", ex.Message)); return; }
            envelope["id"] = m.messageId;
            envelope["pk"] = _crypto.PublicKey;
            SendInbox(peerId, "dm", envelope);
        }

        private readonly HashSet<string> _keyRequestSent = new HashSet<string>();

        private void FlushOutbox(string peerId)
        {
            _keyRequestedAt.Remove(peerId);
            _keyRequestSent.Remove(peerId);
            if (!_outbox.TryGetValue(peerId, out var list) || !_pins.TryGetValue(peerId, out var pk)) return;
            _outbox.Remove(peerId);
            foreach (var m in list) DeliverDirect(peerId, pk, m);
        }

        private void SendInbox(string peerId, string evt, Dictionary<string, object> payload)
        {
            payload["f"] = LocalId;
            payload["n"] = _online.LocalDisplayName;
            _realtime.PostBroadcast("inbox:" + peerId, evt, payload);
        }

        private void HandleInbox(string evt, Dictionary<string, object> p)
        {
            string from = p.Str("f");
            if (string.IsNullOrEmpty(from) || from == LocalId || IsBlocked(from)) return;
            string name = SupabaseOnlineWorldService.Clean(p.Str("n", "Learner"));
            Remember(from, name);
            if (SupabaseRealtimeClient.Verbose) Debug.Log($"[SocialHub] {LocalId}: inbox {evt} from {from} (pinned: {_pins.ContainsKey(from)}, keys ready: {_crypto.Ready})");
            switch (evt)
            {
                case "kq": // key request: pin theirs, answer with ours
                    if (PinKey(from, p.Str("pk")) && _crypto.Ready)
                        SendInbox(from, "ka", new Dictionary<string, object> { ["pk"] = _crypto.PublicKey });
                    break;
                case "ka":
                    if (PinKey(from, p.Str("pk"))) FlushOutbox(from);
                    break;
                case "dm":
                {
                    if (!PinKey(from, p.Str("pk"))) return;
                    string text = _crypto.Open(p, _pins[from]);
                    if (text == null)
                    {
                        Debug.LogWarning($"[SocialHub] Dropped a private message from {from}: signature or MAC check failed.");
                        return;
                    }
                    text = SupabaseOnlineWorldService.Clean(text);
                    var c = OpenDirect(from, name);
                    if (c.Messages.Exists(x => x.messageId == p.Str("id"))) return;
                    Append(c, Make(c.Id, from, name, text, p.Str("id")), true);
                    if (ActiveConversationId != c.Id) NihongoLife.UI.HudFeed.Post(NihongoLife.Core.Loc.F("Tin riêng từ {0}: {1}", "Message from {0}: {1}", "{0}さんからのメッセージ：{1}", name, Shorten(text)), NihongoLife.UI.HudFeed.Kind.Info, 4f, "dm");
                    break;
                }
                case "inv":
                {
                    string g = p.Str("g", "");
                    if (g.Length == 0 || _groups.ContainsKey(g) || _invites.Exists(x => x.GroupId == g)) return;
                    var invite = new Invite { GroupId = g, GroupName = SupabaseOnlineWorldService.Clean(p.Str("gn", NihongoLife.Core.Loc.T("Nhóm", "Group", "グループ"))), FromId = from, FromName = name };
                    _invites.Add(invite);
                    OnInvite?.Invoke(invite);
                    OnConversationsChanged?.Invoke();
                    NihongoLife.UI.HudFeed.Post(NihongoLife.Core.Loc.F("{0} mời bạn vào nhóm “{1}” — mở Chat (Enter) › Nhóm.", "{0} invites you to the group “{1}” — open Chat (Enter) › Groups.", "{0}さんが「{1}」に招待しています — チャット（Enter）› グループ", name, invite.GroupName), NihongoLife.UI.HudFeed.Kind.Info, 6f, "invite");
                    break;
                }
                case "call":
                    _incomingCalls[from] = Time.unscaledTime;
                    OpenDirect(from, name);
                    AddSystem(DirectId(from), Tri("{0} đang gọi thoại — bấm “Nghe” để trả lời.", "{0} is calling — press “Answer” to pick up.", "{0}さんから音声通話です。「出る」を押してください。", name));
                    NihongoLife.UI.HudFeed.Post(NihongoLife.Core.Loc.F("{0} đang gọi thoại cho bạn — Chat › Riêng tư › Nghe.", "{0} is calling you — Chat › Private › Answer.", "{0}さんから通話 — チャット › 個人 › 出る", name), NihongoLife.UI.HudFeed.Kind.Info, 8f, "call");
                    break;
                case "fr":
                    NihongoLife.UI.HudFeed.Post(NihongoLife.Core.Loc.F("{0} muốn kết bạn với bạn — xem tab Bạn bè.", "{0} wants to be friends — see the Friends tab.", "{0}さんから友だち申請 — 友だちタブを見てください。", name), NihongoLife.UI.HudFeed.Kind.Info, 6f, "friend");
                    if (CanUseFriends) _friendService.RefreshFriends();
                    break;
            }
        }

        /// <summary>Trust on first use; a changed key is accepted but announced in the conversation.</summary>
        private bool PinKey(string peerId, string publicKey)
        {
            if (!ChatCrypto.IsValidPublicKey(publicKey)) return false;
            if (_pins.TryGetValue(peerId, out var known))
            {
                if (known == publicKey) return true;
                OpenDirect(peerId, NameOf(peerId));
                AddSystem(DirectId(peerId), Tri("⚠ Khóa bảo mật của {0} đã đổi (cài lại game hoặc máy khác). Mã an toàn mới: {1}.", "⚠ {0}'s security key changed (reinstalled the game or a different device). New safety code: {1}.", "⚠ {0}さんのセキュリティキーが変わりました（再インストールか別の端末）。新しい安全コード：{1}", NameOf(peerId), ChatCrypto.SafetyCodeOf(publicKey)));
            }
            _pins[peerId] = publicKey;
            SavePins();
            return true;
        }

        // ──────────────────────── Friends ────────────────────────

        public void RefreshFriends()
        {
            if (CanUseFriends) _friendService.RefreshFriends();
            else RebuildFriends();
        }

        public void AddFriend(string peerId)
        {
            if (!CanUseFriends || string.IsNullOrEmpty(peerId)) return;
            _friendService.SendFriendRequest(peerId, ok =>
            {
                NihongoLife.UI.HudFeed.Post(ok ? NihongoLife.Core.Loc.F("Đã gửi lời mời kết bạn tới {0}.", "Friend request sent to {0}.", "{0}さんに友だち申請を送りました。", NameOf(peerId)) : NihongoLife.Core.Loc.T("Không gửi được lời mời kết bạn.", "Could not send the friend request.", "友だち申請を送れませんでした。"), ok ? NihongoLife.UI.HudFeed.Kind.Info : NihongoLife.UI.HudFeed.Kind.Warning, 4f, "friend");
                if (ok && _online != null) SendInbox(peerId, "fr", new Dictionary<string, object>());
            });
        }

        public void AcceptFriend(Friend f) { if (CanUseFriends && f?.FriendshipId != null) _friendService.AcceptFriendRequest(f.FriendshipId); }
        public void RemoveFriend(Friend f) { if (CanUseFriends && f?.FriendshipId != null) _friendService.RemoveFriend(f.FriendshipId); }

        public bool IsFriend(string id) => _friends.Exists(f => f.UserId == id && !f.IncomingRequest && !f.OutgoingRequest);

        private void HandleFriendsUpdated() => RebuildFriends();

        private void HandleLobbyChanged()
        {
            // The lobby can change many times a second with lots of players: rebuild at most twice a second.
            if (_friendsDirtyAt < 0f) _friendsDirtyAt = Time.unscaledTime + 0.5f;
        }

        private void RebuildFriends()
        {
            _friends.Clear();
            if (_friendService != null && CanUseFriends)
            {
                string me = LocalId;
                foreach (var e in _friendService.Friends) _friends.Add(MakeFriend(e.friendUserId, e.id, false, false));
                foreach (var e in _friendService.PendingRequests) _friends.Add(MakeFriend(e.requester_id == me ? e.addressee_id : e.requester_id, e.id, true, false));
            }
            _friends.Sort((a, b) => a.IncomingRequest != b.IncomingRequest ? (a.IncomingRequest ? -1 : 1)
                : a.Online != b.Online ? (a.Online ? -1 : 1) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            OnFriendsChanged?.Invoke();
        }

        private Friend MakeFriend(string userId, string friendshipId, bool incoming, bool outgoing)
        {
            var f = new Friend { UserId = userId, FriendshipId = friendshipId, IncomingRequest = incoming, OutgoingRequest = outgoing, Name = NameOf(userId) };
            if (_online != null && _online.TryGetOnline(userId, out string n, out _, out string scene, out _))
            {
                f.Online = true;
                f.Name = string.IsNullOrEmpty(n) ? f.Name : n;
                f.Scene = string.IsNullOrEmpty(scene) ? "" : WorldLocationCatalog.Get(scene).VietnameseName;
                Remember(userId, n);
            }
            return f;
        }

        /// <summary>Tests/tools: pretend these ids are accepted friends (no Supabase auth needed).</summary>
        public void DebugSetFriends(IEnumerable<string> ids)
        {
            _friends.Clear();
            foreach (var id in ids) _friends.Add(MakeFriend(id, null, false, false));
            _friends.Sort((a, b) => a.Online != b.Online ? (a.Online ? -1 : 1) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            OnFriendsChanged?.Invoke();
        }

        // ──────────────────────── Block / names ────────────────────────

        public bool IsBlocked(string id) => !string.IsNullOrEmpty(id) && _blocked.Contains(id);

        public void SetBlocked(string id, bool blocked)
        {
            if (string.IsNullOrEmpty(id) || id == LocalId) return;
            if (blocked) _blocked.Add(id); else _blocked.Remove(id);
            PlayerPrefs.SetString(BlockedKey, string.Join(",", _blocked));
            PlayerPrefs.Save();
            OnConversationsChanged?.Invoke();
        }

        public string NameOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Learner";
            if (_names.TryGetValue(id, out var n)) return n;
            if (_online != null && _online.TryGetOnline(id, out string ln, out _, out _, out _) && !string.IsNullOrEmpty(ln)) return ln;
            return "Learner·" + (id.Length > 4 ? id.Substring(0, 4) : id);
        }

        private void Remember(string id, string name)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(name) || id == "system") return;
            _names[id] = name;
            var c = Get(DirectId(id));
            if (c != null) c.Title = name;
        }

        private static string Shorten(string s) => s.Length > 48 ? s.Substring(0, 47) + "…" : s;

        private void LoadPins()
        {
            var o = MiniJson.ParseObject(PlayerPrefs.GetString(PinsKey + _keySuffix, "{}"));
            if (o == null) return;
            foreach (var kv in o) if (kv.Value is string s) _pins[kv.Key] = s;
        }

        private void SavePins()
        {
            var o = new Dictionary<string, object>();
            foreach (var kv in _pins) o[kv.Key] = kv.Value;
            PlayerPrefs.SetString(PinsKey + _keySuffix, MiniJson.Serialize(o));
            PlayerPrefs.Save();
        }

        private void LoadBlocked()
        {
            foreach (var id in PlayerPrefs.GetString(BlockedKey, "").Split(',')) if (id.Length > 0) _blocked.Add(id);
        }
    }
}
