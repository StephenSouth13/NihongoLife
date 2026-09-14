using System;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Scenario
{
    public class ScenarioCampaignManager : MonoBehaviour, IGameService
    {
        private IScenarioRepository _repository;
        private GameControlService _controlService;
        private ScenarioManager _subscribedScenarioManager;

        public string CurrentScenarioId { get; private set; }
        public string NextScenarioId { get; private set; }
        public bool HasNextScenario => !string.IsNullOrEmpty(NextScenarioId);

        public event Action<string, string> OnCampaignStepChanged;

        public void Initialize()
        {
            GameServices.TryGet(out _repository);
            GameServices.TryGet(out _controlService);
            Refresh();
        }

        private void OnEnable()
        {
            EnsureScenarioSubscription();
        }

        private void OnDisable()
        {
            if (_subscribedScenarioManager != null)
            {
                _subscribedScenarioManager.OnScenarioStarted -= HandleScenarioStarted;
                _subscribedScenarioManager = null;
            }
        }

        private void Update()
        {
            EnsureScenarioSubscription();
        }

        private void EnsureScenarioSubscription()
        {
            if (_subscribedScenarioManager == ScenarioManager.Instance)
            {
                return;
            }

            if (_subscribedScenarioManager != null)
            {
                _subscribedScenarioManager.OnScenarioStarted -= HandleScenarioStarted;
            }

            _subscribedScenarioManager = ScenarioManager.Instance;
            if (_subscribedScenarioManager != null)
            {
                _subscribedScenarioManager.OnScenarioStarted += HandleScenarioStarted;
                Refresh();
            }
        }

        public string GetNextScenarioId(string currentScenarioId)
        {
            if (_controlService != null)
            {
                return _controlService.FindNextCampaignScenarioId(currentScenarioId, _repository);
            }

            if (_repository == null || string.IsNullOrEmpty(currentScenarioId)) return string.Empty;

            var scenarios = _repository.GetAllScenarios();
            scenarios.RemoveAll(s => s == null || string.IsNullOrEmpty(s.id));
            scenarios.Sort((a, b) =>
            {
                int chapter = a.chapterIndex.CompareTo(b.chapterIndex);
                return chapter != 0 ? chapter : string.CompareOrdinal(a.id, b.id);
            });

            int index = scenarios.FindIndex(s => s.id == currentScenarioId);
            return index >= 0 && index + 1 < scenarios.Count ? scenarios[index + 1].id : string.Empty;
        }

        public bool ShouldStayInGameplaySceneAfterResult()
        {
            return _controlService == null || _controlService.ShouldContinueCampaignInGameplayScene();
        }

        private void HandleScenarioStarted(ScenarioDefinition scenario)
        {
            CurrentScenarioId = scenario != null ? scenario.id : string.Empty;
            NextScenarioId = GetNextScenarioId(CurrentScenarioId);
            OnCampaignStepChanged?.Invoke(CurrentScenarioId, NextScenarioId);
        }

        private void Refresh()
        {
            CurrentScenarioId = ScenarioManager.Instance != null && ScenarioManager.Instance.CurrentScenario != null
                ? ScenarioManager.Instance.CurrentScenario.id
                : string.Empty;
            NextScenarioId = GetNextScenarioId(CurrentScenarioId);
        }
    }
}
