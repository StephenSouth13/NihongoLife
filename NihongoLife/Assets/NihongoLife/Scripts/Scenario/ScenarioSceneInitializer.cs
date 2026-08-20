using UnityEngine;

namespace NihongoLife.Scenario
{
    public class ScenarioSceneInitializer : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string scenarioId = "scenario.konbini.buy_onigiri";
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
            {
                TriggerScenarioStart();
            }
        }

        public void TriggerScenarioStart()
        {
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.StartScenario(scenarioId);
            }
            else
            {
                Debug.LogError("[ScenarioSceneInitializer] ScenarioManager.Instance is null! Cannot launch scenario.");
            }
        }
    }
}
