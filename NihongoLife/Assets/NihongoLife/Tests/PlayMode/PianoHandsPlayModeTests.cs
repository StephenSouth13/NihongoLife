using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Gestures;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// At the piano the hands rest on the keys for every body: the keyboard is placed from the shoulders (as the hand
    /// targets are), not from the feet at an adult's height — on the chibi kids the keys stood at the chest and the hands
    /// played in the air under the keyboard. Measured in the keyboard's own space (its pieces' units) for every character.
    /// </summary>
    public class PianoHandsPlayModeTests : GestureTestBase
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/gestures"));

        [Test]
        public void Piano_HandsRestOnTheKeys_ForEveryBody()
        {
            var keyboard = GestureLibrary.Prop("keyboard");
            // The white keys: the playing surface.
            var keys = keyboard.pieces.First(p => p.hex == "#F7F7F2");
            float top = keys.pos[1] + keys.size[1] * 0.5f;
            float front = keys.pos[2] - keys.size[2] * 0.5f, back = keys.pos[2] + keys.size[2] * 0.5f, half = keys.size[0] * 0.5f;
            var report = new StringBuilder("character;arm m;hand above keys (key units) R/L;hand depth on keys R/L;hand across R/L\n");
            var failures = new List<string>();
            var variants = CharacterVariants.File.variants;
            for (int i = 0; i < variants.Length; i++)
            {
                string id = variants[i].id;
                Person(id, new Vector3(i * 3f, 0f, 0f), out var layer);
                Tick(layer, 20);
                Assert.IsTrue(layer.Play("piano_play"), id);
                float sumUp = 0f; int samples = 0;
                float minUp = float.MaxValue, maxUp = float.MinValue, minIn = float.MaxValue, maxIn = float.MinValue, maxSide = 0f;
                Transform prop = null;
                for (int s = 0; s < 240; s++)
                {
                    Tick(layer);
                    if (s < 90) continue; // seated, intro done
                    prop = layer.ActiveProps.FirstOrDefault(p => p.id == "keyboard").transform;
                    if (prop == null) continue;
                    foreach (var bone in new[] { HumanBodyBones.RightHand, HumanBodyBones.LeftHand })
                    {
                        Vector3 local = prop.InverseTransformPoint(Bone(layer, bone));
                        minUp = Mathf.Min(minUp, local.y - top); sumUp += local.y - top; samples++; maxUp = Mathf.Max(maxUp, local.y - top);
                        minIn = Mathf.Min(minIn, local.z); maxIn = Mathf.Max(maxIn, local.z);
                        maxSide = Mathf.Max(maxSide, Mathf.Abs(local.x));
                    }
                }
                if (prop == null) { failures.Add(id + ": no keyboard"); continue; }
                float meanUp = sumUp / Mathf.Max(1, samples);
                report.AppendLine($"{id};{layer.ArmLength:0.00};{minUp:0.000}…{maxUp:0.000} mean {meanUp:0.000};{minIn:0.00}…{maxIn:0.00};{maxSide:0.00}");
                // The hand bone is the wrist: it rides a little above the keys, never under them or a hand's length above.
                if (minUp < 0.0f) failures.Add($"{id}: a wrist goes {-minUp:0.000} under the keys");
                // Between notes a hand lifts (the playing bounce); resting, the wrist is a few centimetres over the keys.
                if (maxUp > 0.26f) failures.Add($"{id}: a wrist lifts {maxUp:0.000} above the keys");
                if (meanUp < 0.01f || meanUp > 0.12f) failures.Add($"{id}: the wrists rest {meanUp:0.000} above the keys on average");
                // The wrists over the front half of the keys (the fingers reach forward onto them), never in front of the keyboard.
                if (minIn < front - 0.06f || maxIn > back) failures.Add($"{id}: wrists {minIn:0.00}…{maxIn:0.00} deep, keys {front:0.00}…{back:0.00}");
                if (maxSide > half) failures.Add($"{id}: a wrist {maxSide:0.00} to the side, keys ±{half:0.00}");
                if (id.StartsWith("musician_") || id == "resident_child_m" || id == "chef_haruto") Shoot(layer, Path.Combine(Root, "piano_hands_" + id + ".png"));
                layer.Stop(0f); Tick(layer, 10);
            }
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "piano_hands.csv"), report.ToString());
            Debug.Log("[PianoHands]\n" + report);
            Assert.IsEmpty(failures, string.Join("\n", failures) + "\n" + report);
        }

        /// <summary>A picture from the front and a little to the side, at the height of the keys.</summary>
        private static void Shoot(GestureLayer layer, string file)
        {
            var root = layer.transform;
            var light = new GameObject("PianoShotLight").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(40f, root.eulerAngles.y + 150f, 0f);
            var cam = new GameObject("PianoShotCamera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.24f, 0.17f, 0.32f); cam.fieldOfView = 35f;
            Vector3 focus = root.position + Vector3.up * (layer.ArmLength * 1.25f) + root.forward * 0.3f;
            cam.transform.position = focus + root.forward * 1.5f + root.right * 0.9f + Vector3.up * 0.35f;
            cam.transform.LookAt(focus);
            var rt = new RenderTexture(900, 700, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = null; cam.targetTexture = null;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt); Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(light.gameObject);
        }
    }
}
