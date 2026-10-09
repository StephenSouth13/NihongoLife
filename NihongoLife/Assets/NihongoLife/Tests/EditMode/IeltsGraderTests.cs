using System.Collections.Generic;
using System.Linq;
using NihongoLife.Exam.Ielts;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>IELTS marking rules on synthetic content (no licensed exam text in the repository).</summary>
    public class IeltsGraderTests
    {
        [Test]
        public void OptionalParts_Expand()
        {
            CollectionAssert.AreEquivalent(new[] { "red", "reddish" }, IeltsGrader.Expand("red(dish)").ToArray());
            CollectionAssert.AreEquivalent(new[] { "12", "12 dollars" }, IeltsGrader.Expand("12 (dollars)").ToArray());
            CollectionAssert.AreEquivalent(new[] { "3 may", "3rd may" }, IeltsGrader.Expand("3(rd) May").ToArray());
        }

        [Test]
        public void Matching_IgnoresCaseCurrencyAndDigitSpacing_ButNotSpelling()
        {
            Assert.IsTrue(IeltsGrader.Matches("  River ", new[] { "river" }));
            Assert.IsTrue(IeltsGrader.Matches("£40", new[] { "40 (pounds)" }));
            Assert.IsTrue(IeltsGrader.Matches("40 pounds", new[] { "40 (pounds)" }));
            Assert.IsTrue(IeltsGrader.Matches("1234567", new[] { "123 4567" }));
            Assert.IsFalse(IeltsGrader.Matches("rivr", new[] { "river" }));
            Assert.IsFalse(IeltsGrader.Matches("", new[] { "river" }));
        }

        [Test]
        public void WordLimits_AreEnforced()
        {
            Assert.IsTrue(IeltsGrader.WithinLimit("harbour", "ONE_WORD"));
            Assert.IsFalse(IeltsGrader.WithinLimit("the harbour", "ONE_WORD"));
            Assert.IsTrue(IeltsGrader.WithinLimit("5th June", "ONE_WORD_AND_OR_NUMBER"));
            Assert.IsTrue(IeltsGrader.WithinLimit("123 4567", "ONE_WORD_AND_OR_NUMBER"));
            Assert.IsFalse(IeltsGrader.WithinLimit("old blue box", "ONE_WORD_AND_OR_NUMBER"));
        }

        [Test]
        public void Grade_ScoresEitherOrderSets_LimitsAndBand()
        {
            var test = new IeltsTest
            {
                id = "synthetic", skill = "listening",
                parts = new[]
                {
                    new IeltsPart { number = 1, groups = new[]
                    {
                        new IeltsGroup { type = "completion", from = 1, to = 2, limit = "ONE_WORD", lines = new[] { new IeltsLine { text = "{1} and {2}" } } },
                        new IeltsGroup { type = "mcq_multi", from = 3, to = 4, choose = 2 },
                        new IeltsGroup { type = "matching", from = 5, to = 5 },
                    } },
                },
            };
            var key = new IeltsKey
            {
                testId = "synthetic",
                answers = new[]
                {
                    new IeltsKeyAnswer { number = 1, accepted = new[] { "colour" } },
                    new IeltsKeyAnswer { number = 2, accepted = new[] { "harbour" } },
                    new IeltsKeyAnswer { number = 3, accepted = new[] { "B", "D" }, set = new[] { 3, 4 } },
                    new IeltsKeyAnswer { number = 4, accepted = new[] { "B", "D" }, set = new[] { 3, 4 } },
                    new IeltsKeyAnswer { number = 5, accepted = new[] { "C" } },
                },
            };
            var answers = new Dictionary<int, string> { [1] = "Colour", [2] = "the harbour", [3] = "D", [4] = "B", [5] = "c" };
            var result = IeltsGrader.Grade(test, key, answers);
            Assert.AreEqual(5, result.Max);
            Assert.AreEqual(4, result.Raw, "Q2 is over the one-word limit; the either-order pair scores in any order.");
            Assert.IsTrue(result.Items.Single(i => i.Number == 2).OverLimit);

            answers[4] = "D"; // the same letter twice only counts once
            Assert.AreEqual(3, IeltsGrader.Grade(test, key, answers).Raw);
        }

        [Test]
        public void ListeningBand_FollowsPublishedConversion()
        {
            Assert.AreEqual(9f, IeltsGrader.ListeningBand(39));
            Assert.AreEqual(7f, IeltsGrader.ListeningBand(30));
            Assert.AreEqual(6.5f, IeltsGrader.ListeningBand(26));
            Assert.AreEqual(5.5f, IeltsGrader.ListeningBand(18));
            Assert.AreEqual(4f, IeltsGrader.ListeningBand(10));
        }
    }
}
