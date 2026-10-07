using NihongoLife.Interaction;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.School
{
    /// <summary>The exam desk at the front of the Hibari classroom: F opens the JLPT / IELTS exam centre
    /// (the same exams that K opens anywhere), so the loaded tests have a physical place in the school.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ClassroomExamDesk : MonoBehaviour, IInteractable, IInteractionPriority
    {
        public float InteractionPriority => 0.5f;
        public string GetPromptJa() => "しけんを うける";
        public string GetpromptEn() => "Làm bài thi JLPT / IELTS";
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            var exams = FindFirstObjectByType<ExamCenterPopup>(FindObjectsInactive.Include);
            if (exams == null) return;
            ClassroomRuntime.Instance?.OnExamDeskUsed();
            exams.Show();
        }
    }
}
