using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// The map opens step by step (lộ trình mở bản đồ). A new learner can only use Hibari Mart and the school; every
    /// other place needs Trí tuệ (knowledge points, earned in class, at the desk, in exams, quests and stories) AND the
    /// story quest before it. The arrival opens the student room. Stages (defaults below, overridable in
    /// GameControlDatabase.locationUnlocks):
    ///   Phòng trọ (home)   — when the arrival starts
    ///   Sushi Hibari       — 100 · after the first konbini purchase (the end of the opening)
    ///   Ga Hibari          — 160 · after the dinner at Sushi Hibari
    ///   Game Center        — 240 · after the station trip
    ///   Đảo Midori         — 320 · after Kim's afternoon at the Game Center
    /// An opened place is remembered in the save as the story flag "map.&lt;scene&gt;" (synced to the cloud like other
    /// flags), so it stays open. The moment a stage opens, <see cref="Unlocked"/> fires (MapUnlockCardUI shows it).
    /// The place the running story step needs is never blocked.
    /// </summary>
    public static class LocationUnlocks
    {
        private const string FlagPrefix = "map.";
        private const string LegacyVisitedKey = "NL.Unlocked.";

        private static readonly LocationUnlock[] Defaults =
        {
            new LocationUnlock
            {
                sceneName = WorldLocationCatalog.HomeBedroomScene, requiredKnowledge = 0, startedByScenarioId = "scenario.intro.arrival", guideKey = "HOME",
                titleVi = "Phòng trọ của bạn", titleEn = "Your room", titleJa = "あなたの部屋",
                storyVi = "Bạn nhận phòng ở Hibari Heights — từ giờ về nhà nói 「ただいま」.", storyEn = "You move into Hibari Heights — from now on, say 「ただいま」 when you come home.", storyJa = "ひばりハイツに入居しました。帰ったら「ただいま」。",
            },
            new LocationUnlock
            {
                sceneName = WorldLocationCatalog.SushiRestaurantScene, requiredKnowledge = 100, requiredScenarioIds = new[] { "scenario.konbini.buy_onigiri" },
                gatewayScenarioId = "scenario.restaurant.sushi_dining", guideKey = "SUSHI",
                titleVi = "Quán quen đầu tiên", titleEn = "Your first restaurant", titleJa = "はじめてのお店",
                storyVi = "Bạn đã tự mua được bữa ăn đầu tiên. Giờ đủ tự tin để vào nhà hàng sushi gọi món.", storyEn = "You bought your first meal on your own. Now you are ready to order at a sushi restaurant.", storyJa = "はじめての買い物ができました。お寿司屋さんで注文してみましょう。",
            },
            new LocationUnlock
            {
                sceneName = WorldLocationCatalog.StationScene, requiredKnowledge = 160, requiredScenarioIds = new[] { "scenario.restaurant.sushi_dining" },
                gatewayScenarioId = "scenario.station.buy_ticket", guideKey = "STATION",
                titleVi = "Đi tàu", titleEn = "Taking the train", titleJa = "電車に乗ろう",
                storyVi = "Bạn đã nói chuyện được ở quán ăn. Ga Hibari mở — học mua vé, hỏi đường và đi tàu.", storyEn = "You can talk your way through a restaurant now. Hibari Station opens — buy tickets, ask the way, ride the train.", storyJa = "お店で話せるようになりました。ひばり駅へ。切符を買って電車に乗りましょう。",
            },
            new LocationUnlock
            {
                sceneName = WorldLocationCatalog.GameCenterScene, requiredKnowledge = 240, requiredScenarioIds = new[] { "scenario.station.buy_ticket" },
                gatewayScenarioId = "scenario.arcade.kim_invite", guideKey = "ARCADE",
                titleVi = "Bạn cùng lớp", titleEn = "A classmate's invitation", titleJa = "クラスメートのさそい",
                storyVi = "Kim rủ bạn sau giờ học ra Game Center — luyện số đếm và kana bằng trò chơi.", storyEn = "Kim invites you to the Game Center after class — practise numbers and kana with games.", storyJa = "キムさんが放課後ゲームセンターにさそってくれます。",
            },
            new LocationUnlock
            {
                sceneName = WorldLocationCatalog.MidoriIslandScene, requiredKnowledge = 320, requiredScenarioIds = new[] { "scenario.arcade.kim_invite" },
                gatewayScenarioId = "scenario.island.midori_trip", guideKey = "MIDORI",
                titleVi = "Đảo Xanh", titleEn = "Midori Island", titleJa = "みどり島",
                storyVi = "Tanaka mời bạn về trang trại của bà anh ấy trên đảo Midori — đi tàu ra đảo, trồng rau, câu cá.", storyEn = "Tanaka invites you to his grandmother's farm on Midori Island — take the train, grow vegetables, go fishing.", storyJa = "田中さんがみどり島のおばあさんの畑にさそってくれます。",
            },
        };

        /// <summary>Raised once when a stage opens (not while <see cref="Bypass"/> is on).</summary>
        public static event Action<LocationUnlock> Unlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Unlocked = null;

        public static IReadOnlyList<LocationUnlock> All
        {
            get
            {
                if (GameServices.TryGet(out GameControlService control) && control.Database != null && control.Database.locationUnlocks != null && control.Database.locationUnlocks.Length > 0)
                    return control.Database.locationUnlocks;
                return Defaults;
            }
        }

        public static LocationUnlock Stage(string sceneName) => All.FirstOrDefault(u => u != null && u.sceneName == sceneName);

        public static int Knowledge
        {
            get
            {
                if (NihongoLife.Player.PlayerStatus.Instance != null) return NihongoLife.Player.PlayerStatus.Instance.Knowledge;
                if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repo)) return repo.GetProgress()?.knowledge ?? 0;
                return 0;
            }
        }

        /// <summary>Trí tuệ needed for a scene (0 = no knowledge gate).</summary>
        public static int Required(string sceneName) => Mathf.Max(0, Stage(sceneName)?.requiredKnowledge ?? 0);

        /// <summary>Whether the scene is gated at all (Hibari Mart, the school and the town are not).</summary>
        public static bool IsGated(string sceneName) => Stage(sceneName) != null;

        /// <summary>Tests that walk through every place set this; the unlock / map tests clear it.</summary>
        public static bool Bypass;

        public static bool IsUnlocked(string sceneName)
        {
            if (Bypass) return true;
            var stage = Stage(sceneName);
            if (stage == null) return true;
            if (NihongoLife.Scenario.StoryFlags.Has(FlagPrefix + sceneName)) return true;
            if (ConditionsMet(stage)) return true;
            if (Legacy(stage)) return true;
            return StoryNeeds(sceneName);
        }

        public static int Missing(string sceneName) => Mathf.Max(0, Required(sceneName) - Knowledge);

        /// <summary>Story quests of the stage that are not finished yet.</summary>
        public static IEnumerable<string> MissingQuests(string sceneName)
        {
            var stage = Stage(sceneName);
            if (stage == null) yield break;
            foreach (var id in stage.requiredScenarioIds ?? Array.Empty<string>())
                if (!NihongoLife.Scenario.StoryDirector.IsDone(id)) yield return id;
            if (!string.IsNullOrEmpty(stage.startedByScenarioId) && !Started(stage.startedByScenarioId)) yield return stage.startedByScenarioId;
        }

        public static bool ConditionsMet(LocationUnlock stage)
        {
            if (stage == null) return true;
            if (Knowledge < stage.requiredKnowledge) return false;
            foreach (var id in stage.requiredScenarioIds ?? Array.Empty<string>())
                if (!NihongoLife.Scenario.StoryDirector.IsDone(id)) return false;
            return string.IsNullOrEmpty(stage.startedByScenarioId) || Started(stage.startedByScenarioId);
        }

        private static bool Started(string scenarioId)
        {
            if (NihongoLife.Scenario.StoryDirector.IsDone(scenarioId)) return true;
            var running = NihongoLife.Scenario.ScenarioManager.Instance != null ? NihongoLife.Scenario.ScenarioManager.Instance.CurrentScenario : null;
            if (running != null && running.id == scenarioId) return true;
            return GameServices.TryGet(out NihongoLife.Save.IProgressRepository repo) && repo.GetProgress()?.activeScenarioId == scenarioId;
        }

        /// <summary>Older saves remembered visits in PlayerPrefs: kept when the knowledge is there too.</summary>
        private static bool Legacy(LocationUnlock stage) =>
            PlayerPrefs.GetInt(LegacyVisitedKey + stage.sceneName, 0) == 1 && Knowledge >= stage.requiredKnowledge && (stage.requiredScenarioIds == null || stage.requiredScenarioIds.Length == 0);

        /// <summary>
        /// Opens every stage whose conditions are met now (records the flag and raises <see cref="Unlocked"/>).
        /// Called when knowledge changes, a quest starts or ends, and on entering a scene.
        /// </summary>
        public static void Evaluate()
        {
            if (Bypass || !GameServices.TryGet(out NihongoLife.Save.IProgressRepository repo)) return;
            // A save from before the roadmap (past the opening, no map flags yet) gets its open places recorded
            // quietly, instead of a row of "new place" cards at once.
            bool quiet = repo.GetProgress()?.storyFlags?.Any(f => f.StartsWith(FlagPrefix)) != true && !NihongoLife.Scenario.StoryDirector.InPrologue;
            foreach (var stage in All)
            {
                if (stage == null || NihongoLife.Scenario.StoryFlags.Has(FlagPrefix + stage.sceneName)) continue;
                if (!ConditionsMet(stage) && !Legacy(stage)) continue;
                NihongoLife.Scenario.StoryFlags.Set(FlagPrefix + stage.sceneName);
                if (!quiet) Unlocked?.Invoke(stage);
            }
        }

        /// <summary>A place entered (story-led or unlocked) stays open.</summary>
        public static void MarkVisited(string sceneName)
        {
            if (Bypass || Stage(sceneName) == null) return;
            if (!NihongoLife.Scenario.StoryFlags.Has(FlagPrefix + sceneName)) NihongoLife.Scenario.StoryFlags.Set(FlagPrefix + sceneName);
        }

        /// <summary>Tests: forget opened places.</summary>
        public static void ResetVisits()
        {
            foreach (var u in All)
            {
                if (u == null) continue;
                PlayerPrefs.DeleteKey(LegacyVisitedKey + u.sceneName);
                NihongoLife.Scenario.StoryFlags.Remove(FlagPrefix + u.sceneName);
            }
        }

        /// <summary>The next stage to open (null when everything is open).</summary>
        public static LocationUnlock Next() => All.Where(u => u != null && !IsUnlocked(u.sceneName)).OrderBy(u => u.requiredKnowledge).FirstOrDefault();

        public static string QuestTitle(string scenarioId)
        {
            if (GameServices.TryGet(out NihongoLife.Scenario.IScenarioRepository repo))
            {
                var all = repo.GetAllScenarios();
                var s = all?.FirstOrDefault(x => x != null && x.id == scenarioId);
                if (s != null) return s.DisplayTitle;
            }
            return scenarioId;
        }

        public static string StageTitle(LocationUnlock u) => u == null ? "" : Loc.Pick(u.titleVi, u.titleEn, u.titleJa) is string t && t.Length > 0 ? t : WorldLocationCatalog.Get(u.sceneName).DisplayName;
        public static string StageStory(LocationUnlock u) => u == null ? "" : Loc.Pick(u.storyVi, u.storyEn, u.storyJa);

        /// <summary>One line for doors, map pins and the guide: "Đã khóa — cần 160 trí tuệ (thiếu 40) · xong «…»".</summary>
        public static string ShortStatus(string sceneName)
        {
            var parts = new List<string>();
            int missing = Missing(sceneName);
            if (missing > 0) parts.Add(Loc.F("cần {0} trí tuệ (thiếu {1})", "needs {0} knowledge ({1} to go)", "知識{0}が必要（あと{1}）", Required(sceneName), missing));
            foreach (var id in MissingQuests(sceneName)) parts.Add(Loc.F("xong «{0}»", "finish “{0}”", "「{0}」を終える", QuestTitle(id)));
            return Loc.T("Đã khóa", "Locked", "未開放") + (parts.Count > 0 ? " — " + string.Join(" · ", parts) : "");
        }

        public static string LockedMessage(string sceneName)
        {
            string place = WorldLocationCatalog.Get(sceneName).DisplayName;
            return place + ": " + ShortStatus(sceneName) + ". " + Loc.T("Trí tuệ tăng khi học ở Trường Hibari, làm bài thi, nhiệm vụ và cốt truyện — xem Lộ trình trong Sổ nhiệm vụ (N).",
                "Knowledge grows with classes at Hibari School, exams, tasks and the story — see the Roadmap in the task journal (N).",
                "知識はひばり学校の授業・試験・タスク・ストーリーで増えます。タスク帳（N）のロードマップを見てください。");
        }

        private static bool StoryNeeds(string sceneName)
        {
            var scenario = NihongoLife.Scenario.ScenarioManager.Instance != null ? NihongoLife.Scenario.ScenarioManager.Instance.CurrentScenario : null;
            if (scenario == null) return false;
            var stage = Stage(sceneName);
            if (stage != null && (stage.gatewayScenarioId == scenario.id || stage.startedByScenarioId == scenario.id)) return true;
            var place = WorldLocationCatalog.Get(sceneName);
            string hint = (scenario.locationHintVi ?? "") + " " + (scenario.locationHintEn ?? "") + " " + (scenario.locationHintJa ?? "");
            return (!string.IsNullOrEmpty(place.VietnameseName) && hint.IndexOf(place.VietnameseName, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrEmpty(place.EnglishName) && hint.IndexOf(place.EnglishName, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
