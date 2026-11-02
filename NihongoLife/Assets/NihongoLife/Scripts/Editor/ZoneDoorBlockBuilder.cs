using System;
using System.Linq;
using NihongoLife.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Closes the doorways of the zones with solid, invisible blocks: leaving is by the F portal in front of them, and past
    /// them there is no floor. The positions come from ZoneLeakTests, which lists every hole a player can walk into.
    /// The sushi restaurant also gets its way in and way out, two lanes split by rope posts, so guests know where to walk.
    /// Only these pieces change, the scenes are not rebuilt.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.ZoneDoorBlockBuilder.Build
    /// </summary>
    public static class ZoneDoorBlockBuilder
    {
        private const string Scenes = "Assets/NihongoLife/Scenes/";
        private const string MatDir = "Assets/NihongoLife/Materials/ZoneDoors";
        private const string Group = "ExitDoorBlocks";

        private static readonly (string scene, Vector3 centre, Vector3 size)[] Blocks =
        {
            ("30_SushiRestaurant", new Vector3(500f, 1.5f, -8.9f), new Vector3(5.4f, 3f, 0.3f)),
            ("35_HibariClinic", new Vector3(650f, 1.5f, -8.2f), new Vector3(3.8f, 3f, 0.3f)),
            ("40_ HIBARICLASS", new Vector3(1000.5f, 1.5f, -6.75f), new Vector3(3.8f, 3f, 0.3f)),
            ("20_StationDistrict", new Vector3(800f, 1.5f, 11.2f), new Vector3(5.5f, 3f, 0.3f)),
            ("20_StationDistrict", new Vector3(753.9f, 1.5f, 10f), new Vector3(0.3f, 3f, 2f)),
            ("20_StationDistrict", new Vector3(846.1f, 1.5f, 10f), new Vector3(0.3f, 3f, 2f)),
        };

        public static void Build()
        {
            try
            {
                foreach (var name in Blocks.Select(b => b.scene).Distinct())
                {
                    var scene = EditorSceneManager.OpenScene(Scenes + name + ".unity", OpenSceneMode.Single);
                    var root = scene.GetRootGameObjects().FirstOrDefault(r => r.GetComponent<SceneZoneVisibility>() != null);
                    if (root == null) throw new InvalidOperationException("No zone root in " + name);
                    var old = root.transform.Find(Group);
                    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                    var group = new GameObject(Group).transform;
                    group.SetParent(root.transform, false);
                    group.position = Vector3.zero;
                    group.rotation = Quaternion.identity;
                    int n = 0;
                    foreach (var block in Blocks.Where(b => b.scene == name))
                    {
                        var go = new GameObject("ExitDoorBlock_" + n++);
                        go.transform.SetParent(group, false);
                        go.transform.position = block.centre;
                        go.AddComponent<BoxCollider>().size = block.size;
                    }
                    if (name == "30_SushiRestaurant")
                    {
                        SushiLanes(group);
                        SushiRoom(root.transform, group);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + name);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[ZoneDoorBlockBuilder] Doorways closed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>Rope posts down the middle of the door, 入口 on one side and 出口 on the other.</summary>
        private static void SushiLanes(Transform group)
        {
            ZoneBuildKit.EnsureFolder(MatDir);
            var brass = ZoneBuildKit.Lit(MatDir, "Lane_Brass", new Color(0.78f, 0.62f, 0.3f), 0.6f);
            var rope = ZoneBuildKit.Lit(MatDir, "Lane_Rope", new Color(0.62f, 0.12f, 0.14f), 0.3f);
            var lanes = ZoneBuildKit.Group(group, "EntranceLanes");
            const float X = 500f, Top = 0.92f;
            float[] posts = { -8.55f, -7.75f, -6.95f };
            foreach (float z in posts)
            {
                ZoneBuildKit.Cylinder(lanes, "LanePost", new Vector3(X, Top * 0.5f, z), new Vector3(0.07f, Top * 0.5f, 0.07f), brass, true);
                ZoneBuildKit.Cylinder(lanes, "LaneBase", new Vector3(X, 0.025f, z), new Vector3(0.3f, 0.025f, 0.3f), brass, false);
            }
            ZoneBuildKit.Block(lanes, "LaneRope", new Vector3(X, Top - 0.06f, (posts[0] + posts[2]) * 0.5f), new Vector3(0.05f, 0.05f, posts[2] - posts[0]), rope, false);
            Sign(lanes, "LaneIn", "入口\n<size=60%>Vào · In</size>", new Vector3(X - 1.15f, 0.02f, -7.6f), new Color(0.4f, 0.9f, 0.55f));
            Sign(lanes, "LaneOut", "出口\n<size=60%>Ra · Out</size>", new Vector3(X + 1.15f, 0.02f, -7.6f), new Color(1f, 0.72f, 0.3f));
        }

        /// <summary>A room rather than a box: a ceiling (the sky showed over the walls), a warm wooden floor and panelled walls.</summary>
        private static void SushiRoom(Transform root, Transform group)
        {
            var floor = ZoneBuildKit.Lit(MatDir, "Sushi_FloorWood", new Color(0.5f, 0.33f, 0.2f), 0.3f);
            var ceiling = ZoneBuildKit.Lit(MatDir, "Sushi_Ceiling", new Color(0.78f, 0.7f, 0.58f), 0.1f);
            var panel = ZoneBuildKit.Lit(MatDir, "Sushi_WallPanel", new Color(0.42f, 0.25f, 0.14f), 0.25f);
            var plaster = ZoneBuildKit.Lit(MatDir, "Sushi_Plaster", new Color(0.93f, 0.88f, 0.78f), 0.1f);
            var beam = ZoneBuildKit.Lit(MatDir, "Sushi_Beam", new Color(0.3f, 0.18f, 0.1f), 0.2f);
            root.Find("Floor").GetComponent<Renderer>().sharedMaterial = floor;
            foreach (string wall in new[] { "BackWall", "LeftWall", "RightWall", "FrontWall_L", "FrontWall_R" })
                root.Find(wall).GetComponent<Renderer>().sharedMaterial = plaster;
            var room = ZoneBuildKit.Group(group, "RoomDressing");
            const float X = 500f;
            ZoneBuildKit.Block(room, "Ceiling", new Vector3(X, 4.45f, 0f), new Vector3(16.4f, 0.1f, 18.4f), ceiling, true);
            for (int i = -3; i <= 3; i++)
                ZoneBuildKit.Block(room, "CeilingBeam", new Vector3(X, 4.3f, i * 2.6f), new Vector3(15.6f, 0.2f, 0.22f), beam, false);
            ZoneBuildKit.Block(room, "PanelBack", new Vector3(X, 0.6f, 8.62f), new Vector3(15.5f, 1.2f, 0.06f), panel, false);
            ZoneBuildKit.Block(room, "PanelWest", new Vector3(X - 7.72f, 0.6f, 0f), new Vector3(0.06f, 1.2f, 17.4f), panel, false);
            ZoneBuildKit.Block(room, "PanelEast", new Vector3(X + 7.72f, 0.6f, 0f), new Vector3(0.06f, 1.2f, 17.4f), panel, false);
            ZoneBuildKit.Block(room, "PanelFrontWest", new Vector3(X - 5.2f, 0.6f, -8.62f), new Vector3(5.1f, 1.2f, 0.06f), panel, false);
            ZoneBuildKit.Block(room, "PanelFrontEast", new Vector3(X + 5.2f, 0.6f, -8.62f), new Vector3(5.1f, 1.2f, 0.06f), panel, false);
        }

        private static void Sign(Transform parent, string name, string text, Vector3 at, Color color)
        {
            var label = ZoneBuildKit.Text(parent, name, text, at, 0f, 0.42f, color, 1.8f);
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
