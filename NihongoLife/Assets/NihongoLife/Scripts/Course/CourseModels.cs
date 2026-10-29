using System.Collections.Generic;
using NihongoLife.Core;

namespace NihongoLife.Course
{
    public sealed class CourseSpeech
    {
        public string Locale, Text;
        public bool IsEmpty => string.IsNullOrEmpty(Text);

        public static CourseSpeech From(object node)
        {
            var d = node as Dictionary<string, object>;
            return d == null ? new CourseSpeech() : new CourseSpeech { Locale = d.Str("locale"), Text = d.Str("text") };
        }
    }

    public sealed class CourseWord
    {
        public string Id, Text, Reading;
        public CourseText Gloss = CourseText.Empty;
        public CourseSpeech Speech = new CourseSpeech();
    }

    public sealed class CourseLine
    {
        public string Id, Speaker, Text;
        public CourseText Translation = CourseText.Empty;
        public CourseSpeech Speech = new CourseSpeech();
    }

    public sealed class CourseExercise
    {
        public string Id, Type, Skill;
        public CourseText Prompt = CourseText.Empty;
        public List<CourseQuestion> Questions = new List<CourseQuestion>();
    }

    public sealed class CourseUnit
    {
        public string Id, Target, Theme, Level, GrammarId;
        public int No;
        public CourseText Title = CourseText.Empty;
        public List<CourseWord> Words = new List<CourseWord>();
        public List<CourseLine> Dialogue = new List<CourseLine>();
        public CourseText ReadingTitle = CourseText.Empty;
        public string ReadingText;
        public CourseText SpeakingPrompt = CourseText.Empty;
        public List<string> SpeakingModelLineIds = new List<string>();
        public List<CourseExercise> Exercises = new List<CourseExercise>();

        public int QuestionCount { get { int n = 0; foreach (var e in Exercises) n += e.Questions.Count; return n; } }

        public CourseLine Line(string id) => Dialogue.Find(l => l.Id == id);
    }

    public sealed class CourseGrammar
    {
        public string Id, Pattern, Level, ExampleText;
        public int UnitNo;
        public CourseText Title = CourseText.Empty;
        public CourseText Explanation = CourseText.Empty;
        public CourseText ExampleTranslation = CourseText.Empty;
    }

    public sealed class CourseGlyph
    {
        public string Id, Char, Sound, Name, Kind, ExampleWord, ExampleReading, ToneName;
        public CourseText ExampleGloss = CourseText.Empty;
        public CourseSpeech Speech = new CourseSpeech();
    }

    public sealed class CourseAlphabetLesson
    {
        public string Id;
        public CourseText Title = CourseText.Empty;
        public CourseText Tip = CourseText.Empty;
        public List<string> ItemIds = new List<string>();
    }

    public sealed class CourseAlphabet
    {
        public string Id, Target, Kind;
        public CourseText Title = CourseText.Empty;
        public CourseText Intro = CourseText.Empty;
        public List<CourseGlyph> Glyphs = new List<CourseGlyph>();
        public List<CourseAlphabetLesson> Lessons = new List<CourseAlphabetLesson>();
        public List<CourseQuestion> Quiz = new List<CourseQuestion>();

        public CourseGlyph Glyph(string id) => Glyphs.Find(g => g.Id == id);
    }

    public sealed class CoursePlacementLevel
    {
        public string Cefr, Label, StartUnitId, CertificateId;
    }

    public sealed class CoursePlacement
    {
        public string Id, Target;
        public CourseText Instructions = CourseText.Empty;
        public CourseText DontKnow = CourseText.Empty;
        public List<CourseQuestion> Questions = new List<CourseQuestion>();
        public List<CoursePlacementLevel> Levels = new List<CoursePlacementLevel>();
        public int PassPerLevel = 3;
    }

    public sealed class CourseSection
    {
        public string Id;
        public CourseText Label = CourseText.Empty;
        public string Directions;
        public List<CourseQuestion> Questions = new List<CourseQuestion>();
    }

    /// <summary>Writing / speaking task of the IELTS-style practice: shown as a prompt sheet, never scored.</summary>
    public sealed class CoursePromptCard
    {
        public string Id, Kind, Title, Prompt;
        public int MinWords, Minutes;
        public List<string> Lines = new List<string>();
    }

    public sealed class CourseCertificate
    {
        public string Id, Track, Target, Level, Cefr, Disclaimer;
        public CourseText Title = CourseText.Empty;
        public CourseText Label = CourseText.Empty;
        public int TimeLimitMinutes;
        public double PassRatio = 0.6;
        public List<CourseSection> Sections = new List<CourseSection>();
        public List<CoursePromptCard> Prompts = new List<CoursePromptCard>();
        public List<string> SelfCheck = new List<string>();

        public List<CourseQuestion> Questions
        {
            get { var all = new List<CourseQuestion>(); foreach (var s in Sections) all.AddRange(s.Questions); return all; }
        }
    }
}