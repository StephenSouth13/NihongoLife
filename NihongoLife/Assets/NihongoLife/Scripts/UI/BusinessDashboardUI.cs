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

            _hireButton = AddButton(Card, NihongoLife.Core.Loc.T("Tuyển nhân viên (¥25,000)", "Hire staff (¥25,000)", "社員を雇う（25,000円）"), 100f, -380f, 280f, 60f, true);
            _partnerButton = AddButton(Card, NihongoLife.Core.Loc.T("Ký kết liên doanh", "Sign a joint venture", "共同事業を結ぶ"), 420f, -380f, 280f, 60f, true);

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
                _companyNameText.text = NihongoLife.Core.Loc.T("Bạn chưa có công ty.", "You don't have a company yet.", "まだ会社がありません。");
                _statsText.text = NihongoLife.Core.Loc.T("Hãy học lên 300 Điểm Trí Tuệ và tiết kiệm 50,000 Yen để khởi nghiệp!", "Reach 300 knowledge points and save ¥50,000 to start a business!", "知識ポイント300と50,000円をためて起業しましょう！");
                _hireButton.interactable = false;
                _partnerButton.interactable = false;
                return;
            }

            var b = BusinessSystem.Instance.Business;
            _companyNameText.text = NihongoLife.Core.Loc.T("Công ty: ", "Company: ", "会社：") + b.companyName;
            
            _statsText.text = NihongoLife.Core.Loc.F(
                "Vốn điều lệ: <color=#FFD700>{0}</color>\nNhân viên: {1} người\nĐiểm Uy tín: {2}\nĐánh giá Đãi ngộ: {3}/100\nĐối tác Liên doanh: {4}",
                "Capital: <color=#FFD700>{0}</color>\nStaff: {1}\nReputation: {2}\nFair pay rating: {3}/100\nJoint ventures: {4}",
                "資本金：<color=#FFD700>{0}</color>\n社員：{1}人\n信用：{2}\n待遇評価：{3}/100\n共同事業：{4}",
                NihongoLife.Core.Loc.Yen((int)b.capitalYen), b.employeeCount, b.reputation, b.fairWageScore, b.partnershipCount);

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
