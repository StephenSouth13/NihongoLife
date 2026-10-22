using System.Linq;
using NihongoLife.Save;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    /// <summary>Partial of HUDUI: interaction prompt and objective text. The MonoBehaviour entry point stays HUDUI.cs.</summary>
    public partial class HUDUI : MonoBehaviour
    {
        private void HandleInteractableChanged(IInteractable interactable)
        {
            _currentInteractable = interactable;
            if (interactable == null)
            {
                HidePrompt();
            }
            else
            {
                ShowPrompt(interactable);
            }
        }

        private void ShowPrompt(IInteractable interactable)
        {
            if (promptPanel == null || promptText == null) return;
            promptPanel.SetActive(true);
            HudPrompt.ContextualVisible = true;
            string key = GameInputService.GetOrCreate().GetBindingLabel(GameInputId.Interact);
            if (_keyCap != null) _keyCap.text = key;
            promptText.text = _keyCap != null ? InteractionPrompt.Of(interactable) : $"<color=#F2B233><b>[ {key} ]</b></color>   {InteractionPrompt.Of(interactable)}";
        }

        private void HidePrompt()
        {
            if (promptPanel != null) promptPanel.SetActive(false);
            HudPrompt.ContextualVisible = false;
        }

        private void UpdateScenarioInfo(ScenarioDefinition scenario)
        {
            if (scenarioTitleText != null)
            {
                scenarioTitleText.text = ScenarioText(scenario);
            }
            UpdateObjectivesDisplay();
        }

        private void HandleObjectiveChanged(RuntimeObjective objective)
        {
            UpdateObjectivesDisplay();
        }

        private void UpdateObjectivesDisplay()
        {
            if (objectivesText == null) return;

            if (ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentScenario == null)
            {
                objectivesText.text = string.Empty;
                if (scenarioTitleText != null) scenarioTitleText.text = Text("Không có nhiệm vụ", "No active mission", "ミッションなし");
                return;
            }

            var builder = new StringBuilder();
            foreach (var obj in ScenarioManager.Instance.Objectives)
            {
                string check = obj.state == ObjectiveState.Completed ? "●" : obj.state == ObjectiveState.Failed ? "×" : "○";
                string color = obj.state switch
                {
                    ObjectiveState.Completed => "#74d680",
                    ObjectiveState.Failed => "#ff7676",
                    ObjectiveState.Active => "#f5f2e8",
                    _ => "#8fa3b8"
                };
                builder.AppendLine($"<color={color}>{check} {ObjectiveText(obj)}</color>");
            }

            objectivesText.text = builder.ToString();
        }

        private string ObjectiveText(RuntimeObjective objective)
        {
            if (objective == null) return string.Empty;
            return objective.Title;
        }

        private string ScenarioText(ScenarioDefinition scenario)
        {
            if (scenario == null) return string.Empty;
            return scenario.DisplayTitle;
        }
    }
}
