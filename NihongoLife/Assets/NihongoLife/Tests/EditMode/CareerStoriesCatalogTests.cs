using System.Linq;
using NihongoLife.Progression;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The milestone stories (career_stories.json, Tools/story/career_stories.py): every promotion of every career has
    /// its story, in Japanese with Vietnamese and English; three answers — one best (trí tuệ), one okay, one poor (it
    /// costs performance, so the interview waits) — each with a reply; the dialogue ends on the chosen answer.
    /// </summary>
    public class CareerStoriesCatalogTests
    {
        [Test]
        public void EveryPromotion_HasAStory_WithThreeAnswers()
        {
            var stories = CareerStories.All;
            Assert.GreaterOrEqual(stories.Count, 70);
            foreach (var career in CareerCatalog.Load().careers)
            {
                for (int level = 2; level <= career.levels.Length + 1; level++)
                    Assert.IsNotNull(stories.FirstOrDefault(s => s.career == career.id && s.toLevel == level && string.IsNullOrEmpty(s.branch)), $"{career.id}: a story before level {level}");
                foreach (var branch in career.branches)
                    for (int k = 2; k <= branch.levels.Length; k++)
                    {
                        var story = CareerStories.Find(career.id, career.levels.Length + k, branch.id);
                        Assert.IsNotNull(story, $"{career.id}/{branch.id}: a story before level {career.levels.Length + k}");
                        Assert.AreEqual(branch.id, story.branch, $"{career.id}: the branch's own story");
                    }
            }
            foreach (var s in stories)
            {
                Assert.IsNotEmpty(s.titleVi + s.titleEn + s.titleJa, s.id);
                Assert.IsNotNull(s.ask, s.id);
                Assert.AreEqual(3, s.choices.Length, s.id);
                CollectionAssert.AreEquivalent(new[] { "best", "ok", "poor" }, s.choices.Select(c => c.kind), s.id);
                foreach (var l in s.lines.Append(s.ask).Concat(s.choices.Select(c => c.reply)))
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(l.ja) || string.IsNullOrWhiteSpace(l.vi) || string.IsNullOrWhiteSpace(l.en), $"{s.id}: a line in three languages");
                    Assert.IsTrue(l.speaker == "boss" || l.speaker == "narrator" || s.cast.Any(c => c.id == l.speaker), $"{s.id}: speaker {l.speaker}");
                }
                var best = s.choices.First(c => c.kind == "best");
                var poor = s.choices.First(c => c.kind == "poor");
                Assert.Greater(best.knowledge, 0, s.id + ": the best answer gives trí tuệ");
                Assert.Less(poor.performance, 0, s.id + ": a poor answer costs performance");
                Assert.IsTrue(s.choices.All(c => !string.IsNullOrWhiteSpace(c.ja) && !string.IsNullOrWhiteSpace(c.vi)), s.id);
            }
        }

        [Test]
        public void TheDialogue_EndsOnTheChosenAnswer()
        {
            var story = CareerStories.Find("retail", 2, null);
            var career = CareerCatalog.Load().Career("retail");
            var nodes = CareerStories.BuildNodes(story, career);
            var ask = nodes.Single(n => n.choices.Count > 0);
            Assert.AreEqual(3, ask.choices.Count);
            Assert.AreEqual(ask.id, nodes.Single(n => n.nextNodeId == ask.id).nextNodeId, "the lines lead to the question");
            foreach (var choice in ask.choices)
            {
                var reply = nodes.Single(n => n.id == choice.nextNodeId);
                StringAssert.StartsWith("story_choice:", reply.nextNodeId, "a reply ends the dialogue on its answer");
            }
            Assert.AreEqual(career.BossName, nodes[0].speakerName, "the boss speaks first");
        }
    }
}
