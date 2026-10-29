using NihongoLife.Core;

namespace NihongoLife.Island
{
    /// <summary>
    /// The fishing skill: learned by fishing, never given. Level comes from fish caught (IslandRecord.fished). It unlocks the
    /// set (AFK) rod and shortens the wait for a bite. Shown in the rod prompts; the skill handbook lists it too.
    /// </summary>
    public static class FishingSkill
    {
        public static readonly int[] Thresholds = { 0, 3, 12, 30 };   // fish caught for level 1..4

        public static int Level => LevelFor(IslandState.Record != null ? IslandState.Record.fished : 0);

        public static int LevelFor(int fished)
        {
            int level = 1;
            for (int i = 1; i < Thresholds.Length; i++) if (fished >= Thresholds[i]) level = i + 1;
            return level;
        }

        /// <summary>Fish still to catch for the next level (0 at the top).</summary>
        public static int ToNext(int fished)
        {
            int level = LevelFor(fished);
            return level >= Thresholds.Length ? 0 : Thresholds[level] - fished;
        }

        /// <summary>Multiplier on the wait for a bite: better anglers wait less.</summary>
        public static float BiteTimeScale(int level) => level <= 2 ? 1f : level == 3 ? 0.8f : 0.65f;

        /// <summary>The set rod needs level 2 (three fish with the basic rod).</summary>
        public const int AfkRodLevel = 2;

        public static string Title(int level) => level switch
        {
            1 => Loc.T("Người mới câu", "Beginner angler", "釣り入門"),
            2 => Loc.T("Cần thủ", "Angler", "釣り人"),
            3 => Loc.T("Cần thủ giỏi", "Skilled angler", "釣り上手"),
            _ => Loc.T("Bậc thầy câu cá", "Master angler", "釣り名人"),
        };
    }
}