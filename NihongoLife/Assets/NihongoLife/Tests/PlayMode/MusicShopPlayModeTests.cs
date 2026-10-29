using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Music;
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
    /// Hibiki Music &amp; Art (55_MusicShop) entered from the town like a player: every painting hangs in the gallery
    /// with its picture, a painting's card opens from the wall; a gig needs a learnt song and only a good show counts;
    /// a DJ set counts the right mixes; a painting finished at the easel counts for the commission; the owner pays.
    /// Captures: Bao_Cao/music-shop.
    /// </summary>
    public class MusicShopPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/music-shop"));
        private static readonly Vector3 O = new Vector3(1300f, 0f, 0f);
        private const string Song = "ode-to-joy";

        [UnityTest]
        public IEnumerator MusicShop_GalleryGigDjAndPainting()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var entrance = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).FirstOrDefault(p => p.name == "MusicShopPortal");
            Assert.IsNotNull(entrance, "the shop's door on Sakura-dōri");
            CaptureFrom("00_street_entrance", entrance.transform.position + new Vector3(-3.5f, 2.6f, 7.5f), entrance.transform.position + Vector3.up * 2f);

            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            flow.EnterZone(WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.MusicShopEntrance, "Hibiki");
            for (float t = 0f; t < 25f && (flow.IsLoading || SceneManager.GetActiveScene().name != WorldLocationCatalog.MusicShopScene); t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(WorldLocationCatalog.MusicShopScene, SceneManager.GetActiveScene().name, "inside the music shop");
            yield return new WaitForSecondsRealtime(1.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player);
            Assert.Less(Vector3.Distance(player.transform.position, O + new Vector3(0f, 0f, -6.6f)), 2f, "at the shop door");

            // The gallery: every painting hangs with its picture, and its card opens from the wall.
            var paintings = Object.FindObjectsByType<ArtworkSpot>(FindObjectsSortMode.None);
            Assert.AreEqual(MusicCatalog.Artworks.Length, paintings.Length, "every painting is hung");
            CollectionAssert.AreEquivalent(MusicCatalog.Artworks.Select(a => a.id).ToArray(), paintings.Select(p => p.ArtworkId).ToArray());
            foreach (var p in paintings)
            {
                var canvas = p.transform.parent.Find("Canvas").GetComponent<Renderer>();
                Assert.IsNotNull(canvas.sharedMaterial.GetTexture("_BaseMap"), p.ArtworkId + ": picture on the canvas");
                Assert.Greater(p.transform.position.z, O.z + 8f, p.ArtworkId + ": in the gallery");
            }
            CaptureFrom("01_shop_from_door", O + new Vector3(0f, 2.2f, -7.3f), O + new Vector3(0f, 1.2f, 2f));
            CaptureFrom("02_stage_dj", O + new Vector3(4f, 2.0f, -1.5f), O + new Vector3(8f, 1.0f, 6f));
            CaptureFrom("03_pianos_guitars", O + new Vector3(-2f, 2.0f, -2.5f), O + new Vector3(-8f, 1.2f, 5f));
            CaptureFrom("04_gallery_north", O + new Vector3(0f, 1.8f, 9.2f), O + new Vector3(0f, 1.6f, 24f));
            CaptureFrom("05_gallery_west", O + new Vector3(4f, 1.7f, 12f), O + new Vector3(-12f, 1.6f, 18f));
            CaptureFrom("06_gallery_wall_close", O + new Vector3(-2.5f, 1.7f, 20.5f), O + new Vector3(-2.5f, 1.6f, 24f));
            var wave = paintings.First(p => p.ArtworkId.Contains("Tsunami"));
            wave.Interact(player.gameObject);
            yield return null;
            Assert.AreEqual(wave.ArtworkId, MusicStudioUI.ShownPainting, "the painting's card");
            CaptureUi("07_painting_card");
            MusicStudioUI.Close();

            foreach (var q in QuestService.Active.Where(q => q.IsJob).ToList()) QuestService.Abandon(q.id);
            MusicStudioUI.ForgetSong(Song);
            try
            {
                // The grand piano: the player sits on the bench and the character plays a song; tempo and volume change live.
                var piano = Object.FindObjectsByType<MusicSpot>(FindObjectsSortMode.None).First(s => s.name == "Spot_Piano");
                piano.Interact(player.gameObject);
                yield return null;
                Assert.Less(Vector3.Distance(player.transform.position, piano.Seat), 0.3f, "seated at the piano");
                MusicStudioUI.AutoPlay(Song);
                Assert.IsTrue(MusicStudioUI.IsAutoPlaying);
                Assert.IsTrue(MusicStudioUI.Mini, "a small bar: the character stays in view");
                MusicStudioUI.Tempo = 1.3f;
                MusicStudioUI.Volume = 0.5f;
                Assert.AreEqual(1.3f, MusicStudioUI.Tempo, 0.001f);
                Assert.AreEqual(0.5f, MusicStudioUI.Volume, 0.001f);
                yield return new WaitForSecondsRealtime(2f);
                Assert.Greater(MusicStudioUI.AutoProgress, 0f, "the character is playing");
                var gestures = player.GetComponentInChildren<NihongoLife.Gestures.GestureLayer>();
                Assert.IsNotNull(gestures);
                Assert.AreEqual("piano_play", gestures.Current, "hands on the keys");
                CaptureUi("08_character_plays_piano");
                MusicStudioUI.Tempo = 1f;
                MusicStudioUI.Volume = 0.8f;
                MusicStudioUI.StopAuto();
                MusicStudioUI.Close();

                // A gig on the stage: the character performs; near the song's tempo the audience loves it, far from it not.
                Assert.IsNull(QuestService.Accept(MusicShop.GigQuest));
                var stage = Object.FindObjectsByType<MusicSpot>(FindObjectsSortMode.None).First(s => s.name == "Spot_Stage");
                stage.Interact(player.gameObject);
                yield return null;
                Assert.IsTrue(MusicStudioUI.OnStage);
                Assert.IsTrue(MusicStudioUI.Perform(Song));
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.AreEqual("guitar_strum", gestures.Current, "on stage with the guitar");
                CaptureUi("09_stage_show");
                MusicStudioUI.SkipToEnd();
                Assert.IsTrue(MusicStudioUI.LastResult.gig && MusicStudioUI.LastResult.good);
                Assert.AreEqual(1, QuestService.ObjectiveProgress(MusicShop.GigQuest, "perform"));
                Assert.AreEqual(1, MusicStudioUI.Fans);
                MusicStudioUI.Tempo = 1.6f;
                Assert.IsTrue(MusicStudioUI.Perform(Song));
                MusicStudioUI.SkipToEnd();
                Assert.IsFalse(MusicStudioUI.LastResult.good, "far too fast");
                Assert.AreEqual(1, QuestService.ObjectiveProgress(MusicShop.GigQuest, "perform"), "a rushed show does not count");
                MusicStudioUI.Tempo = 1f;
                Assert.IsTrue(MusicStudioUI.Perform(Song));
                MusicStudioUI.SkipToEnd();
                yield return null;
                CaptureUi("10_gig_done");
                MusicStudioUI.Close();
                Assert.IsTrue(QuestService.ReadyToReport(MusicShop.GigQuest));
                MusicShop.Use(MusicSpotKind.Counter);
                Assert.AreEqual("completed", QuestService.State(MusicShop.GigQuest).status, "Mr Hibiki pays the gig");

                // A DJ set: four right mixes.
                Assert.IsNull(QuestService.Accept(MusicShop.DjQuest));
                for (int i = 0; i < 4; i++)
                {
                    MusicShop.Use(MusicSpotKind.DjBooth);
                    yield return null;
                    Assert.IsTrue(JobQuizCard.IsOpen);
                    if (i == 0) CaptureUi("11_dj_mix");
                    int right = JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct);
                    Assert.GreaterOrEqual(right, 0);
                    JobQuizCard.Answer(right);
                    for (float t = 0f; t < 5f && JobQuizCard.IsOpen; t += Time.unscaledDeltaTime) yield return null;
                }
                Assert.AreEqual(4, QuestService.ObjectiveProgress(MusicShop.DjQuest, "mix"));
                MusicShop.TalkToOwner();
                Assert.AreEqual("completed", QuestService.State(MusicShop.DjQuest).status);

                // A commission: paint by numbers at the easel (one wrong colour is a slip).
                Assert.IsNull(QuestService.Accept(MusicShop.ArtQuest));
                MusicShop.Use(MusicSpotKind.Easel);
                yield return null;
                Assert.IsTrue(PaintingUI.IsOpen);
                Assert.Greater(PaintingUI.CellCount, 60);
                int wrong = PaintingUI.Target(0) == 0 ? 1 : 0;
                PaintingUI.Select(wrong);
                Assert.IsFalse(PaintingUI.Fill(0), "the wrong colour");
                Assert.AreEqual(1, PaintingUI.Mistakes);
                for (int i = 0; i < PaintingUI.CellCount / 2; i++) { PaintingUI.Select(PaintingUI.Target(i)); PaintingUI.Fill(i); }
                yield return null;
                CaptureUi("12_painting_half");
                for (int i = 0; i < PaintingUI.CellCount; i++) { PaintingUI.Select(PaintingUI.Target(i)); PaintingUI.Fill(i); }
                Assert.IsTrue(PaintingUI.Done);
                yield return null;
                CaptureUi("13_painting_done");
                PaintingUI.Close();
                Assert.AreEqual(1, QuestService.ObjectiveProgress(MusicShop.ArtQuest, "paint"));
                MusicShop.TalkToOwner();
                Assert.AreEqual("completed", QuestService.State(MusicShop.ArtQuest).status);
            }
            finally
            {
                MusicStudioUI.Close();
                PaintingUI.Close();
                MusicStudioUI.ForgetSong(Song);
            }
            // Let this test's feed lines (written in the test's language) run out before the next test reads the HUD.
            yield return new WaitForSecondsRealtime(8f);
        }

        private static void CaptureFrom(string name, Vector3 position, Vector3 lookAt)
        {
            var go = new GameObject("TestShot");
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.05f;
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
            Render(camera, name);
            Object.DestroyImmediate(go);
        }

        private static void CaptureUi(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Render(camera, name);
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Canvas.ForceUpdateCanvases();
        }

        private static void Render(Camera camera, string name)
        {
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            try { File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG()); }
            catch (IOException e) { Debug.LogWarning("Capture " + name + " skipped (the file is open elsewhere): " + e.Message); }
            camera.targetTexture = previous;
            RenderTexture.active = null;
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
