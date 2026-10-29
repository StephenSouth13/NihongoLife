#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// P0.b recorder. WebGL compiles a shader variant the first time it is drawn, so the first character spawn hitches.
    /// <c>WebGLShaderVariants</c> only lists the keyword-less variant of every shader, which URP never uses. This test plays
    /// the town (day, dusk, night), all eight Kids with different hair, and every zone, and saves the variants that were REALLY
    /// requested to <c>Resources/WebGL/RecordedShaderVariants</c>. The WebGL loader warms them up progressively.
    /// It writes the file only when NIHONGO_RECORD_SHADERS=1; without it, it is skipped (the EditMode test validates the file).
    /// </summary>
    public class WebGLShaderRecordingPlayModeTests
    {
        public const string OutputPath = "Assets/NihongoLife/Resources/WebGL/RecordedShaderVariants.shadervariants";
        private static readonly string[] Kids = { "kid_haruto", "kid_yui", "kid_sota", "kid_hina", "kid_riku", "kid_mio", "kid_yuto", "kid_sakura" };

        /// <summary>The editor's variant tracker is internal in Unity 6; the same calls the Project Settings window makes.</summary>
        private static object Tracker(string method, params object[] args)
        {
            var info = typeof(ShaderUtil).GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "UnityEditor.ShaderUtil." + method + " exists in this editor version");
            return info.Invoke(null, args);
        }

        [UnityTest]
        public IEnumerator Record_RealVariants_OfTownCharactersAndZones()
        {
            if (Environment.GetEnvironmentVariable("NIHONGO_RECORD_SHADERS") != "1")
                Assert.Ignore("Set NIHONGO_RECORD_SHADERS=1 to re-record Resources/WebGL/RecordedShaderVariants.");
            LogAssert.ignoreFailingMessages = true;
            Tracker("ClearCurrentShaderVariantCollection");

            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player, "the player is in town");
            var crowd = new GameObject("ShaderRecordingCrowd");
            var hairs = CharacterLook.Options.hairs;
            for (int i = 0; i < Kids.Length; i++)
            {
                var kid = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Characters/Kids/NL_" + Kids[i]), crowd.transform);
                kid.transform.position = player.transform.position + player.transform.forward * 2.5f + player.transform.right * ((i - 3.5f) * 0.9f);
                CharacterLook.Apply(kid, hairs[i % hairs.Length].id, "none", 20 + i * 8, i % 3 == 0);
            }
            foreach (float hour in new[] { 12f, 18.3f, 22f, 5.5f })
            {
                UnityEngine.Object.FindFirstObjectByType<DayNightCycle>()?.SetHour(hour);
                yield return new WaitForSecondsRealtime(1.5f);
            }
            UnityEngine.Object.Destroy(crowd);

            foreach (var scene in new[]
            {
                WorldLocationCatalog.StationScene, WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SchoolScene,
                WorldLocationCatalog.HomeBedroomScene, WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.ClinicScene,
                WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.PhuQuocIslandScene
            })
            {
                // Play inside a zone boots the town and enters the zone, as a player does.
                yield return SceneManager.LoadSceneAsync(scene);
                for (float t = 0f; t < 40f && (SceneManager.GetActiveScene().name != scene || UnityEngine.Object.FindFirstObjectByType<PlayerController>() == null); t += Time.unscaledDeltaTime) yield return null;
                yield return new WaitForSecondsRealtime(2.5f);
            }

            int before = (int)Tracker("GetCurrentShaderVariantCollectionVariantCount");
            Assert.Greater(before, 10, "the editor tracked real shader variants while playing");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutputPath)) ?? ".");
            Tracker("SaveCurrentShaderVariantCollection", OutputPath);
            Debug.Log($"[ShaderRecording] {Tracker("GetCurrentShaderVariantCollectionShaderCount")} shaders, {before} variants -> {OutputPath}");
        }
    }
}
#endif