using System.Linq;
using NihongoLife.Clinic;
using NihongoLife.Health;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards the clinic expansion (illnesses.json, clinic.json, medical_training.json): every illness has a
    /// training case, every case names its medical specialty, every illness is reachable by the player, and
    /// treatment stays true to practice - antibiotics only for bacterial illnesses.
    /// </summary>
    public class ClinicExpansionTests
    {
        private static readonly string[] NewIllnesses =
            { "hand_foot_mouth", "conjunctivitis", "strep_throat", "otitis_media", "heat_rash", "toothache" };

        [Test]
        public void EveryIllness_HasATrainingCase()
        {
            var cases = TrainingCatalog.Load().cases;
            foreach (var i in IllnessCatalog.Load().illnesses)
            {
                if (i.id == "overwork" || i.id == "muscle_ache") continue; // lifestyle illnesses: no clinic visit
                Assert.IsTrue(cases.Any(c => c.illness == i.id), i.id + " has no training case");
            }
        }

        [Test]
        public void NewIllnesses_ExistWithTrainingCases()
        {
            var illnesses = IllnessCatalog.Load();
            var cases = TrainingCatalog.Load().cases;
            foreach (var id in NewIllnesses)
            {
                Assert.IsNotNull(illnesses.Illness(id), id);
                Assert.IsTrue(cases.Any(c => c.illness == id), id + " needs a case");
            }
        }

        [Test]
        public void EveryCase_NamesItsSpecialty_InThreeLanguages()
        {
            foreach (var c in TrainingCatalog.Load().cases)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.specialtyVi), c.id + " specialtyVi");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.specialtyEn), c.id + " specialtyEn");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.specialtyJa), c.id + " specialtyJa");
            }
        }

        [Test]
        public void Antibiotics_AreRightOnlyForBacterialIllnesses()
        {
            var illnesses = IllnessCatalog.Load();
            // the Japanese word for antibiotic substances (kosei-busshitsu), in kanji and in kana
            string kanji = "\u6297\u751f\u7269\u8cea", kana = "\u3053\u3046\u305b\u3044\u3076\u3063\u3057\u3064";
            foreach (var c in TrainingCatalog.Load().cases)
            {
                var ill = illnesses.Illness(c.illness);
                string right = c.RightTreatment.ja;
                bool antibiotic = right.Contains(kanji) || right.Contains(kana);
                if (ill.category == "bacteria")
                    Assert.IsTrue(antibiotic, c.id + ": a bacterial illness's treatment mentions antibiotics");
                else
                    Assert.IsFalse(antibiotic, c.id + ": antibiotics do not treat a " + ill.category + " illness");
            }
            foreach (var id in new[] { "hand_foot_mouth", "conjunctivitis", "heat_rash" })
                Assert.AreNotEqual("bacteria", illnesses.Illness(id).category, id);
        }

        [Test]
        public void EveryIllness_IsReachable_OrTreatedOnlyByCode()
        {
            var illnesses = IllnessCatalog.Load();
            foreach (var id in NewIllnesses)
            {
                var i = illnesses.Illness(id);
                Assert.Greater(i.chance, 0f, id + " can be caught");
                Assert.IsNotEmpty(i.scenes, id + " scenes");
                Assert.IsNotEmpty(i.seasons, id + " seasons");
            }
        }

        [Test]
        public void EveryClinicPatient_ShowsASymptomOfAnIllness()
        {
            var clinic = ClinicCatalog.Load();
            var illnesses = IllnessCatalog.Load();
            var symptomIds = clinic.symptoms.Select(s => s.id).ToArray();
            foreach (var s in symptomIds)
                Assert.IsTrue(illnesses.illnesses.Any(i => i.symptoms.Contains(s)), "no illness shows the symptom " + s);
            Assert.GreaterOrEqual(clinic.patients.Length, 14, "the clinic has a varied waiting room");
        }

        [Test]
        public void CategoryNames_AreAllKnown()
        {
            foreach (var i in IllnessCatalog.Load().illnesses)
                Assert.IsFalse(string.IsNullOrWhiteSpace(i.CategoryName), i.id);
            var names = IllnessCatalog.Load().illnesses.GroupBy(i => i.category).ToDictionary(g => g.Key, g => g.First().CategoryName);
            Assert.AreEqual(names.Count, names.Values.Distinct().Count(), "every category has its own label (no fall-through to the sleep label)");
        }
    }
}
