using System.Linq;
using NihongoLife.UI;

namespace NihongoLife.Island
{
    /// <summary>
    /// Tool rules on the island: work that needs a tool cannot be done without it (no bare-hand fallback), and every
    /// finished use wears the tool in use by one. At zero it breaks — one is taken out of the bag and a spare (if any)
    /// starts fresh. Durability comes from island_catalog.json; uses left are saved in IslandRecord.toolWear.
    /// </summary>
    public static class IslandTools
    {
        public static int Max(string toolId) => IslandCatalog.Load().Tool(toolId)?.durability ?? 0;

        /// <summary>Uses left on the tool in use (0 when none is owned).</summary>
        public static int Left(string toolId)
        {
            if (IslandEconomy.Owned(toolId) <= 0) return 0;
            int max = Max(toolId);
            if (max <= 0) return int.MaxValue;
            var wear = IslandState.Record.toolWear.FirstOrDefault(w => w.itemId == toolId);
            return wear == null ? max : UnityEngine.Mathf.Clamp(wear.usesLeft, 0, max);
        }

        public static bool Usable(string toolId) => Left(toolId) > 0;

        /// <summary>The first usable tool of the list (preference order), or null.</summary>
        public static string FirstUsable(params string[] toolIds) => toolIds.FirstOrDefault(Usable);

        /// <summary>"bền 38/40" for labels (empty for tools that never wear).</summary>
        public static string Label(string toolId)
        {
            int max = Max(toolId);
            return max <= 0 || IslandEconomy.Owned(toolId) <= 0 ? "" : $"bền {Left(toolId)}/{max}";
        }

        /// <summary>One finished use. Returns a message when the tool broke, otherwise null.</summary>
        public static string Wear(string toolId)
        {
            int max = Max(toolId);
            if (max <= 0 || IslandEconomy.Owned(toolId) <= 0) return null;
            var record = IslandState.Record;
            var wear = record.toolWear.FirstOrDefault(w => w.itemId == toolId);
            if (wear == null) { wear = new ToolWear { itemId = toolId, usesLeft = max }; record.toolWear.Add(wear); }
            wear.usesLeft = UnityEngine.Mathf.Min(wear.usesLeft, max) - 1;
            string message = null;
            if (wear.usesLeft <= 0)
            {
                var tool = IslandCatalog.Load().Tool(toolId);
                NihongoLife.Player.PlayerInventory.Instance.RemoveItem(toolId, 1);
                wear.usesLeft = max; // a spare in the bag starts fresh
                bool spare = IslandEconomy.Owned(toolId) > 0;
                message = $"{IslandLanguage.Primary(tool?.word)} ({tool?.word.vi}) đã hỏng sau {max} lần dùng" +
                          (spare ? " — đang dùng cái dự phòng." : $" — mua cái mới ở cửa hàng Midori (¥{tool?.price}).");
                IslandUI.Toast(message, !spare);
                HudFeed.Post(message, HudFeed.Kind.Warning, 5f);
            }
            else if (wear.usesLeft == 5)
            {
                var tool = IslandCatalog.Load().Tool(toolId);
                HudFeed.Post($"{tool?.word.ja} sắp hỏng — còn 5 lần dùng.", HudFeed.Kind.Warning, 4f);
            }
            IslandState.Save();
            return message;
        }
    }
}
