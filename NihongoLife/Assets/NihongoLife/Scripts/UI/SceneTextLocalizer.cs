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
            ("Vào phòng khám", "Enter the clinic", "診察室へ"),
            ("Trung tâm trò chơi học tiếng Nhật", "Japanese-learning arcade", "日本語を学べるゲームセンター"),
            ("Phụ việc nông trại", "Farm helper", "農場のお手伝い"),
            ("Tuyển làm thêm", "Part-time job", "アルバイト募集"),
            ("Tuyển phục vụ", "Server wanted", "ホールスタッフ募集"),
            ("/ca", "/shift", "／回"),
            ("/buổi", "/show", "／回"),
            ("/bức", "/piece", "／枚"),
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

        [System.Serializable] private sealed class Phrase { public string vi, en, ja, ru; }
        [System.Serializable] private sealed class Repay { public string from, to; }
        [System.Serializable] private sealed class PhraseFile { public Phrase[] entries = new Phrase[0]; public Repay[] repay = new Repay[0]; }

        private static PhraseFile _file;

        /// <summary>Signs of the areas (scene_phrases.json), longest first. Job boards were typed with the old pay: repay corrects
        /// them in every language.</summary>
        private static PhraseFile File
        {
            get
            {
                if (_file != null) return _file;
                var asset = Resources.Load<TextAsset>("Content/Localization/scene_phrases");
                _file = asset != null ? JsonUtility.FromJson<PhraseFile>(asset.text) ?? new PhraseFile() : new PhraseFile();
                System.Array.Sort(_file.entries, (a, b) => b.vi.Length.CompareTo(a.vi.Length));
                return _file;
            }
        }
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
                if (text == null || Entries.ContainsKey(text) || !(HasVietnamese(text.text) || HasYen(text.text))) continue;
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
            if (string.IsNullOrEmpty(original)) return original;
            string value = original;
            foreach (var pay in File.repay)
                if (value.Contains(pay.from)) value = value.Replace(pay.from, pay.to);
            if (Loc.IsVietnamese) return Loc.Currency(value);
            foreach (var phrase in File.entries)
                if (value.Contains(phrase.vi))
                {
                    string shown = Loc.IsJapanese ? phrase.ja : Loc.Code == "ru" && !string.IsNullOrEmpty(phrase.ru) ? phrase.ru : phrase.en;
                    value = value.Replace(phrase.vi, shown);
                }
            foreach (var (vi, en, ja) in Phrases)
                if (value.Contains(vi)) value = value.Replace(vi, Loc.IsJapanese ? ja : Loc.Data(en));
            return Loc.IsJapanese ? CleanUp(value) : Loc.Currency(value);
        }

        // G3: prices on shop tags and posters (¥150) follow the language's currency too.
        private static bool HasYen(string text) => !string.IsNullOrEmpty(text) && text.IndexOf('\u00a5') >= 0;

        private static string CleanUp(string value)
        {
            value = Regex.Replace(value, @"<size=[^>]*>\s*</size>", "");
            var lines = value.Split('\n');
            var kept = new List<string>();
            foreach (string raw in lines)
            {
                string line = Regex.Replace(raw, @"^\s*[/·]\s*", "");
                line = Regex.Replace(line, @"\s*[·|/]\s*(?=<size|$)", " ");
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
                if ("ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵĂÂĐÊÔƠƯ".IndexOf(char.ToLowerInvariant(c)) >= 0) return true;
            return false;
        }
    }
}
