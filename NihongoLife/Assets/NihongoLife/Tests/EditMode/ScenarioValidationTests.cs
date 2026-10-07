using System.Linq;
using NihongoLife.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>Runs the existing ScenarioValidator over every scenario asset in Resources/Scenarios (including
    /// the ramen / station / festival drafts) and checks that requirements and unlocks name real scenarios.</summary>
    public class ScenarioValidationTests
    {
        [Test]
        public void AllScenarioAssets_PassValidation()
        {
            var scenarios = Resources.LoadAll<ScenarioDefinition>("Scenarios");
            Assert.IsNotEmpty(scenarios);
            var ids = scenarios.Select(s => s.id).ToHashSet();
            var report = new System.Text.StringBuilder();
            bool ok = true;
            foreach (var scenario in scenarios)
            {
                var result = ScenarioValidator.Validate(scenario);
                foreach (var issue in result.Issues) report.AppendLine($"{scenario.id}: {issue.Severity} {issue.Message}");
                ok &= !result.HasErrors;
                foreach (string required in scenario.requiredScenarioIds.Where(r => !ids.Contains(r)))
                {
                    report.AppendLine($"{scenario.id}: requires unknown scenario '{required}'");
                    ok = false;
                }
                foreach (string unlock in scenario.unlockScenarioIds.Where(u => !ids.Contains(u)))
                {
                    report.AppendLine($"{scenario.id}: unlocks unknown scenario '{unlock}'");
                    ok = false;
                }
            }
            Debug.Log($"[ScenarioValidation] {scenarios.Length} scenarios\n{report}");
            Assert.IsTrue(ok, "Scenario problems:\n" + report);
        }
    }
}
