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

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI profileText;
        [SerializeField] private TextMeshProUGUI startButtonText;
        [SerializeField] private TextMeshProUGUI quitButtonText;
        [SerializeField] private TextMeshProUGUI languageLabelText;

        [Header("Settings")]
        [SerializeField] private string targetGameplayScene = "90_TestSandbox";
        [SerializeField] private string targetScenarioId = "scenario.konbini.buy_onigiri";

        private void Start()
        {
            EnsureSettingsService();
            AutoBindExistingMenu();
            CleanExistingLayout();
            EnsureLanguageSelector();

            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
            if (vietnameseButton != null) vietnameseButton.onClick.AddListener(() => SetLanguage(GameLanguage.Vietnamese));
            if (englishButton != null) englishButton.onClick.AddListener(() => SetLanguage(GameLanguage.English));
            if (japaneseButton != null) japaneseButton.onClick.AddListener(() => SetLanguage(GameLanguage.Japanese));

            if (GameServices.TryGet(out GameSettingsService settings))
            {
                settings.OnLanguageChanged += HandleLanguageChanged;
            }

            RefreshTexts();
            DisplayProfileStats();
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

            SetButtonSelected(vietnameseButton, language == GameLanguage.Vietnamese);
            SetButtonSelected(englishButton, language == GameLanguage.English);
            SetButtonSelected(japaneseButton, language == GameLanguage.Japanese);
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
    }
}
