using System.Collections.Generic;
using NihongoLife.Home;
using NihongoLife.Learning;
using NUnit.Framework;

namespace NihongoLife.Tests.EditMode
{
    public class EduV1DeskScaffoldTests
    {
        [Test]
        public void Session_FlipBeforeGrade_AgainRequeues()
        {
            var cards = new List<FlashcardState> { new FlashcardState { id = "neko" }, new FlashcardState { id = "inu" } };
            var faces = new Dictionary<string, FlashcardSession.Face> { ["neko"] = new FlashcardSession.Face("neko", "cat") };
            var s = new FlashcardSession(cards, faces, 0);
            Assert.AreEqual(2, s.Remaining);
            Assert.IsFalse(s.Grade(2), "no grading blind");
            s.Flip(); Assert.AreEqual("cat", s.Back);
            Assert.IsTrue(s.Grade(0)); Assert.AreEqual(2, s.Remaining, "Again goes to the back");
            s.Flip(); s.Grade(2); s.Flip(); s.Grade(3);
            Assert.IsTrue(s.Done); Assert.AreEqual(3, s.Reviewed); Assert.AreEqual(1, s.Again);
        }

        [Test]
        public void DeskMenu_UnlocksWithProgress()
        {
            var m = StudyDeskMenu.Build(0, 0, 0, false);
            Assert.AreEqual(5, m.Count);
            Assert.IsTrue(m[0].unlocked); Assert.IsFalse(m[1].unlocked); Assert.IsFalse(m[2].unlocked);
            m = StudyDeskMenu.Build(40, 3, 1, true);
            Assert.IsTrue(m.TrueForAll(e => e.unlocked));
            Assert.AreEqual("20+", StudyDeskMenu.DueBadge(25)); Assert.AreEqual("", StudyDeskMenu.DueBadge(0));
        }

        [Test]
        public void Album_NoDuplicates_NewestFirst()
        {
            var a = new StudyAlbum();
            Assert.IsTrue(a.Add("dinh_cau", "DuongDong", "umi", "sea", 1));
            Assert.IsFalse(a.Add("dinh_cau", "DuongDong", "umi", "sea", 2));
            a.Add("resort_pool", "Resort", "puuru", "pool", 3);
            Assert.AreEqual("resort_pool", a.pages[0].id); Assert.AreEqual(1, a.ByPlace("Resort").Count);
        }

        [Test]
        public void Wardrobe_BuyEquip_UniformReplacesTopBottom()
        {
            var w = new WardrobeCatalog();
            Assert.AreEqual(0, w.Buy("yukata", 100), "too poor");
            Assert.AreEqual(600, w.Buy("yukata", 1000));
            w.Buy("shorts", 1000); w.Buy("server_uniform", 0);
            Assert.IsTrue(w.Equip("yukata")); Assert.IsTrue(w.Equip("shorts"));
            Assert.IsTrue(w.Equip("server_uniform"));
            CollectionAssert.AreEqual(new[] { "server_uniform" }, w.equipped);
            Assert.IsFalse(w.Equip("ao_dai"), "not owned");
        }
    }
}
