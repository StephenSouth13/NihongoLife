using UnityEngine;
using TMPro;
using NihongoLife.Player;
using System.Text;

namespace NihongoLife.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI inventoryText;
        [SerializeField] private TextMeshProUGUI yenText;

        private void OnEnable()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += UpdateUI;
                UpdateUI();
            }
        }

        private void OnDisable()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= UpdateUI;
            }
        }

        private void UpdateUI()
        {
            if (PlayerInventory.Instance == null) return;

            if (yenText != null)
            {
                yenText.text = NihongoLife.Core.Loc.T("Ví tiền: ", "Wallet: ", "財布：") + NihongoLife.Core.Loc.Yen(PlayerInventory.Instance.Yen);
            }

            if (inventoryText != null)
            {
                if (PlayerInventory.Instance.Items.Count == 0)
                {
                    inventoryText.text = NihongoLife.Core.Loc.T("Túi đồ trống.", "Your bag is empty.", "かばんはからっぽです。");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in PlayerInventory.Instance.Items)
                    {
                        sb.AppendLine($"- {item.Name} x{item.quantity}");
                    }
                    inventoryText.text = sb.ToString();
                }
            }
        }
    }
}
