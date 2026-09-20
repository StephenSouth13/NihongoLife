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
                if (!PlayerInventory.Instance.AddItem(itemId, displayNameJa, displayNameEn, priceYen, 1, isLitter))
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
