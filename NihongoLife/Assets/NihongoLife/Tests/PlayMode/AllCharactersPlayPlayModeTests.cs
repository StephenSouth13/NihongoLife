using System.Collections;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Gestures;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Every character of the select screen goes into the game like a player: chosen (saved selection), the town loads with
    /// that character's model, it walks with the on-screen joystick (feet on the ground, actually moving), and it plays the
    /// guitar — held in front of the belly as one piece (body, neck and headstock joined), not behind the head.
    /// Captures: Bao_Cao/all-characters.
    /// </summary>
    public class AllCharactersPlayPlayModeTests
    {
        private const string Topic = "all-characters";

        [UnityTest]
        public IEnumerator EveryCharacter_EntersTheTown_Walks_AndPlaysTheGuitar()
        {
            LogAssert.ignoreFailingMessages = true;
            string original = PlayableCharacterCatalog.GetSelected().Id;
            var report = new StringBuilder();
            var input = GameInputService.GetOrCreate();
            try
            {
                for (int i = 0; i < PlayableCharacterCatalog.Count; i++)
                {
                    var character = PlayableCharacterCatalog.Get(i);
                    Assert.IsTrue(character.IsAvailable, character.Id + " can be chosen");
                    PlayableCharacterCatalog.SaveSelected(character.Id);
                    yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                    for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
                    var dm = DialogueManager.Instance;
                    for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
                    var player = Object.FindFirstObjectByType<PlayerController>();
                    Assert.IsNotNull(player, character.Id + ": a player in the town");
                    var status = PlayerStatus.Instance;
                    if (status != null) status.DebugSetNeeds(health: 100f, energy: 100f, hunger: 100f, thirst: 100f, sleepiness: 0f);

                    // The chosen model is the one in the game.
                    var prefab = PlayableCharacterCatalog.LoadPrefab(character);
                    Assert.IsNotNull(prefab, character.Id + " prefab");
                    var expected = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.sharedMesh != null ? r.sharedMesh.name : "").OrderBy(n => n).ToArray();
                    var visual = player.transform.Find("Visual");
                    Assert.IsNotNull(visual, character.Id + ": the player's visual");
                    var actual = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.sharedMesh != null ? r.sharedMesh.name : "").OrderBy(n => n).ToArray();
                    CollectionAssert.AreEqual(expected, actual, character.Id + ": the town shows the chosen character");
                    var animator = player.GetComponentInChildren<Animator>();
                    Assert.IsTrue(animator != null && animator.isHuman, character.Id + ": humanoid rig");

                    // Walk with the on-screen joystick.
                    Vector3 start = player.transform.position;
                    input.SetMobileMove(Vector2.up);
                    yield return new WaitForSecondsRealtime(1.2f);
                    float foot = Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
                    input.SetMobileMove(Vector2.zero);
                    float walked = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(player.transform.position.x, 0f, player.transform.position.z));
                    Assert.Greater(walked, 1.2f, character.Id + " walks");
                    Assert.Less(Mathf.Abs(foot - player.transform.position.y), 0.2f, character.Id + ": feet on the ground while walking");
                    yield return new WaitForSecondsRealtime(0.5f);

                    // Play the guitar where it stands.
                    MusicStudioUI.SetPerformer(player.gameObject, true);
                    MusicStudioUI.AutoPlay("ode-to-joy");
                    yield return new WaitForSecondsRealtime(1.3f);
                    var layer = player.GetComponent<GestureLayer>();
                    Assert.IsNotNull(layer);
                    Assert.AreEqual("guitar_strum", layer.Current, character.Id + " strums");
                    var guitar = layer.ActiveProps.FirstOrDefault(p => p.id == "guitar");
                    Assert.IsNotNull(guitar.transform, character.Id + ": the guitar is shown");
                    var renderers = guitar.transform.GetComponentsInChildren<Renderer>();
                    var whole = renderers[0].bounds; foreach (var r in renderers) whole.Encapsulate(r.bounds);
                    Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips), head = animator.GetBoneTransform(HumanBodyBones.Head);
                    Vector3 body = guitar.transform.TransformPoint(new Vector3(0.06f, 0.17f, 0.26f));
                    report.AppendLine($"{character.Id}: walked {walked:0.0} m, guitar {whole.size}, body {body.y - hips.position.y:0.00} m above the hips, head {head.position.y - hips.position.y:0.00} m");
                    Assert.Less(whole.size.magnitude, 1.1f, character.Id + ": one guitar, not pieces spread out");
                    Assert.Greater(Vector3.Dot(body - hips.position, player.transform.forward), 0.02f, character.Id + ": the guitar in front of the body");
                    Assert.Less(body.y, head.position.y - 0.08f, character.Id + ": below the head");
                    // Each piece touches the rest (a gap means the guitar looks broken apart).
                    foreach (var r in renderers)
                    {
                        var grown = r.bounds; grown.Expand(0.03f);
                        Assert.IsTrue(renderers.Any(o => o != r && o.bounds.Intersects(grown)), $"{character.Id}: guitar piece {r.name} floats apart");
                    }
                    TestShots.Portrait(Topic, $"{i:00}_{character.Id}_guitar", player.transform, 1.5f, 0.6f);
                    MusicStudioUI.Close();
                    yield return null;
                }
                System.IO.Directory.CreateDirectory(TestShots.Folder(Topic));
                System.IO.File.WriteAllText(System.IO.Path.Combine(TestShots.Folder(Topic), "report.txt"), report.ToString());
            }
            finally
            {
                input.SetMobileMove(Vector2.zero);
                MusicStudioUI.Close();
                PlayableCharacterCatalog.SaveSelected(original);
            }
        }
    }
}
