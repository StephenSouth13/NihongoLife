using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Exam
{
    public enum ExamType { Jlpt, Ielts }

    /// <summary>JLPT: 文字語彙/文法読解/聴解. IELTS: the four skills. One enum covers both so the runtime
    /// (timer, navigation, question rendering) is shared code; only the label and scoring weight differ.</summary>
    public enum ExamSectionType
    {
        JlptVocabulary,
        JlptGrammarReading,
        JlptListening,
        IeltsListening,
        IeltsReading,
        IeltsWriting,
        IeltsSpeaking
    }

    public enum ExamQuestionType
    {
        /// <summary>Single correct choice out of 3-5 options.</summary>
        MultipleChoice,
        /// <summary>IELTS Reading style: choices are exactly "True", "False", "Not Given".</summary>
        TrueFalseNotGiven,
        /// <summary>Learner types a short word/phrase; graded by matching against acceptedAnswers (case/space-insensitive).</summary>
        FillBlank,
        /// <summary>Free-form long-form writing, graded by ExamGradingService (AI) with a rubric fallback.</summary>
        Essay,
        /// <summary>A cue card / talk prompt; the learner records with the microphone. Practice only — see
        /// ExamGradingService for what automated feedback can and cannot honestly provide for this type.</summary>
        SpeakingPrompt
    }

    /// <summary>A shared reading passage or listening script, referenced by id from one or more questions
    /// in the same section so it is shown once even when several questions depend on it.</summary>
    [Serializable]
    public class ExamPassage
    {
        public string id;
        [Tooltip("Audio used by Listening questions. Assign an imported WAV/MP3 clip in the exam asset.")]
        public AudioClip audioClip;
        [Tooltip("Optional CDN/Supabase Storage URL. Prefer this for WebGL instead of shipping audio in Assets.")]
        public string audioUrl;
        [Tooltip("Optional remote MP4/WebM URL for video-based listening material.")]
        public string videoUrl;
        [Tooltip("Optional public YouTube URL. Resolve it through the website player, never with a service key in Unity.")]
        public string youtubeUrl;
        [Min(0f)] public float mediaStartSeconds;
        [Min(0f)] public float mediaDurationSeconds;
        [Tooltip("0 = preserve source ratio; otherwise use width/height, e.g. 16/9 = 1.7778.")]
        [Min(0f)] public float mediaAspectRatio;
        [TextArea(4, 20)] public string titleVi;
        [TextArea(4, 20)] public string titleEn;
        [TextArea(4, 20)] public string titleJa;
        [TextArea(6, 30)] public string bodyVi;
        [TextArea(6, 30)] public string bodyEn;
        [TextArea(6, 30)] public string bodyJa;
        [TextArea(6, 30)] public string bodyReading;
        /// <summary>Listening sections: JLPT allows the audio to play only this many times (usually 1 or 2).
        /// Reading/Writing/Speaking passages leave this at 0 (unlimited / not applicable).</summary>
        public int maxPlays;
    }

    [Serializable]
    public class ExamChoice
    {
        public string textVi;
        public string textEn;
        public string textJa;
    }

    [Serializable]
    public class ExamQuestion
    {
        public string id;
        public ExamQuestionType type = ExamQuestionType.MultipleChoice;
        /// <summary>Id of the ExamPassage this question refers to, or empty for a standalone question.</summary>
        public string passageId;
        [TextArea(2, 6)] public string promptVi;
        [TextArea(2, 6)] public string promptEn;
        [TextArea(2, 6)] public string promptJa;
        [TextArea(1, 3)] public string promptReading;

        [Header("MultipleChoice / TrueFalseNotGiven")]
        public List<ExamChoice> choices = new List<ExamChoice>();
        public int correctChoiceIndex = -1;

        [Header("FillBlank")]
        public List<string> acceptedAnswers = new List<string>();

        [Header("Essay / SpeakingPrompt")]
        [TextArea(2, 6)] public string taskInstructionsVi;
        [TextArea(2, 6)] public string taskInstructionsEn;
        [Min(0)] public int minWords;
        [Min(0)] public int timeLimitSeconds;

        [TextArea(2, 8)] public string explanationVi;
        [TextArea(2, 8)] public string explanationEn;
        public List<string> tags = new List<string>();
        [Min(1)] public int points = 1;
        /// <summary>JLPT もんだい id inside the section (e.g. "m1"); used by ExamAnalysis for per-mondai advice.</summary>
        public string mondai;
    }

    /// <summary>A JLPT もんだい (question type) of a section: what it tests, for the result analysis.</summary>
    [Serializable]
    public class ExamMondai
    {
        public string id;
        public string titleJa;
        public string titleVi;
    }

    [Serializable]
    public class ExamSection
    {
        public string id;
        public ExamSectionType type = ExamSectionType.JlptVocabulary;
        public string titleVi;
        public string titleEn;
        public string titleJa;
        [Min(0)] public int timeLimitSeconds;
        /// <summary>JLPT: the section score is scaled to 0..scoreScaleMax (default 60, matching the
        /// familiar JLPT per-section range) regardless of how many raw questions this section has.
        /// IELTS: unused — IELTS sections report a 0-9 band instead.</summary>
        [Min(1)] public int scoreScaleMax = 60;
        /// <summary>JLPT scoring group (ExamDefinition.scoreGroups) this section counts toward; empty = scored alone.</summary>
        public string scoreGroup;
        public List<ExamMondai> mondai = new List<ExamMondai>();
        public List<ExamPassage> passages = new List<ExamPassage>();
        public List<ExamQuestion> questions = new List<ExamQuestion>();

        public ExamPassage FindPassage(string passageId)
        {
            if (string.IsNullOrEmpty(passageId) || passages == null) return null;
            return passages.Find(p => p != null && p.id == passageId);
        }

        public int MaxObjectivePoints()
        {
            int total = 0;
            if (questions == null) return 0;
            foreach (var q in questions)
            {
                if (q != null && q.type != ExamQuestionType.Essay && q.type != ExamQuestionType.SpeakingPrompt) total += q.points;
            }

            return total;
        }
    }
}
