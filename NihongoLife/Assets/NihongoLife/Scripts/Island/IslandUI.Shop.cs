using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Island
{
    /// <summary>Partial of IslandUI: island shop, price guide, and shop entry list. The MonoBehaviour entry point stays IslandUI.cs.</summary>
    public sealed partial class IslandUI : MonoBehaviour
    {
        // ─────────── Shop ───────────

        public static bool ShopOpen => _instance != null && _instance._shop != null;
        private string _shopTab = "agriculture";
        private string _shopSelected;
        private int _shopQty = 1;
        private TextMeshProUGUI _shopFeedback;

        public static void OpenShop()
        {
            var ui = Instance;
            bool wasOpen = ui._shop != null;
            ui.BuildShop(null, false);
            if (!wasOpen)
            {
                ui.LockPlayer(true);
                UiModalStack.Register(ui, () => ui._shop != null, () => ui.Close(ref ui._shop), "Island shop");
                IslandState.Discover("place:shop");
            }
        }

        /// <summary>Opens the store on a tab with an item selected (e.g. from the farm card's "Store" button).</summary>
        public static void OpenShopAt(string tab, string itemId)
        {
            var ui = Instance;
            ui._shopTab = tab;
            ui._shopSelected = itemId;
            ui._shopQty = 1;
            OpenShop();
        }

        public static void CloseShop() { if (_instance != null) _instance.Close(ref _instance._shop); }

        public sealed class ShopEntry { public string ItemId; public IslandWord Word; public int Price; public string Info; public bool Sell; }

        public static List<ShopEntry> Entries(string tab)
        {
            var c = IslandCatalog.Load();
            var list = new List<ShopEntry>();
            switch (tab)
            {
                case "agriculture":
                    foreach (var crop in c.crops)
                        list.Add(new ShopEntry { ItemId = crop.SeedItemId, Word = crop.word, Price = crop.seedPrice,
                            Info = Loc.F("Hạt giống · lớn trong {0} giây · thu {1} quả, bán ¥{2}/quả · lãi ¥{3}/ô", "Seeds · grow in {0}s · yield {1}, sell ¥{2} each · profit ¥{3} per plot",
                                "種 · {0}秒で育つ · {1}個収穫、1個¥{2}で売れる · 1区画の利益¥{3}", Mathf.RoundToInt(crop.secondsPerStage * 3), crop.yield, crop.sellPrice, crop.yield * crop.sellPrice - crop.seedPrice) });
                    foreach (var tool in c.tools)
                        list.Add(new ShopEntry { ItemId = tool.id, Word = tool.word, Price = tool.price,
                            Info = Loc.F("Dụng cụ · {0} · bền {1} lần dùng (≈¥{2:0.#}/lần)", "Tool · {0} · lasts {1} uses (≈¥{2:0.#} per use)", "道具 · {0} · {1}回使える（1回あたり約¥{2:0.#}）",
                                IslandLanguage.Name(tool.verb), tool.durability, tool.durability > 0 ? tool.price / (float)tool.durability : tool.price)
                                + (IslandEconomy.Owned(tool.id) > 0 ? "  ·  " + Loc.F("cái đang dùng: {0}/{1}", "current one: {0}/{1}", "使用中：{0}/{1}", IslandTools.Left(tool.id), tool.durability) : "") });
                    break;
                case "technology":
                    foreach (var p in c.products.Where(p => p.category == "technology"))
                        list.Add(new ShopEntry { ItemId = p.itemId, Word = p.word, Price = p.price, Info = p.Note });
                    break;
                case "decor":
                    foreach (var d in c.decor ?? Array.Empty<IslandDecorDef>())
                        list.Add(new ShopEntry { ItemId = d.ItemId, Word = d.word, Price = d.price,
                            Info = Loc.F("Đồ trang trí · vào kho trang trí (không chiếm balo) · đang có {0}, đã đặt {1}. Bấm \"Trang trí\" trên thanh công cụ để đặt.",
                                "Decoration · goes to your decor stock (not the bag) · {0} in stock, {1} placed. Press \"Decorate\" on the toolbar to place it.",
                                "かざり · かざりの倉庫へ（かばんは使わない）· 在庫{0}、設置{1}。ツールバーの「かざる」で置けます。",
                                IslandDecor.Stock(d.id), IslandDecor.Placed.Count(p => p.id == d.id)) });
                    break;
                case "sell":
                    foreach (var crop in c.crops.Where(cr => IslandEconomy.Owned(cr.ProduceItemId) > 0))
                        list.Add(new ShopEntry { ItemId = crop.ProduceItemId, Word = crop.word, Price = crop.sellPrice, Sell = true,
                            Info = Loc.F("Nông sản của bạn · đang có {0}", "Your produce · you have {0}", "あなたの作物 · 所持{0}", IslandEconomy.Owned(crop.ProduceItemId)) });
                    foreach (var fish in (c.fish ?? Array.Empty<IslandFish>()).Where(f => IslandEconomy.Owned(f.id) > 0))
                        list.Add(new ShopEntry { ItemId = fish.id, Word = fish.word, Price = fish.sellPrice, Sell = true,
                            Info = Loc.F("Cá bạn câu được · đang có {0}", "Fish you caught · you have {0}", "釣った魚 · 所持{0}", IslandEconomy.Owned(fish.id)) });
                    break;
            }
            return list;
        }

        public static void SelectShopTab(string tab) { var ui = Instance; ui._shopTab = tab; ui._shopSelected = null; ui._shopQty = 1; if (ui._shop != null) ui.BuildShop(null, false); }
        public static void SelectShopItem(string itemId) { var ui = Instance; ui._shopSelected = itemId; ui._shopQty = 1; if (ui._shop != null) ui.BuildShop(null, false); }
        public static void SetShopQuantity(int qty) { var ui = Instance; ui._shopQty = Mathf.Clamp(qty, 1, 20); if (ui._shop != null) ui.BuildShop(null, false); }

        /// <summary>Buys or sells the selected entry (also used by tests); returns the economy result.</summary>
        public static IslandEconomy.Result ConfirmShop()
        {
            var ui = Instance;
            var entry = Entries(ui._shopTab).FirstOrDefault(e => e.ItemId == ui._shopSelected);
            if (entry == null) return IslandEconomy.Result.Unknown;
            var verbs = IslandCatalog.Load().verbs;
            var result = entry.Sell ? IslandEconomy.Sell(entry.ItemId, entry.Price, ui._shopQty)
                : ui._shopTab == "decor" ? IslandDecor.Buy(entry.ItemId, ui._shopQty)
                : IslandEconomy.Buy(entry.ItemId, entry.Price, ui._shopQty);
            string name = IslandLanguage.Name(entry.Word);
            string message = result switch
            {
                IslandEconomy.Result.Ok when entry.Sell => Loc.F("Đã bán {0} {1} · +¥{2:N0}.", "Sold {0} {1} · +¥{2:N0}.", "{1}を{0}個売りました · +¥{2:N0}。", ui._shopQty, name, entry.Price * ui._shopQty),
                IslandEconomy.Result.Ok => Loc.F("Đã mua {0} {1} · −¥{2:N0}.", "Bought {0} {1} · −¥{2:N0}.", "{1}を{0}個買いました · −¥{2:N0}。", ui._shopQty, name, entry.Price * ui._shopQty),
                IslandEconomy.Result.NoMoney => Loc.F("Không đủ tiền: cần ¥{0:N0}, bạn có ¥{1:N0}.", "Not enough money: ¥{0:N0} needed, you have ¥{1:N0}.", "お金が足りません：¥{0:N0}必要、所持¥{1:N0}。", entry.Price * ui._shopQty, IslandEconomy.Yen),
                IslandEconomy.Result.BagFull => Loc.T("Balo đầy — bỏ bớt đồ hoặc bán nông sản.", "Your bag is full — drop something or sell produce.", "バッグがいっぱいです。物を減らすか作物を売りましょう。"),
                IslandEconomy.Result.NotOwned => Loc.T("Bạn không có đủ món này để bán.", "You do not have enough of this to sell.", "売る数が足りません。"),
                _ => Loc.T("Không thực hiện được.", "That did not work.", "できませんでした。"),
            };
            if (result == IslandEconomy.Result.Ok)
            {
                IslandState.Discover(entry.Sell ? "verb:" + verbs.sell.en : "verb:" + verbs.buy.en);
                IslandState.Discover(WordKey(entry.ItemId));
                if (entry.Sell && IslandEconomy.Owned(entry.ItemId) == 0) ui._shopSelected = null;
            }
            if (ui._shop != null) ui.BuildShop(message, result != IslandEconomy.Result.Ok);
            return result;
        }

        private static readonly (string id, string icon, string vi, string en, string ja)[] ShopTabs =
        {
            ("agriculture", "agriculture", "Nông nghiệp", "Farming", "農業"),
            ("fashion", "checkroom", "Thời trang", "Fashion", "ファッション"),
            ("decor", "yard", "Trang trí", "Decor", "かざり"),
            ("technology", "devices", "Công nghệ", "Technology", "テクノロジー"),
            ("sell", "sell", "Bán", "Sell", "売る"),
            ("prices", "price_check", "Bảng giá & lãi", "Prices & profit", "価格と利益"),
        };

        private void BuildShop(string feedback, bool error)
        {
            Discard(_shop);
            _shop = Card("IslandShop", new Vector2(0.5f, 0.5f), Vector2.zero, 1180f);
            var header = Header(_shop, "storefront", Loc.T("Cửa hàng Midori", "Midori Store", "みどりしょうてん"), Loc.T("ĐẢO XANH", "MIDORI ISLAND", "みどりじま"), CloseShop);
            var wallet = NLUi.Panel(header, "Wallet", new Color(0f, 0f, 0f, 0.22f), new RectOffset(12, 16, 6, 6), 6f, vertical: false);
            wallet.GetComponent<Image>().sprite = UiKit.Rounded(16);
            wallet.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            NLUi.Anchor(wallet, new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(150f, 40f));
            wallet.pivot = new Vector2(1f, 0.5f);
            ((HorizontalLayoutGroup)wallet.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            Coin(wallet, IslandEconomy.Yen, 19f, new Color(1f, 0.92f, 0.62f));

            var main = NLUi.Group(_shop, "Main", false, 18f, TextAnchor.UpperLeft, false);
            main.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(24, 24, 18, 0);
            // Tabs
            var tabs = NLUi.Group(main, "Tabs", true, 8f);
            NLUi.Size(tabs, 210f);
            foreach (var (id, icon, vi, en, ja) in ShopTabs)
            {
                bool on = _shopTab == id;
                string captured = id;
                var tab = IconBtn(tabs, "Tab_" + id, icon, Loc.T(vi, en, ja), () => SelectShopTab(captured), on ? Green : Cream2, on ? Color.white : Ink, 52f, 15.5f);
            }

            if (_shopTab == "prices") { BuildPriceGuide(main); FinishShop(feedback, error); return; }

            // Grid
            var gridHost = NLUi.Group(main, "GridHost", true, 10f);
            NLUi.Size(gridHost, 540f);
            var entries = Entries(_shopTab);
            if (_shopTab == "fashion")
            {
                var empty = NLUi.Panel(gridHost, "Empty", Cream2, new RectOffset(20, 20, 18, 18), 6f);
                empty.GetComponent<Image>().sprite = UiKit.Rounded(14);
                Text(empty, Loc.T("Chưa có hàng thời trang", "No fashion items yet", "ファッション商品はまだありません"), 20f, Ink, FontStyles.Bold);
                Text(empty, Loc.T("Dự án chưa có mô hình quần áo / phụ kiện nên mục này để trống thay vì bán đồ không có thật. Trang phục sẽ được thêm khi có mô hình phù hợp.",
                    "There are no clothing models yet, so this shelf stays empty instead of selling things that do not exist. Outfits will come with suitable models.",
                    "服やアクセサリーのモデルがまだないため、存在しない商品は売らずに空けてあります。合うモデルができたら追加します。"), 15f, Muted);
            }
            else if (entries.Count == 0)
            {
                var empty = NLUi.Panel(gridHost, "Empty", Cream2, new RectOffset(20, 20, 18, 18), 6f);
                empty.GetComponent<Image>().sprite = UiKit.Rounded(14);
                Text(empty, _shopTab == "sell" ? Loc.T("Chưa có nông sản để bán — thu hoạch ở ruộng trước nhé.", "Nothing to sell yet — harvest in the fields first.", "まだ売るものがありません — 先に畑で収穫しましょう。")
                                               : Loc.T("Chưa có mặt hàng.", "No items yet.", "商品がありません。"), 17f, Muted);
            }
            else
            {
                var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                gridGo.transform.SetParent(gridHost, false);
                var grid = gridGo.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(126f, 158f); grid.spacing = new Vector2(10f, 10f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
                gridGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                foreach (var e in entries)
                {
                    bool selected = e.ItemId == _shopSelected;
                    string id = e.ItemId;
                    var cell = new GameObject("Item_" + e.ItemId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
                    cell.transform.SetParent(gridGo.transform, false);
                    var bg = cell.GetComponent<Image>();
                    bg.sprite = UiKit.Rounded(14);
                    bg.type = Image.Type.Sliced;
                    bg.color = selected ? GreenSoft : Color.white;
                    if (selected) UiKit.Border((RectTransform)cell.transform, Green, 14);
                    cell.GetComponent<Button>().onClick.AddListener(() => SelectShopItem(id));
                    var v = cell.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(8, 8, 10, 8); v.spacing = 3f; v.childAlignment = TextAnchor.UpperCenter; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
                    Icon((RectTransform)cell.transform, e.ItemId, 72f);
                    var name = Text((RectTransform)cell.transform, IslandLanguage.Capitalize(ItemName(e.ItemId)), 14.5f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
                    name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
                    var priceRow = NLUi.Group((RectTransform)cell.transform, "Price", false, 4f, TextAnchor.MiddleCenter, false);
                    Coin(priceRow, e.Price, 15f, e.Sell ? Green : new Color(0.62f, 0.42f, 0.06f), e.Sell);
                }
            }

            // Detail
            var detail = NLUi.Panel(main, "Detail", Cream2, new RectOffset(20, 20, 18, 18), 10f);
            detail.GetComponent<Image>().sprite = UiKit.Rounded(16);
            NLUi.Size(detail, 300f);
            var entry = entries.FirstOrDefault(e => e.ItemId == _shopSelected);
            if (entry == null)
            {
                Text(detail, _shopTab == "sell" ? Loc.T("Chọn nông sản để bán.", "Pick something to sell.", "売るものを選びましょう。")
                                                : Loc.T("Chọn một món để xem chi tiết.", "Pick an item to see the details.", "商品を選ぶと詳しく表示されます。"), 16f, Muted);
            }
            else
            {
                var iconRow = NLUi.Group(detail, "IconRow", false, 0f, TextAnchor.MiddleCenter, false);
                Icon(iconRow, entry.ItemId, 118f);
                Text(detail, IslandLanguage.Capitalize(ItemName(entry.ItemId)), 22f, Ink, FontStyles.Bold);
                WordChip(detail, entry.Word, 17f);
                Text(detail, entry.Info, 14f, Muted);
                Text(detail, Loc.F("Đang có: <b>{0}</b>", "You have: <b>{0}</b>", "所持：<b>{0}</b>", IslandEconomy.Owned(entry.ItemId)), 15f, Ink);
                var qtyRow = NLUi.Group(detail, "Qty", false, 8f, TextAnchor.MiddleCenter, false);
                var minus = Btn(qtyRow, "−", () => SetShopQuantity(_shopQty - 1), Color.white, Ink, 40f, 20f); NLUi.Size(minus, 46f, 40f);
                var q = Text(qtyRow, _shopQty.ToString(), 20f, Ink, FontStyles.Bold, TextAlignmentOptions.Center); NLUi.Size(q, 50f);
                var plus = Btn(qtyRow, "+", () => SetShopQuantity(_shopQty + 1), Color.white, Ink, 40f, 20f); NLUi.Size(plus, 46f, 40f);
                bool canAfford = entry.Sell ? IslandEconomy.Owned(entry.ItemId) >= _shopQty : IslandEconomy.Yen >= entry.Price * _shopQty;
                var totalRow = NLUi.Group(detail, "Total", false, 6f, TextAnchor.MiddleLeft, false);
                Text(totalRow, entry.Sell ? Loc.T("Bạn nhận", "You get", "受け取り") : Loc.T("Tổng", "Total", "合計"), 15f, Muted);
                Coin(totalRow, entry.Price * _shopQty, 20f, entry.Sell ? Green : new Color(0.62f, 0.42f, 0.06f), entry.Sell);
                var confirm = IconBtn(detail, "Confirm", entry.Sell ? "sell" : "shopping_cart", entry.Sell ? Loc.T("Bán", "Sell", "売る") : Loc.T("Mua", "Buy", "買う"),
                    () => ConfirmShop(), canAfford ? Green : new Color(0.62f, 0.64f, 0.6f), Color.white, 52f, 18f);
                if (!canAfford) Text(detail, entry.Sell ? Loc.T("Không đủ số lượng.", "Not enough to sell.", "数が足りません。")
                                                        : Loc.F("Thiếu ¥{0:N0}.", "¥{0:N0} short.", "¥{0:N0}足りません。", entry.Price * _shopQty - IslandEconomy.Yen), 14f, Error);
            }
            FinishShop(feedback, error);
        }

        private void FinishShop(string feedback, bool error)
        {
            _shopFeedback = Text(_shop, feedback ?? "", 15.5f, error ? Error : Green, FontStyles.Bold, TextAlignmentOptions.Center);
            _shopFeedback.margin = new Vector4(24f, 6f, 24f, 0f);
        }

        /// <summary>
        /// How money works on the island, in one table: what each seed costs, how long it grows, what it sells for and
        /// the profit per plot; what each tool costs per use; what fish sell for; the return ticket.
        /// </summary>
        private void BuildPriceGuide(RectTransform main)
        {
            var c = IslandCatalog.Load();
            var host = NLUi.Group(main, "PriceGuide", true, 10f);
            NLUi.Size(host, 860f);
            Text(host, Loc.T("Trồng gì có lãi?", "What pays to grow?", "何を育てるともうかる？"), 19f, Ink, FontStyles.Bold);
            var table = NLUi.Group(host, "Crops", true, 4f);
            Row(table, true, Loc.T("Cây", "Crop", "作物"), Loc.T("Hạt", "Seed", "種"), Loc.T("Thời gian", "Time", "時間"), Loc.T("Thu × giá bán", "Yield × price", "収穫 × 単価"), Loc.T("Lãi / ô", "Profit / plot", "1区画の利益"), null);
            var best = c.crops.OrderByDescending(x => x.yield * x.sellPrice - x.seedPrice).First();
            foreach (var crop in c.crops)
            {
                int profit = crop.yield * crop.sellPrice - crop.seedPrice;
                Row(table, false, IslandLanguage.Title(crop.word), $"¥{crop.seedPrice}", Loc.F("{0} giây", "{0}s", "{0}秒", Mathf.RoundToInt(crop.secondsPerStage * 3)),
                    $"{crop.yield} × {NihongoLife.Core.Loc.Money(crop.sellPrice)}", (profit >= 0 ? "+" : "") + $"{NihongoLife.Core.Loc.Money(profit)}", crop.SeedItemId, crop == best);
            }
            Text(host, Loc.T("Dụng cụ (dùng mòn dần)", "Tools (wear out with use)", "道具（使うと減ります）"), 17f, Ink, FontStyles.Bold);
            var tools = NLUi.Group(host, "Tools", true, 4f);
            Row(tools, true, Loc.T("Dụng cụ", "Tool", "道具"), Loc.T("Giá", "Price", "価格"), Loc.T("Số lần", "Uses", "回数"), Loc.T("Mỗi lần", "Per use", "1回あたり"), Loc.T("Dùng để", "For", "用途"), null);
            foreach (var tool in c.tools)
                Row(tools, false, IslandLanguage.Title(tool.word), $"{NihongoLife.Core.Loc.Money(tool.price)}", tool.durability > 0 ? tool.durability.ToString() : "∞",
                    tool.durability > 0 ? $"¥{tool.price / (float)tool.durability:0.#}" : "—", IslandLanguage.Name(tool.verb), tool.id);
            var tips = NLUi.Panel(host, "Tips", GreenSoft, new RectOffset(18, 18, 12, 14), 4f);
            tips.GetComponent<Image>().sprite = UiKit.Rounded(14);
            Text(tips, Loc.T("Quy tắc tiền trên đảo", "How money works here", "島のお金のルール"), 14.5f, Green, FontStyles.Bold);
            Text(tips, Loc.F("• Quà chào mừng: cuốc, bình tưới và 3 túi hạt cà rốt — không cần mua gì để bắt đầu.\n• Mua hạt → gieo → tưới 3 lần → thu hoạch → bán ở đây. {0} lãi nhiều nhất mỗi ô.\n• Dụng cụ hỏng sau số lần dùng; giá mỗi lần nằm trong lãi. Cá câu được bán ¥{1}–¥{2}.\n• Vé tàu về Hibari: ¥{3}. Mọi khoản thu chi đều ghi trong lịch sử ví.",
                    "• Welcome gift: a hoe, a watering can and 3 bags of carrot seeds — nothing to buy to start.\n• Buy seeds → plant → water 3 times → harvest → sell here. {0} earns the most per plot.\n• Tools break after their uses; their cost per use is part of the profit. Fish sell for ¥{1}–¥{2}.\n• Train ticket to Hibari: ¥{3}. Every income and expense is in the wallet history.",
                    "• ようこそギフト：くわ、じょうろ、にんじんの種3袋 — 何も買わずに始められます。\n• 種を買う → まく → 水やり3回 → 収穫 → ここで売る。1区画で一番もうかるのは{0}。\n• 道具は回数を使うと壊れます。1回あたりの費用も利益に入ります。魚は¥{1}〜¥{2}で売れます。\n• ひばり行きの切符：¥{3}。収入と支出はすべて財布の履歴に残ります。",
                    IslandLanguage.Name(best.word), (c.fish ?? Array.Empty<IslandFish>()).Select(f => f.sellPrice).DefaultIfEmpty(0).Min(), (c.fish ?? Array.Empty<IslandFish>()).Select(f => f.sellPrice).DefaultIfEmpty(0).Max(), c.ticketPrice), 14f, Ink);
        }

        private void Row(RectTransform table, bool head, string a, string b, string cText, string d, string e, string iconItem, bool highlight = false)
        {
            var row = NLUi.Panel(table, head ? "Head" : "Row", head ? new Color(0f, 0f, 0f, 0.06f) : highlight ? GreenSoft : Color.white, new RectOffset(12, 12, 6, 6), 10f, vertical: false);
            row.GetComponent<Image>().sprite = UiKit.Rounded(10);
            ((HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            if (!head && iconItem != null) Icon(row, iconItem, 30f);
            else if (!head) { var sp = UiKit.Rect(row, "Spacer"); UiKit.Size(sp, 30f, 30f); }
            else { var sp = UiKit.Rect(row, "Spacer"); UiKit.Size(sp, 30f, 18f); }
            float[] widths = { 170f, 90f, 110f, 170f, 200f };
            string[] cells = { a, b, cText, d, e };
            for (int i = 0; i < cells.Length; i++)
            {
                var t = Text(row, cells[i] ?? "", head ? 13f : 15f, head ? Muted : i == 4 && !head ? Green : Ink, head || i == 0 || i == 4 ? FontStyles.Bold : FontStyles.Normal);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Ellipsis;
                NLUi.Size(t, widths[i]);
            }
        }
    }
}
