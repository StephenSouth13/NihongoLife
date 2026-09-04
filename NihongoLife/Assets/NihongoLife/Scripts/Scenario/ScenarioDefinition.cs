using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Scenario
{
    public enum ScenarioNodeType
    {
        Dialogue,
        CollectItem,
        InspectItem,
        GoToArea,
        Complete,
        Fail
    }

    [Serializable]
    public class ObjectiveDefinition
    {
        public string id;
        public string titleJa;
        public string titleVi;
        public bool isOptional;
    }

    [Serializable]
    public class DialogueChoice
    {
        public string textJa;
        public string textVi;
        public string nextNodeId;
        
        // Scoring and Learning modifications
        public List<ScoreEventModifier> scoreModifiers = new List<ScoreEventModifier>();
        public List<string> grammarTags = new List<string>();
        public List<string> vocabularyTags = new List<string>();
    }

    [Serializable]
    public class ScoreEventModifier
    {
        public string category; // e.g. "Vocabulary", "Grammar", "Listening", "ResponseAccuracy", "TaskCompletion"
        public int value; // e.g. +10, -5
        public string reason;
    }

    [Serializable]
    public class ScenarioNode
    {
        public string id;
        public ScenarioNodeType nodeType;
        
        [Header("Common Node Config")]
        public string nextNodeId; // Default next node
        public string objectiveIdToComplete; // If this node completes an objective

        [Header("Dialogue Node Config")]
        public string speakerName;
        public string speakerId;
        public string textJa;
        public string textReading; // Furigana/Hiragana helper
        public string textVi;
        public string textRomaji;
        public string animationCue;
        public AudioClip voiceClip;
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [Header("Interact/Collect/Area Node Config")]
        public string targetItemId; // ID of item to collect/inspect
        public string targetAreaId; // ID of trigger area
    }

    [CreateAssetMenu(fileName = "NewScenario", menuName = "NihongoLife/Scenario Definition")]
    public class ScenarioDefinition : ScriptableObject
    {
        [Header("Metadata")]
        public string id;
        public int version = 1;
        public string titleJa;
        public string titleVi;
        [TextArea(3, 5)] public string descriptionJa;
        [TextArea(3, 5)] public string descriptionVi;
        public int chapterIndex = 1;

        [Header("Learning Configurations")]
        public List<string> learningTargets = new List<string>();

        [Header("Objectives")]
        public List<ObjectiveDefinition> objectives = new List<ObjectiveDefinition>();

        [Header("Scenario Nodes")]
        public List<ScenarioNode> nodes = new List<ScenarioNode>();
        public string startNodeId;

        public ScenarioNode GetNode(string nodeId)
        {
            if (nodes == null) return null;
            return nodes.Find(n => n.id == nodeId);
        }
    }
}
