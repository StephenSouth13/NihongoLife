using System.Linq;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.Save;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    /// <summary>Online/offline merge (sign-in sync): no field is dropped and rewards/money cannot duplicate.</summary>
    public class ProgressMergeTests
    {
        [Test]
        public void Merge_KeepsBagIslandAndQuests_WithoutDuplicatingRewards()
        {
            var local = new PlayerProgressDto { yen = 900 };
            local.inventory.Add(new InventoryEntry { itemId = "carrot", quantity = 4 });
            local.island.words.Add("crop:carrot"); local.island.harvested = 2;
            local.quests.Add(new QuestStateRecord { questId = "job_konbini_shift", status = "completed", timesCompleted = 1, rewardClaimed = true });
            local.quests.Add(new QuestStateRecord { questId = "job_farm_shift", status = "active", acceptedTicks = 50 });

            var cloud = new PlayerProgressDto { yen = 1500 };
            cloud.inventory.Add(new InventoryEntry { itemId = "onigiri_sake", quantity = 1 });
            // The same konbini shift is still "active" in the older cloud copy: the claimed local run must win.
            cloud.quests.Add(new QuestStateRecord { questId = "job_konbini_shift", status = "active", timesCompleted = 0, acceptedTicks = 10 });

            var merged = SupabaseProgressRepository.MergeProgress(local, cloud);

            Assert.AreEqual(1500, merged.yen, "wallet follows the cloud");
            Assert.AreEqual("onigiri_sake", merged.inventory.Single().itemId, "the bag follows the same save as the wallet");
            Assert.AreEqual(2, merged.island.harvested, "the island save with more progress is kept");
            var konbini = merged.quests.Single(q => q.questId == "job_konbini_shift");
            Assert.AreEqual("completed", konbini.status);
            Assert.IsTrue(konbini.rewardClaimed, "a reward claimed on one device stays claimed");
            Assert.AreEqual("active", merged.quests.Single(q => q.questId == "job_farm_shift").status, "quests only on one side are kept");
        }

        [Test]
        public void Merge_EmptyCloud_KeepsLocalBag()
        {
            var local = new PlayerProgressDto();
            local.inventory.Add(new InventoryEntry { itemId = "tea", quantity = 1 });
            var merged = SupabaseProgressRepository.MergeProgress(local, new PlayerProgressDto());
            Assert.AreEqual("tea", merged.inventory.Single().itemId);
        }
    }
}
