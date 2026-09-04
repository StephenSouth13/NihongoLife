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
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
            if (vietnameseButton != null) vietnameseButton.onClick.AddListener(() => SetLanguage(GameLanguage.Vietnamese));
            if (englishButton != null) englishButton.onClick.AddListener(() => SetLanguage(GameLanguage.English));
            if (japaneseButton != null) japaneseButton.onClick.AddListener(() => SetLanguage(GameLanguage.Japanese));

            var settings = GameServices.Get<GameSettingsService>();
            if (settings != null)
            {
                settings.OnLanguageChanged += HandleLanguageChanged;
            }

            RefreshTexts();
            DisplayProfileStats();
        }

        private void OnDestroy()
        {
            var settings = GameServices.Get<GameSettingsService>();
            if (settings != null)
            {
                settings.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void SetLanguage(GameLanguage language)
        {
            var settings = GameServices.Get<GameSettingsService>();
            if (settings != null)
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

            if (titleText != null) titleText.text = language == GameLanguage.Japanese ? "日本語 LIFE" : "NIHONGO LIFE";
            if (subtitleText != null)
            {
                subtitleText.text = language switch
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

            var progressRepo = GameServices.Get<Save.IProgressRepository>();
            if (progressRepo == null)
            {
                profileText.text = "WASD / Shift  |  E interact  |  B bag  |  Tab character";
                return;
            }

            var progress = progressRepo.GetProgress();
            string details =
                $"{Text("Học viên", "Learner", "学習者")}: {progress.displayName}\n" +
                $"{Text("Cấp độ", "Level", "レベル")}: {progress.level} (XP: {progress.xp})\n" +
                $"{Text("Đã hoàn thành", "Completed", "完了")}: {progress.completedScenarios.Count}\n";

            if (progress.bestScores.Count > 0)
            {
                details += "\n" + Text("Điểm cao nhất", "Best scores", "ベストスコア") + ":\n";
                foreach (var score in progress.bestScores)
                {
                    string displayId = score.scenarioId.Replace("scenario.konbini.", Text("Cửa hàng tiện lợi: ", "Convenience store: ", "コンビニ: "));
                    details += $"- {displayId}: {score.bestScore}/100\n";
                }
            }

            details += "\nWASD / Shift  |  E " + Text("tương tác", "interact", "話す") +
                       "  |  B " + Text("balo", "bag", "バッグ") +
                       "  |  Tab " + Text("nhân vật", "character", "キャラ");
            profileText.text = details;
        }

        private void OnStartClicked()
        {
            PlayerPrefs.SetString("ActiveScenarioId", targetScenarioId);
            PlayerPrefs.Save();

            var sceneFlow = GameServices.Get<SceneFlowController>();
            if (sceneFlow != null)
            {
                sceneFlow.LoadScene(targetGameplayScene);
            }
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }

        private string Text(string vi, string en, string ja)
        {
            var settings = GameServices.Get<GameSettingsService>();
            return settings != null ? settings.Text(vi, en, ja) : vi;
        }

        private GameLanguage GetLanguage()
        {
            var settings = GameServices.Get<GameSettingsService>();
            return settings != null ? settings.Language : (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt("NihongoLife.UiLanguage", 0), 0, 2);
        }

        private static void SetButtonSelected(Button button, bool selected)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? new Color(0.95f, 0.72f, 0.25f, 1f) : new Color(0.12f, 0.15f, 0.17f, 1f);
            }
        }
    }
}
