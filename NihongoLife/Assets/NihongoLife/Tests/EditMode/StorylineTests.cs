using System.Linq;
using NihongoLife.Core;
using NihongoLife.Progression;
using NihongoLife.Scenario;
using NUnit.Framework;

namespace NihongoLife.Tests.EditMode
{
    /// <summary>Docs/STORY.md: the main storyline and the career arcs (Resources/Story/storyline.json) against
    /// progression.json, careers.json and the Russian table.</summary>
    public sealed class StorylineTests
    {
        private static Storyline Story => Storyline.Load();

        [Test]
        public void Storyline_IsValidAgainstQuestsAndCareers()
        {
            var errors = Story.Validate(ProgressionCatalog.Load(), CareerCatalog.Load());
            Assert.IsEmpty(errors, string.Join("\n", errors));
            Assert.IsEmpty(ProgressionCatalog.Load().Validate(), "progression.json");
        }

        [Test]
        public void MainStory_HasChaptersFromArrivalToHomecoming()
        {
            Assert.GreaterOrEqual(Story.chapters.Length, 8);
            var first = ProgressionCatalog.Load().Quest(Story.chapters.OrderBy(c => c.order).First().questId);
            Assert.IsEmpty(first.requiresQuests, "the first chapter is open from the start");
            Assert.AreEqual("N1", Story.chapters.OrderBy(c => c.order).Last().jlpt);
            Assert.IsNotNull(Story.chapters.FirstOrDefault(c => c.scene == "65_PhuQuocIsland"), "a chapter leads to Phú Quốc");
        }

        [Test]
        public void EveryCareer_HasFiveStagesTraineeToOwner_WithRisingLanguageLevel()
        {
            foreach (var career in CareerCatalog.Load().careers)
            {
                var arc = Story.Arc(career.id);
                Assert.IsNotNull(arc, career.id);
                CollectionAssert.AreEqual(Storyline.TierOrder, arc.stages.Select(s => s.tier).ToArray(), career.id);
                var knowledge = arc.stages.Select(s => Story.KnowledgeFor(s.jlpt)).ToArray();
                CollectionAssert.IsOrdered(knowledge, career.id);
                Assert.AreEqual(career.MaxLevel, arc.stages.Last().careerLevel, career.id + ": the last stage is the top of the ladder");
            }
            Assert.IsNotNull(Story.Arc("hospitality"), "Gánh Ngon arc");
            Assert.IsNotNull(Story.Arc("medical"), "clinic / doctor arc");
        }

        [Test]
        public void LanguageLevel_FollowsKnowledge()
        {
            Assert.AreEqual("N5", Story.JlptFor(0));
            Assert.AreEqual("N4", Story.JlptFor(Story.KnowledgeFor("N4")));
            Assert.AreEqual("N4", Story.JlptFor(Story.KnowledgeFor("N3") - 1));
            Assert.AreEqual("N1", Story.JlptFor(100000));
        }

        [Test]
        public void StageUnlock_NeedsTheCareer_ItsLevel_AndTheLanguageLevel()
        {
            var arc = Story.Arc("medical");
            var manager = arc.stages.First(s => s.tier == "manager");
            int enough = Story.KnowledgeFor(manager.jlpt);
            Assert.IsTrue(Story.StageOpen(manager, "medical", "medical", manager.careerLevel, enough));
            Assert.IsFalse(Story.StageOpen(manager, "medical", "retail", manager.careerLevel, enough), "other career");
            Assert.IsFalse(Story.StageOpen(manager, "medical", "medical", manager.careerLevel - 1, enough), "career level too low");
            Assert.IsFalse(Story.StageOpen(manager, "medical", "medical", manager.careerLevel, enough - 1), "language level too low");
            Assert.AreEqual("senior", Story.StageAtLevel("medical", 4).tier);
            Assert.AreEqual("owner", Story.StageAtLevel("hospitality", 6).tier);
            Assert.IsNull(Story.StageAtLevel("hospitality", 0));
        }

