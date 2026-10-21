using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Exam;
using NihongoLife.Exam.Ielts;
using NihongoLife.Save;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Exam hall: JLPT and IELTS tests side by side, the live Speaking exam with a teacher and the results mailbox in
    /// the left rail; a banner and the test cards (level badge, facts, best result, actions) on the right.
    /// Exams are data-driven (ExamDefinition assets under Resources/Exams and IELTS packages); see Docs/EXAM_SYSTEM.md.
    /// Every text follows the interface language.
    /// </summary>
    public class ExamCenterPopup : MenuPopupBase
    {
        private const float W = 1380f, H = 870f, RailW = 286f;

        private ExamType _tab = ExamType.Jlpt;
        private Button _jlptTab, _ieltsTab;
        private RectTransform _hero, _filters, _listRoot;
        private TextMeshProUGUI _emptyText;
        private readonly List<Button> _skillButtons = new List<Button>();
        private string _ieltsSkill = "all";
        private RectTransform _stats;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "Trung tâm luyện thi";
            en = "Exam Center";
            ja = "試験センター";
        }

        protected override void Build(RectTransform card)
        {
            var sub = UiKit.Label(card, "Subtitle", "Chọn kỳ thi rồi luyện tập hoặc thi thử — kết quả được lưu và gửi vào hộp thư.",
                "Pick an exam, then practise or take a mock test — results are saved to your mailbox.",
                "試験を選んで練習または模試を受けましょう。結果は保存され、ボックスに届きます。", 15f, UiTheme.Muted);
            UiKit.Place(sub.rectTransform, 36f, 60f, W - 160f, 24f);

            // ── Left rail ──
            float y = 112f;
            _jlptTab = RailTile(card, "JlptTab", "JLPT", "Năng lực tiếng Nhật", "Japanese proficiency", "日本語能力試験", "translate", UiTheme.Jlpt, y, () => SelectTab(ExamType.Jlpt)); y += 96f;
            _ieltsTab = RailTile(card, "IeltsTab", "IELTS", "Tiếng Anh học thuật", "Academic English", "アカデミック英語", "language", UiTheme.Ielts, y, () => SelectTab(ExamType.Ielts)); y += 96f;
            RailTile(card, "LiveTab", Loc.T("Thi trực tuyến", "Live exam", "ライブ試験"), "Speaking với giám khảo", "Speaking with an examiner", "試験官とスピーキング", "co_present", UiTheme.Live, y, OpenLiveExam, fixedTitle: false,
                titleVi: "Thi trực tuyến", titleEn: "Live exam", titleJa: "ライブ試験"); y += 96f;
            RailTile(card, "MailTab", Loc.T("Hộp thư", "Mailbox", "ボックス"), "Kết quả đã chấm", "Marked results", "採点結果", "inbox", UiTheme.Gold, y, OpenMailbox, fixedTitle: false,
                titleVi: "Hộp thư kết quả", titleEn: "Results mailbox", titleJa: "結果ボックス"); y += 104f;

            _stats = UiKit.Card(card, "Stats", UiTheme.Surface, 18, 0f, false).rectTransform;
            UiKit.Place(_stats, 32f, y, RailW, H - y - 32f);

            // ── Right side ──
            float x = 32f + RailW + 24f, w = W - x - 32f;
            _hero = UiKit.Card(card, "Hero", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(_hero, x, 112f, w, 148f);

            _filters = UiKit.Rect(card, "Filters");
            UiKit.Place(_filters, x, 276f, w, 40f);
            UiKit.RowLayout(_filters, 10f);
            string[] skills = { "all", "listening", "reading", "writing", "speaking" };
            string[][] labels =
            {
                new[] { "TẤT CẢ", "ALL", "すべて" }, new[] { "NGHE", "LISTENING", "リスニング" }, new[] { "ĐỌC", "READING", "リーディング" },
                new[] { "VIẾT", "WRITING", "ライティング" }, new[] { "NÓI", "SPEAKING", "スピーキング" },
            };
            string[] icons = { "checklist", "headphones", "menu_book", "edit_note", "mic" };
            for (int i = 0; i < skills.Length; i++)
            {
                string skill = skills[i];
                var b = UiKit.Button(_filters, "Skill_" + skill, labels[i][0], labels[i][1], labels[i][2], () => { _ieltsSkill = skill; RebuildList(); }, UiKit.Kind.Ghost, icons[i], 13.5f, 18);
                UiKit.Size(b, 168f, 38f);
                _skillButtons.Add(b);
            }

            var scrollContent = UiKit.Scroll(card, "List", out var scroll, 14f, new RectOffset(2, 10, 2, 16));
            _listRoot = scrollContent;
            UiKit.Place((RectTransform)scroll.transform, x, 330f, w, H - 330f - 28f);

            _emptyText = UiKit.Label(card, "Empty", "Chưa có bài luyện thi nào ở mục này.", "No practice test in this category yet.", "このカテゴリーにはまだ試験がありません。",
                17f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Center);
            UiKit.Place(_emptyText.rectTransform, x, 520f, w, 40f);
        }

        private Button RailTile(RectTransform card, string name, string title, string vi, string en, string ja, string icon, Color color, float y, Action click,
            bool fixedTitle = true, string titleVi = null, string titleEn = null, string titleJa = null)
        {
            var image = UiKit.Card(card, name, UiTheme.Surface, 18, 0f, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            UIStyleKit.StyleButton(button, UiTheme.Surface, UiTheme.Raised, UiTheme.Sunken);
            image.sprite = UiKit.Rounded(18);
            var rect = image.rectTransform;
            UiKit.Place(rect, 32f, y, RailW, 84f);
            // The title is the first text of the tile (the tab's name).
            var t = UiKit.Label(rect, "Title", title, 19f, UiTheme.Text, FontStyles.Bold);
            if (!fixedTitle) LocText.Set(t, titleVi, titleEn, titleJa);
            UiKit.Place(t.rectTransform, 82f, 16f, RailW - 96f, 28f);
            var s = UiKit.Label(rect, "Sub", vi, en, ja, 13f, UiTheme.Muted);
            UiKit.Place(s.rectTransform, 82f, 46f, RailW - 96f, 22f);
            var disc = UiKit.Image(rect, "Disc", UiKit.Alpha(color, 0.18f), UiKit.Rounded(14));
            UiKit.Place(disc.rectTransform, 16f, 16f, 52f, 52f);
            var ic = UiKit.Icon(disc.transform, icon, color, 28f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 28f, 28f);
            var marker = UiKit.Image(rect, "Selected", color, UiKit.Rounded(3));
            UiKit.Place(marker.rectTransform, 0f, 18f, 5f, 48f);
            marker.gameObject.SetActive(false);
            button.onClick.AddListener(() => click());
            return button;
        }

        protected override void Update()
        {
            base.Update();
            if (Keyboard.current == null || !Keyboard.current.kKey.wasPressedThisFrame) return;
            if (IsOpen) { Hide(); return; }
            if (!UiModalStack.BlocksHotkeys) Show();
        }

        protected override void OnOpened() => SelectTab(_tab);

        protected override void OnLanguageApplied()
        {
            if (PanelObject != null && PanelObject.activeSelf) SelectTab(_tab);
        }

        private void SelectTab(ExamType tab)
        {
            _tab = tab;
            bool jlpt = tab == ExamType.Jlpt;
            SetTile(_jlptTab, jlpt, UiTheme.Jlpt);
            SetTile(_ieltsTab, !jlpt, UiTheme.Ielts);
            _filters.gameObject.SetActive(!jlpt);
            BuildHero();
            BuildStats();
            RebuildList();
        }

        private static void SetTile(Button tile, bool on, Color color)
        {
            if (tile == null) return;
            var swap = tile.GetComponent<ButtonColorSwap>();
            Color bg = on ? UiKit.Alpha(UiKit.Lighten(UiTheme.Surface, 0.04f), 1f) : UiTheme.Surface;
            if (swap != null) swap.Configure((Image)tile.targetGraphic, bg, UiTheme.Raised, UiTheme.Sunken);
            tile.transform.Find("Selected")?.gameObject.SetActive(on);
            var outline = tile.GetComponent<Outline>() ?? UiKit.Outline((RectTransform)tile.transform, UiKit.Alpha(color, 0.5f), 1.4f);
            outline.enabled = on;
        }

        private void BuildHero()
        {
            UiKit.Clear(_hero);
            bool jlpt = _tab == ExamType.Jlpt;
            Color color = jlpt ? UiTheme.Jlpt : UiTheme.Ielts;
            var wash = UiKit.Image(_hero, "Wash", UiKit.Alpha(color, 0.22f), UiKit.HorizontalFade);
            UiKit.Fill(wash.rectTransform);
            var big = UiKit.Icon(_hero, jlpt ? "translate" : "language", UiKit.Alpha(color, 0.25f), 150f);
            UiKit.Pin(big.rectTransform, new Vector2(1f, 0.5f), -30f, 0f, 150f, 150f);
            var chip = UiKit.Chip(_hero, "Family", jlpt ? "JLPT" : "IELTS", color, Color.white, jlpt ? "translate" : "language", 12.5f);
            UiKit.Place(chip, 28f, 22f, 90f, 26f);
            var title = UiKit.Label(_hero, "Title",
                jlpt ? "Kỳ thi Năng lực Nhật ngữ" : "Hệ thống kiểm tra tiếng Anh quốc tế",
                jlpt ? "Japanese-Language Proficiency Test" : "International English Language Testing System",
                jlpt ? "日本語能力試験" : "IELTS（アイエルツ）", 26f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 28f, 54f, 760f, 36f);
            var body = UiKit.Label(_hero, "Body",
                jlpt ? "Đề N5 đúng cấu trúc thật: Từ vựng · Ngữ pháp–Đọc · Nghe có audio. Chấm ngay, có điểm liệt từng phần và phân tích từng dạng bài."
                     : "Bốn kỹ năng: Nghe và Đọc chấm theo đáp án; Viết và Nói do AI Examiner chấm theo tiêu chí công khai — hoặc thi Nói trực tiếp với giám khảo.",
                jlpt ? "N5 papers in the real layout: Vocabulary · Grammar & Reading · Listening with audio. Instant marking with section minimums and a breakdown by question type."
                     : "Four skills: Listening and Reading marked by key; Writing and Speaking by the AI Examiner on the public criteria — or take Speaking live with an examiner.",
                jlpt ? "本番どおりのN5形式：文字・語彙・文法・読解・音声つき聴解。すぐに採点し、基準点と問題タイプ別の分析を表示。"
                     : "4技能：リスニングとリーディングは正答で採点、ライティングとスピーキングは公開基準でAI試験官が採点。試験官とのライブ試験も可能。",
                14.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(body.rectTransform, 28f, 92f, 780f, 48f);
        }

        private void BuildStats()
        {
            UiKit.Clear(_stats);
            var head = UiKit.Label(_stats, "Head", "Thành tích", "Your record", "これまでの成績", 14f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 20f, 18f, RailW - 40f, 22f);
            var progress = GameServices.TryGet(out IProgressRepository repo) ? repo.GetProgress() : null;
            var attempts = progress?.examAttempts ?? new List<ExamAttemptRecord>();
            var bestJlpt = attempts.Where(a => a.examId != null && a.examId.Contains("jlpt")).OrderByDescending(a => a.totalMaxScore > 0 ? (float)a.totalScore / a.totalMaxScore : 0f).FirstOrDefault();
            var history = IeltsAttemptStore.LoadHistory().entries;
            float bestBand = history.Count > 0 ? history.Max(h => h.band) : 0f;
            StatRow(48f, "translate", UiTheme.Jlpt, "JLPT", bestJlpt != null ? $"{bestJlpt.totalScore}/{bestJlpt.totalMaxScore}" : "—");
            StatRow(108f, "language", UiTheme.Ielts, "IELTS", bestBand > 0f ? $"Band {bestBand:0.0}" : "—");
            int total = attempts.Count + history.Count;
            var count = UiKit.Label(_stats, "Count", (Func<string>)(() => Loc.F("{0} lượt làm bài", "{0} attempts", "受験 {0} 回", total)), 13.5f, UiTheme.Muted);
            UiKit.Place(count.rectTransform, 20f, 170f, RailW - 40f, 22f);
        }

        private void StatRow(float y, string icon, Color color, string label, string value)
        {
            var disc = UiKit.Image(_stats, "Disc_" + label, UiKit.Alpha(color, 0.16f), UiKit.Circle);
            UiKit.Place(disc.rectTransform, 20f, y, 44f, 44f);
            var ic = UiKit.Icon(disc.transform, icon, color, 22f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 22f, 22f);
            var l = UiKit.Label(_stats, "Label_" + label, label, 13f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(l.rectTransform, 76f, y, 180f, 18f);
            var v = UiKit.Label(_stats, "Value_" + label, value, 20f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(v.rectTransform, 76f, y + 18f, 190f, 28f);
        }

        private void RebuildList()
        {
            UiKit.Clear(_listRoot);
            var exams = new List<ExamDefinition>();
            if (GameServices.TryGet(out ExamRepository repository))
                foreach (var exam in repository.GetAllExams())
                    if (exam != null && exam.examType == _tab) exams.Add(exam);
            exams.Sort((a, b) => string.CompareOrdinal(a.level, b.level));

            var packages = _tab == ExamType.Ielts ? IeltsLibrary.Discover() : new List<IeltsLibrary.Package>();
            if (_ieltsSkill != "all") packages = packages.Where(p => p.Test.skill == _ieltsSkill).ToList();
            packages = packages.OrderBy(p => p.Test.skill == "writing" ? 0 : p.Test.skill == "speaking" ? 1 : p.Test.skill == "listening" ? 2 : 3).ThenBy(p => p.Id).ToList();
            if (_tab == ExamType.Ielts && _ieltsSkill != "all") exams.Clear(); // built-in short sets mix skills

            string[] skillIds = { "all", "listening", "reading", "writing", "speaking" };
            for (int i = 0; i < _skillButtons.Count; i++) UiKit.SetKind(_skillButtons[i], skillIds[i] == _ieltsSkill ? UiKit.Kind.Accent : UiKit.Kind.Ghost);

            _emptyText.gameObject.SetActive(exams.Count == 0 && packages.Count == 0);
            foreach (var package in packages) BuildPackageCard(package);
            PlayerProgressDto progress = GameServices.TryGet(out IProgressRepository progressRepository) ? progressRepository.GetProgress() : null;
            foreach (var exam in exams) BuildCard(exam, progress);
        }

        // ─────────── Cards ───────────

        private RectTransform NewCard(Color accent, string badge, string icon, float height)
        {
            var card = UiKit.Card(_listRoot, "ExamCard", UiTheme.Surface, 18, 0f, false);
            UiKit.Size(card, -1f, height);
            var rect = card.rectTransform;
            var stripe = UiKit.Image(rect, "Accent", accent, UiKit.Rounded(3));
            UiKit.Place(stripe.rectTransform, 0f, 20f, 4f, height - 40f);
            var tile = UiKit.Image(rect, "Badge", UiKit.Alpha(accent, 0.16f), UiKit.Rounded(16));
            UiKit.Place(tile.rectTransform, 22f, 22f, 76f, 76f);
            if (!string.IsNullOrEmpty(icon))
            {
                var ic = UiKit.Icon(tile.transform, icon, accent, 38f);
                UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 38f, 38f);
            }
            else
            {
                var b = UiKit.Label(tile.transform, "Level", badge, 28f, accent, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Fill(b.rectTransform);
            }
            return rect;
        }

        private TextMeshProUGUI CardTitle(RectTransform card, string text)
        {
            // Direct child of the card: the card's name for players and tests.
            var t = UiKit.Label(card, "Title", text, 19f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(t.rectTransform, 116f, 18f, 640f, 28f);
            t.transform.SetSiblingIndex(0);
            return t;
        }

        private RectTransform Facts(RectTransform card, float y)
        {
            var row = UiKit.Rect(card, "Facts");
            UiKit.Place(row, 116f, y, 680f, 26f);
            UiKit.RowLayout(row, 8f);
            return row;
        }

        private void BuildCard(ExamDefinition exam, PlayerProgressDto progress)
        {
            bool jlpt = exam.examType == ExamType.Jlpt;
            Color accent = jlpt ? UiTheme.Jlpt : UiTheme.Ielts;
            var card = NewCard(accent, jlpt ? exam.level : null, jlpt ? null : "school", 170f);
            // Start first: it is the card's main action (first button).
            var start = UiKit.Button(card, "Start", "Bắt đầu", "Start", "始める", () => StartExam(exam), UiKit.Kind.Primary, "play_arrow", 16f);
            UiKit.Pin((RectTransform)start.transform, new Vector2(1f, 1f), -22f, -22f, 190f, 50f);
            CardTitle(card, $"{exam.level}  ·  {Loc.Pick(exam.titleVi, exam.titleEn, string.IsNullOrEmpty(exam.titleJa) ? exam.titleEn : exam.titleJa)}");
            var desc = UiKit.Label(card, "Description", Loc.Pick(exam.descriptionVi, exam.descriptionEn, string.IsNullOrEmpty(exam.descriptionJa) ? exam.descriptionEn : exam.descriptionJa),
                14f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(desc.rectTransform, 116f, 50f, 660f, 44f);

            int questions = exam.sections?.Sum(s => s?.questions?.Count ?? 0) ?? 0;
            int minutes = Mathf.RoundToInt((exam.sections?.Sum(s => s != null ? s.timeLimitSeconds : 0) ?? 0) / 60f);
            var facts = Facts(card, 104f);
            UiKit.Chip(facts, "Q", Loc.F("{0} câu", "{0} questions", "{0}問", questions), UiKit.Alpha(Color.white, 0.06f), UiTheme.TextSoft, "quiz", 12f);
            if (minutes > 0) UiKit.Chip(facts, "Time", Loc.F("{0} phút", "{0} min", "{0}分", minutes), UiKit.Alpha(Color.white, 0.06f), UiTheme.TextSoft, "schedule", 12f);
            UiKit.Chip(facts, "Sections", Loc.F("{0} phần", "{0} sections", "{0}セクション", exam.sections?.Count ?? 0), UiKit.Alpha(Color.white, 0.06f), UiTheme.TextSoft, "checklist", 12f);
            UiKit.Chip(facts, "Own", "Đề tự soạn", "Original set", "オリジナル問題", UiKit.Alpha(Color.white, 0.06f), UiTheme.TextSoft, "edit_note", 12f);
            if (!jlpt) UiKit.Chip(facts, "Ai", "Viết/Nói chấm bằng AI", "Writing/Speaking by AI", "記述・会話はAI採点", UiKit.Alpha(UiTheme.Ielts, 0.16f), UiTheme.Ielts, "auto_awesome", 12f);

            BestResult(card, exam, progress);
        }

        private void BestResult(RectTransform card, ExamDefinition exam, PlayerProgressDto progress)
        {
            ExamAttemptRecord best = null;
            if (progress?.examAttempts != null)
                foreach (var attempt in progress.examAttempts)
                {
                    if (attempt.examId != exam.id) continue;
                    if (best == null || (exam.examType == ExamType.Jlpt ? attempt.totalScore > best.totalScore : attempt.estimatedBand > best.estimatedBand)) best = attempt;
                }
            var label = UiKit.Label(card, "Best", (Func<string>)(() =>
            {
                if (best == null) return Loc.T("Chưa làm lần nào", "Not attempted yet", "まだ受けていません");
                return exam.examType == ExamType.Jlpt
                    ? $"{Loc.T("Cao nhất", "Best", "最高")}: <b>{best.totalScore}/{best.totalMaxScore}</b>  ·  {(best.passed ? $"<color={UiTheme.Hex(UiTheme.Good)}>{Loc.T("Đạt", "Pass", "合格")}</color>" : $"<color={UiTheme.Hex(UiTheme.Bad)}>{Loc.T("Chưa đạt", "Not yet", "不合格")}</color>")}"
                    : $"{Loc.T("Band cao nhất", "Best band", "最高バンド")}: <b>{best.estimatedBand:0.0}</b>";
            }), 13.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            UiKit.Pin(label.rectTransform, new Vector2(1f, 1f), -22f, -84f, 260f, 24f);
        }

        /// <summary>IELTS packages (built-in practice sets and local books): practice / mock exam / resume, and live Speaking.</summary>
        private void BuildPackageCard(IeltsLibrary.Package package)
        {
            var test = package.Test;
            string icon = test.skill switch { "listening" => "headphones", "reading" => "menu_book", "writing" => "edit_note", "speaking" => "mic", _ => "school" };
            bool speaking = test.skill == "speaking";
            var saved = IeltsAttemptStore.LoadAttempt(test.id);
            bool resumable = saved != null && !saved.submitted;
            float height = speaking ? 196f : resumable ? 196f : 170f;
            var card = NewCard(UiTheme.Ielts, null, icon, height);

            // Buttons: a right-hand column (first = main action).
            float by = 20f;
            if (resumable)
            {
                var resume = UiKit.Button(card, "Resume", "Tiếp tục", "Resume", "続きから", () => OpenLocal(package, saved.mode, true), UiKit.Kind.Primary, "play_arrow", 15f);
                UiKit.Pin((RectTransform)resume.transform, new Vector2(1f, 1f), -22f, -by, 190f, 42f); by += 50f;
            }
            var practice = UiKit.Button(card, "Practice", "Luyện tập", "Practice", "練習", () => OpenLocal(package, IeltsTestUI.PracticeMode, false), resumable ? UiKit.Kind.Secondary : UiKit.Kind.Primary, "school", 15f);
            UiKit.Pin((RectTransform)practice.transform, new Vector2(1f, 1f), -22f, -by, 190f, 42f); by += 50f;
            var exam = UiKit.Button(card, "Exam", "Thi thử", "Mock exam", "模試", () => OpenLocal(package, IeltsTestUI.ExamMode, false), UiKit.Kind.Secondary, "timer", 15f);
            UiKit.Pin((RectTransform)exam.transform, new Vector2(1f, 1f), -22f, -by, 190f, 42f); by += 50f;
            if (speaking && !resumable)
            {
                var live = UiKit.Button(card, "LiveExam", "Thi với giám khảo", "Live with examiner", "試験官とライブ", () => OpenLive(test), UiKit.Kind.Danger, "co_present", 14.5f);
                UiKit.Pin((RectTransform)live.transform, new Vector2(1f, 1f), -22f, -by, 190f, 42f);
            }

            CardTitle(card, test.DisplayTitle);
            int questions = test.parts?.SelectMany(p => p.groups ?? Array.Empty<IeltsGroup>()).Select(g => g.to).DefaultIfEmpty(0).Max() ?? 0;
            int parts = test.parts?.Length ?? 0;
            string skillLine = test.skill switch
            {
                "listening" => Loc.F("Nghe · {0} câu · {1} phần", "Listening · {0} questions · {1} parts", "リスニング · {0}問 · {1}パート", questions, parts),
                "reading" => Loc.F("Đọc học thuật · {0} câu · {1} bài đọc · {2} phút", "Academic Reading · {0} questions · {1} passages · {2} min", "アカデミック・リーディング · {0}問 · {1}パッセージ · {2}分", questions, parts, test.timeLimitMinutes),
                "writing" => Loc.F("Viết · Task 1 + Task 2 · {0} phút", "Writing · Task 1 + Task 2 · {0} min", "ライティング · Task 1 + Task 2 · {0}分", test.timeLimitMinutes),
                "speaking" => Loc.F("Nói · Part 1–3 · khoảng {0} phút", "Speaking · Parts 1–3 · about {0} min", "スピーキング · Part 1–3 · 約{0}分", test.timeLimitMinutes),
                _ => test.skill,
            };
            var line = UiKit.Label(card, "Skill", skillLine, 14f, UiTheme.TextSoft);
            UiKit.Place(line.rectTransform, 116f, 50f, 640f, 22f);

            string how = test.skill == "listening" ? Loc.T("Có audio · thi thử: nghe một lần, có 2 phút kiểm tra", "Audio · mock: one hearing, then 2 minutes to check", "音声あり · 模試：音声は1回のみ、確認2分")
                       : test.skill == "reading" ? Loc.T("Bài đọc bên cạnh câu hỏi · thi thử: đếm ngược, hết giờ tự nộp", "Passage beside the questions · mock: countdown, auto-submit", "本文と設問を並べて表示 · 模試：時間切れで自動提出")
                       : test.skill == "writing" ? Loc.T("AI Examiner chấm 4 tiêu chí · AI Coach góp ý từng câu", "AI Examiner marks 4 criteria · AI Coach comments per sentence", "AI試験官が4項目を採点 · AIコーチが一文ずつ添削")
                       : Loc.T("Ghi âm từng câu, AI chấm — hoặc thi trực tiếp với giám khảo", "Record each answer for AI marking — or take it live with an examiner", "一問ずつ録音してAI採点、または試験官とライブで受験");
            var howLabel = UiKit.Label(card, "How", how, 13f, UiTheme.Muted, FontStyles.Italic);
            UiKit.Place(howLabel.rectTransform, 116f, 76f, 640f, 20f);

            var facts = Facts(card, 108f);
            if (test.localOnly) UiKit.Chip(facts, "Local", "Bản local · không phát hành", "Local copy · not published", "ローカル版 · 非公開", UiKit.Alpha(UiTheme.Warn, 0.16f), UiTheme.Warn, "lock", 12f);
            else UiKit.Chip(facts, "Own", "Đề tự soạn", "Original set", "オリジナル問題", UiKit.Alpha(Color.white, 0.06f), UiTheme.TextSoft, "edit_note", 12f);
            if (speaking) UiKit.Chip(facts, "Live", "Có thể thi trực tuyến", "Live exam available", "ライブ試験対応", UiKit.Alpha(UiTheme.Live, 0.16f), UiTheme.Live, "co_present", 12f);

            var history = IeltsAttemptStore.LoadHistory().entries.FindAll(e => e.testId == test.id);
            var best = UiKit.Label(card, "Best", (Func<string>)(() =>
            {
                string b = history.Count == 0 ? Loc.T("Chưa làm lần nào", "Not attempted yet", "まだ受けていません")
                    : history[0].maxScore > 0
                        ? Loc.F("Tốt nhất: {0}/{1} · band ước tính {2:0.0} ({3} lần)", "Best: {0}/{1} · est. band {2:0.0} ({3} tries)", "最高：{0}/{1} · 推定バンド {2:0.0}（{3}回）", history.Max(h => h.rawScore), history[0].maxScore, history.Max(h => h.band), history.Count)
                        : Loc.F("Band tốt nhất {0:0.0} ({1} lần)", "Best band {0:0.0} ({1} tries)", "最高バンド {0:0.0}（{1}回）", history.Max(h => h.band), history.Count);
                if (resumable) b += "   ·   " + Loc.F("Đang làm dở ({0} câu)", "In progress ({0} answers)", "途中（{0}問）", saved.responses.Count);
                return b;
            }), 13.5f, UiTheme.Teal, FontStyles.Normal);
            UiKit.Place(best.rectTransform, 116f, 142f, 640f, 22f);
        }

        private void OpenLocal(IeltsLibrary.Package package, string mode, bool resume)
        {
            Hide();
            IeltsTestUI.Open(package, mode, resume);
        }

        private void OpenLive(IeltsTest test)
        {
            Hide();
            var meeting = NihongoLife.Learning.EduMeetingManager.Instance ?? FindFirstObjectByType<NihongoLife.Learning.EduMeetingManager>();
            if (meeting == null)
            {
                HudFeed.Post(Loc.T("Phòng thi trực tuyến chỉ có ở trường Hibari.", "The live exam room is at Hibari school.", "ライブ試験室はひばり学院にあります。"), HudFeed.Kind.Warning, 3.5f);
                return;
            }
            meeting.OpenForSpeakingExam(test.id, test.DisplayTitle);
        }

        private void OpenLiveExam()
        {
            var first = IeltsLibrary.Discover().FirstOrDefault(p => p.Test.skill == "speaking");
            if (first != null) OpenLive(first.Test);
            else
            {
                Hide();
                (NihongoLife.Learning.EduMeetingManager.Instance ?? FindFirstObjectByType<NihongoLife.Learning.EduMeetingManager>())?.SetOpen(true);
            }
        }

        private void OpenMailbox()
        {
            Hide();
            FindFirstObjectByType<ExamMailboxUI>()?.SetOpen(true);
        }

        private void StartExam(ExamDefinition exam)
        {
            if (!GameServices.TryGet(out ExamManager manager)) return;
            // Resume an attempt still in progress instead of wiping it — the learner may have closed this screen mid-exam.
            bool alreadyInProgress = manager.CurrentExam == exam && manager.IsAttemptActive;
            if (!alreadyInProgress) manager.StartAttempt(exam);
            Hide();
            ExamPlayUI.GetOrCreate(transform).Show();
        }
    }
}
