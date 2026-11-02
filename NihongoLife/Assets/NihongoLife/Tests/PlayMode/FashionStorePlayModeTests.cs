using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Fashion;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>Hibari Mode: try on, buy and wear a set; every character can wear every accessory; a full shift pays.
    /// Captures: Bao_Cao/fashion.</summary>
    public class FashionStorePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/fashion"));

        private static IEnumerator EnterStore()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            GameServices.TryGet(out SceneFlowController flow);
            Assert.NotNull(flow, "Scene flow");
            flow.EnterZone(WorldLocationCatalog.FashionStoreScene, WorldLocationCatalog.FashionStoreEntrance, WorldLocationCatalog.FashionStoreScene);
            for (float t = 0f; t < 20f && SceneManager.GetActiveScene().name != WorldLocationCatalog.FashionStoreScene; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(WorldLocationCatalog.FashionStoreScene, SceneManager.GetActiveScene().name);
        }

        private static void Shot(string name, Vector3? position = null, Vector3? target = null, int width = 1600, int height = 900)
        {
            Directory.CreateDirectory(Folder);
            var camera = Camera.main;
            Vector3 oldPosition = camera.transform.position;
            Quaternion oldRotation = camera.transform.rotation;
            var controller = camera.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (position.HasValue)
            {
                if (controller != null) controller.enabled = false;
                camera.transform.position = position.Value;
                camera.transform.LookAt(target ?? position.Value + Vector3.forward);
            }
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".jpg"), tex.EncodeToJPG(88));
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(rt);
            Object.Destroy(tex);
            if (position.HasValue)
            {
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                if (controller != null) controller.enabled = true;
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Shop_TryOnBuyAndWear_AndClosingRestores()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EnterStore();
            Vector3 o = FashionStoreOrigin.O;
            Shot("01_entrance", o + new Vector3(0f, 2.6f, -8.2f), o + new Vector3(0f, 1f, 4f));
            Shot("02_floor_west", o + new Vector3(3f, 2.4f, 8f), o + new Vector3(-8f, 0.8f, -2f));
            Shot("03_floor_east", o + new Vector3(-3f, 2.4f, -7f), o + new Vector3(9f, 0.8f, 6f));
            Shot("04_fitting_till", o + new Vector3(0f, 2.2f, 1f), o + new Vector3(-1f, 1.2f, 12f));

            var player = Object.FindFirstObjectByType<PlayerController>();
            var bag = PlayerInventory.Instance;
            Assert.NotNull(bag);
            GameServices.TryGet(out NihongoLife.Save.IProgressRepository repository);
            var progress = repository.GetProgress();
            progress.outfits.Clear();
            progress.outfit = string.Empty;
            bag.AddYen(60000);
            var spot = Object.FindObjectsByType<FashionSpot>(FindObjectsSortMode.None).First(s => s.Category == "office");
            spot.Interact(player.gameObject);
            yield return null;
            Assert.IsTrue(FashionShopUI.IsOpen, "The rail opens the shop");
            Assert.AreEqual("office", FashionShopUI.Category);

            FashionShopUI.Pick("office_navy");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual("office_navy", FashionService.ShownId, "Trying on shows the set");
            Assert.AreEqual(string.Empty, FashionService.WornId, "Trying on buys nothing");

            // Headphones sit on the crown and the brow, a straw hat on the crown only: nothing of the first set stays.
            FashionShopUI.Pick("street_neon");
            yield return new WaitForSecondsRealtime(0.8f);
            FashionShopUI.Pick("summer_beach");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(1, player.GetComponentsInChildren<Transform>(false).Count(t => t.name == CharacterLook.WearName), "Only the straw hat is on the head");
            FashionShopUI.Pick("office_navy");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsTrue(player.GetComponentsInChildren<Transform>(true).Any(t => t.name == CharacterLook.WearName), "The tie is on the character");
            Shot("05_try_on");

            int before = bag.Yen;
            FashionShopUI.Close();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(string.Empty, FashionService.ShownId, "Leaving the mirror takes the try-on off");
            Assert.IsFalse(player.GetComponentsInChildren<Transform>(true).Any(t => t.name == CharacterLook.WearName));

            FashionShopUI.Open("office");
            FashionShopUI.Purchase("office_navy");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(before - 18000, bag.Yen, "The set is paid for once");
            Assert.AreEqual("office_navy", FashionService.WornId, "Bought sets are worn");
            Assert.IsTrue(FashionService.Owns("office_navy"));
            FashionShopUI.Purchase("office_navy");
            Assert.AreEqual(before - 18000, bag.Yen, "An owned set costs nothing");
            FashionShopUI.Close();
            yield return null;
            Assert.AreEqual("office_navy", FashionService.ShownId, "The set stays on");

            int poor = bag.Yen;
            bag.SpendYen(poor);
            FashionShopUI.Open("festival");
            FashionShopUI.Purchase("festival_gold");
            Assert.IsFalse(FashionService.Owns("festival_gold"), "No money, no sale");
            FashionService.Wear(string.Empty);
            FashionShopUI.Close();
            bag.AddYen(poor);
        }

        /// <summary>Stepping out of a shop, nothing of the entrance sits in front of the camera.</summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator LeavingTheShop_TheCameraIsClearOfTheCanopy()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EnterStore();
            GameServices.TryGet(out SceneFlowController flow);
            flow.ExitZone(WorldLocationCatalog.CityScene, WorldLocationCatalog.CityFashionStoreReturn);
            for (float t = 0f; t < 20f && (flow.IsLoading || SceneManager.GetActiveScene().name != WorldLocationCatalog.CityScene); t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            var camera = Camera.main;
            var player = Object.FindFirstObjectByType<PlayerController>();
            Vector3 head = player.transform.position + Vector3.up * 1.2f;
            Assert.Greater(Vector3.Distance(camera.transform.position, head), 3f, "The camera is not squeezed against the entrance");
            var near = Physics.OverlapSphere(camera.transform.position, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Where(c => !c.transform.IsChildOf(player.transform)).Select(c => c.name).ToList();
            Assert.IsEmpty(near, "Nothing beside the camera");
            Assert.IsFalse(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(r => r.name.StartsWith("CanopyStripe") && r.bounds.Contains(camera.transform.position)), "Not inside a canopy");
            Shot("06_left_the_shop");
        }

        /// <summary>Every playable character in accessories that sit on the head and on the chest, for a look.</summary>
        [UnityTest, Timeout(180000)]
        public IEnumerator Every_Character_WearsTheSets()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EnterStore();
            var player = Object.FindFirstObjectByType<PlayerController>();
            string original = PlayableCharacterCatalog.GetSelected().Id;
            var outfits = new[] { "casual_mint", "street_neon", "office_navy", "summer_beach", "winter_camel", "street_bomber", "casual_knit", "sport_tennis" };
            var kids = new[] { "kid_haruto", "kid_yui", "kid_sota", "kid_hina", "kid_riku", "kid_mio", "kid_yuto", "kid_sakura" };
            Vector3 stand = FashionStoreOrigin.O + new Vector3(0f, 0.05f, -2f);
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(stand, Quaternion.identity);
            body.enabled = true;
            try
            {
                for (int i = 0; i < kids.Length; i++)
                {
                    PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(kids[i]));
                    FashionService.TryOn(outfits[i]);
                    yield return new WaitForSecondsRealtime(1f);
                    var head = player.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                    Vector3 at = head.position;
                    Shot($"10_{kids[i]}_{outfits[i]}", at + player.transform.forward * 2.2f + Vector3.up * 0.1f, at + Vector3.down * 0.45f, 700, 700);
                    Shot($"11_{kids[i]}_{outfits[i]}_side", at + player.transform.right * 1.6f + player.transform.forward * 0.6f, at + Vector3.down * 0.3f, 700, 700);
                }
            }
            finally
            {
                FashionService.EndTryOn();
                PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(original));
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Shift_RunsToTheEnd_AndPaysOnce()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EnterStore();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var bag = PlayerInventory.Instance;
            Assert.IsTrue(Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).Any(s => s.QuestId == "job_fashion_shift" && s.Kind == JobStationKind.Board), "The job board");
            Assert.IsNull(QuestService.Accept("job_fashion_shift"), "Shift accepted");
            yield return null;
            var stations = Object.FindObjectsByType<JobStation>(FindObjectsSortMode.None).Where(s => s.QuestId == "job_fashion_shift" && s.Kind != JobStationKind.Board).OrderBy(s => s.Kind).ToList();
            Assert.AreEqual(7, stations.Count, "Three tables, two customers, the fitting room, the till");
            yield return null;
            foreach (var station in stations)
            {
                Assert.IsTrue(station.IsInteractionAvailable, station.name);
                station.Interact(player.gameObject);
                if (station.Kind == JobStationKind.FashionFold)
                {
                    for (float t = 0f; t < 5f && TimedAction.Busy; t += Time.unscaledDeltaTime) yield return null;
                }
                else
                {
                    yield return null;
                    Shot("20_quiz_" + station.Kind);
                    int right = JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct);
                    Assert.GreaterOrEqual(right, 0, station.name + " has a right answer");
                    JobQuizCard.Answer(right);
                    for (float t = 0f; t < 3f && JobQuizCard.IsOpen; t += Time.unscaledDeltaTime) yield return null;
                }
                yield return null;
            }
            Assert.IsTrue(QuestService.ReadyToReport("job_fashion_shift"), "Every step done");
            int yen = bag.Yen;
            var manager = Object.FindFirstObjectByType<JobGiver>();
            manager.Interact(player.gameObject);
            yield return null;
            Assert.AreEqual("completed", QuestService.State("job_fashion_shift").status);
            Assert.AreEqual(yen + Mathf.RoundToInt(1900 * CareerService.RewardFactor("fashion")), bag.Yen, "Shift pay");
            Assert.IsNotNull(CareerIndustries.IndustryOf("fashion"), "The fashion field exists");
        }
    }

    public static class FashionStoreOrigin
    {
        public static readonly Vector3 O = new Vector3(1600f, 0f, 0f);
    }
}
