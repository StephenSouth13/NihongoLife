using System.Collections;
using System.IO;
using NihongoLife.Core;
using NihongoLife.Health;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// P0: a new game or a loaded save never starts with the player fainting or waking up in the clinic. Hostile saves
    /// (everything at zero, tired, ill) are loaded in town and the game runs a while: no collapse, no clinic scene.
    /// Capture: Bao_Cao/startup-safety.
    /// </summary>
    public class StartupSafetyPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/startup-safety"));

        [UnityTest]
        public IEnumerator HostileSave_LoadsAwake_NoFaint_NoClinic()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(1.5f);
            var status = PlayerStatus.Instance;
            Assert.IsNotNull(status);
            var consequences = Object.FindAnyObjectByType<StatusConsequences>();
            Assert.IsNotNull(consequences, "StatusConsequences lives in town");
            int before = consequences.CollapseCount;

            status.DebugSetNeeds(health: 0f, energy: 0f, hunger: 0f, thirst: 0f, sleepiness: 100f);
            PlayerIllness.Catch("heatstroke", false);
            PlayerIllness.Catch("norovirus", false);
            if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repository))
            {
                var progress = repository.GetProgress();
                progress.health = 0f; progress.energy = 0f; progress.hunger = 0f; progress.thirst = 0f; progress.sleepiness = 100f;
            }
            status.ReloadProgress();
            Assert.Greater(status.CurrentHealth, 0f, "Awake after loading a save written at zero");

            // First 25 seconds of play: the player has time to read the ring, eat, or walk to the clinic by choice.
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 25f)
            {
                Assert.AreEqual(before, consequences.CollapseCount, "No collapse in the first seconds");
                Assert.IsFalse(consequences.IsBusy, "No faint screen");
                Assert.AreEqual(WorldLocationCatalog.CityScene, SceneManager.GetActiveScene().name, "Still in town, not carried to the clinic");
                yield return null;
            }
            Assert.Greater(status.CurrentHealth, 0f, "Still on their feet after 25 s");
            Directory.CreateDirectory(Folder);
            var camera = Camera.main;
            if (camera != null)
            {
                var rt = new RenderTexture(1280, 720, 24);
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                camera.targetTexture = null;
                File.WriteAllBytes(Path.Combine(Folder, "01_hostile_save_after_25s.png"), tex.EncodeToPNG());
                Object.Destroy(rt); Object.Destroy(tex);
            }
            yield return null;
        }
    }
}