using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Progression
{
    /// <summary>The player's career (saved in PlayerProgressDto.career). Empty careerId = no career.</summary>
    [Serializable]
    public sealed class CareerState
    {
        public string careerId = string.Empty;
        public string branchId = string.Empty;
        public int level;
        /// <summary>0–100: full = promotion interview.</summary>
        public float performance;
        public int shifts;
        public int lastShiftDay = -1;
        public int lastCheckedDay = -1;
        /// <summary>The player was in the game during today's working hours (a skipped shift then counts as missed).</summary>
        public bool presentAtWork;
        public int remindedDay = -1;
        /// <summary>The "choose a career" card was shown once at the unlock level.</summary>
        public bool prompted;
        public List<string> pastCareers = new List<string>();
        /// <summary>UTC ticks of the last change (cloud merge keeps the newest).</summary>
        public long updatedTicks;

        public bool HasCareer => !string.IsNullOrEmpty(careerId) && level > 0;
    }

    /// <summary>
    /// Careers from level 10, after The Sims: a ladder of titles with salaries, a weekly schedule, a performance bar
    /// raised by shifts worked in working hours (more in a good mood, less when tired or annoyed) and lowered by a
    /// missed workday, a promotion interview in Japanese when the bar is full, a branch half way up, and events at
    /// work with choices. The work itself is the part-time shift the game already has (progression.json); a career
    /// adds the salary, the title and the ladder on top. Guests (offline) and accounts both have careers.
    /// </summary>
    public static class CareerService
    {
        /// <summary>Tests only: the unlock level and the random work events are ignored.</summary>
        public static bool Bypass;
        /// <summary>Chance of a work event after a good shift (tests set 0 or 1).</summary>
        public static float EventChance = 0.35f;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Changed = null; Bypass = false; EventChance = 0.35f; _interviewing = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            QuestService.Completed -= OnQuestCompleted;
            QuestService.Completed += OnQuestCompleted;
        }

        public static CareerCatalog Catalog => CareerCatalog.Load();

        private static PlayerProgressDto Progress =>
            GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;

        public static CareerState State
        {
            get
            {
                var progress = Progress;
                if (progress == null) return null;
                progress.career ??= new CareerState();
                return progress.career;
            }
        }

        public static CareerDefinition Current => State is { HasCareer: true } s ? Catalog.Career(s.careerId) : null;
        public static CareerLevelDefinition CurrentLevel => Current?.Level(State.level, State.branchId);
        public static string CurrentTitle => CurrentLevel?.Title;

        public static int PlayerLevel => PlayerStatus.Instance != null ? PlayerStatus.Instance.Level : ProgressionCatalog.Load().LevelFor(Progress?.xp ?? 0);
        public static bool Unlocked => Bypass || PlayerLevel >= Catalog.unlockLevel;

        private static void Save()
        {
            var state = State;
            if (state != null) state.updatedTicks = DateTime.UtcNow.Ticks;
            if (GameServices.TryGet(out IProgressRepository repository)) repository.SaveProgress(repository.GetProgress());
            Changed?.Invoke();
        }

        private static int Today => DayNightCycle.Active != null ? DayNightCycle.Active.Day : Progress?.calendarDay ?? 0;
        private static float Hour => DayNightCycle.Active != null ? DayNightCycle.Active.Hour : Progress?.calendarHour ?? 12f;

        public static bool IsWorkDay(CareerDefinition career, int day) => career != null && career.IsWorkDay(GameCalendar.DateOf(day).DayOfWeek);
        public static bool IsWorkTime(CareerDefinition career, int day, float hour) => IsWorkDay(career, day) && hour >= career.workStart && hour < career.workEnd;
        public static bool WorkingHoursNow => IsWorkTime(Current, Today, Hour);

        // ─────────── Choosing and leaving ───────────

        /// <summary>Starts a career at its first level. Null on success, else why not.</summary>
        public static string Choose(string careerId)
        {
            var career = Catalog.Career(careerId);
            var state = State;
            if (career == null || state == null) return Loc.T("Không tìm thấy nghề này.", "Career not found.", "その職業は見つかりません。");
            if (!Unlocked) return Loc.F("Mở khi đạt cấp {0} (bạn đang cấp {1}).", "Opens at level {0} (you are level {1}).", "レベル{0}で開きます（今はレベル{1}）。", Catalog.unlockLevel, PlayerLevel);
            if (state.HasCareer) return Loc.T("Bạn đang có nghề — nghỉ việc trước khi đổi nghề.", "You already have a career — quit first to change.", "今の仕事をやめてから変えましょう。");
            state.careerId = career.id;
            state.branchId = string.Empty;
            state.level = 1;
            state.performance = 0f;
            state.shifts = 0;
            state.lastShiftDay = -1;
            state.prompted = true;
            Save();
            HudFeed.Post(Loc.F("Bạn đã vào nghề {0}: {1} ở {2}. Lịch làm: {3}.", "You joined {0}: {1} at {2}. Schedule: {3}.", "{0}の仕事を始めました：{2}の{1}。勤務：{3}。",
                career.Name, career.levels[0].Title, career.Workplace, career.Schedule), HudFeed.Kind.Reward, 7f);
            NihongoLife.Audio.Soundscape.Jingle("unlock");
            return null;
        }

        public static void Quit()
        {
            var state = State;
            if (state == null || !state.HasCareer) return;
            var career = Current;
            if (!state.pastCareers.Contains(state.careerId)) state.pastCareers.Add(state.careerId);
            state.careerId = state.branchId = string.Empty;
            state.level = 0;
            state.performance = 0f;
            Save();
            HudFeed.Post(Loc.F("Bạn đã nghỉ việc ở {0}.", "You left your job at {0}.", "{0}を辞めました。", career?.Workplace ?? ""), HudFeed.Kind.Info, 5f);
        }

        // ─────────── Shifts, performance, promotion ───────────

        /// <summary>Good mood works better (×1.25), tired / tense / annoyed / embarrassed worse (×0.75).</summary>
        public static float MoodFactor => SocialStanding.Dominant switch
        {
            SocialMood.Happy or SocialMood.Confident or SocialMood.Inspired or SocialMood.Proud => 1.25f,
            SocialMood.Tired or SocialMood.Tense or SocialMood.Annoyed or SocialMood.Embarrassed => 0.75f,
            _ => 1f,
        };

        private static void OnQuestCompleted(QuestDefinition quest)
        {
            var career = Current;
            if (career == null || quest == null || quest.id != career.shiftQuest) return;
            ShiftDone(Today, Hour);
        }

        /// <summary>A shift of the career's job finished at <paramref name="day"/> / <paramref name="hour"/>.</summary>
        public static void ShiftDone(int day, float hour)
        {
            var career = Current;
            var state = State;
            var level = CurrentLevel;
            if (career == null || level == null) return;
            bool onTime = IsWorkTime(career, day, hour);
            bool firstToday = state.lastShiftDay != day;
            float step = level.shiftsToPromote > 0 ? 100f / level.shiftsToPromote : 0f;
            float gain = step * MoodFactor * (onTime ? 1f : 0.5f);
            state.performance = Mathf.Clamp(state.performance + gain, 0f, 100f);
            state.shifts++;
            if (onTime) state.lastShiftDay = day;
            Save();
            if (onTime && firstToday && level.salary > 0)
                PlayerInventory.Instance?.AddYen(level.salary, Loc.Triple("Lương: " + level.titleVi, "Salary: " + level.titleEn, "給料：" + level.titleJa));
            HudFeed.Post(onTime
                ? Loc.F("Ca làm đúng giờ · {0} · hiệu suất {1:0}%{2}", "On-time shift · {0} · performance {1:0}%{2}", "時間どおりの勤務 · {0} · 評価 {1:0}%{2}", level.Title, state.performance, firstToday && level.salary > 0 ? "  +" + Loc.Yen(level.salary) : "")
                : Loc.F("Ca ngoài giờ làm ({0}): hiệu suất chỉ tăng một nửa, không có lương nghề.", "Shift outside working hours ({0}): half the performance, no salary.", "勤務時間外のシフト（{0}）：評価は半分、給料なし。", career.Schedule),
                HudFeed.Kind.Reward, 6f, "career-shift");

            var events = career.events;
            if (onTime && events.Length > 0 && UnityEngine.Random.value < EventChance) ShowEvent(events[UnityEngine.Random.Range(0, events.Length)], () => TryOfferPromotion());
            else TryOfferPromotion();
        }

        /// <summary>Why the next promotion is not possible yet (null when the interview can happen).</summary>
        public static string PromotionBlocker()
        {
            var career = Current;
            var state = State;
            var level = CurrentLevel;
            if (career == null || level == null) return Loc.T("Chưa có nghề.", "No career yet.", "まだ仕事がありません。");
            if (state.level >= career.MaxLevel || level.shiftsToPromote <= 0) return Loc.T("Bạn đã ở bậc cao nhất.", "You are at the top.", "最高のポストです。");
            if (state.performance < 99.5f) return Loc.F("Hiệu suất {0:0}% / 100%.", "Performance {0:0}% / 100%.", "評価 {0:0}% / 100%。", state.performance);
            int knowledge = PlayerStatus.Instance != null ? PlayerStatus.Instance.Knowledge : Progress?.knowledge ?? 0;
            if (knowledge < level.knowledgeToPromote) return Loc.F("Cần {0} trí tuệ để phỏng vấn thăng chức (bạn có {1}).", "Needs {0} knowledge for the promotion interview (you have {1}).", "昇進面接には知識{0}が必要です（今は{1}）。", level.knowledgeToPromote, knowledge);
            return null;
        }

        private static bool _interviewing;

        /// <summary>Opens the promotion interview when the bar is full and nothing blocks it.</summary>
        public static bool TryOfferPromotion()
        {
            if (_interviewing || Current == null || State.performance < 99.5f) return false;
            string blocker = PromotionBlocker();
            if (blocker != null) { HudFeed.Post(blocker, HudFeed.Kind.Info, 6f, "career-promotion"); return false; }
            var career = Current;
            var state = State;
            var interview = career.Interview(state.level + 1, state.branchId);
            if (interview == null) { Promote(null); return true; } // no interview written: promote straight away
            _interviewing = true;
            var choices = interview.choices.Select(c => new JobQuizCard.Choice
            {
                Id = c.branch ?? c.ja,
                Label = c.ja,
                Detail = c.Gloss,
                Correct = c.correct,
                WrongHint = c.Hint,
            }).ToList();
            string next = interview.toLevel == career.branchAtLevel && career.branches.Length > 0
                ? Loc.T("chọn hướng đi", "choose your path", "コースを選ぶ")
                : career.Level(interview.toLevel, state.branchId)?.Title;
            JobQuizCard.Show(Loc.F("PHỎNG VẤN THĂNG CHỨC → {0}", "PROMOTION INTERVIEW → {0}", "昇進面接 → {0}", next), career.BossName,
                interview.lineJa, interview.readingJa, interview.Meaning,
                Loc.T("Chọn câu trả lời đúng (sai thì được giải thích và thử lại).", "Pick the right answer (a wrong one is explained; try again).", "正しい答えを選びましょう（まちがえたら説明のあと、もう一度）。"),
                choices, chosen => { _interviewing = false; Promote(interview.toLevel == career.branchAtLevel ? chosen?.Id : null); });
            // Closed without an answer: the bar stays full, the interview comes back after the next shift.
            CareerClock.WatchInterview(() => _interviewing = false);
            return true;
        }

        /// <summary>Moves up one level (choosing <paramref name="branchId"/> at the branch level).</summary>
        public static void Promote(string branchId)
        {
            var career = Current;
            var state = State;
            if (career == null || state.level >= career.MaxLevel) return;
            if (state.level + 1 == career.branchAtLevel && career.branches.Length > 0)
                state.branchId = career.Branch(branchId) != null ? branchId : career.branches[0].id;
            state.level++;
            state.performance = 0f;
            Save();
            var level = CurrentLevel;
            var branch = career.Branch(state.branchId);
            HudFeed.Post(Loc.F("Thăng chức! Bạn là {0}{1} · lương {2}/ca", "Promoted! You are now {0}{1} · salary {2}/shift", "昇進！ {0}{1} · 給料 {2}/シフト",
                level?.Title, branch != null && state.level == career.branchAtLevel ? " (" + branch.Name + ")" : "", Loc.Yen(level?.salary ?? 0)), HudFeed.Kind.Reward, 8f);
            SocialStanding.AddReputation(5, "promotion");
            SocialStanding.AddMoodlet("promoted", SocialMood.Proud, 0.8f, 6f);
            NihongoLife.Audio.Soundscape.Jingle("level_up");
        }

        /// <summary>A work event (Sims 4 style): every answer is allowed and has its consequence.</summary>
        public static void ShowEvent(CareerPromptDefinition evt, Action then = null)
        {
            if (evt == null) { then?.Invoke(); return; }
            var choices = evt.choices.Select((c, i) => new JobQuizCard.Choice { Id = i.ToString(), Label = c.ja, Detail = c.Gloss, Correct = true }).ToList();
            JobQuizCard.Show(Loc.T("TÌNH HUỐNG Ở NƠI LÀM", "AT WORK", "仕事のできごと"), Current?.Workplace ?? "", evt.lineJa, evt.readingJa, evt.Meaning,
                Loc.T("Bạn sẽ nói gì?", "What do you say?", "なんと言いますか。"), choices, chosen =>
                {
                    int index = int.TryParse(chosen?.Id, out int i) ? i : 0;
                    ApplyEventChoice(evt.choices[Mathf.Clamp(index, 0, evt.choices.Length - 1)]);
                    then?.Invoke();
                });
        }

        public static void ApplyEventChoice(CareerChoiceDefinition choice)
        {
            var state = State;
            if (state == null || choice == null) return;
            state.performance = Mathf.Clamp(state.performance + choice.performance, 0f, 100f);
            if (choice.reputation > 0) SocialStanding.AddReputation(choice.reputation, "work");
            else if (choice.reputation < 0) SocialStanding.AddNotoriety(-choice.reputation, "work");
            if (!string.IsNullOrEmpty(choice.mood) && Enum.TryParse(choice.mood, out SocialMood mood)) SocialStanding.AddMoodlet("work_event", mood, 0.5f, 3f);
            Save();
            HudFeed.Post(Loc.F("Hiệu suất {0}{1} → {2:0}%", "Performance {0}{1} → {2:0}%", "評価 {0}{1} → {2:0}%", choice.performance >= 0 ? "+" : "", choice.performance, state.performance),
                choice.performance >= 0 ? HudFeed.Kind.Reward : HudFeed.Kind.Warning, 5f, "career-event");
        }

        // ─────────── The working day (CareerClock ticks this) ───────────

        /// <summary>Day rollover and reminders. A workday you spent in the game during working hours without a shift costs performance.</summary>
        public static void Tick(int day, float hour, bool inGame)
        {
            var state = State;
            var career = Current;
            if (state == null || career == null) return;
            if (state.lastCheckedDay < 0) { state.lastCheckedDay = day; return; }
            if (day > state.lastCheckedDay)
            {
                int previous = state.lastCheckedDay;
                bool missed = IsWorkDay(career, previous) && state.presentAtWork && state.lastShiftDay != previous;
                state.lastCheckedDay = day;
                state.presentAtWork = false;
                if (missed)
                {
                    state.performance = Mathf.Max(0f, state.performance - Catalog.missedShiftPenalty);
                    HudFeed.Post(Loc.F("Hôm qua bạn bỏ ca ở {0}: hiệu suất −{1} → {2:0}%.", "You missed yesterday's shift at {0}: performance −{1} → {2:0}%.", "昨日の{0}のシフトを休みました：評価 −{1} → {2:0}%。",
                        career.Workplace, Catalog.missedShiftPenalty, state.performance), HudFeed.Kind.Warning, 7f, "career-missed");
                }
                Save();
            }
            if (inGame && IsWorkTime(career, day, hour) && !state.presentAtWork) { state.presentAtWork = true; Save(); }
            if (inGame && IsWorkDay(career, day) && state.remindedDay != day && hour >= career.workStart - 1f && hour < career.workEnd && state.lastShiftDay != day)
            {
                state.remindedDay = day;
                Save();
                HudFeed.Post(Loc.F("Hôm nay có ca: {0} ở {1}, {2}:00–{3}:00. Nhận ca trong Sổ nhiệm vụ (N).", "Shift today: {0} at {1}, {2}:00–{3}:00. Take it in the Task journal (N).", "今日はシフト：{1}で{0}、{2}:00–{3}:00。タスク帳（N）で受けましょう。",
                    CurrentTitle, career.Workplace, career.workStart, career.workEnd), HudFeed.Kind.Info, 7f, "career-reminder");
            }
        }
    }

    /// <summary>Ticks the career once a second in gameplay scenes and shows the career card once at the unlock level.</summary>
    public sealed class CareerClock : MonoBehaviour
    {
        private static CareerClock _instance;
        private float _next;
        private Action _onInterviewClosed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("CareerClock");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CareerClock>();
        }

        /// <summary>Runs <paramref name="onClosed"/> once the interview card is gone (answered or closed).</summary>
        public static void WatchInterview(Action onClosed) { if (_instance != null) _instance._onInterviewClosed = onClosed; }

        public static bool InGameplay
        {
            get
            {
                string scene = SceneManager.GetActiveScene().name;
                return PlayerStatus.Instance != null && !IntroTrailer.Playing && scene != "01_MainMenu" && scene != "00_Bootstrap" && scene != IntroTrailer.SceneName;
            }
        }

        private void Update()
        {
            if (_onInterviewClosed != null && !JobQuizCard.IsOpen) { var done = _onInterviewClosed; _onInterviewClosed = null; done(); }
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            var cycle = DayNightCycle.Active;
            bool inGame = InGameplay;
            if (cycle != null) CareerService.Tick(cycle.Day, cycle.Hour, inGame);
            PromptAtUnlockLevel(inGame);
        }

        /// <summary>Tests turn the one-time "choose a career" card off (PlayModeSaveGuard) unless they test it.</summary>
        public static bool PromptEnabled = true;

        private static void PromptAtUnlockLevel(bool inGame)
        {
            var state = CareerService.State;
            if (!PromptEnabled || !inGame || state == null || state.prompted || state.HasCareer || !CareerService.Unlocked) return;
            if (UiModalStack.AnyOpen || JobQuizCard.IsOpen || NihongoLife.Dialogue.DialogueManager.Instance is { IsOpen: true }) return;
            state.prompted = true;
            if (GameServices.TryGet(out IProgressRepository repository)) repository.SaveProgress(repository.GetProgress());
            HudFeed.Post(Loc.F("Cấp {0}! Giờ bạn có thể chọn nghề nghiệp.", "Level {0}! You can now choose a career.", "レベル{0}！ 職業を選べるようになりました。", CareerService.Catalog.unlockLevel), HudFeed.Kind.Reward, 6f);
            CareerUI.Open();
        }
    }
}
