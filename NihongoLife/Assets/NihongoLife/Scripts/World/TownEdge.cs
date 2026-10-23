using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.World
{
    /// <summary>
    /// Closes the town's map edge with a fence, a river, low hills and a fog-coloured skirt so the player
    /// meets scenery instead of a black void. Standing at the edge shows a short in-world note, and F can
    /// leave for Hibari Station once that zone is unlocked. The far skyline already built by HorizonBackdrop
    /// is left in place.
    /// </summary>
    public sealed class TownEdge : MonoBehaviour
    {
        public static TownEdge Active { get; private set; }

        private Bounds _bounds;
        private bool _ready;
        private bool _nearEdge;
        private bool _hasHorizon;
        private float _nextNote;
        private Material _skirtMat;
        private Material _hillMat;
        private Material _wallMat;
        private Material _riverMat;
        private readonly List<Renderer> _windows = new();
        private MaterialPropertyBlock _block;

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
                if (root.GetComponent<TownEdge>() != null) return;
            if (!IsTown(scene)) return;
            var go = new GameObject("TownEdge");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<TownEdge>();
        }

        /// <summary>
        /// Only the town has an edge to walk out of. Zones (island, station, shops) also carry a HorizonBackdrop for the
        /// view through their doors; treating them as the town made their counters and fishing spots "the edge", and F
        /// there travelled to the station instead of using them.
        /// </summary>
        private static bool IsTown(Scene scene) => scene.name == WorldLocationCatalog.CityScene;

        private void OnEnable()
        {
            if (gameObject.scene == SceneManager.GetActiveScene()) Active = this;
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
        }

        private void Start()
        {
            if (gameObject.scene == SceneManager.GetActiveScene()) Active = this;
            _hasHorizon = false;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root != null && root.GetComponentInChildren<HorizonBackdrop>(true) != null) { _hasHorizon = true; break; }
            }
            if (!Measure(out _bounds)) return;
            BuildCollar();
            _ready = true;
        }

        /// <summary>
        /// Slides the player back inside the town, keeping their height. Returns false when this edge is not
        /// active (interiors) or the player has fallen, so <see cref="WorldBoundsGuard"/> can still rescue a fall.
        /// Returns true even when the player is already inside, so the old X-only clamp does not also fire.
        /// </summary>
        public static bool TryClamp(Transform body, CharacterController controller)
        {
            var edge = Active;
            if (edge == null || !edge._ready || body == null) return false;
            if (body.gameObject.scene != edge.gameObject.scene) return false;
            if (body.position.y < -4f) return false;
            Vector3 p = body.position;
            const float margin = 1.15f;
            Vector3 next = p;
            next.x = Mathf.Clamp(p.x, edge._bounds.min.x + margin, edge._bounds.max.x - margin);
            next.z = Mathf.Clamp(p.z, edge._bounds.min.z + margin, edge._bounds.max.z - margin);
            if ((next - p).sqrMagnitude > 0.0004f)
            {
                if (controller != null) controller.enabled = false;
                body.position = next;
                if (controller != null) controller.enabled = true;
            }
            return true;
        }

        /// <summary>F at the edge travels to Hibari Station when it is unlocked. Otherwise the key is left for the world.</summary>
        public static bool TryTravel()
        {
            var edge = Active;
            if (edge == null || !edge._ready || !edge._nearEdge) return false;
            if (SceneManager.GetActiveScene() != edge.gameObject.scene) return false; // in a zone, F belongs to the zone
            if (!LocationUnlocks.IsUnlocked(WorldLocationCatalog.StationScene)) return false;
            if (!GameServices.TryGet(out SceneFlowController flow) || flow.IsLoading) return false;
            flow.EnterZone(WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance,
                Loc.T("Ga Hibari", "Hibari Station", "ひばり駅"));
            edge._nearEdge = false;
            return true;
        }

        private void LateUpdate()
        {
            if (!_ready) return;
            TintToFog();
            var player = PlayerStatus.Instance;
            if (player == null || player.gameObject.scene != gameObject.scene) { _nearEdge = false; return; }
            Vector3 p = player.transform.position;
            float dx = Mathf.Min(p.x - _bounds.min.x, _bounds.max.x - p.x);
            float dz = Mathf.Min(p.z - _bounds.min.z, _bounds.max.z - p.z);
            _nearEdge = dx < 7f || dz < 7f;
            if (!_nearEdge || Time.unscaledTime < _nextNote) return;
            _nextNote = Time.unscaledTime + 9f;
            bool station = LocationUnlocks.IsUnlocked(WorldLocationCatalog.StationScene);
            string msg = Loc.T(
                "Con đường này dẫn ra khỏi Hibari-chō…",
                "This road leads out of Hibari-chō…",
                "この道はひばり町の外へ続いています…");
            msg += "\n" + (station
                ? Loc.T("Nhấn F để đi Ga Hibari.", "Press F to travel to Hibari Station.", "Fキーでひばり駅へ。")
                : Loc.T("Ga Hibari sẽ mở khi bạn học thêm.", "Hibari Station opens as you learn more.", "ひばり駅は、もう少し学ぶと開きます。"));
            HudFeed.Post(msg, HudFeed.Kind.Info, 6.5f, "town-edge");
        }

        private bool Measure(out Bounds bounds)
        {
            bool any = false;
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root == null || root == gameObject) continue;
                if (SkipRoot(root.name)) continue;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer) continue;
                    string name = renderer.name;
                    if (name.IndexOf("Horizon", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Backdrop", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Sky", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Rain", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    Bounds b = renderer.bounds;
                    if (b.size.y > 40f || b.size.x > 220f || b.size.z > 220f) continue;
                    if (Mathf.Abs(b.center.x) > 180f || Mathf.Abs(b.center.z) > 180f) continue;
                    if (!any) { bounds = b; any = true; }
                    else bounds.Encapsulate(b);
                }
            }
            return any && bounds.size.x >= 20f && bounds.size.z >= 20f;
        }

        private static bool SkipRoot(string name) =>
            name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Canvas", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("EventSystem", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("AppRoot", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Horizon", StringComparison.OrdinalIgnoreCase) >= 0;

        private void BuildCollar()
        {
            _skirtMat = Unlit("TownEdgeSkirt", new Color(0.2f, 0.22f, 0.24f));
            _riverMat = Unlit("TownEdgeRiver", new Color(0.16f, 0.28f, 0.36f));
            _hillMat = Unlit("TownEdgeHill", new Color(0.28f, 0.34f, 0.3f));
            _wallMat = Unlit("TownEdgeWall", new Color(0.22f, 0.26f, 0.3f));
            var fenceMat = Unlit("TownEdgeFence", new Color(0.42f, 0.36f, 0.28f));
            var buildingMat = Unlit("TownEdgeBuilding", new Color(0.2f, 0.22f, 0.28f));
            var windowMat = Unlit("TownEdgeWindow", new Color(0.16f, 0.13f, 0.09f));

            Vector3 min = _bounds.min;
            Vector3 max = _bounds.max;
            float y = -0.04f;
            const float outReach = 72f;
            const float overlap = 3f;
            Strip("Skirt_N", min.x - outReach, max.x + outReach, max.z - overlap, max.z + outReach, y, 0.08f, _skirtMat);
            Strip("Skirt_S", min.x - outReach, max.x + outReach, min.z - outReach, min.z + overlap, y, 0.08f, _skirtMat);
            Strip("Skirt_E", max.x - overlap, max.x + outReach, min.z + overlap, max.z - overlap, y, 0.08f, _skirtMat);
            Strip("Skirt_W", min.x - outReach, min.x + overlap, min.z + overlap, max.z - overlap, y, 0.08f, _skirtMat);

            float width = max.x - min.x;
            float depth = max.z - min.z;
            Fence("Fence_N", new Vector3((min.x + max.x) * 0.5f, 0.9f, max.z), new Vector3(width + 1.2f, 1.8f, 0.38f), fenceMat);
            Fence("Fence_S", new Vector3((min.x + max.x) * 0.5f, 0.9f, min.z), new Vector3(width + 1.2f, 1.8f, 0.38f), fenceMat);
            Fence("Fence_E", new Vector3(max.x, 0.9f, (min.z + max.z) * 0.5f), new Vector3(0.38f, 1.8f, depth + 1.2f), fenceMat);
            Fence("Fence_W", new Vector3(min.x, 0.9f, (min.z + max.z) * 0.5f), new Vector3(0.38f, 1.8f, depth + 1.2f), fenceMat);

            const float river = 7f;
            Strip("River_N", min.x - 8f, max.x + 8f, max.z + 1.4f, max.z + 1.4f + river, y + 0.05f, 0.06f, _riverMat);
            Strip("River_S", min.x - 8f, max.x + 8f, min.z - 1.4f - river, min.z - 1.4f, y + 0.05f, 0.06f, _riverMat);
            Strip("River_E", max.x + 1.4f, max.x + 1.4f + river, min.z - 8f, max.z + 8f, y + 0.05f, 0.06f, _riverMat);
            Strip("River_W", min.x - 1.4f - river, min.x - 1.4f, min.z - 8f, max.z + 8f, y + 0.05f, 0.06f, _riverMat);

            // A low wall just outside the fence reads as land even when the night sky is nearly black.
            Wall("Wall_N", new Vector3((min.x + max.x) * 0.5f, 4f, max.z + 14f), new Vector3(width + 40f, 8f, 3f));
            Wall("Wall_S", new Vector3((min.x + max.x) * 0.5f, 4f, min.z - 14f), new Vector3(width + 40f, 8f, 3f));
            Wall("Wall_E", new Vector3(max.x + 14f, 4f, (min.z + max.z) * 0.5f), new Vector3(3f, 8f, depth + 40f));
            Wall("Wall_W", new Vector3(min.x - 14f, 4f, (min.z + max.z) * 0.5f), new Vector3(3f, 8f, depth + 40f));

            float near = _hasHorizon ? 22f : 32f;
            float far = _hasHorizon ? 42f : 62f;
            float hill = _hasHorizon ? 9f : 16f;
            Hill("Hill_N0", new Vector3(min.x + width * 0.3f, 0f, max.z + near), hill);
            Hill("Hill_N1", new Vector3(min.x + width * 0.72f, 0f, max.z + far), hill * 1.35f);
            Hill("Hill_S0", new Vector3(min.x + width * 0.4f, 0f, min.z - near), hill * 0.9f);
            Hill("Hill_S1", new Vector3(min.x + width * 0.8f, 0f, min.z - far), hill * 1.2f);
            Hill("Hill_E0", new Vector3(max.x + near, 0f, min.z + depth * 0.35f), hill);
            Hill("Hill_W0", new Vector3(min.x - near, 0f, min.z + depth * 0.6f), hill * 1.1f);

            if (_hasHorizon) return;
            Building("EdgeBuilding_N", new Vector3((min.x + max.x) * 0.5f, 0f, max.z + 46f), buildingMat, windowMat);
            Building("EdgeBuilding_S", new Vector3((min.x + max.x) * 0.5f, 0f, min.z - 46f), buildingMat, windowMat);
            Building("EdgeBuilding_E", new Vector3(max.x + 46f, 0f, (min.z + max.z) * 0.5f), buildingMat, windowMat);
            Building("EdgeBuilding_W", new Vector3(min.x - 46f, 0f, (min.z + max.z) * 0.5f), buildingMat, windowMat);
        }

        private void Strip(string name, float x0, float x1, float z0, float z1, float y, float h, Material mat)
        {
            var go = Block(name, new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f), new Vector3(Mathf.Abs(x1 - x0), h, Mathf.Abs(z1 - z0)), mat, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f);
        }

        private void Fence(string name, Vector3 pos, Vector3 scale, Material mat) => Block(name, pos, scale, mat, true);
        private void Wall(string name, Vector3 pos, Vector3 scale) => Block(name, pos, scale, _wallMat, false);

        private void Hill(string name, Vector3 pos, float height)
        {
            float width = height * 2.4f;
            Block(name, pos + Vector3.up * (height * 0.5f), new Vector3(width, height, width * 0.72f), _hillMat, false);
        }

        private void Building(string name, Vector3 pos, Material body, Material window)
        {
            float h = 12f;
            Block(name, pos + Vector3.up * (h * 0.5f), new Vector3(8f, h, 7f), body, false);
            var win = Block(name + "_Windows", pos + new Vector3(0f, h * 0.62f, 3.6f), new Vector3(5.2f, 4.2f, 0.12f), window, false);
            _windows.Add(win.GetComponent<Renderer>());
        }

        private GameObject Block(string name, Vector3 pos, Vector3 scale, Material mat, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!solid)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        private void TintToFog()
        {
            Color fog = RenderSettings.fogColor;
            if (fog.maxColorComponent < 0.02f) fog = new Color(0.07f, 0.08f, 0.12f);
            Set(_skirtMat, Color.Lerp(fog, new Color(0.24f, 0.26f, 0.28f), 0.28f));
            Set(_hillMat, Color.Lerp(fog, new Color(0.32f, 0.4f, 0.34f), 0.4f));
            Set(_wallMat, Color.Lerp(fog, new Color(0.3f, 0.34f, 0.38f), 0.22f));
            Set(_riverMat, Color.Lerp(fog, new Color(0.18f, 0.32f, 0.42f), 0.45f));
            float night = TownNightLights.Factor;
            if (_windows.Count == 0) return;
            _block ??= new MaterialPropertyBlock();
            _block.SetColor(EmissionId, new Color(1.2f, 0.82f, 0.46f) * (1.7f * night));
            for (int i = 0; i < _windows.Count; i++)
                if (_windows[i] != null) _windows[i].SetPropertyBlock(_block);
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private static void Set(Material mat, Color color)
        {
            if (mat == null) return;
            if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, color);
            if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, color);
        }

        private static Material Unlit(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var mat = new Material(shader) { name = name };
            Set(mat, color);
            return mat;
        }
    }
}
