using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Data;
using NihongoLife.Core;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    public class ResultUI : MonoBehaviour
    {
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI overallScoreText;

        [Header("Breakdown Texts")]
        [SerializeField] private TextMeshProUGUI vocabularyScoreText;
        [SerializeField] private TextMeshProUGUI grammarScoreText;
        [SerializeField] private TextMeshProUGUI listeningScoreText;
        [SerializeField] private TextMeshProUGUI readingScoreText;
        [SerializeField] private TextMeshProUGUI accuracyScoreText;
        [SerializeField] private TextMeshProUGUI completionScoreText;

        [Header("Control Buttons")]
        [SerializeField] private Button returnToMenuButton;

        private void Start()
        {
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(OnReturnClicked);
            }
        }

        public void DisplayResults(ScoreBreakdownDto breakdown)
        {
            if (breakdown == null) return;

            // Resolve scenario names
            string titleJa = "ミッション完了";
            string titleEn = "Nhiệm vụ hoàn thành";
            
            if (GameServices.TryGet(out IScenarioRepository repo))
            {
                var definition = repo.GetScenarioById(breakdown.scenarioId);
                if (definition != null)
                {
                    titleJa = definition.titleJa;
                    titleEn = definition.titleEn;
                }
            }

            if (missionTitleText != null)
            {
                missionTitleText.text = $"{titleJa}\n<size=70%>{titleEn}</size>";
            }

            if (overallScoreText != null)
            {
                overallScoreText.text = $"{breakdown.overallScore} / 100";
            }

            // Find categories and populate
            foreach (var cat in breakdown.categories)
            {
                string catName = cat.category.ToLower();
                string scoreStr = $"{cat.score}";

                if (catName.Contains("vocab") && vocabularyScoreText != null) vocabularyScoreText.text = scoreStr;
                else if (catName.Contains("gram") && grammarScoreText != null) grammarScoreText.text = scoreStr;
                else if (catName.Contains("list") && listeningScoreText != null) listeningScoreText.text = scoreStr;
                else if (catName.Contains("read") && readingScoreText != null) readingScoreText.text = scoreStr;
                else if (catName.Contains("accur") && accuracyScoreText != null) accuracyScoreText.text = scoreStr;
                else if (catName.Contains("compl") && completionScoreText != null) completionScoreText.text = scoreStr;
            }
        }

        private void OnReturnClicked()
        {
            Debug.Log("[ResultUI] Returning to main menu...");
            
            // Release cursor lock
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (GameServices.TryGet(out SceneFlowController sceneFlow))
            {
                sceneFlow.LoadScene("01_MainMenu");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("01_MainMenu");
            }
        }
    }
}
