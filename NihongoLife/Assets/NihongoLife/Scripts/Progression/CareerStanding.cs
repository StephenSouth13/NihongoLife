using NihongoLife.Core;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.Progression
{
    /// <summary>
    /// Fame (danh tiếng / 評判) and notoriety (tai tiếng / 悪評) as career forces, not vanity numbers.
    /// Pure rules so they can be tested and tuned in one place; CareerService hooks them in at three points
    /// (see Docs/Expansion/edu-v1/CAREER_STANDING_HOOK.md):
    ///  • PerformanceFactor — each shift's performance gain (fame helps, notoriety hurts);
    ///  • PromotionBlocker  — high notoriety blocks the promotion interview until it is worked off;
    ///  • SalaryFactor      — known professionals earn more, scandal costs tips/bonuses;
    ///  • OfferBonus        — fame opens doors: extra careers/clients (Gánh Ngon VIP rooms, tour guide groups).
    /// Values are 0–100 like SocialStanding.
    /// </summary>
    public static class CareerStanding
    {
        public const int ScandalBlock = 60;   // notoriety at or above: no promotion interview
        public const int Celebrity = 70;      // fame at or above: VIP clients / headhunting

        public static float PerformanceFactor(int fame, int notoriety)
        {
            float f = 1f + Mathf.Clamp(fame, 0, 100) * 0.004f - Mathf.Clamp(notoriety, 0, 100) * 0.006f; // 0.4 … 1.4
            return Mathf.Clamp(f, 0.4f, 1.4f);
        }

        public static float SalaryFactor(int fame, int notoriety)
        {
            float f = 1f + Mathf.Max(0, fame - 30) * 0.005f - Mathf.Max(0, notoriety - 20) * 0.005f; // 0.6 … 1.35
            return Mathf.Clamp(f, 0.6f, 1.35f);
        }

        /// <summary>Null when standing allows a promotion; otherwise the reason (vi / en / ja).</summary>
        public static string PromotionBlocker(int fame, int notoriety)
        {
            if (notoriety >= ScandalBlock)
                return Loc.F("Tai tiếng {0}/100 — sếp hoãn phỏng vấn thăng chức. Làm tốt vài ca, xin lỗi khách để giảm dưới {1}.",
                    "Notoriety {0}/100 — the boss postpones your promotion interview. Do good shifts and apologise to get it under {1}.",
                    "悪評{0}/100 — 昇進面接は延期。良い勤務とお詫びで{1}未満に。", notoriety, ScandalBlock);
            return null;
        }

        public static bool VipClients(int fame, int notoriety) => fame >= Celebrity && notoriety < ScandalBlock / 2;

        /// <summary>Notoriety slowly fades with each good shift (redemption, not a permanent mark).</summary>
        public static int NotorietyAfterGoodShift(int notoriety) => Mathf.Max(0, notoriety - 2);

        public static float Performance => PerformanceFactor(SocialStanding.Reputation, SocialStanding.Notoriety);
        public static float Salary => SalaryFactor(SocialStanding.Reputation, SocialStanding.Notoriety);
        public static string Blocker => PromotionBlocker(SocialStanding.Reputation, SocialStanding.Notoriety);
    }
}
