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
    /// H2: the konbini door camera. Over repeated enter/leave cycles the camera must never whip (large yaw turn or position
    /// jump in one frame, except hidden behind the black ScreenCut), never swap cameras, and never sit inside a wall.
    /// Captures + numbers: Bao_Cao/konbini-camera.
    /// </summary>
    public class KonbiniCameraPlayModeTests
    {
        [UnityTest]
        public IEnumerator Konbini_EnterLeave_NoCameraWhip()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            PlayerController player = null;
            for (float t = 0f; t < 25f && (player = Object.FindFirstObjectByType<PlayerController>()) == null; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(3f);
            var door = Object.FindFirstObjectByType<DoorInteractable>();
            var cam = Object.FindFirstObjectByType<ThirdPersonCameraController>();
            Assert.IsNotNull(door); Assert.IsNotNull(cam);
            var camera = Camera.main;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/konbini-camera"));
            Directory.CreateDirectory(folder);
            var log = new List<string>();
            float maxYaw = 0f, maxMove = 0f, maxSpeed = 0f, accMove = 0f, accT = 0f; var fast = new List<string>(); int frames = 0, insideWall = 0, swaps = 0;
            int cams0 = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled && c.targetTexture == null);
            int listeners0 = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled);

            Vector3 prevPos = Vector3.zero; float prevYaw = 0f; bool havePrev = false;
            IEnumerator Watch(int n)
            {
                for (int i = 0; i < n; i++)
                {
                    yield return null;
                    var c = Camera.main;
                    if (c == null) { swaps++; continue; }
                    if (c != camera) { swaps++; camera = c; }
                    float yaw = c.transform.eulerAngles.y;
                    if (havePrev && ScreenCut.Alpha < 0.9f && !ScreenCut.Running)
                    {
                        maxYaw = Mathf.Max(maxYaw, Mathf.Abs(Mathf.DeltaAngle(prevYaw, yaw)));
                        float mv = Vector3.Distance(prevPos, c.transform.position); maxMove = Mathf.Max(maxMove, mv); accMove += mv; accT += Time.unscaledDeltaTime; float speed = 0f; if (accT >= 0.05f) { speed = accMove / accT; accMove = 0f; accT = 0f; } maxSpeed = Mathf.Max(maxSpeed, speed); if (speed > 35f && fast.Count < 8) fast.Add($"f{frames} {mv:0.00}m dt={Time.unscaledDeltaTime:0.000} playerZ={player.transform.position.z:0.00} indoor={cam.IsIndoor} cut={ScreenCut.Alpha:0.00}");
                    }
                    prevPos = c.transform.position; prevYaw = yaw; havePrev = true; frames++;
                    if (Physics.OverlapSphere(c.transform.position, 0.06f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Any(h => h.GetComponentInParent<PlayerController>() == null)) insideWall++;
                }
            }
            void Snap(string name)
            {
                var rt = new RenderTexture(960, 540, 24);
                var cc = Camera.main; cc.targetTexture = rt; cc.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply();
                RenderTexture.active = null; cc.targetTexture = null; Object.Destroy(rt);
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
                Object.Destroy(tex);
            }

            for (int cycle = 0; cycle < 4; cycle++)
            {
                Place(player, new Vector3(0f, 0.08f, -0.9f), 0f, cam);
                yield return new WaitForSecondsRealtime(0.5f);
                havePrev = false;
                if (cycle == 0) Snap("01_street_before_door");
                door.Close(); yield return new WaitForSecondsRealtime(0.6f);
                door.Interact(player.gameObject);
                for (float tt = 0f; tt < 2.2f; tt += Time.unscaledDeltaTime) yield return Watch(1);   // door opens, cut, fade in
                Assert.Greater(player.transform.position.z, 0.6f, "Inside the store (cycle " + cycle + ")");
                Assert.IsTrue(cam.IsIndoor, "Indoor camera mode inside (cycle " + cycle + ")");
                if (cycle == 0) Snap("02_inside_after_door");
                // Walk out through the zone edge, then back in, a few frames each step (like a real walk).
                for (int k = 0; k < 3; k++)
                {
                    for (float z = 2.0f; z > -1.5f; z -= 0.09f) { Place(player, new Vector3(0f, 0.08f, z), 180f, null); yield return Watch(1); }
                    for (float tt = 0f; tt < 0.7f; tt += Time.unscaledDeltaTime) yield return Watch(1);
                    if (k == 0 && cycle == 0) Snap("03_walked_out");
                    for (float z = -1.5f; z < 2.0f; z += 0.09f) { Place(player, new Vector3(0f, 0.08f, z), 0f, null); yield return Watch(1); }
                    for (float tt = 0f; tt < 0.7f; tt += Time.unscaledDeltaTime) yield return Watch(1);
                }
                Assert.IsTrue(cam.IsIndoor, "Back indoors after re-entering (cycle " + cycle + ")");
                for (float z = 2.0f; z > -2.5f; z -= 0.09f) { Place(player, new Vector3(0f, 0.08f, z), 180f, null); yield return Watch(1); }
                for (float tt = 0f; tt < 1.2f; tt += Time.unscaledDeltaTime) yield return Watch(1);
                Assert.IsFalse(cam.IsIndoor, "Outdoor camera again after leaving (cycle " + cycle + ")");
            }
            int cams1 = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled && c.targetTexture == null);
            int listeners1 = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled);
            string summary = $"frames={frames} maxYawPerFrame={maxYaw:0.00} maxCameraMovePerFrame={maxMove:0.00} maxSpeed={maxSpeed:0.0}m/s fast=[{string.Join("; ", fast)}] insideWallFrames={insideWall} cameraSwaps={swaps} cameras {cams0}->{cams1} listeners {listeners0}->{listeners1}";
            File.WriteAllText(Path.Combine(folder, "camera.txt"), summary);
            Debug.Log("[KonbiniCam] " + summary);
            Assert.AreEqual(0, swaps, "The one camera is never swapped.");
            Assert.AreEqual(cams0, cams1); Assert.AreEqual(listeners0, listeners1); Assert.AreEqual(1, listeners1);
            Assert.Less(maxYaw, 6f, "No yaw whip: " + summary);
            Assert.Less(maxSpeed, 45f, "No position jump (camera speed): " + summary);
            Assert.AreEqual(0, insideWall, "The camera is never inside a wall: " + summary);
        }

        private static void Place(PlayerController player, Vector3 position, float yaw, ThirdPersonCameraController cam)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            if (cam != null) cam.SetOrbit(yaw, 16f, 7.2f);
        }
    }
}