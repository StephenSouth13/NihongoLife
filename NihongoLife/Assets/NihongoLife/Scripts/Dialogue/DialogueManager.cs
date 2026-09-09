using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using NihongoLife.Scenario;
using NihongoLife.Scoring;
using NihongoLife.Learning;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Cameras;

namespace NihongoLife.Dialogue
{
    public struct DialogueDisplayData
    {
        public string speakerName;
        public string textJa;
        public string textReading;
        public string textEn;
        public string textRomaji;
        public string textEnglishIpa;
        public List<DialogueChoice> choices;
        public LearningMode learningMode;
    }

    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Learning Settings")]
        [SerializeField] private LearningMode currentMode = LearningMode.GuidedPractice;
        [SerializeField] private bool generateDialogueVoice = false;
        [SerializeField] private float generatedVoiceVolume = 0.85f;

        private ScenarioNode _currentNode;
        private AudioSource _generatedVoiceSource;

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
            _generatedVoiceSource = gameObject.AddComponent<AudioSource>();
            _generatedVoiceSource.playOnAwake = false;
            _generatedVoiceSource.spatialBlend = 0f;
        }

        public void StartDialogue(ScenarioNode node)
        {
            if (node == null || node.nodeType != ScenarioNodeType.Dialogue) return;

            _currentNode = node;
            Debug.Log($"[DialogueManager] Starting dialogue node: {node.id}");

            // Play voice clip if assigned
            AudioClip configuredClip = node.voiceClip;
            VoiceLineEntry configuredVoiceLine = null;
            if (configuredClip == null
                && GameServices.TryGet(out GameControlService control)
                && GameServices.TryGet(out GameSettingsService settings))
            {
                configuredVoiceLine = control.FindVoiceLine(node.id, settings.Language);
                configuredClip = configuredVoiceLine != null ? configuredVoiceLine.clip : null;
            }

            if (configuredClip != null)
            {
                if (GameServices.TryGet(out IAudioService audioService))
                {
                    audioService.PlayVoice(configuredClip);
                }
            }
            else if (configuredVoiceLine != null && !string.IsNullOrWhiteSpace(configuredVoiceLine.remoteUrl))
            {
                    StartCoroutine(PlayRemoteVoice(configuredVoiceLine.remoteUrl, configuredVoiceLine.remoteAudioType, node));
            }
            else if (generateDialogueVoice && ShouldUseProceduralVoice())
            {
                PlayGeneratedVoice(node);
            }

            // Animation Integration
            if (ScenarioManager.Instance != null && ScenarioManager.Instance.LastInteractedNPC != null)
            {
                SetConversationCamera(ScenarioManager.Instance.LastInteractedNPC.transform);
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
                textEn = _currentNode.textEn,
                textRomaji = _currentNode.textRomaji,
                textEnglishIpa = ResolveEnglishIpa(_currentNode),
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
                ScenarioManager.Instance?.TransitionToNode(nextNodeId);
            }
            else if (ScenarioManager.Instance != null)
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
                ScenarioManager.Instance?.TransitionToNode(nextNodeId);
            }
            else if (ScenarioManager.Instance != null)
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

                ScenarioManager.Instance.LastInteractedNPC.StopInteracting();
            }

            ClearConversationCamera();
            _currentNode = null;
            OnDialogueClosed?.Invoke();

            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.SetPlayerInputLocked(false);
            }
        }

        private void PlayGeneratedVoice(ScenarioNode node)
        {
            if (_generatedVoiceSource == null) return;

            string line = node.textJa;
            if (GameServices.TryGet(out GameSettingsService settings))
            {
                line = settings.Language == GameLanguage.English ? node.textEn : node.textJa;
            }

            if (string.IsNullOrWhiteSpace(line)) return;

            AudioClip clip = CreateSpeechToneClip(line);
            _generatedVoiceSource.Stop();
            _generatedVoiceSource.volume = generatedVoiceVolume;
            _generatedVoiceSource.PlayOneShot(clip);
        }

        private IEnumerator PlayRemoteVoice(string url, AudioType audioType, ScenarioNode fallbackNode)
        {
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[DialogueManager] Failed to load remote voice: {request.error}");
                    PlayGeneratedVoice(fallbackNode);
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    PlayGeneratedVoice(fallbackNode);
                    yield break;
                }

                _generatedVoiceSource.Stop();
                _generatedVoiceSource.volume = generatedVoiceVolume;
                _generatedVoiceSource.PlayOneShot(clip);
            }
        }

        public bool TryGetCurrentPracticePhrase(out string phrase, out string ipa)
        {
            phrase = string.Empty;
            ipa = string.Empty;
            if (_currentNode == null) return false;

            phrase = _currentNode.textJa;
            ipa = _currentNode.textRomaji;
            if (GameServices.TryGet(out GameSettingsService settings) && settings.Language == GameLanguage.English)
            {
                phrase = _currentNode.textEn;
                ipa = ResolveEnglishIpa(_currentNode);
            }

            return !string.IsNullOrWhiteSpace(phrase);
        }

        private static bool ShouldUseProceduralVoice()
        {
            if (!GameServices.TryGet(out GameControlService control) || control.Database == null) return true;
            return control.Database.useProceduralVoiceWhenMissingClip;
        }

        private static string ResolveEnglishIpa(ScenarioNode node)
        {
            if (node == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(node.textEnglishIpa)) return node.textEnglishIpa;
            if (GameServices.TryGet(out GameControlService control))
            {
                string fromDatabase = control.FindEnglishIpa(node.id);
                if (!string.IsNullOrWhiteSpace(fromDatabase)) return fromDatabase;
            }

            return string.Empty;
        }

        private static AudioClip CreateSpeechToneClip(string line)
        {
            const int sampleRate = 22050;
            int syllables = Mathf.Clamp(line.Length / 3, 8, 34);
            float duration = Mathf.Clamp(0.12f * syllables, 0.8f, 3.4f);
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples];

            int hash = Mathf.Abs(line.GetHashCode());
            for (int i = 0; i < samples; i++)
            {
                float time = i / (float)sampleRate;
                float syllable = Mathf.Floor(time / 0.12f);
                float baseFrequency = 180f + ((hash + (int)syllable * 37) % 140);
                float envelope = Mathf.Sin(Mathf.Clamp01((time % 0.12f) / 0.12f) * Mathf.PI);
                float pause = (time % 0.36f) > 0.29f ? 0.15f : 1f;
                data[i] = Mathf.Sin(2f * Mathf.PI * baseFrequency * time) * envelope * pause * 0.22f;
            }

            AudioClip clip = AudioClip.Create("GeneratedDialogueVoice", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void PlaySelectSound()
        {
            if (GameServices.TryGet(out IAudioService audioService))
            {
                // We'll play a generic click sound or handle via sound settings
            }
        }

        private static void SetConversationCamera(Transform target)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var controller = camera.GetComponent<ThirdPersonCameraController>();
            if (controller != null) controller.SetConversationTarget(target);
        }

        private static void ClearConversationCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var controller = camera.GetComponent<ThirdPersonCameraController>();
            if (controller != null) controller.ClearConversationTarget();
        }
    }
}
