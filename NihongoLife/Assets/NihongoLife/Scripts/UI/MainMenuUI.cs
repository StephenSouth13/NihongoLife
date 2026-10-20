using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Main menu. The layout lives in the 01_MainMenu scene (built by MainMenuBaker) and is never moved or restyled at
    /// runtime, so the Editor and Play look the same; this component only translates the texts (Vietnamese, English
    /// or Japanese — the language switch at the top right changes the whole game at once), wires the buttons, adds
    /// hover feedback and opens the popups and the character selection.
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
        [SerializeField] private string targetScenarioId = "scenario.konbini.buy_onigiri";

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
            Wire(friendsButton, () => friendsUI.Show());
            profileUI = gameObject.AddComponent<PlayerProfileUI>();
            profileUI.Initialize(font);
            Wire(profileButton, () => profileUI.ShowOwnProfile());
            coopLobbyUI = gameObject.AddComponent<CoopLobbyUI>();
            coopLobbyUI.Initialize(font);
            Wire(onlineButton, OnOnlineClicked);

            menuMusic = gameObject.GetComponent<MenuMusicController>() ?? gameObject.AddComponent<MenuMusicController>();
            var dayNight = FindFirstObjectByType<DayNightCycle>();
            if (dayNight != null) dayNight.SetHour(9f); // the menu is always a bright morning

            BuildCharacterSelect(font);
            Translate();
            DisplayProfileStats();
            Loc.Watch(this, () => { Translate(); DisplayProfileStats(); });
        }

        private void OnDestroy()
        {
            if (characterPreviewTexture != null) { characterPreviewTexture.Release(); Destroy(characterPreviewTexture); }
            if (characterPreviewStage != null) Destroy(characterPreviewStage);
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
            Label(aboutButton, "Về tác giả", "About", "作者について");
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

        private void DisplayProfileStats()
        {
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

        // ─────────── Character selection ───────────

        private void BuildCharacterSelect(TMP_FontAsset font)
        {
            selectedCharacterIndex = PlayableCharacterCatalog.IndexOf(PlayerPrefs.GetString(PlayableCharacterCatalog.PlayerPrefsKey, PlayableCharacterCatalog.DefaultId));
            if (selectedCharacterIndex < 0) selectedCharacterIndex = 0;

            var dim = UiKit.Image(transform, "CharacterSelectPanel", UiTheme.Backdrop, null, true);
            characterSelectPanel = dim.gameObject;
            UiKit.Fill(dim.rectTransform);
            var card = UiKit.Card(dim.transform, "Card", UiTheme.Ink, 24, 0.6f).rectTransform;
            UiKit.Pin(card, new Vector2(0.5f, 0.5f), 0f, 0f, 1040f, 620f);

            var title = UiKit.Label(card, "Title", "", 30f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 40f, 30f, 700f, 46f);
            var frame = UiKit.Card(card, "PreviewFrame", new Color(0.05f, 0.07f, 0.1f, 1f), 20, 0f, false).rectTransform;
            UiKit.Place(frame, 40f, 96f, 440f, 490f);
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(frame, false);
            UiKit.Fill(preview.rectTransform, 10f, 10f, 10f, 10f);
            characterPreviewImage = preview;
            preview.gameObject.AddComponent<CharacterPreviewDragRotate>().Configure(ApplyCharacterPreviewYaw);
            var hint = UiKit.Label(frame, "DragHint", "Kéo để xoay", "Drag to turn", "ドラッグで回転", 13f, UiTheme.Muted, FontStyles.Italic, TextAlignmentOptions.Center);
            UiKit.Pin(hint.rectTransform, new Vector2(0.5f, 0f), 0f, 16f, 300f, 22f);

            characterNameText = UiKit.Label(card, "CharacterName", "", 36f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(characterNameText.rectTransform, 520f, 120f, 460f, 52f);
            characterTaglineText = UiKit.Label(card, "CharacterTagline", "", 16f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(characterTaglineText.rectTransform, 520f, 178f, 460f, 70f);

            previousCharacterButton = UiKit.IconButton(card, "PreviousCharacterButton", "chevron_left", () => SelectCharacter(selectedCharacterIndex - 1), UiKit.Kind.Secondary, 52f, 28f);
            UiKit.Place((RectTransform)previousCharacterButton.transform, 520f, 262f, 52f, 52f);
            nextCharacterButton = UiKit.IconButton(card, "NextCharacterButton", "chevron_right", () => SelectCharacter(selectedCharacterIndex + 1), UiKit.Kind.Secondary, 52f, 28f);
            UiKit.Place((RectTransform)nextCharacterButton.transform, 584f, 262f, 52f, 52f);

            var nameLabel = UiKit.Label(card, "NameLabel", "Tên của bạn", "Your name", "あなたの名前", 13f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(nameLabel.rectTransform, 520f, 340f, 460f, 20f);
            playerNameInput = NameInput(card, font);
            UiKit.Place((RectTransform)playerNameInput.transform, 520f, 364f, 460f, 52f);
            playerNameInput.text = PlayableCharacterCatalog.GetPlayerName();

            confirmCharacterButton = UiKit.Button(card, "ConfirmCharacterButton", null, null, null, ConfirmCharacterAndStart, UiKit.Kind.Primary, "play_arrow", 19f, 16);
            UiKit.Place((RectTransform)confirmCharacterButton.transform, 520f, 446f, 460f, 62f);
            closeCharacterButton = UiKit.Button(card, "CloseCharacterButton", null, null, null, () => characterSelectPanel.SetActive(false), UiKit.Kind.Ghost, "arrow_back", 16f, 16);
            UiKit.Place((RectTransform)closeCharacterButton.transform, 520f, 520f, 200f, 48f);

            CreateCharacterPreviewStage();
            characterSelectPanel.SetActive(false);
            TranslateCharacterSelect();
            SelectCharacter(selectedCharacterIndex);
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
            characterPreviewTexture = new RenderTexture(768, 900, 24, RenderTextureFormat.ARGB32) { name = "CharacterSelectPreviewTexture", antiAliasing = 4 };
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
            characterPreviewCamera.backgroundColor = new Color(0.05f, 0.07f, 0.1f, 1f);
            characterPreviewCamera.fieldOfView = 30f;
            characterPreviewCamera.nearClipPlane = 0.05f;
            characterPreviewCamera.farClipPlane = 20f;
            characterPreviewCamera.targetTexture = characterPreviewTexture;
            PreviewLight("KeyLight", new Vector3(-2.2f, 4f, 2.4f), Quaternion.Euler(52f, -32f, 0f), 3.2f, new Color(1f, 0.93f, 0.78f, 1f));
            PreviewLight("RimLight", new Vector3(2.8f, 2.8f, -2f), Quaternion.Euler(34f, 136f, 0f), 1.8f, new Color(0.48f, 0.74f, 1f, 1f));
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
            ApplyCharacterPreviewYaw(0f);
            foreach (var collider in characterPreviewModel.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        }

        private void ApplyCharacterPreviewYaw(float yaw)
        {
            if (characterPreviewModel == null) return;
            characterPreviewModel.transform.localRotation = Quaternion.Euler(0f, 180f + Mathf.Clamp(yaw, -90f, 90f), 0f);
        }

        // ─────────── Flow ───────────

        private void HideInfoPopups()
        {
            guidePopup?.Hide();
            aboutPopup?.Hide();
        }

        private void OnStartClicked()
        {
            startOnlineAfterCharacterConfirm = false;
            if (characterSelectPanel == null) { BeginGameWithSelectedCharacter(); return; }
            HideInfoPopups();
            characterSelectPanel.SetActive(true);
            characterSelectPanel.transform.SetAsLastSibling();
            SelectCharacter(selectedCharacterIndex);
            UIStyleKit.PlayShowAnimation(characterSelectPanel.transform.Find("Card").gameObject);
        }

        private void OnOnlineClicked()
        {
            startOnlineAfterCharacterConfirm = true;
            HideInfoPopups();
            if (characterSelectPanel != null)
            {
                characterSelectPanel.SetActive(true);
                characterSelectPanel.transform.SetAsLastSibling();
                SelectCharacter(selectedCharacterIndex);
                UIStyleKit.PlayShowAnimation(characterSelectPanel.transform.Find("Card").gameObject);
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
            coopLobbyUI.Show();
        }

        private void BeginGameWithSelectedCharacter()
        {
            menuMusic?.FadeOut(1f);
            ScenarioSceneInitializer.QueueLaunch(targetScenarioId);
            if (GameServices.TryGet(out SceneFlowController sceneFlow)) sceneFlow.LoadScene(targetGameplayScene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(targetGameplayScene);
        }

        private void OnQuitClicked() => Application.Quit();
    }
}
