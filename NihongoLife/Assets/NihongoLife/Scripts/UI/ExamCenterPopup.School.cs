using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>Links the exam hall to the school: a shortcut to the practice certificate that matches the learner's saved path.</summary>
    public partial class ExamCenterPopup
    {
        private void BuildHero()
        {
            BuildHeroCore();
            BuildSchoolShortcut();
        }

        /// <summary>The certificate step of the saved route that comes next (null when there is no route or all are passed).</summary>
        public static CourseRouteStep NextCertificateStep()
        {
            var route = LearningPath.CurrentRoute();
            return route?.Steps.FirstOrDefault(s => s.Type == "certificate" && !LearningPath.IsDone(s));
        }

        private void BuildSchoolShortcut()
        {
            var step = NextCertificateStep();
            string label = step != null
                ? Loc.T("Luyện theo lộ trình: ", "Practise on your path: ", "コースで練習：") + LearningPath.StepTitle(step)
                : Loc.T("Học và luyện tập trong Trường học", "Study and practise in the School", "学校で勉強・練習");
            var b = UiKit.Button(_hero, "SchoolShortcut", label, () => OpenSchool(step), UiKit.Kind.Accent, "cast_for_education", 13.5f, 10);
            float w = Mathf.Clamp(60f + label.Length * 8f, 260f, 520f);
            UiKit.Place((RectTransform)b.transform, _english ? 216f : 150f, 18f, w, 34f);
        }

        public void OpenSchool(CourseRouteStep step)
        {
            Hide();
            var school = GetComponent<CoursePopup>();
            if (school == null) school = gameObject.AddComponent<CoursePopup>();
            school.Initialize(Font);
            school.Show();
            if (step != null) school.OpenStep(step);
        }
    }
}