using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Scenario
{
    public class ScenarioSceneInitializer : MonoBehaviour
    {
        private const string PendingLaunchKey = "NihongoLife.PendingScenarioLaunch";
        private const string ScenarioIdKey = "ActiveScenarioId";

        [Header("Config")]
        [SerializeField] private string scenarioId = "scenario.intro.arrival";
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (!runOnStart) return;
            string requested = PlayerPrefs.GetInt(PendingLaunchKey, 0) == 1 ? PlayerPrefs.GetString(ScenarioIdKey, null) : null;
            ClearPendingLaunch();

            // An explicit launch (co-op lobby practising together, tests) always wins, even for a finished quest.
            ScenarioDefinition pick = null;
            if (!string.IsNullOrEmpty(requested) && GameServices.TryGet(out IScenarioRepository repo)) pick = repo.GetScenarioById(requested);
            // Otherwise the story decides: resume an interrupted quest, or the next prologue step; free play starts nothing.
            if (pick == null && StoryDirector.Campaign.Count > 0) pick = StoryDirector.AutoStart();
            else if (pick == null && !StoryDirector.Bypass) { TriggerScenarioStart(); return; } // no campaign configured: the scene's own scenario
            if (pick != null && ScenarioManager.Instance != null && ScenarioManager.Instance.CurrentScenario?.id != pick.id)
                ScenarioManager.Instance.StartScenario(pick);
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
