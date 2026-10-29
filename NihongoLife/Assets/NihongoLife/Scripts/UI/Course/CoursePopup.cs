using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Course;
using TMPro;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// The school: learning path, 40 lessons per language, alphabets, grammar, level certificates and the 12 English tenses.
    /// Opened from the Study hub. Every tab is a <see cref="CourseTabView"/> drawn into one shared area.
    /// </summary>
    public sealed class CoursePopup : MenuPopupBase
    {
        public enum Tab { Path, Lessons, Alphabet, Grammar, Certificates, Tenses }

        /// <summary>The not-yet-saved choice on the path tab.</summary>
        public sealed class PathDraft
        {
            public string Target = "ja", Goal = "daily", Level = "A1";
            public bool IsSet => !string.IsNullOrEmpty(Target) && !string.IsNullOrEmpty(Goal) && !string.IsNullOrEmpty(Level);
        }

        private const float W = 1280f, H = 780f, AreaTop = 162f;

        private RectTransform _area, _bar;
        private readonly Dictionary<Tab, CourseTabView> _views = new Dictionary<Tab, CourseTabView>();
        private Tab _tab = Tab.Path;
        private string _target = "ja";

        public PathDraft Draft { get; } = new PathDraft();
        public string Target => _target;
        public Tab CurrentTab => _tab;
        public CourseTabView ActiveView => _views.TryGetValue(_tab, out var v) ? v : null;
        public CoursePathView PathView => (CoursePathView)_views[Tab.Path];
        public CourseLessonsView LessonsView => (CourseLessonsView)_views[Tab.Lessons];
        public CourseAlphabetView AlphabetView => (CourseAlphabetView)_views[Tab.Alphabet];
        public CourseGrammarView GrammarView => (CourseGrammarView)_views[Tab.Grammar];
        public CourseCertificatesView CertificatesView => (CourseCertificatesView)_views[Tab.Certificates];
        public CourseTensesView TensesView => (CourseTensesView)_views[Tab.Tenses];

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Trường học"; en = "School"; ja = "学校"; }

        public static CoursePopup GetOrCreate(GameObject host, TMP_FontAsset font)
        {
            var popup = host.GetComponent<CoursePopup>();
            if (popup == null) popup = host.AddComponent<CoursePopup>();
            popup.Initialize(font);
            return popup;
        }

        protected override void Build(RectTransform card)
        {
            _bar = UiKit.Rect(card, "TabBar");
            UiKit.Place(_bar, 32f, 106f, W - 64f, 44f);
            _area = UiKit.Rect(card, "Area");
            UiKit.Place(_area, 32f, AreaTop, W - 64f, H - AreaTop - 28f);
            float aw = W - 64f, ah = H - AreaTop - 28f;
            _views[Tab.Path] = new CoursePathView(this);
            _views[Tab.Lessons] = new CourseLessonsView(this);
            _views[Tab.Alphabet] = new CourseAlphabetView(this);
            _views[Tab.Grammar] = new CourseGrammarView(this);
            _views[Tab.Certificates] = new CourseCertificatesView(this);
            _views[Tab.Tenses] = new CourseTensesView(this);
            foreach (var v in _views.Values) v.Attach(_area, aw, ah);
            var saved = LearningPath.Current;
            if (saved.IsSet) { Draft.Target = saved.Target; Draft.Goal = saved.Goal; Draft.Level = saved.StartLevel; _target = saved.Target; }
            else { _target = Draft.Target; }
        }

        protected override void OnOpened()
        {
            var saved = LearningPath.Current;
            if (saved.IsSet) { Draft.Target = saved.Target; Draft.Goal = saved.Goal; Draft.Level = saved.StartLevel; _target = saved.Target; }
            ShowTab(_tab);
        }

        protected override void OnLanguageApplied() { if (IsOpen) { BuildBar(); ActiveView?.Redraw(); } }

        protected override void OnOpenUpdate() { ActiveView?.Tick(Time.unscaledDeltaTime); }

        protected override bool HandleEscape() => ActiveView != null && ActiveView.HandleBack();

        // ---------------- navigation ----------------

        public void SetTarget(string target)
        {
            if (!SchoolCatalog.IsTarget(target) || target == _target) { Draft.Target = _target; return; }
            _target = target;
            Draft.Target = target;
            BuildBar();
        }

        /// <summary>Switch the learning language from the selector (redraws the active tab).</summary>
        public void ChooseTarget(string target)
        {
            if (!SchoolCatalog.IsTarget(target)) return;
            SetTarget(target);
            ActiveView?.Show();
        }

        public void ShowTab(Tab tab)
        {
            _tab = tab;
            BuildBar();
            ActiveView?.Show();
        }

        public void OpenUnit(string unitId)
        {
            var u = SchoolCatalog.Unit(unitId);
            if (u == null) return;
            SetTarget(u.Target);
            _tab = Tab.Lessons;
            BuildBar();
            LessonsView.Select(unitId);
        }

        public void OpenAlphabet(string alphabetId)
        {
            var a = SchoolCatalog.Alphabet(alphabetId);
            if (a == null) return;
            SetTarget(a.Target);
            _tab = Tab.Alphabet;
            BuildBar();
            AlphabetView.SelectAlphabet(alphabetId);
        }

        public void OpenCertificate(string certificateId)
        {
            var c = SchoolCatalog.Certificate(certificateId);
            if (c == null) return;
            SetTarget(c.Target);
            _tab = Tab.Certificates;
            BuildBar();
            CertificatesView.Select(certificateId);
        }

        public void OpenStep(CourseRouteStep step)
        {
            if (step == null) return;
            if (step.Type == "alphabet") OpenAlphabet(step.Id);
            else if (step.Type == "certificate") OpenCertificate(step.Id);
            else OpenUnit(step.Id);
        }

        public void RefreshTabBadges() { if (IsOpen) BuildBar(); }

        // ---------------- tab bar ----------------

        private void BuildBar()
        {
            if (_bar == null) return;
            UiKit.Clear(_bar);
            (Tab tab, string vi, string en, string ja, string icon)[] tabs =
            {
                (Tab.Path, "Lộ trình", "My path", "コース", "flag"),
                (Tab.Lessons, "Bài học", "Lessons", "レッスン", "menu_book"),
                (Tab.Alphabet, "Bảng chữ", "Alphabet", "文字", "translate"),
                (Tab.Grammar, "Ngữ pháp", "Grammar", "文法", "edit_note"),
                (Tab.Certificates, "Chứng chỉ", "Certificates", "検定", "workspace_premium"),
                (Tab.Tenses, "12 thì tiếng Anh", "12 English tenses", "英語の12時制", "schedule"),
            };
            float x = 0f;
            foreach (var t in tabs)
            {
                var tab = t.tab;
                float w = tab == Tab.Tenses ? 190f : 144f;
                var b = UiKit.Button(_bar, "Tab_" + tab, t.vi, t.en, t.ja, () => ShowTab(tab), _tab == tab ? UiKit.Kind.Primary : UiKit.Kind.Secondary, t.icon, 15f);
                UiKit.Place((RectTransform)b.transform, x, 0f, w, 44f);
                x += w + 6f;
            }
            // learning language selector
            float bw = 56f, gap = 4f, total = 4 * bw + 3 * gap, sx = W - 64f - total;
            var lab = UiKit.Label(_bar, "LangLabel", "Học:", "Learn:", "学ぶ:", 14f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Place(lab.rectTransform, sx - 74f, 0f, 68f, 44f);
            for (int i = 0; i < SchoolCatalog.Targets.Length; i++)
            {
                string tg = SchoolCatalog.Targets[i];
                var b = UiKit.Button(_bar, "Target_" + tg, tg.ToUpperInvariant(), () => ChooseTarget(tg), _target == tg ? UiKit.Kind.Accent : UiKit.Kind.Ghost, null, 15f, 10);
                UiKit.Place((RectTransform)b.transform, sx + i * (bw + gap), 0f, bw, 44f);
            }
        }
    }
}