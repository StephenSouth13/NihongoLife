using NihongoLife.Interaction;
using NihongoLife.UI;
using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.School
{
    /// <summary>The desk at the front of the Hibari classroom: F opens the classroom (lessons, videos and mock tests of every
    /// language, the same window as the dock's Classroom button), so studying has a physical place in the school.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ClassroomExamDesk : MonoBehaviour, IInteractable, IInteractionPriority
    {
        public float InteractionPriority => 0.5f;
        public string GetPromptJa() => "じゅぎょうを うける";
        public string GetpromptEn() => NihongoLife.Core.Loc.T("Vào học: bài học, video, thi thử", "Study: lessons, videos, mock tests", "授業：レッスン・動画・模試");
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            var hub = SceneRefs.Find<StudyHubPopup>(FindObjectsInactive.Include);
            if (hub == null) return;
            ClassroomRuntime.Instance?.OnExamDeskUsed();
            hub.Show();
        }
    }
}
