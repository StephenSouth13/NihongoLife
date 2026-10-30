using System;
using System.Collections.Generic;

namespace NihongoLife.Home
{
    /// <summary>Fashion/wardrobe scaffold (no new scene): outfits bought in existing shops, one per slot equipped.
    /// Rules only; visuals hook into the character later.</summary>
    [Serializable]
    public sealed class WardrobeCatalog
    {
        public enum Slot { Top, Bottom, Shoes, Hat, Uniform }

        [Serializable]
        public sealed class Item { public string id; public Slot slot; public int price; public string nameKey; }

        public List<string> owned = new List<string>();
        public List<string> equipped = new List<string>();

        public static readonly Item[] Catalog =
        {
            new Item { id = "ao_dai", slot = Slot.Top, price = 450, nameKey = "wear.ao_dai" },
            new Item { id = "yukata", slot = Slot.Top, price = 600, nameKey = "wear.yukata" },
            new Item { id = "linen_shirt", slot = Slot.Top, price = 180, nameKey = "wear.linen_shirt" },
            new Item { id = "shorts", slot = Slot.Bottom, price = 120, nameKey = "wear.shorts" },
            new Item { id = "sandals", slot = Slot.Shoes, price = 90, nameKey = "wear.sandals" },
            new Item { id = "non_la", slot = Slot.Hat, price = 60, nameKey = "wear.non_la" },
            new Item { id = "server_uniform", slot = Slot.Uniform, price = 0, nameKey = "wear.server_uniform" },
        };

        public static Item Find(string id) => Array.Find(Catalog, i => i.id == id);
        public bool Owns(string id) => owned.Contains(id);

        /// <summary>Buy with the player's money; returns the price paid (0 if not bought).</summary>
        public int Buy(string id, int money)
        {
            var it = Find(id);
            if (it == null || Owns(id) || money < it.price) return 0;
            owned.Add(id);
            return it.price;
        }

        /// <summary>Equip an owned item, replacing whatever is in its slot. A uniform replaces top and bottom.</summary>
        public bool Equip(string id)
        {
            var it = Find(id);
            if (it == null || !Owns(id)) return false;
            equipped.RemoveAll(e => { var o = Find(e); return o != null && (o.slot == it.slot ||
                (it.slot == Slot.Uniform && (o.slot == Slot.Top || o.slot == Slot.Bottom)) ||
                (o.slot == Slot.Uniform && (it.slot == Slot.Top || it.slot == Slot.Bottom))); });
            equipped.Add(id);
            return true;
        }
    }
}
