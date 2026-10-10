using System.Collections.Generic;
using System.Linq;
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

        public string GetPromptJa() => labelJa;
        public string GetpromptEn() => kind switch
        {
            JobStationKind.Board => labelVi,
            JobStationKind.StockCrate => "Lấy thùng hàng (kho)",
            JobStationKind.Shelf => $"Xếp hàng lên kệ — {labelVi}",
            JobStationKind.Customer => "Hỏi khách cần gì",
            JobStationKind.Register => "Tính tiền cho khách",
            JobStationKind.OrderTable => JobShift.OrderAt(stationId) == null ? $"Nhận món — {labelVi}" : $"Mang món tới {labelVi}",
            JobStationKind.DishPass => "Lấy món ở quầy bếp",
            _ => labelVi,
        };
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (kind == JobStationKind.Board) { TaskJournalUI.Open(questId); return; }
            if (!IsInteractionAvailable) return;
            switch (kind)
            {
                case JobStationKind.StockCrate:
                    TimedAction.Run("Đang bê thùng hàng…", "kho", 1.2f, () => JobShift.Carry("stock", "box", "Thùng hàng"));
                    break;
                case JobStationKind.Shelf:
                    TimedAction.Run($"Đang xếp hàng lên {labelVi.ToLowerInvariant()}…", "tay", 2.5f, () =>
                    {
                        if (JobShift.CarryKind != "stock") return;
                        JobShift.DropCarry();
                        JobShift.MarkDone(stationId);
                        QuestService.Raise("restock", "konbini_shelf");
                    });
                    break;
                case JobStationKind.Customer: AskCustomer(); break;
                case JobStationKind.Register: TimedAction.Run("Đang quét mã hàng…", "máy quét", 1.2f, Checkout); break;
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
                    Detail = KonbiniCatalog.SectionVi(section),
                    Correct = section == product.section,
                    WrongHint = $"{product.japanese} ({product.vietnamese}) không nằm ở kệ đó.",
                });
            JobQuizCard.Show("LÀM THÊM · ひばりマート", "おきゃくさん · Khách hàng", $"すみません、{product.japanese}は どこですか。", null,
                $"Xin lỗi, {product.vietnamese} ở đâu ạ?", "Chỉ khách tới đúng kệ:", choices, (System.Action)(() =>
                {
                    JobShift.MarkDone(stationId);
                    Mastery("konbini." + product.id, true);
                    QuestService.Raise("assist", "konbini_customer");
                    HudFeed.Post("「ありがとうございます！」 — Khách cảm ơn bạn.", HudFeed.Kind.Info, 3f);
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
                Detail = $"¥{v}",
                Correct = v == total,
                WrongHint = $"Cộng lại nhé: {a.japanese} ¥{a.price} + {b.japanese} ¥{b.price}.",
            }).ToList();
            JobQuizCard.Show("LÀM THÊM · ひばりマート · レジ", "おきゃくさん · Khách hàng", $"{a.japanese}と {b.japanese}を ください。",
                null, $"Cho tôi {a.vietnamese} và {b.vietnamese}.",
                $"Trên máy: {a.japanese} ¥{a.price}  ·  {b.japanese} ¥{b.price}. ごうけいは いくらですか — tổng bao nhiêu?", choices, () =>
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
            if (dishes.Count < 3) { HudFeed.Post("Thực đơn chưa sẵn sàng.", HudFeed.Kind.Warning); return; }
            int seed = Mathf.Abs((stationId + QuestService.State(questId).acceptedTicks).GetHashCode());
            var dish = dishes[seed % dishes.Count];
            var options = new List<RestaurantDish> { dish, dishes[(seed / 3 + 1) % dishes.Count], dishes[(seed / 5 + 2) % dishes.Count], dishes[(seed / 7 + 4) % dishes.Count] }
                .GroupBy(d => d.id).Select(g => g.First()).OrderBy(d => (d.id.GetHashCode() & 0x7fffffff) % 13).ToList();
            var choices = options.Select(d => new JobQuizCard.Choice
            {
                Label = d.nameVi,
                Detail = d.nameEn,
                IconItemId = "sushi_" + d.id,
                Correct = d.id == dish.id,
                WrongHint = $"Khách nói 「{dish.nameJa}」 — đọc là {dish.reading}.",
            }).ToList();
            JobQuizCard.Show($"LÀM THÊM · ひばり寿司 · {labelVi}", "おきゃくさん · Khách", $"すみません、{dish.nameJa}を ひとつ ください。", $"sumimasen, {dish.romaji} o hitotsu kudasai",
                $"Cho tôi một phần {dish.nameVi}.", "Khách gọi món gì? Ghi đúng phiếu:", choices, () =>
                {
                    JobShift.SetOrder(stationId, dish.id);
                    Mastery("food." + dish.id, true);
                    QuestService.Raise("order", "sushi_table");
                    HudFeed.Post($"Phiếu {labelVi}: {dish.nameJa} ({dish.nameVi}). Lấy món ở quầy bếp.", HudFeed.Kind.Info, 4f);
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
                WrongHint = "Chưa bàn nào gọi món này — xem lại phiếu gọi món.",
            }).ToList();
            JobQuizCard.Show("LÀM THÊM · ひばり寿司 · キッチン", "いたまえ · Đầu bếp Ota", "どれを もって いきますか。", "dore o motte ikimasu ka", "Bạn mang món nào đi?",
                "Chọn món đã có người gọi (tên tiếng Nhật):", choices, chosen => OnDishChosen(chosen?.Id));
        }

        private void Serve()
        {
            string order = JobShift.OrderAt(stationId);
            if (JobShift.CarryKind != "dish") return;
            if (JobShift.CarryId != order)
            {
                var wanted = Dishes().FirstOrDefault(d => d.id == order);
                HudFeed.Post($"Bàn này gọi {wanted?.nameJa} ({wanted?.nameVi}) — món bạn cầm là của bàn khác.", HudFeed.Kind.Warning, 4f);
                return;
            }
            TimedAction.Run($"Đang bưng món tới {labelVi}…", "khay", 1f, () =>
            {
                if (JobShift.CarryId != order) return;
                JobShift.DropCarry();
                JobShift.SetOrder(stationId, null);
                JobShift.MarkDone(stationId);
                QuestService.Raise("serve", "sushi_table");
                HudFeed.Post("「おまたせしました！」 — Món đã lên bàn.", HudFeed.Kind.Info, 3f);
            });
        }

        private void OnDishChosen(string dishId)
        {
            var dish = Dishes().FirstOrDefault(d => d.id == dishId);
            if (dish == null) return;
            TimedAction.Run($"Đang lấy {dish.nameJa}…", "khay", 0.8f, () => JobShift.Carry("dish", dish.id, dish.nameJa));
        }

        private static void Mastery(string target, bool correct)
        {
            var mastery = Object.FindFirstObjectByType<LearningMasteryManager>();
            if (mastery != null) mastery.RegisterUsage(target, correct);
        }
    }
}
