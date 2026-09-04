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
    }
}
