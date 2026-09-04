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
}
