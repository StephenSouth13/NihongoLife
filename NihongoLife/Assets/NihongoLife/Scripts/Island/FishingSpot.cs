using NihongoLife.Interaction;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>The end of the Midori pier: F (with a fishing rod in the bag) starts a fishing round.</summary>
    public sealed class FishingSpot : MonoBehaviour, IInteractable
    {
        [Tooltip("Where the float lands on the water.")] public Transform castTarget;

        public string GetPromptJa() => IslandLanguage.Primary(IslandCatalog.Load().places?.pier) is { Length: > 0 } p ? p : "つりば";
        public string GetpromptEn() => IslandEconomy.Owned(IslandFishing.RodId) > 0 ? "Câu cá" : "Câu cá (cần có cần câu)";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => IslandFishing.Begin(this, player);
    }
}
