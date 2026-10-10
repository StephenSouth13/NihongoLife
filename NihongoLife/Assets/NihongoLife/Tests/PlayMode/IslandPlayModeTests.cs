using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;
using NihongoLife.Interaction;
using NihongoLife.Island;
using NihongoLife.Player;
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
    /// Midori Island acceptance flow, played through the real game: city → Hibari Station → train to Midori (ticket paid
    /// once from the shared wallet) → starter kit → Midori store (buy seeds) → till → plant → water → grow (farm clock
    /// fast-forwarded through IslandState.ClockOffset) → harvest → produce in the bag → sell → wallet change → switch
    /// Japanese/English → animal (feed, pet) → leave by train → come back → plots/words restored → travel back to Hibari.
    /// Windows are opened with the real F / P input and driven through their buttons. Captures: Bao_Cao/island-regression.
    /// </summary>
    public class IslandPlayModeTests
    {
        private const string Folder = "island-regression";
        private SceneFlowController _flow;
        private PlayerController _player;
        private readonly List<string> _log = new();

        [UnityTest]
        public IEnumerator MidoriIsland_FullAcceptanceFlow()
        {
            float started = Time.realtimeSinceStartup;
            IslandTrainService.RideSeconds = 1.2f;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return WaitReady(20f);
            Assert.NotNull(_player, "City must spawn the player.");

            // Clean island state for a deterministic run (tests play as a guest: nothing is written to the real save).
            Assert.IsTrue(GameServices.TryGet(out IProgressRepository repository));
            var progress = repository.GetProgress();
            progress.island = new IslandRecord();
            IslandState.ClockOffset = System.TimeSpan.Zero;
            var bag = PlayerInventory.Instance;
            foreach (var id in bag.Items.Select(i => i.itemId).ToList())
                if (id.StartsWith("seed_") || id.StartsWith("tool_") || id.StartsWith("train_ticket") || IslandCatalog.Load().Crop(id) != null) bag.RemoveItem(id, bag.GetItemQuantity(id));
            if (bag.Yen < 3000) bag.AddYen(3000 - bag.Yen);
            while (bag.Items.Count > bag.MaxSlots - 8) bag.RemoveItem(bag.Items[0].itemId, bag.Items[0].quantity);
            Note($"start yen={bag.Yen} slots={bag.Items.Count}/{bag.MaxSlots}");

            // ── 1–2. Station: ticket to Midori, paid once ──
            _flow.EnterZone(WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, "station");
            yield return WaitZone(WorldLocationCatalog.StationScene);
            var station = Object.FindFirstObjectByType<StationTravelController>();
            Assert.NotNull(station);
            Assert.IsTrue(StationTravelController.Line.Any(s => s.Id == "midori" && s.Price == IslandCatalog.Load().ticketPrice), "Line must end at Midori for the catalog fare.");
            int yen0 = bag.Yen;
            Assert.IsTrue(station.PurchaseTicket("midori"));
            Assert.IsFalse(station.PurchaseTicket("midori"), "A second ticket must be refused.");
            Assert.AreEqual(yen0 - 450, bag.Yen, "Ticket must be charged exactly once.");
            station.Execute(StationAction.PassGate, _player.gameObject);
            Assert.IsTrue(station.GatePassed);
            for (float t = 0f; t < 30f && !station.IsTrainBoarding; t += Time.unscaledDeltaTime) yield return null;
            station.Execute(StationAction.BoardTrain, _player.gameObject);
            Assert.IsTrue(station.IsOnboard, "Validated passenger boards the train.");
            Capture("01_onboard_to_midori");
            Time.timeScale = 4f;
            yield return WaitZone(WorldLocationCatalog.MidoriIslandScene, 60f);
            Time.timeScale = 1f;
            Assert.IsFalse(bag.HasItem(StationTravelController.TicketItemId), "The ticket is collected on arrival.");
            yield return new WaitForSecondsRealtime(1.2f);
            float gap = FloorGap(out string floor);
            Note($"arrived island at {_player.transform.position} gap={gap:0.000} floor={floor}");
            Assert.Less(Mathf.Abs(gap), 0.05f, "Player must stand on the platform, not float.");
            Assert.IsFalse(_player.InputLocked, "Player can move after arriving.");
            Capture("02_arrival_welcome");

            // ── 3. Starter kit ──
            Assert.AreEqual(1, bag.GetItemQuantity("tool_hoe"));
            Assert.AreEqual(1, bag.GetItemQuantity("tool_watering_can"));
            Assert.AreEqual(3, bag.GetItemQuantity("seed_carrot"));

            // ── 4. Midori store: buy seeds (opened with F at the counter) ──
            yield return StandAt(Object.FindFirstObjectByType<IslandShopCounter>().transform, 1.2f);
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(IslandUI.ShopOpen, "F at the counter opens the Midori store.");
            Assert.IsTrue(_player.InputLocked, "Walking is locked while the store is open.");
            IslandUI.SelectShopItem("seed_tomato");
            IslandUI.SetShopQuantity(2);
            Capture("03_shop_agriculture");
            CaptureSizes("03_shop");
            int yen1 = bag.Yen;
            Click("IslandShop", "Confirm");
            Assert.AreEqual(yen1 - 120, bag.Yen, "2 tomato seeds cost ¥120.");
            Assert.AreEqual(2, bag.GetItemQuantity("seed_tomato"));
            IslandUI.SelectShopTab("fashion");
            Capture("04_shop_fashion_honest_empty");
            IslandUI.SelectShopTab("technology");
            IslandUI.SelectShopItem("tech_laptop");
            Capture("05_shop_technology");
            IslandUI.SelectShopTab("agriculture");
            IslandUI.SelectShopItem("tool_shovel");
            IslandUI.SetShopQuantity(20);
            Assert.AreEqual(IslandEconomy.Result.NoMoney, IslandUI.ConfirmShop(), "Unaffordable purchase is refused.");
            Capture("06_shop_not_enough_money");
            Assert.IsTrue(UiModalStack.CloseTop(), "Esc closes the store.");
            Assert.IsFalse(IslandUI.ShopOpen);
            yield return null;
            Assert.IsFalse(_player.InputLocked);

            // ── 5–9. Farm: till, plant, water, grow, harvest ──
            var plot = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).First(p => p.Number == 1);
            yield return StandAt(plot.transform, 1.9f);
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(IslandUI.FarmOpen && IslandUI.OpenPlot == plot, "F at the plot opens its farm card.");
            Assert.AreEqual(FarmPlot.Phase.Untilled, plot.CurrentPhase);
            Capture("07_farm_untilled");
            ClickText("FarmCard", "Xới đất");
            Assert.AreEqual(FarmPlot.Phase.Tilled, plot.CurrentPhase);
            Capture("08_farm_seed_picker");
            Click("FarmCard", "Plant_carrot");
            Assert.AreEqual(FarmPlot.Phase.NeedsWater, plot.CurrentPhase);
            Assert.AreEqual(2, bag.GetItemQuantity("seed_carrot"));
            ClickText("FarmCard", "Tưới nước");
            Assert.AreEqual(FarmPlot.Phase.Growing, plot.CurrentPhase);
            Assert.IsNotNull(Error(plot.Water()), "Watering twice in one stage is refused with a message.");
            yield return new WaitForSecondsRealtime(0.6f);
            Capture("09_farm_growing");
            var crop = plot.Crop;
            for (int stage = 1; stage <= 3; stage++)
            {
                IslandState.ClockOffset += System.TimeSpan.FromSeconds(crop.secondsPerStage + 1);
                yield return new WaitForSecondsRealtime(0.7f);
                Assert.AreEqual(stage, plot.Record.stage, $"Stage {stage} after the growth time.");
                string model = plot.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.StartsWith(crop.model + "_"))?.name;
                Note($"stage {stage}: phase={plot.CurrentPhase} model={model}");
                Assert.AreEqual($"{crop.model}_{stage + 1}", model, "The crop model grows with each stage.");
                if (stage < 3) { Assert.AreEqual(FarmPlot.Phase.NeedsWater, plot.CurrentPhase); yield return new WaitForSecondsRealtime(0.3f); ClickText("FarmCard", "Tưới nước"); }
                if (stage == 2) { yield return new WaitForSecondsRealtime(0.3f); CaptureWorld("10_crop_stage3", plot.transform); }
            }
            Assert.AreEqual(FarmPlot.Phase.Ready, plot.CurrentPhase);
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("11_farm_ready");
            ClickText("FarmCard", "Thu hoạch");
            Assert.AreEqual(2, bag.GetItemQuantity("carrot"), "Harvest puts the produce in the bag.");
            Assert.AreEqual(FarmPlot.Phase.Untilled, plot.CurrentPhase);
            Capture("12_harvested");
            Assert.IsTrue(UiModalStack.CloseTop());

            // ── 10–11. Sell with P ──
            yield return Press(GameInputId.Shop);
            Assert.IsTrue(IslandUI.ShopOpen, "P opens the Midori store anywhere on the island.");
            IslandUI.SelectShopTab("sell");
            IslandUI.SelectShopItem("carrot");
            Capture("13_shop_sell");
            int yen2 = bag.Yen;
            Click("IslandShop", "Confirm");
            Assert.AreEqual(yen2 + 35, bag.Yen, "Selling one carrot pays ¥35.");
            Assert.AreEqual(1, bag.GetItemQuantity("carrot"));
            Assert.AreEqual(1, IslandState.Record.sold);
            yield return Press(GameInputId.Shop);
            Assert.IsFalse(IslandUI.ShopOpen, "P toggles the store closed.");

            // ── 12. Japanese ↔ English ──
            Assert.AreEqual(TargetLanguage.Japanese, IslandLanguage.Target);
            string promptJa = plot.GetPromptJa();
            IslandUI.ToggleLanguage();
            Assert.AreEqual(TargetLanguage.English, IslandLanguage.Target);
            Assert.AreNotEqual(promptJa, plot.GetPromptJa());
            yield return Press(GameInputId.Shop);
            IslandUI.SelectShopTab("agriculture");
            IslandUI.SelectShopItem("seed_corn");
            Capture("14_shop_english");
            Assert.IsTrue(UiModalStack.CloseTop());
            IslandUI.ToggleLanguage();
            Assert.AreEqual(TargetLanguage.Japanese, IslandLanguage.Target);

            // ── 13. Animals ──
            var cow = Object.FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).First(a => a.AnimalId == "cow");
            var animator = cow.GetComponentInChildren<Animator>();
            Assert.NotNull(animator?.runtimeAnimatorController, "Animals use real animated models.");
            yield return StandAt(cow.transform, 1.8f);
            // Animals wander: talk to whichever animal F actually targets (a farm animal that eats carrots).
            var targeted = _player.GetComponent<InteractionDetector>().CurrentInteractable as IslandAnimal;
            Assert.NotNull(targeted, "An animal is in reach after walking up to the herd.");
            Assert.IsTrue(targeted.Def.foods.Contains("carrot"), $"{targeted.AnimalId} eats carrots.");
            Note($"talking to {targeted.AnimalId}");
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(IslandUI.AnimalOpen, "F at the animal opens its card.");
            Capture("15_animal_card");
            Click("AnimalCard", "Feed");
            Assert.AreEqual(1, IslandState.Record.fed);
            Assert.AreEqual(0, bag.GetItemQuantity("carrot"), "Feeding uses one carrot.");
            Click("AnimalCard", "Feed");
            Assert.AreEqual(1, IslandState.Record.fed, "No food left: feeding is refused with a message.");
            Click("AnimalCard", "Pet");
            yield return new WaitForSecondsRealtime(0.5f);
            Capture("16_animal_fed");
            Assert.IsTrue(IslandState.Record.animalsMet.Contains(targeted.AnimalId));
            Assert.IsTrue(UiModalStack.CloseTop());
            var walkers = Object.FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).Select(a => a.transform.position).ToList();
            yield return new WaitForSecondsRealtime(4f);
            var moved = Object.FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).Select(a => a.transform.position).Zip(walkers, (a, b) => (a - b).magnitude).Max();
            Note($"animal max wander in 4s = {moved:0.00} m");

            // Notebook / progress window.
            IslandUI.OpenProgress();
            Capture("17_notebook");
            CaptureSizes("17_notebook");
            Assert.IsTrue(UiModalStack.CloseTop());

            // Plant something to check it survives leaving.
            var plot2 = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).First(p => p.Number == 2);
            Assert.IsNull(plot2.Till()); Assert.IsNull(plot2.Plant("tomato")); Assert.IsNull(plot2.Water());
            int words = IslandState.Record.words.Count;
            Note($"words={words} achievements={string.Join(",", IslandState.Record.achievements)}");
            CaptureOverview("18_island_overview");

            // ── 14–15. Leave by train, come back, progress restored ──
            int yen3 = bag.Yen;
            var kiosk = Object.FindFirstObjectByType<IslandTicketKiosk>();
            var door = Object.FindFirstObjectByType<IslandTrainDoor>();
            door.Interact(_player.gameObject);
            Assert.IsFalse(IslandTrainService.Departing, "Boarding without a ticket is refused.");
            yield return StandAt(kiosk.transform, 1.0f);
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(IslandTrainService.HasTicket, "F at the kiosk buys the ticket.");
            kiosk.Interact(_player.gameObject);
            Assert.AreEqual(yen3 - 450, bag.Yen, "Return ticket charged once.");
            Capture("19_return_ticket");
            yield return StandAt(door.transform, -0.8f);
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(IslandTrainService.Departing);
            yield return new WaitForSecondsRealtime(0.6f);
            Capture("20_travel_screen");
            yield return WaitZone(WorldLocationCatalog.StationScene, 30f);
            Assert.IsFalse(IslandTrainService.HasTicket, "The return ticket is collected.");
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(_player.InputLocked);
            Assert.IsNull(Object.FindFirstObjectByType<IslandRuntime>(), "The island is unloaded.");
            Capture("21_back_at_hibari");

            var json = JsonUtility.ToJson(repository.GetProgress());
            var reloaded = JsonUtility.FromJson<PlayerProgressDto>(json);
            Assert.AreEqual(words, reloaded.island.words.Count, "Island vocabulary is part of the saved progress.");
            Assert.AreEqual("tomato", reloaded.island.plots.First(p => p.plotId == "plot_2").cropId, "Planted plot is saved.");

            station = Object.FindFirstObjectByType<StationTravelController>();
            Assert.IsTrue(station.PurchaseTicket("midori"));
            station.Execute(StationAction.PassGate, _player.gameObject);
            for (float t = 0f; t < 30f && !station.IsTrainBoarding; t += Time.unscaledDeltaTime) yield return null;
            station.Execute(StationAction.BoardTrain, _player.gameObject);
            IslandState.ClockOffset += System.TimeSpan.FromSeconds(IslandCatalog.Load().Crop("tomato").secondsPerStage + 1); // time passes while away
            Time.timeScale = 4f;
            yield return WaitZone(WorldLocationCatalog.MidoriIslandScene, 60f);
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(1.2f);
            plot2 = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).First(p => p.Number == 2);
            Assert.AreEqual(1, plot2.Record.stage, "The tomato kept growing while the player was away.");
            Assert.AreEqual(FarmPlot.Phase.NeedsWater, plot2.CurrentPhase);
            Assert.AreEqual(words, IslandState.Record.words.Count);
            Assert.AreEqual(3, bag.GetItemQuantity("seed_carrot") + 1, "Starter kit is not given twice.");
            Capture("22_reentered_restored");

            // ── 17. Travel back again ──
            Assert.IsNull(IslandTrainService.BuyTicket());
            Assert.IsNull(IslandTrainService.Depart(door = Object.FindFirstObjectByType<IslandTrainDoor>()));
            yield return WaitZone(WorldLocationCatalog.StationScene, 30f);

            // Performance sample on the island is taken in its own test; here just the totals.
            Note($"total {Time.realtimeSinceStartup - started:0.0}s");
            WriteLog();
        }

        [UnityTest]
        public IEnumerator MidoriIsland_PerformanceAndLayout()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.MidoriIslandScene); // standalone boot through the city
            for (float t = 0f; t < 25f; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                _flow = GameServices.TryGet(out SceneFlowController f) ? f : null;
                if (_player != null && _flow != null && !_flow.IsLoading && !StandaloneZoneBootstrap.IsBooting && IslandRuntime.Active != null && SceneManager.GetActiveScene().name == WorldLocationCatalog.MidoriIslandScene && t > 3f) break;
                yield return null;
            }
            Assert.AreEqual(WorldLocationCatalog.MidoriIslandScene, SceneManager.GetActiveScene().name, "Play in 60_MidoriIsland boots through the city and enters the island.");
            yield return new WaitForSecondsRealtime(2f);
            float gap = FloorGap(out string floor);
            Assert.Less(Mathf.Abs(gap), 0.05f, $"Standalone spawn floats {gap:0.000} m above {floor}.");
            int suns = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.type == LightType.Directional && l.enabled && l.gameObject.activeInHierarchy);
            Assert.AreEqual(1, suns, "Exactly one sun lights the island.");

            for (float t = 0f; t < 20f && (Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).Length < 6 || Object.FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).Length < 5); t += Time.unscaledDeltaTime) yield return null;
            Note($"scenes: {string.Join(", ", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i)).Select(sc => sc.name + (sc.isLoaded ? "" : "(loading)")))} active={SceneManager.GetActiveScene().name}");
            Note($"island objects: plots={Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).Length} animals={string.Join(",", Object.FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).Select(x => x.AnimalId))} counters={Object.FindObjectsByType<IslandShopCounter>(FindObjectsSortMode.None).Length} spots={Object.FindObjectsByType<IslandWordSpot>(FindObjectsSortMode.None).Length}");
            WriteLog("island-perf.txt");
            // Walk test: every key place is reachable on foot from the station along the paths (capsule moves, colliders block).
            var body = _player.GetComponent<CharacterController>();
            Vector3 O = new Vector3(-1200f, 0f, 0f);
            Vector3 store = Object.FindFirstObjectByType<IslandShopCounter>().transform.position;
            Vector3 plot1 = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).First(p => p.Number == 1).transform.position;
            Vector3 cow = Object.FindObjectsByType<IslandAnimal>(FindObjectsSortMode.None).First(a => a.AnimalId == "cow").transform.position;
            Vector3 view = Object.FindObjectsByType<IslandWordSpot>(FindObjectsSortMode.None).OrderByDescending(w => w.transform.position.z).First().transform.position;
            var routes = new Dictionary<string, Vector3[]>
            {
                ["store"] = new[] { O + new Vector3(0f, 0f, -18f), store + Vector3.left * 1.2f },
                ["farm"] = new[] { O + new Vector3(0f, 0f, -4f), O + new Vector3(plot1.x - O.x, 0f, -4f), plot1 },
                ["animal pen"] = new[] { O + new Vector3(0f, 0f, -4f), O + new Vector3(18f, 0f, -4f), O + new Vector3(18f, 0f, 1f), cow },
                ["lookout"] = new[] { O + new Vector3(0f, 0f, -4f), O + new Vector3(0f, 0f, view.z - O.z - 1.5f) },
            };
            Vector3 home = _player.transform.position;
            foreach (var route in routes)
            {
                Teleport(home, 0f);
                yield return new WaitForSecondsRealtime(0.2f);
                Vector3 goal = route.Value.Last();
                float best = float.MaxValue;
                int leg = 0;
                for (float t = 0f; t < 30f && leg < route.Value.Length; t += Time.unscaledDeltaTime)
                {
                    Vector3 to = route.Value[leg] - _player.transform.position; to.y = 0f;
                    Vector3 toGoal = goal - _player.transform.position; toGoal.y = 0f;
                    best = Mathf.Min(best, toGoal.magnitude);
                    if (to.magnitude < (leg == route.Value.Length - 1 ? 2.2f : 0.6f)) { leg++; continue; }
                    body.Move(to.normalized * 6f * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
                    yield return null;
                }
                Note($"walk to {route.Key}: closest {best:0.0} m, ended at {_player.transform.position}");
                Assert.Less(best, 2.6f, $"{route.Key} must be reachable on foot from the station.");
            }

            // Frame time over 4 s with the camera looking across the island.
            Teleport(home + new Vector3(0f, 0f, 12f), 0f);
            yield return new WaitForSecondsRealtime(1f);
            var samples = new List<float>();
            for (float t = 0f; t < 4f; t += Time.unscaledDeltaTime) { samples.Add(Time.unscaledDeltaTime * 1000f); yield return null; }
            samples.Sort();
            int renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(r => r.enabled && r.gameObject.activeInHierarchy && r.gameObject.scene.name == WorldLocationCatalog.MidoriIslandScene);
            string stats = $"frames={samples.Count} avg={samples.Average():0.0}ms p95={samples[(int)(samples.Count * 0.95f)]:0.0}ms max={samples.Last():0.0}ms islandRenderers={renderers}";
#if UNITY_EDITOR
            stats += $" batches={UnityEditor.UnityStats.batches} setPass={UnityEditor.UnityStats.setPassCalls} tris={UnityEditor.UnityStats.triangles}";
#endif
            Note("perf (editor, batchmode): " + stats);
            Capture("30_perf_view");
            WriteLog("island-perf.txt");
        }

        // ─────────── helpers ───────────

        private IEnumerator WaitReady(float timeout)
        {
            for (float t = 0f; t < timeout; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                _flow = GameServices.TryGet(out SceneFlowController f) ? f : Object.FindFirstObjectByType<SceneFlowController>();
                if (_player != null && _flow != null && !_flow.IsLoading && t > 2f) yield break;
                yield return null;
            }
        }

        private IEnumerator WaitZone(string scene, float timeout = 30f)
        {
            for (float t = 0f; t < timeout; t += Time.unscaledDeltaTime)
            {
                if (SceneManager.GetActiveScene().name == scene && _flow != null && !_flow.IsLoading) { yield return new WaitForSecondsRealtime(0.4f); yield break; }
                yield return null;
            }
            Assert.Fail($"Timed out waiting for {scene} (active: {SceneManager.GetActiveScene().name}).");
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

        /// <summary>Puts the player <paramref name="distance"/> m south of a target, facing it, and waits for the detector.</summary>
        private IEnumerator StandAt(Transform target, float distance)
        {
            Vector3 p = target.position + Vector3.back * distance;
            Teleport(new Vector3(p.x, target.root.position.y + 0.3f, p.z), 0f);
            yield return new WaitForSecondsRealtime(0.5f);
            var detector = _player.GetComponent<InteractionDetector>();
            Note($"at {target.name}: current={detector?.CurrentInteractable}");
        }

        private void Teleport(Vector3 position, float yaw)
        {
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.SetOrbit(yaw, 22f, 6.5f);
        }

        private float FloorGap(out string floorName)
        {
            Vector3 bottom = _player.transform.position;
            floorName = "(none)";
            var hits = Physics.RaycastAll(bottom + Vector3.up * 0.5f, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.transform.root != _player.transform.root).OrderBy(h => h.distance).ToArray();
            if (hits.Length == 0) return 99f;
            floorName = hits[0].collider.name;
            return bottom.y - hits[0].point.y;
        }

        private static string Error(string message) => message;

        private static RectTransform Window(string name)
        {
            var go = GameObject.Find(name);
            Assert.NotNull(go, $"Window {name} must be open.");
            return (RectTransform)go.transform;
        }

        private void Click(string window, string buttonName)
        {
            var button = Window(window).GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == buttonName);
            Assert.NotNull(button, $"Button {buttonName} in {window}");
            Assert.IsTrue(button.interactable);
            button.onClick.Invoke();
            Note($"click {window}/{buttonName}");
        }

        private void ClickText(string window, string label)
        {
            var button = Window(window).GetComponentsInChildren<Button>().FirstOrDefault(b => b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains(label) == true);
            Assert.NotNull(button, $"Button '{label}' in {window}; buttons: " + string.Join(" | ", Window(window).GetComponentsInChildren<Button>().Select(x => x.name + ":" + x.GetComponentInChildren<TextMeshProUGUI>()?.text)) + "; texts: " + string.Join(" | ", Window(window).GetComponentsInChildren<TextMeshProUGUI>().Select(x => x.text)));
            button.onClick.Invoke();
            var feedback = Window(window).GetComponentsInChildren<TextMeshProUGUI>().Select(t => t.text).LastOrDefault(t => !string.IsNullOrEmpty(t));
            Note($"click {window}/'{label}' → {feedback}");
        }

        private void Note(string line) { _log.Add(line); Debug.Log("[IslandTest] " + line); }

        private void WriteLog(string file = "island-flow.txt") => File.WriteAllLines(Path.Combine(FolderPath(), file), _log);

        private static string FolderPath()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + Folder));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static void CaptureOverview(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var t = camera.transform; Vector3 p = t.position; Quaternion r = t.rotation;
            var follow = camera.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (follow != null) follow.enabled = false;
            t.SetPositionAndRotation(new Vector3(-1200f, 38f, -58f), Quaternion.Euler(36f, 0f, 0f));
            Capture(name, 1600, 900, ui: false);
            t.SetPositionAndRotation(p, r);
            if (follow != null) follow.enabled = true;
        }

        private static void CaptureWorld(string name, Transform target)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var t = camera.transform; Vector3 p = t.position; Quaternion r = t.rotation;
            var follow = camera.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (follow != null) follow.enabled = false;
            t.position = target.position + new Vector3(0f, 2.6f, -4.2f);
            t.LookAt(target.position + Vector3.up * 0.4f);
            Capture(name, 1600, 900, ui: false);
            t.SetPositionAndRotation(p, r);
            if (follow != null) follow.enabled = true;
        }

        private static void CaptureSizes(string name)
        {
            Capture(name + "_1920x1080", 1920, 1080);
            Capture(name + "_1366x768", 1366, 768);
            Capture(name + "_1024x600", 1024, 600);
        }

        private static void Capture(string name, int width = 1600, int height = 900, bool ui = true)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var canvases = ui ? Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray() : new Canvas[0];
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
            camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(FolderPath(), name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); target.Release(); Object.Destroy(target);
        }
    }
}
