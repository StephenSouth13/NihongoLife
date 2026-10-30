using System.Collections.Generic;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>Interface strings for the study desk, flashcards, album and wardrobe screens, keyed, in vi / en / ja / ru.
    /// Russian is written here directly (these keys are not in tools/content/ru yet).</summary>
    public static class EduUiText
    {
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            { "desk.title", new[] { "B\u00E0n h\u1ECDc", "Study desk", "\u52C9\u5F37\u673A", "\u041F\u0438\u0441\u044C\u043C\u0435\u043D\u043D\u044B\u0439 \u0441\u0442\u043E\u043B" } },
            { "desk.quiz", new[] { "Ki\u1EC3m tra t\u1EEB v\u1EF1ng", "Vocabulary quiz", "\u5358\u8A9E\u30AF\u30A4\u30BA", "\u0422\u0435\u0441\u0442 \u043F\u043E \u0441\u043B\u043E\u0432\u0430\u043C" } },
            { "desk.flashcards", new[] { "Th\u1EBB ghi nh\u1EDB", "Flashcards", "\u5358\u8A9E\u30AB\u30FC\u30C9", "\u041A\u0430\u0440\u0442\u043E\u0447\u043A\u0438" } },
            { "desk.album", new[] { "Album", "Album", "\u30A2\u30EB\u30D0\u30E0", "\u0410\u043B\u044C\u0431\u043E\u043C" } },
            { "desk.exam", new[] { "Luy\u1EC7n thi", "Exam prep", "\u8A66\u9A13\u5BFE\u7B56", "\u041F\u043E\u0434\u0433\u043E\u0442\u043E\u0432\u043A\u0430 \u043A \u044D\u043A\u0437\u0430\u043C\u0435\u043D\u0443" } },
            { "desk.wardrobe", new[] { "T\u1EE7 \u0111\u1ED3", "Wardrobe", "\u30AF\u30ED\u30FC\u30BC\u30C3\u30C8", "\u0413\u0430\u0440\u0434\u0435\u0440\u043E\u0431" } },
            { "desk.locked", new[] { "(kh\u00F3a)", "(locked)", "(\u30ED\u30C3\u30AF\u4E2D)", "(\u0437\u0430\u043A\u0440\u044B\u0442\u043E)" } },
            { "desk.examToast", new[] { "Luy\u1EC7n thi: d\u00F9ng \u0111\u1EC1 thi th\u1EED JLPT trong S\u1ED5 tay (Y).", "Exam prep: use the JLPT mock exam in the Handbook (Y).", "\u8A66\u9A13\u5BFE\u7B56\uFF1A\u30CF\u30F3\u30C9\u30D6\u30C3\u30AF(Y)\u306EJLPT\u6A21\u8A66\u3092\u4F7F\u3063\u3066\u306D\u3002", "\u041F\u043E\u0434\u0433\u043E\u0442\u043E\u0432\u043A\u0430: \u043F\u0440\u043E\u0431\u043D\u044B\u0439 JLPT \u0432 \u0421\u043F\u0440\u0430\u0432\u043E\u0447\u043D\u0438\u043A\u0435 (Y)." } },
            { "ui.close", new[] { "\u0110\u00F3ng", "Close", "\u9589\u3058\u308B", "\u0417\u0430\u043A\u0440\u044B\u0442\u044C" } },
            { "fc.left", new[] { "c\u00F2n l\u1EA1i", "left", "\u679A\u6B8B\u308A", "\u043E\u0441\u0442\u0430\u043B\u043E\u0441\u044C" } },
            { "fc.reviewed", new[] { "\u0111\u00E3 \u00F4n", "reviewed", "\u679A\u5FA9\u7FD2", "\u043F\u043E\u0432\u0442\u043E\u0440\u0435\u043D\u043E" } },
            { "fc.done", new[] { "Xong b\u00E0i h\u00F4m nay!", "Done for today!", "\u4ECA\u65E5\u306F\u304A\u3057\u307E\u3044\uFF01", "\u041D\u0430 \u0441\u0435\u0433\u043E\u0434\u043D\u044F \u0432\u0441\u0451!" } },
            { "fc.show", new[] { "Xem \u0111\u00E1p \u00E1n", "Show answer", "\u7B54\u3048\u3092\u898B\u308B", "\u041F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u043E\u0442\u0432\u0435\u0442" } },
            { "fc.again", new[] { "L\u1EA1i", "Again", "\u3082\u3046\u4E00\u5EA6", "\u0421\u043D\u043E\u0432\u0430" } },
            { "fc.hard", new[] { "Kh\u00F3", "Hard", "\u96E3\u3057\u3044", "\u0422\u0440\u0443\u0434\u043D\u043E" } },
            { "fc.good", new[] { "T\u1ED1t", "Good", "\u826F\u3044", "\u0425\u043E\u0440\u043E\u0448\u043E" } },
            { "fc.easy", new[] { "D\u1EC5", "Easy", "\u7C21\u5358", "\u041B\u0435\u0433\u043A\u043E" } },
            { "album.empty", new[] { "Ch\u01B0a c\u00F3 \u1EA3nh - h\u00E3y \u0111i kh\u00E1m ph\u00E1 v\u00E0 h\u1ECDc \u0111\u1EC3 l\u1EA5p \u0111\u1EA7y album.", "No photos yet - visit places and study to fill it.", "\u307E\u3060\u5199\u771F\u304C\u306A\u3044\u3088\u3002\u3044\u308D\u3093\u306A\u5834\u6240\u306B\u884C\u3063\u3066\u52C9\u5F37\u3057\u3088\u3046\u3002", "\u0424\u043E\u0442\u043E \u043F\u043E\u043A\u0430 \u043D\u0435\u0442 - \u043F\u0443\u0442\u0435\u0448\u0435\u0441\u0442\u0432\u0443\u0439\u0442\u0435 \u0438 \u0443\u0447\u0438\u0442\u0435\u0441\u044C." } },
            { "album.day", new[] { "ng\u00E0y", "day", "\u65E5\u76EE", "\u0434\u0435\u043D\u044C" } },
            { "wr.title", new[] { "T\u1EE7 \u0111\u1ED3", "Wardrobe", "\u30AF\u30ED\u30FC\u30BC\u30C3\u30C8", "\u0413\u0430\u0440\u0434\u0435\u0440\u043E\u0431" } },
            { "wr.worn", new[] { "\u0111ang m\u1EB7c", "worn", "\u7740\u7528\u4E2D", "\u043D\u0430\u0434\u0435\u0442\u043E" } },
            { "wr.equip", new[] { "m\u1EB7c", "equip", "\u7740\u308B", "\u043D\u0430\u0434\u0435\u0442\u044C" } },
            { "wr.buy", new[] { "mua", "buy", "\u8CB7\u3046", "\u043A\u0443\u043F\u0438\u0442\u044C" } },
            { "wr.noMoney", new[] { "Kh\u00F4ng \u0111\u1EE7 ti\u1EC1n", "Not enough money", "\u304A\u91D1\u304C\u8DB3\u308A\u306A\u3044", "\u041D\u0435\u0434\u043E\u0441\u0442\u0430\u0442\u043E\u0447\u043D\u043E \u0434\u0435\u043D\u0435\u0433" } },
            { "place.20_StationDistrict", new[] { "Ga t\u00E0u", "The station", "\u99C5", "\u0421\u0442\u0430\u043D\u0446\u0438\u044F" } },
            { "place.30_SushiRestaurant", new[] { "Nh\u00E0 h\u00E0ng sushi", "Sushi restaurant", "\u3059\u3057\u5C4B", "\u0421\u0443\u0448\u0438-\u0440\u0435\u0441\u0442\u043E\u0440\u0430\u043D" } },
            { "place.40_ HIBARICLASS", new[] { "L\u1EDBp h\u1ECDc Hibari", "Hibari classroom", "\u3072\u3070\u308A\u6559\u5BA4", "\u041A\u043B\u0430\u0441\u0441 \u0425\u0438\u0431\u0430\u0440\u0438" } },
            { "place.45_HomeBedroom", new[] { "Ph\u00F2ng c\u1EE7a t\u00F4i", "My room", "\u81EA\u5206\u306E\u90E8\u5C4B", "\u041C\u043E\u044F \u043A\u043E\u043C\u043D\u0430\u0442\u0430" } },
            { "place.50_GameCenter", new[] { "Trung t\u00E2m tr\u00F2 ch\u01A1i", "Game center", "\u30B2\u30FC\u30E0\u30BB\u30F3\u30BF\u30FC", "\u0418\u0433\u0440\u043E\u0432\u043E\u0439 \u0446\u0435\u043D\u0442\u0440" } },
            { "place.60_MidoriIsland", new[] { "\u0110\u1EA3o Midori", "Midori Island", "\u307F\u3069\u308A\u5CF6", "\u041E\u0441\u0442\u0440\u043E\u0432 \u041C\u0438\u0434\u043E\u0440\u0438" } },
            { "place.35_HibariClinic", new[] { "Ph\u00F2ng kh\u00E1m Hibari", "Hibari clinic", "\u3072\u3070\u308A\u30AF\u30EA\u30CB\u30C3\u30AF", "\u041A\u043B\u0438\u043D\u0438\u043A\u0430 \u0425\u0438\u0431\u0430\u0440\u0438" } },
            { "place.65_PhuQuocIsland", new[] { "\u0110\u1EA3o Ph\u00FA Qu\u1ED1c", "Phu Quoc Island", "\u30D5\u30FC\u30B3\u30C3\u30AF\u5CF6", "\u041E\u0441\u0442\u0440\u043E\u0432 \u0424\u0443\u043A\u0443\u043E\u043A" } },
            { "place.55_MusicShop", new[] { "C\u1EEDa h\u00E0ng nh\u1EA1c", "Music shop", "\u697D\u5668\u5E97", "\u041C\u0443\u0437\u044B\u043A\u0430\u043B\u044C\u043D\u044B\u0439 \u043C\u0430\u0433\u0430\u0437\u0438\u043D" } },
            { "place.90_TestSandbox", new[] { "Th\u1ECB tr\u1EA5n", "The town", "\u753A", "\u0413\u043E\u0440\u043E\u0434" } },
            { "photo.visit", new[] { "L\u1EA7n \u0111\u1EA7u \u0111\u1EBFn", "First visit", "\u306F\u3058\u3081\u3066\u6765\u305F\uFF01", "\u041F\u0435\u0440\u0432\u044B\u0439 \u0432\u0438\u0437\u0438\u0442" } },
            { "photo.words", new[] { "\u0110\u00E3 h\u1ECDc {0} t\u1EEB", "Learned {0} words", "{0}\u8A9E\u899A\u3048\u305F\uFF01", "\u0412\u044B\u0443\u0447\u0435\u043D\u043E \u0441\u043B\u043E\u0432: {0}" } },
        };

        public static IEnumerable<string> Keys => Table.Keys;

        public static string Get(string key) => Get(key, Loc.Language);

        public static string Get(string key, GameLanguage lang)
        {
            if (!Table.TryGetValue(key, out var v)) return key;
            int i = lang == GameLanguage.English ? 1 : lang == GameLanguage.Japanese ? 2 : lang == GameLanguage.Russian ? 3 : 0;
            return v[i];
        }

        public static string Ja(string key) => Get(key, GameLanguage.Japanese);
    }
}
