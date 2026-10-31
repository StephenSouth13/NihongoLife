using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Course;

namespace NihongoLife.StudyModes.Plan
{
    /// <summary>One passed milestone checkpoint: the badge and the in-game certificate are drawn from it.</summary>
    [Serializable]
    public sealed class MilestoneRecord
    {
        public PlanTrack track;
        public string levelId;
        public string playerName;
        public int correct, total;
        public long utcTicks;
        public List<SkillScore> skills = new List<SkillScore>();
        public double Ratio => total <= 0 ? 0 : (double)correct / total;
        public int Percent => (int)Math.Round(Ratio * 100);
        public DateTime UtcDate => new DateTime(utcTicks, DateTimeKind.Utc);
        public string Serial => "NL-" + track.ToString().Substring(0, 2).ToUpperInvariant() + "-" + levelId.Replace(".", "") + "-" + UtcDate.ToString("yyyyMMdd");
    }

    [Serializable]
    public sealed class SkillScore
    {
        public string skill;
        public int correct, total;
    }

    /// <summary>
    /// Milestone checkpoint tests: a mixed set (vocabulary, grammar, reading, listening) drawn from content the game
    /// already has (school certificate practice sets + unit exercises at that level), played in the existing
    /// <c>CourseQuizView</c> (timer, instant feedback, summary with review). Passing (70 %) unlocks the next phase.
    /// </summary>
    public static class MilestoneCheckpoint
    {
        public const double PassRatio = 0.7;
        public const int QuestionCount = 16;
        public const int SecondsPerQuestion = 45;
        public static readonly string[] Skills = { "vocab", "grammar", "reading", "listening", "other" };

        public static string Target(PlanTrack t) => t switch { PlanTrack.Japanese => "ja", PlanTrack.Russian => "ru", _ => "en" };

        /// <summary>School level (A1..C1) whose units and practice set feed the checkpoint.</summary>
        public static string SchoolLevel(PlanTrack t, string levelId)
        {
            switch (t)
            {
                case PlanTrack.Japanese:
                    return levelId switch { "N5" => "A1", "N4" => "A2", "N3" => "B1", "N2" => "B2", _ => "C1" };
                case PlanTrack.Ielts:
                    double b = double.TryParse(levelId, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
                    return b < 4 ? "A1" : b < 5 ? "A2" : b < 6 ? "B1" : b < 7 ? "B2" : "C1";
                default:
                    return levelId == "C2" ? "C1" : levelId;
            }
        }

        public static string CertificateIdFor(PlanTrack t, string levelId)
        {
            var ids = SchoolCatalog.CertificateIds(Target(t));
            if (t == PlanTrack.Japanese) return ids.FirstOrDefault(i => i.EndsWith("_" + levelId.ToLowerInvariant()));
            if (t == PlanTrack.Ielts)
            {
                string lv = SchoolLevel(t, levelId);
                return string.CompareOrdinal(levelId, "5.0") >= 0 ? SchoolCatalog.IeltsStyleId : ids.FirstOrDefault(i => i.EndsWith("_" + lv.ToLowerInvariant()));
            }
            return ids.FirstOrDefault(i => i.EndsWith("_" + SchoolLevel(t, levelId).ToLowerInvariant()));
        }

        public static string SkillOf(CourseQuestion q)
        {
            string s = ((q.Skill ?? "") + " " + (q.SourceType ?? "")).ToLowerInvariant();
            if (q.ContextIsListening || s.Contains("listen") || q.AudioLines.Count > 0 || !string.IsNullOrEmpty(q.StemAudio)) return "listening";
            if (s.Contains("read") || !string.IsNullOrEmpty(q.Context)) return "reading";
            if (s.Contains("gram") || s.Contains("tense") || s.Contains("pattern") || s.Contains("order")) return "grammar";
            if (s.Contains("vocab") || s.Contains("word") || s.Contains("kanji") || s.Contains("alphabet") || s.Contains("meaning")) return "vocab";
            return "other";
        }

        /// <summary>The checkpoint's questions: round-robin over skills so every skill the content has is represented.</summary>
        public static List<CourseQuestion> BuildQuestions(PlanTrack t, string levelId, int seed = 0)
        {
            var pool = new List<CourseQuestion>();
            string cid = CertificateIdFor(t, levelId);
            var cert = cid == null ? null : SchoolCatalog.Certificate(cid);
            if (cert != null) pool.AddRange(cert.Questions);
            string lv = SchoolLevel(t, levelId);
            foreach (var u in SchoolCatalog.Units(Target(t)).Where(u => u.Level == lv))
                foreach (var e in u.Exercises) pool.AddRange(e.Questions);
            pool = pool.Where(q => q != null && !string.IsNullOrEmpty(q.Id)).GroupBy(q => q.Id).Select(g => g.First()).ToList();
            var rng = new Random(seed ^ (t.ToString() + levelId).GetHashCode());
            var buckets = Skills.Select(s => pool.Where(q => SkillOf(q) == s).OrderBy(_ => rng.Next()).ToList()).ToList();
            var outList = new List<CourseQuestion>();
            for (int round = 0; outList.Count < QuestionCount && buckets.Any(b => b.Count > round); round++)
                foreach (var b in buckets)
                    if (b.Count > round && outList.Count < QuestionCount) outList.Add(b[round]);
            return outList;
        }

        public static List<SkillScore> Breakdown(IList<CourseQuestion> questions, IDictionary<string, bool> byId)
        {
            var list = new List<SkillScore>();
            foreach (var s in Skills)
            {
                var qs = questions.Where(q => SkillOf(q) == s).ToList();
                if (qs.Count == 0) continue;
                list.Add(new SkillScore { skill = s, total = qs.Count, correct = qs.Count(q => byId != null && byId.TryGetValue(q.Id, out bool ok) && ok) });
            }
            return list;
        }

        public static bool Passed(LearningPlanState s, PlanTrack t, string levelId) => Best(s, t, levelId) != null;

        public static MilestoneRecord Best(LearningPlanState s, PlanTrack t, string levelId) =>
            s?.milestones?.Where(m => m.track == t && m.levelId == levelId).OrderByDescending(m => m.correct * 1000 / Math.Max(1, m.total)).FirstOrDefault();

        /// <summary>Phase 0 is always open; each later phase opens once the previous checkpoint is passed.</summary>
        public static bool IsUnlocked(LearningPlan plan, LearningPlanState s, int index) =>
            index <= 0 || (index < plan.phases.Count && Passed(s, plan.track, plan.phases[index - 1].levelId));

        /// <summary>Stores the attempt when it passes (keeping the best per level). Returns the new record, or null if not passed.</summary>
        public static MilestoneRecord Record(LearningPlanState s, PlanTrack t, string levelId, int correct, int total, List<SkillScore> skills, string playerName, DateTime utcNow)
        {
            if (total <= 0 || (double)correct / total + 1e-9 < PassRatio) return null;
            var rec = new MilestoneRecord { track = t, levelId = levelId, correct = correct, total = total, skills = skills ?? new List<SkillScore>(), playerName = playerName, utcTicks = utcNow.Ticks };
            var old = Best(s, t, levelId);
            if (old != null && old.Ratio >= rec.Ratio) return old;
            s.milestones.RemoveAll(m => m.track == t && m.levelId == levelId);
            s.milestones.Add(rec);
            if (!s.doneLevels.Contains(levelId) && s.track == t) s.doneLevels.Add(levelId);
            return rec;
        }
    }
}
