using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;
using UnityEngine;

namespace NihongoLife.Player
{
    public class CareerSystem : MonoBehaviour
    {
        public static CareerSystem Instance { get; private set; }
        
        public event Action<string> OnCareerMessage;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public bool WorkShift(string roleId)
        {
            // 1. Energy check (Require 30 Energy to work)
            if (PlayerStatus.Instance == null || PlayerStatus.Instance.CurrentEnergy < 30f)
            {
                Publish("Bạn quá mệt để đi làm. Hãy về nhà ngủ (Energy < 30).");
                return false;
            }

            if (!GameServices.TryGet(out IProgressRepository repo)) return false;
            var progress = repo.GetProgress();
            
            var career = progress.careers.FirstOrDefault(c => c.roleId == roleId);
            if (career == null)
            {
                career = new CareerRecord { roleId = roleId, rank = 1, completedShifts = 0, reputation = 0 };
                progress.careers.Add(career);
            }

            // 2. Consume Stats
            PlayerStatus.Instance.ConsumeEnergy(30f);
            PlayerStatus.Instance.RestoreNeeds(-20f, -10f, 0f); // Deduct hunger and thirst
            
            // 3. Reward Calculation (Base Salary + Rank Bonus)
            int baseSalary = roleId == "Kombini" ? 1000 : (roleId == "Restaurant" ? 1500 : 800);
            int bonus = career.rank * 200;
            int totalEarned = baseSalary + bonus;

            // 4. Update Progression
            career.completedShifts++;
            if (career.completedShifts >= 5) 
            {
                career.rank++;
                career.completedShifts = 0; // Reset shifts for next rank
                Publish($"Chúc mừng! Bạn đã thăng chức nghề {roleId} lên Cấp {career.rank}!");
            }

            // 5. Save Data securely
            if (PlayerInventory.Instance != null) 
            {
                PlayerInventory.Instance.AddYen(totalEarned); 
            }
            
            // Re-fetch progress because AddYen saved it, we need to append career info
            progress = repo.GetProgress();
            var careerToSave = progress.careers.FirstOrDefault(c => c.roleId == roleId);
            if (careerToSave == null) 
            { 
                careerToSave = career; 
                progress.careers.Add(careerToSave); 
            }
            careerToSave.completedShifts = career.completedShifts;
            careerToSave.rank = career.rank;
            repo.SaveProgress(progress);

            Publish($"Bạn đã hoàn thành ca làm {roleId}. Nhận được Y{totalEarned}.");
            return true;
        }

        private void Publish(string message) 
        { 
            Debug.Log("[CareerSystem] " + message); 
            OnCareerMessage?.Invoke(message); 
        }
    }
}
