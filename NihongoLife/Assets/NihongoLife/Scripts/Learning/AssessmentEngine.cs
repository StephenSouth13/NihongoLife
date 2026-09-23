using System;
using System.Collections.Generic;
using System.Linq;

namespace NihongoLife.Learning
{
    /// Builds reproducible assessments from curated items. A remote provider can
    /// replace the repository later without changing the exam or UI layer.
    public sealed class AssessmentEngine
    {
        private readonly IAssessmentRepository _repository;

        public AssessmentEngine(IAssessmentRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public IReadOnlyList<AssessmentQuestion> Build(AssessmentBlueprint blueprint)
        {
            if (blueprint == null) throw new ArgumentNullException(nameof(blueprint));
            if (blueprint.questionCount <= 0) return Array.Empty<AssessmentQuestion>();

            var candidates = _repository.Find(blueprint.track, blueprint.level, blueprint.skills)
                .Where(item => item != null)
                .ToList();
            if (candidates.Count == 0) return Array.Empty<AssessmentQuestion>();

            var random = new Random(blueprint.seed);
            var selected = new List<AssessmentQuestion>();
            var pool = new List<AssessmentQuestion>(candidates);
            while (selected.Count < blueprint.questionCount && pool.Count > 0)
            {
                int index = random.Next(pool.Count);
                selected.Add(pool[index]);
                pool.RemoveAt(index);
            }

            // If a small bank is used, fill by cycling only after every item has
            // appeared once. This keeps repetition explicit and deterministic.
            for (int i = 0; selected.Count < blueprint.questionCount; i++)
                selected.Add(candidates[i % candidates.Count]);
            return selected;
        }

        public AssessmentResult Grade(AssessmentBlueprint blueprint, IEnumerable<AssessmentAnswer> answers)
        {
            var result = new AssessmentResult { track = blueprint.track, level = blueprint.level };
            var byId = _repository.Find(blueprint.track, blueprint.level, blueprint.skills)
                .Where(item => item != null).ToDictionary(item => item.id, item => item);
            foreach (var answer in answers ?? Array.Empty<AssessmentAnswer>())
            {
                if (answer == null || !byId.TryGetValue(answer.questionId, out var question)) continue;
                answer.correct = Normalize(answer.response) == Normalize(question.answer);
                result.answers.Add(answer);
                if (answer.correct) result.correct++;
                else if (!string.IsNullOrWhiteSpace(question.targetId) && !result.weakTargetIds.Contains(question.targetId))
                    result.weakTargetIds.Add(question.targetId);
            }
            result.total = result.answers.Count;
            result.score = result.total == 0 ? 0 : (int)Math.Round(result.correct * 100d / result.total);
            return result;
        }

        private static string Normalize(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    public interface IAssessmentRepository
    {
        IReadOnlyList<AssessmentQuestion> Find(AssessmentTrack track, string level, IReadOnlyList<AssessmentSkill> skills);
    }
}
