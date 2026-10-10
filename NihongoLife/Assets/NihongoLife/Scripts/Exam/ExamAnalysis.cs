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
                report.advice.Add($"⚠ {GroupTitle(exam, g.groupId)}: {g.scored}/{g.max} — dưới điểm liệt {g.passMin}. Dù tổng điểm đủ vẫn chưa đậu.");

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
                string text = $"{Name(weakest)}: đúng {weakest.correct}/{weakest.total}. Làm đúng thêm {k} câu → {GroupTitle(exam, g.groupId)} từ {g.scored} lên {newGroup}/{g.max}; " +
                              $"tổng từ {total} lên {newTotal}/{totalMax} — {Status(newTotal, pass, Replace(record.groups, g.groupId, newGroup))}.";
                if (k < need) text += $" (もんだい này chỉ còn {weakest.Wrong} câu sai — cần cải thiện thêm phần khác.)";
                suggestions.Add((newTotal - total, text));
            }
            report.advice.AddRange(suggestions.OrderByDescending(s => s.gain).Select(s => s.text));

            var strongest = report.mondai.Where(m => m.total >= 2).OrderByDescending(m => m.Accuracy).FirstOrDefault();
            if (strongest != null && strongest.Accuracy >= 0.8f)
                report.advice.Add($"Điểm mạnh: {Name(strongest)} ({strongest.correct}/{strongest.total}). Giữ phong độ phần này.");
            report.advice.Add("Điểm quy đổi tuyến tính để luyện tập; JLPT thật dùng điểm quy chuẩn (scaled score) nên có thể lệch vài điểm.");
            return report;
        }

        /// <summary>"chưa đạt" / "đạt sát ngưỡng (nguy hiểm)" / "khá an toàn" / "an toàn" for a total and division scores.</summary>
        public static string Status(int total, int pass, IEnumerable<ExamGroupResult> groups)
        {
            var below = groups?.Where(g => g.scored < g.passMin).ToList() ?? new List<ExamGroupResult>();
            if (total < pass) return $"chưa đạt (cần {pass})";
            if (below.Count > 0) return "tổng đủ nhưng có phần dưới điểm liệt";
            int margin = total - pass;
            if (margin < 10) return $"đạt sát ngưỡng {pass} — nguy hiểm";
            if (margin < 25) return "khá an toàn";
            return "an toàn";
        }

        private static IEnumerable<ExamGroupResult> Replace(List<ExamGroupResult> groups, string id, int scored) =>
            groups.Select(g => g.groupId == id ? new ExamGroupResult { groupId = g.groupId, scored = scored, max = g.max, passMin = g.passMin } : g);

        private static int Scale(int raw, int rawMax, int max) => rawMax > 0 ? Mathf.RoundToInt((float)raw / rawMax * max) : 0;

        private static int RawNeeded(int raw, int rawMax, int max, int target)
        {
            for (int k = 1; raw + k <= rawMax; k++) if (Scale(raw + k, rawMax, max) >= target) return k;
            return rawMax - raw;
        }

        private static string Name(MondaiStat m) => m.mondaiId == "_" ? m.titleJa : $"{m.titleJa}" + (string.IsNullOrEmpty(m.titleVi) ? "" : $" ({m.titleVi})");

        public static string GroupTitle(ExamDefinition exam, string groupId)
        {
            var g = exam.scoreGroups?.FirstOrDefault(x => x.id == groupId);
            return g == null ? groupId : g.titleJa;
        }
    }
}
