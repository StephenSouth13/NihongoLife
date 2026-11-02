using System.Collections;
using System.Linq;
using NihongoLife.Clinic;
using NihongoLife.Core;
using NihongoLife.Fashion;
using NihongoLife.Health;
using NihongoLife.Interaction;
using NihongoLife.Island;
using NihongoLife.MiniGames;
using NihongoLife.Music;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.Data;
using NihongoLife.UI;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The full product tour with narration: every part of the game in one take, through the real game logic. The chapters
    /// run in order; NL_TOUR_FROM starts at a later one while a chapter is being fixed. Beat ids match Tools/video/tour_lines.tsv.
    /// Explicit: -testFilter Tour_RecordFrames, with NL_DEMO_DIR (frames) and NL_DEMO_VO (voice lengths).
    /// </summary>
    public partial class DemoRecordingTests
    {
        private static int TourFrom => int.TryParse(System.Environment.GetEnvironmentVariable("NL_TOUR_FROM"), out int n) ? n : 0;

        [UnityTest, Explicit, Timeout(7200000)]
        public IEnumerator Tour_RecordFrames()
        {
            LoadVoiceDurations();
            if (System.IO.Directory.Exists(FrameFolder)) System.IO.Directory.Delete(FrameFolder, true);
            System.IO.Directory.CreateDirectory(FrameFolder);
            // The menu and the HUD lay out for a computer, not for the small batch window.
            UiScreen.Override = new Vector2Int(1920, 1080);
            UiScreen.PhoneOverride = false;
            Time.captureFramerate = 30;
            TimedAction.SpeedScale = 1f;
            CursorDirector.AssumeFocusForTests = true;
            ControlSettings.Scheme = ControlScheme.MouseLook;

            SceneManager.LoadSceneAsync("01_MainMenu");
            for (float t = 0f; t < 60f && SceneManager.GetActiveScene().name != "01_MainMenu"; t += Time.unscaledDeltaTime) yield return null;
            for (int i = 0; i < 90; i++) yield return null;
            _rec = new GameObject("DemoRecorder").AddComponent<DemoRecorder>();
            _rec.Init(FrameFolder);
            _rec.ShowCard("NihongoLife", "Sống ở Nhật, học tiếng Nhật\n<size=70%>Đồ án Capstone 2026 · Hình ảnh ghi trực tiếp trong game</size>", true);
            _rec.Recording = true;
            yield return Frames(1.5f);
            Beat("intro");
            yield return Hold();
            yield return _rec.FadeCard(false);

            if (TourFrom <= 0) yield return TourMenu();
            yield return Card("Thị trấn Hibari", "Cuộc sống bắt đầu từ đây", () => SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene), WorldLocationCatalog.CityScene);
            yield return Prepare();
            if (TourFrom <= 1) yield return TourTown();
            if (TourFrom <= 2) yield return TourKonbini();
            if (TourFrom <= 3) yield return TourFashion();
            if (TourFrom <= 4) yield return TourHealth();
            if (TourFrom <= 5) yield return TourSushi();
            if (TourFrom <= 6) yield return TourSchool();
            if (TourFrom <= 7) yield return TourArcade();
            if (TourFrom <= 8) yield return TourMusic();
            if (TourFrom <= 9) yield return TourIsland();
            if (TourFrom <= 10) yield return TourPhuQuoc();
            if (TourFrom <= 11) yield return TourCareer();
            if (TourFrom <= 12) yield return TourOnline();

            _rec.ShowCard("Cảm ơn bạn đã xem", "NihongoLife · Quách Thành Long · Nguyễn Lê Gia Huy · Lương Nguyễn Tấn Dũng", false);
            yield return _rec.FadeCard(true);
            Beat("outro");
            yield return Hold();
            yield return Frames(1.5f);
            _rec.Recording = false;
            _rec.Finish();
            UiScreen.Override = null;
            UiScreen.PhoneOverride = null;
        }

        private IEnumerator Prepare()
        {
            CloseDialogues();
            var bag = PlayerInventory.Instance;
            ResetQuests();
            if (bag.Yen < 90000) bag.AddYen(90000 - bag.Yen);
            var status = PlayerStatus.Instance;
            status.DebugSetNeeds(health: 100f, energy: 100f, hunger: 90f, thirst: 90f, sleepiness: 5f);
            yield return null;
        }

        private IEnumerator Zone(string title, string subtitle, string scene, string spawn)
        {
            yield return Card(title, subtitle, () => _flow.TransferZone(scene, spawn, scene), scene);
            Refresh();
        }

        private static void Refresh()
        {
            if (PlayerStatus.Instance != null) PlayerStatus.Instance.DebugSetNeeds(health: 100f, energy: 100f, hunger: 90f, thirst: 90f, sleepiness: 5f);
        }

        /// <summary>Stands on free floor beside a spot, in sight of it, facing it (behind a counter or a wall will not do).</summary>
        private IEnumerator StandBeside(Transform target, float distance)
        {
            Vector3 centre = target.position;
            centre.y = _player.transform.position.y + 1f;
            Vector3 chosen = _player.transform.position;
            foreach (var direction in new[] { target.forward, -target.forward, target.right, -target.right, (Vector3.forward + Vector3.right).normalized, (Vector3.back + Vector3.left).normalized })
            {
                Vector3 flat = new Vector3(direction.x, 0f, direction.z).normalized;
                Vector3 spot = centre + flat * distance;
                if (Physics.CheckCapsule(spot + Vector3.down * 0.3f, spot + Vector3.up * 0.5f, 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(spot, centre, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (!Physics.Raycast(spot, Vector3.down, out var floor, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                chosen = floor.point + Vector3.up * 0.08f;
                break;
            }
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            Vector3 look = target.position - chosen;
            look.y = 0f;
            _player.transform.SetPositionAndRotation(chosen, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : _player.transform.rotation);
            body.enabled = true;
            Physics.SyncTransforms();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null)
            {
                var (yaw, dist) = ClearOrbit(_player.transform.eulerAngles.y, 24f);
                camera.SetOrbit(yaw, 24f, dist);
            }
            yield return Frames(0.5f);
        }

        /// <summary>Answers the card on screen after a moment of reading.</summary>
        private IEnumerator AnswerCard(float read = 2.2f)
        {
            yield return Frames(read);
            int right = JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct);
            Assert.GreaterOrEqual(right, 0, "The card has a right answer");
            JobQuizCard.Answer(right);
            for (float t = 0f; t < 3f && JobQuizCard.IsOpen; t += 1f / 30f) yield return null;
            yield return Frames(0.5f);
        }

        /// <summary>Works a whole shift: each step of the quest at its own station.</summary>
        private IEnumerator RunShift(string quest)
        {
            Assert.IsNull(QuestService.Accept(quest), quest);
            yield return Frames(0.4f);
            for (int step = 0; step < 24 && !QuestService.ReadyToReport(quest); step++)
            {
                var station = Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).Where(s => s.QuestId == quest && s.Kind != JobStationKind.Board).OrderBy(s => s.Kind).FirstOrDefault(s => s.IsInteractionAvailable);
                Assert.NotNull(station, quest + ": a station is free at step " + step);
                yield return StandBeside(station.transform, 1.1f);
                yield return InteractStation(station);
                if (JobQuizCard.IsOpen) yield return AnswerCard();
                else yield return WaitWork();
            }
            Assert.IsTrue(QuestService.ReadyToReport(quest), quest + " finished");
        }

        private IEnumerator TourMenu()
        {
            Beat("menu");
            foreach (var language in new[] { GameLanguage.English, GameLanguage.Japanese, GameLanguage.Russian, GameLanguage.Vietnamese })
            {
                Loc.Set(language);
                yield return Frames(1.5f);
            }
            yield return Hold();
            Beat("mode");
            int mode = TouchControlPrefs.Mode;
            yield return Click("ControlsButton");
            yield return Frames(1.6f);
            yield return Click("ControlsButton");
            yield return Frames(1.6f);
            TouchControlPrefs.Mode = mode;
            yield return Hold();
        }

        private IEnumerator TourTown()
        {
            Beat("town");
            yield return OrbitAround(9f);
            yield return Hold();
            Beat("dock");
            yield return Click("ActionDrawerToggle");
            yield return Frames(3f);
            yield return Click("ActionDrawerToggle");
            yield return Hold();
            Beat("toggles");
            yield return Click("ToggleQuest");
            yield return Frames(1f);
            yield return Click("ToggleStatus");
            yield return Frames(2f);
            yield return Click("ToggleStatus");
            yield return Frames(1f);
            yield return Click("ToggleQuest");
            yield return Hold();
            Beat("map");
            yield return Press(GameInputId.Map, "M");
            yield return Frames(4f);
            yield return Hold();
            yield return Press(GameInputId.Map, "M");
        }

        private IEnumerator TourKonbini()
        {
            yield return Card("Làm thêm ở Hibari Mart", "Mỗi ca là một bài tiếng Nhật");
            Beat("konbini");
            var ito = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_cashier");
            yield return StandBeside(ito.transform, 2.2f);
            yield return Hold();
            yield return RunShift("job_konbini_shift");
            Beat("wage");
            ito.Interact(_player.gameObject);
            yield return Frames(0.5f);
            CloseDialogues();
            yield return Hold();
        }

        private IEnumerator TourFashion()
        {
            yield return Zone("Hibari Mode", "Mua sắm và ngành thời trang", WorldLocationCatalog.FashionStoreScene, WorldLocationCatalog.FashionStoreEntrance);
            Beat("fashion");
            yield return OrbitAround(9f);
            yield return Hold();
            Beat("tryon");
            FashionShopUI.Open("street");
            foreach (var id in new[] { "street_neon", "summer_beach", "winter_camel" })
            {
                FashionShopUI.Pick(id);
                yield return Frames(2.6f);
            }
            yield return Hold();
            Beat("buy");
            FashionShopUI.Open("office");
            FashionShopUI.Pick("office_navy");
            yield return Frames(2f);
            FashionShopUI.Purchase("office_navy");
            yield return Frames(2f);
            yield return Hold();
            FashionShopUI.Close();
            Beat("fashionjob");
            yield return RunShift("job_fashion_shift");
            Object.FindFirstObjectByType<JobGiver>().Interact(_player.gameObject);
            yield return Hold();
        }

        private IEnumerator TourHealth()
        {
            yield return Zone("Sức khoẻ", "Bệnh như ngoài đời, chữa như ngoài đời", WorldLocationCatalog.ClinicScene, WorldLocationCatalog.ClinicEntrance);
            PlayerIllness.CureAll();
            PlayerIllness.Catch("heatstroke");
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            camera.SetOrbit(_player.transform.eulerAngles.y + 160f, 8f, 3f);
            Beat("heat");
            for (int i = 0; i < 3; i++)
            {
                KidExpression.On(_player.gameObject).PlayGesture("ill_heat");
                SymptomBubble.Show(_player.transform, "あつい… くらくらする…", new Color(0.95f, 0.5f, 0.2f));
                yield return Frames(3.1f);
            }
            yield return Hold();
            Beat("handbook");
            HandbookUI.Open(HandbookUI.Chapter.Health);
            yield return Hold();
            HandbookUI.Toggle();
            Beat("clinic");
            Assert.IsNull(QuestService.Accept(ClinicVisit.FirstVisitQuest), "First-visit task");
            var spots = Object.FindObjectsByType<ClinicSpot>(FindObjectsSortMode.None);
            yield return StandAt(spots.First(s => s.Kind == ClinicSpotKind.Reception).transform, 1.2f);
            ClinicVisit.Use(ClinicSpotKind.Reception);
            yield return AnswerCard();
            yield return AnswerCard();
            yield return StandAt(spots.First(s => s.Kind == ClinicSpotKind.Doctor).transform, 1.2f);
            ClinicVisit.Use(ClinicSpotKind.Doctor);
            yield return AnswerCard();
            yield return Hold();
            Beat("medicine");
            yield return StandAt(spots.First(s => s.Kind == ClinicSpotKind.Reception).transform, 1.2f);
            ClinicVisit.Use(ClinicSpotKind.Reception);
            yield return AnswerCard();
            yield return StandAt(spots.First(s => s.Kind == ClinicSpotKind.Pharmacy).transform, 1.2f);
            ClinicVisit.Use(ClinicSpotKind.Pharmacy);
            yield return AnswerCard();
            PlayerIllness.CureAll();
            yield return Hold();
        }

        private IEnumerator TourSushi()
        {
            yield return Zone("Sushi Hibari", "Gọi món, ngồi xuống, ăn ngon", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance);
            Beat("sushi");
            yield return OrbitAround(6f);
            var table = Object.FindObjectsByType<RestaurantTable>(FindObjectsSortMode.None).First(t => t.name.EndsWith("_A"));
            var menu = Resources.Load<RestaurantMenuDefinition>("Restaurants/menu_sushi_hibari");
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.SetPositionAndRotation(table.transform.position + new Vector3(0.9f, 0.1f, -2.2f), Quaternion.identity);
            body.enabled = true;
            Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>().SetOrbit(_player.transform.eulerAngles.y - 70f, 24f, 3.2f);
            yield return Frames(0.5f);
            Assert.IsTrue(table.TryPlaceOrder(menu, new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>> { new System.Collections.Generic.KeyValuePair<string, int>(menu.dishes[0].id, 2) }, out string error), error);
            for (float t = 0f; t < 15f && table.State != RestaurantTable.ServiceState.Served; t += 1f / 30f) yield return null;
            yield return Hold();
            Beat("eat");
            table.Interact(_player.gameObject);
            yield return Frames(1.2f);
            Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>().SetOrbit(_player.transform.eulerAngles.y - 70f, 22f, 2.4f);
            yield return Frames(5.8f);
            for (float t = 0f; t < 15f && table.State != RestaurantTable.ServiceState.ReadyToPay; t += 1f / 30f) yield return null;
            table.Interact(_player.gameObject);
            yield return Hold();
        }

        private IEnumerator TourSchool()
        {
            yield return Zone("Trường Nhật ngữ Hibari", "Học bài, luyện thi, xem video", WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance);
            Beat("school");
            yield return OrbitAround(6f);
            yield return Hold();
            Beat("courses");
            var hub = Object.FindFirstObjectByType<StudyHubPopup>(FindObjectsInactive.Include);
            hub.Show();
            yield return Frames(2.5f);
            hub.OpenVideos();
            var library = Object.FindFirstObjectByType<VideoCoursesUI>(FindObjectsInactive.Include);
            _rec.Recording = false;
            for (float t = 0f; t < 3f; t += Time.unscaledDeltaTime) yield return null; // the covers come from the network
            _rec.Recording = true;
            yield return Frames(2.5f);
            library.OpenCourse(NihongoLife.StudyModes.Video.VideoCourseCatalog.File.courses.First(c => c.id == "russian"));
            _rec.Recording = false;
            for (float t = 0f; t < 2f; t += Time.unscaledDeltaTime) yield return null;
            _rec.Recording = true;
            yield return Hold();
            library.Hide();
            Beat("exam");
            var exams = Object.FindFirstObjectByType<ExamCenterPopup>(FindObjectsInactive.Include);
            exams.Show();
            yield return Hold();
            exams.Hide();
        }

        private IEnumerator TourArcade()
        {
            yield return Zone("Game Center Hibari", "Chơi để nhớ chữ", WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.GameCenterEntrance);
            Beat("arcade");
            yield return OrbitAround(6f);
            yield return Hold();
            var launchers = Object.FindObjectsByType<MiniGameLauncher>(FindObjectsSortMode.None);
            var controller = MiniGameController.GetOrCreate();

            Beat("hoop");
            var launcher = launchers.First(l => l.Definition != null && l.Definition.id == "hoop_shot");
            Assert.IsTrue(controller.Launch(launcher, _player.gameObject));
            var hoop = (HoopShotGame)controller.CurrentGame;
            yield return null;
            hoop.StartRound(launcher.Definition.contentSets.FirstOrDefault(s => s != null && s.pairs.Count >= 4 && s.AvailableInCurrentLanguage));
            yield return null;
            hoop.SkipCountdown();
            yield return Frames(1f);
            for (int i = 0; i < 4; i++)
            {
                hoop.Throw(hoop.PerfectThrow());
                for (float t = 0f; t < 4f && hoop.Flying; t += 1f / 30f) yield return null;
                yield return Frames(0.6f);
            }
            yield return Hold();
            hoop.EndRound();
            yield return Frames(1.6f);
            controller.Close();
            yield return Frames(0.4f);

            Beat("ufo");
            launcher = launchers.First(l => l.Definition != null && l.Definition.id == "ufo_catcher");
            Assert.IsTrue(controller.Launch(launcher, _player.gameObject));
            var ufo = (UfoCatcherGame)controller.CurrentGame;
            yield return null;
            ufo.StartRound(launcher.Definition.contentSets.FirstOrDefault(s => s != null && s.pairs.Count >= 4 && s.AvailableInCurrentLanguage));
            yield return null;
            ufo.SkipCountdown();
            yield return Frames(1f);
            for (int i = 0; i < 3; i++)
            {
                ufo.AimAt(ufo.Target);
                yield return Frames(1f);
                ufo.DropClaw();
                for (float t = 0f; t < 4f && ufo.Dropping; t += 1f / 30f) { ufo.AimAt(ufo.Target); yield return null; }
                yield return Frames(0.6f);
            }
            yield return Hold();
            ufo.EndRound();
            yield return Frames(1.6f);
            controller.Close();
            yield return Frames(0.4f);
        }

        private IEnumerator TourMusic()
        {
            yield return Zone("Hibiki Music & Art", "Âm nhạc và hội hoạ", WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.MusicShopEntrance);
            Beat("music");
            yield return OrbitAround(5f);
            var piano = Object.FindObjectsByType<MusicSpot>(FindObjectsSortMode.None).First(s => s.Kind == MusicSpotKind.Piano);
            piano.Interact(_player.gameObject);
            yield return Frames(1f);
            MusicStudioUI.AutoPlay("ode-to-joy");
            yield return Frames(6f);
            yield return Hold();
            MusicStudioUI.Close();
            Beat("gallery");
            var art = Object.FindObjectsByType<ArtworkSpot>(FindObjectsSortMode.None).First();
            yield return StandAt(art.transform, 2f);
            yield return Hold();
        }

        private IEnumerator TourIsland()
        {
            yield return Zone("Đảo Xanh", "Trang trại, câu cá, bơi biển", WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation);
            Beat("island");
            yield return OrbitAround(7f);
            yield return Hold();

            Beat("fishing");
            var bag = PlayerInventory.Instance;
            var catalog = IslandCatalog.Load();
            if (!bag.HasItem(IslandFishing.RodId)) bag.AddItem(IslandFishing.RodId, "釣り竿", "Rod", 0);
            var spot = Object.FindFirstObjectByType<FishingSpot>();
            IslandFishing.BiteDelayOverride = 1.5f;
            IslandFishing.ForcedFishId = "fish_same";
            yield return StandAt(spot.transform, 0.2f);
            yield return Press(GameInputId.Interact, "F");
            for (float t = 0f; t < 10f && IslandFishing.Current != IslandFishing.Phase.Bite; t += 1f / 30f) yield return null;
            yield return Press(GameInputId.Interact, "F");
            for (float t = 0f; t < 8f && IslandFishing.Current != IslandFishing.Phase.Showing; t += 1f / 30f) yield return null;
            yield return Frames(3f);
            yield return Hold();
            for (float t = 0f; t < 8f && IslandFishing.Active; t += 1f / 30f) yield return null;
            IslandFishing.BiteDelayOverride = null;
            IslandFishing.ForcedFishId = null;

            Beat("swim");
            var swim = _player.GetComponent<PlayerSwim>();
            var body = _player.GetComponent<CharacterController>();
            var water = WaterSurface.At(_player.transform.position);
            Vector3 outward = (_player.transform.position - new Vector3(-1200f, _player.transform.position.y, 0f)).normalized;
            Vector3 probe = _player.transform.position;
            for (int i = 0; i < 700 && Physics.Raycast(probe + Vector3.up * 4f, Vector3.down, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore); i++) probe += outward;
            Vector3 sea = probe + outward * 26f;
            sea.y = water.Y + 0.2f;
            body.enabled = false;
            _player.transform.position = sea;
            body.enabled = true;
            yield return Frames(1.5f);
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            camera.SetOrbit(Quaternion.LookRotation(-outward).eulerAngles.y, 22f, 6f);
            for (float t = 0f; t < 18f && swim.Swimming; t += 1f / 30f)
            {
                _player.SetClickDestination(probe - outward * 30f);
                yield return null;
            }
            yield return Hold();
        }

        private IEnumerator TourPhuQuoc()
        {
            yield return Zone("Đảo Phú Quốc", "Bản đồ thật, đồ ăn thật", WorldLocationCatalog.PhuQuocIslandScene, WorldLocationCatalog.PhuQuocArrival);
            Beat("phuquoc");
            yield return OrbitAround(10f);
            yield return Hold();
        }

        private IEnumerator TourCareer()
        {
            yield return Card("Sự nghiệp và tiền bạc", "Kiếm khó, tiêu phải tính", () => _flow.ExitZone(WorldLocationCatalog.CityScene, WorldLocationCatalog.CityFashionStoreReturn), WorldLocationCatalog.CityScene);
            CareerService.Bypass = true;
            Beat("career");
            CareerUI.Open(CareerUI.Tab.Careers);
            yield return Hold();
            CareerUI.Close();
            Beat("bank");
            BankUI.Open();
            yield return Hold();
            BankUI.Close();
            CareerService.Bypass = false;
        }

        private IEnumerator TourOnline()
        {
            yield return Card("Chơi cùng bạn bè", "Gặp nhau, nhắn tin, luyện nói");
            Beat("online");
            OnlineAccess.Bypass = true;
            bool live = GameServices.TryGet(out IOnlineWorldService world) && world is SupabaseOnlineWorldService;
            if (live) yield return TourBot();
            yield return Hold();
            OnlineAccess.Bypass = false;
        }

        private IEnumerator TourBot()
        {
            var db = Resources.Load<GameControlDatabase>("Control/NihongoLifeControlDatabase");
            string key = db.ResolveSupabaseAnonKey();
            var client = new GameObject("TourBot").AddComponent<SupabaseRealtimeClient>();
            client.InitializeRaw(db.ResolveSupabaseProjectUrl().TrimEnd('/').Replace("https://", "wss://") + "/realtime/v1/websocket?apikey=" + key + "&vsn=1.0.0", key);
            var bot = client.gameObject.AddComponent<SupabaseOnlineWorldService>();
            bot.InitializeWith(client);
            bot.UseAuthIdentity = false;
            bot.LocalCharacterId = "kid_mio";
            bot.ConnectLocalPlayer("tour_" + System.Guid.NewGuid().ToString("N").Substring(0, 8), "Aki");
            string scene = SceneManager.GetActiveScene().name;
            var manager = Object.FindFirstObjectByType<RemotePlayerManager>();
            Vector3 spot = _player.transform.position + _player.transform.forward * 2.4f;
            for (float t = 0f; t < 15f && !manager.Avatars.Values.Any(a => a.DisplayName == "Aki"); t += 1f / 30f)
            {
                bot.UpdateLocalPlayerPose(scene, spot, Quaternion.LookRotation(-_player.transform.forward));
                yield return null;
            }
            bot.SetLocalOutfit("street_neon");
            for (int i = 0; i < 3; i++)
            {
                bot.SendEmote(i);
                for (float t = 0f; t < 3f; t += 1f / 30f)
                {
                    bot.UpdateLocalPlayerPose(scene, spot, Quaternion.LookRotation(-_player.transform.forward));
                    yield return null;
                }
            }
            Object.Destroy(client.gameObject);
        }
    }
}
