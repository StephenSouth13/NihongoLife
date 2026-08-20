using UnityEngine;
using NihongoLife.Scenario;

namespace NihongoLife.Interaction
{
    public class InteractiveItem : MonoBehaviour, IInteractable
    {
        [Header("Item Config")]
        [SerializeField] private string itemId;
        [SerializeField] private string promptJa = "調べる"; // Inspect
        [SerializeField] private string promptVi = "Kiểm tra";
        [SerializeField] private bool destroyOnInteract = true;

        public string ItemId => itemId;

        public string GetPromptJa() => promptJa;
        public string GetPromptVi() => promptVi;
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            Debug.Log($"[InteractiveItem] Interacted with: {itemId}");

            // Notify ScenarioManager about the interaction
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
            {
                scenarioManager.OnItemInteracted(itemId, this);
            }

            if (destroyOnInteract)
            {
                // Disable visuals and collision instead of destroying immediately,
                // in case other scripts need the reference or for clean cleanup.
                gameObject.SetActive(false);
            }
        }
    }
}
