using System.Linq;
using NihongoLife.Clinic;
using NihongoLife.Health;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards Resources/Clinic/medical_training.json: every case has one right diagnosis and one right treatment,
    /// every wrong answer is explained, red flags and a source are given, and treatment stays true to practice —
    /// an antibiotic is never the right answer for a virus.
    /// </summary>
    public class MedicalTrainingTests
    {
        [Test]
        public void RealCatalog_IsValid_AndCoversTheIllnesses()
        {
            var catalog = TrainingCatalog.Load();
            Assert.GreaterOrEqual(catalog.cases.Length, 8, "medical_training.json did not load");
            var illnesses = IllnessCatalog.Load();
            var errors = catalog.Validate(id => illnesses.Illness(id) != null);
            Assert.IsEmpty(errors, string.Join("\n", errors));
            Assert.Greater(catalog.knowledgePerCase, 0);
        }

        [Test]
        public void RealCatalog_NeverTreatsAVirusWithAntibiotics()
        {
            var illnesses = IllnessCatalog.Load();
            foreach (var c in TrainingCatalog.Load().cases.Where(c => illnesses.Illness(c.illness)?.category == "virus"))
                Assert.IsFalse(c.RightTreatment.ja.Contains("抗菌薬"), c.id + ": antibiotics do not treat a virus");
            var flu = TrainingCatalog.Load().cases.First(c => c.illness == "flu");
            StringAssert.Contains("48", flu.RightTreatment.vi, "Flu antivirals within 48 hours");
        }
    }
}
