using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Audio;

namespace NihongoLife.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class ScenePortal : MonoBehaviour, IInteractable, IInteractionPriority
    {
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnId = "entrance";
        [SerializeField] private string displayName = "移動 / Di chuyển";
        [SerializeField] private string promptJa = "入る";
        [SerializeField] private string promptEn = "Đi vào";
        [SerializeField] private bool exitsZone;

        public string TargetSpawnId => targetSpawnId;
        public float InteractionPriority => 0.75f;
        public string GetPromptJa() => promptJa;
        public string GetpromptEn() => promptEn;
        public Transform GetTransform() => transform;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void Start()
        {
            string title = exitsZone ? "出口 · Ra phố" : displayName.Replace(" / ", " · ");
            PortalBeacon.Attach(this, title, exitsZone ? "Trở lại thành phố" : promptEn, exitsZone);
        }

        public void Configure(string scene, string spawnId, string locationName, bool exit)
        {
            targetScene = scene;
            targetSpawnId = spawnId;
            displayName = locationName;
            exitsZone = exit;
        }

        public void Interact(GameObject player)
        {
            if (exitsZone && ExitGuard.TryBlock()) return;

            SceneFlowController flow = null;
            if (!GameServices.TryGet(out flow)) flow = FindFirstObjectByType<SceneFlowController>();
            if (flow == null || string.IsNullOrWhiteSpace(targetScene)) return;
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Portal, 0.8f);
            if (exitsZone) flow.ExitZone(targetScene, targetSpawnId, displayName);
            else flow.EnterZone(targetScene, targetSpawnId, displayName);
        }
    }
}
