using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Data;
using NihongoLife.Core;
using NihongoLife.Scenario;
using System.Collections.Generic;
using System.Linq;

namespace NihongoLife.UI
{
    /// <summary>
    /// End of a story quest. A card drawn with UiKit over the town: chapter, title (Japanese + interface language),
    /// score with stars, the score categories as bars, rewards, where the story goes next, and one main button whose
    /// job follows StoryDirector — "Next mission" during the opening, "Try again" after a failure, otherwise
    /// "Keep exploring" (back to free play; the first time the free-play card follows). The old serialized texts of
    /// the scene panel are hidden; the fields stay so the scene keeps its references.
    /// </summary>
    public class ResultUI : MonoBehaviour
    {
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI overallScoreText;

        [Header("Breakdown Texts")]
        [SerializeField] private TextMeshProUGUI vocabularyScoreText;
        [SerializeField] private TextMeshProUGUI grammarScoreText;
        [SerializeField] private TextMeshProUGUI listeningScoreText;
        [SerializeField] private TextMeshProUGUI readingScoreText;
        [SerializeField] private TextMeshProUGUI accuracyScoreText;
        [SerializeField] private TextMeshProUGUI completionScoreText;

        [Header("Control Buttons")]
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private TextMeshProUGUI returnToMenuButtonText;

        private string _completedScenarioId;
        private string _nextScenarioId;
        private bool _lastResultWasSuccess;
        private bool _freePlayJustOpened;
        private RectTransform _card;

        /// <summary>What the main button does now (tests read it): "next", "retry" or "explore".</summary>
        public string ContinueAction => !string.IsNullOrEmpty(_nextScenarioId) ? "next" : !_lastResultWasSuccess && !string.IsNullOrEmpty(_completedScenarioId) ? "retry" : "explore";

        public void DisplayResults(ScoreBreakdownDto breakdown)
        {
            if (breakdown == null) return;
            _completedScenarioId = breakdown.scenarioId;
            _lastResultWasSuccess = breakdown.success;
            // The opening runs straight on; after it the story is the player's choice (StoryDirector), so the result
            // only goes back to the town — the HUD then shows the next story step.
            _nextScenarioId = string.Empty;
            if (_lastResultWasSuccess && StoryDirector.Campaign.Count > 0)
            {
                if (StoryDirector.InPrologue) _nextScenarioId = StoryDirector.AutoStart()?.id ?? string.Empty;
            }
            else if (_lastResultWasSuccess) _nextScenarioId = FindNextScenarioId(_completedScenarioId);
            _freePlayJustOpened = _lastResultWasSuccess && _completedScenarioId == StoryDirector.PrologueEnd && !StoryDirector.FreePlayAnnounced;

            if (_lastResultWasSuccess) NihongoLife.Audio.Soundscape.Jingle("quest_complete");
            ScenarioDefinition definition = null;
            if (GameServices.TryGet(out IScenarioRepository repo)) definition = repo.GetScenarioById(breakdown.scenarioId);
            Build(breakdown, definition);
        }

        // ─────────── The card ───────────

