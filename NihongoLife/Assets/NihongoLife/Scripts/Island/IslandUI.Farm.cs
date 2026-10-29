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
    /// <summary>Partial of IslandUI: farm card, growth, and the farm Update tick. The MonoBehaviour entry point stays IslandUI.cs.</summary>
    public sealed partial class IslandUI : MonoBehaviour
    {
        // ─────────── Farm card ───────────

        private FarmPlot _plot;
        private RectTransform _growFill;
        private TextMeshProUGUI _growText, _farmFeedback;

        public static bool FarmOpen => _instance != null && _instance._farmCard != null;
        public static FarmPlot OpenPlot => _instance != null ? _instance._plot : null;

        public static void OpenFarm(FarmPlot plot)
        {
            var ui = Instance;
            bool wasOpen = ui._farmCard != null;
            ui._plot = plot;
            ui.BuildFarm(null, false);
            if (!wasOpen)
            {
                ui.LockPlayer(true);
                UiModalStack.Register(ui, () => ui._farmCard != null, () => ui.Close(ref ui._farmCard), "Farm");
            }
        }

        public static void CloseFarm() { if (_instance != null) _instance.Close(ref _instance._farmCard); }

        private static readonly (string icon, string vi, string en, string ja)[] FarmSteps =
        {
            ("agriculture", "Xới", "Till", "耕す"), ("potted_plant", "Gieo", "Plant", "まく"), ("water_drop", "Tưới", "Water", "水"),
            ("eco", "Lớn", "Grow", "育つ"), ("nutrition", "Thu", "Harvest", "収穫"),
        };

        private static int StepOf(FarmPlot.Phase phase) => phase switch
        {
            FarmPlot.Phase.Untilled => 0,
            FarmPlot.Phase.Tilled => 1,
            FarmPlot.Phase.NeedsWater => 2,
            FarmPlot.Phase.Growing => 3,
            _ => 4,
        };

        private void BuildFarm(string feedback, bool error)
        {
            Discard(_farmCard);
            var plot = _plot;
            var catalog = IslandCatalog.Load();
            var phase = plot.CurrentPhase;
            _lastPhase = phase; // the card always reflects this phase; Update rebuilds only on a real change
            var crop = plot.Crop;
            _farmCard = Card("FarmCard", new Vector2(1f, 0.5f), new Vector2(-28f, 10f), 500f);
            Header(_farmCard, "agriculture", Loc.F("Ô ruộng {0}", "Field {0}", "はたけ {0}", plot.Number), Loc.T("NÔNG TRẠI ĐẢO XANH", "MIDORI FARM", "みどり農園"), CloseFarm);
            var body = Body(_farmCard);

            // Stepper: till → plant → water → grow → harvest.
            var steps = NLUi.Group(body, "Steps", false, 0f, TextAnchor.MiddleCenter, true);
            int current = StepOf(phase);
            for (int i = 0; i < FarmSteps.Length; i++)
            {
                var s = FarmSteps[i];
                bool done = i < current, now = i == current;
                var cell = NLUi.Group(steps, "Step" + i, true, 3f, TextAnchor.UpperCenter, false);
                var disc = UiKit.Image(cell, "Disc", now ? Green : done ? new Color(0.36f, 0.62f, 0.44f, 1f) : new Color(0f, 0f, 0f, 0.07f), UiKit.Circle);
                UiKit.Size(disc, 40f, 40f);
                var ic = UiKit.Icon(disc.transform, done ? "check_circle" : s.icon, now || done ? Color.white : Muted, 22f);
                UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 22f, 22f);
                var label = Text(cell, Loc.T(s.vi, s.en, s.ja), 12.5f, now ? Green : Muted, now ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.Center);
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // The crop (or the soil) with its name, word card and what it earns.
            var top = NLUi.Panel(body, "Crop", Color.white, new RectOffset(14, 14, 12, 12), 14f, vertical: false);
            top.GetComponent<Image>().sprite = UiKit.Rounded(14);
            ((HorizontalLayoutGroup)top.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            Icon(top, crop != null ? crop.ProduceItemId : "seed_carrot", 78f).color = crop != null ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            var info = NLUi.Group(top, "Info", true, 4f);
            NLUi.Size(info, flexibleWidth: 1f);
            if (crop != null)
            {
                Text(info, IslandLanguage.Title(crop.word), 23f, Ink, FontStyles.Bold);
                WordChip(info, crop.word, 16f);
                var earn = NLUi.Group(info, "Earn", false, 6f, TextAnchor.MiddleLeft, false);
                Text(earn, Loc.F("Thu {0} quả × ", "Yields {0} × ", "{0}個 × ", crop.yield), 13.5f, Muted);
                Coin(earn, crop.sellPrice, 13.5f, Gold);
                Text(earn, " = ", 13.5f, Muted);
                Coin(earn, crop.yield * crop.sellPrice, 14f, Green);
            }
            else Text(info, phase == FarmPlot.Phase.Untilled
                ? Loc.T("Đất chưa xới", "Untilled soil", "まだ耕していない土")
                : Loc.T("Đất đã xới, chờ gieo hạt", "Tilled — ready for seeds", "耕しました — 種まきを待っています"), 20f, Ink, FontStyles.Bold);

            // Growth.
            var status = NLUi.Group(body, "Status", true, 5f);
            Color dot = phase switch { FarmPlot.Phase.NeedsWater => Water, FarmPlot.Phase.Ready => Gold, FarmPlot.Phase.Growing => Green, _ => Muted };
            Text(status, $"<color=#{ColorUtility.ToHtmlStringRGB(dot)}>●</color>  {Capital(FarmPlot.PhaseLabel(phase))}" +
                (crop != null ? "   ·   " + Loc.F("giai đoạn {0}/4", "stage {0}/4", "段階 {0}/4", Mathf.Min(plot.Record.stage + 1, 4)) : ""), 16f, Ink, FontStyles.Bold);
            _growFill = UiKit.ProgressBar(status, "Growth", Green, new Color(0f, 0f, 0f, 0.08f), 5);
            UiKit.Size(_growFill.parent.GetComponent<Image>(), -1f, 10f);
            _growText = Text(status, "", 13.5f, Muted);

            // The action this phase needs, with the phrase it teaches.
            var actions = NLUi.Group(body, "Actions", true, 8f);
            switch (phase)
            {
                case FarmPlot.Phase.Untilled:
                {
                    var tool = catalog.Tool("tool_hoe");
                    PhraseCard(body, tool.verb);
                    var (toolId, toolName, seconds) = FarmActionTimes.Till();
                    if (toolId != null)
                        ActionButton(actions, Loc.T("Xới đất", "Till the soil", "土を耕す"), toolId, seconds,
                            () => Work(Loc.T("Đang xới đất…", "Tilling…", "耕しています…"), toolName, seconds, () => Do(plot.Till(), tool.verb, Loc.T("Đã xới đất.", "Soil tilled.", "耕しました。")), "Work_Hoe", toolId));
                    else NeedTool(actions, "tool_hoe", Loc.T("Không có cuốc hay xẻng thì không xới được đất.", "Without a hoe or a shovel you cannot till.", "くわかシャベルがないと耕せません。"));
                    break;
                }
                case FarmPlot.Phase.Tilled:
                {
                    PhraseCard(body, catalog.verbs.plant);
                    BuildSeedPicker(actions, plot);
                    break;
                }
                case FarmPlot.Phase.NeedsWater:
                {
                    var tool = catalog.Tool("tool_watering_can");
                    PhraseCard(body, tool.verb);
                    if (IslandTools.Usable(tool.id))
                        ActionButton(actions, Loc.T("Tưới nước", "Water it", "水をあげる"), tool.id, FarmActionTimes.Water,
                            () => Work(Loc.T("Đang tưới nước…", "Watering…", "水やり中…"), IslandLanguage.Name(tool.word), FarmActionTimes.Water,
                                () => Do(plot.Water(), tool.verb, Loc.T("Đã tưới — cây bắt đầu lớn.", "Watered — it starts growing.", "水をあげました — 育ち始めます。")), "Work_Water", "tool_watering_can"));
                    else NeedTool(actions, "tool_watering_can", Loc.T("Không có bình tưới thì không mang nước được.", "Without a watering can you cannot carry water.", "じょうろがないと水を運べません。"));
                    break;
                }
                case FarmPlot.Phase.Growing:
                    Text(actions, Loc.T("Cây đang lớn. Bạn có thể đi làm việc khác — cây vẫn lớn kể cả khi bạn rời đảo.",
                        "It is growing. Go and do something else — it keeps growing even when you leave the island.",
                        "育っています。ほかのことをしても大丈夫 — 島を離れても育ち続けます。"), 15f, Muted);
                    break;
                case FarmPlot.Phase.Ready:
                {
                    PhraseCard(body, catalog.verbs.harvest);
                    ActionButton(actions, Loc.F("Thu hoạch ×{0}", "Harvest ×{0}", "収穫 ×{0}", crop.yield), crop.ProduceItemId, FarmActionTimes.Harvest, () =>
                        Work(Loc.T("Đang thu hoạch…", "Harvesting…", "収穫中…"), Loc.T("tay", "hands", "手"), FarmActionTimes.Harvest, pose: "Work_Harvest", done: () =>
                        {
                            string err = plot.Harvest(out int amount);
                            Do(err, catalog.verbs.harvest, Loc.F("Thu hoạch {0} {1} — đã vào balo. Bán ở cửa hàng Midori.", "Harvested {0} {1} — in your bag. Sell them at the Midori Store.", "{1}を{0}個収穫 — バッグに入りました。みどりしょうてんで売れます。", amount, IslandLanguage.Name(crop.word)));
                            if (err == null) WordToast(crop.word, Loc.T("Thu hoạch", "Harvest", "収穫"));
                        }));
                    break;
                }
            }
            if (crop != null && phase != FarmPlot.Phase.Ready)
            {
                var shovel = catalog.Tool("tool_shovel");
                var (clearId, clearTool, clearSeconds) = FarmActionTimes.Clear();
                if (clearId != null)
                {
                    var clear = IconBtn(actions, "Clear", "delete_sweep", $"{Loc.T("Dọn ô", "Clear plot", "片づける")}  ·  {clearTool}  ·  {clearSeconds:0.#}s  ·  {IslandTools.Label(clearId)}",
                        () => Work(Loc.T("Đang dọn ô đất…", "Clearing…", "片づけ中…"), clearTool, clearSeconds, () => Do(plot.Clear(), shovel.verb, Loc.T("Đã dọn ô đất.", "Plot cleared.", "片づけました。")), "Work_Clear", clearId), Cream2, Muted, 38f, 13.5f);
                }
            }
            _farmFeedback = Text(body, feedback ?? "", 15f, error ? Error : Green, FontStyles.Bold);
            UpdateGrowth();
        }

        /// <summary>No tool: says why, offers to buy it right here (coin price) or to open the store.</summary>
        private void NeedTool(RectTransform actions, string toolId, string why)
        {
            var tool = IslandCatalog.Load().Tool(toolId);
            var box = NLUi.Panel(actions, "NeedTool", ErrorSoft, new RectOffset(16, 16, 12, 14), 8f);
            box.GetComponent<Image>().sprite = UiKit.Rounded(14);
            Text(box, why, 15f, Error, FontStyles.Bold);
            var row = NLUi.Group(box, "Row", false, 10f, TextAnchor.MiddleLeft, false);
            if (tool != null)
            {
                bool afford = IslandEconomy.Yen >= tool.price;
                var buy = IconBtn(row, "QuickBuy_" + toolId, "shopping_cart", Loc.F("Mua {0} · ¥{1:N0}", "Buy a {0} · ¥{1:N0}", "{0}を買う · ¥{1:N0}", IslandLanguage.Name(tool.word), tool.price),
                    () => QuickBuy(toolId, tool.price), afford ? Green : new Color(0.62f, 0.64f, 0.6f), Color.white, 44f, 15f);
                buy.interactable = afford;
                NLUi.Size(buy, 250f, 44f);
            }
            var shop = IconBtn(row, "OpenShop", "storefront", Loc.T("Cửa hàng", "Store", "お店"), () => { CloseFarm(); OpenShopAt("agriculture", toolId); }, Gold, new Color(0.18f, 0.11f, 0.02f), 44f, 15f);
            NLUi.Size(shop, 150f, 44f);
        }

        /// <summary>Buys one item without leaving the farm card (seed or tool), then redraws the card.</summary>
        private void QuickBuy(string itemId, int price)
        {
            var result = IslandEconomy.Buy(itemId, price, 1);
            string message = result switch
            {
                IslandEconomy.Result.Ok => Loc.F("Đã mua 1 {0} (−¥{1:N0}).", "Bought 1 {0} (−¥{1:N0}).", "{0}を1つ買いました（−¥{1:N0}）。", ItemName(itemId), price),
                IslandEconomy.Result.NoMoney => Loc.F("Không đủ tiền: cần ¥{0:N0}, bạn có ¥{1:N0}.", "Not enough money: ¥{0:N0} needed, you have ¥{1:N0}.", "お金が足りません：¥{0:N0}必要、所持¥{1:N0}。", price, IslandEconomy.Yen),
                IslandEconomy.Result.BagFull => Loc.T("Balo đầy — bán bớt nông sản trước.", "Your bag is full — sell some produce first.", "バッグがいっぱいです。先に作物を売りましょう。"),
                _ => Loc.T("Không thực hiện được.", "That did not work.", "できませんでした。"),
            };
            if (result == IslandEconomy.Result.Ok) IslandState.Discover(WordKey(itemId));
            if (_farmCard != null) BuildFarm(message, result != IslandEconomy.Result.Ok);
        }

        private static string ItemName(string itemId)
        {
            var c = IslandCatalog.Load();
            var tool = c.Tool(itemId);
            if (tool != null) return IslandLanguage.Name(tool.word);
            var crop = c.crops.FirstOrDefault(x => x.SeedItemId == itemId || x.ProduceItemId == itemId);
            if (crop != null) return itemId == crop.SeedItemId ? Loc.F("hạt {0}", "{0} seeds", "{0}の種", IslandLanguage.Name(crop.word)) : IslandLanguage.Name(crop.word);
            var fish = c.Fish(itemId);
            if (fish != null) return IslandLanguage.Name(fish.word);
            var decor = c.Decor(itemId);
            if (decor != null) return IslandLanguage.Name(decor.word);
            var product = c.products?.FirstOrDefault(p => p.itemId == itemId);
            return product != null ? IslandLanguage.Name(product.word) : itemId;
        }

        /// <summary>Farm work takes time (progress card); repeated clicks while working are ignored.</summary>
        private static void Work(string label, string tool, float seconds, Action done, string pose = null, string heldToolId = null)
        {
            if (TimedAction.Busy) return;
            TimedAction.Run(label, tool, seconds, done, pose: pose, prop: HeldTool(heldToolId));
        }

        /// <summary>Hand-held model of a tool item (built by IslandBuilder as "Held_&lt;toolId&gt;"), or null.</summary>
        public static GameObject HeldTool(string toolId) =>
            string.IsNullOrEmpty(toolId) || IslandModelLibrary.Instance == null ? null : IslandModelLibrary.Instance.Find("Held_" + toolId);

        /// <summary>The main farm action: item icon, label (its first text), time, and the tool's remaining uses.</summary>
        private void ActionButton(RectTransform parent, string label, string iconItem, float seconds, Action click)
        {
            string wear = IslandTools.Label(iconItem);
            var b = Btn(parent, $"<b>{label}</b>  <size=80%><color=#CFE3D3>·  {seconds:0.#}s{(string.IsNullOrEmpty(wear) ? "" : "  ·  " + wear)}</color></size>", click, Green, Color.white, 58f, 18f);
            b.name = "Action";
            b.GetComponent<HorizontalOrVerticalLayoutGroup>().padding = new RectOffset(64, 18, 10, 10);
            var icon = Icon((RectTransform)b.transform, iconItem, 38f);
            icon.GetComponent<LayoutElement>().ignoreLayout = true;
            var r = icon.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f); r.anchoredPosition = new Vector2(14f, 0f); r.sizeDelta = new Vector2(38f, 38f);
        }

        private void BuildSeedPicker(RectTransform parent, FarmPlot plot)
        {
            var catalog = IslandCatalog.Load();
            var owned = catalog.crops.Where(c => IslandEconomy.Owned(c.SeedItemId) > 0).ToList();
            Text(parent, Loc.T("Chọn hạt giống", "Choose a seed", "種をえらぶ"), 15f, Green, FontStyles.Bold);
            if (owned.Count == 0) Text(parent, Loc.T("Bạn chưa có hạt giống nào — mua ngay bên dưới.", "You have no seeds — buy some below.", "種がありません — 下で買えます。"), 15f, Error);
            foreach (var crop in owned)
            {
                var row = NLUi.Panel(parent, "Seed_" + crop.id, Color.white, new RectOffset(10, 10, 6, 6), 10f, vertical: false);
                row.GetComponent<Image>().sprite = UiKit.Rounded(12);
                ((HorizontalLayoutGroup)row.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
                Icon(row, crop.SeedItemId, 46f);
                var col = NLUi.Group(row, "Text", true, 1f);
                NLUi.Size(col, flexibleWidth: 1f);
                Text(col, $"<b>{IslandLanguage.Title(crop.word)}</b>  <size=80%><color=#6B7566>×{IslandEconomy.Owned(crop.SeedItemId)}</color></size>", 17f, Ink);
                Text(col, Loc.F("Lớn trong {0} giây · tưới 3 lần · thu {1}", "Grows in {0}s · water 3 times · yields {1}", "{0}秒で育つ · 水やり3回 · {1}個収穫", Mathf.RoundToInt(crop.secondsPerStage * 3), crop.yield), 12.5f, Muted);
                var plant = Btn(row, Loc.T("Gieo", "Plant", "まく"), () => Work(Loc.T("Đang gieo hạt…", "Planting…", "種まき中…"), Loc.T("tay", "hands", "手"), FarmActionTimes.Plant,
                    () => Do(plot.Plant(crop.id), catalog.verbs.plant, Loc.F("Đã gieo hạt {0}. Giờ hãy tưới nước.", "{0} planted. Now water it.", "{0}の種をまきました。水をあげましょう。", IslandLanguage.Name(crop.word))), "Work_Plant"), Green, Color.white, 40f, 15.5f);
                NLUi.Size(plant, 88f, 40f);
                plant.name = "Plant_" + crop.id;
            }
            // Buy seeds here: one tap per seed, with what it earns.
            Text(parent, Loc.T("Mua hạt giống", "Buy seeds", "種を買う"), 13.5f, Muted, FontStyles.Bold);
            var buyRow = NLUi.Group(parent, "BuySeeds", false, 6f, TextAnchor.MiddleLeft, true);
            foreach (var crop in catalog.crops)
            {
                bool afford = IslandEconomy.Yen >= crop.seedPrice;
                var b = Btn(buyRow, $"<b>{IslandLanguage.Title(crop.word)}</b>\n<size=90%><color=#{(afford ? "A06C0A" : "8E948A")}>{NihongoLife.Core.Loc.Money(crop.seedPrice)}</color>  <color=#2E6B45>+{NihongoLife.Core.Loc.Money(crop.yield * crop.sellPrice - crop.seedPrice)}</color></size>",
                    () => QuickBuy(crop.SeedItemId, crop.seedPrice), Color.white, Ink, 54f, 14.5f);
                b.name = "BuySeed_" + crop.id;
                b.interactable = afford;
            }
        }

        private void Do(string error, IslandWord phrase, string success)
        {
            if (error != null) { BuildFarm(error, true); return; }
            IslandState.Discover("verb:" + phrase.en);
            IslandAchievements.Check();
            BuildFarm(success, false);
            ActionFeedback(_plot != null ? _plot.transform.position : Vector3.zero);
        }

        private void ActionFeedback(Vector3 at)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (at == Vector3.zero || shader == null) return;
            var puff = new GameObject("FarmPuff").AddComponent<ParticleSystem>();
            puff.transform.position = at + Vector3.up * 0.3f;
            puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = puff.main; main.startLifetime = 0.8f; main.startSpeed = 1.2f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f); main.gravityModifier = 1.2f; main.duration = 0.4f; main.loop = false;
            main.startColor = new Color(0.6f, 0.45f, 0.3f, 0.9f);
            var emission = puff.emission; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var shape = puff.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = 0.5f;
            var renderer = puff.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(shader);
            puff.Play();
            Destroy(puff.gameObject, 2f);
        }

        private void UpdateGrowth()
        {
            if (_plot == null || _growFill == null) return;
            var phase = _plot.CurrentPhase;
            float stage = _plot.Record.stage;
            float total = phase == FarmPlot.Phase.Ready ? 1f : Mathf.Clamp01((stage + _plot.StageProgress) / 3f);
            UiKit.SetProgress(_growFill, _plot.Crop == null ? 0f : total);
            _growText.text = phase switch
            {
                FarmPlot.Phase.Growing => Loc.F("Giai đoạn tiếp theo sau {0} giây", "Next stage in {0}s", "次の段階まで{0}秒", Mathf.CeilToInt(_plot.SecondsLeft)),
                FarmPlot.Phase.NeedsWater => Loc.T("Cây khát nước — tưới để cây lớn tiếp", "Thirsty — water it to keep it growing", "のどが渇いています — 水をあげて育てましょう"),
                FarmPlot.Phase.Ready => Loc.T("Đã chín!", "Ripe!", "実りました！"),
                _ => "",
            };
        }

        private FarmPlot.Phase _lastPhase;
        private void Update()
        {
            if (_farmCard != null && _plot != null)
            {
                var phase = _plot.CurrentPhase;
                if (phase != _lastPhase) { _lastPhase = phase; BuildFarm(null, false); }
                UpdateGrowth();
            }
        }

        private static string Capital(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
