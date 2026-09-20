using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace NihongoLife.World
{
    public sealed class StationMetroEnvironment : MonoBehaviour
    {
        private const float CenterX = 800f;
        private const float Length = 92f;
        private readonly List<(Transform transform, Vector3 origin)> _oppositeTrain = new();
        private readonly Dictionary<Color, Material> _materials = new();
        private Transform _generated;
        private GameObject _boardingBarrier;
        private TextMeshPro _departureBoard;
        private StationTravelController _travelController;

        private void Awake()
        {
            DisableLegacyObstruction("TicketGateArch");
            DisableLegacyObstruction("CanopyPost_0");
            DisableLegacyObstruction("TicketMachine_A");
            DisableLegacyObstruction("TicketGate_A");
            DisableLegacyObstruction("TicketGate_B");
            Transform existing = transform.Find("MetroEnvironment_Runtime");
            if (existing != null) Destroy(existing.gameObject);
            _generated = new GameObject("MetroEnvironment_Runtime").transform;
            _generated.SetParent(transform, false);
            _travelController = GetComponentInChildren<StationTravelController>(true);

            BuildShell();
            BuildPlatforms();
            BuildTrackBeds();
            BuildArchitecture();
            NormalizePlatformTrain();
            BuildTicketConcourse();
            BuildSigns();
            BuildOppositeTrain();

            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.07f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.56f, 0.6f);
            CreateTunnelFillLight();
        }

        private void Update()
        {
            float oppositePhase = Mathf.Repeat(Time.time + 30f, 60f);
            float offset = oppositePhase < 15f ? 0f
                : oppositePhase < 27f ? Mathf.SmoothStep(0f, -105f, (oppositePhase - 15f) / 12f)
                : oppositePhase < 48f ? -105f
                : Mathf.SmoothStep(105f, 0f, (oppositePhase - 48f) / 12f);

            foreach (var item in _oppositeTrain)
                if (item.transform != null) item.transform.position = item.origin + Vector3.right * offset;

            float servicePhase = Mathf.Repeat(Time.time, 60f);
            bool boarding = servicePhase < 15f;
            if (_boardingBarrier != null) _boardingBarrier.SetActive(!boarding);
            if (_departureBoard != null)
            {
                float seconds = boarding ? 15f - servicePhase : 60f - servicePhase;
                string status = boarding ? "NOW BOARDING" : "NEXT DEPARTURE";
                _departureBoard.text = $"DEPARTURES / PLATFORM 1\n<size=65%>MIDORI   {status}   {seconds:00}s\nSHINJUKU   01:15   Y260</size>";
            }
        }

        private void BuildShell()
        {
            CreateBlock("TunnelFloor", new Vector3(CenterX, -0.45f, -4.5f), new Vector3(Length, 0.5f, 27f), new Color(0.055f, 0.07f, 0.085f), true);
            CreateBlock("TunnelCeiling", new Vector3(CenterX, 6.2f, -3.25f), new Vector3(Length, 0.35f, 29.5f), new Color(0.09f, 0.12f, 0.145f), true);
            CreateBlock("TunnelNorthWall", new Vector3(CenterX, 2.9f, 11.5f), new Vector3(Length, 6.6f, 0.45f), new Color(0.12f, 0.15f, 0.17f), true);
            CreateBlock("TunnelSouthWall", new Vector3(CenterX, 2.9f, -18f), new Vector3(Length, 6.6f, 0.45f), new Color(0.12f, 0.15f, 0.17f), true);
            CreateBlock("TunnelWestPortal", new Vector3(CenterX - Length * 0.5f, 2.9f, -4.5f), new Vector3(0.6f, 6.6f, 27f), new Color(0.025f, 0.035f, 0.045f), true);
            CreateBlock("TunnelEastPortal", new Vector3(CenterX + Length * 0.5f, 2.9f, -4.5f), new Vector3(0.6f, 6.6f, 27f), new Color(0.025f, 0.035f, 0.045f), true);
            GameObject catchFloor = CreateBlock("SafetyCatchFloor", new Vector3(CenterX, -0.78f, -4.5f), new Vector3(Length - 1f, 0.6f, 26f), Color.black, true);
            catchFloor.GetComponent<Renderer>().enabled = false;
        }

        private void BuildPlatforms()
        {
            CreateBlock("PlatformNorthExtension", new Vector3(CenterX, 0.08f, 4.5f), new Vector3(Length - 6f, 0.45f, 7.7f), new Color(0.34f, 0.37f, 0.39f), true);
            CreateBlock("PlatformSouth", new Vector3(CenterX, 0.08f, -13.5f), new Vector3(Length - 6f, 0.45f, 7.7f), new Color(0.34f, 0.37f, 0.39f), true);
            CreateBlock("SafetyLineNorth", new Vector3(CenterX, 0.33f, 0.78f), new Vector3(Length - 8f, 0.04f, 0.32f), new Color(0.98f, 0.73f, 0.08f), false);
            CreateBlock("SafetyLineSouth", new Vector3(CenterX, 0.33f, -9.78f), new Vector3(Length - 8f, 0.04f, 0.32f), new Color(0.98f, 0.73f, 0.08f), false);

            CreateBlock("BarrierRailNorthLeft", new Vector3(CenterX - 23f, 1.02f, 0.53f), new Vector3(40f, 0.12f, 0.16f), new Color(0.18f, 0.48f, 0.58f), true).layer = 2;
            CreateBlock("BarrierRailNorthRight", new Vector3(CenterX + 23f, 1.02f, 0.53f), new Vector3(40f, 0.12f, 0.16f), new Color(0.18f, 0.48f, 0.58f), true).layer = 2;
            CreateBlock("BarrierBaseNorthLeft", new Vector3(CenterX - 23f, 0.54f, 0.53f), new Vector3(40f, 0.28f, 0.22f), new Color(0.08f, 0.2f, 0.25f), true).layer = 2;
            CreateBlock("BarrierBaseNorthRight", new Vector3(CenterX + 23f, 0.54f, 0.53f), new Vector3(40f, 0.28f, 0.22f), new Color(0.08f, 0.2f, 0.25f), true).layer = 2;
            _boardingBarrier = CreateBlock("BoardingBarrierDoor", new Vector3(CenterX, 0.9f, 0.53f), new Vector3(5.5f, 1.15f, 0.18f), new Color(0.12f, 0.42f, 0.52f), true);
            _boardingBarrier.layer = 2;
            CreateBlock("BoardingWaitZone", new Vector3(CenterX, 0.34f, 1.55f), new Vector3(5.5f, 0.03f, 1.15f), new Color(0.1f, 0.48f, 0.58f), false);

            for (int i = -7; i <= 7; i++)
            {
                float x = CenterX + i * 5.5f;
                if (i != 0)
                {
                    GameObject northPost = CreateBlock($"ScreenPostNorth_{i}", new Vector3(x, 1.15f, 0.55f), new Vector3(0.12f, 1.7f, 0.12f), new Color(0.18f, 0.48f, 0.58f), true);
                    northPost.layer = 2;
                }
                GameObject southPost = CreateBlock($"ScreenPostSouth_{i}", new Vector3(x, 1.15f, -9.55f), new Vector3(0.12f, 1.7f, 0.12f), new Color(0.18f, 0.48f, 0.58f), true);
                southPost.layer = 2;
            }
            for (int i = -5; i <= 5; i++)
            {
                float x = CenterX + i * 8f;
                CreateLightPanel($"CeilingPanelNorth_{i}", new Vector3(x, 5.95f, 4.2f));
                CreateLightPanel($"CeilingPanelSouth_{i}", new Vector3(x, 5.95f, -13.2f));
            }
        }

        private void BuildTrackBeds()
        {
            CreateBlock("TrackBed_1", new Vector3(CenterX, -0.18f, -3.15f), new Vector3(Length - 2f, 0.25f, 4.6f), new Color(0.045f, 0.055f, 0.065f), false);
            CreateBlock("TrackBed_2", new Vector3(CenterX, -0.18f, -7.15f), new Vector3(Length - 2f, 0.25f, 4.6f), new Color(0.045f, 0.055f, 0.065f), false);
            for (int i = -22; i <= 22; i++)
            {
                float x = CenterX + i * 2f;
                CreateBlock($"Sleeper1_{i}", new Vector3(x, 0.01f, -3.15f), new Vector3(0.18f, 0.12f, 3f), new Color(0.22f, 0.23f, 0.23f), false);
                CreateBlock($"Sleeper2_{i}", new Vector3(x, 0.01f, -7.15f), new Vector3(0.18f, 0.12f, 3f), new Color(0.22f, 0.23f, 0.23f), false);
            }
            foreach (float z in new[] { -2.35f, -3.95f, -6.35f, -7.95f })
                CreateBlock("Rail", new Vector3(CenterX, 0.17f, z), new Vector3(Length - 2f, 0.12f, 0.1f), new Color(0.55f, 0.58f, 0.6f), false);
        }

        private void BuildArchitecture()
        {
            for (int i = -7; i <= 7; i++)
            {
                float x = CenterX + i * 5.5f;
                CreateBlock($"NorthColumn_{i}", new Vector3(x, 3.1f, 6.8f), new Vector3(0.42f, 5.8f, 0.42f), new Color(0.15f, 0.35f, 0.42f), true);
                CreateBlock($"SouthColumn_{i}", new Vector3(x, 3.1f, -15.8f), new Vector3(0.42f, 5.8f, 0.42f), new Color(0.15f, 0.35f, 0.42f), true);
                if (i % 3 == 0)
                {
                    CreateLight($"NorthLight_{i}", new Vector3(x, 5.75f, 4.2f));
                    CreateLight($"SouthLight_{i}", new Vector3(x, 5.75f, -13.2f));
                }
            }
            CreateBlock("ConcourseHeader", new Vector3(CenterX, 4.8f, 8.2f), new Vector3(18f, 2.2f, 1.3f), new Color(0.08f, 0.17f, 0.22f), true);
        }

        private void BuildTicketConcourse()
        {
            CreateBlock("TicketCounter", new Vector3(CenterX - 10f, 0.62f, 8.4f), new Vector3(5.8f, 0.82f, 1.1f), new Color(0.12f, 0.25f, 0.29f), true);
            CreateBlock("TicketCounterTop", new Vector3(CenterX - 10f, 1.08f, 8.4f), new Vector3(6.1f, 0.12f, 1.35f), new Color(0.78f, 0.78f, 0.72f), true);
            CreateSignBoard("TicketCounterSign", "TICKETS / INFORMATION", new Vector3(CenterX - 10f, 3.1f, 9.05f), Quaternion.Euler(0f, 180f, 0f), new Vector2(7f, 1.2f));
            CreateBlock("TicketPOSBase", new Vector3(CenterX - 9.25f, 1.25f, 8.35f), new Vector3(0.75f, 0.28f, 0.55f), new Color(0.05f, 0.08f, 0.1f), false);
            GameObject posScreen = CreateBlock("TicketPOSScreen", new Vector3(CenterX - 9.25f, 1.62f, 8.48f), new Vector3(0.78f, 0.62f, 0.08f), new Color(0.2f, 0.65f, 0.72f), false);
            posScreen.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            CreateBlock("TicketPrinter", new Vector3(CenterX - 10.4f, 1.25f, 8.35f), new Vector3(0.62f, 0.3f, 0.5f), new Color(0.82f, 0.84f, 0.82f), false);
            CreateBlock("ServiceBell", new Vector3(CenterX - 11.25f, 1.22f, 8.15f), new Vector3(0.22f, 0.18f, 0.22f), new Color(0.95f, 0.72f, 0.25f), false);
            CreateWorldLabel("ClerkNamePlate", "STATION STAFF", new Vector3(CenterX - 10f, 1.2f, 7.69f), Quaternion.identity, 0.22f);

            GameObject machine = CreateBlock("ProductionTicketMachine", new Vector3(CenterX - 3.6f, 1.15f, 8.45f), new Vector3(1.35f, 2.05f, 0.85f), new Color(0.08f, 0.35f, 0.48f), true);
            CreateBlock("TicketMachineScreen", new Vector3(CenterX - 3.6f, 1.48f, 7.995f), new Vector3(0.95f, 0.72f, 0.03f), new Color(0.32f, 0.8f, 0.88f), false);
            if (_travelController != null)
                machine.AddComponent<StationTravelInteractable>().Configure(_travelController, StationAction.BuyTicket, "Kippu o kau", "Buy ticket");

            CreateBlock("GateLeft", new Vector3(CenterX - 1.25f, 0.82f, 5.5f), new Vector3(1.1f, 1.25f, 2.2f), new Color(0.08f, 0.3f, 0.38f), true);
            GameObject gate = CreateBlock("GateRightValidator", new Vector3(CenterX + 1.25f, 0.82f, 5.5f), new Vector3(1.1f, 1.25f, 2.2f), new Color(0.08f, 0.3f, 0.38f), true);
            CreateBlock("GateScanner", new Vector3(CenterX + 1.25f, 1.5f, 5.5f), new Vector3(0.68f, 0.06f, 0.65f), new Color(0.2f, 0.9f, 0.82f), false);
            if (_travelController != null)
                gate.AddComponent<StationTravelInteractable>().Configure(_travelController, StationAction.PassGate, "Kaisatsu o toru", "Validate ticket");

            GameObject cashierPrefab = Resources.Load<GameObject>("Characters/NL_Cashier");
            if (cashierPrefab != null)
            {
                GameObject cashier = Instantiate(cashierPrefab, _generated);
                cashier.name = "StationTicketClerk";
                cashier.transform.SetPositionAndRotation(new Vector3(CenterX - 10f, 0.31f, 9.35f), Quaternion.Euler(0f, 180f, 0f));
                foreach (Collider collider in cashier.GetComponentsInChildren<Collider>()) collider.enabled = false;
            }
        }

        private void BuildSigns()
        {
            CreateWorldLabel("StationNameNorth", "SAKURA METRO", new Vector3(CenterX, 4.45f, 11.24f), Quaternion.Euler(0f, 180f, 0f), 0.65f);
            CreateWorldLabel("Platform1", "1  >  MIDORI / SHINJUKU", new Vector3(CenterX - 13f, 4.4f, 5.9f), Quaternion.Euler(0f, 180f, 0f), 0.55f);
            CreateWorldLabel("Platform2", "2  >  ASAKUSA / SAKURA", new Vector3(CenterX + 13f, 4.4f, -14.9f), Quaternion.identity, 0.55f);
            CreateSignBoard("DirectionBoard", "TICKETS  <     |     PLATFORM 1  v     |     EXIT  ^", new Vector3(CenterX, 4.35f, 7.48f), Quaternion.Euler(0f, 180f, 0f), new Vector2(16f, 1.25f));
            _departureBoard = CreateSignBoard("DepartureBoard", string.Empty, new Vector3(CenterX + 12f, 3.2f, 10.95f), Quaternion.Euler(0f, 180f, 0f), new Vector2(10f, 2.5f));
        }

        private void NormalizePlatformTrain()
        {
            string[] names = { "HighSpeed_Front", "HighSpeed_Wagon_A", "HighSpeed_Wagon_B" };
            var train = new List<GameObject>();
            foreach (string itemName in names)
            {
                GameObject item = GameObject.Find(itemName);
                if (item == null) continue;
                Bounds bounds = CalculateBounds(item);
                if (bounds.size.y > 0.01f)
                {
                    float scale = Mathf.Clamp(3.05f / bounds.size.y, 0.25f, 12f);
                    item.transform.localScale *= scale;
                }
                item.transform.rotation = Quaternion.identity;
                train.Add(item);
            }

            float total = 0f;
            foreach (GameObject item in train) total += CalculateBounds(item).size.x + 0.2f;
            float cursor = CenterX - total * 0.5f;
            foreach (GameObject item in train)
            {
                Bounds bounds = CalculateBounds(item);
                Vector3 delta = new Vector3(cursor - bounds.min.x, 0.35f - bounds.min.y, -3.15f - bounds.center.z);
                item.transform.position += delta;
                cursor += bounds.size.x + 0.2f;
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void BuildOppositeTrain()
        {
            string[] sourceNames = { "HighSpeed_Front", "HighSpeed_Wagon_A", "HighSpeed_Wagon_B" };
            for (int i = 0; i < sourceNames.Length; i++)
            {
                GameObject source = GameObject.Find(sourceNames[i]);
                if (source == null) continue;
                GameObject clone = Instantiate(source, _generated);
                clone.name = "Opposite_" + sourceNames[i];
                clone.transform.position = new Vector3(2f * CenterX - source.transform.position.x, source.transform.position.y, -7.15f);
                clone.transform.rotation = Quaternion.Euler(0f, 180f, 0f) * source.transform.rotation;
                foreach (Collider collider in clone.GetComponentsInChildren<Collider>()) collider.enabled = false;
                _oppositeTrain.Add((clone.transform, clone.transform.position));
            }
        }

        private GameObject CreateBlock(string objectName, Vector3 position, Vector3 scale, Color color, bool collider)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName;
            block.transform.SetParent(_generated);
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = scale;
            if (!_materials.TryGetValue(color, out Material material))
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                material.color = color;
                _materials[color] = material;
            }
            block.GetComponent<Renderer>().sharedMaterial = material;
            block.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Collider blockCollider = block.GetComponent<Collider>();
            if (blockCollider != null) blockCollider.enabled = collider;
            return block;
        }

        private void CreateLight(string objectName, Vector3 position)
        {
            GameObject lightObject = new GameObject(objectName);
            lightObject.transform.SetParent(_generated);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 2.5f;
            light.color = new Color(0.78f, 0.9f, 1f);
            light.shadows = LightShadows.None;
        }

        private void CreateLightPanel(string objectName, Vector3 position)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = objectName;
            panel.transform.SetParent(_generated);
            panel.transform.position = position;
            panel.transform.localScale = new Vector3(5.2f, 0.08f, 0.65f);
            Collider collider = panel.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = new Color(0.78f, 0.92f, 1f);
            panel.GetComponent<Renderer>().sharedMaterial = material;
            panel.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private void CreateTunnelFillLight()
        {
            GameObject lightObject = new GameObject("MetroFillLight");
            lightObject.transform.SetParent(_generated);
            lightObject.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.85f;
            light.color = new Color(0.72f, 0.84f, 0.92f);
            light.shadows = LightShadows.None;
        }

        private TextMeshPro CreateSignBoard(string objectName, string value, Vector3 position, Quaternion rotation, Vector2 size)
        {
            CreateBlock(objectName + "_Backing", position, new Vector3(size.x, size.y, 0.12f), new Color(0.025f, 0.08f, 0.105f), false).transform.rotation = rotation;
            Vector3 labelPosition = position + rotation * new Vector3(0f, 0f, -0.075f);
            GameObject labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(_generated);
            labelObject.transform.SetPositionAndRotation(labelPosition, rotation);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.text = value;
            label.fontSize = 0.45f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.86f, 0.96f, 1f);
            label.rectTransform.sizeDelta = size - new Vector2(0.4f, 0.2f);
            return label;
        }

        private static void DisableLegacyObstruction(string objectName)
        {
            GameObject obstruction = GameObject.Find(objectName);
            if (obstruction != null) obstruction.SetActive(false);
        }

        private void CreateWorldLabel(string objectName, string value, Vector3 position, Quaternion rotation, float size)
        {
            GameObject labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(_generated);
            labelObject.transform.SetPositionAndRotation(position, rotation);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.text = value;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.85f, 0.95f, 1f);
            label.rectTransform.sizeDelta = new Vector2(18f, 2f);
        }
    }
}
