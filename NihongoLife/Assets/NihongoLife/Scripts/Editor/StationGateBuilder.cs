#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static NihongoLife.EditorTools.ZoneBuildKit;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Hibari Station's ticket gates (改札) between the hall and platform 2: three cabinets make two lanes with
    /// flaps (TicketGate opens them for a ticket holder, always from the platform side), an IC reader on each
    /// cabinet ("GateScanner" keeps the F prompt), a 改札 sign on a frame, and a railing across the concourse so
    /// the gates are the only way through. Replaces the two solid blocks that blocked the lane between them while
    /// leaving both sides open. Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.StationGateBuilder.Build
    /// </summary>
    public static class StationGateBuilder
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/20_StationDistrict.unity";
        private const string MatDir = "Assets/NihongoLife/Materials/Station";
        private static readonly Vector3 GateCentre = new Vector3(800f, 0.3f, 5.5f); // floor level of the concourse
        private const float CabinetWidth = 0.32f, CabinetLength = 1.9f, CabinetHeight = 1.0f, Pitch = 1.65f, RailingHalfSpan = 43f;
        private static readonly string[] Superseded = { "GateLeft", "GateRightValidator", "GateScanner", "TicketGates" };

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var zone = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "StationDistrict_Zone");
                if (zone == null) throw new InvalidOperationException("StationDistrict_Zone not found");
                foreach (var old in zone.GetComponentsInChildren<Transform>(true).Where(t => Superseded.Contains(t.name)).Select(t => t.gameObject).ToList())
                    if (old != null) Object.DestroyImmediate(old);

                var body = Lit(MatDir, "Gate_Body", new Color(0.2f, 0.25f, 0.32f), 0.45f);
                var cover = Lit(MatDir, "Gate_Cover", new Color(0.86f, 0.87f, 0.88f), 0.6f);
                var reader = Lit(MatDir, "Gate_ICReader", new Color(0.1f, 0.75f, 0.85f), 0.7f, new Color(0.1f, 0.75f, 0.9f) * 1.3f);
                var go = Lit(MatDir, "Gate_GoLamp", new Color(0.15f, 0.8f, 0.35f), 0.5f, new Color(0.1f, 0.9f, 0.35f) * 1.4f);
                var flap = Lit(MatDir, "Gate_Flap", new Color(0.92f, 0.42f, 0.16f), 0.4f);
                var steel = Lit(MatDir, "Gate_Railing", new Color(0.72f, 0.74f, 0.76f), 0.8f);
                var navy = Lit(MatDir, "Gate_Sign", new Color(0.08f, 0.16f, 0.36f), 0.3f);

                var root = new GameObject("TicketGates").transform;
                root.SetParent(zone.transform, false);
                root.position = GateCentre;
                root.rotation = Quaternion.identity;

                // Cabinets (left, middle, right) with a cover, an IC reader and a green "go" lamp at each end.
                for (int i = -1; i <= 1; i++)
                {
                    var cab = Group(root, $"GateCabinet_{i + 1}");
                    cab.localPosition = new Vector3(i * Pitch, 0f, 0f);
                    Block(cab, "Body", new Vector3(0f, CabinetHeight * 0.5f, 0f), new Vector3(CabinetWidth, CabinetHeight, CabinetLength), body, true);
                    Block(cab, "Cover", new Vector3(0f, CabinetHeight + 0.02f, 0f), new Vector3(CabinetWidth + 0.04f, 0.04f, CabinetLength + 0.04f), cover, false);
                    foreach (float end in new[] { 1f, -1f })
                        Block(cab, end > 0 ? "GoLamp_Hall" : "GoLamp_Platform", new Vector3(0f, CabinetHeight - 0.18f, end * (CabinetLength * 0.5f + 0.005f)), new Vector3(0.16f, 0.1f, 0.01f), go, false);
                    if (i == 0)
                    {
                        // The F prompt lives here (StationTravelController looks the scanner up by name).
                        var scanner = new GameObject("GateScanner").transform;
                        scanner.SetParent(cab, false);
                        scanner.localPosition = new Vector3(0f, CabinetHeight + 0.05f, 0.55f);
                        Block(scanner, "Reader", Vector3.zero, new Vector3(0.24f, 0.03f, 0.26f), reader, false);
                    }
                    else Block(cab, "ICReader", new Vector3(0f, CabinetHeight + 0.05f, 0.55f), new Vector3(0.24f, 0.03f, 0.26f), reader, false);
                }

                // Lanes: two flaps that meet in the middle, and a barrier collider while they are shut.
                var lanes = new List<TicketGate.Lane>();
                float laneHalf = (Pitch - CabinetWidth) * 0.5f;
                foreach (float x in new[] { -Pitch * 0.5f, Pitch * 0.5f })
                {
                    var lane = Group(root, x < 0 ? "Lane_W" : "Lane_E");
                    lane.localPosition = new Vector3(x, 0f, 0f);
                    var left = Block(lane, "Flap_L", new Vector3(-laneHalf * 0.5f, 0.62f, 0f), new Vector3(laneHalf, 0.42f, 0.04f), flap, false).transform;
                    var right = Block(lane, "Flap_R", new Vector3(laneHalf * 0.5f, 0.62f, 0f), new Vector3(laneHalf, 0.42f, 0.04f), flap, false).transform;
                    var barrier = new GameObject("Barrier").AddComponent<BoxCollider>();
                    barrier.transform.SetParent(lane, false);
                    barrier.center = new Vector3(0f, 0.6f, 0f);
                    barrier.size = new Vector3(laneHalf * 2f, 1.2f, 0.25f);
                    lanes.Add(new TicketGate.Lane { x = x, leftFlap = left, rightFlap = right, barrier = barrier });
                }
                root.gameObject.AddComponent<TicketGate>().Configure(lanes, laneHalf);

                // A frame over the gates with the 改札 sign, readable from the hall.
                float outer = Pitch + CabinetWidth * 0.5f + 0.12f;
                foreach (float side in new[] { -1f, 1f })
                    Block(root, side < 0 ? "SignPost_W" : "SignPost_E", new Vector3(side * outer, 1.45f, 0f), new Vector3(0.09f, 2.9f, 0.09f), steel, false);
                Block(root, "SignBoard", new Vector3(0f, 2.62f, 0f), new Vector3(outer * 2f + 0.2f, 0.5f, 0.06f), navy, false);
                Text(root, "SignText_Hall", "改札  <size=60%>かいさつ · Cổng soát vé</size>", new Vector3(0f, 2.62f, 0.04f), 180f, 0.34f, Color.white, outer * 2f);
                Text(root, "SignText_Platform", "出口  <size=60%>でぐち · Lối ra</size>", new Vector3(0f, 2.62f, -0.04f), 0f, 0.34f, Color.white, outer * 2f);

                // Railing across the concourse either side of the gates: posts, two rails, one solid collider.
                foreach (float side in new[] { -1f, 1f })
                {
                    var rail = Group(root, side < 0 ? "Railing_W" : "Railing_E");
                    float from = outer + 0.05f, to = RailingHalfSpan, mid = side * (from + to) * 0.5f, len = to - from;
                    Block(rail, "TopRail", new Vector3(mid, 1.0f, 0f), new Vector3(len, 0.05f, 0.06f), steel, false);
                    Block(rail, "MidRail", new Vector3(mid, 0.55f, 0f), new Vector3(len, 0.04f, 0.04f), steel, false);
                    for (float d = from; d <= to; d += 2f)
                        Cylinder(rail, "Post", new Vector3(side * d, 0.5f, 0f), new Vector3(0.06f, 0.5f, 0.06f), steel, false);
                    var solid = new GameObject("Collider").AddComponent<BoxCollider>();
                    solid.transform.SetParent(rail, false);
                    solid.center = new Vector3(mid, 0.6f, 0f);
                    solid.size = new Vector3(len, 1.2f, 0.2f);
                }

                // The parked trains on the tracks had box colliders lying across the platform (one right behind the
                // gates, one by the boarding door) while their meshes sit far down the line: they are scenery, not walls.
                int strayColliders = 0;
                foreach (var train in zone.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("HighSpeed_")))
                    foreach (var collider in train.GetComponents<Collider>()) { Object.DestroyImmediate(collider); strayColliders++; }
                Debug.Log($"[StationGateBuilder] Removed {strayColliders} train colliders that crossed the platform.");

                // The bench that stood right in front of the east lane's exit moves along the platform.
                var bench = zone.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "PlatformBench_B");
                if (bench != null && Mathf.Abs(bench.position.x - GateCentre.x) < 3f) bench.position = new Vector3(GateCentre.x + 4.2f, bench.position.y, bench.position.z);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[StationGateBuilder] Ticket gates built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
#endif
