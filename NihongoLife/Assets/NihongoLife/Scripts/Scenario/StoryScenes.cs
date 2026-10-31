using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.Progression;
using UnityEngine;

namespace NihongoLife.Scenario
{
    /// <summary>
    /// Docs/STORY.md "Cutscenes": when a story quest (chapter or career stage, Resources/Story/storyline.json) completes,
    /// its scene plays in the shared dialogue window: the quest's lines, the speaker's question, the player's answer
    /// (story flag + affinity with that speaker), the reply and the ending. Ending lines carrying a flag condition show
    /// only when it holds, so earlier answers come back later. All lines are pre-written (no runtime AI).
    /// Plays once per quest (<c>story_scene_&lt;quest&gt;</c>); never while <see cref="StoryDirector.Bypass"/> (tests) is on.
    /// No GameObject is created: it listens to <see cref="QuestService.Completed"/>.
    /// </summary>
    public static class StoryScenes
    {
        public const string PlayedPrefix = "story_scene_";
        public const string BondPrefix = "story_bond_";
        private const string EndId = "story_scene_end";

        private static readonly Queue<string> Pending = new Queue<string>();
        private static bool _waiting, _playing;

        /// <summary>Tests: turn the automatic playback off (StoryDirector.Bypass does too).</summary>
        public static bool Enabled = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Pending.Clear();
            _waiting = _playing = false;
            QuestService.Completed -= OnQuestCompleted;
            QuestService.Completed += OnQuestCompleted;
        }

        private static void OnQuestCompleted(QuestDefinition quest)
        {
            if (quest == null || !Enabled || StoryDirector.Bypass || !Application.isPlaying) return;
            if (Storyline.Load().CutsceneFor(quest.id) == null || StoryFlags.Has(PlayedPrefix + quest.id)) return;
            Pending.Enqueue(quest.id);
            TryPlayNext();
        }

        private static void TryPlayNext()
        {
            if (_playing || Pending.Count == 0) return;
            var dm = DialogueManager.Instance;
            if (dm == null) { Pending.Clear(); return; }
            if (dm.IsOpen)
            {
                if (!_waiting) { _waiting = true; dm.OnDialogueClosed += OnClosed; }
                return;
            }
            string questId = Pending.Dequeue();
            var nodes = BuildNodes(questId, StoryFlags.Has);
            if (nodes.Count == 0) { TryPlayNext(); return; }
            _playing = true;
            StoryFlags.Set(PlayedPrefix + questId);
            dm.StartConversation(nodes, nodes[0].id, end => Finished(questId));
        }

        private static void OnClosed()
        {
            var dm = DialogueManager.Instance;
            if (dm != null) dm.OnDialogueClosed -= OnClosed;
            _waiting = false;
            TryPlayNext();
        }

        private static void Finished(string questId)
        {
            _playing = false;
            ApplyChoice(questId, StoryFlags.Has);
            TryPlayNext();
        }

        /// <summary>The answer the player gave (its flag is set by the dialogue) adds its affinity once; a speaker
        /// whose affinity reaches the storyline's bond threshold gets <c>story_bond_&lt;speaker&gt;</c>.</summary>
        public static StoryChoice ApplyChoice(string questId, Func<string, bool> hasFlag)
        {
            var story = Storyline.Load();
            var scene = story.CutsceneFor(questId);
            var chosen = scene?.choices.FirstOrDefault(c => !string.IsNullOrEmpty(c.flag) && hasFlag(c.flag));
            if (chosen == null) return null;
            string speaker = scene.prompt.speaker;
            if (StoryFlags.Has(chosen.flag + "_counted")) return chosen;
            StoryFlags.Set(chosen.flag + "_counted");
            SocialStanding.AddAffinity(speaker, chosen.affinity);
            if (SocialStanding.AffinityOf(speaker) >= story.bondThreshold) StoryFlags.Set(BondPrefix + speaker);
            return chosen;
        }

        public static bool Shows(StoryLine line, Func<string, bool> hasFlag) =>
            line != null && (string.IsNullOrWhiteSpace(line.flag) || (hasFlag != null && hasFlag(line.flag.Trim())));

        /// <summary>The conversation of a story quest: lines → question (answers) → reply → ending lines whose flag holds.</summary>
        public static List<ScenarioNode> BuildNodes(string questId, Func<string, bool> hasFlag)
        {
            var story = Storyline.Load();
            var nodes = new List<ScenarioNode>();
            var scene = story.CutsceneFor(questId);
            if (scene == null) return nodes;
            string Id(string part) => "sc_" + questId + "_" + part;

            var chain = new List<ScenarioNode>();
            int i = 0;
            foreach (var l in story.LinesFor(questId)) if (Shows(l, hasFlag)) chain.Add(Node(story, Id("l" + i++), l));
            var prompt = Node(story, Id("ask"), scene.prompt);
            chain.Add(prompt);
            for (int k = 0; k + 1 < chain.Count; k++) chain[k].nextNodeId = chain[k + 1].id;
            nodes.AddRange(chain);

            foreach (var c in scene.choices)
            {
                // The ending is built per answer, so a line conditioned on this very answer shows right away.
                Func<string, bool> has = f => f == c.flag || (hasFlag != null && hasFlag(f));
                var ending = new List<ScenarioNode>();
                i = 0;
                foreach (var l in scene.ending) if (Shows(l, has)) ending.Add(Node(story, Id(c.id + "_e" + i++), l));
                for (int k = 0; k < ending.Count; k++) ending[k].nextNodeId = k + 1 < ending.Count ? ending[k + 1].id : EndId;
                var reply = Node(story, Id("r_" + c.id), c.reply);
                reply.nextNodeId = ending.Count > 0 ? ending[0].id : EndId;
                nodes.Add(reply);
                nodes.AddRange(ending);
                prompt.choices.Add(new DialogueChoice
                {
                    textJa = c.ja, textVi = c.vi, textEn = Loc.IsRussian ? c.ru : c.en,
                    nextNodeId = reply.id, setFlags = new List<string> { c.flag }
                });
            }
            return nodes;
        }

        private static ScenarioNode Node(Storyline story, string id, StoryLine l)
        {
            var speaker = story.Speaker(l.speaker);
            return new ScenarioNode
            {
                id = id, nodeType = ScenarioNodeType.Dialogue,
                speakerId = l.speaker == "narrator" ? null : l.speaker,
                speakerName = l.speaker == "narrator" ? string.Empty : speaker?.Name ?? l.speaker,
                textJa = l.ja, textVi = l.vi, textEn = Loc.IsRussian ? l.ru : l.en,
            };
        }
    }
}
