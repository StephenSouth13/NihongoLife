using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Read-only survey of a scene for planning (nothing is saved): the hierarchy two levels deep with object counts and
    /// world bounds, plus a straight-down orthographic map render. Output: _work/survey/&lt;scene&gt;_tree.txt and _map.png.
    /// Run: -executeMethod NihongoLife.EditorTools.SceneSurvey.Run (scene from NL_SURVEY_SCENE, default 90_TestSandbox).
    /// </summary>
    public static class SceneSurvey
    {
        public static void Run()
        {
            string sceneName = System.Environment.GetEnvironmentVariable("NL_SURVEY_SCENE") ?? "90_TestSandbox";
            var scene = EditorSceneManager.OpenScene($"Assets/NihongoLife/Scenes/{sceneName}.unity", OpenSceneMode.Single);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../_work/survey"));
            Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            Bounds all = default; bool any = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                sb.AppendLine(Line(root.transform, 0));
                foreach (Transform child in root.transform) sb.AppendLine(Line(child, 1));
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    if (!any) { all = r.bounds; any = true; } else all.Encapsulate(r.bounds);
                }
            }
            File.WriteAllText(Path.Combine(dir, sceneName + "_tree.txt"), sb.ToString());
            string deep = System.Environment.GetEnvironmentVariable("NL_SURVEY_DEEP");
            if (!string.IsNullOrEmpty(deep) && GameObject.Find(deep) is GameObject group)
            {
                var d = new StringBuilder();
                foreach (Transform child in group.transform)
                {
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                    string prefab = src != null ? AssetDatabase.GetAssetPath(src) : "";
                    var rs = child.GetComponentsInChildren<Renderer>(true);
                    string bb = "";
                    if (rs.Length > 0) { Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); bb = $"	{b.min.x:0.0}	{b.max.x:0.0}	{b.min.z:0.0}	{b.max.z:0.0}	{b.max.y:0.0}"; }
                    d.AppendLine($"{child.name}	{child.position.x:0.0}	{child.position.z:0.0}	{child.eulerAngles.y:0}	{child.gameObject.activeSelf}	{System.IO.Path.GetFileNameWithoutExtension(prefab)}{bb}");
                }
                File.WriteAllText(Path.Combine(dir, sceneName + "_deep.tsv"), d.ToString());
            }

            // Map: centre on the playable middle (ignore far skyline), 160 m square unless NL_SURVEY_SIZE.
            float size = float.TryParse(System.Environment.GetEnvironmentVariable("NL_SURVEY_SIZE"), out float s) ? s : 160f;
            Vector3 c = new Vector3(float.TryParse(System.Environment.GetEnvironmentVariable("NL_SURVEY_X"), out float cx) ? cx : 0f, 0f,
                float.TryParse(System.Environment.GetEnvironmentVariable("NL_SURVEY_Z"), out float cz) ? cz : 0f);
            var camGo = new GameObject("SurveyCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = size * 0.5f;
            cam.transform.SetPositionAndRotation(c + Vector3.up * 200f, Quaternion.Euler(90f, 0f, 0f));
            cam.nearClipPlane = 1f; cam.farClipPlane = 400f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var light = new GameObject("SurveyLight").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
            const int px = 2048;
            var rt = new RenderTexture(px, px, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(px, px, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, px, px), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, sceneName + "_map.png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(camGo); Object.DestroyImmediate(light.gameObject);
            Debug.Log($"[SceneSurvey] {sceneName}: bounds {all.center} size {all.size}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Lists renderers with a missing material or an error shader (they draw magenta).</summary>
        public static void BrokenMaterials()
        {
            string sceneName = System.Environment.GetEnvironmentVariable("NL_SURVEY_SCENE") ?? "90_TestSandbox";
            var scene = EditorSceneManager.OpenScene($"Assets/NihongoLife/Scenes/{sceneName}.unity", OpenSceneMode.Single);
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials)
                    if (m == null || m.shader == null || m.shader.name.Contains("InternalErrorShader") || !m.shader.isSupported)
                    {
                        string path = r.name; for (var t = r.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                        Debug.Log($"[Broken] {path} at {r.bounds.center} material={(m != null ? m.name + " / " + (m.shader != null ? m.shader.name : "no shader") : "null")}");
                    }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static string Line(Transform t, int depth)
        {
            int count = t.GetComponentsInChildren<Transform>(true).Length;
            var rs = t.GetComponentsInChildren<Renderer>(true);
            string bounds = "";
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                bounds = $" center({b.center.x:0},{b.center.z:0}) size({b.size.x:0}×{b.size.z:0})";
            }
            int colliders = t.GetComponentsInChildren<Collider>(true).Length;
            return new string(' ', depth * 2) + $"{t.name}{(t.gameObject.activeSelf ? "" : " [off]")}  objs={count} renderers={rs.Length} colliders={colliders}{bounds}";
        }
    }
}
