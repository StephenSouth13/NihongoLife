using System;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;
using UnityEngine;

namespace NihongoLife.Player
{
    public sealed class BusinessSystem : MonoBehaviour
    {
        public const int CompanyKnowledgeRequirement = 300;
        public const int CompanyCapitalRequirement = 50000;
        public event Action<string> OnBusinessMessage;
        public BusinessRecord Business { get; private set; }
        public bool OwnsCompany => Business != null && !string.IsNullOrWhiteSpace(Business.companyName);

        private void Awake() => Load();

        public bool TryOpenCompany(string companyName)
        {
            if (OwnsCompany) return false;
            if (PlayerStatus.Instance == null || PlayerStatus.Instance.Knowledge < CompanyKnowledgeRequirement)
                return Fail($"Can {CompanyKnowledgeRequirement} diem kien thuc de mo cong ty.");
            if (PlayerInventory.Instance == null || !PlayerInventory.Instance.SpendYen(CompanyCapitalRequirement))
                return Fail($"Can Y{CompanyCapitalRequirement} von khoi nghiep.");

            Business = new BusinessRecord
            {
                companyName = string.IsNullOrWhiteSpace(companyName) ? "Mirai Learning" : companyName.Trim(),
                capitalYen = CompanyCapitalRequirement,
                reputation = 10,
                fairWageScore = 50,
                educationScore = 10
            };
            Save();
            Publish("Thanh lap cong ty thanh cong. Tang truong phai di cung dao tao va luong cong bang.");
            return true;
        }

        public bool TryHireEmployee(int monthlyWage)
        {
            if (!OwnsCompany) return Fail("Ban chua co cong ty.");
            int livingWage = 24000 + Business.employeeCount * 1000;
            if (monthlyWage < livingWage)
            {
                Business.fairWageScore = Mathf.Max(0, Business.fairWageScore - 10);
                Business.reputation = Mathf.Max(0, Business.reputation - 5);
                Save();
                return Fail($"Muc luong chua dat chuan Y{livingWage}. Uy tin doanh nghiep bi giam.");
            }
            if (Business.capitalYen < monthlyWage) return Fail("Von doanh nghiep khong du tra ky luong dau tien.");
            Business.capitalYen -= monthlyWage;
            Business.employeeCount++;
            Business.fairWageScore = Mathf.Min(100, Business.fairWageScore + 4);
            Business.reputation += 3;
            Save();
            Publish("Tuyen nhan vien thanh cong. Da cam ket luong va dao tao.");
            return true;
        }

        public bool TryCreatePartnership(int requiredReputation = 60)
        {
            if (!OwnsCompany || Business.reputation < requiredReputation || Business.educationScore < 40)
                return Fail("Can uy tin va dong gop giao duc cao hon de lien doanh.");
            Business.partnershipCount++;
            Business.reputation += 5;
            Save();
            Publish("Lien doanh duoc ky ket. Hai ben chia se dao tao va trach nhiem cong dong.");
            return true;
        }

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
