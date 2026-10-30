using System.Collections.Generic;
using NihongoLife.Learning;
using NUnit.Framework;

namespace NihongoLife.Tests.EditMode
{
    public class FlashcardSchedulerTests
    {
        [Test]
        public void Good_Grows_Again_Resets()
        {
            var c = new FlashcardState { id = "neko" };
            FlashcardScheduler.Review(c, 2, 1); Assert.AreEqual(2, c.dueDay);
            FlashcardScheduler.Review(c, 2, 2); Assert.AreEqual(8, c.dueDay);
            FlashcardScheduler.Review(c, 2, 8); Assert.Greater(c.intervalDays, 6);
            FlashcardScheduler.Review(c, 0, 20);
            Assert.AreEqual(0, c.repetitions); Assert.AreEqual(20, c.dueDay); Assert.AreEqual(1, c.lapses);
            Assert.GreaterOrEqual(c.ease, FlashcardScheduler.MinEase);
        }

        [Test]
        public void Due_IsSortedAndCapped()
        {
            var cards = new List<FlashcardState>();
            for (int i = 0; i < 30; i++) cards.Add(new FlashcardState { id = "c" + i, dueDay = 30 - i });
            var due = FlashcardScheduler.Due(cards, 25, 10);
            Assert.AreEqual(10, due.Count);
            Assert.AreEqual(1, due[0].dueDay);
        }
    }
}
