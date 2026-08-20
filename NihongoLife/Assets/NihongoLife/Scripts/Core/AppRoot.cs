using UnityEngine;
using NihongoLife.Audio;
using NihongoLife.Save;
using NihongoLife.Scenario;

namespace NihongoLife.Core
{
    public class AppRoot : MonoBehaviour
    {
        private static AppRoot _instance;

        [Header("Services")]
        [SerializeField] private AudioService audioService;
        [SerializeField] private SceneFlowController sceneFlowController;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void InitializeServices()
        {
            Debug.Log("[AppRoot] Initializing core services...");

            // 1. Scene Flow Controller
            if (sceneFlowController == null)
            {
                sceneFlowController = gameObject.AddComponent<SceneFlowController>();
            }
            GameServices.Register<SceneFlowController>(sceneFlowController);
            sceneFlowController.Initialize();

            // 2. Audio Service
            if (audioService == null)
            {
                audioService = gameObject.AddComponent<AudioService>();
            }
            GameServices.Register<IAudioService>(audioService);
            audioService.Initialize();

            // 3. Save / Progress Repository
            var progressRepo = new LocalProgressRepository();
            GameServices.Register<IProgressRepository>(progressRepo);
            progressRepo.Initialize();

            // 4. Scenario Repository
            var scenarioRepo = new LocalScenarioRepository();
            GameServices.Register<IScenarioRepository>(scenarioRepo);
            scenarioRepo.Initialize();

            Debug.Log("[AppRoot] Core services initialized successfully.");

            // Start loading main menu
            sceneFlowController.LoadScene("01_MainMenu");
        }
    }
}
