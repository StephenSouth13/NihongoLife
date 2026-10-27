using System;
using NihongoLife.Data;
using NihongoLife.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    /// <summary>
    /// The intro trailer (99_Trailer, NihongoLife.Trailer.TrailerReel) before a guest's first game on this device,
    /// skippable. The reel films the real town, so while it plays it uses a throw-away in-memory save: the player's
    /// own save (health, quests, island, money) is never read or changed by it, and needs do not drain.
    /// </summary>
    public static class IntroTrailer
    {
        public const string SceneName = "99_Trailer";
        private const string SeenKey = "IntroTrailer_Seen";

        private static Action _then;
        private static IProgressRepository _realSaves;
        private static TimeSpan _islandClock;

        /// <summary>Tests only (PlayModeSaveGuard): starting a game never plays the intro.</summary>
        public static bool Bypass;

        /// <summary>True while the trailer films: PlayerStatus / StatusConsequences pause, saves go to memory.</summary>
        public static bool Playing { get; private set; }

        public static bool Seen
        {
            get => PlayerPrefs.GetInt(SeenKey, 0) == 1;
            set { PlayerPrefs.SetInt(SeenKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Plays the trailer once, then runs <paramref name="then"/> (load the game). False: nothing to play.</summary>
        public static bool PlayBefore(Action then)
        {
            if (Bypass || Seen || Playing || !Application.CanStreamedLevelBeLoaded(SceneName)) return false;
            Begin();
            _then = then;
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            return true;
        }

        /// <summary>Called by the reel when it starts (also when a recorder plays it directly).</summary>
        public static void Begin()
        {
            if (Playing) return;
            Playing = true;
            GameServices.TryGet(out _realSaves);
            _islandClock = NihongoLife.Island.IslandState.ClockOffset;
            GameServices.Replace<IProgressRepository>(new MemoryProgressRepository());
        }

        /// <summary>The reel ended or was skipped: real saves back, then the game continues.</summary>
        public static void Finish(bool continueGame = true)
        {
            if (!Playing) return;
            Playing = false;
            if (_realSaves != null) GameServices.Replace(_realSaves);
            else GameServices.Unregister<IProgressRepository>();
            _realSaves = null;
            NihongoLife.Island.IslandState.ClockOffset = _islandClock;
            var then = _then;
            _then = null;
            if (then == null) return;
            Seen = true; // watched or skipped: the intro plays once per device
            if (continueGame) then();
        }

        /// <summary>True when the reel was started as the intro and something waits for it (the game scene).</summary>
        public static bool HasContinuation => _then != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Playing = false; _then = null; _realSaves = null; }

        /// <summary>A save that lives only in memory, fresh for each trailer.</summary>
        private sealed class MemoryProgressRepository : IProgressRepository
        {
            private PlayerProgressDto _progress = new PlayerProgressDto { playerId = "trailer", displayName = "Haruto" };
            public void Initialize() { }
            public PlayerProgressDto GetProgress() => _progress;
            public void SaveProgress(PlayerProgressDto progress) { if (progress != null) _progress = progress; }
            public void ResetProgress() => _progress = new PlayerProgressDto { playerId = "trailer", displayName = "Haruto" };
        }
    }
}
