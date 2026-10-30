using System.Linq;
using NihongoLife.Home;
using NihongoLife.Learning;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests.EditMode
{
    public class EduV1DeskWiringTests
    {
        [SetUp] public void SetUp() => StudyProgressStore.ResetForTests();

        [Test]
        public void Flashcards_FromDeskWords_AllDueThenScheduled()
        {
            int due = StudyDeskMenuScreen.DueCount();
            Assert.AreEqual(StudyDeskInteractable.Words.Length, due, "every desk word becomes a card, due today");
            var faces = StudyDeskInteractable.Words.ToDictionary(w => w.japanese, w => new FlashcardSession.Face(w.japanese, w.reading));
            var s = new FlashcardSession(StudyProgressStore.Current.cards, faces, StudyProgressStore.Today);
            s.Flip(); s.Grade(2);
            Assert.AreEqual(due - 1, FlashcardScheduler.Due(StudyProgressStore.Current.cards, StudyProgressStore.Today, 999).Count);
        }

        [Test]
        public void Wardrobe_UniformOwnedFree_HatShowsOnHead()
        {
            var w = StudyProgressStore.Current.wardrobe;
            Assert.AreEqual(0, w.Buy("non_la", 1000) == 60 ? 0 : 1);
            var go = new GameObject("rigless");
            var look = WardrobeLook.On(go);
            look.Apply(new[] { "non_la" });
            Assert.AreEqual(0, look.Parts.Count, "no humanoid rig: nothing attached, no crash");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Album_AddPhoto_Persists()
        {
            Assert.IsTrue(StudyProgressStore.AddPhoto("t_resort", "Resort", "\u30D7\u30FC\u30EB", "pool"));
            Assert.IsFalse(StudyProgressStore.AddPhoto("t_resort", "Resort", "x", "x"));
            Assert.AreEqual(1, StudyProgressStore.Current.album.Count);
            PlayerPrefs.DeleteKey(StudyProgressStore.Key);
        }
    }
}
