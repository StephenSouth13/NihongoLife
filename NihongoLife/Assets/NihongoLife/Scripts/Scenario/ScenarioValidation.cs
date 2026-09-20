using System.Collections.Generic;
using System.Text;

namespace NihongoLife.Scenario
{
    public enum ScenarioValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class ScenarioValidationIssue
    {
        public ScenarioValidationIssue(ScenarioValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public ScenarioValidationSeverity Severity { get; }
        public string Message { get; }
    }

    public sealed class ScenarioValidationResult
    {
        private readonly List<ScenarioValidationIssue> _issues = new List<ScenarioValidationIssue>();

        public IReadOnlyList<ScenarioValidationIssue> Issues => _issues;
        public bool HasErrors { get; private set; }

        public void AddError(string message)
        {
            HasErrors = true;
            _issues.Add(new ScenarioValidationIssue(ScenarioValidationSeverity.Error, message));
        }

        public void AddWarning(string message)
        {
            _issues.Add(new ScenarioValidationIssue(ScenarioValidationSeverity.Warning, message));
        }

        public string ToLogString()
        {
            if (_issues.Count == 0)
            {
                return "Scenario validation passed.";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < _issues.Count; i++)
            {
                var issue = _issues[i];
                builder.Append('[')
                    .Append(issue.Severity)
                    .Append("] ")
                    .Append(issue.Message);

                if (i < _issues.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }
    }

    public static class ScenarioValidator
    {
        public static ScenarioValidationResult Validate(ScenarioDefinition scenario)
        {
            var result = new ScenarioValidationResult();
            if (scenario == null)
            {
                result.AddError("Scenario asset is null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(scenario.id))
            {
                result.AddError("Scenario id is required.");
            }

            if (scenario.nodes == null || scenario.nodes.Count == 0)
            {
                result.AddError($"Scenario '{scenario.id}' has no nodes.");
                return result;
            }

            var nodeIds = new HashSet<string>();
            var objectiveIds = new HashSet<string>();

            if (scenario.objectives != null)
            {
                foreach (var objective in scenario.objectives)
                {
                    if (objective == null)
                    {
                        result.AddError($"Scenario '{scenario.id}' contains a null objective.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(objective.id))
                    {
                        result.AddError($"Scenario '{scenario.id}' contains an objective without an id.");
                    }
                    else if (!objectiveIds.Add(objective.id))
                    {
                        result.AddError($"Scenario '{scenario.id}' has duplicate objective id '{objective.id}'.");
                    }
                }
            }

            foreach (var node in scenario.nodes)
            {
                if (node == null)
                {
                    result.AddError($"Scenario '{scenario.id}' contains a null node.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.id))
                {
                    result.AddError($"Scenario '{scenario.id}' contains a node without an id.");
                    continue;
                }

                if (!nodeIds.Add(node.id))
                {
                    result.AddError($"Scenario '{scenario.id}' has duplicate node id '{node.id}'.");
                }
            }

            string startNodeId = scenario.startNodeId;
            if (string.IsNullOrWhiteSpace(startNodeId))
            {
                result.AddWarning($"Scenario '{scenario.id}' has no explicit startNodeId; it will use the first node.");
                startNodeId = scenario.nodes[0]?.id;
            }

            if (string.IsNullOrWhiteSpace(startNodeId) || !nodeIds.Contains(startNodeId))
            {
                result.AddError($"Scenario '{scenario.id}' start node '{scenario.startNodeId}' does not exist.");
            }

            foreach (var node in scenario.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id))
                {
                    continue;
                }

                ValidateNodeLinks(scenario.id, node, nodeIds, objectiveIds, result);
                ValidateNodeTargets(scenario.id, node, result);
            }

            ValidateReachability(scenario.id, startNodeId, scenario.nodes, nodeIds, result);
            return result;
        }

        private static void ValidateNodeLinks(
            string scenarioId,
            ScenarioNode node,
            HashSet<string> nodeIds,
            HashSet<string> objectiveIds,
            ScenarioValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(node.nextNodeId) && !nodeIds.Contains(node.nextNodeId))
            {
                result.AddError($"Scenario '{scenarioId}' node '{node.id}' points to missing nextNodeId '{node.nextNodeId}'.");
            }

            if (node.nodeType == ScenarioNodeType.Branch)
            {
                if (string.IsNullOrWhiteSpace(node.flagJumpNodeId) || !nodeIds.Contains(node.flagJumpNodeId))
                {
                    result.AddError($"Scenario '{scenarioId}' branch node '{node.id}' has missing flagJumpNodeId '{node.flagJumpNodeId}'.");
                }

                if (string.IsNullOrWhiteSpace(node.nextNodeId))
                {
                    result.AddError($"Scenario '{scenarioId}' branch node '{node.id}' needs a nextNodeId for the case where the condition is false.");
                }
            }

            if (!string.IsNullOrWhiteSpace(node.objectiveIdToComplete) && !objectiveIds.Contains(node.objectiveIdToComplete))
            {
                result.AddError($"Scenario '{scenarioId}' node '{node.id}' references missing objective '{node.objectiveIdToComplete}'.");
            }

            if (node.choices == null)
            {
                return;
            }

            foreach (var choice in node.choices)
            {
                if (choice == null)
                {
                    result.AddError($"Scenario '{scenarioId}' node '{node.id}' contains a null dialogue choice.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(choice.nextNodeId) && !nodeIds.Contains(choice.nextNodeId))
                {
                    result.AddError($"Scenario '{scenarioId}' node '{node.id}' choice '{choice.textJa}' points to missing node '{choice.nextNodeId}'.");
                }
            }
        }

        private static void ValidateNodeTargets(string scenarioId, ScenarioNode node, ScenarioValidationResult result)
        {
            switch (node.nodeType)
            {
                case ScenarioNodeType.CollectItem:
                case ScenarioNodeType.InspectItem:
                    if (string.IsNullOrWhiteSpace(node.targetItemId))
                    {
                        result.AddError($"Scenario '{scenarioId}' node '{node.id}' requires targetItemId.");
                    }
                    break;

                case ScenarioNodeType.GoToArea:
                    if (string.IsNullOrWhiteSpace(node.targetAreaId))
                    {
                        result.AddError($"Scenario '{scenarioId}' node '{node.id}' requires targetAreaId.");
                    }
                    break;

                case ScenarioNodeType.TalkToNPC:
                    if (string.IsNullOrWhiteSpace(node.targetNpcId))
                    {
                        result.AddError($"Scenario '{scenarioId}' node '{node.id}' requires targetNpcId.");
                    }
                    break;
            }
        }

        private static void ValidateReachability(
            string scenarioId,
            string startNodeId,
            List<ScenarioNode> nodes,
            HashSet<string> nodeIds,
            ScenarioValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(startNodeId) || !nodeIds.Contains(startNodeId))
            {
                return;
            }

            var nodeMap = new Dictionary<string, ScenarioNode>();
            foreach (var node in nodes)
            {
                if (node != null && !string.IsNullOrWhiteSpace(node.id) && !nodeMap.ContainsKey(node.id))
                {
                    nodeMap.Add(node.id, node);
                }
            }

            var visited = new HashSet<string>();
            var pending = new Queue<string>();
            pending.Enqueue(startNodeId);

            while (pending.Count > 0)
            {
                var nodeId = pending.Dequeue();
                if (!visited.Add(nodeId) || !nodeMap.TryGetValue(nodeId, out var node))
                {
                    continue;
                }

                EnqueueIfValid(node.nextNodeId, nodeIds, pending);
                EnqueueIfValid(node.flagJumpNodeId, nodeIds, pending);

                if (node.choices == null)
                {
                    continue;
                }

                foreach (var choice in node.choices)
                {
                    if (choice != null)
                    {
                        EnqueueIfValid(choice.nextNodeId, nodeIds, pending);
                    }
                }
            }

            foreach (var nodeId in nodeIds)
            {
                if (!visited.Contains(nodeId))
                {
                    result.AddWarning($"Scenario '{scenarioId}' node '{nodeId}' is not reachable from start node '{startNodeId}'.");
                }
            }
        }

        private static void EnqueueIfValid(string nodeId, HashSet<string> nodeIds, Queue<string> pending)
        {
            if (!string.IsNullOrWhiteSpace(nodeId) && nodeIds.Contains(nodeId))
            {
                pending.Enqueue(nodeId);
            }
        }
    }
}
