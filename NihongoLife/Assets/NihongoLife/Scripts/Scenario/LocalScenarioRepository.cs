using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Scenario
{
    public class LocalScenarioRepository : IScenarioRepository
    {
        private Dictionary<string, ScenarioDefinition> _scenarios = new Dictionary<string, ScenarioDefinition>();

        public void Initialize()
        {
            LoadScenarios();
        }

        public ScenarioDefinition GetScenarioById(string id)
        {
            if (_scenarios.TryGetValue(id, out var scenario))
            {
                return scenario;
            }
            Debug.LogError($"[LocalScenarioRepository] Scenario with ID {id} not found.");
            return null;
        }

        public List<ScenarioDefinition> GetAllScenarios()
        {
            return new List<ScenarioDefinition>(_scenarios.Values);
        }

        private void LoadScenarios()
        {
            _scenarios.Clear();
            var loaded = Resources.LoadAll<ScenarioDefinition>("Scenarios");
            foreach (var scenario in loaded)
            {
                if (scenario != null && !string.IsNullOrEmpty(scenario.id))
                {
                    var validation = ScenarioValidator.Validate(scenario);
                    if (validation.HasErrors)
                    {
                        Debug.LogError($"[LocalScenarioRepository] Scenario '{scenario.id}' has validation errors:\n{validation.ToLogString()}");
                    }
                    else if (validation.Issues.Count > 0)
                    {
                        Debug.LogWarning($"[LocalScenarioRepository] Scenario '{scenario.id}' has validation warnings:\n{validation.ToLogString()}");
                    }

                    if (!_scenarios.ContainsKey(scenario.id))
                    {
                        _scenarios.Add(scenario.id, scenario);
                    }
                    else
                    {
                        Debug.LogWarning($"[LocalScenarioRepository] Duplicate Scenario ID: {scenario.id}");
                    }
                }
            }
            Debug.Log($"[LocalScenarioRepository] Loaded {_scenarios.Count} scenarios from Resources/Scenarios.");
        }
    }
}
