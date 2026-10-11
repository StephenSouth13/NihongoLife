using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NihongoLife.Player;

namespace NihongoLife.Shop
{
    public enum KonbiniSection { Onigiri, Snacks, Drinks, Hot, Stationery }

    [Serializable]
    public sealed class KonbiniProduct
    {
        public string id;
        public string japanese;
        public string reading;
        public string vietnamese;
        public string glyph;
        public int price;
        public KonbiniSection section;
        public ItemUseType useType;
        public float food, drink, energy;
        /// <summary>Item id the konbini scenario asks for (onigiri / water / tea), or empty.</summary>
        public string scenarioItemId;
        public Color tint;
        /// <summary>Stationery: sheets added to the notebook when paid for (they never go into the bag).</summary>
        public int notebookPages;

        public KonbiniProduct(string id, string japanese, string reading, string vietnamese, string glyph, int price, KonbiniSection section,
            ItemUseType useType, float food, float drink, float energy, string scenarioItemId, Color tint, int notebookPages = 0)
        {
            this.notebookPages = notebookPages;
            this.id = id; this.japanese = japanese; this.reading = reading; this.vietnamese = vietnamese; this.glyph = glyph;
            this.price = price; this.section = section; this.useType = useType; this.food = food; this.drink = drink; this.energy = energy;
            this.scenarioItemId = scenarioItemId; this.tint = tint;
        }
    }

    /// <summary>What Hibari Mart sells. Each product carries its kanji/kana glyph, reading and meaning so
    /// browsing the shelf is itself vocabulary practice.</summary>
    public static class KonbiniCatalog
    {
        private static readonly Color RiceTint = new Color(0.86f, 0.9f, 0.82f);
        private static readonly Color BreadTint = new Color(0.98f, 0.82f, 0.55f);
        private static readonly Color SweetTint = new Color(0.96f, 0.68f, 0.72f);
        private static readonly Color DrinkTint = new Color(0.62f, 0.82f, 0.96f);
        private static readonly Color HotTint = new Color(0.98f, 0.6f, 0.42f);
        private static readonly Color PaperTint = new Color(0.86f, 0.9f, 0.98f);

