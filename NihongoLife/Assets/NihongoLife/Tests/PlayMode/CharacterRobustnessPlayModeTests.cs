using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace NihongoLife.Tests
{
    /// <summary>
    /// P0 regressions on the Kids-pack characters: (a) a character never loses its mesh or atlas, even when the GPU
    /// drops the baked hair texture (WebGL context loss, device reset); (b) the first spawn of every model stays fast.
    /// Captures: Bao_Cao/character-regression.
    /// </summary>
    public class CharacterRobustnessPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-regression"));
        private static readonly string[] Kids = { "kid_haruto", "kid_yui", "kid_sota", "kid_hina", "kid_riku", "kid_mio", "kid_yuto", "kid_sakura" };

        private static GameObject Stage()
        {
            var stage = new GameObject("RegressionStage");
            stage.transform.position = new Vector3(0f, -400f, 0f);
            var light = new GameObject("Key", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f;
            light.transform.SetParent(stage.transform, false);
            light.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
            return stage;
        }

        private static GameObject Spawn(GameObject stage, int index)
        {
            var model = Object.Instantiate(Resources.Load<GameObject>("Characters/Kids/NL_" + Kids[index]), stage.transform);
            model.transform.localPosition = new Vector3((index - 3.5f) * 0.95f, 0f, 0f);
            model.transform.localRotation = Quaternion.identity;
            return model;
        }

        /// <summary>Average brightness (0..1) of a texture: 0 means it is black / lost.</summary>
        private static float Luma(Texture texture)
        {
            var small = RenderTexture.GetTemporary(16, 16, 0);
            Graphics.Blit(texture, small);
            var previous = RenderTexture.active;
            RenderTexture.active = small;
            var read = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, 16, 16), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(small);
            float sum = 0f;
            foreach (var pixel in read.GetPixels()) sum += pixel.grayscale * pixel.a;
            Object.Destroy(read);
            return sum / 256f;
        }

        private static void AssertWhole(GameObject model, string label)
        {
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Assert.IsNotNull(renderer.sharedMesh, label + ": mesh kept");
                Assert.Greater(renderer.sharedMesh.vertexCount, 0, label + ": vertices");
                Assert.IsTrue(renderer.bones.All(b => b != null), label + ": bones kept");
                var material = renderer.sharedMaterial;
                Assert.IsNotNull(material, label + ": material kept");
                var texture = material.GetTexture("_Texture2D");
                Assert.IsNotNull(texture, label + ": atlas kept");
                if (texture is RenderTexture rt) Assert.IsTrue(rt.IsCreated(), label + ": baked hair atlas exists on the GPU");
                Assert.Greater(Luma(texture), 0.05f, label + ": atlas is not black / lost");
            }
        }

        [UnityTest]
        public IEnumerator Hair_AtlasLostByTheGpu_IsBakedAgain()
        {
            var stage = Stage();
            try
            {
                var model = Spawn(stage, 0);
                yield return null;
                CharacterLook.Apply(model, "black", "none", 30, false);
                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
                var atlas = (RenderTexture)renderer.sharedMaterial.GetTexture("_Texture2D");
                AssertWhole(model, "before");

                atlas.Release(); // what a lost WebGL context / device reset does to a runtime RenderTexture
                Assert.IsFalse(atlas.IsCreated());
                CharacterLook.Apply(model, "black", "none", 30, false); // e.g. the next spawn or look change
                yield return null;
                AssertWhole(model, "after re-apply");

                // The component notices on its own (no look change needed).
                var actor = Spawn(stage, 1);
                var look = CharacterLook.On(actor, false).Fixed("pink", "none", 25, false);
                yield return null;
                var atlas2 = (RenderTexture)actor.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.GetTexture("_Texture2D");
                atlas2.Release();
                yield return new WaitForSecondsRealtime(0.8f);
                AssertWhole(actor, "after the component's own check");
                Assert.IsNotNull(look);
            }
            finally { Object.Destroy(stage); }
        }

        [UnityTest]
        public IEnumerator EveryKid_EveryHair_KeepsMeshAndAtlas_AndSpawnsFast()
        {
            Directory.CreateDirectory(Folder);
            var stage = Stage();
            try
            {
                var options = CharacterLook.Options.hairs;
                var timer = new Stopwatch();
                var cold = new double[Kids.Length];
                for (int i = 0; i < Kids.Length; i++)
                {
                    timer.Restart();
                    var model = Spawn(stage, i);
                    CharacterLook.Apply(model, "black", "none", 30, false); // the first recolour: loads mask + blit material
                    cold[i] = timer.Elapsed.TotalMilliseconds;
                    yield return null;
                    foreach (var hair in options)
                    {
                        CharacterLook.Apply(model, hair.id, "none", 30, false);
                        AssertWhole(model, Kids[i] + "/" + hair.id);
                    }
                    CharacterLook.Apply(model, "natural", "none", 30, false);
                    AssertWhole(model, Kids[i] + "/natural");
                }
                Debug.Log("[CharacterRegression] cold spawn+recolour ms: " + string.Join(", ", cold.Select((ms, i) => Kids[i] + "=" + ms.ToString("0"))));
                // Editor budget: a spawn with its first recolour never costs a visible stall (the WebGL build is ~3-5x slower).
                Assert.Less(cold.Max(), 800d, "spawn + first recolour of any model");
                yield return null;
                Capture(stage, "01_all_kids_natural");
                for (int i = 0; i < Kids.Length; i++) CharacterLook.Apply(stage.transform.GetChild(i + 1).gameObject, options[(i * 3 + 2) % options.Length].id, "none", 30, false);
                yield return null;
                Capture(stage, "02_all_kids_other_hair");
            }
            finally { Object.Destroy(stage); }
        }

        private static void Capture(GameObject stage, string name)
        {
            const int width = 1600, height = 600;
            var temp = new GameObject("RegressionCamera", typeof(Camera));
            var camera = temp.GetComponent<Camera>();
            camera.transform.position = stage.transform.position + new Vector3(0f, 0.95f, 8.4f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.05f, -1f));
            camera.fieldOfView = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.66f, 0.74f);
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(temp);
        }
    }
}