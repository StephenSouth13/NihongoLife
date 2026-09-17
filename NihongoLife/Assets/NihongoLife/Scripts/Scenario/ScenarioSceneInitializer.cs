using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Scenario
{
    public class ScenarioSceneInitializer : MonoBehaviour
    {
        private const string PendingLaunchKey = "NihongoLife.PendingScenarioLaunch";
        private const string ScenarioIdKey = "ActiveScenarioId";

        [Header("Config")]
        [SerializeField] private string scenarioId = "scenario.street.first_talk";
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
            {
                if (PlayerPrefs.GetInt(PendingLaunchKey, 0) == 1)
                {
                    scenarioId = PlayerPrefs.GetString(ScenarioIdKey, scenarioId);
                }

                ClearPendingLaunch();
                TriggerScenarioStart();
            }
        }

        public static void QueueLaunch(string requestedScenarioId)
        {
            if (string.IsNullOrWhiteSpace(requestedScenarioId)) return;

            PlayerPrefs.SetString(ScenarioIdKey, requestedScenarioId);
            PlayerPrefs.SetInt(PendingLaunchKey, 1);
            PlayerPrefs.Save();
        }

        private static void ClearPendingLaunch()
        {
            PlayerPrefs.DeleteKey(PendingLaunchKey);
            PlayerPrefs.DeleteKey(ScenarioIdKey);
            PlayerPrefs.Save();
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
