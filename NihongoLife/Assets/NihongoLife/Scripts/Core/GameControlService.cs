using UnityEngine;
using System.Collections.Generic;
using NihongoLife.Scenario;

namespace NihongoLife.Core
{
    public class GameControlService : MonoBehaviour, IGameService
    {
        private const string ControlDatabasePath = "Control/NihongoLifeControlDatabase";

        [SerializeField] private GameControlDatabase database;

        public GameControlDatabase Database => database;

        public void Initialize()
        {
            if (database == null)
            {
                database = Resources.Load<GameControlDatabase>(ControlDatabasePath);
            }
        }

        public string ActiveScenarioIdOrDefault(string fallback)
        {
            return database != null && !string.IsNullOrEmpty(database.activeScenarioId)
                ? database.activeScenarioId
                : fallback;
        }

        public IReadOnlyList<string> GetCampaignScenarioIds(IScenarioRepository repository = null)
        {
            var orderedIds = new List<string>();
            var seen = new HashSet<string>();

            if (database != null && database.campaignScenarioIds != null)
            {
                foreach (string scenarioId in database.campaignScenarioIds)
                {
                    AddScenarioId(scenarioId, orderedIds, seen);
                }
            }

            if (database != null && database.scenarios != null)
            {
                foreach (ScenarioDefinition scenario in database.scenarios)
                {
                    AddScenarioId(scenario != null ? scenario.id : null, orderedIds, seen);
                }
            }

            if (repository != null)
            {
                var scenarios = repository.GetAllScenarios();
                scenarios.RemoveAll(s => s == null || string.IsNullOrEmpty(s.id));
                scenarios.Sort((a, b) =>
                {
                    int chapter = a.chapterIndex.CompareTo(b.chapterIndex);
                    return chapter != 0 ? chapter : string.CompareOrdinal(a.id, b.id);
                });

                foreach (ScenarioDefinition scenario in scenarios)
                {
                    AddScenarioId(scenario.id, orderedIds, seen);
                }
            }

            return orderedIds;
        }

        public string FindNextCampaignScenarioId(string currentScenarioId, IScenarioRepository repository = null)
        {
            if (string.IsNullOrEmpty(currentScenarioId)) return string.Empty;

            IReadOnlyList<string> ids = GetCampaignScenarioIds(repository);
            for (int i = 0; i < ids.Count - 1; i++)
            {
                if (ids[i] == currentScenarioId)
                {
                    return ids[i + 1];
                }
            }

            return string.Empty;
        }

        public bool ShouldContinueCampaignInGameplayScene()
        {
            return database == null || database.continueCampaignInGameplayScene;
        }

        public AudioClip FindVoiceClip(string nodeId, GameLanguage language)
        {
            VoiceLineEntry entry = FindVoiceLine(nodeId, language);
            return entry != null ? entry.clip : null;
        }

        public VoiceLineEntry FindVoiceLine(string nodeId, GameLanguage language)
        {
            if (database == null || database.voiceLines == null || string.IsNullOrEmpty(nodeId)) return null;

            var exact = database.voiceLines.Find(v => v != null && v.nodeId == nodeId && v.language == language && v.clip != null);
            if (exact != null) return exact;

            exact = database.voiceLines.Find(v => v != null && v.nodeId == nodeId && v.language == language && !string.IsNullOrWhiteSpace(v.remoteUrl));
            if (exact != null) return exact;

            var japaneseFallback = database.voiceLines.Find(v => v != null && v.nodeId == nodeId && v.language == GameLanguage.Japanese && (v.clip != null || !string.IsNullOrWhiteSpace(v.remoteUrl)));
            return japaneseFallback;
        }

        public string FindEnglishIpa(string nodeId)
        {
            if (database == null || database.voiceLines == null || string.IsNullOrEmpty(nodeId)) return string.Empty;
            var entry = database.voiceLines.Find(v => v != null && v.nodeId == nodeId && !string.IsNullOrWhiteSpace(v.englishIpa));
            return entry != null ? entry.englishIpa : string.Empty;
        }

        private static void AddScenarioId(string scenarioId, List<string> orderedIds, HashSet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(scenarioId) || seen.Contains(scenarioId)) return;
            seen.Add(scenarioId);
            orderedIds.Add(scenarioId);
        }
    }
}
