using System;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;
using NihongoLife.Scoring;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.Cameras;
using NihongoLife.Player;

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
        private readonly List<RuntimeObjective> _objectives = new List<RuntimeObjective>();
        private NPC.NPCController _lastInteractedNPC;

        public event Action<ScenarioDefinition> OnScenarioStarted;
        public event Action<RuntimeObjective> OnObjectiveStateChanged;
        public event Action<ScenarioNode> OnNodeChanged;
        public event Action<ScoreBreakdownDto> OnScenarioFinished;

        public ScenarioDefinition CurrentScenario => currentScenario;
        public ScenarioNode CurrentNode => _currentNode;
        public List<RuntimeObjective> Objectives => _objectives;
        public NPC.NPCController LastInteractedNPC => _lastInteractedNPC;

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

            ScoringManager.Instance?.ResetScore();
            OnScenarioStarted?.Invoke(currentScenario);

            string startId = scenario.startNodeId;
            if (string.IsNullOrEmpty(startId) && scenario.nodes != null && scenario.nodes.Count > 0)
            {
                startId = scenario.nodes[0].id;
            }

            if (!string.IsNullOrEmpty(startId))
            {
                TransitionToNode(startId);
            }
            else
            {
                Debug.LogError("[ScenarioManager] Scenario has no valid start node!");
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

            if (_currentNode.id == "node_transaction_done")
            {
                ResolveCheckout();
            }

            if (_currentNode.nodeType == ScenarioNodeType.CollectItem
                || _currentNode.nodeType == ScenarioNodeType.InspectItem
                || _currentNode.nodeType == ScenarioNodeType.GoToArea)
            {
                ActivateObjectiveByNodeConfig();
            }
            else if (!string.IsNullOrEmpty(_currentNode.objectiveIdToComplete))
            {
                CompleteObjective(_currentNode.objectiveIdToComplete);
            }

            OnNodeChanged?.Invoke(_currentNode);
            ExecuteCurrentNode();
        }

        private void ExecuteCurrentNode()
        {
            switch (_currentNode.nodeType)
            {
                case ScenarioNodeType.Dialogue:
                    SetPlayerInputLocked(true);
                    if (DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.StartDialogue(_currentNode);
                    }
                    else
                    {
                        Debug.LogError("[ScenarioManager] DialogueManager.Instance is null!");
                        AdvanceNode();
                    }
                    break;

                case ScenarioNodeType.CollectItem:
                case ScenarioNodeType.InspectItem:
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
            if (string.IsNullOrEmpty(_currentNode.objectiveIdToComplete)) return;

            var obj = _objectives.Find(o => o.id == _currentNode.objectiveIdToComplete);
            if (obj != null && obj.state == ObjectiveState.Inactive)
            {
                obj.state = ObjectiveState.Active;
                OnObjectiveStateChanged?.Invoke(obj);
            }
        }

        public void AdvanceNode()
        {
            if (_currentNode != null && !string.IsNullOrEmpty(_currentNode.nextNodeId))
            {
                TransitionToNode(_currentNode.nextNodeId);
            }
        }

        public bool CanEnterArea(string areaId)
        {
            return _currentNode != null
                && _currentNode.nodeType == ScenarioNodeType.GoToArea
                && _currentNode.targetAreaId == areaId;
        }

        public void OnAreaEntered(string areaId)
        {
            if (!CanEnterArea(areaId)) return;

            Debug.Log($"[ScenarioManager] Target area reached: {areaId}");
            if (!string.IsNullOrEmpty(_currentNode.objectiveIdToComplete))
            {
                CompleteObjective(_currentNode.objectiveIdToComplete);
            }

            AdvanceNode();
        }

        public bool OnItemInteracted(string itemId, InteractiveItem item)
        {
            if (_currentNode == null) return true;

            bool isItemNode = _currentNode.nodeType == ScenarioNodeType.CollectItem
                || _currentNode.nodeType == ScenarioNodeType.InspectItem;

            if (isItemNode && _currentNode.targetItemId == itemId)
            {
                Debug.Log($"[ScenarioManager] Objective item interaction successful: {itemId}");
                ScoringManager.Instance?.AddScore("TaskCompletion", 15, $"Đã tìm thấy vật phẩm: {item.GetPromptVi()}", itemId);

                if (!string.IsNullOrEmpty(_currentNode.objectiveIdToComplete))
                {
                    CompleteObjective(_currentNode.objectiveIdToComplete);
                }

                AdvanceNode();
                return true;
            }

            if (isItemNode)
            {
                Debug.Log($"[ScenarioManager] Objective item interaction incorrect: {itemId}");
                ScoringManager.Instance?.AddScore("Vocabulary", -5, "Chọn nhầm vật phẩm", itemId);
                ScoringManager.Instance?.AddScore("ResponseAccuracy", -10, "Chọn sai vật phẩm mục tiêu", itemId);

                var warningNode = new ScenarioNode
                {
                    id = "temp_warning_wrong_item",
                    nodeType = ScenarioNodeType.Dialogue,
                    speakerName = "Hệ thống",
                    speakerId = "system",
                    textJa = "これは違います。おにぎりを探してください。",
                    textReading = "これはちがいます。おにぎりをさがしてください。",
                    textVi = "Đây không phải vật phẩm được yêu cầu. Hãy tìm cơm nắm!",
                    textRomaji = "Kore wa chigaimasu. Onigiri wo sagashite kudasai.",
                    nextNodeId = _currentNode.id
                };

                SetPlayerInputLocked(true);
                DialogueManager.Instance?.StartDialogue(warningNode);
                return false;
            }

            return true;
        }

        public void OnNPCInteracted(string npcId, NPC.NPCController npc)
        {
            _lastInteractedNPC = npc;
            if (_currentNode == null) return;

            if (_currentNode.nodeType == ScenarioNodeType.Dialogue && _currentNode.speakerId == npcId)
            {
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

            foreach (var obj in _objectives)
            {
                if (obj.state == ObjectiveState.Active || obj.state == ObjectiveState.Inactive)
                {
                    obj.state = success ? ObjectiveState.Completed : ObjectiveState.Failed;
                    OnObjectiveStateChanged?.Invoke(obj);
                }
            }

            ScoreBreakdownDto breakdown;
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

                    progress.xp += 100;
                    progress.level = 1 + (progress.xp / 500);

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

            if (LearningMasteryManager.Instance != null && success)
            {
                LearningMasteryManager.Instance.UpdateMasteryFromScenario(currentScenario, breakdown);
            }

            OnScenarioFinished?.Invoke(breakdown);
        }

        private void ResolveCheckout()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null)
            {
                Debug.LogError("[ScenarioManager] Cannot checkout because PlayerInventory is missing.");
                return;
            }

            if (!inventory.HasItem("onigiri"))
            {
                Debug.LogWarning("[ScenarioManager] Checkout blocked: onigiri is not in the player's inventory.");
                ScoringManager.Instance?.AddScore("TaskCompletion", -30, "Thanh toán khi chưa có hàng", "checkout_missing_item");
                return;
            }

            int total = Mathf.Max(497, inventory.GetCartTotalYen());
            if (!inventory.SpendYen(total))
            {
                Debug.LogWarning("[ScenarioManager] Checkout blocked: not enough yen.");
                ScoringManager.Instance?.AddScore("TaskCompletion", -30, "Không đủ tiền thanh toán", "checkout_no_money");
                return;
            }

            inventory.RemoveItem("onigiri");
            ScoringManager.Instance?.AddScore("TaskCompletion", 25, $"Đã thanh toán {total} yen", "checkout_paid");
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

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<ThirdPersonCameraController>();
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
