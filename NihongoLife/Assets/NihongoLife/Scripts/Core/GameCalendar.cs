using System;

namespace NihongoLife.Core
{
    public enum Season { Spring, Summer, Autumn, Winter }

    public enum DayPhase { Dawn, Morning, Noon, Afternoon, Sunset, Evening, Night }

    public enum MoonKind { New, Crescent, Half, Gibbous, Full }

    /// <summary>
    /// In-game calendar. Day 0 is 1 April of year 1 (the school year in Hibari-chō). A day lasts
    /// <see cref="DayNightCycle"/>'s real-time cycle. The moon follows the synodic month.
    /// </summary>
    public static class GameCalendar
    {
        public const float SynodicDays = 29.530588f;
        public static readonly DateTime Epoch = new DateTime(1, 4, 1);

        public static DateTime DateOf(int dayIndex) => Epoch.AddDays(Math.Max(0, dayIndex));

        public static Season SeasonOf(int month) => month switch
        {
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            9 or 10 or 11 => Season.Autumn,
            _ => Season.Winter
        };

        public static Season SeasonOn(int dayIndex) => SeasonOf(DateOf(dayIndex).Month);

        /// <summary>Sunrise and sunset, in hours, for the season. Summer days are long, winter days short.</summary>
        public static void Daylight(Season season, out float sunrise, out float sunset)
        {
            switch (season)
            {
                case Season.Summer: sunrise = 4.85f; sunset = 19.35f; break;
                case Season.Winter: sunrise = 7.15f; sunset = 16.75f; break;
                default: sunrise = 6f; sunset = 18f; break;
            }
        }

        /// <summary>Maps a seasonal clock onto an equinox day (sunrise 6:00, sunset 18:00) so colours and the sun stay in step.</summary>
        public static float EquinoxHour(float hour, float sunrise, float sunset)
        {
            hour = hour - 24f * (float)Math.Floor(hour / 24f);
            if (hour < 0f) hour += 24f;
            float dayLen = Math.Max(1f, sunset - sunrise);
            if (hour >= sunrise && hour <= sunset) return Lerp(6f, 18f, (hour - sunrise) / dayLen);
            float nightLen = Math.Max(1f, 24f - dayLen);
            float since = hour >= sunset ? hour - sunset : hour + 24f - sunset;
            float equinox = 18f + 12f * (since / nightLen);
            return equinox >= 24f ? equinox - 24f : equinox;
        }

        public static DayPhase PhaseOf(float hour, float sunrise, float sunset)
        {
            hour = hour - 24f * (float)Math.Floor(hour / 24f);
            if (hour < 0f) hour += 24f;
            if (hour < sunrise - 0.65f || hour >= sunset + 2.5f) return DayPhase.Night;
            if (hour < sunrise + 0.95f) return DayPhase.Dawn;
            if (hour < 10.5f) return DayPhase.Morning;
            if (hour < 13.5f) return DayPhase.Noon;
            if (hour < sunset - 1.35f) return DayPhase.Afternoon;
            if (hour < sunset + 0.45f) return DayPhase.Sunset;
            return DayPhase.Evening;
        }

        /// <summary>0 new moon, 0.5 full moon, 1 new moon again.</summary>
        public static float MoonPhase01(int dayIndex, float hour) =>
            Repeat((Math.Max(0, dayIndex) + hour / 24f) / SynodicDays);

        public static MoonKind MoonOf(float phase01)
        {
            float x = Repeat(phase01);
            if (x < 0.035f || x > 0.965f) return MoonKind.New;
            if (x > 0.465f && x < 0.535f) return MoonKind.Full;
            if ((x > 0.215f && x < 0.285f) || (x > 0.715f && x < 0.785f)) return MoonKind.Half;
            if ((x >= 0.285f && x <= 0.465f) || (x >= 0.535f && x <= 0.715f)) return MoonKind.Gibbous;
            return MoonKind.Crescent;
        }

        public static int AgeOn(int dayIndex, int birthMonth, int birthDay, int ageAtEpoch)
        {
            birthMonth = ClampMonth(birthMonth);
            birthDay = ClampDay(birthMonth, birthDay);
            var date = DateOf(dayIndex).Date;
            int age = Math.Max(1, ageAtEpoch);
            var next = NextBirthdayOnOrAfter(Epoch, birthMonth, birthDay);
            int guard = 0;
            while (date >= next.Date && guard++ < 4000)
            {
                age++;
                next = next.AddYears(1);
            }
            return age;
        }

