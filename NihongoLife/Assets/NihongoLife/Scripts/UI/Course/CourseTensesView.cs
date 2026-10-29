using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>The 12 English tenses: formula, usage, signal words, examples, the typical mistake, a conjugation table and graded practice.</summary>
    public sealed class CourseTensesView : CourseTabView
    {
        private const float LW = 330f;
        private string _tenseId;
        private int _verb;

        public CourseTensesView(CoursePopup host) : base(host) { }

        public string SelectedTenseId => _tenseId;

        public void Select(string tenseId) { _tenseId = tenseId; Show(); }

        public void ChooseVerb(int index)
        {
            var verbs = TensesCatalog.Verbs();
            if (verbs.Count == 0) return;
            _verb = ((index % verbs.Count) + verbs.Count) % verbs.Count;
            Show();
        }

        public int VerbIndex => _verb;

        protected override void Draw()
        {
            var lessons = TensesCatalog.Lessons();
            var lesson = lessons.Find(l => l.TenseId == _tenseId) ?? lessons.FirstOrDefault();
            if (lesson == null) return;
            _tenseId = lesson.TenseId;
            var store = LearningProgressStore.Shared;

            var listBg = UiKit.Card(Area, "ListBg", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(listBg.rectTransform, 0f, 0f, LW, H);
            var list = UiKit.Scroll(listBg.transform, "List", out _, 8f, new RectOffset(8, 8, 8, 8));
            UiKit.Fill((RectTransform)list.parent.parent);
            foreach (var l in lessons)
            {
                var ll = l;
                var row = CourseUi.Row(list, "Tense_" + l.TenseId, l == lesson, CourseUi.LevelColor(l.Cefr), 62f, () => Select(ll.TenseId));
                var no = UiKit.Label(row.transform, "No", l.Order.ToString(), 15f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(no.rectTransform, 8f, 0f, 36f, 62f);
                var t = UiKit.Label(row.transform, "Title", l.Title.Resolve(), 16f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(t.rectTransform, 48f, 4f, LW - 16f - 48f - 90f, 30f);
                var lv = UiKit.Chip(row.transform, "Level", l.Cefr, UiKit.Alpha(CourseUi.LevelColor(l.Cefr), 0.35f), UiTheme.Text, null, 12f);
                UiKit.Place(lv, 48f, 34f, 44f, 22f);
                var s = UiKit.Label(row.transform, "Stars", CourseUi.Stars(store.StarsFor(LearningPath.TenseKey(l.TenseId))), 15f, UiTheme.Gold, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
                UiKit.Place(s.rectTransform, LW - 16f - 80f, 32f, 76f, 26f);
            }
            DrawDetail(lesson, LW + 20f, W - LW - 20f);
        }

        private void DrawDetail(TenseLesson l, float x, float w)
        {
            var panel = UiKit.Card(Area, "Detail", UiTheme.Surface, 20, 0f, false);
            UiKit.Place(panel.rectTransform, x, 0f, w, H);
            var chip = UiKit.Chip(panel.transform, "Level", l.Cefr, CourseUi.LevelColor(l.Cefr), Color.white, "schedule", 13f);
            UiKit.Place(chip, 24f, 18f, 84f, 26f);
            var title = UiKit.Label(panel.transform, "Title", l.Order + ". " + l.Title.Resolve(), 27f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 24f, 46f, w - 48f - 480f, 42f);
            var e = LearningProgressStore.Shared.Get(LearningPath.TenseKey(l.TenseId));
            var stars = UiKit.Label(panel.transform, "Stars", CourseUi.Stars(e?.stars ?? 0) + (e == null ? "" : "  " + e.bestCorrect + "/" + e.total), 15f, UiTheme.Gold);
            UiKit.Place(stars.rectTransform, 24f, 92f, 300f, 24f);
            var p1 = UiKit.Button(panel.transform, "Practice", Loc.F("Luyện tập ({0})", "Practice ({0})", "練習（{0}）", TensesCatalog.Exercises(l.TenseId).Count), StartPractice, UiKit.Kind.Primary, "quiz", 16f);
            UiKit.Place((RectTransform)p1.transform, w - 24f - 460f, 30f, 220f, 52f);
            var p2 = UiKit.Button(panel.transform, "Mixed", "Ôn tổng hợp (20)", "Mixed review (20)", "総合復習（20）", StartMixed, UiKit.Kind.Secondary, "auto_awesome", 16f);
            UiKit.Place((RectTransform)p2.transform, w - 24f - 230f, 30f, 230f, 52f);

            var host = UiKit.Rect(panel.transform, "Scroll");
            UiKit.Place(host, 14f, 126f, w - 28f, H - 138f);
            var content = CourseUi.ScrollIn(host);
            var body = CourseUi.Body(content);
            float bw = w - 28f - 24f, y = 6f;

            y = Head(body, "Công thức", "Formula", "公式", y, bw);
            string[,] formulas = { { "+", l.Affirmative }, { "−", l.Negative }, { "?", l.Question } };
            for (int i = 0; i < 3; i++)
            {
                var c = UiKit.Card(body, "Formula" + i, UiTheme.Raised, 12, 0f, false);
                var sign = UiKit.Label(c.transform, "Sign", formulas[i, 0], 24f, i == 0 ? UiTheme.Good : i == 1 ? UiTheme.Bad : UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(sign.rectTransform, 6f, 0f, 44f, 48f);
                var f = UiKit.Label(c.transform, "Text", formulas[i, 1], 18f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(f.rectTransform, 58f, 0f, bw - 16f - 70f, 48f);
                UiKit.Place(c.rectTransform, 8f, y, bw - 16f, 48f);
                y += 56f;
            }
            y += 10f;

            var usage = l.Usage();
            if (usage != null && usage.Count > 0)
            {
                y = Head(body, "Dùng khi nào", "When to use it", "使う場面", y, bw);
                foreach (var u in usage)
                {
                    CourseUi.Para(body, "Use", "• " + u, 17f, UiTheme.TextSoft, 12f, y, bw - 28f, out float h);
                    y += h + 4f;
                }
                y += 12f;
            }
            if (l.SignalWords.Count > 0)
            {
                y = Head(body, "Từ nhận biết", "Signal words", "合図になる語", y, bw);
                float sx = 8f, sy = y;
                foreach (var sw in l.SignalWords)
                {
                    float cw = 28f + sw.Length * 9.5f;
                    if (sx + cw > bw) { sx = 8f; sy += 38f; }
                    var sc = UiKit.Chip(body, "Signal", sw, UiKit.Alpha(UiTheme.Teal, 0.28f), UiTheme.Text, null, 14f);
                    UiKit.Place(sc, sx, sy, cw, 30f);
                    sx += cw + 8f;
                }
                y = sy + 48f;
            }
            if (l.Examples.Count > 0)
            {
                y = Head(body, "Ví dụ", "Examples", "例文", y, bw);
                for (int i = 0; i < l.Examples.Count; i++)
                {
                    var c = UiKit.Card(body, "Example" + i, UiTheme.Raised, 12, 0f, false);
                    string en = l.ExampleEnglish(i);
                    CourseUi.Para(c.transform, "En", en, 19f, UiTheme.Text, 16f, 10f, bw - 16f - 90f, out float eh, FontStyles.Bold);
                    float ch = 10f + eh;
                    if (Loc.Code != "en")
                    {
                        string tr = l.Examples[i].Resolve();
                        if (!string.IsNullOrEmpty(tr) && tr != en) { CourseUi.Para(c.transform, "Tr", tr, 15f, UiTheme.Gold, 16f, ch + 2f, bw - 16f - 90f, out float th); ch += th + 2f; }
                    }
                    CourseUi.SpeakButton(c.transform, en, "en-US", bw - 16f - 50f, 8f);
                    ch += 12f;
                    UiKit.Place(c.rectTransform, 8f, y, bw - 16f, ch);
                    y += ch + 8f;
                }
                y += 8f;
            }
            string mistake = l.CommonMistake();
            if (!string.IsNullOrEmpty(mistake))
            {
                y = Head(body, "Lỗi hay gặp", "Common mistake", "よくある間違い", y, bw);
                var c = UiKit.Card(body, "Mistake", UiKit.Alpha(UiTheme.Bad, 0.12f), 12, 0f, false);
                CourseUi.Para(c.transform, "Text", mistake, 17f, UiTheme.Text, 16f, 12f, bw - 16f - 32f, out float mh);
                UiKit.Place(c.rectTransform, 8f, y, bw - 16f, mh + 24f);
                y += mh + 40f;
            }
            y = DrawConjugation(body, l, y, bw);
            CourseUi.EndBody(body, y + 10f);
        }

        private float DrawConjugation(RectTransform body, TenseLesson l, float y, float bw)
        {
            var verbs = TensesCatalog.Verbs();
            if (verbs.Count == 0) return y;
            y = Head(body, "Chia động từ", "Conjugation", "活用", y, bw);
            var v = verbs[Mathf.Clamp(_verb, 0, verbs.Count - 1)];
            var prev = UiKit.IconButton(body, "PrevVerb", "chevron_left", () => ChooseVerb(_verb - 1), UiKit.Kind.Secondary, 40f, 22f);
            UiKit.Place((RectTransform)prev.transform, 8f, y, 40f, 40f);
            var next = UiKit.IconButton(body, "NextVerb", "chevron_right", () => ChooseVerb(_verb + 1), UiKit.Kind.Secondary, 40f, 22f);
            UiKit.Place((RectTransform)next.transform, bw - 8f - 40f, y, 40f, 40f);
            string meaning = v.Meaning.IsEmpty ? "" : "  —  " + v.Meaning.Resolve();
            var vl = UiKit.Label(body, "Verb", v.Verb + meaning + (v.Irregular ? "  (" + Loc.T("bất quy tắc", "irregular", "不規則") + ")" : ""), 20f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(vl.rectTransform, 60f, y, bw - 120f, 40f);
            y += 48f;
            var forms = UiKit.Label(body, "Forms", v.Base + " · " + v.ThirdPerson + " · " + v.Past + " · " + v.PastParticiple + " · " + v.Ing, 16f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center);
            UiKit.Place(forms.rectTransform, 0f, y, bw, 26f);
            y += 36f;
            if (!v.Forms.TryGetValue(l.TenseId, out var subjects)) return y;
            float c0 = 90f, cw = (bw - 16f - c0) / 3f;
            string[] heads = { Loc.T("Khẳng định", "Affirmative", "肯定"), Loc.T("Phủ định", "Negative", "否定"), Loc.T("Nghi vấn", "Question", "疑問") };
            for (int i = 0; i < 3; i++)
            {
                var h = UiKit.Label(body, "Head" + i, heads[i], 14f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(h.rectTransform, 8f + c0 + i * cw, y, cw, 24f);
            }
            y += 28f;
            foreach (var kv in subjects)
            {
                var rowBg = UiKit.Card(body, "Row_" + kv.Key, UiTheme.Raised, 10, 0f, false);
                UiKit.Place(rowBg.rectTransform, 8f, y, bw - 16f, 46f);
                var sj = UiKit.Label(rowBg.transform, "Subject", kv.Key, 16f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(sj.rectTransform, 0f, 0f, c0, 46f);
                for (int i = 0; i < 3 && i < kv.Value.Length; i++)
                {
                    var cell = UiKit.Label(rowBg.transform, "Cell" + i, kv.Value[i], 14.5f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.Center);
                    UiKit.Place(cell.rectTransform, c0 + i * cw, 0f, cw, 46f);
                }
                y += 52f;
            }
            return y + 8f;
        }

        private static float Head(RectTransform body, string vi, string en, string ja, float y, float bw)
        {
            var h = UiKit.Label(body, "Head", vi, en, ja, 17f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(h.rectTransform, 8f, y, bw, 26f);
            return y + 32f;
        }

        public void StartPractice()
        {
            var l = TensesCatalog.Lesson(_tenseId);
            if (l == null) return;
            StartQuiz(new List<CourseQuestion>(TensesCatalog.Exercises(l.TenseId)), new CourseQuizOptions
            {
                Title = l.Title.Resolve(), StoreKey = LearningPath.TenseKey(l.TenseId), Target = "en", Accent = CourseUi.LevelColor(l.Cefr),
            });
        }

        public void StartMixed()
        {
            var all = new List<CourseQuestion>();
            foreach (var l in TensesCatalog.Lessons()) all.AddRange(TensesCatalog.Exercises(l.TenseId));
            var pick = CourseUi.Shuffled(all, "tenses-mix").Take(20).ToList();
            StartQuiz(pick, new CourseQuizOptions
            {
                Title = Loc.T("Ôn tổng hợp 12 thì", "Mixed review of all 12 tenses", "12時制の総合復習"), Target = "en", Accent = UiTheme.Ielts,
            });
        }
    }
}