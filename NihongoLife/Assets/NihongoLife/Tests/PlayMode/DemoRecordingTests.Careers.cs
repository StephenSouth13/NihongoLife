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
    /// career logic: a slice of the real shift, a whole shift's task cards answered on screen, then the climb to the top as a
    /// counted fast-forward — every shift and task card really done (CareerService.ShiftDone, CareerTasks), the ticker shows
    /// the counts (more than 300 tasks), each milestone story plays (the first one at reading speed) before its Japanese
    /// interview, the branch at level 4. Beat ids and the branch followed match Tools/video/career_voice.py.
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
            CareerTasks.Enabled = true;
            CareerStories.Enabled = true;

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
            CareerTasks.Enabled = false;
            CareerStories.Enabled = false;
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
            yield return Hold();

            // A whole shift's task cards, read and answered on screen; the overview then counts the tasks.
            Beat(id + "_tasks");
            int day = DayNightCycle.Active != null ? DayNightCycle.Active.Day : 0;
            int NextWorkday() { do day++; while (!CareerService.IsWorkDay(career, day)); return day; }
            CareerService.ShiftDone(NextWorkday(), career.workStart + 1f);
            for (int card = 0; card < 4 && JobQuizCard.IsOpen; card++) yield return AnswerCard(1.8f);
            CareerUI.Open(CareerUI.Tab.Overview);
            yield return Hold();
            CareerUI.Close();

            // The climb, fast-forwarded but counted: every shift and card is really done; stories and interviews on the way.
            Beat(id + "_up");
            var shown = default(NihongoLife.Dialogue.DialogueDisplayData);
            void OnShown(NihongoLife.Dialogue.DialogueDisplayData d) => shown = d;
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            dm.OnDialogueUpdated += OnShown;
            bool firstStory = true;
            int toTop = CareerTasks.TasksToTop(career);
            try
            {
                for (int guard = 0; CareerService.State.level < career.MaxLevel; guard++)
                {
                    Assert.Less(guard, 400, id + ": the climb ends");
                    var state = CareerService.State;
                    _rec.Ticker($"» Tua nhanh · việc vẫn tính thật · ca {state.shifts} · {state.tasksDone}/{toTop} việc · bậc {state.level}");
                    var story = CareerStories.Pending(career, state);
                    CareerService.ShiftDone(NextWorkday(), career.workStart + 1f);
                    for (int step = 0; step < 40 && (JobQuizCard.IsOpen || dm.IsOpen); step++)
                    {
                        if (dm.IsOpen)
                        {
                            // The milestone story: the first one of a career at reading speed, the others briefly.
                            _rec.Ticker(null);
                            if (shown.choices != null && shown.choices.Count > 0 && story != null)
                            {
                                yield return Frames(firstStory ? 2.6f : 0.6f);
                                dm.SelectChoice(System.Array.FindIndex(story.choices, c => c.kind == "best"));
                            }
                            else
                            {
                                yield return Frames(firstStory ? 2.2f : 0.35f);
                                dm.ContinueDialogue();
                            }
                            yield return null;
                            if (!dm.IsOpen) firstStory = false;
                            continue;
                        }
                        // Task cards number their answers ("0", "1", "2"); an interview names them (the branch, the Japanese answer).
                        bool interview = !JobQuizCard.CurrentChoices.All(c => int.TryParse(c.Id, out _));
                        if (interview) _rec.Ticker(null);
                        int pick = IndexOf(c => c.Id == branch);
                        if (pick < 0) pick = IndexOf(c => c.Correct);
                        Assert.GreaterOrEqual(pick, 0, id + ": an answer to give");
                        if (interview) yield return Frames(1.4f);
                        JobQuizCard.Answer(pick);
                        yield return null;
                    }
                    yield return null;
                }
            }
            finally { dm.OnDialogueUpdated -= OnShown; }
            _rec.Ticker($"Đỉnh nghề sau {CareerService.State.shifts} ca · {CareerService.State.tasksDone} việc");
            Assert.AreEqual(branch, CareerService.State.branchId, id + ": the branch the narration names");
            Assert.GreaterOrEqual(CareerService.State.tasksDone, 300, id + ": more than 300 tasks to the top");
            yield return Hold();
            _rec.Ticker(null);

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
