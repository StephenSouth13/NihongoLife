using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Course;
using NihongoLife.Core;
using NihongoLife.StudyModes.Plan;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Personal learning plan: pick a language (Japanese, English, Russian or IELTS), the current level, a goal and a
    /// daily study time; the plan lists every phase with the in-game content to use, week ranges, milestones and an
    /// estimated finish (Docs/LEARNING_PLAN.md). Logged study time moves the progress bar and re-estimates the finish.
    /// Built entirely in code (no scene edits).
    /// </summary>
    public sealed class LearningPlanPopup : MenuPopupBase
    {
        private const float W = 1320f, H = 820f, LW = 420f;
        private RectTransform _form, _main;
        private ScrollRect _mainScroll;
        private TextMeshProUGUI _status;
        private LearningPlanState _state;
        private RectTransform _overlay;
        private List<CourseQuestion> _checkQs;
        private int _checkIndex = -1;
        private CourseQuizResult _checkResult;

        /// <summary>The running checkpoint test (null when none).</summary>
        public CourseQuizView Checkpoint { get; private set; }
        /// <summary>Last record written by a passed checkpoint.</summary>
        public MilestoneRecord LastRecord { get; private set; }
        /// <summary>What the overlay shows: "", "quiz", "result", "certificate", "gallery".</summary>
        public string OverlayMode { get; private set; } = "";
        public string LastExportPath { get; private set; }
        /// <summary>Tests set the player name printed on certificates.</summary>
        public Func<string> PlayerName = () => NihongoLife.Player.PlayableCharacterCatalog.GetPlayerName();

        public LearningPlanState State => _state ??= LearningPlanStore.Load();
        public LearningPlan Plan { get; private set; }
        /// <summary>Tests set the clock.</summary>
        public Func<DateTime> UtcNow = () => DateTime.UtcNow;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);
        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Lộ trình học cá nhân"; en = "My learning plan"; ja = "わたしの学習プラン"; }

        protected override void Build(RectTransform card)
        {
            _form = UiKit.Rect(card, "Form");
            UiKit.Place(_form, 32f, 96f, LW, H - 128f);
            var right = UiKit.Rect(card, "Timeline");
            UiKit.Place(right, LW + 56f, 96f, W - LW - 88f, H - 128f);
            _main = UiKit.Scroll(right, "Scroll", out _mainScroll, 12f, new RectOffset(4, 16, 4, 24));
            _overlay = UiKit.Rect(card, "Overlay");
            UiKit.Place(_overlay, 24f, 88f, W - 48f, H - 112f);
            _overlay.gameObject.SetActive(false);
        }

        protected override void OnOpenUpdate() { Checkpoint?.Tick(Time.unscaledDeltaTime); }

        protected override bool HandleEscape()
        {
            if (OverlayMode == "") return false;
            CloseOverlay();
            return true;
        }

        protected override void OnOpened()
        {
            _state = LearningPlanStore.Load();
            Plan = State.HasPlan ? SafeBuild() : null;
            Rebuild();
        }

        protected override void OnLanguageApplied()
        {
            if (!IsOpen) return;
            Rebuild();
            if (OverlayMode == "quiz") Checkpoint?.Draw();
            else if (OverlayMode == "result") ShowResult();
            else if (OverlayMode == "certificate" && LastRecord != null) ShowCertificate(LastRecord);
            else if (OverlayMode == "gallery") ShowGallery();
        }

        private LearningPlan SafeBuild()
        {
            try { return State.Build(); }
            catch (ArgumentException) { return null; }
        }

        // ── actions (public for tests) ──

        public void SetTrack(PlanTrack t)
        {
            State.track = t;
            var levels = LearningPlanner.Levels(t);
            if (!levels.Contains(State.fromLevel)) State.fromLevel = levels[0];
            if (!levels.Contains(State.goalLevel)) State.goalLevel = t == PlanTrack.Japanese ? "N3" : t == PlanTrack.Ielts ? "6.5" : "B1";
            Rebuild();
        }
        public void SetFrom(string level) { State.fromLevel = level; Rebuild(); }
        public void SetGoal(string level) { State.goalLevel = level; Rebuild(); }
        public void SetMinutes(int minutes) { State.minutesPerDay = minutes; Rebuild(); }

        /// <summary>Builds the plan from the form and starts tracking from today.</summary>
        public LearningPlan Generate()
        {
            var levels = LearningPlanner.Levels(State.track);
            if (Array.IndexOf(levels, State.goalLevel) <= Array.IndexOf(levels, State.fromLevel))
            {
                Plan = null;
                Rebuild();
                if (_status != null) _status.text = Loc.T("Mục tiêu phải cao hơn trình độ hiện tại.", "The goal must be above your current level.", "目標は今のレベルより上にしてください。");
                return null;
            }
            State.startedUtcTicks = UtcNow().Ticks;
            State.minutesLogged = 0;
            State.doneLevels.Clear();
            LearningPlanStore.Save(State);
            Plan = State.Build();
            Rebuild();
            return Plan;
        }

        public void LogMinutes(int minutes)
        {
            if (!State.HasPlan || minutes <= 0) return;
            State.minutesLogged += minutes;
            if (Plan != null)
                foreach (var p in Plan.phases)
                    if (State.HoursDone >= p.cumulativeHours - 1e-6 && !State.doneLevels.Contains(p.levelId)) State.doneLevels.Add(p.levelId);
            LearningPlanStore.Save(State);
            Rebuild();
        }

        public void ResetPlan()
        {
            State.startedUtcTicks = 0; State.minutesLogged = 0; State.doneLevels.Clear();
            LearningPlanStore.Save(State);
            Plan = null;
            Rebuild();
        }

        public int RemainingDays() => Plan == null ? 0 : LearningPlanner.RemainingDays(Plan, State.HoursDone, State.DaysElapsed(UtcNow()));

        // ── view ──

        private static string TrackName(PlanTrack t) => t switch
        {
            PlanTrack.Japanese => Loc.T("Tiếng Nhật (JLPT)", "Japanese (JLPT)", "日本語（JLPT）"),
            PlanTrack.English => Loc.T("Tiếng Anh (CEFR)", "English (CEFR)", "英語（CEFR）"),
            PlanTrack.Russian => Loc.T("Tiếng Nga (TORFL)", "Russian (TORFL)", "ロシア語（TORFL）"),
            _ => "IELTS",
        };

        private static string LevelName(PlanTrack t, string level)
        {
            if (level == "0") return Loc.T("Chưa biết gì", "Total beginner", "ゼロから");
            if (t == PlanTrack.Ielts) return "Band " + level;
            return level;
        }

        private void Rebuild()
        {
            if (_form == null) return;
            BuildForm();
            BuildTimeline();
        }

        private void BuildForm()
        {
            UiKit.Clear(_form);
            float y = 0f;
            Heading(ref y, Loc.T("1. Ngôn ngữ", "1. Language", "1. 言語"));
            var tracks = (PlanTrack[])Enum.GetValues(typeof(PlanTrack));
            Grid(ref y, tracks.Select(t => (TrackName(t), State.track == t, (Action)(() => SetTrack(t)), "Track_" + t)).ToArray(), 2, 44f);
            var levels = LearningPlanner.Levels(State.track);
            Heading(ref y, Loc.T("2. Trình độ hiện tại", "2. Current level", "2. 今のレベル"));
            Grid(ref y, levels.Take(levels.Length - 1).Select(l => (LevelName(State.track, l), State.fromLevel == l, (Action)(() => SetFrom(l)), "From_" + l)).ToArray(), 4, 36f);
            Heading(ref y, Loc.T("3. Mục tiêu", "3. Goal", "3. 目標"));
            Grid(ref y, levels.Skip(1).Select(l => (l == LearningPlanner.MasterLevel(State.track) ? LevelName(State.track, l) + " ★" : LevelName(State.track, l), State.goalLevel == l, (Action)(() => SetGoal(l)), "Goal_" + l)).ToArray(), 4, 36f);
            var master = UiKit.Label(_form, "MasterHint", Loc.T("★ = thành thạo (C2 / N1 / band 9)", "★ = mastery (C2 / N1 / band 9)", "★ = 習得（C2 / N1 / バンド9）"), 12.5f, UiTheme.Muted);
            UiKit.Place(master, 0f, y, LW, 20f); y += 26f;
            Heading(ref y, Loc.T("4. Thời gian học mỗi ngày", "4. Study time per day", "4. 1日の学習時間"));
            Grid(ref y, LearningPlanner.DailyMinutes.Select(m => (Loc.F("{0} phút", "{0} min", "{0}分", m), State.minutesPerDay == m, (Action)(() => SetMinutes(m)), "Min_" + m)).ToArray(), 4, 40f);
            y += 8f;
            var go = UiKit.Button(_form, "Generate", Loc.T("Tạo lộ trình", "Build my plan", "プランを作る"), () => Generate(), UiKit.Kind.Primary, "route", 17f);
            UiKit.Place((RectTransform)go.transform, 0f, y, LW, 50f); y += 60f;
            _status = UiKit.Label(_form, "Status", "", 13.5f, UiTheme.Warn, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            UiKit.Place(_status, 0f, y, LW, 60f);
        }

        private void Heading(ref float y, string text)
        {
            var l = UiKit.Label(_form, "H_" + text.GetHashCode(), text, 15f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(l, 0f, y, LW, 24f);
            y += 28f;
        }

        private void Grid(ref float y, (string label, bool on, Action act, string name)[] cells, int cols, float h)
        {
            const float gap = 8f;
            float w = (LW - gap * (cols - 1)) / cols;
            for (int i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                var b = UiKit.Button(_form, c.name, c.label, c.act, c.on ? UiKit.Kind.Primary : UiKit.Kind.Secondary, null, 14f);
                UiKit.Place((RectTransform)b.transform, (i % cols) * (w + gap), y + (i / cols) * (h + gap), w, h);
            }
            y += ((cells.Length + cols - 1) / cols) * (h + gap) + 6f;
        }

        private void BuildTimeline()
        {
            UiKit.Clear(_main);
            if (Plan == null || Plan.phases.Count == 0)
            {
                var intro = ReferenceUi.Panel(_main, "Intro", UiTheme.Surface);
                ReferenceUi.Text(intro, "IntroTitle", Loc.T("Lộ trình từ con số 0 đến mục tiêu của bạn", "A plan from zero to your goal", "ゼロから目標までのプラン"), 22f, UiTheme.Gold, FontStyles.Bold);
                ReferenceUi.Text(intro, "IntroBody", Loc.T(
                    "Chọn ngôn ngữ, trình độ, mục tiêu và thời gian mỗi ngày. Game sẽ xếp các bài học có sẵn thành từng chặng, có mốc theo tuần và thời gian dự kiến. Số giờ dựa trên số liệu công bố (Cambridge English, Trung tâm Giáo dục tiếng Nhật) và chỉ là ước tính.",
                    "Pick a language, your level, a goal and a daily time. The game orders the lessons it already has into phases with weekly milestones and an estimated duration. Hours come from published figures (Cambridge English, Japanese Language Education Center) and are estimates only.",
                    "言語・レベル・目標・1日の時間を選ぶと、ゲーム内の教材を段階に分け、週ごとの目標と予想期間を示します。時間は公表データ（Cambridge English、日本語教育センター）に基づく目安です。"), 15f, UiTheme.TextSoft);
                return;
            }

            DateTime now = UtcNow();
            DateTime start = State.HasPlan ? new DateTime(State.startedUtcTicks, DateTimeKind.Utc) : now;
            int elapsed = State.DaysElapsed(now);
            double done = State.HoursDone, expected = LearningPlanner.ExpectedHours(Plan, elapsed);
            int left = RemainingDays();

            var sum = ReferenceUi.Panel(_main, "Summary", UiTheme.Surface);
            ReferenceUi.Text(sum, "SummaryTitle", Loc.F("{0}: {1} → {2}", "{0}: {1} → {2}", "{0}：{1} → {2}", TrackName(Plan.track), LevelName(Plan.track, Plan.fromLevel), LevelName(Plan.track, Plan.goalLevel)), 22f, UiTheme.Gold, FontStyles.Bold);
            ReferenceUi.Text(sum, "SummaryEstimate", Loc.F("Ước tính ~{0} giờ · {1} phút/ngày · khoảng {2} tuần (đến {3:dd/MM/yyyy})",
                "Estimated ~{0} hours · {1} min/day · about {2} weeks (until {3:dd/MM/yyyy})",
                "目安 約{0}時間・1日{1}分・約{2}週間（{3:yyyy/MM/dd}まで）",
                Math.Round(Plan.totalHours), Plan.minutesPerDay, Plan.totalWeeks, start.AddDays(Plan.totalWeeks * 7).ToLocalTime()), 16f, UiTheme.Text);
            float overall = (float)Math.Min(1.0, done / Math.Max(1e-6, Plan.totalHours));
            var bar = UiKit.ProgressBar(sum, "PlanProgress", UiTheme.Teal, UiKit.Alpha(UiTheme.Text, 0.12f), 9);
            UiKit.Size((RectTransform)bar.parent, -1f, 18f);
            AnimatedBar.Attach(bar, overall);
            ReferenceUi.Text(sum, "OverallPercent", Loc.F("{0}% \u00B7 {1:0.#} / {2:0} gi\u1EDD", "{0}% \u00B7 {1:0.#} / {2:0} h", "{0}% \u00B7 {1:0.#} / {2:0}\u6642\u9593", Mathf.RoundToInt(overall * 100f), done, Plan.totalHours), 15f, UiTheme.Teal, FontStyles.Bold);
            ReferenceUi.Text(sum, "SummaryProgress", Loc.F("Đã học {0:0.#} / {1:0} giờ · kế hoạch đến hôm nay: {2:0.#} giờ",
                "Studied {0:0.#} / {1:0} h · plan by today: {2:0.#} h",
                "学習 {0:0.#} / {1:0}時間・今日までの計画：{2:0.#}時間", done, Plan.totalHours, expected), 14.5f, UiTheme.TextSoft);
            string pace = done + 1e-6 >= expected ? Loc.T("Đúng hoặc nhanh hơn kế hoạch", "On track or ahead", "計画どおり、または先行") : Loc.T("Chậm hơn kế hoạch", "Behind the plan", "計画より遅れ");
            ReferenceUi.Text(sum, "SummaryEta", left == 0
                ? Loc.T("Bạn đã hoàn thành lộ trình!", "You finished the plan!", "プラン完了！")
                : Loc.F("{0} · dự kiến xong sau {1} ngày (theo tốc độ thực tế)", "{0} · estimated finish in {1} days (from your real pace)", "{0}・実際のペースであと{1}日", pace, left), 15f, UiTheme.Teal, FontStyles.Bold);
            var actions = ReferenceUi.Row(sum, "Log", 8f);
            UiKit.Size(actions, -1f, 42f);
            foreach (int m in new[] { 15, 30, 60 })
                UiKit.Size(UiKit.Button(actions, "Log_" + m, Loc.F("+{0} phút đã học", "+{0} min studied", "+{0}分 学習", m), () => LogMinutes(m), UiKit.Kind.Secondary, "timer", 14f), 170f, 40f);
            UiKit.Size(UiKit.Button(actions, "Badges", Loc.T("Huy hi\u1EC7u", "Badges", "\u30D0\u30C3\u30B8"), ShowGallery, UiKit.Kind.Accent, "workspace_premium", 14f), 140f, 40f);
            UiKit.Size(UiKit.Button(actions, "Reset", Loc.T("Làm lại", "Start over", "やり直す"), ResetPlan, UiKit.Kind.Quiet, "restart_alt", 14f), 130f, 40f);
            ReferenceUi.Text(sum, "Disclaimer", Loc.T("Ước tính dựa trên giờ học có hướng dẫn đã công bố; thời gian thực tế tùy người học. Không phải tư vấn chính thức của IELTS/JLPT/TORFL.",
                "Estimates use published guided-learning hours; real time varies by learner. Not official IELTS/JLPT/TORFL guidance.",
                "公表された学習時間に基づく目安で、実際は人により異なります。IELTS/JLPT/TORFLの公式情報ではありません。"), 12f, UiTheme.Muted);

            var current = Plan.PhaseAtHours(done);
            int n = 0;
            foreach (var p in Plan.phases)
            {
                int idx = n;
                n++;
                bool unlocked = MilestoneCheckpoint.IsUnlocked(Plan, State, idx);
                var best = MilestoneCheckpoint.Best(State, Plan.track, p.levelId);
                bool finished = done >= p.cumulativeHours - 1e-6;
                bool now_ = !finished && p == current;
                Color tone = finished ? UiTheme.Teal : now_ ? UiTheme.Gold : UiTheme.Muted;
                var panel = ReferenceUi.Panel(_main, "Phase_" + p.levelId, now_ ? UiKit.Alpha(UiTheme.Gold, 0.12f) : UiTheme.Surface);
                var head = ReferenceUi.Row(panel, "Head", 10f);
                UiKit.Size(head, -1f, 30f);
                UiKit.Chip(head, "Week", Loc.F("Tuần {0}–{1}", "Weeks {0}–{1}", "{0}〜{1}週目", p.startWeek, p.endWeek), UiKit.Alpha(tone, 0.3f), UiTheme.Text, finished ? "check_circle" : now_ ? "flag" : "radio_button_unchecked", 13f);
                UiKit.Chip(head, "Hours", Loc.F("~{0} giờ", "~{0} h", "約{0}時間", Math.Round(p.hours)), UiKit.Alpha(tone, 0.2f), UiTheme.Text, "schedule", 13f);
                if (best != null) UiKit.Chip(head, "Passed", "\u2605 " + best.Percent + "%", UiKit.Alpha(UiTheme.Gold, 0.35f), UiTheme.Text, "workspace_premium", 13f);
                else if (!unlocked) UiKit.Chip(head, "Locked", Loc.T("\u0110ang kho\u00E1", "Locked", "\u30ED\u30C3\u30AF\u4E2D"), UiKit.Alpha(UiTheme.Muted, 0.25f), UiTheme.TextSoft, "lock", 13f);
                ReferenceUi.Text(panel, "Title", $"{n}. {LevelName(Plan.track, p.levelId)} — {p.Milestone}", 18f, tone, FontStyles.Bold);
                double from = p.cumulativeHours - p.hours;
                var pb = UiKit.ProgressBar(panel, "PhaseProgress", tone, UiKit.Alpha(UiTheme.Text, 0.1f));
                UiKit.Size((RectTransform)pb.parent, -1f, 10f);
                float pf = Mathf.Clamp01((float)((done - from) / Math.Max(1e-6, p.hours)));
                AnimatedBar.Attach(pb, pf);
                ReferenceUi.Text(panel, "PhasePercent", Loc.F("{0}% \u00B7 {1:0.#} / {2:0} gi\u1EDD", "{0}% \u00B7 {1:0.#} / {2:0} h", "{0}% \u00B7 {1:0.#} / {2:0}\u6642\u9593", Mathf.RoundToInt(pf * 100f), Math.Max(0.0, Math.Min(p.hours, done - from)), p.hours), 13f, tone);
                foreach (var item in p.items)
                {
                    var row = ReferenceUi.Row(panel, "Item", 8f);
                    UiKit.Size(row, -1f, 34f);
                    var t = ReferenceUi.Text(row, "Text", "• " + item.Text, 14.5f, UiTheme.TextSoft);
                    UiKit.Size(t, 560f, 30f);
                    if (item.target != PlanTarget.None)
                    {
                        var target = item.target;
                        UiKit.Size(UiKit.Button(row, "Go_" + target, Loc.T("Học", "Study", "学ぶ"), () => Open(target), UiKit.Kind.Quiet, "arrow_forward", 13f), 100f, 30f);
                    }
                }
                var cp = ReferenceUi.Row(panel, "Checkpoint", 10f);
                UiKit.Size(cp, -1f, 40f);
                var cb = UiKit.Button(cp, "Checkpoint_" + p.levelId, best != null ? Loc.T("Thi l\u1EA1i c\u1ED9t m\u1ED1c", "Retake checkpoint", "\u518D\u30C1\u30E3\u30EC\u30F3\u30B8") : Loc.T("B\u00E0i ki\u1EC3m tra c\u1ED9t m\u1ED1c", "Checkpoint test", "\u30C1\u30A7\u30C3\u30AF\u30DD\u30A4\u30F3\u30C8\u30C6\u30B9\u30C8"),
                    () => StartCheckpoint(idx), unlocked ? UiKit.Kind.Primary : UiKit.Kind.Quiet, unlocked ? "quiz" : "lock", 14f);
                cb.interactable = unlocked;
                UiKit.Size(cb, 240f, 38f);
                if (best != null)
                {
                    var rec = best;
                    UiKit.Size(UiKit.Button(cp, "Certificate_" + p.levelId, Loc.T("Ch\u1EE9ng nh\u1EADn", "Certificate", "\u4FEE\u4E86\u8A3C"), () => ShowCertificate(rec), UiKit.Kind.Accent, "workspace_premium", 14f), 190f, 38f);
                }
                else
                {
                    var hint = ReferenceUi.Text(cp, "CheckpointHint", unlocked
                        ? Loc.F("\u0110\u1EA1t {0}% \u0111\u1EC3 m\u1EDF ch\u1EB7ng ti\u1EBFp theo", "Score {0}% to unlock the next stage", "{0}%\u4EE5\u4E0A\u3067\u6B21\u306E\u30B9\u30C6\u30FC\u30B8\u3078", Mathf.RoundToInt((float)MilestoneCheckpoint.PassRatio * 100f))
                        : Loc.T("Qua b\u00E0i ki\u1EC3m tra c\u1ED9t m\u1ED1c tr\u01B0\u1EDBc \u0111\u1EC3 m\u1EDF", "Pass the previous checkpoint to unlock", "\u524D\u306E\u30C1\u30A7\u30C3\u30AF\u30DD\u30A4\u30F3\u30C8\u306B\u5408\u683C\u3059\u308B\u3068\u89E3\u653E"), 13f, UiTheme.Muted);
                    UiKit.Size(hint, 420f, 38f);
                }
            }
        }

        // \u2500\u2500 checkpoint tests, results, certificates, badges \u2500\u2500

        private RectTransform OpenOverlay(string mode)
        {
            OverlayMode = mode;
            UiKit.Clear(_overlay);
            _overlay.gameObject.SetActive(true);
            var bg = UiKit.Card(_overlay, "OverlayBg", UiTheme.Backdrop, 18, 0f, true);
            UiKit.Place(bg.rectTransform, 0f, 0f, W - 48f, H - 112f);
            var area = UiKit.Rect(_overlay, "Area");
            UiKit.Place(area, 16f, 12f, W - 80f, H - 136f);
            return area;
        }

        public void CloseOverlay()
        {
            Checkpoint = null; OverlayMode = "";
            if (_overlay == null) return;
            UiKit.Clear(_overlay);
            _overlay.gameObject.SetActive(false);
            Rebuild();
        }

        /// <summary>Starts the mixed, timed checkpoint test for phase <paramref name="index"/> (null if locked or no content).</summary>
        public CourseQuizView StartCheckpoint(int index)
        {
            if (Plan == null || index < 0 || index >= Plan.phases.Count || !MilestoneCheckpoint.IsUnlocked(Plan, State, index)) return null;
            var phase = Plan.phases[index];
            var qs = MilestoneCheckpoint.BuildQuestions(Plan.track, phase.levelId, State.milestones.Count);
            if (qs.Count == 0)
            {
                if (_status != null) _status.text = Loc.T("Ch\u01B0a c\u00F3 c\u00E2u h\u1ECFi cho c\u1ED9t m\u1ED1c n\u00E0y.", "No questions for this milestone yet.", "\u3053\u306E\u30DE\u30A4\u30EB\u30B9\u30C8\u30FC\u30F3\u306E\u554F\u984C\u306F\u307E\u3060\u3042\u308A\u307E\u305B\u3093\u3002");
                return null;
            }
            _checkQs = qs; _checkIndex = index; _checkResult = null;
            var area = OpenOverlay("quiz");
            var track = Plan.track;
            Checkpoint = new CourseQuizView(area, W - 80f, H - 136f, qs, new CourseQuizOptions
            {
                Title = Loc.F("C\u1ED9t m\u1ED1c {0} \u00B7 {1}", "Checkpoint {0} \u00B7 {1}", "\u30C1\u30A7\u30C3\u30AF\u30DD\u30A4\u30F3\u30C8 {0} \u00B7 {1}", CertificateView.LevelName(track, phase.levelId), CertificateView.TrackName(track)),
                Target = MilestoneCheckpoint.Target(track),
                Accent = CertificateView.TrackColor(track),
                PassRatio = MilestoneCheckpoint.PassRatio,
                TimeLimitSeconds = qs.Count * MilestoneCheckpoint.SecondsPerQuestion,
                OnFinished = OnCheckpointFinished,
                OnExit = () => { if (_checkResult != null) ShowResult(); else CloseOverlay(); },
            });
            Checkpoint.Start();
            return Checkpoint;
        }

        private void OnCheckpointFinished(CourseQuizResult r)
        {
            _checkResult = r;
            var phase = Plan.phases[_checkIndex];
            var skills = MilestoneCheckpoint.Breakdown(_checkQs, r.ById);
            var rec = MilestoneCheckpoint.Record(State, Plan.track, phase.levelId, r.Correct, r.Total, skills, PlayerName(), UtcNow());
            if (rec != null) { LastRecord = rec; LearningPlanStore.Save(State); }
        }

        /// <summary>Score breakdown by skill, pass/fail, and the certificate when passed.</summary>
        public void ShowResult()
        {
            if (_checkResult == null || Plan == null) { CloseOverlay(); return; }
            var r = _checkResult;
            var phase = Plan.phases[_checkIndex];
            bool pass = r.Ratio + 1e-9 >= MilestoneCheckpoint.PassRatio;
            Checkpoint = null;
            var area = OpenOverlay("result");
            float w = W - 80f;
            var title = UiKit.Label(area, "ResultTitle", pass ? Loc.T("\u0110\u1EA1t c\u1ED9t m\u1ED1c!", "Milestone passed!", "\u30DE\u30A4\u30EB\u30B9\u30C8\u30FC\u30F3\u5408\u683C\uFF01") : Loc.T("Ch\u01B0a \u0111\u1EA1t c\u1ED9t m\u1ED1c", "Not passed yet", "\u307E\u3060\u4E0D\u5408\u683C"),
                34f, pass ? UiTheme.Good : UiTheme.Warn, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(title, 0f, 10f, w, 50f);
            var score = UiKit.Label(area, "ResultScore", Loc.F("{0}/{1} \u00B7 {2}% (c\u1EA7n {3}%)", "{0}/{1} \u00B7 {2}% (pass mark {3}%)", "{0}/{1} \u00B7 {2}%\uFF08\u5408\u683C\u30E9\u30A4\u30F3{3}%\uFF09", r.Correct, r.Total, Mathf.RoundToInt((float)r.Ratio * 100f), Mathf.RoundToInt((float)MilestoneCheckpoint.PassRatio * 100f)), 22f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(score, 0f, 64f, w, 34f);
            float y = 120f, bx = w / 2f - 330f;
            foreach (var s in MilestoneCheckpoint.Breakdown(_checkQs, r.ById))
            {
                var l = UiKit.Label(area, "Skill_" + s.skill, CertificateView.SkillName(s.skill), 18f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(l, bx, y, 180f, 30f);
                var pb = UiKit.ProgressBar(area, "SkillBar_" + s.skill, CertificateView.TrackColor(Plan.track), UiKit.Alpha(UiTheme.Text, 0.12f), 8);
                UiKit.Place((RectTransform)pb.parent, bx + 190f, y + 7f, 360f, 16f);
                AnimatedBar.Attach(pb, s.total == 0 ? 0f : (float)s.correct / s.total);
                var v = UiKit.Label(area, "SkillScore_" + s.skill, s.correct + "/" + s.total, 18f, UiTheme.TextSoft, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
                UiKit.Place(v, bx + 560f, y, 100f, 30f);
                y += 42f;
            }
            y += 10f;
            var note = UiKit.Label(area, "ResultNote", pass
                ? (_checkIndex + 1 < Plan.phases.Count ? Loc.T("Ch\u1EB7ng ti\u1EBFp theo \u0111\u00E3 m\u1EDF.", "The next stage is unlocked.", "\u6B21\u306E\u30B9\u30C6\u30FC\u30B8\u304C\u89E3\u653E\u3055\u308C\u307E\u3057\u305F\u3002") : Loc.T("B\u1EA1n \u0111\u00E3 ho\u00E0n th\u00E0nh l\u1ED9 tr\u00ECnh!", "You finished the plan!", "\u5B66\u7FD2\u30D7\u30E9\u30F3\u5B8C\u4E86\uFF01"))
                : Loc.T("\u00D4n l\u1EA1i c\u00E1c ph\u1EA7n c\u00F2n y\u1EBFu r\u1ED3i th\u1EED l\u1EA1i.", "Review the weaker skills and try again.", "\u82E6\u624B\u306A\u5206\u91CE\u3092\u5FA9\u7FD2\u3057\u3066\u518D\u6311\u6226\u3057\u307E\u3057\u3087\u3046\u3002"), 17f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center);
            UiKit.Place(note, 0f, y, w, 30f); y += 50f;
            float x = w / 2f - (pass ? 290f : 190f);
            if (pass && LastRecord != null)
            {
                var rec = LastRecord;
                var cert = UiKit.Button(area, "ViewCertificate", Loc.T("Xem ch\u1EE9ng nh\u1EADn", "View certificate", "\u4FEE\u4E86\u8A3C\u3092\u898B\u308B"), () => ShowCertificate(rec), UiKit.Kind.Primary, "workspace_premium", 16f);
                UiKit.Place((RectTransform)cert.transform, x, y, 270f, 50f); x += 290f;
            }
            else
            {
                int idx = _checkIndex;
                var again = UiKit.Button(area, "TryAgain", Loc.T("Thi l\u1EA1i", "Try again", "\u3082\u3046\u4E00\u5EA6"), () => StartCheckpoint(idx), UiKit.Kind.Primary, "refresh", 16f);
                UiKit.Place((RectTransform)again.transform, x, y, 180f, 50f); x += 200f;
            }
            var back = UiKit.Button(area, "BackToPlan", Loc.T("V\u1EC1 l\u1ED9 tr\u00ECnh", "Back to plan", "\u30D7\u30E9\u30F3\u306B\u623B\u308B"), CloseOverlay, UiKit.Kind.Secondary, "arrow_back", 16f);
            UiKit.Place((RectTransform)back.transform, x, y, 200f, 50f);
        }

        /// <summary>The in-game certificate with PNG export.</summary>
        public void ShowCertificate(MilestoneRecord rec)
        {
            if (rec == null) return;
            LastRecord = rec;
            Checkpoint = null;
            var area = OpenOverlay("certificate");
            float w = W - 80f, h = H - 136f - 64f;
            float scale = Mathf.Min(w / CertificateView.CW, h / CertificateView.CH);
            var holder = UiKit.Rect(area, "CertHolder");
            UiKit.Place(holder, (w - CertificateView.CW * scale) / 2f, 0f, CertificateView.CW, CertificateView.CH);
            holder.localScale = new Vector3(scale, scale, 1f);
            CertificateView.Build(holder, rec);
            float y = h + 10f, x = w / 2f - 330f;
            var export = UiKit.Button(area, "ExportPng", Loc.T("L\u01B0u \u1EA3nh PNG", "Save as PNG", "PNG\u3067\u4FDD\u5B58"), () => ExportCertificate(rec), UiKit.Kind.Primary, "download", 15f);
            UiKit.Place((RectTransform)export.transform, x, y, 200f, 46f);
            var gal = UiKit.Button(area, "OpenGallery", Loc.T("Huy hi\u1EC7u", "Badges", "\u30D0\u30C3\u30B8"), ShowGallery, UiKit.Kind.Accent, "workspace_premium", 15f);
            UiKit.Place((RectTransform)gal.transform, x + 216f, y, 200f, 46f);
            var close = UiKit.Button(area, "CloseCertificate", Loc.T("\u0110\u00F3ng", "Close", "\u9589\u3058\u308B"), CloseOverlay, UiKit.Kind.Secondary, "close", 15f);
            UiKit.Place((RectTransform)close.transform, x + 432f, y, 200f, 46f);
        }

        public string ExportCertificate(MilestoneRecord rec, string folder = null)
        {
            LastExportPath = CertificateView.ExportPng(rec, folder);
            if (_status != null)
                _status.text = LastExportPath != null ? Loc.F("\u0110\u00E3 l\u01B0u: {0}", "Saved: {0}", "\u4FDD\u5B58\u3057\u307E\u3057\u305F:{0}", LastExportPath)
                    : Loc.T("Kh\u00F4ng xu\u1EA5t \u0111\u01B0\u1EE3c \u1EA3nh tr\u00EAn thi\u1EBFt b\u1ECB n\u00E0y.", "Could not export the image on this device.", "\u3053\u306E\u7AEF\u672B\u3067\u306F\u753B\u50CF\u3092\u4FDD\u5B58\u3067\u304D\u307E\u305B\u3093\u3002");
            return LastExportPath;
        }

        /// <summary>Every badge earned (all tracks) plus the locked milestones still ahead in the current plan.</summary>
        public void ShowGallery()
        {
            Checkpoint = null;
            var area = OpenOverlay("gallery");
            float w = W - 80f;
            var head = UiKit.Label(area, "GalleryTitle", Loc.T("B\u1ED9 s\u01B0u t\u1EADp huy hi\u1EC7u", "Badge gallery", "\u30D0\u30C3\u30B8\u30B3\u30EC\u30AF\u30B7\u30E7\u30F3"), 28f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(head, 8f, 0f, w - 230f, 44f);
            var close = UiKit.Button(area, "CloseGallery", Loc.T("\u0110\u00F3ng", "Close", "\u9589\u3058\u308B"), CloseOverlay, UiKit.Kind.Secondary, "close", 15f);
            UiKit.Place((RectTransform)close.transform, w - 200f, 0f, 200f, 44f);
            var tiles = new List<(MilestoneRecord rec, PlanTrack track, string level)>();
            foreach (var m in State.milestones.OrderBy(m => m.track).ThenBy(m => Array.IndexOf(LearningPlanner.Levels(m.track), m.levelId))) tiles.Add((m, m.track, m.levelId));
            if (Plan != null)
                foreach (var p in Plan.phases)
                    if (!MilestoneCheckpoint.Passed(State, Plan.track, p.levelId)) tiles.Add((null, Plan.track, p.levelId));
            if (tiles.Count == 0)
            {
                var empty = UiKit.Label(area, "GalleryEmpty", Loc.T("Ch\u01B0a c\u00F3 huy hi\u1EC7u. H\u00E3y t\u1EA1o l\u1ED9 tr\u00ECnh v\u00E0 v\u01B0\u1EE3t qua b\u00E0i ki\u1EC3m tra c\u1ED9t m\u1ED1c \u0111\u1EA7u ti\u00EAn.", "No badges yet. Build a plan and pass your first checkpoint.", "\u307E\u3060\u30D0\u30C3\u30B8\u306F\u3042\u308A\u307E\u305B\u3093\u3002\u30D7\u30E9\u30F3\u3092\u4F5C\u3063\u3066\u6700\u521D\u306E\u30C1\u30A7\u30C3\u30AF\u30DD\u30A4\u30F3\u30C8\u306B\u5408\u683C\u3057\u307E\u3057\u3087\u3046\u3002"), 18f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center, true);
                UiKit.Place(empty, 0f, 120f, w, 80f);
                return;
            }
            var host = UiKit.Rect(area, "GalleryScroll");
            UiKit.Place(host, 0f, 60f, w, H - 136f - 60f);
            var grid = UiKit.Scroll(host, "Grid", out _, 0f, new RectOffset(4, 16, 4, 16));
            var gl = grid.gameObject.GetComponent<VerticalLayoutGroup>();
            if (gl != null) UnityEngine.Object.DestroyImmediate(gl);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(220f, 250f); g.spacing = new Vector2(16f, 16f);
            var mark = CertificateView.Mark;
            foreach (var t in tiles)
            {
                bool earned = t.rec != null;
                var c = CertificateView.TrackColor(t.track);
                var tile = UiKit.Card(grid, "Badge_" + t.track + "_" + t.level, earned ? UiKit.Alpha(c, 0.22f) : UiTheme.Sunken, 20, 0f, true);
                var ring = UiKit.Image(tile.transform, "Ring", earned ? UiTheme.Gold : UiKit.Alpha(UiTheme.Muted, 0.4f), UiKit.Rounded(60));
                UiKit.Place(ring.rectTransform, 50f, 16f, 120f, 120f);
                var disc = UiKit.Image(ring.transform, "Disc", earned ? c : UiTheme.Raised, UiKit.Rounded(54));
                UiKit.Place(disc.rectTransform, 6f, 6f, 108f, 108f);
                if (earned && mark != null)
                {
                    var mr = UiKit.Rect(disc.transform, "Mark"); UiKit.Place(mr, 24f, 10f, 60f, 60f);
                    var raw = mr.gameObject.AddComponent<RawImage>(); raw.texture = mark; raw.raycastTarget = false;
                }
                var lv = UiKit.Label(disc.transform, "Level", CertificateView.LevelName(t.track, t.level), earned ? 24f : 30f, earned ? Color.white : UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(lv, 0f, earned ? 66f : 0f, 108f, earned ? 40f : 108f);
                var name = UiKit.Label(tile.transform, "Track", CertificateView.TrackName(t.track) + " \u00B7 " + CertificateView.LevelName(t.track, t.level), 15f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(name, 6f, 146f, 208f, 26f);
                var sub = UiKit.Label(tile.transform, "Sub", earned ? t.rec.Percent + "% \u00B7 " + t.rec.UtcDate.ToLocalTime().ToString("dd/MM/yyyy") : Loc.T("Ch\u01B0a \u0111\u1EA1t", "Not earned yet", "\u672A\u7372\u5F97"), 13f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center);
                UiKit.Place(sub, 6f, 174f, 208f, 22f);
                if (earned)
                {
                    var rec = t.rec;
                    var view = UiKit.Button(tile.transform, "View", Loc.T("Ch\u1EE9ng nh\u1EADn", "Certificate", "\u4FEE\u4E86\u8A3C"), () => ShowCertificate(rec), UiKit.Kind.Quiet, "visibility", 13f);
                    UiKit.Place((RectTransform)view.transform, 30f, 202f, 160f, 36f);
                }
            }
        }

        /// <summary>Opens the screen a plan item points to.</summary>
        public void Open(PlanTarget target)
        {
            var hub = StudyHubPopup.GetOrCreate(gameObject, Font);
            switch (target)
            {
                case PlanTarget.School: Hide(); hub.OpenSchool(); break;
                case PlanTarget.Patterns: Hide(); hub.OpenPatterns(); break;
                case PlanTarget.EnglishBasic: Hide(); hub.OpenEnglishBasic(); break;
                case PlanTarget.Tenses: Hide(); hub.OpenTenses(); break;
                case PlanTarget.RussianPath: Hide(); hub.OpenRussianPath(); break;
                case PlanTarget.ModelAnswers: Hide(); hub.OpenModelAnswers(); break;
                case PlanTarget.Videos: Hide(); hub.OpenVideos(); break;
                case PlanTarget.Dictionary: Hide(); hub.OpenDictionary(); break;
                case PlanTarget.ExamCenter:
                    if (_status != null) _status.text = Loc.T("Mở Trung tâm luyện thi bằng phím K.", "Open the Exam Center with the K key.", "Kキーで試験センターを開きます。");
                    break;
            }
        }
    }
}
