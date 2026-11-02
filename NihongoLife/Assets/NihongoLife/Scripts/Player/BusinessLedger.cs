using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Player
{
    [Serializable]
    public sealed class BusinessKind
    {
        public string id, nameVi, nameEn, nameJa, nameRu;
        public int weeklyRent, weeklyUtilities, weeklySupplies;
        public float revenueFactor = 1f;

        public string Name => Loc.Code == "ru" && !string.IsNullOrEmpty(nameRu) ? nameRu : Loc.T(nameVi, nameEn, nameJa);
        public int WeeklyCost => weeklyRent + weeklyUtilities + weeklySupplies;
    }

    [Serializable]
    public sealed class BusinessFile
    {
        public int weekDays = 7, ownerDrawPercent = 60, reserveWeeks = 2;
        public BusinessKind[] kinds = Array.Empty<BusinessKind>();
    }

    /// <summary>
    /// The company's money, on the game's calendar (Resources/Progression/business.json). Staff earn every day by themselves.
    /// Every seven days the fixed costs of the kind of business are paid from the capital, and part of the week's profit goes
    /// to the owner's wallet. A short week never touches the wallet: what could not be paid waits, and reputation suffers.
    /// </summary>
    public static class BusinessLedger
    {
        public const string Path = "Progression/business";
        private static BusinessFile _file;

        public static BusinessFile File
        {
            get
            {
                if (_file != null) return _file;
                var asset = Resources.Load<TextAsset>(Path);
                _file = asset != null ? JsonUtility.FromJson<BusinessFile>(asset.text) ?? new BusinessFile() : new BusinessFile();
                return _file;
            }
        }

        public static BusinessKind Kind(string id) => File.kinds.FirstOrDefault(k => k.id == id) ?? File.kinds.FirstOrDefault();
        public static BusinessKind KindOf(BusinessRecord b) => Kind(b != null ? b.kind : null);
        public static int DaysToWeekEnd(BusinessRecord b, int today) => Mathf.Max(0, File.weekDays - (today - b.weekStartDay));
        public static int Reserve(BusinessRecord b) => (KindOf(b)?.WeeklyCost ?? 0) * File.reserveWeeks + b.unpaidCostYen;

        /// <summary>Called on a timer with the game's day: settles each new day, and each finished week.</summary>
        public static void Tick(BusinessSystem sys, int today)
        {
            if (sys == null || !sys.OwnsCompany) return;
            var b = sys.Business;
            if (b.lastSettledDay < 0 || b.lastSettledDay > today)
            {
                b.lastSettledDay = b.weekStartDay = today; // a company from an older save, or a new character's calendar
                sys.Persist();
                return;
            }
            if (today <= b.lastSettledDay) return;
            BusinessWorkers.Pending(sys, out int gross, out int wages, out _);
            b.weekIncomeYen += gross;
            b.weekWagesYen += wages;
            BusinessWorkers.Collect(sys);
            while (today - b.weekStartDay >= File.weekDays) CloseWeek(sys);
            sys.Persist();
        }

        private static void CloseWeek(BusinessSystem sys)
        {
            var b = sys.Business;
            var kind = KindOf(b);
            int cost = kind != null ? kind.WeeklyCost : 0;
            int due = cost + b.unpaidCostYen;
            int paid = Mathf.Min(b.capitalYen, due);
            b.capitalYen -= paid;
            b.unpaidCostYen = due - paid;
            int profit = b.weekIncomeYen - b.weekWagesYen - cost;
            int draw = 0;
            if (b.unpaidCostYen > 0)
            {
                b.reputation = Mathf.Max(0, b.reputation - 3);
                HudFeed.Post(Loc.F("Công ty thiếu {0} chi phí tuần này. Uy tín giảm, hãy nạp thêm doanh thu.", "The company is {0} short for this week's costs. Reputation drops.", "今週の経費が{0}足りません。信用が下がります。", Loc.Yen(b.unpaidCostYen)), HudFeed.Kind.Warning, 8f);
            }
            else
            {
                draw = Mathf.Clamp(Mathf.RoundToInt(profit * File.ownerDrawPercent / 100f), 0, b.capitalYen);
                if (draw > 0)
                {
                    b.capitalYen -= draw;
                    PlayerInventory.Instance?.AddYen(draw, Loc.Triple("Lợi nhuận công ty", "Company profit", "会社の利益"));
                }
                HudFeed.Post(Loc.F("Báo cáo tuần: thu {0}, lương {1}, chi phí {2}, về ví {3}.", "Weekly report: income {0}, wages {1}, costs {2}, to your wallet {3}.", "週報：収入{0}、給料{1}、経費{2}、受取{3}。",
                    Loc.Yen(b.weekIncomeYen), Loc.Yen(b.weekWagesYen), Loc.Yen(cost), Loc.Yen(draw)), profit >= 0 ? HudFeed.Kind.Reward : HudFeed.Kind.Warning, 8f);
            }
            b.weekStartDay += File.weekDays;
            b.weekIncomeYen = b.weekWagesYen = 0;
            b.lastWeekProfitYen = profit;
        }

        /// <summary>Moves what is above the reserve into the wallet; returns the amount.</summary>
        public static int Withdraw(BusinessSystem sys)
        {
            if (sys == null || !sys.OwnsCompany) return 0;
            var b = sys.Business;
            int amount = Mathf.Max(0, b.capitalYen - Reserve(b));
            if (amount == 0) return 0;
            b.capitalYen -= amount;
            PlayerInventory.Instance?.AddYen(amount, Loc.Triple("Rút từ công ty", "Taken from the company", "会社から引き出し"));
            sys.Persist();
            return amount;
        }
    }
}
