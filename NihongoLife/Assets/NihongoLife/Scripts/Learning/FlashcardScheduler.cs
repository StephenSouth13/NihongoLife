using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Learning
{
    /// <summary>One flashcard's review state (SM-2). Serializable so it can live on the progress save later.</summary>
    [Serializable]
    public class FlashcardState
    {
        public string id;
        public int repetitions;
        public int intervalDays;
        public float ease = 2.5f;
        public int dueDay;
        public int lapses;
    }

    /// <summary>
    /// Anki-style spaced repetition (SM-2), pure rules for the study desk's "Flashcards" option. Days are in-game
    /// calendar days. Grades: 0 Again, 1 Hard, 2 Good, 3 Easy. No UI and no save hook yet (scaffold for Edu v1).
    /// </summary>
    public static class FlashcardScheduler
    {
        public const float MinEase = 1.3f;

        public static void Review(FlashcardState card, int grade, int today)
        {
            if (card == null) return;
            grade = Mathf.Clamp(grade, 0, 3);
            if (grade == 0)
            {
                card.repetitions = 0;
                card.intervalDays = 0;
                card.lapses++;
                card.ease = Mathf.Max(MinEase, card.ease - 0.2f);
                card.dueDay = today; // again today
                return;
            }
            int q = grade + 2; // Hard 3, Good 4, Easy 5 on the SM-2 0..5 scale
            card.ease = Mathf.Max(MinEase, card.ease + (0.1f - (5 - q) * (0.08f + (5 - q) * 0.02f)));
            card.repetitions++;
            if (card.repetitions == 1) card.intervalDays = grade == 3 ? 3 : 1;
            else if (card.repetitions == 2) card.intervalDays = grade == 3 ? 8 : grade == 1 ? 4 : 6;
            else card.intervalDays = Mathf.Max(card.intervalDays + 1, Mathf.RoundToInt(card.intervalDays * card.ease * (grade == 1 ? 0.8f : grade == 3 ? 1.3f : 1f)));
            card.dueDay = today + card.intervalDays;
        }

        /// <summary>Cards due on or before today, most overdue first.</summary>
        public static List<FlashcardState> Due(IEnumerable<FlashcardState> cards, int today, int max = 20)
        {
            var list = new List<FlashcardState>();
            if (cards == null) return list;
            foreach (var c in cards) if (c != null && c.dueDay <= today) list.Add(c);
            list.Sort((a, b) => a.dueDay.CompareTo(b.dueDay));
            if (list.Count > max) list.RemoveRange(max, list.Count - max);
            return list;
        }
    }
}
