using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NihongoLife.Core;

namespace NihongoLife.World
{
    /// <summary>
    /// Turns the town's street lamps, a few building windows and (via <see cref="Factor"/>) car bulbs on after dusk.
    /// Emission does the work. Only the nearest handful of realtime lights stay on, and none of them cast shadows,
    /// so WebGL's two-additional-lights-per-object budget is not asked to light the whole street.
    /// </summary>
    [DefaultExecutionOrder(80)]
    public sealed class TownNightLights : MonoBehaviour
    {
        /// <summary>0 in daylight, 1 at night. Car headlights read this.</summary>
        public static float Factor { get; private set; }

        private struct Lamp
        {
            public Light Light;
            public float Intensity;
            public Vector3 Point;
            public int Stamp;
        }

        private readonly List<Lamp> _lamps = new();
        private readonly List<int> _order = new();
        private readonly List<Renderer> _windows = new();
        private readonly List<Renderer> _heads = new();
        private MaterialPropertyBlock _block;
        private int _stamp;
        private bool _scanned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) TryInstall(SceneManager.GetSceneAt(i));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _) => TryInstall(scene);

        private static void TryInstall(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (scene.name.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0) return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<TownNightLights>() != null) return;
            if (scene.name != WorldLocationCatalog.CityScene && !ContainsStreetLight(scene)) return;

            var go = new GameObject("TownNightLights");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<TownNightLights>();
        }

        private static bool ContainsStreetLight(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (FindLamp(root.transform) != null) return true;
            return false;
        }

        private void Start() => Scan();

        private void Scan()
        {
            if (_scanned) return;
            _scanned = true;
            var scene = gameObject.scene;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null || root == gameObject) continue;
                Collect(root.transform);
            }
        }

        private void Collect(Transform node)
        {
            if (IsStreetLamp(node))
            {
                AddLamp(node);
                return;
            }
            for (int i = 0; i < node.childCount; i++) Collect(node.GetChild(i));
        }

        private void AddLamp(Transform lamp)
        {
            Renderer headRenderer = null;
            var renderers = lamp.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "LampHead";
                var col = head.GetComponent<Collider>();
                if (col != null) Destroy(col);
                head.transform.SetParent(lamp, true);
                head.transform.position = new Vector3(bounds.center.x, bounds.max.y - 0.12f, bounds.center.z);
                float s = 0.28f;
                var lossy = head.transform.lossyScale;
                head.transform.localScale = new Vector3(
                    s / Mathf.Max(0.01f, lossy.x), s / Mathf.Max(0.01f, lossy.y), s / Mathf.Max(0.01f, lossy.z));
                headRenderer = head.GetComponent<Renderer>();
                headRenderer.sharedMaterial = GlowMaterial();
                headRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                headRenderer.receiveShadows = false;
                _heads.Add(headRenderer);
            }

            Light light = null;
            foreach (var candidate in lamp.GetComponentsInChildren<Light>(true))
            {
                if (candidate == null || candidate.type == LightType.Directional) continue;
                light = candidate;
                break;
            }
            if (light == null)
            {
                var go = new GameObject("StreetLampLight");
                go.transform.SetParent(lamp, false);
                light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.78f, 0.48f);
                light.intensity = 1.35f;
                light.range = 7.5f;
                if (headRenderer != null) go.transform.position = headRenderer.transform.position;
            }
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            if (light.range > 9f) light.range = 9f;
            light.enabled = false;
            // DayNightCycle may already have dimmed this light in Awake. Keep the authored brightness, not that temporary value.
            float authored = light.intensity >= 0.75f ? light.intensity : light.type == LightType.Spot ? 1.6f : 1.35f;
            _lamps.Add(new Lamp { Light = light, Intensity = authored, Point = light.transform.position });
        }

        private void LateUpdate()
        {
            if (!_scanned) Scan();
            var sky = OutdoorSky.Active;
            bool drives = sky == null
                ? gameObject.scene == SceneManager.GetActiveScene()
                : sky.gameObject.scene == gameObject.scene;
            if (!drives)
            {
                for (int i = 0; i < _lamps.Count; i++)
                    if (_lamps[i].Light != null) _lamps[i].Light.enabled = false;
                return;
            }

            float daylight = sky != null ? sky.Daylight : 1f;
            Factor = Mathf.Clamp01((0.42f - daylight) / 0.28f);
            bool night = Factor > 0.04f;
            PaintHeads(night ? Factor : 0f);
            PaintWindows(night ? Factor : 0f);
            BudgetLights(night);
        }

        private void BudgetLights(bool night)
        {
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            int budget = Cheap() ? 4 : 8;
            _order.Clear();
            for (int i = 0; i < _lamps.Count; i++) _order.Add(i);
            int keep = night ? Mathf.Min(budget, _order.Count) : 0;
            for (int i = 0; i < keep; i++)
            {
                int best = i;
                float bestD = Distance(_order[i], eye);
                for (int j = i + 1; j < _order.Count; j++)
                {
                    float d = Distance(_order[j], eye);
                    if (d < bestD) { bestD = d; best = j; }
                }
                int swap = _order[i];
                _order[i] = _order[best];
                _order[best] = swap;
            }
            _stamp++;
            for (int i = 0; i < keep; i++)
            {
                int index = _order[i];
                var lamp = _lamps[index];
                lamp.Stamp = _stamp;
                _lamps[index] = lamp;
            }
            for (int i = 0; i < _lamps.Count; i++)
            {
                var lamp = _lamps[i];
                if (lamp.Light == null) continue;
                bool on = lamp.Stamp == _stamp;
                lamp.Light.enabled = on;
                if (on) lamp.Light.intensity = lamp.Intensity * Mathf.Lerp(0.35f, 1f, Factor);
                lamp.Light.shadows = LightShadows.None;
            }
        }

        private float Distance(int index, Vector3 eye)
        {
            var lamp = _lamps[index];
            lamp.Point = lamp.Light != null ? lamp.Light.transform.position : lamp.Point;
            _lamps[index] = lamp;
            return (lamp.Point - eye).sqrMagnitude;
        }

        private void PaintHeads(float factor)
        {
            if (_heads.Count == 0) return;
            _block ??= new MaterialPropertyBlock();
            Color glow = new Color(1f, 0.72f, 0.38f) * (2.8f * factor);
            _block.SetColor(BaseColorId, new Color(1f, 0.82f, 0.5f, 1f));
            _block.SetColor(ColorId, new Color(1f, 0.82f, 0.5f, 1f));
            _block.SetColor(EmissionId, glow);
            for (int i = 0; i < _heads.Count; i++)
                if (_heads[i] != null) _heads[i].SetPropertyBlock(_block);
        }

        private void PaintWindows(float factor)
        {
            if (_windows.Count == 0 && factor <= 0f) return;
            if (_windows.Count == 0) CollectWindows();
            _block ??= new MaterialPropertyBlock();
            Color glow = new Color(1.15f, 0.78f, 0.42f) * (1.8f * factor);
            _block.SetColor(EmissionId, glow);
            for (int i = 0; i < _windows.Count; i++)
                if (_windows[i] != null) _windows[i].SetPropertyBlock(_block);
        }

        private void CollectWindows()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root == null || root.name.IndexOf("Horizon", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (_windows.Count >= 96) return;
                    if (renderer == null || renderer is ParticleSystemRenderer) continue;
                    string name = renderer.name;
                    if (name.IndexOf("poster", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (name.IndexOf("window", StringComparison.OrdinalIgnoreCase) < 0
                        && name.IndexOf("FrontGlass", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (Blocked(renderer.transform)) continue;
                    _windows.Add(renderer);
                }
            }
        }

        private static bool Cheap()
        {
#if UNITY_WEBGL
            return true;
#else
            var names = QualitySettings.names;
            int level = QualitySettings.GetQualityLevel();
            if (level >= 0 && level < names.Length && names[level] == "WebGL") return true;
            return Application.platform == RuntimePlatform.WebGLPlayer;
#endif
        }

        private static Transform FindLamp(Transform node)
        {
            if (IsStreetLamp(node)) return node;
            for (int i = 0; i < node.childCount; i++)
            {
                var found = FindLamp(node.GetChild(i));
                if (found != null) return found;
            }
            return null;
        }

        private static bool IsStreetLamp(Transform node)
        {
            string name = node.name;
            if (name.IndexOf("StreetLight", StringComparison.OrdinalIgnoreCase) < 0
                && name.IndexOf("street_light", StringComparison.OrdinalIgnoreCase) < 0) return false;
            return !Blocked(node);
        }

        private static bool Blocked(Transform node)
        {
            while (node != null)
            {
                string name = node.name;
                if (name.IndexOf("CarSignal", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("WalkSignal", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                node = node.parent;
            }
            return false;
        }

        private static Material _glow;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private static Material GlowMaterial()
        {
            if (_glow != null) return _glow;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            _glow = shader != null ? new Material(shader) { name = "StreetLampHead" } : null;
            if (_glow != null)
            {
                var warm = new Color(1f, 0.82f, 0.5f, 1f);
                if (_glow.HasProperty(BaseColorId)) _glow.SetColor(BaseColorId, warm);
                if (_glow.HasProperty(ColorId)) _glow.SetColor(ColorId, warm);
            }
            return _glow;
        }
    }
}
