using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>Hiragana/katakana, the Cyrillic and Latin alphabets, Vietnamese letters and tones: lessons of glyph cards plus a quiz.</summary>
    public sealed class CourseAlphabetView : CourseTabView
    {
        private const float LW = 300f;
        private string _alphabetId, _lessonId;

        public CourseAlphabetView(CoursePopup host) : base(host) { }

        public string SelectedAlphabetId => _alphabetId;
        public string SelectedLessonId => _lessonId;

        public void SelectAlphabet(string id)
        {
            if (SchoolCatalog.Alphabet(id) == null) return;
            _alphabetId = id; _lessonId = null;
            Show();
        }

        public void SelectLesson(string lessonId) { _lessonId = lessonId; Show(); }

        private CourseAlphabet Current()
        {
            var ids = SchoolCatalog.AlphabetIds(Host.Target);
            if (_alphabetId == null || !ids.Contains(_alphabetId)) { _alphabetId = ids.FirstOrDefault(); _lessonId = null; }
            return SchoolCatalog.Alphabet(_alphabetId);
        }

        protected override void Draw()
        {
            var a = Current();
            if (a == null) return;
            var ids = SchoolCatalog.AlphabetIds(Host.Target);
            float top = 0f;
            if (ids.Length > 1)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    string id = ids[i];
                    var alpha = SchoolCatalog.Alphabet(id);
                    var b = UiKit.Button(Area, "Alpha_" + id, alpha.Title.Resolve(), () => SelectAlphabet(id), id == _alphabetId ? UiKit.Kind.Primary : UiKit.Kind.Secondary, null, 15f);
                    UiKit.Place((RectTransform)b.transform, i * 222f, 0f, 210f, 40f);
                }
                top = 52f;
            }
            if (_lessonId == null || a.Lessons.Find(l => l.Id == _lessonId) == null) _lessonId = a.Lessons.FirstOrDefault()?.Id;
            var lesson = a.Lessons.Find(l => l.Id == _lessonId);

            var listBg = UiKit.Card(Area, "ListBg", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(listBg.rectTransform, 0f, top, LW, H - top);
            var list = UiKit.Scroll(listBg.transform, "List", out _, 8f, new RectOffset(8, 8, 8, 8));
            UiKit.Fill((RectTransform)list.parent.parent);
            var color = CourseUi.TargetColor(Host.Target);
            foreach (var l in a.Lessons)
            {
                var ll = l;
                var row = CourseUi.Row(list, "Lesson_" + l.Id, l == lesson, color, 58f, () => SelectLesson(ll.Id));
                var t = UiKit.Label(row.transform, "Title", l.Title.Resolve(), 16f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(t.rectTransform, 16f, 0f, LW - 16f - 60f, 58f);
                var n = UiKit.Label(row.transform, "Count", l.ItemIds.Count.ToString(), 14f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
                UiKit.Place(n.rectTransform, LW - 16f - 50f, 0f, 40f, 58f);
            }

            float x = LW + 20f, w = W - LW - 20f;
            var panel = UiKit.Card(Area, "Detail", UiTheme.Surface, 20, 0f, false);
            UiKit.Place(panel.rectTransform, x, top, w, H - top);
            if (lesson == null) return;
            var title = UiKit.Label(panel.transform, "Title", lesson.Title.Resolve(), 26f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 24f, 14f, w - 48f - 440f, 40f);
            var store = LearningProgressStore.Shared;
            string key = LearningPath.AlphabetKey(a.Id);
            var lessonQs = a.Quiz.Where(q => q.GroupId == lesson.Id).ToList();
            var q1 = UiKit.Button(panel.transform, "QuizLesson", Loc.F("Đố bài này ({0})", "Quiz this lesson ({0})", "このレッスンのクイズ（{0}）", lessonQs.Count), StartLessonQuiz, UiKit.Kind.Secondary, "quiz", 15f);
            UiKit.Place((RectTransform)q1.transform, w - 24f - 430f, 12f, 210f, 44f);
            q1.interactable = lessonQs.Count > 0;
            var q2 = UiKit.Button(panel.transform, "QuizAll", Loc.F("Cả bảng chữ ({0})", "Whole alphabet ({0})", "全体クイズ（{0}）", a.Quiz.Count), StartFullQuiz, UiKit.Kind.Primary, "quiz", 15f);
            UiKit.Place((RectTransform)q2.transform, w - 24f - 210f, 12f, 210f, 44f);
            var best = store.Get(key);
            var stars = UiKit.Label(panel.transform, "Stars", CourseUi.Stars(best?.stars ?? 0) + (best == null ? "" : "  " + best.bestCorrect + "/" + best.total), 15f, UiTheme.Gold);
            UiKit.Place(stars.rectTransform, 24f, 56f, 300f, 24f);

            var host = UiKit.Rect(panel.transform, "Scroll");
            UiKit.Place(host, 14f, 88f, w - 28f, H - top - 88f - 12f);
            var content = CourseUi.ScrollIn(host);
            var body = CourseUi.Body(content);
            float bw = w - 28f - 24f, y = 4f;
            if (!lesson.Tip.IsEmpty)
            {
                var tip = UiKit.Card(body, "Tip", UiKit.Alpha(UiTheme.Gold, 0.13f), 14, 0f, false);
                CourseUi.Para(tip.transform, "Text", lesson.Tip.Resolve(), 16f, UiTheme.Text, 16f, 10f, bw - 32f, out float th);
                UiKit.Place(tip.rectTransform, 0f, y, bw, th + 22f);
                y += th + 34f;
            }
            const float gap = 12f, cardW = 188f, cardH = 170f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((bw + gap) / (cardW + gap)));
            float cw = (bw - gap * (cols - 1)) / cols;
            string locale = CourseUi.LocaleOf(Host.Target);
            int idx = 0;
            foreach (var gid in lesson.ItemIds)
            {
                var g = a.Glyph(gid);
                if (g == null) continue;
                var c = UiKit.Card(body, "Glyph_" + gid, UiTheme.Raised, 14, 0f, false);
                UiKit.Place(c.rectTransform, (idx % cols) * (cw + gap), y + (idx / cols) * (cardH + gap), cw, cardH);
                var big = UiKit.Label(c.transform, "Char", g.Char, 46f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(big.rectTransform, 0f, 6f, cw, 64f);
                string sound = g.Sound + (string.IsNullOrEmpty(g.ToneName) ? "" : " · " + g.ToneName);
                var s = UiKit.Label(c.transform, "Sound", sound, 16f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(s.rectTransform, 6f, 70f, cw - 12f, 24f);
                string nm = string.IsNullOrEmpty(g.Name) ? "" : g.Name;
                var n = UiKit.Label(c.transform, "Name", nm, 13f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center);
                UiKit.Place(n.rectTransform, 6f, 94f, cw - 12f, 20f);
                if (!string.IsNullOrEmpty(g.ExampleWord))
                {
                    string gl = g.ExampleGloss.IsEmpty ? "" : g.ExampleGloss.Gloss(Host.Target);
                    var e = UiKit.Label(c.transform, "Example", g.ExampleWord + (string.IsNullOrEmpty(gl) ? "" : "  ·  " + gl), 13.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center);
                    UiKit.Place(e.rectTransform, 6f, 118f, cw - 12f, 44f);
                    e.textWrappingMode = TextWrappingModes.Normal;
                }
                CourseUi.SpeakButton(c.transform, g.Speech.IsEmpty ? g.Char : g.Speech.Text, g.Speech.Locale ?? locale, cw - 38f, 2f);
                idx++;
            }
            y += ((idx + cols - 1) / cols) * (cardH + gap);
            CourseUi.EndBody(body, y + 10f);
        }

        public void StartLessonQuiz()
        {
            var a = Current();
            var qs = a?.Quiz.Where(q => q.GroupId == _lessonId).ToList();
            if (qs == null || qs.Count == 0) return;
            StartQuiz(qs, new CourseQuizOptions { Title = a.Title.Resolve(), Target = a.Target, Accent = CourseUi.TargetColor(a.Target) });
        }

        public void StartFullQuiz()
        {
            var a = Current();
            if (a == null || a.Quiz.Count == 0) return;
            StartQuiz(new List<CourseQuestion>(a.Quiz), new CourseQuizOptions { Title = a.Title.Resolve(), StoreKey = LearningPath.AlphabetKey(a.Id), Target = a.Target, Accent = CourseUi.TargetColor(a.Target) });
        }
    }
}