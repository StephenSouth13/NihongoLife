using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Player;
using NihongoLife.Core;
using NihongoLife.Save;

namespace NihongoLife.Home
{
    public class StorageChest : MonoBehaviour
    {
        private List<InventoryEntry> _storedItems = new List<InventoryEntry>();

        public IReadOnlyList<InventoryEntry> StoredItems => _storedItems;

        private void Start()
        {
            LoadStorage();
        }

        private void LoadStorage()
        {
            if (GameServices.TryGet(out IProgressRepository repository))
            {
                var progress = repository.GetProgress();
                if (progress != null && progress.homeStorage != null)
                {
                    _storedItems.Clear();
                    _storedItems.AddRange(progress.homeStorage);
                }
            }
        }

        public void SaveStorage()
        {
            if (GameServices.TryGet(out IProgressRepository repository))
            {
                var progress = repository.GetProgress();
                if (progress != null)
                {
                    progress.homeStorage = new List<InventoryEntry>(_storedItems);
                    repository.SaveProgress(progress);
                    Debug.Log("[StorageChest] Saved items to cloud.");
                }
            }
        }

        public bool DepositItem(string itemId, int quantity = 1)
        {
            if (PlayerInventory.Instance == null || !PlayerInventory.Instance.HasItem(itemId, quantity))
                return false;

            // Remove from player
            PlayerInventory.Instance.RemoveItem(itemId, quantity);

            // Add to chest
            var existing = _storedItems.Find(x => x.itemId == itemId);
            if (existing != null)
            {
                existing.quantity += quantity;
            }
            else
            {
                // We should ideally copy the item details, but for simplicity we rely on the catalog
                // In a full implementation, pass the whole InventoryEntry from the player
                _storedItems.Add(new InventoryEntry { itemId = itemId, quantity = quantity });
            }

            SaveStorage();
            return true;
        }

        public bool WithdrawItem(string itemId, int quantity = 1)
        {
            var existing = _storedItems.Find(x => x.itemId == itemId);
            if (existing == null || existing.quantity < quantity)
                return false;

            if (PlayerInventory.Instance == null || PlayerInventory.Instance.IsFull)
            {
                Debug.LogWarning("[StorageChest] Player inventory is full!");
                return false;
            }

            existing.quantity -= quantity;
            if (existing.quantity <= 0)
                _storedItems.Remove(existing);

            // Re-add to player inventory (requires full details in real game, using placeholder here)
            PlayerInventory.Instance.AddItem(itemId, itemId, itemId, 0, quantity, false, ItemUseType.None);
            
            SaveStorage();
            return true;
        }
    }
}
