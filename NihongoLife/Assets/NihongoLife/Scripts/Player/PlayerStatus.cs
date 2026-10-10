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
        [SerializeField] private float hungerDrainPerMinute = 1.6f;
        [SerializeField] private float thirstDrainPerMinute = 2.4f;
        [SerializeField] private float sleepinessGainPerMinute = 0.65f;
        [SerializeField] private float passiveEnergyRecoveryPerSecond = 9f;
        [SerializeField] private float walkingEnergyRecoveryPerSecond = 4f;
        [SerializeField] private float recoveryDelayAfterSprint = 1.2f;
        [SerializeField] private float sprintAgainAtEnergy = 25f;
        [SerializeField] private float exhaustedHealthLossPerSecond = 1.5f;

        public float CurrentHealth { get; private set; } = 100f;
        public float MaxHealth => maxHealth;
        public float CurrentEnergy { get; private set; } = 100f;
        public float MaxEnergy => maxEnergy;
        public float Hunger { get; private set; } = 100f;
        public float Thirst { get; private set; } = 100f;
        public float Sleepiness { get; private set; }
        public float Restfulness => 100f - Sleepiness;
        public int Knowledge { get; private set; }
        public int CurrentStamina => Mathf.RoundToInt(CurrentEnergy);
        public int MaxStamina => Mathf.RoundToInt(MaxEnergy);

        public event Action OnStatusChanged;

        private bool _moving;
        private bool _sprinting;
        private float _lastSprintTime = -10f;
        private float _lastReportTime = -10f;

        /// <summary>True from the moment energy hits 0 until it has recovered to <c>sprintAgainAtEnergy</c>.</summary>
        public bool IsExhausted { get; private set; }
        public bool CanSprint => !IsExhausted && CurrentEnergy > 0f;
        public bool IsSprinting => _sprinting;

        /// <summary>Called every frame by the PlayerController so needs follow what the body is doing.</summary>
        public void ReportActivity(bool moving, bool sprinting)
        {
            _moving = moving;
            _sprinting = sprinting;
            _lastReportTime = Time.time;
            if (sprinting) _lastSprintTime = Time.time;
        }
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
            if (Time.time - _lastReportTime > 0.25f) { _moving = false; _sprinting = false; } // locked / in a menu
            // Moving burns more food and water than standing; sprinting burns a lot more.
            float hungerRate = _sprinting ? 4f : _moving ? 1.5f : 1f;
            float thirstRate = _sprinting ? 5f : _moving ? 1.5f : 1f;
            Hunger = Mathf.Max(0f, Hunger - hungerDrainPerMinute * hungerRate * Time.deltaTime / 60f);
            Thirst = Mathf.Max(0f, Thirst - thirstDrainPerMinute * thirstRate * Time.deltaTime / 60f);
            Sleepiness = Mathf.Min(100f, Sleepiness + sleepinessGainPerMinute * (_sprinting ? 2f : 1f) * Time.deltaTime / 60f);

            // Energy only comes back once the player has stopped sprinting for a moment; hungry or thirsty
            // bodies recover slower.
            float recoveryMultiplier = Mathf.Clamp01(Mathf.Min(Hunger, Thirst) / 25f);
            bool resting = !_sprinting && Time.time - _lastSprintTime >= recoveryDelayAfterSprint;
            if (resting && CurrentEnergy < maxEnergy && recoveryMultiplier > 0f)
            {
                float rate = _moving ? walkingEnergyRecoveryPerSecond : passiveEnergyRecoveryPerSecond;
                CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + rate * recoveryMultiplier * Time.deltaTime);
            }
            if (CurrentEnergy <= 0f) IsExhausted = true;
            else if (IsExhausted && CurrentEnergy >= sprintAgainAtEnergy) IsExhausted = false;

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

        /// <summary>
        /// Experience (general progression). Knowledge is separate (AddKnowledge). Level thresholds come from
        /// Resources/Progression/progression.json; progress.xp stores the total XP ever earned.
        /// </summary>
        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            int levelBefore = Level;
            TotalExp += amount;
            ApplyLevelFromTotal();
            for (int l = levelBefore; l < Level; l++) { maxHealth += 10f; CurrentHealth = maxHealth; }
            if (Level > levelBefore) NihongoLife.UI.HudFeed.Post($"Lên cấp {Level}!", NihongoLife.UI.HudFeed.Kind.Reward, 6f);
            SaveProgress();
            OnStatusChanged?.Invoke();
        }

        /// <summary>Total XP ever earned (saved as progress.xp).</summary>
        public int TotalExp { get; private set; }

        private void ApplyLevelFromTotal()
        {
            var table = NihongoLife.Progression.ProgressionCatalog.Load();
            Level = table.LevelFor(TotalExp);
            int floor = table.XpForLevel(Level);
            CurrentExp = TotalExp - floor;
            MaxExp = Mathf.Max(1, table.XpForLevel(Level + 1) - floor);
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
            if (CurrentEnergy <= 0f) IsExhausted = true;
            OnStatusChanged?.Invoke();
            return CurrentEnergy > 0f;
        }

        public bool TrySpendEnergy(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (CurrentEnergy < amount) return false;
            CurrentEnergy -= amount;
            if (CurrentEnergy <= 0f) IsExhausted = true;
            OnStatusChanged?.Invoke();
            SaveProgress();
            return true;
        }

        public void RestoreNeeds(float food, float drink, float energy)
        {
            Hunger = Mathf.Clamp(Hunger + food, 0f, 100f);
            Thirst = Mathf.Clamp(Thirst + drink, 0f, 100f);
            CurrentEnergy = Mathf.Clamp(CurrentEnergy + energy, 0f, maxEnergy);
            if (CurrentEnergy >= sprintAgainAtEnergy) IsExhausted = false;
            OnStatusChanged?.Invoke();
            SaveProgress();
        }

        public void Sleep(float hours = 8f)
        {
            float recovery = Mathf.Clamp(hours / 8f, 0.25f, 1f) * 100f;
            Sleepiness = Mathf.Clamp(Sleepiness - recovery, 0f, 100f);
            CurrentEnergy = Mathf.Clamp(maxEnergy, 0f, maxEnergy);
            IsExhausted = false;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + 8f);
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
            if (!string.IsNullOrWhiteSpace(progress.displayName)) PlayerName = progress.displayName;
            // progress.xp is the total XP. Older saves stored XP inside the level: never drop a level on load.
            var table = NihongoLife.Progression.ProgressionCatalog.Load();
            TotalExp = Mathf.Max(0, progress.xp);
            if (table.LevelFor(TotalExp) < progress.level) TotalExp = table.XpForLevel(Mathf.Max(1, progress.level)) + Mathf.Max(0, progress.xp);
            ApplyLevelFromTotal();
            Knowledge = Mathf.Max(0, progress.knowledge > 0 ? progress.knowledge : progress.xp);
            CurrentHealth = Mathf.Clamp(progress.health, 0f, maxHealth);
            CurrentEnergy = Mathf.Clamp(progress.energy, 0f, maxEnergy);
            Hunger = Mathf.Clamp(progress.hunger, 0f, 100f);
            Thirst = Mathf.Clamp(progress.thirst, 0f, 100f);
            IsExhausted = CurrentEnergy <= 0f;
            Sleepiness = Mathf.Clamp(progress.sleepiness, 0f, 100f);
        }

        public void ReloadProgress()
        {
            LoadProgress();
            OnStatusChanged?.Invoke();
        }

        private void SaveProgress()
        {
            if (!Core.GameServices.TryGet(out Save.IProgressRepository repository)) return;
            var progress = repository.GetProgress();
            if (progress == null) return;
            progress.level = Level;
            progress.xp = TotalExp;
            progress.displayName = PlayerName;
            progress.knowledge = Knowledge;
            progress.health = CurrentHealth;
            progress.energy = CurrentEnergy;
            progress.hunger = Hunger;
            progress.thirst = Thirst;
            progress.sleepiness = Sleepiness;
            repository.SaveProgress(progress);
        }
    }
}
