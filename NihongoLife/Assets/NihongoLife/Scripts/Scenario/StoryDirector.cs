using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;

namespace NihongoLife.Scenario
{
    /// <summary>
    /// How the story and free play fit together.
    ///
    /// Prologue — the campaign up to <see cref="GameControlDatabase.prologueEndScenarioId"/> (arrival → first talk with
    /// Tanaka → greeting at his house → first lesson → first konbini purchase). It is guided: entering the town resumes
    /// the quest in progress or starts the next one, the result screen goes straight on, and jobs / farm / daily tasks
    /// wait until it is done.
    ///
    /// Free play — afterwards the story becomes one thread among many: chapter 3 opens several quests at once, each
    /// needing some knowledge, which the player earns in any order (school, desk, jobs, farm, exams, games). The HUD
    /// shows the next story step (or what is missing for it); the player starts it from the journal (N) when they like.
    /// Nothing starts by itself any more, except a quest that was interrupted.
    /// </summary>
    public static class StoryDirector
    {
        public const string FreePlayFlag = "story.free_play";
        public const string DefaultPrologueEnd = "scenario.konbini.buy_onigiri";

        /// <summary>Tests that are not about the story set this (PlayModeSaveGuard): no auto-start, no prologue gate.</summary>
        public static bool Bypass;

        private static PlayerProgressDto Progress =>
            GameServices.TryGet(out IProgressRepository repository) ? repository.GetProgress() : null;

        private static IScenarioRepository Repository =>
            GameServices.TryGet(out IScenarioRepository repository) ? repository : null;

        public static string PrologueEnd
        {
            get
            {
                if (GameServices.TryGet(out GameControlService control) && control.Database != null && !string.IsNullOrWhiteSpace(control.Database.prologueEndScenarioId))
                    return control.Database.prologueEndScenarioId;
                return DefaultPrologueEnd;
            }
        }

        /// <summary>The main quests in story order.</summary>
        public static IReadOnlyList<string> Campaign
        {
            get
            {
                if (GameServices.TryGet(out GameControlService control)) return control.GetCampaignScenarioIds(Repository);
                return new List<string>();
            }
        }

        public static bool IsDone(string scenarioId)
        {
            var progress = Progress;
            return progress?.completedScenarios != null && progress.completedScenarios.Contains(scenarioId);
        }

        /// <summary>Still in the guided opening (no progress service = not in a game: false).</summary>
        public static bool InPrologue => Progress != null && !IsDone(PrologueEnd) && Campaign.Contains(PrologueEnd);

        /// <summary>Jobs, farm and daily tasks wait for the end of the prologue.</summary>
        public static bool GatesFreePlay => !Bypass && InPrologue;

        public static bool StoryComplete => Progress != null && Campaign.Count > 0 && Campaign.All(IsDone);

        /// <summary>The prologue quests (campaign order up to and including its last quest).</summary>
        public static IEnumerable<string> PrologueIds
        {
            get
            {
                foreach (var id in Campaign)
                {
                    yield return id;
                    if (id == PrologueEnd) yield break;
                }
            }
        }

        public static (int done, int total) PrologueProgress()
        {
            var ids = PrologueIds.ToList();
            return (ids.Count(IsDone), ids.Count);
        }

        /// <summary>A story quest that was interrupted (quit or scene change mid-dialogue), still open.</summary>
        public static ScenarioDefinition Interrupted()
        {
            var progress = Progress;
            if (progress == null || string.IsNullOrEmpty(progress.activeScenarioId) || IsDone(progress.activeScenarioId)) return null;
            // Only story quests count (a side or test scenario left in the save is not "the story"), and only ids the
            // campaign lists — so an unknown id is never looked up (the repository logs an error for those).
            if (!Campaign.Contains(progress.activeScenarioId)) return null;
            return Repository?.GetScenarioById(progress.activeScenarioId);
        }

        /// <summary>Main quests not done yet, in story order.</summary>
        public static IEnumerable<ScenarioDefinition> Remaining()
        {
            var repo = Repository;
            if (repo == null) yield break;
            foreach (var id in Campaign)
            {
                if (IsDone(id)) continue;
                var s = repo.GetScenarioById(id);
                if (s != null) yield return s;
            }
        }

