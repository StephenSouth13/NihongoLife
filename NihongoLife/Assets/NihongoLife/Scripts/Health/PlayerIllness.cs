using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.Save;
using NihongoLife.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Health
{
    // ─────────── Saved state (PlayerProgressDto.illness) ───────────

    [Serializable]
    public sealed class IllnessRecord
    {
        public string id;
        /// <summary>Absolute game hours (day × 24 + hour) when it started and when it ends untreated.</summary>
        public float since, until;
        /// <summary>Over-the-counter medicine taken (symptoms relieved), seen by the doctor.</summary>
        public bool relieved, treated;
    }

    [Serializable]
    public sealed class ImmunityRecord
    {
        public string id;
        public float until;
    }

    [Serializable]
    public sealed class HealthRecord
    {
        public List<IllnessRecord> illnesses = new List<IllnessRecord>();
        /// <summary>Vaccinations ("flu") and a mask worn today ("mask").</summary>
        public List<ImmunityRecord> immunities = new List<ImmunityRecord>();
        /// <summary>Illnesses the player has had (the handbook marks them).</summary>
        public List<string> history = new List<string>();
        public int workDay = -1, shiftsToday, sprintDay = -1;
        public float sprintSeconds;
        public long updatedTicks;
    }

    /// <summary>
    /// The player's illnesses (Resources/Health/illnesses.json). Catching them follows what the player does — soaked in
    /// the rain (cold), crowded places in autumn/winter (flu), spring outdoors without a mask (hay fever), two or three
    /// shifts in a day or a lot of sprinting (muscle ache, overwork), the midday sun with little water (heatstroke),
    /// eating when full (indigestion), staying up at full sleepiness (headache). While ill, PlayerStatus applies
    /// <see cref="Move"/> / <see cref="Energy"/> / <see cref="Sleepiness"/> / <see cref="Thirst"/> /
    /// <see cref="HealthLossPerSecond"/>, careers <see cref="WorkFactor"/>; the screen tints (IllnessOverlay) and the
    /// character sneezes or coughs. Treatment is realistic: medicine eases, the doctor treats, sleep and time heal.
    /// Lives on the player beside PlayerStatus; state is saved in PlayerProgressDto.illness.
    /// </summary>
    public sealed class PlayerIllness : MonoBehaviour
    {
        public static PlayerIllness Instance { get; private set; }
        /// <summary>False in tests (PlayModeSaveGuard): nothing is caught by itself; Catch() still works.</summary>
        public static bool Triggers = true;
        public static event Action Changed;

        private const float CheckInterval = 0.5f, ContagionInterval = 30f;
        private float _nextCheck, _nextContagion, _soaked, _heat, _sleepy, _shadeRecovery, _nextSymptom;
        private PlayerStatus _status;

        public float Move { get; private set; } = 1f;
        public float Energy { get; private set; } = 1f;
        public float Sleepiness { get; private set; } = 1f;
        public float Thirst { get; private set; } = 1f;
        public float Work { get; private set; } = 1f;
        public float HealthLossPerSecond { get; private set; }
        public bool BlocksHealing { get; private set; }

        public static IllnessCatalog Catalog => IllnessCatalog.Load();
        /// <summary>Work performance factor for careers (1 when healthy or no player).</summary>
        public static float WorkFactor => Instance != null ? Instance.Work : 1f;

        // ─────────── Clock and state ───────────

        /// <summary>Absolute game hours: day × 24 + hour (1 game hour = 1 real minute).</summary>
        public static float Now
        {
            get
            {
                var cycle = DayNightCycle.Active;
                if (cycle != null) return cycle.Day * 24f + cycle.Hour;
                var p = Progress;
                return p != null ? p.calendarDay * 24f + p.calendarHour : 0f;
            }
        }

        private static int Today => Mathf.FloorToInt(Now / 24f);

        private static PlayerProgressDto Progress => GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;

        public static HealthRecord State
        {
            get
            {
                var p = Progress;
                if (p == null) return null;
                return p.illness ??= new HealthRecord();
            }
        }

        public static IReadOnlyList<IllnessRecord> Active => (IReadOnlyList<IllnessRecord>)State?.illnesses ?? Array.Empty<IllnessRecord>();

        public static bool Has(string id) => State?.illnesses.Any(i => i.id == id) == true;

        public static bool IsImmune(string id) => State?.immunities.Any(i => i.id == id && i.until > Now) == true;

        public static float ImmuneUntil(string id) => State?.immunities.Where(i => i.id == id).Select(i => i.until).DefaultIfEmpty(0f).Max() ?? 0f;

        /// <summary>The illness that matters most now (the one the HUD shows and the doctor diagnoses).</summary>
        public static IllnessDefinition Main
        {
            get
            {
                var state = State;
                if (state == null || state.illnesses.Count == 0) return null;
                return state.illnesses.Select(r => Catalog.Illness(r.id)).Where(d => d != null)
                    .OrderByDescending(d => d.healthPerMinute + (1f - d.move) * 20f + (1f - d.work) * 10f).FirstOrDefault();
            }
        }

        private static void Save()
        {
            var state = State;
            if (state != null) state.updatedTicks = DateTime.UtcNow.Ticks;
            if (GameServices.TryGet(out IProgressRepository repository)) repository.SaveProgress(repository.GetProgress());
            if (Instance != null) Instance.Recompute();
            Changed?.Invoke();
        }

        // ─────────── Catching, curing ───────────

        /// <summary>Starts an illness (null = refused: unknown, already ill with it, or protected).</summary>
        public static IllnessRecord Catch(string id, bool announce = true)
        {
            var def = Catalog.Illness(id);
            var state = State;
            if (def == null || state == null || Has(id) || IsImmune(id)) return null;
            float now = Now;
            var record = new IllnessRecord { id = id, since = now, until = now + def.hours };
            state.illnesses.Add(record);
            if (!state.history.Contains(id)) state.history.Add(id);
            if (Enum.TryParse(def.mood, out SocialMood mood)) SocialStanding.AddMoodlet("ill_" + id, mood, 0.6f, Mathf.Min(def.hours, 12f));
            Save();
            if (announce)
                HudFeed.Post(Loc.F("Bạn bị {0} ({1})! {2}", "You have {0} ({1})! {2}", "{1}になりました！ {2}", def.Name, def.kanji, def.sayJa),
                    HudFeed.Kind.Warning, 7f, "illness-" + id);
            return record;
        }

        public static bool Cure(string id, bool announce = true)
        {
            var state = State;
            if (state == null || state.illnesses.RemoveAll(i => i.id == id) == 0) return false;
            Save();
            var def = Catalog.Illness(id);
            if (announce && def != null) HudFeed.Post(Loc.F("Đã khỏi {0}. お大事に！", "{0} is over. お大事に！", "{0}が治りました。", def.Name), HudFeed.Kind.Reward, 5f, "illness-" + id);
            return true;
        }

        public static void CureAll()
        {
            var state = State;
            if (state == null || state.illnesses.Count == 0) return;
            state.illnesses.Clear();
            Save();
        }

        public static void Immunize(string id, float hours)
        {
            var state = State;
            if (state == null || hours <= 0f) return;
            state.immunities.RemoveAll(i => i.id == id);
            state.immunities.Add(new ImmunityRecord { id = id, until = Now + hours });
            Save();
        }

        /// <summary>Over-the-counter medicine (or a mask) from the bag. Returns what it did, for the HUD.</summary>
        public static string TakeMedicine(string medicineId)
        {
            var medicine = Catalog.Medicine(medicineId);
            var state = State;
            if (medicine == null || state == null) return null;
            if (medicineId == "mask")
            {
                Immunize("mask", 24f);
                return Loc.T("Đã đeo khẩu trang (1 ngày): chặn phấn hoa, ít lây cúm hơn.", "Mask on (one day): no pollen, less flu.", "マスクをつけました（1日）。");
            }
            float now = Now;
            var helped = new List<string>();
            foreach (var record in state.illnesses)
            {
                var def = Catalog.Illness(record.id);
                if (def == null || !def.medicines.Contains(medicineId)) continue;
                record.relieved = true;
                record.until -= Mathf.Max(0f, record.until - now) * def.medicineShorten;
                helped.Add(def.Name);
            }
            if (medicine.drowsy && PlayerStatus.Instance != null) PlayerStatus.Instance.AddSleepiness(15f);
            Save();
            if (helped.Count == 0) return Loc.F("{0}: không đúng bệnh — thuốc không giúp gì.", "{0}: not for what you have — it doesn't help.", "{0}：今の症状には効きません。", medicine.Name);
            return Loc.F("{0}: đỡ triệu chứng {1}. Vẫn cần nghỉ ngơi.", "{0}: eases {1}. You still need rest.", "{0}：{1}の症状が楽になりました。", medicine.Name, string.Join(", ", helped))
                   + (medicine.drowsy ? Loc.T(" (gây buồn ngủ)", " (makes you drowsy)", "（眠くなります）") : "");
        }

        /// <summary>The doctor's treatment: each illness is shortened as in real practice (flu antivirals work best
        /// in the first 48 h, heatstroke is cured by a drip). Returns the illness diagnosed (or null).</summary>
        public static IllnessDefinition TreatAtClinic()
        {
            var state = State;
            if (state == null || state.illnesses.Count == 0) return null;
            var main = Main;
            float now = Now;
            foreach (var record in state.illnesses.ToList())
            {
                var def = Catalog.Illness(record.id);
                if (def == null) continue;
                bool early = def.earlyHours > 0f && now - record.since <= def.earlyHours;
                float shorten = early ? def.earlyClinicShorten : def.clinicShorten;
                record.treated = record.relieved = true;
                if (shorten >= 0.999f) state.illnesses.Remove(record);
                else record.until -= Mathf.Max(0f, record.until - now) * shorten;
            }
            Save();
            return main;
        }

        /// <summary>A night's sleep: ends the illnesses that sleep cures.</summary>
        public static void Slept(float hours)
        {
            var state = State;
            if (state == null || hours < 6f) return;
            int before = state.illnesses.Count;
            state.illnesses.RemoveAll(r => Catalog.Illness(r.id)?.sleepCures == true);
            if (state.illnesses.Count != before) Save();
        }

        /// <summary>Hit by a car: a bruise (first aid RICE, a patch, the clinic for an X-ray).</summary>
        public static void Hit()
        {
            if (Triggers && !Has("bruise")) Catch("bruise");
        }

        /// <summary>Food eaten when already full: indigestion.</summary>
        public static void Ate(float hungerBefore, float food)
        {
            if (Triggers && food > 0f && hungerBefore >= 92f) Catch("stomach");
        }

        /// <summary>A job shift finished: two in a day ache, three exhaust; working while contagious costs reputation.</summary>
        public static void ShiftWorked()
        {
            var state = State;
            if (state == null) return;
            int today = Today;
            if (state.workDay != today) { state.workDay = today; state.shiftsToday = 0; }
            state.shiftsToday++;
            Save();
            if (state.illnesses.Any(r => Catalog.Illness(r.id)?.stayHome == true))
            {
                SocialStanding.AddNotoriety(4, "work_while_contagious");
                HudFeed.Post(Loc.T("Đi làm khi đang bị cúm: dễ lây cho đồng nghiệp — ở Nhật nên nghỉ ở nhà. Danh tiếng giảm.",
                    "Working with flu spreads it to your colleagues — in Japan you stay home. Reputation down.",
                    "インフルエンザで出勤すると周りにうつります。家で休みましょう。"), HudFeed.Kind.Warning, 7f, "illness-work");
            }
            if (!Triggers) return;
            if (state.shiftsToday >= 3) Catch("overwork");
            else if (state.shiftsToday >= 2) Catch("muscle_ache");
        }

        // ─────────── The component ───────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _status = GetComponent<PlayerStatus>();
            QuestService.Completed += OnQuestCompleted;
            Recompute();
            IllnessOverlay.Ensure();
        }

        private void OnDestroy()
        {
            QuestService.Completed -= OnQuestCompleted;
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Changed = null; Instance = null; }

        private static void OnQuestCompleted(QuestDefinition quest)
        {
            if (quest != null && quest.id != null && quest.id.StartsWith("job_", StringComparison.Ordinal)) ShiftWorked();
        }

        private void Update()
        {
            if (IntroTrailer.Playing || _status == null) return;
            if (_status.IsSprinting && Triggers) SprintTick(Time.deltaTime);
            if (Time.time < _nextCheck) return;
            float dt = CheckInterval + Mathf.Max(0f, Time.time - _nextCheck);
            _nextCheck = Time.time + CheckInterval;
            Expire();
            Recompute(); // the save can change under us (account switch, cloud merge): cheap, a handful of records
            if (Triggers) CheckTriggers(dt);
            Symptoms();
        }

        private void SprintTick(float dt)
        {
            var state = State;
            if (state == null) return;
            int today = Today;
            if (state.sprintDay != today) { state.sprintDay = today; state.sprintSeconds = 0f; }
            state.sprintSeconds += dt;
            if (state.sprintSeconds >= 180f && !Has("muscle_ache")) Catch("muscle_ache");
        }

        private void Expire()
        {
            var state = State;
            if (state == null) return;
            float now = Now;
            foreach (var record in state.illnesses.Where(r => r.until <= now).ToList()) Cure(record.id);
            if (state.immunities.RemoveAll(i => i.until <= now) > 0) Save();

            // Heatstroke eases in the shade with enough water (first aid), even untreated.
            if (Has("heatstroke"))
            {
                _shadeRecovery = !_status.InHotSun && _status.Thirst >= 60f ? _shadeRecovery + CheckInterval : 0f;
                if (_shadeRecovery >= 60f) { _shadeRecovery = 0f; Cure("heatstroke"); }
            }
        }

        private void CheckTriggers(float dt)
        {
            _soaked = _status.Wetness >= 0.7f ? _soaked + dt : 0f;
            if (_soaked >= 25f) { _soaked = 0f; Catch("cold"); }
            _heat = _status.InHotSun && _status.Thirst < 35f ? _heat + dt : 0f;
            if (_heat >= 40f) { _heat = 0f; Catch("heatstroke"); }
            _sleepy = _status.Sleepiness >= 95f ? _sleepy + dt : 0f;
            if (_sleepy >= 60f) { _sleepy = 0f; Catch("headache"); }

            if (Time.time < _nextContagion) return;
            _nextContagion = Time.time + ContagionInterval;
            string scene = SceneManager.GetActiveScene().name;
            string season = GameCalendar.SeasonOn(Today).ToString().ToLowerInvariant();
            bool masked = IsImmune("mask");
            bool outdoors = OutdoorSky.Active != null && OutdoorSky.Active.DrivesAmbientAndFog && !WeatherCycle.IsSheltered(transform.position);
            foreach (var def in Catalog.illnesses)
            {
                if (def.chance <= 0f || !def.scenes.Contains(scene) || !def.seasons.Contains(season) || Has(def.id) || IsImmune(def.id)) continue;
                if (def.category == "allergy" && (masked || !outdoors)) continue;
                float chance = def.chance * (_status.CurrentHealth < _status.MaxHealth * 0.4f ? 2f : 1f) * (masked ? 0.2f : 1f);
                if (UnityEngine.Random.value < chance) Catch(def.id);
            }
        }

        /// <summary>Combines every illness (eased by medicine) into the body's multipliers.</summary>
        private void Recompute()
        {
            float move = 1f, energy = 1f, sleepiness = 1f, thirst = 1f, work = 1f, loss = 0f;
            bool block = false;
            foreach (var record in Active)
            {
                var def = Catalog.Illness(record.id);
                if (def == null) continue;
                float keep = record.relieved ? 1f - def.relief : 1f; // the share of the symptoms still felt
                move *= Ease(def.move, keep);
                energy *= Ease(def.energy, keep);
                sleepiness *= Ease(def.sleepiness, keep);
                thirst *= Ease(def.thirst, keep);
                work *= Ease(def.work, keep);
                loss += def.healthPerMinute / 60f * keep;
                block |= !def.heals;
            }
            Move = move; Energy = energy; Sleepiness = sleepiness; Thirst = thirst; Work = work;
            HealthLossPerSecond = loss; BlocksHealing = block;
        }

        private static float Ease(float factor, float keep) => 1f + (factor - 1f) * keep;

        private void Symptoms()
        {
            if (Time.time < _nextSymptom) return;
            var main = Main;
            if (main == null) { _nextSymptom = Time.time + 5f; return; }
            _nextSymptom = Time.time + UnityEngine.Random.Range(22f, 38f);
            string line = main.effect switch
            {
                "sneeze" => "ハクション！",
                "cough" => "ゴホッ、ゴホッ…",
                "fever" => "うう… ねつが…",
                "heat" => "あつい… くらくらする…",
                "dizzy" => "あたまが いたい…",
                _ => "いたた…",
            };
            SymptomBubble.Show(transform, line, main.Tint);
            if (Enum.TryParse(main.mood, out SocialMood mood)) KidExpression.On(gameObject).Play(mood, 2.5f);
        }
    }
}
