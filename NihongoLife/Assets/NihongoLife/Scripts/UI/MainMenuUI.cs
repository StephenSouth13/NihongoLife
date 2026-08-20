using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        [Header("Profile Display")]
        [SerializeField] private TMPro.TextMeshProUGUI profileText;

        [Header("Settings")]
        [SerializeField] private string targetGameplayScene = "90_TestSandbox";
        [SerializeField] private string targetScenarioId = "scenario.konbini.buy_onigiri";

        private void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            DisplayProfileStats();
        }

        private void DisplayProfileStats()
        {
            if (profileText != null)
            {
                var progressRepo = GameServices.Get<Save.IProgressRepository>();
                if (progressRepo != null)
                {
                    var progress = progressRepo.GetProgress();
                    string details = $"Học viên: {progress.displayName}\n" +
                                     $"Cấp độ: {progress.level} (XP: {progress.xp})\n" +
                                     $"Đã hoàn thành: {progress.completedScenarios.Count} bài\n";

                    if (progress.bestScores.Count > 0)
                    {
                        details += "\nĐiểm số cao nhất:\n";
                        foreach (var score in progress.bestScores)
                        {
                            string displayId = score.scenarioId.Replace("scenario.konbini.", "Cửa hàng tiện lợi: ");
                            details += $"- {displayId}: {score.bestScore}/100\n";
                        }
                    }
                    profileText.text = details;
                }
            }
        }

        private void OnStartClicked()
        {
            Debug.Log("[MainMenuUI] Starting sandbox gameplay...");
            
            // Pass the target scenario ID dynamically using PlayerPrefs
            PlayerPrefs.SetString("ActiveScenarioId", targetScenarioId);
            PlayerPrefs.Save();

            var sceneFlow = GameServices.Get<SceneFlowController>();
            if (sceneFlow != null)
            {
                sceneFlow.LoadScene(targetGameplayScene);
            }
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] Quitting game...");
            Application.Quit();
        }
    }
}
