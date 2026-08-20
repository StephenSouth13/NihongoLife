using System;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;
using NihongoLife.Scoring;
using NihongoLife.Dialogue;

namespace NihongoLife.Scenario
{
    public enum ObjectiveState
    {
        Inactive,
        Active,
        Completed,
        Failed
    }

    [Serializable]
    public class RuntimeObjective
    {
        public string id;
        public string titleJa;
        public string titleVi;
        public bool isOptional;
        public ObjectiveState state;
    }

    public class ScenarioManager : MonoBehaviour
    {
        public static ScenarioManager Instance { get; private set; }

        [Header("Current State")]
        [SerializeField] private ScenarioDefinition currentScenario;
        private ScenarioNode _currentNode;
        private List<RuntimeObjective> _objectives = new List<RuntimeObjective>();

        public event Action<ScenarioDefinition> OnScenarioStarted;
        public event Action<RuntimeObjective> OnObjectiveStateChanged;
        public event Action<ScenarioNode> OnNodeChanged;
        public event Action<ScoreBreakdownDto> OnScenarioFinished;

        public ScenarioDefinition CurrentScenario => currentScenario;
        public ScenarioNode CurrentNode => _currentNode;
        public List<RuntimeObjective> Objectives => _objectives;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartScenario(string scenarioId)
        {
            var repo = GameServices.Get<IScenarioRepository>();
            if (repo == null)
            {
                Debug.LogError("[ScenarioManager] IScenarioRepository service not found!");
                return;
            }

            var scenario = repo.GetScenarioById(scenarioId);
            if (scenario == null)
            {
                Debug.LogError($"[ScenarioManager] Cannot start scenario. Scenario '{scenarioId}' not found.");
                return;
            }

            StartScenario(scenario);
        }

        public void StartScenario(ScenarioDefinition scenario)
        {
            currentScenario = scenario;
            Debug.Log($"[ScenarioManager] Starting scenario: {scenario.titleJa} ({scenario.id})");

            // Setup runtime objectives
            _objectives.Clear();
            foreach (var objDef in scenario.objectives)
            {
                _objectives.Add(new RuntimeObjective
                {
                    id = objDef.id,
                    titleJa = objDef.titleJa,
                    titleVi = objDef.titleVi,
                    isOptional = objDef.isOptional,
                    state = ObjectiveState.Inactive
                });
            }

            // Setup ScoringManager
            if (ScoringManager.Instance != null)
            {
                ScoringManager.Instance.ResetScore();
            }

            OnScenarioStarted?.Invoke(currentScenario);

            // Navigate to start node
            if (!string.IsNullOrEmpty(scenario.startNodeId))
            {
                TransitionToNode(scenario.startNodeId);
            }
            else
            {
                Debug.LogError("[ScenarioManager] Scenario startNodeId is empty!");
            }
        }

        public void TransitionToNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                Debug.LogWarning("[ScenarioManager] Attempted to transition to empty node ID.");
                return;
            }

            var nextNode = currentScenario.GetNode(nodeId);
            if (nextNode == null)
            {
                Debug.LogError($"[ScenarioManager] Node '{nodeId}' not found in current scenario.");
                return;
            }

            _currentNode = nextNode;
            Debug.Log($"[ScenarioManager] Transitioning to Node: {_currentNode.id} (Type: {_currentNode.nodeType})");

            // Complete objective associated with node if configured
            if (!string.IsNullOrEmpty(_currentNode.objectiveIdToComplete))
            {
                CompleteObjective(_currentNode.objectiveIdToComplete);
            }

            OnNodeChanged?.Invoke(_currentNode);

