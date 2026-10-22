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
        public string GetpromptEn() => !exitsZone && !string.IsNullOrEmpty(targetScene) && !LocationUnlocks.IsUnlocked(targetScene)
            ? LocationUnlocks.ShortStatus(targetScene)
            : promptEn;
        public string TargetScene => targetScene;
        public Transform GetTransform() => transform;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void Start()
        {
            PortalBeacon.Attach(this, () => exitsZone ? NihongoLife.Core.Loc.T("Ra phố", "Exit to the street", "出口") : PlaceName(),
                () => InteractionPrompt.Of(this), exitsZone);
        }

        /// <summary>The destination's name in the interface language (catalog first, then the scene-authored "日本語 / Tiếng Việt" label).</summary>
        private string PlaceName()
        {
            if (WorldLocationCatalog.Has(targetScene)) return WorldLocationCatalog.Get(targetScene).DisplayName;
            int split = displayName != null ? displayName.IndexOf(" / ", System.StringComparison.Ordinal) : -1;
            if (split < 0) return displayName;
            return NihongoLife.Core.Loc.IsJapanese ? displayName.Substring(0, split) : displayName.Substring(split + 3);
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
            if (!GameServices.TryGet(out flow)) flow = SceneRefs.Find<SceneFlowController>();
            if (flow == null || string.IsNullOrWhiteSpace(targetScene)) return;
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Portal, 0.8f);
            if (exitsZone) flow.ExitZone(targetScene, targetSpawnId, displayName);
            else flow.EnterZone(targetScene, targetSpawnId, displayName);
        }
    }
}
