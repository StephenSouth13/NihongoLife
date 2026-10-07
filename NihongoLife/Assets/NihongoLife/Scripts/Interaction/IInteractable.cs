using UnityEngine;

namespace NihongoLife.Interaction
{
    public interface IInteractable
    {
        string GetPromptJa();
        string GetpromptEn();
        void Interact(GameObject player);
        Transform GetTransform();
    }

    /// <summary>Optional bonus (in metres) when several interactables are in reach; doors use it.</summary>
    public interface IInteractionPriority
    {
        float InteractionPriority { get; }
    }

    public interface IConditionalInteractable
    {
        bool IsInteractionAvailable { get; }
    }
}
