using System;
using UnityEngine;

namespace NihongoLife.Player
{
    public class PlayerStatus : MonoBehaviour
    {
        public static PlayerStatus Instance { get; private set; }

        public string PlayerName { get; private set; } = "Học viên Nihongo";
        public int Level { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int MaxExp { get; private set; } = 100;
        
        [Header("Survival")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float hungerDrainPerMinute = 0.8f;
        [SerializeField] private float thirstDrainPerMinute = 1.25f;
        [SerializeField] private float passiveEnergyRecoveryPerSecond = 7f;
        [SerializeField] private float exhaustedHealthLossPerSecond = 1.5f;

        public float CurrentHealth { get; private set; } = 100f;
        public float MaxHealth => maxHealth;
        public float CurrentEnergy { get; private set; } = 100f;
        public float MaxEnergy => maxEnergy;
        public float Hunger { get; private set; } = 100f;
        public float Thirst { get; private set; } = 100f;
        public int Knowledge { get; private set; }
        public int CurrentStamina => Mathf.RoundToInt(CurrentEnergy);
        public int MaxStamina => Mathf.RoundToInt(MaxEnergy);

        public event Action OnStatusChanged;
        private float _statusNotifyTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            PlayerName = PlayableCharacterCatalog.GetPlayerName();
            LoadProgress();
        }

        private void Update()
        {
            Hunger = Mathf.Max(0f, Hunger - hungerDrainPerMinute * Time.deltaTime / 60f);
            Thirst = Mathf.Max(0f, Thirst - thirstDrainPerMinute * Time.deltaTime / 60f);

            float recoveryMultiplier = Mathf.Clamp01(Mathf.Min(Hunger, Thirst) / 25f);
            if (CurrentEnergy < maxEnergy && recoveryMultiplier > 0f)
            {
                CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + passiveEnergyRecoveryPerSecond * recoveryMultiplier * Time.deltaTime);
            }

            if (Hunger <= 0f || Thirst <= 0f)
            {
                CurrentHealth = Mathf.Max(0f, CurrentHealth - exhaustedHealthLossPerSecond * Time.deltaTime);
            }

            _statusNotifyTimer += Time.deltaTime;
            if (_statusNotifyTimer >= 0.25f)
            {
                _statusNotifyTimer = 0f;
                OnStatusChanged?.Invoke();
            }
        }

        public void SetPlayerName(string playerName)
        {
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? PlayableCharacterCatalog.DefaultPlayerName : playerName.Trim();
            PlayableCharacterCatalog.SavePlayerName(PlayerName);
            OnStatusChanged?.Invoke();
        }

        private void OnDestroy()
        {
            SaveProgress();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            CurrentExp += amount;
            AddKnowledge(amount);
            while (CurrentExp >= MaxExp)
            {
                CurrentExp -= MaxExp;
                Level++;
                MaxExp = Mathf.FloorToInt(MaxExp * 1.5f);
                maxHealth += 10f;
                CurrentHealth = maxHealth;
            }
            OnStatusChanged?.Invoke();
        }

        public void ConsumeStamina(int amount)
        {
            ConsumeEnergy(amount);
        }

        public bool ConsumeEnergy(float amount)
        {
            if (amount <= 0f) return true;
            if (CurrentEnergy <= 0f) return false;
            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
            OnStatusChanged?.Invoke();
            return CurrentEnergy > 0f;
        }

        public void RestoreNeeds(float food, float drink, float energy)
        {
            Hunger = Mathf.Clamp(Hunger + food, 0f, 100f);
            Thirst = Mathf.Clamp(Thirst + drink, 0f, 100f);
            CurrentEnergy = Mathf.Clamp(CurrentEnergy + energy, 0f, maxEnergy);
            OnStatusChanged?.Invoke();
            SaveProgress();
        }

        public void AddKnowledge(int amount)
        {
            if (amount <= 0) return;
            Knowledge += amount;
            OnStatusChanged?.Invoke();
            SaveProgress();
        }

        public bool HasKnowledge(int required) => Knowledge >= Mathf.Max(0, required);

        private void LoadProgress()
        {
            if (!Core.GameServices.TryGet(out Save.IProgressRepository repository)) return;
            var progress = repository.GetProgress();
            if (progress == null) return;
            Level = Mathf.Max(1, progress.level);
            CurrentExp = Mathf.Max(0, progress.xp);
            Knowledge = Mathf.Max(0, progress.knowledge > 0 ? progress.knowledge : progress.xp);
            CurrentHealth = Mathf.Clamp(progress.health <= 0f ? maxHealth : progress.health, 0f, maxHealth);
            CurrentEnergy = Mathf.Clamp(progress.energy <= 0f ? maxEnergy : progress.energy, 0f, maxEnergy);
            Hunger = Mathf.Clamp(progress.hunger <= 0f ? 100f : progress.hunger, 0f, 100f);
            Thirst = Mathf.Clamp(progress.thirst <= 0f ? 100f : progress.thirst, 0f, 100f);
        }

        private void SaveProgress()
        {
            if (!Core.GameServices.TryGet(out Save.IProgressRepository repository)) return;
            var progress = repository.GetProgress();
            if (progress == null) return;
            progress.level = Level;
            progress.xp = CurrentExp;
            progress.knowledge = Knowledge;
            progress.health = CurrentHealth;
            progress.energy = CurrentEnergy;
            progress.hunger = Hunger;
            progress.thirst = Thirst;
            repository.SaveProgress(progress);
        }
    }
}
