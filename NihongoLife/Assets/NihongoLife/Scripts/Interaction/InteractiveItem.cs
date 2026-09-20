using UnityEngine;
using System.Collections.Generic;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.Audio;
using NihongoLife.Core;

namespace NihongoLife.Interaction
{
    public class InteractiveItem : MonoBehaviour, IInteractable
    {
        private static readonly Dictionary<string, InteractiveItem> Templates = new();
        [Header("Item Config")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayNameJa;
        [SerializeField] private string displayNameReading;
        [SerializeField] private string displayNameEn;
        [SerializeField] private string promptJa = "調べる";
        [SerializeField] private string promptEn = "Kiểm tra";
        [SerializeField] private int priceYen;
        [SerializeField] private bool addToInventory = true;
        [SerializeField] private bool destroyOnInteract = true;
        [SerializeField] private bool isLitter;

        private void Awake()
        {
            if (!string.IsNullOrWhiteSpace(itemId) && !Templates.ContainsKey(itemId)) Templates[itemId] = this;
        }

        public string ItemId => itemId;
        public string DisplayNameJa => displayNameJa;
        public string DisplayNameReading => displayNameReading;
        public string DisplayNameEn => displayNameEn;
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
                ConsumableCatalog.Resolve(itemId, out ItemUseType useType, out float food, out float drink, out float energy);
                if (!PlayerInventory.Instance.AddItem(itemId, displayNameJa, displayNameEn, priceYen, 1, isLitter,
                        useType, food, drink, energy))
                {
                    Debug.LogWarning($"[InteractiveItem] Inventory is full. Item '{itemId}' remains in the world.", this);
                    return;
                }
            }

            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.Pickup, 0.75f);

            if (destroyOnInteract)
            {
                gameObject.SetActive(false);
            }
        }

        private static class ConsumableCatalog
        {
            public static void Resolve(string id, out ItemUseType type, out float food, out float drink, out float energy)
            {
                type = ItemUseType.None;
                food = drink = energy = 0f;
                string key = (id ?? string.Empty).ToLowerInvariant();
                if (key.Contains("water") || key.Contains("tea") || key.Contains("drink") || key.Contains("soda"))
                {
                    type = ItemUseType.Drink;
                    drink = key.Contains("water") ? 38f : 28f;
                    energy = key.Contains("tea") ? 8f : 3f;
                    return;
                }

                if (key.Contains("onigiri") || key.Contains("rice") || key.Contains("ramen") || key.Contains("udon") ||
                    key.Contains("sushi") || key.Contains("nigiri") || key.Contains("roll") || key.Contains("dango") || key.Contains("food"))
                {
                    type = ItemUseType.Food;
                    food = key.Contains("ramen") || key.Contains("udon") ? 45f : 28f;
                    energy = 10f;
                }
            }
        }

        public static bool TrySpawnDropped(InventoryEntry entry, Vector3 position, Quaternion rotation)
        {
            if (entry == null || !Templates.TryGetValue(entry.itemId, out InteractiveItem template) || template == null)
            {
                Debug.LogWarning($"[InteractiveItem] No world prefab/template registered for '{entry?.itemId}'. Drop cancelled.");
                return false;
            }

            var clone = Instantiate(template.gameObject, position, rotation);
            clone.name = $"Dropped_{entry.itemId}";
            clone.SetActive(true);
            return true;
        }
    }
}
