using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Island;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Island decorating and visiting, in the real 60_MidoriIsland: buy decorations in the Midori store (Decor tab) →
    /// decorating mode → ghost refused on the plaza, outside the island and on top of another piece → place, rotate,
    /// move and store pieces → the player bumps into solid pieces → the layout survives a respawn and goes into the
    /// showcase → visit a friend's island (showcase from FetchOverride): their pieces stand, nothing can be used, a heart
    /// can be left → leave → the own island is back. Captures: Bao_Cao/island-decor.
    /// </summary>
    public class IslandDecorPlayModeTests
    {
        private const string Folder = "island-decor";
        private PlayerController _player;
        private readonly List<string> _log = new();

        [TearDown]
        public void TearDown()
        {
            IslandShowcaseService.FetchOverride = null;
            if (IslandVisit.Active) IslandVisit.End();
        }

        [UnityTest]
        public IEnumerator Island_DecorateAndVisit()
        {
            LogAssert.ignoreFailingMessages = true; // offline: showcase publishing / likes log warnings only
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.MidoriIslandScene);
            yield return WaitIsland();
            var runtime = IslandDecorRuntime.Instance;
            Assert.NotNull(runtime, "IslandDecorRuntime lives in 60_MidoriIsland.");
            runtime.PointerDriven = false;

            // Clean slate for this test.
            var record = IslandState.Record;
            record.decor.Clear(); record.decorStock.Clear();
            runtime.Respawn();
            int walletBefore = PlayerInventory.Instance.Yen;
            PlayerInventory.Instance.AddYen(20000, Loc.Triple("Kiểm thử", "Test", "テスト"));

            // ── Shop: Decor tab ──
            IslandUI.OpenShopAt("decor", "decor_tree");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.NotNull(GameObject.Find("Tab_decor"), "The store has a Decor tab.");
            Assert.GreaterOrEqual(IslandUI.Entries("decor").Count, 20, "The Decor tab lists the catalog decorations.");
            Capture("01_shop_decor");
            int yen = PlayerInventory.Instance.Yen;
            IslandUI.SetShopQuantity(2);
            Assert.AreEqual(IslandEconomy.Result.Ok, IslandUI.ConfirmShop());
            Assert.AreEqual(2, IslandDecor.Stock("tree"));
            Assert.AreEqual(yen - 2 * IslandCatalog.Load().Decor("tree").price, PlayerInventory.Instance.Yen, "Paid from the shared wallet.");
            Assert.IsTrue(IslandState.Record.words.Contains("decor:tree"), "Buying teaches the word.");
            foreach (var id in new[] { "bench", "flower_bed", "streetlight", "well", "rock" })
                Assert.AreEqual(IslandEconomy.Result.Ok, IslandDecor.Buy(id, 1), id);
            yield return new WaitForSecondsRealtime(0.2f);
            Capture("02_shop_decor_bought");
            IslandUI.CloseShop();
            yield return null;

            // ── Decorating mode ──
            runtime.BeginEdit();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsTrue(runtime.Editing);
            Assert.IsTrue(_player.InputLocked, "The player stands still while decorating.");
            Capture("03_edit_stock_bar");

            runtime.Select("tree");
            Assert.AreEqual("tree", runtime.Selected);
            Assert.IsFalse(runtime.AimAt(0f, -4f), "The plaza stays free.");
            Assert.IsFalse(runtime.AimAt(0f, 10f), "The main path stays free.");
            Assert.IsFalse(runtime.AimAt(49f, 0f), "Not on the beach edge.");
            runtime.FocusOn(0f, -4f, 22f);
            runtime.AimAt(0f, -4f);
            yield return new WaitForSecondsRealtime(0.2f);
            Capture("04_ghost_blocked_on_plaza");

            var spots = FreeSpots(runtime, 8).ToList();
            Assert.GreaterOrEqual(spots.Count, 6, "There is room to decorate.");
            Note("spots: " + string.Join(" ", spots.Select(s => $"({s.x:0},{s.y:0})")));

            runtime.FocusOn(spots[0].x + 6f, spots[0].y + 4f, 24f);
            Assert.IsTrue(runtime.AimAt(spots[0].x, spots[0].y));
            runtime.Rotate(45f);
            yield return new WaitForSecondsRealtime(0.2f);
            Capture("05_ghost_free");
            Assert.IsTrue(runtime.Confirm(), "Placing on a free spot works.");
            Assert.AreEqual(1, IslandDecor.Placed.Count);
            Assert.AreEqual(1, IslandDecor.Stock("tree"));
            Assert.AreEqual(45f, IslandDecor.Placed[0].yaw, 0.01f);
            Physics.SyncTransforms();
            Assert.IsFalse(runtime.AimAt(spots[0].x, spots[0].y), "Two pieces cannot stand in one place.");
            Assert.IsTrue(runtime.AimAt(spots[1].x, spots[1].y));
            Assert.IsTrue(runtime.Confirm());
            Assert.AreEqual(0, IslandDecor.Stock("tree"));
            Assert.IsNull(runtime.Selected, "The last one placed empties the hand.");

            string[] rest = { "bench", "flower_bed", "streetlight", "well", "rock" };
            for (int i = 0; i < rest.Length; i++)
            {
                runtime.Select(rest[i]);
                Physics.SyncTransforms();
                var spot = FreeSpots(runtime, 1).FirstOrDefault();
                Assert.IsTrue(runtime.AimAt(spot.x, spot.y), rest[i]);
                Assert.IsTrue(runtime.Confirm(), rest[i]);
                yield return null;
            }
            Assert.AreEqual(7, IslandDecor.Placed.Count);
            Assert.AreEqual(7, runtime.Pieces.Count);
            Physics.SyncTransforms();

            // Move the first tree, store the rock.
            var tree = runtime.Pieces.First(p => p.Def.id == "tree");
            runtime.PickUp(tree);
            var moveTo = FreeSpots(runtime, 1).First();
            Assert.IsTrue(runtime.AimAt(moveTo.x, moveTo.y));
            Assert.IsTrue(runtime.Confirm());
            Assert.AreEqual(moveTo.x, tree.Placement.x, 0.6f);
            var rock = runtime.Pieces.First(p => p.Def.id == "rock");
            runtime.PickUp(rock);
            runtime.FocusOn(rock.Placement.x, rock.Placement.z, 20f);
            yield return new WaitForSecondsRealtime(0.2f);
            Capture("06_moving_piece");
            runtime.StoreMoving();
            Assert.AreEqual(6, IslandDecor.Placed.Count);
            Assert.AreEqual(1, IslandDecor.Stock("rock"), "Stored pieces go back to the stock.");
            yield return new WaitForSecondsRealtime(0.3f);
            runtime.FocusOn(spots[0].x + 10f, spots[0].y + 4f, 40f);
            yield return null;
            Capture("07_edit_after_placing");
            runtime.EndEdit();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsFalse(_player.InputLocked);

            // ── The world: looks and collision ──
            var well = runtime.Pieces.First(p => p.Def.id == "well");
            CaptureWorld("08_world_well", well.transform, 7f);
            CaptureWorld("09_world_tree", tree.transform, 9f);
            CaptureOverview("10_overview_decorated");
            yield return Bump(well.transform);
            var flower = runtime.Pieces.First(p => p.Def.id == "flower_bed");
            yield return WalkThrough(flower.transform);

            // ── Persistence + showcase ──
            var before = IslandDecor.Placed.Select(p => (p.id, p.x, p.z, p.yaw)).ToList();
            var json = JsonUtility.ToJson(IslandState.Record);
            var copy = JsonUtility.FromJson<IslandRecord>(json);
            CollectionAssert.AreEqual(before, copy.decor.Select(p => (p.id, p.x, p.z, p.yaw)).ToList(), "The layout is saved with the island.");
            Assert.AreEqual(1, copy.decorStock.First(s => s.id == "rock").count);
            runtime.Respawn();
            Assert.AreEqual(6, runtime.Pieces.Count, "Respawn rebuilds every piece.");
            var showcase = IslandShowcase.Of("me", "Me", IslandState.Record);
            Assert.AreEqual(6, showcase.Layout.decor.Count, "The showcase carries the layout.");
            int ownCount = IslandDecor.Placed.Count;

            // ── Visit a friend ──
            var friend = new IslandShowcase
            {
                user_id = "friend-hana", display_name = "Hana", likes = 4,
                layout_json = JsonUtility.ToJson(new IslandLayout
                {
                    decor = new List<DecorPlacement>
                    {
                        new DecorPlacement { id = "tree_large", x = spots[2].x, z = spots[2].y, yaw = 0f },
                        new DecorPlacement { id = "tent", x = spots[3].x, z = spots[3].y, yaw = 90f },
                        new DecorPlacement { id = "campfire", x = spots[3].x + 3f, z = spots[3].y, yaw = 0f },
                    },
                    harvested = 12, fished = 3, words = 40,
                }),
            };
            IslandShowcaseService.FetchOverride = id => id == "friend-hana" ? friend : null;
            string visitError = "pending";
            IslandVisit.Visit("friend-hana", "Hana", e => visitError = e);
            Assert.IsNull(visitError);
            Assert.IsTrue(IslandVisit.Active);
            yield return new WaitForSecondsRealtime(0.8f); // already on Midori Island: the layout swaps in place
            Assert.AreEqual(WorldLocationCatalog.MidoriIslandScene, SceneManager.GetActiveScene().name);
            Assert.AreEqual(3, runtime.Pieces.Count, "The friend's pieces stand on the island.");
            Assert.NotNull(GameObject.Find("VisitBar"), "The visit bar shows whose island it is.");
            Assert.IsNull(GameObject.Find("IslandToolbar"), "No farming toolbar on a friend's island.");
            Assert.IsNull(IslandDecor.Place("bench", 0f, 20f, 0f), "A friend's island cannot be changed.");
            Assert.AreEqual(IslandEconomy.Result.Unknown, IslandDecor.Buy("bench", 1));
            var plot = Object.FindFirstObjectByType<FarmPlot>();
            plot.Interact(_player.gameObject);
            Assert.IsNull(GameObject.Find("FarmCard"), "The friend's fields cannot be used.");
            runtime.BeginEdit();
            Assert.IsFalse(runtime.Editing, "No decorating on a friend's island.");
            yield return new WaitForSecondsRealtime(0.6f);
            Capture("11_visit_friend");
            IslandVisit.Like();
            Assert.IsTrue(IslandVisit.Liked);
            Assert.AreEqual(5, IslandVisit.Current.likes, "The heart counts (locally when offline).");
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("12_visit_liked");
            var hanaTent = runtime.Pieces.First(p => p.Def.id == "tent");
            CaptureWorld("13_visit_world", hanaTent.transform, 9f);

            IslandVisit.End();
            Assert.IsFalse(IslandVisit.Active);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(ownCount, IslandState.Record.decor.Count, "The own island is back after the visit.");
            Assert.AreEqual(ownCount, runtime.Pieces.Count, "The own pieces stand again.");
            Assert.NotNull(GameObject.Find("IslandToolbar"), "The farming toolbar is back.");
            Assert.IsNull(GameObject.Find("VisitBar"));
            Capture("14_back_home");

            // Leave the save as it was (layout and wallet: later tests count on the real balance).
            IslandState.Record.decor.Clear(); IslandState.Record.decorStock.Clear(); IslandState.Save();
            RestoreWallet(walletBefore);
            WriteLog();
        }

        /// <summary>
        /// The Japanese-style pieces (TASK-O: torii, stone lantern, sakura, bamboo, koi pond, mailbox, bench, chōchin):
        /// each one is in the Decor tab, places on the island, stands on the ground at its catalog size, and is captured
        /// up close (Bao_Cao/island-decor/jp_*.png) so a wrong model shows at a glance.
        /// </summary>
        [UnityTest]
        public IEnumerator Island_JapaneseDecor()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.MidoriIslandScene);
            yield return WaitIsland();
            var runtime = IslandDecorRuntime.Instance;
            runtime.PointerDriven = false;
            var record = IslandState.Record;
            record.decor.Clear(); record.decorStock.Clear();
            runtime.Respawn();
            int walletBefore = PlayerInventory.Instance.Yen;
            PlayerInventory.Instance.AddYen(50000, Loc.Triple("Kiểm thử", "Test", "テスト"));
            var japanese = IslandCatalog.Load().decor.Where(d => d.id.StartsWith("jp_")).ToList();
            Assert.GreaterOrEqual(japanese.Count, 8, "The Japanese set is in the catalog.");
            var shop = IslandUI.Entries("decor").Select(e => e.ItemId).ToList();
            runtime.BeginEdit();
            yield return null;
            foreach (var def in japanese)
            {
                Assert.Contains(def.ItemId, shop, def.id + " is sold in the Decor tab.");
                Assert.NotNull(IslandModelLibrary.Instance.Find("decor:" + def.id), def.id + " has a built prefab.");
                Assert.AreEqual(IslandEconomy.Result.Ok, IslandDecor.Buy(def.id, 1), def.id);
                runtime.Select(def.id);
                Physics.SyncTransforms();
                var spot = FreeSpots(runtime, 1).FirstOrDefault();
                Assert.IsTrue(runtime.AimAt(spot.x, spot.y), def.id + " finds a free spot.");
                Assert.IsTrue(runtime.Confirm(), def.id + " places.");
                yield return null;
            }
            runtime.EndEdit();
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (var def in japanese)
            {
                var piece = runtime.Pieces.First(p => p.Def.id == def.id);
                var bounds = new Bounds(piece.transform.position, Vector3.zero);
                foreach (var r in piece.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                Note($"{def.id}: size {bounds.size.x:0.00}×{bounds.size.y:0.00}×{bounds.size.z:0.00} (catalog height {def.height}), bottom {bounds.min.y:0.00}");
                Assert.Less(Mathf.Abs(bounds.min.y), 0.15f, def.id + " stands on the ground.");
                Assert.AreEqual(def.height, bounds.size.y, def.height * 0.35f + 0.1f, def.id + " has its catalog height.");
                CaptureWorld("jp_" + def.id.Substring(3), piece.transform, Mathf.Clamp(def.height * 2.2f, 3.5f, 11f));
            }
            IslandState.Record.decor.Clear(); IslandState.Record.decorStock.Clear(); IslandState.Save();
            RestoreWallet(walletBefore);
            WriteLog();
        }

        private static void RestoreWallet(int yen)
        {
            var bag = PlayerInventory.Instance;
            if (bag == null) return;
            int diff = bag.Yen - yen;
            if (diff > 0) bag.SpendYen(diff, Loc.Triple("Kiểm thử", "Test", "テスト"));
            else if (diff < 0) bag.AddYen(-diff, Loc.Triple("Kiểm thử", "Test", "テスト"));
        }

        /// <summary>Free island-local spots for the selected piece, spread out over the island.</summary>
        private static IEnumerable<Vector2> FreeSpots(IslandDecorRuntime runtime, int count)
        {
            int found = 0;
            var picked = new List<Vector2>();
            for (float z = -28f; z <= 30f && found < count; z += 4f)
                for (float x = -36f; x <= 36f && found < count; x += 4f)
                {
                    var v = new Vector2(x, z);
                    if (picked.Any(p => (p - v).magnitude < 5f)) continue;
                    if (!runtime.AimAt(x, z)) continue;
                    picked.Add(v);
                    found++;
                }
            return picked;
        }

        private IEnumerator Bump(Transform piece)
        {
            var body = _player.GetComponent<CharacterController>();
            Vector3 start = piece.position + Vector3.back * 3f + Vector3.up * 0.1f;
            Teleport(start);
            yield return new WaitForFixedUpdate();
            var solid = piece.GetComponentsInChildren<Collider>().First(c => !c.isTrigger);
            bool blocked = false, inside = false;
            for (int i = 0; i < 60; i++)
            {
                var flags = body.Move(Vector3.forward * 0.1f);
                blocked |= (flags & CollisionFlags.Sides) != 0;
                Vector3 p = _player.transform.position;
                Vector3 chest = p + Vector3.up * 1f;
                inside |= (solid.ClosestPoint(chest) - chest).sqrMagnitude < 1e-6f; // ClosestPoint returns the point itself when inside
                yield return null;
            }
            Note($"bump {piece.name}: blocked={blocked} entered={inside} end={_player.transform.position}");
            Assert.IsTrue(blocked, $"Walking into {piece.name} hits it.");
            Assert.IsFalse(inside, $"The player never walks through {piece.name}.");
        }

        private IEnumerator WalkThrough(Transform piece)
        {
            var body = _player.GetComponent<CharacterController>();
            Teleport(piece.position + Vector3.back * 2f + Vector3.up * 0.1f);
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < 40; i++) { body.Move(Vector3.forward * 0.1f); yield return null; }
            Note($"walk through {piece.name}: now {_player.transform.position.z - piece.position.z:0.00} m past it");
            Assert.Greater(_player.transform.position.z, piece.position.z + 1f, "Flowers do not block walking.");
        }

        private void Teleport(Vector3 p)
        {
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.SetPositionAndRotation(p, Quaternion.identity);
            body.enabled = true;
            Physics.SyncTransforms();
        }

        private IEnumerator WaitIsland(bool reloaded = false)
        {
            var old = IslandDecorRuntime.Instance;
            for (float t = 0f; t < 40f; t += Time.unscaledDeltaTime)
            {
                _player = Object.FindFirstObjectByType<PlayerController>();
                var flow = GameServices.TryGet(out SceneFlowController f) ? f : null;
                bool ready = _player != null && flow != null && !flow.IsLoading && !StandaloneZoneBootstrap.IsBooting && IslandRuntime.Active != null
                    && IslandDecorRuntime.Instance != null && (!reloaded || IslandDecorRuntime.Instance != old)
                    && SceneManager.GetActiveScene().name == WorldLocationCatalog.MidoriIslandScene;
                if (ready && t > 2f) break;
                yield return null;
            }
            Assert.AreEqual(WorldLocationCatalog.MidoriIslandScene, SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(1.5f);
        }

        private void Note(string line) { _log.Add(line); Debug.Log("[DecorTest] " + line); }
        private void WriteLog() => File.WriteAllLines(Path.Combine(FolderPath(), "decor-flow.txt"), _log);

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
            Capture(name, ui: false);
            t.SetPositionAndRotation(p, r);
            if (follow != null) follow.enabled = true;
        }

        private static void CaptureWorld(string name, Transform target, float distance)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var t = camera.transform; Vector3 p = t.position; Quaternion r = t.rotation;
            var follow = camera.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (follow != null) follow.enabled = false;
            t.position = target.position + new Vector3(distance * 0.35f, distance * 0.45f, -distance);
            t.LookAt(target.position + Vector3.up * 1f);
            Capture(name, ui: false);
            t.SetPositionAndRotation(p, r);
            if (follow != null) follow.enabled = true;
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
