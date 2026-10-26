using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.StudyModes;
using NihongoLife.StudyModes.Video;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// "Học thêm / Study more" hub (U, or the Study button on touch): Japanese sentence patterns (文型), English Basic
    /// and the in-game video library. Separate from the Exam Center (K), which stays for real JLPT/IELTS tests.
    /// English Basic opens whatever registered <see cref="StudyModeRegistry.EnglishBasic"/>.
    /// </summary>
    public sealed class StudyHubPopup : MenuPopupBase
    {
        private const float W = 1180f, H = 640f;
        private RectTransform _tiles;
        private TextMeshProUGUI _status;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Học thêm"; en = "Study more"; ja = "もっと学ぶ"; }

        public static StudyHubPopup GetOrCreate(GameObject host, TMP_FontAsset font)
        {
            var hub = host.GetComponent<StudyHubPopup>();
            if (hub == null) hub = host.AddComponent<StudyHubPopup>();
            hub.Initialize(font);
            return hub;
        }

        protected override void Build(RectTransform card)
        {
            var sub = UiKit.Label(card, "Subtitle",
                "Luyện mẫu câu, tiếng Anh cơ bản và xem video ngay trong game. Thi thật vẫn ở Trung tâm luyện thi (K).",
                "Practise sentence patterns and basic English, and watch videos in-game. Real exams stay in the Exam Center (K).",
                "文型・基礎英語の練習と動画視聴をゲーム内で。本番形式の試験は試験センター（K）へ。", 15f, UiTheme.Muted, wrap: true);
            UiKit.Place(sub.rectTransform, 36f, 60f, W - 120f, 26f);
            _tiles = UiKit.Rect(card, "Tiles");
            UiKit.Place(_tiles, 32f, 112f, W - 64f, 420f);
            _status = UiKit.Label(card, "Status", "", 15f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true);
            UiKit.Place(_status.rectTransform, 36f, H - 88f, W - 72f, 52f);
        }

        protected override void OnOpened()
        {
            // English Basic is built in: (re)register its launcher against this hub so the tile opens EnglishBasicUI.
            StudyModeRegistry.Register(StudyModeRegistry.EnglishBasic, OpenEnglishBasic);
            Rebuild();
        }
        protected override void OnLanguageApplied() { if (IsOpen) Rebuild(); }

        protected override void Update()
        {
            base.Update();
            if (Keyboard.current == null || !Keyboard.current.uKey.wasPressedThisFrame) return;
            if (IsOpen) { Hide(); return; }
            if (!UiModalStack.BlocksHotkeys) Show();
        }

        private void Rebuild()
        {
            if (_tiles == null) return;
            UiKit.Clear(_tiles);
            float tw = (W - 64f - 2 * 24f) / 3f;
            var store = LearningProgressStore.Shared;
            int stars = store.TotalStars(JapanesePatternCatalog.All.Select(p => p.id));
            int maxStars = JapanesePatternCatalog.All.Count * 3;
            int watched = VideoCatalog.All.Count(v => store.IsVideoWatched(v.id));
            bool english = StudyModeRegistry.IsAvailable(StudyModeRegistry.EnglishBasic);

            Tile(0, tw, "Mẫu câu tiếng Nhật", "Japanese patterns", "文型練習", "translate", UiTheme.Jlpt,
                Loc.F("N5 → N3 · {0} mẫu câu · ví dụ có furigana, romaji và bài luyện điền/chọn/sắp xếp.",
                      "N5 → N3 · {0} patterns · examples with furigana and romaji, fill-in / choose / reorder drills.",
                      "N5→N3・{0}文型・ふりがな/ローマ字付き例文と穴埋め・選択・並べ替えドリル。", JapanesePatternCatalog.All.Count),
                Loc.F("★ {0}/{1}", "★ {0}/{1}", "★ {0}/{1}", stars, maxStars), true, OpenPatterns);
            Tile(1, tw, "Tiếng Anh cơ bản", "English Basic", "基礎英語", "school", UiTheme.Ielts,
                Loc.F("A1 · {0} chủ đề: chào hỏi, số đếm, mua sắm, hỏi đường… Từ vựng, câu ví dụ và bài kiểm tra nhỏ.",
                      "A1 · {0} topics: greetings, numbers, shopping, directions… Vocabulary, examples and short quizzes.",
                      "A1・{0}テーマ：あいさつ・数字・買い物・道案内…単語・例文・ミニクイズ。", EnglishBasicCatalog.All.Count),
                english ? Loc.F("★ {0}/{1}", "★ {0}/{1}", "★ {0}/{1}", store.TotalStars(EnglishBasicCatalog.All.Select(l => l.id)), EnglishBasicCatalog.All.Count * 3) : Loc.T("Sắp ra mắt", "Coming soon", "近日公開"), english, OpenEnglish);
            Tile(2, tw, "Video bài học", "Video library", "動画ライブラリ", "videocam", UiTheme.Teal,
                Loc.F("{0} video từ kênh NihongoLife — xem trong game, có tóm tắt và phụ đề.",
                      "{0} videos from the NihongoLife channel — watch in-game with summary and transcript.",
                      "NihongoLifeチャンネルの動画{0}本—要約・字幕付きでゲーム内視聴。", VideoCatalog.All.Count),
                Loc.F("Đã xem {0}/{1}", "Watched {0}/{1}", "視聴済み {0}/{1}", watched, VideoCatalog.All.Count), true, OpenVideos);
            _status.text = "";
        }

        private void Tile(int index, float width, string vi, string en, string ja, string icon, Color color, string body, string badge, bool enabled, Action open)
        {
            var card = UiKit.Card(_tiles, "Tile" + index, UiTheme.Surface, 20, 0.3f);
            UiKit.Place(card.rectTransform, index * (width + 24f), 0f, width, 420f);
            var wash = UiKit.Image(card.transform, "Wash", UiKit.Alpha(color, 0.18f), UiKit.VerticalFade);
            UiKit.Strip(wash.rectTransform, true, 0f, 150f);
            var disc = UiKit.Card(card.transform, "Disc", UiKit.Alpha(color, 0.25f), 40, 0f, false);
            UiKit.Place(disc.rectTransform, 28f, 28f, 80f, 80f);
            var ic = UiKit.Icon(disc.transform, icon, color, 44f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 44f, 44f);
            var title = UiKit.Label(card.transform, "Title", Loc.T(vi, en, ja), 25f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 28f, 126f, width - 56f, 36f);
            var b = UiKit.Label(card.transform, "Body", body, 15.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            UiKit.Place(b.rectTransform, 28f, 170f, width - 56f, 120f);
            var chip = UiKit.Chip(card.transform, "Badge", badge, UiKit.Alpha(color, 0.3f), UiTheme.Text, enabled ? "star" : "hourglass_top", 13f);
            UiKit.Place(chip, 28f, 300f, width - 56f, 28f);
            var go = UiKit.Button(card.transform, "Open", "Mở", "Open", "開く", () => open(), enabled ? UiKit.Kind.Primary : UiKit.Kind.Quiet, "arrow_forward");
            UiKit.Place((RectTransform)go.transform, 28f, 348f, width - 56f, 48f);
        }

        private T Child<T>() where T : MenuPopupBase
        {
            var popup = GetComponent<T>();
            if (popup == null) popup = gameObject.AddComponent<T>();
            popup.Initialize(Font);
            return popup;
        }

        public void OpenPatterns() { Hide(); Child<JapanesePatternsUI>().Show(); }
        public void OpenVideos() { Hide(); Child<InGameVideoPlayerUI>().Show(); }
        public void OpenEnglishBasic() { Hide(); Child<EnglishBasicUI>().Show(); }

        private void OpenEnglish()
        {
            if (StudyModeRegistry.TryLaunch(StudyModeRegistry.EnglishBasic)) { Hide(); return; }
            _status.text = Loc.T("Khóa tiếng Anh cơ bản đang được hoàn thiện — trong lúc chờ, hãy xem video hoặc luyện IELTS ở Trung tâm luyện thi (K).",
                "English Basic is being finished — meanwhile watch videos or practise IELTS in the Exam Center (K).",
                "基礎英語は準備中です。それまで動画視聴や試験センター（K）でIELTS練習をどうぞ。");
        }
    }
}
