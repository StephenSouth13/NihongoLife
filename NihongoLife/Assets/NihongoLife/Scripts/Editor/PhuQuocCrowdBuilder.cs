using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.PhuQuoc;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Pose = NihongoLife.PhuQuoc.PhuQuocCrowd.Pose;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Fifth pass of 65_PhuQuocIsland (after island, downtown, resort and life; chained by PhuQuocIslandBuilder.Build):
    /// the land and people around the 1:1 Dương Đông area. All under GanhNgon_Restaurant/PQ_Crowd (local metres):
    ///   • land behind the town and around the resort (the resort no longer sits on a patch of sand in the open sea),
    ///     green hills with dense forest behind it, invisible edges where the land ends;
    ///   • Chợ Hải Sản Dương Đông: a covered seafood market with 15 stalls (fish, crabs, prawns, squid, basins of live
    ///     fish), a vendor behind every stall and shoppers in the aisles;
    ///   • a hẻm quarter: narrow alleys of tube houses with laundry lines, lanterns, stools, plants and residents;
    ///   • Chùa Sùng Hưng, the fish-sauce barrel house and a pepper garden (map places inside the 1:1 area);
    ///   • the island-tour terminal: a signpost (PhuQuocTourTicket) for each of the 31 map places; places outside the
    ///     1:1 town get an arrival pad at their marker with a way back (PhuQuocTourReturn);
    ///   • people (PhuQuocCrowd, CC0 Kenney mini-characters): tourist families, swimmers in swimwear in the sea and the
    ///     pool, sunbathers, resort guests and staff, alley residents and the Gánh Ngon owner and staff.
    /// Run alone: Unity -batchmode -executeMethod NihongoLife.EditorTools.PhuQuocCrowdBuilder.Build
    /// </summary>
    public static class PhuQuocCrowdBuilder
    {
        public const string RootName = "PQ_Crowd";
        public const string MatDir = "Assets/NihongoLife/Materials/PhuQuocCrowd";
        public const string MeshDir = "Assets/NihongoLife/Meshes/PhuQuocCrowd";
        private const string Minis = PhuQuocIslandBuilder.AssetRoot + "/kenney_mini-characters/FBX/";
        private const string Nature = PhuQuocIslandBuilder.AssetRoot + "/kenney_nature-kit/FBX/";
        private const string Furniture = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
        public const float AdultScale = 2.45f, KidScale = 1.65f; // Kenney minis are 0.67 high: 1.64 m adults, 1.1 m kids
        public const float SeaY = -0.02f, ResortSeaY = -0.08f, PoolY = 0.3f;
        /// <summary>The 1:1 area (restaurant-local x0,z0,x1,z1): overview markers inside it are hidden.</summary>
        public static readonly Rect Built = Rect.MinMaxRect(-112f, -80f, 312f, 114f);
        public const float LandNorth = 112f, LandEast = 310f, LandWest = -86f;
        public static readonly Vector3 TerminalArrive = new Vector3(-12f, 0f, 41.5f);

        private static readonly string[] Males = { "character-male-a", "character-male-b", "character-male-c", "character-male-d", "character-male-f", "character-male-e" };
        private static readonly string[] Females = { "character-female-a", "character-female-b", "character-female-c", "character-female-d", "character-female-e", "character-female-f" };

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(PhuQuocIslandBuilder.ScenePath, OpenSceneMode.Single);
                var island = GameObject.Find("PhuQuocIsland");
                var restaurant = island != null ? island.transform.Find("GanhNgon_Restaurant") : null;
                if (restaurant == null || restaurant.Find(PhuQuocLifeBuilder.RootName) == null) throw new InvalidOperationException("Run PhuQuocIslandBuilder.Build first");
                BuildInto(island.transform, restaurant);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + PhuQuocIslandBuilder.ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[PhuQuocCrowd] built");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1); else throw;
            }
        }

        // ───────────────────────── state of one build ─────────────────────────
        private sealed class Ctx
        {
            public Transform island, restaurant, root, people;
            public readonly List<Transform> persons = new List<Transform>();
            public readonly List<Pose> poses = new List<Pose>();
            public readonly List<Transform> groups = new List<Transform>();
            public readonly List<Vector3> from = new List<Vector3>(), to = new List<Vector3>();
            public readonly List<float> speed = new List<float>();
            public System.Random rnd = new System.Random(6503);
            public Mats m;
            public float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            public int I(int n) => rnd.Next(n);
        }

        public static Transform BuildInto(Transform island, Transform restaurant)
        {
            var old = restaurant.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform;
            root.SetParent(restaurant, false);
            var c = new Ctx { island = island, restaurant = restaurant, root = root, m = new Mats() };
            c.people = ZoneBuildKit.Group(root, "People");
            Physics.SyncTransforms();

            Land(c);
            Hills(c);
            SeafoodMarket(c);
            Alleys(c);
            Temple(c);
            FishSauce(c);
            PepperGarden(c);
            Tour(c);
            GanhNgonStaff(c);
            Beach(c);
            Resort(c);
            Families(c);
            HideOverviewInBuiltArea(island, restaurant);

            var crowd = c.people.gameObject.AddComponent<PhuQuocCrowd>();
            crowd.Configure(c.persons.ToArray(), c.poses.ToArray(), c.groups.ToArray(), c.from.ToArray(), c.to.ToArray(), c.speed.ToArray());
            Optimise(root, c.people, c.persons);
            return root;
        }

        // ───────────────────────── people ─────────────────────────
        private static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Minis + name + ".fbx");

        /// <summary>A posed Kenney mini-character: wrapper (moves) → model (scaled, bobs). No colliders.</summary>
        private static Transform Person(Ctx c, Transform parent, string name, Vector3 pos, float yaw, Pose pose, bool female, bool kid = false, bool swimwear = false, int look = -1)
        {
            var wrap = new GameObject(name).transform;
            wrap.SetParent(parent, false);
            wrap.localPosition = pos;
            wrap.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var list = female ? Females : Males;
            string id = list[look >= 0 ? look % list.Length : c.I(swimwear ? list.Length - 1 : list.Length)];
            var asset = Model(id);
            if (asset == null) return wrap;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            inst.transform.SetParent(wrap, false);
            float scale = kid ? KidScale : AdultScale;
            inst.transform.localScale = Vector3.one * scale;
            if (pose == Pose.Lie) { inst.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); inst.transform.localPosition = new Vector3(0f, 0.14f * scale, 0.3f * scale); }
            if (pose == Pose.Sit) inst.transform.localPosition = new Vector3(0f, -0.176f * scale, -0.05f * scale);
            foreach (var col in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            if (swimwear) foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (smr.name == "body-mesh") smr.sharedMesh = SwimMesh(smr.sharedMesh, id, female);
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.updateWhenOffscreen = false; smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            PhuQuocCrowd.ApplyPose(PhuQuocCrowd.Bone(wrap, "leg-left"), PhuQuocCrowd.Bone(wrap, "leg-right"), PhuQuocCrowd.Bone(wrap, "arm-left"), PhuQuocCrowd.Bone(wrap, "arm-right"), pose, 0f, 0.5f);
            c.persons.Add(wrap); c.poses.Add(pose);
            return wrap;
        }

        /// <summary>Swimwear: the body mesh with shirt, sleeves, shoes and lower legs recoloured to the character's own skin
        /// (sampled at the hand); trunks stay (the shorts' colour), women keep a top band. Saved once per model.</summary>
        private static Mesh SwimMesh(Mesh src, string id, bool female)
        {
            ZoneBuildKit.EnsureFolder(MeshDir);
            string path = MeshDir + "/Swim_" + id + ".asset";
            var have = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (have != null) return have;
            Mesh mesh; Vector3[] v; Vector2[] uv; BoneWeight[] bw;
            try
            {
                mesh = Object.Instantiate(src);
                mesh.name = "Swim_" + id;
                v = src.vertices; uv = src.uv; bw = src.boneWeights;
            }
            catch (Exception e) { Debug.LogWarning("[PhuQuocCrowd] swimwear skipped for " + id + ": " + e.Message); return src; }
            if (uv == null || uv.Length != v.Length || bw.Length != v.Length) { Debug.LogWarning("[PhuQuocCrowd] swimwear: no uv/weights on " + id); return src; }
            int hand = -1; float far = 0f;
            for (int i = 0; i < v.Length; i++)
                if ((bw[i].boneIndex0 == 4 || bw[i].boneIndex0 == 5) && Mathf.Abs(v[i].x) > far) { far = Mathf.Abs(v[i].x); hand = i; }
            if (hand >= 0)
            {
                var skin = uv[hand];
                for (int i = 0; i < v.Length; i++)
                {
                    int b = bw[i].boneIndex0;
                    bool keep;
                    if (b == 4 || b == 5) keep = false;
                    else if (b == 3) keep = female && v[i].y > 0.25f;
                    else if (b == 6) keep = true;
                    else keep = v[i].y > 0.105f;
                    if (!keep) uv[i] = skin;
                }
                mesh.uv = uv;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>A family or group walking a lane back and forth (positions local to PQ_Crowd/People).</summary>
        private static void Group(Ctx c, string name, Vector3 a, Vector3 b, int adults, int kids, float speed = 1.15f)
        {
            var g = new GameObject("Group_" + name).transform;
            g.SetParent(c.people, false);
            g.localPosition = Vector3.Lerp(a, b, c.R(0.1f, 0.9f));
            g.localRotation = Quaternion.LookRotation(b - a);
            for (int i = 0; i < adults; i++)
                Person(c, g, name + "_Adult" + i, new Vector3(adults == 1 ? 0f : -0.5f + i * 1.0f / Mathf.Max(1, adults - 1), 0f, 0f), 0f, Pose.Walk, i % 2 == 1);
            for (int k = 0; k < kids; k++)
                Person(c, g, name + "_Kid" + k, new Vector3(-0.4f + k * 0.8f, 0f, 0.9f), 0f, Pose.Walk, k % 2 == 0, true);
            c.groups.Add(g); c.from.Add(a); c.to.Add(b); c.speed.Add(speed * c.R(0.85f, 1.1f));
        }

        private static Vector3 Floor(Ctx c, Vector3 local, float from = 3f)
        {
            var top = c.restaurant.TransformPoint(local + Vector3.up * from);
            if (Physics.Raycast(top, Vector3.down, out var hit, from + 6f, ~0, QueryTriggerInteraction.Ignore))
                local.y = c.restaurant.InverseTransformPoint(hit.point).y;
            return local;
        }

        // ───────────────────────── land, hills, forest ─────────────────────────
        private static void Land(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "Land");
            const float top = -0.02f, depth = 0.6f;
            void Slab(string n, float x0, float x1, float z0, float z1, Material mat)
                => ZoneBuildKit.Block(g, "Land_" + n, new Vector3((x0 + x1) * 0.5f, top - depth * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, depth, z1 - z0), mat, true);
            Slab("Quarter", LandWest, 84f, 37f, LandNorth, c.m.earth);
            Slab("ResortNorth", 84f, LandEast, 26f, LandNorth, c.m.grass);
            Slab("ResortEast", 141f, LandEast, -6f, 26f, c.m.grass);
            Slab("ResortEastBeach", 267f, LandEast, -30f, -6f, c.m.sand);
            // Where the land ends: invisible walls (the forest and hills stay a backdrop).
            void Wall(string n, Vector3 centre, Vector3 size)
            {
                var w = new GameObject("Edge_" + n); w.transform.SetParent(g, false); w.transform.localPosition = centre;
                w.AddComponent<BoxCollider>().size = size;
            }
            Wall("North", new Vector3((LandWest + LandEast) * 0.5f, 2f, LandNorth - 6f), new Vector3(LandEast - LandWest, 6f, 1f));
            Wall("East", new Vector3(LandEast - 1f, 2f, (LandNorth - 30f) * 0.5f), new Vector3(1f, 6f, LandNorth + 30f));
            Wall("West", new Vector3(LandWest + 0.5f, 2f, (LandNorth + 30f) * 0.5f), new Vector3(1f, 6f, LandNorth - 30f));
        }

        private static void Hills(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "HillsAndForest");
            var hills = new List<(Vector3 centre, Vector3 radius)>();
            float x = LandWest - 30f; int i = 0;
            while (x < LandEast + 60f)
            {
                var r = new Vector3(c.R(34f, 52f), c.R(16f, 34f), c.R(26f, 36f));
                var centre = new Vector3(x + r.x * 0.6f, -r.y * 0.25f, LandNorth + 18f + c.R(0f, 30f));
                hills.Add((centre, r));
                var hill = ZoneBuildKit.Block(g, "PQC_Hill_" + i, centre, r * 2f, i % 3 == 0 ? c.m.forestDark : c.m.forest, false);
                Object.DestroyImmediate(hill.GetComponent<MeshFilter>());
                hill.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                x += r.x * 1.1f; i++;
            }
            // A second, higher ridge behind (Núi Chúa side) and a hill east of the resort.
            for (int k = 0; k < 5; k++)
            {
                var r = new Vector3(c.R(60f, 80f), c.R(40f, 60f), 40f);
                var centre = new Vector3(LandWest + 40f + k * 85f, -r.y * 0.3f, LandNorth + 75f);
                hills.Add((centre, r));
                var hill = ZoneBuildKit.Cylinder(g, "PQC_Hill_Back" + k, centre, r * 2f, c.m.forestDark, false);
                Object.DestroyImmediate(hill.GetComponent<MeshFilter>());
                hill.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                hill.transform.localScale = r * 2f;
            }
            var trees = new[] { "tree_default", "tree_oak", "tree_detailed", "tree_fat", "tree_tall", "tree_default_dark", "tree_oak_dark", "tree_palmTall", "tree_palmBend", "tree_blocks" }
                .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(Nature + n + ".fbx")).Where(a => a != null).ToArray();
            int t = 0;
            // Dense forest on the hills.
            foreach (var (centre, r) in hills)
            {
                int n = Mathf.RoundToInt(r.x * r.z / 70f);
                for (int k = 0; k < n; k++)
                {
                    float a = c.R(0f, Mathf.PI * 2f), d = Mathf.Sqrt(c.R(0f, 0.85f));
                    float px = centre.x + Mathf.Cos(a) * r.x * d, pz = centre.z + Mathf.Sin(a) * r.z * d;
                    float h = 1f - d * d; float py = centre.y + r.y * Mathf.Sqrt(Mathf.Max(0f, h));
                    if (py < 0.2f || pz < LandNorth - 4f) continue;
                    Tree(c, g, trees, "Tree_" + t++, new Vector3(px, py - 0.3f, pz), c.R(7f, 12f));
                }
            }
            // Foothill belt along the north edge and the land east of the resort.
            for (float fx = LandWest + 3f; fx < LandEast - 3f; fx += c.R(3.2f, 5.5f))
                for (int row = 0; row < 3; row++)
                    Tree(c, g, trees, "Tree_" + t++, new Vector3(fx + c.R(-1.5f, 1.5f), 0f, LandNorth - 9f - row * c.R(3.5f, 5f)), c.R(6f, 10f));
            for (float fz = -2f; fz < LandNorth - 20f; fz += c.R(4f, 7f))
                for (int col = 0; col < 3; col++)
                {
                    float fx = LandEast - 4f - col * c.R(4f, 6f);
                    if (fz > 4f && fz < 66f && fx < 306f && fx > 268f && col > 0) continue; // the pepper garden
                    Tree(c, g, trees, "Tree_" + t++, new Vector3(fx, 0f, fz), c.R(6f, 10f));
                }
            for (float fx = 92f; fx < 140f; fx += c.R(5f, 8f))
                Tree(c, g, trees, "Tree_" + t++, new Vector3(fx, 0f, c.R(78f, 100f)), c.R(6f, 10f));
        }

        private static void Tree(Ctx c, Transform g, GameObject[] kinds, string name, Vector3 at, float height)
        {
            if (kinds.Length == 0) return;
            Place(g, kinds[c.I(kinds.Length)], name, at, c.R(0f, 360f), height, true);
        }

        /// <summary>Instantiates a model scaled to size (height when byHeight, else longest side), standing on at.</summary>
        private static GameObject Place(Transform parent, GameObject prefab, string name, Vector3 at, float yaw, float size, bool byHeight = false)
        {
            if (prefab == null) return null;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = name;
            inst.transform.localPosition = at; inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f); inst.transform.localScale = Vector3.one;
            var b = ZoneBuildKit.BoundsOf(inst);
            float measure = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            inst.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, measure));
            b = ZoneBuildKit.BoundsOf(inst);
            inst.transform.position += Vector3.up * (parent.TransformPoint(at).y - b.min.y);
            foreach (var col in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            return inst;
        }

        private static void Tag(GameObject go, string vi, string ja) => go.AddComponent<PhuQuocPlaceTag>().Set(vi, ja);

        // ───────────────────────── Chợ Hải Sản Dương Đông ─────────────────────────
        public static readonly Rect MarketArea = Rect.MinMaxRect(-76f, 43f, -24f, 72f);

        private static void SeafoodMarket(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "ChoHaiSan_DuongDong");
            Tag(g.gameObject, "Chợ Hải Sản Dương Đông", "ズオンドン海鮮市場");
            var a = MarketArea;
            ZoneBuildKit.Block(g, "Floor", new Vector3(a.center.x, 0.02f, a.center.y), new Vector3(a.width, 0.08f, a.height), c.m.tile, true);
            for (float x = a.xMin + 1f; x <= a.xMax - 0.9f; x += 8.5f)
                foreach (var z in new[] { a.yMin + 1f, a.yMax - 1f })
                    ZoneBuildKit.Cylinder(g, "Post", new Vector3(x, 2.6f, z), new Vector3(0.35f, 2.6f, 0.35f), c.m.steel, true);
            ZoneBuildKit.Block(g, "Roof_S", new Vector3(a.center.x, 5.6f, a.center.y - a.height * 0.25f), new Vector3(a.width + 2f, 0.15f, a.height * 0.55f), c.m.roofBlue, false).transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            ZoneBuildKit.Block(g, "Roof_N", new Vector3(a.center.x, 5.6f, a.center.y + a.height * 0.25f), new Vector3(a.width + 2f, 0.15f, a.height * 0.55f), c.m.roofBlue, false).transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
            ZoneBuildKit.Block(g, "SignBoard", new Vector3(a.center.x, 4.6f, a.yMin + 0.6f), new Vector3(14f, 1.5f, 0.15f), c.m.signRed, false);
            ZoneBuildKit.Text(g, "Sign", "Chợ Hải Sản Dương Đông\n<size=55%>ズオンドン海鮮市場 · Seafood market</size>", new Vector3(a.center.x, 4.6f, a.yMin + 0.5f), 0f, 0.55f, Color.white, 13.5f);
            float[] rows = { 50f, 58.5f, 67f };
            int s = 0;
            foreach (var z in rows)
                for (int k = 0; k < 5; k++)
                {
                    float x = a.xMin + 6f + k * 10f;
                    Stall(c, g, s++, new Vector3(x, 0f, z));
                }
            // Shoppers in the aisles (pairs and singles).
            float[] aisles = { 46f, 54.5f, 63f };
            for (int i = 0; i < 6; i++)
                Group(c, "Shoppers" + i, new Vector3(a.xMin + 3f, 0.06f, aisles[i % 3] + (i < 3 ? -0.6f : 0.6f)), new Vector3(a.xMax - 3f, 0.06f, aisles[i % 3] + (i < 3 ? -0.6f : 0.6f)), i % 2 == 0 ? 2 : 1, i == 1 ? 1 : 0, 0.8f);
            // Standing customers choosing at the counters.
            for (int i = 0; i < 6; i++)
                Person(c, c.people, "Buyer_" + i, new Vector3(a.xMin + 6f + (i % 5) * 10f + c.R(-0.8f, 0.8f), 0.06f, rows[i % 3] - 1.6f), 0f, Pose.Stand, i % 2 == 0);
        }

        private static void Stall(Ctx c, Transform g, int i, Vector3 at)
        {
            var s = ZoneBuildKit.Group(g, "SeafoodStall_" + i); s.localPosition = at;
            ZoneBuildKit.Block(s, "Counter", new Vector3(0f, 0.45f, 0f), new Vector3(3.6f, 0.9f, 1.4f), c.m.tileWhite, true);
            ZoneBuildKit.Block(s, "Ice", new Vector3(0f, 0.93f, 0f), new Vector3(3.4f, 0.06f, 1.2f), c.m.ice, false);
            int kind = i % 5;
            for (int k = 0; k < 9; k++)
            {
                var p = new Vector3(-1.4f + (k % 5) * 0.7f, 1.0f, k < 5 ? -0.3f : 0.3f);
                switch ((kind + k) % 5)
                {
                    case 0: Fish(s, "Fish_" + k, p, c.m.fishSilver, c); break;
                    case 1: Fish(s, "Fish_" + k, p, c.m.fishRed, c); break;
                    case 2: Crab(s, "Crab_" + k, p, c); break;
                    case 3: for (int q = 0; q < 3; q++) ZoneBuildKit.Cylinder(s, "Prawn_" + k + "_" + q, p + new Vector3(-0.15f + q * 0.15f, 0f, 0f), new Vector3(0.06f, 0.12f, 0.06f), c.m.prawn, false).transform.localRotation = Quaternion.Euler(90f, 20f * q, 0f); break;
                    default: ZoneBuildKit.Cylinder(s, "Squid_" + k, p + Vector3.up * 0.05f, new Vector3(0.14f, 0.18f, 0.14f), c.m.squid, false).transform.localRotation = Quaternion.Euler(90f, c.R(0, 60), 0f); break;
                }
            }
            if (i % 3 == 0)
            {
                ZoneBuildKit.Cylinder(s, "Basin", new Vector3(2.4f, 0.25f, -0.2f), new Vector3(1f, 0.25f, 1f), c.m.basin, true);
                ZoneBuildKit.Cylinder(s, "BasinWater", new Vector3(2.4f, 0.47f, -0.2f), new Vector3(0.92f, 0.02f, 0.92f), c.m.water, false);
                for (int f = 0; f < 3; f++) Fish(s, "LiveFish_" + f, new Vector3(2.2f + f * 0.2f, 0.47f, -0.3f + f * 0.1f), c.m.fishSilver, c);
            }
            for (int b = 0; b < 2; b++)
                ZoneBuildKit.Block(s, "Styrofoam_" + b, new Vector3(-2.3f, 0.2f + b * 0.4f, 0.3f), new Vector3(0.8f, 0.4f, 0.55f), c.m.foam, b == 0);
            ZoneBuildKit.Block(s, "PriceBoard", new Vector3(0f, 2.3f, 0.75f), new Vector3(1.6f, 0.5f, 0.05f), c.m.Board(i), false);
            ZoneBuildKit.Cylinder(s, "Lamp", new Vector3(0f, 3.4f, 0f), new Vector3(0.35f, 0.08f, 0.35f), c.m.lamp, false);
            // The vendor behind the counter, facing the aisle; every third one is calling out.
            Person(c, s, "Vendor_" + i, new Vector3(0.3f, 0.06f, 1.3f), 180f, i % 3 == 1 ? Pose.Wave : Pose.Stand, i % 2 == 0);
        }

        private static void Fish(Transform s, string name, Vector3 p, Material mat, Ctx c)
        {
            var f = ZoneBuildKit.Cylinder(s, name, p + Vector3.up * 0.04f, new Vector3(0.12f, 0.2f, 0.06f), mat, false);
            Object.DestroyImmediate(f.GetComponent<MeshFilter>());
            f.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
            f.transform.localRotation = Quaternion.Euler(0f, c.R(-25f, 25f), 90f);
            ZoneBuildKit.Block(f.transform, "Tail", new Vector3(0f, 1.05f, 0f), new Vector3(1.1f, 0.25f, 0.3f), mat, false);
        }

        private static void Crab(Transform s, string name, Vector3 p, Ctx c)
        {
            var b = ZoneBuildKit.Block(s, name, p + Vector3.up * 0.05f, new Vector3(0.3f, 0.1f, 0.24f), c.m.crab, false, c.R(0f, 360f));
            for (int l = 0; l < 2; l++) ZoneBuildKit.Block(b.transform, "Claw" + l, new Vector3(l == 0 ? -0.55f : 0.55f, 0f, 0.6f), new Vector3(0.3f, 0.6f, 0.4f), c.m.crab, false);
        }

        // ───────────────────────── hẻm quarter ─────────────────────────
        public static readonly float[] AlleyX = { 14f, 44f, 74f };
        public const float AlleyZ0 = 38f, AlleyZ1 = 92f, AlleyCrossZ = 64f, AlleyWidth = 2.6f;

        private static void Alleys(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "Hem_Quarter");
            Tag(g.gameObject, "Hẻm phố Dương Đông", "ズオンドンの路地");
            int h = 0;
            foreach (var ax in AlleyX)
            {
                ZoneBuildKit.Block(g, "Hem_" + ax, new Vector3(ax, 0.01f, (AlleyZ0 + AlleyZ1) * 0.5f), new Vector3(AlleyWidth, 0.06f, AlleyZ1 - AlleyZ0), c.m.concrete, true);
                ZoneBuildKit.Block(g, "HemSign_" + ax, new Vector3(ax + 1.6f, 2.6f, AlleyZ0 + 0.5f), new Vector3(0.9f, 0.45f, 0.06f), c.m.signBlue, false);
                ZoneBuildKit.Text(g, "HemNo_" + ax, "Hẻm " + (100 + (int)ax), new Vector3(ax + 1.6f, 2.6f, AlleyZ0 + 0.46f), 0f, 0.2f, Color.white, 0.9f);
                foreach (var side in new[] { -1f, 1f })
                    for (float z = AlleyZ0 + 2.5f; z < AlleyZ1 - 2f; z += 4.6f)
                    {
                        if (Mathf.Abs(z - AlleyCrossZ) < 3.2f) continue;
                        House(c, g, h++, new Vector3(ax + side * (AlleyWidth * 0.5f + 3.5f), 0f, z), side);
                    }
                // Laundry lines and lanterns across the alley.
                for (float z = AlleyZ0 + 6f; z < AlleyZ1 - 4f; z += 9f)
                {
                    var line = ZoneBuildKit.Cylinder(g, "Line", new Vector3(ax, 3.1f, z), new Vector3(0.02f, AlleyWidth * 0.5f + 0.2f, 0.02f), c.m.dark, false);
                    line.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    for (int k = 0; k < 3; k++)
                        ZoneBuildKit.Block(g, "Laundry", new Vector3(ax - 0.8f + k * 0.8f, 2.75f, z), new Vector3(0.55f, 0.65f, 0.03f), c.m.Cloth(h + k), false);
                    ZoneBuildKit.Block(g, "Lantern", new Vector3(ax, 3.4f, z + 4.5f), new Vector3(0.35f, 0.45f, 0.35f), c.m.lantern, false);
                }
                // Residents: grandmas on stools, kids playing, a neighbour chatting; people walking the hẻm.
                for (int k = 0; k < 3; k++)
                {
                    float z = AlleyZ0 + 8f + k * 15f;
                    ZoneBuildKit.Block(g, "Stool", new Vector3(ax + 0.9f, 0.22f, z), new Vector3(0.35f, 0.44f, 0.35f), c.m.Cloth(k + 2), true);
                    Person(c, c.people, "Resident_" + ax + "_" + k, new Vector3(ax + 0.9f, 0.46f, z), -90f, Pose.Sit, k % 2 == 0);
                    Person(c, c.people, "AlleyKid_" + ax + "_" + k, new Vector3(ax - 0.5f, 0.04f, z + 2.5f), c.R(0f, 360f), Pose.Wave, k % 2 == 1, true);
                }
                Group(c, "Hem" + ax, new Vector3(ax - 0.4f, 0.04f, AlleyZ0 + 1f), new Vector3(ax - 0.4f, 0.04f, AlleyZ1 - 2f), 1, 0, 0.9f);
            }
            ZoneBuildKit.Block(g, "Hem_Cross", new Vector3((AlleyX[0] + AlleyX[2]) * 0.5f, 0.01f, AlleyCrossZ), new Vector3(AlleyX[2] - AlleyX[0] + AlleyWidth, 0.06f, AlleyWidth), c.m.concrete, true);
            Group(c, "HemCross", new Vector3(AlleyX[0], 0.04f, AlleyCrossZ), new Vector3(AlleyX[2], 0.04f, AlleyCrossZ), 2, 1, 0.8f);
        }

        /// <summary>A narrow tube house (nhà ống): 4.4 m front, 7 m deep, 2–3 floors, front door onto the alley.</summary>
        private static void House(Ctx c, Transform g, int i, Vector3 at, float side)
        {
            var h = ZoneBuildKit.Group(g, "AlleyHouse_" + i); h.localPosition = at; h.localRotation = Quaternion.Euler(0f, side > 0 ? 90f : -90f, 0f);
            int floors = 2 + c.I(2); float fh = 3.1f, H = floors * fh;
            ZoneBuildKit.Block(h, "Body", new Vector3(0f, H * 0.5f, 0f), new Vector3(4.2f, H, 7f), c.m.Wall(i), true);
            ZoneBuildKit.Block(h, "Roof", new Vector3(0f, H + 0.1f, 0f), new Vector3(4.4f, 0.2f, 7.2f), c.m.roofTile, false);
            ZoneBuildKit.Block(h, "Door", new Vector3(0f, 1.2f, -3.52f), new Vector3(2.2f, 2.4f, 0.06f), c.m.shutter, false);
            for (int f = 1; f < floors; f++)
            {
                ZoneBuildKit.Block(h, "Window_" + f, new Vector3(0f, f * fh + 1.5f, -3.52f), new Vector3(1.8f, 1.3f, 0.06f), c.m.glass, false);
                ZoneBuildKit.Block(h, "Balcony_" + f, new Vector3(0f, f * fh + 0.1f, -3.9f), new Vector3(3.6f, 0.12f, 0.8f), c.m.concrete, false);
                ZoneBuildKit.Block(h, "Rail_" + f, new Vector3(0f, f * fh + 0.6f, -4.28f), new Vector3(3.6f, 0.9f, 0.05f), c.m.dark, false);
            }
            if (i % 4 == 0) ZoneBuildKit.Block(h, "Plant", new Vector3(1.6f, 0.4f, -3.9f), new Vector3(0.5f, 0.8f, 0.5f), c.m.leaf, false);
        }

        // ───────────────────────── Sùng Hưng, fish sauce, pepper ─────────────────────────
        public static readonly Vector3 TempleAt = new Vector3(-12f, 0f, 72f), FishSauceAt = new Vector3(112f, 0f, 46f), PepperAt = new Vector3(287f, 0f, 36f);

        private static void Temple(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "Chua_SungHung"); g.localPosition = TempleAt;
            Tag(g.gameObject, "Chùa Sùng Hưng", "スンフン寺");
            ZoneBuildKit.Block(g, "Plinth", new Vector3(0f, 0.3f, 2f), new Vector3(14f, 0.6f, 10f), c.m.stone, true);
            ZoneBuildKit.Block(g, "Steps", new Vector3(0f, 0.15f, -3.6f), new Vector3(5f, 0.3f, 1.2f), c.m.stone, true);
            for (int k = 0; k < 4; k++) ZoneBuildKit.Cylinder(g, "Column", new Vector3(-4.5f + k * 3f, 2.3f, -1.8f), new Vector3(0.45f, 1.7f, 0.45f), c.m.templeRed, true);
            ZoneBuildKit.Block(g, "Hall", new Vector3(0f, 2.3f, 3f), new Vector3(11f, 3.4f, 6f), c.m.templeYellow, true);
            ZoneBuildKit.Block(g, "Roof1", new Vector3(0f, 4.3f, 1.8f), new Vector3(15f, 0.5f, 9f), c.m.roofTile, false);
            ZoneBuildKit.Block(g, "Roof2", new Vector3(0f, 5.4f, 2.2f), new Vector3(10f, 0.5f, 6.5f), c.m.roofTile, false);
            ZoneBuildKit.Block(g, "Ridge", new Vector3(0f, 5.9f, 2.2f), new Vector3(10.5f, 0.3f, 0.4f), c.m.templeRed, false);
            ZoneBuildKit.Cylinder(g, "Incense", new Vector3(0f, 0.55f, -5.5f), new Vector3(0.9f, 0.5f, 0.9f), c.m.bronze, true);
            ZoneBuildKit.Text(g, "Name", "Chùa Sùng Hưng\n<size=60%>スンフン寺</size>", new Vector3(0f, 3.6f, -2.0f), 0f, 0.45f, new Color(1f, 0.85f, 0.3f), 8f);
            Person(c, g, "Monk", new Vector3(1.5f, 0.6f, -1f), 180f, Pose.Stand, false, false, false, 2);
            Person(c, g, "Pilgrim_0", new Vector3(-0.6f, 0.02f, -5.2f), 0f, Pose.Stand, true);
            Person(c, g, "Pilgrim_1", new Vector3(0.7f, 0.02f, -5.3f), 0f, Pose.Stand, false);
        }

        private static void FishSauce(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "NhaThung_NuocMam"); g.localPosition = FishSauceAt;
            Tag(g.gameObject, "Nhà thùng nước mắm Khải Hoàn", "ヌクマム工場");
            ZoneBuildKit.Block(g, "Floor", new Vector3(0f, 0.04f, 4f), new Vector3(26f, 0.08f, 20f), c.m.concrete, true);
            for (float x = -12f; x <= 12f; x += 6f) foreach (var z in new[] { -5.5f, 13.5f })
                    ZoneBuildKit.Cylinder(g, "Post", new Vector3(x, 2.5f, z), new Vector3(0.3f, 2.5f, 0.3f), c.m.wood, true);
            ZoneBuildKit.Block(g, "Roof", new Vector3(0f, 5.2f, 4f), new Vector3(27f, 0.2f, 21f), c.m.roofTile, false);
            int n = 0;
            for (int ix = 0; ix < 4; ix++) for (int iz = 0; iz < 2; iz++)
                {
                    var p = new Vector3(-8.5f + ix * 5.6f, 1.6f, 1f + iz * 7f);
                    ZoneBuildKit.Cylinder(g, "Barrel_" + n++, p, new Vector3(3.2f, 1.6f, 3.2f), c.m.wood, true);
                    for (int b = 0; b < 3; b++) ZoneBuildKit.Cylinder(g, "Hoop", p + new Vector3(0f, -1.1f + b * 1.1f, 0f), new Vector3(3.3f, 0.06f, 3.3f), c.m.dark, false);
                }
            ZoneBuildKit.Block(g, "Board", new Vector3(0f, 3.6f, -6f), new Vector3(9f, 1.2f, 0.12f), c.m.signRed, false);
            ZoneBuildKit.Text(g, "Name", "Nhà thùng nước mắm Khải Hoàn\n<size=55%>ヌクマム工場 · Fish sauce</size>", new Vector3(0f, 3.6f, -6.1f), 0f, 0.4f, Color.white, 8.6f);
            Person(c, g, "Worker_0", new Vector3(-3f, 0.08f, -2f), 20f, Pose.Stand, false);
            Person(c, g, "Guide", new Vector3(2f, 0.08f, -3.5f), 180f, Pose.Wave, true);
            Group(c, "FishSauceTour", FishSauceAt + new Vector3(-10f, 0.08f, -3.8f), FishSauceAt + new Vector3(10f, 0.08f, -3.8f), 2, 1, 0.7f);
        }

        private static void PepperGarden(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "VuonTieu"); g.localPosition = PepperAt;
            Tag(g.gameObject, "Vườn tiêu", "コショウ農園");
            int n = 0;
            for (float x = -14f; x <= 14f; x += 3.5f)
                for (float z = -26f; z <= 26f; z += 3.5f)
                {
                    ZoneBuildKit.Cylinder(g, "Pole", new Vector3(x, 1.6f, z), new Vector3(0.18f, 1.6f, 0.18f), c.m.wood, false);
                    ZoneBuildKit.Cylinder(g, "Vine_" + n++, new Vector3(x, 1.4f, z), new Vector3(0.8f, 1.3f, 0.8f), c.m.leaf, false);
                }
            ZoneBuildKit.Text(g, "Name", "Vườn tiêu Phú Quốc\n<size=60%>コショウ農園</size>", new Vector3(0f, 2.6f, -29f), 0f, 0.45f, new Color(0.2f, 0.12f, 0.05f), 8f);
            ZoneBuildKit.Block(g, "Board", new Vector3(0f, 2.6f, -28.9f), new Vector3(7f, 1.3f, 0.1f), c.m.signWood, false);
            Person(c, g, "Farmer_0", new Vector3(-1.75f, 0f, -10f), 90f, Pose.Stand, true);
            Person(c, g, "Farmer_1", new Vector3(5.25f, 0f, 6f), -90f, Pose.Stand, false);
        }

        // ───────────────────────── island tour: 31 places ─────────────────────────
        private static Vector3 TownSpot(Ctx c, string id)
        {
            switch (id)
            {
                case "ganh_ngon": return new Vector3(0f, 0.15f, -6f);
                case "duong_dong": return Floor(c, new Vector3(-24f, 0f, -6f));
                case "cho_dem": return Floor(c, new Vector3(60f, 0f, -46.5f));
                case "cho_duong_dong": return new Vector3(MarketArea.center.x, 0.06f, MarketArea.yMin - 1.5f);
                case "dinh_cau": return Floor(c, new Vector3(92f, 0f, 14f));
                case "sung_hung": return TempleAt + new Vector3(0f, 0f, -7.5f);
                case "fish_sauce": return FishSauceAt + new Vector3(0f, 0.08f, -9f);
                case "pepper_farm": return PepperAt + new Vector3(0f, 0f, -31f);
                default: return new Vector3(float.NaN, 0f, 0f);
            }
        }

        private static void Tour(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "TourTerminal");
            Tag(g.gameObject, "Bến xe Tour đảo", "島ツアーのバスターミナル");
            ZoneBuildKit.Block(g, "Plaza", new Vector3(-12f, 0.02f, 49f), new Vector3(22f, 0.08f, 18f), c.m.tile, true);
            ZoneBuildKit.Block(g, "Shelter", new Vector3(-12f, 3.2f, 53.5f), new Vector3(22f, 0.2f, 6f), c.m.roofBlue, false);
            for (float x = -22f; x <= -2f; x += 5f) ZoneBuildKit.Cylinder(g, "ShelterPost", new Vector3(x, 1.6f, 55.8f), new Vector3(0.2f, 1.6f, 0.2f), c.m.steel, true);
            ZoneBuildKit.Text(g, "Name", "Bến xe Tour đảo · 島ツアー\n<size=55%>31 điểm đến · 31 destinations</size>", new Vector3(-12f, 4.1f, 41.4f), 0f, 0.5f, new Color(0.1f, 0.25f, 0.45f), 12f);
            ZoneBuildKit.Block(g, "NameBoard", new Vector3(-12f, 4.1f, 41.5f), new Vector3(12.5f, 1.4f, 0.1f), c.m.ice, false);
            var arrive = new GameObject("Terminal_Arrive").transform; arrive.SetParent(g, false); arrive.localPosition = TerminalArrive + Vector3.up * 0.06f;
            Person(c, g, "TourGuide", new Vector3(-23f, 0.06f, 44f), 90f, Pose.Wave, true);
            Group(c, "TourQueue", new Vector3(-21f, 0.06f, 51.6f), new Vector3(-3f, 0.06f, 51.6f), 2, 2, 0.6f);

            var map = PhuQuocMap.Load();
            var places = c.island.Find("Places");
            var pads = ZoneBuildKit.Group(c.root, "TourPads");
            for (int i = 0; i < map.places.Length; i++)
            {
                var p = map.places[i];
                // Signposts: two rows of 16 across the plaza.
                var post = ZoneBuildKit.Trigger(g, "Ticket_" + p.id, new Vector3(-21.5f + (i % 16) * 1.27f, 1.0f, i < 16 ? 45f : 48.5f), new Vector3(1.1f, 2f, 1.1f));
                ZoneBuildKit.Cylinder(post.transform, "Pole", new Vector3(0f, 0f, 0.2f), new Vector3(0.08f, 1f, 0.08f), c.m.steel, false);
                ZoneBuildKit.Block(post.transform, "Panel", new Vector3(0f, 0.75f, 0.15f), new Vector3(1.15f, 0.62f, 0.05f), c.m.Board(i), false);
                var label = ZoneBuildKit.Text(post.transform, "Label", p.nameVi + "\n<size=70%>" + p.nameJa + "</size>", new Vector3(0f, 0.75f, 0.12f), 0f, 0.12f, Color.white, 1.05f);
                label.textWrappingMode = TMPro.TextWrappingModes.Normal; label.enableAutoSizing = true; label.fontSizeMin = 0.35f; label.fontSizeMax = 1.2f;
                label.rectTransform.sizeDelta = new Vector2(1.05f, 0.55f);
                var ticket = post.AddComponent<PhuQuocTourTicket>();
                ticket.placeId = p.id; ticket.nameVi = p.nameVi; ticket.nameEn = p.nameEn; ticket.nameJa = p.nameJa;

                var spot = TownSpot(c, p.id);
                var dest = new GameObject("Arrive_" + p.id).transform;
                if (!float.IsNaN(spot.x)) { dest.SetParent(g, false); dest.localPosition = spot + Vector3.up * 0.05f; }
                else
                {
                    var marker = places != null ? places.Find("Place_" + p.id) : null;
                    var w = marker != null ? c.restaurant.InverseTransformPoint(marker.position) : map.ToWorld(p);
                    var at = new Vector3(w.x, 0f, w.z - 8f);
                    if (Built.Contains(new Vector2(at.x, at.z))) // a marker over the 1:1 area: push the pad out along its bearing
                    {
                        var dir = new Vector2(at.x, at.z).normalized;
                        while (Built.Contains(new Vector2(at.x, at.z))) { at.x += dir.x * 4f; at.z += dir.y * 4f; }
                        at.x += dir.x * 14f; at.z += dir.y * 14f;
                    }
                    dest.SetParent(pads, false);
                    dest.localPosition = Pad(c, pads, p, at);
                    dest.localRotation = Quaternion.identity;
                    dest.name = "Arrive_" + p.id;
                }
                ticket.destination = dest;
            }
            foreach (var ret in pads.GetComponentsInChildren<PhuQuocTourReturn>(true)) ret.destination = arrive;
        }

        /// <summary>Arrival pad (12 m) at a place outside the 1:1 town: name board, a few props of its kind, a visitor
        /// and the bus-stop post back to the terminal. Returns the arrival point (local to pads).</summary>
        private static Vector3 Pad(Ctx c, Transform pads, PhuQuocPlace p, Vector3 at)
        {
            var top = c.restaurant.TransformPoint(at + Vector3.up * 120f);
            float ground = 0f;
            if (Physics.Raycast(top, Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                ground = Mathf.Max(0f, c.restaurant.InverseTransformPoint(hit.point).y);
            var g = ZoneBuildKit.Group(pads, "Pad_" + p.id); g.localPosition = new Vector3(at.x, ground, at.z);
            Tag(g.gameObject, p.nameVi, p.nameJa);
            ZoneBuildKit.Block(g, "Deck", new Vector3(0f, 0.15f, 0f), new Vector3(12f, 0.3f, 12f), p.category == "beach" || p.category == "island" ? c.m.sand : c.m.tile, true);
            foreach (var (pos, size) in new[] { (new Vector3(0f, 1f, 6.2f), new Vector3(12.4f, 2f, 0.4f)), (new Vector3(0f, 1f, -6.2f), new Vector3(12.4f, 2f, 0.4f)), (new Vector3(6.2f, 1f, 0f), new Vector3(0.4f, 2f, 12.4f)), (new Vector3(-6.2f, 1f, 0f), new Vector3(0.4f, 2f, 12.4f)) })
            { var rail = new GameObject("Rail"); rail.transform.SetParent(g, false); rail.transform.localPosition = pos + Vector3.up * 0.3f; rail.AddComponent<BoxCollider>().size = size; }
            ZoneBuildKit.Block(g, "Board", new Vector3(0f, 2.6f, 5.4f), new Vector3(7f, 1.4f, 0.12f), c.m.Board(p.id.Length), false);
            ZoneBuildKit.Text(g, "Name", p.nameVi + "\n<size=60%>" + p.nameJa + "</size>", new Vector3(0f, 2.6f, 5.33f), 0f, 0.42f, Color.white, 6.8f);
            var nature = new[] { "tree_palmTall", "tree_palmBend", "tree_default", "tree_oak" };
            for (int k = 0; k < 3; k++)
            {
                string kind = p.category == "beach" || p.category == "island" ? nature[k % 2] : nature[2 + k % 2];
                Place(g, AssetDatabase.LoadAssetAtPath<GameObject>(Nature + kind + ".fbx"), "Prop_" + k, new Vector3(-4.5f + k * 4.5f, 0.3f, 4.2f), c.R(0f, 360f), c.R(5f, 7f), true);
            }
            var post = ZoneBuildKit.Trigger(g, "ReturnToTown", new Vector3(4.2f, 1.3f, -3.5f), new Vector3(1.4f, 2.2f, 1.4f));
            ZoneBuildKit.Cylinder(post.transform, "Pole", new Vector3(0f, 0f, 0f), new Vector3(0.12f, 1.1f, 0.12f), c.m.steel, false);
            ZoneBuildKit.Block(post.transform, "Sign", new Vector3(0f, 0.9f, 0f), new Vector3(1.3f, 0.5f, 0.06f), c.m.signBlue, false);
            ZoneBuildKit.Text(post.transform, "Label", "← Dương Đông\n<size=70%>バス停</size>", new Vector3(0f, 0.9f, -0.04f), 0f, 0.14f, Color.white, 1.3f);
            post.AddComponent<PhuQuocTourReturn>();
            Person(c, g, "Visitor_" + p.id, new Vector3(-2f, 0.3f, 1.5f), 180f, Pose.Wave, p.id.Length % 2 == 0);
            return new Vector3(at.x, ground + 0.35f, at.z - 2.5f);
        }

        /// <summary>Overview markers (1:100) that sit over the 1:1 area would float in the street or the sea: hide them.</summary>
        private static void HideOverviewInBuiltArea(Transform island, Transform restaurant)
        {
            var places = island.Find("Places");
            if (places == null) return;
            foreach (Transform marker in places)
            {
                var l = restaurant.InverseTransformPoint(marker.position);
                if (Built.Contains(new Vector2(l.x, l.z))) marker.gameObject.SetActive(false);
            }
        }

        // ───────────────────────── Gánh Ngon staff ─────────────────────────
        private static void GanhNgonStaff(Ctx c)
        {
            var g = ZoneBuildKit.Group(c.root, "GanhNgon_Staff");
            Vector3 F(float x, float z) => Floor(c, new Vector3(x, 0f, z), 2.2f);
            Person(c, g, "Staff_Receptionist", F(0f, -1.1f), 180f, Pose.Stand, true, false, false, 1);
            Person(c, g, "Staff_Chef", F(-3f, 4.1f), 180f, Pose.Stand, false, false, false, 2);
            Person(c, g, "Staff_Bartender", F(3f, 4.1f), 180f, Pose.Stand, false, false, false, 3);
            Person(c, g, "Staff_ManagerMai", F(1.1f, 3.4f), 200f, Pose.Stand, true, false, false, 0);
            Person(c, g, "Owner_ChuNam", F(-1.8f, -3.2f), 180f, Pose.Wave, false, false, false, 0);
            for (int w = 0; w < 2; w++)
            {
                var a = F(-2.2f, 1.6f + w * 0.1f); var b = F(2.2f, 1.6f + w * 0.1f);
                var grp = new GameObject("Group_Waiter" + w).transform; grp.SetParent(c.people, false);
                var la = c.people.InverseTransformPoint(c.restaurant.TransformPoint(a)); var lb = c.people.InverseTransformPoint(c.restaurant.TransformPoint(b));
                grp.localPosition = Vector3.Lerp(la, lb, w * 0.6f + 0.2f);
                Person(c, grp, "Staff_Waiter" + w, Vector3.zero, 0f, Pose.Walk, w == 0, false, false, w + 4);
                c.groups.Add(grp); c.from.Add(la); c.to.Add(lb); c.speed.Add(0.9f);
            }
        }

        // ───────────────────────── beach, sea, resort ─────────────────────────
        private static void Beach(Ctx c)
        {
            // Swimmers in swimwear, chest-deep, spread along the town beach.
            for (int i = 0; i < 18; i++)
            {
                float x = -90f + i * 10.5f + c.R(-3f, 3f);
                if (x > 108f) x = c.R(-60f, 60f);
                float z = PhuQuocLifeBuilder.CoastZ(x) - c.R(4f, 22f);
                bool kid = i % 6 == 5;
                Person(c, c.people, "Swimmer_" + i, new Vector3(x, SeaY - (kid ? 0.42f : 0.62f), z), c.R(0f, 360f), Pose.Swim, i % 2 == 0, kid, true);
                if (i % 4 == 0) ZoneBuildKit.Cylinder(c.people, "SwimRing_" + i, new Vector3(x, SeaY + 0.05f, z), new Vector3(1.0f, 0.06f, 1.0f), c.m.Cloth(i), false);
            }
            // Kids splashing at the waterline, parents watching.
            for (int i = 0; i < 6; i++)
            {
                float x = -70f + i * 26f, z = PhuQuocLifeBuilder.CoastZ(x) + 0.5f;
                Person(c, c.people, "BeachKid_" + i, new Vector3(x, Mathf.Max(SeaY, PhuQuocLifeBuilder.SandHeight(x, z)) - 0.15f, z), c.R(140f, 220f), Pose.Wave, i % 2 == 0, true, true);
                float pz = z + 3f;
                Person(c, c.people, "BeachParent_" + i, new Vector3(x + 1.2f, PhuQuocLifeBuilder.SandHeight(x + 1.2f, pz), pz), 180f, Pose.Stand, i % 2 == 1, false, true);
            }
            // Sunbathers on towels.
            for (int i = 0; i < 10; i++)
            {
                float x = -85f + i * 18f + c.R(-2f, 2f), z = c.R(-58f, -52f);
                float y = PhuQuocLifeBuilder.SandHeight(x, z);
                ZoneBuildKit.Block(c.people, "Towel_" + i, new Vector3(x, y + 0.02f, z), new Vector3(0.9f, 0.03f, 1.9f), c.m.Cloth(i + 1), false);
                Person(c, c.people, "Sunbather_" + i, new Vector3(x, y + 0.03f, z), 0f, Pose.Lie, i % 2 == 1, false, true);
            }
        }

        private static void Resort(Ctx c)
        {
            var o = PhuQuocResortBuilder.Origin;
            // Sea swimmers in front of the resort.
            for (int i = 0; i < 12; i++)
            {
                float x = o.x - 55f + i * 10f + c.R(-2f, 2f), z = o.z - c.R(34f, 52f);
                Person(c, c.people, "ResortSwimmer_" + i, new Vector3(x, ResortSeaY - 0.62f, z), c.R(0f, 360f), Pose.Swim, i % 2 == 1, false, true);
            }
            // Pool: swimmers and someone on the edge.
            var pool = o + PhuQuocResortBuilder.PoolCentre;
            for (int i = 0; i < 7; i++)
                Person(c, c.people, "PoolSwimmer_" + i, new Vector3(pool.x - 11f + i * 3.4f, PoolY - 0.62f, pool.z - 2.6f + (i % 3) * 2.6f), c.R(0f, 360f), Pose.Swim, i % 2 == 0, i == 3, true);
            // Sunbathers on the resort beach.
            for (int i = 0; i < 9; i++)
            {
                float x = o.x - 54f + i * 13.5f, z = o.z - 16f + (i % 2) * 3f;
                ZoneBuildKit.Block(c.people, "ResortTowel_" + i, new Vector3(x + 3f, 0.08f, z), new Vector3(0.9f, 0.03f, 1.9f), c.m.Cloth(i + 3), false);
                Person(c, c.people, "ResortSunbather_" + i, new Vector3(x + 3f, 0.09f, z), 0f, Pose.Lie, i % 2 == 0, false, true);
            }
            // Pool bar guests on the stools, the barman, lobby guests and the receptionist.
            var bar = c.restaurant.Find(PhuQuocResortBuilder.RootName + "/PoolBar");
            if (bar != null)
                for (int s = 0; s < 3; s++)
                {
                    var stool = bar.Find("Stool_" + s);
                    if (stool == null) continue;
                    var b = ZoneBuildKit.BoundsOf(stool.gameObject);
                    var l = c.people.InverseTransformPoint(new Vector3(b.center.x, b.max.y, b.center.z));
                    Person(c, c.people, "BarGuest_" + s, l, 0f, Pose.Sit, s != 1, false, s == 0);
                }
            Person(c, c.people, "Resort_Barman", new Vector3(o.x + 16f, 0.5f, o.z + 18.2f), 180f, Pose.Stand, false, false, false, 3);
            Person(c, c.people, "Resort_Receptionist", new Vector3(o.x, 0.45f, o.z + 42.4f), 180f, Pose.Stand, true, false, false, 4);
            Person(c, c.people, "Resort_Guest_0", new Vector3(o.x - 0.7f, 0.45f, o.z + 40.2f), 0f, Pose.Stand, true);
            Person(c, c.people, "Resort_Guest_1", new Vector3(o.x + 0.6f, 0.45f, o.z + 40.0f), 0f, Pose.Stand, false);
            // Guests walking the resort paths and families on the resort sand.
            Group(c, "ResortWings", new Vector3(o.x - 58f, 0.12f, o.z + 3f), new Vector3(o.x + 58f, 0.12f, o.z + 3f), 2, 1);
            Group(c, "ResortWest", new Vector3(o.x - 41f, 0.12f, o.z + 5f), new Vector3(o.x - 41f, 0.12f, o.z + 40f), 2, 0);
            Group(c, "ResortSand", new Vector3(o.x - 55f, 0.06f, o.z - 22f), new Vector3(o.x + 55f, 0.06f, o.z - 22f), 2, 2, 0.9f);
        }

        private static void Families(Ctx c)
        {
            float P = PhuQuocLifeBuilder.PromenadeZ;
            Group(c, "Promenade0", new Vector3(-80f, 0.25f, P - 0.4f), new Vector3(80f, 0.25f, P - 0.4f), 2, 2);
            Group(c, "Promenade1", new Vector3(-60f, 0.25f, P + 0.5f), new Vector3(100f, 0.25f, P + 0.5f), 2, 1);
            Group(c, "Promenade2", new Vector3(-95f, 0.25f, P), new Vector3(40f, 0.25f, P), 2, 0, 0.9f);
            Group(c, "Duong30Thang4", new Vector3(-70f, 0.25f, PhuQuocLifeBuilder.NorthStreetZ1 + 1.0f), new Vector3(70f, 0.25f, PhuQuocLifeBuilder.NorthStreetZ1 + 1.0f), 2, 1);
            Group(c, "TranHungDaoN", Floor(c, new Vector3(-80f, 0f, -6.5f)), Floor(c, new Vector3(80f, 0f, -6.5f)), 2, 2);
            Group(c, "TranHungDaoS", Floor(c, new Vector3(80f, 0f, -24f)), Floor(c, new Vector3(-80f, 0f, -24f)), 2, 1);
            Group(c, "Boardwalk", new Vector3(86f, 0.22f, 14f), new Vector3(140f, 0.22f, 14f), 2, 1, 1f);
            Group(c, "MarketFront", new Vector3(-74f, 0.06f, 41.5f), new Vector3(0f, 0.06f, 41.5f), 2, 2, 0.9f);
        }

        // ───────────────────────── optimisation ─────────────────────────
        private static void Optimise(Transform root, Transform people, List<Transform> persons)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == people || t.IsChildOf(people)) continue;
                if (persons.Any(p => t == p || t.IsChildOf(p))) continue;
                if (t.GetComponent<TMPro.TextMeshPro>() != null || t.TryGetComponent<Collider>(out var col) && col.isTrigger) continue;
                if (t.GetComponentInParent<PhuQuocTourTicket>() != null || t.GetComponentInParent<PhuQuocTourReturn>() != null) continue;
                if (t.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }
            foreach (var groupName in new[] { "HillsAndForest", "Hem_Quarter", "ChoHaiSan_DuongDong", "TourPads" })
            {
                var parent = root.Find(groupName);
                if (parent == null) continue;
                foreach (Transform item in parent)
                {
                    if (item.name.StartsWith("PQC_Hill")) continue;
                    var rs = item.GetComponentsInChildren<Renderer>(true).Where(r => !(r is SkinnedMeshRenderer)).ToArray();
                    if (rs.Length == 0) continue;
                    var lod = item.gameObject.AddComponent<LODGroup>();
                    float size = ZoneBuildKit.BoundsOf(item.gameObject).size.magnitude;
                    lod.SetLODs(new[] { new LOD(size < 3f ? 0.012f : size < 12f ? 0.006f : 0.002f, rs) });
                    lod.RecalculateBounds();
                }
            }
            // Small market goods, tree shadows: off (perf); hills and buildings keep theirs.
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r.bounds.size.magnitude < 1.2f) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ───────────────────────── materials ─────────────────────────
        private sealed class Mats
        {
            public readonly Material earth, grass, sand, forest, forestDark, tile, tileWhite, steel, roofBlue, roofTile, signRed, signBlue, signWood, ice, water, basin,
                foam, lamp, fishSilver, fishRed, crab, prawn, squid, concrete, dark, lantern, shutter, glass, leaf, stone, templeRed, templeYellow, bronze, wood;
            private static readonly Color[] Walls =
            {
                new Color(0.98f, 0.86f, 0.55f), new Color(0.72f, 0.88f, 0.82f), new Color(0.98f, 0.75f, 0.68f), new Color(0.82f, 0.86f, 0.98f),
                new Color(0.97f, 0.95f, 0.88f), new Color(0.75f, 0.9f, 0.6f), new Color(0.98f, 0.8f, 0.9f), new Color(0.9f, 0.78f, 0.6f),
            };
            private static readonly Color[] Cloths =
            {
                new Color(0.9f, 0.2f, 0.25f), new Color(0.15f, 0.45f, 0.85f), new Color(0.98f, 0.8f, 0.1f), new Color(0.2f, 0.7f, 0.4f),
                new Color(0.95f, 0.5f, 0.1f), new Color(0.6f, 0.3f, 0.8f), new Color(0.98f, 0.98f, 0.98f), new Color(0.1f, 0.75f, 0.8f),
            };
            private static readonly Color[] Boards =
            {
                new Color(0.75f, 0.15f, 0.12f), new Color(0.1f, 0.35f, 0.6f), new Color(0.1f, 0.5f, 0.35f), new Color(0.85f, 0.5f, 0.1f),
                new Color(0.45f, 0.2f, 0.55f), new Color(0.15f, 0.45f, 0.5f), new Color(0.6f, 0.35f, 0.15f), new Color(0.2f, 0.25f, 0.35f),
            };

            public Mats()
            {
                ZoneBuildKit.EnsureFolder(MatDir);
                earth = L("PQC_Earth", new Color(0.55f, 0.6f, 0.36f), 0.05f);
                grass = L("PQC_Grass", new Color(0.38f, 0.6f, 0.3f), 0.05f);
                sand = L("PQC_Sand", new Color(0.97f, 0.9f, 0.74f), 0.1f);
                forest = L("PQC_Forest", new Color(0.24f, 0.5f, 0.24f), 0.05f);
                forestDark = L("PQC_ForestDark", new Color(0.16f, 0.38f, 0.2f), 0.05f);
                tile = L("PQC_Tile", new Color(0.78f, 0.74f, 0.68f), 0.25f);
                tileWhite = L("PQC_TileWhite", new Color(0.92f, 0.94f, 0.95f), 0.5f);
                steel = L("PQC_Steel", new Color(0.45f, 0.5f, 0.55f), 0.5f);
                roofBlue = L("PQC_RoofBlue", new Color(0.25f, 0.45f, 0.65f), 0.3f);
                roofTile = L("PQC_RoofTile", new Color(0.7f, 0.3f, 0.18f), 0.15f);
                signRed = L("PQC_SignRed", new Color(0.75f, 0.12f, 0.1f), 0.3f);
                signBlue = L("PQC_SignBlue", new Color(0.1f, 0.3f, 0.65f), 0.3f);
                signWood = L("PQC_SignWood", new Color(0.85f, 0.7f, 0.45f), 0.2f);
                ice = L("PQC_Ice", new Color(0.85f, 0.95f, 1f), 0.8f);
                water = L("PQC_Water", new Color(0.2f, 0.55f, 0.7f), 0.9f);
                basin = L("PQC_Basin", new Color(0.2f, 0.4f, 0.8f), 0.4f);
                foam = L("PQC_Styrofoam", new Color(0.97f, 0.97f, 0.97f), 0.1f);
                lamp = ZoneBuildKit.Lit(MatDir, "PQC_Lamp", new Color(1f, 0.95f, 0.8f), 0.3f, new Color(1f, 0.9f, 0.7f) * 1.2f);
                fishSilver = L("PQC_FishSilver", new Color(0.7f, 0.76f, 0.82f), 0.85f);
                fishRed = L("PQC_FishRed", new Color(0.88f, 0.35f, 0.3f), 0.7f);
                crab = L("PQC_Crab", new Color(0.85f, 0.25f, 0.12f), 0.5f);
                prawn = L("PQC_Prawn", new Color(0.98f, 0.55f, 0.45f), 0.5f);
                squid = L("PQC_Squid", new Color(0.97f, 0.93f, 0.9f), 0.6f);
                concrete = L("PQC_Concrete", new Color(0.66f, 0.65f, 0.62f), 0.15f);
                dark = L("PQC_Dark", new Color(0.1f, 0.1f, 0.11f), 0.3f);
                lantern = ZoneBuildKit.Lit(MatDir, "PQC_Lantern", new Color(0.9f, 0.15f, 0.1f), 0.4f, new Color(1f, 0.3f, 0.1f) * 0.8f);
                shutter = L("PQC_Shutter", new Color(0.55f, 0.58f, 0.6f), 0.4f);
                glass = L("PQC_Glass", new Color(0.35f, 0.5f, 0.55f), 0.9f);
                leaf = L("PQC_Leaf", new Color(0.2f, 0.5f, 0.2f), 0.1f);
                stone = L("PQC_Stone", new Color(0.75f, 0.72f, 0.66f), 0.2f);
                templeRed = L("PQC_TempleRed", new Color(0.7f, 0.1f, 0.08f), 0.35f);
                templeYellow = L("PQC_TempleYellow", new Color(0.98f, 0.82f, 0.4f), 0.2f);
                bronze = L("PQC_Bronze", new Color(0.55f, 0.4f, 0.2f), 0.6f);
                wood = L("PQC_Wood", new Color(0.55f, 0.37f, 0.22f), 0.15f);
            }

            public Material Wall(int i) => L("PQC_Wall" + (i % Walls.Length), Walls[i % Walls.Length], 0.1f);
            public Material Cloth(int i) => L("PQC_Cloth" + (i % Cloths.Length), Cloths[i % Cloths.Length], 0.2f);
            public Material Board(int i) => L("PQC_Board" + (i % Boards.Length), Boards[i % Boards.Length], 0.3f);
            private static Material L(string name, Color col, float s) => ZoneBuildKit.Lit(MatDir, name, col, s);
        }
    }
}
