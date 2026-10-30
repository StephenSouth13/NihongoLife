using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Learning
{
    /// <summary>Saves the study desk extras (flashcard states, album, wardrobe, words seen) as one JSON blob in PlayerPrefs.
    /// Days are real calendar days for now (the in-game calendar hook comes with the progress save).</summary>
    public static class StudyProgressStore
    {
        public const string Key = "nl.studydesk.v1";

        [Serializable]
        public sealed class Data
        {
            public List<FlashcardState> cards = new List<FlashcardState>();
            public StudyAlbum album = new StudyAlbum();
            public NihongoLife.Home.WardrobeCatalog wardrobe = new NihongoLife.Home.WardrobeCatalog();
            public int wordsSeen;
        }

        private static Data _data;
        public static Data Current => _data ??= Load();
        public static event Action Changed;

        /// <summary>The in-game day (DayNightCycle.Day) when a day/night cycle is running, else real calendar days since 2024-01-01.</summary>
        public static int Today
        {
            get
            {
                var c = NihongoLife.Core.DayNightCycle.Active;
                if (c != null && c.Day > 0) return c.Day;
                return (int)(DateTime.Today - new DateTime(2024, 1, 1)).TotalDays;
            }
        }

        private static Data Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            Data d = null;
            if (!string.IsNullOrEmpty(json)) { try { d = JsonUtility.FromJson<Data>(json); } catch { d = null; } }
            d ??= new Data();
            d.cards ??= new List<FlashcardState>(); d.album ??= new StudyAlbum(); d.wardrobe ??= new NihongoLife.Home.WardrobeCatalog();
            if (!d.wardrobe.Owns("server_uniform")) d.wardrobe.owned.Add("server_uniform"); // the job's uniform comes free
            return d;
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ResetForTests() { _data = new Data(); }

        /// <summary>Makes sure every word id has a card (new words are due today).</summary>
        public static void EnsureCards(IEnumerable<string> ids, int today)
        {
            foreach (var id in ids)
                if (!string.IsNullOrEmpty(id) && !Current.cards.Exists(c => c.id == id))
                    Current.cards.Add(new FlashcardState { id = id, dueDay = today });
        }

        public static void AddWordsSeen(int n)
        {
            int before = Current.wordsSeen;
            Current.wordsSeen += Mathf.Max(0, n);
            Save();
            AlbumRecorder.RecordWords(before, Current.wordsSeen);
        }

        public static bool AddPhoto(string id, string place, string captionJa, string captionLocal)
        {
            bool added = Current.album.Add(id, place, captionJa, captionLocal, Today);
            if (added) Save();
            return added;
        }
    }
}
