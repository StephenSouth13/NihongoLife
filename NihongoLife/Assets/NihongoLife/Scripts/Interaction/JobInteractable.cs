using System;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Save;
using UnityEngine;

namespace NihongoLife.Interaction
{
    public enum JobRole
    {
        StoreClerk,
        StationAssistant,
        TeachingAssistant,
        JapaneseTeacher
    }

    public enum JobRank
    {
        Trainee,
        Employee,
        ShiftLeader,
        Manager
    }

    [RequireComponent(typeof(Collider))]
    public class JobInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private JobRole role;
        [SerializeField] private int requiredKnowledge = 20;
        [SerializeField] private int shiftPayYen = 450;
        [SerializeField] private int shiftKnowledge = 8;
        [SerializeField] private float shiftEnergyCost = 22f;
        [SerializeField] private float shiftCooldownSeconds = 60f;

        public string GetPromptJa() => EmploymentSystem.Instance != null && EmploymentSystem.Instance.IsEmployedAs(role)
            ? "仕事をする" : "応募する";
        public string GetpromptEn() => EmploymentSystem.Instance != null && EmploymentSystem.Instance.IsEmployedAs(role)
            ? "Lam ca lam viec" : "Ung tuyen";
        public Transform GetTransform() => transform;

        public void Configure(JobRole configuredRole, int knowledge, int pay, int knowledgeReward, float energyCost)
        {
            role = configuredRole;
            requiredKnowledge = Mathf.Max(0, knowledge);
            shiftPayYen = Mathf.Max(0, pay);
            shiftKnowledge = Mathf.Max(0, knowledgeReward);
            shiftEnergyCost = Mathf.Max(0f, energyCost);
        }

        public void Interact(GameObject player)
        {
            EmploymentSystem system = player.GetComponent<EmploymentSystem>() ?? player.AddComponent<EmploymentSystem>();
            if (!system.IsEmployedAs(role)) system.TryApply(role, requiredKnowledge);
            else system.TryWorkShift(role, shiftPayYen, shiftKnowledge, shiftEnergyCost, shiftCooldownSeconds);
        }
    }

    public class EmploymentSystem : MonoBehaviour
    {
        public static EmploymentSystem Instance { get; private set; }
        public event Action<string> OnEmploymentMessage;
        public JobRole? CurrentJob { get; private set; }
        public JobRank CurrentRank => CurrentJob.HasValue ? GetCareer(CurrentJob.Value).rank : JobRank.Trainee;
        private float _nextShiftTime;

        private sealed class CareerState
        {
            public JobRank rank;
            public int completedShifts;
            public int reputation;
        }

        private readonly System.Collections.Generic.Dictionary<JobRole, CareerState> _careers = new();

        private void Awake()
        {
            Instance = this;
            LoadProgress();
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public bool IsEmployedAs(JobRole role) => CurrentJob == role;

        public bool TryApply(JobRole role, int requiredKnowledge)
        {
            PlayerStatus status = PlayerStatus.Instance;
            if (status == null || !status.HasKnowledge(requiredKnowledge))
            {
                Publish($"Can {requiredKnowledge} diem kien thuc de ung tuyen {RoleName(role)}.");
                return false;
            }

            CurrentJob = role;
            GetCareer(role).rank = JobRank.Employee;
            SaveProgress();
            Publish($"Ung tuyen thanh cong: {RoleName(role)}.");
            return true;
        }

        public bool TryWorkShift(JobRole role, int pay, int knowledgeReward, float energyCost, float cooldown)
        {
            if (!IsEmployedAs(role) || Time.unscaledTime < _nextShiftTime) return false;
            if (PlayerStatus.Instance == null || !PlayerStatus.Instance.TrySpendEnergy(energyCost))
            {
                Publish("Khong du nang luong de bat dau ca lam.");
                return false;
            }

            PlayerInventory.Instance?.AddYen(pay);
            PlayerStatus.Instance.AddKnowledge(knowledgeReward);
            CareerState career = GetCareer(role);
            career.completedShifts++;
            career.reputation += 10;
            TryPromote(role, career);
            _nextShiftTime = Time.unscaledTime + Mathf.Max(1f, cooldown);
            SaveProgress();
            Publish($"Hoan thanh ca {RoleName(role)}: +Y{pay}, +{knowledgeReward} kien thuc.");
            return true;
        }

        private void TryPromote(JobRole role, CareerState career)
        {
            int knowledge = PlayerStatus.Instance != null ? PlayerStatus.Instance.Knowledge : 0;
            if (career.rank == JobRank.Employee && career.completedShifts >= 5 && career.reputation >= 50 && knowledge >= 80)
            {
                career.rank = JobRank.ShiftLeader;
                Publish($"Thang chuc truong ca: {RoleName(role)}.");
            }
            else if (career.rank == JobRank.ShiftLeader && career.completedShifts >= 15 && career.reputation >= 160 && knowledge >= 220)
            {
                career.rank = JobRank.Manager;
                Publish($"Thang chuc quan ly: {RoleName(role)}.");
            }
        }

        private CareerState GetCareer(JobRole role)
        {
            if (!_careers.TryGetValue(role, out CareerState career))
            {
                career = new CareerState();
                _careers.Add(role, career);
            }
            return career;
        }

        private void LoadProgress()
        {
            if (!GameServices.TryGet(out IProgressRepository repository)) return;
            PlayerProgressDto progress = repository.GetProgress();
            if (progress == null) return;
            if (Enum.TryParse(progress.activeJobRole, out JobRole activeRole)) CurrentJob = activeRole;
            if (progress.careers == null) return;
            foreach (CareerRecord record in progress.careers)
            {
                if (!Enum.TryParse(record.roleId, out JobRole role)) continue;
                _careers[role] = new CareerState
                {
                    rank = (JobRank)Mathf.Clamp(record.rank, 0, (int)JobRank.Manager),
                    completedShifts = Mathf.Max(0, record.completedShifts),
                    reputation = Mathf.Max(0, record.reputation)
                };
            }
        }

        private void SaveProgress()
        {
            if (!GameServices.TryGet(out IProgressRepository repository)) return;
            PlayerProgressDto progress = repository.GetProgress();
            if (progress == null) return;
            progress.activeJobRole = CurrentJob?.ToString() ?? string.Empty;
            progress.careers ??= new System.Collections.Generic.List<CareerRecord>();
            progress.careers.Clear();
            foreach (var pair in _careers)
            {
                progress.careers.Add(new CareerRecord
                {
                    roleId = pair.Key.ToString(),
                    rank = (int)pair.Value.rank,
                    completedShifts = pair.Value.completedShifts,
                    reputation = pair.Value.reputation
                });
            }
            repository.SaveProgress(progress);
        }

        private void Publish(string message)
        {
            Debug.Log("[Employment] " + message);
            OnEmploymentMessage?.Invoke(message);
        }

        private static string RoleName(JobRole role) => role switch
        {
            JobRole.StoreClerk => "nhan vien ban hang",
            JobRole.StationAssistant => "nhan vien nha ga",
            JobRole.TeachingAssistant => "tro giang",
            JobRole.JapaneseTeacher => "giao vien tieng Nhat",
            _ => role.ToString()
        };
    }
}
