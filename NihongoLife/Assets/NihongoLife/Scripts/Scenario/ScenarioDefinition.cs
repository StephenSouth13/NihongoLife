using System;
using System.Collections.Generic;
using System.Linq;
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
        Fail,
        TalkToNPC,
        /// <summary>Invisible router: goes to flagJumpNodeId when flagCondition holds, otherwise to nextNodeId.</summary>
        Branch
    }

    [Serializable]
    public class ObjectiveDefinition
    {
        public string id;
        public string titleJa;

        public string titleVi;
        public string titleEn;
        public bool isOptional;

        /// <summary>The objective in the interface language.</summary>
        public string Title => NihongoLife.Core.Loc.Pick(titleVi, titleEn, titleJa);
    }

    [Serializable]
    public class DialogueChoice
    {
        public string textJa;
        public string textVi;
        public string textEn;

        /// <summary>Meaning of the Japanese answer in the interface language (empty in the Japanese interface).</summary>
        public string Gloss => NihongoLife.Core.Loc.Gloss(textVi, textEn);

        public string nextNodeId;
        
        // Scoring and Learning modifications
        public List<ScoreEventModifier> scoreModifiers = new List<ScoreEventModifier>();
        public List<string> grammarTags = new List<string>();
        public List<string> vocabularyTags = new List<string>();

        [Tooltip("Story flags stored when this answer is chosen (see StoryFlags). Later quests can react to them.")]
        public List<string> setFlags = new List<string>();
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
        public string textEn;

        /// <summary>Meaning of the Japanese line in the interface language (empty in the Japanese interface).</summary>
        public string Gloss => NihongoLife.Core.Loc.Gloss(textVi, textEn);

        public string textRomaji;
        public string textEnglishIpa;
        public string animationCue;
        public AudioClip voiceClip;
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [Header("Branch Node Config (nodeType = Branch)")]
        [Tooltip("Comma-separated AND tokens: flag, !flag, done:scenarioId, !done:scenarioId")]
        public string flagCondition;
        public string flagJumpNodeId;

        [Header("Interact/Collect/Area Node Config")]
        public string targetItemId; // ID of item to collect/inspect
        public string targetNpcId;
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
        public string titleEn;

        [TextArea(3, 5)] public string descriptionJa;
        [TextArea(3, 5)] public string descriptionVi;
        [TextArea(3, 5)] public string descriptionEn;

        /// <summary>Title, description, briefing and place hint in the interface language.</summary>
        public string DisplayTitle => NihongoLife.Core.Loc.Pick(titleVi, titleEn, titleJa);
        public string Description => NihongoLife.Core.Loc.Pick(descriptionVi, descriptionEn, descriptionJa);
        public string Briefing => NihongoLife.Core.Loc.Pick(briefingVi, briefingEn, briefingJa);
        public string LocationHint => NihongoLife.Core.Loc.Pick(locationHintVi, locationHintEn, locationHintJa);

        public int chapterIndex = 1;

        [Header("Branching Story")]
        public string branchId = "main";
        [Min(0)] public int requiredKnowledge;
        public List<string> requiredScenarioIds = new List<string>();
        public List<string> unlockScenarioIds = new List<string>();
        public bool repeatable;

        [Header("Quest")]
        [Tooltip("main | side | career | community | coop. Shown as a badge in the quest journal.")]
        public string questType = "main";
        [Tooltip("NPC who gives the quest (speakerId), shown in the journal.")]
        public string giverNpcId;
        [TextArea(2, 4)] public string briefingVi;
        [TextArea(2, 4)] public string briefingEn;
        [TextArea(2, 4)] public string briefingJa;
        [Tooltip("Where to go, in plain words.")]
        public string locationHintVi;
        public string locationHintEn;
        public string locationHintJa;
        [Min(0)] public int rewardYen;
        public List<string> rewardItems = new List<string>();

        [Header("Knowledge Progression")]
        [Range(1, 10)] public int learningDifficulty = 1;
        [Min(1)] public int baseKnowledgeReward = 60;

        [Header("Learning Configurations")]
        public List<string> learningTargets = new List<string>();

        [Header("Objectives")]
        public List<ObjectiveDefinition> objectives = new List<ObjectiveDefinition>();

        [Header("Scenario Nodes")]
        public List<ScenarioNode> nodes = new List<ScenarioNode>();
        public string startNodeId;

        [Header("Co-op Settings")]
        public bool supportsCoOp = false;
        public int minPlayers = 1;
        public int maxPlayers = 2;
        public List<string> speakerRoles = new List<string>(); // e.g. ["Customer", "Shopkeeper"]

        public ScenarioNode GetNode(string nodeId)
        {
            if (nodes == null) return null;
            return nodes.Find(n => n.id == nodeId);
        }

        public bool IsUnlocked(int knowledge, IReadOnlyCollection<string> completedScenarioIds)
        {
            if (StoryDirector.Bypass) return true;
            if (knowledge < requiredKnowledge) return false;
            if (requiredScenarioIds == null || requiredScenarioIds.Count == 0) return true;
            if (completedScenarioIds == null) return false;
            foreach (string requirement in requiredScenarioIds)
            {
                if (!completedScenarioIds.Contains(requirement)) return false;
            }
            return true;
        }
    }
}
