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
        public string displayNameVi;
        public int priceYen;
        public int quantity;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private int startingYen = 1200;

        private readonly List<InventoryEntry> _items = new List<InventoryEntry>();

        public event Action OnInventoryChanged;

        public int Yen { get; private set; }
        public IReadOnlyList<InventoryEntry> Items => _items;

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

        public void AddItem(string itemId, string displayNameJa, string displayNameVi, int priceYen, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0) return;

            var existing = _items.Find(item => item.itemId == itemId);
            if (existing == null)
            {
                _items.Add(new InventoryEntry
                {
                    itemId = itemId,
                    displayNameJa = displayNameJa,
                    displayNameVi = displayNameVi,
                    priceYen = Mathf.Max(0, priceYen),
                    quantity = quantity
                });
            }
            else
            {
                existing.quantity += quantity;
            }

            OnInventoryChanged?.Invoke();
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
