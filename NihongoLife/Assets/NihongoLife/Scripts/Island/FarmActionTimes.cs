namespace NihongoLife.Island
{
    /// <summary>
    /// How long farm work takes, by tool. Tilling needs a hoe (or, slower, a shovel), clearing a plot a shovel (or a hoe),
    /// watering a watering can — there is no bare-hand way. Planting, harvesting and feeding are done by hand.
    /// A tool counts only while it is usable (IslandTools: worn-out tools break).
    /// </summary>
    public static class FarmActionTimes
    {
        public const float TillHoe = 1.6f, TillShovel = 2.6f;
        public const float ClearShovel = 1.8f, ClearHoe = 2.5f;
        public const float Plant = 1.2f, Water = 1.5f, Harvest = 1.4f, Feed = 1f;

        /// <summary>(tool item id, tool name, seconds); tool id null = no usable tool, the work cannot be done.</summary>
        public static (string toolId, string toolName, float seconds) Till()
        {
            string id = IslandTools.FirstUsable("tool_hoe", "tool_shovel");
            return id == null ? (null, null, 0f) : (id, Name(id), id == "tool_hoe" ? TillHoe : TillShovel);
        }

        public static (string toolId, string toolName, float seconds) Clear()
        {
            string id = IslandTools.FirstUsable("tool_shovel", "tool_hoe");
            return id == null ? (null, null, 0f) : (id, Name(id), id == "tool_shovel" ? ClearShovel : ClearHoe);
        }

        private static string Name(string toolId) => IslandLanguage.Primary(IslandCatalog.Load().Tool(toolId)?.word);
    }
}
