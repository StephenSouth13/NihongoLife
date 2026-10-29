using System.Linq;
using NihongoLife.Gestures;
using NihongoLife.Player;
using NihongoLife.Progression;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests.EditMode
{
    public class CareerIndustryTests
    {
        [SetUp]
        public void SetUp() { CareerIndustries.Reset(); CharacterVariants.Reset(); GestureLibrary.Reset(); }

        [Test]
        public void EveryCareer_HasALook_AUniformPerson_AndKnownGestures()
        {
            var catalog = CareerCatalog.Load();
            Assert.GreaterOrEqual(catalog.careers.Length, 6);
            foreach (var career in catalog.careers)
            {
                var look = CareerIndustries.Look(career.id);
                Assert.NotNull(look, career.id + " has a look");
                var spec = CharacterVariants.Find(look.variant);
                Assert.NotNull(spec, career.id + ": variant " + look.variant);
                Assert.NotNull(CharacterVariants.Prefab(look.variant), career.id + ": prefab for " + look.variant);
                Assert.IsTrue(spec.kid.StartsWith("kid_"), "Kids family: " + spec.kid);
                Assert.IsNotNull(GestureLibrary.Find(look.gesture), career.id + ": gesture " + look.gesture);
                Assert.IsNotNull(GestureLibrary.Find(look.alt), career.id + ": alt " + look.alt);
                Assert.IsNotNull(CareerIndustries.IndustryOf(career.id), career.id + " belongs to an industry");
                Assert.AreEqual(look.industry, CareerIndustries.IndustryOf(career.id).id, career.id + ": look and industry agree");
            }
        }

        [Test]
        public void Industries_HaveFourLanguages_AColour_AndNoCareerTwice()
        {
            var f = CareerIndustries.File;
            Assert.GreaterOrEqual(f.industries.Length, 5);
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var i in f.industries)
            {
                foreach (var s in new[] { i.nameVi, i.nameEn, i.nameJa, i.nameRu, i.blurbVi, i.blurbEn, i.blurbJa, i.blurbRu })
                    Assert.IsFalse(string.IsNullOrWhiteSpace(s), i.id + " is named and described in vi/en/ja/ru");
                Assert.IsTrue(ColorUtility.TryParseHtmlString(i.color, out _), i.id + " colour");
                foreach (var c in i.careers) Assert.IsTrue(seen.Add(c), c + " is listed once");
            }
        }

        [Test]
        public void Group_CoversEveryCareer_InIndustryOrder()
        {
            var catalog = CareerCatalog.Load();
            var groups = CareerIndustries.Group(catalog.careers);
            var all = groups.SelectMany(g => g.careers).Select(c => c.id).ToList();
            CollectionAssert.AreEquivalent(catalog.careers.Select(c => c.id), all, "No career hidden, none repeated");
            Assert.GreaterOrEqual(groups.Count, 4, "Several industries");
            Assert.AreEqual("commerce", groups[0].industry.id);
            Assert.IsTrue(groups.First(g => g.industry.id == "foodservice").careers.Select(c => c.id).Contains("chef"));
        }

        [Test]
        public void UniformVariants_AreDistinctPeople_ForTheNewJobs()
        {
            foreach (var u in new[] { "konbini", "farmer", "sushi", "musician", "producer", "painter" })
            {
                Assert.NotNull(CharacterVariants.File.Uniform(u), u + " uniform");
                var people = CharacterVariants.File.variants.Where(v => v.uniform == u).ToList();
                Assert.GreaterOrEqual(people.Count, 2, u + " has two people");
                Assert.AreEqual(people.Count, people.Select(p => p.person).Distinct().Count(), u + ": distinct names");
                Assert.Greater(CharacterVariants.File.Uniform(u).pieces.Length, 2, u + " has pieces");
                foreach (var p in people) Assert.NotNull(CharacterVariants.Prefab(p.id), p.id + " prefab built");
            }
        }
    }
}