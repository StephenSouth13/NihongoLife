using NihongoLife.Player;
using NihongoLife.Progression;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>Edu v1 rules: fame/notoriety drive the career; the character grows with real proportions.</summary>
    public class EduV1RulesTests
    {
        [Test]
        public void Fame_SpeedsTheCareer_NotorietySlowsAndBlocksIt()
        {
            Assert.Greater(CareerStanding.PerformanceFactor(80, 0), 1.2f);
            Assert.Less(CareerStanding.PerformanceFactor(0, 60), 0.7f);
            Assert.AreEqual(1f, CareerStanding.PerformanceFactor(0, 0), 1e-4);
            Assert.IsNull(CareerStanding.PromotionBlocker(10, CareerStanding.ScandalBlock - 1));
            Assert.IsNotNull(CareerStanding.PromotionBlocker(90, CareerStanding.ScandalBlock));
            Assert.Greater(CareerStanding.SalaryFactor(90, 0), CareerStanding.SalaryFactor(30, 0));
            Assert.Less(CareerStanding.SalaryFactor(30, 80), 1f);
            Assert.IsTrue(CareerStanding.VipClients(75, 10));
            Assert.IsFalse(CareerStanding.VipClients(75, 40), "a scandal loses the VIP clients");
            Assert.AreEqual(8, CareerStanding.NotorietyAfterGoodShift(10));
        }

        [Test]
        public void Character_GrowsWithLevel_AdultAtTwentyOne()
        {
            Assert.AreEqual(8, CharacterGrowth.AgeForLevel(1));
            Assert.AreEqual(18, CharacterGrowth.AgeForLevel(21));
            float prev = 0f;
            for (int age = 8; age <= 18; age++) { float h = CharacterGrowth.HeightAt(age); Assert.GreaterOrEqual(h, prev); prev = h; }
            Assert.AreEqual(1.28f, CharacterGrowth.HeightAt(8), 0.01f);
            Assert.AreEqual(CharacterGrowth.AdultHeight, CharacterGrowth.HeightAt(30), 1e-4);
            Assert.Greater(CharacterGrowth.HeadRatio(8), CharacterGrowth.HeadRatio(18), "a child's head is a larger share of the body");
        }
    }
}
