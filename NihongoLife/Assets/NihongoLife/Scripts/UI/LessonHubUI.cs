using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            // Logic to fetch N5/N4 curriculum data based on `level` goes here
        }

        private void OpenIELTS()
        {
            jlptPanel.SetActive(false);
            ieltsPanel.SetActive(true);
            // Logic to fetch IELTS curriculum data goes here
        }
    }
}
