using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
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
        [SerializeField] private TextMeshProUGUI guideTitleText;
        [SerializeField] private TextMeshProUGUI guideBodyText;
        [SerializeField] private TextMeshProUGUI creditsTitleText;
        [SerializeField] private TextMeshProUGUI creditsBodyText;

        [Header("Settings")]
        [SerializeField] private string targetGameplayScene = "90_TestSandbox";
        [SerializeField] private string targetScenarioId = "scenario.konbini.buy_onigiri";
        [SerializeField] private string authorName = "quachthanhlong.com";
        [SerializeField] private string projectRole = "Creator of Nihongo Life";

        private void Start()
        {
            EnsureSettingsService();
            authorName = "quachthanhlong.com";
            projectRole = "Creator of Nihongo Life";
            AutoBindExistingMenu();
            ImproveMenuPresentation();
            CleanExistingLayout();
            EnsureLanguageSelector();
            EnsureMenuInfoPanels();

            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
            if (vietnameseButton != null) vietnameseButton.onClick.AddListener(() => SetLanguage(GameLanguage.Vietnamese));
            if (englishButton != null) englishButton.onClick.AddListener(() => SetLanguage(GameLanguage.English));
            if (japaneseButton != null) japaneseButton.onClick.AddListener(() => SetLanguage(GameLanguage.Japanese));
            if (guideButton != null) guideButton.onClick.AddListener(() => ToggleInfoPanel(guideTitleText));
            if (aboutButton != null) aboutButton.onClick.AddListener(() => ToggleInfoPanel(creditsTitleText));

            if (GameServices.TryGet(out GameSettingsService settings))
            {
                settings.OnLanguageChanged += HandleLanguageChanged;
            }

            EnsureSettingsUI();
            RefreshTexts();
            DisplayProfileStats();
        }

        private void ImproveMenuPresentation()
        {
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0.03f, 0.035f, 0.04f, 0.08f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.78f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.86f, 1f);
            RenderSettings.fogDensity = 0.0045f;

            var dayNight = FindFirstObjectByType<DayNightCycle>();
            if (dayNight != null)
            {
                dayNight.SetHour(9f);
            }

            var sun = RenderSettings.sun != null ? RenderSettings.sun : FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.intensity = 1.45f;
                sun.color = new Color(1f, 0.95f, 0.84f, 1f);
                sun.transform.rotation = Quaternion.Euler(42f, -34f, 0f);
                RenderSettings.sun = sun;
            }

            var orbit = Camera.main != null ? Camera.main.GetComponent<MenuCameraOrbit>() : null;
            if (orbit != null)
            {
                orbit.Configure(44f, 15.5f, 2.15f, 205f, 3.1f);
            }
        }

        private void OnDestroy()
        {
            if (GameServices.TryGet(out GameSettingsService settings))
            {
                settings.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void EnsureSettingsService()
        {
            if (GameServices.TryGet(out GameSettingsService _)) return;

            var existing = FindFirstObjectByType<GameSettingsService>();
            if (existing == null)
            {
                var go = new GameObject("GameSettingsService");
                existing = go.AddComponent<GameSettingsService>();
                DontDestroyOnLoad(go);
            }

            existing.Initialize();
            GameServices.Register<GameSettingsService>(existing);
        }

        private void AutoBindExistingMenu()
        {
            if (startButton == null) startButton = transform.Find("StartButton")?.GetComponent<Button>();
            if (quitButton == null) quitButton = transform.Find("QuitButton")?.GetComponent<Button>();
            if (titleText == null) titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            if (subtitleText == null) subtitleText = transform.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
            if (profileText == null) profileText = transform.Find("ProfileText")?.GetComponent<TextMeshProUGUI>();
            if (guideTitleText == null) guideTitleText = transform.Find("GuidePanel/Title")?.GetComponent<TextMeshProUGUI>();
            if (guideBodyText == null) guideBodyText = transform.Find("GuidePanel/Body")?.GetComponent<TextMeshProUGUI>();
            if (creditsTitleText == null) creditsTitleText = transform.Find("CreditsPanel/Title")?.GetComponent<TextMeshProUGUI>();
            if (creditsBodyText == null) creditsBodyText = transform.Find("CreditsPanel/Body")?.GetComponent<TextMeshProUGUI>();
            if (guideButton == null) guideButton = transform.Find("GuideButton")?.GetComponent<Button>();
            if (aboutButton == null) aboutButton = transform.Find("AboutButton")?.GetComponent<Button>();
            if (startButtonText == null && startButton != null) startButtonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
            if (quitButtonText == null && quitButton != null) quitButtonText = quitButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void CleanExistingLayout()
        {
            MoveRect(titleText, new Vector2(0f, 220f), new Vector2(1000f, 92f));
            MoveRect(subtitleText, new Vector2(0f, 148f), new Vector2(980f, 42f));
            MoveRect(startButton, new Vector2(0f, 48f), new Vector2(320f, 62f));
            MoveRect(quitButton, new Vector2(0f, -30f), new Vector2(320f, 56f));

            if (profileText != null)
            {
                profileText.enableAutoSizing = true;
                profileText.fontSizeMin = 12f;
                profileText.fontSizeMax = 17f;
                profileText.textWrappingMode = TextWrappingModes.Normal;
                MoveRect(profileText, new Vector2(0f, -292f), new Vector2(900f, 132f));
            }
        }

        private Button settingsButton;
        private SettingsUI settingsUI;

        private void EnsureMenuInfoPanels()
        {
            TMP_FontAsset font = titleText != null ? titleText.font : null;

            if (guideTitleText == null || guideBodyText == null)
            {
                CreateInfoPanel(
                    "GuidePanel",
                    new Vector2(-500f, -72f),
                    new Vector2(405f, 330f),
                    font,
                    out guideTitleText,
                    out guideBodyText);
            }

            ConfigureInfoPanel(guideTitleText, guideBodyText, new Vector2(0f, -28f), new Vector2(520f, 300f));

            if (creditsTitleText == null || creditsBodyText == null)
            {
                CreateInfoPanel(
                    "CreditsPanel",
                    new Vector2(500f, -72f),
                    new Vector2(405f, 330f),
                    font,
                    out creditsTitleText,
                    out creditsBodyText);
            }

            ConfigureInfoPanel(creditsTitleText, creditsBodyText, new Vector2(0f, -28f), new Vector2(520f, 300f));

            guideButton ??= CreateLanguageButton("GuideButton", "How", new Vector2(-112f, -230f), font);
            aboutButton ??= CreateLanguageButton("AboutButton", "About", new Vector2(112f, -230f), font);
            MoveRect(guideButton, new Vector2(-112f, -230f), new Vector2(170f, 46f));
            MoveRect(aboutButton, new Vector2(112f, -230f), new Vector2(170f, 46f));
            HideInfoPanels();
        }

        private void EnsureLanguageSelector()
        {
            TMP_FontAsset font = titleText != null ? titleText.font : null;
            if (languageLabelText == null)
            {
                languageLabelText = CreateMenuText("LanguageLabel", new Vector2(0f, -118f), new Vector2(560f, 32f), 19f, font);
            }

            languageLabelText.color = new Color(1f, 0.91f, 0.54f);
            MoveRect(languageLabelText, new Vector2(0f, -118f), new Vector2(560f, 32f));

            vietnameseButton ??= CreateLanguageButton("LanguageVietnameseButton", "VI", new Vector2(-132f, -166f), font);
            englishButton ??= CreateLanguageButton("LanguageEnglishButton", "EN", new Vector2(0f, -166f), font);
            japaneseButton ??= CreateLanguageButton("LanguageJapaneseButton", "JP", new Vector2(132f, -166f), font);
        }

        private void EnsureSettingsUI()
        {
            TMP_FontAsset font = titleText != null ? titleText.font : null;

            // Settings UI script attached to MainMenuPanel
            settingsUI = gameObject.AddComponent<SettingsUI>();
            settingsUI.Initialize(font);

            // Settings Button
            settingsButton = CreateLanguageButton("SettingsButton", "Cài đặt", new Vector2(400f, 200f), font);
            MoveRect(settingsButton, new Vector2(420f, 220f), new Vector2(140f, 44f));
            var txt = settingsButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            txt.text = "Cài đặt";

            SetButtonText(settingsButton, Text("Cài đặt", "Settings", "設定"));
            settingsButton.onClick.AddListener(() => settingsUI.Show());
        }

        private TextMeshProUGUI CreateMenuText(string name, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }

        private void CreateInfoPanel(string name, Vector2 position, Vector2 size, TMP_FontAsset font, out TextMeshProUGUI title, out TextMeshProUGUI body)
        {
            var panelGo = new GameObject(name);
            panelGo.transform.SetParent(transform, false);

            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = panelGo.AddComponent<Image>();
            image.color = new Color(0.045f, 0.055f, 0.06f, 0.82f);

            title = CreateMenuText("Title", new Vector2(0f, 116f), new Vector2(size.x - 34f, 34f), 21f, font);
            title.transform.SetParent(panelGo.transform, false);
            title.alignment = TextAlignmentOptions.Left;
            title.color = new Color(1f, 0.91f, 0.54f, 1f);
            title.fontStyle = FontStyles.Bold;

            body = CreateMenuText("Body", new Vector2(0f, -26f), new Vector2(size.x - 34f, 226f), 15.5f, font);
            body.transform.SetParent(panelGo.transform, false);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.color = new Color(0.92f, 0.96f, 1f, 1f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 7f;
        }

        private static void ConfigureInfoPanel(TextMeshProUGUI title, TextMeshProUGUI body, Vector2 position, Vector2 size)
        {
            Transform panel = title != null ? title.transform.parent : body != null ? body.transform.parent : null;
            if (panel != null)
            {
                var rect = panel.GetComponent<RectTransform>();
                MoveRect(rect, position, size);

                var image = panel.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.035f, 0.045f, 0.05f, 0.9f);
                }
            }

            if (title != null)
            {
                MoveRect(title, new Vector2(0f, 128f), new Vector2(size.x - 44f, 38f));
                title.enableAutoSizing = true;
                title.fontSizeMin = 15f;
                title.fontSizeMax = 22f;
                title.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (body != null)
            {
                MoveRect(body, new Vector2(0f, -24f), new Vector2(size.x - 44f, 248f));
                body.enableAutoSizing = true;
                body.fontSizeMin = 10.5f;
                body.fontSizeMax = 14.5f;
                body.textWrappingMode = TextWrappingModes.Normal;
                body.overflowMode = TextOverflowModes.Ellipsis;
                body.lineSpacing = 4f;
            }
        }

        private Button CreateLanguageButton(string name, string label, Vector2 position, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(92f, 44f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.15f, 0.17f, 0.94f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.2f, 0.24f, 0.26f, 1f);
            colors.pressedColor = new Color(0.08f, 0.1f, 0.11f, 1f);
            button.colors = colors;

            // Add hover animation
            go.AddComponent<UIHoverScale>();

            var text = CreateMenuText("Text", Vector2.zero, new Vector2(74f, 32f), 18f, font);
            text.transform.SetParent(go.transform, false);
            text.text = label;
            return button;
        }

        private void SetLanguage(GameLanguage language)
        {
            if (GameServices.TryGet(out GameSettingsService settings))
            {
                settings.SetLanguage(language);
            }
            else
            {
                PlayerPrefs.SetInt("NihongoLife.UiLanguage", (int)language);
                PlayerPrefs.Save();
            }

            RefreshTexts();
            DisplayProfileStats();
        }

        private void HandleLanguageChanged(GameLanguage language)
        {
            RefreshTexts();
            DisplayProfileStats();
        }

        private void RefreshTexts()
        {
            GameLanguage language = GetLanguage();

            GameControlDatabase database = GameServices.TryGet(out GameControlService controlService) ? controlService.Database : null;

            if (titleText != null)
            {
                titleText.text = database != null && !string.IsNullOrEmpty(database.menuTitle)
                    ? database.menuTitle
                    : language == GameLanguage.Japanese ? "日本語 LIFE" : "NIHONGO LIFE";
            }
            if (subtitleText != null)
            {
                subtitleText.text = database != null
                    ? language switch
                    {
                        GameLanguage.English => database.menuSubtitleEn,
                        GameLanguage.Japanese => database.menuSubtitleJa,
                        _ => database.menuSubtitleVi
                    }
                    : language switch
                {
                    GameLanguage.English => "A polished Japanese town for real shopping practice",
                    GameLanguage.Japanese => "コンビニで買い物を練習する街",
                    _ => "Khu phố học tiếng Nhật qua mua hàng thật"
                };
            }

            if (startButtonText != null) startButtonText.text = Text("Bắt đầu", "Start", "始める");
            if (quitButtonText != null) quitButtonText.text = Text("Thoát", "Quit", "終了");
            if (languageLabelText != null) languageLabelText.text = Text("Ngôn ngữ giao diện", "Interface language", "表示言語");
            SetButtonText(guideButton, Text("Cách chơi", "How to play", "遊び方"));
            SetButtonText(aboutButton, Text("Về tôi", "About me", "作者"));
            SetButtonText(settingsButton, Text("Cài đặt", "Settings", "設定"));

            if (guideTitleText != null) guideTitleText.text = Text("Cách chơi", "How to play", "遊び方");
            if (guideBodyText != null) guideBodyText.text = BuildGuideText(language);
            if (creditsTitleText != null) creditsTitleText.text = Text("Về tôi", "About me", "作者");
            if (creditsBodyText != null) creditsBodyText.text = BuildCreditsText(language);

            SetButtonSelected(vietnameseButton, language == GameLanguage.Vietnamese);
            SetButtonSelected(englishButton, language == GameLanguage.English);
            SetButtonSelected(japaneseButton, language == GameLanguage.Japanese);
        }

        private string BuildGuideText(GameLanguage language)
        {
            return language switch
            {
                GameLanguage.English =>
                    "1. Walk through town and find the active NPC, shop, or mission marker.\n" +
                    "2. Press E to talk, choose short N5 Japanese replies, and follow the objective.\n" +
                    "3. Use B for your bag, Tab for character status, and V to practice speaking.\n" +
                    "4. Listen first, read hints when needed, finish the scene, and gain XP.",
                GameLanguage.Japanese =>
                    "1. 町を歩いて、NPC・店・ミッションを探します。\n" +
                    "2. Eで話して、N5レベルの短い返事を選びます。\n" +
                    "3. Bでバッグ、Tabでステータス、Vで発音練習。\n" +
                    "4. まず聞いて、必要ならヒントを読み、XPを獲得します。",
                _ =>
                    "1. Đi quanh khu phố để tìm NPC, cửa hàng hoặc điểm nhiệm vụ.\n" +
                    "2. Nhấn E để trò chuyện, chọn câu đáp tiếng Nhật N5 phù hợp.\n" +
                    "3. Dùng B mở balo, Tab xem nhân vật, V luyện phát âm.\n" +
                    "4. Nghe trước, đọc gợi ý khi cần, hoàn thành tình huống để nhận XP."
            };
        }

        private string BuildCreditsText(GameLanguage language)
        {
            return language switch
            {
                GameLanguage.English =>
                    $"Author: {authorName}\n" +
                    "Website: quachthanhlong.com\n" +
                    $"Role: {projectRole}\n\n" +
                    "Japanese learning simulation\n" +
                    "Scenario design, dialogue, scoring, inventory, AI NPC replies\n" +
                    "Prepared for future online learner communication.",
                GameLanguage.Japanese =>
                    $"作者: {authorName}\n" +
                    "Website: quachthanhlong.com\n" +
                    $"役割: {projectRole}\n\n" +
                    "シナリオ型日本語学習シミュレーション\n" +
                    "会話、スコア、バッグ、AI NPC\n" +
                    "将来のオンライン交流に対応する設計。",
                _ =>
                    $"Tác giả: {authorName}\n" +
                    "Website: quachthanhlong.com\n" +
                    $"Vai trò: {projectRole}\n\n" +
                    "Mô phỏng học tiếng Nhật theo tình huống\n" +
                    "Hội thoại, điểm số, balo, NPC AI\n" +
                    "Có nền tảng để phát triển giao tiếp online giữa người chơi."
            };
        }

        private void DisplayProfileStats()
        {
            if (profileText == null) return;

            string details = string.Empty;
            if (GameServices.TryGet(out Save.IProgressRepository progressRepo))
            {
                var progress = progressRepo.GetProgress();
                details =
                    $"{Text("Học viên", "Learner", "学習者")}: {progress.displayName}\n" +
                    $"{Text("Cấp độ", "Level", "レベル")}: {progress.level} (XP: {progress.xp})\n" +
                    $"{Text("Đã hoàn thành", "Completed", "完了")}: {progress.completedScenarios.Count}\n";
            }

            details += "\nWASD / Shift  |  E " + Text("tương tác", "interact", "話す") +
                       "  |  B " + Text("balo", "bag", "バッグ") +
                       "  |  Tab " + Text("nhân vật", "character", "キャラ");
            profileText.text = details.Trim();
        }

        private void OnStartClicked()
        {
            PlayerPrefs.SetString("ActiveScenarioId", targetScenarioId);
            if (GameServices.TryGet(out GameControlService controlService))
            {
                PlayerPrefs.SetString("ActiveScenarioId", controlService.ActiveScenarioIdOrDefault(targetScenarioId));
            }
            PlayerPrefs.Save();

            if (GameServices.TryGet(out SceneFlowController sceneFlow))
            {
                sceneFlow.LoadScene(targetGameplayScene);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetGameplayScene);
            }
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }

        private string Text(string vi, string en, string ja)
        {
            return GameServices.TryGet(out GameSettingsService settings) ? settings.Text(vi, en, ja) : vi;
        }

        private GameLanguage GetLanguage()
        {
            return GameServices.TryGet(out GameSettingsService settings)
                ? settings.Language
                : (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt("NihongoLife.UiLanguage", 0), 0, 2);
        }

        private static void MoveRect(TextMeshProUGUI text, Vector2 position, Vector2 size)
        {
            if (text == null) return;
            MoveRect(text.rectTransform, position, size);
        }

        private static void MoveRect(Button button, Vector2 position, Vector2 size)
        {
            if (button == null) return;
            MoveRect(button.GetComponent<RectTransform>(), position, size);
        }

        private static void MoveRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetButtonSelected(Button button, bool selected)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? new Color(0.95f, 0.72f, 0.25f, 1f) : new Color(0.12f, 0.15f, 0.17f, 0.94f);
            }
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) return;

            label.text = text;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 18f;
            label.overflowMode = TextOverflowModes.Ellipsis;

            var labelRect = label.GetComponent<RectTransform>();
            var buttonRect = button.GetComponent<RectTransform>();
            if (labelRect != null && buttonRect != null)
            {
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = new Vector2(Mathf.Max(48f, buttonRect.sizeDelta.x - 24f), buttonRect.sizeDelta.y - 10f);
            }
        }

        private void ToggleInfoPanel(TextMeshProUGUI title)
        {
            Transform panel = title != null ? title.transform.parent : null;
            if (panel == null) return;

            bool shouldShow = !panel.gameObject.activeSelf;
            HideInfoPanels();
            panel.gameObject.SetActive(shouldShow);
        }

        private void HideInfoPanels()
        {
            SetInfoPanelVisible(guideTitleText, false);
            SetInfoPanelVisible(creditsTitleText, false);
        }

        private static void SetInfoPanelVisible(TextMeshProUGUI title, bool visible)
        {
            Transform panel = title != null ? title.transform.parent : null;
            if (panel != null) panel.gameObject.SetActive(visible);
        }
    }
}
