using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Scenario;
using NihongoLife.Data;

namespace NihongoLife.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private MainMenuUI mainMenuPanel;
        [SerializeField] private HUDUI hudPanel;
        [SerializeField] private ResultUI resultPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-detect components if they exist on the same or child gameobjects
            if (mainMenuPanel == null) mainMenuPanel = GetComponentInChildren<MainMenuUI>(true);
            if (hudPanel == null) hudPanel = GetComponentInChildren<HUDUI>(true);
            if (resultPanel == null) resultPanel = GetComponentInChildren<ResultUI>(true);

            ConfigureInitialUIState();

            // Connect Scenario events
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioStarted += HandleScenarioStarted;
                ScenarioManager.Instance.OnScenarioFinished += HandleScenarioFinished;
            }
        }

        private void OnDestroy()
        {
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioStarted -= HandleScenarioStarted;
                ScenarioManager.Instance.OnScenarioFinished -= HandleScenarioFinished;
            }
        }

        private void ConfigureInitialUIState()
        {
            var sceneFlow = GameServices.Get<SceneFlowController>();
            if (sceneFlow != null)
            {
                string currentSceneName = sceneFlow.GetCurrentSceneName();
                if (currentSceneName == "00_Bootstrap" || currentSceneName == "01_MainMenu")
                {
                    ShowMainMenu();
                }
                else
                {
                    ShowHUDOnly();
                }
            }
            else
            {
                // Fallback: If sceneFlow isn't ready, show main menu first
                ShowMainMenu();
            }
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(true);
            if (hudPanel != null) hudPanel.gameObject.SetActive(false);
            if (resultPanel != null) resultPanel.gameObject.SetActive(false);
            
            // Ensure cursor is free in main menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ShowHUDOnly()
        {
            if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(false);
            if (hudPanel != null) hudPanel.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.gameObject.SetActive(false);
        }

        public void ShowResultScreen(ScoreBreakdownDto breakdown)
        {
            if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(false);
            if (hudPanel != null) hudPanel.gameObject.SetActive(false);
            
            if (resultPanel != null)
            {
                resultPanel.gameObject.SetActive(true);
                resultPanel.DisplayResults(breakdown);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HandleScenarioStarted(ScenarioDefinition scenario)
        {
            ShowHUDOnly();
        }

        private void HandleScenarioFinished(ScoreBreakdownDto breakdown)
        {
            ShowResultScreen(breakdown);
        }
    }
}
