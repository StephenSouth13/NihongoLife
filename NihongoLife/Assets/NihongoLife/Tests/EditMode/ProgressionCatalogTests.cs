using System.Linq;
using NihongoLife.Island;
using NihongoLife.Progression;
using NihongoLife.Shop;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Guards the editable progression data (Resources/Progression/progression.json): run after every edit.
    /// The real catalog must validate cleanly; deliberately broken catalogs must be rejected with clear messages.
    /// </summary>
    public class ProgressionCatalogTests
    {
        private static bool ItemExists(string id) =>
            IslandCatalog.Load().TryDescribe(id, out _, out _, out _) || KonbiniCatalog.Find(id) != null;

        [Test]
        public void RealCatalog_IsValid()
        {
            var catalog = ProgressionCatalog.Load();
            Assert.Greater(catalog.quests.Length, 0, "progression.json did not load");
            var errors = catalog.Validate(ItemExists);
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void RealCatalog_HasTheThreeStarterJobs_AndAFirstMoneyPath()
        {
            var catalog = ProgressionCatalog.Load();
            foreach (var id in new[] { "job_konbini_shift", "job_sushi_shift", "job_farm_shift" })
            {
                var job = catalog.Quest(id);
                Assert.NotNull(job, id);
                Assert.AreEqual("job", job.category);
                Assert.Greater(job.rewards.yen, 0, id + " must pay yen");
                Assert.IsTrue(job.objectives.Last().afterAll && job.objectives.Last().@event == "talk", id + " must end with reporting to the employer");
            }
            // A brand-new player (level 1, no knowledge) can always earn money.
            Assert.IsTrue(catalog.quests.Any(q => q.IsJob && q.minLevel <= 1 && q.minKnowledge == 0 && q.rewards.yen > 0));
        }

        [Test]
        public void Levels_AreDerivedFromTotalXp()
        {
            var catalog = ProgressionCatalog.Parse("{\"schema\":\"nihongolife.progression.v1\",\"levels\":{\"xpToNext\":[100,150]},\"quests\":[]}");
            Assert.AreEqual(1, catalog.LevelFor(0));
            Assert.AreEqual(1, catalog.LevelFor(99));
            Assert.AreEqual(2, catalog.LevelFor(100));
            Assert.AreEqual(3, catalog.LevelFor(250));
            Assert.AreEqual(250, catalog.XpForLevel(3));
            Assert.AreEqual(4, catalog.LevelFor(400), "past the table, the last step repeats");
        }

        [Test]
        public void BrokenCatalog_IsRejected()
        {
            const string json = @"{
              ""schema"": ""nihongolife.progression.v1"",
              ""levels"": { ""xpToNext"": [100, 0] },
              ""quests"": [
                { ""id"": ""a"", ""category"": ""job"", ""titleVi"": ""A"", ""titleEn"": ""A"", ""descriptionVi"": ""d"", ""minLevel"": 1,
                  ""requiresQuests"": [""b""], ""objectives"": [ { ""id"": ""x"", ""event"": ""fly"", ""target"": ""*"", ""count"": 1, ""textVi"": ""t"", ""textEn"": ""t"" } ],
                  ""rewards"": { ""yen"": -5, ""xp"": 1, ""knowledge"": 0 }, ""grantOnAccept"": [ { ""itemId"": ""no_such_item"", ""quantity"": 1 } ] },
                { ""id"": ""b"", ""category"": ""weird"", ""titleVi"": ""B"", ""titleEn"": """", ""descriptionVi"": ""d"", ""minLevel"": 1,
                  ""requiresQuests"": [""a""], ""objectives"": [ { ""id"": ""y"", ""event"": ""talk"", ""target"": ""npc"", ""count"": 0, ""afterAll"": true, ""textVi"": ""t"", ""textEn"": ""t"" } ],
                  ""rewards"": { ""yen"": 0, ""xp"": 0, ""knowledge"": 0 } },
                { ""id"": ""a"", ""category"": ""daily"", ""titleVi"": ""dup"", ""titleEn"": ""dup"", ""descriptionVi"": ""d"", ""minLevel"": 1,
                  ""objectives"": [ { ""id"": ""z"", ""event"": ""buy"", ""target"": ""*"", ""count"": 1, ""textVi"": ""t"", ""textEn"": ""t"" } ], ""rewards"": { ""yen"": 0, ""xp"": 0, ""knowledge"": 0 } }
              ]
            }";
            var errors = ProgressionCatalog.Parse(json).Validate(ItemExists);
            string all = string.Join("\n", errors);
            StringAssert.Contains("xpToNext[1] must be positive", all);
            StringAssert.Contains("duplicate quest id 'a'", all);
            StringAssert.Contains("unsupported event 'fly'", all);
            StringAssert.Contains("rewards must be present and non-negative", all);
            StringAssert.Contains("unknown category 'weird'", all);
            StringAssert.Contains("missing Vietnamese or English title", all);
            StringAssert.Contains("count must be ≥ 1", all);
            StringAssert.Contains("every objective is afterAll", all);
            StringAssert.Contains("circular requiresQuests", all);
            StringAssert.Contains("'no_such_item' is not a known item", all);
        }
    }
}
