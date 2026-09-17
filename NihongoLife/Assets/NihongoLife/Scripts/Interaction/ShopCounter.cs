using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Scenario;
using NihongoLife.UI;

namespace NihongoLife.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class ShopCounter : MonoBehaviour, IInteractable, IConditionalInteractable
    {
        [SerializeField] private List<InteractiveItem> stockItems = new List<InteractiveItem>();
        [SerializeField] private string blockedScenarioId = "scenario.konbini.buy_onigiri";
        [SerializeField] private string promptJa = "買い物をする";
        [SerializeField] private string promptEn = "Mua hàng tự do";

        public bool IsInteractionAvailable
        {
            get
            {
                var scenario = ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentScenario : null;
                return scenario == null || scenario.id != blockedScenarioId;
            }
        }

        public string GetPromptJa() => promptJa;
        public string GetpromptEn() => promptEn;
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (!IsInteractionAvailable || player == null) return;

            var availableItems = new List<InteractiveItem>();
            foreach (var item in stockItems)
            {
                if (item != null && item.gameObject.activeInHierarchy)
                {
                    availableItems.Add(item);
                }
            }

            ShopUI.GetOrCreate().Show(availableItems, player);
        }
    }
}
