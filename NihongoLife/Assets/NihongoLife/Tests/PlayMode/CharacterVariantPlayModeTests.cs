using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The town's people as the game shows them: every variant prefab is spawned, takes its person's colours through
    /// CharacterLook's baked atlas (checked pixel by pixel against the measured palette), stands on the floor at a
    /// sensible height, and is photographed into Bao_Cao/characters-variants (an overview and one close-up per job).
    /// </summary>
    public class CharacterVariantPlayModeTests
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/characters-variants"));

        private Scene _scene, _previous;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            CharacterVariants.Reset(); LookPaletteCatalog.Reset();
            _previous = SceneManager.GetActiveScene();
            _scene = SceneManager.CreateScene("VariantSheet_" + System.Guid.NewGuid().ToString("N").Substring(0, 6));
            SceneManager.SetActiveScene(_scene);
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.15f; light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(38f, 160f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.68f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Floor"; ground.transform.position = new Vector3(0f, -0.05f, 0f); ground.transform.localScale = new Vector3(40f, 0.1f, 40f);
            Object.Destroy(ground.GetComponent<Collider>());
            ground.GetComponent<Renderer>().sharedMaterial.color = new Color(0.86f, 0.87f, 0.88f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            _spawned.Clear();
            if (_previous.IsValid() && _previous.isLoaded) SceneManager.SetActiveScene(_previous);
            if (_scene.IsValid()) SceneManager.UnloadSceneAsync(_scene);
        }

        private GameObject Spawn(string id, Vector3 position)
        {
            var person = CharacterVariants.Spawn(id, position, 0f);
            Assert.NotNull(person, id + " spawns");
            SceneManager.MoveGameObjectToScene(person, _scene);
            _spawned.Add(person);
            return person;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        [UnityTest]
        public IEnumerator EveryPerson_WearsTheirOwnColours_AndStandsOnTheFloor()
        {
            var file = CharacterVariants.File;
            var people = new List<(CharacterVariantSpec spec, GameObject go)>();
            int i = 0;
            foreach (var spec in file.variants) people.Add((spec, Spawn(spec.id, new Vector3(i++ * 2f, 0f, 0f))));
            yield return new WaitForSecondsRealtime(0.6f);
            yield return null;

            var originals = new Dictionary<string, Color32[]>();
            foreach (var (spec, go) in people)
            {
                var smr = go.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMesh.vertexCount).First();
                var baked = smr.sharedMaterial.GetTexture("_Texture2D") as RenderTexture;
                Assert.NotNull(baked, spec.id + ": the atlas is baked with the person's colours");
                Assert.IsTrue(baked.IsCreated(), spec.id + ": baked atlas alive");
                var palette = LookPaletteCatalog.ForKid(spec.kid);
                if (!originals.TryGetValue(spec.kid, out var original))
                {
                    var source = Resources.Load<GameObject>("Characters/Kids/NL_" + spec.kid).GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMesh.vertexCount).First().sharedMaterial.GetTexture("_Texture2D");
                    originals[spec.kid] = original = ReadPixels(source);
                }
                var now = ReadPixels(baked);
                var tint = spec.TintOf(file);
                Check(spec.id + " skin", original, now, palette.skinFrom, tint.skin);
                Check(spec.id + " top", original, now, palette.topFrom, tint.top);
                if (!string.IsNullOrEmpty(palette.bottomFrom)) Check(spec.id + " bottom", original, now, palette.bottomFrom, tint.bottom);

                var bounds = BoundsOf(go);
                Assert.That(bounds.min.y, Is.InRange(-0.12f, 0.12f), spec.id + ": feet on the floor");
                Assert.That(bounds.size.y, Is.InRange(0.85f, 2.2f), spec.id + ": a plausible height");
                Assert.IsTrue(go.GetComponentsInChildren<Renderer>(true).Count(r => r.name.StartsWith("Uniform_") && r.enabled) == file.Uniform(spec.uniform).pieces.Length, spec.id + ": props shown");
            }
            // The same person twice shares one baked atlas (cheap to spawn a crowd).
            var again = Spawn(file.variants[0].id, new Vector3(-3f, 0f, 0f));
            yield return null;
            Assert.AreSame(people[0].go.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial, again.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial, "clones share the baked material");
        }

        [Test]
        public void PaletteShader_RecolorsAKnownBlock_AndSparesTheRest()
        {
            var material = Resources.Load<Material>("Characters/Variants/NL_PaletteRecolor");
            Assert.NotNull(material, "material");
            Assert.NotNull(material.shader, "shader");
            Assert.IsTrue(material.shader.isSupported, "shader supported: " + material.shader.name);
            var source = new Texture2D(4, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            source.SetPixels32(new[] { new Color32(204, 73, 73, 255), new Color32(230, 195, 163, 255), new Color32(20, 20, 20, 255), new Color32(180, 62, 62, 255) });
            source.Apply();
            var mask = Texture2D.blackTexture;
            var swap = new Material(Shader.Find("Hidden/NihongoLife/PaletteRecolor"));
            swap.SetTexture("_Mask", mask);
            swap.SetFloat("_Tolerance", 0.05f); swap.SetFloat("_ChromaTolerance", 0.03f);
            Debug.Log("[PaletteTest] shader " + swap.shader.name + " has From0 " + swap.HasProperty("_From0") + " tol " + swap.GetFloat("_Tolerance") + " mask " + (swap.GetTexture("_Mask") != null) + " colorspace " + QualitySettings.activeColorSpace);
            swap.SetVector("_From0", new Vector4(230 / 255f, 195 / 255f, 163 / 255f, 1f)); swap.SetVector("_To0", new Vector4(97 / 255f, 64 / 255f, 43 / 255f, 1f));
            swap.SetVector("_From1", new Vector4(204 / 255f, 73 / 255f, 73 / 255f, 1f)); swap.SetVector("_To1", new Vector4(246 / 255f, 248 / 255f, 250 / 255f, 1f));
            var target = new RenderTexture(4, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { filterMode = FilterMode.Point };
            Graphics.Blit(source, target, swap);
            var read = ReadPixels(target);
            Debug.Log("[PaletteTest] " + string.Join(" ", read.Select(c => c.ToString())));
            Assert.That(read[0].r, Is.InRange(236, 255), "the red block became white: " + read[0]);
            Assert.That(read[1].r, Is.InRange(85, 110), "the skin block became dark: " + read[1]);
            Assert.That(read[2].r, Is.InRange(15, 25), "a dark texel is left alone: " + read[2]);
            Assert.That(read[3].r, Is.InRange(176, 184), "a similar-hue texel of the top, not the exact block colour, is left alone: " + read[3]);
            Object.Destroy(target); Object.Destroy(source); Object.Destroy(swap);
        }

        private static Color32[] ReadPixels(Texture texture)
        {
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            var pixels = read.GetPixels32();
            Object.Destroy(read);
            return pixels;
        }

        private static void Check(string what, Color32[] original, Color32[] now, string fromHex, string toHex)
        {
            ColorUtility.TryParseHtmlString(fromHex, out var f); ColorUtility.TryParseHtmlString(toHex, out var t);
            Color32 from = f, to = t;
            int hits = 0, right = 0;
            string sample = "";
            for (int p = 0; p < original.Length; p += 3)
            {
                var o = original[p];
                if (Mathf.Abs(o.r - from.r) > 3 || Mathf.Abs(o.g - from.g) > 3 || Mathf.Abs(o.b - from.b) > 3) continue;
                hits++;
                var n = now[p];
                if (sample == "") sample = " first texel " + o + " -> " + n;
                if (Mathf.Abs(n.r - to.r) <= 16 && Mathf.Abs(n.g - to.g) <= 16 && Mathf.Abs(n.b - to.b) <= 16) right++;
            }
            Assert.Greater(hits, 200, what + ": the model's block " + fromHex + " is on its atlas");
            Assert.GreaterOrEqual(right, hits * 0.97f, what + ": " + right + "/" + hits + " texels of " + fromHex + " became " + toHex + sample);
        }

        [UnityTest]
        public IEnumerator ContactSheets_AreCaptured_OverviewAndOnePerJob()
        {
            var file = CharacterVariants.File;
            const float step = 1.7f;
            // Overview: every person, 8 across.
            var order = file.variants.ToArray();
            int columns = 8, rows = (order.Length + columns - 1) / columns;
            var index = new StringBuilder();
            var overview = new GameObject("Overview").transform;
            for (int k = 0; k < order.Length; k++)
            {
                int col = k % columns, row = k / columns;
                var person = Spawn(order[k].id, new Vector3((col - (columns - 1) / 2f) * step, 0f, -row * 0.0f));
                person.transform.position = new Vector3((col - (columns - 1) / 2f) * step, (rows - 1 - row) * 2.0f, 0f);
                index.AppendLine($"{row},{col},{order[k].id},{file.Uniform(order[k].uniform).nameEn},{order[k].person},{order[k].age},{order[k].skinHex}");
            }
            // The floor under the upper rows: thin slabs.
            for (int row = 1; row < rows; row++)
            {
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(slab.GetComponent<Collider>());
                slab.transform.position = new Vector3(0f, row * 2.0f - 0.05f, 0f); slab.transform.localScale = new Vector3(columns * step + 1f, 0.1f, 2.2f);
                slab.GetComponent<Renderer>().sharedMaterial.color = new Color(0.86f, 0.87f, 0.88f);
                SceneManager.MoveGameObjectToScene(slab, _scene); _spawned.Add(slab);
            }
            yield return new WaitForSecondsRealtime(1.0f);
            foreach (var go in _spawned.Where(g => g.name.Contains("chef_haruto") || g.name.Contains("train_yuto") || g.name.Contains("tourist_mio") || g.name.Contains("nurse_hina")))
            {
                var body = go.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMesh.vertexCount).First();
                Debug.Log("[VariantProbe] " + go.name + " body y " + body.bounds.min.y.ToString("0.00") + ".." + body.bounds.max.y.ToString("0.00") + " x " + body.bounds.center.x.ToString("0.00") + " scale " + go.transform.lossyScale + " headY " + go.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head).position.y.ToString("0.00") + " " + string.Join(" | ", go.GetComponentsInChildren<MeshRenderer>(true).Select(m => m.name + " y " + m.bounds.center.y.ToString("0.00") + " h " + m.bounds.size.y.ToString("0.00") + " w " + m.bounds.size.x.ToString("0.00"))));
            }
            var cam = NewCamera();
            float height = rows * 2.0f + 0.2f, width = columns * step + 0.4f;
            Shoot(cam, new Vector3(0f, (rows * 2.0f - 0.2f) / 2f + 0.1f, 8f), Mathf.Max(height / 2f, width / 2f / (2400f / 1350f)), 2400, 1350, "01_all_people");
            File.WriteAllText(Path.Combine(Root, "01_all_people_index.csv"), "row,col,id,job,person,age,skin\n" + index);
            foreach (var go in _spawned.ToArray()) { Object.Destroy(go); _spawned.Remove(go); }
            yield return null;

            // One close-up per job.
            foreach (var uniform in file.uniforms)
            {
                var team = file.variants.Where(v => v.uniform == uniform.id).ToArray();
                for (int k = 0; k < team.Length; k++) Spawn(team[k].id, new Vector3((k - (team.Length - 1) / 2f) * 1.6f, 0f, 0f));
                yield return new WaitForSecondsRealtime(0.6f);
                float half = Mathf.Max(1.2f, (team.Length * 1.6f + 0.4f) / 2f / (1920f / 720f));
                Shoot(cam, new Vector3(0f, half - 0.05f, 8f), half, 1920, 720, "02_job_" + uniform.id);
                foreach (var go in _spawned.ToArray()) { Object.Destroy(go); _spawned.Remove(go); }
                yield return null;
            }
            Object.Destroy(cam.gameObject);
            Assert.GreaterOrEqual(Directory.GetFiles(Root, "02_job_*.png").Length, file.uniforms.Length);
        }

        private Camera NewCamera()
        {
            var cam = new GameObject("SheetCamera").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(cam.gameObject, _scene);
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.72f, 0.80f, 0.88f);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 60f;
            cam.enabled = false;
            return cam;
        }

        private static void Shoot(Camera cam, Vector3 position, float halfHeight, int width, int height, string name)
        {
            cam.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
            cam.orthographicSize = halfHeight;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;
            Directory.CreateDirectory(Root);
            File.WriteAllBytes(Path.Combine(Root, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
