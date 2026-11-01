#if UNITY_WEBGL && !UNITY_EDITOR
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Core
{
    /// <summary>
    /// Web release check, off unless the page URL has qa=1: every area is a separate pack, built with code stripping, so
    /// what breaks only in the browser shows up area by area (a missing script, a shader that cannot run, a class stripped
    /// from the player). A browser driver calls Play / Go / Report through unityInstance.SendMessage("NLWebQa", …) and reads
    /// window.__nlQa. Going to a place uses the normal doors of SceneFlowController, so locks still apply.
    /// </summary>
    public sealed class WebQaBridge : MonoBehaviour
    {
        [DllImport("__Internal")] private static extern void NL_QaLog(int type, string message);
        [DllImport("__Internal")] private static extern void NL_QaReport(string json);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (string.IsNullOrEmpty(Application.absoluteURL) || !Application.absoluteURL.Contains("qa=1")) return;
            var go = new GameObject("NLWebQa");
            DontDestroyOnLoad(go);
            go.AddComponent<WebQaBridge>();
        }

        private void Awake()
        {
            Application.logMessageReceived += OnLog;
            Debug.LogWarning("[WebQa] log capture is on"); // the driver checks this line: an empty log then means no errors
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        private static void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            NL_QaLog((int)type, condition);
        }

        /// <summary>The town, as Play does after the intro.</summary>
        public void Play(string _)
        {
            IntroTrailer.Bypass = true;
            if (GameServices.TryGet(out SceneFlowController flow)) flow.LoadScene(WorldLocationCatalog.CityScene);
            else SceneContentLoader.LoadFallback(WorldLocationCatalog.CityScene);
        }

        /// <summary>"scene|spawn": into an area, from the town or from another area.</summary>
        public void Go(string target)
        {
            var parts = target.Split('|');
            if (GameServices.TryGet(out SceneFlowController flow)) flow.TransferZone(parts[0], parts[1], parts[0]);
        }

        /// <summary>"scene|spawn": back to the town from an area.</summary>
        public void Leave(string target)
        {
            var parts = target.Split('|');
            if (GameServices.TryGet(out SceneFlowController flow)) flow.ExitZone(parts[0], parts[1]);
        }

        /// <summary>Walks forward with the on-screen stick input for a moment ("start" / "stop").</summary>
        public void Walk(string what)
        {
            GameInputService.GetOrCreate().SetMobileMove(what == "start" ? Vector2.up : Vector2.zero);
        }

        public void Report(string _)
        {
            var sb = new StringBuilder("{");
            var active = SceneManager.GetActiveScene();
            int missing = 0, broken = 0, renderers = 0, objects = 0;
            var firstMissing = new List<string>();
            var firstBroken = new HashSet<string>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        objects++;
                        foreach (var c in t.GetComponents<Component>())
                        {
                            if (c != null) continue;
                            missing++;
                            if (firstMissing.Count < 8) firstMissing.Add(scene.name + "/" + t.name);
                        }
                    }
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    {
                        renderers++;
                        foreach (var m in r.sharedMaterials)
                        {
                            if (m != null && m.shader != null && m.shader.isSupported && m.shader.name != "Hidden/InternalErrorShader") continue;
                            broken++;
                            if (firstBroken.Count < 8) firstBroken.Add((m != null ? m.name + "/" + (m.shader != null ? m.shader.name : "no shader") : "no material") + " on " + r.name);
                        }
                    }
                }
            }
            var player = FindFirstObjectByType<NihongoLife.Player.PlayerController>();
            var hud = FindFirstObjectByType<NihongoLife.UI.HUDUI>();
            var flow = FindFirstObjectByType<SceneFlowController>();
            sb.Append("\"scene\":\"").Append(Esc(active.name)).Append("\"");
            sb.Append(",\"loading\":").Append(flow != null && flow.IsLoading ? "true" : "false");
            sb.Append(",\"objects\":").Append(objects).Append(",\"renderers\":").Append(renderers);
            sb.Append(",\"missing\":").Append(missing).Append(",\"broken\":").Append(broken);
            sb.Append(",\"player\":").Append(player != null ? "true" : "false");
            sb.Append(",\"hud\":").Append(hud != null ? "true" : "false");
            if (player != null)
            {
                var p = player.transform.position;
                sb.Append(",\"pos\":[").Append(p.x.ToString("0.00")).Append(',').Append(p.y.ToString("0.00")).Append(',').Append(p.z.ToString("0.00")).Append(']');
            }
            sb.Append(",\"fps\":").Append((1f / Mathf.Max(Time.smoothDeltaTime, 0.001f)).ToString("0"));
            sb.Append(",\"error\":\"").Append(Esc(SceneContentLoader.LastError ?? "")).Append("\"");
            sb.Append(",\"firstMissing\":[");
            for (int i = 0; i < firstMissing.Count; i++) sb.Append(i > 0 ? "," : "").Append('"').Append(Esc(firstMissing[i])).Append('"');
            sb.Append("],\"firstBroken\":[");
            int n = 0;
            foreach (var b in firstBroken) sb.Append(n++ > 0 ? "," : "").Append('"').Append(Esc(b)).Append('"');
            sb.Append("]}");
            NL_QaReport(sb.ToString());
        }

        private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
    }
}
#endif
