using System.Linq;
using NihongoLife.English;
using NUnit.Framework;

namespace NihongoLife.Tests.EditMode
{
    public sealed class EnglishCourseTests
    {
        [Test]
        public void StarterCatalog_CoversA1ToC1AndAllActivityTypes()
        {
            var lessons = EnglishCourseCatalog.All;
            CollectionAssert.AreEquivalent(new[] { "A1", "A2", "B1", "B2", "C1" }, lessons.Select(x => x.level).Distinct().ToArray());
            foreach (string level in new[] { "A1", "A2", "B1", "B2", "C1" })
                CollectionAssert.AreEquivalent(new[] { EnglishSkill.Listening, EnglishSkill.Speaking, EnglishSkill.Reading, EnglishSkill.Writing },
                    lessons.Where(x => x.level == level).Select(x => x.skill).ToArray(), level + " must cover all four skills");
            CollectionAssert.AreEquivalent(
                new[] { EnglishActivityType.SentencePatterns, EnglishActivityType.Grammar, EnglishActivityType.GapFill, EnglishActivityType.Reading, EnglishActivityType.Listening },
                lessons.Select(x => x.type).Distinct().ToArray());
            Assert.AreEqual(lessons.Count, lessons.Select(x => x.id).Distinct().Count(), "Lesson ids must be unique");
        }

        [Test]
        public void StarterCatalog_QuestionsArePlayableAndOriginalMetadataIsComplete()
        {
            Assert.AreEqual(20, EnglishCourseCatalog.All.Count, "Five CEFR levels × four skills");
            foreach (var lesson in EnglishCourseCatalog.All)
            {
                Assert.IsNotEmpty(lesson.titleVi, lesson.id);
                Assert.IsNotEmpty(lesson.titleEn, lesson.id);
                Assert.IsNotEmpty(lesson.objective, lesson.id);
                Assert.GreaterOrEqual(lesson.questions.Count, 3, lesson.id);
                foreach (var q in lesson.questions)
                {
                    Assert.IsNotEmpty(q.prompt, lesson.id);
                    Assert.GreaterOrEqual(q.choices.Length, 2, q.prompt);
                    Assert.That(q.correctIndex, Is.InRange(0, q.choices.Length - 1), q.prompt);
                    Assert.IsNotEmpty(q.explanationVi, q.prompt);
                    if (lesson.type == EnglishActivityType.Listening) Assert.IsNotEmpty(q.spokenText, q.prompt);
                }
            }
        }
    }
}
