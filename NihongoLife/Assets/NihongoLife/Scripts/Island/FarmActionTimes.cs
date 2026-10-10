namespace NihongoLife.Island
{
    /// <summary>
    /// How long farm work takes, by tool. Without a hoe the soil is dug by hand (slow, never a dead end);
    /// watering needs the can (there is nothing to carry water in otherwise).
    /// </summary>
    public static class FarmActionTimes
    {
        public const float TillHoe = 1.6f, TillShovel = 2.6f, TillHand = 7f;
        public const float ClearShovel = 1.8f, ClearHoe = 2.5f, ClearHand = 6f;
        public const float Plant = 1.2f, Water = 1.5f, Harvest = 1.4f, Feed = 1f;

        /// <summary>(tool item id or null for bare hands, tool name, seconds).</summary>
        public static (string toolId, string toolName, float seconds) Till()
        {
            var catalog = IslandCatalog.Load();
            if (IslandEconomy.Owned("tool_hoe") > 0) return ("tool_hoe", IslandLanguage.Primary(catalog.Tool("tool_hoe")?.word), TillHoe);
            if (IslandEconomy.Owned("tool_shovel") > 0) return ("tool_shovel", IslandLanguage.Primary(catalog.Tool("tool_shovel")?.word), TillShovel);
            return (null, "tay không", TillHand);
        }

        public static (string toolId, string toolName, float seconds) Clear()
        {
            var catalog = IslandCatalog.Load();
            if (IslandEconomy.Owned("tool_shovel") > 0) return ("tool_shovel", IslandLanguage.Primary(catalog.Tool("tool_shovel")?.word), ClearShovel);
            if (IslandEconomy.Owned("tool_hoe") > 0) return ("tool_hoe", IslandLanguage.Primary(catalog.Tool("tool_hoe")?.word), ClearHoe);
            return (null, "tay không", ClearHand);
        }
    }
}
