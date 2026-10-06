using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Exam
{
    /// <summary>
    /// One full mock test (a JLPT level or an IELTS practice test). Data-driven: adding a new exam is
    /// duplicating an asset in Resources/Exams and editing its sections/questions in the Inspector — no
    /// code change. See Docs/EXAM_SYSTEM.md for the content pipeline and Tools/exam/ for generator scripts.
    /// </summary>
    public class ExamDefinition : ScriptableObject
    {
        public string id;
        public ExamType examType = ExamType.Jlpt;
        /// <summary>JLPT: "N5".."N1". IELTS: "Academic" or "General Training".</summary>
        public string level = "N5";
        [Header("Learner level mapping")]
        [Tooltip("Human-readable level such as Basic, Elementary, Intermediate, Upper-intermediate or Advanced.")]
        public string learnerLevel = "Beginner";
        [Min(0f)] public float recommendedBandMin;
        [Min(0f)] public float recommendedBandMax;
        public string titleVi;
        public string titleEn;
        public string titleJa;
        [TextArea(2, 4)] public string descriptionVi;
        [TextArea(2, 4)] public string descriptionEn;
        public List<ExamSection> sections = new List<ExamSection>();

        [Header("JLPT pass criteria (ignored for IELTS)")]
        [Min(1)] public int jlptTotalPassScore = 80;
        [Min(0)] public int jlptSectionPassScore = 19;

        public ExamSection FindSection(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId) || sections == null) return null;
            return sections.Find(s => s != null && s.id == sectionId);
        }
    }
}
