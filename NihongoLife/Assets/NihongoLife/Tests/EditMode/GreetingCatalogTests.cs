using NihongoLife.Gestures;
using NUnit.Framework;
using System.Linq;

namespace NihongoLife.Tests.EditMode
{
    public class GreetingCatalogTests
    {
        [SetUp] public void SetUp() { GestureLibrary.Reset(); GreetingCatalog.Reset(); }

        [Test]
        public void Greetings_AreCompleteAndUseKnownGestures()
        {
            Assert.GreaterOrEqual(GreetingCatalog.All.Count, 18);
            foreach (var g in GreetingCatalog.All)
            {
                Assert.NotNull(GestureLibrary.Find(g.gesture), g.id + " gesture " + g.gesture);
                foreach (var s in new[] { g.native, g.roman, g.vi, g.en, g.ja, g.ru, g.cultureVi, g.cultureEn, g.cultureJa, g.cultureRu }) Assert.IsFalse(string.IsNullOrWhiteSpace(s), g.id);
                Assert.Contains(g.ctx, new[] { "formal", "casual", "thanks", "farewell", "celebrate" });
            }
        }

        [Test]
        public void Cultures_AreDiverse_AndCycleWalksThrough()
        {
            var cultures = GreetingCatalog.All.Select(g => g.culture).Distinct().ToList();
            Assert.GreaterOrEqual(cultures.Count, 10, "at least ten cultures");
            Assert.GreaterOrEqual(GreetingCatalog.All.Select(g => g.gesture).Distinct().Count(), 14, "at least fourteen different gestures");
            var seen = Enumerable.Range(0, 8).Select(_ => GreetingCatalog.Cycle("casual").id).Distinct().Count();
            Assert.GreaterOrEqual(seen, 6, "cycling shows different greetings");
        }

        [Test]
        public void WorkPoses_MapToGestures()
        {
            foreach (var pose in new[] { "Work_Hoe", "Work_Plant", "Work_Water", "Work_Harvest", "Work_Shelf", "Work_Carry" })
                Assert.NotNull(GestureLibrary.Find(WorkGestures.ForPose(pose)), pose);
            foreach (var job in new[] { "carry", "stock", "scan", "pay", "serve", "welcome", "check", "sweep", "mop", "wipe", "chop", "stir", "flip", "plate", "sushi", "roll", "paint" })
                Assert.NotNull(GestureLibrary.Find(WorkGestures.ForJob(job)), job);
        }
    }
}