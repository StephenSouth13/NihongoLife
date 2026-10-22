using System;
using NihongoLife.Core;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    public class GameCalendarTests
    {
        [Test]
        public void DayZero_IsAprilFirst_Spring_AgeStaysUntilTheBirthday()
        {
            Assert.AreEqual(new DateTime(1, 4, 1), GameCalendar.DateOf(0));
            Assert.AreEqual(Season.Spring, GameCalendar.SeasonOn(0));
            Assert.AreEqual(16, GameCalendar.AgeOn(0, 4, 15, 16));
            Assert.IsFalse(GameCalendar.IsBirthday(0, 4, 15));
        }

        [Test]
        public void Birthday_AdvancesAgeOnApril15()
        {
            Assert.AreEqual(new DateTime(1, 4, 15), GameCalendar.DateOf(14));
            Assert.IsTrue(GameCalendar.IsBirthday(14, 4, 15));
            Assert.AreEqual(17, GameCalendar.AgeOn(14, 4, 15, 16));
        }

        [Test]
        public void Moon_NewAtEpoch_FullHalfwayThroughTheMonth()
        {
            Assert.AreEqual(0f, GameCalendar.MoonPhase01(0, 0f), 0.0001f);
            Assert.AreEqual(MoonKind.New, GameCalendar.MoonOf(0f));
            float full = GameCalendar.SynodicDays * 0.5f;
            int day = (int)Math.Floor(full);
            float hour = (full - day) * 24f;
            Assert.AreEqual(0.5f, GameCalendar.MoonPhase01(day, hour), 0.002f);
            Assert.AreEqual(MoonKind.Full, GameCalendar.MoonOf(0.5f));
            Assert.AreEqual(MoonKind.Half, GameCalendar.MoonOf(0.25f));
            Assert.AreEqual(MoonKind.Crescent, GameCalendar.MoonOf(0.1f));
            Assert.AreEqual(MoonKind.Gibbous, GameCalendar.MoonOf(0.35f));
        }

        [Test]
        public void WinterDay_IsShorter_AndSunriseMapsToSix()
        {
            GameCalendar.Daylight(Season.Winter, out float winterRise, out float winterSet);
            GameCalendar.Daylight(Season.Summer, out float summerRise, out float summerSet);
            Assert.Less(winterSet - winterRise, summerSet - summerRise);
            Assert.AreEqual(6f, GameCalendar.EquinoxHour(winterRise, winterRise, winterSet), 0.02f);
            Assert.AreEqual(18f, GameCalendar.EquinoxHour(winterSet, winterRise, winterSet), 0.02f);
        }

        [Test]
        public void Phases_CoverTheDay()
        {
            Assert.AreEqual(DayPhase.Night, GameCalendar.PhaseOf(23f, 6f, 18f));
            Assert.AreEqual(DayPhase.Dawn, GameCalendar.PhaseOf(6.2f, 6f, 18f));
            Assert.AreEqual(DayPhase.Morning, GameCalendar.PhaseOf(9f, 6f, 18f));
            Assert.AreEqual(DayPhase.Noon, GameCalendar.PhaseOf(12f, 6f, 18f));
            Assert.AreEqual(DayPhase.Afternoon, GameCalendar.PhaseOf(15f, 6f, 18f));
            Assert.AreEqual(DayPhase.Sunset, GameCalendar.PhaseOf(17.2f, 6f, 18f));
            Assert.AreEqual(DayPhase.Evening, GameCalendar.PhaseOf(19f, 6f, 18f));
        }

        [Test]
        public void February29_IsNotStored()
        {
            Assert.AreEqual(28, GameCalendar.ClampDay(2, 29));
            Assert.AreEqual(4, GameCalendar.ClampMonth(0));
        }
    }
}
