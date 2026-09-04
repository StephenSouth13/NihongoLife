using UnityEngine;
using TMPro;
using NihongoLife.Player;

namespace NihongoLife.UI
{
    public class StatusUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI staminaText;
        [SerializeField] private TextMeshProUGUI expText;

        private void OnEnable()
        {
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.OnStatusChanged += UpdateUI;
                UpdateUI();
            }
        }

        private void OnDisable()
        {
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.OnStatusChanged -= UpdateUI;
            }
        }

        private void UpdateUI()
        {
            if (PlayerStatus.Instance == null) return;
            
            if (nameText != null) nameText.text = PlayerStatus.Instance.PlayerName;
            if (levelText != null) levelText.text = $"Level: {PlayerStatus.Instance.Level}";
            if (healthText != null) healthText.text = $"HP: {PlayerStatus.Instance.CurrentHealth} / {PlayerStatus.Instance.MaxHealth}";
            if (staminaText != null) staminaText.text = $"SP: {PlayerStatus.Instance.CurrentStamina} / {PlayerStatus.Instance.MaxStamina}";
            if (expText != null) expText.text = $"EXP: {PlayerStatus.Instance.CurrentExp} / {PlayerStatus.Instance.MaxExp}";
        }
    }
}
