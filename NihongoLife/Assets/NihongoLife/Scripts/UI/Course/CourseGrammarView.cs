using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>The 40 grammar points of the chosen language, each linked to the unit that teaches it.</summary>
    public sealed class CourseGrammarView : CourseTabView
    {
        private const float LW = 330f;
        private string _id;

        public CourseGrammarView(CoursePopup host) : base(host) { }

        public string SelectedId => _id;

        public void Select(string id) { _id = id; Show(); }

        protected override void Draw()
        {
            var points = SchoolCatalog.Grammar(Host.Target);
            var g = points.Find(p => p.Id == _id) ?? points.FirstOrDefault();
            _id = g?.Id;
            var listBg = UiKit.Card(Area, "ListBg", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(listBg.rectTransform, 0f, 0f, LW, H);
            var list = UiKit.Scroll(listBg.transform, "List", out _, 8f, new RectOffset(8, 8, 8, 8));
            UiKit.Fill((RectTransform)list.parent.parent);
            foreach (var p in points)
            {
                var pp = p;
                var row = CourseUi.Row(list, "G" + p.UnitNo, p == g, CourseUi.LevelColor(p.Level), 62f, () => Select(pp.Id));
                var no = UiKit.Label(row.transform, "No", p.UnitNo.ToString(), 15f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Place(no.rectTransform, 8f, 0f, 36f, 62f);
                var t = UiKit.Label(row.transform, "Pattern", p.Pattern, 16f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(t.rectTransform, 48f, 0f, LW - 16f - 48f - 56f, 62f);
                var lv = UiKit.Chip(row.transform, "Level", p.Level, UiKit.Alpha(CourseUi.LevelColor(p.Level), 0.35f), UiTheme.Text, null, 12f);
                UiKit.Place(lv, LW - 16f - 48f, 20f, 44f, 22f);
            }
            if (g == null) return;
            float x = LW + 20f, w = W - LW - 20f;
            var panel = UiKit.Card(Area, "Detail", UiTheme.Surface, 20, 0f, false);
            UiKit.Place(panel.rectTransform, x, 0f, w, H);
            var host = UiKit.Rect(panel.transform, "Scroll");
            UiKit.Place(host, 14f, 12f, w - 28f, H - 24f);
            var content = CourseUi.ScrollIn(host);
            var body = CourseUi.Body(content);
            float bw = w - 28f - 24f, y = 10f;
            var chip = UiKit.Chip(body, "Level", g.Level, CourseUi.LevelColor(g.Level), Color.white, "school", 13f);
            UiKit.Place(chip, 8f, y, 84f, 26f); y += 36f;
            CourseUi.Para(body, "Pattern", g.Pattern, 34f, UiTheme.Gold, 8f, y, bw - 16f, out float ph, FontStyles.Bold); y += ph + 6f;
            if (!g.Title.IsEmpty) { CourseUi.Para(body, "Title", g.Title.Resolve(), 21f, UiTheme.Text, 8f, y, bw - 16f, out float th, FontStyles.Bold); y += th + 12f; }
            if (!g.Explanation.IsEmpty) { CourseUi.Para(body, "Expl", g.Explanation.Resolve(), 18f, UiTheme.TextSoft, 8f, y, bw - 16f, out float eh); y += eh + 18f; }
            if (!string.IsNullOrEmpty(g.ExampleText))
            {
                var ex = UiKit.Card(body, "Example", UiTheme.Raised, 14, 0f, false);
                CourseUi.Para(ex.transform, "Text", g.ExampleText, 24f, UiTheme.Text, 18f, 14f, bw - 100f, out float xh, FontStyles.Bold);
                float ch = 14f + xh + 2f;
                string tr = g.ExampleTranslation.IsEmpty ? null : g.ExampleTranslation.Gloss(Host.Target);
                if (!string.IsNullOrEmpty(tr)) { CourseUi.Para(ex.transform, "Tr", tr, 17f, UiTheme.Gold, 18f, ch, bw - 36f, out float xt); ch += xt + 2f; }
                CourseUi.SpeakButton(ex.transform, g.ExampleText, CourseUi.LocaleOf(Host.Target), bw - 56f, 10f);
                ch += 14f;
                UiKit.Place(ex.rectTransform, 8f, y, bw - 16f, ch);
                y += ch + 20f;
            }
            var unit = SchoolCatalog.Units(Host.Target).FirstOrDefault(u => u.GrammarId == g.Id);
            if (unit != null)
            {
                string uid = unit.Id;
                var open = UiKit.Button(body, "OpenUnit", Loc.F("Học trong bài {0}", "Study it in unit {0}", "ユニット{0}で学ぶ", unit.No), () => Host.OpenUnit(uid), UiKit.Kind.Primary, "menu_book", 17f);
                UiKit.Place((RectTransform)open.transform, 8f, y, 320f, 52f);
                y += 62f;
            }
            CourseUi.EndBody(body, y + 10f);
        }
    }
}