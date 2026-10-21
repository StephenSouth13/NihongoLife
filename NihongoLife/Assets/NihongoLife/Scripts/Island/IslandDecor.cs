using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>
    /// The decoration stock and layout of the player's island, inside the island record of the shared save:
    /// buying adds to the stock (it does not take bag slots), placing moves one piece from the stock onto the island,
    /// storing puts it back. Every decoration is a vocabulary word ("decor:&lt;id&gt;"), learned when bought.
    /// </summary>
    public static class IslandDecor
    {
        public const int MaxPieces = 80;

        public static IReadOnlyList<DecorPlacement> Placed => IslandState.Record.decor;

        public static int Stock(string id) => IslandState.Record.decorStock.Find(s => s.id == id)?.count ?? 0;

        public static IEnumerable<(IslandDecorDef def, int count)> StockList()
        {
            var catalog = IslandCatalog.Load();
            foreach (var s in IslandState.Record.decorStock.Where(s => s.count > 0))
            {
                var def = catalog.Decor(s.id);
                if (def != null) yield return (def, s.count);
            }
        }

        private static void AddStock(string id, int delta)
        {
            var record = IslandState.Record;
            var entry = record.decorStock.Find(s => s.id == id);
            if (entry == null) { entry = new DecorStock { id = id }; record.decorStock.Add(entry); }
            entry.count = Mathf.Max(0, entry.count + delta);
            if (entry.count == 0) record.decorStock.Remove(entry);
        }

        /// <summary>Buys decorations with yen into the island stock.</summary>
        public static IslandEconomy.Result Buy(string id, int quantity)
        {
            var def = IslandCatalog.Load().Decor(id);
            var inventory = PlayerInventory.Instance;
            if (def == null || inventory == null || quantity <= 0) return IslandEconomy.Result.Unknown;
            if (IslandVisit.Active) return IslandEconomy.Result.Unknown;
            int total = def.price * quantity;
            if (!inventory.SpendYen(total, Loc.Triple("Đồ trang trí đảo", "Island decoration", "島のかざり"))) return IslandEconomy.Result.NoMoney;
            AddStock(def.id, quantity);
            IslandState.Discover("decor:" + def.id);
            NihongoLife.Progression.QuestService.Raise("buy", def.ItemId, quantity);
            NihongoLife.Progression.QuestService.Raise("buy", "midori_store", quantity);
            IslandState.Save();
            NihongoLife.Audio.Soundscape.Jingle("purchase");
            return IslandEconomy.Result.Ok;
        }

        /// <summary>Places one piece from the stock at island-local (x, z). Returns the new placement, or null.</summary>
        public static DecorPlacement Place(string id, float x, float z, float yaw)
        {
            if (IslandVisit.Active || Stock(id) <= 0 || Placed.Count >= MaxPieces) return null;
            var placement = new DecorPlacement { id = id, x = x, z = z, yaw = Mathf.Repeat(yaw, 360f) };
            AddStock(id, -1);
            IslandState.Record.decor.Add(placement);
            IslandState.Save();
            return placement;
        }

        public static void Move(DecorPlacement placement, float x, float z, float yaw)
        {
            if (placement == null || IslandVisit.Active) return;
            placement.x = x; placement.z = z; placement.yaw = Mathf.Repeat(yaw, 360f);
            IslandState.Save();
        }

        /// <summary>Takes a placed piece back into the stock.</summary>
        public static void Store(DecorPlacement placement)
        {
            if (placement == null || IslandVisit.Active || !IslandState.Record.decor.Remove(placement)) return;
            AddStock(placement.id, 1);
            IslandState.Save();
        }
    }
}
