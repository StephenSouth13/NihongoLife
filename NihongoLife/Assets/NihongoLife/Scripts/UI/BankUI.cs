using System.Linq;
using NihongoLife.Bank;
using NihongoLife.Core;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Hibari Shinkin Bank (ひばり信用金庫): the wallet, what is owed and the credit score at the top; each loan with its
    /// due day and Repay / Extend; the loans on offer with amount chips that say what comes back and when; the rules in
    /// plain words so a late payment is never a surprise. Opened from the HUD wallet's history and the bank ATMs.
    /// </summary>
    public sealed class BankUI : MonoBehaviour
    {
        private static BankUI _instance;
        private TMP_FontAsset _font;
        private RectTransform _root, _body;
        private ScrollRect _scroll;
        private PlayerController _player;
        private bool _wasLocked;
        private string _message;
        private bool _messageGood;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeInHierarchy;

        private static BankUI Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("BankCanvas", 570);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<BankUI>();
                _instance._font = NLUi.ResolveFont();
                _instance.Build((RectTransform)canvas.transform);
                Loc.Watch(_instance, () => { if (IsOpen) _instance.Rebuild(); });
                UiModalStack.Register(_instance, () => IsOpen, Close, "Bank");
                BankService.Changed += () => { if (IsOpen) _instance.Rebuild(); };
                return _instance;
            }
        }

        public static void Open()
        {
            var ui = Instance;
            ui._message = null;
            if (!ui._root.gameObject.activeSelf)
            {
                ui._root.gameObject.SetActive(true);
                ui._player = SceneRefs.Find<PlayerController>();
                if (ui._player != null) { ui._wasLocked = ui._player.InputLocked; ui._player.InputLocked = true; }
            }
            ui._root.SetAsLastSibling();
            ui.Rebuild();
            ui._scroll.verticalNormalizedPosition = 1f;
        }

        public static void Close()
        {
            if (!IsOpen) return;
            _instance._root.gameObject.SetActive(false);
            if (_instance._player != null) _instance._player.InputLocked = _instance._wasLocked;
            _instance._player = null;
        }

        private void Build(RectTransform canvas)
        {
            _root = new GameObject("Bank", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _root.SetParent(canvas, false);
            NLUi.Stretch(_root);
            _root.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.72f);
            var card = NLUi.Panel(_root, "Card", NLUi.Ink);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(920f, 700f);
            card.gameObject.AddComponent<HudFitRect>().Configure(0.94f, 0.94f, Vector2.zero, 0.4f);
            var scrollRoot = new GameObject("Scroll", typeof(RectTransform)).GetComponent<RectTransform>();
            scrollRoot.SetParent(card, false);
            NLUi.Stretch(scrollRoot);
            scrollRoot.offsetMin = new Vector2(28f, 20f); scrollRoot.offsetMax = new Vector2(-20f, -20f);
            _body = UiKit.Scroll(scrollRoot, "Content", out _scroll, 12f, new RectOffset(2, 18, 4, 16));
            NLUi.Stretch((RectTransform)_scroll.transform);
            NLUi.CloseButton(card, _font, Close, 46f, 16f);
            _root.gameObject.SetActive(false);
        }

        private void Say(string error, string success = null)
        {
            _message = error ?? success;
            _messageGood = error == null;
            Rebuild();
        }

        private void Rebuild()
        {
            UiKit.Clear(_body);
            var catalog = BankService.Catalog;
            int today = BankService.Today;
            int wallet = PlayerInventory.Instance != null ? PlayerInventory.Instance.Yen : 0;

            NLUi.Label(_body, "Title", "ひばり信用金庫  <size=60%><color=#A8B4C4>" + catalog.Name + "</color></size>", 30f, NLUi.Gold, _font, FontStyles.Bold);
            var stats = NLUi.Group(_body, "Stats", false, 10f, TextAnchor.MiddleLeft, false);
            NLUi.Pill(stats, "Wallet", Loc.T("Ví ", "Wallet ", "財布 ") + Loc.Yen(wallet), _font, NLUi.Card, NLUi.Gold, 17f);
            int owed = BankService.TotalOwed;
            NLUi.Pill(stats, "Owed", Loc.T("Đang nợ ", "Owed ", "借入残高 ") + Loc.Yen(owed), _font, NLUi.Card, owed > 0 ? NLUi.Soft : NLUi.Muted, 17f);
            int credit = BankService.Credit;
            var creditColor = credit >= catalog.creditStart ? NLUi.Good : credit >= catalog.creditMinForCard ? NLUi.Soft : NLUi.Bad;
            NLUi.Pill(stats, "Credit", Loc.F("Tín dụng {0}/{1}", "Credit {0}/{1}", "信用 {0}/{1}", credit, catalog.creditMax), _font, NLUi.Card, creditColor, 17f);
            if (!string.IsNullOrEmpty(_message)) NLUi.Label(_body, "Message", _message, 16f, _messageGood ? NLUi.Good : NLUi.Bad, _font, FontStyles.Bold);

            // ─── Loans ───
            NLUi.Label(_body, "LoansTitle", Loc.T("Khoản đang nợ", "What you owe", "返済中"), 20f, NLUi.Gold, _font, FontStyles.Bold);
            var loans = BankService.Loans.ToList();
            if (loans.Count == 0) NLUi.Label(_body, "NoLoans", Loc.T("Bạn không nợ gì.", "You owe nothing.", "借入はありません。"), 15f, NLUi.Muted, _font);
            foreach (var loan in loans) LoanCard(loan, today, wallet);

            // ─── Borrow ───
            NLUi.Label(_body, "BorrowTitle", Loc.T("Vay tiền", "Borrow", "借りる"), 20f, NLUi.Gold, _font, FontStyles.Bold);
            foreach (var product in catalog.products) ProductCard(product, today);

            // ─── Rules ───
            NLUi.Label(_body, "RulesTitle", Loc.T("Quy định khi trễ hạn", "If a payment is late", "延滞したとき"), 20f, NLUi.Gold, _font, FontStyles.Bold);
            var rules = NLUi.Panel(_body, "Rules", NLUi.Card, new RectOffset(18, 18, 12, 14), 4f);
            NLUi.Label(rules, "Text", Loc.F(
                "1. {0} ngày trước hạn: ngân hàng nhắc.\n2. Quá hạn {1} ngày đầu: ân hạn, không phạt.\n3. Sau đó: phí trễ {2}% mỗi {3} ngày, tối đa {4}% số vay; điểm tín dụng −{5}; chưa vay cá nhân được nữa.\n4. {6}% mỗi khoản thu nhập (lương, thưởng) tự trả nợ — bạn luôn giữ phần còn lại.\n5. Mỗi khoản được gia hạn {7} ngày một lần, miễn phí.\nNgân hàng không tịch thu đồ, ví không bao giờ âm. Trả đúng hạn: tín dụng +{8}, hạn mức tăng.",
                "1. {0} days before the due day: a reminder.\n2. The first {1} days late: grace, no charge.\n3. Then: a {2}% late fee every {3} days, at most {4}% of the loan; credit score −{5}; no new personal loan.\n4. {6}% of each income (salary, rewards) repays it — you always keep the rest.\n5. Each loan can be extended by {7} days once, free.\nThe bank never takes your things and the wallet never goes below zero. Repay on time: credit +{8}, a higher limit.",
                "1. 期日の{0}日前にお知らせ。\n2. 延滞して最初の{1}日は猶予（手数料なし）。\n3. その後：{3}日ごとに{2}%の遅延損害金（借入額の{4}%まで）、信用スコア−{5}、新しいカードローン不可。\n4. 収入（給料・報酬）の{6}%が自動で返済に。残りは手元に残ります。\n5. 一度だけ無料で{7}日延長できます。\n持ち物の差し押さえはなく、財布がマイナスになることもありません。期日どおりの返済で信用+{8}、限度額アップ。",
                catalog.remindDaysBefore, catalog.graceDays, catalog.lateFeePercent, catalog.lateFeeEveryDays, catalog.lateFeeCapPercent, catalog.creditLate,
                catalog.garnishPercent, catalog.extensionDays, catalog.creditOnTimeRepay), 15f, NLUi.Text, _font);
        }

        private void LoanCard(LoanRecord loan, int today, int wallet)
        {
            var c = BankService.Catalog;
            bool overdue = BankService.IsOverdue(loan, today);
            bool grace = !overdue && today > loan.dueDay;
            var card = NLUi.Panel(_body, "Loan_" + loan.id, overdue ? new Color(0.28f, 0.1f, 0.09f, 1f) : NLUi.Card, new RectOffset(18, 18, 12, 14), 6f);
            string status = overdue ? Loc.F("Trễ hạn {0} ngày", "{0} days overdue", "{0}日延滞", today - loan.dueDay)
                : grace ? Loc.F("Ân hạn — còn {0} ngày", "Grace — {0} days left", "猶予中 · あと{0}日", loan.dueDay + c.graceDays - today)
                : Loc.F("Còn {0} ngày", "{0} days left", "あと{0}日", loan.dueDay - today);
            string color = overdue ? "#FF8A75" : grace ? "#F2D27A" : "#7BE38E";
            NLUi.Label(card, "Head", $"<b>{BankService.ProductName(loan.productId)}</b>   <color={color}>{status}</color>", 18f, NLUi.Text, _font);
            NLUi.Label(card, "Detail", Loc.F("Còn nợ {0} · đã vay {1} · phí {2}{3} · hạn {4}{5}", "Owed {0} · borrowed {1} · fee {2}{3} · due {4}{5}", "残り{0} · 借入{1} · 手数料{2}{3} · 期日{4}{5}",
                Loc.Yen(loan.owed), Loc.Yen(loan.principal), Loc.Yen(loan.fee), loan.lateFees > 0 ? Loc.F(" + trễ hạn {0}", " + late {0}", " + 遅延{0}", Loc.Yen(loan.lateFees)) : "",
                BankService.DueText(loan.dueDay), loan.extended ? Loc.T(" (đã gia hạn)", " (extended)", "（延長済み）") : ""), 15f, NLUi.Muted, _font);
            var row = NLUi.Group(card, "Actions", false, 8f, TextAnchor.MiddleLeft, false);
            string id = loan.id;
            int all = Mathf.Min(loan.owed, wallet);
            var payAll = NLUi.Button(row, "PayAll", all >= loan.owed ? Loc.F("Trả hết {0}", "Repay all {0}", "全額返済 {0}", Loc.Yen(loan.owed)) : Loc.F("Trả {0} (hết ví)", "Repay {0} (all you have)", "{0}返済", Loc.Yen(all)), _font,
                () => Say(BankService.Repay(id, loan.owed), Loc.T("Đã trả.", "Paid.", "返済しました。")), NLUi.Gold, 16f, NLUi.Ink, 44f);
            payAll.interactable = all > 0;
            int part = Mathf.Min(1000, loan.owed);
            if (loan.owed > 1000 && wallet >= part)
                NLUi.Button(row, "PayPart", Loc.F("Trả {0}", "Repay {0}", "{0}返済", Loc.Yen(part)), _font, () => Say(BankService.Repay(id, part), Loc.T("Đã trả một phần.", "Part paid.", "一部返済しました。")), NLUi.CardHover, 16f, NLUi.Text, 44f);
            if (!loan.extended)
                NLUi.Button(row, "Extend", Loc.F("Gia hạn +{0} ngày (miễn phí)", "Extend +{0} days (free)", "+{0}日延長（無料）", c.extensionDays), _font,
                    () => Say(BankService.Extend(id), Loc.T("Đã gia hạn.", "Extended.", "延長しました。")), NLUi.CardHover, 16f, NLUi.Text, 44f);
        }

        private void ProductCard(LoanProduct product, int today)
        {
            string blocked = BankService.Blocker(product);
            int limit = BankService.Limit(product);
            var card = NLUi.Panel(_body, "Product_" + product.id, NLUi.Card, new RectOffset(18, 18, 12, 14), 6f);
            string terms = product.interestPercent > 0 ? Loc.F("phí {0}%", "{0}% fee", "手数料{0}%", product.interestPercent) : Loc.T("không lãi", "no interest", "無利子");
            NLUi.Label(card, "Head", $"<b>{product.Name}</b>  <size=80%><color=#A8B4C4>{product.ja} · {terms} · {Loc.F("trả trong {0} ngày", "repay within {0} days", "{0}日以内に返済", product.termDays)}</color></size>", 18f, NLUi.Text, _font);
            NLUi.Label(card, "Note", product.Note, 14.5f, NLUi.Muted, _font);
            if (blocked != null) { NLUi.Label(card, "Blocked", blocked, 15f, NLUi.Soft, _font); return; }
            NLUi.Label(card, "Limit", Loc.F("Có thể vay tới {0}", "You can borrow up to {0}", "{0}まで借りられます", Loc.Yen(limit)), 15f, NLUi.Good, _font, FontStyles.Bold);
            var row = NLUi.Group(card, "Amounts", false, 8f, TextAnchor.MiddleLeft, false);
            var amounts = new[] { 500, 1000, 3000, 5000, 10000, limit }.Where(a => a > 0 && a <= limit).Distinct().OrderBy(a => a).TakeLast(4);
            string id = product.id;
            foreach (int amount in amounts)
            {
                int back = amount + BankService.FeeFor(product, amount);
                NLUi.Button(row, "Borrow_" + amount, Loc.F("Vay {0}\n<size=75%>trả {1} trước {2}</size>", "Borrow {0}\n<size=75%>repay {1} by {2}</size>", "{0}借りる\n<size=75%>{2}までに{1}</size>", Loc.Yen(amount), Loc.Yen(back), BankService.DueText(today + product.termDays)), _font,
                    () => Say(BankService.Borrow(id, amount), Loc.F("Đã nhận {0} vào ví.", "{0} added to your wallet.", "{0}を受け取りました。", Loc.Yen(amount))), NLUi.CardHover, 15f, NLUi.Text, 54f);
            }
        }
    }
}
