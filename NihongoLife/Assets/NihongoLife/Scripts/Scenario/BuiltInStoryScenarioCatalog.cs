using System.Collections.Generic;

namespace NihongoLife.Scenario
{
    public static class BuiltInStoryScenarioCatalog
    {
        /// <summary>
        /// Superseded (2026-09-22): scenario.neighborhood.cat_followup, scenario.neighborhood.recycling_morning
        /// and scenario.konbini.evening_shift are full ScenarioDefinition assets under Resources/Scenarios (with
        /// Vietnamese, English and Japanese text). The old code-built copies were removed on 2026-10-20; see git
        /// history if they are needed. LocalScenarioRepository still calls this hook for code-built scenarios.
        /// </summary>
        public static IEnumerable<ScenarioDefinition> CreateAll()
        {
            yield break;
        }
    }
}
