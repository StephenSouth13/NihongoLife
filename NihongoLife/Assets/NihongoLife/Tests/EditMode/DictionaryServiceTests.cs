using System.Linq;
using NihongoLife.Content;
using NUnit.Framework;

namespace NihongoLife.Tests.EditMode
{
    public sealed class DictionaryServiceTests
    {
        [SetUp] public void SetUp() { DictionaryService.ClearCache(); }

        [Test]
        public void AllTwelveDirections_Load_WithHeadwords()
        {
            int pairs = 0;
            foreach (var from in DictionaryService.Languages)
                foreach (var to in DictionaryService.Languages)
                {
                    if (!DictionaryService.Supports(from, to)) continue;
                    Assert.Greater(DictionaryService.HeadwordCount(from, to), 1000, from + "-" + to);
                    pairs++;
                }
            Assert.AreEqual(12, pairs);
        }

        [Test]
        public void JapaneseLookup_FindsByWord_ByKanaReading_AndKatakanaFold()
        {
            var byWord = DictionaryService.Search("ja", "vi", "ATM");
            Assert.IsNotEmpty(byWord);
            Assert.IsTrue(byWord.Any(h => h.Headword == "ATM" && h.Meaning.ToLowerInvariant().Contains("atm")));
            var byReading = DictionaryService.Search("ja", "vi", "\u30A8\u30FC\u30C6\u30A3\u30FC\u30A8\u30E0"); // katakana reading
            Assert.IsTrue(byReading.Any(h => h.Headword == "ATM"), "katakana query is folded to the hiragana reading index");
            Assert.IsTrue(DictionaryService.Search("ja", "vi", "\u3048\u30FC\u3066\u3043\u30FC\u3048\u3080").Any(h => h.Headword == "ATM"), "hiragana query finds it too");
        }

        [Test]
        public void RussianToVietnamese_AndReverse_AreConsistent()
        {
            var first = DictionaryService.Search("vi", "ru", "xin ch\u00E0o");
            Assert.IsNotEmpty(first, "a basic greeting exists vi->ru");
            var ruWord = first[0].Meaning;
            Assert.IsTrue(ruWord.Any(c => c >= '\u0400' && c <= '\u04FF'), "the Russian side is Cyrillic: " + ruWord);
            var back = DictionaryService.Search("ru", "vi", ruWord.Split(',', ';')[0].Trim());
            Assert.IsNotEmpty(back, "reverse direction finds it");
        }

        [Test]
        public void EmptyAndUnsupported_ReturnNothing_AndLimitIsHonoured()
        {
            Assert.IsEmpty(DictionaryService.Search("vi", "ru", "   "));
            Assert.IsEmpty(DictionaryService.Search("vi", "vi", "a"));
            Assert.LessOrEqual(DictionaryService.Search("en", "vi", "a", 5).Count, 5);
        }
    }
}
