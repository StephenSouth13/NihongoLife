using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
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
    /// The second wave of arcade machines, played for real in 50_GameCenter: Word Whack, Echo Pads, Kana Snake, Kana
    /// Breaker, Dance Arrows, Tense Typer, Wheel Quiz and Air Hockey. Each starts from its own machine, takes right and
    /// wrong play through the game's own logic, finishes with tickets paid and a submitted result, in Vietnamese,
    /// English and Japanese (no Vietnamese on screen in the other two). Captures: Bao_Cao/arcade/&lt;game&gt;_&lt;lang&gt;.jpg,
    /// counts: Bao_Cao/arcade/report_more.txt
    /// </summary>
    public class ArcadeMoreGamesPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/arcade"));
        private static readonly Regex Vietnamese = new Regex("[\u00C0-\u00C3\u00C8-\u00CA\u00CC\u00CD\u00D2-\u00D5\u00D9\u00DA\u00DD\u00E0-\u00E3\u00E8-\u00EA\u00EC\u00ED\u00F2-\u00F5\u00F9\u00FA\u00FD\u0102\u0103\u0110\u0111\u0128\u0129\u0168\u0169\u01A0\u01A1\u01AF\u01B0\u1EA0-\u1EF9]");
        private static readonly string[] NewIds = { "word_whack", "echo_pads", "kana_snake", "kana_breaker", "dance_arrows", "tense_typer", "wheel_quiz", "air_hockey" };

        [UnityTest, Timeout(900000)]
        public IEnumerator Arcade_EightMoreGames_PlayAndSubmit_InThreeLanguages()
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

            var launchers = Object.FindObjectsByType<MiniGameLauncher>(FindObjectsSortMode.None).Where(l => l.Definition != null).ToList();
            var machines = GameObject.Find("Machines");
            int machineCount = machines != null ? machines.transform.childCount : -1;
            var report = new StringBuilder();
            report.AppendLine($"Game Center: {launchers.Count} playable machines with a launcher, {launchers.Select(l => l.Definition.id).Distinct().Count()} distinct games, {machineCount} objects under Machines");
            Assert.GreaterOrEqual(launchers.Count, 14, "6 old + 8 new machines carry a game");
            Assert.AreEqual(launchers.Count, launchers.Select(l => l.Definition.id).Distinct().Count(), "one game per machine");
            try
            {
                foreach (string id in NewIds)
                {
                    var launcher = launchers.FirstOrDefault(l => l.Definition.id == id);
                    Assert.NotNull(launcher, id + " machine is in the Game Center");
                    foreach (var lang in new[] { GameLanguage.English, GameLanguage.Japanese, GameLanguage.Vietnamese })
                    {
                        Loc.Set(lang);
                        int ticketsBefore = PrizeCounter.Tickets;
                        var controller = MiniGameController.GetOrCreate();
                        Assert.IsTrue(controller.Launch(launcher, player.gameObject), id + " launches");
                        var game = (ArcadeGame)controller.CurrentGame;
                        Assert.NotNull(game);
                        yield return null;
                        var set = launcher.Definition.contentSets.FirstOrDefault(s => s != null && s.pairs.Count >= 4 && s.AvailableInCurrentLanguage && s.pairs.Count(p => !string.IsNullOrEmpty(p.Clue(s.type))) >= 4
                            && ((id != "kana_snake" && id != "kana_breaker") || s.type == KanaPairType.HiraganaRomaji));
                        game.StartRound(set);
                        yield return null;
                        game.SkipCountdown();
                        yield return new WaitForSecondsRealtime(0.5f);
                        yield return Play(game, id);
                        yield return new WaitForSecondsRealtime(0.3f);
                        Capture($"{id}_{Loc.Code}");
                        if (lang != GameLanguage.Vietnamese) AssertOneLanguage(controller, $"{id} ({Loc.Code}) playing");
                        Assert.Greater(game.CorrectCount, 0, id + " counted right play");
                        Assert.Greater(game.WrongCount, 0, id + " counted a mistake");
                        game.EndRound();
                        for (float t = 0f; t < 5f && !controller.IsShowingResult; t += Time.unscaledDeltaTime) yield return null;
                        Assert.IsTrue(controller.IsShowingResult, id + " shows its result");
                        var result = controller.LastResult;
                        Assert.IsTrue(result.completed && result.submittedToScoring, id + " result is submitted");
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
            File.WriteAllText(Path.Combine(Out, "report_more.txt"), report.ToString());
        }

        private static IEnumerator Play(ArcadeGame game, string id)
        {
            switch (game)
            {
                case WordWhackGame whack:
                    for (int i = 0; i < 3; i++) { Assert.IsTrue(whack.WhackTarget()); yield return null; }
                    var decoy = whack.CurrentSet.pairs.First(p => p != whack.Target && !string.IsNullOrEmpty(p.Clue(whack.CurrentSet.type)));
                    whack.Whack(whack.Raise(decoy));
                    whack.Raise(null);
                    break;
                case EchoPadsGame echo:
                    for (int round = 0; round < 2; round++)
                    {
                        for (float t = 0f; t < 12f && !echo.IsRepeating; t += Time.unscaledDeltaTime) yield return null;
                        Assert.IsTrue(echo.IsRepeating, "echo pads reach the player's turn");
                        foreach (int pad in echo.Sequence.ToList()) { echo.Press(pad); yield return null; }
                    }
                    for (float t = 0f; t < 12f && !echo.IsRepeating; t += Time.unscaledDeltaTime) yield return null;
                    echo.Press((echo.Sequence[0] + 1) % 4);
                    break;
                case KanaSnakeGame snake:
                    for (int step = 0; step < 90 && snake.CorrectCount < 2 && snake.State == ArcadeGame.Phase.Playing; step++)
                    {
                        var tile = snake.Tiles.First(t => t.Pair == snake.Target);
                        var head = snake.Snake[0];
                        var want = head.x != tile.Cell.x ? new Vector2Int((int)Mathf.Sign(tile.Cell.x - head.x), 0) : new Vector2Int(0, (int)Mathf.Sign(tile.Cell.y - head.y));
                        if (want + snake.Direction == Vector2Int.zero) want = new Vector2Int(0, tile.Cell.y >= head.y ? 1 : -1);
                        snake.Steer(want);
                        snake.Advance();
                        yield return null;
                    }
                    for (int step = 0; step < 12 && snake.WrongCount == 0 && snake.State == ArcadeGame.Phase.Playing; step++)
                    {
                        snake.Steer(Vector2Int.up);
                        snake.Advance();
                        yield return null;
                    }
                    break;
                case KanaBreakerGame breaker:
                    for (int i = 0; i < 3; i++) { Assert.IsTrue(breaker.BreakTarget()); yield return null; }
                    breaker.LoseBall();
                    break;
                case DanceArrowsGame dance:
                    for (int i = 0; i < 3; i++) { dance.Press(i); dance.PlaceOnLine(dance.Spawn(i)); dance.Press(i); yield return null; }
                    var late = dance.Spawn(3);
                    late.Y = 5000f;
                    yield return null;
                    break;
                case TenseTyperGame typer:
                    Assert.GreaterOrEqual(typer.DeckSize, 72, "all single-blank exercises of the 12 tenses");
                    for (int i = 0; i < 3; i++) { typer.Type(typer.Current.acceptedAnswers[0]); typer.Submit(); yield return null; }
                    typer.Type("zzzz");
                    typer.Submit();
                    break;
                case WheelQuizGame wheel:
                    for (int round = 0; round < 3; round++)
                    {
                        wheel.Spin(160f);
                        for (float t = 0f; t < 6f && !wheel.IsAsking; t += Time.unscaledDeltaTime) yield return null;
                        Assert.IsTrue(wheel.IsAsking, "the wheel stops and asks");
                        Assert.AreEqual(4, wheel.Choices.Count);
                        wheel.Answer(round < 2 ? wheel.CorrectIndex : (wheel.CorrectIndex + 1) % 4);
                        yield return null;
                    }
                    break;
                case AirHockeyGame hockey:
                    hockey.ShootAtRobotGoal();
                    for (float t = 0f; t < 3f && hockey.CorrectCount == 0; t += Time.unscaledDeltaTime) yield return null;
                    hockey.ShootAtPlayerGoal();
                    for (float t = 0f; t < 3f && hockey.WrongCount == 0; t += Time.unscaledDeltaTime) yield return null;
                    break;
                default:
                    Assert.Fail("no play script for " + id);
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
