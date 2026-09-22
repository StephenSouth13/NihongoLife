using System;
using System.Collections.Generic;

namespace NihongoLife.Data
{
    [Serializable]
    public class ScenarioScoreRecord
    {
        public string scenarioId;
        public int bestScore;
        public long completedAt;
    }

    [Serializable]
    public class ActiveObjectiveRecord
    {
        public string objectiveId;
        public int state;
    }

    [Serializable]
    public class MasteryRecord
    {
        public string targetId; // vocabulary/grammar item ID
        public float masteryValue; // e.g. 0 to 100
        public long lastUpdatedAt;
    }

    [Serializable]
    public class CareerRecord
    {
        public string roleId;
        public int rank;
        public int completedShifts;
        public int reputation;
    }

    [Serializable]
    public class BusinessRecord
    {
        public string companyName;
        public int capitalYen;
        public int reputation;
        public int employeeCount;
        public int fairWageScore = 50;
        public int educationScore;
        public int partnershipCount;
    }

    [Serializable]
    public class PlayerProgressDto
    {
        public string playerId = "local_player";
        public string displayName = "Gakusei";
        public int xp = 0;
        public int level = 1;
        public int currentChapter = 1;
        public float health = 100f;
        public float energy = 100f;
        public float hunger = 100f;
        public float thirst = 100f;
        public int knowledge = 0;
        public int yen = 1200;
        public string activeScenarioId = string.Empty;
        public string activeScenarioNodeId = string.Empty;
        public List<ActiveObjectiveRecord> activeObjectives = new List<ActiveObjectiveRecord>();
        public string activeJobRole = string.Empty;
        public BusinessRecord business = new BusinessRecord();
        
        public List<string> completedScenarios = new List<string>();
        public List<string> storyFlags = new List<string>();
        public List<ScenarioScoreRecord> bestScores = new List<ScenarioScoreRecord>();
        public List<MasteryRecord> masteryLevels = new List<MasteryRecord>();
        public List<CareerRecord> careers = new List<CareerRecord>();
    }
}
