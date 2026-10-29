using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>"Lộ trình": pick the language, goal and starting level (or take the placement test), then follow the route step by step.</summary>
    public sealed class CoursePathView : CourseTabView
    {
        private enum Mode { Main, PlacementIntro, PlacementResult }

        private Mode _mode = Mode.Main;
        private CourseQuizResult _placement;
        private string _placementLevel;

        public CoursePathView(CoursePopup host) : base(host) { }

        public string PlacementLevel => _placementLevel;
        public bool ShowingPlacementResult => _mode == Mode.PlacementResult;

        protected override void Draw()
        {
            switch (_mode)
            {
                case Mode.PlacementIntro: DrawPlacementIntro(); break;
                case Mode.PlacementResult: DrawPlacementResult(); break;
                default: DrawMain(); break;
            }
        }

        public override bool HandleBack()
        {
            if (Quiz != null) { CloseQuiz(); return true; }
            if (_mode != Mode.Main) { _mode = Mode.Main; Show(); return true; }
            return false;
        }

        protected override void CloseQuiz() { Quiz = null; _mode = Mode.PlacementIntro; UiKit.Clear(Area); Draw(); }

        // ---------------- main ----------------

        private void DrawMain()
        {
            const float LW = 410f;
            var left = UiKit.Card(Area, "Settings", UiTheme.Surface, 18, 0f, false);
            UiKit.Place(left.rectTransform, 0f, 0f, LW, H);
            float y = 14f, x = 16f, iw = LW - 32f;

            Section(left.transform, "Ngôn ngữ muốn học", "Language to learn", "学ぶ言語", x, ref y);
            float bw = (iw - 10f) / 2f;
            for (int i = 0; i < SchoolCatalog.Targets.Length; i++)
            {
                string t = SchoolCatalog.Targets[i];
                var b = UiKit.Button(left.transform, "Target_" + t, LearningPath.TargetName(t).Resolve(), () => { Host.Draft.Target = t; Host.Draft.Level = Host.Draft.Level ?? "A1"; Host.SetTarget(t); Redraw(); },
                    Host.Draft.Target == t ? UiKit.Kind.Primary : UiKit.Kind.Secondary, null, 16f);
                UiKit.Place((RectTransform)b.transform, x + (i % 2) * (bw + 10f), y + (i / 2) * 48f, bw, 40f);
            }
            y += 100f;

            Section(left.transform, "Mục tiêu", "Your goal", "目標", x, ref y);
            var goals = LearningPath.Goals();
            for (int i = 0; i < goals.Count; i++)
            {
                string id = goals[i].Id;
                var b = UiKit.Button(left.transform, "Goal_" + id, goals[i].Label.Resolve(), () => { Host.Draft.Goal = id; Redraw(); },
                    Host.Draft.Goal == id ? UiKit.Kind.Primary : UiKit.Kind.Secondary, null, 14f);
                UiKit.Place((RectTransform)b.transform, x + (i % 2) * (bw + 10f), y + (i / 2) * 44f, bw, 38f);
            }
            y += ((goals.Count + 1) / 2) * 44f + 10f;

            Section(left.transform, "Trình độ bắt đầu", "Starting level", "開始レベル", x, ref y);
            float lw = (iw - 4f * 6f) / 5f;
            for (int i = 0; i < SchoolCatalog.Levels.Length; i++)
            {
                string lv = SchoolCatalog.Levels[i];
                var b = UiKit.Button(left.transform, "Level_" + lv, LearningPath.LevelLabel(Host.Draft.Target, lv), () => { Host.Draft.Level = lv; Redraw(); },
                    Host.Draft.Level == lv ? UiKit.Kind.Accent : UiKit.Kind.Secondary, null, 12.5f, 10);
                UiKit.Place((RectTransform)b.transform, x + i * (lw + 6f), y, lw, 42f);
            }
            y += 56f;

            var place = UiKit.Button(left.transform, "Placement", "Kiểm tra trình độ (20 câu)", "Placement test (20 questions)", "レベルチェック（20問）", () => { _mode = Mode.PlacementIntro; Show(); }, UiKit.Kind.Ghost, "quiz", 15f);
            UiKit.Place((RectTransform)place.transform, x, y, iw, 44f);
            var save = UiKit.Button(left.transform, "Save", "Dùng lộ trình này", "Use this path", "このコースにする", SaveDraft, UiKit.Kind.Primary, "flag", 17f);
            UiKit.Place((RectTransform)save.transform, x, y + 52f, iw, 48f);

            DrawRoute(LW + 20f, W - LW - 20f);
        }

        private void Section(Transform parent, string vi, string en, string ja, float x, ref float y)
        {
            var l = UiKit.Label(parent, "Section", vi, en, ja, 14f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(l.rectTransform, x, y, 300f, 22f);
            y += 28f;
        }

        public void SaveDraft()
        {
            var d = Host.Draft;
            if (!d.IsSet) return;
            LearningPath.Save(new LearningPath.Choice { Target = d.Target, Goal = d.Goal, StartLevel = d.Level });
            Host.SetTarget(d.Target);
            Redraw();
        }

        private void DrawRoute(float x, float w)
        {
            var panel = UiKit.Card(Area, "Route", UiTheme.Surface, 18, 0f, false);
            UiKit.Place(panel.rectTransform, x, 0f, w, H);
            var d = Host.Draft;
            var route = d.IsSet ? LearningPath.Route(d.Target, d.Goal, d.Level) : null;
            var saved = LearningPath.Current;
            bool isSaved = saved.IsSet && saved.Target == d.Target && saved.Goal == d.Goal && saved.StartLevel == d.Level;
            if (route == null)
            {
                var none = UiKit.Label(panel.transform, "None", "Chọn ngôn ngữ, mục tiêu và trình độ để xem lộ trình.", "Pick a language, goal and level to see the route.", "言語・目標・レベルを選ぶとコースが表示されます。", 18f, UiTheme.Muted);
                UiKit.Place(none.rectTransform, 24f, 24f, w - 48f, 40f);
                return;
            }
            var title = UiKit.Label(panel.transform, "Title", route.Title.Resolve(), 22f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 20f, 12f, w - 40f - (isSaved ? 130f : 0f), 34f);
            if (isSaved)
            {
                var chip = UiKit.Chip(panel.transform, "Active", "Đang học", "Your path", "学習中", UiKit.Alpha(UiTheme.Good, 0.3f), UiTheme.Text, "check_circle", 13f);
                UiKit.Place(chip, w - 20f - 120f, 16f, 120f, 28f);
            }
            int done = route.Steps.Count(s => LearningPath.IsDone(s));
            var bar = UiKit.ProgressBar(panel.transform, "Progress", UiTheme.Good, UiTheme.Sunken);
            UiKit.Place(bar.parent as RectTransform ?? bar, 20f, 54f, w - 40f - 150f, 10f);
            UiKit.SetProgress(bar, route.Steps.Count == 0 ? 0f : (float)done / route.Steps.Count);
            var count = UiKit.Label(panel.transform, "Count", Loc.F("{0}/{1} bước", "{0}/{1} steps", "{0}/{1}ステップ", done, route.Steps.Count), 15f, UiTheme.TextSoft, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Place(count.rectTransform, w - 20f - 140f, 46f, 140f, 26f);

            var next = LearningPath.NextStep(route);
            float top = 80f;
            if (next != null)
            {
                var nb = UiKit.Card(panel.transform, "Next", UiKit.Alpha(UiTheme.Gold, 0.14f), 14, 0f, false);
                UiKit.Place(nb.rectTransform, 20f, top, w - 40f, 56f);
                var nl = UiKit.Label(nb.transform, "Label", Loc.T("Bước tiếp theo: ", "Next up: ", "次のステップ：") + LearningPath.StepTitle(next), 17f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(nl.rectTransform, 16f, 0f, w - 40f - 32f - 190f, 56f);
                var go = UiKit.Button(nb.transform, "Go", "Học ngay", "Continue", "続ける", () => Host.OpenStep(next), UiKit.Kind.Primary, "arrow_forward", 15f);
                UiKit.Place((RectTransform)go.transform, w - 40f - 16f - 170f, 8f, 170f, 40f);
                top += 66f;
            }
            else
            {
                var all = UiKit.Label(panel.transform, "AllDone", "Bạn đã hoàn thành cả lộ trình — chúc mừng!", "You finished the whole route — congratulations!", "コースを全部終えました—おめでとう！", 17f, UiTheme.Good, FontStyles.Bold);
                UiKit.Place(all.rectTransform, 20f, top, w - 40f, 34f);
                top += 44f;
            }

            var host = UiKit.Rect(panel.transform, "Steps");
            UiKit.Place(host, 12f, top, w - 24f, H - top - 12f);
            var content = CourseUi.ScrollIn(host, 8f, new RectOffset(0, 0, 0, 0));
            int n = 0;
            foreach (var step in route.Steps)
            {
                var s = step; n++;
                bool isDone = LearningPath.IsDone(s);
                var row = CourseUi.Row(content, "Step" + n, next == s, UiTheme.Gold, 60f, () => Host.OpenStep(s));
                var num = UiKit.Label(row.transform, "No", n.ToString(), 15f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(num.rectTransform, 12f, 0f, 36f, 60f);
                string icon = s.Type == "alphabet" ? "translate" : s.Type == "certificate" ? "workspace_premium" : "menu_book";
                var ic = UiKit.Icon(row.transform, icon, s.Type == "certificate" ? UiTheme.Gold : UiTheme.Teal, 26f);
                UiKit.Place(ic.rectTransform, 54f, 17f, 26f, 26f);
                var t = UiKit.Label(row.transform, "Title", LearningPath.StepTitle(s), 17f, isDone ? UiTheme.TextSoft : UiTheme.Text, FontStyles.Bold);
                UiKit.Place(t.rectTransform, 92f, 0f, w - 24f - 92f - 190f, 60f);
                if (s.Priority)
                {
                    var pc = UiKit.Chip(row.transform, "Priority", "Ưu tiên", "Priority", "優先", UiKit.Alpha(UiTheme.Gold, 0.3f), UiTheme.Text, "star", 12f);
                    UiKit.Place(pc, w - 24f - 170f, 16f, 90f, 28f);
                }
                var st = UiKit.Label(row.transform, "State", isDone ? "✓" : "", 22f, UiTheme.Good, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
                UiKit.Place(st.rectTransform, w - 24f - 70f, 0f, 50f, 60f);
            }
        }

        // ---------------- placement ----------------

        private void DrawPlacementIntro()
        {
            var p = SchoolCatalog.Placement(Host.Target);
            var card = UiKit.Card(Area, "Intro", UiTheme.Surface, 18, 0f, false);
            UiKit.Place(card.rectTransform, 0f, 0f, W, H);
            var title = UiKit.Label(card.transform, "Title", "Kiểm tra trình độ", "Placement test", "レベルチェック", 28f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 32f, 24f, W - 64f, 44f);
            var name = UiKit.Label(card.transform, "Lang", LearningPath.TargetName(Host.Target).Resolve(), 20f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(name.rectTransform, 32f, 72f, W - 64f, 30f);
            float y = 116f;
            if (p != null)
            {
                CourseUi.Para(card.transform, "Instr", p.Instructions.Resolve(), 19f, UiTheme.TextSoft, 32f, y, W - 64f, out float ih);
                y += ih + 18f;
                CourseUi.Para(card.transform, "Info", Loc.F("{0} câu, từ dễ đến khó. Làm đến đâu hay đến đó; chọn “Tôi không biết” khi chưa biết.",
                    "{0} questions, from easy to hard. Answer what you can; choose “I don't know” when you are unsure.",
                    "{0}問、やさしい順。わかるところまで答え、わからなければ「わからない」を選びます。", p.Questions.Count), 17f, UiTheme.Muted, 32f, y, W - 64f, out float nh);
                y += nh + 26f;
                var start = UiKit.Button(card.transform, "Start", "Bắt đầu", "Start", "はじめる", StartPlacement, UiKit.Kind.Primary, "play_arrow", 18f);
                UiKit.Place((RectTransform)start.transform, 32f, y, 220f, 54f);
            }
            var back = UiKit.Button(card.transform, "Back", "Quay lại", "Back", "戻る", () => { _mode = Mode.Main; Show(); }, UiKit.Kind.Ghost, "arrow_back", 15f);
            UiKit.Place((RectTransform)back.transform, 270f, y, 160f, 54f);
        }

        public void StartPlacement()
        {
            var p = SchoolCatalog.Placement(Host.Target);
            if (p == null) return;
            StartQuiz(new List<CourseQuestion>(p.Questions), new CourseQuizOptions
            {
                Title = Loc.T("Kiểm tra trình độ", "Placement test", "レベルチェック") + " · " + LearningPath.TargetName(Host.Target).Resolve(),
                Target = Host.Target, Accent = CourseUi.TargetColor(Host.Target), SuppressSummary = true, AllowDontKnow = true, OnFinished = FinishPlacement,
            });
        }

        private void FinishPlacement(CourseQuizResult result)
        {
            var p = SchoolCatalog.Placement(Host.Target);
            var rights = new Dictionary<string, int>();
            foreach (var q in p.Questions)
                if (result.ById.TryGetValue(q.Id, out bool ok) && ok) { rights.TryGetValue(q.GroupId ?? "", out int n); rights[q.GroupId ?? ""] = n + 1; }
            int idx = CourseGrader.PlacementLevelIndex(SchoolCatalog.Levels, rights, p.PassPerLevel);
            _placementLevel = SchoolCatalog.Levels[Mathf.Max(0, idx)];
            _placement = result;
            Quiz = null; _mode = Mode.PlacementResult;
            UiKit.Clear(Area); Draw();
        }

        private void DrawPlacementResult()
        {
            var p = SchoolCatalog.Placement(Host.Target);
            var level = p.Levels.Find(l => l.Cefr == _placementLevel);
            var card = UiKit.Card(Area, "Result", UiTheme.Surface, 18, 0f, false);
            UiKit.Place(card.rectTransform, 0f, 0f, W, H);
            var head = UiKit.Label(card.transform, "Head", "Trình độ của bạn", "Your level", "あなたのレベル", 20f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(head.rectTransform, 0f, 40f, W, 30f);
            var big = UiKit.Label(card.transform, "Level", LearningPath.LevelLabel(Host.Target, _placementLevel) + "  (" + _placementLevel + ")", 56f, CourseUi.LevelColor(_placementLevel), FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(big.rectTransform, 0f, 78f, W, 78f);
            var score = UiKit.Label(card.transform, "Score", _placement.Correct + " / " + _placement.Total, 24f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(score.rectTransform, 0f, 160f, W, 36f);
            float y = 210f;
            var unit = level != null ? SchoolCatalog.Unit(level.StartUnitId) : null;
            if (unit != null)
            {
                CourseUi.Para(card.transform, "Start", Loc.T("Bạn sẽ bắt đầu từ: ", "You will start at: ", "ここから始めます：") + unit.No + ". " + unit.Title.Resolve(), 19f, UiTheme.Gold, 120f, y, W - 240f, out float sh, FontStyles.Bold, TextAlignmentOptions.Top);
                y += sh + 10f;
            }
            CourseUi.Para(card.transform, "Note", Loc.T("Đây là bài xếp lớp để gợi ý điểm bắt đầu, không phải điểm thi chính thức.", "This is a placement guide for where to start, not an official score.", "これは開始地点の目安で、公式のスコアではありません。"), 16f, UiTheme.Muted, 120f, y, W - 240f, out float nh, FontStyles.Normal, TextAlignmentOptions.Top);
            y += nh + 28f;
            var use = UiKit.Button(card.transform, "Use", "Dùng trình độ này", "Use this level", "このレベルで始める", UsePlacement, UiKit.Kind.Primary, "flag", 18f);
            UiKit.Place((RectTransform)use.transform, W / 2f - 280f, y, 270f, 54f);
            var again = UiKit.Button(card.transform, "Again", "Làm lại bài", "Retake", "もう一度", () => { _mode = Mode.PlacementIntro; Show(); }, UiKit.Kind.Secondary, "refresh", 16f);
            UiKit.Place((RectTransform)again.transform, W / 2f + 10f, y, 220f, 54f);
        }

        public void UsePlacement()
        {
            if (string.IsNullOrEmpty(_placementLevel)) return;
            Host.Draft.Target = Host.Target;
            Host.Draft.Level = _placementLevel;
            if (string.IsNullOrEmpty(Host.Draft.Goal)) Host.Draft.Goal = "daily";
            _mode = Mode.Main;
            SaveDraft();
            Show();
        }
    }
}