using System;
using System.Collections.Generic;

namespace NihongoLife.MiniGames
{
    /// <summary>One wrong answer: what was expected and what the player picked (learning target ids).</summary>
    [Serializable]
    public struct MiniGameMistake
    {
        public string expectedTargetId;
        public string chosenTargetId;
        public string note;
    }

    /// <summary>Outcome of one mini-game round, shared by every mini-game and handed to the existing
    /// scoring/mastery layer by <see cref="MiniGameController"/>.</summary>
    [Serializable]
    public sealed class MiniGameResult
    {
        public string gameId;
        public string contentId;
        public bool completed;
        public int score;
        public float accuracy;
        public int correctCount;
        public int incorrectCount;
        public List<MiniGameMistake> mistakes = new();
        public List<string> learningTargetIds = new();
        /// <summary>Targets answered right on the first try (mastery up); the others go down.</summary>
        public List<string> masteredTargetIds = new();
        public float completionSeconds;
        public float masteryGain;
        public int expReward;
        public int knowledgeReward;

        // Filled by MiniGameController once the result reached the shared systems.
        public bool submittedToScoring;
        public bool submittedToMastery;
    }
}
