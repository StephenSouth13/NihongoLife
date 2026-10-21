using NihongoLife.Save;
using NihongoLife.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    public class OnlineWorldBootstrap : MonoBehaviour
    {
        // The world service decides how often a pose actually goes out (≈6 Hz moving, every 3 s idle);
        // this only has to feed it often enough.
        [SerializeField] private float posePublishIntervalSeconds = 0.05f;

        private float _nextPublishTime;
        private Transform _playerTransform;
        private IAuthService _auth;
        /// <summary>Connect was held back because the player is a guest: connect as soon as online is allowed.</summary>
        private bool _heldForGuest;

        private void Start()
        {
            Connect();
            if (GameServices.TryGet(out _auth)) _auth.OnAuthStateChanged += HandleAuthChanged;
        }

        private void OnDestroy()
        {
            if (_auth != null) _auth.OnAuthStateChanged -= HandleAuthChanged;
        }

        private void Connect()
        {
            if (!GameServices.TryGet(out IOnlineWorldService onlineWorld) || onlineWorld.IsConnected)
            {
                return;
            }
            // The real online world is for registered, signed-in accounts; guests play offline (OnlineAccess).
            _heldForGuest = onlineWorld is SupabaseOnlineWorldService && !OnlineAccess.IsRegistered;
            if (_heldForGuest) return;

            string playerId = SystemInfo.deviceUniqueIdentifier;
            string displayName = PlayableCharacterCatalog.GetPlayerName();
            if (GameServices.TryGet(out IProgressRepository progressRepository))
            {
                var progress = progressRepository.GetProgress();
                if (displayName == PlayableCharacterCatalog.DefaultPlayerName && progress != null && !string.IsNullOrWhiteSpace(progress.displayName))
                {
                    displayName = progress.displayName;
                }
            }

            onlineWorld.ConnectLocalPlayer(playerId, displayName);
            CachePlayerTransform();
        }

        /// <summary>Signing in or out changes who we are online (friends need the account id): reconnect.</summary>
        private void HandleAuthChanged(AuthState state)
        {
            if (state != AuthState.SignedIn && state != AuthState.SignedOut) return;
            if (!GameServices.TryGet(out IOnlineWorldService onlineWorld)) return;
            if (onlineWorld is SupabaseOnlineWorldService && !OnlineAccess.IsRegistered)
            {
                if (onlineWorld.IsConnected) onlineWorld.Disconnect(); // signed out: back to offline
                _heldForGuest = true;
                return;
            }
            string expected = _auth != null && _auth.IsAuthenticated ? _auth.UserId : SystemInfo.deviceUniqueIdentifier;
            if (onlineWorld.IsConnected && onlineWorld.LocalPlayerId == expected) return;
            onlineWorld.Disconnect();
            Connect();
        }

        private void Update()
        {
            if (_heldForGuest && OnlineAccess.IsRegistered) Connect();
            if (Time.unscaledTime < _nextPublishTime) return;
            _nextPublishTime = Time.unscaledTime + posePublishIntervalSeconds;

            if (!GameServices.TryGet(out IOnlineWorldService onlineWorld) || !onlineWorld.IsConnected)
            {
                return;
            }

            if (_playerTransform == null)
            {
                CachePlayerTransform();
                if (_playerTransform == null) return;
            }

            onlineWorld.UpdateLocalPlayerPose(
                SceneManager.GetActiveScene().name,
                _playerTransform.position,
                _playerTransform.rotation);
        }

        private void CachePlayerTransform()
        {
            var player = GameObject.FindWithTag("Player");
            _playerTransform = player != null ? player.transform : null;
        }
    }
}
