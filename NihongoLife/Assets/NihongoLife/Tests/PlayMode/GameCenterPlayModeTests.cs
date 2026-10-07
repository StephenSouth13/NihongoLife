using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.MiniGames;
using NihongoLife.Player;
using NihongoLife.Scoring;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Game Center vertical slice, played the way a player does it: city → neon entrance → 50_GameCenter →
    /// walk to the Kana Match cabinet → play a full round (one deliberate mistake) → result reaches
    /// ScoringManager / LearningMastery / PlayerStatus → back to third person → exit to the same doorway.
    /// Any runtime exception or error log fails the test. Captures: Bao_Cao/gamecenter-regression.
    /// </summary>
    public class GameCenterPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameCenter_KanaMatchVerticalSlice()
        {
            // 1) The outside world still works.
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var dm = DialogueManager.Instance;
            Assert.NotNull(player); Assert.NotNull(flow); Assert.NotNull(Object.FindFirstObjectByType<HUDUI>());
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }

            var portal = GameObject.Find("AdditiveZonePortals/GameCenterPortal").GetComponent<ScenePortal>();
            var citySpawn = GameObject.Find("AdditiveZonePortals/Spawn_" + WorldLocationCatalog.CityGameCenterReturn).transform;
            Teleport(player, citySpawn.position + Vector3.forward * 4f, 180f);
            yield return new WaitForSecondsRealtime(1.2f);
            Capture("01_city_entrance");
            Teleport(player, citySpawn.position, 180f);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreSame(portal, player.GetComponent<InteractionDetector>().CurrentInteractable as ScenePortal, "The neon doorway must offer F · Vào Game Center.");

            // 2–4) Enter, Remy spawns at the entrance, the follow camera is in the room.
            portal.Interact(player.gameObject);
            yield return null;
            yield return WaitUntil(() => !flow.IsLoading, 30f, "Game Center transition timed out.");
            Assert.AreEqual(WorldLocationCatalog.GameCenterScene, SceneManager.GetActiveScene().name);
            var spawn = Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None).First(s => s.Id == WorldLocationCatalog.GameCenterEntrance);
            Assert.Less(Vector3.Distance(player.transform.position, spawn.transform.position), 0.6f, "Remy must spawn at the Game Center entrance.");
            yield return new WaitForSecondsRealtime(1.5f);
            var follow = Camera.main.GetComponent<ThirdPersonCameraController>();
            Assert.NotNull(follow); Assert.IsTrue(follow.enabled);
            Assert.Less(Vector3.Distance(Camera.main.transform.position, player.transform.position), 9f, "Camera must follow Remy inside.");
            Assert.IsTrue(SceneManager.GetSceneByName(WorldLocationCatalog.CityScene).GetRootGameObjects().Where(r => r.GetComponent<SceneZoneVisibility>() != null).All(r => !r.activeSelf), "City geometry hidden while inside.");
            Capture("02_arcade_spawn");

            // 5) Walk (real CharacterController movement) from the door to the Kana Match cabinet.
            var launcher = Object.FindObjectsByType<MiniGameLauncher>(FindObjectsSortMode.None).First(l => l.Definition.id == "kana_match");
            var body = player.GetComponent<CharacterController>();
            Vector3 start = player.transform.position;
            Vector3 goal = launcher.transform.position; goal.y = start.y;
            float walkUntil = Time.realtimeSinceStartup + 8f;
            while (Vector3.Distance(Flat(player.transform.position), Flat(goal)) > 0.5f && Time.realtimeSinceStartup < walkUntil)
            {
                Vector3 dir = (Flat(goal) - Flat(player.transform.position)).normalized;
                player.transform.rotation = Quaternion.LookRotation(dir);
                body.Move((dir * 3.2f + Vector3.down * 2f) * Time.deltaTime);
                yield return null;
            }
            Assert.Greater(Vector3.Distance(start, player.transform.position), 4f, "Remy must be able to walk across the arcade.");
            Assert.Less(Mathf.Abs(player.transform.position.y - start.y), 0.3f, "Walking keeps Remy on the floor.");
            Capture("03_walk_to_cabinet");
            var cabinetFacing = launcher.ViewPoint.position - launcher.transform.position; cabinetFacing.y = 0f;
            player.transform.rotation = Quaternion.LookRotation(-cabinetFacing.normalized);
            yield return new WaitForSecondsRealtime(0.6f);

            // 6–7) Interact: movement locks, the camera frames the machine.
            Assert.AreSame(launcher, player.GetComponent<InteractionDetector>().CurrentInteractable as MiniGameLauncher, "F at the cabinet must offer Kana Match.");
            Vector3 beforeGame = player.transform.position;
            launcher.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.8f);
            var controller = MiniGameController.Instance;
            Assert.NotNull(controller); Assert.IsTrue(controller.IsRunning);
            Assert.IsTrue(player.InputLocked, "Movement must lock while playing.");
            Assert.IsFalse(follow.enabled, "Camera switches to mini-game mode.");
            var game = (KanaMatchGame)controller.CurrentGame;
            Assert.AreEqual(KanaMatchGame.Phase.ChoosingSet, game.State);
            Capture("04_set_picker");

            // 8) Play a full hiragana round with exactly one mistake.
            var hiragana = launcher.Definition.contentSets.First(s => s.type == KanaPairType.HiraganaRomaji);
            game.StartRound(hiragana);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(16, game.Cards.Count, "8 pairs → 16 cards.");
            Capture("05_board");
            var cards = game.Cards.ToList();
            int a = 0, b = cards.FindIndex(c => c.PairIndex != cards[0].PairIndex);
            game.Pick(a); yield return null; game.Pick(b);
            yield return new WaitForSecondsRealtime(0.45f);
            Capture("06_mismatch");
            yield return WaitUntil(() => !game.IsBusy, 5f, "Mismatch must resolve.");
            Assert.IsFalse(cards[a].FaceUp || cards[b].FaceUp, "Wrong cards flip back.");
            for (int pair = 0; pair < cards.Count / 2; pair++)
            {
                var both = Enumerable.Range(0, cards.Count).Where(i => cards[i].PairIndex == pair).ToArray();
                game.Pick(both[0]); yield return null; game.Pick(both[1]);
                yield return WaitUntil(() => !game.IsBusy, 5f, "Match must resolve.");
                if (pair == 3) Capture("07_matching");
            }
            yield return WaitUntil(() => controller.IsShowingResult, 5f, "Completing the board must show the result.");

            // 9–10) Result produced and handed to the shared systems.
            var result = controller.LastResult;
            Assert.IsTrue(result.completed);
            Assert.AreEqual(8, result.correctCount);
            Assert.AreEqual(1, result.incorrectCount);
            Assert.AreEqual(1, result.mistakes.Count);
            Assert.AreEqual(8f / 9f, result.accuracy, 0.001f);
            Assert.Greater(result.score, 0);
            Assert.AreEqual(8, result.learningTargetIds.Count);
            Assert.AreEqual(6, result.masteredTargetIds.Count, "The two pairs in the mistake are not mastered yet.");
            Assert.IsTrue(result.submittedToScoring, "Result must reach ScoringManager.");
            Assert.IsTrue(ScoringManager.Instance.Events.Any(e => e.sourceId == "minigame.kana_match" && e.category == "Vocabulary"));
            Assert.IsTrue(ScoringManager.Instance.Events.Any(e => e.sourceId == "minigame.kana_match" && e.category == "ResponseAccuracy"));
            if (result.submittedToMastery)
                Assert.Greater(LearningMasteryManager.Instance.GetMastery(result.masteredTargetIds[0]), 50f, "Mastered kana gain mastery.");
            Capture("08_result");

            // 11) Back to normal third-person play at the same spot.
            controller.Close();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsFalse(controller.IsRunning);
            Assert.IsFalse(player.InputLocked, "PlayerController restored.");
            Assert.IsTrue(follow.enabled, "Follow camera restored.");
            Assert.Less(Vector3.Distance(beforeGame, player.transform.position), 0.05f, "Player keeps the position they played from.");
            Capture("09_back_to_world");

            // 12) Exit returns to the matching outdoor doorway.
            var exit = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).First(p => p.gameObject.scene.name == WorldLocationCatalog.GameCenterScene);
            exit.Interact(player.gameObject);
            yield return null;
            yield return WaitUntil(() => !flow.IsLoading, 30f, "Exit transition timed out.");
            Assert.AreEqual(WorldLocationCatalog.CityScene, SceneManager.GetActiveScene().name);
            Assert.Less(Vector3.Distance(player.transform.position, citySpawn.position), 0.6f, "Exit must land at the Game Center doorway outside.");
            yield return new WaitForSecondsRealtime(1f);
            Capture("10_back_outside");
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds, string message)
        {
            float timeout = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(condition(), message);
        }

        private static void Teleport(PlayerController player, Vector3 position, float yaw)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            Object.FindFirstObjectByType<ThirdPersonCameraController>()?.SetOrbit(yaw, 16f, 5f);
        }

        private static void Capture(string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/gamecenter-regression"));
            Directory.CreateDirectory(folder);
            var camera = Camera.main;
            if (camera == null) return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(x => x.isRootCanvas && x.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(x => x.sortingOrder).ToArray();
            var target = new RenderTexture(1600, 900, 24);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
            Canvas.ForceUpdateCanvases();
            var oldTarget = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            var oldActive = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            Object.Destroy(texture);
            target.Release();
            Object.Destroy(target);
        }
    }
}
