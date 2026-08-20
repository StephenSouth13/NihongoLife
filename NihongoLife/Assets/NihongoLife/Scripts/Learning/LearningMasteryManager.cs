using System;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;
using NihongoLife.Scenario;

namespace NihongoLife.Learning
{
    public class LearningMasteryManager : MonoBehaviour
    {
        public static LearningMasteryManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterUsage(string targetId, bool correct)
        {
            var progressRepo = GameServices.Get<IProgressRepository>();
            if (progressRepo == null) return;

            var progress = progressRepo.GetProgress();
            var record = progress.masteryLevels.Find(r => r.targetId == targetId);

            if (record == null)
            {
                record = new MasteryRecord
                {
                    targetId = targetId,
                    masteryValue = 50f, // start halfway
                    lastUpdatedAt = DateTime.UtcNow.Ticks
                };
                progress.masteryLevels.Add(record);
            }

            // Adjust mastery based on correct or incorrect usage
            float change = correct ? 5.0f : -3.0f;
            record.masteryValue = Mathf.Clamp(record.masteryValue + change, 0f, 100f);
            record.lastUpdatedAt = DateTime.UtcNow.Ticks;

            progressRepo.SaveProgress(progress);
            Debug.Log($"[LearningMasteryManager] Quick update: {targetId} Mastery => {record.masteryValue:0.0}");
        }

        public void UpdateMasteryFromScenario(ScenarioDefinition scenario, ScoreBreakdownDto breakdown)
        {
            var progressRepo = GameServices.Get<IProgressRepository>();
            if (progressRepo == null) return;

            var progress = progressRepo.GetProgress();
            float overallScore = breakdown.overallScore;

            Debug.Log($"[LearningMasteryManager] Updating mastery for {scenario.learningTargets.Count} targets from scenario: {scenario.id}");

            foreach (var targetId in scenario.learningTargets)
            {
                var record = progress.masteryLevels.Find(r => r.targetId == targetId);
                if (record == null)
                {
                    record = new MasteryRecord
                    {
                        targetId = targetId,
                        masteryValue = 50f,
                        lastUpdatedAt = DateTime.UtcNow.Ticks
                    };
                    progress.masteryLevels.Add(record);
                }

                // Smooth mastery progression toward overall score
                // E.g. if overallScore is 90 and current is 50, increase by 15% of distance: 50 -> 56
                float currentVal = record.masteryValue;
                float learningFactor = 0.15f; // how fast mastery matches performance
                float targetVal = overallScore;

                record.masteryValue = Mathf.Clamp(currentVal + (targetVal - currentVal) * learningFactor, 0f, 100f);
                record.lastUpdatedAt = DateTime.UtcNow.Ticks;

                Debug.Log($"[LearningMasteryManager] Mastery target '{targetId}' updated from {currentVal:0.0} => {record.masteryValue:0.0}");
            }

            progressRepo.SaveProgress(progress);
        }

        public float GetMastery(string targetId)
        {
            var progressRepo = GameServices.Get<IProgressRepository>();
            if (progressRepo == null) return 0f;

            var progress = progressRepo.GetProgress();
            var record = progress.masteryLevels.Find(r => r.targetId == targetId);
            return record?.masteryValue ?? 0f;
        }
    }
}
