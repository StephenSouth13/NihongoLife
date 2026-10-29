using NihongoLife.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Audio
{
    /// <summary>
    /// Music and ambience for wherever the player is (files: Resources/Audio/Music|Ambience|Jingles, CC0 — see
    /// Docs/AUDIO_CREDITS.md). Each place has its own track: the town by day and by night, inside the konbini, the
    /// station, sushi bar, school, bedroom, game centre and Midori Island. Under it an ambience loop (birds by day, crickets
    /// at night, birds and wind on the island). The music ducks while a character speaks and stops during exams, where
    /// the listening audio must be heard alone. Short jingles mark a finished quest, a purchase, an unlock, a level-up.
    /// Lives on its own (created after the first scene loads); the main menu keeps its own music (MenuMusicController).
    /// </summary>
    public sealed class Soundscape : MonoBehaviour
    {
        public static Soundscape Instance { get; private set; }
        /// <summary>Key of the music playing now ("city_day", "konbini", …; "" = none). Tests read it.</summary>
        public string MusicKey { get; private set; } = "";
        public string AmbienceKey { get; private set; } = "";
        /// <summary>A track being listened to in the handbook (plays instead of the place's own; null = none).</summary>
        public static string PreviewKey;

        private AudioSource _music, _ambience, _jingle;
        private string _wantMusic = "", _wantAmbience = "";
        private float _musicGain, _ambienceGain;
        private float _next;
        private int _lastLevel = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Instance != null) return;
            var go = new GameObject("Soundscape");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<Soundscape>();
        }

        private void Awake()
        {
            _music = Source(true);
            _ambience = Source(true);
            _jingle = Source(false);
        }

        private AudioSource Source(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = loop; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f;
            return s;
        }

        /// <summary>Plays a short jingle: "quest_complete", "purchase", "unlock", "level_up".</summary>
        public static void Jingle(string key)
        {
            if (Instance == null) return;
            var clip = Resources.Load<AudioClip>("Audio/Jingles/" + key);
            if (clip != null) Instance._jingle.PlayOneShot(clip, 0.8f * Sfx);
        }

        private static float Bgm => Mathf.Clamp01(PlayerPrefs.GetFloat("Volume_BGM", 0.5f) * 2f);
        private static float Sfx => Mathf.Clamp01(PlayerPrefs.GetFloat("Volume_SFX", 0.8f));

        private void Update()
        {
            if (Time.unscaledTime >= _next)
            {
                _next = Time.unscaledTime + 0.5f;
                Decide();
                LevelJingle();
            }
            // Crossfade: the old clip fades out, then the new one starts and fades in.
            float quiet = Ducking();
            Fade(_music, ref _musicGain, _wantMusic, "Music", 0.7f * Bgm * quiet, () => MusicKey = _wantMusic);
            // Ambience (birds, insects, wind) has its own switch and volume in Settings (AudioPrefs).
            Fade(_ambience, ref _ambienceGain, _wantAmbience, "Ambience", 0.6f * AudioPrefs.AmbienceGain * (Exam ? 0f : 1f), () => AmbienceKey = _wantAmbience);
            _music.mute = !AudioPrefs.MusicOn;
            _ambience.mute = !AudioPrefs.AmbienceOn;
        }

        private void Fade(AudioSource source, ref float gain, string want, string folder, float target, System.Action started)
        {
            string current = source.clip != null ? source.clip.name : "";
            float speed = Time.unscaledDeltaTime / 1.4f;
            if (current != want)
            {
                gain = Mathf.Max(0f, gain - speed);
                if (gain <= 0f)
                {
                    // The web build fetches music on demand (WebAudioStore): stay quiet until it has arrived.
                    string path = string.IsNullOrEmpty(want) ? null : $"Audio/{folder}/{want}";
                    var next = path != null ? WebAudioStore.Get(path) : null;
                    if (next == null && WebAudioStore.IsPending(path)) { source.volume = 0f; return; }
                    source.Stop();
                    source.clip = next;
                    if (source.clip != null) source.Play();
                    started();
                }
            }
            else gain = Mathf.MoveTowards(gain, 1f, speed);
            source.volume = gain * target;
        }

        private static bool Exam =>
            (NihongoLife.Exam.ExamManager.Instance != null && NihongoLife.Exam.ExamManager.Instance.IsAttemptActive)
            || (NihongoLife.UI.IeltsTestUI.Instance != null && NihongoLife.UI.IeltsTestUI.Instance.IsOpen);

        /// <summary>1 normally, lower while someone speaks, 0 during exams.</summary>
        private static float Ducking()
        {
            if (Exam) return 0f;
            var dialogue = NihongoLife.Dialogue.DialogueManager.Instance;
            return dialogue != null && dialogue.IsOpen ? 0.45f : 1f;
        }

        private void Decide()
        {
            string scene = SceneManager.GetActiveScene().name;
            float hour = 12f;
            var cycle = SceneRefs.Find<DayNightCycle>();
            if (cycle != null) hour = cycle.Hour;
            bool night = hour < 6f || hour >= 18.5f;
            bool raining = WeatherCycle.Instance != null && WeatherCycle.Instance.IsRaining;

            string music = "", ambience = "";
            switch (scene)
            {
                case WorldLocationCatalog.CityScene:
                    var cam = Camera.main != null ? Camera.main.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>() : null;
                    bool indoor = cam != null && cam.IsIndoor;
                    // Outdoors: the rain has its own track by day, spring mornings theirs (Sakura Michi).
                    bool spring = cycle != null && GameCalendar.SeasonOn(cycle.Day) == Season.Spring && hour >= 8f && hour < 14f;
                    music = indoor ? "konbini" : night ? "city_night" : raining ? "rain" : spring ? "spring" : "city_day";
                    ambience = indoor || raining ? "" : night ? "city_night" : "city_day";
                    break;
                case WorldLocationCatalog.StationScene: music = "station"; ambience = night ? "city_night" : "city_day"; break;
                case WorldLocationCatalog.SushiRestaurantScene: music = "sushi"; break;
                case WorldLocationCatalog.SchoolScene: music = "school"; break;
                case WorldLocationCatalog.HomeBedroomScene: music = NihongoLife.Health.PlayerIllness.Main != null ? "sick" : "bedroom"; break; // ill at home: a lullaby
                case WorldLocationCatalog.GameCenterScene: music = "game_center"; break;
                case WorldLocationCatalog.ClinicScene: music = "clinic"; break; // calm and quiet, as a clinic should be
                case WorldLocationCatalog.MusicShopScene: music = "spring"; break; // the shop's own record on the counter
                case WorldLocationCatalog.MidoriIslandScene: music = "island"; ambience = raining ? "" : night ? "city_night" : "island"; break;
                default: music = ""; ambience = ""; break; // menu / bootstrap: MenuMusicController plays there
            }
            if (music != "" && AtWork(scene)) music = "work";
            // The handbook's player: a track listened to now, or one pinned to play everywhere.
            if (music != "" && !string.IsNullOrEmpty(PreviewKey)) music = PreviewKey;
            else if (music != "" && !string.IsNullOrEmpty(AudioPrefs.PinnedMusic)) music = AudioPrefs.PinnedMusic;
            _wantMusic = music;
            _wantAmbience = ambience;
        }

        /// <summary>Working hours of the player's career, at its workplace: the busy-shift track.</summary>
        private static bool AtWork(string scene)
        {
            var career = NihongoLife.Progression.CareerService.Current;
            if (career == null || !NihongoLife.Progression.CareerService.WorkingHoursNow) return false;
            return career.id switch
            {
                "food" => scene == WorldLocationCatalog.SushiRestaurantScene,
                "farm" => scene == WorldLocationCatalog.MidoriIslandScene,
                "medical" => scene == WorldLocationCatalog.ClinicScene,
                "hospitality" => scene == "65_PhuQuocIsland", // Gánh Ngon, Phú Quốc
                "musician" or "producer" or "painter" => false, // the music shop: a stage needs quiet, not the busy-shift track
                _ => false, // retail works inside the konbini, which keeps its own music
            };
        }

        private void LevelJingle()
        {
            var status = NihongoLife.Player.PlayerStatus.Instance;
            if (status == null) { _lastLevel = -1; return; }
            if (_lastLevel >= 0 && status.Level > _lastLevel) Jingle("level_up");
            _lastLevel = status.Level;
        }
    }
}
