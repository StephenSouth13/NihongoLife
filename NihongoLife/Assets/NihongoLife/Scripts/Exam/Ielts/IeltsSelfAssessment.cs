using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NihongoLife.Exam.Ielts
{
    [Serializable] public sealed class IeltsRubricBand { public int band; public IeltsText text; }

    [Serializable]
    public sealed class IeltsRubricCriterion
    {
        public string code;
        public IeltsText name;
        public IeltsText help;
        public List<IeltsRubricBand> bands = new();
        public List<IeltsText> checklist = new();
    }

    /// <summary>Self-assessment rubric paraphrased from the public IELTS band descriptors (Resources/Content/Ielts/self_assessment_rubric.json).</summary>
    [Serializable]
    public sealed class IeltsRubric
    {
        public string schema;
        public IeltsText source;
        public List<IeltsRubricCriterion> writing_task1 = new();
        public List<IeltsRubricCriterion> writing_task2 = new();
        public List<IeltsRubricCriterion> speaking = new();

        public const string ResourcePath = "Content/Ielts/self_assessment_rubric";
        private static IeltsRubric _loaded;

        public static IeltsRubric Loaded
        {
            get
            {
                if (_loaded != null) return _loaded;
                var asset = Resources.Load<TextAsset>(ResourcePath);
                try { _loaded = asset != null ? JsonUtility.FromJson<IeltsRubric>(asset.text) : null; } catch (Exception) { _loaded = null; }
                return _loaded ??= new IeltsRubric();
            }
        }

        public static void ResetCache() => _loaded = null;

        /// <summary>The four criteria for a Writing task (1 or 2) or for Speaking (task 0).</summary>
        public List<IeltsRubricCriterion> For(string skill, int task) =>
            skill == "speaking" ? speaking : task == 1 ? writing_task1 : writing_task2;
    }

    /// <summary>The learner's own marking of one Writing task or of a Speaking paper — always labelled "self-assessed".</summary>
    [Serializable]
    public sealed class IeltsSelfAssessment
    {
        public const string Source = "self-assessed";
        public string source = Source;
        public List<IeltsCriterionScore> criteria = new();
        /// <summary>Ticked checklist items, as "CODE:index".</summary>
        public List<string> ticked = new();
        public float band;
        public string createdUtc;

        public bool Complete => criteria.Count == 4 && criteria.All(c => c.band > 0f);

        /// <summary>Recomputes <see cref="band"/> with the official rounding (Writing task: down to the half band; Speaking: overall rounding).</summary>
        public float Compute(string skill)
        {
            band = !Complete ? 0f : skill == "speaking" ? IeltsBands.Speaking(criteria) : IeltsBands.WritingTask(criteria);
            return band;
        }

        public void Set(string code, float value)
        {
            var c = criteria.FirstOrDefault(x => x.code == code);
            if (c == null) criteria.Add(c = new IeltsCriterionScore { code = code });
            c.band = IeltsBands.Clamp(value);
            c.assessed = true;
            c.explanation = Source;
        }

        public float Get(string code) => criteria.FirstOrDefault(x => x.code == code)?.band ?? 0f;
    }

    public static class IeltsSelfAssessmentService
    {
        /// <summary>Overall self-assessed band for the record (Writing = (T1 + 2·T2)/3; Speaking = the paper band). 0 when incomplete.</summary>
        public static float Overall(IeltsProductiveRecord record)
        {
            if (record == null) return 0f;
            if (record.skill == "speaking") return record.speakingSelfAssessment != null && record.speakingSelfAssessment.Complete ? record.speakingSelfAssessment.Compute("speaking") : 0f;
            var t1 = record.writing.FirstOrDefault(t => t.task == 1)?.selfAssessment;
            var t2 = record.writing.FirstOrDefault(t => t.task == 2)?.selfAssessment;
            bool h1 = t1 != null && t1.Complete, h2 = t2 != null && t2.Complete;
            if (!h1 && !h2) return 0f;
            return IeltsBands.WritingOverall(h1 ? t1.Compute("writing") : 0f, h2 ? t2.Compute("writing") : 0f, h1, h2);
        }

        /// <summary>Stores the self-assessment next to the attempt (never touches the AI/teacher <c>overall</c>).</summary>
        public static void Save(IeltsProductiveRecord record)
        {
            if (record == null) return;
            record.selfAssessedOverall = Overall(record);
            IeltsProductiveStore.Save(record);
        }
    }
}
