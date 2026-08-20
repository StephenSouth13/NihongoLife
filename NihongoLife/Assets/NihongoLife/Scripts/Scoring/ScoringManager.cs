using System;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Data;

namespace NihongoLife.Scoring
{
    [Serializable]
    public class ScoreEvent
    {
        public string category; // e.g. "Vocabulary", "Grammar", "Listening", "ResponseAccuracy", "TaskCompletion"
        public int value;
        public string reason;
        public string sourceId;
        public long timestamp;
    }

    public class ScoringManager : MonoBehaviour
    {
        public static ScoringManager Instance { get; private set; }

        private List<ScoreEvent> _events = new List<ScoreEvent>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetScore()
        {
            _events.Clear();
            Debug.Log("[ScoringManager] Score tracking reset.");
        }

        public void AddScore(string category, int value, string reason, string sourceId)
        {
            var evt = new ScoreEvent
            {
                category = category,
                value = value,
                reason = reason,
                sourceId = sourceId,
                timestamp = DateTime.UtcNow.Ticks
            };
            _events.Add(evt);
            Debug.Log($"[ScoringManager] Added Score Event: Category={category}, Value={value:+#;-#;0}, Reason={reason}");
        }

        public ScoreBreakdownDto GetBreakdown(string scenarioId)
        {
            var breakdown = new ScoreBreakdownDto
            {
                scenarioId = scenarioId,
                success = true
            };

            // Define categories we want to track
            string[] categories = { "Vocabulary", "Grammar", "Listening", "ResponseAccuracy", "TaskCompletion" };
            
            int activeCategoryCount = 0;
            int totalWeightedScore = 0;

            foreach (var cat in categories)
            {
                // Calculate category score
                int baseScore = cat == "TaskCompletion" ? 0 : 100;
                int sum = 0;
                int count = 0;

                foreach (var evt in _events)
                {
                    if (evt.category.Equals(cat, StringComparison.OrdinalIgnoreCase))
                    {
                        sum += evt.value;
                        count++;
                    }
                }

                int finalCatScore = Mathf.Clamp(baseScore + sum, 0, 100);

                // If TaskCompletion and we successfully completed the scenario, force it to 100
                if (cat == "TaskCompletion" && count == 0)
                {
                    // If no explicit task completion events were added but scenario is finishing, give 100
                    finalCatScore = 100;
                }

                breakdown.categories.Add(new ScoreCategoryResult
                {
                    category = cat,
                    score = finalCatScore
                });

                totalWeightedScore += finalCatScore;
                activeCategoryCount++;
            }

            breakdown.overallScore = activeCategoryCount > 0 ? Mathf.RoundToInt((float)totalWeightedScore / activeCategoryCount) : 100;
            return breakdown;
        }
    }
}
