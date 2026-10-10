using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Dialogue;
using NihongoLife.Island;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.Save;
using NihongoLife.Shop;
using NihongoLife.UI;
using NihongoLife.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Records the Capstone gameplay demo at a fixed 30 fps (1920×1080 JPG frames) through the real gameplay camera with
    /// the HUD visible: city HUD and controls → konbini shift → spend the wage → train to Midori → farm shift with Hana →
    /// sell, JA/EN → train back to Hibari. Every step is the real game logic (the same flow LifeLoopPlayModeTests asserts);
    /// a pointer and key-cap overlay show what the player presses. Each narration beat is held at least as long as its
    /// voice line (NL_DEMO_VO: "id\tseconds" per line), and timeline.tsv maps beats to frames for the audio mix.
    /// Explicit: run with -testFilter Demo_RecordFrames; NL_DEMO_DIR picks the frame folder.
    /// </summary>
    public class DemoRecordingTests
    {
        private static string FrameFolder => System.Environment.GetEnvironmentVariable("NL_DEMO_DIR") is { Length: > 0 } dir ? dir : Path.Combine(Path.GetTempPath(), "NihongoLifeDemoFrames");

        private DemoRecorder _rec;
        private PlayerController _player;
        private SceneFlowController _flow;
        private readonly Dictionary<string, float> _vo = new();
        private int _beatStart;
        private string _beat;

        [TearDown]
        public void TearDown()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            CursorDirector.AssumeFocusForTests = false;
            TimedAction.SpeedScale = 1f;
            IslandLanguage.Target = TargetLanguage.Japanese;
            if (_rec != null) Object.Destroy(_rec.gameObject);
        }

        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Demo_RecordFrames()
        {
            LoadVoiceDurations();
            if (Directory.Exists(FrameFolder)) Directory.Delete(FrameFolder, true);
            Directory.CreateDirectory(FrameFolder);
            Time.captureFramerate = 30;
            TimedAction.SpeedScale = 1f;

            // In batchmode Unity 6 can leave the AsyncOperation yield instruction pending even
            // after the scene has activated (all scene Start methods have already run). Polling
            // the observable scene state keeps the recorder deterministic and gives failures a
            // bounded diagnostic instead of hanging the editor indefinitely.
            SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            float loadStartedAt = Time.realtimeSinceStartup;
            while (SceneManager.GetActiveScene().name != WorldLocationCatalog.CityScene)
            {
                Assert.Less(Time.realtimeSinceStartup - loadStartedAt, 60f, $"Timed out activating {WorldLocationCatalog.CityScene}");
                yield return null;
            }
            yield return WaitZone(WorldLocationCatalog.CityScene, 40f);
            _rec = new GameObject("DemoRecorder").AddComponent<DemoRecorder>();
            _rec.Init(FrameFolder);
            CloseDialogues();
            var bag = PlayerInventory.Instance;
            ResetQuests();
            Repository().GetProgress().island = new IslandRecord();
            IslandState.ClockOffset = System.TimeSpan.Zero;
            foreach (var id in bag.Items.Select(i => i.itemId).ToList())
                if (id.StartsWith("seed_") || id.StartsWith("tool_") || id.StartsWith("train_ticket") || IslandCatalog.Load().Crop(id) != null) bag.RemoveItem(id, bag.GetItemQuantity(id));
            if (bag.Yen > 300) bag.SpendYen(bag.Yen - 300);
            if (bag.Yen < 300) bag.AddYen(300 - bag.Yen);
            CursorDirector.AssumeFocusForTests = true;
            ControlSettings.Scheme = ControlScheme.MouseLook;

            // ── Title ──
            _rec.ShowCard("NihongoLife — Demo gameplay", "Ghi hình trực tiếp trong Unity · Đồ án Capstone 2026\n<size=70%>Lồng tiếng: giọng đọc AI (TTS)</size>", true);
            _rec.Recording = true;
            yield return Frames(1.5f);
            Beat("intro");
            yield return Frames(3f);
            yield return _rec.FadeCard(false);
            yield return OrbitAround(10f);
            yield return Hold();

            // ── 1. HUD & controls ──
            yield return Card("1 · Giao diện & điều khiển", "Thị trấn Hibari");
            Beat("hud");
            HudFeed.Post("Chào mừng tới Hibari! Nhấn N để mở sổ nhiệm vụ.");
            yield return Hold();

            Beat("move");
            yield return Walk(3f, 0f);
            _rec.Key("Ctrl");
            var input = GameInputService.GetOrCreate();
            input.SetMobileButton(GameInputId.FreeCursor, true);
            _rec.ShowPointer(true);
            yield return Frames(2f);
            input.SetMobileButton(GameInputId.FreeCursor, false);
            _rec.ShowPointer(false);
            yield return Hold();

            Beat("settings");
            yield return Press(GameInputId.Settings, "O");
            yield return Frames(0.6f);
            var sens = GameObject.Find("ControlScheme")?.GetComponent<Button>();
            if (sens != null) yield return _rec.PointTo((RectTransform)sens.transform, false);
            yield return HoldMinus(1.2f);
            _rec.Key("Esc");
            UiModalStack.CloseTop();
            yield return Hold();

            Beat("profile");
            yield return Press(GameInputId.Character, "Tab");
            yield return Hold();
            _rec.Key("Esc");
            UiModalStack.CloseTop();
            yield return Frames(0.4f);

            Beat("journal");
            yield return Press(GameInputId.Journal, "N");
            TaskJournalUI.Select("job_konbini_shift");
            yield return HoldMinus(1.6f);
            yield return Click("Accept");
            yield return Hold();
            UiModalStack.CloseTop();
            CursorDirector.AssumeFocusForTests = false;

            // ── 2. Konbini shift ──
            yield return Card("2 · Làm thêm tại Konbini", "Ca làm ở Hibari Mart — lương ¥600", () => { });
            var stations = Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).Where(s => s.QuestId == "job_konbini_shift").ToList();
            JobStation S(string id) => stations.First(s => s.StationId == id);
            if (KonbiniShopUI.GetOrCreate().IsOpen) KonbiniShopUI.GetOrCreate().Close();
            yield return StandAt(S("stock").transform, 1.0f);
            yield return _rec.FadeCard(false);
            Beat("stock");
            yield return InteractStation(S("stock"));
            yield return WaitWork();
            foreach (var shelf in new[] { "shelf_onigiri", "shelf_snacks", "shelf_drinks" })
            {
                if (JobShift.CarryKind != "stock")
                {
                    yield return StandAt(S("stock").transform, 1.0f);
                    yield return InteractStation(S("stock"));
                    yield return WaitWork();
                }
                yield return StandAt(S(shelf).transform, 1.1f);
                yield return InteractStation(S(shelf));
                yield return WaitWork();
                yield return Frames(0.3f);
            }
            yield return Hold();

            Beat("customer");
            yield return StandAt(S("customer_a").transform, 1.3f);
            yield return InteractStation(S("customer_a"));
            yield return Frames(1.4f);
            yield return Click("Choice_" + IndexOf(c => !c.Correct));
            yield return Frames(1.8f);
            yield return HoldMinus(1.2f);
            yield return Click("Choice_" + IndexOf(c => c.Correct));
            yield return Hold();

            Beat("customer2");
            yield return StandAt(S("customer_b").transform, 1.3f);
            yield return InteractStation(S("customer_b"));
            yield return Frames(1.6f);
            yield return Click("Choice_" + IndexOf(c => c.Correct));
            yield return Hold();

            Beat("register");
            yield return StandAt(S("register").transform, 1.2f);
            yield return InteractStation(S("register"));
            yield return WaitWork();
            yield return Frames(1.4f);
            yield return Click("Choice_" + IndexOf(c => c.Correct));
            yield return Hold();

            Beat("paid");
            var ito = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_cashier");
            yield return StandAt(ito.transform, -1.5f);
            _rec.Key("F");
            ito.Interact(_player.gameObject);
            yield return Frames(3f);
            CloseDialogues();
            yield return Hold();

            Beat("spend");
            QuestService.Accept("daily_konbini_meal");
            var onigiri = KonbiniCatalog.Products.First(p => p.section == KonbiniSection.Onigiri);
            var shop = KonbiniShopUI.GetOrCreate();
            KonbiniBasket.Clear();
            shop.OpenSection(KonbiniSection.Onigiri);
            shop.Select(onigiri);
            yield return Frames(1.2f);
            yield return Click("AddToBasket");
            yield return Frames(0.6f);
            shop.OpenCheckout();
            yield return Frames(1.0f);
            yield return Click("Pay");
            yield return Frames(1.0f);
            CloseDialogues();
            yield return Hold();
            if (shop.IsOpen) shop.Close();
            Assert.AreEqual("completed", QuestService.State("job_konbini_shift")?.status, "Konbini shift completed in the demo");

            // ── 3. Station & train ──
            CursorDirector.AssumeFocusForTests = true;
            yield return Card("3 · Ga Hibari → Đảo Midori", "Mua vé · soát vé · lên tàu", () =>
                _flow.EnterZone(WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, "station"), WorldLocationCatalog.StationScene);
            var station = Object.FindFirstObjectByType<StationTravelController>();
            Beat("station");
            yield return Walk(1.2f, 0f);
            _rec.Key("F");
            station.Execute(StationAction.BuyTicket, _player.gameObject);
            yield return Frames(2.4f);
            Assert.IsTrue(station.PurchaseTicket("midori"), "Ticket to Midori");
            yield return Frames(1.5f);
            _rec.Key("F");
            station.Execute(StationAction.PassGate, _player.gameObject);
            yield return Hold();
            _rec.Recording = !station.IsTrainBoarding ? false : true;
            for (float t = 0f; t < 90f && !station.IsTrainBoarding; t += Time.deltaTime) yield return null;
            _rec.Recording = true;
            Beat("train");
            _rec.Key("F");
            station.Execute(StationAction.BoardTrain, _player.gameObject);
            Assert.IsTrue(station.IsOnboard, "Boarded");
            yield return Hold();
            yield return Card("4 · Đảo Midori", "Ca phụ việc nông trại với Hana", () => Time.timeScale = 4f, WorldLocationCatalog.MidoriIslandScene, 90f);
            Time.timeScale = 1f;
            for (float t = 0f; t < 10f && IslandRuntime.Active == null; t += Time.deltaTime) yield return null;

            // ── 4. Midori Island ──
            Beat("island");
            yield return OrbitAround(8f);
            yield return Hold();

            Beat("hana");
            var hana = Object.FindFirstObjectByType<JobGiver>();
            yield return StandAt(hana.transform, -1.4f);
            yield return Press(GameInputId.Interact, "F");
            if (!TaskJournalUI.IsOpen) hana.Interact(_player.gameObject);
            yield return HoldMinus(1.6f);
            yield return Click("Accept");
            yield return Hold();
            UiModalStack.CloseTop();

            // Show the bare-hands case: the starter hoe is put away for this step and handed back right after.
            if (bag.GetItemQuantity("tool_hoe") > 0) bag.RemoveItem("tool_hoe", bag.GetItemQuantity("tool_hoe"));
            if (bag.GetItemQuantity("tool_shovel") > 0) bag.RemoveItem("tool_shovel", bag.GetItemQuantity("tool_shovel"));
            if (bag.GetItemQuantity("tool_watering_can") == 0) IslandEconomy.Give("tool_watering_can", 1);
            var plots = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).OrderBy(p => p.Number).Take(2).ToList();
            foreach (var p in plots) { var r = p.Record; r.tilled = false; r.cropId = null; r.stage = 0; r.watered = false; p.Refresh(true); }

            Beat("hand");
            yield return StandAt(plots[0].transform, 1.9f);
            yield return Press(GameInputId.Interact, "F");
            if (!IslandUI.FarmOpen) IslandUI.OpenFarm(plots[0]);
            yield return Frames(0.5f);
            yield return ClickFarm("Xới bằng tay");
            yield return WaitWork();
            yield return Hold();
            IslandUI.CloseFarm();

            Beat("hoe");
            IslandEconomy.Give("tool_hoe", 1);
            IslandUI.Toast("Đã cầm cuốc (くわ).");
            yield return StandAt(plots[1].transform, 1.9f);
            yield return Press(GameInputId.Interact, "F");
            if (!IslandUI.FarmOpen) IslandUI.OpenFarm(plots[1]);
            yield return Frames(0.5f);
            yield return ClickFarm("Xới đất");
            yield return WaitWork();
            yield return Hold();

            Beat("plant");
            foreach (var plot in plots)
            {
                IslandUI.OpenFarm(plot);
                yield return Frames(0.5f);
                yield return Click("Plant_carrot"); yield return WaitWork();
                yield return ClickFarm("Tưới nước"); yield return WaitWork();
                IslandUI.CloseFarm();
            }
            yield return Hold();

            Beat("grow");
            var crop = plots[0].Crop;
            yield return StandAt(plots[0].transform, 1.6f);
            var cam = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (cam != null) cam.SetOrbit(_player.transform.eulerAngles.y + 35f, 32f, 3.2f);
            for (int stage = 0; stage < 3; stage++)
            {
                yield return Frames(1.4f);
                IslandState.ClockOffset += System.TimeSpan.FromSeconds(crop.secondsPerStage + 1);
                yield return Frames(0.6f);
                if (plots[0].CurrentPhase == FarmPlot.Phase.NeedsWater)
                {
                    IslandUI.OpenFarm(plots[0]);
                    yield return ClickFarm("Tưới nước"); yield return WaitWork();
                    IslandUI.CloseFarm();
                }
            }
            yield return Hold();

            Beat("harvest");
            IslandUI.OpenFarm(plots[0]);
            yield return Frames(0.5f);
            yield return ClickFarm("Thu hoạch"); yield return WaitWork();
            IslandUI.CloseFarm();
            var animal = Object.FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).First(a => a.Def.foods.Contains("carrot"));
            yield return StandAt(animal.transform, 2.4f);
            IslandUI.OpenAnimal(animal);
            yield return Frames(0.8f);
            yield return ClickIn("AnimalCard", "Feed"); yield return WaitWork();
            yield return Hold();
            IslandUI.CloseAnimal();
            Assert.IsTrue(QuestService.ReadyToReport("job_farm_shift"), "Farm shift objectives done");

            Beat("farmpaid");
            yield return StandAt(hana.transform, -1.4f);
            _rec.Key("F");
            hana.Interact(_player.gameObject);
            yield return Frames(2f);
            CloseDialogues();
            if (TaskJournalUI.IsOpen) UiModalStack.CloseTop();
            yield return Hold();
            Assert.AreEqual("completed", QuestService.State("job_farm_shift")?.status, "Farm shift paid");

            Beat("sell");
            yield return StandAt(Object.FindFirstObjectByType<IslandShopCounter>().transform, 1.2f);
            yield return Press(GameInputId.Interact, "F");
            if (!IslandUI.ShopOpen) IslandUI.OpenShop();
            IslandUI.SelectShopTab("sell");
            IslandUI.SelectShopItem("carrot");
            yield return Frames(1.4f);
            yield return ClickIn("IslandShop", "Confirm");
            yield return Hold();

            Beat("english");
            IslandLanguage.Target = TargetLanguage.English;
            IslandUI.SelectShopTab("agriculture");
            yield return Frames(0.3f);
            yield return Hold();
            IslandLanguage.Target = TargetLanguage.Japanese;
            IslandUI.CloseShop();

            Beat("save");
            IslandState.Save();
            yield return Press(GameInputId.Character, "Tab");
            yield return Hold();
            UiModalStack.CloseTop();

            Beat("return");
            yield return StandAt(Object.FindFirstObjectByType<IslandTicketKiosk>().transform, 1.3f);
            _rec.Key("F");
            Object.FindFirstObjectByType<IslandTicketKiosk>().Interact(_player.gameObject);
            yield return Frames(1.6f);
            var door = Object.FindFirstObjectByType<IslandTrainDoor>();
            yield return StandAt(door.transform, 1.4f);
            _rec.Key("F");
            Assert.IsNull(IslandTrainService.Depart(door), "Departing with the bought ticket");
            yield return Frames(IslandTrainService.RideSeconds - 0.5f);
            yield return Hold();
            _rec.Recording = false;
            yield return WaitZone(WorldLocationCatalog.StationScene, 60f);
            _rec.Recording = true;

            Beat("back");
            yield return Frames(1f); // the platform is narrow: a steady shot, no orbit into the pillars
            yield return Hold();

            // ── End ──
            _rec.ShowCard("NihongoLife", "Hội thoại · Làm thêm · Tàu điện · Đảo Midori · Nông trại · Luyện thi · Kana Match\n<size=70%>Đồ án Capstone 2026 · VTC Academy</size>", false);
            yield return _rec.FadeCard(true);
            Beat("outro");
            yield return Hold();
            yield return Frames(1.5f);
            _rec.Recording = false;
            _rec.Finish();
            Debug.Log($"[DemoRecording] {_rec.Frames} frames → {FrameFolder}");
            Assert.Greater(_rec.Frames, 30 * 120, "Expected a few minutes of footage.");
        }

        // ─────────── beats & timing ───────────

        private void LoadVoiceDurations()
        {
            string path = System.Environment.GetEnvironmentVariable("NL_DEMO_VO");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            foreach (var line in File.ReadAllLines(path))
            {
                var parts = line.Split('\t');
                if (parts.Length == 2 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s)) _vo[parts[0]] = s;
            }
        }

        private void Beat(string id)
        {
            _beat = id;
            _beatStart = _rec.Frames;
            _rec.Mark(id);
        }

        /// <summary>Waits until the current beat has lasted its voice line plus a short breath.</summary>
        private IEnumerator Hold() => HoldMinus(-0.6f);

        private IEnumerator HoldMinus(float early)
        {
            float seconds = (_vo.TryGetValue(_beat, out float s) ? s : 5f) - early;
            for (int guard = 0; _rec.Frames - _beatStart < seconds * 30f; guard++)
            {
                Assert.Less(guard, 30 * 120, $"Beat '{_beat}' is not recording frames");
                yield return null;
            }
        }

        private static IEnumerator Frames(float seconds)
        {
            int n = Mathf.RoundToInt(seconds * 30f);
            for (int i = 0; i < n; i++) yield return null;
        }

        /// <summary>Title card between parts; the optional action (a zone change) runs behind the opaque card.</summary>
        private IEnumerator Card(string title, string subtitle, System.Action behind = null, string waitScene = null, float timeout = 60f)
        {
            _rec.ShowCard(title, subtitle, false);
            yield return _rec.FadeCard(true);
            yield return Frames(1.6f);
            if (behind != null)
            {
                behind();
                if (waitScene != null)
                {
                    _rec.Recording = false;
                    yield return WaitZone(waitScene, timeout);
                    CloseDialogues();
                    _rec.Recording = true;
                    yield return Frames(0.6f);
                    yield return _rec.FadeCard(false);
                }
                yield break;
            }
            yield return _rec.FadeCard(false);
        }

        // ─────────── player & camera ───────────

        private IEnumerator Walk(float seconds, float turn)
        {
            var input = GameInputService.GetOrCreate();
            var cam = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            float yaw = _player.transform.eulerAngles.y;
            input.SetMobileMove(Vector2.up);
            for (float t = 0f; t < seconds; t += 1f / 30f)
            {
                if (cam != null && turn != 0f) cam.SetOrbit(yaw + turn * (t / seconds), 18f, 4.6f);
                yield return null;
            }
            input.SetMobileMove(Vector2.zero);
            yield return Frames(0.3f);
        }

        private IEnumerator OrbitAround(float seconds)
        {
            var cam = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            float yaw = _player.transform.eulerAngles.y;
            for (float t = 0f; t < seconds; t += 1f / 30f)
            {
                if (cam != null) cam.SetOrbit(yaw - 35f + 35f * Mathf.SmoothStep(0f, 1f, t / seconds), 20f + 6f * Mathf.Sin(t / seconds * Mathf.PI), 5.2f - 0.6f * (t / seconds));
                yield return null;
            }
        }

        private IEnumerator StandAt(Transform target, float distance)
        {
            Vector3 f = target.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            f.Normalize();
            Vector3 p = target.position - f * distance;
            var facing = Quaternion.LookRotation(distance >= 0f ? f : -f);
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.SetPositionAndRotation(new Vector3(p.x, target.position.y > 0.5f ? target.position.y - 0.9f : target.position.y + 0.1f, p.z), facing);
            body.enabled = true;
            Physics.SyncTransforms();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null)
            {
                var (yaw, dist) = ClearOrbit(facing.eulerAngles.y, 24f);
                camera.SetOrbit(yaw, 24f, dist);
            }
            yield return Frames(0.5f);
        }

        /// <summary>First orbit (behind the player, then turning to the sides, then closer) whose line from the
        /// player's head to the camera hits no wall — indoors (the konbini store room) the default orbit can sit
        /// outside the building.</summary>
        private (float yaw, float distance) ClearOrbit(float facingYaw, float pitch)
        {
            Vector3 head = _player.transform.position + Vector3.up * 1.5f;
            foreach (float dist in new[] { 4.2f, 3.0f, 2.2f })
                foreach (float turn in new[] { 0f, 35f, -35f, 70f, -70f, 110f, -110f })
                {
                    float yaw = facingYaw + turn;
                    Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
                    bool blocked = Physics.RaycastAll(head, dir, dist + 0.4f, ~0, QueryTriggerInteraction.Ignore)
                        .Any(h => !h.collider.transform.IsChildOf(_player.transform));
                    if (!blocked) return (yaw, dist);
                }
            return (facingYaw, 2.2f);
        }

        private IEnumerator Press(GameInputId id, string key)
        {
            _rec.Key(key);
            var input = GameInputService.GetOrCreate();
            yield return new WaitForFixedUpdate();
            input.SetMobileButton(id, true);
            yield return null;
            input.SetMobileButton(id, false);
            yield return Frames(0.4f);
        }

        /// <summary>
        /// Shows the same visible F-key cue as player input while targeting the intended work
        /// station directly. Several konbini shelves overlap the job interaction radius, so a
        /// synthetic global Interact press can nondeterministically open the retail shop instead.
        /// </summary>
        private IEnumerator InteractStation(JobStation station)
        {
            Assert.NotNull(station);
            _rec.Key("F");
            yield return null;
            station.Interact(_player.gameObject);
            yield return Frames(0.4f);
        }

        private static IEnumerator WaitWork()
        {
            yield return null;
            for (float t = 0f; t < 15f && TimedAction.Busy; t += Time.deltaTime) yield return null;
            yield return Frames(0.2f);
        }

        private IEnumerator WaitZone(string scene, float timeout)
        {
            for (float t = 0f; t < timeout; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                _flow = GameServices.TryGet(out SceneFlowController f) ? f : Object.FindFirstObjectByType<SceneFlowController>();
                if (_player != null && _flow != null && !_flow.IsLoading && !StandaloneZoneBootstrap.IsBooting && SceneManager.GetActiveScene().name == scene && t > 2f)
                { yield return Frames(0.8f); yield break; }
                yield return null;
            }
            Assert.Fail($"Timed out waiting for {scene}");
        }

        // ─────────── clicks with a visible pointer ───────────

        private IEnumerator Click(string name)
        {
            yield return null;
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.gameObject.activeInHierarchy && b.interactable);
            Assert.NotNull(button, "Button " + name);
            yield return _rec.PointTo((RectTransform)button.transform, true);
            button.onClick.Invoke();
            yield return Frames(0.3f);
        }

        private IEnumerator ClickIn(string window, string name)
        {
            yield return null;
            var root = GameObject.Find(window);
            Assert.NotNull(root, window);
            var button = root.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == name);
            Assert.NotNull(button, name);
            yield return _rec.PointTo((RectTransform)button.transform, true);
            button.onClick.Invoke();
            yield return Frames(0.3f);
        }

        private IEnumerator ClickFarm(string label)
        {
            yield return null;
            var card = GameObject.Find("FarmCard");
            Assert.NotNull(card, "Farm card open");
            var button = card.GetComponentsInChildren<Button>().FirstOrDefault(b => b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains(label) == true);
            Assert.NotNull(button, $"Button '{label}'");
            yield return _rec.PointTo((RectTransform)button.transform, true);
            button.onClick.Invoke();
            yield return Frames(0.3f);
        }

        // ─────────── game state helpers (as in LifeLoopPlayModeTests) ───────────

        private static IProgressRepository Repository() { GameServices.TryGet(out IProgressRepository r); return r; }

        private static void ResetQuests()
        {
            var progress = Repository().GetProgress();
            progress.quests.Clear();
            progress.trackedQuestId = string.Empty;
            JobShift.Sync("none");
        }

        private static int IndexOf(System.Func<JobQuizCard.Choice, bool> predicate) => JobQuizCard.CurrentChoices.ToList().FindIndex(c => predicate(c));

        private static void CloseDialogues()
        {
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
        }
    }

    /// <summary>
    /// Frame grabber + demo overlay (title cards, key caps, pointer). While recording it renders the active main camera
    /// into a 1920×1080 target with every screen-space canvas drawn in front of it, and writes one JPG per frame.
    /// </summary>
    public sealed class DemoRecorder : MonoBehaviour
    {
        private const int Width = 1920, Height = 1080;
        public bool Recording;
        public int Frames { get; private set; }

        private string _folder;
        private RenderTexture _target;
        private Texture2D _frame;
        private Camera _camera;
        private readonly StringBuilder _timeline = new();
        private Canvas _overlay;
        private CanvasGroup _card;
        private TextMeshProUGUI _cardTitle, _cardSub;
        private Image _cardBack;
        private RectTransform _pointer;
        private CanvasGroup _pointerGroup;
        private RectTransform _key;
        private CanvasGroup _keyGroup;
        private TextMeshProUGUI _keyText;
        private float _keyUntil;

        public void Init(string folder)
        {
            _folder = folder;
            DontDestroyOnLoad(gameObject);
            _target = new RenderTexture(Width, Height, 24);
            _frame = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            BuildOverlay();
            StartCoroutine(Grab());
        }

        public void Mark(string id) => _timeline.AppendLine($"{Frames}\t{id}");

        public void Finish()
        {
            File.WriteAllText(Path.Combine(_folder, "timeline.tsv"), _timeline.ToString());
            if (_camera != null) _camera.targetTexture = null;
        }

        private void OnDestroy()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();
        }

        private void LateUpdate()
        {
            if (_key != null) _keyGroup.alpha = Mathf.MoveTowards(_keyGroup.alpha, Time.time < _keyUntil ? 1f : 0f, Time.deltaTime * 6f);
            Attach();
        }

        /// <summary>Keeps the current main camera rendering into the capture target with every screen-space canvas in
        /// front of it (so canvas scaling uses the 1920×1080 target, as in LifeLoopPlayModeTests captures).</summary>
        private void Attach()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (cam != _camera)
            {
                if (_camera != null) _camera.targetTexture = null;
                _camera = cam;
            }
            _camera.targetTexture = _target;
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                .OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = cam.nearClipPlane + 0.05f + 0.002f * (canvases.Length - i);
            }
        }

        /// <summary>Batchmode never resumes WaitForEndOfFrame, so (like TrailerRecordingTests) each frame renders the
        /// camera explicitly and reads the target back.</summary>
        private IEnumerator Grab()
        {
            while (true)
            {
                yield return null;
                if (!Recording) continue;
                Attach();
                if (_camera == null) continue;
                Canvas.ForceUpdateCanvases();
                _camera.Render();
                var active = RenderTexture.active;
                RenderTexture.active = _target;
                _frame.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                _frame.Apply();
                RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(_folder, $"f{Frames:00000}.jpg"), _frame.EncodeToJPG(90));
                Frames++;
            }
        }

        // ─────────── overlay ───────────

        private void BuildOverlay()
        {
            var font = NLUi.ResolveFont();
            _overlay = NLUi.CreateCanvas("DemoOverlay", 5000, transform);
            _overlay.GetComponent<GraphicRaycaster>().enabled = false;
            var root = (RectTransform)_overlay.transform;

            var card = new GameObject("DemoCard", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            card.transform.SetParent(root, false);
            NLUi.Stretch((RectTransform)card.transform);
            _cardBack = card.GetComponent<Image>();
            _cardBack.raycastTarget = false;
            _card = card.GetComponent<CanvasGroup>();
            _card.alpha = 0f;
            _cardTitle = NLUi.Label(card.transform, "Title", "", 64, Color.white, font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Anchor(_cardTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1500f, 120f));
            _cardSub = NLUi.Label(card.transform, "Sub", "", 30, NLUi.Soft, font, FontStyles.Normal, TextAlignmentOptions.Top);
            NLUi.Anchor(_cardSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1500f, 160f));

            var key = NLUi.Panel(root, "DemoKey", new Color(0.98f, 0.98f, 0.98f, 0.95f));
            NLUi.Anchor(key, new Vector2(0f, 0.5f), new Vector2(110f, 40f), new Vector2(150f, 70f));
            _key = key;
            _keyGroup = key.gameObject.AddComponent<CanvasGroup>();
            _keyGroup.alpha = 0f;
            _keyText = NLUi.Label(key, "Text", "", 30, new Color(0.1f, 0.12f, 0.16f), font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Stretch(_keyText.rectTransform);

            var pointer = new GameObject("DemoPointer", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            pointer.transform.SetParent(root, false);
            _pointer = (RectTransform)pointer.transform;
            _pointer.anchorMin = _pointer.anchorMax = new Vector2(0.5f, 0.5f);
            _pointer.sizeDelta = new Vector2(34f, 34f);
            var img = pointer.GetComponent<Image>();
            img.sprite = Ring();
            img.raycastTarget = false;
            _pointerGroup = pointer.GetComponent<CanvasGroup>();
            _pointerGroup.alpha = 0f;
            _pointer.anchoredPosition = new Vector2(0f, -120f);
        }

        private static Sprite Ring()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                    Color c = d < 22f ? new Color(1f, 1f, 1f, 0.55f) : d < 27f ? new Color(1f, 0.78f, 0.25f, 1f) : d < 30f ? new Color(0f, 0f, 0f, 0.6f) : Color.clear;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        public void ShowCard(string title, string subtitle, bool opaqueNow)
        {
            _cardTitle.text = title;
            _cardSub.text = subtitle;
            _cardBack.color = new Color(0.04f, 0.055f, 0.08f, 1f);
            if (opaqueNow) _card.alpha = 1f;
        }

        public IEnumerator FadeCard(bool show)
        {
            float from = _card.alpha, to = show ? 1f : 0f;
            for (int i = 1; i <= 12; i++) { _card.alpha = Mathf.Lerp(from, to, i / 12f); yield return null; }
        }

        public void Key(string label)
        {
            _keyText.text = label;
            NLUi.Anchor(_key, new Vector2(0f, 0.5f), new Vector2(40f + Mathf.Max(150f, 34f * label.Length + 40f) / 2f, 40f), new Vector2(Mathf.Max(150f, 34f * label.Length + 40f), 70f));
            _keyUntil = Time.time + 1.3f;
        }

        public void ShowPointer(bool show) => _pointerGroup.alpha = show ? 1f : 0f;

        /// <summary>Glides the pointer to a UI element (the click itself is done by the caller) and pulses it.</summary>
        public IEnumerator PointTo(RectTransform target, bool pulse)
        {
            yield return null; yield return null; // let the target's canvas switch to the camera this frame
            if (_camera == null || target == null) yield break;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 world = (corners[0] + corners[2]) * 0.5f;
            Vector2 screen = _camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_overlay.transform, screen, _camera, out var local);
            if (_pointerGroup.alpha < 0.5f) { _pointer.anchoredPosition = local + new Vector2(260f, -180f); _pointerGroup.alpha = 1f; }
            _pointerToken++;
            Vector2 start = _pointer.anchoredPosition;
            for (int i = 1; i <= 16; i++)
            {
                float t = i / 16f;
                _pointer.anchoredPosition = Vector2.Lerp(start, local, t * t * (3f - 2f * t));
                yield return null;
            }
            if (pulse)
                for (int i = 0; i <= 8; i++) { _pointer.localScale = Vector3.one * (1f - 0.3f * Mathf.Sin(i / 8f * Mathf.PI)); yield return null; }
            StartCoroutine(HidePointerLater(++_pointerToken));
        }

        private int _pointerToken;

        private IEnumerator HidePointerLater(int token)
        {
            for (int i = 0; i < 30; i++) yield return null;
            if (token == _pointerToken) _pointerGroup.alpha = 0f;
        }
    }
}
