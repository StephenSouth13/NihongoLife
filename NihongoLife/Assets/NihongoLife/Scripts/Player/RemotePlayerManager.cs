using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NihongoLife.Core;

namespace NihongoLife.Player
{
    /// <summary>
    /// Spawns, updates and removes RemotePlayerAvatar instances from IOnlineWorldService events (lives on AppRoot,
    /// added by SupabaseOnlineWorldService). Only players in the same place are shown, and of those only the
    /// <c>maxVisiblePlayers</c> nearest are drawn — the rest keep moving but have no model or name plate.
    /// </summary>
    public class RemotePlayerManager : MonoBehaviour
    {
        private readonly Dictionary<string, RemotePlayerAvatar> _avatars = new Dictionary<string, RemotePlayerAvatar>();
        private readonly List<RemotePlayerAvatar> _sorted = new List<RemotePlayerAvatar>();
        private IOnlineWorldService _onlineWorld;
        private int _maxVisible = 24;
        private float _nextCull;
        private Transform _root;

        public int AvatarCount => _avatars.Count;
        public int VisibleCount { get; private set; }
        public IReadOnlyDictionary<string, RemotePlayerAvatar> Avatars => _avatars;

        private void Start()
        {
            if (GameServices.TryGet(out GameControlService control) && control.Database != null)
                _maxVisible = Mathf.Max(4, control.Database.maxVisiblePlayers);
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void OnDestroy()
        {
            if (_onlineWorld != null)
            {
                _onlineWorld.OnPlayerJoinedOrUpdated -= HandlePlayerUpdate;
                _onlineWorld.OnPlayerLeft -= HandlePlayerLeft;
            }
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        }

        private void Update()
        {
            if (_onlineWorld == null && GameServices.TryGet(out _onlineWorld))
            {
                _onlineWorld.OnPlayerJoinedOrUpdated += HandlePlayerUpdate;
                _onlineWorld.OnPlayerLeft += HandlePlayerLeft;
                foreach (var p in _onlineWorld.VisiblePlayers) HandlePlayerUpdate(p);
            }
            if (Time.unscaledTime >= _nextCull)
            {
                _nextCull = Time.unscaledTime + 0.5f;
                CullToNearest();
            }
        }

        private void HandleActiveSceneChanged(Scene from, Scene to) => ClearAll();

        private void HandlePlayerUpdate(OnlinePlayerSnapshot snapshot)
        {
            if (snapshot == null || _onlineWorld == null || snapshot.playerId == _onlineWorld.LocalPlayerId || !snapshot.hasPose) return;
            if (snapshot.sceneName != SceneManager.GetActiveScene().name)
            {
                DespawnAvatar(snapshot.playerId);
                return;
            }
            if (_avatars.TryGetValue(snapshot.playerId, out var avatar) && avatar != null) avatar.UpdateFromSnapshot(snapshot);
            else SpawnAvatar(snapshot);
        }

        private void HandlePlayerLeft(string playerId) => DespawnAvatar(playerId);

        private void SpawnAvatar(OnlinePlayerSnapshot snapshot)
        {
            if (_root == null)
            {
                // Avatars live in the active scene so they go away with it and never follow the player into a menu.
                _root = new GameObject("RemotePlayers").transform;
                SceneManager.MoveGameObjectToScene(_root.gameObject, SceneManager.GetActiveScene());
            }
            var go = new GameObject($"RemotePlayer_{snapshot.displayName}");
            go.transform.SetParent(_root, false);
            var avatar = go.AddComponent<RemotePlayerAvatar>();
            avatar.Setup(snapshot);
            _avatars[snapshot.playerId] = avatar;
            _nextCull = 0f;
        }

        private void DespawnAvatar(string playerId)
        {
            if (!_avatars.TryGetValue(playerId, out var avatar)) return;
            if (avatar != null) Destroy(avatar.gameObject);
            _avatars.Remove(playerId);
        }

        private void CullToNearest()
        {
            var player = GameObject.FindWithTag("Player");
            Vector3 origin = player != null ? player.transform.position : Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            _sorted.Clear();
            foreach (var kv in _avatars) if (kv.Value != null) _sorted.Add(kv.Value);
            _sorted.Sort((a, b) => (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));
            VisibleCount = 0;
            for (int i = 0; i < _sorted.Count; i++)
            {
                bool show = i < _maxVisible;
                if (show) VisibleCount++;
                if (_sorted[i].VisualVisible != show) _sorted[i].SetVisualVisible(show);
            }
        }

        /// <summary>Clean up all avatars (e.g., on scene change).</summary>
        public void ClearAll()
        {
            foreach (var kvp in _avatars)
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            _avatars.Clear();
            if (_root != null) Destroy(_root.gameObject);
            _root = null;
        }
    }
}