        private void Build(ScoreBreakdownDto b, ScenarioDefinition s)
        {
            if (_card != null) Destroy(_card.gameObject);
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            var background = GetComponent<Image>();
            if (background != null) background.color = new Color(0f, 0f, 0f, 0f);

            var root = UiKit.Rect(transform, "ResultCard");
            UiKit.Fill(root);
            _card = root;
            var dim = UiKit.Image(root, "Dim", UiTheme.Backdrop, null, true);
            UiKit.Fill(dim.rectTransform);
            var card = UiKit.Card(root, "Card", UiTheme.Ink, 26, 0.5f).rectTransform;
            UiKit.Pin(card, new Vector2(0.5f, 0.5f), 0f, 0f, 820f, 620f);

            bool ok = b.success;
            Color accent = ok ? UiTheme.Gold : UiTheme.Bad;
            string chapter = s != null ? StoryDirector.ChapterName(s.chapterIndex) : "";
            string kicker = (ok ? Loc.T("NHIỆM VỤ HOÀN THÀNH", "MISSION COMPLETE", "ミッション完了") : Loc.T("CHƯA ĐẠT — THỬ LẠI NHÉ", "NOT YET — TRY AGAIN", "もう少し — もう一度"))
                + (string.IsNullOrEmpty(chapter) ? "" : "  ·  " + chapter.ToUpperInvariant());
            UiKit.Place(UiKit.Label(card, "Kicker", kicker, 14f, accent, FontStyles.Bold), 44f, 34f, 560f, 22f);
            string ja = s != null ? s.titleJa : "ミッション";
            string local = s != null ? s.DisplayTitle : Loc.T("Nhiệm vụ", "Mission", "ミッション");
            var titleJa = UiKit.Label(card, "TitleJa", ja, 30f, UiTheme.Text, FontStyles.Bold);
            titleJa.enableAutoSizing = true; titleJa.fontSizeMin = 20f; titleJa.fontSizeMax = 30f;
            UiKit.Place(titleJa, 44f, 60f, 560f, 44f);
            if (!Loc.IsJapanese && local != ja) UiKit.Place(UiKit.Label(card, "Title", local, 18f, UiTheme.TextSoft), 44f, 106f, 560f, 26f);

            // Score medallion with stars under it
            var medal = UiKit.Card(card, "Score", UiKit.Alpha(accent, 0.14f), 70).rectTransform;
            UiKit.Pin(medal, new Vector2(1f, 1f), -44f, -34f, 140f, 140f);
            UiKit.Border(medal, UiKit.Alpha(accent, 0.6f), 70);
            UiKit.Pin(UiKit.Label(medal, "Value", b.overallScore.ToString(), 46f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center).rectTransform, new Vector2(0.5f, 0.5f), 0f, 10f, 130f, 56f);
            UiKit.Pin(UiKit.Label(medal, "Of", "/ 100", 15f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center).rectTransform, new Vector2(0.5f, 0.5f), 0f, -28f, 130f, 20f);
            int stars = b.overallScore >= 90 ? 3 : b.overallScore >= 70 ? 2 : b.overallScore >= 50 ? 1 : 0;
            for (int i = 0; i < 3; i++)
            {
                var star = UiKit.Icon(card, "star", i < stars ? UiTheme.Gold : UiKit.Alpha(UiTheme.Muted, 0.35f), 26f);
                UiKit.Pin(star.rectTransform, new Vector2(1f, 1f), -144f + i * 32f, -186f, 26f, 26f);
            }

            // Categories
            var cats = (b.categories ?? new List<ScoreCategoryResult>()).Where(c => c != null && !string.IsNullOrEmpty(c.category)).ToList();
            float y = 150f;
            UiKit.Place(UiKit.Label(card, "CatTitle", "Kết quả từng phần", "Breakdown", "内訳", 15f, UiTheme.Muted, FontStyles.Bold), 44f, y, 400f, 22f);
            y += 30f;
            foreach (var c in cats.Take(6))
            {
                UiKit.Place(UiKit.Label(card, "Cat_" + c.category, CategoryName(c.category), 16f, UiTheme.TextSoft), 44f, y, 170f, 24f);
                var track = UiKit.Card(card, "Track_" + c.category, new Color(1f, 1f, 1f, 0.07f), 5).rectTransform;
                UiKit.Place(track, 220f, y + 8f, 300f, 10f);
                var fill = UiKit.Card(track, "Fill", c.score >= 70 ? UiTheme.Good : c.score >= 50 ? UiTheme.Warn : UiTheme.Bad, 5).rectTransform;
                fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01(c.score / 100f), 1f); fill.offsetMin = fill.offsetMax = Vector2.zero;
                UiKit.Place(UiKit.Label(card, "CatValue_" + c.category, c.score.ToString(), 16f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineRight), 528f, y, 60f, 24f);
                y += 34f;
            }

