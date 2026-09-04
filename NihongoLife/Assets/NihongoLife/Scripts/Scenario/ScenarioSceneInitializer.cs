using UnityEngine;
using NihongoLife.Core;

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
                // Load scenario ID dynamically if set by MainMenu
                scenarioId = PlayerPrefs.GetString("ActiveScenarioId", scenarioId);
                if (GameServices.TryGet(out GameControlService controlService))
                {
                    scenarioId = controlService.ActiveScenarioIdOrDefault(scenarioId);
                }
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
