using System.Linq;
using NihongoLife.PhuQuoc;
using NihongoLife.Progression;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards the Phú Quốc content: Gánh Ngon's menu, hours and pre-written service dialogues (ganh_ngon.json),
    /// the real-geography map (phu_quoc_map.json) and the restaurant / service career plugged into careers.json.
    /// </summary>
    public class PhuQuocRestaurantTests
    {
        [Test]
        public void GanhNgon_CatalogIsValid()
        {
            var c = GanhNgonCatalog.Load();
            Assert.Greater(c.dishes.Length, 0, "ganh_ngon.json did not load");
            var errors = c.Validate();
            Assert.IsEmpty(errors, string.Join("\n", errors));
            Assert.AreEqual("113 Trần Hưng Đạo, Dương Đông, Phú Quốc", c.address);
            foreach (var id in new[] { "seafood_bbq", "grilled_beef", "chicken_durian_hotpot", "fried_durian", "com_nieu", "cocktail" })
                Assert.IsNotNull(c.Dish(id), id);
        }

        [Test]
        public void GanhNgon_HoursAndLunchFollowTheRealRestaurant()
        {
            var c = GanhNgonCatalog.Load();
            Assert.IsFalse(c.IsOpen(9 * 60 + 59));
            Assert.IsTrue(c.IsOpen(10 * 60));
            Assert.IsTrue(c.IsOpen(22 * 60 + 59));
            Assert.IsFalse(c.IsOpen(23 * 60));
            Assert.IsTrue(c.MenuAt(12 * 60).Any(d => d.id == "com_nieu"), "cơm niêu at lunch");
            Assert.IsFalse(c.MenuAt(13 * 60).Any(d => d.id == "com_nieu"), "no cơm niêu after 13:00");
            Assert.IsTrue(c.MenuAt(20 * 60).Any(d => d.id == "chicken_durian_hotpot"), "dinner menu");
            Assert.IsEmpty(c.MenuAt(23 * 60 + 30).ToList(), "closed at night");
            Assert.AreEqual(9, c.vipRooms);
        }

        [Test]
        public void GanhNgon_ValidatorRejectsWildlifePricesAndBadDialogues()
        {
            var broken = GanhNgonCatalog.Parse("{\"schema\":\"x\",\"openMinute\":600,\"closeMinute\":1380,\"vipRooms\":9,\"dishes\":[{\"id\":\"snake_wine\",\"category\":\"grill\",\"nameVi\":\"Rắn nướng\",\"nameEn\":\"Grilled snake 200000 VND\",\"nameJa\":\"x\",\"readingJa\":\"x\"}],\"dialogues\":[{\"id\":\"d\",\"job\":\"waiter\",\"steps\":[{\"id\":\"s\",\"lineJa\":\"a\",\"readingJa\":\"a\",\"lineEn\":\"a\",\"lineVi\":\"a\",\"choices\":[{\"ja\":\"a\",\"en\":\"a\",\"vi\":\"a\",\"correct\":true}]}]}]}");
            var errors = broken.Validate();
            Assert.IsTrue(errors.Any(e => e.Contains("schema")));
            Assert.IsTrue(errors.Any(e => e.Contains("forbidden")), "wildlife / price words rejected");
            Assert.IsTrue(errors.Any(e => e.Contains("at least two answers")));
            Assert.IsTrue(errors.Any(e => e.Contains("no dialogue for job receptionist")));
        }

        [Test]
        public void ServiceDialogues_AreInEveryLanguage_AndTeachJapaneseAndEnglish()
        {
            var c = GanhNgonCatalog.Load();
            foreach (var job in GanhNgonCatalog.Jobs) Assert.IsTrue(c.DialoguesFor(job).Any(), job);
            Assert.IsTrue(c.dialogues.Any(d => d.guestLanguage == "en"), "English practice");
            Assert.IsTrue(c.dialogues.Any(d => d.guestLanguage == "ja"), "Japanese practice");
            Assert.IsTrue(c.dialogues.SelectMany(d => d.steps).Any(s => s.questEvent == "order"));
            Assert.IsTrue(c.dialogues.SelectMany(d => d.steps).Any(s => s.questEvent == "serve"));
            foreach (var p in new[] { "welcome", "certainly", "enjoy", "thanks" }) Assert.IsTrue(c.phrases.Any(x => x.id == p), p);
        }

        [Test]
        public void Shift_RotatesDialogues_AndAnswersInTheGuestsLanguage()
        {
            var c = GanhNgonCatalog.Load();
            GanhNgonShift.Reset();
            Assert.AreEqual("receptionist", GanhNgonShift.JobOf(GanhNgonStation.Reception));
            Assert.AreEqual("chef", GanhNgonShift.JobOf(GanhNgonStation.Kitchen));
            var first = GanhNgonShift.NextDialogue(c, GanhNgonStation.Table, 0);
            var second = GanhNgonShift.NextDialogue(c, GanhNgonStation.Table, 1);
            Assert.AreNotEqual(first.id, second.id, "two waiter dialogues rotate");
            var en = c.dialogues.First(d => d.guestLanguage == "en");
            var choices = GanhNgonShift.Choices(en, en.steps[0]);
            Assert.AreEqual(1, choices.Count(x => x.Correct));
            Assert.AreEqual(en.steps[0].choices.First(x => x.correct).en, choices.First(x => x.Correct).Label, "English guest → English answer");
            var ja = c.dialogues.First(d => d.guestLanguage == "ja");
            Assert.AreEqual(ja.steps[0].choices.First(x => x.correct).ja, GanhNgonShift.Choices(ja, ja.steps[0]).First(x => x.Correct).Label);
        }

        [Test]
        public void Map_IsValid_AndFaithfulToRealGeography()
        {
            var m = PhuQuocMap.Load();
            var errors = m.Validate();
            Assert.IsEmpty(errors, string.Join("\n", errors));
            Assert.AreEqual(100f, m.metersPerUnit);
            var outline = m.OutlineWorld();
            float ns = outline.Max(p => p.y) - outline.Min(p => p.y);
            Assert.That(ns, Is.InRange(420f, 540f), "≈50 km north–south at 1 unit = 100 m");
            Vector3 P(string id) => m.ToWorld(m.Place(id));
            Assert.Less(Vector3.Distance(P("ganh_ngon"), P("dinh_cau")), 6f, "Gánh Ngon is in Dương Đông, by Dinh Cậu");
            Assert.Less(Vector3.Distance(P("ganh_ngon"), P("cho_dem")), 3f, "the night market is a short walk");
            Assert.Less(P("hon_thom").z, P("an_thoi").z, "Hòn Thơm is south of An Thới");
            Assert.Greater(P("bai_dai").z, P("duong_dong").z, "Bãi Dài is north…");
            Assert.Less(P("bai_dai").x, P("duong_dong").x, "…and west of Dương Đông");
            Assert.Greater(P("ham_ninh").x, P("duong_dong").x, "Hàm Ninh is on the east coast");
            Assert.Greater(P("bai_sao").x, P("an_thoi").x, "Bãi Sao is east of An Thới");
            Assert.IsFalse(m.OnIsland(10.2, 103.7), "open sea is not island");
            foreach (var id in new[] { "duong_dong", "cho_dem", "dinh_cau", "bai_sao", "bai_dai", "cable_car", "an_thoi", "sunset_town", "grand_world", "vinwonders", "ham_ninh", "ganh_dau", "prison", "fish_sauce", "pepper_farm", "suoi_tranh", "national_park", "airport" })
                Assert.IsNotNull(m.Place(id), id);
        }

        [Test]
        public void HospitalityCareer_IsPluggedIntoCareerOrientation()
        {
            var careers = CareerCatalog.Load();
            var h = careers.Career("hospitality");
            Assert.IsNotNull(h, "careers.json has the restaurant / service line");
            Assert.AreEqual("job_ganhngon_shift", h.shiftQuest);
            Assert.AreEqual(6, h.MaxLevel);
            CollectionAssert.AreEquivalent(new[] { "kitchen", "manager" }, h.branches.Select(b => b.id).ToArray());
            Assert.GreaterOrEqual(h.workStart, 10, "inside opening hours");
            Assert.LessOrEqual(h.workEnd, 23);
            var quest = ProgressionCatalog.Load().Quest("job_ganhngon_shift");
            Assert.IsNotNull(quest);
            Assert.AreEqual("65_PhuQuocIsland", quest.scene);
            Assert.AreEqual(GanhNgonShift.ManagerId, quest.giverNpc);
        }
    }
}
