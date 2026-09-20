using System;
using NihongoLife.Core;
using NihongoLife.Save;
using System.Collections.Generic;
using System.Linq;
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

        public List<ScenarioDefinition> GetAvailableBranches()
        {
            var available = new List<ScenarioDefinition>();
            if (_repository == null) return available;
            int knowledge = 0;
            IReadOnlyCollection<string> completed = System.Array.Empty<string>();
            if (GameServices.TryGet(out IProgressRepository progressRepository))
            {
                var progress = progressRepository.GetProgress();
                if (progress != null)
                {
                    knowledge = progress.knowledge;
                    completed = progress.completedScenarios ?? new List<string>();
                }
            }

            foreach (ScenarioDefinition scenario in _repository.GetAllScenarios())
            {
                if (scenario == null || !scenario.IsUnlocked(knowledge, completed)) continue;
                if (!scenario.repeatable && completed.Contains(scenario.id)) continue;
                available.Add(scenario);
            }
            available.Sort((a, b) => a.chapterIndex.CompareTo(b.chapterIndex));
            return available;
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
