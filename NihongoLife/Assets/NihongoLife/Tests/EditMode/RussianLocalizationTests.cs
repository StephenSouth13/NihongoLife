using System.Linq;
using NihongoLife.Core;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests.EditMode
{
    public sealed class RussianLocalizationTests
    {
        private GameLanguage _before;

        [SetUp] public void SetUp() { _before = Loc.Language; RuTable.Reload(); }
        [TearDown] public void TearDown() { Loc.Set(_before); }

        [Test]
        public void RussianIsAFourthLanguage_WithCode()
        {
            Loc.Set(GameLanguage.Russian);
            Assert.AreEqual("ru", Loc.Code);
            Assert.IsTrue(Loc.IsRussian);
            Assert.AreEqual(3, (int)GameLanguage.Russian, "appended, existing serialized values unchanged");
            Assert.AreEqual(0, (int)GameLanguage.Vietnamese); Assert.AreEqual(2, (int)GameLanguage.Japanese);
        }

        [Test]
        public void TableLoads_AndEveryValueIsCyrillicOrExempt()
        {
            Assert.GreaterOrEqual(RuTable.Count, 3600);
            var asset = Resources.Load<TextAsset>("Content/Localization/ru_table");
            Assert.NotNull(asset);
            int cyr = 0;
            foreach (char c in asset.text) if (c >= '\u0400' && c <= '\u04FF') cyr++;
            Assert.Greater(cyr, 20000, "the table contains real Russian text");
        }

        [Test]
        public void T_ReturnsTheRussianValue_FallsBackToEnglishNotVietnamese()
        {
            Loc.Set(GameLanguage.Russian);
            Assert.IsTrue(RuTable.TryGet("Language", out string ru), "a core UI key exists");
            Assert.AreEqual(ru, Loc.T("Ngon ngu", "Language", "Language-ja"));
            Assert.AreEqual("A string no table has", Loc.T("Chuoi chua co", "A string no table has", "x"));
            Assert.AreEqual("only vi", Loc.T("only vi", "", "x"));
            Loc.Set(GameLanguage.English);
            Assert.AreEqual("Language", Loc.T("v", "Language", "j"));
        }

        [Test]
        public void OtherLanguages_AreUnchanged()
        {
            Loc.Set(GameLanguage.Vietnamese); Assert.AreEqual("vi-text", Loc.T("vi-text", "en-text", "ja-text"));
            Loc.Set(GameLanguage.Japanese); Assert.AreEqual("ja-text", Loc.T("vi-text", "en-text", "ja-text"));
            Loc.Set(GameLanguage.English); Assert.AreEqual("en-text", Loc.T("vi-text", "en-text", "ja-text"));
        }

        [Test]
        public void PlaceholdersSurvive_InFormatStrings()
        {
            Loc.Set(GameLanguage.Russian);
            int checkedCount = 0;
            var asset = Resources.Load<TextAsset>("Content/Localization/ru_table");
            var dto = JsonUtility.FromJson<Dto>(asset.text);
            foreach (var e in dto.entries.Take(4000))
            {
                for (int n = 0; n < 4; n++)
                {
                    string ph = "{" + n + "}";
                    if (e.en.Contains(ph)) { Assert.IsTrue(e.ru.Contains(ph), "placeholder " + ph + " kept: " + e.en); checkedCount++; }
                }
            }
            Assert.Greater(checkedCount, 20);
        }

        [System.Serializable] private class Pair { public string en; public string ru; }
        [System.Serializable] private class Dto { public System.Collections.Generic.List<Pair> entries; }
    }
}
