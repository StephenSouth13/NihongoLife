using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Gestures;
using NihongoLife.Music;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The player's own guitar, played like a player: bought at the Hibiki counter (asked in Japanese), it rides on the
    /// back, it is held in front of the body (not behind a kid's head) when playing on the stage, and in the town the
    /// character busks with it and passers-by tip a song at a good tempo. The shop's price tags read the right way round.
    /// Captures: Bao_Cao/busking.
    /// </summary>
    public class GuitarBuskingPlayModeTests
    {
        private const string Topic = "busking";
        private static readonly Vector3 O = new Vector3(1300f, 0f, 0f);

        [UnityTest]
        public IEnumerator Guitar_BoughtWornOnTheBack_PlayedOnStage_AndBuskedInTown()
        {
            LogAssert.ignoreFailingMessages = true;
            StoryFlags.Remove(MusicShop.GuitarFlag);
            try
            {
                yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                yield return new WaitForSecondsRealtime(2.5f);
                var dm = DialogueManager.Instance;
                while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
                var flow = Object.FindFirstObjectByType<SceneFlowController>();
                flow.EnterZone(WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.MusicShopEntrance, "Hibiki");
                for (float t = 0f; t < 25f && (flow.IsLoading || SceneManager.GetActiveScene().name != WorldLocationCatalog.MusicShopScene); t += Time.unscaledDeltaTime) yield return null;
                Assert.AreEqual(WorldLocationCatalog.MusicShopScene, SceneManager.GetActiveScene().name);
                yield return new WaitForSecondsRealtime(1f);
                var player = Object.FindFirstObjectByType<PlayerController>();
                var status = player.GetComponent<PlayerStatus>();
                if (status != null) status.DebugSetNeeds(100f, 100f, 100f, 100f, 0f);

                // Price tags face the shop (a tag turned the wrong way reads mirrored).
                Vector3 middle = O + new Vector3(0f, 1.2f, 0f);
                foreach (var tag in Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None).Where(t => (t.name.StartsWith("PriceTag_") || t.name == "Price") && Mathf.Abs(t.transform.position.x - O.x) < 14f))
                    Assert.Less(Vector3.Dot(middle - tag.transform.position, tag.transform.forward), 0f, tag.name + " at " + tag.transform.position + " reads the right way from the room");

                // The counter: Mr Hibiki offers a second-hand guitar; "はい、ください" buys it.
                var inventory = PlayerInventory.Instance;
                inventory.AddYen(MusicShop.GuitarPrice + 500, "test");
                int before = inventory.Yen;
                MusicShop.TalkToOwner();
                Assert.IsTrue(JobQuizCard.IsOpen, "the owner asks whether to buy the guitar");
                JobQuizCard.Answer(JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Id == "buy"));
                yield return null;
                Assert.IsTrue(MusicShop.OwnsGuitar);
                Assert.AreEqual(before - MusicShop.GuitarPrice, inventory.Yen, "paid at the counter");
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.IsTrue(inventory.HasItem(MusicShop.GuitarItem), "the guitar is in the bag");
                var layer = player.GetComponent<GestureLayer>();
                Assert.IsNotNull(layer);
                Assert.AreEqual("carry_guitar", layer.Current, "worn on the back");
                var animator = layer.Animator;
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips), head = animator.GetBoneTransform(HumanBodyBones.Head);
                var back = layer.ActiveProps.FirstOrDefault(p => p.id == "guitar_back");
                Assert.IsNotNull(back.transform, "the guitar is shown");
                Assert.Less(Vector3.Dot(back.transform.TransformPoint(new Vector3(0.05f, 0.3f, -0.27f)) - hips.position, player.transform.forward), -0.05f, "behind the back");
                TestShots.Shot(Topic, "01_guitar_on_back", player.transform.position - player.transform.forward * 1.8f + player.transform.right * 0.8f + Vector3.up * 1.3f, player.transform.position + Vector3.up * 0.7f);

                // The stage: the guitar is held in front of the body, between the hips and the head.
                var stage = Object.FindObjectsByType<MusicSpot>(FindObjectsSortMode.None).First(s => s.name == "Spot_Stage");
                stage.Interact(player.gameObject);
                yield return null;
                Assert.IsTrue(MusicStudioUI.Perform("ode-to-joy"));
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.AreEqual("guitar_strum", layer.Current);
                var front = layer.ActiveProps.FirstOrDefault(p => p.id == "guitar");
                Assert.IsNotNull(front.transform, "a guitar in the hands");
                Vector3 body = front.transform.TransformPoint(new Vector3(0.06f, 0.17f, 0.26f));
                Assert.Greater(Vector3.Dot(body - hips.position, player.transform.forward), 0.03f, "in front of the body");
                Assert.Less(body.y, head.position.y - 0.1f, "below the head, not behind it");
                Assert.Greater(body.y, hips.position.y - 0.15f, "at the belly");
                TestShots.Portrait(Topic, "02_guitar_on_stage", player.transform, 2.2f, 0.7f);
                MusicStudioUI.Close();
                yield return null;

                // Busking is for the town, not inside the shop.
                Assert.IsFalse(MusicShop.Busk(), "no street show inside the shop");
                flow.ExitZone(WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.CityMusicShopReturn);
                for (float t = 0f; t < 25f && (flow.IsLoading || SceneManager.GetActiveScene().name != WorldLocationCatalog.CityScene); t += Time.unscaledDeltaTime) yield return null;
                Assert.AreEqual(WorldLocationCatalog.CityScene, SceneManager.GetActiveScene().name);
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.AreEqual("carry_guitar", layer.Current, "still on the back in the street");

                // In the street: the character plays standing; a good tempo earns tips.
                Assert.IsTrue(MusicShop.Busk());
                Assert.IsTrue(MusicStudioUI.OnStreet);
                MusicStudioUI.Tempo = 1f;
                Assert.IsTrue(MusicStudioUI.Perform("ode-to-joy"));
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.AreEqual("guitar_strum", layer.Current, "busking with the guitar");
                TestShots.Portrait(Topic, "03_busking", player.transform, 2.6f, 0.7f);
                int wallet = inventory.Yen;
                MusicStudioUI.SkipToEnd();
                Assert.IsTrue(MusicStudioUI.LastResult.good);
                Assert.Greater(MusicStudioUI.LastTips, 0, "passers-by tip a good song");
                Assert.AreEqual(wallet + MusicStudioUI.LastTips, inventory.Yen);
                MusicStudioUI.Close();
                yield return new WaitForSecondsRealtime(1f);
                Assert.AreEqual("carry_guitar", layer.Current, "back on the back after the show");
            }
            finally
            {
                MusicStudioUI.Close();
                JobQuizCard.Close();
                StoryFlags.Remove(MusicShop.GuitarFlag);
                GuitarCarrier.Sync();
            }
        }
    }
}
