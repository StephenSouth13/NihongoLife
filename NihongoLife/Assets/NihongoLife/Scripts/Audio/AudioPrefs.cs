using System;
using UnityEngine;

namespace NihongoLife.Audio
{
    /// <summary>
    /// The player's sound choices, in one place: background music on/off, ambience (birds, insects, wind, rain, street
    /// voices) on/off and its own volume. Music and effects volumes stay where they were (Volume_BGM / Volume_SFX,
    /// AudioService). Every music / ambience source reads these and listens to <see cref="Changed"/>.
    /// Values are cached after the first read, so sources may read them every frame.
    /// </summary>
    public static class AudioPrefs
    {
        private const string MusicOnKey = "Audio_MusicOn";
        private const string AmbienceOnKey = "Audio_AmbienceOn";
        private const string AmbienceVolumeKey = "Volume_Ambience";
        private const string PinnedMusicKey = "Audio_PinnedMusic";

        private static bool _loaded, _musicOn, _ambienceOn;
        private static float _ambienceVolume;
        private static string _pinned;

        public static event Action Changed;

        public static bool MusicOn
        {
            get { Load(); return _musicOn; }
            set { Load(); _musicOn = value; PlayerPrefs.SetInt(MusicOnKey, value ? 1 : 0); Save(); }
        }

        public static bool AmbienceOn
        {
            get { Load(); return _ambienceOn; }
            set { Load(); _ambienceOn = value; PlayerPrefs.SetInt(AmbienceOnKey, value ? 1 : 0); Save(); }
        }

        /// <summary>Ambience slider 0–1 (0.5 = the level the game shipped with).</summary>
        public static float AmbienceVolume
        {
            get { Load(); return _ambienceVolume; }
            set { Load(); _ambienceVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(AmbienceVolumeKey, _ambienceVolume); Save(); }
        }

        /// <summary>A track chosen in the handbook's 音楽 chapter that plays everywhere ("" = each place its own).</summary>
        public static string PinnedMusic
        {
            get { Load(); return _pinned; }
            set { Load(); _pinned = value ?? ""; PlayerPrefs.SetString(PinnedMusicKey, _pinned); Save(); }
        }

        /// <summary>Gain for ambience sources: 0 when off, 1 at the default slider position, up to 2.</summary>
        public static float AmbienceGain => AmbienceOn ? AmbienceVolume * 2f : 0f;

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _musicOn = PlayerPrefs.GetInt(MusicOnKey, 1) == 1;
            _ambienceOn = PlayerPrefs.GetInt(AmbienceOnKey, 1) == 1;
            _ambienceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(AmbienceVolumeKey, 0.5f));
            _pinned = PlayerPrefs.GetString(PinnedMusicKey, "");
        }

        private static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // Domain reload may be off in the Editor: start each Play session from the saved values.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { _loaded = false; Changed = null; }
    }
}
