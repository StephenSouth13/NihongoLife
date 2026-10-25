using System;
using System.Collections.Generic;
using NihongoLife.Exam;
using NihongoLife.Player;

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
    public class ReviewRecord
    {
        public string questionId;
        public string targetId; // skill or vocab ID
        public int incorrectCount;
        public long nextReviewAt;
    }

    [Serializable]
    public class CareerRecord
    {
        public string roleId;
        public int rank;
        public int completedShifts;
        public int reputation;
        /// <summary>All shifts ever finished in this role (completedShifts resets on promotion).</summary>
        public int totalShifts;
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
        public float sleepiness = 0f;
        public int knowledge = 0;
        public int yen = 1200;
        public string activeScenarioId = string.Empty;
        public string activeScenarioNodeId = string.Empty;
        public List<ActiveObjectiveRecord> activeObjectives = new List<ActiveObjectiveRecord>();
        public string activeJobRole = string.Empty;
        public BusinessRecord business = new BusinessRecord();
        
        public List<string> completedScenarios = new List<string>();
        public List<string> storyFlags = new List<string>();
        public List<ExamAttemptRecord> examAttempts = new List<ExamAttemptRecord>();
        /// <summary>Submitted IELTS Writing/Speaking papers and AI feedback, synced with cloud progress.</summary>
        public List<NihongoLife.Exam.Ielts.IeltsProductiveRecord> productiveAttempts = new List<NihongoLife.Exam.Ielts.IeltsProductiveRecord>();
        public List<ScenarioScoreRecord> bestScores = new List<ScenarioScoreRecord>();
        public List<MasteryRecord> masteryLevels = new List<MasteryRecord>();
        public List<ReviewRecord> reviewItems = new List<ReviewRecord>();
        public List<CareerRecord> careers = new List<CareerRecord>();
        public List<NihongoLife.Player.InventoryEntry> inventory = new List<NihongoLife.Player.InventoryEntry>();
        public List<NihongoLife.Player.InventoryEntry> homeStorage = new List<NihongoLife.Player.InventoryEntry>();
        /// <summary>Midori Island: farm plots, vocabulary, animals, achievements and the chosen target language.</summary>
        public NihongoLife.Island.IslandRecord island = new NihongoLife.Island.IslandRecord();
        /// <summary>Quest / part-time job state (definitions: Resources/Progression/progression.json).</summary>
        public List<NihongoLife.Progression.QuestStateRecord> quests = new List<NihongoLife.Progression.QuestStateRecord>();
        public string trackedQuestId = string.Empty;
        /// <summary>The paper notebook (L): limited pages, more paper from Hibari Mart's stationery shelf.</summary>
        public NihongoLife.Notebook.NotebookRecord notebook = new NihongoLife.Notebook.NotebookRecord();
        /// <summary>In-game calendar. False on older saves so the first launch does not jump the clock.</summary>
        public bool calendarStarted;
        public int calendarDay;
        public float calendarHour;
        public int birthdayMonth = 4;
        public int birthdayDay = 15;
        public int ageAtEpoch = 16;
        /// <summary>Danh tiếng / 名声. Polite answers, giving up a seat, helping. Not a level gate.</summary>
        public int reputation;
        /// <summary>Tai tiếng / 悪名. Blocking cars, rudeness, littering.</summary>
        public int notoriety;
        /// <summary>Free on-demand AI exam gradings already used by this player (see ExamAiSettings.FreeGradings).</summary>
        public int aiFreeGradesUsed;
        public List<NpcAffinityRecord> affinities = new List<NpcAffinityRecord>();
        public List<MoodletRecord> moodlets = new List<MoodletRecord>();
    }
}
