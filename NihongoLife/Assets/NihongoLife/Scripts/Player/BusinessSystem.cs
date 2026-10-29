using System;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;
using UnityEngine;

namespace NihongoLife.Player
{
    public sealed class BusinessSystem : MonoBehaviour
    {
        public static BusinessSystem Instance { get; private set; }
        public const int CompanyKnowledgeRequirement = 300;
        public const int CompanyCapitalRequirement = 50000;
        public event Action<string> OnBusinessMessage;
        public BusinessRecord Business { get; private set; }
        public bool OwnsCompany => Business != null && !string.IsNullOrWhiteSpace(Business.companyName);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Load();
        }

        public bool TryOpenCompany(string companyName)
        {
            if (OwnsCompany) return false;
            // Offline exploration (StoryDirector.Bypass) skips the knowledge gate. The start-up capital still applies.
            if (!NihongoLife.Scenario.StoryDirector.Bypass && (PlayerStatus.Instance == null || PlayerStatus.Instance.Knowledge < CompanyKnowledgeRequirement))
                return Fail(Loc.F("Cần {0} điểm kiến thức để mở công ty.", "You need {0} knowledge points to start a company.", "会社を作るには知識{0}ポイントが必要です。", CompanyKnowledgeRequirement));
            if (PlayerInventory.Instance == null || !PlayerInventory.Instance.SpendYen(CompanyCapitalRequirement, NihongoLife.Core.Loc.Triple("Vốn mở công ty", "Company start-up capital", "会社の資本金")))
                return Fail(Loc.F("Cần ¥{0} vốn khởi nghiệp.", "You need ¥{0} of start-up capital.", "創業資金が¥{0}必要です。", CompanyCapitalRequirement.ToString("N0")));

            Business = new BusinessRecord
            {
                companyName = string.IsNullOrWhiteSpace(companyName) ? "Mirai Learning" : companyName.Trim(),
                capitalYen = CompanyCapitalRequirement,
                reputation = 10,
                fairWageScore = 50,
                educationScore = 10
            };
            Save();
            Publish(Loc.T("Thành lập công ty thành công. Tăng trưởng phải đi cùng đào tạo và lương công bằng.", "Company founded. Growth must go together with training and fair pay.", "会社を設立しました。成長には研修と公正な給料が必要です。"));
            return true;
        }

        public bool TryHireEmployee(int monthlyWage)
        {
            if (!OwnsCompany) return Fail(Loc.T("Bạn chưa có công ty.", "You don't have a company yet.", "まだ会社がありません。"));
            int livingWage = 24000 + Business.employeeCount * 1000;
            if (monthlyWage < livingWage)
            {
                Business.fairWageScore = Mathf.Max(0, Business.fairWageScore - 10);
                Business.reputation = Mathf.Max(0, Business.reputation - 5);
                Save();
                return Fail(Loc.F("Mức lương chưa đạt chuẩn ¥{0}. Uy tín doanh nghiệp bị giảm.", "The wage is below the ¥{0} living wage. Your reputation drops.", "給料が最低生活水準の¥{0}に届きません。信用が下がります。", livingWage.ToString("N0")));
            }
            if (Business.capitalYen < monthlyWage) return Fail(Loc.T("Vốn doanh nghiệp không đủ trả kỳ lương đầu tiên.", "The company cannot afford the first wage.", "最初の給料を払う資金が足りません。"));
            Business.capitalYen -= monthlyWage;
            Business.employeeCount++;
            Business.fairWageScore = Mathf.Min(100, Business.fairWageScore + 4);
            Business.reputation += 3;
            Save();
            Publish(Loc.T("Tuyển nhân viên thành công. Đã cam kết lương và đào tạo.", "Hired. Pay and training are promised.", "採用しました。給料と研修を約束しました。"));
            return true;
        }

        public bool TryCreatePartnership(int requiredReputation = 60)
        {
            if (!OwnsCompany || Business.reputation < requiredReputation || Business.educationScore < 40)
                return Fail(Loc.T("Cần uy tín và đóng góp giáo dục cao hơn để liên doanh.", "You need more reputation and education points to form a joint venture.", "共同事業には、より高い信用と教育ポイントが必要です。"));
            Business.partnershipCount++;
            Business.reputation += 5;
            Save();
            Publish(Loc.T("Liên doanh được ký kết. Hai bên chia sẻ đào tạo và trách nhiệm cộng đồng.", "Joint venture signed. Both sides share training and community duty.", "共同事業を結びました。研修と地域への責任を分かち合います。"));
            return true;
        }

        /// <summary>Writes the company to the save (used by <see cref="BusinessWorkers"/>).</summary>
        public void Persist() => Save();

        private void Load()
        {
            if (!GameServices.TryGet(out IProgressRepository repository)) { Business = new BusinessRecord(); return; }
            var progress = repository.GetProgress();
            Business = progress?.business ?? new BusinessRecord();
        }

        private void Save()
        {
            if (!GameServices.TryGet(out IProgressRepository repository)) return;
            var progress = repository.GetProgress();
            if (progress == null) return;
            progress.business = Business;
            repository.SaveProgress(progress);
        }

        private bool Fail(string message) { Publish(message); return false; }
        private void Publish(string message) { Debug.Log("[Business] " + message); OnBusinessMessage?.Invoke(message); }
    }
}
