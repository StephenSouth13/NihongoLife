#if UNITY_EDITOR
using System.Collections.Generic;
using NihongoLife.World;
using TMPro;
using UnityEngine;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// The view out of the window follows the line on the game's map (StationTravelController.Line):
    /// Hibari → Gakuen-mae (houses, paddies) · Gakuen-mae → Minato (Hibari Japanese School, its sports ground, sakura)
    /// · Minato → Midori Island (the harbour: cranes, containers, warehouses, fishing boats) · Midori → the airport
    /// (the bridge over the sea, then the island's farms) · the airport (runway, terminal, tower, aeroplanes).
    /// Plus the platform with the station's name board (slides in at each stop) and a tunnel between legs.
    /// Scenery-local: +Z is the window side the passenger looks at, the ground is at y −1.1 (the carriage floor is 0).
    /// </summary>
    public static partial class StationTrainBuilder
    {
        private static Material _schoolWall, _schoolWindow, _schoolRoof, _sakura, _trunk, _trackRed, _white, _craneOrange, _apron, _water,
            _warehouse, _roofBlue, _asphalt, _planeWhite, _planeBlue, _terminalGlass, _tunnel, _tunnelLamp, _platformFloor, _tactile, _boardWhite, _boatHull, _steelGreen, _barnRed;

        private static void RouteMaterials()
        {
            _schoolWall = Lit(MatDir, "Route_SchoolWall", new Color(0.93f, 0.92f, 0.88f), 0.2f);
            _schoolWindow = Lit(MatDir, "Route_SchoolWindow", new Color(0.32f, 0.48f, 0.62f), 0.8f);
            _schoolRoof = Lit(MatDir, "Route_SchoolRoof", new Color(0.36f, 0.4f, 0.44f), 0.3f);
            _sakura = Lit(MatDir, "Route_Sakura", new Color(0.98f, 0.74f, 0.82f), 0.1f);
            _trunk = Lit(MatDir, "Route_Trunk", new Color(0.33f, 0.24f, 0.18f), 0.1f);
            _trackRed = Lit(MatDir, "Route_RunningTrack", new Color(0.72f, 0.36f, 0.26f), 0.1f);
            _white = Lit(MatDir, "Route_White", new Color(0.97f, 0.97f, 0.96f), 0.2f);
            _craneOrange = Lit(MatDir, "Route_CraneOrange", new Color(0.93f, 0.48f, 0.12f), 0.4f);
            _apron = Lit(MatDir, "Route_Apron", new Color(0.6f, 0.6f, 0.58f), 0.1f);
            _water = Lit(MatDir, "Route_HarbourWater", new Color(0.13f, 0.4f, 0.58f), 0.92f);
            _warehouse = Lit(MatDir, "Route_Warehouse", new Color(0.78f, 0.76f, 0.7f), 0.2f);
            _roofBlue = Lit(MatDir, "Route_RoofBlue", new Color(0.24f, 0.4f, 0.62f), 0.3f);
            _asphalt = Lit(MatDir, "Route_Asphalt", new Color(0.24f, 0.25f, 0.27f), 0.15f);
            _planeWhite = Lit(MatDir, "Route_PlaneWhite", new Color(0.96f, 0.97f, 0.98f), 0.6f);
            _planeBlue = Lit(MatDir, "Route_PlaneBlue", new Color(0.12f, 0.32f, 0.68f), 0.5f);
            _terminalGlass = Lit(MatDir, "Route_TerminalGlass", new Color(0.45f, 0.62f, 0.75f), 0.9f);
            _tunnel = Lit(MatDir, "Route_Tunnel", new Color(0.09f, 0.09f, 0.1f), 0.1f);
            _tunnelLamp = Lit(MatDir, "Route_TunnelLamp", new Color(1f, 0.85f, 0.55f), 0.2f, new Color(1f, 0.78f, 0.4f) * 2.2f);
            _platformFloor = Lit(MatDir, "Route_PlatformFloor", new Color(0.66f, 0.66f, 0.64f), 0.2f);
            _tactile = Lit(MatDir, "Route_Tactile", new Color(0.98f, 0.82f, 0.12f), 0.2f);
            _boardWhite = Lit(MatDir, "Route_StationBoard", new Color(0.98f, 0.98f, 0.97f), 0.3f, new Color(0.25f, 0.25f, 0.25f));
            _boatHull = Lit(MatDir, "Route_BoatHull", new Color(0.95f, 0.95f, 0.93f), 0.4f);
            _steelGreen = Lit(MatDir, "Route_BridgeSteel", new Color(0.42f, 0.55f, 0.5f), 0.5f);
            _barnRed = Lit(MatDir, "Route_BarnRed", new Color(0.66f, 0.2f, 0.16f), 0.2f);
        }

        private static void BuildRoute(Transform scenery, TrainWindowScenery component, List<TrainWindowScenery.Theme> themes, System.Random random)
        {
            RouteMaterials();
            float Rand(float min, float max) => min + (float)random.NextDouble() * (max - min);
            themes.Add(SchoolLeg(scenery, Rand));
            themes.Add(HarbourLeg(scenery, Rand, random));
            themes.Add(BridgeLeg(scenery, Rand));
            themes.Add(AirportLeg(scenery, Rand));
            var (platform, sign) = Platform(scenery);
            var tunnel = Tunnel(scenery);
            component.ConfigureRoute(themes, platform, sign, tunnel);
            component.ShowLeg(0);
            platform.gameObject.SetActive(false);
            tunnel.gameObject.SetActive(false);
        }

        private static TrainWindowScenery.Theme Theme(Transform scenery, string id, out Transform root, out Transform statics)
        {
            root = Group(scenery, "Theme_" + id);
            statics = Group(scenery, "Static_" + id);
            return new TrainWindowScenery.Theme { id = id, root = root, statics = statics };
        }

        private static Transform Layer(TrainWindowScenery.Theme theme, string name, float loop)
        {
            var layer = Group(theme.root, name);
            theme.layers.Add(new TrainWindowScenery.Layer { root = layer, loopWidth = loop });
            return layer;
        }

        // ─── Gakuen-mae → Minato: Hibari Japanese School ───
        private static TrainWindowScenery.Theme SchoolLeg(Transform scenery, System.Func<float, float, float> Rand)
        {
            var theme = Theme(scenery, "gakuen", out _, out var statics);
            Block(statics, "Ground_North", new Vector3(0f, -1.12f, 70f), new Vector3(500f, 0.1f, 134f), _grass, false);
            Block(statics, "Ground_South", new Vector3(0f, -1.1f, -14f), new Vector3(500f, 0.1f, 22f), _grass, false);
            var campus = Layer(theme, "Layer_Campus", 170f);
            // The school: a long three-storey building, rows of windows, a clock tower, its name on the wall.
            var school = Group(campus, "School");
            school.localPosition = new Vector3(10f, 0f, 0f);
            Block(school, "Main", new Vector3(0f, 4.2f, 40f), new Vector3(56f, 10.6f, 13f), _schoolWall, false);
            Block(school, "Roof", new Vector3(0f, 9.6f, 40f), new Vector3(56.6f, 0.4f, 13.6f), _schoolRoof, false);
            for (int floor = 0; floor < 3; floor++)
                for (int w = 0; w < 13; w++)
                    Block(school, $"Window_{floor}_{w}", new Vector3(-24f + w * 4f, 0.6f + floor * 3.1f, 33.45f), new Vector3(2.6f, 1.6f, 0.1f), _schoolWindow, false);
            Block(school, "ClockTower", new Vector3(0f, 12.4f, 40f), new Vector3(7f, 6f, 7f), _schoolWall, false);
            Cylinder(school, "Clock", new Vector3(0f, 13.2f, 36.45f), new Vector3(3.6f, 0.05f, 3.6f), _white, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Block(school, "HandHour", new Vector3(0f, 13.7f, 36.38f), new Vector3(0.18f, 1.1f, 0.05f), _black, false);
            Block(school, "HandMinute", new Vector3(0.6f, 13.2f, 36.38f), new Vector3(1.4f, 0.14f, 0.05f), _black, false);
            Text(school, "SchoolName", "ひばり日本語学院", new Vector3(-14f, 7.9f, 33.4f), 0f, 1.3f, new Color(0.15f, 0.25f, 0.4f), 16f);
            // The sports ground: a red running track round a field, the white lines, two goals.
            var ground = Group(campus, "SportsGround");
            ground.localPosition = new Vector3(-55f, 0f, 0f);
            Block(ground, "Track", new Vector3(0f, -1.04f, 24f), new Vector3(44f, 0.05f, 22f), _trackRed, false);
            Block(ground, "Field", new Vector3(0f, -1.02f, 24f), new Vector3(36f, 0.05f, 14f), _paddyA, false);
            for (int i = 0; i < 4; i++) Block(ground, $"Lane_{i}", new Vector3(0f, -1.0f, 13.6f + i * 0.9f), new Vector3(44f, 0.02f, 0.08f), _white, false);
            foreach (float gx in new[] { -16f, 16f })
            {
                Block(ground, $"GoalL_{gx}", new Vector3(gx, 0f, 21.5f), new Vector3(0.12f, 2.1f, 0.12f), _white, false);
                Block(ground, $"GoalR_{gx}", new Vector3(gx, 0f, 26.5f), new Vector3(0.12f, 2.1f, 0.12f), _white, false);
                Block(ground, $"GoalBar_{gx}", new Vector3(gx, 1.0f, 24f), new Vector3(0.12f, 0.12f, 5f), _white, false);
            }
            // Sakura along the school fence.
            for (float x = -80f; x < 85f; x += 9f) Sakura(campus, new Vector3(x + Rand(-1.5f, 1.5f), 0f, 12.5f + Rand(0f, 2f)), Rand(0.9f, 1.25f));
            // A few houses beyond the campus.
            for (float x = 45f; x < 85f; x += 12f) PlaceKenney(campus, "building-type-" + "abcdefghij"[(int)Rand(0f, 9.99f)], new Vector3(x, -1.05f, 30f + Rand(0f, 6f)), 180f, Rand(6.5f, 8f));
            return theme;
        }

        private static void Sakura(Transform parent, Vector3 at, float scale)
        {
            var tree = Group(parent, "Sakura");
            tree.localPosition = at;
            tree.localScale = Vector3.one * scale;
            Cylinder(tree, "Trunk", new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 1.4f, 0.45f), _trunk, false);
            foreach (var (o, s) in new[] { (new Vector3(0f, 2.6f, 0f), 3.4f), (new Vector3(1.1f, 2.2f, 0.4f), 2.4f), (new Vector3(-1.0f, 2.3f, -0.3f), 2.5f) })
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(crown.GetComponent<Collider>());
                crown.name = "Blossom";
                crown.transform.SetParent(tree, false);
                crown.transform.localPosition = o;
                crown.transform.localScale = new Vector3(s, s * 0.8f, s);
                crown.GetComponent<Renderer>().sharedMaterial = _sakura;
            }
        }

        // ─── Minato → Midori: the harbour ───
        private static TrainWindowScenery.Theme HarbourLeg(Transform scenery, System.Func<float, float, float> Rand, System.Random random)
        {
            var theme = Theme(scenery, "minato", out _, out var statics);
            Block(statics, "Apron", new Vector3(0f, -1.1f, 13f), new Vector3(500f, 0.1f, 18f), _apron, false);
            Block(statics, "QuayEdge", new Vector3(0f, -1.05f, 22.1f), new Vector3(500f, 0.2f, 0.4f), _concrete, false);
            Block(statics, "HarbourWater", new Vector3(0f, -1.6f, 160f), new Vector3(700f, 0.1f, 276f), _water, false);
            Block(statics, "Ground_South", new Vector3(0f, -1.1f, -14f), new Vector3(500f, 0.1f, 22f), _grass, false);
            var port = Layer(theme, "Layer_Port", 180f);
            Material[] boxes = { _adBlue, _adRed, _adOrange, _adGreen, _adGrey, _craneOrange };
            // Container stacks on the apron.
            for (float x = -85f; x < 85f; x += 7f)
            {
                if (random.Next(4) == 0) continue;
                int high = 1 + random.Next(2);
                for (int k = 0; k < high; k++)
                    Block(port, $"Container_{x:0}_{k}", new Vector3(x, -0.25f + k * 2.6f, 17.5f + (random.Next(2) == 0 ? 0f : 2.6f)), new Vector3(6.1f, 2.55f, 2.45f), boxes[random.Next(boxes.Length)], false);
            }
            // Gantry cranes on the quay, booms out over the water.
            for (float x = -60f; x < 70f; x += 45f)
            {
                var crane = Group(port, $"Crane_{x:0}");
                crane.localPosition = new Vector3(x, 0f, 0f);
                foreach (float lx in new[] { -4f, 4f })
                    foreach (float lz in new[] { 17f, 23f })
                        Block(crane, $"Leg_{lx}_{lz}", new Vector3(lx, 7f, lz), new Vector3(0.8f, 16f, 0.8f), _craneOrange, false);
                Block(crane, "Beam", new Vector3(0f, 15.2f, 22f), new Vector3(10f, 1.2f, 1.2f), _craneOrange, false);
                Block(crane, "Boom", new Vector3(0f, 15.6f, 38f), new Vector3(1.4f, 1f, 34f), _craneOrange, false);
                Block(crane, "Cab", new Vector3(0f, 13.6f, 30f), new Vector3(2.2f, 1.8f, 2.4f), _white, false);
            }
            // Warehouses with blue roofs; the port's name.
            for (float x = -75f; x < 80f; x += 50f)
            {
                Block(port, $"Warehouse_{x:0}", new Vector3(x + 20f, 2.4f, 9.5f), new Vector3(18f, 7f, 5f), _warehouse, false);
                Block(port, $"WarehouseRoof_{x:0}", new Vector3(x + 20f, 6.05f, 9.5f), new Vector3(18.4f, 0.3f, 5.4f), _roofBlue, false);
            }
            Text(port, "PortName", "ミナト港  <size=60%>Minato Port</size>", new Vector3(-25f, 4.4f, 6.9f), 0f, 1.1f, new Color(0.15f, 0.25f, 0.45f), 14f);
            // Fishing boats in the water beyond the quay.
            var boats = Layer(theme, "Layer_Boats", 220f);
            for (float x = -100f; x < 110f; x += 26f)
            {
                var boat = Group(boats, $"Boat_{x:0}");
                boat.localPosition = new Vector3(x + Rand(-4f, 4f), 0f, Rand(30f, 70f));
                Block(boat, "Hull", new Vector3(0f, -1.1f, 0f), new Vector3(9f, 1.4f, 2.8f), _boatHull, false);
                Block(boat, "Stripe", new Vector3(0f, -0.65f, 0f), new Vector3(9.05f, 0.25f, 2.85f), _adBlue, false);
                Block(boat, "Cabin", new Vector3(-1.5f, 0.4f, 0f), new Vector3(3f, 1.8f, 2.2f), _white, false);
                Block(boat, "Mast", new Vector3(1.8f, 1.6f, 0f), new Vector3(0.15f, 4f, 0.15f), _mast, false);
            }
            var lighthouse = Group(boats, "Lighthouse");
            lighthouse.localPosition = new Vector3(40f, 0f, 95f);
            Cylinder(lighthouse, "Tower", new Vector3(0f, 6f, 0f), new Vector3(3f, 7f, 3f), _white, false);
            Cylinder(lighthouse, "Band", new Vector3(0f, 9f, 0f), new Vector3(3.05f, 1f, 3.05f), _adRed, false);
            Cylinder(lighthouse, "Lamp", new Vector3(0f, 13.6f, 0f), new Vector3(2f, 0.7f, 2f), _tunnelLamp, false);
            return theme;
        }

        // ─── Midori Island → the airport: the bridge over the sea, then the island's farms ───
        private static TrainWindowScenery.Theme BridgeLeg(Transform scenery, System.Func<float, float, float> Rand)
        {
            var theme = Theme(scenery, "midori", out _, out var statics);
            Block(statics, "SeaNorth", new Vector3(0f, -6f, 150f), new Vector3(700f, 0.1f, 296f), _sea, false);
            Block(statics, "SeaSouth", new Vector3(0f, -6f, -60f), new Vector3(700f, 0.1f, 120f), _sea, false);
            // The steel truss passing the window: verticals and diagonals on both sides of the track.
            var truss = Layer(theme, "Layer_Truss", 72f);
            for (float x = -36f; x < 36f; x += 6f)
                foreach (float side in new[] { 3.7f, -3.7f })
                {
                    var bay = Group(truss, $"Bay_{x:0}_{(side > 0 ? "N" : "S")}");
                    bay.localPosition = new Vector3(x, 0f, side);
                    Block(bay, "Post", new Vector3(0f, 1.8f, 0f), new Vector3(0.35f, 6f, 0.35f), _steelGreen, false);
                    var diagonal = Block(bay, "Diagonal", new Vector3(3f, 1.8f, 0f), new Vector3(0.25f, 7.4f, 0.25f), _steelGreen, false);
                    diagonal.transform.localRotation = Quaternion.Euler(0f, 0f, 39f);
                    Block(bay, "Chord", new Vector3(3f, 4.8f, 0f), new Vector3(6f, 0.35f, 0.35f), _steelGreen, false);
                    Block(bay, "Rail", new Vector3(3f, -0.6f, 0f), new Vector3(6f, 0.3f, 0.3f), _steelGreen, false);
                }
            Block(statics, "Deck", new Vector3(0f, -1.3f, 0f), new Vector3(500f, 0.5f, 8f), _concrete, false);
            // Midori Island ahead on the horizon: green hills, a red barn, a windmill; boats on the sea.
            var island = Layer(theme, "Layer_IslandFar", 260f);
            for (float x = -120f; x < 130f; x += 40f)
            {
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(hill.GetComponent<Collider>());
                hill.name = $"IslandHill_{x:0}";
                hill.transform.SetParent(island, false);
                hill.transform.localPosition = new Vector3(x + Rand(0f, 12f), -4f, 120f + Rand(0f, 30f));
                hill.transform.localScale = new Vector3(Rand(50f, 70f), Rand(12f, 22f), 40f);
                hill.GetComponent<Renderer>().sharedMaterial = _paddyA;
                Block(island, $"Barn_{x:0}", new Vector3(x + 6f, 0.5f, 104f), new Vector3(6f, 5f, 5f), _barnRed, false);
                Block(island, $"BarnRoof_{x:0}", new Vector3(x + 6f, 3.4f, 104f), new Vector3(6.4f, 0.6f, 5.4f), _white, false);
            }
            for (float x = -110f; x < 120f; x += 55f)
            {
                Block(island, $"SeaBoat_{x:0}", new Vector3(x, -5.5f, 50f + Rand(0f, 20f)), new Vector3(5f, 1f, 1.6f), _boatHull, false);
                Block(island, $"SeaBoatCabin_{x:0}", new Vector3(x - 0.8f, -4.6f, 50f), new Vector3(1.6f, 1f, 1.2f), _adRed, false);
            }
            return theme;
        }

        // ─── The airport ───
        private static TrainWindowScenery.Theme AirportLeg(Transform scenery, System.Func<float, float, float> Rand)
        {
            var theme = Theme(scenery, "airport", out _, out var statics);
            Block(statics, "Ground_North", new Vector3(0f, -1.12f, 70f), new Vector3(500f, 0.1f, 134f), _grass, false);
            Block(statics, "Ground_South", new Vector3(0f, -1.1f, -14f), new Vector3(500f, 0.1f, 22f), _grass, false);
            Block(statics, "Runway", new Vector3(0f, -1.06f, 58f), new Vector3(500f, 0.1f, 40f), _asphalt, false);
            Block(statics, "RunwayEdgeN", new Vector3(0f, -1.0f, 77.5f), new Vector3(500f, 0.02f, 0.5f), _white, false);
            Block(statics, "RunwayEdgeS", new Vector3(0f, -1.0f, 38.5f), new Vector3(500f, 0.02f, 0.5f), _white, false);
            var marks = Layer(theme, "Layer_RunwayMarks", 60f);
            for (float x = -30f; x < 30f; x += 20f) Block(marks, $"Centreline_{x:0}", new Vector3(x, -1.0f, 58f), new Vector3(10f, 0.02f, 0.7f), _white, false);
            var apron = Layer(theme, "Layer_Airport", 240f);
            // The terminal (glass), the tower, the airport's name.
            Block(apron, "Terminal", new Vector3(-30f, 3f, 105f), new Vector3(80f, 9f, 18f), _terminalGlass, false);
            Block(apron, "TerminalRoof", new Vector3(-30f, 7.8f, 105f), new Vector3(82f, 0.8f, 20f), _white, false);
            Text(apron, "AirportName", "ひばり空港  <size=60%>Hibari Airport · bay đi Phú Quốc</size>", new Vector3(-30f, 6.2f, 95.9f), 0f, 2.2f, new Color(0.1f, 0.2f, 0.4f), 40f);
            var tower = Group(apron, "ControlTower");
            tower.localPosition = new Vector3(35f, 0f, 112f);
            Cylinder(tower, "Shaft", new Vector3(0f, 10f, 0f), new Vector3(3f, 11f, 3f), _white, false);
            Cylinder(tower, "Cab", new Vector3(0f, 22f, 0f), new Vector3(7f, 1.6f, 7f), _terminalGlass, false);
            Cylinder(tower, "CabRoof", new Vector3(0f, 23.8f, 0f), new Vector3(7.6f, 0.3f, 7.6f), _white, false);
            // Parked aeroplanes by the terminal, one rolling on the runway.
            foreach (var (x, z, yaw) in new[] { (-55f, 88f, 180f), (-15f, 88f, 180f), (60f, 58f, 90f) })
                Aeroplane(apron, new Vector3(x, 0f, z), yaw);
            return theme;
        }

        private static void Aeroplane(Transform parent, Vector3 at, float yaw)
        {
            var plane = Group(parent, "Aeroplane");
            plane.localPosition = at;
            plane.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var body = Cylinder(plane, "Fuselage", new Vector3(0f, 2.2f, 0f), new Vector3(3.6f, 16f, 3.6f), _planeWhite, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Block(plane, "Stripe", new Vector3(0f, 2.0f, 0f), new Vector3(3.7f, 0.5f, 26f), _planeBlue, false);
            Block(plane, "Wings", new Vector3(0f, 1.6f, 1f), new Vector3(30f, 0.4f, 5f), _planeWhite, false);
            Block(plane, "Tailplane", new Vector3(0f, 2.6f, -13.5f), new Vector3(11f, 0.3f, 3f), _planeWhite, false);
            Block(plane, "Fin", new Vector3(0f, 5.4f, -13.8f), new Vector3(0.4f, 6f, 3.6f), _planeBlue, false);
            Cylinder(plane, "EngineL", new Vector3(-6f, 0.9f, 2f), new Vector3(1.6f, 1.8f, 1.6f), _planeWhite, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Cylinder(plane, "EngineR", new Vector3(6f, 0.9f, 2f), new Vector3(1.6f, 1.8f, 1.6f), _planeWhite, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // ─── The platform at a stop: floor, yellow tactile line, canopy, the station name board ───
        private static (Transform platform, TextMeshPro sign) Platform(Transform scenery)
        {
            var platform = Group(scenery, "StopPlatform");
            Block(platform, "Floor", new Vector3(0f, -0.35f, 6.6f), new Vector3(90f, 0.7f, 6.6f), _platformFloor, false);
            Block(platform, "Tactile", new Vector3(0f, 0.01f, 3.9f), new Vector3(90f, 0.02f, 0.4f), _tactile, false);
            Block(platform, "Edge", new Vector3(0f, -0.02f, 3.4f), new Vector3(90f, 0.06f, 0.3f), _white, false);
            for (float x = -40f; x <= 40f; x += 10f)
                Block(platform, $"Pillar_{x:0}", new Vector3(x, 1.6f, 8f), new Vector3(0.3f, 3.2f, 0.3f), _mast, false);
            Block(platform, "Canopy", new Vector3(0f, 3.3f, 7.2f), new Vector3(90f, 0.25f, 5f), _roofBlue, false);
            for (float x = -30f; x <= 30f; x += 20f)
                Block(platform, $"Bench_{x:0}", new Vector3(x + 5f, 0.25f, 8.8f), new Vector3(2.4f, 0.5f, 0.6f), _seat, false);
            // The name board, as on Japanese lines: the station big, its reading below.
            var board = Group(platform, "NameBoard");
            board.localPosition = new Vector3(5f, 2.2f, 8.3f); // between two pillars, beside the window
            Block(board, "Frame", Vector3.zero, new Vector3(5.6f, 1.6f, 0.12f), _boardWhite, false);
            Block(board, "Stripe", new Vector3(0f, -0.6f, -0.07f), new Vector3(5.6f, 0.22f, 0.02f), _adGreen, false);
            Block(board, "PostL", new Vector3(-2.3f, -1.9f, 0f), new Vector3(0.12f, 2.2f, 0.12f), _mast, false);
            Block(board, "PostR", new Vector3(2.3f, -1.9f, 0f), new Vector3(0.12f, 2.2f, 0.12f), _mast, false);
            var sign = Text(board, "Name", "ひばり\n<size=45%>Hibari</size>", new Vector3(0f, 0.05f, -0.08f), 0f, 0.62f, new Color(0.08f, 0.1f, 0.14f), 5.4f);
            sign.textWrappingMode = TextWrappingModes.Normal;
            return (platform, sign);
        }

        // ─── The tunnel between legs (it hides the change of view) ───
        private static Transform Tunnel(Transform scenery)
        {
            var tunnel = Group(scenery, "Tunnel");
            Block(tunnel, "WallN", new Vector3(0f, 2f, 4.3f), new Vector3(400f, 7f, 0.4f), _tunnel, false);
            Block(tunnel, "WallS", new Vector3(0f, 2f, -4.3f), new Vector3(400f, 7f, 0.4f), _tunnel, false);
            Block(tunnel, "Ceiling", new Vector3(0f, 5.4f, 0f), new Vector3(400f, 0.4f, 9f), _tunnel, false);
            for (float x = -195f; x < 200f; x += 12f)
            {
                Block(tunnel, $"LampN_{x:0}", new Vector3(x, 3.2f, 4.05f), new Vector3(1.6f, 0.18f, 0.1f), _tunnelLamp, false);
                Block(tunnel, $"LampS_{x:0}", new Vector3(x, 3.2f, -4.05f), new Vector3(1.6f, 0.18f, 0.1f), _tunnelLamp, false);
            }
            return tunnel;
        }
    }
}
#endif
