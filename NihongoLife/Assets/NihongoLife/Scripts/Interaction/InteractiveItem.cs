using UnityEngine;
using NihongoLife.Player;
using NihongoLife.Scenario;

namespace NihongoLife.Interaction
{
    public class InteractiveItem : MonoBehaviour, IInteractable
    {
        [Header("Item Config")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayNameJa;
        [SerializeField] private string displayNameEn;
        [SerializeField] private string promptJa = "調べる";
        [SerializeField] private string promptEn = "Kiểm tra";
        [SerializeField] private int priceYen;
        [SerializeField] private bool addToInventory = true;
        [SerializeField] private bool destroyOnInteract = true;

        public string ItemId => itemId;
        public int PriceYen => priceYen;

        public string GetPromptJa() => promptJa;
        public string GetpromptEn() => promptEn;
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            Debug.Log($"[InteractiveItem] Interacted with: {itemId}");

            bool acceptedByScenario = true;
            if (ScenarioManager.Instance != null)
            {
                acceptedByScenario = ScenarioManager.Instance.OnItemInteracted(itemId, this);
            }

            if (!acceptedByScenario)
            {
                return;
            }

            if (addToInventory && PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(itemId, displayNameJa, displayNameEn, priceYen);
            }

            if (destroyOnInteract)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
