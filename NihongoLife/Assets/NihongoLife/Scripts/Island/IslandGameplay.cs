using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>
    /// Buying, selling and starter items on the island. Uses the one existing wallet and bag (PlayerInventory):
    /// there is no separate farm currency. Every operation is synchronous and checks the bag before taking money,
    /// so repeated clicks cannot double-charge or duplicate items.
    /// </summary>
    public static class IslandEconomy
    {
        public enum Result { Ok, NoMoney, BagFull, NotOwned, Unknown }

        public static int Owned(string itemId) => PlayerInventory.Instance != null ? PlayerInventory.Instance.GetItemQuantity(itemId) : 0;

        public static int Yen => PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;

        public static bool CanStore(string itemId)
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return false;
            return inventory.HasItem(itemId) || inventory.Items.Count < inventory.MaxSlots;
        }

        public static bool Give(string itemId, int quantity)
        {
            var inventory = PlayerInventory.Instance;
            var catalog = IslandCatalog.Load();
            if (inventory == null || !catalog.TryDescribe(itemId, out string ja, out string vi, out int price)) return false;
            return inventory.AddItem(itemId, ja, vi, price, quantity);
        }

        public static Result Buy(string itemId, int unitPrice, int quantity)
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null || quantity <= 0) return Result.Unknown;
            if (!CanStore(itemId)) return Result.BagFull;
            int total = unitPrice * quantity;
            if (!inventory.SpendYen(total)) return Result.NoMoney;
            if (!Give(itemId, quantity)) { inventory.AddYen(total); return Result.BagFull; }
            NihongoLife.Progression.QuestService.Raise("buy", itemId, quantity);
            NihongoLife.Progression.QuestService.Raise("buy", "midori_store", quantity);
            return Result.Ok;
        }

        public static Result Sell(string itemId, int unitPrice, int quantity)
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null || quantity <= 0) return Result.Unknown;
            if (inventory.GetItemQuantity(itemId) < quantity) return Result.NotOwned;
            if (!inventory.RemoveItem(itemId, quantity)) return Result.NotOwned;
            inventory.AddYen(unitPrice * quantity);
            var record = IslandState.Record;
            record.sold += quantity;
            record.earned += unitPrice * quantity;
            IslandState.Save();
            if (record.sold >= 1) IslandAchievements.Check();
            NihongoLife.Progression.QuestService.Raise("sell", itemId, quantity);
            return Result.Ok;
        }

        /// <summary>First arrival: a hoe, a watering can and three carrot seeds (data-driven, given once).</summary>
        public static bool GiveStarterKit()
        {
            var record = IslandState.Record;
            if (record.starterKitGiven) return false;
            var catalog = IslandCatalog.Load();
            foreach (var item in catalog.starterKit ?? Array.Empty<IslandKitItem>())
                if (Owned(item.itemId) == 0 || item.itemId.StartsWith("seed_")) Give(item.itemId, item.quantity);
            record.starterKitGiven = true;
            IslandState.Save();
            return true;
        }
    }

    public static class IslandAchievements
    {
        public sealed class Def { public string Id, Vi, Ja; public Func<IslandRecord, bool> Done; }

        public static readonly Def[] All =
        {
            new Def { Id = "first_harvest", Ja = "はじめての しゅうかく", Vi = "Lần thu hoạch đầu tiên", Done = r => r.harvested >= 1 },
            new Def { Id = "farmer", Ja = "のうか デビュー", Vi = "Thu hoạch 10 lần", Done = r => r.harvested >= 10 },
            new Def { Id = "first_fish", Ja = "はじめての つり", Vi = "Câu được con cá đầu tiên", Done = r => r.fished >= 1 },
            new Def { Id = "first_sale", Ja = "はじめての うりあげ", Vi = "Bán nông sản lần đầu", Done = r => r.sold >= 1 },
            new Def { Id = "animal_friend", Ja = "どうぶつの ともだち", Vi = "Làm quen cả 5 con vật", Done = r => r.animalsMet.Count >= 5 },
            new Def { Id = "word_collector", Ja = "ことば あつめ", Vi = "Học 15 từ trên đảo", Done = r => r.words.Count >= 15 },
        };

        public static event Action<Def> Unlocked;

        public static void Check()
        {
            var record = IslandState.Record;
            foreach (var def in All)
                if (def.Done(record) && IslandState.Unlock(def.Id)) Unlocked?.Invoke(def);
        }
    }

    // ─────────── Farm plot ───────────
}
