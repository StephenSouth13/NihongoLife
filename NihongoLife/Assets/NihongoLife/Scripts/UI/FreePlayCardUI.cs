using NihongoLife.Core;
using NihongoLife.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Shown once, when the story's opening is finished (StoryDirector): the town is now the player's to explore.
    /// Six tiles say what opened and how it feeds the story (knowledge unlocks the next chapters and places).
    /// </summary>
    public sealed class FreePlayCardUI : MonoBehaviour
    {
        public static FreePlayCardUI Open { get; private set; }

        /// <summary>Shows the card if free play has not been announced yet (returns whether it is showing).</summary>
        public static bool ShowOnce()
        {
            if (StoryDirector.FreePlayAnnounced || StoryDirector.InPrologue) return false;
            Show();
            return true;
        }

        public static void Show()
        {
            if (Open != null) return;
            var canvas = NLUi.CreateCanvas("FreePlayCard", 320);
            Open = canvas.gameObject.AddComponent<FreePlayCardUI>();
            Open.Build(canvas.transform);
            StoryDirector.MarkFreePlayAnnounced();
            UiModalStack.Register(Open, () => Open != null, Close, "Free play");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void Close()
        {
            if (Open == null) return;
            Destroy(Open.gameObject);
            Open = null;
        }

        private void OnDestroy() { if (Open == this) Open = null; }

        private static readonly (string icon, Color color, string vi, string en, string ja, string dVi, string dEn, string dJa)[] Tiles =
        {
            ("menu_book", UiTheme.Gold, "Cốt truyện tiếp tục", "The story goes on", "ストーリーはつづく",
                "Chương 3 mở nhiều việc cùng lúc: mèo của Suzuki, luật đổ rác, ramen, sushi. Mỗi việc cần một ít điểm trí tuệ.",
                "Chapter 3 opens several quests at once: Suzuki's cat, garbage rules, ramen, sushi. Each needs some knowledge.",
                "第3章は同時にいくつも：鈴木さんのねこ、ごみのルール、ラーメン、すし。それぞれ知識ポイントが必要。"),
            ("school", UiTheme.Teal, "Học & thi", "Study & exams", "勉強・試験",
                "Trường Hibari, bàn học ở nhà, khu thi JLPT / IELTS: cách nhanh nhất để có điểm trí tuệ.",
                "Hibari School, your desk at home, the JLPT / IELTS exam hall: the fastest way to earn knowledge.",
                "ひばり学校、家の机、JLPT・IELTSの試験：知識ポイントがいちばん早くたまる。"),
            ("work", UiTheme.Good, "Làm thêm", "Part-time jobs", "アルバイト",
                "Ca làm ở konbini, quán sushi, nông trại: kiếm tiền và kinh nghiệm, học kính ngữ khi phục vụ khách.",
                "Shifts at the konbini, the sushi bar and the farm: money, experience and polite Japanese with customers.",
                "コンビニ・すし屋・農場のシフト：お金と経験、接客の敬語。"),
            ("agriculture", new Color(0.45f, 0.78f, 0.35f), "Ga & đảo Midori", "Station & Midori Island", "駅・みどり島",
                "Mở theo điểm trí tuệ. Trên đảo: trồng rau, câu cá, nuôi thú, trang trí đảo và thăm đảo bạn bè.",
                "Open with knowledge. On the island: grow vegetables, fish, keep animals, decorate it and visit friends' islands.",
                "知識ポイントで開く。島では野菜・つり・動物・島のかざり・友だちの島へ。"),
            ("sports_esports", UiTheme.Live, "Game Center", "Game Center", "ゲームセンター",
                "Sáu trò chơi luyện kana, từ vựng, số đếm; đổi vé lấy quà.",
                "Six games for kana, words and numbers; trade tickets for prizes.",
                "かな・ことば・かずの6つのゲーム。チケットでけいひん。"),
            ("groups", UiTheme.Ielts, "Bạn bè online", "Friends online", "オンラインの友だち",
                "Người chơi khác là bạn cùng lớp thật: chat ở bảng tin khu phố, học cùng, thăm đảo của nhau.",
                "Other players are real classmates: chat on the town board, study together, visit each other's islands.",
                "ほかのプレイヤーは本当のクラスメート：町の掲示板でチャット、いっしょに勉強、島をたずねる。"),
        };

        private void Build(Transform root)
        {
            var dim = UiKit.Image(root, "Dim", UiTheme.Backdrop, null, true);
            UiKit.Fill(dim.rectTransform);
            var card = UiKit.Card(root, "Card", UiTheme.Ink, 26, 0.5f);
            var rect = card.rectTransform;
            UiKit.Pin(rect, new Vector2(0.5f, 0.5f), 0f, 0f, 1120f, 700f);

            UiKit.Place(UiKit.Label(rect, "Kicker", "PHẦN MỞ ĐẦU ĐÃ XONG", "THE OPENING IS DONE", "オープニングおわり", 15f, UiTheme.Gold, FontStyles.Bold), 48f, 34f, 600f, 22f);
            UiKit.Place(UiKit.Label(rect, "Title", "Hibari giờ là nhà của bạn — tự do khám phá!", "Hibari is home now — explore freely!", "ひばりはもうあなたの町 — 自由に歩こう！", 34f, UiTheme.Text, FontStyles.Bold), 48f, 60f, 1020f, 48f);
            var sub = UiKit.Label(rect, "Sub", "Từ giờ bạn tự chọn làm gì, theo thứ tự nào cũng được. Điểm trí tuệ mở các chương tiếp theo và các địa điểm mới.",
                "From now on you choose what to do, in any order. Knowledge opens the next chapters and new places.",
                "これからは何を、どの順番でしても大丈夫。知識ポイントで次の章や新しい場所が開きます。", 17f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            UiKit.Place(sub, 48f, 112f, 1020f, 50f);

            for (int i = 0; i < Tiles.Length; i++)
            {
                var t = Tiles[i];
                int col = i % 3, row = i / 3;
                var tile = UiKit.Card(rect, "Tile_" + t.icon, UiTheme.Surface, 18);
                UiKit.Place(tile.rectTransform, 48f + col * 348f, 182f + row * 196f, 332f, 180f);
                var badge = UiKit.Card(tile.transform, "Badge", UiKit.Alpha(t.color, 0.18f), 14);
                UiKit.Place(badge.rectTransform, 18f, 18f, 48f, 48f);
                var icon = UiKit.Icon(badge.transform, t.icon, t.color, 28f);
                UiKit.Pin(icon.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 28f, 28f);
                UiKit.Place(UiKit.Label(tile.transform, "Name", t.vi, t.en, t.ja, 19f, UiTheme.Text, FontStyles.Bold), 78f, 22f, 240f, 40f);
                var d = UiKit.Label(tile.transform, "Desc", t.dVi, t.dEn, t.dJa, 14.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
                UiKit.Place(d, 18f, 76f, 296f, 96f);
            }

            UiKit.Place(UiKit.Label(rect, "Keys", () => Loc.T("N  Sổ nhiệm vụ   ·   M  Bản đồ   ·   H  Góc nhìn trên cao", "N  Task journal   ·   M  Map   ·   H  Overhead view", "N  タスク帳   ·   M  地図   ·   H  上からの視点"), 15f, UiTheme.Muted), 48f, 618f, 640f, 40f);
            var go = UiKit.Button(rect, "StartExploring", "Bắt đầu khám phá", "Start exploring", "探検をはじめる", Close, UiKit.Kind.Primary, "explore", 18f);
            UiKit.Pin((RectTransform)go.transform, new Vector2(1f, 0f), -48f, 36f, 280f, 54f);
        }
    }
}
