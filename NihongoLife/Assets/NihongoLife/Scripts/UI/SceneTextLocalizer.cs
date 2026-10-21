using System.Collections.Generic;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.UI
{
    /// <summary>
    /// Text typed into scenes (street signs, shop signs, job boards, arcade cabinets, old HUD labels) was written as
    /// "日本語 · Tiếng Việt". When a scene loads, and whenever the language changes, each such text is shown in the
    /// chosen language: the Vietnamese part becomes English in the English interface and is dropped (or replaced by
    /// Japanese) in the Japanese interface. Scene files keep their authored text; texts that code rewrites later are
    /// left alone (a text is only rewritten while it still shows what this class last put there).
    /// </summary>
    public static class SceneTextLocalizer
    {
        // Longest phrases first. ja "" = drop it (the sign already says it in Japanese).
        private static readonly (string vi, string en, string ja)[] Phrases =
        {
            ("Trung tâm trò chơi học tiếng Nhật", "Japanese-learning arcade", "日本語を学べるゲームセンター"),
            ("Phụ việc nông trại · ¥500/ca", "Farm helper · ¥500/shift", "農場のお手伝い · 1回500円"),
            ("Tuyển làm thêm · ¥600/ca", "Part-time job · ¥600/shift", "アルバイト募集 · 1回600円"),
            ("Tuyển phục vụ · ¥700/ca", "Server wanted · ¥700/shift", "ホールスタッフ募集 · 1回700円"),
            ("[F] xem việc · N sổ nhiệm vụ", "[F] see the job · N task journal", "[F] 仕事を見る · N タスク帳"),
            ("Chào mừng tới Game Center", "Welcome to the Game Center", "ゲームセンターへようこそ"),
            ("Ga Hibari · Tàu điện", "Hibari Station · Trains", ""),
            ("Nhà hàng Sushi Hibari", "Sushi Hibari", ""),
            ("Trường Nhật ngữ Hibari", "Hibari Japanese School", ""),
            ("Trung tâm trò chơi", "Arcade", "ゲームセンター"),
            ("Cửa hàng tiện lợi", "Convenience store", "コンビニ"),
            ("Thanh toán ở đây", "Pay here", ""),
            ("Bảng tin khu phố", "Notice board", ""),
            ("quản lý nông trại", "farm manager", "農場の管理人"),
            ("Tủ game cổ điển", "Retro cabinet", "レトロゲーム機"),
            ("Nhà trọ của bạn", "Your home", ""),
            ("Cơm nắm · Đồ ăn", "Rice balls · Food", ""),
            ("Đổi vé lấy quà", "Trade tickets for prizes", ""),
            ("Làm bài thi · F", "Take a test · F", "F"),
            ("Lễ hội mùa hè", "Summer festival", ""),
            ("NEW! 5 bộ thẻ", "NEW! 5 card sets", "NEW! カード5セット"),
            ("Độ chính xác:", "Accuracy:", "正確さ："),
            ("Nhân vật  [Tab]", "Character  [Tab]", "キャラ  [Tab]"),
            ("[E] Tương tác", "[E] Interact", "[E] 調べる"),
            ("Quầy đổi quà", "Prize counter", ""),
            ("Ga tàu điện", "Train station", "駅"),
            ("Lịch đổ rác", "Garbage days", ""),
            ("Máy gắp thú", "Claw machine", "クレーンゲーム"),
            ("Vé tàu ¥450", "Train ticket ¥450", "450円"),
            ("Món đã xong", "Dish ready", ""),
            ("Hoàn thành:", "Completed:", "完了："),
            ("Nghe hiểu:", "Listening:", "聴解："),
            ("Ngữ pháp:", "Grammar:", "文法："),
            ("Đọc hiểu:", "Reading:", "読解："),
            ("Từ vựng:", "Vocabulary:", "語彙："),
            ("Thanh toán", "Pay", ""),
            ("Máy nhảy", "Dance machine", "ダンスゲーム"),
            ("Cửa hàng", "Store", "みどりしょうてん"),
            ("Kho hàng", "Stockroom", ""),
            ("Đảo Xanh", "Midori Island", "みどりじま"),
            ("Đồ uống", "Drinks", ""),
            ("Nhiệm vụ", "Tasks", "タスク"),
            ("Cô Hana", "Hana", ""),
            ("Lối ra", "Exit", ""),
            ("Lớp N5", "N5 class", ""),
        };

        private sealed class Entry { public string Original, Applied; }
        private static readonly Dictionary<TMP_Text, Entry> Entries = new Dictionary<TMP_Text, Entry>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Entries.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Loc.Changed -= ApplyAll;
            Loc.Changed += ApplyAll;
            for (int i = 0; i < SceneManager.sceneCount; i++) Scan(SceneManager.GetSceneAt(i));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Scan(scene);

        private static void Scan(Scene scene)
        {
            if (!scene.IsValid() || scene.name == "01_MainMenu") return; // the menu translates itself (MainMenuUI)
            foreach (var root in scene.GetRootGameObjects())
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null || Entries.ContainsKey(text) || !HasVietnamese(text.text)) continue;
                var entry = new Entry { Original = text.text, Applied = text.text };
                Entries[text] = entry;
                Apply(text, entry);
            }
        }

        private static void ApplyAll()
        {
            var dead = new List<TMP_Text>();
            foreach (var pair in Entries)
            {
                if (pair.Key == null) { dead.Add(pair.Key); continue; }
                Apply(pair.Key, pair.Value);
            }
            foreach (var key in dead) Entries.Remove(key);
        }

        private static void Apply(TMP_Text text, Entry entry)
        {
            if (text.text != entry.Applied) return; // code has taken this text over
            string value = Translate(entry.Original);
            text.text = value;
            entry.Applied = value;
        }

        /// <summary>The authored text in the current language (public for tests).</summary>
        public static string Translate(string original)
        {
            if (string.IsNullOrEmpty(original) || Loc.IsVietnamese) return original;
            string value = original;
            foreach (var (vi, en, ja) in Phrases)
                if (value.Contains(vi)) value = value.Replace(vi, Loc.IsEnglish ? en : ja);
            return Loc.IsJapanese ? CleanUp(value) : value;
        }

        private static string CleanUp(string value)
        {
            value = Regex.Replace(value, @"<size=[^>]*>\s*</size>", "");
            var lines = value.Split('\n');
            var kept = new List<string>();
            foreach (string raw in lines)
            {
                string line = Regex.Replace(raw, @"\s*[·|]\s*(?=<size|$)", " ");
                line = Regex.Replace(line, @"(\s*·\s*)+$", "").TrimEnd();
                line = Regex.Replace(line, @"\s{3,}", "  ");
                if (line.Trim().Length > 0) kept.Add(line);
            }
            return string.Join("\n", kept);
        }

        private static bool HasVietnamese(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
                if ("ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵĂÂĐÊÔƠƯ".IndexOf(c) >= 0) return true;
            return false;
        }
    }
}
