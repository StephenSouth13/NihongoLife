using NihongoLife.Save;
using NihongoLife.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    public class OnlineWorldBootstrap : MonoBehaviour
    {
        [SerializeField] private float posePublishIntervalSeconds = 0.2f;

        private float _nextPublishTime;

        private void Start()
        {
            if (!GameServices.TryGet(out IOnlineWorldService onlineWorld) || onlineWorld.IsConnected)
            {
                return;
            }

            string playerId = SystemInfo.deviceUniqueIdentifier;
            string displayName = PlayableCharacterCatalog.GetPlayerName();
            if (GameServices.TryGet(out IProgressRepository progressRepository))
            {
                var progress = progressRepository.GetProgress();
                if (displayName == PlayableCharacterCatalog.DefaultPlayerName && progress != null && !string.IsNullOrWhiteSpace(progress.displayName))
                {
                    displayName = progress.displayName;
                }
            }

            onlineWorld.ConnectLocalPlayer(playerId, displayName);
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextPublishTime) return;
            _nextPublishTime = Time.unscaledTime + posePublishIntervalSeconds;

            if (!GameServices.TryGet(out IOnlineWorldService onlineWorld) || !onlineWorld.IsConnected)
            {
                return;
            }

            var player = GameObject.FindWithTag("Player");
            if (player == null) return;

            onlineWorld.UpdateLocalPlayerPose(
                SceneManager.GetActiveScene().name,
                player.transform.position,
                player.transform.rotation);
        }
    }
}