            // Rewards
            float rx = 44f, ry = 400f;
            if (ok && b.rewardKnowledge > 0) rx = Chip(card, rx, ry, "school", UiTheme.Teal, Loc.F("+{0} trí tuệ", "+{0} knowledge", "知識 +{0}", b.rewardKnowledge));
            if (ok && b.rewardYen > 0) rx = Chip(card, rx, ry, "payments", UiTheme.Gold, "+" + Loc.Yen(b.rewardYen));
            if (ok && !b.firstCompletion) Chip(card, rx, ry, "replay", UiTheme.Muted, Loc.T("Chơi lại — thưởng ít hơn", "Replay — smaller reward", "再挑戦 — 報酬は少なめ"));

            // Where the story goes
            string story = StoryLine();
            if (!string.IsNullOrEmpty(story))
            {
                var note = UiKit.Card(card, "StoryNote", UiTheme.Surface, 14).rectTransform;
                UiKit.Place(note, 44f, 452f, 732f, 60f);
                UiKit.Pin(UiKit.Icon(note, "menu_book", UiTheme.Gold, 22f).rectTransform, new Vector2(0f, 0.5f), 18f, 0f, 22f, 22f);
                var text = UiKit.Label(note, "Text", story, 15f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true);
                UiKit.Fill(text.rectTransform, 52f, 6f, 16f, 6f);
            }

            // Buttons
            string label = ContinueAction switch
            {
                "next" => Loc.T("Nhiệm vụ tiếp theo", "Next mission", "次のミッション"),
                "retry" => Loc.T("Thử lại", "Try again", "もう一度"),
                _ => Loc.T("Tiếp tục khám phá", "Keep exploring", "探検をつづける"),
            };
            string icon = ContinueAction == "next" ? "arrow_forward" : ContinueAction == "retry" ? "replay" : "explore";
            returnToMenuButton = UiKit.Button(card, "Continue", label, OnContinueClicked, UiKit.Kind.Primary, icon, 18f);
            UiKit.Pin((RectTransform)returnToMenuButton.transform, new Vector2(1f, 0f), -44f, 30f, 270f, 54f);
            returnToMenuButtonText = UiKit.LabelOf(returnToMenuButton);
            if (ContinueAction == "retry")
            {
                var town = UiKit.Button(card, "BackToTown", "Về thị trấn", "Back to town", "町へもどる", BackToTown, UiKit.Kind.Secondary, "home", 16f);
                UiKit.Pin((RectTransform)town.transform, new Vector2(1f, 0f), -330f, 30f, 200f, 54f);
            }
        }

        private static float Chip(RectTransform card, float x, float y, string icon, Color color, string text)
        {
            var chip = UiKit.Card(card, "Reward_" + icon, UiKit.Alpha(color, 0.14f), 18).rectTransform;
            float w = 60f + text.Length * 8.8f;
            UiKit.Place(chip, x, y, w, 38f);
            UiKit.Pin(UiKit.Icon(chip, icon, color, 20f).rectTransform, new Vector2(0f, 0.5f), 14f, 0f, 20f, 20f);
            UiKit.Fill(UiKit.Label(chip, "Text", text, 15f, UiTheme.Text, FontStyles.Bold).rectTransform, 42f, 0f, 10f, 0f);
            return x + w + 10f;
        }

