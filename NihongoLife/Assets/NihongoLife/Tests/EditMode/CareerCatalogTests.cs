using System.Linq;
using NihongoLife.Progression;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards the editable career data (Resources/Progression/careers.json): every career works an existing shift,
    /// has a ladder with a branch, an interview for every promotion (both branches) and events with real choices.
    /// </summary>
    public class CareerCatalogTests
    {
        [Test]
        public void RealCatalog_IsValid()
        {
            var catalog = CareerCatalog.Load();
            Assert.Greater(catalog.careers.Length, 0, "careers.json did not load");
            var errors = catalog.Validate(ProgressionCatalog.Load());
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void RealCatalog_FourCareersFromLevel10_WithBranchesAndSixLevels()
        {
            var catalog = CareerCatalog.Load();
            Assert.AreEqual(10, catalog.unlockLevel);
            CollectionAssert.AreEquivalent(new[] { "retail", "food", "farm", "medical", "hospitality", "chef", "musician", "producer", "painter" }, catalog.careers.Select(c => c.id).ToArray());
            foreach (var career in catalog.careers)
            {
                Assert.AreEqual(6, career.MaxLevel, career.id);
                Assert.AreEqual(2, career.branches.Length, career.id + " has two paths");
                Assert.AreEqual(4, career.branchAtLevel, career.id);
                Assert.Greater(career.Level(6, career.branches[1].id).salary, career.Level(1, null).salary, career.id + " pays more higher up");
                Assert.AreNotEqual(career.Level(5, career.branches[0].id).titleJa, career.Level(5, career.branches[1].id).titleJa, career.id + " branches differ");
                Assert.GreaterOrEqual(career.events.Length, 2, career.id + " has events at work");
            }
        }

        [Test]
        public void RealCatalog_HasADevelopmentPath_OfMilestones()
        {
            var catalog = CareerCatalog.Load();
            Assert.GreaterOrEqual(catalog.milestones.Length, 6, "milestones");
            foreach (var id in new[] { "first_shift", "first_promotion", "branch", "top" })
                Assert.IsTrue(catalog.milestones.Any(m => m.id == id), "milestone " + id);
            Assert.IsTrue(catalog.milestones.All(m => m.rewardYen > 0 || m.rewardKnowledge > 0), "every milestone rewards something");
            Assert.IsTrue(catalog.milestones.All(m => !string.IsNullOrEmpty(m.titleJa)), "Japanese titles");
        }

        [Test]
        public void BrokenMilestones_AreRejected()
        {
            var broken = CareerCatalog.Parse("{\"unlockLevel\":10,\"milestones\":[{\"id\":\"a\",\"type\":\"shifts\",\"titleVi\":\"x\",\"titleEn\":\"x\"},{\"id\":\"a\",\"type\":\"dance\"}]}");
            var errors = broken.Validate(null);
            Assert.IsTrue(errors.Any(e => e.Contains("duplicate")), "duplicate id");
            Assert.IsTrue(errors.Any(e => e.Contains("unknown type")), "unknown type");
            Assert.IsTrue(errors.Any(e => e.Contains("missing title")), "missing title");
        }

        [Test]
        public void BrokenCatalog_IsRejected()
        {
            var broken = CareerCatalog.Parse("{\"unlockLevel\":10,\"careers\":[{\"id\":\"x\",\"shiftQuest\":\"no_such_job\",\"workStart\":14,\"workEnd\":8,\"workDays\":[9],\"levels\":[{\"titleVi\":\"a\",\"salary\":1,\"shiftsToPromote\":1},{\"titleVi\":\"b\"}]}]}");
            var errors = broken.Validate(ProgressionCatalog.Load());
            Assert.IsTrue(errors.Any(e => e.Contains("shiftQuest")), "unknown shift");
            Assert.IsTrue(errors.Any(e => e.Contains("working hours")), "hours");
            Assert.IsTrue(errors.Any(e => e.Contains("workDays")), "days");
            Assert.IsTrue(errors.Any(e => e.Contains("no interview")), "a promotion without an interview");
        }
    }
}
