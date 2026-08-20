using System;
using System.Collections.Generic;

namespace NihongoLife.Data
{
    [Serializable]
    public class ScoreCategoryResult
    {
        public string category; // e.g. "Vocabulary", "Grammar", "Listening", "ResponseAccuracy", "TaskCompletion"
        public int score; // 0 to 100
    }

    [Serializable]
    public class ScoreBreakdownDto
    {
        public string scenarioId;
        public int overallScore;
        public bool success;
        public List<ScoreCategoryResult> categories = new List<ScoreCategoryResult>();
    }
}
