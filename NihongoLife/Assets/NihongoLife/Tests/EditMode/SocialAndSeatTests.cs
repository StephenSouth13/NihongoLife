using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.World;
using NUnit.Framework;

namespace NihongoLife.Tests
{
    public class SocialAndSeatTests
    {
        [SetUp]
        public void Reset() => SocialStanding.ResetForTests();

        [Test]
        public void HipTarget_LandsOnTheCushion_ClearOfTheWall()
        {
            Vector3 hip = SeatAnchor.HipTarget(1.35f, "NL_kid_sakura");
            Assert.GreaterOrEqual(hip.y, SeatAnchor.HipMinY);
            Assert.LessOrEqual(hip.y, SeatAnchor.HipMaxY);
            Assert.GreaterOrEqual(hip.z, SeatAnchor.HipRearLimit);
            Assert.LessOrEqual(hip.z, SeatAnchor.HipFrontLimit);
            Assert.IsTrue(SeatAnchor.TalkVolumeClearsWall());
        }

        [Test]
        public void SolvedHipZ_PullsABodyThatSticksThroughTheWallForward()
        {
            float z = SeatAnchor.SolvedHipZ(0f, -0.8f, -0.04f);
            Assert.Greater(z, 0.2f);
            float clear = SeatAnchor.SolvedHipZ(0f, -0.1f, -0.04f);
            Assert.AreEqual(-0.04f, clear, 0.001f);
        }

        [Test]
        public void RigBias_DiffersForKidAndClerk()
        {
            Vector3 kid = SeatAnchor.HipTarget(1.35f, "NL_Neighbor");
            Vector3 clerk = SeatAnchor.HipTarget(1.35f, "NL_Cashier");
            Assert.Less(kid.y, clerk.y);
        }

        [Test]
        public void Moodlet_Decays_AndScalesXpWithoutALevelGate()
        {
            SocialStanding.AddMoodlet("up", SocialMood.Happy, 0.9f, 1f);
            Assert.AreEqual(SocialMood.Happy, SocialStanding.Dominant);
            Assert.Greater(SocialStanding.ScaleXp(100), 100);
            SocialStanding.AddMoodlet("down", SocialMood.Tired, 1f, 2f);
            Assert.AreEqual(SocialMood.Tired, SocialStanding.Dominant);
            Assert.Less(SocialStanding.ScaleXp(100), 100);
            SocialStanding.AdvanceHours(2.1f);
            Assert.AreEqual(SocialMood.Neutral, SocialStanding.Dominant);
            Assert.AreEqual(100, SocialStanding.ScaleXp(100));
        }

        [Test]
        public void PoliteAnswerRaisesReputation_RudeAnswerRaisesNotoriety()
        {
            SocialStanding.NoteAnswer("npc_kimura", 10, "Correct greeting");
            Assert.Greater(SocialStanding.Reputation, 0);
            Assert.Greater(SocialStanding.AffinityOf("npc_kimura"), 0);
            int reputation = SocialStanding.Reputation;
            SocialStanding.NoteAnswer("npc_kimura", -5, "rude reply");
            Assert.Greater(SocialStanding.Notoriety, 0);
            Assert.AreEqual(reputation, SocialStanding.Reputation);
            Assert.Less(SocialStanding.AffinityOf("npc_kimura"), 4);
        }

        [Test]
        public void NeedsKeepATiredMoodlet_UntilTheBodyRecovers()
        {
            SocialStanding.ReflectNeeds(hunger: 80f, thirst: 80f, sleepiness: 90f, energy: 40f, health01: 1f);
            Assert.AreEqual(SocialMood.Tired, SocialStanding.Dominant);
            SocialStanding.AdvanceHours(0.2f);
            SocialStanding.ReflectNeeds(hunger: 80f, thirst: 80f, sleepiness: 10f, energy: 90f, health01: 1f);
            Assert.AreEqual(SocialMood.Neutral, SocialStanding.Dominant);
        }

        [Test]
        public void Merge_KeepsHigherStanding_AndDoesNotDropMoodlets()
        {
            var local = new PlayerProgressDto
            {
                calendarStarted = true,
                calendarDay = 3,
                reputation = 10,
                notoriety = 4
            };
            local.affinities.Add(new NpcAffinityRecord { npcId = "grandma", affinity = 20 });
            local.affinities.Add(new NpcAffinityRecord { npcId = "student", affinity = 5 });
            local.moodlets.Add(new MoodletRecord { id = "kind", mood = (int)SocialMood.Proud, strength = 0.8f, hoursLeft = 1.5f });

            var cloud = new PlayerProgressDto
            {
                calendarStarted = true,
                calendarDay = 1,
                reputation = 40,
                notoriety = 1
            };
            cloud.affinities.Add(new NpcAffinityRecord { npcId = "grandma", affinity = 8 });
            cloud.affinities.Add(new NpcAffinityRecord { npcId = "kimura", affinity = 15 });
            cloud.moodlets.Add(new MoodletRecord { id = "old", mood = (int)SocialMood.Happy, strength = 0.5f, hoursLeft = 3f });

            var merged = SupabaseProgressRepository.MergeProgress(local, cloud);
            Assert.AreEqual(40, merged.reputation);
            Assert.AreEqual(4, merged.notoriety);
            Assert.AreEqual(20, Find(merged, "grandma"));
            Assert.AreEqual(15, Find(merged, "kimura"));
            Assert.AreEqual(5, Find(merged, "student"));
            Assert.AreEqual(1, merged.moodlets.Count);
            Assert.AreEqual("kind", merged.moodlets[0].id);
        }

        private static int Find(PlayerProgressDto progress, string id)
        {
            foreach (var row in progress.affinities)
                if (row.npcId == id) return row.affinity;
            return int.MinValue;
        }
    }
}
