using System;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;

namespace NihongoLife.Scenario
{
    /// <summary>
    /// Persistent story memory. Dialogue choices set flags (DialogueChoice.setFlags); Branch nodes read them
    /// so later quests can remember what the player did ("Suzuki remembers you helped with the cat").
    /// Flags live in PlayerProgressDto.storyFlags, so they are saved locally and to the cloud with the rest of the progress.
    ///
    /// Condition syntax (used by ScenarioNode.flagCondition): tokens separated by commas are ANDed.
    ///   "flag"            flag is set
    ///   "!flag"           flag is not set
    ///   "done:scenarioId" that quest is completed
    ///   "!done:scenarioId" that quest is not completed
    /// </summary>
    public static class StoryFlags
    {
        public static bool Has(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return false;
            var progress = GetProgress();
            return progress != null && progress.storyFlags != null && progress.storyFlags.Contains(flag.Trim());
        }

        public static void Set(string flag)
        {
            SetAll(new[] { flag });
        }

        public static void SetAll(IEnumerable<string> flags)
        {
            if (flags == null) return;
            if (!GameServices.TryGet(out IProgressRepository repository)) return;

            var progress = repository.GetProgress();
            if (progress == null) return;
            if (progress.storyFlags == null) progress.storyFlags = new List<string>();

            bool changed = false;
            foreach (string raw in flags)
            {
                string flag = raw != null ? raw.Trim() : string.Empty;
                if (flag.Length == 0 || progress.storyFlags.Contains(flag)) continue;
                progress.storyFlags.Add(flag);
                changed = true;
                Debug.Log($"[StoryFlags] Set '{flag}'");
            }

            if (changed) repository.SaveProgress(progress);
        }

        /// <summary>True when every comma-separated token of the condition holds. An empty condition is true.</summary>
        public static bool Evaluate(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;

            var progress = GetProgress();
            foreach (string rawToken in condition.Split(','))
            {
                string token = rawToken.Trim();
                if (token.Length == 0) continue;

                bool negate = token.StartsWith("!", StringComparison.Ordinal);
                if (negate) token = token.Substring(1).Trim();

                bool value;
                if (token.StartsWith("done:", StringComparison.OrdinalIgnoreCase))
                {
                    string scenarioId = token.Substring(5).Trim();
                    value = progress != null && progress.completedScenarios != null && progress.completedScenarios.Contains(scenarioId);
                }
                else
                {
                    value = progress != null && progress.storyFlags != null && progress.storyFlags.Contains(token);
                }

                if (value == negate) return false;
            }

            return true;
        }

        private static PlayerProgressDto GetProgress()
        {
            return GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;
        }
    }
}
