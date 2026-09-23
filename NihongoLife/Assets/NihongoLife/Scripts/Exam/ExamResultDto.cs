using System;
using System.Collections.Generic;

namespace NihongoLife.Exam
{
    [Serializable]
    public class ExamSectionResult
    {
        public string sectionId;
        public int scoredPoints;
        public int maxPoints;
        /// <summary>AI (or fallback rubric) feedback text for Essay/SpeakingPrompt sections; empty for objective sections.</summary>
        public string feedback;
        public bool aiGraded;
    }

    [Serializable]
    public class ExamQuestionRecord
    {
        public string questionId;
        public string givenAnswer;
        public bool correct;
        /// <summary>Essay/Speaking answers are not simply right/wrong; this holds the raw text/transcript so
        /// the review screen can show what was actually written or said.</summary>
        public string rawAnswerText;
    }

    /// <summary>One completed attempt. Stored in PlayerProgressDto.examAttempts (local + cloud save already
    /// carries this automatically since it round-trips through JsonUtility with the rest of the progress).</summary>
    [Serializable]
    public class ExamAttemptRecord
    {
        public string examId;
        public long completedAtUtcTicks;
        public int totalScore;
        public int totalMaxScore;
        /// <summary>JLPT only: whether both the total and every section met their pass thresholds.</summary>
        public bool passed;
        /// <summary>IELTS only: estimated overall band, 0-9 in 0.5 steps. This is Claude's own scoring
        /// model for practice purposes, not an official IELTS band and not affiliated with IELTS/British
        /// Council/IDP/Cambridge — see Docs/EXAM_SYSTEM.md.</summary>
        public float estimatedBand;
        public List<ExamSectionResult> sections = new List<ExamSectionResult>();
        public List<ExamQuestionRecord> answers = new List<ExamQuestionRecord>();
    }

    /// <summary>Full result of the attempt just finished, handed to the result screen. Not persisted as-is;
    /// ToRecord() is what gets appended to PlayerProgressDto.examAttempts.</summary>
    [Serializable]
    public class ExamAttemptResult
    {
        public ExamDefinition exam;
        public ExamAttemptRecord record = new ExamAttemptRecord();
        public bool isBestAttempt;

        public ExamSectionResult FindSection(string sectionId) => record.sections.Find(s => s.sectionId == sectionId);
    }
}
