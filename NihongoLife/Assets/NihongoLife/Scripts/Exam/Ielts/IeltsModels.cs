using System;
using System.Collections.Generic;

namespace NihongoLife.Exam.Ielts
{
    /// <summary>
    /// Content format for one IELTS test package (schema "nihongolife.ielts.v1"), loaded from JSON at runtime.
    /// Questions and answer keys live in separate files so the test screen never holds the answers before
    /// grading. Licensed material (e.g. Cambridge books) is only ever stored in the gitignored LocalContent
    /// folder next to Assets — never in Assets/, Resources/ or a build.
    /// Group types: completion (form / notes / sentence gaps written as {n} in line text), mcq_single,
    /// mcq_multi (choose N letters for questions from..to), matching (shared option box, one letter per item),
    /// tfng (True / False / Not Given), ynng (Yes / No / Not Given), writing_task, speaking_part.
    /// </summary>
    [Serializable]
    public sealed class IeltsTest
    {
        public string schema;
        public string id;
        public string title;
        public string skill;          // listening | reading | writing | speaking
        public string source;
        public string provenance;
        public bool localOnly;
        public int reviewSeconds = 120;  // listening: review time after the last part (computer-delivered IELTS)
        public int timeLimitMinutes;     // reading / writing: total time; 0 = derived from audio
        public IeltsPart[] parts;
    }

    [Serializable]
    public sealed class IeltsPart
    {
        public int number;
        public string title;
        public string audio;          // relative path inside the package (listening)
        public string passageTitle;   // reading
        public string passage;        // reading: paragraphs separated by blank lines
        public IeltsGroup[] groups;
    }

    [Serializable]
    public sealed class IeltsGroup
    {
        public string type;
        public int from;
        public int to;
        public int choose;            // mcq_multi
        public string instruction;
        public string limit;          // ONE_WORD | ONE_WORD_AND_OR_NUMBER | TWO_WORDS | NO_MORE_THAN_TWO_WORDS_AND_OR_A_NUMBER | NO_MORE_THAN_THREE_WORDS
        public string heading;
        public string stem;           // mcq_multi question / writing task / speaking prompt
        public string optionsTitle;
        public int minWords;          // writing_task
        public int minutes;           // writing_task / speaking_part guidance
        public IeltsLine[] lines;
        public IeltsOption[] options;
        public IeltsItem[] items;

        public bool Covers(int number) => number >= from && number <= to;
    }

    [Serializable]
    public sealed class IeltsLine
    {
        public string label;
        public string text;
        public string style;          // subheading | example | (empty)
    }

    [Serializable]
    public sealed class IeltsOption
    {
        public string letter;
        public string text;
    }

    [Serializable]
    public sealed class IeltsItem
    {
        public int number;
        public string text;
        public IeltsOption[] options;
    }

    [Serializable]
    public sealed class IeltsKey
    {
        public string schema;
        public string testId;
        public string source;
        public string verification;
        public IeltsKeyAnswer[] answers;
    }

    [Serializable]
    public sealed class IeltsKeyAnswer
    {
        public int number;
        public string[] accepted;     // key notation; "(...)" marks an optional part, e.g. "red(dish)", "12 (dollars)"
        public int[] set;             // "IN EITHER ORDER" questions share one set of letters
    }

    /// <summary>One saved attempt (answers are stored by question number).</summary>
    [Serializable]
    public sealed class IeltsAttempt
    {
        public string testId;
        public string mode;           // practice | exam
        public string startedUtc;
        public float elapsedSeconds;
        public int partIndex;
        public float[] audioPositions;
        public bool[] partsFinished;
        public float reviewRemaining = -1f;
        public List<IeltsResponse> responses = new();
        public bool submitted;
        public int rawScore;
        public float band;
    }

    [Serializable]
    public sealed class IeltsResponse
    {
        public int number;
        public string value;
    }

    [Serializable]
    public sealed class IeltsHistoryEntry
    {
        public string testId;
        public string finishedUtc;
        public string mode;
        public int rawScore;
        public int maxScore;
        public float band;
    }

    [Serializable]
    public sealed class IeltsHistory
    {
        public List<IeltsHistoryEntry> entries = new();
    }
}
