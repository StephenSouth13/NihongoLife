using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>Level certificates (JLPT-, CEFR-, TORFL-, TRKI- and IELTS-style practice sets): timed quizzes with an honest "practice, not official" note.</summary>
    public sealed class CourseCertificatesView : CourseTabView
    {
        private const float LW = 330f;
        private string _id;

        public CourseCertificatesView(CoursePopup host) : base(host) { }

        public string SelectedId => _id;
        public CourseCertificate Selected => SchoolCatalog.Certificate(_id);

        public void Select(string id) { _id = id; Show(); }

        protected override void Draw()
        {
            var ids = SchoolCatalog.CertificateIds(Host.Target);
            if (ids.Length == 0) return;
            if (_id == null || !ids.Contains(_id)) _id = ids[0];
            var cert = SchoolCatalog.Certificate(_id);
            var store = LearningProgressStore.Shared;

            var listBg = UiKit.Card(Area, "ListBg", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(listBg.rectTransform, 0f, 0f, LW, H);
            var list = UiKit.Scroll(listBg.transform, "List", out _, 8f, new RectOffset(8, 8, 8, 8));
            UiKit.Fill((RectTransform)list.parent.parent);
            foreach (var id in ids)
            {
                var c = SchoolCatalog.Certificate(id);
                if (c == null) continue;
                string cid = id;
                bool passed = LearningPath.CertificatePassed(id);
                var row = CourseUi.Row(list, "Cert_" + id, id == _id, CourseUi.LevelColor(c.Cefr), 68f, () => Select(cid));
                var t = UiKit.Label(row.transform, "Title", c.Label.Resolve(), 16f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(t.rectTransform, 14f, 6f, LW - 16f - 14f - 50f, 30f);
                var lv = UiKit.Chip(row.transform, "Level", c.Cefr ?? c.Level, UiKit.Alpha(CourseUi.LevelColor(c.Cefr), 0.35f), UiTheme.Text, null, 12f);
                UiKit.Place(lv, 14f, 38f, 24f + (c.Cefr ?? c.Level).Length * 8f, 22f);
                var e = store.Get(LearningPath.CertificateKey(id));
                var st = UiKit.Label(row.transform, "State", passed ? "✓" : "", 22f, UiTheme.Good, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
                UiKit.Place(st.rectTransform, LW - 16f - 50f, 4f, 40f, 30f);
                if (e != null)
                {
                    var best = UiKit.Label(row.transform, "Best", e.bestCorrect + "/" + e.total, 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
                    UiKit.Place(best.rectTransform, LW - 16f - 120f, 38f, 110f, 22f);
                }
            }
            if (cert != null) DrawDetail(cert, LW + 20f, W - LW - 20f);
        }

        private void DrawDetail(CourseCertificate c, float x, float w)
        {
            var panel = UiKit.Card(Area, "Detail", UiTheme.Surface, 20, 0f, false);
            UiKit.Place(panel.rectTransform, x, 0f, w, H);
            var chip = UiKit.Chip(panel.transform, "Level", c.Cefr ?? c.Level, CourseUi.LevelColor(c.Cefr), Color.white, "workspace_premium", 13f);
            UiKit.Place(chip, 24f, 18f, 90f, 26f);
            var title = UiKit.Label(panel.transform, "Title", c.Title.Resolve(), 26f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 24f, 48f, w - 48f - 260f, 40f);
            var start = UiKit.Button(panel.transform, "Start", "Bắt đầu thi thử", "Start the practice exam", "模擬試験を始める", StartExam, UiKit.Kind.Primary, "timer", 16f);
            UiKit.Place((RectTransform)start.transform, w - 24f - 250f, 30f, 250f, 54f);

            var host = UiKit.Rect(panel.transform, "Scroll");
            UiKit.Place(host, 14f, 100f, w - 28f, H - 112f);
            var content = CourseUi.ScrollIn(host);
            var body = CourseUi.Body(content);
            float bw = w - 28f - 24f, y = 6f;

            // key facts
            var store = LearningProgressStore.Shared;
            var e = store.Get(LearningPath.CertificateKey(c.Id));
            var facts = new List<string>
            {
                Loc.F("{0} câu", "{0} questions", "{0}問", c.Questions.Count),
                Loc.F("{0} phút", "{0} minutes", "{0}分", c.TimeLimitMinutes),
                Loc.F("Đạt từ {0}%", "Pass mark {0}%", "合格ライン {0}%", Mathf.RoundToInt((float)c.PassRatio * 100f)),
            };
            float fx = 8f;
            foreach (var f in facts)
            {
                var fc = UiKit.Chip(body, "Fact", f, UiTheme.Raised, UiTheme.Text, null, 14f);
                float fw = 40f + f.Length * 9f;
                UiKit.Place(fc, fx, y, fw, 30f);
                fx += fw + 10f;
            }
            y += 42f;
            if (e != null)
            {
                bool passed = LearningPath.CertificatePassed(c.Id);
                var r = UiKit.Label(body, "Result", (passed ? "✓ " : "") + Loc.F("Điểm cao nhất: {0}/{1}", "Best score: {0}/{1}", "最高得点：{0}/{1}", e.bestCorrect, e.total) + "   " + CourseUi.Stars(e.stars),
                    17f, passed ? UiTheme.Good : UiTheme.Gold, FontStyles.Bold);
                UiKit.Place(r.rectTransform, 8f, y, bw - 16f, 28f);
                y += 36f;
            }

            // honest note
            var note = UiKit.Card(body, "Note", UiKit.Alpha(UiTheme.Warn, 0.12f), 14, 0f, false);
            CourseUi.Para(note.transform, "Text", Loc.T("Đây là đề luyện tập tự soạn theo cấu trúc kỳ thi, không phải đề thi thật và không cho ra điểm chính thức.",
                "This is self-made practice in the shape of the exam — not a real exam paper and it gives no official score.",
                "試験の形式に合わせた自作の練習問題です。本物の試験ではなく、公式スコアは出ません。"), 15f, UiTheme.Text, 16f, 10f, bw - 48f, out float nh);
            float nch = nh + 20f;
            if (!string.IsNullOrEmpty(c.Disclaimer))
            {
                CourseUi.Para(note.transform, "Data", c.Disclaimer, 14f, UiTheme.TextSoft, 16f, nch - 6f, bw - 48f, out float dh);
                nch += dh;
            }
            UiKit.Place(note.rectTransform, 8f, y, bw - 16f, nch);
            y += nch + 18f;

            // sections
            var sh = UiKit.Label(body, "SectionsHead", "Các phần thi", "Sections", "セクション", 17f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(sh.rectTransform, 8f, y, bw, 26f); y += 32f;
            foreach (var s in c.Sections)
            {
                var sc = UiKit.Card(body, "Section_" + s.Id, UiTheme.Raised, 12, 0f, false);
                var sl = UiKit.Label(sc.transform, "Label", s.Label.Resolve(), 17f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(sl.rectTransform, 16f, 0f, bw - 16f - 32f - 90f, 46f);
                var sn = UiKit.Label(sc.transform, "Count", Loc.F("{0} câu", "{0} questions", "{0}問", s.Questions.Count), 15f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
                UiKit.Place(sn.rectTransform, bw - 16f - 16f - 120f, 0f, 120f, 46f);
                UiKit.Place(sc.rectTransform, 8f, y, bw - 16f, 46f);
                y += 54f;
            }
            y += 10f;

            // writing / speaking prompt cards (IELTS-style): shown, never scored
            if (c.Prompts.Count > 0)
            {
                var ph = UiKit.Label(body, "PromptsHead", "Viết & Nói (tự luyện, không chấm điểm)", "Writing & Speaking (self-practice, not scored)", "ライティング＆スピーキング（自習・採点なし）", 17f, UiTheme.Muted, FontStyles.Bold);
                UiKit.Place(ph.rectTransform, 8f, y, bw, 26f); y += 32f;
                foreach (var p in c.Prompts)
                {
                    var pc = UiKit.Card(body, "Prompt_" + p.Id, UiTheme.Raised, 14, 0f, false);
                    string head = p.Title + (p.Minutes > 0 ? "  ·  " + Loc.F("{0} phút", "{0} min", "{0}分", p.Minutes) : "") + (p.MinWords > 0 ? "  ·  " + Loc.F("≥ {0} từ", "≥ {0} words", "{0}語以上", p.MinWords) : "");
                    CourseUi.Para(pc.transform, "Head", head, 16f, UiTheme.Gold, 16f, 12f, bw - 48f, out float hh, FontStyles.Bold);
                    float ch = 12f + hh + 4f;
                    CourseUi.Para(pc.transform, "Text", p.Prompt, 17f, UiTheme.Text, 16f, ch, bw - 48f, out float th);
                    ch += th + 4f;
                    foreach (var line in p.Lines)
                    {
                        CourseUi.Para(pc.transform, "Line", "• " + line, 15f, UiTheme.TextSoft, 20f, ch, bw - 56f, out float lh);
                        ch += lh + 2f;
                    }
                    ch += 12f;
                    UiKit.Place(pc.rectTransform, 8f, y, bw - 16f, ch);
                    y += ch + 10f;
                }
                y += 6f;
            }
            if (c.SelfCheck.Count > 0)
            {
                var sc = UiKit.Card(body, "SelfCheck", UiTheme.Raised, 14, 0f, false);
                CourseUi.Para(sc.transform, "Head", Loc.T("Tự kiểm tra bài viết/nói của bạn", "Check your own writing and speaking", "自分のライティング・スピーキングをチェック"), 16f, UiTheme.Gold, 16f, 12f, bw - 48f, out float hh, FontStyles.Bold);
                float ch = 12f + hh + 4f;
                foreach (var line in c.SelfCheck)
                {
                    CourseUi.Para(sc.transform, "Item", "☐ " + line, 15f, UiTheme.TextSoft, 20f, ch, bw - 56f, out float lh);
                    ch += lh + 2f;
                }
                ch += 12f;
                UiKit.Place(sc.rectTransform, 8f, y, bw - 16f, ch);
                y += ch + 10f;
            }
            CourseUi.EndBody(body, y + 10f);
        }

        public void StartExam()
        {
            var c = Selected;
            if (c == null) return;
            var qs = c.Questions;
            StartQuiz(qs, new CourseQuizOptions
            {
                Title = c.Label.Resolve(), StoreKey = LearningPath.CertificateKey(c.Id), Target = c.Target, Accent = CourseUi.LevelColor(c.Cefr),
                PassRatio = c.PassRatio, TimeLimitSeconds = Mathf.Max(0, c.TimeLimitMinutes * 60),
            });
        }

        protected override void CloseQuiz() { base.CloseQuiz(); Host.RefreshTabBadges(); }
    }
}