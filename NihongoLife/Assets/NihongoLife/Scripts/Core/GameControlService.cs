using UnityEngine;

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
    }
}
