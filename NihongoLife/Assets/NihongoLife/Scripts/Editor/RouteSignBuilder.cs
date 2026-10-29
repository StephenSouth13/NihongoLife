#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NihongoLife.EditorTools.ZoneBuildKit;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Bakes the direction signposts of every map from Resources/Navigation/route_signs.json: a pole, one board per target, text on both faces
    /// and a RouteSignPost that keeps the text in the interface language. Idempotent: the map's root is deleted and rebuilt.
    /// Headless: Unity -batchmode -executeMethod NihongoLife.EditorTools.RouteSignBuilder.Build (CityTownBuilder calls BuildInto for the town).
    /// </summary>
    public static class RouteSignBuilder
    {
        private const string MatDir = "Assets/NihongoLife/Materials/Navigation";
        private const string ScenesDir = "Assets/NihongoLife/Scenes/";
        private const float BoardWidth = 3.0f, BoardHeight = 0.27f, BoardPitch = 0.3f, TopBoardY = 2.5f;

        public static void Build()
        {
            try
            {
                RouteSignCatalog.Reload();
                foreach (var map in RouteSignCatalog.File.maps)
                {
                    var scene = EditorSceneManager.OpenScene(ScenesDir + map.scene + ".unity", OpenSceneMode.Single);
                    BuildInto(scene, map.scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + map.scene);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[RouteSignBuilder] Done: " + string.Join(", ", RouteSignCatalog.File.maps.Select(m => m.scene)));
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>Rebuilds the signs of <paramref name="sceneName"/> in the already open <paramref name="scene"/>.</summary>
        public static Transform BuildInto(Scene scene, string sceneName)
        {
            var map = RouteSignCatalog.Map(sceneName) ?? throw new InvalidOperationException("No route signs for " + sceneName);
            foreach (var old in scene.GetRootGameObjects().Where(r => r.name == map.root).ToList()) Object.DestroyImmediate(old);
            var root = new GameObject(map.root);
            SceneManager.MoveGameObjectToScene(root, scene);

            var metal = Lit(MatDir, "RouteSign_Metal", new Color(0.62f, 0.65f, 0.7f), 0.6f);
            var green = Lit(MatDir, "RouteSign_Green", new Color(0.12f, 0.58f, 0.36f), 0.35f);
            var blue = Lit(MatDir, "RouteSign_Blue", new Color(0.13f, 0.3f, 0.6f), 0.3f);

            foreach (var post in map.posts)
            {
                var group = Group(root.transform, post.name);
                group.position = RouteSignCatalog.ToVector(post.pos);
                group.rotation = Quaternion.Euler(0f, post.yaw, 0f);
                float height = TopBoardY + BoardHeight;
                Cylinder(group, "Pole", new Vector3(-BoardWidth * 0.5f - 0.05f, height * 0.5f, 0f), new Vector3(0.12f, height * 0.5f, 0.12f), metal, true);
                var views = new List<RouteSignPost.BoardView>();
                for (int i = 0; i < post.boards.Length; i++)
                {
                    var board = post.boards[i];
                    var target = Resolve(board, scene);
                    float y = TopBoardY - i * BoardPitch;
                    Block(group, "Board_" + i, new Vector3(0f, y, 0f), new Vector3(BoardWidth, BoardHeight, 0.06f), board.color == "green" ? green : blue, false);
                    // Front face: read by someone walking along the post's +Z. Back face: read by someone coming the other way.
                    var front = Text(group, "Board_" + i + "_N", board.id, new Vector3(0f, y, -0.04f), 0f, 0.1f, Color.white, BoardWidth - 0.1f);
                    var back = Text(group, "Board_" + i + "_S", board.id, new Vector3(0f, y, 0.04f), 180f, 0.1f, Color.white, BoardWidth - 0.1f);
                    views.Add(new RouteSignPost.BoardView { id = board.id, target = target, front = front, back = back });
                }
                var component = group.gameObject.AddComponent<RouteSignPost>();
                component.Configure(sceneName, post.id, views.ToArray());
                component.Refresh();
            }
            return root.transform;
        }

        private static Vector3 Resolve(RouteSignBoardData board, Scene scene)
        {
            if (string.IsNullOrEmpty(board.anchor)) return RouteSignCatalog.ToVector(board.pos);
            Transform found = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (Matches(t, board.anchor)) { found = t; break; }
            if (found == null) throw new InvalidOperationException("Route sign target not found: " + board.anchor);
            return new Vector3(found.position.x, 0f, found.position.z);
        }

        /// <summary>"Name" matches the first object with that name, "Parent/Name" one whose parent has that name.</summary>
        private static bool Matches(Transform t, string anchor)
        {
            int slash = anchor.LastIndexOf('/');
            if (slash < 0) return t.name == anchor;
            return t.name == anchor.Substring(slash + 1) && t.parent != null && t.parent.name == anchor.Substring(0, slash).Split('/').Last();
        }
    }
}
#endif