            // Execute node behavior
            ExecuteCurrentNode();
        }

        private void ExecuteCurrentNode()
        {
            switch (_currentNode.nodeType)
            {
                case ScenarioNodeType.Dialogue:
                    // Stop player movement
                    SetPlayerInputLocked(true);
                    
                    // Display Dialogue via DialogueManager
                    if (DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.StartDialogue(_currentNode);
                    }
                    else
                    {
                        Debug.LogError("[ScenarioManager] DialogueManager.Instance is null!");
                        // Fallback: auto advance if dialogue manager is missing
                        AdvanceNode();
                    }
                    break;

                case ScenarioNodeType.CollectItem:
                    SetPlayerInputLocked(false);
                    // Activate corresponding objective if defined
                    ActivateObjectiveByNodeConfig();
                    break;

                case ScenarioNodeType.InspectItem:
                    SetPlayerInputLocked(false);
                    ActivateObjectiveByNodeConfig();
                    break;

                case ScenarioNodeType.GoToArea:
                    SetPlayerInputLocked(false);
                    ActivateObjectiveByNodeConfig();
                    break;

                case ScenarioNodeType.Complete:
                    SetPlayerInputLocked(true);
                    FinishScenario(true);
                    break;

                case ScenarioNodeType.Fail:
                    SetPlayerInputLocked(true);
                    FinishScenario(false);
                    break;
            }
        }

        private void ActivateObjectiveByNodeConfig()
        {
            // If the node completion triggers an objective, activate it first if it is currently inactive
            if (!string.IsNullOrEmpty(_currentNode.objectiveIdToComplete))
            {
                var obj = _objectives.Find(o => o.id == _currentNode.objectiveIdToComplete);
                if (obj != null && obj.state == ObjectiveState.Inactive)
                {
                    obj.state = ObjectiveState.Active;
                    OnObjectiveStateChanged?.Invoke(obj);
                }
            }
        }

        public void AdvanceNode()
        {
            if (_currentNode != null && !string.IsNullOrEmpty(_currentNode.nextNodeId))
            {
                TransitionToNode(_currentNode.nextNodeId);
            }
        }

        public void OnItemInteracted(string itemId, InteractiveItem item)
        {
            if (_currentNode == null) return;

            if ((_currentNode.nodeType == ScenarioNodeType.CollectItem || _currentNode.nodeType == ScenarioNodeType.InspectItem) 
                && _currentNode.targetItemId == itemId)
            {
                Debug.Log($"[ScenarioManager] Objective item interaction successful: {itemId}");
                
                // Add default score for task completion
                if (ScoringManager.Instance != null)
                {
                    ScoringManager.Instance.AddScore("TaskCompletion", 15, $"Đã tìm thấy vật phẩm: {item.GetPromptVi()}", itemId);
                }

                AdvanceNode();
            }
        }

        public void OnNPCInteracted(string npcId, NPC.NPCController npc)
        {
            if (_currentNode == null) return;

            // Trigger dialogue if player approaches the cashier or target NPC
            if (_currentNode.nodeType == ScenarioNodeType.Dialogue && _currentNode.speakerId == npcId)
            {
                // Already started or waiting to start dialogue node
                ExecuteCurrentNode();
            }
        }

        public void CompleteObjective(string objectiveId)
        {
            var obj = _objectives.Find(o => o.id == objectiveId);
            if (obj != null && obj.state != ObjectiveState.Completed)
            {
                obj.state = ObjectiveState.Completed;
                Debug.Log($"[ScenarioManager] Objective Completed: {obj.titleJa}");
                OnObjectiveStateChanged?.Invoke(obj);
            }
        }

        public void FailObjective(string objectiveId)
        {
            var obj = _objectives.Find(o => o.id == objectiveId);
            if (obj != null && obj.state != ObjectiveState.Failed)
            {
                obj.state = ObjectiveState.Failed;
                Debug.Log($"[ScenarioManager] Objective Failed: {obj.titleJa}");
                OnObjectiveStateChanged?.Invoke(obj);
            }
        }

        private void FinishScenario(bool success)
        {
            Debug.Log($"[ScenarioManager] Scenario Finished. Status: {(success ? "Success" : "Failed")}");

            // Automatically complete all remaining objectives as completed or failed
            foreach (var obj in _objectives)
            {
                if (obj.state == ObjectiveState.Active || obj.state == ObjectiveState.Inactive)
                {
                    obj.state = success ? ObjectiveState.Completed : ObjectiveState.Failed;
                    OnObjectiveStateChanged?.Invoke(obj);
                }
            }

            // Calculate scoring breakdown
            ScoreBreakdownDto breakdown = null;
            if (ScoringManager.Instance != null)
            {
                breakdown = ScoringManager.Instance.GetBreakdown(currentScenario.id);
            }
            else
            {
                breakdown = new ScoreBreakdownDto
                {
                    scenarioId = currentScenario.id,
                    overallScore = 100,
                    success = success
                };
            }

            // Update local persistence progress
            var progressRepo = GameServices.Get<IProgressRepository>();
            if (progressRepo != null)
            {
                var progress = progressRepo.GetProgress();
                if (success)
                {
                    if (!progress.completedScenarios.Contains(currentScenario.id))
                    {
                        progress.completedScenarios.Add(currentScenario.id);
                    }

                    // Update XP
                    progress.xp += 100;
                    progress.level = 1 + (progress.xp / 500);

                    // Update best score
                    var record = progress.bestScores.Find(r => r.scenarioId == currentScenario.id);
                    if (record == null)
                    {
                        progress.bestScores.Add(new ScenarioScoreRecord
                        {
                            scenarioId = currentScenario.id,
                            bestScore = breakdown.overallScore,
                            completedAt = DateTime.UtcNow.Ticks
                        });
                    }
                    else if (breakdown.overallScore > record.bestScore)
                    {
                        record.bestScore = breakdown.overallScore;
                        record.completedAt = DateTime.UtcNow.Ticks;
                    }
                }
                progressRepo.SaveProgress(progress);
            }

            // Update Learning Mastery targets
            if (LearningMasteryManager.Instance != null && success)
            {
                LearningMasteryManager.Instance.UpdateMasteryFromScenario(currentScenario, breakdown);
            }

            // Trigger complete event
            OnScenarioFinished?.Invoke(breakdown);
        }

        public void SetPlayerInputLocked(bool locked)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var controller = player.GetComponent<Player.PlayerController>();
                if (controller != null)
                {
                    controller.InputLocked = locked;
                }
            }

            // Lock camera rotation too
            var cam = Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<Camera.ThirdPersonCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.IsLocked = locked;
                    if (locked)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
        }
    }
}
