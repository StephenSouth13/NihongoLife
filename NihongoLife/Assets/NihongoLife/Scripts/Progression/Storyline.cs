using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Progression
{
    // ─────────── Long story (read-only data: Resources/Story/storyline.json, written by Tools/story/gen.py) ───────────

    /// <summary>One pre-written NPC line: Japanese plus its meaning in Vietnamese, English and Russian.</summary>
    [Serializable]
    public sealed class StoryLine
    {
        public string speaker, ja, vi, en, ru;
        /// <summary>Optional StoryFlags condition: the line is part of the scene only when it holds (an earlier choice).</summary>
        public string flag;
        /// <summary>The meaning in the interface language (empty in Japanese, where the line itself is the text).</summary>
        public string Gloss => Loc.IsJapanese ? string.Empty : Storyline.Pick(vi, en, ja, ru);
    }

    /// <summary>One answer of a cutscene choice: sets <see cref="flag"/>, adds <see cref="affinity"/> with the speaker.</summary>
    [Serializable]
    public sealed class StoryChoice
    {
        public string id, ja, vi, en, ru, flag;
        public int affinity;
        public StoryLine reply;
    }

    /// <summary>The scene played when a chapter / stage quest completes (StoryScenes): question, answers, ending.</summary>
    [Serializable]
    public sealed class StoryCutscene
    {
        public StoryLine prompt;
        public StoryChoice[] choices = Array.Empty<StoryChoice>();
        public StoryLine[] ending = Array.Empty<StoryLine>();
    }

    [Serializable] public sealed class StoryLanguageLevel { public string id; public int knowledge; }

    /// <summary>A stage of every career arc: trainee → staff → senior → manager → owner/specialist.</summary>
    [Serializable]
    public sealed class StoryTier
    {
        public string id, jlpt, nameEn, nameVi, nameJa, nameRu;
        public int careerLevel;
        public string Name => Storyline.Pick(nameVi, nameEn, nameJa, nameRu);
    }

    [Serializable]
    public sealed class StorySpeaker
    {
        public string id, nameJa, nameVi, nameEn, nameRu;
        public string Name => Storyline.Pick(nameVi, nameEn, nameJa, nameRu);
    }

    [Serializable]
    public sealed class StoryChapter
    {
        public string id, questId, jlpt, speaker, scene;
        public string titleEn, titleVi, titleJa, titleRu, summaryEn, summaryVi, summaryJa, summaryRu;
        public int order, minLevel;
        public StoryLine[] lines = Array.Empty<StoryLine>();
        public StoryCutscene cutscene;
        public string Title => Storyline.Pick(titleVi, titleEn, titleJa, titleRu);
        public string Summary => Storyline.Pick(summaryVi, summaryEn, summaryJa, summaryRu);
    }

    [Serializable]
    public sealed class StoryStage
    {
        public string tier, jlpt, questId;
        public string titleEn, titleVi, titleJa, titleRu, goalEn, goalVi, goalJa, goalRu;
        public int careerLevel;
        public StoryLine[] lines = Array.Empty<StoryLine>();
        public StoryCutscene cutscene;
        public string Title => Storyline.Pick(titleVi, titleEn, titleJa, titleRu);
        public string Goal => Storyline.Pick(goalVi, goalEn, goalJa, goalRu);
    }

    [Serializable]
    public sealed class StoryArc
    {
        public string career, speaker, scene;
        public StoryStage[] stages = Array.Empty<StoryStage>();
    }

    /// <summary>
    /// The long story: a main storyline of chapters and one arc per career (careers.json), five stages each. Every chapter
    /// and stage is a quest in progression.json (category "story"), so QuestService does the unlocking, tracking and
    /// rewards: a stage needs its career at <see cref="StoryStage.careerLevel"/> (quest.career/careerLevel), its JLPT level
    /// as knowledge (quest.minKnowledge = <see cref="KnowledgeFor"/>) and the stage before it (requiresQuests).
    /// This class only adds what quests do not carry: the language levels, the tiers and the pre-written NPC dialogue.
    /// </summary>
    [Serializable]
    public sealed class Storyline
    {
        public const string ResourcePath = "Story/storyline";
        public const string Schema = "nihongolife.story.v1";
        public static readonly string[] TierOrder = { "trainee", "staff", "senior", "manager", "owner" };

        public string schema;
        public StoryLanguageLevel[] languageLevels = Array.Empty<StoryLanguageLevel>();
        public StoryTier[] tiers = Array.Empty<StoryTier>();
        public StorySpeaker[] speakers = Array.Empty<StorySpeaker>();
        public StoryChapter[] chapters = Array.Empty<StoryChapter>();
        public StoryArc[] arcs = Array.Empty<StoryArc>();
        /// <summary>Affinity with a speaker at which <c>story_bond_&lt;speaker&gt;</c> is set.</summary>
        public int bondThreshold = 20;

        private static Storyline _cached;

        public static Storyline Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>(ResourcePath);
            _cached = text != null ? Parse(text.text) : new Storyline();
            return _cached;
        }

        public static Storyline Parse(string json) => JsonUtility.FromJson<Storyline>(json) ?? new Storyline();

        /// <summary>vi/en/ja like <see cref="Loc.Pick"/>, with the Russian field used directly in Russian.</summary>
        public static string Pick(string vi, string en, string ja, string ru) =>
            Loc.IsRussian && !string.IsNullOrEmpty(ru) ? ru : Loc.Pick(vi, en, ja);

        // ── Language level (JLPT) from knowledge ──

        public string JlptFor(int knowledge)
        {
            string level = languageLevels.Length > 0 ? languageLevels[0].id : "N5";
            foreach (var l in languageLevels) if (knowledge >= l.knowledge) level = l.id;
            return level;
        }

        public int KnowledgeFor(string jlpt) => languageLevels.FirstOrDefault(l => l.id == jlpt)?.knowledge ?? int.MaxValue;

        // ── Lookups ──

        public StoryArc Arc(string career) => arcs.FirstOrDefault(a => a.career == career);
        public StoryChapter Chapter(string questId) => chapters.FirstOrDefault(c => c.questId == questId);
        public StoryTier Tier(string id) => tiers.FirstOrDefault(t => t.id == id);
        public StorySpeaker Speaker(string id) => speakers.FirstOrDefault(s => s.id == id);

        public StoryStage Stage(string questId, out StoryArc arc)
        {
            foreach (var a in arcs)
                foreach (var s in a.stages)
                    if (s.questId == questId) { arc = a; return s; }
            arc = null;
            return null;
        }

        public StoryCutscene CutsceneFor(string questId) =>
            Chapter(questId)?.cutscene ?? Stage(questId, out _)?.cutscene;

        public StoryLine[] LinesFor(string questId) =>
            Chapter(questId)?.lines ?? Stage(questId, out _)?.lines ?? Array.Empty<StoryLine>();

        /// <summary>The highest stage of the career's arc the career level has reached (null before the first).</summary>
        public StoryStage StageAtLevel(string career, int careerLevel) =>
            Arc(career)?.stages.Where(s => s.careerLevel <= careerLevel).OrderBy(s => s.careerLevel).LastOrDefault();

        /// <summary>The unlock rule of a stage, as QuestService applies it through the quest's fields.</summary>
        public bool StageOpen(StoryStage stage, string arcCareer, string playerCareer, int playerCareerLevel, int knowledge) =>
            stage != null && arcCareer == playerCareer && playerCareerLevel >= stage.careerLevel && knowledge >= KnowledgeFor(stage.jlpt);

        /// <summary>The scene dialogue of a story quest for the journal: speaker, Japanese line, meaning.</summary>
        public string DialogueText(string questId)
        {
            var lines = LinesFor(questId);
            if (lines.Length == 0) return string.Empty;
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                string name = Speaker(line.speaker)?.Name ?? line.speaker;
                sb.Append("<b>").Append(name).Append("</b>: ").Append(line.ja);
                string gloss = line.Gloss;
                if (!string.IsNullOrEmpty(gloss)) sb.Append("\n<size=85%><color=#A9B7C4>").Append(gloss).Append("</color></size>");
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        public static string DialogueFor(string questId) => Load().DialogueText(questId);

        // ── Validation (EditMode test StorylineTests) ──

        public List<string> Validate(ProgressionCatalog progression, CareerCatalog careers)
        {
            var errors = new List<string>();
            if (schema != Schema) errors.Add($"schema must be {Schema} (was '{schema}')");
            for (int i = 1; i < languageLevels.Length; i++)
                if (languageLevels[i].knowledge <= languageLevels[i - 1].knowledge) errors.Add($"language level {languageLevels[i].id} must need more knowledge than {languageLevels[i - 1].id}");
            if (!tiers.Select(t => t.id).SequenceEqual(TierOrder)) errors.Add("tiers must be " + string.Join(", ", TierOrder));
            for (int i = 1; i < tiers.Length; i++)
                if (tiers[i].careerLevel <= tiers[i - 1].careerLevel) errors.Add($"tier {tiers[i].id}: career level must rise");

            var questIds = new HashSet<string>();
            void CheckLines(string where, StoryLine[] lines)
            {
                if (lines == null || lines.Length < 3) errors.Add($"{where}: needs at least 3 dialogue lines");
                foreach (var l in lines ?? Array.Empty<StoryLine>())
                {
                    if (Speaker(l.speaker) == null) errors.Add($"{where}: unknown speaker '{l.speaker}'");
                    if (string.IsNullOrWhiteSpace(l.ja) || string.IsNullOrWhiteSpace(l.vi) || string.IsNullOrWhiteSpace(l.en) || string.IsNullOrWhiteSpace(l.ru))
                        errors.Add($"{where}: a line is missing ja/vi/en/ru");
                }
            }
            var flags = new HashSet<string>();
            void CheckCutscene(string where, StoryCutscene c)
            {
                if (c == null || c.prompt == null) { errors.Add($"{where}: needs a cutscene with a question"); return; }
                CheckLines(where + " question", new[] { c.prompt, c.prompt, c.prompt });
                if (c.choices == null || c.choices.Length < 2) errors.Add($"{where}: a cutscene needs at least 2 answers");
                foreach (var ch in c.choices ?? Array.Empty<StoryChoice>())
                {
                    if (string.IsNullOrWhiteSpace(ch.ja) || string.IsNullOrWhiteSpace(ch.vi) || string.IsNullOrWhiteSpace(ch.en) || string.IsNullOrWhiteSpace(ch.ru))
                        errors.Add($"{where}: answer '{ch.id}' is missing ja/vi/en/ru");
                    if (string.IsNullOrWhiteSpace(ch.flag) || !flags.Add(ch.flag)) errors.Add($"{where}: answer '{ch.id}' needs its own story flag");
                    if (ch.affinity < 0 || ch.affinity > 20) errors.Add($"{where}: answer '{ch.id}' affinity out of 0..20");
                    if (ch.reply == null) errors.Add($"{where}: answer '{ch.id}' needs a reply");
                    else CheckLines(where + " reply", new[] { ch.reply, ch.reply, ch.reply });
                }
                if (c.ending == null || c.ending.Length == 0 || c.ending.All(l => !string.IsNullOrEmpty(l.flag)))
                    errors.Add($"{where}: the ending needs at least one line that is always shown");
                else CheckLines(where + " ending", c.ending.Length >= 3 ? c.ending : c.ending.Concat(c.ending).Concat(c.ending).ToArray());
            }
            QuestDefinition CheckQuest(string where, string questId, string jlpt)
            {
                if (!questIds.Add(questId)) errors.Add($"{where}: quest '{questId}' used twice");
                var q = progression?.Quest(questId);
                if (q == null) { errors.Add($"{where}: quest '{questId}' is not in progression.json"); return null; }
                if (q.category != "story") errors.Add($"{where}: quest '{questId}' must be category story");
                if (q.minKnowledge != KnowledgeFor(jlpt)) errors.Add($"{where}: quest '{questId}' minKnowledge {q.minKnowledge} ≠ JLPT {jlpt} ({KnowledgeFor(jlpt)})");
                return q;
            }

            string previous = null;
            foreach (var c in chapters.OrderBy(c => c.order))
            {
                string where = $"chapter '{c.id}'";
                var q = CheckQuest(where, c.questId, c.jlpt);
                if (q != null)
                {
                    if (q.minLevel != c.minLevel) errors.Add($"{where}: quest minLevel {q.minLevel} ≠ {c.minLevel}");
                    if (previous != null && !q.requiresQuests.Contains(previous)) errors.Add($"{where}: must require the chapter before ({previous})");
                    if (q.IsCareerTask) errors.Add($"{where}: a main chapter is not tied to one career");
                }
                CheckLines(where, c.lines);
                CheckCutscene(where, c.cutscene);
                previous = c.questId;
            }

            foreach (var career in careers?.careers ?? Array.Empty<CareerDefinition>())
                if (Arc(career.id) == null) errors.Add($"career '{career.id}' has no story arc");
            foreach (var a in arcs)
            {
                var career = careers?.Career(a.career);
                if (career == null) { errors.Add($"arc '{a.career}': not a career in careers.json"); continue; }
                if (!a.stages.Select(s => s.tier).SequenceEqual(TierOrder)) errors.Add($"arc '{a.career}': stages must be " + string.Join(", ", TierOrder));
                string before = null;
                foreach (var s in a.stages)
                {
                    string where = $"arc '{a.career}' stage '{s.tier}'";
                    var tier = Tier(s.tier);
                    if (tier != null && (tier.careerLevel != s.careerLevel || tier.jlpt != s.jlpt)) errors.Add($"{where}: career level / JLPT differ from the tier");
                    if (s.careerLevel > career.MaxLevel) errors.Add($"{where}: career level {s.careerLevel} is above the ladder ({career.MaxLevel})");
                    var q = CheckQuest(where, s.questId, s.jlpt);
                    if (q != null)
                    {
                        if (q.career != a.career || q.careerLevel != s.careerLevel) errors.Add($"{where}: quest must be career '{a.career}' level {s.careerLevel}");
                        if (before != null && !q.requiresQuests.Contains(before)) errors.Add($"{where}: must require the stage before ({before})");
                    }
                    CheckLines(where, s.lines);
                    CheckCutscene(where, s.cutscene);
                    before = s.questId;
                }
            }
            return errors;
        }
    }
}
