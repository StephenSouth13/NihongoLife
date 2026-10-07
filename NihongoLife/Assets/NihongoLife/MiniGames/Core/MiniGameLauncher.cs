using NihongoLife.Interaction;
using NihongoLife.Scenario;
using UnityEngine;

namespace NihongoLife.MiniGames
{
    /// <summary>
    /// An arcade machine the player walks up to and presses F on. Playable machines start their
    /// mini-game through <see cref="MiniGameController"/>; machines whose game is not built yet say so.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class MiniGameLauncher : MonoBehaviour, IInteractable, IInteractionPriority, IConditionalInteractable
    {
        [SerializeField] private MiniGameDefinition definition;
        [SerializeField] private Transform viewPoint;

        public MiniGameDefinition Definition => definition;
        public Transform ViewPoint => viewPoint;
        public float InteractionPriority => 0.4f;
        public bool IsInteractionAvailable => MiniGameController.Instance == null || !MiniGameController.Instance.IsRunning;

        public void Configure(MiniGameDefinition gameDefinition, Transform cameraPoint)
        {
            definition = gameDefinition;
            viewPoint = cameraPoint;
        }

        public string GetPromptJa() => definition != null ? definition.titleJa : "ゲーム";
        public string GetpromptEn() => definition == null ? "Chơi"
            : definition.playable ? $"Chơi {definition.titleVi}" : $"{definition.titleVi} — sắp ra mắt";
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (definition == null) return;
            if (!definition.playable)
            {
                NihongoLife.Dialogue.DialogueManager.Instance?.StartConversation(new[]
                {
                    new ScenarioNode
                    {
                        id = "soon", nodeType = ScenarioNodeType.Dialogue, speakerName = definition.titleJa + " · " + definition.titleVi,
                        textJa = "じゅんびちゅう です。またね！", textReading = "じゅんびちゅう です。またね！",
                        textEn = "Máy này đang được lắp đặt — quay lại sau nhé! Hãy thử Kana Match.",
                        choices = new System.Collections.Generic.List<DialogueChoice>()
                    }
                }, "soon", null);
                return;
            }
            MiniGameController.GetOrCreate().Launch(this, player);
        }
    }
}
