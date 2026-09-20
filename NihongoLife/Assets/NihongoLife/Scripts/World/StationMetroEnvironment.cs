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

        private void Awake()
        {
            DisableLegacyObstruction("TicketGateArch");
            DisableLegacyObstruction("CanopyPost_0");
            Transform existing = transform.Find("MetroEnvironment_Runtime");
            if (existing != null) Destroy(existing.gameObject);
            _generated = new GameObject("MetroEnvironment_Runtime").transform;
            _generated.SetParent(transform, false);

            BuildShell();
            BuildPlatforms();
            BuildTrackBeds();
            BuildArchitecture();
            BuildSigns();
            BuildOppositeTrain();

            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.07f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.56f, 0.6f);
        }

        private void Update()
        {
            float phase = Mathf.Repeat(Time.time + 30f, 60f);
            float offset = phase < 15f ? 0f
                : phase < 27f ? Mathf.SmoothStep(0f, -105f, (phase - 15f) / 12f)
                : phase < 48f ? -105f
                : Mathf.SmoothStep(105f, 0f, (phase - 48f) / 12f);

            foreach (var item in _oppositeTrain)
                if (item.transform != null) item.transform.position = item.origin + Vector3.right * offset;
        }

        private void BuildShell()
        {
            CreateBlock("TunnelFloor", new Vector3(CenterX, -0.45f, -4.5f), new Vector3(Length, 0.5f, 27f), new Color(0.055f, 0.07f, 0.085f), true);
            CreateBlock("TunnelCeiling", new Vector3(CenterX, 6.2f, -3.25f), new Vector3(Length, 0.35f, 29.5f), new Color(0.09f, 0.12f, 0.145f), true);
            CreateBlock("TunnelNorthWall", new Vector3(CenterX, 2.9f, 11.5f), new Vector3(Length, 6.6f, 0.45f), new Color(0.12f, 0.15f, 0.17f), true);
            CreateBlock("TunnelSouthWall", new Vector3(CenterX, 2.9f, -18f), new Vector3(Length, 6.6f, 0.45f), new Color(0.12f, 0.15f, 0.17f), true);
            CreateBlock("TunnelWestPortal", new Vector3(CenterX - Length * 0.5f, 2.9f, -4.5f), new Vector3(0.6f, 6.6f, 27f), new Color(0.025f, 0.035f, 0.045f), true);
            CreateBlock("TunnelEastPortal", new Vector3(CenterX + Length * 0.5f, 2.9f, -4.5f), new Vector3(0.6f, 6.6f, 27f), new Color(0.025f, 0.035f, 0.045f), true);
        }

        private void BuildPlatforms()
        {
            CreateBlock("PlatformNorthExtension", new Vector3(CenterX, 0.08f, 4.5f), new Vector3(Length - 6f, 0.45f, 7.7f), new Color(0.34f, 0.37f, 0.39f), true);
            CreateBlock("PlatformSouth", new Vector3(CenterX, 0.08f, -13.5f), new Vector3(Length - 6f, 0.45f, 7.7f), new Color(0.34f, 0.37f, 0.39f), true);
            CreateBlock("SafetyLineNorth", new Vector3(CenterX, 0.33f, 0.78f), new Vector3(Length - 8f, 0.04f, 0.32f), new Color(0.98f, 0.73f, 0.08f), false);
            CreateBlock("SafetyLineSouth", new Vector3(CenterX, 0.33f, -9.78f), new Vector3(Length - 8f, 0.04f, 0.32f), new Color(0.98f, 0.73f, 0.08f), false);

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

        private void BuildSigns()
        {
            CreateWorldLabel("StationNameNorth", "SAKURA METRO", new Vector3(CenterX, 4.45f, 11.24f), Quaternion.Euler(0f, 180f, 0f), 0.65f);
            CreateWorldLabel("Platform1", "1  >  MIDORI / SHINJUKU", new Vector3(CenterX - 13f, 4.4f, 5.9f), Quaternion.Euler(0f, 180f, 0f), 0.55f);
            CreateWorldLabel("Platform2", "2  >  ASAKUSA / SAKURA", new Vector3(CenterX + 13f, 4.4f, -14.9f), Quaternion.identity, 0.55f);
        }

        private void BuildOppositeTrain()
        {
            string[] sourceNames = { "HighSpeed_Front", "HighSpeed_Wagon_A", "HighSpeed_Wagon_B" };
            float[] offsets = { 10f, 0f, -10f };
            for (int i = 0; i < sourceNames.Length; i++)
            {
                GameObject source = GameObject.Find(sourceNames[i]);
                if (source == null) continue;
                GameObject clone = Instantiate(source, _generated);
                clone.name = "Opposite_" + sourceNames[i];
                clone.transform.position = new Vector3(CenterX + offsets[i], source.transform.position.y, -7.15f);
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
