using UnityEngine;
using System;
using System.Collections.Generic;
using NihongoLife.Data;
using NihongoLife.Save;

namespace NihongoLife.Core
{
    [Serializable]
    public class DayNightTimelineKey
    {
        [Range(0f, 24f)] public float hour = 12f;
        public Color sunColor = new Color(1f, 0.93f, 0.78f);
        public float sunIntensity = 1.2f;
        public Color ambientColor = new Color(0.58f, 0.62f, 0.66f);
        public Color fogColor = new Color(0.67f, 0.76f, 0.83f);
        [Range(0f, 0.08f)] public float fogDensity = 0.006f;
    }

    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Active { get; private set; }

        [SerializeField] private float cycleMinutes = 24f;
        [SerializeField, Range(0f, 24f)] private float startHour = 15.5f;
        [SerializeField] private bool runCycle = true;
        [SerializeField] private List<DayNightTimelineKey> timeline = new List<DayNightTimelineKey>
        {
            new DayNightTimelineKey { hour = 5.1f, sunColor = new Color(1f, 0.48f, 0.32f), sunIntensity = 0.22f, ambientColor = new Color(0.2f, 0.18f, 0.28f), fogColor = new Color(0.42f, 0.28f, 0.32f), fogDensity = 0.012f },
            new DayNightTimelineKey { hour = 8f, sunColor = new Color(1f, 0.9f, 0.72f), sunIntensity = 0.95f, ambientColor = new Color(0.55f, 0.6f, 0.66f), fogColor = new Color(0.72f, 0.78f, 0.84f), fogDensity = 0.007f },
            new DayNightTimelineKey { hour = 12f, sunColor = new Color(1f, 0.97f, 0.9f), sunIntensity = 1.25f, ambientColor = new Color(0.62f, 0.66f, 0.7f), fogColor = new Color(0.7f, 0.8f, 0.9f), fogDensity = 0.005f },
            new DayNightTimelineKey { hour = 15.5f, sunColor = new Color(1f, 0.9f, 0.7f), sunIntensity = 1.05f, ambientColor = new Color(0.58f, 0.58f, 0.56f), fogColor = new Color(0.75f, 0.74f, 0.68f), fogDensity = 0.006f },
            new DayNightTimelineKey { hour = 17.7f, sunColor = new Color(1f, 0.5f, 0.26f), sunIntensity = 0.48f, ambientColor = new Color(0.4f, 0.28f, 0.3f), fogColor = new Color(0.55f, 0.32f, 0.26f), fogDensity = 0.01f },
            new DayNightTimelineKey { hour = 19.6f, sunColor = new Color(0.55f, 0.42f, 0.72f), sunIntensity = 0.16f, ambientColor = new Color(0.12f, 0.1f, 0.18f), fogColor = new Color(0.1f, 0.08f, 0.14f), fogDensity = 0.014f },
            new DayNightTimelineKey { hour = 22.5f, sunColor = new Color(0.42f, 0.52f, 0.95f), sunIntensity = 0.08f, ambientColor = new Color(0.07f, 0.08f, 0.13f), fogColor = new Color(0.05f, 0.06f, 0.1f), fogDensity = 0.016f }
        };

        [SerializeField] private int birthdayMonth = 4;
        [SerializeField] private int birthdayDay = 15;
        [SerializeField] private int ageAtEpoch = 16;

        private float _hour;
        private bool _loaded;
        private float _nextSave;

        public float Hour => _hour;
        /// <summary>Real minutes for one in-game day. The default is a full day about every 24 real minutes.</summary>
        public float CycleMinutes => cycleMinutes;
        /// <summary>In-game day counter (advances at midnight). Day 0 is 1 April of year 1.</summary>
        public int Day { get; private set; }
        public int BirthdayMonth => GameCalendar.ClampMonth(birthdayMonth);
        public int BirthdayDay => GameCalendar.ClampDay(birthdayMonth, birthdayDay);
        public int Age => GameCalendar.AgeOn(Day, birthdayMonth, birthdayDay, ageAtEpoch);
        public Season Season => GameCalendar.SeasonOn(Day);
        public DayPhase Phase => GameCalendar.PhaseOf(_hour, Sunrise, Sunset);
        public MoonKind Moon => GameCalendar.MoonOf(GameCalendar.MoonPhase01(Day, _hour));
        public float MoonPhase01 => GameCalendar.MoonPhase01(Day, _hour);
        public float Sunrise { get; private set; } = 6f;
        public float Sunset { get; private set; } = 18f;
        public string HudCompact => GameCalendar.Compact(Day, birthdayMonth, birthdayDay, ageAtEpoch);
        public string ProfileLine => GameCalendar.ProfileLine(Day, _hour, birthdayMonth, birthdayDay, ageAtEpoch);

        private void Awake()
        {
            Active = this;
            _hour = startHour;
            RefreshSeason();
            ApplyLighting();
        }

        private void OnDestroy()
        {
            if (_loaded) SaveCalendar();
            if (Active == this) Active = null;
        }

        private void Start()
        {
            LoadCalendar();
            _loaded = true;
            _nextSave = Time.unscaledTime + 30f;
        }

        private void Update()
        {
            if (runCycle && cycleMinutes > 0.01f)
            {
                int before = Day;
                _hour += Time.deltaTime * 24f / (cycleMinutes * 60f);
                while (_hour >= 24f) { _hour -= 24f; Day++; }
                if (Day != before) OnDayAdvanced();
                ApplyLighting();
            }
            if (_loaded && Time.unscaledTime >= _nextSave)
            {
                _nextSave = Time.unscaledTime + 30f;
                SaveCalendar();
            }
        }

