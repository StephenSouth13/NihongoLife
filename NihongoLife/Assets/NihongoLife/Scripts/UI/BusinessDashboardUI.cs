using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
        private Button _partnerButton, _roleButton, _collectButton, _trainButton, _fireButton;
        private TextMeshProUGUI _workersText;
        private int _roleIndex;
        public Button HireButton => _hireButton;
        public Button CollectButton => _collectButton;
        public Button RoleButton => _roleButton;
        public Button TrainButton => _trainButton;
        public Button FireButton => _fireButton;
        public string WorkersTextValue => _workersText != null ? _workersText.text : "";

        private string _message = "";
        protected override bool LocksGameplay => true;

        /// <summary>F4: the company screen, opened with Z (also from tests).</summary>
        public static BusinessDashboardUI GetOrCreate(GameObject host, TMP_FontAsset font)
        {
            var ui = host.GetComponent<BusinessDashboardUI>();
            if (ui == null) ui = host.AddComponent<BusinessDashboardUI>();
            ui.Initialize(font);
            return ui;
        }

        protected override void Update()
        {
            base.Update();
            if (Keyboard.current == null || !Keyboard.current.zKey.wasPressedThisFrame) return;
            if (IsOpen) { Hide(); return; }
            if (!UiModalStack.BlocksHotkeys) Show();
        }

        protected override void OnOpened()
        {
            if (BusinessSystem.Instance != null) { BusinessSystem.Instance.OnBusinessMessage -= HandleBusinessMessage; BusinessSystem.Instance.OnBusinessMessage += HandleBusinessMessage; }
            RefreshUI();
        }

        protected override void OnLanguageApplied() { _message = ""; RefreshUI(); }

        protected override Vector2 CardSize => new Vector2(800f, 600f);

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

            _hireButton = AddButton(Card, "Hire", 30f, -470f, 240f, 56f, true);
            _roleButton = AddButton(Card, "Role", 280f, -470f, 240f, 56f, true);
            _collectButton = AddButton(Card, "Collect", 530f, -470f, 240f, 56f, true);
            _trainButton = AddButton(Card, "Train", 30f, -534f, 240f, 56f, true);
            _fireButton = AddButton(Card, "Fire", 280f, -534f, 240f, 56f, true);
            _workersText = AddText(card, "Workers", 17f, 30f, -270f, 740f, 190f, TextAlignmentOptions.Left, FontStyles.Normal, false, new Color(0.85f, 0.9f, 0.8f));
            _partnerButton = AddButton(Card, "Partner", 530f, -534f, 240f, 56f, true);

            _hireButton.onClick.AddListener(OnHireClicked);
            _partnerButton.onClick.AddListener(OnPartnerClicked);
            _roleButton.onClick.AddListener(() => { _roleIndex = (_roleIndex + 1) % BusinessWorkers.Roles.Length; RefreshUI(); });
            _collectButton.onClick.AddListener(() => { BusinessWorkers.Collect(BusinessSystem.Instance); RefreshUI(); });
            _trainButton.onClick.AddListener(() => { var w = Weakest(); if (w != null) BusinessWorkers.Train(BusinessSystem.Instance, w.id); RefreshUI(); });
            _fireButton.onClick.AddListener(() => { var w = Latest(); if (w != null) BusinessWorkers.Fire(BusinessSystem.Instance, w.id); RefreshUI(); });

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
            Debug.Log("[BusinessUI] " + msg); _message = msg;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_workersText == null || _statsText == null) return;
            SetLabel(_partnerButton, NihongoLife.Core.Loc.T("Ký kết liên doanh", "Sign a joint venture", "共同事業を結ぶ"));
            if (BusinessSystem.Instance == null || !BusinessSystem.Instance.OwnsCompany)
            {
                _companyNameText.text = NihongoLife.Core.Loc.T("Bạn chưa có công ty.", "You don't have a company yet.", "まだ会社がありません。");
                _statsText.text = NihongoLife.Core.Loc.T("Hãy học lên 300 Điểm Trí Tuệ và tiết kiệm 50,000 Yen để khởi nghiệp!", "Reach 300 knowledge points and save ¥50,000 to start a business!", "知識ポイント300と50,000円をためて起業しましょう！");
                SetLabel(_hireButton, Loc.T("Lập công ty (", "Found company (", "会社を作る（") + Loc.Yen(BusinessSystem.CompanyCapitalRequirement) + Loc.T(")", ")", "）")); _hireButton.interactable = BusinessSystem.Instance != null; DisableWorkerButtons();
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

            int wage = Mathf.Max(25000, BusinessWorkers.LivingWage(b));
            var role = BusinessWorkers.Roles[_roleIndex];
            SetLabel(_hireButton, Loc.T("Tuyển", "Hire", "雇う") + " (" + Loc.Yen(wage) + ")");
            SetLabel(_roleButton, Loc.T("Vị trí: ", "Role: ", "役割：") + role.Label);
            _hireButton.interactable = b.capitalYen >= wage && b.workers != null && b.workers.Count < BusinessWorkers.MaxWorkers;
            BusinessWorkers.Pending(BusinessSystem.Instance, out int gross, out int wages, out int net);
            SetLabel(_collectButton, Loc.T("Thu về", "Collect", "回収") + " (" + Loc.Yen(Mathf.Max(0, net)) + ")");
            _collectButton.interactable = net > 0;
            _roleButton.interactable = true;
            var weakest = Weakest();
            SetLabel(_trainButton, Loc.T("Đào tạo", "Train", "研修") + (weakest != null ? " (" + Loc.Yen(BusinessWorkers.TrainCost(weakest)) + ")" : ""));
            _trainButton.interactable = weakest != null && weakest.level < BusinessWorkers.MaxLevel && b.capitalYen >= BusinessWorkers.TrainCost(weakest);
            SetLabel(_fireButton, Loc.T("Cho nghỉ việc", "Dismiss", "解雇"));
            _fireButton.interactable = Latest() != null;
            var sb = new System.Text.StringBuilder();
            if (b.workers == null || b.workers.Count == 0) sb.Append(Loc.T("Chưa có nhân viên nào. Chọn vị trí rồi bấm Tuyển.", "No staff yet. Pick a role, then hire.", "まだ社員はいません。役割を選んで雇いましょう。"));
            else
            {
                foreach (var w in b.workers) sb.AppendLine(BusinessWorkers.Describe(w));
                sb.Append(Loc.T("Đang chờ thu: ", "Waiting to collect: ", "回収待ち：") + Loc.Yen(gross) + " − " + Loc.T("lương ", "wages ", "給料 ") + Loc.Yen(wages) + " = " + Loc.Yen(net));
            }
            if (!string.IsNullOrEmpty(_message)) sb.Append("\n").Append(_message);
            _workersText.text = sb.ToString();
            _partnerButton.interactable = b.reputation >= 60;
        }

        private static void SetLabel(Button b, string text) { var l = b != null ? b.GetComponentInChildren<TMP_Text>(true) : null; if (l != null) l.text = text; }

        private void DisableWorkerButtons()
        {
            _roleButton.interactable = _collectButton.interactable = _trainButton.interactable = _fireButton.interactable = false;
            SetLabel(_roleButton, Loc.T("Vị trí", "Role", "役割"));
            SetLabel(_collectButton, Loc.T("Thu về", "Collect", "回収"));
            SetLabel(_trainButton, Loc.T("Đào tạo", "Train", "研修"));
            SetLabel(_fireButton, Loc.T("Cho nghỉ việc", "Dismiss", "解雇"));
            _workersText.text = _message;
        }

        private static NihongoLife.Data.WorkerRecord Weakest()
        {
            var ws = BusinessSystem.Instance != null && BusinessSystem.Instance.OwnsCompany ? BusinessSystem.Instance.Business.workers : null;
            return ws == null || ws.Count == 0 ? null : System.Linq.Enumerable.First(System.Linq.Enumerable.OrderBy(ws, x => x.level));
        }

        private static NihongoLife.Data.WorkerRecord Latest()
        {
            var ws = BusinessSystem.Instance != null && BusinessSystem.Instance.OwnsCompany ? BusinessSystem.Instance.Business.workers : null;
            return ws == null || ws.Count == 0 ? null : ws[ws.Count - 1];
        }

        private void OnHireClicked()
        {
            if (BusinessSystem.Instance != null)
            {
                var sys = BusinessSystem.Instance; if (!sys.OwnsCompany) { sys.TryOpenCompany("Mirai Learning"); RefreshUI(); return; } BusinessWorkers.Hire(sys, BusinessWorkers.Roles[_roleIndex].Id, Mathf.Max(25000, BusinessWorkers.LivingWage(sys.Business)));
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
