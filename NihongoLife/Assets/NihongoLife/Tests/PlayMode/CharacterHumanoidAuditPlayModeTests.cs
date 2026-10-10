using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Focused, non-destructive character pilot. It exercises the existing Remy visual in the existing
    /// gameplay sandbox and captures evidence without replacing PlayerController, camera, collider,
    /// inventory, networking, scene hierarchy, or the shared animator controller.
    /// </summary>
    public class CharacterHumanoidAuditPlayModeTests
    {
        private const string RunAsset =
            "Assets/Devion Games/Third Person Controller/Example/Art/Animations/Basic Movement/Run.fbx";

        [UnityTest]
        public IEnumerator Remy_HumanoidRetarget_IdleWalkRunTalkSit()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);

            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.NotNull(player, "The existing test sandbox must provide the real player.");
            Assert.NotNull(player.GetComponent<CharacterController>(), "Keep the existing collision capsule.");
            Assert.NotNull(player.GetComponent<PlayerInventory>(), "Keep the existing inventory component.");
            Assert.NotNull(Camera.main, "Keep the existing gameplay camera.");

            var animation = player.GetComponent<CharacterAnimationController>();
            var animator = player.GetComponentInChildren<Animator>();
            Assert.NotNull(animation);
            Assert.NotNull(animator);
            Assert.NotNull(animator.avatar);
            Assert.IsTrue(animator.avatar.isValid, "Remy's generated Avatar must be valid.");
            Assert.IsTrue(animator.avatar.isHuman, "Remy must use a Unity Humanoid Avatar.");
            Assert.IsTrue(animator.isHuman, "The live Animator must be Humanoid.");
            Assert.NotNull(animator.runtimeAnimatorController);
            Assert.IsFalse(animator.applyRootMotion, "PlayerController, not animation root motion, owns movement.");

            player.InputLocked = true;
            var camera = CreateEvidenceCamera(player.transform);

            yield return ShowControllerState(animator, camera, "Idle", "01_idle");
            yield return ShowControllerState(animator, camera, "Walk", "02_walk");
            yield return ShowControllerState(animator, camera, "Talking", "04_talk");

            Assert.IsTrue(animation.HasSitState, "The shared controller must expose the real Sit clip.");
            Assert.IsTrue(animation.SetSitting(true));
            yield return new WaitForSecondsRealtime(0.45f);
            Capture("05_sit", camera);
            Assert.AreEqual("Sit", animator.GetCurrentAnimatorStateInfo(0).IsName("Sit") ? "Sit" : "Missing");
            animation.SetSitting(false);

            // Run is deliberately tested as a Humanoid retarget pilot only. The gameplay controller does not
            // yet expose a Run state, so this must not silently rewrite locomotion or fake sprint movement.
            var run = LoadClip(RunAsset);
            Assert.NotNull(run, "The audited repository Run clip is missing.");
            Assert.IsTrue(run.isHumanMotion, "Run must be a genuine Humanoid clip before retargeting.");
            var graph = PlayableGraph.Create("NL_Humanoid_Run_Audit");
            animator.gameObject.AddComponent<AnimationEventReceiver>();
            var output = AnimationPlayableOutput.Create(graph, "Run", animator);
            var playable = AnimationClipPlayable.Create(graph, run);
            playable.SetApplyFootIK(true);
            output.SetSourcePlayable(playable);
            graph.Play();
            yield return new WaitForSecondsRealtime(0.55f);
            Capture("03_run_retarget_pilot", camera);
            graph.Destroy();

            Assert.NotNull(player.GetComponent<PlayerController>());
            Assert.NotNull(player.GetComponent<CharacterController>());
            Assert.NotNull(player.GetComponent<PlayerInventory>());
            Object.Destroy(camera.gameObject);
        }

        private sealed class AnimationEventReceiver : MonoBehaviour
        {
            // The audited third-party clip contains this legitimate footstep event. The gameplay audio
            // receiver lives on the player root, while the audit graph targets the nested visual Animator.
            public void Footsteps() { }
        }

        private static IEnumerator ShowControllerState(Animator animator, Camera camera, string state, string shot)
        {
            int hash = Animator.StringToHash(state);
            Assert.IsTrue(animator.HasState(0, hash), $"NL_Humanoid is missing {state}.");
            animator.SetFloat("Speed", state == "Walk" ? 1f : 0f);
            animator.SetBool("IsTalking", state == "Talking");
            animator.Play(hash, 0, 0f);
            animator.Update(0f);
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(state), $"Could not enter {state}.");
            Capture(shot, camera);
            animator.SetBool("IsTalking", false);
        }

        private static AnimationClip LoadClip(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(x => !x.name.StartsWith("__preview__"));
        }

        private static Camera CreateEvidenceCamera(Transform target)
        {
            var camera = new GameObject("CharacterAuditCamera").AddComponent<Camera>();
            if (Camera.main != null) camera.CopyFrom(Camera.main);
            camera.enabled = false;
            Vector3 chest = target.position + Vector3.up * 1.05f;
            camera.transform.position = chest + target.forward * 2.8f + target.right * 0.35f;
            camera.transform.LookAt(chest);
            return camera;
        }

        private static void Capture(string name, Camera camera)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-humanoid-audit"));
            Directory.CreateDirectory(folder);
            var target = new RenderTexture(1600, 900, 24);
            var oldActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = oldActive;
            Object.Destroy(texture);
            target.Release();
            Object.Destroy(target);
        }
    }
}
