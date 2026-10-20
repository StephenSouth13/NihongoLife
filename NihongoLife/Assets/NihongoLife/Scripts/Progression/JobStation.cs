using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.Shop;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Progression
{
    public enum JobStationKind { Board, StockCrate, Shelf, Customer, Register, OrderTable, DishPass }

    /// <summary>
    /// One work spot of a part-time job, placed in the scene by JobSiteBuilder. It only becomes interactable while
    /// its shift is active and its step is still open, so it never steals the [F] prompt from the shop shelves or
    /// tables outside work. Every completed step is a real action (carry, place, answer correctly) reported to
    /// QuestService — nothing pays for merely opening a window.
    /// </summary>
    public sealed class JobStation : MonoBehaviour, IInteractable, IConditionalInteractable, IInteractionPriority
    {
        [SerializeField] private string questId = "job_konbini_shift";
        [SerializeField] private JobStationKind kind;
        [SerializeField] private string stationId = "station";
        [SerializeField] private string labelJa = "";
        [SerializeField] private string labelVi = "";
        [SerializeField] private GameObject shiftVisual;

        private static RestaurantMenuDefinition _menu;

        public string QuestId => questId;
        public JobStationKind Kind => kind;
        public string StationId => stationId;

        public void Configure(string quest, JobStationKind stationKind, string id, string ja, string vi, GameObject visual = null)
        {
            questId = quest; kind = stationKind; stationId = id; labelJa = ja; labelVi = vi; shiftVisual = visual;
        }

        private bool ShiftActive => QuestService.State(questId)?.status == "active";
        public float InteractionPriority => kind == JobStationKind.Board ? 0.2f : 0.9f;

        public bool IsInteractionAvailable
        {
            get
            {
                JobShift.Sync(questId);
                if (kind == JobStationKind.Board) return true;
                if (!ShiftActive || TimedAction.Busy) return false;
                switch (kind)
                {
                    case JobStationKind.StockCrate: return QuestService.ObjectiveOpen(questId, "restock") && JobShift.CarryKind == null;
                    case JobStationKind.Shelf: return QuestService.ObjectiveOpen(questId, "restock") && JobShift.CarryKind == "stock" && !JobShift.IsDone(stationId);
                    case JobStationKind.Customer: return QuestService.ObjectiveOpen(questId, "assist") && !JobShift.IsDone(stationId);
                    case JobStationKind.Register: return QuestService.ObjectiveOpen(questId, "checkout");
                    case JobStationKind.OrderTable:
                        if (JobShift.IsDone(stationId)) return false;
                        string order = JobShift.OrderAt(stationId);
                        return order == null ? QuestService.ObjectiveOpen(questId, "order") : JobShift.CarryKind == "dish";
                    case JobStationKind.DishPass: return QuestService.ObjectiveOpen(questId, "serve") && JobShift.CarryKind == null && JobShift.Orders.Any();
                }
                return false;
            }
        }

        private void Update()
        {
            // Customers and guests only appear while a shift is running.
            if (shiftVisual != null)
            {
                bool show = ShiftActive && (kind != JobStationKind.Customer || !JobShift.IsDone(stationId)) && (kind != JobStationKind.OrderTable || !JobShift.IsDone(stationId));
                if (shiftVisual.activeSelf != show) shiftVisual.SetActive(show);
            }
        }

        /// <summary>The station label in the interface language (scenes store the Japanese and Vietnamese labels).</summary>
        private string Label => Loc.IsJapanese && !string.IsNullOrEmpty(labelJa) ? labelJa : Loc.IsEnglish ? LabelEn(labelVi) : labelVi;

        private static string LabelEn(string vi) => vi switch
        {
            "Bảng tuyển làm thêm" => "Part-time job board",
            "bàn A" => "table A", "bàn B" => "table B", "bàn C" => "table C",
            "Quầy bếp" => "Kitchen pass",
            "Khách hàng" => "Customer",
            "Tủ đồ uống" => "Drinks fridge",
            "Kệ cơm nắm" => "Rice ball shelf",
            "Kệ bánh kẹo" => "Snack shelf",
            "Kho hàng" => "Stockroom",
            "Quầy thu ngân" => "Register",
            _ => vi,
        };

        private static string Kicker(string place) => Loc.T("LÀM THÊM", "PART-TIME JOB", "アルバイト") + " · " + place;

        public string GetPromptJa() => labelJa;
        public string GetpromptEn() => kind switch
        {
            JobStationKind.Board => Label,
            JobStationKind.StockCrate => Loc.T("Lấy thùng hàng (kho)", "Pick up a box (stockroom)", "倉庫から箱を取る"),
            JobStationKind.Shelf => Loc.F("Xếp hàng lên kệ — {0}", "Stock the shelf — {0}", "棚に並べる — {0}", Label),
            JobStationKind.Customer => Loc.T("Hỏi khách cần gì", "Ask what the customer needs", "お客さんに用件を聞く"),
            JobStationKind.Register => Loc.T("Tính tiền cho khách", "Ring up the customer", "レジで会計する"),
            JobStationKind.OrderTable => JobShift.OrderAt(stationId) == null ? Loc.F("Nhận món — {0}", "Take the order — {0}", "注文を聞く — {0}", Label) : Loc.F("Mang món tới {0}", "Bring the dish to {0}", "{0}に料理を運ぶ", Label),
            JobStationKind.DishPass => Loc.T("Lấy món ở quầy bếp", "Pick up a dish at the kitchen pass", "キッチンで料理を受け取る"),
            _ => Label,
        };
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (kind == JobStationKind.Board) { TaskJournalUI.Open(questId); return; }
            if (!IsInteractionAvailable) return;
            switch (kind)
            {
                case JobStationKind.StockCrate:
                    TimedAction.Run(Loc.T("Đang bê thùng hàng…", "Carrying a box…", "箱を運んでいます…"), Loc.T("kho", "stockroom", "倉庫"), 1.2f, () => JobShift.Carry("stock", "box", Loc.T("Thùng hàng", "Box of stock", "商品の箱")), pose: "Work_Carry");
                    break;
                case JobStationKind.Shelf:
                    TimedAction.Run(Loc.F("Đang xếp hàng lên {0}…", "Stocking the {0}…", "{0}に並べています…", Label.ToLowerInvariant()), Loc.T("tay", "by hand", "手で"), 2.5f, () =>
                    {
                        if (JobShift.CarryKind != "stock") return;
                        JobShift.DropCarry();
                        JobShift.MarkDone(stationId);
                        QuestService.Raise("restock", "konbini_shelf");
                    }, pose: "Work_Shelf");
                    break;
                case JobStationKind.Customer: AskCustomer(); break;
                case JobStationKind.Register: TimedAction.Run(Loc.T("Đang quét mã hàng…", "Scanning items…", "バーコードを読んでいます…"), Loc.T("máy quét", "scanner", "スキャナー"), 1.2f, Checkout, pose: "Work_Hold"); break;
                case JobStationKind.OrderTable:
                    if (JobShift.OrderAt(stationId) == null) TakeOrder(); else Serve();
                    break;
                case JobStationKind.DishPass: PickDish(); break;
            }
        }

        // ─────────── Konbini ───────────

        private void AskCustomer()
        {
            var products = KonbiniCatalog.Products;
            var product = products[Mathf.Abs((stationId + QuestService.State(questId).acceptedTicks).GetHashCode()) % products.Count];
            var choices = new List<JobQuizCard.Choice>();
            foreach (KonbiniSection section in System.Enum.GetValues(typeof(KonbiniSection)))
                choices.Add(new JobQuizCard.Choice
                {
                    Label = KonbiniCatalog.SectionJa(section),
                    Detail = Loc.IsJapanese ? null : KonbiniCatalog.SectionName(section),
                    Correct = section == product.section,
                    WrongHint = Loc.F("{0} ({1}) không nằm ở kệ đó.", "{0} ({1}) is not on that shelf.", "{0}はその棚にありません。", product.japanese, product.Meaning),
                });
            JobQuizCard.Show(Kicker("ひばりマート"), Loc.T("Khách hàng", "Customer", "おきゃくさん"), $"すみません、{product.japanese}は どこですか。", null,
                Loc.F("Xin lỗi, {0} ở đâu ạ?", "Excuse me, where is the {0}?", "", product.Meaning), Loc.T("Chỉ khách tới đúng kệ:", "Show the customer the right shelf:", "正しい棚を教えましょう："), choices, (System.Action)(() =>
                {
                    JobShift.MarkDone(stationId);
                    Mastery("konbini." + product.id, true);
                    QuestService.Raise("assist", "konbini_customer");
                    HudFeed.Post("「ありがとうございます！」" + Loc.T(" — Khách cảm ơn bạn.", " — The customer thanks you.", ""), HudFeed.Kind.Info, 3f);
                }), product.id);
        }

        private void Checkout()
        {
            var products = KonbiniCatalog.Products;
            int seed = Mathf.Abs((int)(QuestService.State(questId).acceptedTicks % 100000));
            var a = products[seed % products.Count];
            var b = products[(seed / 7 + 3) % products.Count];
            if (a == b) b = products[(products.ToList().IndexOf(a) + 1) % products.Count];
            int total = a.price + b.price;
            var options = new List<int> { total, total + 20, total - 30, total + 100 }.Where(v => v > 0).Distinct().Take(4).ToList();
            options = options.OrderBy(v => (v * 37) % 11).ToList();
            var choices = options.Select(v => new JobQuizCard.Choice
            {
                Label = $"{v}えん",
                Detail = Loc.Yen(v),
                Correct = v == total,
                WrongHint = Loc.F("Cộng lại nhé: {0} ¥{1} + {2} ¥{3}.", "Add it up again: {0} ¥{1} + {2} ¥{3}.", "もう一度たしましょう：{0} {1}円 + {2} {3}円", a.japanese, a.price, b.japanese, b.price),
            }).ToList();
            JobQuizCard.Show(Kicker("ひばりマート · レジ"), Loc.T("Khách hàng", "Customer", "おきゃくさん"), $"{a.japanese}と {b.japanese}を ください。",
                null, Loc.F("Cho tôi {0} và {1}.", "{0} and {1}, please.", "", a.Meaning, b.Meaning),
                Loc.F("Trên máy: {0} ¥{1}  ·  {2} ¥{3}. Tổng bao nhiêu?", "On the register: {0} ¥{1}  ·  {2} ¥{3}. What is the total?", "レジ：{0} {1}円 ・ {2} {3}円。ごうけいは いくらですか。", a.japanese, a.price, b.japanese, b.price), choices, () =>
                {
                    Mastery("number." + total, true);
                    QuestService.Raise("checkout", "konbini_register");
                    HudFeed.Post($"「{total}えん です。ありがとうございました！」", HudFeed.Kind.Info, 3f);
                });
        }

        // ─────────── Sushi restaurant ───────────

        private static List<RestaurantDish> Dishes()
        {
            if (_menu == null) _menu = Resources.Load<RestaurantMenuDefinition>("Restaurants/menu_sushi_hibari");
            return _menu != null ? _menu.dishes.Where(d => d != null && !string.IsNullOrEmpty(d.nameJa)).ToList() : new List<RestaurantDish>();
        }

        private void TakeOrder()
        {
            var dishes = Dishes();
            if (dishes.Count < 3) { HudFeed.Post(Loc.T("Thực đơn chưa sẵn sàng.", "The menu is not ready yet.", "メニューの準備ができていません。"), HudFeed.Kind.Warning); return; }
            int seed = Mathf.Abs((stationId + QuestService.State(questId).acceptedTicks).GetHashCode());
            var dish = dishes[seed % dishes.Count];
            var options = new List<RestaurantDish> { dish, dishes[(seed / 3 + 1) % dishes.Count], dishes[(seed / 5 + 2) % dishes.Count], dishes[(seed / 7 + 4) % dishes.Count] }
                .GroupBy(d => d.id).Select(g => g.First()).OrderBy(d => (d.id.GetHashCode() & 0x7fffffff) % 13).ToList();
            var choices = options.Select(d => new JobQuizCard.Choice
            {
                Label = Loc.IsJapanese ? d.nameJa : Loc.IsEnglish ? d.nameEn : d.nameVi,
                Detail = Loc.IsJapanese ? d.reading : null,
                IconItemId = "sushi_" + d.id,
                Correct = d.id == dish.id,
                WrongHint = Loc.F("Khách nói 「{0}」 — đọc là {1}.", "The customer said 「{0}」 — read as {1}.", "お客さんは「{0}」（{1}）と言いました。", dish.nameJa, dish.reading),
            }).ToList();
            JobQuizCard.Show(Kicker("ひばり寿司 · " + Label), Loc.T("Khách", "Guest", "おきゃくさん"), $"すみません、{dish.nameJa}を ひとつ ください。", $"sumimasen, {dish.romaji} o hitotsu kudasai",
                Loc.F("Cho tôi một phần {0}.", "One {0}, please.", "", Loc.IsEnglish ? dish.nameEn : dish.nameVi), Loc.T("Khách gọi món gì? Ghi đúng phiếu:", "What did the guest order? Write the right ticket:", "何を注文しましたか。正しく伝票に書きましょう："), choices, () =>
                {
                    JobShift.SetOrder(stationId, dish.id);
                    Mastery("food." + dish.id, true);
                    QuestService.Raise("order", "sushi_table");
                    HudFeed.Post(Loc.F("Phiếu {0}: {1} ({2}). Lấy món ở quầy bếp.", "Ticket {0}: {1} ({2}). Pick it up at the kitchen pass.", "{0}の伝票：{1}。キッチンで受け取りましょう。", Label, dish.nameJa, Loc.IsEnglish ? dish.nameEn : dish.nameVi), HudFeed.Kind.Info, 4f);
                });
        }

        private void PickDish()
        {
            var dishes = Dishes();
            var pending = JobShift.Orders.Select(o => o.Value).Distinct().ToList();
            var options = dishes.Where(d => pending.Contains(d.id)).Concat(dishes.Where(d => !pending.Contains(d.id)).Take(2)).OrderBy(d => (d.id.GetHashCode() & 0x7fffffff) % 7).ToList();
            var choices = options.Select(d => new JobQuizCard.Choice
            {
                Id = d.id,
                IconItemId = "sushi_" + d.id,
                Label = d.nameJa,
                Detail = d.reading,
                Correct = pending.Contains(d.id),
                WrongHint = Loc.T("Chưa bàn nào gọi món này — xem lại phiếu gọi món.", "No table ordered this — check the order tickets.", "この料理はまだ注文されていません。伝票を確認しましょう。"),
            }).ToList();
            JobQuizCard.Show(Kicker("ひばり寿司 · キッチン"), Loc.T("Đầu bếp Ota", "Chef Ota", "いたまえの太田さん"), "どれを もって いきますか。", "dore o motte ikimasu ka", Loc.T("Bạn mang món nào đi?", "Which dish will you take?", ""),
                Loc.T("Chọn món đã có người gọi (tên tiếng Nhật):", "Pick a dish someone ordered (Japanese name):", "注文された料理を選びましょう："), choices, chosen => OnDishChosen(chosen?.Id));
        }

        private void Serve()
        {
            string order = JobShift.OrderAt(stationId);
            if (JobShift.CarryKind != "dish") return;
            if (JobShift.CarryId != order)
            {
                var wanted = Dishes().FirstOrDefault(d => d.id == order);
                HudFeed.Post(Loc.F("Bàn này gọi {0} ({1}) — món bạn cầm là của bàn khác.", "This table ordered {0} ({1}) — the dish you have is for another table.", "このテーブルの注文は{0}です。持っている料理は別のテーブルのものです。", wanted?.nameJa, Loc.IsEnglish ? wanted?.nameEn : wanted?.nameVi), HudFeed.Kind.Warning, 4f);
                return;
            }
            TimedAction.Run(Loc.F("Đang bưng món tới {0}…", "Bringing the dish to {0}…", "{0}に運んでいます…", Label), Loc.T("khay", "tray", "おぼん"), 1f, () =>
            {
                if (JobShift.CarryId != order) return;
                JobShift.DropCarry();
                JobShift.SetOrder(stationId, null);
                JobShift.MarkDone(stationId);
                QuestService.Raise("serve", "sushi_table");
                HudFeed.Post("「おまたせしました！」" + Loc.T(" — Món đã lên bàn.", " — The dish is served.", ""), HudFeed.Kind.Info, 3f);
            }, pose: "Work_Hold");
        }

        private void OnDishChosen(string dishId)
        {
            var dish = Dishes().FirstOrDefault(d => d.id == dishId);
            if (dish == null) return;
            TimedAction.Run(Loc.F("Đang lấy {0}…", "Picking up {0}…", "{0}を受け取っています…", dish.nameJa), Loc.T("khay", "tray", "おぼん"), 0.8f, () => JobShift.Carry("dish", dish.id, dish.nameJa), pose: "Work_Hold");
        }

        private static void Mastery(string target, bool correct)
        {
            var mastery = Object.FindFirstObjectByType<LearningMasteryManager>();
            if (mastery != null) mastery.RegisterUsage(target, correct);
        }
    }
}
