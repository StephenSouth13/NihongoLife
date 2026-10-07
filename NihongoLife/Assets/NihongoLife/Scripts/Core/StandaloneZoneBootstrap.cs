using NihongoLife.Cameras;
using NihongoLife.Interaction;
using NihongoLife.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    public sealed class StandaloneZoneBootstrap : MonoBehaviour
    {
        [SerializeField] private string spawnId = "default";

        public void Configure(string value) => spawnId = value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterStandaloneSceneSupport()
        {
            _bootingThroughCity = false;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static bool _bootingThroughCity;

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_bootingThroughCity || FindFirstObjectByType<PlayerController>() != null) return;
            string targetSpawn = scene.name switch
            {
                WorldLocationCatalog.StationScene => WorldLocationCatalog.StationEntrance,
                WorldLocationCatalog.SushiRestaurantScene => WorldLocationCatalog.SushiEntrance,
                WorldLocationCatalog.SchoolScene => WorldLocationCatalog.SchoolEntrance,
                WorldLocationCatalog.HomeBedroomScene => WorldLocationCatalog.HomeBedroomEntrance,
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(targetSpawn)) return;

            // Pressing Play inside a zone boots the real game: the city (HUD, bag, map, services, exits)
            // loads as the host and the zone is entered through SceneFlowController, exactly like walking in.
            if (Application.CanStreamedLevelBeLoaded(WorldLocationCatalog.CityScene))
            {
                var runner = new GameObject("StandaloneZoneBootstrap_CityBoot");
                DontDestroyOnLoad(runner);
                runner.AddComponent<StandaloneZoneBootstrap>().BootThroughCity(scene.name, targetSpawn);
                return;
            }

            var bootstrapObject = new GameObject("StandaloneZoneBootstrap_Runtime");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            bootstrapObject.AddComponent<StandaloneZoneBootstrap>().Configure(targetSpawn);
        }

        private string _zoneToEnter;

        private void BootThroughCity(string zoneScene, string targetSpawn)
        {
            _zoneToEnter = zoneScene;
            spawnId = targetSpawn;
            _bootingThroughCity = true;
            StartCoroutine(BootRoutine());
        }

        private System.Collections.IEnumerator BootRoutine()
        {
            yield return null; // let the zone finish its first frame before it is replaced
            AsyncOperation load = SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene, LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            float timeout = Time.realtimeSinceStartup + 10f;
            SceneFlowController flow = null;
            while (Time.realtimeSinceStartup < timeout)
            {
                if (!GameServices.TryGet(out flow)) flow = FindFirstObjectByType<SceneFlowController>();
                if (flow != null && !flow.IsLoading && FindFirstObjectByType<PlayerController>() != null) break;
                yield return null;
            }
            _bootingThroughCity = false;
            if (flow != null)
            {
                Debug.Log($"[StandaloneZoneBootstrap] Booted through {WorldLocationCatalog.CityScene}; entering {_zoneToEnter}.");
                flow.EnterZone(_zoneToEnter, spawnId, WorldLocationCatalog.Get(_zoneToEnter).DisplayName);
            }
            else Debug.LogError("[StandaloneZoneBootstrap] City booted without a SceneFlowController.");
            Destroy(gameObject);
        }

        public static bool IsBooting => _bootingThroughCity;

        private void Start()
        {
            if (!string.IsNullOrEmpty(_zoneToEnter)) return;
            PlayerController existingPlayer = FindFirstObjectByType<PlayerController>();
            if (existingPlayer != null)
            {
                DisablePreviewCameras();
                return;
            }

            Transform spawn = FindSpawn();
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetPositionAndRotation(
                spawn != null ? spawn.position : transform.position + Vector3.up * 0.3f,
                spawn != null ? spawn.rotation : Quaternion.identity);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.35f;
            controller.center = Vector3.up;
            controller.stepOffset = 0.35f;

            var ground = new GameObject("GroundCheck").transform;
            ground.SetParent(player.transform, false);
            ground.localPosition = new Vector3(0f, 0.08f, 0f);

            player.AddComponent<PlayerStatus>();
            player.AddComponent<PlayerInventory>();
            player.AddComponent<InteractionDetector>();
            player.AddComponent<CharacterAnimationController>();
            PlayerController playerController = player.AddComponent<PlayerController>();

            DisablePreviewCameras();
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
            cameraController.SetTarget(player.transform);

            Debug.Log($"[StandaloneZoneBootstrap] Spawned standalone player for {gameObject.scene.name}.", playerController);
        }

        private Transform FindSpawn()
        {
            foreach (SceneSpawnPoint point in FindObjectsByType<SceneSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (point.gameObject.scene != gameObject.scene) continue;
                if (string.Equals(point.Id, spawnId, System.StringComparison.OrdinalIgnoreCase)) return point.transform;
            }

            string expectedName = "Spawn_" + spawnId;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (string.Equals(candidate.name, expectedName, System.StringComparison.OrdinalIgnoreCase))
                        return candidate;
                }
            }

            Debug.LogError($"[StandaloneZoneBootstrap] Spawn '{spawnId}' was not found in {gameObject.scene.name}.");
            return null;
        }

        private static void DisablePreviewCameras()
        {
            foreach (ZonePreviewCamera preview in FindObjectsByType<ZonePreviewCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                preview.gameObject.SetActive(false);
            }
        }
    }
}
