using System.Linq;
using NihongoLife.Audio;
using NihongoLife.Clinic;
using NihongoLife.Health;
using NihongoLife.Shop;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards the health data (Resources/Health/illnesses.json): every illness has a cause, real treatment and
    /// prevention, symptoms the clinic knows, medicines sold at Hibari Mart; the music list points at real clips;
    /// the handbook's introduction loads.
    /// </summary>
    public class IllnessCatalogTests
    {
        [Test]
        public void RealCatalog_IsValid_AndMatchesTheClinic()
        {
            var catalog = IllnessCatalog.Load();
            Assert.GreaterOrEqual(catalog.illnesses.Length, 8, "illnesses.json did not load");
            var errors = catalog.Validate(ClinicCatalog.Load().symptoms.Select(s => s.id));
            Assert.IsEmpty(errors, string.Join("\n", errors));
            foreach (var id in new[] { "cold", "flu", "muscle_ache", "overwork", "heatstroke", "stomach", "hay_fever", "headache" })
                Assert.IsNotNull(catalog.Illness(id), id);
        }

        [Test]
        public void Treatment_FollowsRealPractice()
        {
            var catalog = IllnessCatalog.Load();
            var cold = catalog.Illness("cold");
            var flu = catalog.Illness("flu");
            Assert.Less(cold.relief, 1f, "Cold medicine only relieves symptoms");
            StringAssert.Contains("kháng sinh", cold.cureVi.ToLowerInvariant(), "Says antibiotics don't work for a cold");
            Assert.AreEqual(48f, flu.earlyHours, "Flu antivirals work best in the first 48 hours");
            Assert.Greater(flu.earlyClinicShorten, flu.clinicShorten, "Early treatment helps more");
            Assert.IsTrue(flu.stayHome, "Stay home with flu");
            Assert.Greater(flu.immunityHours, 0f, "A flu shot protects");
            Assert.AreEqual(1f, catalog.Illness("heatstroke").clinicShorten, 0.001f, "A drip cures heatstroke");
            Assert.IsTrue(catalog.Medicine("allergy_med").drowsy, "Older antihistamines make you drowsy");
        }

        [Test]
        public void Medicines_AreOnTheKonbiniShelf()
        {
            foreach (var m in IllnessCatalog.Load().medicines)
            {
                var product = KonbiniCatalog.Find(m.id);
                Assert.IsNotNull(product, m.id);
                Assert.AreEqual(KonbiniSection.Medicine, product.section);
                Assert.AreEqual(m.price, product.price);
            }
        }

        [Test]
        public void BrokenCatalog_IsRejected()
        {
            var broken = IllnessCatalog.Parse("{\"illnesses\":[{\"id\":\"x\",\"vi\":\"a\",\"en\":\"a\",\"kanji\":\"a\",\"hours\":0,\"symptoms\":[\"nope\"],\"medicines\":[\"pill\"],\"effect\":\"glow\",\"relief\":2,\"chance\":0.1}]}");
            var errors = broken.Validate(new[] { "fever" });
            foreach (var expected in new[] { "hours", "symptom nope", "unknown medicine", "unknown effect", "fractions", "chance", "cause" })
                Assert.IsTrue(errors.Any(e => e.Contains(expected)), "expected an error about " + expected + ":\n" + string.Join("\n", errors));
        }

        [Test]
        public void MusicList_PointsAtRealTracks()
        {
            var tracks = MusicCatalog.Load().tracks;
            Assert.GreaterOrEqual(tracks.Length, 15);
            foreach (var t in tracks)
            {
                Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Music/" + t.key), "clip " + t.key);
                Assert.IsFalse(string.IsNullOrEmpty(t.titleVi) || string.IsNullOrEmpty(t.credit), t.key);
            }
            foreach (var key in new[] { "clinic", "rain", "sick", "spring", "work" }) Assert.IsNotNull(MusicCatalog.Load().Track(key), key);
        }

        [Test]
        public void Handbook_IntroLoads()
        {
            var text = Resources.Load<TextAsset>("Guide/handbook");
            Assert.IsNotNull(text);
            StringAssert.Contains("\"intro\"", text.text);
            StringAssert.Contains("医師国家試験", text.text, "The doctor path names the national exam");
        }
    }
}
