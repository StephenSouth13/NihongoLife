using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Read from the scene files, so what only a build drops is caught here. A component whose class sits in a file of
    /// another name is saved with a local script reference and no GUID: it works in the Editor and vanishes from the player
    /// (92 of them once took the menu's hover colours and language redraw out of the web build). A material slot saved
    /// empty means a piece was copied out of the scene that held its material.
    /// </summary>
    public class SceneIntegrityTests
    {
        private static string[] Scenes => Directory.GetFiles(Path.Combine(Application.dataPath, "NihongoLife/Scenes"), "*.unity");

        [Test]
        public void EveryComponent_HasARealScript()
        {
            foreach (var path in Scenes)
            {
                var text = File.ReadAllText(path);
                var found = Regex.Matches(text, @"m_Script: \{fileID: -?\d+\}\s+m_Name:[^\n]*\s+m_EditorClassIdentifier: ([^\n]*)")
                    .Cast<Match>().Select(m => m.Groups[1].Value.Trim()).Distinct().ToList();
                Assert.IsEmpty(found, Path.GetFileName(path) + ": classes that need a file of their own name");
            }
        }

        /// <summary>The menu binds to these at run time; without them Offline, Settings and the top bar do nothing.</summary>
        [Test]
        public void Menu_KeepsItsAuthoredParts()
        {
            var text = File.ReadAllText(Path.Combine(Application.dataPath, "NihongoLife/Scenes/01_MainMenu.unity"));
            foreach (var part in new[] { "TopCluster", "ControlsButton", "ModeSelectPanel", "FriendsPanel", "TouchControlsSettings" })
                StringAssert.Contains("m_Name: " + part, text, "01_MainMenu lost " + part);
        }

        /// <summary>A material that was never committed leaves its GUID in the scene: the slot looks filled and renders nothing.</summary>
        [Test]
        public void EveryMaterial_ExistsInTheProject()
        {
            foreach (var path in Scenes)
            {
                var missing = Regex.Matches(File.ReadAllText(path), @"^  - \{fileID: 2100000, guid: ([0-9a-f]{32}), type: 2\}", RegexOptions.Multiline)
                    .Cast<Match>().Select(m => m.Groups[1].Value).Distinct()
                    .Where(guid => AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(guid)) == null).ToList();
                Assert.IsEmpty(missing, Path.GetFileName(path) + ": material GUIDs with no asset");
            }
        }

        /// <summary>A model saved as its old Prefab object cannot be instantiated: serving a dish threw and the table stayed stuck.</summary>
        [Test]
        public void EveryDishModel_PointsAtARealObject()
        {
            foreach (var path in Directory.GetFiles(Path.Combine(Application.dataPath, "NihongoLife/Resources/Restaurants"), "*.asset"))
                StringAssert.DoesNotContain("fileID: 100100000", File.ReadAllText(path), Path.GetFileName(path));
        }

        [Test]
        public void EveryRenderer_HasItsMaterials()
        {
            foreach (var path in Scenes)
            {
                // The menu's sakura petals get theirs from MenuSakura at run time.
                if (Path.GetFileName(path) == "01_MainMenu.unity") continue;
                int empty = Regex.Matches(File.ReadAllText(path), @"m_Materials:\s+- \{fileID: 0\}").Count;
                Assert.AreEqual(0, empty, Path.GetFileName(path) + ": renderers with an empty material slot");
            }
        }
    }
}
