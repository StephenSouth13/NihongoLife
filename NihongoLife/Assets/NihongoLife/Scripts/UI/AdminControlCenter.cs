using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Core;
using NihongoLife.Save;
using NihongoLife.Data;
using NihongoLife.Player;

namespace NihongoLife.UI
{
    public class AdminControlCenter : MenuPopupBase
    {
        [Header("Admin Auth")]
        // [SerializeField] private string adminPasscode = "dev1234";
        
        private bool _isAuthenticated = false;
        private Transform _playerListContainer;
        private string _selectedPlayerId = "";
        private TextMeshProUGUI _selectedPlayerText;

        // Input fields for editing
        private TMP_InputField _yenInput;
        private TMP_InputField _knowledgeInput;
        
        protected override Vector2 CardSize => new Vector2(1000f, 650f);

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "GOD MODE - ADMIN CONTROL CENTER";
            en = "GOD MODE - ADMIN CONTROL CENTER";
            ja = "GOD MODE - ADMIN CONTROL CENTER";
        }

        protected override void Build(RectTransform card)
        {
            if (!_isAuthenticated)
            {
                BuildLoginScreen();
            }
            else
            {
                BuildDashboard();
            }
        }

        private void BuildLoginScreen()
        {
            AddText(Card, NihongoLife.Core.Loc.T("Vui lòng nhập mã PIN Quản trị viên", "Enter the administrator PIN", "管理者PINを入力してください"), 32, 0, -200, 1000, 50, TextAlignmentOptions.Center, FontStyles.Normal, false, Color.white);
            
            // Note: In a real UI builder we'd use a TMP_InputField. Here we simulate a simple click to bypass for Editor speed.
            var btn = AddButton(Card, NihongoLife.Core.Loc.T("XÁC NHẬN (Bấm vào đây để vào Admin)", "CONFIRM (enter Admin)", "確認（管理画面へ）"), 300, -300, 400, 60, true);
            btn.onClick.AddListener(() => 
            {
                _isAuthenticated = true;
                ClearCard();
                Build(Card); // Rebuild UI
            });
        }

        private void ClearCard()
        {
            foreach (Transform child in Card)
            {
                Destroy(child.gameObject);
            }
        }

        private void BuildDashboard()
        {
            // Left Panel: Online Players
            AddText(Card, NihongoLife.Core.Loc.T("NGƯỜI CHƠI ĐANG ONLINE (Realtime)", "PLAYERS ONLINE (realtime)", "オンラインのプレイヤー（リアルタイム）"), 24, 30, -80, 400, 40, TextAlignmentOptions.Left, FontStyles.Normal, false, Color.white);
            
            var listBg = AddImage(Card, "ListBG", 30, -120, 350, 500, new Color(0.1f, 0.1f, 0.15f), UIStyleKit.RoundedSprite());
            _playerListContainer = listBg.transform;

            // Right Panel: Player Details & Edit
            AddText(Card, NihongoLife.Core.Loc.T("BẢNG ĐIỀU KHIỂN QUYỀN LỰC", "ADMIN CONTROLS", "管理コントロール"), 24, 420, -80, 500, 40, TextAlignmentOptions.Left, FontStyles.Normal, false, UIStyleKit.AccentGold);
            _selectedPlayerText = AddText(Card, NihongoLife.Core.Loc.T("Chưa chọn người chơi nào.", "No player selected.", "プレイヤーが選ばれていません。"), 20, 420, -130, 500, 60, TextAlignmentOptions.Left, FontStyles.Normal, false, Color.white);

            AddText(Card, NihongoLife.Core.Loc.T("Tiền (Yen):", "Money (yen):", "お金（円）："), 20, 420, -220, 200, 40, TextAlignmentOptions.Left, FontStyles.Normal, false, Color.white);
            _yenInput = CreateAdminInput("YenInput", 520, -220, 200, 40);

            AddText(Card, NihongoLife.Core.Loc.T("Kiến thức:", "Knowledge:", "知識："), 20, 420, -280, 200, 40, TextAlignmentOptions.Left, FontStyles.Normal, false, Color.white);
            _knowledgeInput = CreateAdminInput("KnowledgeInput", 520, -280, 200, 40);

            var applyBtn = AddButton(Card, NihongoLife.Core.Loc.T("Áp dụng & Ghi đè DB", "Apply & overwrite DB", "適用してDBを上書き"), 420, -350, 300, 50, true);
            applyBtn.onClick.AddListener(ApplyChangesToSelectedPlayer);

            var kickBtn = AddButton(Card, NihongoLife.Core.Loc.T("Đá khỏi Server (Kick)", "Kick from server", "サーバーからキック"), 420, -420, 300, 50, false);
            kickBtn.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;

            var teleportBtn = AddButton(Card, NihongoLife.Core.Loc.T("Dịch chuyển tới người này", "Teleport to this player", "このプレイヤーへ移動"), 420, -490, 300, 50, false);
            teleportBtn.onClick.AddListener(TeleportToPlayer);

            InvokeRepeating(nameof(RefreshOnlinePlayers), 0f, 2f);
        }