        public static readonly IReadOnlyList<KonbiniProduct> Products = new List<KonbiniProduct>
        {
            new KonbiniProduct("onigiri_ume", "うめおにぎり", "うめ おにぎり", "Cơm nắm mơ muối", "梅", 150, KonbiniSection.Onigiri, ItemUseType.Food, 28f, 0f, 10f, "onigiri", RiceTint),
            new KonbiniProduct("onigiri_sake", "しゃけおにぎり", "しゃけ おにぎり", "Cơm nắm cá hồi", "鮭", 160, KonbiniSection.Onigiri, ItemUseType.Food, 30f, 0f, 10f, "onigiri", RiceTint),
            new KonbiniProduct("onigiri_tuna", "ツナマヨおにぎり", "ツナマヨ おにぎり", "Cơm nắm cá ngừ mayo", "ツ", 150, KonbiniSection.Onigiri, ItemUseType.Food, 30f, 0f, 10f, "onigiri", RiceTint),
            new KonbiniProduct("sandwich_egg", "たまごサンド", "たまご サンド", "Bánh mì kẹp trứng", "卵", 280, KonbiniSection.Onigiri, ItemUseType.Food, 34f, 0f, 12f, "", BreadTint),
            new KonbiniProduct("melonpan", "メロンパン", "メロン パン", "Bánh mì dưa lưới", "パ", 140, KonbiniSection.Onigiri, ItemUseType.Food, 26f, 0f, 14f, "", BreadTint),
            new KonbiniProduct("pocky", "ポッキー", "ポッキー", "Bánh que Pocky", "ポ", 180, KonbiniSection.Snacks, ItemUseType.Food, 12f, 0f, 16f, "", SweetTint),
            new KonbiniProduct("senbei", "せんべい", "せんべい", "Bánh gạo nướng", "煎", 200, KonbiniSection.Snacks, ItemUseType.Food, 14f, 0f, 8f, "", SweetTint),
            new KonbiniProduct("ice_cream", "アイス", "アイス", "Kem que", "氷", 130, KonbiniSection.Snacks, ItemUseType.Food, 8f, 6f, 10f, "", SweetTint),
            new KonbiniProduct("water", "みず", "みず", "Nước suối", "水", 120, KonbiniSection.Drinks, ItemUseType.Drink, 0f, 38f, 3f, "water", DrinkTint),
            new KonbiniProduct("tea", "おちゃ", "おちゃ", "Trà xanh", "茶", 150, KonbiniSection.Drinks, ItemUseType.Drink, 0f, 30f, 8f, "tea", DrinkTint),
            new KonbiniProduct("orange_juice", "オレンジジュース", "オレンジ ジュース", "Nước cam", "柑", 160, KonbiniSection.Drinks, ItemUseType.Drink, 4f, 30f, 10f, "", DrinkTint),
            new KonbiniProduct("coffee", "コーヒー", "コーヒー", "Cà phê lon", "珈", 140, KonbiniSection.Drinks, ItemUseType.Drink, 0f, 22f, 22f, "", DrinkTint),
            new KonbiniProduct("milk", "ぎゅうにゅう", "ぎゅうにゅう", "Sữa tươi", "乳", 130, KonbiniSection.Drinks, ItemUseType.Drink, 10f, 26f, 8f, "", DrinkTint),
            new KonbiniProduct("karaage", "からあげ", "からあげ", "Gà rán Nhật", "唐", 220, KonbiniSection.Hot, ItemUseType.Food, 40f, 0f, 18f, "", HotTint),
            new KonbiniProduct("nikuman", "にくまん", "にくまん", "Bánh bao nhân thịt", "肉", 160, KonbiniSection.Hot, ItemUseType.Food, 32f, 0f, 12f, "", HotTint),
            new KonbiniProduct("oden", "おでん", "おでん", "Lẩu oden", "煮", 250, KonbiniSection.Hot, ItemUseType.Food, 36f, 14f, 14f, "", HotTint),
            new KonbiniProduct("note_paper_5", "ルーズリーフ（5まい）", "ルーズリーフ ごまい", "Giấy rời · +5 trang sổ tay", "紙", 120, KonbiniSection.Stationery, ItemUseType.None, 0f, 0f, 0f, "", PaperTint, 5),
            new KonbiniProduct("note_paper_12", "ノートのかみ（12まい）", "ノートの かみ じゅうにまい", "Tập giấy · +12 trang sổ tay", "帳", 260, KonbiniSection.Stationery, ItemUseType.None, 0f, 0f, 0f, "", PaperTint, 12),
            new KonbiniProduct("note_paper_30", "ノート（30まい）", "ノート さんじゅうまい", "Vở dày · +30 trang sổ tay", "冊", 580, KonbiniSection.Stationery, ItemUseType.None, 0f, 0f, 0f, "", PaperTint, 30),
        };

        public static IEnumerable<KonbiniProduct> InSection(KonbiniSection section) => Products.Where(p => p.section == section);
        public static KonbiniProduct Find(string id) => Products.FirstOrDefault(p => p.id == id);

        public static string SectionJa(KonbiniSection s) => s switch
        {
            KonbiniSection.Onigiri => "おにぎり・パン",
            KonbiniSection.Snacks => "おかし",
            KonbiniSection.Drinks => "のみもの",
            KonbiniSection.Stationery => "ぶんぼうぐ",
            _ => "ホットスナック",
        };

        public static string SectionVi(KonbiniSection s) => s switch
        {
            KonbiniSection.Onigiri => "Cơm nắm · Bánh mì",
            KonbiniSection.Snacks => "Bánh kẹo",
            KonbiniSection.Drinks => "Đồ uống",
            KonbiniSection.Stationery => "Văn phòng phẩm",
            _ => "Đồ ăn nóng",
        };
    }

    /// <summary>The shopping basket (かご). Items only reach the bag after paying at the register.</summary>
    public static class KonbiniBasket
    {
        private static readonly Dictionary<string, int> Lines = new Dictionary<string, int>();
        public static event Action Changed;

        public static IReadOnlyDictionary<string, int> Items => Lines;
        public static int Count => Lines.Values.Sum();
        public static int Total => Lines.Sum(kv => (KonbiniCatalog.Find(kv.Key)?.price ?? 0) * kv.Value);
        public static bool IsEmpty => Lines.Count == 0;

        public static void Add(string productId, int quantity)
        {
            if (quantity <= 0 || KonbiniCatalog.Find(productId) == null) return;
            Lines.TryGetValue(productId, out int current);
            Lines[productId] = Mathf.Min(9, current + quantity);
            Changed?.Invoke();
        }

        public static void Remove(string productId)
        {
            if (Lines.Remove(productId)) Changed?.Invoke();
        }

        public static void Clear()
        {
            if (Lines.Count == 0) return;
            Lines.Clear();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Lines.Clear();
            Changed = null;
        }
    }
}
