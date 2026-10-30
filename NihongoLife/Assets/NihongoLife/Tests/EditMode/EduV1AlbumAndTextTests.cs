using NUnit.Framework;
using NihongoLife.Core;
using NihongoLife.Learning;
using NihongoLife.UI;

namespace NihongoLife.Tests.EditMode
{
    public class EduV1AlbumAndTextTests
    {
        [SetUp] public void SetUp() => StudyProgressStore.ResetForTests();

        [Test] public void EveryKey_HasFourLanguages()
        {
            foreach (var k in EduUiText.Keys)
                foreach (GameLanguage l in new[] { GameLanguage.Vietnamese, GameLanguage.English, GameLanguage.Japanese, GameLanguage.Russian })
                    Assert.IsFalse(string.IsNullOrEmpty(EduUiText.Get(k, l)), k + " " + l);
        }

        [Test] public void PlaceVisit_AddsOncePerPlace_AndSkipsMenus()
        {
            Assert.IsTrue(AlbumRecorder.RecordPlace("65_PhuQuocIsland"));
            Assert.IsFalse(AlbumRecorder.RecordPlace("65_PhuQuocIsland"));
            Assert.IsFalse(AlbumRecorder.RecordPlace("01_MainMenu"));
            Assert.AreEqual(1, StudyProgressStore.Current.album.Count);
        }

        [Test] public void WordMilestones_AddPages()
        {
            StudyProgressStore.AddWordsSeen(12);
            StudyProgressStore.AddWordsSeen(40);
            Assert.AreEqual(2, StudyProgressStore.Current.album.Count); // 10 and 50
        }
    }
}
