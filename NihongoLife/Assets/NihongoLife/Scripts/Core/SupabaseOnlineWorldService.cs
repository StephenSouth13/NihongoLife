using System;
using System.Collections.Generic;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// The shared world on Supabase Realtime, built to stay smooth with many players online:
    ///
    /// · <b>Lobby</b> ("nl:lobby", presence only): who is online, in which place and shard. Re-tracked only when the
    ///   player changes place, so it costs nothing while walking. Friends' online/offline comes from here.
    /// · <b>Shards</b> ("w:{scene}:{n}"): a place is split into rooms of at most <see cref="ShardCapacity"/> players.
    ///   You only receive the people in your shard, so traffic per player stays flat whether 50 or 5000 are online.
    ///   Group members are placed in the same shard.
    /// · <b>Pose</b>: broadcast "p" with position in centimetres and yaw in degrees, ~6 Hz while moving and every 3 s
    ///   while standing still. Remote avatars interpolate ~150 ms behind (RemotePlayerAvatar).
    /// · <b>Area chat</b>: broadcast "c" in the shard — live only, nothing stored.
    /// Bedrooms are private: no shard is joined there.
    /// </summary>
    public class SupabaseOnlineWorldService : MonoBehaviour, IOnlineWorldService
    {
        public const int ShardCapacity = 40;
        public const string LobbyTopic = "nl:lobby";
        public const int MaxChatLength = 200;

        private readonly List<OnlinePlayerSnapshot> _visiblePlayers = new List<OnlinePlayerSnapshot>();
        private readonly Dictionary<string, OnlinePlayerSnapshot> _byId = new Dictionary<string, OnlinePlayerSnapshot>();
        private readonly List<OnlineChatMessage> _chatHistory = new List<OnlineChatMessage>();

        private string _localPlayerId;
        private string _localDisplayName;
        private string _localCharacterId;
        private int _chatHistoryLimit = 80;
        private SupabaseRealtimeClient _realtime;
        private SupabaseRealtimeClient.Channel _lobby, _shard;
        private OnlinePlayerSnapshot _local;

        private string _scene;
        private string _pendingScene;
        private float _shardDecisionAt = -1f;
        private bool _lobbySynced;

        // Pose sending
        private Vector3 _lastSentPos;
        private float _lastSentYaw;
        private float _lastSentAt = -10f;
        private int _lastSentMove;
        private const float MovingInterval = 1f / 6f;
        private const float IdleInterval = 3f;
        private float _lastChatAt = -10f;

        public bool IsConnected { get; private set; }
        public string LocalPlayerId => _localPlayerId;
        public string LocalDisplayName => _localDisplayName;
        public string LocalCharacterId { get => _localCharacterId; set => _localCharacterId = value; }
        public IReadOnlyList<OnlinePlayerSnapshot> VisiblePlayers => _visiblePlayers;
        public IReadOnlyList<OnlineChatMessage> ChatHistory => _chatHistory;
        public SupabaseRealtimeClient Realtime => _realtime;
        public string ShardTopic => _shard?.Topic;
        public int ShardMemberCount => _shard?.MemberCount ?? 0;
        public int OnlineCount => _lobby?.MemberCount ?? 0;
        public bool LobbySynced => _lobbySynced;

        /// <summary>Tests and bots pass their own id instead of the signed-in account.</summary>
        [NonSerialized] public bool UseAuthIdentity = true;

        /// <summary>Lets the social layer keep a group together: given a place, return a shard index to prefer (or -1).</summary>
        public Func<string, int> PreferredShard;

        public event Action<OnlinePlayerSnapshot> OnPlayerJoinedOrUpdated;
        public event Action<string> OnPlayerLeft;
        public event Action<OnlineChatMessage> OnChatMessageReceived;
        /// <summary>Someone came online, went offline or moved to another place.</summary>
        public event Action OnLobbyChanged;
        public event Action<string> OnShardChanged;

        // ──────────────────────── Lifecycle ────────────────────────

        public void Initialize()
        {
            _realtime = GetComponent<SupabaseRealtimeClient>() ?? FindFirstObjectByType<SupabaseRealtimeClient>();
            if (GameServices.TryGet(out GameControlService control) && control.Database != null)
                _chatHistoryLimit = Mathf.Max(10, control.Database.chatHistoryLimit);
            if (GetComponent<RemotePlayerManager>() == null) gameObject.AddComponent<RemotePlayerManager>();
        }

        /// <summary>Tests: run against a client that is not on AppRoot.</summary>
        public void InitializeWith(SupabaseRealtimeClient client)
        {
            _realtime = client;
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        // ──────────────────────── Connect ────────────────────────

        public void ConnectLocalPlayer(string playerId, string displayName)
        {
            _localPlayerId = string.IsNullOrWhiteSpace(playerId) ? Guid.NewGuid().ToString("N") : playerId;
            _localDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Learner" : displayName;
            if (UseAuthIdentity && GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated)
            {
                _localPlayerId = auth.UserId;
                if (!PlayableCharacterCatalog.IsPlaceholderName(auth.DisplayName)) _localDisplayName = auth.DisplayName.Trim();
            }
            if (string.IsNullOrEmpty(_localCharacterId) || UseAuthIdentity) _localCharacterId = PlayableCharacterCatalog.GetSelected().Id;
            if (_realtime == null) { Debug.LogWarning("[OnlineWorld] No realtime client."); return; }

            IsConnected = true;
            _local = new OnlinePlayerSnapshot { playerId = _localPlayerId, displayName = _localDisplayName, characterId = _localCharacterId };
            _visiblePlayers.Add(_local);
            _byId[_localPlayerId] = _local;

            _lobbySynced = false;
            _lobby = _realtime.Join(LobbyTopic, _localPlayerId);
            _lobby.OnPresenceSync += HandleLobbySync;
            _lobby.OnPresenceJoin += HandleLobbyJoin;
            _lobby.OnPresenceLeave += HandleLobbyLeave;
            TrackLobby();
            Debug.Log($"[OnlineWorld] {_localDisplayName} ({_localPlayerId}) going online.");
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            IsConnected = false;
            LeaveShard();
            if (_lobby != null)
            {
                _lobby.OnPresenceSync -= HandleLobbySync;
                _lobby.OnPresenceJoin -= HandleLobbyJoin;
                _lobby.OnPresenceLeave -= HandleLobbyLeave;
                _lobby.Leave();
                _lobby = null;
            }
            _visiblePlayers.Clear();
            _byId.Clear();
            if (_localPlayerId != null) OnPlayerLeft?.Invoke(_localPlayerId);
        }

        /// <summary>Character or name changed in the menu: tell everyone.</summary>
        public void RefreshLocalIdentity(string displayName, string characterId)
        {
            if (!string.IsNullOrWhiteSpace(displayName)) _localDisplayName = displayName.Trim();
            if (!string.IsNullOrWhiteSpace(characterId)) _localCharacterId = characterId;
            if (_local != null) { _local.displayName = _localDisplayName; _local.characterId = _localCharacterId; }
            TrackLobby();
            TrackShard();
        }

        // ──────────────────────── Lobby ────────────────────────

        private void TrackLobby()
        {
            _lobby?.Track(new Dictionary<string, object>
            {
                ["n"] = _localDisplayName,
                ["c"] = _localCharacterId,
                ["s"] = _scene ?? string.Empty,
                ["sh"] = _shard != null ? _shard.Topic : string.Empty
            });
        }

        private void HandleLobbySync()
        {
            _lobbySynced = true;
            OnLobbyChanged?.Invoke();
        }

        private void HandleLobbyJoin(string key, Dictionary<string, object> meta) => OnLobbyChanged?.Invoke();
        private void HandleLobbyLeave(string key) => OnLobbyChanged?.Invoke();

        public bool IsOnline(string playerId) => _lobby != null && _lobby.MetaOf(playerId) != null;

        /// <summary>Lobby info of an online player: name, character, place, shard topic. Null when offline.</summary>
        public bool TryGetOnline(string playerId, out string displayName, out string characterId, out string scene, out string shard)
        {
            var meta = _lobby?.MetaOf(playerId);
            displayName = meta.Str("n", "");
            characterId = meta.Str("c", "");
            scene = meta.Str("s", "");
            shard = meta.Str("sh", "");
            return meta != null;
        }

        public IEnumerable<string> OnlinePlayerIds => _lobby != null ? _lobby.Members : Array.Empty<string>();

        // ──────────────────────── Shards ────────────────────────

        public static bool IsPrivateScene(string scene) =>
            string.IsNullOrEmpty(scene) || scene.IndexOf("Bedroom", StringComparison.OrdinalIgnoreCase) >= 0
            || scene.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 || scene.IndexOf("Boot", StringComparison.OrdinalIgnoreCase) >= 0;

        public static string SceneKey(string scene)
        {
            var sb = new System.Text.StringBuilder(scene.Length);
            foreach (char c in scene) sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
            return sb.ToString();
        }

        private void BeginSceneChange(string scene)
        {
            _scene = scene;
            LeaveShard();
            _pendingScene = IsPrivateScene(scene) ? null : scene;
            // Wait (briefly) for the lobby so we can count who is where before picking a shard.
            _shardDecisionAt = _pendingScene == null ? -1f : Time.unscaledTime + (_lobbySynced ? 0f : 2.5f);
            TrackLobby();
        }

        private void Update()
        {
            if (!IsConnected) return;
            if (_pendingScene != null && (_lobbySynced || Time.unscaledTime >= _shardDecisionAt))
            {
                string scene = _pendingScene;
                _pendingScene = null;
                JoinShard(scene, ChooseShard(scene));
            }
            // Drop remote players that went silent (crash, lost connection before presence noticed).
            if (Time.frameCount % 60 == 0)
            {
                long cutoff = DateTime.UtcNow.AddSeconds(-20).Ticks;
                for (int i = _visiblePlayers.Count - 1; i >= 0; i--)
                {
                    var p = _visiblePlayers[i];
                    if (p == _local || p.serverTick > cutoff || (_shard != null && _shard.MetaOf(p.playerId) != null)) continue;
                    RemovePlayer(p.playerId);
                }
            }
        }

        /// <summary>Lowest shard of this place with room; a group mate's shard first.</summary>
        public int ChooseShard(string scene)
        {
            string prefix = "w:" + SceneKey(scene) + ":";
            int preferred = PreferredShard != null ? PreferredShard(scene) : -1;
            var counts = new Dictionary<int, int>();
            if (_lobby != null)
            {
                foreach (var id in _lobby.Members)
                {
                    if (id == _localPlayerId) continue;
                    string sh = _lobby.MetaOf(id).Str("sh", "");
                    if (sh.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(sh.Substring(prefix.Length), out int n))
                        counts[n] = counts.TryGetValue(n, out int c) ? c + 1 : 1;
                }
            }
            if (preferred >= 0 && (!counts.TryGetValue(preferred, out int pc) || pc < ShardCapacity + 4)) return preferred; // a little slack for friends
            for (int n = 0; ; n++)
                if (!counts.TryGetValue(n, out int c) || c < ShardCapacity) return n;
        }

        public static string ShardTopicFor(string scene, int shard) => "w:" + SceneKey(scene) + ":" + shard;

        private void JoinShard(string scene, int index)
        {
            LeaveShard();
            _shard = _realtime.Join(ShardTopicFor(scene, index), _localPlayerId);
            _shard.OnBroadcast += HandleShardBroadcast;
            _shard.OnPresenceJoin += HandleShardJoin;
            _shard.OnPresenceLeave += HandleShardLeave;
            _shard.OnJoined += HandleShardJoined;
            if (_local != null) { _local.sceneName = scene; _local.shardTopic = _shard.Topic; }
            TrackShard();
            TrackLobby();
            _lastSentAt = -10f; // send our position right away
            OnShardChanged?.Invoke(_shard.Topic);
            Debug.Log($"[OnlineWorld] Joined {_shard.Topic}");
        }

        private void HandleShardJoined()
        {
            _lastSentAt = -10f;
            // The room may have filled up while we were choosing: move on if there are far too many.
            if (_shard != null && _shard.MemberCount > ShardCapacity + 8 && _scene != null)
            {
                string topic = _shard.Topic;
                int n = int.Parse(topic.Substring(topic.LastIndexOf(':') + 1));
                JoinShard(_scene, n + 1);
            }
        }

        private void TrackShard()
        {
            _shard?.Track(new Dictionary<string, object> { ["n"] = _localDisplayName, ["c"] = _localCharacterId });
        }

        private void LeaveShard()
        {
            if (_shard == null) return;
            _shard.OnBroadcast -= HandleShardBroadcast;
            _shard.OnPresenceJoin -= HandleShardJoin;
            _shard.OnPresenceLeave -= HandleShardLeave;
            _shard.OnJoined -= HandleShardJoined;
            _shard.Leave();
            _shard = null;
            for (int i = _visiblePlayers.Count - 1; i >= 0; i--)
                if (_visiblePlayers[i] != _local) RemovePlayer(_visiblePlayers[i].playerId);
        }

        private void HandleShardJoin(string key, Dictionary<string, object> meta)
        {
            if (key == _localPlayerId) return;
            var p = GetOrAdd(key);
            p.displayName = meta.Str("n", p.displayName ?? "Learner");
            p.characterId = meta.Str("c", p.characterId);
            p.sceneName = _scene;
            p.roomId = _scene;
            p.shardTopic = _shard?.Topic;
            if (p.hasPose) OnPlayerJoinedOrUpdated?.Invoke(p);
            _lastSentAt = -10f; // newcomers see us without waiting for the idle keep-alive
        }

        private void HandleShardLeave(string key) => RemovePlayer(key);

        private OnlinePlayerSnapshot GetOrAdd(string id)
        {
            if (_byId.TryGetValue(id, out var p)) return p;
            p = new OnlinePlayerSnapshot { playerId = id, hasPose = false, sceneName = _scene, roomId = _scene, displayName = "Learner" };
            _byId[id] = p;
            _visiblePlayers.Add(p);
            return p;
        }

        private void RemovePlayer(string id)
        {
            if (!_byId.TryGetValue(id, out var p) || p == _local) return;
            _byId.Remove(id);
            _visiblePlayers.Remove(p);
            OnPlayerLeft?.Invoke(id);
        }

        // ──────────────────────── Pose ────────────────────────

        public void UpdateLocalPlayerPose(string sceneName, Vector3 position, Quaternion rotation)
        {
            if (!IsConnected || _local == null) return;
            if (sceneName != _scene) BeginSceneChange(sceneName);

            float yaw = rotation.eulerAngles.y;
            float now = Time.unscaledTime;
            float moved = (position - _lastSentPos).magnitude;
            int move = moved / Mathf.Max(0.05f, now - _lastSentAt) > 3.2f ? 2 : moved > 0.04f ? 1 : 0;
            _local.position = position;
            _local.rotation = rotation;
            _local.sceneName = sceneName;
            _local.serverTick = DateTime.UtcNow.Ticks;
            _local.moveState = move;

            if (_shard == null || !_shard.Joined) return;
            bool changed = moved > 0.05f || Mathf.Abs(Mathf.DeltaAngle(yaw, _lastSentYaw)) > 4f || move != _lastSentMove;
            float wait = changed ? MovingInterval : IdleInterval;
            if (now - _lastSentAt < wait) return;
            _lastSentAt = now;
            _lastSentPos = position;
            _lastSentYaw = yaw;
            _lastSentMove = move;
            _shard.Send("p", EncodePose(_localPlayerId, position, yaw, move));
        }

        public static Dictionary<string, object> EncodePose(string id, Vector3 position, float yaw, int move)
        {
            return new Dictionary<string, object>
            {
                ["i"] = id,
                ["x"] = Mathf.RoundToInt(position.x * 100f),
                ["y"] = Mathf.RoundToInt(position.y * 100f),
                ["z"] = Mathf.RoundToInt(position.z * 100f),
                ["r"] = Mathf.RoundToInt(Mathf.Repeat(yaw, 360f)),
                ["m"] = move
            };
        }

        private void HandleShardBroadcast(string evt, Dictionary<string, object> payload)
        {
            string id = payload.Str("i");
            if (string.IsNullOrEmpty(id) || id == _localPlayerId) return;
            if (evt == "p")
            {
                var p = GetOrAdd(id);
                p.position = new Vector3((float)payload.Num("x") / 100f, (float)payload.Num("y") / 100f, (float)payload.Num("z") / 100f);
                p.rotation = Quaternion.Euler(0f, (float)payload.Num("r"), 0f);
                p.moveState = (int)payload.Num("m");
                p.serverTick = DateTime.UtcNow.Ticks;
                p.sceneName = _scene;
                p.roomId = _scene;
                p.shardTopic = _shard?.Topic;
                var meta = _shard?.MetaOf(id);
                if (meta != null) { p.displayName = meta.Str("n", p.displayName); p.characterId = meta.Str("c", p.characterId); }
                p.hasPose = true;
                OnPlayerJoinedOrUpdated?.Invoke(p);
            }
            else if (evt == "c")
            {
                string text = Clean(payload.Str("t", ""));
                if (text.Length == 0) return;
                AddChatMessage(new OnlineChatMessage
                {
                    messageId = payload.Str("id", Guid.NewGuid().ToString("N")),
                    senderPlayerId = id,
                    senderDisplayName = Clean(payload.Str("n", "Learner")),
                    channelId = "area",
                    text = text,
                    sentAtUtcTicks = DateTime.UtcNow.Ticks
                });
            }
        }

        // ──────────────────────── Area chat ────────────────────────

        /// <summary>"town"/"area" (default): the people in your shard. Other channels belong to SocialHub.</summary>
        public void SendChatMessage(string channelId, string text)
        {
            text = Clean(text);
            if (!IsConnected || text.Length == 0) return;
            if (GameServices.TryGet(out SocialHub hub) && channelId != null && channelId != "town" && channelId != "area")
            {
                hub.Send(channelId, text);
                return;
            }
            if (Time.unscaledTime - _lastChatAt < 0.8f)
            {
                SendSystemMessage("area", "Bạn gửi hơi nhanh — chờ một chút nhé.");
                return;
            }
            _lastChatAt = Time.unscaledTime;
            var message = new OnlineChatMessage
            {
                messageId = Guid.NewGuid().ToString("N"),
                senderPlayerId = _localPlayerId,
                senderDisplayName = _localDisplayName,
                channelId = "area",
                text = text,
                sentAtUtcTicks = DateTime.UtcNow.Ticks
            };
            AddChatMessage(message);
            if (_shard == null) { SendSystemMessage("area", "Ở đây chỉ có mình bạn (phòng riêng) — tin khu vực không gửi đi."); return; }
            _shard.Send("c", new Dictionary<string, object> { ["i"] = _localPlayerId, ["n"] = _localDisplayName, ["t"] = text, ["id"] = message.messageId });
        }

        /// <summary>Trims, caps the length and strips rich-text tags so nobody can paint the chat.</summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.Trim().Replace("<", "‹").Replace(">", "›").Replace("\n", " ").Replace("\r", " ");
            return text.Length > MaxChatLength ? text.Substring(0, MaxChatLength) : text;
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
            while (_chatHistory.Count > _chatHistoryLimit) _chatHistory.RemoveAt(0);
            OnChatMessageReceived?.Invoke(message);
        }
    }
}
