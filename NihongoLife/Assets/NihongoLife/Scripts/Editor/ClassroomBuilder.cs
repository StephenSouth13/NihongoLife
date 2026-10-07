#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.School;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Rebuilds the Hibari classroom (40_ HIBARICLASS) from the Styloo classroom pack: a real room with a
    /// chalkboard (today's lesson + best exam score), teacher's desk, 12 student desks, lockers, shelves, TV,
    /// curtains, kana poster and a highlighted exam desk wired to the JLPT/IELTS exam centre, plus the
    /// ClassroomRuntime that reacts to exams (lights, banners, confetti, Morita's comment).
    /// Keeps TeacherMorita, ClassmateKim, the spawn, the exit portal and the preview camera; the old box
    /// shell and sign are removed so nothing is stacked. Idempotent.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.ClassroomBuilder.Build
    /// </summary>
    public static class ClassroomBuilder
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/40_ HIBARICLASS.unity";
        private const string StylooDir = "Assets/ThirdParty/StylooClassroomAssetPack GLTF & FBX/StylooClassroomAssetPack GLTF & FBX/classroom/FBX/";
        private const string MatDir = "Assets/NihongoLife/Materials/School";
        private static readonly Vector3 O = new Vector3(1000f, 0f, 0f);
        private const float HalfX = 7.9f, HalfZ = 6.9f, Height = 4.2f;
        private static Material _floor, _wall, _wainscot, _ceiling, _lamp, _board, _poster, _deskMark, _window, _wood, _woodDark, _steel, _locker;

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = scene.GetRootGameObjects().First(r => r.name == "HibariSchool_Zone").transform;
                var keep = new HashSet<string> { "TeacherMorita", "ClassmateKim", "Spawn_school_entrance", "ExitToCity", "SchoolSceneCamera" };
                foreach (Transform child in root.Cast<Transform>().ToArray())
                    if (!keep.Contains(child.name)) Object.DestroyImmediate(child.gameObject);
                foreach (var old in root.GetComponents<ClassroomRuntime>()) Object.DestroyImmediate(old);

                CreateMaterials();
                var room = Group(root, "Classroom");
                var lights = BuildShell(room);
                BuildFurniture(room);
                var score = BuildBoard(room);
                var spot = BuildExamDesk(room);
                root.gameObject.AddComponent<ClassroomRuntime>().Configure(score, spot, lights);

                var kim = root.Find("ClassmateKim");
                if (kim != null) kim.SetPositionAndRotation(O + new Vector3(-1.1f, 0.05f, 3.2f), Quaternion.Euler(0f, 150f, 0f));

                foreach (var r in room.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; }
                foreach (var t in room.GetComponentsInChildren<Transform>(true))
                    if (t.GetComponent<TextMeshPro>() == null && t.GetComponent<Light>() == null && t.GetComponent<Collider>() == null)
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                Debug.Log("[ClassroomBuilder] Classroom rebuilt.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void CreateMaterials()
        {
            _floor = Lit(MatDir, "School_Floor", new Color(0.74f, 0.55f, 0.36f), 0.35f);
            _wall = Lit(MatDir, "School_Wall", new Color(0.93f, 0.9f, 0.82f), 0.2f);
            _wainscot = Lit(MatDir, "School_Wainscot", new Color(0.55f, 0.66f, 0.58f), 0.25f);
            _ceiling = Lit(MatDir, "School_Ceiling", new Color(0.96f, 0.96f, 0.94f), 0.1f);
            _lamp = Lit(MatDir, "School_Lamp", Color.white, 0.2f, new Color(1f, 0.98f, 0.9f) * 1.6f);
            _board = Lit(MatDir, "School_Chalkboard", new Color(0.12f, 0.27f, 0.2f), 0.15f);
            _poster = Lit(MatDir, "School_Poster", new Color(0.98f, 0.95f, 0.86f), 0.1f);
            _deskMark = Lit(MatDir, "School_ExamMark", new Color(0.85f, 0.62f, 0.25f), 0.3f, new Color(0.35f, 0.22f, 0.05f));
            _wood = Lit(MatDir, "School_DeskWood", new Color(0.78f, 0.58f, 0.36f), 0.35f);
            _woodDark = Lit(MatDir, "School_TeacherWood", new Color(0.52f, 0.34f, 0.2f), 0.4f);
            _steel = Lit(MatDir, "School_Steel", new Color(0.5f, 0.55f, 0.6f), 0.6f);
            _locker = Lit(MatDir, "School_Locker", new Color(0.35f, 0.6f, 0.62f), 0.45f);
            _window = Lit(MatDir, "School_Window", new Color(0.7f, 0.86f, 1f), 0.9f, new Color(0.55f, 0.75f, 0.95f) * 0.7f);
        }

        private static List<Light> BuildShell(Transform room)
        {
            Block(room, "Floor", O + new Vector3(0f, -0.1f, 0f), new Vector3(HalfX * 2f, 0.2f, HalfZ * 2f), _floor, true);
            Block(room, "Ceiling", O + new Vector3(0f, Height, 0f), new Vector3(HalfX * 2f, 0.15f, HalfZ * 2f), _ceiling, true);
            Block(room, "Wall_N", O + new Vector3(0f, Height / 2f, HalfZ), new Vector3(HalfX * 2f, Height, 0.25f), _wall, true);
            Block(room, "Wall_E", O + new Vector3(HalfX, Height / 2f, 0f), new Vector3(0.25f, Height, HalfZ * 2f), _wall, true);
            Block(room, "Wall_S_L", O + new Vector3(-4.95f, Height / 2f, -HalfZ), new Vector3(5.9f, Height, 0.25f), _wall, true);
            Block(room, "Wall_S_R", O + new Vector3(4.95f, Height / 2f, -HalfZ), new Vector3(5.9f, Height, 0.25f), _wall, true);
            Block(room, "Wall_S_Header", O + new Vector3(0f, 3.4f, -HalfZ), new Vector3(4f, 1.6f, 0.25f), _wall, true);
            // West wall: windows with daylight between low wall and header.
            Block(room, "Wall_W_Low", O + new Vector3(-HalfX, 0.5f, 0f), new Vector3(0.25f, 1f, HalfZ * 2f), _wall, true);
            Block(room, "Wall_W_High", O + new Vector3(-HalfX, 3.55f, 0f), new Vector3(0.25f, 1.3f, HalfZ * 2f), _wall, true);
            for (int i = 0; i < 4; i++)
            {
                float z = -4.8f + i * 3.2f;
                Block(room, "Window_" + i, O + new Vector3(-HalfX - 0.02f, 1.95f, z), new Vector3(0.05f, 1.9f, 2.6f), _window, true);
                Block(room, "WindowPost_" + i, O + new Vector3(-HalfX + 0.05f, 1.95f, z + 1.55f), new Vector3(0.12f, 1.9f, 0.12f), _wall, true);
            }
            foreach (var (name, pos, size) in new[]
            {
                ("Wainscot_N", new Vector3(0f, 0.45f, HalfZ - 0.14f), new Vector3(HalfX * 2f, 0.9f, 0.04f)),
                ("Wainscot_E", new Vector3(HalfX - 0.14f, 0.45f, 0f), new Vector3(0.04f, 0.9f, HalfZ * 2f)),
            }) Block(room, name, O + pos, size, _wainscot, false);

            var lights = new List<Light>();
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z++)
                {
                    Block(room, $"CeilingLamp_{x}_{z}", O + new Vector3(x * 3.2f, Height - 0.1f, z * 3.6f), new Vector3(1.6f, 0.06f, 0.35f), _lamp, false);
                    lights.Add(PointLight(room, $"Light_{x}_{z}", O + new Vector3(x * 3.2f, Height - 0.5f, z * 3.6f), 8.5f, 1.7f, new Color(1f, 0.95f, 0.85f)));
                }
            var sun = new GameObject("WindowDaylight").AddComponent<Light>();
            sun.transform.SetParent(room);
            sun.transform.SetPositionAndRotation(O + new Vector3(-HalfX - 2f, 3f, 0f), Quaternion.Euler(35f, 90f, 0f));
            sun.type = LightType.Spot; sun.range = 16f; sun.spotAngle = 95f; sun.intensity = 2.2f; sun.color = new Color(1f, 0.93f, 0.8f);
            lights.Add(sun);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.69f, 0.64f);
            return lights;
        }

        private static void BuildFurniture(Transform room)
        {
            var furniture = Group(room, "Furniture");
            // Student desks: 3 rows × 4, facing the board (+Z), centre aisle at x = 0.
            float[] xs = { -4.6f, -2.2f, 2.2f, 4.6f };
            float[] zs = { 1.3f, -0.9f, -3.1f };
            int n = 0;
            foreach (float z in zs)
                foreach (float x in xs)
                    Styloo(furniture, "chairtable", $"StudentDesk_{n++}", O + new Vector3(x, 0f, z), 180f, 1.0f, true);
            Styloo(furniture, "desk", "TeacherDesk", O + new Vector3(1.6f, 0f, 4.4f), 180f, 0.82f, true);
            Styloo(furniture, "lamp", "TeacherLamp", O + new Vector3(2.3f, 0.82f, 4.5f), 180f, 0.45f, false);
            Styloo(furniture, "book", "TeacherBooks", O + new Vector3(1.1f, 0.82f, 4.4f), 160f, 0.12f, false);
            for (int i = 0; i < 4; i++) Styloo(furniture, "locker", $"Locker_{i}", O + new Vector3(-4.2f + i * 1.0f, 0f, -HalfZ + 0.45f), 0f, 1.9f, true);
            Styloo(furniture, "shelf", "Shelf_E", O + new Vector3(HalfX - 0.45f, 0f, -2.6f), -90f, 1.8f, true);
            Styloo(furniture, "shelfwithwheels", "Shelf_Cart", O + new Vector3(HalfX - 0.6f, 0f, 0.6f), -90f, 1.2f, true);
            Styloo(furniture, "TVNew", "ClassTV", O + new Vector3(HalfX - 0.18f, 2.4f, 3.0f), -90f, 0.75f, false);
            Styloo(furniture, "radio", "Radio", O + new Vector3(HalfX - 0.45f, 1.8f, -2.6f), -90f, 0.25f, false);
            for (int i = 0; i < 4; i++)
                Styloo(furniture, "curtains", $"Curtains_{i}", O + new Vector3(-HalfX + 0.18f, 2.95f, -6.1f + i * 3.2f), 90f, 2.3f, false);

            // Kana poster + timetable on the east wall.
            Block(room, "Poster_Kana", O + new Vector3(HalfX - 0.15f, 2.3f, -5.0f), new Vector3(0.03f, 1.3f, 1.6f), _poster, false);
            Text(room, "Poster_Kana_Text", "あいうえお\nかきくけこ\nさしすせそ\nたちつてと", O + new Vector3(HalfX - 0.17f, 2.3f, -5.0f), 90f, 0.13f, new Color(0.2f, 0.25f, 0.35f), 1.5f);
            Block(room, "Poster_Rules", O + new Vector3(HalfX - 0.15f, 2.3f, 5.4f), new Vector3(0.03f, 1.0f, 1.2f), _poster, false);
            Text(room, "Poster_Rules_Text", "きょうしつの ルール\n<size=70%>あいさつ · しずかに · がんばろう</size>", O + new Vector3(HalfX - 0.17f, 2.3f, 5.4f), 90f, 0.09f, new Color(0.55f, 0.2f, 0.2f), 1.15f);
        }

        private static TextMeshPro BuildBoard(Transform room)
        {
            var board = Group(room, "Chalkboard");
            Styloo(board, "blackboardbig", "BoardModel", O + new Vector3(0f, 0.9f, HalfZ - 0.2f), 180f, 1.5f, false, fitWidth: 5.4f);
            Block(board, "BoardSurface", O + new Vector3(0f, 2.05f, HalfZ - 0.16f), new Vector3(5.2f, 1.45f, 0.03f), _board, false);
            Text(board, "Lesson", "きょうの べんきょう\n<size=75%>じこしょうかい · はじめまして · 〜から きました</size>", O + new Vector3(-0.9f, 2.3f, HalfZ - 0.19f), 0f, 0.16f, new Color(0.95f, 0.97f, 0.92f), 3.2f, TextAlignmentOptions.Left);
            Text(board, "Date", "10月8日 (木)\n<size=70%>にっちょく · Kim</size>", O + new Vector3(2.0f, 2.45f, HalfZ - 0.19f), 0f, 0.11f, new Color(1f, 0.95f, 0.75f), 1.4f);
            var score = Text(board, "BestScore", "もぎしけん", O + new Vector3(1.6f, 1.65f, HalfZ - 0.19f), 0f, 0.12f, new Color(1f, 0.85f, 0.45f), 2.0f);
            Text(room, "ClassSign", "ひばり日本語学院 · 1年A組  <size=70%>Lớp N5</size>", O + new Vector3(0f, 3.6f, HalfZ - 0.15f), 0f, 0.16f, new Color(0.25f, 0.3f, 0.4f), 6f);
            return score;
        }

        private static Light BuildExamDesk(Transform room)
        {
            var desk = Group(room, "ExamDesk");
            Vector3 p = O + new Vector3(-3.4f, 0f, 4.2f);
            Styloo(desk, "chairtable", "ExamDeskModel", p, 180f, 1.0f, true);
            Styloo(desk, "pencilcase", "ExamPencils", p + new Vector3(0.1f, 0.78f, 0.15f), 180f, 0.08f, false);
            Block(desk, "ExamMat", p + new Vector3(0f, 0.01f, -0.2f), new Vector3(1.6f, 0.01f, 1.8f), _deskMark, false);
            Text(desk, "ExamSign", "しけん  <size=70%>Làm bài thi · F</size>", p + new Vector3(0f, 1.75f, 0f), 0f, 0.16f, new Color(1f, 0.8f, 0.35f), 2.4f);
            var trigger = Trigger(desk, "ExamDeskInteraction", p + new Vector3(0f, 1f, -0.7f), new Vector3(1.6f, 2f, 1.4f));
            trigger.AddComponent<ClassroomExamDesk>();
            var spot = new GameObject("ExamSpotlight").AddComponent<Light>();
            spot.transform.SetParent(desk);
            spot.transform.SetPositionAndRotation(p + new Vector3(0f, 3.8f, 0f), Quaternion.Euler(90f, 0f, 0f));
            spot.type = LightType.Spot; spot.range = 6f; spot.spotAngle = 45f; spot.intensity = 4f; spot.color = new Color(1f, 0.9f, 0.7f);
            spot.enabled = false;
            return spot;
        }

        /// <summary>Styloo model scaled uniformly (to a height, or to a width when given), resting on y.</summary>
        private static GameObject Styloo(Transform parent, string model, string name, Vector3 position, float yaw, float height, bool collider, float fitWidth = 0f)
        {
            Material tint = model switch
            {
                "chairtable" => _wood,
                "desk" => _woodDark,
                "shelf" or "shelfwithwheels" => _wood,
                "locker" or "locker_001" => _locker,
                _ => null
            };
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(StylooDir + model + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing Styloo model " + model);
            var holder = Group(parent, name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(holder, false);
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            if (tint != null)
                foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterials = r.sharedMaterials.Select((m, i) => i == 0 || model != "chairtable" ? tint : _steel).ToArray();
            Bounds b = BoundsOf(instance);
            float scale = fitWidth > 0f ? fitWidth / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z)) : height / Mathf.Max(0.01f, b.size.y);
            instance.transform.localScale *= scale;
            b = BoundsOf(instance);
            Vector3 anchor = holder.position;
            instance.transform.position -= new Vector3(b.center.x - anchor.x, b.min.y - anchor.y, b.center.z - anchor.z);
            holder.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (collider)
            {
                Bounds world = BoundsOf(instance);
                var box = holder.gameObject.AddComponent<BoxCollider>();
                box.center = holder.InverseTransformPoint(world.center);
                Vector3 size = holder.InverseTransformVector(world.size);
                box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }
            return holder.gameObject;
        }
    }
}
#endif
