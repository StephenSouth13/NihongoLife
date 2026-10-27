using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The career window (The Sims-style career panel), in tabs:
    /// Overview — title, performance, mood, what the next promotion needs, salary, today, "Go to work" / "Quit";
    /// Path — the ladder as a tree (shared levels, then the branches) with salary and requirements per rung;
    /// Schedule — this week's work days, with the shifts worked and missed; Profile — the career's record
    /// (shifts, days, salary earned, events, promotions, past careers); Milestones — the development path from
    /// careers.json with progress and rewards; Careers — every career, to choose one or switch (quit first).
    /// Without a career only Careers / Milestones / Profile show. Opened once at level 10, and from the task journal.
    /// </summary>
    public sealed class CareerUI : MonoBehaviour
    {
        public enum Tab { Overview, Path, Schedule, Profile, Milestones, Careers }

        private static CareerUI _instance;
        private TMP_FontAsset _font;
        private RectTransform _root, _header, _tabs, _body;
        private ScrollRect _scroll;
        private PlayerController _player;
        private bool _wasLocked;
        private bool _confirmQuit;
        private string _switchTo;
        private Tab _tab = Tab.Overview;
        private float _nextRefresh;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeSelf;
        public static Tab CurrentTab => _instance != null ? _instance._tab : Tab.Overview;

        private static CareerUI Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var canvas = NLUi.CreateCanvas("CareerCanvas", 127);
                DontDestroyOnLoad(canvas.gameObject);
                _instance = canvas.gameObject.AddComponent<CareerUI>();
                _instance._font = NLUi.ResolveFont();
                _instance.Build((RectTransform)canvas.transform);
                Loc.Watch(_instance, () => { if (IsOpen) _instance.Rebuild(); });
                UiModalStack.Register(_instance, () => IsOpen, Close, "Career");
                CareerService.Changed += () => { if (IsOpen) _instance.Rebuild(); };
                return _instance;
            }
        }

        public static void Open() => Open(CareerService.Current != null ? Tab.Overview : Tab.Careers);

        public static void Open(Tab tab)
        {
            var ui = Instance;
            ui._confirmQuit = false;
            ui._switchTo = null;
            ui._tab = tab;
            if (!ui._root.gameObject.activeSelf)
            {
                ui._root.gameObject.SetActive(true);
                ui._player = SceneRefs.Find<PlayerController>();
                if (ui._player != null) { ui._wasLocked = ui._player.InputLocked; ui._player.InputLocked = true; }
            }
            ui._root.SetAsLastSibling();
            ui.Rebuild();
        }

        public static void Close()
        {
            if (!IsOpen) return;
            _instance._root.gameObject.SetActive(false);
            if (_instance._player != null) _instance._player.InputLocked = _instance._wasLocked;
            _instance._player = null;
        }

        /// <summary>Shows another tab of the open window (also the tab buttons, "Tab_&lt;name&gt;").</summary>
        public static void Show(Tab tab)
        {
            if (!IsOpen) { Open(tab); return; }
            _instance._tab = tab;
            _instance._confirmQuit = false;
            _instance._switchTo = null;
            _instance.Rebuild();
            _instance._scroll.verticalNormalizedPosition = 1f;
        }

        private void Build(RectTransform canvas)
        {
            _root = new GameObject("Career", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _root.SetParent(canvas, false);
            NLUi.Stretch(_root);
            _root.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.72f);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            card.SetParent(_root, false);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(1140f, 680f);
            card.GetComponent<Image>().color = NLUi.Ink;
            card.gameObject.AddComponent<HudFitRect>().Configure(0.96f, 0.94f, Vector2.zero, 0.4f);

            // Fixed header (title, subtitle, tabs); the tab's content scrolls below it.
            _header = NLUi.Group(card, "Header", true, 8f);
            _header.anchorMin = new Vector2(0f, 1f); _header.anchorMax = new Vector2(1f, 1f); _header.pivot = new Vector2(0.5f, 1f);
            _header.offsetMin = new Vector2(30f, -168f); _header.offsetMax = new Vector2(-80f, -20f);

            var scrollRoot = new GameObject("Scroll", typeof(RectTransform)).GetComponent<RectTransform>();
            scrollRoot.SetParent(card, false);
            NLUi.Stretch(scrollRoot);
            scrollRoot.offsetMin = new Vector2(30f, 20f); scrollRoot.offsetMax = new Vector2(-24f, -176f);
            _body = UiKit.Scroll(scrollRoot, "Content", out _scroll, 12f, new RectOffset(2, 18, 4, 16));
            NLUi.Stretch((RectTransform)_scroll.transform);

            NLUi.CloseButton(card, _font, Close, 46f, 18f);
            _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Today's status follows the clock while the window is open (the tabs that show it).
            if (IsOpen && CareerService.Current != null && (_tab == Tab.Overview || _tab == Tab.Schedule) && Time.unscaledTime > _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 2f;
                Rebuild();
            }
        }

        private void Rebuild()
        {
            UiKit.Clear(_header);
            UiKit.Clear(_body);
            bool hasCareer = CareerService.Current != null;
            if (!hasCareer && _tab != Tab.Milestones && _tab != Tab.Profile) _tab = Tab.Careers;
            BuildHeader(hasCareer);
            switch (_tab)
            {
                case Tab.Overview: BuildOverview(); break;
                case Tab.Path: BuildPath(); break;
                case Tab.Schedule: BuildSchedule(); break;
                case Tab.Profile: BuildProfile(); break;
                case Tab.Milestones: BuildMilestones(); break;
                default: BuildCareers(); break;
            }
        }

        // ─────────── Header and tabs ───────────

        private void BuildHeader(bool hasCareer)
        {
            var state = CareerService.State;
            if (hasCareer)
            {
                var career = CareerService.Current;
                var level = CareerService.CurrentLevel;
                var branch = career.Branch(state.branchId);
                NLUi.Label(_header, "Title", $"{level.Title}  <size=60%><color=#A8B4C4>{level.titleJa}</color></size>", 30f, NLUi.Text, _font, FontStyles.Bold);
                NLUi.Label(_header, "Career", $"{career.Name}{(branch != null ? " · " + branch.Name : "")} · {career.Workplace} · " +
                    Loc.F("bậc {0}/{1}", "level {0}/{1}", "レベル{0}/{1}", state.level, career.MaxLevel), 17f, NLUi.Muted, _font);
            }
            else
            {
                NLUi.Label(_header, "Title", Loc.T("Hướng nghiệp", "Choose a career", "職業を選ぶ"), 30f, NLUi.Text, _font, FontStyles.Bold);
                NLUi.Label(_header, "Career", CareerService.Unlocked
                    ? Loc.T("Chưa có nghề — chọn một nghề để bắt đầu lộ trình.", "No career yet — choose one to start your path.", "まだ仕事がありません — 選んでキャリアを始めましょう。")
                    : Loc.F("Mở khi đạt cấp {0} — bạn đang cấp {1}.", "Opens at level {0} — you are level {1}.", "レベル{0}で開きます — 今はレベル{1}。", CareerService.Catalog.unlockLevel, CareerService.PlayerLevel),
                    17f, CareerService.Unlocked ? NLUi.Muted : NLUi.Bad, _font);
            }

            _tabs = NLUi.Group(_header, "Tabs", false, 8f, TextAnchor.MiddleLeft, false);
            if (hasCareer)
            {
                TabButton(Tab.Overview, Loc.T("Tổng quan", "Overview", "概要"));
                TabButton(Tab.Path, Loc.T("Lộ trình", "Path", "キャリアパス"));
                TabButton(Tab.Schedule, Loc.T("Lịch làm", "Schedule", "勤務表"));
            }
            TabButton(Tab.Careers, Loc.T("Các nghề", "Careers", "職業一覧"));
            TabButton(Tab.Milestones, Loc.F("Cột mốc {0}/{1}", "Milestones {0}/{1}", "節目 {0}/{1}", state?.milestones.Count ?? 0, CareerService.Catalog.milestones.Length));
            TabButton(Tab.Profile, Loc.T("Hồ sơ", "Profile", "プロフィール"));
        }

        private void TabButton(Tab tab, string label)
        {
            bool on = _tab == tab;
            var button = NLUi.Button(_tabs, "Tab_" + tab, label, _font, () => Show(tab), on ? NLUi.Gold : NLUi.Card, 16f, on ? NLUi.Ink : NLUi.Text, 40f);
            ((HorizontalOrVerticalLayoutGroup)button.GetComponent<HorizontalOrVerticalLayoutGroup>()).padding = new RectOffset(16, 16, 8, 8);
            button.GetComponentInChildren<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.NoWrap;
            NLUi.FitContent((RectTransform)button.transform, width: true, height: false);
            NLUi.Size(button, -1f, -1f, 0f); // packed to the left, not spread over the row
        }

        // ─────────── Overview ───────────

        private void BuildOverview()
        {
            var career = CareerService.Current;
            var state = CareerService.State;
            var level = CareerService.CurrentLevel;

            var columns = NLUi.Group(_body, "Columns", false, 22f, TextAnchor.UpperLeft, true);
            var left = NLUi.Group(columns, "Left", true, 10f);
            NLUi.Size(left, 560f, -1f, 1f);
            var right = NLUi.Group(columns, "Right", true, 6f);
            NLUi.Size(right, 440f);

            NLUi.Label(left, "PerformanceLabel", Loc.F("Hiệu suất {0:0}%", "Performance {0:0}%", "評価 {0:0}%", state.performance), 18f, NLUi.Text, _font, FontStyles.Bold);
            Bar(left, "PerformanceBar", state.performance / 100f, state.performance >= 99.5f ? NLUi.Gold : NLUi.Good, 18f);
            string mood = CareerService.MoodFactor > 1f ? Loc.T("Tâm trạng tốt: làm hiệu quả hơn (×1.25).", "Good mood: working better (×1.25).", "気分がいい：よく働ける（×1.25）。")
                : CareerService.MoodFactor < 1f ? Loc.T("Đang mệt / căng / bực: hiệu suất ca này giảm (×0.75).", "Tired / tense / annoyed: this shift counts less (×0.75).", "疲れ・緊張・イライラ：評価が下がる（×0.75）。")
                : Loc.T("Tâm trạng bình thường.", "Normal mood.", "ふつうの気分。");
            NLUi.Label(left, "Mood", mood, 15f, NLUi.Muted, _font);
            string blocker = CareerService.PromotionBlocker();
            NLUi.Label(left, "Next", blocker == null ? Loc.T("Sẵn sàng phỏng vấn thăng chức!", "Ready for the promotion interview!", "昇進面接の準備ができました！")
                : Loc.T("Thăng chức: ", "Promotion: ", "昇進：") + blocker, 16f, blocker == null ? NLUi.Gold : NLUi.Soft, _font);

            NLUi.Divider(left);
            NLUi.Label(left, "Salary", Loc.F("Lương nghề: {0} / ca đúng giờ (cộng thêm tiền công ca làm)", "Salary: {0} per on-time shift (on top of the shift's pay)", "給料：時間どおりのシフトごとに{0}（シフト代に加えて）", Loc.Yen(level.salary)), 16f, NLUi.Text, _font);
            NLUi.Label(left, "Schedule", Loc.T("Lịch làm: ", "Schedule: ", "勤務：") + career.Schedule, 16f, NLUi.Text, _font);
            NLUi.Label(left, "Today", TodayLine(career, state), 16f, NLUi.Soft, _font);

            var actions = NLUi.Group(left, "Actions", false, 10f, TextAnchor.MiddleLeft, false);
            var work = NLUi.Button(actions, "GoToWork", Loc.T("Đi làm (mở ca)", "Go to work (open the shift)", "仕事へ（シフトを開く）"), _font, () => { Close(); TaskJournalUI.Open(career.shiftQuest); }, NLUi.Gold, 17f, NLUi.Ink, 46f);
            NLUi.Size(work, 260f, 46f);
            var quit = NLUi.Button(actions, "Quit", _confirmQuit ? Loc.T("Chắc chắn nghỉ việc?", "Really quit?", "本当にやめる？") : Loc.T("Nghỉ việc", "Quit job", "仕事をやめる"), _font, () =>
            {
                if (!_confirmQuit) { _confirmQuit = true; Rebuild(); return; }
                _confirmQuit = false;
                CareerService.Quit();
            }, new Color(0.45f, 0.16f, 0.15f), 16f, Color.white, 46f);
            NLUi.Size(quit, 200f, 46f);

            // Right: the next rung and the next milestone, the two things to aim for.
            NLUi.Label(right, "NextTitle", Loc.T("Mục tiêu tiếp theo", "Next goals", "次の目標"), 18f, NLUi.Gold, _font, FontStyles.Bold);
            var next = career.Level(state.level + 1, state.branchId);
            var goal = NLUi.Panel(right, "NextRung", NLUi.Card, new RectOffset(16, 16, 12, 12), 4f);
            if (next == null) NLUi.Label(goal, "Top", Loc.T("Bạn đã ở đỉnh nấc thang nghề.", "You are at the top of the ladder.", "はしごの一番上です。"), 16f, NLUi.Gold, _font);
            else
            {
                bool split = state.level + 1 == career.branchAtLevel && career.branches.Length > 0;
                NLUi.Label(goal, "Rung", split
                    ? Loc.F("Bậc {0}: chọn hướng — {1}", "Level {0}: choose a path — {1}", "レベル{0}：コースを選ぶ — {1}", state.level + 1, string.Join(" / ", career.branches.Select(b => b.Name)))
                    : $"{state.level + 1}. {next.Title}  <size=75%><color=#A8B4C4>{next.titleJa}</color></size>", 16f, NLUi.Text, _font, FontStyles.Bold);
                NLUi.Label(goal, "Needs", Requirement(level), 14f, NLUi.Muted, _font);
                NLUi.Label(goal, "Pay", Loc.F("Lương sau thăng chức: {0}/ca", "Salary after the promotion: {0}/shift", "昇進後の給料：{0}/シフト", Loc.Yen(split ? career.branches.Min(b => b.levels.FirstOrDefault()?.salary ?? 0) : next.salary)), 14f, NLUi.Soft, _font);
            }
            var milestone = CareerService.Catalog.milestones.FirstOrDefault(m => !CareerService.Reached(m));
            if (milestone != null) MilestoneRow(right, milestone);
            var path = NLUi.Button(right, "OpenPath", Loc.T("Xem lộ trình đầy đủ →", "See the whole path →", "キャリアパスを見る →"), _font, () => Show(Tab.Path), NLUi.Card, 15f, NLUi.Soft, 40f);
            NLUi.Size(path, -1f, 40f);
        }

        // ─────────── Path (the ladder as a tree) ───────────

        private void BuildPath()
        {
            var career = CareerService.Current;
            var state = CareerService.State;
            NLUi.Label(_body, "PathIntro", Loc.F("Mỗi bậc: làm đủ ca đúng giờ để đầy hiệu suất, đủ trí tuệ, rồi qua phỏng vấn tiếng Nhật với {0}. Bậc {1} chọn hướng đi riêng — hướng kia khép lại.",
                "Each rung: fill the performance bar with on-time shifts, have the knowledge, then pass a Japanese interview with {0}. At level {1} you choose your path — the other one closes.",
                "各レベル：時間どおりのシフトで評価を満たし、知識をため、{0}と日本語で面接。レベル{1}で自分のコースを選びます。", career.BossName, career.branchAtLevel), 15f, NLUi.Muted, _font);

            var tree = NLUi.Group(_body, "Tree", false, 16f, TextAnchor.UpperLeft, false);
            var trunk = NLUi.Group(tree, "Shared", true, 8f);
            NLUi.Size(trunk, 320f);
            NLUi.Label(trunk, "SharedTitle", Loc.T("Chung", "Shared", "共通"), 16f, NLUi.Gold, _font, FontStyles.Bold);
            for (int i = 0; i < career.levels.Length; i++)
            {
                RungNode(trunk, i + 1, career.levels[i], state.level, false);
                if (i < career.levels.Length - 1) NLUi.Label(trunk, "Arrow", "↓", 16f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            }

            foreach (var b in career.branches)
            {
                bool chosen = state.branchId == b.id;
                bool closed = !string.IsNullOrEmpty(state.branchId) && !chosen;
                var column = NLUi.Group(tree, "Branch_" + b.id, true, 8f);
                NLUi.Size(column, 320f);
                NLUi.Label(column, "Name", "↘ " + b.Name + (chosen ? Loc.T("  · hướng của bạn", "  · your path", "  · あなたのコース") : closed ? Loc.T("  · đã khép", "  · closed", "  · 選ばなかった") : ""),
                    16f, chosen ? NLUi.Gold : closed ? NLUi.Muted : NLUi.Soft, _font, FontStyles.Bold);
                NLUi.Label(column, "Description", b.Description, 13f, NLUi.Muted, _font);
                for (int i = 0; i < b.levels.Length; i++)
                {
                    int index = career.levels.Length + 1 + i;
                    RungNode(column, index, b.levels[i], chosen ? state.level : 0, closed);
                    if (i < b.levels.Length - 1) NLUi.Label(column, "Arrow", "↓", 16f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
                }
            }
        }

        private void RungNode(RectTransform parent, int index, CareerLevelDefinition level, int current, bool closed)
        {
            bool here = index == current, done = index < current;
            var color = here ? new Color(0.32f, 0.26f, 0.1f, 1f) : done ? new Color(0.1f, 0.22f, 0.16f, 1f) : NLUi.Card;
            var node = NLUi.Panel(parent, "Rung_" + index, closed ? new Color(1f, 1f, 1f, 0.04f) : color, new RectOffset(14, 14, 10, 10), 3f);
            string mark = here ? "▶" : done ? "✓" : "·";
            NLUi.Label(node, "Title", $"{mark} {index}. {level.Title}  <size=75%><color=#A8B4C4>{level.titleJa}</color></size>", 16f,
                closed ? NLUi.Muted : here ? NLUi.Gold : done ? NLUi.Good : NLUi.Text, _font, here ? FontStyles.Bold : FontStyles.Normal);
            NLUi.Label(node, "Salary", Loc.F("Lương {0}/ca", "Salary {0}/shift", "給料 {0}/シフト", Loc.Yen(level.salary)), 14f, closed ? NLUi.Muted : NLUi.Soft, _font);
            NLUi.Label(node, "Needs", Requirement(level), 13f, NLUi.Muted, _font);
        }

        private static string Requirement(CareerLevelDefinition level)
        {
            if (level == null || level.shiftsToPromote <= 0) return Loc.T("Bậc cao nhất", "Top rung", "最高ポスト");
            return Loc.F("Lên bậc: ~{0} ca đúng giờ · {1} trí tuệ · phỏng vấn", "To move up: ~{0} on-time shifts · {1} knowledge · interview", "昇進：時間どおりのシフト約{0}回 · 知識{1} · 面接",
                level.shiftsToPromote, level.knowledgeToPromote);
        }

        // ─────────── Schedule (this week) ───────────

        private void BuildSchedule()
        {
            var career = CareerService.Current;
            var state = CareerService.State;
            int today = DayNightCycle.Active != null ? DayNightCycle.Active.Day : CareerService.State.lastCheckedDay;
            today = Mathf.Max(0, today);
            // This week from Monday (the calendar starts on day 0: the first week starts there).
            int monday = Mathf.Max(0, today - ((int)GameCalendar.DateOf(today).DayOfWeek + 6) % 7);

            NLUi.Label(_body, "ScheduleLine", Loc.T("Lịch làm: ", "Schedule: ", "勤務：") + career.Schedule + " · " + career.Workplace, 17f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Label(_body, "ScheduleRule", Loc.F("Ca làm trong giờ: hiệu suất đầy đủ + lương nghề. Ngoài giờ: nửa hiệu suất, không lương. Bỏ một ngày làm: −{0}% hiệu suất.",
                "A shift in working hours: full performance + salary. Outside: half, no salary. A missed work day: −{0}% performance.",
                "勤務時間内：評価満点＋給料。時間外：評価半分・給料なし。休むと評価 −{0}%。", CareerService.Catalog.missedShiftPenalty), 14f, NLUi.Muted, _font);

            var week = NLUi.Group(_body, "Week", false, 10f, TextAnchor.UpperLeft, true);
            int planned = 0, worked = 0;
            for (int i = 0; i < 7; i++)
            {
                int day = monday + i;
                var date = GameCalendar.DateOf(day);
                bool workDay = CareerService.IsWorkDay(career, day);
                bool isToday = day == today;
                bool did = state.workedDays.Contains(day);
                bool missed = workDay && !did && day < today && day >= state.startedDay;
                if (workDay) planned++;
                if (did) worked++;
                var cell = NLUi.Panel(week, "Day_" + i, isToday ? new Color(0.32f, 0.26f, 0.1f, 1f) : workDay ? NLUi.Card : new Color(1f, 1f, 1f, 0.04f), new RectOffset(10, 10, 12, 12), 6f);
                NLUi.Size(cell, 130f, 190f, 1f);
                NLUi.Label(cell, "Name", CareerDefinition.DayName((int)date.DayOfWeek) + (isToday ? Loc.T(" · nay", " · today", " · 今日") : ""), 17f, isToday ? NLUi.Gold : NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center);
                NLUi.Label(cell, "Date", $"{date.Day}/{date.Month}", 14f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
                NLUi.Label(cell, "Hours", workDay ? $"{career.workStart}:00–{career.workEnd}:00" : Loc.T("Nghỉ", "Day off", "休み"), 15f, workDay ? NLUi.Soft : NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
                string status = did ? "✓ " + Loc.T("Đã làm", "Worked", "勤務済み") : missed ? "✗ " + Loc.T("Vắng", "Missed", "欠勤") : workDay && day >= today ? Loc.T("Sắp tới", "Upcoming", "予定") : "";
                NLUi.Label(cell, "Status", status, 15f, did ? NLUi.Good : missed ? NLUi.Bad : NLUi.Muted, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            }
            NLUi.Label(_body, "WeekTotal", Loc.F("Tuần này: {0}/{1} ngày làm · tổng cộng {2} ngày đi làm", "This week: {0}/{1} work days · {2} days worked in all", "今週：{0}/{1}日 · 合計{2}日勤務", worked, planned, state.workedDays.Count), 16f, NLUi.Text, _font);
            NLUi.Label(_body, "Today", TodayLine(career, state), 16f, NLUi.Soft, _font);
            var work = NLUi.Button(_body, "GoToWork", Loc.T("Đi làm (mở ca)", "Go to work (open the shift)", "仕事へ（シフトを開く）"), _font, () => { Close(); TaskJournalUI.Open(career.shiftQuest); }, NLUi.Gold, 17f, NLUi.Ink, 46f);
            NLUi.Size(work, 260f, 46f, 0f);
        }

        // ─────────── Profile ───────────

        private void BuildProfile()
        {
            var catalog = CareerService.Catalog;
            var state = CareerService.State;
            var career = CareerService.Current;
            int today = DayNightCycle.Active != null ? DayNightCycle.Active.Day : 0;

            var stats = NLUi.Group(_body, "Stats", false, 10f, TextAnchor.UpperLeft, true);
            if (career != null)
            {
                Stat(stats, "StatDays", Loc.T("Ngày trong nghề", "Days in career", "在職日数"), state.startedDay >= 0 ? (today - state.startedDay + 1).ToString() : "—");
                Stat(stats, "StatShifts", Loc.T("Ca đã làm", "Shifts", "シフト"), state.shifts.ToString());
                Stat(stats, "StatWorked", Loc.T("Ngày đi làm", "Days worked", "勤務日"), state.workedDays.Count.ToString());
                Stat(stats, "StatSalary", Loc.T("Lương đã nhận", "Salary earned", "給料合計"), Loc.Yen(state.totalSalary));
                Stat(stats, "StatEvents", Loc.T("Tình huống", "Work events", "できごと"), state.eventsHandled.ToString());
            }
            Stat(stats, "StatMilestones", Loc.T("Cột mốc", "Milestones", "節目"), $"{state.milestones.Count}/{catalog.milestones.Length}");

            NLUi.Label(_body, "PromotionsTitle", Loc.T("Lịch sử thăng chức", "Promotions", "昇進の記録"), 18f, NLUi.Gold, _font, FontStyles.Bold);
            if (state.promotions.Count == 0)
                NLUi.Label(_body, "NoPromotions", Loc.T("Chưa thăng chức lần nào.", "No promotions yet.", "まだ昇進していません。"), 15f, NLUi.Muted, _font);
            for (int i = state.promotions.Count - 1; i >= 0 && i >= state.promotions.Count - 8; i--)
            {
                var p = state.promotions[i];
                var c = catalog.Career(p.careerId);
                var l = c?.Level(p.level, p.branchId);
                var date = GameCalendar.DateOf(p.day);
                NLUi.Label(_body, "Promotion_" + i, $"▲ {date.Day}/{date.Month}/{date.Year} · {c?.Name ?? p.careerId} · " +
                    Loc.F("bậc {0}", "level {0}", "レベル{0}", p.level) + $" · {l?.Title}  <size=75%><color=#A8B4C4>{l?.titleJa}</color></size>", 15f, NLUi.Text, _font);
            }

            NLUi.Label(_body, "PastTitle", Loc.T("Nghề đã từng làm", "Past careers", "以前の仕事"), 18f, NLUi.Gold, _font, FontStyles.Bold);
            var past = state.pastCareers.Select(id => catalog.Career(id)?.Name ?? id).ToList();
            NLUi.Label(_body, "Past", past.Count == 0 ? Loc.T("Chưa có.", "None yet.", "まだありません。") : string.Join(" · ", past), 15f, past.Count == 0 ? NLUi.Muted : NLUi.Text, _font);
        }

        private void Stat(RectTransform parent, string name, string label, string value)
        {
            var box = NLUi.Panel(parent, name, NLUi.Card, new RectOffset(14, 14, 10, 12), 2f);
            NLUi.Size(box, 160f, -1f, 1f);
            NLUi.Label(box, "Value", value, 24f, NLUi.Gold, _font, FontStyles.Bold);
            NLUi.Label(box, "Label", label, 14f, NLUi.Muted, _font);
        }

        // ─────────── Milestones ───────────

        private void BuildMilestones()
        {
            NLUi.Label(_body, "MilestonesIntro", Loc.T("Tuyến phát triển nghề nghiệp: mỗi cột mốc thưởng một lần (giữ cả khi đổi nghề).",
                "Your career development path: each milestone pays once (kept when you change careers).",
                "キャリアの成長：節目ごとに一度だけごほうび（転職しても残ります）。"), 15f, NLUi.Muted, _font);
            foreach (var m in CareerService.Catalog.milestones) MilestoneRow(_body, m);
        }

        private void MilestoneRow(RectTransform parent, CareerMilestoneDefinition m)
        {
            bool reached = CareerService.Reached(m);
            int goal = Mathf.Max(1, m.value);
            int progress = Mathf.Min(goal, CareerService.MilestoneProgress(m));
            var row = NLUi.Panel(parent, "Milestone_" + m.id, reached ? new Color(0.1f, 0.22f, 0.16f, 1f) : NLUi.Card, new RectOffset(16, 16, 10, 12), 4f);
            NLUi.Label(row, "Title", (reached ? "★ " : "☆ ") + m.Title + $"  <size=75%><color=#A8B4C4>{m.titleJa}</color></size>", 17f, reached ? NLUi.Gold : NLUi.Text, _font, FontStyles.Bold);
            NLUi.Label(row, "Description", m.Description, 14f, NLUi.Muted, _font);
            if (!reached && goal > 1) Bar(row, "Progress", progress / (float)goal, NLUi.Good, 10f);
            string reward = string.Join(" · ", new[] { m.rewardYen > 0 ? Loc.Yen(m.rewardYen) : null, m.rewardKnowledge > 0 ? Loc.F("{0} trí tuệ", "{0} knowledge", "知識 {0}", m.rewardKnowledge) : null }.Where(x => x != null));
            NLUi.Label(row, "Reward", (reached ? Loc.T("Đã nhận: ", "Received: ", "受け取り済み：") : Loc.F("{0}/{1} · Thưởng: ", "{0}/{1} · Reward: ", "{0}/{1} · ごほうび：", progress, goal)) + reward,
                14f, reached ? NLUi.Good : NLUi.Soft, _font);
        }

        // ─────────── Careers (choose or switch) ───────────

        private void BuildCareers()
        {
            var catalog = CareerService.Catalog;
            var state = CareerService.State;
            bool hasCareer = CareerService.Current != null;
            NLUi.Label(_body, "Intro", hasCareer
                ? Loc.T("Đổi nghề: nghỉ việc hiện tại và bắt đầu lại từ bậc 1 của nghề mới (cột mốc đã đạt vẫn giữ).",
                    "Switching: you leave your job and start the new career at level 1 (milestones stay).",
                    "転職：今の仕事をやめ、新しい仕事のレベル1から（節目は残ります）。")
                : Loc.T("Chọn một nghề: làm ca đúng giờ để tăng hiệu suất, qua phỏng vấn tiếng Nhật để thăng chức, đến bậc 4 thì chọn hướng đi riêng.",
                    "Pick a career: work your shifts on time to raise performance, pass a Japanese interview to be promoted, choose your own path at level 4.",
                    "仕事を選びましょう。時間どおりに働いて評価を上げ、日本語の面接で昇進。レベル4で自分のコースを選びます。"), 16f, NLUi.Muted, _font);

            // A grid, three cards a row: five careers and more fit without running off the card.
            var row = new GameObject("Careers", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            row.SetParent(_body, false);
            var grid = row.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(334f, 452f);
            grid.spacing = new Vector2(14f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            NLUi.FitContent(row);
            foreach (var career in catalog.careers)
            {
                bool current = state.careerId == career.id && hasCareer;
                bool before = state.pastCareers.Contains(career.id);
                var c = NLUi.Panel(row, "Career_" + career.id, current ? new Color(0.2f, 0.18f, 0.1f, 1f) : NLUi.Card, new RectOffset(18, 18, 16, 16), 8f);
                NLUi.Label(c, "Name", $"{career.Name}\n<size=70%><color=#A8B4C4>{career.nameJa}</color></size>", 24f, NLUi.Gold, _font, FontStyles.Bold);
                NLUi.Label(c, "Workplace", career.Workplace, 16f, NLUi.Text, _font);
                NLUi.Label(c, "Schedule", career.Schedule, 16f, NLUi.Text, _font);
                NLUi.Label(c, "Description", career.Description, 15f, NLUi.Muted, _font);
                NLUi.Divider(c);
                NLUi.Label(c, "Start", Loc.F("Bắt đầu: {0} · {1}/ca", "Start: {0} · {1}/shift", "スタート：{0} · {1}/シフト", career.levels[0].Title, Loc.Yen(career.levels[0].salary)), 15f, NLUi.Soft, _font);
                if (career.branches.Length > 0)
                    NLUi.Label(c, "Branches", Loc.F("Bậc {0} chọn hướng: {1}", "Level {0} path: {1}", "レベル{0}でコース：{1}", career.branchAtLevel, string.Join(" / ", career.branches.Select(b => b.Name))), 15f, NLUi.Soft, _font);
                var top = career.branches.Select(b => b.levels.LastOrDefault()?.Title).Where(t => t != null);
                NLUi.Label(c, "Top", Loc.F("Đỉnh cao: {0}", "Top: {0}", "最高：{0}", string.Join(" / ", top)), 15f, NLUi.Muted, _font);
                if (before && !current) NLUi.Label(c, "Before", Loc.T("Bạn đã từng làm nghề này.", "You worked here before.", "以前この仕事をしました。"), 14f, NLUi.Muted, _font, FontStyles.Italic);

                string id = career.id;
                if (current)
                {
                    NLUi.Pill(c, "Current", Loc.T("Nghề hiện tại", "Your career", "今の仕事"), _font, NLUi.Gold, NLUi.Ink, 16f);
                    continue;
                }
                bool confirming = _switchTo == id;
                string label = !hasCareer ? Loc.T("Chọn nghề này", "Choose this career", "この仕事にする")
                    : confirming ? Loc.T("Chắc chắn đổi nghề?", "Really switch?", "本当に転職する？")
                    : Loc.T("Đổi sang nghề này", "Switch to this career", "この仕事に転職");
                var choose = NLUi.Button(c, "Choose_" + id, label, _font, () =>
                {
                    if (CareerService.Current != null)
                    {
                        if (_switchTo != id) { _switchTo = id; Rebuild(); return; }
                        _switchTo = null;
                        CareerService.Quit();
                    }
                    string why = CareerService.Choose(id);
                    if (why != null) HudFeed.Post(why, HudFeed.Kind.Warning, 5f);
                    else Show(Tab.Overview);
                }, CareerService.Unlocked ? (confirming ? new Color(0.45f, 0.16f, 0.15f) : NLUi.Gold) : new Color(1f, 1f, 1f, 0.08f), 18f,
                    CareerService.Unlocked ? (confirming ? Color.white : NLUi.Ink) : NLUi.Muted, 48f);
                choose.interactable = CareerService.Unlocked;
            }
        }

        // ─────────── Shared bits ───────────

        private static void Bar(RectTransform parent, string name, float value, Color color, float height)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            bar.GetComponent<LayoutElement>().preferredHeight = height;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            fill.SetParent(bar, false);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = color;
        }

        private static string TodayLine(CareerDefinition career, CareerState state)
        {
            var cycle = DayNightCycle.Active;
            if (cycle == null) return "";
            int day = cycle.Day;
            if (state.lastShiftDay == day) return Loc.T("Hôm nay: đã làm ca ✓", "Today: shift done ✓", "今日：シフト済み ✓");
            if (!CareerService.IsWorkDay(career, day)) return Loc.T("Hôm nay: ngày nghỉ", "Today: day off", "今日：休み");
            if (cycle.Hour < career.workStart) return Loc.F("Hôm nay: ca bắt đầu lúc {0}:00", "Today: shift starts at {0}:00", "今日：{0}:00からシフト", career.workStart);
            if (cycle.Hour < career.workEnd) return Loc.F("Đang trong giờ làm (đến {0}:00) — đi làm ngay!", "Working hours now (until {0}:00) — go to work!", "勤務時間中（{0}:00まで）— 仕事へ！", career.workEnd);
            return Loc.T("Hôm nay: đã hết giờ làm", "Today: working hours are over", "今日：勤務時間は終わり");
        }
    }
}
