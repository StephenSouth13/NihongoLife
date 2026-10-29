using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// H3/H4 seasonal climate driven by the game calendar: a temperature for season, hour and weather, and the HUD clock text
    /// (time, weather, temperature). Pure functions, so tests can check every season.
    /// </summary>
    public static class ClimateService
    {
        public enum Sky { Clear, Cloudy, Rain }

        /// <summary>Mean temperature of the season in degrees Celsius (a temperate coastal town; the island is warmer).</summary>
        public static float SeasonMean(Season season) => season switch
        {
            Season.Summer => 28f,
            Season.Autumn => 18f,
            Season.Winter => 6f,
            _ => 15f
        };

        public static float TemperatureC(Season season, float hour, Sky sky, bool tropical = false)
        {
            // coldest around 05:00, warmest around 14:00
            float diurnal = Mathf.Cos((hour - 14f) / 24f * Mathf.PI * 2f) * 4.5f;
            float t = SeasonMean(season) + diurnal + (tropical ? 4f : 0f);
            if (sky == Sky.Rain) t -= 3f; else if (sky == Sky.Cloudy) t -= 1f;
            return t;
        }

        public static Sky CurrentSky()
        {
            var w = WeatherCycle.Instance;
            if (w == null) return Sky.Clear;
            if (w.IsRaining) return Sky.Rain;
            return w.Overcast > 0.5f ? Sky.Cloudy : Sky.Clear;
        }

        public static string SkyName(Sky sky, bool night) => sky switch
        {
            Sky.Rain => Loc.T("Mưa", "Rain", "雨"),
            Sky.Cloudy => Loc.T("Âm u", "Cloudy", "くもり"),
            _ => night ? Loc.T("Trời quang", "Clear", "晴れ") : Loc.T("Nắng", "Sunny", "晴れ")
        };

        public static string Format(Season season, float hour, Sky sky, bool tropical)
        {
            int h = Mathf.FloorToInt(hour) % 24, m = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
            bool night = hour < 5.5f || hour >= 19f;
            return $"{h:00}:{m:00} · {SkyName(sky, night)} · {Mathf.RoundToInt(TemperatureC(season, hour, sky, tropical))}°C";
        }

        /// <summary>HUD line for the active scene, or "" when there is no day cycle.</summary>
        public static string HudLine(DayNightCycle cycle, string sceneName)
        {
            if (cycle == null) return "";
            return Format(cycle.Season, cycle.Hour, CurrentSky(), sceneName == WorldLocationCatalog.MidoriIslandScene);
        }
    }
}