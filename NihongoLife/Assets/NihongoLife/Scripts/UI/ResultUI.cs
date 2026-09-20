using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Data;
using NihongoLife.Core;
using NihongoLife.Scenario;
using System.Collections.Generic;

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
        [SerializeField] private TextMeshProUGUI returnToMenuButtonText;

        private string _completedScenarioId;
        private string _nextScenarioId;
        private bool _lastResultWasSuccess;

        private void Start()
        {
            if (returnToMenuButtonText == null && returnToMenuButton != null)
            {
                returnToMenuButtonText = returnToMenuButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(OnContinueClicked);
            }
        }

        public void DisplayResults(ScoreBreakdownDto breakdown)
        {
            if (breakdown == null) return;
            _completedScenarioId = breakdown.scenarioId;
            _lastResultWasSuccess = breakdown.success;
            _nextScenarioId = _lastResultWasSuccess ? FindNextScenarioId(_completedScenarioId) : string.Empty;

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
                if (breakdown.success && (breakdown.rewardYen > 0 || breakdown.rewardKnowledge > 0))
                {
                    overallScoreText.text += $"\n<size=55%>+{breakdown.rewardKnowledge} kiến thức" + (breakdown.rewardYen > 0 ? $"   +¥{breakdown.rewardYen:N0}" : string.Empty) + "</size>";
                }
            }

            if (returnToMenuButtonText != null)
            {
                returnToMenuButtonText.text = string.IsNullOrEmpty(_nextScenarioId)
                    ? (_lastResultWasSuccess ? "Về menu" : "Thử lại")
                    : "Nhiệm tiếp theo";
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

        private void OnContinueClicked()
        {
            if (!string.IsNullOrEmpty(_nextScenarioId) && ScenarioManager.Instance != null)
            {
                Debug.Log($"[ResultUI] Continuing to next scenario: {_nextScenarioId}");
                ScenarioManager.Instance.StartScenario(_nextScenarioId);
                return;
            }

            if (!_lastResultWasSuccess && !string.IsNullOrEmpty(_completedScenarioId) && ScenarioManager.Instance != null)
            {
                Debug.Log($"[ResultUI] Retrying scenario: {_completedScenarioId}");
                ScenarioManager.Instance.StartScenario(_completedScenarioId);
                return;
            }

            OnReturnClicked();
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

        private static string FindNextScenarioId(string currentScenarioId)
        {
            if (string.IsNullOrEmpty(currentScenarioId)) return string.Empty;
            if (GameServices.TryGet(out ScenarioCampaignManager campaign))
            {
                return campaign.GetNextScenarioId(currentScenarioId);
            }

            if (GameServices.TryGet(out GameControlService control) && GameServices.TryGet(out IScenarioRepository configuredRepo))
            {
                return control.FindNextCampaignScenarioId(currentScenarioId, configuredRepo);
            }

            if (!GameServices.TryGet(out IScenarioRepository repo)) return string.Empty;

            List<ScenarioDefinition> scenarios = repo.GetAllScenarios();
            scenarios.RemoveAll(s => s == null || string.IsNullOrEmpty(s.id));
            scenarios.Sort((a, b) =>
            {
                int chapter = a.chapterIndex.CompareTo(b.chapterIndex);
                return chapter != 0 ? chapter : string.CompareOrdinal(a.id, b.id);
            });

            int index = scenarios.FindIndex(s => s.id == currentScenarioId);
            if (index < 0 || index + 1 >= scenarios.Count) return string.Empty;
            return scenarios[index + 1].id;
        }
    }
}