        /// <summary>Why a story quest cannot start yet (null when it can).</summary>
        public static string Blocker(ScenarioDefinition scenario)
        {
            var progress = Progress;
            if (scenario == null || progress == null) return Loc.T("Chưa sẵn sàng.", "Not ready.", "準備中です。");
            foreach (var required in scenario.requiredScenarioIds ?? new List<string>())
                if (!IsDone(required))
                {
                    var r = Repository?.GetScenarioById(required);
                    return Loc.F("Xong \"{0}\" trước.", "Finish \"{0}\" first.", "先に「{0}」を終えましょう。", r != null ? r.DisplayTitle : required);
                }
            if (progress.knowledge < scenario.requiredKnowledge)
                return Loc.F("Cần {0} điểm kiến thức — còn thiếu {1}.", "Needs {0} knowledge — {1} to go.", "知識ポイント{0}が必要（あと{1}）。", scenario.requiredKnowledge, scenario.requiredKnowledge - progress.knowledge);
            return null;
        }

        /// <summary>
        /// The story step to point at: the interrupted quest, else the first remaining quest that can start, else the
        /// closest one (the first remaining whose earlier quests are done — then its blocker is the knowledge to earn).
        /// </summary>
        public static ScenarioDefinition Next()
        {
            var interrupted = Interrupted();
            if (interrupted != null) return interrupted;
            var remaining = Remaining().ToList();
            return remaining.FirstOrDefault(s => Blocker(s) == null)
                ?? remaining.FirstOrDefault(s => (s.requiredScenarioIds ?? new List<string>()).All(IsDone))
                ?? remaining.FirstOrDefault();
        }

        /// <summary>What to start on entering the town: an interrupted quest always; the next one only in the prologue.</summary>
        public static ScenarioDefinition AutoStart()
        {
            if (Bypass) return null;
            var interrupted = Interrupted();
            if (interrupted != null) return interrupted;
            if (!InPrologue) return null;
            var next = Next();
            return next != null && Blocker(next) == null ? next : null;
        }

        /// <summary>Free play has been announced (the card shows once).</summary>
        public static bool FreePlayAnnounced => StoryFlags.Has(FreePlayFlag);

        public static void MarkFreePlayAnnounced() => StoryFlags.Set(FreePlayFlag);

        public static string ChapterName(int chapter) => chapter switch
        {
            0 => Loc.T("Mở màn", "Prologue", "プロローグ"),
            1 => Loc.T("Chương 1 · はじめまして", "Chapter 1 · はじめまして", "第1章 · はじめまして"),
            2 => Loc.T("Chương 2 · コンビニ", "Chapter 2 · コンビニ", "第2章 · コンビニ"),
            3 => Loc.T("Chương 3 · 近所とレストラン", "Chapter 3 · 近所とレストラン", "第3章 · 近所とレストラン"),
            4 => Loc.T("Chương 4 · 駅と夏祭り", "Chapter 4 · 駅と夏祭り", "第4章 · 駅と夏祭り"),
            _ => Loc.F("Chương {0}", "Chapter {0}", "第{0}章", chapter),
        };

        /// <summary>The HUD line when no quest is running: (kicker, text, progress) or null.</summary>
        public static (string kicker, string text, string progress)? Hud()
        {
            if (Progress == null || Campaign.Count == 0) return null;
            if (StoryComplete)
                return ("◆ " + Loc.T("CỐT TRUYỆN", "STORY", "ストーリー"), Loc.T("Đã xong cốt truyện mùa xuân – hè. Tự do sống ở Hibari!", "The spring–summer story is done. Live freely in Hibari!", "春〜夏のストーリーはおわり。ひばりで自由に暮らそう！"), Loc.T("Thi cử, làm thêm, đảo Midori vẫn chờ bạn", "Exams, jobs and Midori Island are still waiting", "試験・アルバイト・みどり島が待っています"));
            var next = Next();
            if (next == null) return null;
            string blocker = Blocker(next);
            string kicker = "◆ " + Loc.T("CỐT TRUYỆN", "STORY", "ストーリー") + " · " + ChapterName(next.chapterIndex);
            string where = next.LocationHint;
            string text = next.DisplayTitle + (string.IsNullOrWhiteSpace(where) ? "" : "  ·  " + where);
            string line;
            if (blocker != null) line = blocker + "  " + Loc.T("Học ở trường, làm thêm hoặc chơi tự do để tích điểm.", "Study, work or play freely to earn it.", "学校・アルバイト・自由行動で集めよう。");
            else if (InPrologue)
            {
                var (done, total) = PrologueProgress();
                line = Loc.F("Mở đầu {0}/{1} · xong mở đầu sẽ mở chế độ tự do", "Opening {0}/{1} · free play opens after it", "オープニング {0}/{1} · 終わると自由行動", done, total);
            }
            else line = Loc.T("Sẵn sàng · N mở sổ nhiệm vụ để bắt đầu", "Ready · press N and start it from the journal", "準備OK · Nのタスク帳から開始");
            return (kicker, text, line);
        }
    }
}
