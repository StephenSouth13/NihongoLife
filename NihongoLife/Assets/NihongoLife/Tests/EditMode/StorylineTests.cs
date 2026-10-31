using System.Linq;
using NihongoLife.Core;
using NihongoLife.Progression;
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
    }
}
