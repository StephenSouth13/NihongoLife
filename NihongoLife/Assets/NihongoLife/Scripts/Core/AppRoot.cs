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
        [SerializeField] private GameSettingsService settingsService;
        [SerializeField] private GameControlService controlService;
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private ScenarioCampaignManager campaignManager;
        [SerializeField] private OnlineWorldBootstrap onlineWorldBootstrap;
        [SerializeField] private CoopSessionService coopSessionService;

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

            // 3. Player-facing settings
            if (settingsService == null)
            {
                settingsService = gameObject.AddComponent<GameSettingsService>();
            }
            GameServices.Register<GameSettingsService>(settingsService);
            settingsService.Initialize();

            if (controlService == null)
            {
                controlService = gameObject.AddComponent<GameControlService>();
            }
            GameServices.Register<GameControlService>(controlService);
            controlService.Initialize();

            if (dayNightCycle == null)
            {
                dayNightCycle = gameObject.AddComponent<DayNightCycle>();
            }

            // Determine if online features are enabled
            bool onlineEnabled = controlService.Database != null && controlService.Database.enableOnlineSync;

            // 4. Supabase Client (foundation for all online services)
            SupabaseClient supabaseClient = null;
            if (onlineEnabled)
            {
                supabaseClient = gameObject.AddComponent<SupabaseClient>();
                supabaseClient.Initialize(controlService.Database);
            }

            // 5. Authentication Service
            if (onlineEnabled && supabaseClient != null && supabaseClient.IsConfigured)
            {
                var authService = gameObject.AddComponent<SupabaseAuthService>();
                GameServices.Register<IAuthService>(authService);
                authService.Initialize();
                Debug.Log("[AppRoot] Supabase Auth service registered.");
            }

            // 6. Save / Progress Repository (cloud or local)
            IProgressRepository progressRepo;
            if (onlineEnabled && supabaseClient != null && supabaseClient.IsConfigured)
            {
                progressRepo = new SupabaseProgressRepository(supabaseClient);
                Debug.Log("[AppRoot] Using SupabaseProgressRepository (cloud save).");
            }
            else
            {
                progressRepo = new LocalProgressRepository();
                Debug.Log("[AppRoot] Using LocalProgressRepository (offline save).");
            }
            GameServices.Register<IProgressRepository>(progressRepo);
            progressRepo.Initialize();

            // 7. Scenario Repository
            var scenarioRepo = new LocalScenarioRepository();
            GameServices.Register<IScenarioRepository>(scenarioRepo);
            scenarioRepo.Initialize();

            if (campaignManager == null)
            {
                campaignManager = gameObject.AddComponent<ScenarioCampaignManager>();
            }
            GameServices.Register<ScenarioCampaignManager>(campaignManager);
            campaignManager.Initialize();

            // 8. Online World Service (Supabase realtime or local simulation)
            IOnlineWorldService onlineWorld;
            if (onlineEnabled && supabaseClient != null && supabaseClient.IsConfigured)
            {
                var realtimeClient = gameObject.AddComponent<SupabaseRealtimeClient>();
                realtimeClient.Initialize(supabaseClient);

                var supabaseOnline = gameObject.AddComponent<SupabaseOnlineWorldService>();
                onlineWorld = supabaseOnline;
                Debug.Log("[AppRoot] Using SupabaseOnlineWorldService (real online).");
            }
            else
            {
                onlineWorld = new LocalOnlineWorldService();
                Debug.Log("[AppRoot] Using LocalOnlineWorldService (local simulation).");
            }
            GameServices.Register<IOnlineWorldService>(onlineWorld);
            onlineWorld.Initialize();

            if (coopSessionService == null)
            {
                coopSessionService = gameObject.AddComponent<CoopSessionService>();
            }
            GameServices.Register<CoopSessionService>(coopSessionService);
            coopSessionService.Initialize();

            if (onlineWorldBootstrap == null)
            {
                onlineWorldBootstrap = gameObject.AddComponent<OnlineWorldBootstrap>();
            }

            Debug.Log("[AppRoot] Core services initialized successfully.");

            // Start loading main menu only if we are in Bootstrap
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "00_Bootstrap")
            {
                sceneFlowController.LoadScene("01_MainMenu");
            }
        }
    }
}
