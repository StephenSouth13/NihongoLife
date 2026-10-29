using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>A job a hired worker can do and what it earns the company per hour at level 1.</summary>
    public sealed class WorkerRole
    {
        public readonly string Id, Vi, En, Ja; public readonly int RevenuePerHour;
        public WorkerRole(string id, string vi, string en, string ja, int rev) { Id = id; Vi = vi; En = en; Ja = ja; RevenuePerHour = rev; }
        public string Label => Loc.T(Vi, En, Ja);
    }

    /// <summary>
    /// F4: hire workers for the player's company. Each has a name, a role, a wage and a level. Workers earn for the company by
    /// the (real) hour up to <see cref="MaxHours"/> hours between collections - so it also works after the tab was closed -
    /// the wage is paid pro rata out of what they earn, and <see cref="Collect"/> banks the net into the company's capital.
    /// Training raises a worker's level (+20% earnings per level). Hiring still goes through <see cref="BusinessSystem.TryHireEmployee"/>
    /// so the living-wage rule and the reputation effects stay in one place.
    /// </summary>
    public static class BusinessWorkers
    {
        public const int MaxHours = 8, MaxLevel = 5, HoursPerMonth = 720, MaxWorkers = 12;
        public static Func<long> NowTicks = () => DateTime.UtcNow.Ticks;

        public static readonly WorkerRole[] Roles =
        {
            new WorkerRole("cashier", "Thu ngân", "Cashier", "レジ係", 180),
            new WorkerRole("farmhand", "Phụ vườn", "Farm hand", "農作業員", 160),
            new WorkerRole("cook", "Phụ bếp", "Cook", "調理スタッフ", 220),
            new WorkerRole("angler", "Người câu cá", "Angler", "釣り担当", 140),
        };
        private static readonly string[] Names = { "Haru", "Rina", "Daiki", "Nana", "Sho", "Emi", "Ryo", "Kana", "Jun", "Mari", "Taku", "Noa" };

        public static WorkerRole Role(string id) => Roles.FirstOrDefault(r => r.Id == id) ?? Roles[0];

        public static int LivingWage(BusinessRecord b) => 24000 + b.employeeCount * 1000;
        public static int RevenuePerHour(WorkerRecord w) => Mathf.RoundToInt(Role(w.role).RevenuePerHour * (1f + 0.2f * (Mathf.Clamp(w.level, 1, MaxLevel) - 1)));
        public static float WagePerHour(WorkerRecord w) => w.wage / (float)HoursPerMonth;
        public static int TrainCost(WorkerRecord w) => 5000 * w.level;

        /// <summary>Hires a worker (null on failure: no company, wage below the living wage, no capital, full payroll).</summary>
        public static WorkerRecord Hire(BusinessSystem sys, string roleId, int wage)
        {
            if (sys == null || !sys.OwnsCompany) return null;
            var b = sys.Business; b.workers ??= new System.Collections.Generic.List<WorkerRecord>();
            if (b.workers.Count >= MaxWorkers) return null;
            if (!sys.TryHireEmployee(wage)) return null;
            var used = b.workers.Select(x => x.name).ToHashSet();
            string name = Names.FirstOrDefault(n => !used.Contains(n)) ?? "Worker" + (b.workers.Count + 1);
            var w = new WorkerRecord { id = "w" + (b.workers.Count + 1) + "_" + NowTicks() % 100000, name = name, role = Role(roleId).Id, wage = wage, level = 1, hiredTicks = NowTicks() };
            b.workers.Add(w);
            sys.Persist();
            return w;
        }

        public static bool Fire(BusinessSystem sys, string id)
        {
            if (sys == null || !sys.OwnsCompany) return false;
            var b = sys.Business; var w = b.workers?.FirstOrDefault(x => x.id == id);
            if (w == null) return false;
            Collect(sys); // settle what they earned first
            b.workers.Remove(w);
            b.employeeCount = Mathf.Max(0, b.employeeCount - 1);
            b.reputation = Mathf.Max(0, b.reputation - 2);
            sys.Persist();
            return true;
        }

        public static bool Train(BusinessSystem sys, string id)
        {
            if (sys == null || !sys.OwnsCompany) return false;
            var b = sys.Business; var w = b.workers?.FirstOrDefault(x => x.id == id);
            if (w == null || w.level >= MaxLevel || b.capitalYen < TrainCost(w)) return false;
            b.capitalYen -= TrainCost(w); w.level++; b.educationScore += 3;
            sys.Persist();
            return true;
        }

        private static float Hours(BusinessRecord b, WorkerRecord w)
        {
            long since = Math.Max(b.lastCollectTicks, w.hiredTicks);
            return Mathf.Clamp((float)((NowTicks() - since) / (double)TimeSpan.TicksPerHour), 0f, MaxHours);
        }

        /// <summary>What the staff have earned (gross), cost in wages, and the net, since the last collection.</summary>
        public static void Pending(BusinessSystem sys, out int gross, out int wages, out int net)
        {
            gross = wages = net = 0;
            if (sys == null || !sys.OwnsCompany || sys.Business.workers == null) return;
            float g = 0f, c = 0f;
            foreach (var w in sys.Business.workers) { float h = Hours(sys.Business, w); g += RevenuePerHour(w) * h; c += WagePerHour(w) * h; }
            gross = Mathf.FloorToInt(g); wages = Mathf.CeilToInt(c); net = gross - wages;
        }

        /// <summary>Banks the net earnings into the company's capital; returns the amount.</summary>
        public static int Collect(BusinessSystem sys)
        {
            Pending(sys, out _, out _, out int net);
            if (sys == null || !sys.OwnsCompany) return 0;
            var b = sys.Business;
            b.capitalYen = Mathf.Max(0, b.capitalYen + net);
            if (net > 0) b.totalEarnedYen += net;
            b.lastCollectTicks = NowTicks();
            sys.Persist();
            return net;
        }

        public static string Describe(WorkerRecord w) =>
            $"{w.name} · {Role(w.role).Label} · Lv{w.level} · {Loc.Yen(RevenuePerHour(w))}/h";
    }
}