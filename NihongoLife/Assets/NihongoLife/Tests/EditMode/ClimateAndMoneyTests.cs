using NihongoLife.Core;
using NUnit.Framework;

namespace NihongoLife.Tests.EditMode
{
    /// <summary>H3/H4 seasonal climate + HUD clock text, G3 currency by language.</summary>
    public class ClimateAndMoneyTests
    {
        private GameLanguage _before;
        [SetUp] public void SetUp() { _before = Loc.Language; }
        [TearDown] public void TearDown() { Loc.Set(_before); }

        [Test]
        public void Temperature_FollowsSeasonHourAndWeather()
        {
            float summerNoon = ClimateService.TemperatureC(Season.Summer, 14f, ClimateService.Sky.Clear);
            float winterNoon = ClimateService.TemperatureC(Season.Winter, 14f, ClimateService.Sky.Clear);
            float winterNight = ClimateService.TemperatureC(Season.Winter, 3f, ClimateService.Sky.Clear);
            Assert.Greater(summerNoon, 28f); Assert.Less(winterNoon, 14f);
            Assert.Greater(winterNoon, winterNight, "afternoons are warmer than nights");
            Assert.Less(ClimateService.TemperatureC(Season.Summer, 14f, ClimateService.Sky.Rain), summerNoon, "rain cools");
            Assert.Greater(ClimateService.TemperatureC(Season.Spring, 14f, ClimateService.Sky.Clear, true), ClimateService.TemperatureC(Season.Spring, 14f, ClimateService.Sky.Clear, false), "the island is warmer");
            foreach (Season s in System.Enum.GetValues(typeof(Season)))
                for (float h = 0; h < 24; h += 3) Assert.That(ClimateService.TemperatureC(s, h, ClimateService.Sky.Clear), Is.InRange(-5f, 40f));
        }

        [Test]
        public void HudClock_IsLocalized_AndShowsTimeWeatherTemperature()
        {
            Loc.Set(GameLanguage.English);
            string en = ClimateService.Format(Season.Summer, 14.5f, ClimateService.Sky.Rain, false);
            StringAssert.StartsWith("14:30", en); StringAssert.Contains("Rain", en); StringAssert.EndsWith("°C", en);
            Loc.Set(GameLanguage.Vietnamese);
            StringAssert.Contains("Mưa", ClimateService.Format(Season.Summer, 14.5f, ClimateService.Sky.Rain, false));
            StringAssert.Contains("Trời quang", ClimateService.Format(Season.Winter, 22f, ClimateService.Sky.Clear, false));
            Loc.Set(GameLanguage.Japanese);
            StringAssert.Contains("雨", ClimateService.Format(Season.Summer, 14.5f, ClimateService.Sky.Rain, false));
        }

        [Test]
        public void Money_IsShownInTheLanguagesCurrency()
        {
            Loc.Set(GameLanguage.Japanese); Assert.AreEqual("¥12,400", Loc.Money(12400)); Assert.AreEqual("-¥500", Loc.Yen(-500));
            Loc.Set(GameLanguage.English); Assert.AreEqual("$124.00", Loc.Money(12400)); Assert.AreEqual("$6.00", Loc.Money(600));
            Loc.Set(GameLanguage.Vietnamese); Assert.AreEqual("2.108.000đ", Loc.Money(12400)); Assert.AreEqual("102.000đ", Loc.Money(600));
            Loc.Set(GameLanguage.Russian); StringAssert.EndsWith("руб.", Loc.Money(12400)); StringAssert.StartsWith("12", Loc.Money(12400));
            Assert.IsFalse(Loc.Money(600).Contains("¥"), "no yen sign in a Russian UI");
            // Yen written into text follows the language, with or without {0} holes; sums stay exact.
            Loc.Set(GameLanguage.Vietnamese);
            Assert.AreEqual("Vé tàu 76.500đ", Loc.T("Vé tàu ¥450", "x", "y"));
            Assert.AreEqual("Cần 30.600đ, có 1.700đ.", Loc.F("Cần ¥{0:N0}, có ¥{1:N0}.", "x {0} {1}", "y {0} {1}", 180, 10));
            Assert.AreEqual(Loc.Money(180) , "30.600đ"); Assert.AreEqual("Tổng 51.000đ", Loc.T("Tổng ¥300", "", ""));
            Loc.Set(GameLanguage.English);
            Assert.AreEqual("Ticket $4.50, you have $0.60", Loc.F("x", "Ticket ¥{0}, you have −¥{1}", "z", 450, 60).Replace("−", "") .Replace("have -", "have "));
            Assert.IsFalse(Loc.T("a", "Bought 1 (−¥160).", "c").Contains("¥"));
            Loc.Set(GameLanguage.Russian);
            Assert.IsFalse(Loc.Gloss("Giá ¥180", "Price ¥180").Contains("¥"));
            Loc.Set(GameLanguage.Japanese);
            Assert.AreEqual("¥180", Loc.T("v", "e", "¥180"), "Japanese keeps yen");
        }
    }
}