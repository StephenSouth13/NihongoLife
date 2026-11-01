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
    /// Owner rule: the menu's objects are authored in 01_MainMenu, not created by code. Objects loaded from the scene file have a
    /// positive instance id and objects made at runtime (new GameObject / Instantiate) a negative one, so a negative id in the
    /// checked part of the menu means code created it. The full list of runtime objects in the scene goes to
    /// Bao_Cao/menu-avatar/runtime_objects.txt.
    /// </summary>
    public class MenuNoRuntimeObjectsPlayModeTests
    {
        private static readonly string[] Authored = { "MainMenuPanel/TopCluster", "MenuAvatarStage" };

        [UnityTest, Timeout(120000)]
        public IEnumerator MainMenu_TopBarAndCharacter_AreAuthored_NotCreatedAtRuntime()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            yield return new WaitForSecondsRealtime(2.5f);
            var scene = SceneManager.GetSceneByName("01_MainMenu");

            var probe = new GameObject("probe");
            Assert.Less(probe.GetInstanceID(), 0, "sanity: a runtime object has a negative id");
            Object.Destroy(probe);

            var created = new List<string>();
            var all = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.gameObject.GetInstanceID() < 0) all.Add(Path(t));

            foreach (var path in Authored)
            {
                var node = Find(scene, path);
                Assert.NotNull(node, path + " is in 01_MainMenu");
                Assert.Greater(node.gameObject.GetInstanceID(), 0, path + " comes from the scene file");
                foreach (var t in node.GetComponentsInChildren<Transform>(true))
                {
                    // Bones inside a figure come from its imported model and Unity expands them on load with fresh ids, so for
                    // the character stage only the stage, its light and the figure roots themselves are checked.
                    if (path == "MenuAvatarStage" && t.parent != node && t != node) continue;
                    if (t.gameObject.GetInstanceID() < 0) created.Add(Path(t));
                }
            }
            var controls = Find(scene, "MainMenuPanel/TopCluster/ControlsButton");
            Assert.NotNull(controls, "the touch-controls pill is authored in the top bar");
            Assert.AreEqual(1, Object.FindObjectsByType<MenuAvatarStage>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);

            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../Bao_Cao/menu-avatar"));
            Directory.CreateDirectory(dir);
            File.WriteAllLines(System.IO.Path.Combine(dir, "runtime_objects.txt"), all);
            Assert.IsEmpty(created, "created at runtime:\n" + string.Join("\n", created));
        }

        private static Transform Find(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != parts[parts.Length - 1]) continue;
                    if (Path(t).EndsWith(path)) return t;
                }
            }
            return null;
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
