using System.Collections;
using System.IO;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The Kid's body follows the player's state: run clip when sprinting, tired walk when sleepy, limping when hurt,
    /// a real faint when collapsing. Captures: Bao_Cao/character-regression/mood_*.jpg
    /// </summary>
    public class KidMoodPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-regression"));

        [UnityTest]
        public IEnumerator Kid_RunTiredHurtFaint_Clips()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var player = Object.FindFirstObjectByType<PlayerController>();
            var mood = player.GetComponent<KidMoodAnimator>();
            Assert.NotNull(mood, "Mood animator on the player");
            var status = PlayerStatus.Instance;
            var input = GameInputService.GetOrCreate();
            status.DebugSetNeeds(health: 100f, hunger: 90f, thirst: 90f, sleepiness: 10f);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(KidMoodAnimator.Mood.Normal, mood.Current);
            StringAssert.Contains("move_walk", mood.WalkClipName);

            // Sprint: the real run cycle.
            input.SetMobileMove(Vector2.up);
            input.SetMobileButton(GameInputId.Sprint, true);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(KidMoodAnimator.Mood.Run, mood.Current);
            StringAssert.Contains("move_run", mood.WalkClipName);
            Shoot(player, "mood_run");
            input.SetMobileButton(GameInputId.Sprint, false);

            // Sleepy: shuffling, tired walk.
            status.DebugSetNeeds(sleepiness: 90f);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(KidMoodAnimator.Mood.Tired, mood.Current);
            StringAssert.Contains("tired", mood.WalkClipName);
            Shoot(player, "mood_tired");

            // Hurt: limping.
            status.DebugSetNeeds(sleepiness: 10f, health: 15f);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(KidMoodAnimator.Mood.Hurt, mood.Current);
            StringAssert.Contains("injure", mood.WalkClipName);
            input.SetMobileMove(Vector2.zero);
            yield return new WaitForSecondsRealtime(0.5f);
            StringAssert.Contains("injure", mood.IdleClipName);
            Shoot(player, "mood_hurt_idle");

            // Health 0: a real faint, then back to normal after the clinic.
            var inventory = PlayerInventory.Instance;
            status.DebugSetNeeds(health: 0f);
            for (float t = 0f; t < 3f && mood.Current != KidMoodAnimator.Mood.Faint; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(KidMoodAnimator.Mood.Faint, mood.Current);
            StringAssert.Contains("faint", mood.LayClipName);
            yield return new WaitForSecondsRealtime(0.9f);
            Shoot(player, "mood_faint");
            var consequences = status.GetComponent<StatusConsequences>();
            for (float t = 0f; t < 12f && consequences.IsBusy; t += Time.unscaledDeltaTime) yield return null;
            status.DebugSetNeeds(health: 100f, sleepiness: 10f);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(KidMoodAnimator.Mood.Normal, mood.Current);
            if (inventory != null && consequences.LastFee > 0) inventory.AddYen(consequences.LastFee, "test");
        }

        private static void Shoot(PlayerController player, string name)
        {
            var cam = Camera.main;
            var controller = cam.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (controller != null) controller.enabled = false;
            var p = player.transform;
            cam.transform.position = p.position + p.right * 2.6f + p.forward * 1.2f + Vector3.up * 1.3f;
            cam.transform.LookAt(p.position + Vector3.up * 0.7f);
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".jpg"), tex.EncodeToJPG(86));
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
            if (controller != null) controller.enabled = true;
        }
    }
}
