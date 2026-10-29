using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>The 40 units of the chosen language: words, dialogue, reading, the grammar point and a graded practice set.</summary>
    public sealed class CourseLessonsView : CourseTabView
    {
        private const float LW = 330f;
        private string _unitId;
        private RectTransform _list;

        public CourseLessonsView(CoursePopup host) : base(host) { }

        public string SelectedUnitId => _unitId;
        public CourseUnit SelectedUnit => SchoolCatalog.Unit(_unitId);

        public void Select(string unitId)
        {
            if (SchoolCatalog.Unit(unitId) == null) return;
            _unitId = unitId;
            Show();
        }

        private CourseUnit Current()
        {
            var units = SchoolCatalog.Units(Host.Target);
            var u = units.Find(x => x.Id == _unitId);
            if (u == null)
            {
                var route = LearningPath.CurrentRoute();
                var next = route != null && route.Target == Host.Target ? LearningPath.NextStep(route) : null;
                u = next != null && next.Type == "unit" ? SchoolCatalog.Unit(next.Id) : null;
                u ??= units.FirstOrDefault();
                _unitId = u?.Id;
            }
            return u;
        }

        protected override void Draw()
        {
            var unit = Current();
            var listBg = UiKit.Card(Area, "ListBg", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(listBg.rectTransform, 0f, 0f, LW, H);
            _list = UiKit.Scroll(listBg.transform, "List", out _, 8f, new RectOffset(8, 8, 8, 8));
            UiKit.Fill((RectTransform)_list.parent.parent);
            var store = LearningProgressStore.Shared;
            var route = LearningPath.CurrentRoute();
            var next = route != null && route.Target == Host.Target ? LearningPath.NextStep(route) : null;
            foreach (var u in SchoolCatalog.Units(Host.Target))
            {
                var uu = u;
                var row = CourseUi.Row(_list, "Unit" + u.No, unit == u, CourseUi.LevelColor(u.Level), 66f, () => Select(uu.Id));
                var no = UiKit.Label(row.transform, "No", u.No.ToString(), 16f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(no.rectTransform, 10f, 0f, 36f, 66f);
                var t = UiKit.Label(row.transform, "Title", u.Title.Resolve(), 16f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(t.rectTransform, 50f, 6f, LW - 16f - 50f - 76f, 30f);
                var lv = UiKit.Chip(row.transform, "Level", u.Level, UiKit.Alpha(CourseUi.LevelColor(u.Level), 0.35f), UiTheme.Text, null, 12f);
                UiKit.Place(lv, 50f, 36f, 44f, 22f);
                if (next != null && next.Type == "unit" && next.Id == u.Id)
                {
                    var nx = UiKit.Chip(row.transform, "Next", "Tiếp", "Next", "次", UiKit.Alpha(UiTheme.Gold, 0.35f), UiTheme.Text, null, 12f);
                    UiKit.Place(nx, 100f, 36f, 56f, 22f);
                }
                var s = UiKit.Label(row.transform, "Stars", CourseUi.Stars(store.StarsFor(LearningPath.UnitKey(u.Id))), 15f, UiTheme.Gold, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
                UiKit.Place(s.rectTransform, LW - 16f - 80f, 34f, 76f, 26f);
            }
            if (unit != null) DrawDetail(unit, LW + 20f, W - LW - 20f);
        }

        private void DrawDetail(CourseUnit u, float x, float w)
        {
            var panel = UiKit.Card(Area, "Detail", UiTheme.Surface, 20, 0f, false);
            UiKit.Place(panel.rectTransform, x, 0f, w, H);
            var chip = UiKit.Chip(panel.transform, "Level", u.Level, CourseUi.LevelColor(u.Level), Color.white, "school", 13f);
            UiKit.Place(chip, 24f, 18f, 84f, 26f);
            var theme = UiKit.Label(panel.transform, "Theme", (u.Theme ?? "").Replace('_', ' '), 14f, UiTheme.Muted);
            UiKit.Place(theme.rectTransform, 118f, 18f, w - 118f - 280f, 26f);
            var title = UiKit.Label(panel.transform, "Title", u.No + ". " + u.Title.Resolve(), 28f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 24f, 48f, w - 48f - 250f, 42f);
            var entry = LearningProgressStore.Shared.Get(LearningPath.UnitKey(u.Id));
            string best = entry == null ? Loc.T("Chưa làm bài", "Not practised yet", "未挑戦") : Loc.F("Tốt nhất {0}/{1} · {2} lần", "Best {0}/{1} · {2} tries", "最高 {0}/{1}・{2}回", entry.bestCorrect, entry.total, entry.attempts);
            var bestLabel = UiKit.Label(panel.transform, "Best", CourseUi.Stars(entry?.stars ?? 0) + "  " + best, 15f, UiTheme.TextSoft);
            UiKit.Place(bestLabel.rectTransform, 24f, 92f, w - 48f - 250f, 24f);
            var start = UiKit.Button(panel.transform, "Practice", Loc.F("Luyện tập ({0} câu)", "Practice ({0} questions)", "練習（{0}問）", u.QuestionCount), StartPractice, UiKit.Kind.Primary, "quiz", 17f);
            UiKit.Place((RectTransform)start.transform, w - 24f - 240f, 30f, 240f, 52f);

            var host = UiKit.Rect(panel.transform, "Scroll");
            UiKit.Place(host, 14f, 126f, w - 28f, H - 126f - 12f);
            var content = CourseUi.ScrollIn(host);
            var body = CourseUi.Body(content);
            float bw = w - 28f - 4f - 20f, y = 8f;
            string target = Host.Target;
            string locale = CourseUi.LocaleOf(target);

            // words
            y = Head(body, "Từ vựng", "Vocabulary", "語彙", y, bw);
            float cols = bw >= 700f ? 3f : 2f, gap = 12f, cw = (bw - gap * (cols - 1)) / cols;
            for (int i = 0; i < u.Words.Count; i++)
            {
                var wd = u.Words[i];
                var c = UiKit.Card(body, "Word" + i, UiTheme.Raised, 12, 0f, false);
                UiKit.Place(c.rectTransform, (i % (int)cols) * (cw + gap), y + (i / (int)cols) * 78f, cw, 68f);
                var wt = UiKit.Label(c.transform, "Text", wd.Text, 21f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(wt.rectTransform, 14f, 4f, cw - 70f, 32f);
                string sub = string.IsNullOrEmpty(wd.Reading) || wd.Reading == wd.Text ? "" : wd.Reading;
                var gl = UiKit.Label(c.transform, "Gloss", (sub.Length > 0 ? sub + "  ·  " : "") + (wd.Gloss.Gloss(target) ?? ""), 14f, UiTheme.TextSoft);
                UiKit.Place(gl.rectTransform, 14f, 36f, cw - 24f, 26f);
                CourseUi.SpeakButton(c.transform, wd.Speech.IsEmpty ? wd.Text : wd.Speech.Text, wd.Speech.Locale ?? locale, cw - 50f, 6f);
            }
            y += ((u.Words.Count + (int)cols - 1) / (int)cols) * 78f + 8f;

            // dialogue
            if (u.Dialogue.Count > 0)
            {
                y = Head(body, "Hội thoại", "Dialogue", "会話", y, bw);
                foreach (var line in u.Dialogue)
                {
                    var c = UiKit.Card(body, "Line", UiTheme.Raised, 14, 0f, false);
                    var sp = UiKit.Label(c.transform, "Speaker", line.Speaker ?? "", 14f, UiTheme.Gold, FontStyles.Bold);
                    UiKit.Place(sp.rectTransform, 16f, 8f, 120f, 24f);
                    CourseUi.Para(c.transform, "Text", line.Text, 20f, UiTheme.Text, 140f, 8f, bw - 140f - 70f, out float th, FontStyles.Bold);
                    float ch = 8f + th;
                    string tr = line.Translation.IsEmpty ? null : line.Translation.Gloss(target);
                    if (!string.IsNullOrEmpty(tr))
                    {
                        CourseUi.Para(c.transform, "Tr", tr, 15f, UiTheme.TextSoft, 140f, ch + 2f, bw - 140f - 70f, out float trh);
                        ch += trh + 2f;
                    }
                    CourseUi.SpeakButton(c.transform, line.Speech.IsEmpty ? line.Text : line.Speech.Text, line.Speech.Locale ?? locale, bw - 52f, 8f);
                    float h = Mathf.Max(52f, ch + 10f);
                    UiKit.Place(c.rectTransform, 0f, y, bw, h);
                    y += h + 8f;
                }
                y += 6f;
            }

            // reading
            if (!string.IsNullOrEmpty(u.ReadingText))
            {
                y = Head(body, "Đọc hiểu", "Reading", "読解", y, bw);
                var c = UiKit.Card(body, "Reading", UiTheme.Raised, 14, 0f, false);
                float ch = 14f;
                if (!u.ReadingTitle.IsEmpty)
                {
                    CourseUi.Para(c.transform, "RTitle", u.ReadingTitle.Resolve(), 18f, UiTheme.Gold, 18f, ch, bw - 36f, out float rh, FontStyles.Bold);
                    ch += rh + 4f;
                }
                CourseUi.Para(c.transform, "RText", u.ReadingText, 19f, UiTheme.Text, 18f, ch, bw - 70f, out float tx);
                CourseUi.SpeakButton(c.transform, u.ReadingText, locale, bw - 52f, 10f);
                ch += tx + 14f;
                UiKit.Place(c.rectTransform, 0f, y, bw, ch);
                y += ch + 16f;
            }

            // grammar
            var g = SchoolCatalog.GrammarPoint(target, u.GrammarId);
            if (g != null)
            {
                y = Head(body, "Ngữ pháp", "Grammar", "文法", y, bw);
                var c = UiKit.Card(body, "Grammar", UiTheme.Raised, 14, 0f, false);
                CourseUi.Para(c.transform, "Pattern", g.Pattern, 24f, UiTheme.Gold, 18f, 14f, bw - 36f, out float ph, FontStyles.Bold);
                float ch = 14f + ph + 4f;
                if (!g.Title.IsEmpty) { CourseUi.Para(c.transform, "GTitle", g.Title.Resolve(), 17f, UiTheme.Text, 18f, ch, bw - 36f, out float gh, FontStyles.Bold); ch += gh + 4f; }
                if (!g.Explanation.IsEmpty) { CourseUi.Para(c.transform, "Expl", g.Explanation.Resolve(), 16f, UiTheme.TextSoft, 18f, ch, bw - 36f, out float eh); ch += eh + 6f; }
                if (!string.IsNullOrEmpty(g.ExampleText))
                {
                    CourseUi.Para(c.transform, "Ex", g.ExampleText, 19f, UiTheme.Text, 18f, ch, bw - 36f, out float xh, FontStyles.Bold); ch += xh + 2f;
                    string tr = g.ExampleTranslation.IsEmpty ? null : g.ExampleTranslation.Gloss(target);
                    if (!string.IsNullOrEmpty(tr)) { CourseUi.Para(c.transform, "ExTr", tr, 15f, UiTheme.Gold, 18f, ch, bw - 36f, out float xth); ch += xth + 2f; }
                }
                ch += 12f;
                UiKit.Place(c.rectTransform, 0f, y, bw, ch);
                y += ch + 16f;
            }

            // speaking
            if (!u.SpeakingPrompt.IsEmpty)
            {
                y = Head(body, "Nói", "Speaking", "スピーキング", y, bw);
                var c = UiKit.Card(body, "Speaking", UiTheme.Raised, 14, 0f, false);
                CourseUi.Para(c.transform, "Prompt", u.SpeakingPrompt.Resolve(), 18f, UiTheme.Text, 18f, 14f, bw - 36f, out float sph);
                float ch = 14f + sph + 6f;
                var model = u.SpeakingModelLineIds.Select(id => u.Line(id)).Where(l => l != null).ToList();
                foreach (var l in model)
                {
                    CourseUi.Para(c.transform, "Model", "▸ " + l.Text, 17f, UiTheme.Gold, 18f, ch, bw - 36f, out float mh, FontStyles.Bold);
                    ch += mh + 3f;
                }
                ch += 12f;
                UiKit.Place(c.rectTransform, 0f, y, bw, ch);
                y += ch + 16f;
            }
            CourseUi.EndBody(body, y + 10f);
        }

        private static float Head(RectTransform body, string vi, string en, string ja, float y, float bw)
        {
            var l = UiKit.Label(body, "Head", vi, en, ja, 17f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(l.rectTransform, 0f, y, bw, 26f);
            return y + 32f;
        }

        public void StartPractice()
        {
            var u = Current();
            if (u == null) return;
            var qs = u.Exercises.SelectMany(e => e.Questions).ToList();
            StartQuiz(qs, new CourseQuizOptions
            {
                Title = u.No + ". " + u.Title.Resolve(), StoreKey = LearningPath.UnitKey(u.Id), Target = u.Target, Accent = CourseUi.LevelColor(u.Level),
            });
        }

        protected override void CloseQuiz() { base.CloseQuiz(); Host.RefreshTabBadges(); }
    }
}