#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    // Temporary survey (deleted after use): portal surroundings in the interior scenes.
    public static class TempPortalSurvey
    {
        public static void Island()
        {
            var sb = new StringBuilder();
            EditorSceneManager.OpenScene("Assets/NihongoLife/Scenes/60_MidoriIsland.unity", OpenSceneMode.Single);
            sb.AppendLine($"fog {RenderSettings.fogMode} {RenderSettings.fogDensity} {RenderSettings.fogColor} amb {RenderSettings.ambientSkyColor} {RenderSettings.ambientEquatorColor} {RenderSettings.ambientGroundColor} sunrot {RenderSettings.sun.transform.eulerAngles} int {RenderSettings.sun.intensity}");
            var root = GameObject.Find("MidoriIsland_Zone").transform;
            foreach (Transform c in root)
            {
                sb.AppendLine($"{c.name} pos={c.position} children={c.childCount}");
                foreach (Transform g in c) sb.AppendLine($"   {g.name} pos={g.position}");
            }
            File.WriteAllText("D:/VTC_Academy/NihongoLife/_work/island_survey.txt", sb.ToString());
            EditorApplication.Exit(0);
        }

        public static void Run()
        {
            var sb = new StringBuilder();
            foreach (var s in new[] { "20_StationDistrict", "30_SushiRestaurant", "40_ HIBARICLASS", "45_HomeBedroom", "50_GameCenter", "60_MidoriIsland", "90_TestSandbox" })
            {
                var scene = EditorSceneManager.OpenScene($"Assets/NihongoLife/Scenes/{s}.unity", OpenSceneMode.Single);
                sb.AppendLine($"=== {s} sky={RenderSettings.skybox?.name}:{RenderSettings.skybox?.shader.name} fog={RenderSettings.fog} {RenderSettings.fogMode} amb={RenderSettings.ambientMode} sun={RenderSettings.sun?.name}");
                foreach (var root in scene.GetRootGameObjects()) sb.AppendLine($"  root {root.name} active={root.activeSelf} children={root.transform.childCount}");
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).GroupBy(l => l.type))
                    sb.AppendLine($"  lights {l.Key}: {l.Count()} e.g. {string.Join(", ", l.Take(4).Select(x => Path(x.transform)))}");
                foreach (var p in Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None))
                {
                    sb.AppendLine($"  PORTAL {Path(p.transform)} pos={p.transform.position} fwd={p.transform.forward}");
                    foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                                 .Where(r => Vector3.Distance(r.bounds.ClosestPoint(p.transform.position), p.transform.position) < 2.5f)
                                 .OrderBy(r => Vector3.Distance(r.bounds.center, p.transform.position)).Take(25))
                        sb.AppendLine($"     {Path(r.transform)} c={r.bounds.center} s={r.bounds.size} mat={r.sharedMaterial?.name}");
                }
            }
            File.WriteAllText("D:/VTC_Academy/NihongoLife/_work/portal_survey.txt", sb.ToString());
            EditorApplication.Exit(0);
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
#endif
