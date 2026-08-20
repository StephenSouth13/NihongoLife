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

        [Header("Settings")]
        [SerializeField] private string targetGameplayScene = "90_TestSandbox";
        [SerializeField] private string targetScenarioId = "scenario.konbini.buy_onigiri";

        private void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            Debug.Log("[MainMenuUI] Starting sandbox gameplay...");
            
            var sceneFlow = GameServices.Get<SceneFlowController>();
            if (sceneFlow != null)
            {
                // We load the scene first
                sceneFlow.LoadScene(targetGameplayScene);
                
                // Note: The Scenario will be started automatically by an initializer in the loaded scene,
                // or we can start it directly once loaded.
                // We'll write a simple ScenarioSceneInitializer to automatically launch the scenario.
            }
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] Quitting game...");
            Application.Quit();
        }
    }
}
