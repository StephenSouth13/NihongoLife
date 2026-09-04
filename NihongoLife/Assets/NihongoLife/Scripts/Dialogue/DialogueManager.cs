using System;
using System.Collections.Generic;
using UnityEngine;
using NihongoLife.Scenario;
using NihongoLife.Scoring;
using NihongoLife.Learning;
using NihongoLife.Audio;
using NihongoLife.Core;

namespace NihongoLife.Dialogue
{
    public struct DialogueDisplayData
    {
        public string speakerName;
        public string textJa;
        public string textReading;
        public string textVi;
        public string textRomaji;
        public List<DialogueChoice> choices;
        public LearningMode learningMode;
    }

    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Learning Settings")]
        [SerializeField] private LearningMode currentMode = LearningMode.GuidedPractice;

        private ScenarioNode _currentNode;

        public event Action<DialogueDisplayData> OnDialogueUpdated;
        public event Action OnDialogueClosed;

        public LearningMode CurrentMode
        {
            get => currentMode;
            set => currentMode = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartDialogue(ScenarioNode node)
        {
            if (node == null || node.nodeType != ScenarioNodeType.Dialogue) return;

            _currentNode = node;
            Debug.Log($"[DialogueManager] Starting dialogue node: {node.id}");

            // Play voice clip if assigned
            if (node.voiceClip != null)
            {
                var audioService = GameServices.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlayVoice(node.voiceClip);
                }
            }

            // Animation Integration
            if (ScenarioManager.Instance != null && ScenarioManager.Instance.LastInteractedNPC != null)
            {
                var animCtrl = ScenarioManager.Instance.LastInteractedNPC.GetComponent<NihongoLife.Core.CharacterAnimationController>();
                if (animCtrl != null)
                {
                    animCtrl.SetTalking(true);

                    if (!string.IsNullOrEmpty(node.animationCue))
                    {
                        if (node.animationCue.Equals("bow", StringComparison.OrdinalIgnoreCase))
                        {
                            animCtrl.TriggerBow();
                        }
                        else if (node.animationCue.Equals("point", StringComparison.OrdinalIgnoreCase))
                        {
                            animCtrl.TriggerPoint();
                        }
                    }
                }
            }

            UpdateDialogueUI();
        }

        private void UpdateDialogueUI()
        {
            if (_currentNode == null) return;

            var displayData = new DialogueDisplayData
            {
                speakerName = _currentNode.speakerName,
                textJa = _currentNode.textJa,
                textReading = _currentNode.textReading,
                textVi = _currentNode.textVi,
                textRomaji = _currentNode.textRomaji,
                choices = _currentNode.choices,
                learningMode = currentMode
            };

            OnDialogueUpdated?.Invoke(displayData);
        }

        public void SelectChoice(int choiceIndex)
        {
            if (_currentNode == null || _currentNode.choices == null || choiceIndex < 0 || choiceIndex >= _currentNode.choices.Count)
            {
                Debug.LogWarning("[DialogueManager] Invalid choice selection.");
                return;
            }

            var selectedChoice = _currentNode.choices[choiceIndex];
            Debug.Log($"[DialogueManager] Selected choice index {choiceIndex}: {selectedChoice.textJa}");

            // 1. Process scoring event modifiers
            if (ScoringManager.Instance != null && selectedChoice.scoreModifiers != null)
            {
                foreach (var modifier in selectedChoice.scoreModifiers)
                {
                    ScoringManager.Instance.AddScore(modifier.category, modifier.value, modifier.reason, _currentNode.id);
                }
            }

            // 2. Process mastery additions
            if (LearningMasteryManager.Instance != null)
            {
                // Register grammar/vocab attempts
                foreach (var grammar in selectedChoice.grammarTags)
                {
                    LearningMasteryManager.Instance.RegisterUsage(grammar, true);
                }
                foreach (var vocab in selectedChoice.vocabularyTags)
                {
                    LearningMasteryManager.Instance.RegisterUsage(vocab, true);
                }
            }

            // 3. Play UI select sound
            PlaySelectSound();

            // 4. Close/Transition
            string nextNodeId = selectedChoice.nextNodeId;
            CloseDialogue();

            if (!string.IsNullOrEmpty(nextNodeId))
            {
                ScenarioManager.Instance.TransitionToNode(nextNodeId);
            }
            else
            {
                ScenarioManager.Instance.AdvanceNode();
            }
        }

        public void ContinueDialogue()
        {
            if (_currentNode == null) return;

            // Advancing a plain node with no choices
            string nextNodeId = _currentNode.nextNodeId;
            CloseDialogue();

            if (!string.IsNullOrEmpty(nextNodeId))
            {
                ScenarioManager.Instance.TransitionToNode(nextNodeId);
            }
            else
            {
                ScenarioManager.Instance.AdvanceNode();
            }
        }

        private void CloseDialogue()
        {
            if (ScenarioManager.Instance != null && ScenarioManager.Instance.LastInteractedNPC != null)
            {
                var animCtrl = ScenarioManager.Instance.LastInteractedNPC.GetComponent<NihongoLife.Core.CharacterAnimationController>();
                if (animCtrl != null)
                {
                    animCtrl.SetTalking(false);
                }
            }

            _currentNode = null;
            OnDialogueClosed?.Invoke();
        }

        private void PlaySelectSound()
        {
            var audioService = GameServices.Get<IAudioService>();
            if (audioService != null)
            {
                // We'll play a generic click sound or handle via sound settings
            }
        }
    }
}
