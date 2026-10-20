using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Core
{
    /// <summary>
    /// Places open up with knowledge points. A new learner starts with the basics — Hibari Mart in the city, the
    /// school and home — earns knowledge there (lessons, the study desk, quests, scenarios), and unlocks the station,
    /// the sushi restaurant, the game centre and Midori Island in turn. Thresholds live in GameControlDatabase.
    /// A place once entered stays open (older saves keep what they had), and the place the current story step
    /// takes place in is never blocked.
    /// </summary>
    public static class LocationUnlocks
    {
        private const string VisitedKey = "NL.Unlocked.";
        private static readonly LocationUnlock[] Defaults =
        {
            new LocationUnlock { sceneName = WorldLocationCatalog.StationScene, requiredKnowledge = 40 },
            new LocationUnlock { sceneName = WorldLocationCatalog.SushiRestaurantScene, requiredKnowledge = 90 },
            new LocationUnlock { sceneName = WorldLocationCatalog.GameCenterScene, requiredKnowledge = 160 },
            new LocationUnlock { sceneName = WorldLocationCatalog.MidoriIslandScene, requiredKnowledge = 250 }
        };

        public static IReadOnlyList<LocationUnlock> All
        {
            get
            {
                if (GameServices.TryGet(out GameControlService control) && control.Database != null && control.Database.locationUnlocks != null && control.Database.locationUnlocks.Length > 0)
                    return control.Database.locationUnlocks;
                return Defaults;
            }
        }

        public static int Knowledge
        {
            get
            {
                if (NihongoLife.Player.PlayerStatus.Instance != null) return NihongoLife.Player.PlayerStatus.Instance.Knowledge;
                if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repo)) return repo.GetProgress()?.knowledge ?? 0;
                return 0;
            }
        }

        /// <summary>Knowledge needed for a scene (0 = always open).</summary>
        public static int Required(string sceneName)
        {
            foreach (var u in All) if (u != null && u.sceneName == sceneName) return Mathf.Max(0, u.requiredKnowledge);
            return 0;
        }

        /// <summary>Tests that walk through every place set this; the unlock tests clear it.</summary>
        public static bool Bypass;

        public static bool IsUnlocked(string sceneName)
        {
            if (Bypass) return true;
            int need = Required(sceneName);
            if (need <= 0 || Knowledge >= need) return true;
            if (PlayerPrefs.GetInt(VisitedKey + sceneName, 0) == 1) return true;
            return StoryNeeds(sceneName);
        }

        public static int Missing(string sceneName) => Mathf.Max(0, Required(sceneName) - Knowledge);

        /// <summary>Remember a visit so the place stays open (call when entering).</summary>
        public static void MarkVisited(string sceneName)
        {
            if (Bypass || Required(sceneName) <= 0 || PlayerPrefs.GetInt(VisitedKey + sceneName, 0) == 1) return;
            PlayerPrefs.SetInt(VisitedKey + sceneName, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Tests: forget visits.</summary>
        public static void ResetVisits()
        {
            foreach (var u in All) if (u != null) PlayerPrefs.DeleteKey(VisitedKey + u.sceneName);
        }

        /// <summary>The next place to unlock (null when everything is open).</summary>
        public static LocationUnlock Next()
        {
            LocationUnlock best = null;
            foreach (var u in All)
                if (u != null && !IsUnlocked(u.sceneName) && (best == null || u.requiredKnowledge < best.requiredKnowledge)) best = u;
            return best;
        }

        public static string LockedMessage(string sceneName)
        {
            string place = WorldLocationCatalog.Get(sceneName).DisplayName;
            return NihongoLife.Core.Loc.F("{0} cần {1} điểm kiến thức — bạn còn thiếu {2}. Học ở Trường Hibari, làm bài ở bàn học hoặc nhiệm vụ trong Sổ (N) để mở.",
                "{0} needs {1} knowledge points — you are {2} short. Study at Hibari School, at your desk, or do tasks in the journal (N) to unlock it.",
                "{0}には知識ポイントが{1}必要です（あと{2}）。ひばり学校や机で勉強したり、タスク帳（N）のタスクで集めましょう。", place, Required(sceneName), Missing(sceneName));
        }

        private static bool StoryNeeds(string sceneName)
        {
            var scenario = NihongoLife.Scenario.ScenarioManager.Instance != null ? NihongoLife.Scenario.ScenarioManager.Instance.CurrentScenario : null;
            if (scenario == null) return false;
            var place = WorldLocationCatalog.Get(sceneName);
            string hint = (scenario.locationHintVi ?? "") + " " + (scenario.locationHintEn ?? "") + " " + (scenario.locationHintJa ?? "");
            return (!string.IsNullOrEmpty(place.VietnameseName) && hint.IndexOf(place.VietnameseName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrEmpty(place.EnglishName) && hint.IndexOf(place.EnglishName, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
