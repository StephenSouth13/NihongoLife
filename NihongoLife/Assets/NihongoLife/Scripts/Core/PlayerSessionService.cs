using System;
using NihongoLife.Data;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.Core
{
    public enum PlayerSessionMode { Guest, Account }

    public class PlayerSessionService : MonoBehaviour, IGameService
    {
        private static PlayerSessionService _instance;
        private static bool _isShuttingDown;
        private PlayerProgressDto _guestProgress;

        public static PlayerSessionService Instance => _instance;
        public PlayerSessionMode Mode { get; private set; } = PlayerSessionMode.Guest;
        public bool CanPersist => Mode == PlayerSessionMode.Account;
        public PlayerProgressDto GuestProgress => _guestProgress ??= new PlayerProgressDto { playerId = "guest", displayName = "Guest" };

        public event Action<PlayerSessionMode> OnModeChanged;

        public static PlayerSessionService GetOrCreate()
        {
            if (_instance != null) return _instance;
            if (_isShuttingDown) return null;
            var existing = FindFirstObjectByType<PlayerSessionService>();
            if (existing != null) { existing.Initialize(); return existing; }
            var go = new GameObject("PlayerSessionService");
            DontDestroyOnLoad(go);
            var service = go.AddComponent<PlayerSessionService>();
            service.Initialize();
            GameServices.Register<PlayerSessionService>(service);
            return service;
        }

        public void Initialize()
        {
            if (_instance == this) return;
            _instance = this;
            Mode = PlayerSessionMode.Guest;
            _guestProgress = new PlayerProgressDto { playerId = "guest", displayName = "Guest" };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _isShuttingDown = false;
            Application.quitting -= HandleApplicationQuitting;
            Application.quitting += HandleApplicationQuitting;
        }

        private static void HandleApplicationQuitting() => _isShuttingDown = true;

        public void BeginGuest()
        {
            Mode = PlayerSessionMode.Guest;
            _guestProgress = new PlayerProgressDto { playerId = "guest", displayName = "Guest" };
            OnModeChanged?.Invoke(Mode);
        }

        public void BeginAccount(string userId, string displayName)
        {
            Mode = PlayerSessionMode.Account;
            if (!string.IsNullOrWhiteSpace(displayName)) PlayableCharacterCatalog.SavePlayerName(displayName.Trim());
            PlayerPrefs.Save();
            OnModeChanged?.Invoke(Mode);
        }

        public void UpdateGuestProgress(PlayerProgressDto progress)
        {
            if (Mode == PlayerSessionMode.Guest && progress != null) _guestProgress = progress;
        }

        private void OnApplicationQuit() => _isShuttingDown = true;

        private void OnDestroy()
        {
            if (_instance != this) return;
            _isShuttingDown = true;
            _instance = null;
        }
    }
}
