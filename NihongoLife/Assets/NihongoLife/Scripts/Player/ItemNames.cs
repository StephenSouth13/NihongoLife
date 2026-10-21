using NihongoLife.Core;
using NihongoLife.Island;
using NihongoLife.Shop;

namespace NihongoLife.Player
{
    /// <summary>
    /// The name of a bag item in the interface language. Saved items keep the names they were given when picked up
    /// (often Vietnamese), so the name is looked up again from the item's catalog (Hibari Mart, Midori Island) and
    /// only falls back to the saved text — which may be a <see cref="Loc.Triple"/> — for items without one.
    /// </summary>
    public static class ItemNames
    {
        public static string Of(InventoryEntry entry)
        {
            if (entry == null) return "";
            string stored = entry.displayNameEn;
            if (Loc.IsTriple(stored)) return Loc.Resolve(stored);
            var product = KonbiniCatalog.Find(entry.itemId);
            if (product != null) return product.Name;
            string island = IslandCatalog.Load()?.LocalName(entry.itemId);
            if (!string.IsNullOrEmpty(island)) return island;
            string known = Known(entry.itemId);
            if (known != null) return known;
            if (Loc.IsJapanese && !string.IsNullOrEmpty(entry.displayNameJa)) return entry.displayNameJa;
            return string.IsNullOrEmpty(stored) ? entry.displayNameJa ?? entry.itemId : stored;
        }

        /// <summary>Items placed in scenes or given by stories, which have no catalog of their own.</summary>
        private static string Known(string itemId) => itemId switch
        {
            "onigiri" => Loc.T("Cơm nắm", "Rice ball", "おにぎり"),
            "tea" => Loc.T("Trà xanh", "Green tea", "お茶"),
            "water" => Loc.T("Nước", "Water", "水"),
            "train_ticket_minato" => Loc.T("Vé tàu", "Train ticket", "きっぷ"),
            "train_ticket_hibari" => Loc.T("Vé tàu về Hibari", "Train ticket to Hibari", "きっぷ（ひばり）"),
            _ => null,
        };
    }
}
