using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Player;
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
            StyleCoreButtons();
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
            EnsureSocialUI();
            EnsureCharacterSelectUI();
            RefreshTexts();
            DisplayProfileStats();
        }

        private void Update()
        {
            if (characterPreviewModel != null && characterSelectPanel != null && characterSelectPanel.activeSelf)
            {
                characterPreviewModel.transform.Rotate(0f, 18f * Time.deltaTime, 0f, Space.World);
            }
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

            if (characterPreviewTexture != null)
            {
                characterPreviewTexture.Release();
                Destroy(characterPreviewTexture);
            }

            if (characterPreviewStage != null)
            {
                Destroy(characterPreviewStage);
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

        private void StyleCoreButtons()
        {
            // Start = primary call-to-action (warm gold accent, matches the story's
            // lantern-lit-town mood). Quit = muted secondary so it never competes for
            // attention.
            UIStyleKit.StyleButton(startButton, UIStyleKit.AccentGold, UIStyleKit.AccentGoldHover, UIStyleKit.AccentGoldPressed);
            UIStyleKit.StyleButton(quitButton, UIStyleKit.PanelBase, UIStyleKit.PanelHover, UIStyleKit.PanelPressed);

            if (startButtonText != null) startButtonText.color = new Color(0.08f, 0.06f, 0.02f, 1f);
        }

        private Button settingsButton;
        private SettingsUI settingsUI;
        private Button loginButton;
        private AuthUI authUI;

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
            StylePanelFor(guideTitleText);

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
            StylePanelFor(creditsTitleText);

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

            // Auth UI
            authUI = gameObject.AddComponent<AuthUI>();
            authUI.Initialize(font);

            // Login/Account Button
            loginButton = CreateLanguageButton("LoginButton", "Login", new Vector2(-420f, 220f), font);
            MoveRect(loginButton, new Vector2(-420f, 220f), new Vector2(140f, 44f));
            UpdateLoginButtonText();
            loginButton.onClick.AddListener(() => authUI.Show());

            if (GameServices.TryGet(out IAuthService auth))
            {
                auth.OnAuthStateChanged += _ =>
                {
                    UpdateLoginButtonText();
                    DisplayProfileStats();
                };
            }
        }

        private void UpdateLoginButtonText()
        {
            if (loginButton == null) return;
            bool authenticated = GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated;
            SetButtonText(loginButton, authenticated
                ? Text("Tài khoản", "Account", "アカウント")
                : Text("Đăng nhập", "Login", "ログイン"));
        }

        // ──────────────────────── Social UI ────────────────────────

        private Button leaderboardButton;
        private Button friendsButton;
        private Button profileButton;
        private Button onlineButton;
        private LeaderboardUI leaderboardUI;
        private FriendsUI friendsUI;
        private PlayerProfileUI profileUI;
        private CoopLobbyUI coopLobbyUI;
        private GameObject characterSelectPanel;
        private RawImage characterPreviewImage;
        private TextMeshProUGUI characterSelectTitleText;
        private TextMeshProUGUI characterNameText;
        private TextMeshProUGUI characterTaglineText;
        private TMP_InputField playerNameInput;
        private Button previousCharacterButton;
        private Button nextCharacterButton;
        private Button confirmCharacterButton;
        private Button closeCharacterButton;
        private RenderTexture characterPreviewTexture;
        private Camera characterPreviewCamera;
        private GameObject characterPreviewStage;
        private GameObject characterPreviewModel;
        private int selectedCharacterIndex;
        private bool startOnlineAfterCharacterConfirm;

        private void EnsureSocialUI()
        {
            TMP_FontAsset font = titleText != null ? titleText.font : null;

            // Leaderboard
            leaderboardUI = gameObject.AddComponent<LeaderboardUI>();
            leaderboardUI.Initialize(font);
            leaderboardButton = CreateLanguageButton("LeaderboardBtn", "Rank", new Vector2(-280f, -230f), font);
            MoveRect(leaderboardButton, new Vector2(-280f, -230f), new Vector2(130f, 46f));
            SetButtonText(leaderboardButton, Text("Xếp hạng", "Ranking", "ランキング"));
            leaderboardButton.onClick.AddListener(() => leaderboardUI.Show());

            // Friends
            friendsUI = gameObject.AddComponent<FriendsUI>();
            friendsUI.Initialize(font);
            friendsButton = CreateLanguageButton("FriendsBtn", "Friends", new Vector2(280f, -230f), font);
            MoveRect(friendsButton, new Vector2(280f, -230f), new Vector2(130f, 46f));
            SetButtonText(friendsButton, Text("Bạn bè", "Friends", "フレンド"));
            friendsButton.onClick.AddListener(() => friendsUI.Show());

            // Profile
            profileUI = gameObject.AddComponent<PlayerProfileUI>();
            profileUI.Initialize(font);
            profileButton = CreateLanguageButton("ProfileBtn", "Profile", new Vector2(0f, -290f), font);
            MoveRect(profileButton, new Vector2(0f, -290f), new Vector2(130f, 46f));
            SetButtonText(profileButton, Text("Hồ sơ", "Profile", "プロフィール"));
            profileButton.onClick.AddListener(() => profileUI.ShowOwnProfile());

            coopLobbyUI = gameObject.AddComponent<CoopLobbyUI>();
            coopLobbyUI.Initialize(font);
            onlineButton = CreateLanguageButton("OnlineBtn", "Online", new Vector2(0f, -350f), font);
            MoveRect(onlineButton, new Vector2(0f, -350f), new Vector2(150f, 46f));
            SetButtonText(onlineButton, Text("Online", "Online", "オンライン"));
            onlineButton.onClick.AddListener(OnOnlineClicked);
        }

        private void EnsureCharacterSelectUI()
        {
            TMP_FontAsset font = titleText != null ? titleText.font : null;
            selectedCharacterIndex = PlayableCharacterCatalog.IndexOf(PlayerPrefs.GetString(PlayableCharacterCatalog.PlayerPrefsKey, PlayableCharacterCatalog.DefaultId));
            if (selectedCharacterIndex < 0) selectedCharacterIndex = 0;

            characterSelectPanel = new GameObject("CharacterSelectPanel");
            characterSelectPanel.transform.SetParent(transform, false);
            var panelRect = characterSelectPanel.AddComponent<RectTransform>();
            MoveRect(panelRect, new Vector2(0f, 0f), new Vector2(930f, 560f));
            characterSelectPanel.AddComponent<Image>().color = new Color(0.025f, 0.032f, 0.036f, 0.94f);
            UIStyleKit.StylePanel(panelRect, new Color(0.025f, 0.032f, 0.036f, 0.94f));

            characterSelectTitleText = CreateMenuText("Title", new Vector2(0f, 226f), new Vector2(760f, 48f), 30f, font);
            characterSelectTitleText.transform.SetParent(characterSelectPanel.transform, false);
            characterSelectTitleText.text = Text("Chọn nhân vật", "Choose your character", "キャラクター選択");
            characterSelectTitleText.fontStyle = FontStyles.Bold;
            characterSelectTitleText.color = new Color(1f, 0.91f, 0.58f, 1f);

            characterNameText = CreateMenuText("CharacterName", new Vector2(250f, 96f), new Vector2(300f, 44f), 28f, font);
            characterNameText.transform.SetParent(characterSelectPanel.transform, false);
            characterNameText.fontStyle = FontStyles.Bold;

            characterTaglineText = CreateMenuText("CharacterTagline", new Vector2(250f, 46f), new Vector2(360f, 42f), 16f, font);
            characterTaglineText.transform.SetParent(characterSelectPanel.transform, false);
            characterTaglineText.color = new Color(0.9f, 0.95f, 1f, 1f);

            playerNameInput = CreateInputField("PlayerNameInput", new Vector2(250f, -28f), new Vector2(300f, 44f), font);
            playerNameInput.transform.SetParent(characterSelectPanel.transform, false);
            MoveRect(playerNameInput.GetComponent<RectTransform>(), new Vector2(250f, -28f), new Vector2(300f, 44f));
            playerNameInput.text = PlayableCharacterCatalog.GetPlayerName();

            var previewFrame = new GameObject("PreviewFrame");
            previewFrame.transform.SetParent(characterSelectPanel.transform, false);
            var frameRect = previewFrame.AddComponent<RectTransform>();
            MoveRect(frameRect, new Vector2(-170f, -12f), new Vector2(390f, 430f));
            previewFrame.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.11f, 1f);

            var previewGo = new GameObject("Preview");
            previewGo.transform.SetParent(previewFrame.transform, false);
            var previewRect = previewGo.AddComponent<RectTransform>();
            MoveRect(previewRect, Vector2.zero, new Vector2(370f, 410f));
            characterPreviewImage = previewGo.AddComponent<RawImage>();

            previousCharacterButton = CreateLanguageButton("PreviousCharacterButton", "<", new Vector2(52f, -34f), font);
            previousCharacterButton.transform.SetParent(characterSelectPanel.transform, false);
            MoveRect(previousCharacterButton, new Vector2(52f, -34f), new Vector2(58f, 58f));

            nextCharacterButton = CreateLanguageButton("NextCharacterButton", ">", new Vector2(448f, -34f), font);
            nextCharacterButton.transform.SetParent(characterSelectPanel.transform, false);
            MoveRect(nextCharacterButton, new Vector2(448f, -34f), new Vector2(58f, 58f));

            confirmCharacterButton = CreateLanguageButton("ConfirmCharacterButton", "Play", new Vector2(250f, -120f), font);
            confirmCharacterButton.transform.SetParent(characterSelectPanel.transform, false);
            MoveRect(confirmCharacterButton, new Vector2(250f, -120f), new Vector2(260f, 58f));
            UIStyleKit.StyleButton(confirmCharacterButton, UIStyleKit.AccentGold, UIStyleKit.AccentGoldHover, UIStyleKit.AccentGoldPressed);

            closeCharacterButton = CreateLanguageButton("CloseCharacterButton", "Back", new Vector2(250f, -190f), font);
            closeCharacterButton.transform.SetParent(characterSelectPanel.transform, false);
            MoveRect(closeCharacterButton, new Vector2(250f, -190f), new Vector2(180f, 46f));

            previousCharacterButton.onClick.AddListener(() => SelectCharacter(selectedCharacterIndex - 1));
            nextCharacterButton.onClick.AddListener(() => SelectCharacter(selectedCharacterIndex + 1));
            confirmCharacterButton.onClick.AddListener(ConfirmCharacterAndStart);
            closeCharacterButton.onClick.AddListener(() => characterSelectPanel.SetActive(false));

            CreateCharacterPreviewStage();
            characterSelectPanel.SetActive(false);
            SelectCharacter(selectedCharacterIndex);
        }

        private void CreateCharacterPreviewStage()
        {
            characterPreviewTexture = new RenderTexture(768, 900, 24, RenderTextureFormat.ARGB32)
            {
                name = "CharacterSelectPreviewTexture",
                antiAliasing = 4
            };
            characterPreviewTexture.Create();
            if (characterPreviewImage != null) characterPreviewImage.texture = characterPreviewTexture;

            characterPreviewStage = new GameObject("CharacterSelectPreviewStage");
            characterPreviewStage.transform.position = new Vector3(0f, -500f, 0f);
            DontDestroyOnLoad(characterPreviewStage);

            var cameraGo = new GameObject("PreviewCamera");
            cameraGo.transform.SetParent(characterPreviewStage.transform, false);
            cameraGo.transform.localPosition = new Vector3(0f, 1.35f, 4.2f);
            cameraGo.transform.localRotation = Quaternion.Euler(5f, 180f, 0f);
            characterPreviewCamera = cameraGo.AddComponent<Camera>();
            characterPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            characterPreviewCamera.backgroundColor = new Color(0.02f, 0.026f, 0.03f, 1f);
            characterPreviewCamera.fieldOfView = 30f;
            characterPreviewCamera.nearClipPlane = 0.05f;
            characterPreviewCamera.farClipPlane = 20f;
            characterPreviewCamera.targetTexture = characterPreviewTexture;

            CreatePreviewLight("KeyLight", new Vector3(-2.2f, 4f, 2.4f), Quaternion.Euler(52f, -32f, 0f), 3.2f, new Color(1f, 0.93f, 0.78f, 1f));
            CreatePreviewLight("RimLight", new Vector3(2.8f, 2.8f, -2f), Quaternion.Euler(34f, 136f, 0f), 1.8f, new Color(0.48f, 0.74f, 1f, 1f));
        }

        private void CreatePreviewLight(string name, Vector3 position, Quaternion rotation, float intensity, Color color)
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
            if (characterNameText != null)
            {
                characterNameText.text = character.DisplayName;
                characterNameText.color = character.AccentColor;
            }
            if (characterTaglineText != null) characterTaglineText.text = character.Tagline;
            SetButtonText(confirmCharacterButton, Text("Chọn nhân vật này", "Play as this character", "このキャラで始める"));

            if (characterPreviewModel != null)
            {
                Destroy(characterPreviewModel);
            }

            var prefab = PlayableCharacterCatalog.LoadPrefab(character);
            if (prefab == null || characterPreviewStage == null) return;

            characterPreviewModel = Instantiate(prefab, characterPreviewStage.transform);
            characterPreviewModel.name = "SelectedCharacterPreview";
            characterPreviewModel.transform.localPosition = Vector3.zero;
            characterPreviewModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            characterPreviewModel.transform.localScale = Vector3.one;

            foreach (var collider in characterPreviewModel.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }
        }

        private TMP_InputField CreateInputField(string name, Vector2 position, Vector2 size, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            MoveRect(rect, position, size);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.07f, 0.085f, 0.095f, 0.96f);

            var input = go.AddComponent<TMP_InputField>();
            input.characterLimit = 24;
            input.lineType = TMP_InputField.LineType.SingleLine;

            var text = CreateMenuText("Text", Vector2.zero, new Vector2(size.x - 28f, size.y - 10f), 17f, font);
            text.transform.SetParent(go.transform, false);
            MoveRect(text.rectTransform, Vector2.zero, new Vector2(size.x - 28f, size.y - 10f));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = Color.white;
            input.textComponent = text;

            var placeholder = CreateMenuText("Placeholder", Vector2.zero, new Vector2(size.x - 28f, size.y - 10f), 15f, font);
            placeholder.transform.SetParent(go.transform, false);
            MoveRect(placeholder.rectTransform, Vector2.zero, new Vector2(size.x - 28f, size.y - 10f));
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.color = new Color(0.72f, 0.78f, 0.84f, 0.75f);
            placeholder.text = Text("Tên nhân vật", "Character name", "名前");
            input.placeholder = placeholder;

            return input;
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

            go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            UIStyleKit.StyleButton(button, UIStyleKit.PanelBase, UIStyleKit.PanelHover, UIStyleKit.PanelPressed);

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
            SetButtonText(onlineButton, Text("Online", "Online", "オンライン"));
            SetButtonText(closeCharacterButton, Text("Quay lại", "Back", "戻る"));
            SetButtonText(confirmCharacterButton, Text("Chọn nhân vật này", "Play as this character", "このキャラで始める"));
            if (characterSelectTitleText != null) characterSelectTitleText.text = Text("Chọn nhân vật", "Choose your character", "キャラクター選択");

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
                string name = progress.displayName;
                if (GameServices.TryGet(out IAuthService authSvc) && authSvc.IsAuthenticated && !string.IsNullOrWhiteSpace(authSvc.DisplayName))
                {
                    name = authSvc.DisplayName;
                }
                details =
                    $"{Text("Học viên", "Learner", "学習者")}: {name}\n" +
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
            startOnlineAfterCharacterConfirm = false;
            if (characterSelectPanel == null)
            {
                BeginGameWithSelectedCharacter();
                return;
            }

            HideInfoPanels();
            characterSelectPanel.SetActive(true);
            SelectCharacter(selectedCharacterIndex);
            UIStyleKit.PlayShowAnimation(characterSelectPanel);
        }

        private void OnOnlineClicked()
        {
            startOnlineAfterCharacterConfirm = true;
            HideInfoPanels();
            if (characterSelectPanel != null)
            {
                characterSelectPanel.SetActive(true);
                SelectCharacter(selectedCharacterIndex);
                UIStyleKit.PlayShowAnimation(characterSelectPanel);
                return;
            }

            ShowOnlineLobby();
        }

        private void ConfirmCharacterAndStart()
        {
            var character = PlayableCharacterCatalog.Get(selectedCharacterIndex);
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
            if (coopLobbyUI == null)
            {
                TMP_FontAsset font = titleText != null ? titleText.font : null;
                coopLobbyUI = gameObject.AddComponent<CoopLobbyUI>();
                coopLobbyUI.Initialize(font);
            }

            coopLobbyUI.Show();
        }

        private void BeginGameWithSelectedCharacter()
        {
            ScenarioSceneInitializer.QueueLaunch(targetScenarioId);

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

            Color baseColor = selected ? UIStyleKit.AccentGold : UIStyleKit.PanelBase;
            var swap = button.GetComponent<ButtonColorSwap>();
            if (swap != null)
            {
                swap.SetBaseColor(baseColor);
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = baseColor;
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

        private static void StylePanelFor(TextMeshProUGUI title)
        {
            Transform panel = title != null ? title.transform.parent : null;
            if (panel == null) return;
            UIStyleKit.StylePanel(panel.GetComponent<RectTransform>(), UIStyleKit.PanelBase);
        }

        private void ToggleInfoPanel(TextMeshProUGUI title)
        {
            Transform panel = title != null ? title.transform.parent : null;
            if (panel == null) return;

            bool shouldShow = !panel.gameObject.activeSelf;
            HideInfoPanels();
            panel.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                UIStyleKit.PlayShowAnimation(panel.gameObject);
            }
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
