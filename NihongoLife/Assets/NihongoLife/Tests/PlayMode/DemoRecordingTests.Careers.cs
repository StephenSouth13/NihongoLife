using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The career video: each of the ten careers from its first job to its top title, at its own workplace, through the real
    /// career logic (a slice of the real shift, the Japanese promotion interviews, the branch at level 4). Beat ids and the
    /// branch followed match Tools/video/career_voice.py.
    /// Explicit: -testFilter Careers_RecordFrames, with NL_DEMO_DIR (frames) and NL_DEMO_VO (the voice durations.tsv).
    /// </summary>
    public partial class DemoRecordingTests
    {
        // career, the branch the video follows, its workplace (scene, arrival spawn) — in the order the narration tells them.
        private static readonly (string id, string branch, string scene, string spawn)[] CareerRoute =
        {
            ("retail", "store", WorldLocationCatalog.CityScene, WorldLocationCatalog.CitySushiReturn),
            ("food", "owner", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance),
            ("chef", "sushi", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance),
            ("medical", "doctor", WorldLocationCatalog.ClinicScene, WorldLocationCatalog.ClinicEntrance),
            ("musician", "pianist", WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.MusicShopEntrance),
            ("producer", "producer", WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.MusicShopEntrance),
            ("painter", "fine_art", WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.MusicShopEntrance),
            ("farm", "grower", WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation),
            ("hospitality", "manager", WorldLocationCatalog.PhuQuocIslandScene, WorldLocationCatalog.PhuQuocArrival),
            ("fashion", "designer", WorldLocationCatalog.FashionStoreScene, WorldLocationCatalog.FashionStoreEntrance),
        };

        [UnityTest, Explicit, Timeout(7200000)]
        public IEnumerator Careers_RecordFrames()
        {
            LoadVoiceDurations();
            if (System.IO.Directory.Exists(FrameFolder)) System.IO.Directory.Delete(FrameFolder, true);
            System.IO.Directory.CreateDirectory(FrameFolder);
            UiScreen.Override = new Vector2Int(1920, 1080);
            UiScreen.PhoneOverride = false;
            Time.captureFramerate = 30;
            TimedAction.SpeedScale = 1f;
            CursorDirector.AssumeFocusForTests = true;
            ControlSettings.Scheme = ControlScheme.MouseLook;
            CareerService.EventChance = 0f;
            CareerClock.PromptEnabled = false;

            SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return WaitZone(WorldLocationCatalog.CityScene, 90f);
            yield return Prepare();
            var status = PlayerStatus.Instance;
            if (status.Level < CareerService.Catalog.unlockLevel) status.AddExp(20000);
            status.DebugSetKnowledge(99999);
            _rec = new GameObject("DemoRecorder").AddComponent<DemoRecorder>();
            _rec.Init(FrameFolder);
            _rec.ShowCard("Con đường sự nghiệp", "Mười ngành nghề · từ người mới đến đỉnh cao\n<size=70%>NihongoLife · hình ảnh ghi trực tiếp trong game</size>", true);
            _rec.Recording = true;
            yield return Frames(1.5f);
            Beat("intro");
            yield return Hold();

            foreach (var stop in CareerRoute) yield return CareerChapter(stop.id, stop.branch, stop.scene, stop.spawn);

            _rec.ShowCard("Bạn sẽ chọn nghề nào?", "NihongoLife · thực hiện: Quách Thành Long", false);
            yield return _rec.FadeCard(true);
            Beat("outro");
            yield return Hold();
            yield return Frames(1.5f);
            _rec.Recording = false;
            _rec.Finish();
            UiScreen.Override = null;
            UiScreen.PhoneOverride = null;
            CareerService.EventChance = 0.35f;
        }

        private IEnumerator CareerChapter(string id, string branch, string scene, string spawn)
        {
            var career = CareerService.Catalog.Career(id);
            Assert.NotNull(career, id);
            string top = career.Branch(branch).levels.Last().Title;
            yield return Card(career.Name, career.levels[0].Title + "  →  " + top, Travel(scene, spawn), SceneManager.GetActiveScene().name == scene ? null : scene, 90f);
            Refresh();
            ResetQuests();
            if (DayNightCycle.Active != null) DayNightCycle.Active.SetHour(10f); // every workplace in daylight (Phú Quốc came after dark)
            if (CareerService.State.HasCareer) CareerService.Quit();
            CareerService.State.majorIndustry = string.Empty; // the field of work follows the career, so every salary is full
            Assert.IsNull(CareerService.Choose(id), id);

            Beat(id);
            yield return WorkSlice(career.shiftQuest, 2, id);
            CareerUI.Open(CareerUI.Tab.Overview);
            yield return Hold();
            CareerUI.Close();

            Beat(id + "_up");
            for (int guard = 0; CareerService.State.level < career.MaxLevel; guard++)
            {
                Assert.Less(guard, 8, id + ": promotions");
                CareerService.State.performance = 100f;
                Assert.IsTrue(CareerService.TryOfferPromotion(), id + ": " + CareerService.PromotionBlocker());
                if (JobQuizCard.IsOpen)
                {
                    yield return Frames(2.2f);
                    int pick = IndexOf(c => c.Id == branch);
                    if (pick < 0) pick = IndexOf(c => c.Correct);
                    Assert.GreaterOrEqual(pick, 0, id + ": an answer to give");
                    JobQuizCard.Answer(pick);
                    for (float t = 0f; t < 3f && JobQuizCard.IsOpen; t += 1f / 30f) yield return null;
                }
                yield return Frames(1.1f);
            }
            Assert.AreEqual(branch, CareerService.State.branchId, id + ": the branch the narration names");
            yield return Hold();

            Beat(id + "_top");
            CareerUI.Open(CareerUI.Tab.Path);
            yield return Hold();
            CareerUI.Close();
            ResetQuests();
        }

        /// <summary>The way to a workplace from wherever the player stands: zone to zone, into a zone, or back to the town.</summary>
        private System.Action Travel(string scene, string spawn)
        {
            string here = SceneManager.GetActiveScene().name;
            if (here == scene) return null;
            if (scene == WorldLocationCatalog.CityScene) return () => _flow.ExitZone(scene, spawn);
            if (here == WorldLocationCatalog.CityScene) return () => _flow.EnterZone(scene, spawn, scene);
            return () => _flow.TransferZone(scene, spawn, scene);
        }

        /// <summary>The first steps of the real shift, the work a newcomer does; a workplace without stations shows its boss.</summary>
        private IEnumerator WorkSlice(string quest, int steps, string careerId)
        {
            bool worked = false;
            if (!string.IsNullOrEmpty(quest) && QuestService.Accept(quest) == null)
            {
                yield return Frames(0.4f);
                for (int step = 0; step < steps && !QuestService.ReadyToReport(quest); step++)
                {
                    var station = Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).Where(s => s.QuestId == quest && s.Kind != JobStationKind.Board).OrderBy(s => s.Kind).FirstOrDefault(s => s.IsInteractionAvailable);
                    if (station == null) break;
                    yield return StandBeside(station.transform, 1.1f);
                    worked = true;
                    yield return InteractStation(station);
                    if (JobQuizCard.IsOpen) yield return AnswerCard();
                    else yield return WaitWork();
                }
            }
            if (!worked)
            {
                // No job stations here: the instrument of the job (music shop), else the nearest person, the camera framing both.
                var kind = careerId switch { "musician" => (NihongoLife.Music.MusicSpotKind?)NihongoLife.Music.MusicSpotKind.Piano, "producer" => NihongoLife.Music.MusicSpotKind.DjBooth, "painter" => NihongoLife.Music.MusicSpotKind.Easel, _ => null };
                Transform spot = kind == null ? null : Object.FindObjectsByType<NihongoLife.Music.MusicSpot>(FindObjectsSortMode.None).Where(m => m.Kind == kind).Select(m => m.transform).FirstOrDefault();
                if (spot == null) spot = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).OrderBy(n => (n.transform.position - _player.transform.position).sqrMagnitude).Select(n => n.transform).FirstOrDefault();
                if (spot != null) yield return StandBeside(spot, 2.2f);
            }
            yield return Frames(0.6f);
        }
    }
}
