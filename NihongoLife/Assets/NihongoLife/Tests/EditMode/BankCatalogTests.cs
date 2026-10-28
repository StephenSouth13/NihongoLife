using System.Linq;
using NihongoLife.Bank;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards Resources/Bank/bank.json: an interest-free emergency fund for a broke player, a personal loan whose fee
    /// stays within Japan's legal cap, and late rules that are firm but never take everything (garnish below half,
    /// a capped late fee, a grace period, a free extension).
    /// </summary>
    public class BankCatalogTests
    {
        [Test]
        public void RealCatalog_HasASafetyNetAndALoan()
        {
            var c = BankCatalog.Load();
            Assert.Greater(c.products.Length, 1, "bank.json did not load");
            Assert.AreEqual(c.products.Length, c.products.Select(p => p.id).Distinct().Count(), "product ids are unique");
            var emergency = c.Product("emergency");
            Assert.NotNull(emergency, "an emergency fund exists");
            Assert.AreEqual(0, emergency.interestPercent, "the emergency fund is interest free");
            Assert.AreEqual(1, emergency.minLevel, "open from the first level");
            Assert.Greater(emergency.onlyBelowYen, 0, "only for a nearly empty wallet");
            Assert.IsFalse(emergency.needsCredit, "no credit check for food money");
            foreach (var p in c.products)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(p.vi) || string.IsNullOrWhiteSpace(p.en) || string.IsNullOrWhiteSpace(p.ja), p.id + " is named in three languages");
                Assert.IsFalse(string.IsNullOrWhiteSpace(p.noteVi) || string.IsNullOrWhiteSpace(p.noteEn) || string.IsNullOrWhiteSpace(p.noteJa), p.id + " explains itself");
                Assert.Greater(p.maxYen, 0, p.id);
                Assert.Greater(p.termDays, 0, p.id);
                Assert.LessOrEqual(p.interestPercent, 15, p.id + ": within the Interest Rate Restriction Act");
            }
        }

        [Test]
        public void RealCatalog_LateRulesNeverTakeEverything()
        {
            var c = BankCatalog.Load();
            Assert.Greater(c.graceDays, 0, "a grace period before anything happens");
            Assert.Greater(c.remindDaysBefore, 0, "a reminder before the due day");
            Assert.That(c.garnishPercent, Is.InRange(1, 50), "the player keeps most of each income");
            Assert.That(c.lateFeeCapPercent, Is.InRange(1, 20), "late fees are capped");
            Assert.LessOrEqual(c.lateFeePercent, c.lateFeeCapPercent);
            Assert.Greater(c.extensionDays, 0, "a free extension exists");
            Assert.That(c.creditStart, Is.InRange(c.creditMinForCard, c.creditMax), "a new player can take a personal loan");
        }
    }
}