        private TMP_InputField CreateAdminInput(string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Card, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f);
            
            var input = go.AddComponent<TMP_InputField>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.color = Color.white;
            textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(w-10, h);
            input.textComponent = text;
            return input;
        }

        private void RefreshOnlinePlayers()
        {
            if (!_isAuthenticated || _playerListContainer == null) return;
            
            if (GameServices.TryGet(out IOnlineWorldService online))
            {
                // Clear old list
                foreach (Transform child in _playerListContainer) Destroy(child.gameObject);

                float yOffset = -10f;
                foreach (var p in online.VisiblePlayers)
                {
                    var btn = AddButton(_playerListContainer.transform as RectTransform, p.displayName, 10, yOffset, 330, 40, false);
                    btn.onClick.AddListener(() => SelectPlayer(p));
                    yOffset -= 45f;
                }
            }
        }

        private void SelectPlayer(OnlinePlayerSnapshot p)
        {
            _selectedPlayerId = p.playerId;
            _selectedPlayerText.text = NihongoLife.Core.Loc.F("Đang chọn: <b>{0}</b>\nVị trí: {1} ({2:F1}, {3:F1}, {4:F1})", "Selected: <b>{0}</b>\nPosition: {1} ({2:F1}, {3:F1}, {4:F1})", "選択中：<b>{0}</b>\n位置：{1}（{2:F1}, {3:F1}, {4:F1}）", p.displayName, p.sceneName, p.position.x, p.position.y, p.position.z);
            // In a real scenario, we'd fetch their exact stats from DB here.
            _yenInput.text = "99999"; 
            _knowledgeInput.text = "1000";
        }

        private void ApplyChangesToSelectedPlayer()
        {
            if (string.IsNullOrEmpty(_selectedPlayerId)) return;
            
            int newYen = int.TryParse(_yenInput.text, out int y) ? y : 0;
            int newK = int.TryParse(_knowledgeInput.text, out int k) ? k : 0;

            // Notice: To actually update another user's data on Supabase, the Admin client needs 
            // either a Service Role key or an Edge Function. Here we simulate the logic.
            Debug.Log($"[Admin] Bắn lệnh cập nhật lên DB cho user {_selectedPlayerId}: Yên={newYen}, Kiến thức={newK}");
            
            // To notify the user locally if they are in the same scene, we can send a system chat:
            if (GameServices.TryGet(out IOnlineWorldService online))
            {
                online.SendChatMessage("town", NihongoLife.Core.Loc.F("[HỆ THỐNG] Admin vừa buff tài nguyên cho user {0}!", "[SYSTEM] An admin just boosted resources for user {0}!", "［システム］管理者がユーザー{0}の資源を増やしました！", _selectedPlayerId));
            }
        }

        private void TeleportToPlayer()
        {
            if (string.IsNullOrEmpty(_selectedPlayerId)) return;
            if (GameServices.TryGet(out IOnlineWorldService online))
            {
                var target = online.VisiblePlayers.SystemFind(p => p.playerId == _selectedPlayerId);
                var player = FindFirstObjectByType<PlayerController>();
                if (target != null && player != null)
                {
                    player.transform.position = target.position;
                    Debug.Log($"[Admin] Teleported to {target.displayName}");
                }
            }
        }
    }
    
    // Extension method for finding in ReadOnlyList without LINQ overhead
    public static class ReadOnlyListExtensions
    {
        public static T SystemFind<T>(this IReadOnlyList<T> list, Predicate<T> match)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i])) return list[i];
            }
            return default;
        }
    }
}
