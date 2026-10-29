using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Island;
using NihongoLife.Save;

namespace NihongoLife.Player
{
    /// <summary>One skill that is learned by doing: what counts, and how many it takes to reach each tier.</summary>
    public sealed class SkillDef
    {
        public readonly string Id, Vi, En, Ja, UnitVi, UnitEn, UnitJa;
        public readonly int[] Thresholds;
        public readonly Func<int> Metric;
        public SkillDef(string id, string vi, string en, string ja, string uvi, string uen, string uja, int[] thresholds, Func<int> metric)
        { Id = id; Vi = vi; En = en; Ja = ja; UnitVi = uvi; UnitEn = uen; UnitJa = uja; Thresholds = thresholds; Metric = metric; }

        public string Name => Loc.T(Vi, En, Ja);
        public string Unit => Loc.T(UnitVi, UnitEn, UnitJa);
        public int Value => Math.Max(0, Metric());
        public int Tiers => Thresholds.Length;
        /// <summary>1..Tiers</summary>
        public int TierFor(int value) { int t = 1; for (int i = 1; i < Thresholds.Length; i++) if (value >= Thresholds[i]) t = i + 1; return t; }
        public int Tier => TierFor(Value);
        /// <summary>How many more it takes for the next tier (0 at the top).</summary>
        public int ToNext(int value) { int t = TierFor(value); return t >= Tiers ? 0 : Thresholds[t] - value; }
    }

    /// <summary>
    /// F2: the tiered skill handbook (key Y). Six skills grow from what the player actually does - fish caught, harvests, animals fed,
    /// items sold, part-time shifts, knowledge points - and each has tiers. Fishing's tiers really change the game (set rod at tier 2,
    /// shorter waits at tiers 3-4); the other skills show progress and a title only for now.
    /// </summary>
    public static class SkillTiers
    {
        private static int Isl(Func<IslandRecord, int> f) => IslandState.Record != null ? f(IslandState.Record) : 0;

        public static readonly SkillDef[] All =
        {
            new SkillDef("fishing", "Câu cá", "Fishing", "釣り", "cá đã câu", "fish caught", "匹釣った", FishingSkill.Thresholds, () => Isl(r => r.fished)),
            new SkillDef("farming", "Làm vườn", "Farming", "農業", "lần thu hoạch", "harvests", "回収穫", new[] { 0, 5, 25, 80, 200 }, () => Isl(r => r.harvested)),
            new SkillDef("animals", "Chăm thú", "Animal care", "動物の世話", "lần cho ăn", "feedings", "回えさやり", new[] { 0, 5, 20, 60, 150 }, () => Isl(r => r.fed)),
            new SkillDef("trading", "Buôn bán", "Trading", "商売", "món đã bán", "items sold", "個売った", new[] { 0, 5, 25, 80, 200 }, () => Isl(r => r.sold)),
            new SkillDef("work", "Làm thêm", "Part-time work", "アルバイト", "ca đã xong", "shifts done", "回完了", new[] { 0, 3, 10, 25, 60 }, Shifts),
            new SkillDef("study", "Học tập", "Study", "勉強", "điểm tri thức", "knowledge points", "知識ポイント", new[] { 0, 50, 150, 300, 600 }, () => PlayerStatus.Instance != null ? (int)PlayerStatus.Instance.Knowledge : 0),
        };

        private static int Shifts()
        {
            if (GameServices.TryGet(out IProgressRepository repo) && repo.GetProgress()?.careers != null) return repo.GetProgress().careers.Sum(c => c.totalShifts);
            return 0;
        }

        public static SkillDef Skill(string id) => All.First(s => s.Id == id);

        public static string TierName(string skillId, int tier)
        {
            if (skillId == "fishing") return FishingSkill.Title(tier);
            return tier switch
            {
                1 => Loc.T("Người mới", "Newcomer", "初心者"),
                2 => Loc.T("Học việc", "Apprentice", "見習い"),
                3 => Loc.T("Thành thạo", "Skilled", "一人前"),
                4 => Loc.T("Chuyên gia", "Expert", "達人"),
                _ => Loc.T("Bậc thầy", "Master", "名人"),
            };
        }

        /// <summary>What this tier changes in the game ("" when it only shows a title).</summary>
        public static string Perk(string skillId, int tier)
        {
            if (skillId != "fishing") return "";
            return tier switch
            {
                2 => Loc.T("Mở khóa cần câu đặt sẵn (chế độ chờ).", "Unlocks the set rod (wait mode).", "置き竿（待機モード）が使えます。"),
                3 => Loc.T("Cá cắn câu nhanh hơn 20%.", "Fish bite 20% sooner.", "魚が20%早く食いつきます。"),
                4 => Loc.T("Cá cắn câu nhanh hơn 35%.", "Fish bite 35% sooner.", "魚が35%早く食いつきます。"),
                _ => "",
            };
        }

        /// <summary>"●●○○○" for the tier.</summary>
        public static string Dots(int tier, int tiers) => new string('●', tier) + new string('○', Math.Max(0, tiers - tier));

        public static string Progress(SkillDef s)
        {
            int v = s.Value, tier = s.TierFor(v), next = s.ToNext(v);
            string line = v + " " + s.Unit;
            return next <= 0
                ? line + " · " + Loc.T("đã đạt bậc cao nhất", "top tier reached", "最高段階に到達")
                : line + " · " + Loc.F("còn {0} để lên bậc {1}", "{0} more for tier {1}", "次の段階（{1}）まであと{0}", next, tier + 1);
        }
    }
}