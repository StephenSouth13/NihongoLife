using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Life / career / economy acceptance, played through the real game. Captures and logs: Bao_Cao/lifeloop-regression.
    /// · City: HUD layout at four resolutions (no overlaps, no shortcut bar), cursor rules (gameplay look, Ctrl, windows),
    ///   O/Esc settings, Tab profile, N journal, a full konbini shift (wrong answer gives nothing, reward paid once),
    ///   spending the wage, save → reload of money / XP / quests / inventory.
    /// · Sushi: cancel a shift (no pay), then a full serving shift reported to Aoki.
    /// · Midori Island: farm-helper shift with timed work (hoe vs bare hands), harvest + sell, JA/EN, back to Hibari.
    /// </summary>
    public class LifeLoopPlayModeTests
    {
        private const string Folder = "lifeloop-regression";
        private PlayerController _player;
        private SceneFlowController _flow;
        private readonly List<string> _log = new();

        [TearDown]
        public void TearDown()
        {
            CursorDirector.AssumeFocusForTests = false;
            TimedAction.SpeedScale = 1f;
            QuestService.ClockOffset = System.TimeSpan.Zero;
        }

        // ─────────────────────────────────────────── City ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator City_HudCursorJournalKonbiniShiftAndSpend()
        {
            TimedAction.SpeedScale = 0.15f;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return WaitReady();
            CloseDialogues();
            var bag = PlayerInventory.Instance;
            ResetQuests();
            if (bag.Yen > 300) bag.SpendYen(bag.Yen - 300); // a poor new player: the job must be the way to money
            Note($"start yen={bag.Yen} level={PlayerStatus.Instance.Level} xpTotal={PlayerStatus.Instance.TotalExp} knowledge={PlayerStatus.Instance.Knowledge}");

            // ── HUD policy and layout ──
            var dock = Object.FindFirstObjectByType<StatusDock>();
            Assert.NotNull(dock?.StatusWidget, "Compact status widget");
            Assert.IsNull(GameObject.Find("ActionBar"), "No permanent shortcut bar on desktop");
            HudFeed.Post("Kiểm tra bố cục: thông báo ở giữa phía trên.");
            yield return new WaitForSecondsRealtime(0.6f);
            foreach (var (w, h) in new[] { (1920, 1080), (1600, 900), (1366, 768), (1280, 720) })
            {
                var overlaps = CaptureAndCheck($"01_hud_{w}x{h}", w, h);
                Note($"HUD {w}x{h}: overlaps={(overlaps.Count == 0 ? "none" : string.Join("; ", overlaps))}");
                Assert.IsEmpty(overlaps, $"HUD overlaps at {w}x{h}: {string.Join("; ", overlaps)}");
            }

            // ── Cursor: gameplay look by default, Ctrl and windows give the cursor back ──
            CursorDirector.AssumeFocusForTests = true;
            ControlSettings.Scheme = ControlScheme.MouseLook;
            yield return null; yield return null;
            Assert.IsTrue(CursorDirector.GameplayLook, "Gameplay mode: mouse turns the camera, no button held");
            var input = GameInputService.GetOrCreate();
            input.SetMobileButton(GameInputId.FreeCursor, true);
            yield return null; yield return null;
            Assert.IsFalse(CursorDirector.GameplayLook, "Holding Ctrl frees the cursor");
            input.SetMobileButton(GameInputId.FreeCursor, false);
            yield return null; yield return null;
            Assert.IsTrue(CursorDirector.GameplayLook, "Releasing Ctrl returns to gameplay look");

            // ── Settings: O opens, Esc closes; the controls section exists ──
            yield return Press(GameInputId.Settings);
            Assert.AreEqual("Settings", UiModalStack.TopName);
            Assert.IsFalse(CursorDirector.GameplayLook, "A window frees the cursor");
            Assert.NotNull(GameObject.Find("ControlScheme"), "Settings → control scheme");
            Assert.NotNull(GameObject.Find("Bind_" + GameInputId.Journal), "Settings lists the N key");
            CaptureAndCheck("02_settings_controls", 1600, 900);
            Assert.IsTrue(UiModalStack.CloseTop());
            yield return null; yield return null;
            Assert.IsTrue(CursorDirector.GameplayLook);

            // ── Tab: character profile ──
            yield return Press(GameInputId.Character);
            Assert.IsTrue(dock.CharacterRoot.activeInHierarchy, "Tab opens the profile");
            yield return new WaitForSecondsRealtime(0.4f);
            CaptureAndCheck("03_profile_tab", 1600, 900);
            Assert.IsTrue(UiModalStack.CloseTop());

            // ── N: task journal → accept the konbini shift ──
            yield return Press(GameInputId.Journal);
            Assert.IsTrue(TaskJournalUI.IsOpen, "N opens the task journal");
            TaskJournalUI.Select("job_konbini_shift");
            yield return null;
            CaptureAndCheck("04_journal_job", 1600, 900);
            ClickNamed("Accept");
            Assert.AreEqual("active", QuestService.State("job_konbini_shift")?.status, "Accepted from the journal");
            Assert.IsNotNull(QuestService.Accept("job_konbini_shift"), "Accepting twice is refused");
            yield return null;
            CaptureAndCheck("05_journal_active", 1600, 900);
            Assert.IsTrue(UiModalStack.CloseTop());
            CursorDirector.AssumeFocusForTests = false;

            // ── The shift: real actions only ──
            var stations = Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).Where(s => s.QuestId == "job_konbini_shift").ToList();
            JobStation S(string id) => stations.First(s => s.StationId == id);
            Assert.IsFalse(S("shelf_onigiri").IsInteractionAvailable, "A shelf needs a box first");
            yield return StandAt(S("stock").transform, 1.0f);
            yield return Press(GameInputId.Interact);
            yield return WaitWork();
            Assert.AreEqual("stock", JobShift.CarryKind, "F at the stock crate: carrying a box");
            CaptureAndCheck("06_carrying_box", 1600, 900);
            int restocked = 0;
            foreach (var shelf in new[] { "shelf_onigiri", "shelf_snacks", "shelf_drinks" })
            {
                if (JobShift.CarryKind != "stock") { S("stock").Interact(_player.gameObject); yield return WaitWork(); }
                S(shelf).Interact(_player.gameObject);
                yield return new WaitForSecondsRealtime(0.05f);
                if (restocked == 0) CaptureAndCheck("07_restock_progress", 1600, 900);
                yield return WaitWork();
                restocked++;
                Assert.AreEqual(restocked, QuestService.ObjectiveProgress("job_konbini_shift", "restock"));
            }
            Assert.IsFalse(S("shelf_onigiri").IsInteractionAvailable, "A shelf is restocked once per shift");

            foreach (var customer in new[] { "customer_a", "customer_b" })
            {
                S(customer).Interact(_player.gameObject);
                Assert.IsTrue(JobQuizCard.IsOpen);
                int before = QuestService.ObjectiveProgress("job_konbini_shift", "assist");
                int wrong = IndexOf(c => !c.Correct), right = IndexOf(c => c.Correct);
                JobQuizCard.Answer(wrong);
                Assert.IsTrue(JobQuizCard.IsOpen, "A wrong answer keeps the customer waiting");
                Assert.AreEqual(before, QuestService.ObjectiveProgress("job_konbini_shift", "assist"), "No credit for a wrong answer");
                if (customer == "customer_a") CaptureAndCheck("08_customer_question", 1600, 900);
                JobQuizCard.Answer(right);
                Assert.AreEqual(before + 1, QuestService.ObjectiveProgress("job_konbini_shift", "assist"));
            }
            S("register").Interact(_player.gameObject);
            yield return WaitWork();
            Assert.IsTrue(JobQuizCard.IsOpen, "Scanning leads to the total question");
            CaptureAndCheck("09_register_total", 1600, 900);
            JobQuizCard.Answer(IndexOf(c => c.Correct));
            Assert.AreEqual(1, QuestService.ObjectiveProgress("job_konbini_shift", "checkout"));
            Assert.IsTrue(QuestService.ReadyToReport("job_konbini_shift"));

            // ── Report to Ito: paid once ──
            int yen0 = bag.Yen, xp0 = PlayerStatus.Instance.TotalExp, know0 = PlayerStatus.Instance.Knowledge;
            var ito = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_cashier");
            ito.Interact(_player.gameObject);
            yield return new WaitForSecondsRealtime(0.3f);
            CloseDialogues();
            Assert.AreEqual(yen0 + 600, bag.Yen, "Wage ¥600 paid");
            Assert.AreEqual(xp0 + 40, PlayerStatus.Instance.TotalExp, "+40 XP");
            Assert.AreEqual(know0 + 6, PlayerStatus.Instance.Knowledge, "+6 knowledge (separate from XP)");
            CaptureAndCheck("10_paid", 1600, 900);
            ito.Interact(_player.gameObject); yield return null; CloseDialogues();
            QuestService.Raise("checkout", "konbini_register"); QuestService.Raise("talk", "npc_cashier");
            Assert.AreEqual(yen0 + 600, bag.Yen, "Talking again / repeated events never pay twice");
            Assert.AreEqual(QuestAvailability.Cooldown, QuestService.Availability(QuestService.Catalog.Quest("job_konbini_shift")), "Next shift after the cooldown");
            Assert.AreEqual(1, Progress().careers.First(c => c.roleId == "job_konbini_shift").totalShifts);

            // ── Spend the wage: food at the konbini (daily task) ──
            Assert.IsNull(QuestService.Accept("daily_konbini_meal"));
            var onigiri = KonbiniCatalog.Products.First(p => p.section == KonbiniSection.Onigiri);
            int yen1 = bag.Yen;
            KonbiniBasket.Clear();
            KonbiniBasket.Add(onigiri.id, 1);
            Assert.IsTrue(KonbiniShopUI.GetOrCreate().Pay(), "Buy with earned money");
            yield return new WaitForSecondsRealtime(0.2f);
            CloseDialogues();
            Assert.AreEqual(yen1 - onigiri.price, bag.Yen, "Price taken once");
            Assert.IsTrue(bag.HasItem(onigiri.id));
            Assert.AreEqual("completed", QuestService.State("daily_konbini_meal")?.status, "Daily meal done by a real purchase");
            KonbiniBasket.Clear();
            Assert.IsFalse(KonbiniShopUI.GetOrCreate().Pay(), "Empty basket: nothing charged");

            // ── Save → reload ──
            var repository = Repository();
            string json = JsonUtility.ToJson(repository.GetProgress());
            var reloaded = JsonUtility.FromJson<PlayerProgressDto>(json);
            Assert.AreEqual(bag.Yen, reloaded.yen);
            Assert.AreEqual(PlayerStatus.Instance.TotalExp, reloaded.xp);
            Assert.AreEqual("completed", reloaded.quests.First(q => q.questId == "job_konbini_shift").status);
            Assert.IsTrue(reloaded.inventory.Any(i => i.itemId == onigiri.id));
            PlayerStatus.Instance.ReloadProgress();
            Assert.AreEqual(reloaded.xp, PlayerStatus.Instance.TotalExp, "XP survives a reload");
            Note($"end yen={bag.Yen} level={PlayerStatus.Instance.Level} xpTotal={PlayerStatus.Instance.TotalExp} knowledge={PlayerStatus.Instance.Knowledge}");
            WriteLog("city.txt");
        }

        // ─────────────────────────────────────────── Sushi ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator Sushi_CancelThenFullServingShift()
        {
            TimedAction.SpeedScale = 0.15f;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.SushiRestaurantScene);
            yield return WaitZone(WorldLocationCatalog.SushiRestaurantScene, 30f);
            CloseDialogues();
            ResetQuests();
            var bag = PlayerInventory.Instance;

            var board = Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).First(s => s.QuestId == "job_sushi_shift" && s.Kind == JobStationKind.Board);
            yield return StandAt(board.transform, 0.8f);
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(TaskJournalUI.IsOpen && TaskJournalUI.SelectedQuestId == "job_sushi_shift", "The job board opens the journal on its job");
            ClickNamed("Accept");
            ClickNamed("Abandon");
            Assert.AreNotEqual("active", QuestService.State("job_sushi_shift")?.status, "Cancelled");
            int yenCancelled = bag.Yen;
            Assert.IsNull(QuestService.Accept("job_sushi_shift"), "Can start again after cancelling");
            UiModalStack.CloseTop();

            var stations = Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).Where(s => s.QuestId == "job_sushi_shift").ToList();
            var tables = stations.Where(s => s.Kind == JobStationKind.OrderTable).ToList();
            var pass = stations.First(s => s.Kind == JobStationKind.DishPass);
            Assert.AreEqual(2, tables.Count);
            foreach (var table in tables)
            {
                table.Interact(_player.gameObject);
                Assert.IsTrue(JobQuizCard.IsOpen);
                if (table == tables[0]) CaptureAndCheck("20_sushi_order", 1600, 900);
                JobQuizCard.Answer(IndexOf(c => c.Correct));
            }
            Assert.AreEqual(2, QuestService.ObjectiveProgress("job_sushi_shift", "order"));
            for (int served = 0; served < 2; served++)
            {
                pass.Interact(_player.gameObject);
                Assert.IsTrue(JobQuizCard.IsOpen, "Kitchen pass asks which dish");
                JobQuizCard.Answer(IndexOf(c => c.Correct));
                yield return WaitWork();
                Assert.AreEqual("dish", JobShift.CarryKind);
                var target = tables.First(t => JobShift.OrderAt(t.StationId) == JobShift.CarryId);
                var other = tables.FirstOrDefault(t => t != target && JobShift.OrderAt(t.StationId) != null && JobShift.OrderAt(t.StationId) != JobShift.CarryId);
                if (other != null)
                {
                    other.Interact(_player.gameObject); yield return WaitWork();
                    Assert.AreEqual("dish", JobShift.CarryKind, "Wrong table: the dish is not served");
                }
                target.Interact(_player.gameObject);
                yield return WaitWork();
                Assert.AreEqual(served + 1, QuestService.ObjectiveProgress("job_sushi_shift", "serve"));
            }
            var aoki = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_sushi_staff");
            aoki.Interact(_player.gameObject);
            yield return new WaitForSecondsRealtime(0.3f);
            CloseDialogues();
            Assert.AreEqual(yenCancelled + 700, bag.Yen, "Paid ¥700 for the completed shift only");
            CaptureAndCheck("21_sushi_paid", 1600, 900);
            WriteLog("sushi.txt");
        }

        // ─────────────────────────────────────────── Midori Island ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator Island_FarmShiftHarvestSellLanguageAndReturn()
        {
            TimedAction.SpeedScale = 0.15f;
            IslandTrainService.RideSeconds = 1.2f;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.MidoriIslandScene);
            yield return WaitZone(WorldLocationCatalog.MidoriIslandScene, 30f);
            for (float t = 0f; t < 10f && IslandRuntime.Active == null; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            ResetQuests();
            var bag = PlayerInventory.Instance;
            foreach (var plot in Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None)) { var r = plot.Record; r.tilled = false; r.cropId = null; r.stage = 0; r.watered = false; plot.Refresh(true); }
            if (bag.GetItemQuantity("tool_hoe") > 0) bag.RemoveItem("tool_hoe", bag.GetItemQuantity("tool_hoe"));
            if (bag.GetItemQuantity("tool_shovel") > 0) bag.RemoveItem("tool_shovel", bag.GetItemQuantity("tool_shovel"));
            if (bag.GetItemQuantity("tool_watering_can") == 0) IslandEconomy.Give("tool_watering_can", 1);
            int seedsBefore = bag.GetItemQuantity("seed_carrot");

            // Hana offers the job → journal → accept: 2 seed packets handed over.
            var hana = Object.FindFirstObjectByType<JobGiver>();
            Assert.NotNull(hana, "Farm manager Hana on the island");
            yield return StandAt(hana.transform, -1.4f);
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(TaskJournalUI.IsOpen && TaskJournalUI.SelectedQuestId == "job_farm_shift", "Hana opens the job in the journal");
            ClickNamed("Accept");
            UiModalStack.CloseTop();
            Assert.AreEqual(seedsBefore + 2, bag.GetItemQuantity("seed_carrot"), "Seeds handed over on accepting");

            // Without a hoe: digging by hand is allowed but takes much longer than with a hoe.
            var plots = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).OrderBy(p => p.Number).Take(2).ToList();
            yield return StandAt(plots[0].transform, 1.9f);
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(IslandUI.FarmOpen);
            var (handTool, _, handSeconds) = FarmActionTimes.Till();
            Assert.IsNull(handTool, "No hoe owned → bare hands");
            float t0 = Time.realtimeSinceStartup;
            ClickText("Xới bằng tay");
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsTrue(TimedAction.Busy, "Digging shows a progress bar");
            CaptureAndCheck("30_dig_by_hand", 1600, 900);
            ClickText("Xới bằng tay"); // rapid repeated click: ignored while working
            yield return WaitWork();
            float handTime = Time.realtimeSinceStartup - t0;
            Assert.AreEqual(1, QuestService.ObjectiveProgress("job_farm_shift", "till"), "Repeated clicks count once");
            IslandUI.CloseFarm();
            IslandEconomy.Give("tool_hoe", 1);
            yield return StandAt(plots[1].transform, 1.9f);
            yield return Press(GameInputId.Interact);
            t0 = Time.realtimeSinceStartup;
            ClickText("Xới đất");
            yield return WaitWork();
            float hoeTime = Time.realtimeSinceStartup - t0;
            Note($"till by hand {handTime:0.00}s vs hoe {hoeTime:0.00}s (scale {TimedAction.SpeedScale})");
            Assert.Less(hoeTime * 2.5f, handTime, "A hoe digs much faster than bare hands");
            IslandUI.CloseFarm();

            foreach (var plot in plots)
            {
                IslandUI.OpenFarm(plot);
                ClickNamed("Plant_carrot"); yield return WaitWork();
                ClickText("Tưới nước"); yield return WaitWork();
                IslandUI.CloseFarm();
            }
            Assert.AreEqual(2, QuestService.ObjectiveProgress("job_farm_shift", "plant"));
            Assert.AreEqual(2, QuestService.ObjectiveProgress("job_farm_shift", "water"));

            // Feed one animal (needs a carrot: grow one plot to harvest first).
            var crop = plots[0].Crop;
            for (int stage = 0; stage < 3; stage++)
            {
                IslandState.ClockOffset += System.TimeSpan.FromSeconds(crop.secondsPerStage + 1);
                yield return new WaitForSecondsRealtime(0.6f);
                if (plots[0].CurrentPhase == FarmPlot.Phase.NeedsWater) { IslandUI.OpenFarm(plots[0]); ClickText("Tưới nước"); yield return WaitWork(); IslandUI.CloseFarm(); }
            }
            Assert.AreEqual(FarmPlot.Phase.Ready, plots[0].CurrentPhase);
            IslandUI.OpenFarm(plots[0]);
            ClickText("Thu hoạch"); yield return WaitWork();
            IslandUI.CloseFarm();
            Assert.GreaterOrEqual(bag.GetItemQuantity("carrot"), 2, "Harvest in the bag");
            var animal = Object.FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).First(a => a.Def.foods.Contains("carrot"));
            IslandUI.OpenAnimal(animal);
            ClickNamedIn("AnimalCard", "Feed"); yield return WaitWork();
            IslandUI.CloseAnimal();
            Assert.AreEqual(1, QuestService.ObjectiveProgress("job_farm_shift", "feed"));
            Assert.IsTrue(QuestService.ReadyToReport("job_farm_shift"));

            int yen0 = bag.Yen;
            hana.Interact(_player.gameObject);
            Assert.AreEqual(yen0 + 500, bag.Yen, "Hana pays ¥500");
            hana.Interact(_player.gameObject);
            Assert.AreEqual(yen0 + 500, bag.Yen, "…once");
            CaptureAndCheck("31_farm_paid", 1600, 900);

            // Sell produce, switch the learning language, then travel back.
            int yen1 = bag.Yen;
            Assert.AreEqual(IslandEconomy.Result.Ok, IslandEconomy.Sell("carrot", IslandCatalog.Load().Crop("carrot").sellPrice, 1));
            Assert.AreEqual(yen1 + IslandCatalog.Load().Crop("carrot").sellPrice, bag.Yen);
            IslandLanguage.Target = TargetLanguage.English;
            Assert.AreEqual("Field 1", Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).First(p => p.Number == 1).GetPromptJa());
            IslandLanguage.Target = TargetLanguage.Japanese;

            Assert.IsNull(IslandTrainService.BuyTicket());
            Assert.IsNull(IslandTrainService.Depart(Object.FindFirstObjectByType<IslandTrainDoor>()));
            yield return WaitZone(WorldLocationCatalog.StationScene, 30f);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsFalse(_player.InputLocked, "Player free after returning to Hibari");
            Assert.IsFalse(TimedAction.Busy);
            CaptureAndCheck("32_back_at_hibari", 1600, 900);
            WriteLog("island.txt");
        }

        // ─────────── helpers ───────────

        private static IProgressRepository Repository() { GameServices.TryGet(out IProgressRepository r); return r; }
        private static PlayerProgressDto Progress() => Repository().GetProgress();

        private static void ResetQuests()
        {
            var progress = Progress();
            progress.quests.Clear();
            progress.trackedQuestId = string.Empty;
            JobShift.Sync("none");
        }

        private static int IndexOf(System.Func<JobQuizCard.Choice, bool> predicate)
        {
            var list = JobQuizCard.CurrentChoices.ToList();
            return list.FindIndex(c => predicate(c));
        }

        private IEnumerator WaitReady(float timeout = 20f)
        {
            for (float t = 0f; t < timeout; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                _flow = GameServices.TryGet(out SceneFlowController f) ? f : Object.FindFirstObjectByType<SceneFlowController>();
                if (_player != null && _flow != null && !_flow.IsLoading && t > 2f) yield break;
                yield return null;
            }
        }

        private IEnumerator WaitZone(string scene, float timeout)
        {
            for (float t = 0f; t < timeout; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                _flow = GameServices.TryGet(out SceneFlowController f) ? f : Object.FindFirstObjectByType<SceneFlowController>();
                if (_player != null && _flow != null && !_flow.IsLoading && !StandaloneZoneBootstrap.IsBooting && SceneManager.GetActiveScene().name == scene && t > 2f)
                { yield return new WaitForSecondsRealtime(0.8f); yield break; }
                yield return null;
            }
            Assert.Fail($"Timed out waiting for {scene}");
        }

        private static IEnumerator WaitWork()
        {
            yield return null;
            for (float t = 0f; t < 10f && TimedAction.Busy; t += Time.unscaledDeltaTime) yield return null;
            yield return null;
        }

        private static void CloseDialogues()
        {
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
        }

        private IEnumerator Press(GameInputId id)
        {
            var input = GameInputService.GetOrCreate();
            yield return new WaitForFixedUpdate();
            input.SetMobileButton(id, true);
            yield return null;
            input.SetMobileButton(id, false);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        /// <summary>Puts the player in front of a station along the station's own facing (konbini is rotated in the
        /// city), facing it; a negative distance stands on its front side (people who face the player, e.g. Hana).</summary>
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
            if (camera != null) camera.SetOrbit(facing.eulerAngles.y, 24f, 4.2f);
            yield return new WaitForSecondsRealtime(0.5f);
            Note($"at {target.name}: current={_player.GetComponent<NihongoLife.Interaction.InteractionDetector>()?.CurrentInteractable}");
        }

        private static void ClickNamed(string name)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.gameObject.activeInHierarchy && b.interactable);
            Assert.NotNull(button, "Button " + name);
            button.onClick.Invoke();
        }

        private static void ClickNamedIn(string window, string name)
        {
            var root = GameObject.Find(window);
            Assert.NotNull(root, window);
            var button = root.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == name);
            Assert.NotNull(button, name);
            button.onClick.Invoke();
        }

        private static void ClickText(string label)
        {
            var card = GameObject.Find("FarmCard");
            Assert.NotNull(card, "Farm card open");
            var button = card.GetComponentsInChildren<Button>().FirstOrDefault(b => b.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text.Contains(label) == true);
            Assert.NotNull(button, $"Button '{label}'");
            button.onClick.Invoke();
        }

        private void Note(string line) { _log.Add(line); Debug.Log("[LifeLoop] " + line); }
        private void WriteLog(string file) => File.WriteAllLines(Path.Combine(FolderPath(), file), _log);

        private static string FolderPath()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + Folder));
            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>Renders the game camera + every overlay canvas at the given resolution and returns HUD overlaps
        /// between the persistent HUD pieces (status widget, objective chip, feed, prompt lane, warning, action card).</summary>
        private static List<string> CaptureAndCheck(string name, int width, int height)
        {
            var overlaps = new List<string>();
            var camera = Camera.main;
            if (camera == null) return overlaps;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
            var target = new RenderTexture(width, height, 24);
            var old = RenderTexture.active;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            foreach (var c in canvases) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            Canvas.ForceUpdateCanvases();

            var pieces = new List<(string name, Rect rect)>();
            void Add(string label, RectTransform rt)
            {
                if (rt == null || !rt.gameObject.activeInHierarchy) return;
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                Vector2 a = camera.WorldToScreenPoint(corners[0]), b = camera.WorldToScreenPoint(corners[2]);
                var rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                if (rect.width > 1f && rect.height > 1f) pieces.Add((label, rect));
                if (rect.xMin < -1f || rect.yMin < -1f || rect.xMax > width + 1f || rect.yMax > height + 1f) overlaps.Add($"{label} clipped by the screen edge");
            }
            var dock = Object.FindFirstObjectByType<StatusDock>();
            if (dock != null && !(dock.CharacterRoot != null && dock.CharacterRoot.activeInHierarchy))
            {
                Add("status", dock.StatusWidget);
                Add("objective", dock.ObjectiveChip);
                Add("warning", GameObject.Find("NeedWarning")?.transform as RectTransform);
            }
            foreach (var card in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Where(r => r.name == "FeedCard")) Add("feed", card);
            Add("prompt", GameObject.Find("PromptPanel")?.transform as RectTransform);
            Add("resume", GameObject.Find("ResumeDialogue")?.transform as RectTransform);
            Add("action", GameObject.Find("ActionCard")?.transform as RectTransform);
            for (int i = 0; i < pieces.Count; i++)
                for (int j = i + 1; j < pieces.Count; j++)
                    if (pieces[i].name != pieces[j].name && pieces[i].rect.Overlaps(pieces[j].rect)) overlaps.Add($"{pieces[i].name}×{pieces[j].name}");

            camera.Render();
            RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(FolderPath(), name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); target.Release(); Object.Destroy(target);
            return overlaps;
        }
    }
}
