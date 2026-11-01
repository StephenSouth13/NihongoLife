using System.Collections;
using System.Collections.Generic;
using System.IO;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Owner rule: every object on the main menu is authored in 01_MainMenu; code only finds and hooks it. Before play mode
    /// starts, the scene file is opened in the editor and every object path written down (prefab contents included). After the
    /// menu has run, any object whose path is not in that list was created by code. Paths still on
    /// <see cref="StillBuiltByCode"/> are the parts not moved into the scene yet. Full list: Bao_Cao/menu-avatar/runtime_objects.txt.
    /// </summary>
    [PrebuildSetup(typeof(MenuSceneManifest))]
    public class MenuNoRuntimeObjectsPlayModeTests
    {
        /// <summary>Prefixes of menu parts that code still builds at runtime (shrinks as parts move into the scene).</summary>
        private static readonly string[] StillBuiltByCode =
        {
        };

        [UnityTest, Timeout(180000)]
        public IEnumerator MainMenu_CreatesNoObjectsAtRuntime()
        {
            LogAssert.ignoreFailingMessages = true;
            UiBind.Misses.Clear();
            UiBind.Trace.Clear();
            var authored = MenuSceneManifest.Load();
            Assert.Greater(authored.Count, 100, "scene manifest was written before play mode");

            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            yield return new WaitForSecondsRealtime(3f);
            var scene = SceneManager.GetSceneByName("01_MainMenu");

            // Open and close each pop-up once: showing a panel must not build anything either.
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            Assert.NotNull(menu);
            foreach (var name in new[] { "GuideButton", "AboutButton", "SettingsButton", "LoginButton", "LeaderboardButton", "ProfileButton" })
            {
                var button = FindButton(menu.transform, name);
                if (button == null) continue;
                button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                UiModalStack.CloseTop();
                yield return new WaitForSecondsRealtime(0.2f);
            }

            var left = new Dictionary<string, int>(authored);
            var created = new List<string>();
            var all = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string path = Path(t);
                    if (left.TryGetValue(path, out int n) && n > 0) { left[path] = n - 1; continue; }
                    all.Add(path);
                    if (!Pending(path)) created.Add(path);
                }

            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../Bao_Cao/menu-avatar"));
            Directory.CreateDirectory(dir);
            File.WriteAllLines(System.IO.Path.Combine(dir, "runtime_objects.txt"), all);
            File.WriteAllLines(System.IO.Path.Combine(dir, "bind_trace.txt"), UiBind.Trace);

            Assert.IsEmpty(UiBind.Misses, "build code and authored scene out of step:\n" + string.Join("\n", UiBind.Misses));
            Assert.IsNotNull(FindButton(menu.transform, "ControlsButton"));
            Assert.IsEmpty(created, created.Count + " objects created at runtime:\n" + string.Join("\n", created.GetRange(0, Mathf.Min(40, created.Count))));
        }

        private static bool Pending(string path)
        {
            // TMP_InputField makes its text caret itself when it is enabled (TextMeshPro internals, not our code).
            if (path.EndsWith("/Caret")) return true;
            // TMP adds a sub mesh when glyphs land on a second atlas page: it depends on what earlier scenes drew.
            if (path.Contains("/TMP SubMeshUI")) return true;
            foreach (var prefix in StillBuiltByCode)
                if (path == prefix || path.StartsWith(prefix + "/")) return true;
            return false;
        }

        private static UnityEngine.UI.Button FindButton(Transform root, string name)
        {
            foreach (var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if (b.name == name) return b;
            return null;
        }

        internal static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }

    /// <summary>Writes the object paths of 01_MainMenu as saved on disk (runs in the editor before play mode).</summary>
    public sealed class MenuSceneManifest : IPrebuildSetup
    {
        private static string File => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Temp/menu_scene_manifest.txt"));

        public void Setup()
        {
#if UNITY_EDITOR
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/NihongoLife/Scenes/01_MainMenu.unity");
            var lines = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    lines.Add(MenuNoRuntimeObjectsPlayModeTests.Path(t));
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            System.IO.File.WriteAllLines(File, lines);
#endif
        }

        public static Dictionary<string, int> Load()
        {
            var map = new Dictionary<string, int>();
            if (!System.IO.File.Exists(File)) return map;
            foreach (var line in System.IO.File.ReadAllLines(File))
                map[line] = map.TryGetValue(line, out int n) ? n + 1 : 1;
            return map;
        }
    }
}