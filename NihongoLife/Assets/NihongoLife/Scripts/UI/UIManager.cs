using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] private StatusUI statusPanel;
        [SerializeField] private InventoryUI inventoryPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        /// <summary>Leaving for another place closes a result card still open (it belongs to where the quest ended).</summary>
        private void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (resultPanel != null && resultPanel.gameObject.activeInHierarchy) resultPanel.BackToTown();
        }

        private void Start()
        {
            // Auto-detect components if they exist on the same or child gameobjects
            if (mainMenuPanel == null) mainMenuPanel = GetComponentInChildren<MainMenuUI>(true);
            if (hudPanel == null) hudPanel = GetComponentInChildren<HUDUI>(true);
            if (resultPanel == null) resultPanel = GetComponentInChildren<ResultUI>(true);
            if (statusPanel == null) statusPanel = GetComponentInChildren<StatusUI>(true);
            if (inventoryPanel == null) inventoryPanel = GetComponentInChildren<InventoryUI>(true);

            ConfigureInitialUIState();

            EnsureScenarioSubscription();
        }

        // This manager lives across scenes (DontDestroyOnLoad, usually created before the town exists), while each town
        // load brings its own ScenarioManager and panels — so follow the current ones instead of binding once at Start.
        private ScenarioManager _subscribed;

        private void Update() => EnsureScenarioSubscription();

        private void EnsureScenarioSubscription()
        {
            if (_subscribed == ScenarioManager.Instance) return;
            if (_subscribed != null)
            {
                _subscribed.OnScenarioStarted -= HandleScenarioStarted;
                _subscribed.OnScenarioFinished -= HandleScenarioFinished;
            }
            _subscribed = ScenarioManager.Instance;
            if (_subscribed != null)
            {
                _subscribed.OnScenarioStarted += HandleScenarioStarted;
                _subscribed.OnScenarioFinished += HandleScenarioFinished;
            }
        }

        /// <summary>Panels destroyed with an old scene are found again in the loaded ones (inactive included).</summary>
        private void ResolvePanels()
        {
            if (resultPanel == null) resultPanel = SceneRefs.Find<ResultUI>(FindObjectsInactive.Include);
            if (hudPanel == null) hudPanel = SceneRefs.Find<HUDUI>(FindObjectsInactive.Include);
            if (statusPanel == null) statusPanel = SceneRefs.Find<StatusUI>(FindObjectsInactive.Include);
            if (inventoryPanel == null) inventoryPanel = SceneRefs.Find<InventoryUI>(FindObjectsInactive.Include);
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            if (_subscribed != null)
            {
                _subscribed.OnScenarioStarted -= HandleScenarioStarted;
                _subscribed.OnScenarioFinished -= HandleScenarioFinished;
            }
        }

        private void ConfigureInitialUIState()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (GameServices.TryGet(out SceneFlowController sceneFlow))
            {
                currentSceneName = sceneFlow.GetCurrentSceneName();
            }

            if (currentSceneName == "00_Bootstrap" || currentSceneName == "01_MainMenu")
            {
                ShowMainMenu();
            }
            else
            {
                ShowHUDOnly();
            }
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(true);
            if (hudPanel != null) hudPanel.gameObject.SetActive(false);
            if (resultPanel != null) resultPanel.gameObject.SetActive(false);
            if (statusPanel != null) statusPanel.gameObject.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false);
            
            // Ensure cursor is free in main menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ShowHUDOnly()
        {
            ResolvePanels();
            if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(false);
            if (hudPanel != null) hudPanel.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.gameObject.SetActive(false);
            if (statusPanel != null) statusPanel.gameObject.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false);
        }

        public void ShowResultScreen(ScoreBreakdownDto breakdown)
        {
            ResolvePanels();
            if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(false);
            if (hudPanel != null) hudPanel.gameObject.SetActive(false);
            if (statusPanel != null) statusPanel.gameObject.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false);
            
            if (resultPanel != null)
            {
                resultPanel.gameObject.SetActive(true);
                resultPanel.DisplayResults(breakdown);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ToggleStatusPanel()
        {
            if (statusPanel != null)
            {
                statusPanel.gameObject.SetActive(!statusPanel.gameObject.activeSelf);
            }
        }

        public void ToggleInventoryPanel()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.gameObject.SetActive(!inventoryPanel.gameObject.activeSelf);
            }
        }

        private void HandleScenarioStarted(ScenarioDefinition scenario)
        {
            ShowHUDOnly();
        }

        private void HandleScenarioFinished(ScoreBreakdownDto breakdown)
        {
            NihongoLife.Dialogue.DialogueManager.Instance?.CloseFinishedStory();
            ShowResultScreen(breakdown);
        }
    }
}