        private string StoryLine()
        {
            if (!_lastResultWasSuccess) return Loc.T("Không sao — nói sai cũng là học. Thử lại, mọi người ở đây đều kiên nhẫn với bạn.", "It's fine — mistakes are how you learn. Try again; everyone here is patient.", "まちがえても大丈夫。もう一度やってみよう。");
            if (_freePlayJustOpened) return Loc.T("Phần mở đầu đã xong — chế độ tự do mở ra: làm thêm, nông trại, khám phá theo ý bạn.", "The opening is done — free play opens: jobs, the farm, explore as you like.", "オープニングおわり — 自由行動が開きます。");
            if (StoryDirector.InPrologue)
            {
                var (done, total) = StoryDirector.PrologueProgress();
                var next = StoryDirector.Next();
                return Loc.F("Mở đầu {0}/{1}", "Opening {0}/{1}", "オープニング {0}/{1}", done, total) + (next != null ? "  ·  " + Loc.T("Tiếp theo: ", "Next: ", "つぎ：") + next.DisplayTitle : "");
            }
            if (StoryDirector.StoryComplete) return Loc.T("Bạn đã đi hết cốt truyện mùa xuân – hè. Hibari vẫn chờ bạn mỗi ngày!", "You finished the spring–summer story. Hibari is still here every day!", "春〜夏のストーリーをクリア！ひばりは毎日あなたを待っています。");
            var hud = StoryDirector.Hud();
            return hud != null ? Loc.T("Cốt truyện tiếp: ", "Story next: ", "ストーリーのつぎ：") + hud.Value.text + " — " + hud.Value.progress : null;
        }

        private static string CategoryName(string key)
        {
            string k = key.ToLowerInvariant();
            if (k.Contains("vocab")) return Loc.T("Từ vựng", "Vocabulary", "語彙");
            if (k.Contains("gram")) return Loc.T("Ngữ pháp", "Grammar", "文法");
            if (k.Contains("listen")) return Loc.T("Nghe", "Listening", "聴解");
            if (k.Contains("read")) return Loc.T("Đọc hiểu", "Reading", "読解");
            if (k.Contains("accur") || k.Contains("response")) return Loc.T("Phản hồi đúng", "Right responses", "正しい応答");
            if (k.Contains("compl") || k.Contains("task")) return Loc.T("Hoàn thành", "Completion", "達成");
            if (k.Contains("polite") || k.Contains("keigo")) return Loc.T("Lịch sự", "Politeness", "ていねいさ");
            return key;
        }

        // ─────────── Actions ───────────

        private void OnContinueClicked()
        {
            if (!string.IsNullOrEmpty(_nextScenarioId) && ScenarioManager.Instance != null)
            {
                Debug.Log($"[ResultUI] Continuing to next scenario: {_nextScenarioId}");
                ScenarioManager.Instance.StartScenario(_nextScenarioId);
                return;
            }

            if (!_lastResultWasSuccess && !string.IsNullOrEmpty(_completedScenarioId) && ScenarioManager.Instance != null)
            {
                Debug.Log($"[ResultUI] Retrying scenario: {_completedScenarioId}");
                ScenarioManager.Instance.StartScenario(_completedScenarioId);
                return;
            }

            BackToTown();
        }

        /// <summary>Closes the result and returns to the town (free play); the first time, the free-play card follows.</summary>
        public void BackToTown()
        {
            var manager = SceneRefs.Find<UIManager>();
            if (manager != null) manager.ShowHUDOnly();
            else gameObject.SetActive(false);
            if (_freePlayJustOpened) { _freePlayJustOpened = false; FreePlayCardUI.ShowOnce(); } // the cursor itself is CursorDirector's
        }

        /// <summary>Main menu (kept for the pause menu / tests).</summary>
        public void ReturnToMenu()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (GameServices.TryGet(out SceneFlowController sceneFlow)) sceneFlow.LoadScene("01_MainMenu");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("01_MainMenu");
        }

        private static string FindNextScenarioId(string currentScenarioId)
        {
            if (string.IsNullOrEmpty(currentScenarioId)) return string.Empty;
            if (GameServices.TryGet(out ScenarioCampaignManager campaign)) return campaign.GetNextScenarioId(currentScenarioId);
            if (GameServices.TryGet(out GameControlService control) && GameServices.TryGet(out IScenarioRepository configuredRepo))
                return control.FindNextCampaignScenarioId(currentScenarioId, configuredRepo);
            return string.Empty;
        }
    }
}
