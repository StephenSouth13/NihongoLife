using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>Batch screenshots of 65_PhuQuocIsland (overview, street, beach, restaurant).
    /// Unity -batchmode -executeMethod NihongoLife.EditorTools.PhuQuocShots.Capture (env NL_SHOTS_DIR). Never saves the scene.</summary>
    public static class PhuQuocShots
    {
        public static void Capture()
        {
            int code = 0;
            try
            {
                string dir = Environment.GetEnvironmentVariable("NL_SHOTS_DIR");
                if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Application.dataPath, "../Temp/phuquoc_shots");
                Directory.CreateDirectory(dir);
                EditorSceneManager.OpenScene(PhuQuocIslandBuilder.ScenePath, OpenSceneMode.Single);
                var r = GameObject.Find("GanhNgon_Restaurant");
                if (r == null) throw new InvalidOperationException("GanhNgon_Restaurant missing");
                // The zone borrows the town's sun and sky at runtime: add temporary ones for the shot.
                var sun = new GameObject("ShotSun").AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.25f; sun.color = new Color(1f, 0.96f, 0.88f);
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/NihongoLife/Materials/PhuQuoc/PQ_Skybox.mat");
                if (sky != null) RenderSettings.skybox = sky;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f);
                RenderSettings.ambientEquatorColor = new Color(0.55f, 0.58f, 0.55f);
                RenderSettings.ambientGroundColor = new Color(0.32f, 0.3f, 0.26f);
                Vector3 o = r.transform.position;
                Shot(dir, "01_overview.png", o + new Vector3(-20f, 75f, -150f), o + new Vector3(10f, 0f, -20f), 55f);
                Shot(dir, "02_street.png", o + new Vector3(-30f, 1.8f, -23.8f), o + new Vector3(10f, 1.2f, -12f), 60f);
                Shot(dir, "03_beach.png", o + new Vector3(-22f, 4.5f, -49f), o + new Vector3(-6f, 0f, -73f), 60f);
                Shot(dir, "04_restaurant.png", o + new Vector3(7f, 4f, -14f), o + new Vector3(0f, 1f, 0f), 60f);
                Shot(dir, "05_intersection.png", o + new Vector3(62f, 16f, -38f), o + new Vector3(44f, 0f, -15f), 60f);
                Shot(dir, "06_hotels_street.png", o + new Vector3(-10f, 3f, 15f), o + new Vector3(5f, 4f, 30f), 65f);
                Shot(dir, "07_night_market.png", o + new Vector3(50f, 2.5f, -38f), o + new Vector3(66f, 1.5f, -50f), 60f);
                Shot(dir, "08_resort.png", o + new Vector3(205f, 38f, -95f), o + new Vector3(205f, 0f, 0f), 60f);
                Shot(dir, "09_seafood_market.png", o + new Vector3(-50f, 3.2f, 40f), o + new Vector3(-50f, 1f, 56f), 65f);
                Shot(dir, "10_hem_alley.png", o + new Vector3(44f, 1.7f, 39f), o + new Vector3(44f, 1.6f, 60f), 60f);
                Shot(dir, "11_swimmers.png", o + new Vector3(-10f, 5f, -62f), o + new Vector3(0f, 0f, -85f), 60f);
                Shot(dir, "12_hills_forest.png", o + new Vector3(60f, 30f, 10f), o + new Vector3(100f, 10f, 120f), 65f);
                Shot(dir, "13_ganh_ngon_staff.png", o + new Vector3(0f, 2.2f, -7.5f), o + new Vector3(0f, 1f, 2f), 60f);
                Shot(dir, "14_tour_terminal.png", o + new Vector3(-12f, 3f, 36f), o + new Vector3(-12f, 1f, 48f), 60f);
                Shot(dir, "15_resort_pool.png", o + new Vector3(190f, 5f, -2f), o + new Vector3(208f, 0f, 12f), 60f);
                Shot(dir, "16_families_promenade.png", o + new Vector3(-30f, 3f, -40f), o + new Vector3(0f, 1f, -48f), 60f);
                Object.DestroyImmediate(sun.gameObject);
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static void Shot(string dir, string file, Vector3 pos, Vector3 look, float fov)
        {
            const int W = 1600, H = 900;
            var go = new GameObject("ShotCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = pos; cam.transform.LookAt(look);
            cam.fieldOfView = fov; cam.nearClipPlane = 0.2f; cam.farClipPlane = 4000f;
            cam.clearFlags = CameraClearFlags.Skybox;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, file), tex.EncodeToPNG());
            Object.DestroyImmediate(tex); cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
            Debug.Log("[PhuQuocShots] " + file);
        }
    }
}
