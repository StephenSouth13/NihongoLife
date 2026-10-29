using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Player;
using NihongoLife.Core;
using NihongoLife.Save;

namespace NihongoLife.Home
{
    /// <summary>
    /// G2: the bedroom chest. Whole stacks move between the bag and the chest with every detail kept (name, price, use type, effects);
    /// the contents are saved with the player progress (homeStorage).
    /// </summary>
    public class StorageChest : MonoBehaviour
    {
        public const int MaxSlots = 40;
        public static StorageChest Instance { get; private set; }
        /// <summary>True while a chest exists in the loaded scene (the bedroom): the bag can then store things in it.</summary>
        public static bool Reachable => Instance != null;

        private readonly List<InventoryEntry> _storedItems = new List<InventoryEntry>();
        public IReadOnlyList<InventoryEntry> StoredItems => _storedItems;
        public event System.Action Changed;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void Start() { LoadStorage(); }

        public void LoadStorage()
        {
            if (GameServices.TryGet(out IProgressRepository repository))
            {
                var progress = repository.GetProgress();
                if (progress != null && progress.homeStorage != null)
                {
                    _storedItems.Clear();
                    _storedItems.AddRange(progress.homeStorage);
                    Changed?.Invoke();
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
                }
            }
        }

        private static InventoryEntry Copy(InventoryEntry e, int quantity) => new InventoryEntry
        {
            itemId = e.itemId, displayNameJa = e.displayNameJa, displayNameEn = e.displayNameEn, priceYen = e.priceYen, quantity = quantity,
            isLitter = e.isLitter, useType = e.useType, foodRestore = e.foodRestore, drinkRestore = e.drinkRestore, energyRestore = e.energyRestore,
        };

        /// <summary>Moves a whole bag stack into the chest. Returns false when the chest has no room.</summary>
        public static bool Store(PlayerInventory bag, int bagIndex)
        {
            var chest = Instance;
            if (chest == null || bag == null || bagIndex < 0 || bagIndex >= bag.Items.Count) return false;
            var entry = bag.Items[bagIndex];
            var existing = chest._storedItems.Find(x => x.itemId == entry.itemId);
            if (existing == null && chest._storedItems.Count >= MaxSlots) return false;
            int qty = entry.quantity;
            if (!bag.RemoveAt(bagIndex, qty)) return false;
            if (existing != null) existing.quantity += qty; else chest._storedItems.Add(Copy(entry, qty));
            chest.SaveStorage();
            chest.Changed?.Invoke();
            return true;
        }

        /// <summary>Moves a chest stack (or what fits) back into the bag.</summary>
        public bool Take(int chestIndex)
        {
            if (chestIndex < 0 || chestIndex >= _storedItems.Count || PlayerInventory.Instance == null) return false;
            var e = _storedItems[chestIndex];
            int before = PlayerInventory.Instance.GetItemQuantity(e.itemId);
            if (!PlayerInventory.Instance.AddItem(e.itemId, e.displayNameJa, e.displayNameEn, e.priceYen, e.quantity, e.isLitter, e.useType, e.foodRestore, e.drinkRestore, e.energyRestore)) return false;
            int moved = PlayerInventory.Instance.GetItemQuantity(e.itemId) - before;
            e.quantity -= moved;
            if (e.quantity <= 0) _storedItems.RemoveAt(chestIndex);
            SaveStorage();
            Changed?.Invoke();
            return moved > 0;
        }

        public bool DepositItem(string itemId, int quantity = 1)
        {
            var bag = PlayerInventory.Instance;
            if (bag == null || !bag.HasItem(itemId, quantity)) return false;
            int index = -1;
            for (int i = 0; i < bag.Items.Count; i++) if (bag.Items[i].itemId == itemId) { index = i; break; }
            if (index < 0) return false;
            var entry = bag.Items[index];
            var existing = _storedItems.Find(x => x.itemId == itemId);
            if (existing == null && _storedItems.Count >= MaxSlots) return false;
            var copy = Copy(entry, quantity);
            if (!bag.RemoveAt(index, quantity)) return false;
            if (existing != null) existing.quantity += quantity; else _storedItems.Add(copy);
            SaveStorage();
            Changed?.Invoke();
            return true;
        }

        public bool WithdrawItem(string itemId, int quantity = 1)
        {
            int index = _storedItems.FindIndex(x => x.itemId == itemId);
            if (index < 0 || _storedItems[index].quantity < quantity) return false;
            var e = _storedItems[index];
            var bag = PlayerInventory.Instance;
            if (bag == null || !bag.AddItem(e.itemId, e.displayNameJa, e.displayNameEn, e.priceYen, quantity, e.isLitter, e.useType, e.foodRestore, e.drinkRestore, e.energyRestore)) return false;
            e.quantity -= quantity;
            if (e.quantity <= 0) _storedItems.RemoveAt(index);
            SaveStorage();
            Changed?.Invoke();
            return true;
        }
    }
}