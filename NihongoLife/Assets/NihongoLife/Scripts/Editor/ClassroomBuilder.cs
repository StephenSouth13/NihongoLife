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
                BuildStudents(room);
                var score = BuildBoard(room);
                var spot = BuildExamDesk(room);
                root.gameObject.AddComponent<ClassroomRuntime>().Configure(score, spot, lights);

                var kim = root.Find("ClassmateKim");
                if (kim != null) kim.SetPositionAndRotation(O + new Vector3(-1.1f, 0.05f, 3.2f), Quaternion.Euler(0f, 150f, 0f));

                foreach (var r in room.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; }
                var classmates = room.Find("Students");
                foreach (var t in room.GetComponentsInChildren<Transform>(true))
                    if (t.GetComponent<TextMeshPro>() == null && t.GetComponent<Light>() == null && t.GetComponent<Collider>() == null && (classmates == null || !t.IsChildOf(classmates)))
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

        /// <summary>Seat (bench) and desk top of each student chair-table, measured from its mesh.</summary>
        private static readonly Dictionary<int, (Vector3 seat, Vector3 deskCentre, Vector3 deskSize, float facing)> Seats = new Dictionary<int, (Vector3, Vector3, Vector3, float)>();

        private static void BuildFurniture(Transform room)
        {
            Seats.Clear();
            var furniture = Group(room, "Furniture");
            // Student desks: 3 rows × 4, facing the board (+Z), centre aisle at x = 0.
            float[] xs = { -4.6f, -2.2f, 2.2f, 4.6f };
            float[] zs = { 1.3f, -0.9f, -3.1f };
            int n = 0;
            foreach (float z in zs)
                foreach (float x in xs)
                {
                    // Yaw 0: the bench (with its backrest) is behind the desk, so a student on it faces the desk and the board.
                    // At 180° the bench was on the board side. 0.85 m tall: a kid sits with the body above the desk top.
                    var unit = Styloo(furniture, "chairtable", $"StudentDesk_{n}", O + new Vector3(x, 0f, z), 0f, 0.85f, true);
                    Seats[n++] = MeasureSeat(unit.transform);
                }
            Styloo(furniture, "desk", "TeacherDesk", O + new Vector3(1.6f, 0f, 4.4f), 180f, 0.82f, true);
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

        // ─────────── Classmates ───────────

        private static readonly string[] Kids = { "haruto", "yui", "sota", "hina", "riku", "mio", "yuto", "sakura" };

        /// <summary>
        /// A class in session: classmates sit at nine of the twelve desks facing the board (the humanoid Sit pose,
        /// pinned to the chair by SeatedPassenger), three desks stay free. No colliders — they never block the aisle.
        /// </summary>
        private static void BuildStudents(Transform room)
        {
            var students = Group(room, "Students");
            float[] xs = { -4.6f, -2.2f, 2.2f, 4.6f };
            float[] zs = { 1.3f, -0.9f, -3.1f };
            var empty = new HashSet<int> { 2, 7, 9 };
            int n = 0, kid = 0;
            foreach (float z in zs)
                foreach (float x in xs)
                {
                    int desk = n++;
                    if (empty.Contains(desk)) continue;
                    string id = Kids[kid++ % Kids.Length];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/NihongoLife/Resources/Characters/Kids/NL_kid_{id}.prefab");
                    if (prefab == null) continue;
                    var student = new GameObject($"Student_{desk}_{id}");
                    student.transform.SetParent(students, false);
                    var seat = Seats[desk];
                    student.transform.SetPositionAndRotation(seat.seat + Vector3.up * 0.02f, Quaternion.Euler(0f, seat.facing, 0f)); // on the bench, facing the desk and the board
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    visual.name = "Visual";
                    visual.transform.SetParent(student.transform, false);
                    foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                    var animation = student.AddComponent<NihongoLife.Core.CharacterAnimationController>();
                    animation.SetAnimator(visual.GetComponentInChildren<Animator>(true));
                    student.AddComponent<NihongoLife.World.SeatedPassenger>().Configure(true);
                }
        }

        /// <summary>
        /// Where to sit at a Styloo chair-table, read from its mesh (an offset guessed once put every student inside the
        /// desk: only the heads showed above the desk tops). Along the unit's depth through its middle, the highest
        /// surface of each 4 cm slice: 0.3–0.6 m is the bench, 0.6–0.95 m the desk top. The student sits on the bench
        /// centre facing the desk; an empty "DeskTopArea" marker (position = desk top centre, scale = its size) lets the
        /// play-mode test check that nobody sits inside a desk.
        /// </summary>
        private static (Vector3 seat, Vector3 deskCentre, Vector3 deskSize, float facing) MeasureSeat(Transform unit)
        {
            // Points over every triangle's surface (a low-poly model has too few vertices to profile).
            var points = new List<Vector3>();
            foreach (var mf in unit.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var v = mesh.vertices.Select(x => unit.InverseTransformPoint(mf.transform.TransformPoint(x))).ToArray();
                var tris = mesh.triangles;
                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    Vector3 a = v[tris[t]], b = v[tris[t + 1]], c = v[tris[t + 2]];
                    int n = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max((b - a).magnitude, (c - a).magnitude, (c - b).magnitude) / 0.02f), 1, 120);
                    for (int i = 0; i <= n; i++)
                        for (int j = 0; i + j <= n; j++)
                            points.Add(a + (b - a) * (i / (float)n) + (c - a) * (j / (float)n));
                }
            }
            if (points.Count == 0) throw new InvalidOperationException(unit.name + " has no mesh to measure");
            float minX = points.Min(p => p.x), maxX = points.Max(p => p.x), midX = (minX + maxX) * 0.5f, band = (maxX - minX) * 0.2f;
            float minZ = points.Min(p => p.z), maxZ = points.Max(p => p.z);
            const float Step = 0.04f;
            int slices = Mathf.CeilToInt((maxZ - minZ) / Step) + 1;
            var top = new float[slices];
            for (int i = 0; i < slices; i++) top[i] = float.NegativeInfinity;
            foreach (var p in points)
            {
                if (Mathf.Abs(p.x - midX) > band) continue;
                int i = Mathf.Clamp(Mathf.FloorToInt((p.z - minZ) / Step), 0, slices - 1);
                if (p.y > top[i]) top[i] = p.y;
            }
            (float from, float to) Range(float low, float high)
            {
                // The longest run of slices whose top lies in [low, high].
                int best = 0, bestStart = -1, run = 0;
                for (int i = 0; i < slices; i++)
                {
                    if (top[i] >= low && top[i] <= high) { run++; if (run > best) { best = run; bestStart = i - run + 1; } }
                    else run = 0;
                }
                if (bestStart < 0) throw new InvalidOperationException($"{unit.name}: no surface between {low} and {high} m");
                return (minZ + bestStart * Step, minZ + (bestStart + best) * Step);
            }
            var bench = Range(0.3f, 0.6f);
            var desk = Range(0.6f, 1.05f);
            float benchZ = (bench.from + bench.to) * 0.5f, deskZ = (desk.from + desk.to) * 0.5f;
            float deskTop = Enumerable.Range(0, slices).Where(i => minZ + i * Step >= desk.from && minZ + i * Step < desk.to).Max(i => top[i]);
            Vector3 seat = unit.TransformPoint(new Vector3(midX, 0f, benchZ));
            Vector3 deskCentre = unit.TransformPoint(new Vector3(midX, deskTop, deskZ));
            Vector3 toDesk = deskCentre - seat; toDesk.y = 0f;
            float facing = Quaternion.LookRotation(toDesk.normalized).eulerAngles.y;
            var marker = new GameObject("DeskTopArea").transform;
            marker.SetParent(unit, false);
            marker.position = deskCentre;
            marker.rotation = Quaternion.Euler(0f, facing, 0f);
            marker.localScale = new Vector3(maxX - minX, 0.05f, desk.to - desk.from);
            Debug.Log($"[ClassroomBuilder] {unit.name}: bench z {bench.from:0.00}–{bench.to:0.00}, desk z {desk.from:0.00}–{desk.to:0.00} (top {deskTop:0.00} m), facing {facing:0}°");
            return (new Vector3(seat.x, 0f, seat.z), deskCentre, marker.localScale, facing);
        }

        private static TextMeshPro BuildBoard(Transform room)
        {
            var board = Group(room, "Chalkboard");
            // The green surface in a wooden frame with a chalk tray. (The Styloo "blackboardbig" model rendered as a white
            // panel standing in front of the board — it hid the lesson, so it is not used.)
            Block(board, "BoardSurface", O + new Vector3(0f, 2.05f, HalfZ - 0.16f), new Vector3(5.2f, 1.45f, 0.03f), _board, false);
            Block(board, "BoardFrame_Top", O + new Vector3(0f, 2.81f, HalfZ - 0.15f), new Vector3(5.4f, 0.08f, 0.06f), _woodDark, false);
            Block(board, "BoardFrame_Bottom", O + new Vector3(0f, 1.29f, HalfZ - 0.15f), new Vector3(5.4f, 0.08f, 0.06f), _woodDark, false);
            Block(board, "BoardFrame_Left", O + new Vector3(-2.66f, 2.05f, HalfZ - 0.15f), new Vector3(0.08f, 1.6f, 0.06f), _woodDark, false);
            Block(board, "BoardFrame_Right", O + new Vector3(2.66f, 2.05f, HalfZ - 0.15f), new Vector3(0.08f, 1.6f, 0.06f), _woodDark, false);
            Block(board, "ChalkTray", O + new Vector3(0f, 1.25f, HalfZ - 0.24f), new Vector3(5.2f, 0.04f, 0.14f), _woodDark, false);
            Block(board, "Chalk_White", O + new Vector3(-1.4f, 1.285f, HalfZ - 0.24f), new Vector3(0.12f, 0.025f, 0.025f), _wall, false);
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
            Styloo(desk, "chairtable", "ExamDeskModel", p, 0f, 0.85f, true);
            Styloo(desk, "pencilcase", "ExamPencils", p + new Vector3(0.1f, 0.86f, 0.35f), 180f, 0.08f, false);
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
