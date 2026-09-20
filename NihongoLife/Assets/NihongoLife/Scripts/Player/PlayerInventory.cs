using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Player
{
    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public string displayNameJa;
        public string displayNameEn;
        public int priceYen;
        public int quantity;
        public bool isLitter;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private int startingYen = 1200;
        [SerializeField, Min(1)] private int maxSlots = 16;
        [SerializeField, Min(1)] private int maxStackSize = 20;

        private readonly List<InventoryEntry> _items = new List<InventoryEntry>();

        public event Action OnInventoryChanged;

        public int Yen { get; private set; }
        public IReadOnlyList<InventoryEntry> Items => _items;
        public int MaxSlots => maxSlots;
        public int UsedSlots => _items.Count;
        public int MaxStackSize => maxStackSize;
        public bool IsFull => UsedSlots >= maxSlots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Yen = startingYen;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool AddItem(string itemId, string displayNameJa, string displayNameEn, int priceYen, int quantity = 1, bool isLitter = false)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0) return false;

            var existing = _items.Find(item => item.itemId == itemId);
            if (existing == null)
            {
                if (_items.Count >= maxSlots) return false;
                _items.Add(new InventoryEntry
                {
                    itemId = itemId,
                    displayNameJa = displayNameJa,
                    displayNameEn = displayNameEn,
                    priceYen = Mathf.Max(0, priceYen),
                    quantity = Mathf.Min(quantity, maxStackSize),
                    isLitter = isLitter
                });
            }
            else
            {
                if (existing.quantity >= maxStackSize) return false;
                existing.quantity = Mathf.Min(existing.quantity + quantity, maxStackSize);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            var item = _items.Find(entry => entry.itemId == itemId);
            return item != null && item.quantity >= quantity;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var item = _items.Find(entry => entry.itemId == itemId);
            if (item == null || item.quantity < quantity) return false;

            item.quantity -= quantity;
            if (item.quantity == 0)
            {
                _items.Remove(item);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool SpendYen(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (Yen < amount) return false;

            Yen -= amount;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void AddYen(int amount)
        {
            if (amount <= 0) return;
            Yen += amount;
            OnInventoryChanged?.Invoke();
        }

        public void ApplyFine(int amount)
        {
            Yen = Mathf.Max(0, Yen - Mathf.Max(0, amount));
            OnInventoryChanged?.Invoke();
        }

        public bool TryGetLastItem(out InventoryEntry entry)
        {
            entry = _items.Count > 0 ? _items[_items.Count - 1] : null;
            return entry != null;
        }

        public int GetItemQuantity(string itemId)
        {
            var item = _items.Find(entry => entry.itemId == itemId);
            return item != null ? item.quantity : 0;
        }

        public int GetCartTotalYen()
        {
            int total = 0;
            foreach (var item in _items)
            {
                total += item.priceYen * item.quantity;
            }

            return total;
        }
    }
}
