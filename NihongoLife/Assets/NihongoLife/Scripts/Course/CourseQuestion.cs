using System.Collections.Generic;

namespace NihongoLife.Course
{
    public enum CourseQuestionKind { Choice, Typing, Ordering, Matching, TrueFalse }

    public sealed class CourseChoice
    {
        public string Id;
        public CourseText Text = CourseText.Empty;
    }

    public sealed class CoursePair
    {
        public string Id;
        public CourseText Left = CourseText.Empty;
        public CourseText Right = CourseText.Empty;
    }

    /// <summary>One normalized question, whatever file it came from (unit exercise, alphabet quiz, placement, certificate, tense drill).</summary>
    public sealed class CourseQuestion
    {
        public string Id, Skill, SourceType, Target, GroupId;
        public CourseQuestionKind Kind;
        public CourseText Prompt = CourseText.Empty;
        public CourseText Stem = CourseText.Empty;
        public string StemAudio, StemAudioLocale;
        /// <summary>Sentence with a {blank}; the blank is shown as an underscore gap.</summary>
        public string Blank;
        public CourseText Translation = CourseText.Empty;
        public CourseText Explanation = CourseText.Empty;
        /// <summary>Passage shown next to the question (reading text, listening script).</summary>
        public string Context, ContextTitle;
        public bool ContextIsListening;
        public string AudioLocale;
        public List<string> AudioLines = new List<string>();

        public List<CourseChoice> Choices = new List<CourseChoice>();
        public string AnswerId;
        public List<string> Accepted = new List<string>();
        public bool IgnoreCase = true, IgnorePunctuation = true, IgnoreAccents;
        public List<string> Tokens = new List<string>();
        /// <summary>Joins ordering tokens: a space for most languages, empty for Japanese.</summary>
        public string TokenSeparator = " ";
        public List<string> AnswerTokens = new List<string>();
        public List<CoursePair> Pairs = new List<CoursePair>();
        public bool AnswerBool;
        /// <summary>Allow "I don't know" (placement test).</summary>
        public bool AllowSkip;

        public bool HasAudio => !string.IsNullOrEmpty(StemAudio) || AudioLines.Count > 0;

        /// <summary>What to speak: the stem audio, else the whole script.</summary>
        public string SpeechText => !string.IsNullOrEmpty(StemAudio) ? StemAudio : string.Join(" ", AudioLines);
        public string SpeechLocale => !string.IsNullOrEmpty(StemAudioLocale) ? StemAudioLocale : AudioLocale;

        /// <summary>The correct answer as text, for the feedback line.</summary>
        public string AnswerDisplay
        {
            get
            {
                switch (Kind)
                {
                    case CourseQuestionKind.Choice:
                        foreach (var c in Choices) if (c.Id == AnswerId) return c.Text.Resolve();
                        return "";
                    case CourseQuestionKind.Typing: return Accepted.Count > 0 ? Accepted[0] : "";
                    case CourseQuestionKind.Ordering: return string.Join(TokenSeparator, AnswerTokens);
                    case CourseQuestionKind.TrueFalse: return AnswerBool ? "✓" : "✗";
                    default: return "";
                }
            }
        }
    }
}