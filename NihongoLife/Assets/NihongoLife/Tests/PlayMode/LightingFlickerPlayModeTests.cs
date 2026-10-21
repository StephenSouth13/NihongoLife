using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// No light flicker while the camera swings around the town in the late afternoon (the reported bug: the whole
    /// ground went dark for one or two frames now and then). Renders every frame of a fast camera sweep next to the
    /// Game Center and checks the ground brightness never drops sharply for a single frame.
    /// Captures: Bao_Cao/lighting-flicker.
    /// </summary>
    public class LightingFlickerPlayModeTests
    {
        [UnityTest]
        public IEnumerator Town_FastCameraSweep_NoGroundFlicker()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            PlayerController player = null;
            for (float t = 0f; t < 15f && (player = Object.FindFirstObjectByType<PlayerController>()) == null; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            Object.FindFirstObjectByType<DayNightCycle>()?.SetHour(18.3f);
            var portal = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).First(p => p.TargetScene == WorldLocationCatalog.GameCenterScene);
            Vector3 spot = new Vector3(portal.transform.position.x - 3f, 0f, -15.5f); // south sidewalk, across from the Game Center (as in the report)
            var body = player.GetComponent<CharacterController>();
            body.enabled = false; player.transform.position = new Vector3(spot.x, 0.08f, spot.z); body.enabled = true;
            Physics.SyncTransforms();
            var cam = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            var camera = Camera.main;
            var rt = new RenderTexture(480, 270, 24);
            camera.targetTexture = rt;
            var tex = new Texture2D(480, 270, TextureFormat.RGB24, false);
            var sun = RenderSettings.sun;
            var samples = new List<(int frame, float yaw, float ground, float sunI, float shadowS, float ambient)>();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/lighting-flicker"));
            Directory.CreateDirectory(folder);
            try
            {
                byte[] previous = null;
                for (int f = 0; f < 720; f++)
                {
                    float yaw = f * 6f;                       // a fast sweep: one turn every 2 s at 30 fps
                    cam.SetOrbit(yaw, 34f + 8f * Mathf.Sin(f * 0.11f), 7f);
                    yield return null;                       // the camera follows in LateUpdate
                    camera.Render();                         // batch mode draws nothing by itself
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 480, 270), 0, 0); tex.Apply();
                    RenderTexture.active = null;
                    float ground = 0f; int n = 0;
                    for (int y = 20; y < 110; y += 6) for (int x = 120; x < 360; x += 6) { var c = tex.GetPixel(x, y); ground += c.grayscale; n++; }
                    ground /= n;
                    samples.Add((f, yaw, ground, sun != null ? sun.intensity : -1f, sun != null ? sun.shadowStrength : -1f, RenderSettings.ambientLight.grayscale));
                    byte[] png = tex.EncodeToPNG();
                    if (f % 120 == 0) File.WriteAllBytes(Path.Combine(folder, $"sweep_{f:000}.png"), png);
                    // A dark frame after a bright one: keep both, to see what changed.
                    if (samples.Count > 1 && ground < 0.7f * samples[samples.Count - 2].ground && previous != null)
                    {
                        File.WriteAllBytes(Path.Combine(folder, $"dark_{f:000}_before.png"), previous);
                        File.WriteAllBytes(Path.Combine(folder, $"dark_{f:000}.png"), png);
                    }
                    previous = png;
                }
            }
            finally { camera.targetTexture = null; Object.Destroy(rt); }

            // A flicker: a frame much darker than both neighbours (the camera barely moves between frames).
            var flickers = new List<string>();
            for (int i = 1; i < samples.Count - 1; i++)
            {
                float a = samples[i - 1].ground, b = samples[i].ground, c = samples[i + 1].ground;
                if (b < 0.7f * Mathf.Min(a, c) || (b < 0.7f * a && i + 2 < samples.Count && samples[i + 2].ground > 1.3f * b))
                    flickers.Add($"f{samples[i].frame} yaw{samples[i].yaw:0} ground {a:0.00}->{b:0.00}->{c:0.00} sun {samples[i].sunI:0.00}/{samples[i].shadowS:0.00}");
            }
            File.WriteAllLines(Path.Combine(folder, "sweep.txt"), samples.Select(s => $"{s.frame}\t{s.yaw:0}\t{s.ground:0.000}\t{s.sunI:0.00}\t{s.shadowS:0.00}\t{s.ambient:0.00}"));
            Debug.Log("[Flicker] " + flickers.Count + " flicker frames: " + string.Join(" | ", flickers.Take(12)));
            Assert.IsEmpty(flickers, "The ground must not flash dark for single frames while the camera turns.");
        }
    }
}
