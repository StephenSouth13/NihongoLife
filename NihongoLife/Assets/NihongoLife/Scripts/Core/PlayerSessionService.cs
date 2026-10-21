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
        public PlayerProgressDto GuestProgress => _guestProgress ??= LoadGuest();

        /// <summary>
        /// A guest plays the whole game offline, so its progress is kept on this device (guest_progress.json) between
        /// sessions. PlayMode tests turn this off (PlayModeSaveGuard) so they never touch a real guest save.
        /// </summary>
        public static bool PersistGuest = true;
        public static string GuestSavePath => System.IO.Path.Combine(Application.persistentDataPath, "guest_progress.json");

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
            _guestProgress = LoadGuest();
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

        /// <summary>Play as a guest: offline, continuing this device's guest save.</summary>
        public void BeginGuest()
        {
            Mode = PlayerSessionMode.Guest;
            _guestProgress = LoadGuest();
            OnModeChanged?.Invoke(Mode);
        }

        public void BeginAccount(string userId, string displayName)
        {
            Mode = PlayerSessionMode.Account;
            // The profile name becomes the character's name, unless it is only "Guest"/anonymous.
            if (!PlayableCharacterCatalog.IsPlaceholderName(displayName)) PlayableCharacterCatalog.SavePlayerName(displayName.Trim());
            PlayerPrefs.Save();
            OnModeChanged?.Invoke(Mode);
        }

        public void UpdateGuestProgress(PlayerProgressDto progress)
        {
            if (Mode != PlayerSessionMode.Guest || progress == null) return;
            _guestProgress = progress;
            SaveGuest(progress);
        }

        /// <summary>
        /// The guest's progress when it is worth carrying into a newly registered account (something was played),
        /// else null. Read while still in guest mode, before <see cref="BeginAccount"/>.
        /// </summary>
        public PlayerProgressDto GuestProgressToCarry()
        {
            var guest = GuestProgress;
            bool played = guest != null && (guest.completedScenarios.Count > 0 || guest.knowledge > 0 || guest.xp > 0
                || !string.IsNullOrEmpty(guest.activeScenarioId) || (guest.storyFlags != null && guest.storyFlags.Count > 0));
            return played ? guest : null;
        }

        /// <summary>After the guest's progress moved into an account: the next guest starts fresh.</summary>
        public void ClearGuestProgress()
        {
            _guestProgress = NewGuest();
            if (!PersistGuest) return;
            try { if (System.IO.File.Exists(GuestSavePath)) System.IO.File.Delete(GuestSavePath); }
            catch (Exception ex) { Debug.LogWarning("[PlayerSession] Guest save not cleared: " + ex.Message); }
            NihongoLife.Save.LocalProgressRepository.FlushWebStorage();
        }

        private static PlayerProgressDto NewGuest() => new PlayerProgressDto { playerId = "guest", displayName = "Guest" };

        private static PlayerProgressDto LoadGuest()
        {
            if (!PersistGuest) return NewGuest();
            try
            {
                if (!System.IO.File.Exists(GuestSavePath)) return NewGuest();
                var loaded = JsonUtility.FromJson<PlayerProgressDto>(System.IO.File.ReadAllText(GuestSavePath));
                return loaded ?? NewGuest();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PlayerSession] Guest save unreadable, starting fresh: " + ex.Message);
                return NewGuest();
            }
        }

        private static void SaveGuest(PlayerProgressDto progress)
        {
            if (!PersistGuest) return;
            try
            {
                System.IO.File.WriteAllText(GuestSavePath, JsonUtility.ToJson(progress));
                NihongoLife.Save.LocalProgressRepository.FlushWebStorage();
            }
            catch (Exception ex) { Debug.LogWarning("[PlayerSession] Guest save failed: " + ex.Message); }
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
