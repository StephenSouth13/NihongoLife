using System;
using UnityEngine;
using NihongoLife.Core;

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
        [Tooltip("Health back per second while fed and watered (doubled standing still): 0 → full in about 4 minutes.")]
        [SerializeField] private float healthRegenPerSecond = 0.4f;
        [Tooltip("Health back per point of food / drink eaten.")]
        [SerializeField] private float healthPerNourishment = 0.35f;

        /// <summary>Food and water at or above this keep the body healing.</summary>
        public const float HealthyNeed = 30f;

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
        public bool CanSprint => !IsExhausted && CurrentEnergy > 0f && Thirst > 0f;

        // ─────────── Penalties and weather (read by PlayerController, StatusDock, StatusConsequences) ───────────

        /// <summary>Below this share a need flashes red on the HUD and the warning says what is about to happen.</summary>
        public const float DangerThreshold = 0.15f;
        /// <summary>0 dry … 1 soaked: rises in the rain outdoors, dries under a roof.</summary>
        public float Wetness { get; private set; }
        /// <summary>Soaked for a while: a cold slowly costs health and makes you sleepy.</summary>
        public bool HasCold => Wetness >= 0.7f;
        /// <summary>Clear midday sun outdoors: thirst drains faster.</summary>
        public bool InHotSun { get; private set; }
        /// <summary>Seconds spent at 100 % sleepiness (passing out after <see cref="FallAsleepAfterSeconds"/>).</summary>
        public float NoddingOffSeconds { get; private set; }
        public const float FallAsleepAfterSeconds = 40f;
        /// <summary>Walking speed factor from penalties: hunger at 0 (×0.8), falling asleep (×0.7).</summary>
        public float MoveSpeedMultiplier => (Hunger <= 0f ? 0.8f : 1f) * (Sleepiness >= 100f ? 0.7f : 1f) * (_illness != null ? _illness.Move : 1f);
        private Health.PlayerIllness _illness;
        /// <summary>Raised when health reaches 0 (StatusConsequences plays the faint and the clinic).</summary>
        public event Action OnCollapsed;
        private bool _collapsed;
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
            if (GetComponent<StatusConsequences>() == null) gameObject.AddComponent<StatusConsequences>();
            if (GetComponent<Health.PlayerIllness>() == null) gameObject.AddComponent<Health.PlayerIllness>();
            PlayerName = PlayableCharacterCatalog.ResolvePlayerName(null);
            LoadProgress();
        }

        private void Update()
        {
            if (IntroTrailer.Playing) return; // the intro trailer films the town: needs do not drain, nobody faints
            if (Time.time - _lastReportTime > 0.25f) { _moving = false; _sprinting = false; } // locked / in a menu
            // Moving burns more food and water than standing; sprinting burns a lot more.
            float hungerRate = _sprinting ? 4f : _moving ? 1.5f : 1f;
            float thirstRate = _sprinting ? 5f : _moving ? 1.5f : 1f;
            if (_illness == null) _illness = GetComponent<Health.PlayerIllness>();
            bool ill = _illness != null;
            thirstRate *= ill ? _illness.Thirst : 1f;
            UpdateWeatherExposure();
            if (InHotSun) thirstRate *= 1.6f;
            Hunger = Mathf.Max(0f, Hunger - hungerDrainPerMinute * hungerRate * Time.deltaTime / 60f);
            Thirst = Mathf.Max(0f, Thirst - thirstDrainPerMinute * thirstRate * Time.deltaTime / 60f);
            Sleepiness = Mathf.Min(100f, Sleepiness + sleepinessGainPerMinute * (_sprinting ? 2f : 1f) * (HasCold ? 1.6f : 1f) * (ill ? _illness.Sleepiness : 1f) * Time.deltaTime / 60f);
            NoddingOffSeconds = Sleepiness >= 100f ? NoddingOffSeconds + Time.deltaTime : 0f;

            // Energy only comes back once the player has stopped sprinting for a moment; hungry or thirsty
            // bodies recover slower.
            float recoveryMultiplier = Mathf.Clamp01(Mathf.Min(Hunger, Thirst) / 25f);
            bool resting = !_sprinting && Time.time - _lastSprintTime >= recoveryDelayAfterSprint;
            if (resting && CurrentEnergy < maxEnergy && recoveryMultiplier > 0f)
            {
                float rate = _moving ? walkingEnergyRecoveryPerSecond : passiveEnergyRecoveryPerSecond;
                if (Core.WeatherCycle.PlayerInRain) rate *= 0.5f; // soaked: recovering is slower until you find shelter
                if (ill) rate *= _illness.Energy; // ill: the body recovers slower (Resources/Health/illnesses.json)
                CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + rate * recoveryMultiplier * Time.deltaTime);
            }
            if (CurrentEnergy <= 0f) IsExhausted = true;
            else if (IsExhausted && CurrentEnergy >= sprintAgainAtEnergy) IsExhausted = false;

            if (Hunger <= 0f || Thirst <= 0f)
            {
                CurrentHealth = Mathf.Max(0f, CurrentHealth - exhaustedHealthLossPerSecond * Time.deltaTime);
            }
            else if (!HasCold && !(ill && _illness.BlocksHealing) && !_collapsed && CurrentHealth > 0f && CurrentHealth < maxHealth && Hunger >= HealthyNeed && Thirst >= HealthyNeed) // at 0 you faint first
            {
                // Fed, watered and dry: the body heals by itself, faster when standing still or sitting.
                float heal = (_moving ? healthRegenPerSecond : healthRegenPerSecond * 2f) * (Sleepiness >= 100f ? 0.5f : 1f);
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + heal * Time.deltaTime);
            }
            if (HasCold) CurrentHealth = Mathf.Max(0f, CurrentHealth - 0.12f * Time.deltaTime);
            if (ill && _illness.HealthLossPerSecond > 0f) CurrentHealth = Mathf.Max(0f, CurrentHealth - _illness.HealthLossPerSecond * Time.deltaTime);
            if (CurrentHealth <= 0f && !_collapsed) { _collapsed = true; OnCollapsed?.Invoke(); }
            else if (CurrentHealth > 0f) _collapsed = false;

            SocialStanding.AdvanceHours(Time.deltaTime / 60f);
            _statusNotifyTimer += Time.deltaTime;
            if (_statusNotifyTimer >= 0.25f)
            {
                _statusNotifyTimer = 0f;
                float health01 = maxHealth <= 0f ? 1f : CurrentHealth / maxHealth;
                SocialStanding.ReflectNeeds(Hunger, Thirst, Sleepiness, CurrentEnergy, health01);
                OnStatusChanged?.Invoke();
            }
        }

        private void UpdateWeatherExposure()
        {
            var weather = Core.WeatherCycle.Instance;
            bool raining = Core.WeatherCycle.PlayerInRain;
            // About 45 s of rain soaks you; it takes about 90 s under a roof to dry.
            Wetness = Mathf.Clamp01(Wetness + (raining ? Time.deltaTime / 45f : -Time.deltaTime / 90f));
            var cycle = weather != null ? weather.GetComponent<Core.DayNightCycle>() : null;
            var outdoors = Core.OutdoorSky.Active;
            InHotSun = weather != null && cycle != null && outdoors != null && outdoors.DrivesAmbientAndFog && weather.Overcast < 0.2f &&
                       cycle.Hour >= 11f && cycle.Hour <= 15f && !Core.WeatherCycle.IsSheltered(transform.position);
        }

        /// <summary>A bump from the street. Does not write the save on every tick — the usual autosave still runs.</summary>
        public void ApplyHarm(float amount)
        {
            if (amount <= 0f || _collapsed) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            if (CurrentHealth <= 0f) { _collapsed = true; OnCollapsed?.Invoke(); }
            OnStatusChanged?.Invoke();
        }

        /// <summary>After fainting: the clinic patched you up (StatusConsequences applies time and fee).</summary>
        public void RecoverAfterCollapse()
        {
            CurrentHealth = Mathf.Max(CurrentHealth, maxHealth * 0.45f);
            CurrentEnergy = Mathf.Max(CurrentEnergy, maxEnergy * 0.5f);
            Hunger = Mathf.Max(Hunger, 35f);
            Thirst = Mathf.Max(Thirst, 40f);
            Sleepiness = Mathf.Min(Sleepiness, 40f);
            Wetness = 0f;
            NoddingOffSeconds = 0f;
            IsExhausted = false;
            _collapsed = false;
            OnStatusChanged?.Invoke();
            SaveProgress();
        }

        /// <summary>Seen by the doctor at Hibari Clinic: fully healed, the cold cured, rested enough to walk home.</summary>
        public void TreatAtClinic()
        {
            CurrentHealth = maxHealth;
            Wetness = 0f;
            Health.PlayerIllness.TreatAtClinic(); // each illness treated as a doctor would (flu antivirals within 48 h…)
            CurrentEnergy = Mathf.Max(CurrentEnergy, maxEnergy * 0.6f);
            IsExhausted = false;
            _collapsed = false;
            SocialStanding.AddMoodlet("treated", SocialMood.Happy, 0.4f, 3f);
            OnStatusChanged?.Invoke();
            SaveProgress();
        }

        /// <summary>Tests and debug: put the needs in a given state.</summary>
        public void DebugSetNeeds(float? health = null, float? energy = null, float? hunger = null, float? thirst = null, float? sleepiness = null, float? wetness = null)
        {
            if (health.HasValue) CurrentHealth = Mathf.Clamp(health.Value, 0f, maxHealth);
            if (energy.HasValue) CurrentEnergy = Mathf.Clamp(energy.Value, 0f, maxEnergy);
            if (hunger.HasValue) Hunger = Mathf.Clamp(hunger.Value, 0f, 100f);
            if (thirst.HasValue) Thirst = Mathf.Clamp(thirst.Value, 0f, 100f);
            if (sleepiness.HasValue) Sleepiness = Mathf.Clamp(sleepiness.Value, 0f, 100f);
            if (wetness.HasValue) Wetness = Mathf.Clamp01(wetness.Value);
            OnStatusChanged?.Invoke();
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
            amount = SocialStanding.ScaleXp(amount);
            int levelBefore = Level;
            TotalExp += amount;
            ApplyLevelFromTotal();
            for (int l = levelBefore; l < Level; l++) { maxHealth += 10f; CurrentHealth = maxHealth; }
            if (Level > levelBefore) NihongoLife.UI.HudFeed.Post(NihongoLife.Core.Loc.F("Lên cấp {0}!", "Level up! Level {0}", "レベル{0}になりました！", Level), NihongoLife.UI.HudFeed.Kind.Reward, 6f);
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
            // Eating or drinking shows on the body (hand to mouth), unless seated at a table.
            if (food > 0f || drink > 0f) GetComponent<CharacterAnimationController>()?.PlayGesture("UseItem", 1.6f);
            float hungerBefore = Hunger;
            Hunger = Mathf.Clamp(Hunger + food, 0f, 100f);
            Thirst = Mathf.Clamp(Thirst + drink, 0f, 100f);
            CurrentEnergy = Mathf.Clamp(CurrentEnergy + energy, 0f, maxEnergy);
            // A meal or a drink heals a little at once (the rest comes back over time while fed).
            float nourishment = Mathf.Max(0f, food) + Mathf.Max(0f, drink);
            if (nourishment > 0f && !_collapsed) CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + nourishment * healthPerNourishment);
            if (CurrentEnergy >= sprintAgainAtEnergy) IsExhausted = false;
            Health.PlayerIllness.Ate(hungerBefore, food); // eating when already full upsets the stomach
            OnStatusChanged?.Invoke();
            SaveProgress();
        }

        /// <summary>Medicine side effects (drowsy antihistamines) and the like.</summary>
        public void AddSleepiness(float amount)
        {
            Sleepiness = Mathf.Clamp(Sleepiness + amount, 0f, 100f);
            OnStatusChanged?.Invoke();
        }

        public void Sleep(float hours = 8f)
        {
            float recovery = Mathf.Clamp(hours / 8f, 0.25f, 1f) * 100f;
            Sleepiness = Mathf.Clamp(Sleepiness - recovery, 0f, 100f);
            CurrentEnergy = Mathf.Clamp(maxEnergy, 0f, maxEnergy);
            IsExhausted = false;
            // A full night (8 h) heals completely; a nap heals its share.
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + maxHealth * Mathf.Clamp(hours / 8f, 0.25f, 1f));
            _collapsed = false;
            SocialStanding.OnRested();
            Health.PlayerIllness.Slept(hours); // sleep ends a cold, aches, overwork, a headache
            OnStatusChanged?.Invoke();
            SaveProgress();
        }

        public void AddKnowledge(int amount)
        {
            if (amount <= 0) return;
            Knowledge += amount;
            SocialStanding.AddMoodlet("studied", SocialMood.Inspired, 0.4f, 1f);
            OnStatusChanged?.Invoke();
            SaveProgress();
            // Enough knowledge may open the next stage of the map (MapUnlockCardUI announces it).
            LocationUnlocks.Evaluate();
        }

        /// <summary>Tests: set the knowledge points directly (not saved).</summary>
        public void DebugSetKnowledge(int value)
        {
            Knowledge = Mathf.Max(0, value);
            OnStatusChanged?.Invoke();
        }

        public bool HasKnowledge(int required) => Knowledge >= Mathf.Max(0, required);

        private void LoadProgress()
        {
            if (!Core.GameServices.TryGet(out Save.IProgressRepository repository)) return;
            var progress = repository.GetProgress();
            if (progress == null) return;
            // Never the "Guest" placeholder of a guest save: see PlayableCharacterCatalog.ResolvePlayerName.
            PlayerName = PlayableCharacterCatalog.ResolvePlayerName(progress.displayName);
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
            WakeSafely();
            SocialStanding.Load(progress);
        }

        /// <summary>
        /// A save written while the player was starving, exhausted or fainted (a tab left open on the web, a cut-short
        /// blackout) must not faint them the moment the game opens: a loaded game starts awake, fed enough to heal.
        /// </summary>
        private void WakeSafely()
        {
            const float floor = HealthyNeed + 10f;
            Hunger = Mathf.Max(Hunger, floor);
            Thirst = Mathf.Max(Thirst, floor);
            Sleepiness = Mathf.Min(Sleepiness, 80f);
            CurrentHealth = Mathf.Max(CurrentHealth, maxHealth * 0.4f);
            if (CurrentEnergy <= 0f) { CurrentEnergy = maxEnergy * 0.3f; IsExhausted = false; }
            _collapsed = false;
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
            Core.DayNightCycle.Active?.WriteTo(progress);
            SocialStanding.WriteTo(progress);
            repository.SaveProgress(progress);
        }
    }
}
