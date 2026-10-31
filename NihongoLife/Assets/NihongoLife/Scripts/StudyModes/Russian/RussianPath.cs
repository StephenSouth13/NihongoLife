using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Exam.Ielts;
using NihongoLife.StudyModes.Video;
using UnityEngine;

namespace NihongoLife.StudyModes.Russian
{
    [Serializable] public sealed class RuVideoSlot { public string id; public string youtubeUrl; public bool Filled => VideoCatalog.IsYouTube(youtubeUrl); }

    [Serializable]
    public sealed class RuPathStep
    {
        public string id;
        public string kind;   // alphabet | pronunciation | unit | cert | mock
        public string @ref;
        public IeltsText title;
        public RuVideoSlot video;
    }

    [Serializable] public sealed class RuPathStage { public string id; public IeltsText label; public List<RuPathStep> steps = new(); }
    [Serializable] public sealed class RuExample { public string ru; public IeltsText gloss; }
    [Serializable] public sealed class RuQuizItem { public string id; public string stem; public string[] choices; public int answer; public IeltsText explanation; }

    [Serializable]
    public sealed class RuPronunciationLesson
    {
        public string id;
        public IeltsText title;
        public IeltsText tip;
        public List<RuExample> examples = new();
        public List<RuQuizItem> quiz = new();
    }

    /// <summary>The Russian learning path: alphabet → pronunciation → A1…B2 units → TORFL (ТРКИ) practice and mock tests.</summary>
    [Serializable]
    public sealed class RuPath
    {
        public string schema;
        public string channelUrl;
        public IeltsText videoNote;
        public List<RuPathStage> stages = new();
        public List<RuPronunciationLesson> pronunciation = new();

        public IEnumerable<RuPathStep> Steps => stages.SelectMany(s => s.steps);
        public RuPronunciationLesson Pronunciation(string id) => pronunciation.FirstOrDefault(p => p.id == id);
    }

    [Serializable] public sealed class RuMockSection { public string id; public IeltsText label; public string directions; public string passage; public string script; public List<RuQuizItem> questions = new(); }
    [Serializable] public sealed class RuMockWriting { public IeltsText task; public int minWords; public string model; public List<IeltsText> checklist = new(); }
    [Serializable] public sealed class RuMockSpeaking { public string prompt; public string sample; }

    /// <summary>An original exam-format practice test (not an official TORFL paper) with its answer key.</summary>
    [Serializable]
    public sealed class RuMockTest
    {
        public string schema;
        public string id;
        public string level;
        public IeltsText title;
        public IeltsText disclaimer;
        public int minutes;
        public float passRatio;
        public List<RuMockSection> sections = new();
        public RuMockWriting writing;
        public List<RuMockSpeaking> speaking = new();

        public IEnumerable<RuQuizItem> Questions => sections.SelectMany(s => s.questions);
        public int Score(IReadOnlyDictionary<string, int> answers) => Questions.Count(q => answers.TryGetValue(q.id, out int a) && a == q.answer);
        public bool Passed(int score) => Questions.Any() && score >= Mathf.CeilToInt(passRatio * Questions.Count());
    }

    public static class RussianCourse
    {
        public const string PathResource = "Content/Russian/ru_path";
        public const string TestsFolder = "Content/Russian/tests/";
        public const string VideoModule = "ru-path";
        public static readonly string[] MockIds = { "torfl_a1_mock_1", "torfl_a2_mock_1", "torfl_b1_mock_1", "torfl_b2_mock_1" };

        private static RuPath _path;
        private static readonly Dictionary<string, RuMockTest> Mocks = new();

        public static RuPath Path
        {
            get
            {
                if (_path != null) return _path;
                var asset = Resources.Load<TextAsset>(PathResource);
                try { _path = asset != null ? JsonUtility.FromJson<RuPath>(asset.text) : null; } catch (Exception) { _path = null; }
                _path ??= new RuPath();
                RegisterVideos();
                return _path;
            }
        }

        public static RuMockTest Mock(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Mocks.TryGetValue(id, out var cached)) return cached;
            var asset = Resources.Load<TextAsset>(TestsFolder + id);
            RuMockTest test = null;
            try { test = asset != null ? JsonUtility.FromJson<RuMockTest>(asset.text) : null; } catch (Exception) { }
            if (test != null && string.IsNullOrEmpty(test.id)) test = null;
            Mocks[id] = test;
            return test;
        }

        /// <summary>Video id used in <see cref="VideoCatalog"/> for a path step's slot.</summary>
        public static string VideoId(RuPathStep step) => step?.video?.id;

        /// <summary>Filled slots (a YouTube URL from @NihongoLifeGame) join the in-game video catalog under module "ru-path".</summary>
        public static int RegisterVideos()
        {
            if (_path == null) return 0;
            var entries = new List<VideoEntry>();
            foreach (var step in _path.Steps)
            {
                if (step.video == null || !step.video.Filled || string.IsNullOrEmpty(step.video.id)) continue;
                entries.Add(new VideoEntry
                {
                    id = step.video.id, module = VideoModule, youtubeUrl = step.video.youtubeUrl.Trim(), playableUrl = "",
                    titleVi = step.title?.Get("vi"), titleEn = step.title?.Get("en"), titleJa = step.title?.Get("ja"),
                    summaryVi = "", summaryEn = "", summaryJa = "", transcript = "", attribution = "", license = "own",
                });
            }
            return VideoCatalog.Merge(entries);
        }

        public static void ResetCache() { _path = null; Mocks.Clear(); }
    }
}
