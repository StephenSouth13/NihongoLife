using System.Collections;
using System.Linq;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Main menu. The left column is baked by MainMenuBaker. At play, the top-right language row is gathered so it
    /// stays on screen, and Start opens offline or online before character select. Texts follow Vietnamese, English
    /// or Japanese.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button vietnameseButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button japaneseButton;
        [SerializeField] private Button guideButton;
        [SerializeField] private Button aboutButton;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI profileText;
        [SerializeField] private TextMeshProUGUI startButtonText;
        [SerializeField] private TextMeshProUGUI quitButtonText;
        [SerializeField] private TextMeshProUGUI languageLabelText;

        [Header("Settings")]
        [SerializeField] private string targetGameplayScene = "90_TestSandbox";
        // Kept on the serialized menu. StoryDirector.AutoStart picks the quest; this id is the old fixed launch.
#pragma warning disable CS0414
        [SerializeField] private string targetScenarioId = "scenario.konbini.buy_onigiri";
#pragma warning restore CS0414

        private Button settingsButton, loginButton, leaderboardButton, friendsButton, profileButton, onlineButton;
        private SettingsUI settingsUI;
        private AuthUI authUI;
        private LeaderboardUI leaderboardUI;
        private FriendsUI friendsUI;
        private PlayerProfileUI profileUI;
        private CoopLobbyUI coopLobbyUI;
        private MenuMusicController menuMusic;
        private GuidePopup guidePopup;
        private AboutPopup aboutPopup;

        private GameObject characterSelectPanel;
        private GameObject modeSelectPanel;
        private TextMeshProUGUI modeStatusText;
        private Button modeOfflineButton, modeOnlineButton;
        private CharacterPreviewDragRotate characterDrag;
        private RawImage characterPreviewImage;
        private TextMeshProUGUI characterNameText, characterTaglineText;
        private TMP_InputField playerNameInput;
        private Button previousCharacterButton, nextCharacterButton, confirmCharacterButton, closeCharacterButton;
        private RenderTexture characterPreviewTexture;
        private Camera characterPreviewCamera;
        private GameObject characterPreviewStage;
        private GameObject characterPreviewModel;
        private int selectedCharacterIndex;
        private bool startOnlineAfterCharacterConfirm;

        private void Start()
        {
            EnsureSettingsService();
            Bind();
            var font = titleText != null ? titleText.font : UiKit.Font;

            Wire(startButton, OnStartClicked);
            Wire(quitButton, OnQuitClicked);
            Wire(vietnameseButton, () => Loc.Set(GameLanguage.Vietnamese));
            Wire(englishButton, () => Loc.Set(GameLanguage.English));
            Wire(japaneseButton, () => Loc.Set(GameLanguage.Japanese));

            guidePopup = gameObject.AddComponent<GuidePopup>();
            guidePopup.Initialize(font);
            aboutPopup = gameObject.AddComponent<AboutPopup>();
            aboutPopup.Initialize(font);
            Wire(guideButton, () => guidePopup.Show());
            Wire(aboutButton, () => aboutPopup.Show());

            settingsUI = gameObject.AddComponent<SettingsUI>();
            settingsUI.Initialize(font);
            Wire(settingsButton, () => settingsUI.Show());
            authUI = gameObject.AddComponent<AuthUI>();
            authUI.Initialize(font);
            Wire(loginButton, () => authUI.Show());
            if (GameServices.TryGet(out IAuthService auth)) auth.OnAuthStateChanged += _ => { Translate(); DisplayProfileStats(); };

            leaderboardUI = gameObject.AddComponent<LeaderboardUI>();
            leaderboardUI.Initialize(font);
            Wire(leaderboardButton, () => leaderboardUI.Show());
            friendsUI = gameObject.AddComponent<FriendsUI>();
            friendsUI.Initialize(font);
            Wire(friendsButton, () => { if (OnlineAccess.IsRegistered) friendsUI.Show(); else authUI.ShowForOnline(); });
            profileUI = gameObject.AddComponent<PlayerProfileUI>();
            profileUI.Initialize(font);
            Wire(profileButton, () => profileUI.ShowOwnProfile());
            coopLobbyUI = gameObject.AddComponent<CoopLobbyUI>();
            coopLobbyUI.Initialize(font);
            Wire(onlineButton, OnOnlineClicked);

            menuMusic = gameObject.GetComponent<MenuMusicController>().OrNull() ?? gameObject.AddComponent<MenuMusicController>();
            var dayNight = SceneRefs.Find<DayNightCycle>();
            if (dayNight != null) dayNight.SetHour(9f); // the menu is always a bright morning

            MenuChrome.Arrange((RectTransform)transform);
            if (titleText != null && titleText.GetComponent<MenuTitleShimmer>() == null)
                titleText.gameObject.AddComponent<MenuTitleShimmer>();
            MenuSakura.Ensure(transform);
            ResponsiveCanvasFit.Ensure(transform);
            SafeAreaInset.Ensure(transform);
            MenuResponsive.Ensure((RectTransform)transform);
            BuildModeSelect(font);
            BuildCharacterSelect(font);
            Translate();
            DisplayProfileStats();
            Loc.Watch(this, () => { Translate(); DisplayProfileStats(); RefreshModeStatus(); });
            MenuButtonFeel.Dress(transform);
            StartCoroutine(PlayEntrance());
        }

        private IEnumerator PlayEntrance()
        {
            var group = GetComponent<CanvasGroup>().OrNull() ?? gameObject.AddComponent<CanvasGroup>();
            if (MotionSettings.ReduceMotion)
            {
                group.alpha = 1f;
                yield break;
            }
            group.alpha = 0f;
            var rect = (RectTransform)transform;
            Vector2 end = rect.anchoredPosition;
            Vector2 start = end + new Vector2(0f, -16f);
            const float duration = 0.42f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
                group.alpha = eased;
                rect.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                yield return null;
            }
            group.alpha = 1f;
            rect.anchoredPosition = end;
        }

        private void OnDestroy()
        {
            if (characterPreviewTexture != null) { characterPreviewTexture.Release(); Destroy(characterPreviewTexture); }
            if (characterPreviewStage != null) Destroy(characterPreviewStage);
        }

        private void EnsureSettingsService()
        {
            if (GameServices.TryGet(out GameSettingsService _)) return;
            var existing = SceneRefs.Find<GameSettingsService>();
            if (existing == null)
            {
                var go = new GameObject("GameSettingsService");
                existing = go.AddComponent<GameSettingsService>();
                DontDestroyOnLoad(go);
            }
            existing.Initialize();
            GameServices.Register<GameSettingsService>(existing);
        }

        /// <summary>Finds the baked elements by name (the scene owns their place and look).</summary>
        private void Bind()
        {
            startButton ??= Find<Button>("StartButton");
            quitButton ??= Find<Button>("QuitButton");
            guideButton ??= Find<Button>("GuideButton");
            aboutButton ??= Find<Button>("AboutButton");
            vietnameseButton ??= Find<Button>("LanguageVietnameseButton");
            englishButton ??= Find<Button>("LanguageEnglishButton");
            japaneseButton ??= Find<Button>("LanguageJapaneseButton");
            settingsButton = Find<Button>("SettingsButton");
            loginButton = Find<Button>("LoginButton");
            leaderboardButton = Find<Button>("LeaderboardBtn");
            friendsButton = Find<Button>("FriendsBtn");
            profileButton = Find<Button>("ProfileBtn");
            onlineButton = Find<Button>("OnlineBtn");
            titleText ??= Find<TextMeshProUGUI>("TitleText");
            subtitleText ??= Find<TextMeshProUGUI>("SubtitleText");
            profileText ??= Find<TextMeshProUGUI>("ProfileText");
        }

        private T Find<T>(string name) where T : Component
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name && t.TryGetComponent(out T c)) return c;
            return null;
        }

        /// <summary>Click + hover feedback that keeps the saved resting look.</summary>
        private static void Wire(Button button, System.Action click)
        {
            if (button == null) return;
            if (click != null) button.onClick.AddListener(() => click());
            if (button.GetComponent<UiHover>() == null) button.gameObject.AddComponent<UiHover>();
            if (button.GetComponent<UIButtonAudio>() == null) button.gameObject.AddComponent<UIButtonAudio>();
        }

        // ─────────── Texts ───────────

        private void Translate()
        {
            var database = GameServices.TryGet(out GameControlService control) ? control.Database : null;
            if (titleText != null) titleText.text = database != null && !string.IsNullOrEmpty(database.menuTitle) ? database.menuTitle : "NIHONGO LIFE";
            if (subtitleText != null)
                subtitleText.text = database != null
                    ? Loc.Pick(database.menuSubtitleVi, database.menuSubtitleEn, database.menuSubtitleJa)
                    : Loc.T("Sống một ngày ở Nhật — học tiếng Nhật qua từng việc nhỏ", "Live a day in Japan — learn Japanese one small task at a time", "日本で一日を過ごしながら、日本語を少しずつ学ぼう");
            Label(startButton, "Bắt đầu", "Start", "はじめる");
            Label(onlineButton, "Chơi online", "Play online", "オンライン");
            Label(profileButton, "Hồ sơ", "Profile", "プロフィール");
            Label(friendsButton, "Bạn bè", "Friends", "フレンド");
            Label(leaderboardButton, "Xếp hạng", "Ranking", "ランキング");
            Label(guideButton, "Cách chơi", "How to play", "遊び方");
            Label(settingsButton, "Cài đặt", "Settings", "設定");
            Label(quitButton, "Thoát", "Quit", "終了");
            Label(aboutButton, "Về chúng tôi", "About us", "私たちについて");
            bool signedIn = GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated;
            if (signedIn) Label(loginButton, "Tài khoản", "Account", "アカウント");
            else Label(loginButton, "Đăng nhập", "Sign in", "ログイン");
            if (languageLabelText != null) languageLabelText.text = Loc.T("Ngôn ngữ", "Language", "言語");
            SetSelected(vietnameseButton, Loc.IsVietnamese);
            SetSelected(englishButton, Loc.IsEnglish);
            SetSelected(japaneseButton, Loc.IsJapanese);
            if (characterSelectPanel != null) TranslateCharacterSelect();
        }

        private static void Label(Button button, string vi, string en, string ja)
        {
            var label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label != null) label.text = Loc.T(vi, en, ja);
        }

        /// <summary>The chosen language is the gold pill; the others are glass.</summary>
        private static void SetSelected(Button button, bool selected)
        {
            if (button == null) return;
            Color bg = selected ? UiTheme.Gold : new Color(1f, 1f, 1f, 0f);
            Color fg = selected ? UiTheme.OnGold : UiTheme.Text;
            var hover = button.GetComponent<UiHover>();
            if (hover != null) hover.SetBase(bg); else if (button.targetGraphic is Image image) image.color = bg;
            foreach (var t in button.GetComponentsInChildren<TextMeshProUGUI>(true)) t.color = fg;
        }

        /// <summary>The baked story card: chapter, next quest and how far the opening / the whole story is.</summary>
        private void DisplayStoryCard()
        {
            var card = transform.Find("StoryCard");
            if (card == null) return;
            var kicker = card.Find("Kicker")?.GetComponent<TextMeshProUGUI>();
            var title = card.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var detail = card.Find("Detail")?.GetComponent<TextMeshProUGUI>();
            var fill = card.Find("Track/Fill") as RectTransform;
            var campaign = NihongoLife.Scenario.StoryDirector.Campaign;
            int done = campaign.Count(NihongoLife.Scenario.StoryDirector.IsDone);
            float ratio = campaign.Count > 0 ? done / (float)campaign.Count : 0f;
            var next = NihongoLife.Scenario.StoryDirector.Next();
            if (kicker != null) kicker.text = Loc.T("CỐT TRUYỆN", "STORY", "ストーリー") + (next != null ? "  ·  " + NihongoLife.Scenario.StoryDirector.ChapterName(next.chapterIndex) : "");
            if (title != null)
                title.text = NihongoLife.Scenario.StoryDirector.StoryComplete ? Loc.T("Đã xong mùa xuân – hè ở Hibari!", "Spring–summer in Hibari: done!", "ひばりの春〜夏、クリア！")
                    : next != null ? (Loc.IsJapanese ? next.titleJa : $"{next.titleJa}  <size=72%><color=#C7D0DC>{next.DisplayTitle}</color></size>")
                    : Loc.T("Bắt đầu câu chuyện ở Hibari", "Begin your story in Hibari", "ひばりの物語をはじめよう");
            if (detail != null)
                detail.text = NihongoLife.Scenario.StoryDirector.InPrologue
                    ? Loc.F("Mở đầu {0}/{1}  ·  xong sẽ mở chế độ tự do", "Opening {0}/{1}  ·  free play opens after it", "オープニング {0}/{1} · 終わると自由行動", NihongoLife.Scenario.StoryDirector.PrologueProgress().done, NihongoLife.Scenario.StoryDirector.PrologueProgress().total)
                    : Loc.F("{0}/{1} nhiệm vụ cốt truyện  ·  chế độ tự do đang mở", "{0}/{1} story quests  ·  free play is open", "ストーリー {0}/{1} · 自由行動中", done, campaign.Count);
            if (fill != null) { fill.anchorMax = new Vector2(Mathf.Max(0.02f, ratio), 1f); fill.offsetMax = Vector2.zero; }
        }

        private void DisplayProfileStats()
        {
            DisplayStoryCard();
            if (profileText == null) return;
            string details = "";
            if (GameServices.TryGet(out Save.IProgressRepository progressRepo))
            {
                var progress = progressRepo.GetProgress();
                string name = progress.displayName;
                if (GameServices.TryGet(out IAuthService authSvc) && authSvc.IsAuthenticated && !string.IsNullOrWhiteSpace(authSvc.DisplayName)) name = authSvc.DisplayName;
                details = $"<b><color=#F7BD3D>{Loc.T("Học viên", "Learner", "学習者")}</color></b>  {name}   ·   {Loc.T("Cấp", "Level", "レベル")} {progress.level} ({progress.xp} XP)   ·   " +
                          Loc.F("{0} tình huống đã hoàn thành", "{0} scenarios completed", "完了したシナリオ {0}", progress.completedScenarios.Count) + "\n";
            }
            details += Loc.T("WASD di chuyển  ·  F tương tác  ·  B balo  ·  M bản đồ  ·  Tab nhân vật",
                             "WASD move  ·  F interact  ·  B bag  ·  M map  ·  Tab character",
                             "WASD 移動  ·  F 調べる  ·  B バッグ  ·  M 地図  ·  Tab キャラ");
            profileText.text = details.Trim();
        }

        // ─────────── Mode select ───────────

        private void BuildModeSelect(TMP_FontAsset font)
        {
            var dim = UiKit.Image(transform, "ModeSelectPanel", UiTheme.Backdrop, null, true);
            modeSelectPanel = dim.gameObject;
            UiKit.Fill(dim.rectTransform);
            var card = UiKit.Card(dim.transform, "Card", UiTheme.Ink, 24, 0.6f).rectTransform;
            card.gameObject.AddComponent<MenuSafeFrame>().Configure(new Vector2(860f, 480f), new Vector2(560f, 400f), new Vector2(40f, 32f));
            var title = UiKit.Label(card, "Title", "", 28f, UiTheme.Text, FontStyles.Bold);
            Box(title.rectTransform, 0.05f, 0.84f, 0.72f, 0.96f);
            modeStatusText = UiKit.Label(card, "Status", "", 15f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            Box(modeStatusText.rectTransform, 0.05f, 0.70f, 0.95f, 0.84f);

            modeOfflineButton = UiKit.Button(card, "OfflineModeButton", "Ngoại tuyến", "Offline", "オフライン", ChooseOffline, UiKit.Kind.Primary, "play_arrow", 20f, 16);
            Box((RectTransform)modeOfflineButton.transform, 0.05f, 0.28f, 0.48f, 0.66f);
            modeOnlineButton = UiKit.Button(card, "OnlineModeButton", "Trực tuyến", "Online", "オンライン", ChooseOnline, UiKit.Kind.Secondary, "public", 20f, 16);
            Box((RectTransform)modeOnlineButton.transform, 0.52f, 0.28f, 0.95f, 0.66f);
            var offlineHint = UiKit.Label(card, "OfflineHint", "", 13f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            Box(offlineHint.rectTransform, 0.05f, 0.16f, 0.48f, 0.28f);
            var onlineHint = UiKit.Label(card, "OnlineHint", "", 13f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            Box(onlineHint.rectTransform, 0.52f, 0.16f, 0.95f, 0.28f);
            var back = UiKit.Button(card, "ModeBack", "Quay lại", "Back", "戻る", () => modeSelectPanel.SetActive(false), UiKit.Kind.Ghost, "arrow_back", 16f, 16);
            Box((RectTransform)back.transform, 0.05f, 0.04f, 0.28f, 0.14f);

            modeSelectPanel.SetActive(false);
            UiModalStack.Register(this, () => modeSelectPanel.activeSelf && (characterSelectPanel == null || !characterSelectPanel.activeSelf), () => modeSelectPanel.SetActive(false), "ModeSelect");
            RefreshModeStatus();
        }

        private void RefreshModeStatus()
        {
            if (modeSelectPanel == null) return;
            var title = modeSelectPanel.transform.Find("Card/Title")?.GetComponent<TextMeshProUGUI>();
            if (title != null) title.text = Loc.T("Chọn cách chơi", "How do you want to play?", "遊び方を選ぶ");
            var offlineHint = modeSelectPanel.transform.Find("Card/OfflineHint")?.GetComponent<TextMeshProUGUI>();
            if (offlineHint != null) offlineHint.text = Loc.T("Một người, lưu trên máy này. Mọi nơi và nhiệm vụ đều mở — không khoá cấp.", "Single player, saved on this device. Every place and task is open — no level locks.", "ひとり用、この端末に保存。場所もタスクも全部開放、レベル制限なし。");
            var onlineHint = modeSelectPanel.transform.Find("Card/OnlineHint")?.GetComponent<TextMeshProUGUI>();
            if (onlineHint != null) onlineHint.text = Loc.T("Cốt truyện bình thường, bạn bè, phòng và voice.", "The normal story, plus friends, rooms and voice.", "通常のストーリー、フレンド、部屋、ボイス。");
            var back = modeSelectPanel.transform.Find("Card/ModeBack")?.GetComponent<Button>();
            if (back != null) UiKit.SetLabel(back, Loc.T("Quay lại", "Back", "戻る"));
            if (modeOfflineButton != null) UiKit.SetLabel(modeOfflineButton, Loc.T("Ngoại tuyến", "Offline", "オフライン"));
            if (modeOnlineButton != null) UiKit.SetLabel(modeOnlineButton, Loc.T("Trực tuyến", "Online", "オンライン"));
            if (modeStatusText == null) return;
            bool configured = OnlineServerReady();
            bool signedIn = OnlineAccess.IsRegistered;
            if (!configured)
                modeStatusText.text = Loc.T("Máy chủ chưa được cấu hình. Ngoại tuyến vẫn chơi được.", "The server is not configured yet. Offline play still works.", "サーバー未設定。オフラインでは遊べます。");
            else if (!signedIn)
                modeStatusText.text = Loc.T("Trực tuyến cần đăng nhập. Ngoại tuyến không cần.", "Online needs a sign-in. Offline does not.", "オンラインはログインが必要。オフラインはそのまま。");
            else
                modeStatusText.text = Loc.T("Đã đăng nhập. Phòng, bạn bè và voice sẵn sàng.", "Signed in. Rooms, friends and voice are ready.", "ログイン済み。部屋・フレンド・ボイスが使えます。");
        }

        private void OpenModeSelect()
        {
            if (modeSelectPanel == null) { OpenCharacterSelect(); return; }
            HideInfoPopups();
            if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
            RefreshModeStatus();
            modeSelectPanel.SetActive(true);
            modeSelectPanel.transform.SetAsLastSibling();
            UIStyleKit.PlayShowAnimation(modeSelectPanel.transform.Find("Card").gameObject);
            if (modeOfflineButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(modeOfflineButton.gameObject);
        }

        private void ChooseOffline()
        {
            startOnlineAfterCharacterConfirm = false;
            if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
            OpenCharacterSelect();
        }

        private void ChooseOnline()
        {
            bool configured = OnlineServerReady();
            RefreshModeStatus();
            if (!configured) return;
            if (!OnlineAccess.IsRegistered)
            {
                HideInfoPopups();
                authUI.ShowForOnline();
                return;
            }
            startOnlineAfterCharacterConfirm = true;
            if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
            OpenCharacterSelect();
        }

        // ─────────── Character selection ───────────

        private void BuildCharacterSelect(TMP_FontAsset font)
        {
            selectedCharacterIndex = PlayableCharacterCatalog.IndexOf(PlayerPrefs.GetString(PlayableCharacterCatalog.PlayerPrefsKey, PlayableCharacterCatalog.DefaultId));
            if (selectedCharacterIndex < 0) selectedCharacterIndex = 0;

            var dim = UiKit.Image(transform, "CharacterSelectPanel", UiTheme.Backdrop, null, true);
            characterSelectPanel = dim.gameObject;
            UiKit.Fill(dim.rectTransform);
            var card = UiKit.Card(dim.transform, "Card", UiTheme.Ink, 24, 0.6f).rectTransform;
            card.gameObject.AddComponent<MenuSafeFrame>().Configure(new Vector2(1080f, 660f), new Vector2(720f, 500f), new Vector2(36f, 28f));

            var title = UiKit.Label(card, "Title", "", 28f, UiTheme.Text, FontStyles.Bold);
            Box(title.rectTransform, 0.04f, 0.88f, 0.7f, 0.97f);
            var frame = UiKit.Card(card, "PreviewFrame", new Color(0.05f, 0.07f, 0.1f, 1f), 20, 0f, false).rectTransform;
            Box(frame, 0.04f, 0.08f, 0.48f, 0.86f);
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(frame, false);
            UiKit.Fill(preview.rectTransform, 8f, 8f, 8f, 36f);
            characterPreviewImage = preview;
            characterDrag = preview.gameObject.AddComponent<CharacterPreviewDragRotate>();
            characterDrag.Configure(ApplyCharacterPreviewYaw);
            var hint = UiKit.Label(frame, "DragHint", "Kéo để xoay", "Drag to turn", "ドラッグで回転", 13f, UiTheme.Muted, FontStyles.Italic, TextAlignmentOptions.Center);
            Box(hint.rectTransform, 0.08f, 0.02f, 0.55f, 0.1f);
            var turnLeft = UiKit.Button(frame, "TurnLeft", "↺", "↺", "↺", () => characterDrag.AddYaw(-40f), UiKit.Kind.Secondary, null, 18f, 12);
            Box((RectTransform)turnLeft.transform, 0.58f, 0.02f, 0.76f, 0.12f);
            var turnRight = UiKit.Button(frame, "TurnRight", "↻", "↻", "↻", () => characterDrag.AddYaw(40f), UiKit.Kind.Secondary, null, 18f, 12);
            Box((RectTransform)turnRight.transform, 0.78f, 0.02f, 0.96f, 0.12f);

            characterNameText = UiKit.Label(card, "CharacterName", "", 32f, UiTheme.Gold, FontStyles.Bold);
            Box(characterNameText.rectTransform, 0.52f, 0.74f, 0.96f, 0.86f);
            characterTaglineText = UiKit.Label(card, "CharacterTagline", "", 16f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            Box(characterTaglineText.rectTransform, 0.52f, 0.62f, 0.96f, 0.74f);

            previousCharacterButton = UiKit.IconButton(card, "PreviousCharacterButton", "chevron_left", () => SelectCharacter(selectedCharacterIndex - 1), UiKit.Kind.Secondary, 52f, 28f);
            Box((RectTransform)previousCharacterButton.transform, 0.52f, 0.50f, 0.60f, 0.60f);
            nextCharacterButton = UiKit.IconButton(card, "NextCharacterButton", "chevron_right", () => SelectCharacter(selectedCharacterIndex + 1), UiKit.Kind.Secondary, 52f, 28f);
            Box((RectTransform)nextCharacterButton.transform, 0.62f, 0.50f, 0.70f, 0.60f);

            var nameLabel = UiKit.Label(card, "NameLabel", "Tên của bạn", "Your name", "あなたの名前", 13f, UiTheme.Muted, FontStyles.Bold);
            Box(nameLabel.rectTransform, 0.52f, 0.44f, 0.96f, 0.50f);
            playerNameInput = NameInput(card, font);
            Box((RectTransform)playerNameInput.transform, 0.52f, 0.34f, 0.96f, 0.44f);
            playerNameInput.text = PlayableCharacterCatalog.GetPlayerName();

            confirmCharacterButton = UiKit.Button(card, "ConfirmCharacterButton", null, null, null, ConfirmCharacterAndStart, UiKit.Kind.Primary, "play_arrow", 19f, 16);
            Box((RectTransform)confirmCharacterButton.transform, 0.52f, 0.18f, 0.96f, 0.32f);
            closeCharacterButton = UiKit.Button(card, "CloseCharacterButton", null, null, null, () => characterSelectPanel.SetActive(false), UiKit.Kind.Ghost, "arrow_back", 16f, 16);
            Box((RectTransform)closeCharacterButton.transform, 0.52f, 0.06f, 0.78f, 0.16f);

            CreateCharacterPreviewStage();
            characterSelectPanel.SetActive(false);
            UiModalStack.Register(this, () => characterSelectPanel.activeSelf, () => characterSelectPanel.SetActive(false), "CharacterSelect");
            TranslateCharacterSelect();
            SelectCharacter(selectedCharacterIndex);
        }

        private static void Box(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void TranslateCharacterSelect()
        {
            var title = characterSelectPanel.transform.Find("Card/Title")?.GetComponent<TextMeshProUGUI>();
            if (title != null) title.text = Loc.T("Chọn nhân vật", "Choose your character", "キャラクター選択");
            UiKit.SetLabel(closeCharacterButton, Loc.T("Quay lại", "Back", "戻る"));
            if (playerNameInput != null && playerNameInput.placeholder is TextMeshProUGUI ph) ph.text = Loc.T("Tên nhân vật", "Character name", "名前");
            SelectCharacter(selectedCharacterIndex);
        }

        private TMP_InputField NameInput(RectTransform parent, TMP_FontAsset font)
        {
            var bg = UiKit.Card(parent, "PlayerNameInput", UiTheme.Sunken, 14, 0f, true);
            UiKit.Border(bg.rectTransform, new Color(1f, 1f, 1f, 0.14f), 14);
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            var area = UiKit.Rect(bg.transform, "TextArea");
            UiKit.Fill(area, 16f, 6f, 16f, 6f);
            area.gameObject.AddComponent<RectMask2D>();
            var text = UiKit.Label(area, "Text", "", 18f, UiTheme.Text);
            UiKit.Fill(text.rectTransform);
            var placeholder = UiKit.Label(area, "Placeholder", "", 17f, UiTheme.Muted, FontStyles.Italic);
            UiKit.Fill(placeholder.rectTransform);
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 24;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.fontAsset = font;
            input.pointSize = 18f;
            UiUserContent.Mark(text);
            return input;
        }

        private void CreateCharacterPreviewStage()
        {
            if (Application.isBatchMode) return;
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;
            characterPreviewTexture = new RenderTexture(web ? 512 : 768, web ? 600 : 900, 16, RenderTextureFormat.ARGB32)
            {
                name = "CharacterSelectPreviewTexture",
                antiAliasing = web ? 1 : 2
            };
            characterPreviewTexture.Create();
            if (characterPreviewImage != null) characterPreviewImage.texture = characterPreviewTexture;
            characterPreviewStage = new GameObject("CharacterSelectPreviewStage");
            characterPreviewStage.transform.position = new Vector3(0f, -500f, 0f);
            DontDestroyOnLoad(characterPreviewStage);
            var cameraGo = new GameObject("PreviewCamera");
            cameraGo.transform.SetParent(characterPreviewStage.transform, false);
            cameraGo.transform.localPosition = new Vector3(0f, 1.05f, 2.4f);
            cameraGo.transform.localRotation = Quaternion.Euler(8f, 180f, 0f);
            characterPreviewCamera = cameraGo.AddComponent<Camera>();
            characterPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            characterPreviewCamera.backgroundColor = new Color(0.07f, 0.09f, 0.13f, 1f);
            characterPreviewCamera.fieldOfView = 24f;
            characterPreviewCamera.nearClipPlane = 0.05f;
            characterPreviewCamera.farClipPlane = 20f;
            characterPreviewCamera.targetTexture = characterPreviewTexture;
            PreviewLight("KeyLight", new Vector3(-1.6f, 3.2f, 2.2f), Quaternion.Euler(48f, -28f, 0f), 2.4f, new Color(1f, 0.95f, 0.86f, 1f));
            PreviewLight("FillLight", new Vector3(1.4f, 2.2f, 2.6f), Quaternion.Euler(30f, 24f, 0f), 1.1f, new Color(0.85f, 0.9f, 1f, 1f));
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                PreviewLight("RimLight", new Vector3(1.8f, 2.4f, -1.6f), Quaternion.Euler(28f, 140f, 0f), 1.6f, new Color(1f, 0.82f, 0.55f, 1f));
        }

        private void PreviewLight(string name, Vector3 position, Quaternion rotation, float intensity, Color color)
        {
            if (characterPreviewStage == null) return;
            var lightGo = new GameObject(name);
            lightGo.transform.SetParent(characterPreviewStage.transform, false);
            lightGo.transform.localPosition = position;
            lightGo.transform.localRotation = rotation;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
        }

        private void SelectCharacter(int index)
        {
            int count = PlayableCharacterCatalog.Count;
            if (count <= 0) return;
            selectedCharacterIndex = (index % count + count) % count;
            var character = PlayableCharacterCatalog.Get(selectedCharacterIndex);
            if (characterNameText != null) { characterNameText.text = character.DisplayName; characterNameText.color = character.AccentColor; }
            if (characterTaglineText != null) characterTaglineText.text = character.Tagline;
            if (confirmCharacterButton != null)
            {
                confirmCharacterButton.interactable = character.IsAvailable;
                UiKit.SetLabel(confirmCharacterButton, character.IsAvailable ? Loc.T("Chọn nhân vật này", "Play as this character", "このキャラで始める") : Loc.T("Sắp ra mắt", "Coming soon", "近日公開"));
            }
            if (characterPreviewModel != null) Destroy(characterPreviewModel);
            var prefab = PlayableCharacterCatalog.LoadPrefab(character);
            if (prefab == null || characterPreviewStage == null) return;
            characterPreviewModel = Instantiate(prefab, characterPreviewStage.transform);
            characterPreviewModel.name = "SelectedCharacterPreview";
            characterPreviewModel.transform.localPosition = Vector3.zero;
            characterPreviewModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            characterPreviewModel.transform.localScale = prefab.transform.localScale; // kids are built at 1.35×
            if (characterDrag != null) characterDrag.SetYaw(0f);
            else ApplyCharacterPreviewYaw(0f);
            foreach (var collider in characterPreviewModel.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            FrameCharacterPreview();
        }

        private void FrameCharacterPreview()
        {
            if (characterPreviewModel == null || characterPreviewCamera == null || characterPreviewStage == null) return;
            var renderers = characterPreviewModel.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float height = Mathf.Max(0.6f, bounds.size.y);
            float fov = 24f;
            characterPreviewCamera.fieldOfView = fov;
            float distance = (height * 0.62f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Vector3 localCenter = characterPreviewStage.transform.InverseTransformPoint(bounds.center);
            characterPreviewCamera.transform.localPosition = new Vector3(localCenter.x, localCenter.y, localCenter.z + distance);
            characterPreviewCamera.transform.LookAt(bounds.center);
        }

        private void ApplyCharacterPreviewYaw(float yaw)
        {
            if (characterPreviewModel == null) return;
            characterPreviewModel.transform.localRotation = Quaternion.Euler(0f, 180f + yaw, 0f);
        }

        // ─────────── Flow ───────────

        private void HideInfoPopups()
        {
            guidePopup?.Hide();
            aboutPopup?.Hide();
        }

        private static bool OnlineServerReady()
        {
            var client = SceneRefs.Find<SupabaseClient>();
            return client != null && client.IsConfigured;
        }

        private void OnStartClicked() => OpenModeSelect();

        private void OpenCharacterSelect()
        {
            if (characterSelectPanel == null) { BeginGameWithSelectedCharacter(); return; }
            HideInfoPopups();
            characterSelectPanel.SetActive(true);
            characterSelectPanel.transform.SetAsLastSibling();
            SelectCharacter(selectedCharacterIndex);
            UIStyleKit.PlayShowAnimation(characterSelectPanel.transform.Find("Card").gameObject);
            if (confirmCharacterButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(confirmCharacterButton.gameObject);
        }

        private void OnOnlineClicked()
        {
            // Online is for registered, signed-in accounts; a guest is invited to register / sign in.
            if (!OnlineAccess.IsRegistered) { HideInfoPopups(); authUI.ShowForOnline(); return; }
            startOnlineAfterCharacterConfirm = true;
            HideInfoPopups();
            if (characterSelectPanel != null)
            {
                OpenCharacterSelect();
                return;
            }
            ShowOnlineLobby();
        }

        private void ConfirmCharacterAndStart()
        {
            var character = PlayableCharacterCatalog.Get(selectedCharacterIndex);
            if (!character.IsAvailable) return;
            PlayableCharacterCatalog.SaveSelected(character.Id);
            PlayableCharacterCatalog.SavePlayerName(playerNameInput != null ? playerNameInput.text : PlayableCharacterCatalog.DefaultPlayerName);
            if (startOnlineAfterCharacterConfirm)
            {
                characterSelectPanel?.SetActive(false);
                ShowOnlineLobby();
                return;
            }
            BeginGameWithSelectedCharacter();
        }

        private void ShowOnlineLobby()
        {
            startOnlineAfterCharacterConfirm = false;
            PlayerSessionService.GetOrCreate()?.BeginProgressionPlay();
            coopLobbyUI.Show();
        }

        private void BeginGameWithSelectedCharacter()
        {
            menuMusic?.FadeOut(1f);
            // A signed-in account continues its campaign; otherwise offline guest play with the whole town open.
            var session = PlayerSessionService.GetOrCreate();
            if (OnlineAccess.IsRegistered) session?.BeginProgressionPlay();
            else session?.BeginOfflinePlay();
            var story = StoryDirector.AutoStart();
            if (story != null) ScenarioSceneInitializer.QueueLaunch(story.id);
            if (GameServices.TryGet(out SceneFlowController sceneFlow)) sceneFlow.LoadScene(targetGameplayScene);
            else SceneContentLoader.LoadFallback(targetGameplayScene);
        }

        private void OnQuitClicked() => Application.Quit();
    }
}
