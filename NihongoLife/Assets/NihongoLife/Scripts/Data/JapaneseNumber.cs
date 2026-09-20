using System.Text;

namespace NihongoLife.Data
{
    /// <summary>
    /// Japanese number formatting for prices and quantities (0 - 99,999): kanji for the text layer,
    /// hiragana for the reading layer, including the irregular readings (さんびゃく, ろっぴゃく,
    /// はっぴゃく, さんぜん, はっせん).
    /// </summary>
    public static class JapaneseNumber
    {
        private static readonly string[] Digit = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        private static readonly string[] DigitReading = { "", "いち", "に", "さん", "よん", "ご", "ろく", "なな", "はち", "きゅう" };
        private static readonly string[] HundredReading = { "", "ひゃく", "にひゃく", "さんびゃく", "よんひゃく", "ごひゃく", "ろっぴゃく", "ななひゃく", "はっぴゃく", "きゅうひゃく" };
        private static readonly string[] ThousandReading = { "", "せん", "にせん", "さんぜん", "よんせん", "ごせん", "ろくせん", "ななせん", "はっせん", "きゅうせん" };
        private static readonly string[] Counter = { "", "一つ", "二つ", "三つ", "四つ", "五つ", "六つ", "七つ", "八つ", "九つ", "十" };
        private static readonly string[] CounterReading = { "", "ひとつ", "ふたつ", "みっつ", "よっつ", "いつつ", "むっつ", "ななつ", "やっつ", "ここのつ", "とお" };

        public static string ToKanji(int value)
        {
            if (value <= 0) return "零";
            if (value > 99999) value = 99999;

            var sb = new StringBuilder();
            int man = value / 10000;
            int thousand = value / 1000 % 10;
            int hundred = value / 100 % 10;
            int ten = value / 10 % 10;
            int one = value % 10;

            if (man > 0) sb.Append(Digit[man]).Append('万');
            if (thousand > 0) sb.Append(thousand > 1 ? Digit[thousand] : string.Empty).Append('千');
            if (hundred > 0) sb.Append(hundred > 1 ? Digit[hundred] : string.Empty).Append('百');
            if (ten > 0) sb.Append(ten > 1 ? Digit[ten] : string.Empty).Append('十');
            if (one > 0) sb.Append(Digit[one]);
            return sb.ToString();
        }

        public static string ToReading(int value)
        {
            if (value <= 0) return "ゼロ";
            if (value > 99999) value = 99999;

            var sb = new StringBuilder();
            int man = value / 10000;
            int thousand = value / 1000 % 10;
            int hundred = value / 100 % 10;
            int ten = value / 10 % 10;
            int one = value % 10;

            if (man > 0) sb.Append(DigitReading[man]).Append("まん");
            if (thousand > 0) sb.Append(ThousandReading[thousand]);
            if (hundred > 0) sb.Append(HundredReading[hundred]);
            if (ten > 0) sb.Append(ten > 1 ? DigitReading[ten] : string.Empty).Append("じゅう");
            if (one > 0) sb.Append(DigitReading[one]);
            return sb.ToString();
        }

        /// <summary>Counting with the つ series for 1-10 (ひとつ, ふたつ ... とお); beyond 10 falls back to plain numbers.</summary>
        public static string CountKanji(int count)
        {
            if (count >= 1 && count <= 10) return Counter[count];
            return ToKanji(count) + "個";
        }

        public static string CountReading(int count)
        {
            if (count >= 1 && count <= 10) return CounterReading[count];
            return ToReading(count) + "こ";
        }
    }
}
