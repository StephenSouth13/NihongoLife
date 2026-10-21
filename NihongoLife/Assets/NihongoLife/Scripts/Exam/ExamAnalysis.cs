using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NihongoLife.Exam
{
    /// <summary>
    /// Per-もんだい result analysis for JLPT mock tests. Instead of "improve your vocabulary", it says which question
    /// type lost the points and exactly what more correct answers there would do to the division score, the total and
    /// the pass margin, e.g. "もんだい5 (cách dùng từ): đúng 1/3 — đúng thêm 2 câu → 文字・語彙・文法・読解 52→61/120,
    /// tổng 78→87/180: qua ngưỡng đậu 80". Scores follow ExamManager's linear practice scaling (the real JLPT uses
    /// equated scaled scores), which the result screen states.
    /// </summary>
    public static class ExamAnalysis
    {
        public sealed class MondaiStat
        {
            public string sectionId, groupId, mondaiId, titleJa, titleVi;
            public int correct, total, rawCorrect, rawTotal;
            public float Accuracy => total > 0 ? (float)correct / total : 0f;
            public int Wrong => total - correct;
        }

        public sealed class Report
        {
            public List<MondaiStat> mondai = new();
            public List<string> advice = new();
            public string status;
        }

        public static Report Analyze(ExamDefinition exam, ExamAttemptRecord record)
        {
            var report = new Report();
            if (exam == null || record == null || exam.examType != ExamType.Jlpt) return report;
            var answers = record.answers.ToDictionary(a => a.questionId, a => a);

            foreach (var section in exam.sections)
            {
                if (section == null) continue;
                foreach (var group in section.questions.Where(q => q != null).GroupBy(q => string.IsNullOrEmpty(q.mondai) ? "_" : q.mondai))
                {
                    var info = section.mondai?.FirstOrDefault(m => m.id == group.Key);
                    var stat = new MondaiStat
                    {
                        sectionId = section.id, groupId = section.scoreGroup, mondaiId = group.Key,
                        titleJa = info?.titleJa ?? (group.Key == "_" ? section.titleJa : group.Key), titleVi = info?.titleVi ?? section.titleVi,
                    };
                    foreach (var q in group)
                    {
                        bool ok = answers.TryGetValue(q.id, out var a) && a.correct;
                        stat.total++; stat.rawTotal += q.points;
                        if (ok) { stat.correct++; stat.rawCorrect += q.points; }
                    }
                    report.mondai.Add(stat);
                }
            }

            int pass = exam.jlptTotalPassScore;
            int total = record.totalScore, totalMax = record.totalMaxScore;
            report.status = Status(total, pass, record.groups);
            if (record.groups.Count == 0) return report;

            // Divisions below their minimum first: they fail the test regardless of the total.
            foreach (var g in record.groups.Where(g => g.scored < g.passMin))
                report.advice.Add("⚠ " + NihongoLife.Core.Loc.F("{0}: {1}/{2} — dưới điểm liệt {3}. Dù tổng điểm đủ vẫn chưa đậu.", "{0}: {1}/{2} — below the section minimum of {3}. You fail even if the total is enough.", "{0}：{1}/{2} — 基準点{3}未満です。合計点が足りていても不合格です。", GroupTitle(exam, g.groupId), g.scored, g.max, g.passMin));

            // For each division, the weakest もんだい with points still to win, and what fixing some of it is worth.
            var suggestions = new List<(float gain, string text)>();
            foreach (var g in record.groups)
            {
                float perPoint = g.rawMax > 0 ? (float)g.max / g.rawMax : 0f;
                var weakest = report.mondai.Where(m => m.groupId == g.groupId && m.Wrong > 0)
                                           .OrderBy(m => m.Accuracy).ThenByDescending(m => m.Wrong).FirstOrDefault();
                if (weakest == null || perPoint <= 0f) continue;

                int need;
                if (g.scored < g.passMin) need = RawNeeded(g.rawCorrect, g.rawMax, g.max, g.passMin);
                else if (total < pass) need = Mathf.CeilToInt((pass - total) / perPoint);
                else need = 2;
                int k = Mathf.Clamp(need, 1, weakest.Wrong);

                int newGroup = Scale(g.rawCorrect + k, g.rawMax, g.max);
                int newTotal = total - g.scored + newGroup;
                string text = NihongoLife.Core.Loc.F("{0}: đúng {1}/{2}. Làm đúng thêm {3} câu → {4} từ {5} lên {6}/{7}; tổng từ {8} lên {9}/{10} — {11}.",
                                     "{0}: {1}/{2} correct. {3} more correct → {4} from {5} to {6}/{7}; total from {8} to {9}/{10} — {11}.",
                                     "{0}：{1}/{2}問正解。あと{3}問正解すると → {4}が{5}から{6}/{7}に、合計が{8}から{9}/{10}に — {11}。",
                                     Name(weakest), weakest.correct, weakest.total, k, GroupTitle(exam, g.groupId), g.scored, newGroup, g.max, total, newTotal, totalMax, Status(newTotal, pass, Replace(record.groups, g.groupId, newGroup)));
                if (k < need) text += NihongoLife.Core.Loc.F(" (Phần này chỉ còn {0} câu sai — cần cải thiện thêm phần khác.)", " (Only {0} wrong answers left here — improve other parts too.)", "（このもんだいの間違いは残り{0}問です。ほかの部分も伸ばしましょう。）", weakest.Wrong);
                suggestions.Add((newTotal - total, text));
            }
            report.advice.AddRange(suggestions.OrderByDescending(s => s.gain).Select(s => s.text));

            var strongest = report.mondai.Where(m => m.total >= 2).OrderByDescending(m => m.Accuracy).FirstOrDefault();
            if (strongest != null && strongest.Accuracy >= 0.8f)
                report.advice.Add(NihongoLife.Core.Loc.F("Điểm mạnh: {0} ({1}/{2}). Giữ phong độ phần này.", "Strength: {0} ({1}/{2}). Keep it up.", "得意：{0}（{1}/{2}）。この調子で。", Name(strongest), strongest.correct, strongest.total));
            report.advice.Add(NihongoLife.Core.Loc.T("Điểm quy đổi tuyến tính để luyện tập; JLPT thật dùng điểm quy chuẩn (scaled score) nên có thể lệch vài điểm.", "Scores are converted linearly for practice; the real JLPT uses scaled scores, so it may differ by a few points.", "練習用に点数を直線的に換算しています。本番のJLPTは尺度得点なので、数点ずれることがあります。"));
            return report;
        }

        /// <summary>"chưa đạt" / "đạt sát ngưỡng (nguy hiểm)" / "khá an toàn" / "an toàn" for a total and division scores.</summary>
        public static string Status(int total, int pass, IEnumerable<ExamGroupResult> groups)
        {
            var below = groups?.Where(g => g.scored < g.passMin).ToList() ?? new List<ExamGroupResult>();
            if (total < pass) return NihongoLife.Core.Loc.F("chưa đạt (cần {0})", "not passed (needs {0})", "不合格（{0}点必要）", pass);
            if (below.Count > 0) return NihongoLife.Core.Loc.T("tổng đủ nhưng có phần dưới điểm liệt", "total is enough but a section is below its minimum", "合計は足りているが基準点未満の分野あり");
            int margin = total - pass;
            if (margin < 10) return NihongoLife.Core.Loc.F("đạt sát ngưỡng {0} — nguy hiểm", "just over the {0} pass mark — risky", "合格点{0}ぎりぎり — 危険", pass);
            if (margin < 25) return NihongoLife.Core.Loc.T("khá an toàn", "fairly safe", "まあ安全");
            return NihongoLife.Core.Loc.T("an toàn", "safe", "安全");
        }

        private static IEnumerable<ExamGroupResult> Replace(List<ExamGroupResult> groups, string id, int scored) =>
            groups.Select(g => g.groupId == id ? new ExamGroupResult { groupId = g.groupId, scored = scored, max = g.max, passMin = g.passMin } : g);

        private static int Scale(int raw, int rawMax, int max) => rawMax > 0 ? Mathf.RoundToInt((float)raw / rawMax * max) : 0;

        private static int RawNeeded(int raw, int rawMax, int max, int target)
        {
            for (int k = 1; raw + k <= rawMax; k++) if (Scale(raw + k, rawMax, max) >= target) return k;
            return rawMax - raw;
        }

        private static string Name(MondaiStat m) => m.mondaiId == "_" || !NihongoLife.Core.Loc.IsVietnamese || string.IsNullOrEmpty(m.titleVi) ? m.titleJa : $"{m.titleJa} ({m.titleVi})";

        public static string GroupTitle(ExamDefinition exam, string groupId)
        {
            var g = exam.scoreGroups?.FirstOrDefault(x => x.id == groupId);
            return g == null ? groupId : g.titleJa;
        }
    }
}
