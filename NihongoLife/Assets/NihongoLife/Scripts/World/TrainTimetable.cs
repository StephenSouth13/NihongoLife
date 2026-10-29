using System;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.World
{
    [Serializable]
    public sealed class TimetableStopData
    {
        public string id;
        /// <summary>Station name in kana (shown in Japanese).</summary>
        public string ja;
        /// <summary>Station name in romaji (shown in every other language).</summary>
        public string vi;
        /// <summary>What is at the stop.</summary>
        public LocalizedLine place;
        public string scene;
        public string spawn;
        public int price;
        /// <summary>Seconds of the leg that ends at this stop; 0 means the line default.</summary>
        public float legSeconds;
    }

    [Serializable]
    public sealed class TimetableData
    {
        public string schema;
        public string lineId;
        public LocalizedLine lineName;
        public int platform;
        /// <summary>A train arrives every this many seconds.</summary>
        public float serviceIntervalSeconds;
        /// <summary>Doors stay open this long.</summary>
        public float boardingWindowSeconds;
        /// <summary>How far the parked train rolls out of the station.</summary>
        public float trainTravelDistance;
        public float legSeconds;
        public float dwellSeconds;
        public TimetableStopData[] stops;
    }

    /// <summary>
    /// The Hibari line's timetable (Resources/Train/timetable.json): stops and fares, service interval, door window, leg and dwell times.
    /// StationTravelController reads everything from here; nothing about the line is hardcoded in the controller.
    /// </summary>
    public static class TrainTimetable
    {
        public const string ResourcePath = "Train/timetable";
        private static TimetableData _data;

        public static TimetableData Data => _data ??= Load();

        public static TimetableData Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing " + ResourcePath + ".json");
            var data = JsonUtility.FromJson<TimetableData>(asset.text);
            if (data == null || data.stops == null || data.stops.Length < 2) throw new InvalidOperationException("The timetable needs at least two stops.");
            return data;
        }

        public static void Reload() => _data = null;

        /// <summary>Seconds of the leg that ends at <paramref name="stop"/>.</summary>
        public static float LegSeconds(TimetableStopData stop) => stop != null && stop.legSeconds > 0f ? stop.legSeconds : Data.legSeconds;
    }
}
