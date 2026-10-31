using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.StudyModes.Plan
{
    public enum PlanTrack { Japanese, English, Russian, Ielts }

    /// <summary>Where a plan item sends the player (the Study hub / Exam Center screens that already exist).</summary>
    public enum PlanTarget { None, School, Patterns, EnglishBasic, Tenses, RussianPath, ModelAnswers, ExamCenter, Videos, Dictionary }

    [Serializable]
    public sealed class PlanItem
    {
        public string vi, en, ja, arg;
        public PlanTarget target;
        /// <summary>vi/en/ja are templates ({0} = level) so the Russian table translates the template.</summary>
        public string Text => Loc.F(vi, en, ja, arg ?? "");
    }

    [Serializable]
    public sealed class PlanPhase
    {
        public string levelId;        // "A1", "N5", "6.5" …
        public double hours;          // estimated hours for this phase
        public double cumulativeHours;
        public int startWeek, endWeek; // 1-based, inclusive
        public List<PlanItem> items = new List<PlanItem>();
        public string milestoneVi, milestoneEn, milestoneJa;
        public string Milestone => Loc.F(milestoneVi, milestoneEn, milestoneJa, levelId);
    }

    [Serializable]
    public sealed class LearningPlan
    {
        public PlanTrack track;
        public string fromLevel, goalLevel;
        public int minutesPerDay;
        public int daysPerWeek = 7;
        public double totalHours;
        public int totalWeeks;
        public List<PlanPhase> phases = new List<PlanPhase>();
        public double HoursPerWeek => minutesPerDay * daysPerWeek / 60.0;
        public PlanPhase PhaseAtHours(double hours) => phases.FirstOrDefault(p => hours < p.cumulativeHours - 1e-6) ?? phases.LastOrDefault();
    }

    /// <summary>
    /// Personal roadmap: from a current level to a goal at a daily study time, using published guided-learning-hour
    /// estimates (sources and assumptions: Docs/LEARNING_PLAN.md). All figures are estimates, never promises.
    /// </summary>
    public static class LearningPlanner
    {
        public static readonly int[] DailyMinutes = { 15, 30, 60, 120 };
        public static readonly string[] CefrLevels = { "0", "A1", "A2", "B1", "B2", "C1", "C2" };
        public static readonly string[] JlptLevels = { "0", "N5", "N4", "N3", "N2", "N1" };
        public static readonly string[] IeltsBands = { "0", "4.0", "4.5", "5.0", "5.5", "6.0", "6.5", "7.0", "7.5", "8.0", "8.5", "9.0" };

        /// <summary>Cambridge English cumulative guided learning hours, midpoints (A1 90–100 … C2 1000–1200).</summary>
        public static readonly Dictionary<string, double> CefrHours = new Dictionary<string, double>
        { { "0", 0 }, { "A1", 95 }, { "A2", 190 }, { "B1", 375 }, { "B2", 550 }, { "C1", 750 }, { "C2", 1100 } };

        /// <summary>Japanese Language Education Center survey (2010–2015), learners without a kanji background, midpoints.</summary>
        public static readonly Dictionary<string, double> JlptHours = new Dictionary<string, double>
        { { "0", 0 }, { "N5", 462.5 }, { "N4", 787.5 }, { "N3", 1325 }, { "N2", 2200 }, { "N1", 3900 } };

        /// <summary>Russian has six cases, three genders, verb aspect and a new alphabet: assumed 1.3× the CEFR hours.</summary>
        public const double RussianFactor = 1.3;
        /// <summary>Commonly cited ~200 study hours per IELTS band; band 4.0 taken as ≈ B1 (375 h from zero).</summary>
        public const double IeltsHoursPerBand = 200, IeltsBand4Hours = 375;

        public static string[] Levels(PlanTrack t) => t == PlanTrack.Japanese ? JlptLevels : t == PlanTrack.Ielts ? IeltsBands : CefrLevels;
        /// <summary>The "master" goal: C2, N1 or band 9.0.</summary>
        public static string MasterLevel(PlanTrack t) => Levels(t).Last();

        public static double CumulativeHours(PlanTrack t, string level)
        {
            switch (t)
            {
                case PlanTrack.Japanese: return JlptHours.TryGetValue(level ?? "0", out var j) ? j : throw new ArgumentException("JLPT level " + level);
                case PlanTrack.Ielts:
                    if (!float.TryParse(level, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float band) || band < 0 || band > 9)
                        throw new ArgumentException("IELTS band " + level);
                    return band <= 4f ? IeltsBand4Hours * band / 4.0 : IeltsBand4Hours + (band - 4.0) * IeltsHoursPerBand;
                default:
                    if (!CefrHours.TryGetValue(level ?? "0", out var c)) throw new ArgumentException("CEFR level " + level);
                    return t == PlanTrack.Russian ? c * RussianFactor : c;
            }
        }

        public static int WeeksFor(double hours, int minutesPerDay, int daysPerWeek = 7)
        {
            if (minutesPerDay <= 0 || daysPerWeek <= 0) throw new ArgumentOutOfRangeException(nameof(minutesPerDay));
            if (hours <= 0) return 0;
            return (int)Math.Ceiling(hours * 60.0 / minutesPerDay / daysPerWeek - 1e-9);
        }

        public static LearningPlan Build(PlanTrack track, string fromLevel, string goalLevel, int minutesPerDay, int daysPerWeek = 7)
        {
            var levels = Levels(track);
            int from = Array.IndexOf(levels, fromLevel), goal = Array.IndexOf(levels, goalLevel);
            if (from < 0) throw new ArgumentException("from " + fromLevel);
            if (goal < 0) throw new ArgumentException("goal " + goalLevel);
            var plan = new LearningPlan { track = track, fromLevel = fromLevel, goalLevel = goalLevel, minutesPerDay = minutesPerDay, daysPerWeek = daysPerWeek };
            if (goal <= from) return plan;
            double start = CumulativeHours(track, fromLevel), done = 0;
            int week = 0;
            for (int i = from + 1; i <= goal; i++)
            {
                double cum = CumulativeHours(track, levels[i]) - start;
                var phase = new PlanPhase { levelId = levels[i], hours = cum - done, cumulativeHours = cum };
                phase.startWeek = week + 1;
                // Phase ends at the week in which the cumulative hours are reached (so totals never drift from rounding).
                phase.endWeek = Math.Max(phase.startWeek, WeeksFor(cum, minutesPerDay, daysPerWeek));
                week = phase.endWeek;
                Fill(track, levels[i], i == from + 1 && from == 0, phase);
                foreach (var item in phase.items) item.arg = levels[i];
                plan.phases.Add(phase);
                done = cum;
            }
            plan.totalHours = done;
            plan.totalWeeks = week;
            return plan;
        }

        private static PlanItem I(string vi, string en, string ja, PlanTarget t) => new PlanItem { vi = vi, en = en, ja = ja, target = t };

        private static void Fill(PlanTrack track, string level, bool fromZero, PlanPhase p)
        {
            var it = p.items;
            switch (track)
            {
                case PlanTrack.Japanese:
                    if (fromZero) it.Add(I("Bảng chữ cái hiragana và katakana", "Hiragana and katakana alphabets", "ひらがな・カタカナ", PlanTarget.School));
                    it.Add(I("Bài học tiếng Nhật trình độ {0} ở Trường học", "School: Japanese lessons at {0}", "学校：{0}の日本語レッスン", PlanTarget.School));
                    if (level == "N5" || level == "N4" || level == "N3")
                        it.Add(I("Mẫu câu {0} và bài luyện", "{0} sentence patterns and drills", "{0}の文型とドリル", PlanTarget.Patterns));
                    it.Add(I("Ôn từ vựng mỗi ngày trong Từ điển và Sổ tay", "Daily vocabulary review in the Dictionary and Notebook", "辞書とノートで毎日の単語復習", PlanTarget.Dictionary));
                    it.Add(I("Đề thi thử JLPT {0} ở Trung tâm luyện thi", "JLPT {0} practice test in the Exam Center", "試験センターでJLPT {0}模擬試験", PlanTarget.ExamCenter));
                    p.milestoneVi = "Đậu đề thi thử JLPT {0}"; p.milestoneEn = "Pass the JLPT {0} practice test"; p.milestoneJa = "JLPT {0}模擬試験に合格";
                    break;
                case PlanTrack.Russian:
                    if (fromZero)
                    {
                        it.Add(I("Bảng chữ cái Kirin", "The Cyrillic alphabet", "キリル文字", PlanTarget.RussianPath));
                        it.Add(I("Phát âm: trọng âm, phụ âm mềm/cứng", "Pronunciation: stress, soft and hard consonants", "発音：アクセント、軟子音・硬子音", PlanTarget.RussianPath));
                    }
                    if (level == "C1" || level == "C2")
                    {
                        it.Add(I("Bài học tiếng Nga {0} ở Trường học", "School: Russian lessons at {0}", "学校：{0}のロシア語レッスン", PlanTarget.School));
                        it.Add(I("Đọc báo, nghe tin tức và luyện viết luận bằng tiếng Nga", "Read news, listen to broadcasts and write essays in Russian", "ロシア語でニュースを読み・聞き、小論文を書く", PlanTarget.Videos));
                    }
                    else
                    {
                        it.Add(I("Lộ trình tiếng Nga: các bài {0}", "Russian path: {0} units", "ロシア語コース：{0}のユニット", PlanTarget.RussianPath));
                        it.Add(I("Đề thi thử TORFL {0} có đáp án", "TORFL {0} mock test with answer key", "TORFL {0}模擬試験（解答付き）", PlanTarget.RussianPath));
                    }
                    it.Add(I("Ôn từ vựng Nga–Việt mỗi ngày trong Từ điển", "Daily Russian vocabulary review in the Dictionary", "辞書で毎日のロシア語単語復習", PlanTarget.Dictionary));
                    p.milestoneVi = "Đạt trình độ {0} (TORFL)"; p.milestoneEn = "Reach {0} (TORFL)"; p.milestoneJa = "{0}（TORFL）に到達";
                    break;
                case PlanTrack.English:
                    if (fromZero) it.Add(I("Tiếng Anh cơ bản: chào hỏi, số đếm, mua sắm…", "English Basic: greetings, numbers, shopping…", "基礎英語：あいさつ・数字・買い物…", PlanTarget.EnglishBasic));
                    it.Add(I("Bài học tiếng Anh {0} ở Trường học", "School: English lessons at {0}", "学校：{0}の英語レッスン", PlanTarget.School));
                    if (level == "A2" || level == "B1") it.Add(I("12 thì tiếng Anh và bài tập", "The 12 English tenses with exercises", "英語の12時制と練習", PlanTarget.Tenses));
                    if (level != "C2") it.Add(I("Đề luyện tiếng Anh {0} ở Trung tâm luyện thi", "English {0} practice test in the Exam Center", "試験センターで英語{0}練習問題", PlanTarget.ExamCenter));
                    else it.Add(I("Đọc báo, nghe podcast và viết luận tiếng Anh mỗi ngày", "Read news, listen to podcasts and write essays in English every day", "毎日英語でニュース・ポッドキャスト・小論文", PlanTarget.Videos));
                    p.milestoneVi = "Đạt trình độ {0}"; p.milestoneEn = "Reach {0}"; p.milestoneJa = "{0}に到達";
                    break;
                case PlanTrack.Ielts:
                    float band = float.Parse(level, System.Globalization.CultureInfo.InvariantCulture);
                    if (band <= 4.5f) it.Add(I("Nền tảng: Tiếng Anh cơ bản và 12 thì", "Foundations: English Basic and the 12 tenses", "基礎：基礎英語と12時制", PlanTarget.EnglishBasic));
                    it.Add(I("Luyện Nghe và Đọc IELTS có chấm điểm", "Graded IELTS Listening and Reading practice", "採点付きIELTSリスニング・リーディング", PlanTarget.ExamCenter));
                    if (band >= 5f) it.Add(I("Bài mẫu Viết/Nói band {0} và tự chấm theo tiêu chí", "Band {0} Writing/Speaking model answers and self-assessment", "バンド{0}のライティング・スピーキング模範解答と自己評価", PlanTarget.ModelAnswers));
                    it.Add(I("Một đề thi thử IELTS đầy đủ", "One full IELTS mock test", "IELTS模擬試験を1回", PlanTarget.ExamCenter));
                    p.milestoneVi = "Đạt band {0} trong đề thi thử"; p.milestoneEn = "Score band {0} in a mock test"; p.milestoneJa = "模擬試験でバンド{0}";
                    break;
            }
        }

        /// <summary>
        /// Re-estimates the finish from real progress: once there are at least 7 days of history, the observed pace
        /// (hours per day since the start) replaces the planned pace; before that the plan's pace is used.
        /// Returns days remaining (0 when done).
        /// </summary>
        public static int RemainingDays(LearningPlan plan, double hoursDone, int daysElapsed)
        {
            double left = Math.Max(0, plan.totalHours - hoursDone);
            if (left <= 0) return 0;
            double plannedPerDay = plan.minutesPerDay * plan.daysPerWeek / 7.0 / 60.0;
            double pace = daysElapsed >= 7 && hoursDone > 0 ? hoursDone / daysElapsed : plannedPerDay;
            pace = Math.Max(pace, plannedPerDay * 0.1); // never divide by ~0: a stalled plan still shows a (long) ETA
            return (int)Math.Ceiling(left / pace - 1e-9);
        }

        /// <summary>Hours the plan expects to be done after <paramref name="daysElapsed"/> days.</summary>
        public static double ExpectedHours(LearningPlan plan, int daysElapsed) =>
            Math.Min(plan.totalHours, Math.Max(0, daysElapsed) * plan.minutesPerDay * plan.daysPerWeek / 7.0 / 60.0);
    }

    /// <summary>The player's saved plan settings and logged study time (persistentDataPath/learning_plan.json).</summary>
    [Serializable]
    public sealed class LearningPlanState
    {
        public PlanTrack track;
        public string fromLevel = "0", goalLevel = "B1";
        public int minutesPerDay = 30;
        public long startedUtcTicks;
        public int minutesLogged;
        public List<string> doneLevels = new List<string>();
        /// <summary>Passed checkpoint tests (badges / certificates), kept across plans and tracks.</summary>
        public List<MilestoneRecord> milestones = new List<MilestoneRecord>();
        public bool HasPlan => startedUtcTicks > 0;
        public double HoursDone => minutesLogged / 60.0;
        public int DaysElapsed(DateTime nowUtc) => HasPlan ? Math.Max(0, (int)(nowUtc - new DateTime(startedUtcTicks, DateTimeKind.Utc)).TotalDays) : 0;
        public LearningPlan Build() => LearningPlanner.Build(track, fromLevel, goalLevel, minutesPerDay);
    }

    public static class LearningPlanStore
    {
        /// <summary>Tests point this at a temp file.</summary>
        public static string PathOverride;
        public static string FilePath => PathOverride ?? System.IO.Path.Combine(Application.persistentDataPath, "learning_plan.json");

        public static LearningPlanState Load()
        {
            try { if (File.Exists(FilePath)) return JsonUtility.FromJson<LearningPlanState>(File.ReadAllText(FilePath)) ?? new LearningPlanState(); }
            catch (Exception e) { Debug.LogWarning("[LearningPlan] " + e.Message); }
            return new LearningPlanState();
        }

        public static void Save(LearningPlanState s)
        {
            try { NihongoLife.Save.LocalProgressRepository.WriteAtomic(FilePath, JsonUtility.ToJson(s, true)); NihongoLife.Save.LocalProgressRepository.FlushWebStorage(); }
            catch (Exception e) { Debug.LogWarning("[LearningPlan] " + e.Message); }
        }
    }
}