        public static bool IsBirthday(int dayIndex, int birthMonth, int birthDay)
        {
            var date = DateOf(dayIndex);
            birthMonth = ClampMonth(birthMonth);
            return date.Month == birthMonth && date.Day == ClampDay(birthMonth, birthDay);
        }

        public static string SeasonName(Season season) => season switch
        {
            Season.Summer => Loc.T("Hạ", "Summer", "夏"),
            Season.Autumn => Loc.T("Thu", "Autumn", "秋"),
            Season.Winter => Loc.T("Đông", "Winter", "冬"),
            _ => Loc.T("Xuân", "Spring", "春")
        };

        public static string PhaseName(DayPhase phase) => phase switch
        {
            DayPhase.Dawn => Loc.T("Bình minh", "Dawn", "明け方"),
            DayPhase.Morning => Loc.T("Sáng", "Morning", "朝"),
            DayPhase.Noon => Loc.T("Trưa", "Noon", "昼"),
            DayPhase.Afternoon => Loc.T("Chiều", "Afternoon", "午後"),
            DayPhase.Sunset => Loc.T("Hoàng hôn", "Sunset", "夕方"),
            DayPhase.Evening => Loc.T("Tối", "Evening", "宵"),
            _ => Loc.T("Đêm", "Night", "夜")
        };

        public static string MoonName(MoonKind kind) => kind switch
        {
            MoonKind.New => Loc.T("không trăng", "new moon", "新月"),
            MoonKind.Crescent => Loc.T("trăng lưỡi liềm", "crescent", "三日月"),
            MoonKind.Half => Loc.T("trăng bán nguyệt", "half moon", "半月"),
            MoonKind.Gibbous => Loc.T("trăng khuyết", "gibbous", "十三夜"),
            _ => Loc.T("trăng tròn", "full moon", "満月")
        };

        /// <summary>One short HUD line: day count, date, season, age.</summary>
        public static string Compact(int dayIndex, int birthMonth, int birthDay, int ageAtEpoch)
        {
            var date = DateOf(dayIndex);
            int age = AgeOn(dayIndex, birthMonth, birthDay, ageAtEpoch);
            string season = SeasonName(SeasonOf(date.Month));
            return Loc.F(
                "{0} · {1}/{2} · {3} · {4} tuổi",
                "Day {0} · {2}/{1} · {3} · age {4}",
                "{0}日目 · {2}/{1} · {3} · {4}歳",
                dayIndex + 1, date.Day, date.Month, season, age);
        }

        public static string ProfileLine(int dayIndex, float hour, int birthMonth, int birthDay, int ageAtEpoch)
        {
            var date = DateOf(dayIndex);
            Daylight(SeasonOf(date.Month), out float rise, out float set);
            int age = AgeOn(dayIndex, birthMonth, birthDay, ageAtEpoch);
            string season = SeasonName(SeasonOf(date.Month));
            string phase = PhaseName(PhaseOf(hour, rise, set));
            string moon = MoonName(MoonOf(MoonPhase01(dayIndex, hour)));
            return Loc.F(
                "Ngày {0} · {1}/{2} · {3} · {4} · {5} tuổi · {6}",
                "Day {0} · {2}/{1} · {3} · {4} · age {5} · {6}",
                "{0}日目 · {2}月{1}日 · {3} · {4} · {5}歳 · {6}",
                dayIndex + 1, date.Day, date.Month, season, phase, age, moon);
        }

        public static int ClampMonth(int month) => month < 1 || month > 12 ? 4 : month;
        public static int ClampDay(int month, int day)
        {
            month = ClampMonth(month);
            int max = DateTime.DaysInMonth(2001, month);
            return day < 1 ? 15 : Math.Min(day, max);
        }

        private static DateTime NextBirthdayOnOrAfter(DateTime day, int month, int dayOfMonth)
        {
            int d = Math.Min(dayOfMonth, DateTime.DaysInMonth(day.Year, month));
            var candidate = new DateTime(day.Year, month, d);
            if (candidate.Date < day.Date) candidate = SafeAddYear(candidate);
            return candidate;
        }

        private static DateTime SafeAddYear(DateTime value)
        {
            int year = value.Year + 1;
            int day = Math.Min(value.Day, DateTime.DaysInMonth(year, value.Month));
            return new DateTime(year, value.Month, day);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);
        private static float Repeat(float value)
        {
            float x = value - (float)Math.Floor(value);
            return x < 0f ? x + 1f : x;
        }
    }
}
