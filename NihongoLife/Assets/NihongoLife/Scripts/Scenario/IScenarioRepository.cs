using System.Collections.Generic;
using NihongoLife.Core;

namespace NihongoLife.Scenario
{
    public interface IScenarioRepository : IGameService
    {
        ScenarioDefinition GetScenarioById(string id);
        List<ScenarioDefinition> GetAllScenarios();
    }
}
