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
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindFirstObjectByType<PlayerController>() != null) return;
            string targetSpawn = scene.name switch
            {
                "20_StationDistrict" => "station_entrance",
                "30_SushiRestaurant" => "sushi_entrance",
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(targetSpawn)) return;

            var bootstrapObject = new GameObject("StandaloneZoneBootstrap_Runtime");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            bootstrapObject.AddComponent<StandaloneZoneBootstrap>().Configure(targetSpawn);
        }

        private void Start()
        {
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
