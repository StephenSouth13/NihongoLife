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
    /// <summary>Partial of IslandUI: island progress notebook. The MonoBehaviour entry point stays IslandUI.cs.</summary>
    public sealed partial class IslandUI : MonoBehaviour
    {
        // ─────────── Notebook (island progress) ───────────

        public static bool BookOpen => _instance != null && _instance._book != null;

        public static void OpenProgress()
        {
            var ui = Instance;
            bool wasOpen = ui._book != null;
            ui.BuildBook();
            if (!wasOpen)
            {
                ui.LockPlayer(true);
                UiModalStack.Register(ui, () => ui._book != null, () => ui.Close(ref ui._book), "Island notebook");
            }
        }

        public static void CloseProgress() { if (_instance != null) _instance.Close(ref _instance._book); }

        private void BuildBook()
        {
            Discard(_book);
            var record = IslandState.Record;
            _book = Card("IslandNotebook", new Vector2(0.5f, 0.5f), Vector2.zero, 1000f);
            Header(_book, "menu_book", Loc.T("Sổ tay Đảo Xanh", "Midori Island notebook", "みどりじまノート"), Loc.T("TIẾN BỘ", "PROGRESS", "記録"), CloseProgress);
            var body = Body(_book, 14f);
            var stats = NLUi.Group(body, "Stats", false, 12f, TextAnchor.MiddleLeft, true);
            foreach (var (icon, n, vi, en, ja) in new[]
                     {
                         ("nutrition", record.harvested.ToString(), "lần thu hoạch", "harvests", "収穫"),
                         ("sell", record.sold.ToString(), "nông sản đã bán", "sold", "販売"),
                         ("payments", $"{NihongoLife.Core.Loc.Money(record.earned)}", "tiền kiếm được", "earned", "かせいだお金"),
                         ("translate", DistinctWords().Count.ToString(), "từ đã học", "words", "覚えた言葉"),
                         ("pets", record.animalsMet.Count + "/5", "con vật quen", "animal friends", "動物の友だち"),
                     })
            {
                var tile = NLUi.Panel(stats, "Stat", Color.white, new RectOffset(14, 14, 10, 12), 2f);
                tile.GetComponent<Image>().sprite = UiKit.Rounded(14);
                var ic = UiKit.Icon(tile, icon, Green, 24f);
                UiKit.Size(ic, 24f, 24f);
                Text(tile, n, 26f, Green, FontStyles.Bold);
                Text(tile, Loc.T(vi, en, ja), 13f, Muted);
            }
            Text(body, $"{Loc.T("Từ vựng", "Vocabulary", "語彙")}  <size=75%><color=#6B7566>({Loc.T("đang học", "learning", "学習中")} {IslandLanguage.LanguageLabel})</color></size>", 18f, Ink, FontStyles.Bold);
            var gridGo = new GameObject("Words", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridGo.transform.SetParent(body, false);
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(182f, 70f); grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5;
            gridGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var words = DistinctWords().Take(25).ToList();
            if (words.Count == 0) Text(body, Loc.T("Chưa có từ nào — trồng cây, cho thú ăn và mua sắm để học từ mới.", "No words yet — farm, feed the animals and shop to learn new words.", "まだ言葉がありません — 畑仕事・えさやり・買い物で新しい言葉を覚えましょう。"), 15f, Muted);
            foreach (var w in words)
            {
                var cell = new GameObject("Word", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
                cell.transform.SetParent(gridGo.transform, false);
                var bg = cell.GetComponent<Image>(); bg.sprite = UiKit.Rounded(12); bg.type = Image.Type.Sliced; bg.color = Color.white;
                var v = cell.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(12, 10, 6, 6); v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
                var primary = Text((RectTransform)cell.transform, IslandLanguage.Primary(w), 16f, Ink, FontStyles.Bold);
                primary.textWrappingMode = TextWrappingModes.NoWrap; primary.overflowMode = TextOverflowModes.Ellipsis;
                string reading = IslandLanguage.Reading(w) ?? "";
                string meaning = IslandLanguage.Helper(w);
                var second = Text((RectTransform)cell.transform, string.Join(" · ", new[] { reading, meaning }.Where(x => !string.IsNullOrEmpty(x))), 12f, Muted, FontStyles.Italic);
                second.textWrappingMode = TextWrappingModes.NoWrap; second.overflowMode = TextOverflowModes.Ellipsis;
            }
            Text(body, Loc.T("Thành tích", "Achievements", "実績"), 18f, Ink, FontStyles.Bold);
            var ach = NLUi.Group(body, "Achievements", false, 10f, TextAnchor.MiddleLeft, true);
            foreach (var def in IslandAchievements.All)
            {
                bool done = record.achievements.Contains(def.Id);
                var tile = NLUi.Panel(ach, "A_" + def.Id, done ? GreenSoft : new Color(0.93f, 0.91f, 0.87f, 1f), new RectOffset(12, 12, 8, 8), 2f);
                tile.GetComponent<Image>().sprite = UiKit.Rounded(12);
                Text(tile, (done ? "<color=#2E6B45>✓</color> " : "<color=#9AA095>○</color> ") + def.Title, 14f, done ? Ink : Muted, FontStyles.Bold);
            }
        }

        /// <summary>Learned words without duplicates (a seed, its crop and its produce are one word).</summary>
        private static List<IslandWord> DistinctWords()
        {
            var seen = new HashSet<IslandWord>();
            var list = new List<IslandWord>();
            foreach (var key in IslandState.Record.words)
            {
                var word = Lookup(key);
                if (word != null && seen.Add(word)) list.Add(word);
            }
            return list;
        }

        /// <summary>Vocabulary key for a shop item: seeds and produce teach their crop's word.</summary>
        private static string WordKey(string itemId)
        {
            var crop = IslandCatalog.Load().crops.FirstOrDefault(c => c.SeedItemId == itemId || c.ProduceItemId == itemId);
            if (crop != null) return "crop:" + crop.id;
            var decor = IslandCatalog.Load().Decor(itemId);
            if (decor != null) return "decor:" + decor.id;
            return IslandCatalog.Load().Fish(itemId) != null ? "fish:" + itemId : "item:" + itemId;
        }

        private static IslandWord Lookup(string key)
        {
            var c = IslandCatalog.Load();
            int colon = key.IndexOf(':');
            if (colon < 0) return null;
            string kind = key.Substring(0, colon), id = key.Substring(colon + 1);
            switch (kind)
            {
                case "crop": return c.Crop(id)?.word;
                case "decor": return c.Decor(id)?.word;
                case "animal": return c.Animal(id)?.word;
                case "fish": return c.Fish(id)?.word;
                case "item":
                    if (c.Tool(id) != null) return c.Tool(id).word;
                    var crop = c.crops.FirstOrDefault(x => x.SeedItemId == id || x.ProduceItemId == id);
                    if (crop != null) return crop.word;
                    return c.products.FirstOrDefault(p => p.itemId == id)?.word;
                case "place":
                    var p2 = c.places;
                    return id switch { "station" => p2.station, "farm" => p2.farm, "shop" => p2.shop, "barn" => p2.barn, "view" => p2.view, "pier" => p2.pier ?? p2.island, _ => p2.island };
                case "verb":
                    var v = c.verbs;
                    foreach (var w in new[] { v.plant, v.harvest, v.feed, v.pet, v.buy, v.sell }) if (w.en == id) return w;
                    foreach (var t in c.tools) if (t.verb.en == id) return t.verb;
                    return null;
            }
            return null;
        }
    }
}
