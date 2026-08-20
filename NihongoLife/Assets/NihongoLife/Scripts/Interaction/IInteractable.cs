using UnityEngine;

namespace NihongoLife.Interaction
{
    public interface IInteractable
    {
        string GetPromptJa();
        string GetPromptVi();
        void Interact(GameObject player);
        Transform GetTransform();
    }
}
