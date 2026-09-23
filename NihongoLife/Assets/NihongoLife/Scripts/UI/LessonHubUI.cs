using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;

namespace NihongoLife.UI
{
    public class LessonHubUI : MonoBehaviour
    {
        [Header("Curriculum Selection")]
        [SerializeField] private Button jlptN5Button;
        [SerializeField] private Button jlptN4Button;
        [SerializeField] private Button ieltsButton;

        [Header("Panels")]
        [SerializeField] private GameObject jlptPanel;
        [SerializeField] private GameObject ieltsPanel;
        [SerializeField] private TMP_Text statusText;

        private readonly List<LessonCard> _lessons = new List<LessonCard>
        {
            new LessonCard("jlpt.n5.01", "JLPT N5 - Chao hoi", "Vocabulary + greeting", 1, "N5"),
            new LessonCard("jlpt.n5.02", "JLPT N5 - Mua sam", "Numbers + customer phrases", 2, "N5"),
            new LessonCard("jlpt.n4.01", "JLPT N4 - Ke hoach", "Time + te imasu", 3, "N4"),
            new LessonCard("ielts.01", "IELTS - Daily life", "Listening + speaking", 1, "IELTS"),
            new LessonCard("ielts.02", "IELTS - Reading", "Reading + writing", 2, "IELTS")
        };

        private void Awake()
        {
            if (jlptN5Button) jlptN5Button.onClick.AddListener(() => OpenJLPT(5));
            if (jlptN4Button) jlptN4Button.onClick.AddListener(() => OpenJLPT(4));
            if (ieltsButton) ieltsButton.onClick.AddListener(OpenIELTS);
        }

        private void OpenJLPT(int level)
        {
            ieltsPanel.SetActive(false);
            jlptPanel.SetActive(true);
            RenderCurriculum(level == 5 ? "N5" : "N4", jlptPanel.transform);
        }

        private void OpenIELTS()
        {
            jlptPanel.SetActive(false);
            ieltsPanel.SetActive(true);
            RenderCurriculum("IELTS", ieltsPanel.transform);
        }

        private void RenderCurriculum(string track, Transform panel)
        {
            PlayerProgressDto progress = GameServices.TryGet(out IProgressRepository repository)
                ? repository.GetProgress() : new PlayerProgressDto();
            if (progress == null) progress = new PlayerProgressDto();
            if (progress.completedScenarios == null) progress.completedScenarios = new List<string>();
            int completed = 0;
            foreach (var lesson in _lessons)
            {
                if (lesson.track != track) continue;
                bool done = progress.completedScenarios != null && progress.completedScenarios.Contains(lesson.id);
                bool unlocked = lesson.order == 1 || progress.completedScenarios.Contains(_lessons.Find(x => x.track == track && x.order == lesson.order - 1)?.id);
                completed += done ? 1 : 0;
                string state = done ? "Da hoan thanh" : unlocked ? "San sang" : "Dang khoa";
                var button = FindButton(panel, lesson.id);
                if (button != null)
                {
                    button.interactable = unlocked;
                    var label = button.GetComponentInChildren<TMP_Text>();
                    if (label != null) label.text = $"{lesson.title}\n<size=70%>{lesson.objective} - {state}</size>";
                }
            }
            if (statusText != null) statusText.text = $"{track}  |  {completed} bai da hoan thanh";
        }

        private static Button FindButton(Transform panel, string lessonId)
        {
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
                if (button.name.IndexOf(lessonId, System.StringComparison.OrdinalIgnoreCase) >= 0) return button;
            return null;
        }

        [System.Serializable]
        private sealed class LessonCard
        {
            public string id, title, objective, track;
            public int order;
            public LessonCard(string id, string title, string objective, int order, string track)
            { this.id = id; this.title = title; this.objective = objective; this.order = order; this.track = track; }
        }
    }
}
