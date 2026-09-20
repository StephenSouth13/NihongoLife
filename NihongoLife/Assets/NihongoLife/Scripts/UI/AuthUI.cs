using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>
    /// Authentication UI panel — login, register, and guest play.
    /// Built entirely in code, following the same pattern as MainMenuUI.
    /// </summary>
    public class AuthUI : MonoBehaviour
    {
        private GameObject _panel;
        private TMP_InputField _emailInput;
        private TMP_InputField _passwordInput;
        private TMP_InputField _displayNameInput;
        private Button _signInButton;
        private Button _signUpButton;
        private Button _guestButton;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _userInfoText;
        private Button _logoutButton;
        private TMP_FontAsset _font;
        private bool _isRegistering = false;

        // ──────────────────────── Public API ────────────────────────

        public void Initialize(TMP_FontAsset font)
        {
            PlayerSessionService.GetOrCreate();
            _font = font;
            BuildUI();
            UpdateState();

            if (GameServices.TryGet(out IAuthService auth))
            {
                auth.OnAuthStateChanged += _ => UpdateState();
            }
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            UpdateState();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        // ──────────────────────── Build UI ────────────────────────

        private void BuildUI()
        {
            // Root panel
            _panel = new GameObject("AuthPanel");
            _panel.transform.SetParent(transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Dimmed background
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.015f, 0.02f, 0.92f);
            bg.raycastTarget = true;

            // Center card
            var card = CreateChild(_panel, "AuthCard");
            var cardRect = card.AddComponent<RectTransform>();
            CenterRect(cardRect, new Vector2(440f, 520f));
            var cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.04f, 0.05f, 0.06f, 0.97f);

            // Title
            _titleText = CreateText(card, "Title", new Vector2(0f, 210f), new Vector2(380f, 42f), 26f);
            _titleText.text = "NIHONGO LIFE";
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = new Color(1f, 0.91f, 0.54f, 1f);

            // Email input
            _emailInput = CreateInputField(card, "EmailInput", new Vector2(0f, 130f), "Email");

            // Password input
            _passwordInput = CreateInputField(card, "PasswordInput", new Vector2(0f, 60f), "Password");
            _passwordInput.contentType = TMP_InputField.ContentType.Password;

            // Display name input (for registration)
            _displayNameInput = CreateInputField(card, "NameInput", new Vector2(0f, -10f), "Display Name");

            // Sign In button
            _signInButton = CreateButton(card, "SignInButton", new Vector2(0f, -80f), new Vector2(340f, 48f), "Sign In", new Color(0.18f, 0.56f, 0.34f, 1f));
            _signInButton.onClick.AddListener(OnSignInClicked);

            // Sign Up button
            _signUpButton = CreateButton(card, "SignUpButton", new Vector2(0f, -140f), new Vector2(340f, 48f), "Create Account", new Color(0.22f, 0.45f, 0.72f, 1f));
            _signUpButton.onClick.AddListener(OnSignUpClicked);

            // Guest button
            _guestButton = CreateButton(card, "GuestButton", new Vector2(0f, -200f), new Vector2(340f, 42f), "Play as Guest", new Color(0.15f, 0.17f, 0.2f, 1f));
            _guestButton.onClick.AddListener(OnGuestClicked);

            // Status text
            _statusText = CreateText(card, "StatusText", new Vector2(0f, -246f), new Vector2(380f, 30f), 13f);
            _statusText.color = new Color(0.98f, 0.55f, 0.55f, 1f);
            _statusText.text = string.Empty;

            // User info (shown when logged in)
            _userInfoText = CreateText(card, "UserInfo", new Vector2(0f, 80f), new Vector2(380f, 120f), 16f);
            _userInfoText.color = new Color(0.88f, 0.93f, 1f, 1f);

            // Logout button
            _logoutButton = CreateButton(card, "LogoutButton", new Vector2(0f, -40f), new Vector2(340f, 48f), "Sign Out", new Color(0.6f, 0.2f, 0.2f, 1f));
            _logoutButton.onClick.AddListener(OnLogoutClicked);

            _panel.SetActive(false);
        }

        // ──────────────────────── State Management ────────────────────────

        private void UpdateState()
        {
            bool authenticated = GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated;

            // Login form elements
            SetActive(_emailInput, !authenticated);
            SetActive(_passwordInput, !authenticated);
            SetActive(_displayNameInput, !authenticated && _isRegistering);
            SetActive(_signInButton, !authenticated);
            SetActive(_signUpButton, !authenticated);
            SetActive(_guestButton, !authenticated);

            // Logged in elements
            SetActive(_userInfoText, authenticated);
            SetActive(_logoutButton, authenticated);

            if (authenticated && auth != null)
            {
                string langVi = "Đã đăng nhập";
                string langEn = "Signed in as";
                string label = Text(langVi, langEn, "ログイン中");
                _userInfoText.text = $"<b>{label}</b>\n\n" +
                    $"{Text("Tên", "Name", "名前")}: {auth.DisplayName}\n" +
                    $"ID: {(auth.UserId.Length > 12 ? auth.UserId.Substring(0, 12) + "..." : auth.UserId)}";
                _titleText.text = Text("Tài khoản", "Account", "アカウント");
            }
            else
            {
                _titleText.text = "NIHONGO LIFE";
                _signInButton.GetComponentInChildren<TextMeshProUGUI>().text = _isRegistering
                    ? Text("Quay lại đăng nhập", "Back to Sign In", "ログインに戻る")
                    : Text("Đăng nhập", "Sign In", "ログイン");
                _signUpButton.GetComponentInChildren<TextMeshProUGUI>().text = _isRegistering
                    ? Text("Tạo tài khoản", "Create Account", "アカウント作成")
                    : Text("Đăng ký mới", "Register", "新規登録");
                _guestButton.GetComponentInChildren<TextMeshProUGUI>().text = Text("Chơi thử (Guest)", "Play as Guest", "ゲストとしてプレイ");
            }

            _statusText.text = string.Empty;
        }

        // ──────────────────────── Button Handlers ────────────────────────

        private void OnSignInClicked()
        {
            if (_isRegistering)
            {
                _isRegistering = false;
                UpdateState();
                return;
            }

            string email = _emailInput.text.Trim();
            string password = _passwordInput.text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _statusText.text = Text("Vui lòng nhập email và mật khẩu", "Please enter email and password", "メールとパスワードを入力してください");
                return;
            }

            SetButtonsInteractable(false);
            _statusText.color = new Color(0.88f, 0.93f, 1f, 1f);
            _statusText.text = Text("Đang đăng nhập...", "Signing in...", "ログイン中...");

            if (GameServices.TryGet(out IAuthService auth))
            {
                auth.SignInWithEmail(email, password, (success, error) =>
                {
                    SetButtonsInteractable(true);
                    if (success)
                    {
                        PlayerSessionService.Instance?.BeginAccount(auth.UserId, auth.DisplayName);
                        _statusText.color = new Color(0.45f, 0.84f, 0.5f, 1f);
                        _statusText.text = Text("Đăng nhập thành công!", "Sign in successful!", "ログイン成功！");
                        UpdateState();
                    }
                    else
                    {
                        _statusText.color = new Color(0.98f, 0.55f, 0.55f, 1f);
                        _statusText.text = error;
                    }
                });
            }
        }

        private void OnSignUpClicked()
        {
            if (!_isRegistering)
            {
                _isRegistering = true;
                UpdateState();
                return;
            }

            string email = _emailInput.text.Trim();
            string password = _passwordInput.text;
            string displayName = _displayNameInput.text.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _statusText.text = Text("Vui lòng nhập email và mật khẩu", "Please enter email and password", "メールとパスワードを入力してください");
                return;
            }

            SetButtonsInteractable(false);
            _statusText.color = new Color(0.88f, 0.93f, 1f, 1f);
            _statusText.text = Text("Đang tạo tài khoản...", "Creating account...", "アカウント作成中...");

            if (GameServices.TryGet(out IAuthService auth))
            {
                auth.SignUpWithEmail(email, password, displayName, (success, error) =>
                {
                    SetButtonsInteractable(true);
                    if (success)
                    {
                        _statusText.color = new Color(0.45f, 0.84f, 0.5f, 1f);
                        bool confirmationRequired = error == "EMAIL_CONFIRMATION_REQUIRED";
                        _statusText.text = confirmationRequired
                            ? Text("Đã gửi email xác minh. Hãy mở mail rồi đăng nhập.", "Verification email sent. Confirm it, then sign in.", "確認メールを送信しました。確認後にログインしてください。")
                            : Text("Tạo tài khoản thành công!", "Account created!", "アカウント作成成功！");
                        _isRegistering = false;
                        if (!confirmationRequired)
                        {
                            PlayerSessionService.Instance?.BeginAccount(auth.UserId, auth.DisplayName);
                            UpdateState();
                        }
                        else
                        {
                            SetActive(_displayNameInput, false);
                            _signInButton.GetComponentInChildren<TextMeshProUGUI>().text = Text("Đăng nhập", "Sign In", "ログイン");
                            _signUpButton.GetComponentInChildren<TextMeshProUGUI>().text = Text("Đăng ký mới", "Register", "新規登録");
                        }
                    }
                    else
                    {
                        _statusText.color = new Color(0.98f, 0.55f, 0.55f, 1f);
                        _statusText.text = error;
                    }
                });
            }
        }

        private void OnGuestClicked()
        {
            PlayerSessionService.GetOrCreate().BeginGuest();
            Hide();
        }

        private void OnLogoutClicked()
        {
            if (GameServices.TryGet(out IAuthService auth))
            {
                auth.SignOut();
            }
            PlayerSessionService.Instance?.BeginGuest();
            UpdateState();
        }

        // ──────────────────────── UI Helpers ────────────────────────

        private void SetButtonsInteractable(bool interactable)
        {
            if (_signInButton != null) _signInButton.interactable = interactable;
            if (_signUpButton != null) _signUpButton.interactable = interactable;
            if (_guestButton != null) _guestButton.interactable = interactable;
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void CenterRect(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private TextMeshProUGUI CreateText(GameObject parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var go = CreateChild(parent, name);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.6f;
            text.fontSizeMax = fontSize;
            return text;
        }

        private TMP_InputField CreateInputField(GameObject parent, string name, Vector2 position, string placeholder)
        {
            var go = CreateChild(parent, name);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(340f, 46f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.09f, 0.11f, 0.13f, 1f);

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = rect;
            input.lineType = TMP_InputField.LineType.SingleLine;

            var textGo = CreateChild(go, "Text");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 4f);
            textRect.offsetMax = new Vector2(-14f, -4f);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = 16f;
            text.color = new Color(0.92f, 0.96f, 1f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            input.textComponent = text;

            var phGo = CreateChild(go, "Placeholder");
            var phRect = phGo.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(14f, 4f);
            phRect.offsetMax = new Vector2(-14f, -4f);
            var ph = phGo.AddComponent<TextMeshProUGUI>();
            if (_font != null) ph.font = _font;
            ph.fontSize = 16f;
            ph.color = new Color(0.5f, 0.55f, 0.6f, 0.75f);
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            ph.text = placeholder;
            input.placeholder = ph;

            return input;
        }

        private Button CreateButton(GameObject parent, string name, Vector2 position, Vector2 size, string label, Color bgColor)
        {
            var go = CreateChild(parent, name);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = bgColor;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(bgColor.r + 0.08f, bgColor.g + 0.08f, bgColor.b + 0.08f, 1f);
            colors.pressedColor = new Color(bgColor.r - 0.05f, bgColor.g - 0.05f, bgColor.b - 0.05f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            go.AddComponent<UIHoverScale>();

            var textObj = CreateText(go, "Label", Vector2.zero, new Vector2(size.x - 20f, size.y - 8f), 17f);
            textObj.text = label;
            textObj.fontStyle = FontStyles.Bold;

            return button;
        }

        private static string Text(string vi, string en, string ja)
        {
            return GameServices.TryGet(out GameSettingsService settings) ? settings.Text(vi, en, ja) : vi;
        }
    }
}
