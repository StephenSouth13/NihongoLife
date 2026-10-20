using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Game Center pictures rendered from the project's own low-poly models (TASK-N), so the arcade, the Order Rush
    /// menu and the prize counter show real food and plush toys instead of placeholder icons:
    /// Resources/MiniGames/Food/&lt;dish&gt;.png and Resources/MiniGames/Prizes/&lt;prize&gt;.png (256×256, transparent,
    /// three-quarter view, soft shadow) plus the big plush on the prize shelf (Resources/Items/prize_plush_big.png).
    /// Ids without a fitting model (neko, tori, hoshi, kagi) are left to the games' existing fallbacks.
    /// Run: -executeMethod NihongoLife.EditorTools.ArcadeArtBuilder.Build
    /// </summary>
    public static class ArcadeArtBuilder
    {
        private const string Sushi = "Assets/ThirdParty/Sushi Restaurant Kit - May 2023-20260920T035054Z-1-001/Sushi Restaurant Kit - May 2023/";
        private const string Kenney = "Assets/ThirdParty/Kenney/kenney_food-kit/Models/FBX format/";
        private const string Furniture = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
        private const string FoodPack = "Assets/ThirdParty/Ultimate Food Pack - Oct 2019-20260920T035121Z-1-001/Ultimate Food Pack - Oct 2019/FBX/";
        private const string Animals = "Assets/ThirdParty/Ultimate Animated Animals - July 2021-20260920T035520Z-1-001/Ultimate Animated Animals - July 2021/FBX/";
        private const string Out = "Assets/NihongoLife/Resources/";

        // (output path, model, yaw, pitch)
        private static readonly (string path, string model, float yaw, float pitch)[] Jobs =
        {
            ("MiniGames/Food/onigiri", Sushi + "Food/FBX/Food_Onigiri.fbx", 160f, 10f),
            ("MiniGames/Food/sushi", Sushi + "Food/FBX/Food_SalmonNigiri.fbx", 150f, 20f),
            ("MiniGames/Food/pan", Kenney + "bread.fbx", 150f, 15f),
            ("MiniGames/Food/keeki", Kenney + "cake.fbx", 150f, 18f),
            ("MiniGames/Food/ocha", Kenney + "cup-tea.fbx", 150f, 15f),
            ("MiniGames/Food/koohii", Kenney + "cup-coffee.fbx", 150f, 15f),
            ("MiniGames/Food/tamago", Sushi + "Food/FBX/Food_TamagoNigiri.fbx", 150f, 20f),
            ("MiniGames/Food/sakana", Kenney + "fish.fbx", 90f, 10f),
            ("MiniGames/Food/ringo", Kenney + "apple.fbx", 150f, 10f),
            ("MiniGames/Food/aisu", Kenney + "ice-cream.fbx", 150f, 8f),
            ("MiniGames/Food/karaage", FoodPack + "ChickenLeg.fbx", 140f, 20f),
            ("MiniGames/Food/nikuman", Sushi + "Food/FBX/Food_Chukaman.fbx", 150f, 12f),
            ("MiniGames/Food/ramen", Sushi + "Food/FBX/Food_Ramen.fbx", 150f, 28f),
            ("MiniGames/Food/ichigo", Kenney + "strawberry.fbx", 150f, 10f),

            ("MiniGames/Prizes/kuma", Furniture + "bear.fbx", 160f, 8f),
            ("MiniGames/Prizes/inu", Animals + "ShibaInu.fbx", 130f, 8f),
            ("MiniGames/Prizes/usagi", Sushi + "Characters/Normal/FBX/Rabbit_Pink.fbx", 160f, 6f),
            ("MiniGames/Prizes/sakana", Kenney + "fish.fbx", 90f, 10f),
            ("MiniGames/Prizes/ringo", Kenney + "apple.fbx", 150f, 10f),
            ("MiniGames/Prizes/ichigo", Kenney + "strawberry.fbx", 150f, 10f),
            ("MiniGames/Prizes/hana", Sushi + "Decoration/FBX/Decoration_SakuraFlower.fbx", 150f, 30f),
            ("MiniGames/Prizes/okashi", Kenney + "lollypop.fbx", 150f, 8f),

            ("Items/prize_plush_big", Furniture + "bear.fbx", 160f, 8f),
        };

        public static void Build()
        {
            int made = 0;
            var missing = new List<string>();
            try
            {
                foreach (var job in Jobs)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(job.model);
                    if (model == null) { missing.Add(job.model); continue; }
                    string file = Out + job.path + ".png";
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    Render(model, file, job.path.StartsWith("Items/") ? 128 : 256, job.yaw, job.pitch);
                    made++;
                }
                AssetDatabase.Refresh();
                Debug.Log($"[ArcadeArtBuilder] {made} pictures rendered." + (missing.Count > 0 ? " Missing models: " + string.Join(", ", missing) : ""));
                if (Application.isBatchMode) EditorApplication.Exit(missing.Count == 0 ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void Render(GameObject source, string file, int size, float yaw, float pitch)
        {
            const int super = 2; // render larger, then downsample for clean edges
            var stage = new GameObject("ArtStage") { transform = { position = new Vector3(0f, -800f, 0f) } };
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, stage.transform);
            foreach (var a in model.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            foreach (var l in model.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);
            IslandBuilder.ConvertMaterials(model);
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var key = new GameObject("Key").AddComponent<Light>();
            key.transform.SetParent(stage.transform, false);
            key.type = LightType.Directional; key.intensity = 1.25f; key.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.transform.SetParent(stage.transform, false);
            fill.type = LightType.Directional; fill.intensity = 0.45f; fill.color = new Color(0.85f, 0.9f, 1f); fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

            var cam = new GameObject("ArtCamera").AddComponent<Camera>();
            cam.transform.SetParent(stage.transform, false);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 22f; cam.nearClipPlane = 0.01f; cam.farClipPlane = 500f;
            Bounds b = BoundsOf(model);
            float distance = b.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.98f;
            cam.transform.position = b.center + Quaternion.Euler(pitch + 12f, 0f, 0f) * Vector3.back * distance;
            cam.transform.LookAt(b.center);

            int big = size * super;
            var target = new RenderTexture(big, big, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = target;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var raw = new Texture2D(big, big, TextureFormat.RGBA32, false);
            raw.ReadPixels(new Rect(0, 0, big, big), 0, 0);
            raw.Apply();
            RenderTexture.active = old;
            cam.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(stage);

            var result = WithShadow(Downsample(raw, size));
            File.WriteAllBytes(file, result.EncodeToPNG());
            Object.DestroyImmediate(raw); Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(file);
            if (AssetImporter.GetAtPath(file) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = size;
                importer.SaveAndReimport();
            }
        }

        private static Texture2D Downsample(Texture2D src, int size)
        {
            int f = src.width / size;
            var dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var s = src.GetPixels();
            var d = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color sum = Color.clear;
                    float alpha = 0f;
                    for (int j = 0; j < f; j++)
                        for (int i = 0; i < f; i++)
                        {
                            var c = s[(y * f + j) * src.width + x * f + i];
                            sum += new Color(c.r * c.a, c.g * c.a, c.b * c.a, 0f); // premultiplied
                            alpha += c.a;
                        }
                    d[y * size + x] = alpha > 0f ? new Color(sum.r / alpha, sum.g / alpha, sum.b / alpha, alpha / (f * f)) : Color.clear;
                }
            dst.SetPixels(d);
            dst.Apply();
            return dst;
        }

        /// <summary>A soft oval shadow under the object, behind it.</summary>
        private static Texture2D WithShadow(Texture2D tex)
        {
            int n = tex.width;
            var px = tex.GetPixels();
            int minX = n, maxX = -1, minY = n;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    if (px[y * n + x].a > 0.2f) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); }
            if (maxX < 0) return tex;
            float cx = (minX + maxX) * 0.5f, cy = Mathf.Max(n * 0.06f, minY + n * 0.01f);
            float rx = Mathf.Max(n * 0.12f, (maxX - minX) * 0.42f), ry = rx * 0.22f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    float r = dx * dx + dy * dy;
                    if (r >= 1f) continue;
                    float sa = 0.32f * (1f - r) * (1f - r);
                    var c = px[y * n + x];
                    // shadow behind: out = c over shadow
                    float a = c.a + sa * (1f - c.a);
                    if (a <= 0f) continue;
                    Color shadow = new Color(0.05f, 0.06f, 0.1f);
                    px[y * n + x] = new Color((c.r * c.a + shadow.r * sa * (1f - c.a)) / a, (c.g * c.a + shadow.g * sa * (1f - c.a)) / a, (c.b * c.a + shadow.b * sa * (1f - c.a)) / a, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
