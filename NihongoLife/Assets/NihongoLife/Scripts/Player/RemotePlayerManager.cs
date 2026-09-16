using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NihongoLife.Core;

namespace NihongoLife.Player
{
    /// <summary>
    /// Manages spawning, updating, and despawning of remote player avatars.
    /// Listens to IOnlineWorldService events and maintains a pool of RemotePlayerAvatar instances.
    /// Only displays players in the same scene, culled to maxVisiblePlayers.
    /// </summary>
    public class RemotePlayerManager : MonoBehaviour
    {
        private readonly Dictionary<string, RemotePlayerAvatar> _avatars = new Dictionary<string, RemotePlayerAvatar>();
        private IOnlineWorldService _onlineWorld;
        private string _localPlayerId;
        private int _maxVisible = 24;

        private void Start()
        {
            if (GameServices.TryGet(out _onlineWorld))
            {
                _onlineWorld.OnPlayerJoinedOrUpdated += HandlePlayerUpdate;
                _onlineWorld.OnPlayerLeft += HandlePlayerLeft;
            }

            if (GameServices.TryGet(out GameControlService control) && control.Database != null)
            {
                _maxVisible = Mathf.Max(4, control.Database.maxVisiblePlayers);
            }

            // Determine local player ID
            if (GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated)
            {
                _localPlayerId = auth.UserId;
            }
            else
            {
                _localPlayerId = SystemInfo.deviceUniqueIdentifier;
            }
        }

        private void OnDestroy()
        {
            if (_onlineWorld != null)
            {
                _onlineWorld.OnPlayerJoinedOrUpdated -= HandlePlayerUpdate;
                _onlineWorld.OnPlayerLeft -= HandlePlayerLeft;
            }
        }

        private void HandlePlayerUpdate(OnlinePlayerSnapshot snapshot)
        {
            // Don't create avatar for local player
            if (snapshot.playerId == _localPlayerId) return;

            // Only show players in the same scene
            string currentScene = SceneManager.GetActiveScene().name;
            if (snapshot.sceneName != currentScene)
            {
                // Remove avatar if they left this scene
                if (_avatars.ContainsKey(snapshot.playerId))
                {
                    DespawnAvatar(snapshot.playerId);
                }
                return;
            }

            // Limit visible players
            if (!_avatars.ContainsKey(snapshot.playerId) && _avatars.Count >= _maxVisible)
            {
                return;
            }

            if (_avatars.TryGetValue(snapshot.playerId, out var existingAvatar))
            {
                existingAvatar.UpdateFromSnapshot(snapshot);
            }
            else
            {
                SpawnAvatar(snapshot);
            }
        }

        private void HandlePlayerLeft(string playerId)
        {
            DespawnAvatar(playerId);
        }

        private void SpawnAvatar(OnlinePlayerSnapshot snapshot)
        {
            var go = new GameObject($"RemotePlayer_{snapshot.displayName}");
            go.transform.SetParent(transform, false);
            go.tag = "Untagged"; // Don't tag as Player

            var avatar = go.AddComponent<RemotePlayerAvatar>();
            avatar.Setup(snapshot);

            _avatars[snapshot.playerId] = avatar;
            Debug.Log($"[RemotePlayerManager] Spawned avatar for {snapshot.displayName}");
        }

        private void DespawnAvatar(string playerId)
        {
            if (_avatars.TryGetValue(playerId, out var avatar))
            {
                if (avatar != null && avatar.gameObject != null)
                {
                    Debug.Log($"[RemotePlayerManager] Despawned avatar for {avatar.DisplayName}");
                    Destroy(avatar.gameObject);
                }
                _avatars.Remove(playerId);
            }
        }

        /// <summary>Clean up all avatars (e.g., on scene change).</summary>
        public void ClearAll()
        {
            foreach (var kvp in _avatars)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _avatars.Clear();
        }
    }
}
