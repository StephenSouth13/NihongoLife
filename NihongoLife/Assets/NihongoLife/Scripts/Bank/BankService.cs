using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Bank
{
    /// <summary>One loan or deferred bill. Amounts in yen; days are game days (DayNightCycle.Day).</summary>
    [Serializable]
    public sealed class LoanRecord
    {
        public string id;
        /// <summary>bank.json product id, or <see cref="BankService.ClinicBill"/> for a clinic fee paid later.</summary>
        public string productId;
        public int principal;
        public int fee;
        /// <summary>Still to pay (principal + fee + late fees − repaid).</summary>
        public int owed;
        public int lateFees;
        public int takenDay;
        public int dueDay;
        public bool extended;
        public bool reminded;
        public bool overdueNoticed;
        public int lastLateFeeDay = -1;
    }

    /// <summary>The player's bank file (PlayerProgressDto.bank). Credit −1 = not opened yet (starts at creditStart).</summary>
    [Serializable]
    public sealed class BankRecord
    {
        public int credit = -1;
        public List<LoanRecord> loans = new List<LoanRecord>();
        public int repaidOnTime;
        public int repaidLate;
        public int garnishedYen;
        public int hungerTipDay = -1;
        public long updatedTicks;
    }

    /// <summary>
    /// Hibari Shinkin Bank: loans for a player who has run out of money, and the deferred clinic bill.
    /// · Emergency fund (緊急小口資金): no interest, only while the wallet is nearly empty — so being broke and hungry or
    ///   ill always has a way out.
    /// · Personal loan (カードローン): a flat fee, a limit that grows with the level and the credit score.
    /// Late payment is firm but never punishing enough to make anyone quit: a reminder before the due day, a grace
    /// period, then a capped late fee, a lower credit score (no new personal loan), and 30 % of each income kept back
    /// until the loan is paid — the player always keeps the rest. Nothing is seized, the wallet never goes below zero,
    /// and every loan can be extended once for free. Rules and numbers: Resources/Bank/bank.json.
    /// </summary>
    public static class BankService
    {
        public const string ClinicBill = "clinic_bill";
        public static event Action Changed;

        private static bool _moving; // our own wallet moves are not "income" to keep back

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Changed = null; _moving = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            WalletLedger.Recorded -= OnWallet;
            WalletLedger.Recorded += OnWallet;
        }

        public static BankCatalog Catalog => BankCatalog.Load();

        private static PlayerProgressDto Progress =>
            GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;

        public static BankRecord Record
        {
            get
            {
                var progress = Progress;
                if (progress == null) return null;
                progress.bank ??= new BankRecord();
                progress.bank.loans ??= new List<LoanRecord>();
                if (progress.bank.credit < 0) progress.bank.credit = Catalog.creditStart;
                return progress.bank;
            }
        }

        public static int Today => DayNightCycle.Active != null ? DayNightCycle.Active.Day : Progress?.calendarDay ?? 0;
        public static int Credit => Record?.credit ?? Catalog.creditStart;
        public static IReadOnlyList<LoanRecord> Loans => (IReadOnlyList<LoanRecord>)Record?.loans ?? Array.Empty<LoanRecord>();
        public static int TotalOwed => Loans.Sum(l => l.owed);
        public static bool IsOverdue(LoanRecord loan, int today) => loan != null && loan.owed > 0 && today > loan.dueDay + Catalog.graceDays;
        public static bool AnyOverdue => Loans.Any(l => IsOverdue(l, Today));
        private static int Wallet => PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : Progress?.yen ?? 0;
        private static int PlayerLevel => PlayerStatus.Instance != null ? PlayerStatus.Instance.Level : Mathf.Max(1, Progress?.level ?? 1);

        public static string ProductName(string productId) =>
            productId == ClinicBill ? Loc.T("Viện phí trả sau", "Clinic bill (pay later)", "診察料の後払い") : Catalog.Product(productId)?.Name ?? productId;

        private static void Save()
        {
            var record = Record;
            if (record != null) record.updatedTicks = DateTime.UtcNow.Ticks;
            if (GameServices.TryGet(out IProgressRepository repository)) repository.SaveProgress(repository.GetProgress());
            Changed?.Invoke();
        }

        // ─────────── Borrowing ───────────

        /// <summary>How much more this product lends now (0 when it is closed to the player).</summary>
        public static int Limit(LoanProduct product)
        {
            if (product == null || Blocker(product) != null) return 0;
            int limit = product.maxYen;
            if (product.perLevelYen > 0)
            {
                float credit = Mathf.Clamp(Credit / (float)Mathf.Max(1, Catalog.creditStart), 0.5f, 1.5f);
                limit = Mathf.Min(product.maxYen, Mathf.RoundToInt(product.perLevelYen * PlayerLevel * credit / 100f) * 100);
            }
            int outstanding = Loans.Where(l => l.productId == product.id).Sum(l => l.principal);
            return Mathf.Max(0, limit - outstanding);
        }

        /// <summary>Why this product is closed now, or null.</summary>
        public static string Blocker(LoanProduct product)
        {
            if (product == null) return Loc.T("Không có gói vay này.", "No such loan.", "その商品はありません。");
            if (PlayerLevel < product.minLevel) return Loc.F("Mở từ cấp {0}.", "Opens at level {0}.", "レベル{0}から。", product.minLevel);
            if (product.onlyBelowYen > 0 && Wallet >= product.onlyBelowYen)
                return Loc.F("Chỉ dành cho lúc ví dưới {0}.", "Only while your wallet holds under {0}.", "財布が{0}未満のときだけ。", Loc.Yen(product.onlyBelowYen));
            if (product.onlyBelowYen > 0 && Loans.Any(l => l.productId == product.id))
                return Loc.T("Trả khoản khẩn cấp trước rồi mới vay lại.", "Repay the emergency fund before taking it again.", "前の分を返してから。");
            if (AnyOverdue && product.needsCredit) return Loc.T("Đang có khoản trễ hạn — trả hoặc gia hạn trước.", "A loan is overdue — repay or extend it first.", "延滞中です。先に返済か延長を。");
            if (product.needsCredit && Credit < Catalog.creditMinForCard)
                return Loc.F("Điểm tín dụng {0} — cần {1}. Trả đúng hạn để tăng.", "Credit score {0} — {1} needed. Repay on time to raise it.", "信用スコア{0}（{1}必要）。期日どおりの返済で上がります。", Credit, Catalog.creditMinForCard);
            return null;
        }

        /// <summary>The flat fee for borrowing <paramref name="amount"/>.</summary>
        public static int FeeFor(LoanProduct product, int amount) => product == null ? 0 : Mathf.CeilToInt(amount * product.interestPercent / 100f);

        /// <summary>Lends the money (it goes into the wallet). Null on success, else why not.</summary>
        public static string Borrow(string productId, int amount)
        {
            var product = Catalog.Product(productId);
            var record = Record;
            if (record == null) return Loc.T("Chưa vào game.", "Not in a game.", "ゲーム外です。");
            string blocked = Blocker(product);
            if (blocked != null) return blocked;
            int limit = Limit(product);
            amount = Mathf.Clamp(amount, 0, limit);
            if (amount <= 0) return Loc.T("Đã hết hạn mức của gói này.", "No limit left on this loan.", "限度額に達しています。");
            if (PlayerInventory.Instance == null) return Loc.T("Không tìm thấy ví.", "No wallet found.", "財布が見つかりません。");
            int fee = FeeFor(product, amount);
            int today = Today;
            record.loans.Add(new LoanRecord
            {
                id = productId + "_" + today + "_" + record.loans.Count + "_" + (DateTime.UtcNow.Ticks % 100000),
                productId = productId,
                principal = amount,
                fee = fee,
                owed = amount + fee,
                takenDay = today,
                dueDay = today + Mathf.Max(1, product.termDays),
            });
            _moving = true;
            try { PlayerInventory.Instance.AddYen(amount, Loc.Triple("Vay: " + product.vi, "Loan: " + product.en, "借入：" + product.ja)); }
            finally { _moving = false; }
            Save();
            return null;
        }

        /// <summary>A clinic fee the player could not pay: no interest, due in two weeks (the receptionist's 「こんどで いいですよ」).</summary>
        public static void AddClinicBill(int amount)
        {
            var record = Record;
            if (record == null || amount <= 0) return;
            int today = Today;
            var open = record.loans.FirstOrDefault(l => l.productId == ClinicBill && !IsOverdue(l, today));
            if (open != null) { open.principal += amount; open.owed += amount; }
            else record.loans.Add(new LoanRecord { id = ClinicBill + "_" + today + "_" + record.loans.Count, productId = ClinicBill, principal = amount, owed = amount, takenDay = today, dueDay = today + 14 });
            Save();
        }

        // ─────────── Repaying ───────────

        /// <summary>Pays up to <paramref name="amount"/> (capped at what is owed and what the wallet holds). Null on success.</summary>
        public static string Repay(string loanId, int amount)
        {
            var record = Record;
            var loan = record?.loans.FirstOrDefault(l => l.id == loanId);
            if (loan == null) return Loc.T("Không tìm thấy khoản vay.", "Loan not found.", "見つかりません。");
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return Loc.T("Không tìm thấy ví.", "No wallet found.", "財布が見つかりません。");
            amount = Mathf.Min(amount, loan.owed, inventory.Yen);
            if (amount <= 0) return Loc.T("Ví không đủ tiền để trả.", "Not enough money in the wallet.", "お金が足りません。");
            _moving = true;
            try { inventory.SpendYen(amount, Loc.Triple("Trả nợ: " + ProductName(loan.productId), "Repayment: " + ProductName(loan.productId), "返済：" + ProductName(loan.productId))); }
            finally { _moving = false; }
            Apply(record, loan, amount);
            Save();
            return null;
        }

        private static void Apply(BankRecord record, LoanRecord loan, int amount)
        {
            loan.owed -= amount;
            if (loan.owed > 0) return;
            bool onTime = Today <= loan.dueDay + Catalog.graceDays;
            if (onTime) { record.repaidOnTime++; record.credit = Mathf.Min(Catalog.creditMax, record.credit + Catalog.creditOnTimeRepay); }
            else { record.repaidLate++; record.credit = Mathf.Min(Catalog.creditMax, record.credit + Catalog.creditOnTimeRepay / 2); } // paying off late still rebuilds trust
            record.loans.Remove(loan);
            HudFeed.Post(Loc.F("Đã trả hết khoản {0}. Điểm tín dụng: {1}.", "{0} paid off. Credit score: {1}.", "{0}を完済しました。信用スコア：{1}", ProductName(loan.productId), record.credit), HudFeed.Kind.Reward, 6f);
        }

        /// <summary>Moves the due day once, free of charge, and clears the overdue state. Null on success.</summary>
        public static string Extend(string loanId)
        {
            var loan = Record?.loans.FirstOrDefault(l => l.id == loanId);
            if (loan == null) return Loc.T("Không tìm thấy khoản vay.", "Loan not found.", "見つかりません。");
            if (loan.extended) return Loc.T("Khoản này đã gia hạn một lần.", "This loan was already extended once.", "延長は一度だけです。");
            loan.extended = true;
            loan.dueDay = Mathf.Max(loan.dueDay, Today) + Catalog.extensionDays;
            loan.reminded = false;
            loan.overdueNoticed = false;
            Save();
            return null;
        }

        // ─────────── Each game day / each income ───────────

        /// <summary>Reminders, overdue notices and capped late fees (BankClock, once a second).</summary>
        public static void Tick(int today)
        {
            var record = Record;
            if (record == null || record.loans.Count == 0) return;
            var c = Catalog;
            bool changed = false;
            foreach (var loan in record.loans)
            {
                if (!loan.reminded && today >= loan.dueDay - c.remindDaysBefore && today <= loan.dueDay)
                {
                    loan.reminded = true; changed = true;
                    HudFeed.Post(Loc.F("Ngân hàng: khoản {0} ({1}) đến hạn ngày {2}. Bấm ví ¥ → Ngân hàng để trả hoặc gia hạn.", "Bank: {0} ({1}) is due on {2}. Wallet ¥ → Bank to repay or extend.", "銀行：{0}（{1}）の期日は{2}です。", ProductName(loan.productId), Loc.Yen(loan.owed), DueText(loan.dueDay)), HudFeed.Kind.Info, 8f, "bank-remind");
                }
                if (!IsOverdue(loan, today)) continue;
                if (!loan.overdueNoticed)
                {
                    loan.overdueNoticed = true; changed = true;
                    record.credit = Mathf.Max(0, record.credit - c.creditLate);
                    HudFeed.Post(Loc.F("Khoản {0} đã trễ hạn. Từ nay {1}% mỗi khoản thu nhập tự động trả nợ (bạn vẫn giữ phần còn lại). Có thể gia hạn miễn phí một lần.", "{0} is overdue. From now on {1}% of each income repays it (you keep the rest). You may extend it once for free.", "{0}が延滞中です。収入の{1}%が自動で返済に回ります。一度だけ無料で延長できます。", ProductName(loan.productId), c.garnishPercent), HudFeed.Kind.Warning, 10f, "bank-overdue");
                }
                if (loan.productId == ClinicBill) continue; // the clinic charges no late fee
                int cap = Mathf.CeilToInt(loan.principal * c.lateFeeCapPercent / 100f);
                bool due = loan.lastLateFeeDay < 0 || today - loan.lastLateFeeDay >= c.lateFeeEveryDays;
                if (due && loan.lateFees < cap)
                {
                    int fee = Mathf.Min(cap - loan.lateFees, Mathf.Max(1, Mathf.CeilToInt(loan.principal * c.lateFeePercent / 100f)));
                    loan.lateFees += fee; loan.owed += fee; loan.lastLateFeeDay = today; changed = true;
                }
            }
            if (changed) Save();
        }

        /// <summary>Hungry and nearly out of money: once a day, point at the emergency fund.</summary>
        public static void HungerTip(int today, float hunger)
        {
            var record = Record;
            if (record == null || record.hungerTipDay == today || hunger > 30f || Wallet >= 300) return;
            var emergency = Catalog.products.FirstOrDefault(p => p.onlyBelowYen > 0);
            if (emergency == null || Limit(emergency) <= 0) return;
            record.hungerTipDay = today;
            HudFeed.Post(Loc.F("Hết tiền ăn? Bấm vào ví ¥ → Ngân hàng → {0} (không lãi).", "Out of money for food? Click the ¥ wallet → Bank → {0} (no interest).", "食費がない？財布¥ → 銀行 → {0}（無利子）", emergency.Name), HudFeed.Kind.Info, 9f, "bank-hunger");
        }

        private static void OnWallet(WalletLedger.Entry entry)
        {
            if (_moving || entry.amount <= 0) return;
            var record = Record;
            if (record == null || record.loans.Count == 0) return;
            int today = Today;
            var overdue = record.loans.Where(l => IsOverdue(l, today)).OrderBy(l => l.dueDay).ToList();
            if (overdue.Count == 0) return;
            int keep = Mathf.FloorToInt(entry.amount * Catalog.garnishPercent / 100f);
            var inventory = PlayerInventory.Instance;
            foreach (var loan in overdue)
            {
                int take = Mathf.Min(keep, loan.owed, inventory != null ? inventory.Yen : 0);
                if (take <= 0) break;
                _moving = true;
                try { inventory.SpendYen(take, Loc.Triple("Tự động trả nợ quá hạn", "Overdue loan repayment", "延滞分の自動返済")); }
                finally { _moving = false; }
                record.garnishedYen += take;
                keep -= take;
                Apply(record, loan, take);
            }
            Save();
        }

        public static string DueText(int day)
        {
            var date = GameCalendar.DateOf(day);
            return Loc.F("{0}/{1}", "{1}/{0}", "{1}月{0}日", date.Day, date.Month);
        }
    }
}
