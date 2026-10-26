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
    /// The career window. Without a career: the three careers side by side (workplace, schedule, first title and
    /// salary, the two paths at the branch) and "Choose". With one: title, level, the performance bar, salary,
    /// today's shift, the whole ladder with the current rung, what the next promotion needs, "Go to work" (opens the
    /// shift in the task journal) and "Quit". Opened once at level 10, and from the task journal.
    /// </summary>
    public sealed class CareerUI : MonoBehaviour
    {
        private static CareerUI _instance;
        private TMP_FontAsset _font;
        private RectTransform _root, _body;
        private PlayerController _player;
        private bool _wasLocked;
        private bool _confirmQuit;
        private float _nextRefresh;

        public static bool IsOpen => _instance != null && _instance._root != null && _instance._root.gameObject.activeSelf;

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

        public static void Open()
        {
            var ui = Instance;
            ui._confirmQuit = false;
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
            NLUi.CloseButton(card, _font, Close, 46f, 18f);

            _body = NLUi.Group(card, "Body", true, 12f);
            _body.anchorMin = new Vector2(0f, 1f); _body.anchorMax = new Vector2(1f, 1f); _body.pivot = new Vector2(0.5f, 1f);
            _body.offsetMin = new Vector2(30f, 0f); _body.offsetMax = new Vector2(-80f, -22f);
            NLUi.FitContent(_body);
            _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Today's status follows the clock while the window is open.
            if (IsOpen && CareerService.Current != null && Time.unscaledTime > _nextRefresh) { _nextRefresh = Time.unscaledTime + 2f; Rebuild(); }
        }

        private void Rebuild()
        {
            foreach (Transform child in _body) Destroy(child.gameObject);
            if (CareerService.Current == null) BuildChoice();
            else BuildCareer();
        }

        // ─────────── No career: choose one ───────────

        private void BuildChoice()
        {
            var catalog = CareerService.Catalog;
            NLUi.Label(_body, "Title", Loc.T("Hướng nghiệp", "Choose a career", "職業を選ぶ"), 30f, NLUi.Text, _font, FontStyles.Bold);
            string reason = CareerService.Unlocked ? null : Loc.F("Mở khi đạt cấp {0} — bạn đang cấp {1}.", "Opens at level {0} — you are level {1}.", "レベル{0}で開きます — 今はレベル{1}。", catalog.unlockLevel, CareerService.PlayerLevel);
            NLUi.Label(_body, "Intro", reason ?? Loc.T("Chọn một nghề: làm ca đúng giờ để tăng hiệu suất, qua phỏng vấn tiếng Nhật để thăng chức, đến bậc 4 thì chọn hướng đi riêng.",
                "Pick a career: work your shifts on time to raise performance, pass a Japanese interview to be promoted, choose your own path at level 4.",
                "仕事を選びましょう。時間どおりに働いて評価を上げ、日本語の面接で昇進。レベル4で自分のコースを選びます。"), 17f, reason != null ? NLUi.Bad : NLUi.Muted, _font).textWrappingMode = TextWrappingModes.Normal;

            var row = NLUi.Group(_body, "Careers", false, 14f, TextAnchor.UpperLeft, true);
            foreach (var career in catalog.careers)
            {
                var c = NLUi.Panel(row, "Career_" + career.id, NLUi.Card, new RectOffset(18, 18, 16, 16), 8f);
                NLUi.Size(c, 330f, 470f, 1f);
                NLUi.Label(c, "Name", $"{career.Name}\n<size=70%><color=#A8B4C4>{career.nameJa}</color></size>", 24f, NLUi.Gold, _font, FontStyles.Bold);
                NLUi.Label(c, "Workplace", career.Workplace, 16f, NLUi.Text, _font);
                NLUi.Label(c, "Schedule", career.Schedule, 16f, NLUi.Text, _font);
                NLUi.Label(c, "Description", career.Description, 15f, NLUi.Muted, _font).textWrappingMode = TextWrappingModes.Normal;
                NLUi.Divider(c);
                NLUi.Label(c, "Start", Loc.F("Bắt đầu: {0} · {1}/ca", "Start: {0} · {1}/shift", "スタート：{0} · {1}/シフト", career.levels[0].Title, Loc.Yen(career.levels[0].salary)), 15f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;
                if (career.branches.Length > 0)
                    NLUi.Label(c, "Branches", Loc.F("Bậc {0} chọn hướng: {1}", "Level {0} path: {1}", "レベル{0}でコース：{1}", career.branchAtLevel, string.Join(" / ", career.branches.Select(b => b.Name))), 15f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;
                var top = career.branches.Select(b => b.levels.LastOrDefault()?.Title).Where(t => t != null);
                NLUi.Label(c, "Top", Loc.F("Đỉnh cao: {0}", "Top: {0}", "最高：{0}", string.Join(" / ", top)), 15f, NLUi.Muted, _font).textWrappingMode = TextWrappingModes.Normal;
                string id = career.id;
                var choose = NLUi.Button(c, "Choose_" + id, Loc.T("Chọn nghề này", "Choose this career", "この仕事にする"), _font, () =>
                {
                    string why = CareerService.Choose(id);
                    if (why != null) HudFeed.Post(why, HudFeed.Kind.Warning, 5f);
                }, CareerService.Unlocked ? NLUi.Gold : new Color(1f, 1f, 1f, 0.08f), 18f, CareerService.Unlocked ? NLUi.Ink : NLUi.Muted, 48f);
                choose.interactable = CareerService.Unlocked;
            }
        }

        // ─────────── With a career ───────────

        private void BuildCareer()
        {
            var career = CareerService.Current;
            var state = CareerService.State;
            var level = CareerService.CurrentLevel;
            var branch = career.Branch(state.branchId);
            NLUi.Label(_body, "Title", $"{level.Title}  <size=60%><color=#A8B4C4>{level.titleJa}</color></size>", 30f, NLUi.Text, _font, FontStyles.Bold);
            NLUi.Label(_body, "Career", $"{career.Name}{(branch != null ? " · " + branch.Name : "")} · {career.Workplace} · " +
                Loc.F("bậc {0}/{1}", "level {0}/{1}", "レベル{0}/{1}", state.level, career.MaxLevel), 17f, NLUi.Muted, _font);

            var columns = NLUi.Group(_body, "Columns", false, 22f, TextAnchor.UpperLeft, true);
            var left = NLUi.Group(columns, "Left", true, 10f);
            NLUi.Size(left, 560f, -1f, 1f);
            var right = NLUi.Group(columns, "Right", true, 6f);
            NLUi.Size(right, 440f);

            // Performance bar.
            NLUi.Label(left, "PerformanceLabel", Loc.F("Hiệu suất {0:0}%", "Performance {0:0}%", "評価 {0:0}%", state.performance), 18f, NLUi.Text, _font, FontStyles.Bold);
            var bar = new GameObject("PerformanceBar", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            bar.SetParent(left, false);
            bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            bar.GetComponent<LayoutElement>().preferredHeight = 18f;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            fill.SetParent(bar, false);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01(state.performance / 100f), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = state.performance >= 99.5f ? NLUi.Gold : NLUi.Good;
            string mood = CareerService.MoodFactor > 1f ? Loc.T("Tâm trạng tốt: làm hiệu quả hơn (×1.25).", "Good mood: working better (×1.25).", "気分がいい：よく働ける（×1.25）。")
                : CareerService.MoodFactor < 1f ? Loc.T("Đang mệt / căng / bực: hiệu suất ca này giảm (×0.75).", "Tired / tense / annoyed: this shift counts less (×0.75).", "疲れ・緊張・イライラ：評価が下がる（×0.75）。")
                : Loc.T("Tâm trạng bình thường.", "Normal mood.", "ふつうの気分。");
            NLUi.Label(left, "Mood", mood, 15f, NLUi.Muted, _font);
            string blocker = CareerService.PromotionBlocker();
            NLUi.Label(left, "Next", blocker == null ? Loc.T("Sẵn sàng phỏng vấn thăng chức!", "Ready for the promotion interview!", "昇進面接の準備ができました！")
                : Loc.T("Thăng chức: ", "Promotion: ", "昇進：") + blocker, 16f, blocker == null ? NLUi.Gold : NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;

            NLUi.Divider(left);
            NLUi.Label(left, "Salary", Loc.F("Lương nghề: {0} / ca đúng giờ (cộng thêm tiền công ca làm)", "Salary: {0} per on-time shift (on top of the shift's pay)", "給料：時間どおりのシフトごとに{0}（シフト代に加えて）", Loc.Yen(level.salary)), 16f, NLUi.Text, _font).textWrappingMode = TextWrappingModes.Normal;
            NLUi.Label(left, "Schedule", Loc.T("Lịch làm: ", "Schedule: ", "勤務：") + career.Schedule, 16f, NLUi.Text, _font);
            NLUi.Label(left, "Today", TodayLine(career, state), 16f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;

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

            // The ladder: shared levels, then the chosen branch (or both branches before the split).
            NLUi.Label(right, "LadderTitle", Loc.T("Nấc thang nghề", "Career ladder", "キャリアのはしご"), 18f, NLUi.Gold, _font, FontStyles.Bold);
            int rung = 0;
            foreach (var l in career.levels) Rung(right, ++rung, l, state.level);
            var shown = branch != null ? new[] { branch } : career.branches;
            foreach (var b in shown)
            {
                NLUi.Label(right, "Branch_" + b.id, "→ " + b.Name + (branch == null ? "  <size=80%><color=#A8B4C4>" + b.Description + "</color></size>" : ""), 15f, NLUi.Soft, _font).textWrappingMode = TextWrappingModes.Normal;
                int r = career.levels.Length;
                foreach (var l in b.levels) Rung(right, ++r, l, branch != null ? state.level : 0);
            }
        }

        private void Rung(RectTransform parent, int index, CareerLevelDefinition level, int current)
        {
            bool here = index == current, done = index < current;
            string mark = here ? "▶" : done ? "✓" : "·";
            NLUi.Label(parent, "Rung_" + index, $"{mark} {index}. {level.Title}  <size=75%><color=#A8B4C4>{level.titleJa} · {Loc.Yen(level.salary)}</color></size>", 15f,
                here ? NLUi.Gold : done ? NLUi.Good : NLUi.Text, _font, here ? FontStyles.Bold : FontStyles.Normal);
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
