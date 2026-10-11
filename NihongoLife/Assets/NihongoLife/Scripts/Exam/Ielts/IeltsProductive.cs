using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NihongoLife.Exam.Ielts
{
    /// <summary>One criterion of the IELTS band descriptors (TA/TR, CC, LR, GRA for Writing; FC, LR, GRA, P for Speaking).</summary>
    [Serializable]
    public sealed class IeltsCriterionScore
    {
        public string code;
        public string name;
        public float band;
        public string explanation;
        /// <summary>False when the evidence needed for this criterion was not available (e.g. Pronunciation without usable audio).</summary>
        public bool assessed = true;
    }

    /// <summary>AI Examiner verdict on the learner's own, unaided work.</summary>
    [Serializable]
    public sealed class IeltsAiExamination
    {
        public List<IeltsCriterionScore> criteria = new();
        /// <summary>Computed locally from the criteria (never taken from the model).</summary>
        public float overall;
        public string summary;
        public string model;
        public string createdUtc;
        public bool fromCache;
        /// <summary>Set when no AI verdict could be produced (offline, limit reached, bad response...).</summary>
        public string error;
        public bool Ok => string.IsNullOrEmpty(error) && criteria.Count > 0;
    }

    /// <summary>One sentence-level problem found by the AI Coach.</summary>
    [Serializable]
    public sealed class IeltsCoachIssue
    {
        public int sentence;          // 1-based index of the sentence in the text
        public string original;       // the sentence (or the part of it) with the problem
        public string category;       // grammar | vocabulary | cohesion | task | spelling | punctuation
        public string problem;
        public string explanation;
        public string suggestion;     // corrected version
    }

    [Serializable]
    public sealed class IeltsCoachReport
    {
        public List<IeltsCoachIssue> issues = new();
        public string overallAdvice;
        public string createdUtc;
        public bool fromCache;
        public string error;
        public bool Ok => string.IsNullOrEmpty(error);
    }

    /// <summary>A rewrite after coaching: kept for progress, never used to estimate the learner's ability.</summary>
    [Serializable]
    public sealed class IeltsRewrite
    {
        public string text;
        public string createdUtc;
        public IeltsCoachReport coach;
    }

    [Serializable]
    public sealed class IeltsWritingTaskRecord
    {
        public int number;            // question number in the package (1 = Task 1, 2 = Task 2)
        public int task;              // 1 | 2
        public string prompt;
        public string original;
        public int words;
        public int minWords;
        public IeltsAiExamination examination;
        public IeltsCoachReport coach;
        public List<IeltsRewrite> rewrites = new();
    }

    /// <summary>Measured on the learner's machine from the recording itself (not from the transcript).</summary>
    [Serializable]
    public sealed class IeltsSpeakingMetrics
    {
        public float seconds;         // recording length
        public float speechSeconds;   // time with voice
        public int pauses;            // silences longer than 0.5 s between speech
        public float longestPause;
        public float meanRunSeconds;  // average stretch of continuous speech
        public float wordsPerMinute;  // transcript words / speech time (filled once a transcript exists)
        public float peakLevel;       // 0-1, to spot a muted or far-away microphone
    }

    [Serializable]
    public sealed class IeltsSpeakingAnswer
    {
        public int number;
        public int part;
        public string question;
        public string audioFile;      // WAV file name inside the speaking folder
        public string transcript;
        public IeltsSpeakingMetrics metrics;
    }

    /// <summary>A submitted Writing or Speaking paper with its AI verdicts, coaching and rewrites.</summary>
    [Serializable]
    public sealed class IeltsProductiveRecord
    {
        public string id;
        public string testId;
        public string testTitle;
        public string skill;          // writing | speaking
        public string mode;
        public string finishedUtc;
        public float elapsedSeconds;
        public List<IeltsWritingTaskRecord> writing = new();
        public List<IeltsSpeakingAnswer> speaking = new();
        public IeltsAiExamination speakingExamination;
        /// <summary>Overall band of the original submission (0 until the Examiner has answered).</summary>
        public float overall;
        /// <summary>The band went into the IELTS score history (once, from the original submission only).</summary>
        public bool countedInHistory;
    }

    /// <summary>IELTS rounding rules, applied locally to whatever the model reports per criterion.</summary>
    public static class IeltsBands
    {
        /// <summary>Overall (and Speaking) rule: averages ending in .25 go up to .5, .75 up to the next whole band.</summary>
        public static float RoundOverall(float mean)
        {
            float whole = Mathf.Floor(mean);
            float frac = mean - whole;
            if (frac < 0.25f) return whole;
            if (frac < 0.75f) return whole + 0.5f;
            return whole + 1f;
        }

        /// <summary>One Writing task: the mean of its four criteria, rounded down to the half band below.</summary>
        public static float WritingTask(IEnumerable<IeltsCriterionScore> criteria)
        {
            var assessed = criteria.Where(c => c.assessed).ToList();
            if (assessed.Count == 0) return 0f;
            return Mathf.Floor(assessed.Average(c => c.band) * 2f) / 2f;
        }

        /// <summary>Writing paper: Task 2 counts twice as much as Task 1.</summary>
        public static float WritingOverall(float task1, float task2, bool hasTask1, bool hasTask2)
        {
            if (hasTask1 && hasTask2) return RoundOverall((task1 + 2f * task2) / 3f);
            return hasTask2 ? task2 : task1;
        }

        public static float Speaking(IEnumerable<IeltsCriterionScore> criteria)
        {
            var assessed = criteria.Where(c => c.assessed).ToList();
            return assessed.Count == 0 ? 0f : RoundOverall(assessed.Average(c => c.band));
        }

        public static float Clamp(float band) => Mathf.Clamp(Mathf.Round(band * 2f) / 2f, 0f, 9f);
    }

    /// <summary>Submitted Writing/Speaking papers (next to the IELTS attempts, in persistentDataPath/ielts/productive).</summary>
    public static class IeltsProductiveStore
    {
        public static string Folder => Path.Combine(IeltsAttemptStore.Folder, "productive");
        public static string AudioFolder(string recordId) => Path.Combine(Folder, recordId + "_audio");

        public static void Save(IeltsProductiveRecord record)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, record.id + ".json"), JsonUtility.ToJson(record, true));
            // Writing text + assessment is also carried by the normal progress repository, so a signed-in
            // learner keeps the complete marked paper across devices. Speaking audio itself remains local.
            if (NihongoLife.Core.GameServices.TryGet(out NihongoLife.Save.IProgressRepository repository))
            {
                var progress = repository.GetProgress();
                progress.productiveAttempts ??= new List<IeltsProductiveRecord>();
                int index = progress.productiveAttempts.FindIndex(item => item.id == record.id);
                if (index >= 0) progress.productiveAttempts[index] = record;
                else progress.productiveAttempts.Add(record);
                repository.SaveProgress(progress);
            }
        }

        public static IeltsProductiveRecord Load(string id)
        {
            string path = Path.Combine(Folder, id + ".json");
            return File.Exists(path) ? JsonUtility.FromJson<IeltsProductiveRecord>(File.ReadAllText(path)) : null;
        }

        /// <summary>All submitted papers of a skill, oldest first (progress history).</summary>
        public static List<IeltsProductiveRecord> All(string skill)
        {
            var list = new List<IeltsProductiveRecord>();
            foreach (string file in Directory.Exists(Folder) ? Directory.GetFiles(Folder, "*.json") : Array.Empty<string>())
            {
                try
                {
                    var record = JsonUtility.FromJson<IeltsProductiveRecord>(File.ReadAllText(file));
                    if (record != null && (skill == null || record.skill == skill)) list.Add(record);
                }
                catch (Exception e) { Debug.LogWarning($"[IeltsProductiveStore] Skipped {Path.GetFileName(file)}: {e.GetType().Name}"); }
            }
            if (NihongoLife.Core.GameServices.TryGet(out NihongoLife.Save.IProgressRepository repository))
            {
                foreach (var record in repository.GetProgress().productiveAttempts ?? new List<IeltsProductiveRecord>())
                    if (record != null && (skill == null || record.skill == skill) && list.All(item => item.id != record.id)) list.Add(record);
            }
            return list.OrderBy(r => r.finishedUtc, StringComparer.Ordinal).ToList();
        }

        public static string NewId(string testId) => $"{testId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
    }
}
