using System.Collections.Generic;

namespace NihongoLife.Home
{
    /// <summary>Study desk multi-menu (rules only; StudyDeskInteractable still runs the word quiz).
    /// Options unlock with progress; the menu lists all and greys locked ones.</summary>
    public static class StudyDeskMenu
    {
        public enum Option { WordQuiz, Flashcards, Album, ExamPrep, Wardrobe }

        public struct Entry { public Option option; public string labelKey; public bool unlocked; public string lockReason; }

        public static List<Entry> Build(int wordsLearned, int dueCards, int photosInAlbum, bool hasWardrobe)
        {
            return new List<Entry>
            {
                new Entry { option = Option.WordQuiz, labelKey = "desk.quiz", unlocked = true },
                new Entry { option = Option.Flashcards, labelKey = "desk.flashcards", unlocked = wordsLearned >= 5,
                    lockReason = wordsLearned >= 5 ? null : "learn 5 words first" },
                new Entry { option = Option.Album, labelKey = "desk.album", unlocked = photosInAlbum > 0,
                    lockReason = photosInAlbum > 0 ? null : "take a photo first" },
                new Entry { option = Option.ExamPrep, labelKey = "desk.exam", unlocked = wordsLearned >= 30,
                    lockReason = wordsLearned >= 30 ? null : "learn 30 words first" },
                new Entry { option = Option.Wardrobe, labelKey = "desk.wardrobe", unlocked = hasWardrobe,
                    lockReason = hasWardrobe ? null : "buy an outfit first" },
            };
        }

        /// <summary>Badge for the Flashcards row: due count, capped "20+".</summary>
        public static string DueBadge(int due) => due <= 0 ? "" : due > 20 ? "20+" : due.ToString();
    }
}