        public void SetHour(float hour)
        {
            _hour = Mathf.Repeat(hour, 24f);
            RefreshSeason();
            ApplyLighting();
        }

        public void SetCycleMinutes(float minutes) => cycleMinutes = Mathf.Max(0.05f, minutes);

        /// <summary>Sleep until the next morning: one calendar day passes and the clock jumps to <paramref name="wakeHour"/>.</summary>
        public void SleepUntil(float wakeHour)
        {
            Day++;
            _hour = Mathf.Repeat(wakeHour, 24f);
            RefreshSeason();
            ApplyLighting();
            OnDayAdvanced();
        }

        /// <summary>Copies the calendar into the save object. Ignored until the cycle has loaded, so an early save cannot wipe it.</summary>
        public void WriteTo(PlayerProgressDto progress)
        {
            if (!_loaded || progress == null) return;
            progress.calendarStarted = true;
            progress.calendarDay = Day;
            progress.calendarHour = _hour;
            progress.birthdayMonth = BirthdayMonth;
            progress.birthdayDay = BirthdayDay;
            progress.ageAtEpoch = Mathf.Max(1, ageAtEpoch);
        }

        private void OnDayAdvanced()
        {
            SaveCalendar();
            if (!GameCalendar.IsBirthday(Day, birthdayMonth, birthdayDay)) return;
            NihongoLife.UI.HudFeed.Post(Loc.F(
                "Hôm nay là sinh nhật của bạn — {0} tuổi. おめでとう！",
                "Today is your birthday — you are {0}. おめでとう！",
                "今日はあなたの誕生日。{0}歳。おめでとう！",
                Age), NihongoLife.UI.HudFeed.Kind.Reward, 7f, "birthday");
        }

        private void LoadCalendar()
        {
            if (!GameServices.TryGet(out IProgressRepository repository)) return;
            var progress = repository.GetProgress();
            if (progress == null) return;
            if (progress.birthdayMonth >= 1 && progress.birthdayMonth <= 12)
            {
                birthdayMonth = progress.birthdayMonth;
                birthdayDay = GameCalendar.ClampDay(birthdayMonth, progress.birthdayDay);
            }
            if (progress.ageAtEpoch > 0) ageAtEpoch = progress.ageAtEpoch;
            if (!progress.calendarStarted) return;
            Day = Mathf.Max(0, progress.calendarDay);
            SetHour(progress.calendarHour);
        }

        private void SaveCalendar()
        {
            if (!GameServices.TryGet(out IProgressRepository repository)) return;
            var progress = repository.GetProgress();
            if (progress == null) return;
            WriteTo(progress);
            repository.SaveProgress(progress);
        }

        private void RefreshSeason()
        {
            GameCalendar.Daylight(Season, out float rise, out float set);
            Sunrise = rise;
            Sunset = set;
        }

        /// <summary>Only scenes with an OutdoorSky are lit by the cycle. Room lighting in interiors stays as built.</summary>
        private void ApplyLighting()
        {
            RefreshSeason();
            var outdoors = OutdoorSky.Active;
            if (outdoors == null) return;
            float equinox = GameCalendar.EquinoxHour(_hour, Sunrise, Sunset);
            var key = EvaluateTimeline(equinox);
            key.sunColor = Color.Lerp(key.sunColor, SeasonTint(Season), 0.16f);
            key.fogColor = Color.Lerp(key.fogColor, SeasonTint(Season), 0.08f);
            outdoors.SetCalendarSky(MoonPhase01, Sunrise, Sunset, SeasonTint(Season));
            outdoors.Apply(_hour, key, EvaluateTimeline(12f));
        }

        private static Color SeasonTint(Season season) => season switch
        {
            Season.Summer => new Color(1f, 0.94f, 0.82f),
            Season.Autumn => new Color(1f, 0.82f, 0.62f),
            Season.Winter => new Color(0.78f, 0.86f, 1f),
            _ => new Color(1f, 0.96f, 0.9f)
        };

        /// <summary>Colour key for an hour; the night between the last key and the first wraps over midnight.</summary>
        public DayNightTimelineKey EvaluateTimeline(float hour)
        {
            if (timeline == null || timeline.Count == 0) return new DayNightTimelineKey();
            timeline.Sort((a, b) => a.hour.CompareTo(b.hour));
            DayNightTimelineKey first = timeline[0], last = timeline[timeline.Count - 1];
            DayNightTimelineKey previous, next;
            float from, to, h = hour;
            if (hour < first.hour || hour >= last.hour)
            {
                previous = last; next = first;
                from = last.hour; to = first.hour + 24f;
                if (h < first.hour) h += 24f;
            }
            else
            {
                int i = 0;
                while (i < timeline.Count - 1 && timeline[i + 1].hour <= hour) i++;
                previous = timeline[i]; next = timeline[Mathf.Min(i + 1, timeline.Count - 1)];
                from = previous.hour; to = next.hour;
            }
            float t = to > from ? Mathf.InverseLerp(from, to, h) : 0f;
            return new DayNightTimelineKey
            {
                hour = hour,
                sunColor = Color.Lerp(previous.sunColor, next.sunColor, t),
                sunIntensity = Mathf.Lerp(previous.sunIntensity, next.sunIntensity, t),
                ambientColor = Color.Lerp(previous.ambientColor, next.ambientColor, t),
                fogColor = Color.Lerp(previous.fogColor, next.fogColor, t),
                fogDensity = Mathf.Lerp(previous.fogDensity, next.fogDensity, t)
            };
        }
    }
}
