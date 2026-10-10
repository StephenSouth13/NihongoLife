using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Island;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.Save;
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
    /// Midori Island work animations and fishing, played through the real game (captures: Bao_Cao/fishing-regression).
    /// · Farm work: each timed action plays its NL_Humanoid work state and puts the right tool in the right hand.
    /// · Fishing: buy a rod → cast → wait → bite → pull → the fish lands in the bag exactly once → sell for ¥, quest,
    ///   save; pulling early, missing the bite, Esc and having no rod give nothing.
    /// </summary>
    public class FishingFarmPlayModeTests
    {
        private const string Folder = "fishing-regression";
        private PlayerController _player;
        private CharacterAnimationController _anim;
        private Animator _animator;
        private readonly List<string> _log = new();

        [SetUp]
        public void SetUp() => _log.Clear();

        [TearDown]
        public void TearDown()
        {
            TimedAction.SpeedScale = 1f;
            IslandFishing.BiteDelayOverride = null;
            IslandFishing.ForcedFishId = null;
            IslandFishing.BiteWindow = 1.8f;
            IslandState.ClockOffset = System.TimeSpan.Zero;
        }

        // ─────────────────────────────── farm work animations ───────────────────────────────

        [UnityTest]
        public IEnumerator Island_FarmWorkPlaysAnimationsWithToolInHand()
        {
            yield return LoadIsland();
            var bag = PlayerInventory.Instance;
            var plots = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).OrderBy(p => p.Number).Take(2).ToList();
            foreach (var p in plots) { var r = p.Record; r.tilled = false; r.cropId = null; r.stage = 0; r.watered = false; p.Refresh(true); }
            Take(bag, "tool_hoe"); Take(bag, "tool_shovel");
            if (bag.GetItemQuantity("tool_watering_can") == 0) IslandEconomy.Give("tool_watering_can", 1);
            if (bag.GetItemQuantity("seed_carrot") < 2) IslandEconomy.Give("seed_carrot", 2);

            // No hoe or shovel: tilling cannot be done (no bare-hand way); the card points to the store.
            yield return StandAt(plots[0].transform, 1.9f);
            IslandUI.OpenFarm(plots[0]);
            yield return null;
            Assert.NotNull(GameObject.Find("NeedTool"), "The farm card explains the missing tool");
            Assert.IsFalse(FarmButton("Xới"), "No till button without a tool");
            Assert.IsNotNull(plots[0].Till(), "Tilling refused without a tool");
            Assert.IsFalse(plots[0].Record.tilled);
            Capture("41_no_tool");
            IslandUI.CloseFarm();

            // Hoe: in the right hand while tilling; each finished use wears it by one.
            IslandEconomy.Give("tool_hoe", 1);
            int max = IslandTools.Max("tool_hoe");
            Assert.AreEqual(max, IslandTools.Left("tool_hoe"), "A new hoe is at full durability");
            yield return StandAt(plots[1].transform, 1.9f);
            IslandUI.OpenFarm(plots[1]);
            ClickFarm("Xới đất");
            yield return AssertWork("Work_Hoe", true, "42_till_with_hoe");
            yield return WaitWork();
            yield return AssertIdle();
            Assert.AreEqual(max - 1, IslandTools.Left("tool_hoe"), "One use worn");

            // Cancelling (Esc) wears nothing and tills nothing.
            IslandUI.OpenFarm(plots[0]);
            ClickFarm("Xới đất");
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(UiModalStack.CloseTop(), "Esc stops the work");
            yield return null;
            Assert.IsFalse(plots[0].Record.tilled, "Cancelled work does nothing");
            Assert.AreEqual(max - 1, IslandTools.Left("tool_hoe"), "Cancelled work wears nothing");

            // A worn-out hoe breaks on its last use and leaves the bag.
            IslandState.Record.toolWear.First(w => w.itemId == "tool_hoe").usesLeft = 1;
            TimedAction.SpeedScale = 0.1f;
            IslandUI.OpenFarm(plots[0]);
            ClickFarm("Xới đất");
            yield return WaitWork();
            TimedAction.SpeedScale = 1f;
            Assert.IsTrue(plots[0].Record.tilled, "The last use still counts");
            Assert.AreEqual(0, bag.GetItemQuantity("tool_hoe"), "Broken hoe removed from the bag");
            IslandUI.OpenFarm(plots[0]);
            IslandUI.CloseFarm();

            // Plant, water (the can wears too), grow, harvest on plot 2.
            IslandUI.OpenFarm(plots[1]);
            ClickNamed("Plant_carrot");
            yield return AssertWork("Work_Plant", false, "43_plant");
            yield return WaitWork();
            int can = IslandTools.Left("tool_watering_can");
            IslandUI.OpenFarm(plots[1]);
            ClickFarm("Tưới nước");
            yield return AssertWork("Work_Water", false, "44_water");
            yield return WaitWork();
            yield return AssertIdle();
            Assert.AreEqual(can - 1, IslandTools.Left("tool_watering_can"), "Watering wears the can");

            plots.Reverse(); // the grown plot is now plots[0]
            var crop = plots[0].Crop;
            for (int stage = 0; stage < 3; stage++)
            {
                IslandState.ClockOffset += System.TimeSpan.FromSeconds(crop.secondsPerStage + 1);
                yield return new WaitForSecondsRealtime(0.5f);
                if (plots[0].CurrentPhase == FarmPlot.Phase.NeedsWater)
                {
                    TimedAction.SpeedScale = 0.1f;
                    IslandUI.OpenFarm(plots[0]); ClickFarm("Tưới nước"); yield return WaitWork();
                    TimedAction.SpeedScale = 1f;
                }
            }
            Assert.AreEqual(FarmPlot.Phase.Ready, plots[0].CurrentPhase);
            IslandUI.OpenFarm(plots[0]);
            ClickFarm("Thu hoạch");
            yield return AssertWork("Work_Harvest", false, "45_harvest");
            yield return WaitWork();
            IslandUI.CloseFarm();
            yield return AssertIdle();
            WriteLog("farm_animations.txt");
        }

        // ─────────────────────────────── fishing ───────────────────────────────

        [UnityTest]
        public IEnumerator Island_FishingCastCatchSellOnceAndSave()
        {
            yield return LoadIsland();
            var bag = PlayerInventory.Instance;
            var record = IslandState.Record;
            var catalog = IslandCatalog.Load();
            Take(bag, IslandFishing.RodId);
            foreach (var f in catalog.fish) Take(bag, f.id);
            if (bag.Yen < 2000) bag.AddYen(2000 - bag.Yen);
            var progress = Repository().GetProgress();
            progress.quests.RemoveAll(q => q.questId == "farm_first_catch");
            Assert.IsNull(QuestService.Accept("farm_first_catch"), "Fishing quest can be accepted");

            var spot = Object.FindFirstObjectByType<FishingSpot>();
            Assert.NotNull(spot, "The pier has a fishing spot");
            Assert.NotNull(spot.castTarget, "Cast target on the water");

            // No rod: refused, nothing starts.
            yield return StandAt(spot.transform, 0.2f);
            Assert.IsNotNull(IslandFishing.Begin(spot, _player.gameObject), "No rod → refused");
            Assert.IsFalse(IslandFishing.Active);

            // Buy the rod at the Midori Store (shared wallet).
            int yen0 = bag.Yen;
            IslandUI.OpenShop();
            IslandUI.SelectShopTab("agriculture");
            IslandUI.SelectShopItem(IslandFishing.RodId);
            Assert.AreEqual(IslandEconomy.Result.Ok, IslandUI.ConfirmShop());
            Assert.AreEqual(yen0 - catalog.Tool(IslandFishing.RodId).price, bag.Yen, "Rod paid once");
            IslandUI.CloseShop();
            yield return null;

            // A full round: cast → wait → bite → pull → fish shown → bag.
            IslandFishing.BiteDelayOverride = 1.2f;
            IslandFishing.ForcedFishId = "fish_tai";
            int fished0 = record.fished;
            yield return StandAt(spot.transform, 0.2f);
            yield return Press(GameInputId.Interact);
            Assert.IsTrue(IslandFishing.Active, "F at the spot starts fishing");
            Assert.IsTrue(_player.InputLocked, "The player stands still while fishing");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual("Fish_Cast", _anim.CurrentWork);
            AssertHeldInRightHand("rod");
            Capture("50_cast");
            CaptureFrom("50b_pier_overview", spot.transform.position + new Vector3(9f, 5f, -7f), spot.transform.position);
            yield return WaitPhase(IslandFishing.Phase.Waiting, 8f);
            Assert.AreEqual("Fish_Wait", _anim.CurrentWork);
            var floatGo = GameObject.Find("FishingFloat");
            Assert.NotNull(floatGo, "Float on the water");
            Assert.Less(Vector3.Distance(floatGo.transform.position, spot.castTarget.position), 0.6f, "The float lands at the cast target");
            Capture("51_waiting");
            yield return WaitPhase(IslandFishing.Phase.Bite, 6f);
            Capture("52_bite");
            yield return Press(GameInputId.Interact);
            Assert.AreEqual(IslandFishing.Phase.Reeling, IslandFishing.Current, "Pull during the bite reels the fish in");
            yield return new WaitForSeconds(0.6f);
            Capture("53_reel");
            yield return WaitPhase(IslandFishing.Phase.Showing, 5f);
            Assert.NotNull(GameObject.Find("CaughtFish_fish_tai"), "The caught fish model is shown");
            Capture("54_caught");
            // Hammering F / the button while the fish is shown never adds a second fish.
            for (int i = 0; i < 4; i++) { IslandFishing.Pull(); yield return null; }
            yield return WaitInactive(6f);
            Assert.AreEqual(1, bag.GetItemQuantity("fish_tai"), "Exactly one fish per catch");
            Assert.AreEqual(fished0 + 1, record.fished);
            Assert.AreEqual(1, QuestService.ObjectiveProgress("farm_first_catch", "fish"));
            Assert.IsFalse(_player.InputLocked, "Player free after the round");
            Assert.IsNull(_anim.CurrentWork, "Back to idle");
            Assert.IsNull(_anim.HeldProp, "Rod put away");

            // Pulling too early: nothing.
            yield return Round(early: true);
            Assert.AreEqual(1, bag.GetItemQuantity("fish_tai"), "Early pull gives nothing");
            // Missing the bite: nothing.
            IslandFishing.BiteWindow = 0.3f;
            yield return Round(miss: true);
            IslandFishing.BiteWindow = 1.8f;
            Assert.AreEqual(1, bag.GetItemQuantity("fish_tai"), "Missed bite gives nothing");
            // Esc while waiting: nothing, player released.
            yield return Round(escape: true);
            Assert.AreEqual(1, bag.GetItemQuantity("fish_tai"), "Esc gives nothing");
            Assert.IsFalse(_player.InputLocked);
            Assert.AreEqual(fished0 + 1, record.fished);

            // Second catch (rare tuna) completes the catch objective.
            IslandFishing.ForcedFishId = "fish_maguro";
            yield return Round();
            Assert.AreEqual(1, bag.GetItemQuantity("fish_maguro"));
            Assert.AreEqual(2, QuestService.ObjectiveProgress("farm_first_catch", "fish"));

            // Sell one fish at the store for its catalog price.
            int yen1 = bag.Yen;
            IslandUI.OpenShop();
            IslandUI.SelectShopTab("sell");
            Assert.IsTrue(IslandUI.Entries("sell").Any(e => e.ItemId == "fish_tai"), "Fish are listed in the Sell tab");
            IslandUI.SelectShopItem("fish_tai");
            Capture("55_sell_fish");
            Assert.AreEqual(IslandEconomy.Result.Ok, IslandUI.ConfirmShop());
            Assert.AreEqual(IslandEconomy.Result.NotOwned, IslandEconomy.Sell("fish_tai", 140, 1), "Cannot sell a fish twice");
            IslandUI.CloseShop();
            int reward = QuestService.Catalog.Quest("farm_first_catch").rewards.yen;
            Assert.AreEqual(yen1 + catalog.Fish("fish_tai").sellPrice + reward, bag.Yen, "Fish price + quest reward, once");
            Assert.AreEqual("completed", QuestService.State("farm_first_catch")?.status, "Fishing quest completed");

            // Saved with the progress.
            var reloaded = JsonUtility.FromJson<PlayerProgressDto>(JsonUtility.ToJson(Repository().GetProgress()));
            Assert.IsTrue(reloaded.inventory.Any(i => i.itemId == "fish_maguro"), "Caught fish saved in the bag");
            Assert.AreEqual(record.fished, reloaded.island.fished, "Catch count saved");
            Note($"fished={record.fished} yen={bag.Yen}");
            WriteLog("fishing.txt");
        }

        // ─────────── helpers ───────────

        private IEnumerator Round(bool early = false, bool miss = false, bool escape = false)
        {
            var spot = Object.FindFirstObjectByType<FishingSpot>();
            IslandFishing.BiteDelayOverride = 1f;
            yield return StandAt(spot.transform, 0.2f);
            Assert.IsNull(IslandFishing.Begin(spot, _player.gameObject));
            yield return WaitPhase(IslandFishing.Phase.Waiting, 8f);
            if (early) { IslandFishing.Pull(); Assert.IsFalse(IslandFishing.Active, "Early pull ends the round"); yield break; }
            if (escape) { Assert.IsTrue(UiModalStack.CloseTop(), "Esc closes fishing"); yield return null; Assert.IsFalse(IslandFishing.Active); yield break; }
            yield return WaitPhase(IslandFishing.Phase.Bite, 6f);
            if (miss) { yield return WaitInactive(4f); yield break; }
            IslandFishing.Pull();
            yield return WaitInactive(8f);
        }

        private IEnumerator LoadIsland()
        {
            TimedAction.SpeedScale = 1f;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.MidoriIslandScene);
            // Opening the island directly boots through the city first, then enters the island again: wait for that.
            for (float t = 0f; t < 45f; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                var flow = GameServices.TryGet(out SceneFlowController f) ? f : Object.FindFirstObjectByType<SceneFlowController>();
                if (_player != null && flow != null && !flow.IsLoading && !StandaloneZoneBootstrap.IsBooting && IslandRuntime.Active != null
                    && SceneManager.GetActiveScene().name == WorldLocationCatalog.MidoriIslandScene && Object.FindFirstObjectByType<FishingSpot>() != null && t > 2f) break;
                yield return null;
            }
            Assert.NotNull(_player, "Island player");
            var all = Object.FindObjectsByType<FishingSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsNotNull(Object.FindFirstObjectByType<FishingSpot>(),
                $"Fishing spot active (scene={SceneManager.GetActiveScene().name}, spots incl. inactive={all.Length}, inactive path={(all.Length > 0 ? HierarchyPath(all[0].transform) : "-")}, runtime={IslandRuntime.Active})");
            yield return new WaitForSecondsRealtime(1f);
            _anim = _player.GetComponentInChildren<CharacterAnimationController>();
            _animator = _player.GetComponentInChildren<Animator>();
            Assert.NotNull(_anim); Assert.NotNull(_animator);
            Assert.IsTrue(_animator.HasState(0, Animator.StringToHash("Fish_Cast")), "NL_Humanoid has the work/fishing states");
        }

        private IEnumerator AssertWork(string state, bool expectTool, string capture)
        {
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.SetOrbit(_player.transform.eulerAngles.y + 70f, 14f, 3.1f); // side view of the pose
            yield return new WaitForSeconds(Mathf.Min(1.0f, FarmActionTimesFor(state) * 0.7f));
            Assert.IsTrue(TimedAction.Busy, "Timed action running");
            Assert.AreEqual(state, _anim.CurrentWork);
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            var next = _animator.GetNextAnimatorStateInfo(0);
            string clips = string.Join(",", _animator.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name)) + " → " + string.Join(",", _animator.GetNextAnimatorClipInfo(0).Select(c => c.clip.name));
            Assert.IsTrue(info.IsName(state) || next.IsName(state), $"Animator plays {state} (now: {clips})");
            if (expectTool) AssertHeldInRightHand(state);
            else Assert.IsNull(_anim.HeldProp, "Nothing in the hand for " + state);
            Note($"{state}: tool={(_anim.HeldProp != null ? _anim.HeldProp.name : "none")}");
            Capture(capture);
        }

        private static string HierarchyPath(Transform t) => t == null ? "" : HierarchyPath(t.parent) + "/" + t.name + (t.gameObject.activeSelf ? "" : "(off)");

        private static float FarmActionTimesFor(string state) => state switch
        {
            "Work_Hoe" => FarmActionTimes.TillHoe, "Work_Plant" => FarmActionTimes.Plant, "Work_Water" => FarmActionTimes.Water,
            "Work_Harvest" => FarmActionTimes.Harvest, _ => 2f,
        };

        private void AssertHeldInRightHand(string what)
        {
            Assert.NotNull(_anim.HeldProp, "Tool in hand for " + what);
            var hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.AreEqual(hand, _anim.HeldProp.transform.parent, "Held by the right hand bone");
            Assert.Less(Vector3.Distance(_anim.HeldProp.transform.position, hand.position), 0.25f, "Grip at the palm");
        }

        private IEnumerator AssertIdle()
        {
            IslandUI.CloseFarm(); // the farm card itself keeps the player still
            yield return null;
            Assert.IsFalse(TimedAction.Busy);
            Assert.IsNull(_anim.CurrentWork, "Work pose ended");
            Assert.IsNull(_anim.HeldProp, "Tool put away");
            Assert.IsFalse(_player.InputLocked);
        }

        private static IEnumerator WaitPhase(IslandFishing.Phase phase, float timeout)
        {
            for (float t = 0f; t < timeout && IslandFishing.Current != phase; t += Time.deltaTime) yield return null;
            Assert.AreEqual(phase, IslandFishing.Current, "Fishing phase");
        }

        private static IEnumerator WaitInactive(float timeout)
        {
            for (float t = 0f; t < timeout && IslandFishing.Active; t += Time.deltaTime) yield return null;
            Assert.IsFalse(IslandFishing.Active, "Round finished");
            yield return null;
        }

        private static IEnumerator WaitWork()
        {
            yield return null;
            for (float t = 0f; t < 15f && TimedAction.Busy; t += Time.deltaTime) yield return null;
            yield return null;
        }

        private static void Take(PlayerInventory bag, string id) { int n = bag.GetItemQuantity(id); if (n > 0) bag.RemoveItem(id, n); }

        private static IProgressRepository Repository() { GameServices.TryGet(out IProgressRepository r); return r; }

        private IEnumerator Press(GameInputId id)
        {
            var input = GameInputService.GetOrCreate();
            yield return new WaitForFixedUpdate();
            input.SetMobileButton(id, true);
            yield return null;
            input.SetMobileButton(id, false);
            yield return null;
        }

        private IEnumerator StandAt(Transform target, float distance)
        {
            Vector3 f = target.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            f.Normalize();
            Vector3 p = target.position - f * distance;
            var facing = Quaternion.LookRotation(f);
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.SetPositionAndRotation(new Vector3(p.x, target.position.y > 0.5f ? target.position.y - 0.9f : target.position.y + 0.1f, p.z), facing);
            body.enabled = true;
            Physics.SyncTransforms();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.SetOrbit(facing.eulerAngles.y + 35f, 18f, 3.4f);
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private static void ClickNamed(string name)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.gameObject.activeInHierarchy && b.interactable);
            Assert.NotNull(button, "Button " + name);
            button.onClick.Invoke();
        }

        private static bool FarmButton(string label)
        {
            var card = GameObject.Find("FarmCard");
            return card != null && card.GetComponentsInChildren<Button>().Any(b => b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains(label) == true);
        }

        private static void ClickFarm(string label)
        {
            var card = GameObject.Find("FarmCard");
            Assert.NotNull(card, "Farm card open");
            var button = card.GetComponentsInChildren<Button>().FirstOrDefault(b => b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains(label) == true);
            Assert.NotNull(button, $"Button '{label}'");
            button.onClick.Invoke();
        }

        private void Note(string line) { _log.Add(line); Debug.Log("[FishingFarm] " + line); }
        private void WriteLog(string file) => File.WriteAllLines(Path.Combine(FolderPath(), file), _log);

        private static string FolderPath()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + Folder));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static void CaptureFrom(string name, Vector3 position, Vector3 lookAt)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var pos = camera.transform.position; var rot = camera.transform.rotation;
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
            Capture(name);
            camera.transform.SetPositionAndRotation(pos, rot);
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
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
            camera.Render();
            RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(FolderPath(), name + ".jpg"), tex.EncodeToJPG(88));
            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); target.Release(); Object.Destroy(target);
        }
    }
}
