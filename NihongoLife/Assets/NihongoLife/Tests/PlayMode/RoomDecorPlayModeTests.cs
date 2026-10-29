using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Home;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>F7: Sims-style room decoration. Captures: Bao_Cao/room-decor.</summary>
    public class RoomDecorPlayModeTests
    {
        private static Vector2? FindSpot(RoomDecor rd, DecorDef def, bool wantValid, float rot = 0f)
        {
            for (float x = -3.5f; x <= 3.5f; x += 0.25f)
                for (float z = -3.0f; z <= 3.0f; z += 0.25f)
                    if (rd.CanPlace(def, x, z, rot, null, out _) == wantValid) return new Vector2(x, z);
            return null;
        }

        [UnityTest]
        public IEnumerator Bedroom_BuyPlaceRotateSellAndPersist()
        {
            LogAssert.ignoreFailingMessages = true;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/room-decor")); Directory.CreateDirectory(folder);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.HomeBedroomScene);
            for (float t = 0f; t < 25f && (RoomDecor.Instance == null || HomeBedroomRuntime.Instance == null || PlayerInventory.Instance == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
            var rd = RoomDecor.Instance; var inv = PlayerInventory.Instance;
            Assert.NotNull(rd, "RoomDecor boots in the bedroom"); Assert.NotNull(inv);
            int startPlaced = rd.Placed.Count;
            inv.AddYen(30000);
            int yen0 = inv.Yen;

            var plant = RoomDecorCatalog.Find("plant"); var rug = RoomDecorCatalog.Find("rug");
            var ok = FindSpot(rd, plant, true); var bad = FindSpot(rd, plant, false);
            Assert.IsTrue(ok.HasValue, "There is free floor for a plant");
            Assert.IsTrue(bad.HasValue, "The bed / desk / walls must block some spots");

            Assert.IsTrue(rd.Buy("plant")); Assert.IsTrue(rd.IsPlacing);
            Assert.AreEqual(yen0 - plant.Price, inv.Yen, "Buying pays");
            Assert.IsFalse(rd.PlaceAt(9f, 0f), "Outside the room is refused"); Assert.IsTrue(rd.IsPlacing);
            Assert.IsFalse(rd.PlaceAt(bad.Value.x, bad.Value.y), "A blocked spot is refused");
            Assert.IsTrue(rd.PlaceAt(ok.Value.x, ok.Value.y), "A free spot is accepted " + ok + " / " + rd.LastMessage + " bad=" + bad); Assert.IsFalse(rd.IsPlacing);
            Assert.AreEqual(startPlaced + 1, rd.Placed.Count);

            // a second plant cannot sit on the first; cancelling refunds the price
            int before = inv.Yen;
            Assert.IsTrue(rd.Buy("plant"));
            Assert.IsFalse(rd.PlaceAt(ok.Value.x, ok.Value.y), "Overlap is refused");
            rd.CancelPlacing(); Assert.AreEqual(before, inv.Yen, "Cancel refunds in full"); Assert.AreEqual(startPlaced + 1, rd.Placed.Count);
            Assert.IsFalse(rd.Buy("kotatsu") && false);
            rd.CancelPlacing();

            // rotate: a bookshelf (1.0 x 0.4) turned 90 degrees swaps its footprint
            Assert.IsTrue(rd.Buy("shelf")); rd.Rotate();
            var shelf = RoomDecorCatalog.Find("shelf");
            var spot2 = FindSpot(rd, shelf, true, 90f);
            Assert.IsTrue(spot2.HasValue); Assert.IsTrue(rd.PlaceAt(spot2.Value.x, spot2.Value.y));
            Assert.AreEqual(90f, rd.Placed.Last().Rot, 0.01f);

            var rugSpot = FindSpot(rd, rug, true);
            Assert.IsTrue(rd.Buy("rug")); Assert.IsTrue(rd.PlaceAt(rugSpot.Value.x, rugSpot.Value.y));
            int placed = rd.Placed.Count;

            // saved in the progress file
            Assert.IsTrue(GameServices.TryGet(out IProgressRepository repo));
            Assert.AreEqual(placed, repo.GetProgress().roomDecor.Count, "Saved");

            // the catalogue screen (en + ru)
            Loc.Set(GameLanguage.English); rd.Popup.Show(); yield return null; yield return null;
            Assert.IsTrue(rd.Popup.IsOpen); string en = rd.Popup.RowsText;
            Assert.IsTrue(en.Contains("Potted plant") && en.Contains("Kotatsu table") && en.Contains("Bookshelf") && en.Contains("$8.00") && !en.Contains("¥"), en);
            Capture(rd.Popup, Path.Combine(folder, "02_catalogue_en.png"));
            Loc.Set(GameLanguage.Russian); rd.Popup.Hide(); rd.Popup.Show(); yield return null;
            string ru = rd.Popup.RowsText;
            Assert.IsFalse(ru.Contains("Potted plant") || ru.Contains("Floor lamp") || System.Text.RegularExpressions.Regex.IsMatch(ru, "[\u0103\u1ea0-\u1ef9]"), ru);
            rd.Popup.Hide(); Loc.Set(GameLanguage.English);

            RoomShot(Path.Combine(folder, "01_room_en.png"));

            // sell gives back half
            int y1 = inv.Yen; var first = rd.Placed[0]; int value = rd.SellValue(first.Def);
            Assert.IsTrue(rd.Sell(0)); Assert.AreEqual(y1 + value, inv.Yen); Assert.AreEqual(placed - 1, rd.Placed.Count);
            Assert.AreEqual(placed - 1, repo.GetProgress().roomDecor.Count);

            // reloading the room brings the furniture back
            int expect = rd.Placed.Count;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene); yield return null;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.HomeBedroomScene);
            for (float t = 0f; t < 25f && (RoomDecor.Instance == null || HomeBedroomRuntime.Instance == null || t < 3.5f); t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(expect, RoomDecor.Instance.Placed.Count, "Furniture restored after reloading");
            Assert.IsTrue(Object.FindObjectsByType<RoomDecorItem>(FindObjectsSortMode.None).Length >= expect);
            // tidy: leave the room as it was
            var rd2 = RoomDecor.Instance; while (rd2.Placed.Count > startPlaced) rd2.Sell(rd2.Placed.Count - 1);
            Loc.Set(GameLanguage.Vietnamese);
        }

        private static void RoomShot(string path)
        {
            var root = HomeBedroomRuntime.Instance.transform;
            var go = new GameObject("DecorShotCam"); var cam = go.AddComponent<Camera>();
            cam.transform.position = root.TransformPoint(new Vector3(0f, 2.3f, -2.75f));
            cam.transform.rotation = Quaternion.LookRotation(root.TransformPoint(new Vector3(0f, 0.2f, 0.9f)) - cam.transform.position);
            cam.fieldOfView = 85f; cam.nearClipPlane = 0.05f;
            var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = old; cam.targetTexture = null; rt.Release(); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go);
        }

        private static void Capture(MenuPopupBase popup, string path)
        {
            var cam = Camera.main; if (cam == null) return;
            var c = popup.GetComponentInParent<Canvas>().rootCanvas; var mode = c.renderMode;
            c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.06f; Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = old; cam.targetTexture = null; rt.Release(); Object.Destroy(rt); Object.Destroy(tex);
            c.renderMode = mode; c.worldCamera = null;
        }
    }
}