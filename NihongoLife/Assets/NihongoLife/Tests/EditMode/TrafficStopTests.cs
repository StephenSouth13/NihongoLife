using NihongoLife.World;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    public class TrafficStopTests
    {
        [Test]
        public void Bumper_IsPivotPlusDirectionTimesHalfLength()
        {
            Assert.AreEqual(10f, StreetTraffic.BumperX(6f, 1, 4f), 0.001f);
            Assert.AreEqual(-2f, StreetTraffic.BumperX(2f, -1, 4f), 0.001f);
        }

        [Test]
        public void WholeVehicle_OverlapsTheZebra_ACarBehindTheLineDoesNot()
        {
            Assert.IsTrue(StreetTraffic.OverlapsCrossing(3.5f, 2.2f, 0.7f, 6.3f));
            Assert.IsFalse(StreetTraffic.OverlapsCrossing(-2.2f, 1.2f, 0.7f, 6.3f));
        }

        [Test]
        public void Braking_IsZeroOnceTheGapIsGone()
        {
            Assert.AreEqual(0f, StreetTraffic.Braking(0.02f), 0.001f);
            Assert.Greater(StreetTraffic.Braking(4f), 1f);
            Assert.Less(StreetTraffic.Braking(0.2f), StreetTraffic.Braking(4f));
        }
    }
}
