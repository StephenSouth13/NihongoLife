using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Core;
using NihongoLife.Player;

namespace NihongoLife.UI
{
    public class BusinessDashboardUI : MenuPopupBase
    {
        private TextMeshProUGUI _companyNameText;
        private TextMeshProUGUI _statsText;
        private Button _hireButton;
        private Button _partnerButton;

        protected override Vector2 CardSize => new Vector2(800f, 500f);

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "QUẢN LÝ DOANH NGHIỆP";
            en = "BUSINESS MANAGEMENT";
            ja = "ビジネス管理";
        }

        protected override void Build(RectTransform card)
        {
            _companyNameText = AddText(card, "CompanyName", 28f, 30f, -80f, 740f, 40f, TextAlignmentOptions.Center, FontStyles.Normal, false, Color.white);
            _statsText = AddText(card, "Stats", 22f, 30f, -140f, 740f, 200f, TextAlignmentOptions.Left, FontStyles.Normal, false, new Color(0.8f, 0.85f, 0.9f));

            _hireButton = AddButton(Card, "Tuyển nhân viên (Y25,000)", 100f, -380f, 280f, 60f, true);
            _partnerButton = AddButton(Card, "Ký kết liên doanh", 420f, -380f, 280f, 60f, true);

            _hireButton.onClick.AddListener(OnHireClicked);
            _partnerButton.onClick.AddListener(OnPartnerClicked);

            RefreshUI();
        }

        private void OnEnable()
        {
            RefreshUI();
            if (BusinessSystem.Instance != null)
            {
                BusinessSystem.Instance.OnBusinessMessage += HandleBusinessMessage;
            }
        }

        private void OnDisable()
        {
            if (BusinessSystem.Instance != null)
            {
                BusinessSystem.Instance.OnBusinessMessage -= HandleBusinessMessage;
            }
        }

        private void HandleBusinessMessage(string msg)
        {
            // In a real game, spawn a floating notification. Here we just log and refresh.
            Debug.Log("[BusinessUI] " + msg);
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (BusinessSystem.Instance == null || !BusinessSystem.Instance.OwnsCompany)
            {
                _companyNameText.text = "Bạn chưa có công ty.";
                _statsText.text = "Hãy học lên 300 Điểm Kiến Thức và tiết kiệm 50,000 Yen để khởi nghiệp!";
                _hireButton.interactable = false;
                _partnerButton.interactable = false;
                return;
            }

            var b = BusinessSystem.Instance.Business;
            _companyNameText.text = $"Công ty: {b.companyName}";
            
            _statsText.text = $"Vốn điều lệ: <color=#FFD700>Y{b.capitalYen}</color>\n" +
                              $"Nhân viên: {b.employeeCount} người\n" +
                              $"Điểm Uy tín: {b.reputation}\n" +
                              $"Đánh giá Đãi ngộ: {b.fairWageScore}/100\n" +
                              $"Đối tác Liên doanh: {b.partnershipCount}";

            _hireButton.interactable = b.capitalYen >= 25000;
            _partnerButton.interactable = b.reputation >= 60;
        }

        private void OnHireClicked()
        {
            if (BusinessSystem.Instance != null)
            {
                BusinessSystem.Instance.TryHireEmployee(25000); // Base wage
                RefreshUI();
            }
        }

        private void OnPartnerClicked()
        {
            if (BusinessSystem.Instance != null)
            {
                BusinessSystem.Instance.TryCreatePartnership(60);
                RefreshUI();
            }
        }
    }
}