        [Test]
        public void StoryQuests_HaveRussian_AndDialogueText()
        {
            RuTable.Reload();
            var story = ProgressionCatalog.Load().quests.Where(q => q.id.StartsWith("story_")).ToList();
            Assert.GreaterOrEqual(story.Count, 50);
            foreach (var q in story)
            {
                Assert.IsTrue(RuTable.TryGet(q.titleEn, out _), "ru title: " + q.titleEn);
                Assert.IsTrue(RuTable.TryGet(q.descriptionEn, out _), "ru description: " + q.descriptionEn);
                foreach (var o in q.objectives) Assert.IsTrue(RuTable.TryGet(o.textEn, out _), "ru objective: " + o.textEn);
                string dialogue = Story.DialogueText(q.id);
                Assert.IsNotEmpty(dialogue, q.id);
                StringAssert.Contains(Story.LinesFor(q.id)[0].ja, dialogue);
            }
        }

        [Test]
        public void EveryChapterAndStage_PlaysACutscene_WithChoicesThatReachTheEnding()
        {
            var ids = Story.chapters.Select(c => c.questId).Concat(Story.arcs.SelectMany(a => a.stages).Select(s => s.questId)).ToList();
            Assert.AreEqual(Story.chapters.Length + Story.arcs.Length * 5, ids.Count);
            foreach (var id in ids)
            {
                var nodes = StoryScenes.BuildNodes(id, _ => false);
                Assert.IsNotEmpty(nodes, id);
                var map = nodes.ToDictionary(n => n.id);
                var ask = nodes.Single(n => n.choices.Count > 0);
                Assert.GreaterOrEqual(ask.choices.Count, 2, id);
                // walk from the first node to the question
                var n0 = nodes[0]; int guard = 0;
                while (n0 != ask) { Assert.IsTrue(map.TryGetValue(n0.nextNodeId, out n0), id + ": broken chain"); Assert.Less(++guard, 50); }
                foreach (var c in ask.choices)
                {
                    Assert.AreEqual(1, c.setFlags.Count, id);
                    StringAssert.StartsWith("story_", c.setFlags[0]);
                    var n = map[c.nextNodeId]; guard = 0;
                    while (map.ContainsKey(n.nextNodeId ?? "")) { n = map[n.nextNodeId]; Assert.Less(++guard, 50); }
                    Assert.IsFalse(string.IsNullOrEmpty(n.nextNodeId), id + ": the scene must end");
                    Assert.IsTrue(nodes.All(x => !string.IsNullOrEmpty(x.textJa)), id);
                }
            }
        }

        [Test]
        public void Choices_HaveConsequences_LaterInTheStory()
        {
            var owner = Story.Arc("medical").stages.Last().questId;
            string flag = "story_medical_trainee_careful";
            int without = StoryScenes.BuildNodes(owner, _ => false).Count;
            int with = StoryScenes.BuildNodes(owner, f => f == flag).Count;
            Assert.Greater(with, without, "the owner stage recalls the trainee answer");

            var home = Story.chapters.OrderBy(c => c.order).Last().questId;
            int lonely = StoryScenes.BuildNodes(home, _ => false).Count;
            int bonded = StoryScenes.BuildNodes(home, f => f.StartsWith(StoryScenes.BondPrefix)).Count;
            Assert.Greater(bonded, lonely, "the last chapter thanks the people you bonded with");
            foreach (var c in Story.chapters)
                Assert.IsTrue(c.cutscene.choices.All(ch => ch.affinity > 0 && ch.flag.StartsWith("story_choice_" + c.id + "_")), c.id);
        }

        [Test]
        public void CutsceneLines_HaveRussian()
        {
            RuTable.Reload();
            foreach (var sc in Story.chapters.Select(c => c.cutscene).Concat(Story.arcs.SelectMany(a => a.stages).Select(s => s.cutscene)))
            {
                Assert.IsTrue(RuTable.TryGet(sc.prompt.en, out _), "ru: " + sc.prompt.en);
                foreach (var ch in sc.choices) { Assert.IsTrue(RuTable.TryGet(ch.en, out _), "ru: " + ch.en); Assert.IsTrue(RuTable.TryGet(ch.reply.en, out _), "ru: " + ch.reply.en); }
                foreach (var l in sc.ending) Assert.IsTrue(RuTable.TryGet(l.en, out _), "ru: " + l.en);
            }
        }
    }
}
