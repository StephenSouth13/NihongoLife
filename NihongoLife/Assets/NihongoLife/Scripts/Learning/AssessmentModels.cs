using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NihongoLife.Learning
{
    public enum AssessmentTrack { JLPT, IELTS }
    public enum AssessmentSkill { Vocabulary, Grammar, Reading, Listening, Speaking, Writing }
    public enum AssessmentItemType { MultipleChoice, ShortAnswer, Ordering, ListeningChoice, SpeakingPrompt, WritingPrompt }

    [Serializable]
    public sealed class AssessmentQuestion
    {
        public string id;
        public AssessmentTrack track;
        public string level;
        public AssessmentSkill skill;
        public AssessmentItemType type;
        public string prompt;
        public string audioId;
        public List<string> options = new List<string>();
        public string answer;
        public string explanation;
        public string targetId;
        public int difficulty = 1;
        public string source;
    }

    [Serializable]
    public sealed class AssessmentBlueprint
    {
        public AssessmentTrack track;
        public string level;
        public int questionCount = 10;
        public int seed;
        public List<AssessmentSkill> skills = new List<AssessmentSkill>();
    }

    [Serializable]
    public sealed class AssessmentAnswer
    {
        public string questionId;
        public string response;
        public bool correct;
        public int elapsedSeconds;
    }

    [Serializable]
    public sealed class AssessmentResult
    {
        public AssessmentTrack track;
        public string level;
        public int total;
        public int correct;
        public int score;
        public List<AssessmentAnswer> answers = new List<AssessmentAnswer>();
        public List<string> weakTargetIds = new List<string>();
    }

    [CreateAssetMenu(fileName = "AssessmentQuestionBank", menuName = "NihongoLife/Learning Question Bank")]
    public sealed class AssessmentQuestionBank : ScriptableObject, IAssessmentRepository
    {
        [SerializeField] private List<AssessmentQuestion> questions = new List<AssessmentQuestion>();

        public IReadOnlyList<AssessmentQuestion> Find(AssessmentTrack track, string level, IReadOnlyList<AssessmentSkill> skills)
        {
            return questions.Where(q => q != null && q.track == track && q.level == level
                && (skills == null || skills.Count == 0 || skills.Contains(q.skill))).ToList();
        }
    }
}
