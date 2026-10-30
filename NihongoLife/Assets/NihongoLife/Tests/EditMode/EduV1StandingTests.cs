using NihongoLife.Player;
using NihongoLife.Progression;
using NUnit.Framework;

namespace NihongoLife.Tests.EditMode
{
    public class EduV1StandingTests
    {
        [Test]
        public void ReduceNotoriety_ClampsAtZero_AndUnblocksPromotion()
        {
            SocialStanding.ResetForTests();
            SocialStanding.AddNotoriety(65);
            Assert.IsNotNull(CareerStanding.Blocker);
            Assert.AreEqual(10, SocialStanding.ReduceNotoriety(10, "apology"));
            Assert.IsNull(CareerStanding.Blocker);
            Assert.AreEqual(55, SocialStanding.ReduceNotoriety(500));
            Assert.AreEqual(0, SocialStanding.Notoriety);
            Assert.AreEqual(0, SocialStanding.ReduceNotoriety(3));
        }

        [Test]
        public void HudTag_ShowsVipOrHold()
        {
            Assert.AreEqual("", CareerStanding.HudTag(10, 10));
            StringAssert.Contains("VIP", CareerStanding.HudTag(80, 5));
            Assert.IsNotEmpty(CareerStanding.HudTag(80, 70));
            Assert.IsFalse(CareerStanding.HudTag(80, 70).Contains("VIP"));
        }
    }
}