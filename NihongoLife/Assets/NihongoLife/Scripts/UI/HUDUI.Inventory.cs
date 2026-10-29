using System.Linq;
using NihongoLife.Save;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    /// <summary>Partial of HUDUI: inventory actions and polished player panels. The MonoBehaviour entry point stays HUDUI.cs.</summary>
    public partial class HUDUI : MonoBehaviour
    {
        private void EnsureInventoryActions()
        {
            if (_inventoryWindow != null || inventoryPanel == null || _inventoryDetailText != null) return; // InventoryWindow has its own detail/actions
            var inventoryRect = inventoryPanel.GetComponent<RectTransform>();
            if (inventoryRect != null) inventoryRect.sizeDelta = new Vector2(780f, 420f);
            TMP_FontAsset font = scenarioTitleText != null ? scenarioTitleText.font : null;
            _inventoryDetailText = CreateHudText("InventoryDetail", inventoryPanel.transform,
                new Vector2(430f, -28f), new Vector2(300f, 150f), 15f, font);
            _inventoryDetailText.alignment = TextAlignmentOptions.TopLeft;
            _inventoryDetailText.color = new Color(0.82f, 0.9f, 0.94f, 1f);

            CreateInventoryButton(NihongoLife.Core.Loc.T("Chi tiết", "Details", "詳細"), new Vector2(430f, -190f), () => RefreshInventoryDetail());
            CreateInventoryButton(NihongoLife.Core.Loc.T("Bỏ ra", "Drop", "捨てる"), new Vector2(540f, -190f), DropSelectedInventoryItem);
            CreateInventoryButton(NihongoLife.Core.Loc.T("Chia nhỏ", "Split", "分ける"), new Vector2(650f, -190f), SplitSelectedInventoryItem);
            CreateInventoryButton(NihongoLife.Core.Loc.T("Sắp xếp", "Sort", "並べ替え"), new Vector2(760f, -190f), SortInventory);
        }

        private void CreateInventoryButton(string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("InventoryAction_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(inventoryPanel.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(100f, 40f);
            go.GetComponent<Image>().color = new Color(0.08f, 0.18f, 0.24f, 0.96f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(action);
            var text = CreateHudText("Label", go.transform, Vector2.zero, Vector2.zero, 13f,
                scenarioTitleText != null ? scenarioTitleText.font : null);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            text.raycastTarget = false;
        }

        private void HandleInventoryShortcuts()
        {
            if (Keyboard.current == null || _inventoryWindow != null) return;
            for (int i = 0; i < 9; i++)
            {
                if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                {
                    _selectedInventoryIndex = i;
                    RefreshInventoryDetail();
                    break;
                }
            }
            if (Keyboard.current.deleteKey.wasPressedThisFrame) DropSelectedInventoryItem();
        }

        private InventoryEntry SelectedInventoryItem()
        {
            var inventory = PlayerInventory.Instance;
            return inventory != null && _selectedInventoryIndex >= 0 && _selectedInventoryIndex < inventory.Items.Count
                ? inventory.Items[_selectedInventoryIndex]
                : null;
        }

        private void RefreshInventoryDetail()
        {
            if (_inventoryDetailText == null) return;
            var item = SelectedInventoryItem();
            if (item == null)
            {
                _inventoryDetailText.text = Text("Chọn một ô đồ để xem chi tiết.", "Select an item to view details.", "アイテムを選択してください。");
                return;
            }
            string name = item.Name;
            _inventoryDetailText.text = $"<size=120%><b>{name}</b></size>\n" +
                $"{Text("Số lượng", "Quantity", "数量")}: {item.quantity}\n" +
                $"{Text("Giá trị", "Value", "価格")}: {NihongoLife.Core.Loc.Money(item.priceYen)}\n" +
                (item.itemId == "train_ticket_midori"
                    ? Text("Tuyến: Sakura → Midori\nVé dùng cho chuyến kế tiếp.", "Route: Sakura → Midori\nValid for the next service.", "路線: さくら → みどり\n次の電車に有効です。")
                    : Text("Vật phẩm trong balo.", "Item in your bag.", "バッグのアイテムです。"));
        }

        private void DropSelectedInventoryItem()
        {
            var item = SelectedInventoryItem();
            if (item == null) return;
            PlayerInventory.Instance.DropItem(item.itemId);
            RefreshInventoryDetail();
        }

        private void SplitSelectedInventoryItem()
        {
            var item = SelectedInventoryItem();
            if (item == null || item.quantity < 2) return;
            PlayerInventory.Instance.SplitStack(item.itemId, item.quantity / 2);
            RefreshInventoryDetail();
        }

        private void SortInventory()
        {
            PlayerInventory.Instance?.SortItems();
            _selectedInventoryIndex = 0;
            RefreshInventoryDetail();
        }

        private void RefreshPlayerPanels()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            if (walletText != null)
            {
                walletText.text = $"{NihongoLife.Core.Loc.Money(inventory.Yen)}";
            }

            if (inventoryText != null)
            {
                inventoryText.text = BuildInventoryGrid(inventory);
            }

            if (characterStatsText != null)
            {
                characterStatsText.text =
                    Text("Học viên", "Learner", "学習者") + "\n" +
                    Text("Cấp độ: N5 Starter", "Level: N5 Starter", "レベル: N5 Starter") + "\n" +
                    Text("Di chuyển: đi bộ / chạy", "Movement: walk / run", "移動: 歩く / 走る") + "\n" +
                    $"{Text("Tiền mặt", "Cash", "所持金")}: {NihongoLife.Core.Loc.Money(inventory.Yen)}\n" +
                    Text("Mục tiêu: mua hàng bằng tiếng Nhật", "Goal: shop in Japanese", "目標: 日本語で買い物する") + "\n" +
                    "N: Night  |  M: Morning";
            }

            RenderPolishedPlayerPanels(inventory);
            RefreshInventoryDetail();
        }

        private void RenderPolishedPlayerPanels(PlayerInventory inventory)
        {
            if (walletText != null)
            {
                walletText.text = $"{NihongoLife.Core.Loc.Money(inventory.Yen)}";
            }

            if (inventoryText != null)
            {
                inventoryText.text = BuildInventoryGrid(inventory);
            }

            if (characterStatsText != null)
            {
                string learnerName = NihongoLife.Core.Loc.T("Học viên Nihongo", "Nihongo learner", "日本語の学生");
                int level = 1;
                if (GameServices.TryGet(out Save.IProgressRepository progressRepo))
                {
                    var progress = progressRepo.GetProgress();
                    if (!string.IsNullOrWhiteSpace(progress.displayName)) learnerName = progress.displayName;
                    level = progress.level;
                }
                if (GameServices.TryGet(out IAuthService authSvc) && authSvc.IsAuthenticated && !string.IsNullOrWhiteSpace(authSvc.DisplayName))
                {
                    learnerName = authSvc.DisplayName;
                }

                string goalText = Text("Chưa có nhiệm vụ", "No active goal", "目標なし");
                if (ScenarioManager.Instance != null && ScenarioManager.Instance.Objectives.Count > 0)
                {
                    var activeObjective = ScenarioManager.Instance.Objectives.Find(o => o.state == ObjectiveState.Active)
                        ?? ScenarioManager.Instance.Objectives[0];
                    goalText = ObjectiveText(activeObjective);
                }

                PlayerStatus status = PlayerStatus.Instance;
                BusinessSystem businessSystem = SceneRefs.Find<BusinessSystem>();
                // Part-time work now comes from the data-driven jobs (Task Journal, N).
                var activeJob = NihongoLife.Progression.QuestService.Active.FirstOrDefault(q => q.IsJob);
                int totalShifts = 0;
                if (GameServices.TryGet(out IProgressRepository careerRepository) && careerRepository.GetProgress()?.careers != null)
                    totalShifts = careerRepository.GetProgress().careers.Sum(c => c.totalShifts);
                string careerSummary = activeJob != null
                    ? $"<color=#78c7d4>💼 {Text("Làm thêm", "Part-time", "アルバイト")}</color>: {activeJob.titleVi} · {totalShifts} {Text("ca đã xong", "shifts done", "シフト")}\n"
                    : $"<color=#78c7d4>💼 {Text("Làm thêm", "Part-time", "アルバイト")}</color>: {totalShifts} {Text("ca đã xong", "shifts done", "シフト")}\n";
                string businessSummary = businessSystem != null && businessSystem.OwnsCompany
                    ? $"<color=#7fd39b>🏢 {Text("Công ty", "Company", "会社")}</color>: {businessSystem.Business.companyName} · Uy tín {businessSystem.Business.reputation}\n"
                    : string.Empty;
                characterStatsText.text = careerSummary + businessSummary +
                    $"<size=125%><b>{Text("Hồ sơ học viên", "Learner Profile", "学習者プロフィール")}</b></size>\n" +
                    $"<color=#f1c75b>{Text("Tên", "Name", "名前")}</color>: {learnerName}\n" +
                    $"<color=#f1c75b>{Text("Cấp độ", "Level", "レベル")}</color>: N5 · Lv.{level}\n" +
                    (status != null
                        ? $"{StatLine(Text("Máu", "Health", "体力"), status.CurrentHealth, status.MaxHealth, "#e35d6a")}\n" +
                          $"{StatLine(Text("Năng lượng", "Energy", "元気"), status.CurrentEnergy, status.MaxEnergy, "#64b5f6")}\n" +
                          $"{StatLine(Text("No", "Hunger", "満腹"), status.Hunger, 100f, "#f2b84b")}\n" +
                          $"{StatLine(Text("Khát", "Thirst", "水分"), status.Thirst, 100f, "#4dd0c8")}\n" +
                          $"<color=#b79cff>{Text("Trí tuệ", "Knowledge", "知識")}: {status.Knowledge}</color>\n"
                        : string.Empty) +
                    $"<color=#f1c75b>{Text("Tiền mặt", "Cash", "所持金")}</color>: {NihongoLife.Core.Loc.Money(inventory.Yen)}\n" +
                    $"<color=#f1c75b>{Text("Mục tiêu", "Goal", "目標")}</color>: {goalText}\n\n" +
                    "<color=#8fa3b8>Tab: profile  |  B: bag  |  V: mic  |  E: talk</color>";
            }
        }

        private static string BuildInventoryGrid(PlayerInventory inventory)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"<size=125%><b>{Text("Balo", "Bag", "バッグ")}</b></size>  <color=#8fa3b8>{inventory.UsedSlots}/{inventory.MaxSlots} slots</color>");
            for (int i = 0; i < inventory.MaxSlots; i++)
            {
                if (i < inventory.Items.Count)
                {
                    var item = inventory.Items[i];
                    string name = item.Name;
                    string kind = item.useType == ItemUseType.Food ? " [FOOD]" : item.useType == ItemUseType.Drink ? " [DRINK]" : string.Empty;
                    builder.Append($"<color=#f1c75b>[{i + 1:00}]</color> <b>{name}</b>{kind}  x{item.quantity}/{inventory.MaxStackSize}");
                }
                else
                {
                    builder.Append($"<color=#536170>[{i + 1:00}]  --</color>");
                }
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private static string StatLine(string label, float value, float max, string color)
        {
            const int segments = 10;
            int filled = Mathf.RoundToInt(Mathf.Clamp01(value / Mathf.Max(1f, max)) * segments);
            string bar = new string('|', filled) + new string('.', segments - filled);
            return $"<color={color}>{label} [{bar}] {value:0}/{max:0}</color>";
        }
    }
}
