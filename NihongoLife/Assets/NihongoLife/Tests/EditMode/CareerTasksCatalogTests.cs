using System.Linq;
using NihongoLife.Progression;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The work tasks (career_tasks.json + CareerTasks/&lt;career&gt;.json, Tools/story/career_tasks.py): every career has more than
    /// 300 different tasks, each a Japanese line (or a situation) with three answers and exactly one right; more than 300 tasks
    /// lead from level 1 to the top; the card order runs through the whole pool before a card comes back.
    /// </summary>
    public class CareerTasksCatalogTests
    {
        [Test]
        public void EveryCareer_HasMoreThan300Tasks_AndTheTopTakesMoreThan300()
        {
            var ladder = CareerTasks.Ladder;
            Assert.Greater(ladder.shiftTasks, ladder.offHoursTasks, "outside working hours counts less");
            Assert.Greater(ladder.cardsPerShift, 0);
            foreach (var career in CareerCatalog.Load().careers)
            {
                var tasks = CareerTasks.For(career.id);
                Assert.GreaterOrEqual(tasks.Count, 300, career.id + ": more than 300 different tasks");
                Assert.AreEqual(tasks.Count, tasks.Select(t => t.id).Distinct().Count(), career.id + ": unique ids");
                Assert.GreaterOrEqual(CareerTasks.TasksToTop(career), 300, career.id + ": more than 300 tasks to the top");
                Assert.GreaterOrEqual(tasks.Count(t => t.minLevel <= 1), CareerTasks.TasksToPromote(1), career.id + ": enough cards at level 1");
                foreach (var t in tasks)
                {
                    Assert.AreEqual(3, t.choices.Length, t.id);
                    Assert.AreEqual(1, t.choices.Count(c => c.correct), t.id + ": one right answer");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(t.askVi) || string.IsNullOrWhiteSpace(t.askEn), t.id);
                    Assert.IsTrue(!string.IsNullOrEmpty(t.ja) || t.kind == "situation", t.id + ": a Japanese line");
                    Assert.IsTrue(t.choices.All(c => !string.IsNullOrWhiteSpace(c.vi) && !string.IsNullOrWhiteSpace(c.ja)), t.id);
                    Assert.IsTrue(t.choices.Where(c => !c.correct).All(c => !string.IsNullOrWhiteSpace(c.hintVi)), t.id + ": a wrong answer explains");
                }
            }
        }

        [Test]
        public void Cards_GoThroughThePool_BeforeOneComesBack()
        {
            var state = new CareerState { careerId = "retail", level = 9 };
            var pool = CareerTasks.For("retail");
            var seen = Enumerable.Range(0, pool.Count).Select(_ => CareerTasks.Next("retail", state).id).ToList();
            Assert.AreEqual(pool.Count, seen.Distinct().Count(), "no card twice in one round");
            state.level = 1;
            for (int i = 0; i < 50; i++) Assert.LessOrEqual(CareerTasks.Next("retail", state).minLevel, 1, "only cards for the player's level");
        }
    }
}
