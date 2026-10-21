using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NihongoLife.MiniGames;
using NihongoLife.Player;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Every Game Center machine, played for real in 50_GameCenter: Word Shooter, Order Rush, Kana Rain, UFO Catcher
    /// and Basket Count start from their machine, take right and wrong answers through the game's own input path,
    /// finish with tickets, EXP and mastery submitted. Each game is shown in Vietnamese, English and Japanese and no
    /// Vietnamese may appear on screen in the other two. Captures: Bao_Cao/arcade/&lt;game&gt;_&lt;lang&gt;.jpg
    /// </summary>
    public class ArcadeGamesPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/arcade"));
        private static readonly Regex Vietnamese = new Regex("[ăâđêôơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹĂÂĐÊÔƠƯ]");

        [UnityTest, Timeout(600000)]
        public IEnumerator Arcade_AllNewGames_PlayAndSubmit_InThreeLanguages()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.GameCenterScene);
            PlayerController player = null;
            for (float t = 0f; t < 30f; t += Time.unscaledDeltaTime)
            {
                player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null && !StandaloneZoneBootstrap.IsBooting && SceneManager.GetActiveScene().name == WorldLocationCatalog.GameCenterScene && t > 3f) break;
                yield return null;
            }
            Assert.NotNull(player);
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(1f);
            var launchers = Object.FindObjectsByType<MiniGameLauncher>(FindObjectsSortMode.None);
            var report = new System.Text.StringBuilder();
            try
            {
                foreach (string id in new[] { "word_shooter", "order_rush", "kana_rain", "ufo_catcher", "basket_count" })
                {
                    var launcher = launchers.FirstOrDefault(l => l.Definition != null && l.Definition.id == id);
                    Assert.NotNull(launcher, id + " machine is in the Game Center");
                    Assert.IsTrue(launcher.Definition.playable, id + " is playable");
                    foreach (var lang in new[] { GameLanguage.English, GameLanguage.Japanese, GameLanguage.Vietnamese })
                    {
                        Loc.Set(lang);
                        int ticketsBefore = PrizeCounter.Tickets;
                        var controller = MiniGameController.GetOrCreate();
                        Assert.IsTrue(controller.Launch(launcher, player.gameObject), id + " launches");
                        var game = (ArcadeGame)controller.CurrentGame;
                        Assert.NotNull(game);
                        yield return null;
                        var set = launcher.Definition.contentSets.FirstOrDefault(s => s != null && s.pairs.Count >= 4 && s.AvailableInCurrentLanguage
                            && (id != "kana_rain" || s.type == KanaPairType.HiraganaRomaji));
                        game.StartRound(set);
                        yield return null;
                        game.SkipCountdown();
                        yield return new WaitForSecondsRealtime(0.6f);
                        yield return Play(game, id);
                        yield return new WaitForSecondsRealtime(0.4f);
                        Capture($"{id}_{Loc.Code}");
                        if (lang != GameLanguage.Vietnamese) AssertOneLanguage(controller, $"{id} ({Loc.Code}) playing");
                        Assert.Greater(game.CorrectCount, 0, id + " counted right answers");
                        Assert.Greater(game.WrongCount, 0, id + " counted a wrong answer");
                        game.EndRound();
                        for (float t = 0f; t < 5f && !controller.IsShowingResult; t += Time.unscaledDeltaTime) yield return null;
                        Assert.IsTrue(controller.IsShowingResult, id + " shows its result");
                        var result = controller.LastResult;
                        Assert.IsTrue(result.completed && result.submittedToScoring, id + " result is submitted");
                        Assert.Greater(result.learningTargetIds.Count, 0, id + " reports learning targets");
                        Assert.Greater(PrizeCounter.Tickets, ticketsBefore, id + " pays tickets");
                        yield return new WaitForSecondsRealtime(0.3f);
                        Capture($"{id}_{Loc.Code}_result");
                        if (lang != GameLanguage.Vietnamese) AssertOneLanguage(controller, $"{id} ({Loc.Code}) result");
                        report.AppendLine($"{id} {Loc.Code}: score {result.score}, {result.correctCount} right / {result.incorrectCount} wrong, {result.tickets} tickets, stars {result.stars}");
                        controller.Close();
                        yield return new WaitForSecondsRealtime(0.3f);
                        Assert.IsFalse(player.InputLocked, id + " gives control back");
                    }
                }
            }
            finally { Loc.Set(GameLanguage.Vietnamese); }
            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, "report.txt"), report.ToString());
        }

        private static IEnumerator Play(ArcadeGame game, string id)
        {
            switch (game)
            {
                case WordShooterGame shooter:
                    for (int i = 0; i < 3; i++) { shooter.ShootTarget(); yield return null; }
                    shooter.ShootWrong();
                    break;
                case OrderRushGame rush:
                    for (int i = 0; i < 2; i++) { rush.FillTrayCorrectly(); rush.Serve(); yield return null; }
                    rush.Add(OrderRushGame.Menu.First(d => !rush.Order.ContainsKey(d.Id)).Id);
                    rush.Serve();
                    rush.FillTrayCorrectly();
                    break;
                case KanaRainGame rain:
                    for (int i = 0; i < 3; i++) { var drop = rain.EnsureDrop(); rain.Type(drop.Pair.b); yield return null; }
                    rain.Type("qqq");
                    // A kana that reaches the ground costs a life.
                    var falling = rain.EnsureDrop();
                    falling.Rect.anchoredPosition = new Vector2(falling.Rect.anchoredPosition.x, 30f);
                    yield return null;
                    rain.EnsureDrop();
                    break;
                case UfoCatcherGame ufo:
                    for (int i = 0; i < 2; i++)
                    {
                        ufo.AimAt(ufo.Target); ufo.DropClaw();
                        for (float t = 0f; t < 3f && ufo.Dropping; t += Time.unscaledDeltaTime) { ufo.AimAt(ufo.Target); yield return null; }
                    }
                    var other = ufo.AnyOther();
                    ufo.AimAt(other); ufo.DropClaw();
                    for (float t = 0f; t < 3f && ufo.Dropping; t += Time.unscaledDeltaTime) { ufo.AimAt(other); yield return null; }
                    break;
                case BasketCountGame basket:
                    for (int i = 0; i < 3; i++)
                    {
                        basket.Shoot(basket.RightHoop);
                        for (float t = 0f; t < 2f && basket.Throwing; t += Time.unscaledDeltaTime) yield return null;
                    }
                    basket.Shoot(basket.WrongHoop);
                    for (float t = 0f; t < 2f && basket.Throwing; t += Time.unscaledDeltaTime) yield return null;
                    break;
            }
        }

        private static void AssertOneLanguage(MiniGameController controller, string where)
        {
            var leaks = controller.GetComponentsInChildren<TMP_Text>(false)
                .Concat(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t => t.canvas != null && t.canvas.name == "MiniGameCanvas" && t.gameObject.activeInHierarchy))
                .Where(t => t != null && Vietnamese.IsMatch(t.text)).Select(t => $"{t.name}: '{t.text}'").Distinct().ToList();
            Assert.IsEmpty(leaks, $"{where}: Vietnamese text on screen in {Loc.Code}");
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var signs = Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None).Where(t => t.enabled).ToArray();
            foreach (var s in signs) s.enabled = false;
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old;
            camera.targetTexture = null;
            target.Release();
            Object.Destroy(target); Object.Destroy(tex);
            foreach (var c in canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            foreach (var s in signs) if (s != null) s.enabled = true;
        }
    }
}
