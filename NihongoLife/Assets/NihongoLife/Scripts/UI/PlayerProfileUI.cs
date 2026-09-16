using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>
    /// Player profile panel — shows avatar info, stats, bio, and edit capability for own profile.
    /// </summary>
    public class PlayerProfileUI : MonoBehaviour
    {
        private GameObject _panel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _statsText;
        private TMP_InputField _nameInput;
        private TMP_InputField _bioInput;
        private Button _saveButton;
        private Button _closeButton;
        private Button _addFriendButton;
        private TMP_FontAsset _font;
        private string _viewingUserId;
        private bool _isOwnProfile;

        public void Initialize(TMP_FontAsset font)
        {
            _font = font;
            BuildUI();
        }

        public void ShowOwnProfile()
        {
            _isOwnProfile = true;
            _viewingUserId = GameServices.TryGet(out IAuthService auth) ? auth.UserId : string.Empty;
            if (_panel != null) _panel.SetActive(true);
            LoadProfile();
        }

        public void ShowProfile(string userId)
        {
            _isOwnProfile = GameServices.TryGet(out IAuthService auth) && auth.IsAuthenticated && auth.UserId == userId;
            _viewingUserId = userId;
            if (_panel != null) _panel.SetActive(true);
            LoadProfile();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void BuildUI()
        {
            _panel = new GameObject("ProfilePanel");
            _panel.transform.SetParent(transform, false);
            var rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.92f);

            var card = new GameObject("Card");
            card.transform.SetParent(_panel.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            Center(cardRect, new Vector2(460f, 480f));
            card.AddComponent<Image>().color = new Color(0.035f, 0.045f, 0.055f, 0.97f);

            _titleText = MakeText(card, "Title", new Vector2(0f, 200f), new Vector2(400f, 38f), 24f);
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = new Color(1f, 0.91f, 0.54f, 1f);

            _statsText = MakeText(card, "Stats", new Vector2(0f, 80f), new Vector2(400f, 160f), 15f);
            _statsText.alignment = TextAlignmentOptions.TopLeft;
            _statsText.color = new Color(0.88f, 0.93f, 1f, 1f);
            _statsText.lineSpacing = 6f;

            // Editable fields
            _nameInput = CreateInput(card, "NameInput", new Vector2(0f, -20f), "Display Name");
            _bioInput = CreateInput(card, "BioInput", new Vector2(0f, -76f), "Bio / Status");

            _saveButton = MakeButton(card, "SaveBtn", new Vector2(0f, -136f), new Vector2(200f, 42f), Text("Lưu", "Save", "保存"));
            _saveButton.onClick.AddListener(OnSaveClicked);

            _addFriendButton = MakeButton(card, "AddFriendBtn", new Vector2(0f, -136f), new Vector2(200f, 42f), Text("Kết bạn", "Add Friend", "フレンド追加"));
            _addFriendButton.onClick.AddListener(OnAddFriendClicked);

            _closeButton = MakeButton(card, "CloseBtn", new Vector2(200f, 200f), new Vector2(40f, 40f), "✕");
            _closeButton.onClick.AddListener(Hide);

            _panel.SetActive(false);
        }

        private void LoadProfile()
        {
            _titleText.text = Text("Hồ sơ", "Profile", "プロフィール");
            _statsText.text = Text("Đang tải...", "Loading...", "読み込み中...");

            // Toggle edit vs view mode
            SetActive(_nameInput, _isOwnProfile);
            SetActive(_bioInput, _isOwnProfile);
            SetActive(_saveButton, _isOwnProfile);
            SetActive(_addFriendButton, !_isOwnProfile);

            var profileService = FindFirstObjectByType<ProfileService>();
            if (profileService == null)
            {
                ShowOfflineProfile();
                return;
            }

            profileService.GetProfile(_viewingUserId, profile =>
            {
                if (profile == null)
                {
                    ShowOfflineProfile();
                    return;
                }

                _titleText.text = profile.display_name;

                _statsText.text =
                    $"<color=#f1c75b>{Text("Cấp độ", "Level", "レベル")}</color>: {profile.level}\n" +
                    $"<color=#f1c75b>XP</color>: {profile.xp}\n" +
                    $"<color=#f1c75b>{Text("Chương", "Chapter", "チャプター")}</color>: {profile.current_chapter}\n" +
                    $"\n<color=#8fa3b8>{(string.IsNullOrWhiteSpace(profile.bio) ? Text("Chưa có bio", "No bio yet", "自己紹介未設定") : profile.bio)}</color>";

                if (_isOwnProfile)
                {
                    _nameInput.text = profile.display_name ?? string.Empty;
                    _bioInput.text = profile.bio ?? string.Empty;
                }
            });
        }

        private void ShowOfflineProfile()
        {
            if (GameServices.TryGet(out Save.IProgressRepository repo))
            {
                var p = repo.GetProgress();
                _titleText.text = p.displayName;
                _statsText.text =
                    $"<color=#f1c75b>{Text("Cấp độ", "Level", "レベル")}</color>: {p.level}\n" +
                    $"<color=#f1c75b>XP</color>: {p.xp}\n" +
                    $"<color=#f1c75b>{Text("Hoàn thành", "Completed", "完了")}</color>: {p.completedScenarios.Count}\n" +
                    $"\n<color=#8fa3b8>{Text("Chế độ offline", "Offline mode", "オフラインモード")}</color>";
            }
        }

        private void OnSaveClicked()
        {
            var profileService = FindFirstObjectByType<ProfileService>();
            if (profileService == null) return;

            profileService.UpdateProfile(_nameInput.text.Trim(), _bioInput.text.Trim(), success =>
            {
                if (success)
                {
                    _titleText.text = _nameInput.text.Trim();
                }
            });
        }

        private void OnAddFriendClicked()
        {
            if (string.IsNullOrWhiteSpace(_viewingUserId)) return;

            var friendService = FindFirstObjectByType<FriendService>();
            friendService?.SendFriendRequest(_viewingUserId);
        }

        // ──────────────────────── Helpers ────────────────────────

        private static void SetActive(Component c, bool v)
        {
            if (c != null) c.gameObject.SetActive(v);
        }

        private static void Center(RectTransform r, Vector2 s)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = s;
        }

        private TextMeshProUGUI MakeText(GameObject p, string n, Vector2 pos, Vector2 size, float fs)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            var t = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = fs;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.enableAutoSizing = true;
            t.fontSizeMin = fs * 0.6f;
            t.fontSizeMax = fs;
            return t;
        }

        private TMP_InputField CreateInput(GameObject parent, string name, Vector2 pos, string placeholder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(340f, 40f);
            go.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.13f, 1f);

            var input = go.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.textViewport = r;

            var txt = MakeText(go, "Text", Vector2.zero, new Vector2(320f, 32f), 15f);
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            txt.margin = new Vector4(10, 0, 10, 0);
            input.textComponent = txt;

            var ph = MakeText(go, "Placeholder", Vector2.zero, new Vector2(320f, 32f), 15f);
            ph.text = placeholder;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            ph.margin = new Vector4(10, 0, 10, 0);
            ph.color = new Color(0.5f, 0.55f, 0.6f, 0.7f);
            input.placeholder = ph;

            return input;
        }

        private Button MakeButton(GameObject p, string n, Vector2 pos, Vector2 size, string label)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);
            var btn = go.AddComponent<Button>();
            go.AddComponent<UIHoverScale>();
            var txt = MakeText(go, "Lbl", Vector2.zero, new Vector2(size.x - 10f, size.y - 6f), 15f);
            txt.text = label;
            txt.fontStyle = FontStyles.Bold;
            return btn;
        }

        private static string Text(string vi, string en, string ja)
        {
            return GameServices.TryGet(out GameSettingsService s) ? s.Text(vi, en, ja) : vi;
        }
    }
}
