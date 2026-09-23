using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Exam
{
    /// <summary>
    /// Loads every ExamDefinition under Resources/Exams. Adding a new JLPT level or IELTS test is
    /// dropping a new asset in that folder — no code change, no scene change (see Docs/EXAM_SYSTEM.md).
    /// </summary>
    public class ExamRepository : IGameService
    {
        private readonly List<ExamDefinition> _exams = new List<ExamDefinition>();

        public void Initialize()
        {
            _exams.Clear();
            var loaded = Resources.LoadAll<ExamDefinition>("Exams");
            foreach (var exam in loaded)
            {
                if (exam == null || string.IsNullOrEmpty(exam.id)) continue;
                _exams.Add(exam);
            }

            Debug.Log($"[ExamRepository] Loaded {_exams.Count} exam(s) from Resources/Exams.");
        }

        public IReadOnlyList<ExamDefinition> GetAllExams() => _exams;

        public ExamDefinition GetExamById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _exams.Find(e => e.id == id);
        }
    }
}
