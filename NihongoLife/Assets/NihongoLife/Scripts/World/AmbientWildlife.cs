using System;
using System.Collections.Generic;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace NihongoLife.World
{
    public enum WildKind { Bird, Dog, Cat, Squirrel }

    /// <summary>
    /// K1: ambient animals for the open world. Birds (perched / flying / landing, chirping), dogs and cats (walk, sit, sleep, play,
    /// follow when petted) and squirrels (forage, nibble a nut, run to a tree and climb). What is out depends on the season,
    /// the hour and the rain (<see cref="Budget"/>). Everything is a small pooled primitive rig: a fixed pool is built once per
    /// scene, animals far from the player are hidden and not updated (LOD), and none of it has a physics body - so it stays light on WebGL.
    /// </summary>
    public sealed class AmbientWildlife : MonoBehaviour
    {
        public static AmbientWildlife Instance { get; private set; }
        public const int PoolBirds = 10, PoolDogs = 2, PoolCats = 2, PoolSquirrels = 6;
        public const float ActiveRadius = 45f, DespawnRadius = 75f;

        public readonly List<Critter> Critters = new List<Critter>();
        private readonly List<Vector3> _trees = new List<Vector3>();
        private readonly List<float> _treeTops = new List<float>();
        private Transform _player;
        private Vector3 _lastPlayerPos;
        private float _playerSpeed, _nextBudget, _nextScan;
        private bool _built;

        /// <summary>For tests: pin the clock/season instead of the day-night cycle.</summary>
        public static Season? SeasonOverride;
        public static float? HourOverride;
        public static bool Disabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; SeasonOverride = null; HourOverride = null; Disabled = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("AmbientWildlife");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AmbientWildlife>();
            SceneManager.sceneLoaded += (s, m) => { if (Instance != null) Instance.ResetForScene(); };
        }

        public static bool SceneHasWildlife(string scene) => scene == WorldLocationCatalog.CityScene || scene == WorldLocationCatalog.MidoriIslandScene;

        public Season CurrentSeason => SeasonOverride ?? (DayNightCycle.Active != null ? DayNightCycle.Active.Season : Season.Spring);
        public float CurrentHour => HourOverride ?? (DayNightCycle.Active != null ? DayNightCycle.Active.Hour : 12f);
        public bool Raining => WeatherCycle.Instance != null && WeatherCycle.Instance.IsRaining;
        public Transform Player => _player;
        public float PlayerSpeed => _playerSpeed;
        public IReadOnlyList<Vector3> Trees => _trees;
        public IReadOnlyList<float> TreeTops => _treeTops;

        /// <summary>How many of each kind are out: pure function of kind, season, hour and rain (unit tested).</summary>
        public static int Budget(WildKind kind, Season season, float hour, bool rain)
        {
            bool day = hour >= 5.5f && hour < 18.5f;
            bool dawn = hour >= 5f && hour < 8.5f;
            bool dusk = hour >= 17f && hour < 20.5f;
            switch (kind)
            {
                case WildKind.Bird:
                    if (!day) return 0;
                    int b = season switch { Season.Spring => 9, Season.Summer => 8, Season.Autumn => 6, _ => 3 };
                    if (dawn && season != Season.Winter) b += 1;
                    return rain ? Mathf.Max(1, b / 3) : Mathf.Min(PoolBirds, b);
                case WildKind.Dog:
                    return rain ? 0 : season == Season.Winter ? 1 : 2;
                case WildKind.Cat:
                    if (rain) return 0;
                    return season == Season.Winter ? 1 : (dusk || hour < 5.5f || hour >= 20.5f) ? 2 : 2;
                case WildKind.Squirrel:
                    if (!day || rain) return 0;
                    return season switch { Season.Autumn => PoolSquirrels, Season.Winter => 1, Season.Spring => 4, _ => 3 };
            }
            return 0;
        }

        public static bool IsSleepTime(WildKind kind, float hour)
        {
            if (kind == WildKind.Dog) return hour >= 22f || hour < 5f;
            if (kind == WildKind.Cat) return (hour >= 12f && hour < 14.5f) || hour >= 1f && hour < 4f;
            return false;
        }

        private void ResetForScene()
        {
            foreach (var c in Critters) if (c != null) Destroy(c.gameObject);
            Critters.Clear(); _trees.Clear(); _treeTops.Clear();
            _built = false; _player = null;
        }

        private void Update()
        {
            if (Disabled || !SceneHasWildlife(SceneManager.GetActiveScene().name)) return;
            if (_player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc == null) return;
                _player = pc.transform; _lastPlayerPos = _player.position;
            }
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            _playerSpeed = Mathf.Lerp(_playerSpeed, (_player.position - _lastPlayerPos).magnitude / dt, 0.2f);
            _lastPlayerPos = _player.position;
            if (!_built)
            {
                if (Time.timeSinceLevelLoad < 3f) return; // let the world finish loading first
                Build();
            }
            if (Time.unscaledTime >= _nextBudget) { _nextBudget = Time.unscaledTime + 2.5f; ApplyBudget(); }
        }

        private void Build()
        {
            _built = true;
            ScanTrees();
            for (int i = 0; i < PoolBirds; i++) Add(WildKind.Bird);
            for (int i = 0; i < PoolDogs; i++) Add(WildKind.Dog);
            for (int i = 0; i < PoolCats; i++) Add(WildKind.Cat);
            for (int i = 0; i < PoolSquirrels; i++) Add(WildKind.Squirrel);
            ApplyBudget();
        }

        private void Add(WildKind kind)
        {
            var go = new GameObject(kind + "_" + Critters.Count);
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Critter>();
            c.Init(this, kind, Critters.Count);
            Critters.Add(c);
            go.SetActive(false);
        }

        /// <summary>Remembers where the trees are (name contains "tree") once per scene.</summary>
        private void ScanTrees()
        {
            _trees.Clear(); _treeTops.Clear();
            var seen = new HashSet<Vector2Int>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r == null) continue;
                string n = r.transform.root == r.transform ? r.name : r.transform.parent.name + "/" + r.name;
                if (n.IndexOf("tree", StringComparison.OrdinalIgnoreCase) < 0 || n.IndexOf("street", StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("tree", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var b = r.bounds;
                if (b.size.y < 2.5f || b.size.y > 30f) continue;
                var key = new Vector2Int(Mathf.RoundToInt(b.center.x / 1.5f), Mathf.RoundToInt(b.center.z / 1.5f));
                if (!seen.Add(key)) continue;
                _trees.Add(new Vector3(b.center.x, b.min.y, b.center.z));
                _treeTops.Add(b.max.y);
                if (_trees.Count >= 400) break;
            }
        }

        public bool NearestTree(Vector3 from, float maxDistance, out Vector3 baseAt, out float top)
        {
            baseAt = default; top = 0f; float best = maxDistance * maxDistance; bool found = false;
            for (int i = 0; i < _trees.Count; i++)
            {
                float d = (new Vector3(_trees[i].x, from.y, _trees[i].z) - from).sqrMagnitude;
                if (d < best) { best = d; baseAt = _trees[i]; top = _treeTops[i]; found = true; }
            }
            return found;
        }

        public void ApplyBudget()
        {
            if (_player == null) return;
            var season = CurrentSeason; float hour = CurrentHour; bool rain = Raining;
            int[] want = new int[4];
            for (int k = 0; k < 4; k++) want[k] = Budget((WildKind)k, season, hour, rain);
            if (_trees.Count == 0) want[(int)WildKind.Squirrel] = 0;
            int[] have = new int[4];
            foreach (var c in Critters)
            {
                int k = (int)c.Kind;
                bool keep = c.gameObject.activeSelf && have[k] < want[k] && (c.transform.position - _player.position).sqrMagnitude < DespawnRadius * DespawnRadius;
                if (keep) { have[k]++; continue; }
                if (c.gameObject.activeSelf) c.gameObject.SetActive(false);
            }
            foreach (var c in Critters)
            {
                int k = (int)c.Kind;
                if (c.gameObject.activeSelf || have[k] >= want[k]) continue;
                if (c.Place(_player.position)) { c.gameObject.SetActive(true); c.OnSpawn(); have[k]++; }
            }
        }

        public int ActiveCount(WildKind kind)
        {
            int n = 0; foreach (var c in Critters) if (c.Kind == kind && c.gameObject.activeSelf) n++;
            return n;
        }

        // ───── shared bits ─────
        private static readonly Dictionary<int, Material> Mats = new Dictionary<int, Material>();
        private static Material _template;

        public static Material MaterialFor(Color color)
        {
            int key = ((Color32)color).GetHashCode();
            if (Mats.TryGetValue(key, out var m) && m != null) return m;
            if (_template == null)
            {
                var prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _template = prim.GetComponent<Renderer>().sharedMaterial;
                Destroy(prim);
            }
            m = new Material(_template) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
            Mats[key] = m;
            return m;
        }

        public static bool Ground(Vector3 from, out Vector3 point)
        {
            if (Physics.Raycast(from + Vector3.up * 2f, Vector3.down, out var hit, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.6f)
            {
                point = hit.point; return true;
            }
            point = from; return false;
        }

        private static readonly Dictionary<int, AudioClip> Chirps = new Dictionary<int, AudioClip>();

        /// <summary>Procedural bird calls (no audio files): 3 short chirp patterns.</summary>
        public static AudioClip ChirpClip(int variant)
        {
            variant = Mathf.Abs(variant) % 3;
            if (Chirps.TryGetValue(variant, out var c) && c != null) return c;
            const int rate = 22050;
            float len = 0.22f + 0.12f * variant;
            int n = (int)(rate * len);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float freq = variant == 0 ? 3200f + 1400f * Mathf.Sin(t * Mathf.PI)
                           : variant == 1 ? 4200f - 1800f * t + 400f * Mathf.Sin(t * 40f)
                           : 2800f + 2200f * Mathf.Abs(Mathf.Sin(t * 9f));
                phase += 2f * Mathf.PI * freq / rate;
                float env = Mathf.Sin(t * Mathf.PI); env *= env;
                float gate = variant == 2 ? (Mathf.Sin(t * 28f) > -0.2f ? 1f : 0.2f) : 1f;
                data[i] = Mathf.Sin(phase) * env * gate * 0.35f;
            }
            c = AudioClip.Create("chirp" + variant, n, 1, rate, false);
            c.SetData(data, 0);
            Chirps[variant] = c;
            return c;
        }
    }

    /// <summary>One pooled animal. Behaviour per kind is a small state machine; the body is a handful of primitives animated by code.</summary>
    public sealed class Critter : MonoBehaviour, IInteractable
    {
        public enum State { Idle, Walk, Run, Sit, Sleep, Play, Follow, Perched, Flying, Landing, Forage, Nibble, ToTree, Climb, OnTree, Descend, Flee }

        public WildKind Kind { get; private set; }
        public State Current { get; private set; }
        public int Index { get; private set; }
        public float FriendUntil { get; private set; }
        public Vector3 BirdPerch => _perch;
        public string Variant { get; private set; } = "";

        private AmbientWildlife _owner;
        private Transform _visual, _body, _head, _tail, _wingL, _wingR, _legFL, _legFR, _legBL, _legBR;
        private Renderer[] _renderers;
        private AudioSource _audio;
        private float _stateEnd, _phase, _nextChirp, _heading, _speedNow, _orbitAngle, _orbitRadius, _orbitAlt, _walkSpeed;
        private Vector3 _goal, _perch, _orbitCenter, _treeBase, _landFrom;
        private float _treeTop, _climbT, _heartUntil;
        private bool _visible = true;
        private GameObject _heart;
        private static float _lastChirp;

        public bool IsVisible => _visible;

        public void Init(AmbientWildlife owner, WildKind kind, int index)
        {
            _owner = owner; Kind = kind; Index = index;
            _visual = new GameObject("Visual").transform; _visual.SetParent(transform, false);
            switch (kind)
            {
                case WildKind.Bird: BuildBird(); break;
                case WildKind.Dog: BuildQuadruped(true); break;
                case WildKind.Cat: BuildQuadruped(false); break;
                default: BuildSquirrel(); break;
            }
            _renderers = _visual.GetComponentsInChildren<Renderer>(true);
            foreach (var r in _renderers) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (kind == WildKind.Bird)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.spatialBlend = 1f; _audio.minDistance = 3f; _audio.maxDistance = 30f; _audio.rolloffMode = AudioRolloffMode.Linear;
                _audio.playOnAwake = false; _audio.volume = 0.22f;
            }
            if (kind == WildKind.Dog || kind == WildKind.Cat)
            {
                var col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true; col.radius = kind == WildKind.Squirrel ? 0.5f : 1.1f; col.center = Vector3.up * 0.3f;
            }
        }

        // ───── build ─────
        private Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = AmbientWildlife.MaterialFor(color);
            return go.transform;
        }

        private Transform Pivot(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = pos; return t;
        }

        private void BuildBird()
        {
            _body = Pivot("Body", _visual, new Vector3(0f, 0.1f, 0f));
            var c = new Color(0.45f, 0.33f, 0.22f);
            Part(PrimitiveType.Sphere, _body, Vector3.zero, new Vector3(0.1f, 0.09f, 0.17f), c);
            _head = Part(PrimitiveType.Sphere, _body, new Vector3(0f, 0.05f, 0.09f), Vector3.one * 0.07f, c * 0.9f);
            Part(PrimitiveType.Cube, _head, new Vector3(0f, -0.1f, 0.55f), new Vector3(0.18f, 0.18f, 0.5f), new Color(0.9f, 0.7f, 0.2f));
            _tail = Part(PrimitiveType.Cube, _body, new Vector3(0f, 0.01f, -0.11f), new Vector3(0.05f, 0.012f, 0.1f), c * 0.8f);
            _wingL = Pivot("WingL", _body, new Vector3(-0.045f, 0.02f, 0f));
            _wingR = Pivot("WingR", _body, new Vector3(0.045f, 0.02f, 0f));
            Part(PrimitiveType.Cube, _wingL, new Vector3(-0.07f, 0f, 0f), new Vector3(0.14f, 0.01f, 0.1f), c * 0.75f);
            Part(PrimitiveType.Cube, _wingR, new Vector3(0.07f, 0f, 0f), new Vector3(0.14f, 0.01f, 0.1f), c * 0.75f);
        }

        private void BuildQuadruped(bool dog)
        {
            Color fur = dog ? new Color(0.72f, 0.5f, 0.28f) : new Color(0.35f, 0.33f, 0.32f);
            float s = dog ? 1f : 0.7f;
            Variant = dog ? "dog" : "cat";
            _body = Pivot("Body", _visual, new Vector3(0f, 0.38f * s, 0f));
            Part(PrimitiveType.Cube, _body, Vector3.zero, new Vector3(0.24f, 0.24f, 0.6f) * s, fur);
            _head = Pivot("Head", _body, new Vector3(0f, 0.12f * s, 0.34f * s));
            Part(PrimitiveType.Sphere, _head, Vector3.zero, Vector3.one * 0.22f * s, fur);
            Part(PrimitiveType.Cube, _head, new Vector3(0f, -0.03f * s, 0.12f * s), new Vector3(0.1f, 0.09f, 0.14f) * s, fur * 0.85f);
            Part(PrimitiveType.Cube, _head, new Vector3(-0.07f * s, 0.12f * s, 0f), new Vector3(0.06f, dog ? 0.1f : 0.12f, 0.04f) * s, fur * 0.7f);
            Part(PrimitiveType.Cube, _head, new Vector3(0.07f * s, 0.12f * s, 0f), new Vector3(0.06f, dog ? 0.1f : 0.12f, 0.04f) * s, fur * 0.7f);
            _tail = Pivot("Tail", _body, new Vector3(0f, 0.06f * s, -0.3f * s));
            Part(PrimitiveType.Cube, _tail, new Vector3(0f, 0.1f * s, -0.08f * s), new Vector3(0.05f, dog ? 0.05f : 0.04f, dog ? 0.22f : 0.34f) * s, fur * 0.9f);
            Transform Leg(string n, float x, float z) { var p = Pivot(n, _body, new Vector3(x * s, -0.1f * s, z * s)); Part(PrimitiveType.Cube, p, new Vector3(0f, -0.14f * s, 0f), new Vector3(0.07f, 0.28f, 0.07f) * s, fur * 0.85f); return p; }
            _legFL = Leg("FL", -0.09f, 0.22f); _legFR = Leg("FR", 0.09f, 0.22f);
            _legBL = Leg("BL", -0.09f, -0.22f); _legBR = Leg("BR", 0.09f, -0.22f);
            _walkSpeed = dog ? 1.5f : 1.1f;
        }

        private void BuildSquirrel()
        {
            Color fur = new Color(0.55f, 0.32f, 0.16f);
            _body = Pivot("Body", _visual, new Vector3(0f, 0.12f, 0f));
            Part(PrimitiveType.Sphere, _body, Vector3.zero, new Vector3(0.1f, 0.11f, 0.2f), fur);
            _head = Part(PrimitiveType.Sphere, _body, new Vector3(0f, 0.05f, 0.11f), Vector3.one * 0.08f, fur * 0.95f);
            _tail = Pivot("Tail", _body, new Vector3(0f, 0.03f, -0.1f));
            Part(PrimitiveType.Sphere, _tail, new Vector3(0f, 0.08f, -0.06f), new Vector3(0.07f, 0.18f, 0.09f), fur * 1.15f);
            _legFL = Part(PrimitiveType.Sphere, _body, new Vector3(0f, -0.04f, 0.07f), new Vector3(0.09f, 0.04f, 0.05f), fur * 0.8f);
            _walkSpeed = 2.4f;
        }

        // ───── placement / lifecycle ─────

        public bool Place(Vector3 around)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float ang = Random.value * Mathf.PI * 2f, dist = Random.Range(14f, 38f);
                var p = around + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
                if (Kind == WildKind.Bird)
                {
                    if (FindPerch(p, out var perch)) { transform.position = perch; _perch = perch; return true; }
                    continue;
                }
                if (Kind == WildKind.Squirrel)
                {
                    if (!_owner.NearestTree(p, 12f, out var tb, out var top)) continue;
                    p = tb + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
                }
                if (!AmbientWildlife.Ground(new Vector3(p.x, around.y + 3f, p.z), out var g)) continue;
                if (Mathf.Abs(g.y - around.y) > 3.5f) continue;
                transform.position = g;
                _heading = Random.value * 360f;
                transform.rotation = Quaternion.Euler(0f, _heading, 0f);
                return true;
            }
            return false;
        }

        private bool FindPerch(Vector3 near, out Vector3 perch)
        {
            perch = default;
            var player = _owner.Player;
            if (_owner.NearestTree(near, 14f, out var tb, out var top))
            {
                perch = new Vector3(tb.x + Random.Range(-0.8f, 0.8f), top - Random.Range(0.3f, 1.2f), tb.z + Random.Range(-0.8f, 0.8f));
                return true;
            }
            if (Physics.Raycast(new Vector3(near.x, player.position.y + 40f, near.z), Vector3.down, out var hit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.point.y - player.position.y > 3f)
            { perch = hit.point; return true; }
            perch = new Vector3(near.x, player.position.y + Random.Range(7f, 11f), near.z); // a wire / lamp height
            return true;
        }

        public void OnSpawn()
        {
            _visible = true; SetVisible(true);
            _phase = Random.value * 10f; _speedNow = 0f; FriendUntil = 0f;
            switch (Kind)
            {
                case WildKind.Bird:
                    if (Random.value < 0.35f) StartFlying(); else Enter(State.Perched, Random.Range(6f, 18f));
                    transform.position = Current == State.Flying ? transform.position : _perch;
                    _nextChirp = Time.time + Random.Range(1f, 6f);
                    break;
                case WildKind.Dog:
                case WildKind.Cat:
                    Pick();
                    break;
                default:
                    Enter(State.Forage, Random.Range(4f, 9f));
                    break;
            }
        }

        public void Force(State s, float seconds = 6f) { if (s == State.Flying) StartFlying(); else Enter(s, seconds); }

        private void Enter(State s, float seconds)
        {
            Current = s; _stateEnd = Time.time + seconds;
            if (s == State.Walk || s == State.Forage || s == State.Run || s == State.Play) { _goal = RandomGoal(s == State.Play ? 5f : 9f); }
        }

        private Vector3 RandomGoal(float radius)
        {
            float a = Random.value * Mathf.PI * 2f;
            return transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(radius * 0.4f, radius);
        }

        private void SetVisible(bool v)
        {
            _visible = v;
            for (int i = 0; i < _renderers.Length; i++) if (_renderers[i] != null) _renderers[i].enabled = v;
        }

        // ───── update ─────
        private void Update()
        {
            var player = _owner.Player;
            if (player == null) return;
            float dist2 = (transform.position - player.position).sqrMagnitude;
            if (dist2 > AmbientWildlife.DespawnRadius * AmbientWildlife.DespawnRadius) { gameObject.SetActive(false); return; }
            bool near = dist2 < AmbientWildlife.ActiveRadius * AmbientWildlife.ActiveRadius;
            if (near != _visible) SetVisible(near);
            if (!near) return;                               // LOD: out of range = no animation, no logic
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            _phase += dt;
            switch (Kind)
            {
                case WildKind.Bird: UpdateBird(dt, player); break;
                case WildKind.Dog:
                case WildKind.Cat: UpdateQuad(dt, player); break;
                default: UpdateSquirrel(dt, player); break;
            }
            if (_heart != null && Time.time > _heartUntil) { Destroy(_heart); _heart = null; }
            else if (_heart != null) _heart.transform.position += Vector3.up * 0.6f * dt;
        }

        // ── birds
        private void StartFlying()
        {
            Current = State.Flying; _stateEnd = Time.time + Random.Range(10f, 20f);
            _orbitCenter = _owner.Player != null ? _owner.Player.position + new Vector3(Random.Range(-18f, 18f), 0f, Random.Range(-18f, 18f)) : transform.position;
            _orbitRadius = Random.Range(6f, 13f); _orbitAlt = Random.Range(7f, 14f); _orbitAngle = Random.value * 360f;
            if (_body != null) _body.localRotation = Quaternion.identity;
        }

        private void UpdateBird(float dt, Transform player)
        {
            switch (Current)
            {
                case State.Perched:
                    transform.position = Vector3.MoveTowards(transform.position, _perch, 2f * dt);
                    _head.localRotation = Quaternion.Euler(0f, Mathf.Sin(_phase * 1.7f) * 40f * (Mathf.Sin(_phase * 0.4f) > 0.5f ? 1f : 0f), 0f);
                    _body.localPosition = new Vector3(0f, 0.1f + Mathf.Max(0f, Mathf.Sin(_phase * 9f)) * (Mathf.Sin(_phase * 0.7f) > 0.8f ? 0.02f : 0f), 0f);
                    _wingL.localRotation = Quaternion.Euler(0f, 0f, -75f); _wingR.localRotation = Quaternion.Euler(0f, 0f, 75f);
                    if (Time.time >= _stateEnd || (player.position - transform.position).sqrMagnitude < 25f && _owner.PlayerSpeed > 5f) StartFlying();
                    break;
                case State.Flying:
                    _orbitAngle += (7f / Mathf.Max(3f, _orbitRadius)) * Mathf.Rad2Deg * dt;
                    var want = _orbitCenter + new Vector3(Mathf.Cos(_orbitAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(_orbitAngle * Mathf.Deg2Rad)) * _orbitRadius;
                    want.y = player.position.y + _orbitAlt + Mathf.Sin(_phase * 1.3f) * 0.6f;
                    var delta = want - transform.position;
                    if (delta.sqrMagnitude > 0.0001f) { transform.position = Vector3.MoveTowards(transform.position, want, 9f * dt); transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(delta.x, delta.y * 0.3f, delta.z)), 4f * dt); }
                    float flap = Mathf.Sin(_phase * 26f) * 55f;
                    _wingL.localRotation = Quaternion.Euler(0f, 0f, flap); _wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
                    if (Time.time >= _stateEnd)
                    {
                        if (FindPerch(transform.position + new Vector3(Random.Range(-12f, 12f), 0f, Random.Range(-12f, 12f)), out var p)) { _perch = p; _landFrom = transform.position; Current = State.Landing; _climbT = 0f; }
                        else StartFlying();
                    }
                    break;
                case State.Landing:
                    _climbT += dt / 2.2f;
                    transform.position = Vector3.Lerp(_landFrom, _perch, Mathf.SmoothStep(0f, 1f, _climbT)) + Vector3.up * Mathf.Sin(_climbT * Mathf.PI) * 1.2f;
                    var d2 = _perch - _landFrom; d2.y = 0f; if (d2.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d2), 6f * dt);
                    float fl = Mathf.Sin(_phase * 30f) * 60f * (1f - _climbT * 0.6f);
                    _wingL.localRotation = Quaternion.Euler(0f, 0f, fl); _wingR.localRotation = Quaternion.Euler(0f, 0f, -fl);
                    if (_climbT >= 1f) { transform.position = _perch; Enter(State.Perched, Random.Range(8f, 25f)); }
                    break;
            }
            if (Time.time >= _nextChirp)
            {
                _nextChirp = Time.time + Random.Range(3f, 11f);
                if (Time.time - _lastChirp > 0.7f && !StoreCameraZone.Contains(player.position) && _audio != null && _owner.CurrentHour > 5f)
                { _lastChirp = Time.time; _audio.pitch = Random.Range(0.9f, 1.25f); _audio.PlayOneShot(AmbientWildlife.ChirpClip(Index + (int)(Time.time))); Chirps++; }
            }
        }

        public int Chirps { get; private set; }

        // ── dogs and cats
        private bool Sleepy => AmbientWildlife.IsSleepTime(Kind, _owner.CurrentHour);

        private void Pick()
        {
            bool dog = Kind == WildKind.Dog;
            var r = Random.value;
            if (Sleepy) { Enter(State.Sleep, Random.Range(20f, 50f)); return; }
            if (r < 0.32f) Enter(State.Walk, Random.Range(5f, 10f));
            else if (r < 0.55f) Enter(State.Sit, Random.Range(5f, 12f));
            else if (r < 0.72f) Enter(dog ? State.Play : State.Idle, Random.Range(5f, 9f));
            else if (r < 0.82f && !dog) Enter(State.Sleep, Random.Range(10f, 25f));
            else Enter(State.Idle, Random.Range(3f, 7f));
        }

        private void UpdateQuad(float dt, Transform player)
        {
            bool dog = Kind == WildKind.Dog;
            float toPlayer = Vector3.Distance(transform.position, player.position);
            bool friend = Time.time < FriendUntil;
            // The player rushing at a cat, or any loud approach, scares it off.
            if (!dog && !friend && toPlayer < 2.5f && _owner.PlayerSpeed > 4.5f && Current != State.Run) { Enter(State.Run, 2.5f); _goal = transform.position + (transform.position - player.position).normalized * 8f; }
            if (friend && Current != State.Follow && Current != State.Sit && Current != State.Sleep) Enter(State.Follow, 4f);
            if (!friend && Current == State.Follow) Pick();
            if (Current != State.Follow && Time.time >= _stateEnd) { if (Sleepy && Current != State.Sleep) Enter(State.Sleep, 20f); else Pick(); }
            if (Current == State.Sleep && !Sleepy && Time.time >= _stateEnd) Pick();

            float speed = 0f; bool moving = false;
            Vector3 dir = Vector3.zero;
            switch (Current)
            {
                case State.Walk: moving = true; speed = _walkSpeed; dir = _goal - transform.position; break;
                case State.Run: moving = true; speed = _walkSpeed * 3f; dir = _goal - transform.position; break;
                case State.Play:
                    moving = true; speed = _walkSpeed * 2.6f;
                    { float a = _phase * 1.6f; _goal = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3f; dir = _goal - transform.position; }
                    break;
                case State.Follow:
                    dir = player.position - transform.position;
                    if (toPlayer > 2.4f) { moving = true; speed = toPlayer > 7f ? _walkSpeed * 3f : _walkSpeed * 1.4f; }
                    else if (toPlayer < 1.6f) { dir = Vector3.zero; }
                    break;
            }
            dir.y = 0f;
            if (moving && dir.sqrMagnitude < 0.6f && Current != State.Follow && Current != State.Play) { moving = false; Pick(); }
            if (moving)
            {
                var fwd = dir.normalized;
                _heading = Mathf.LerpAngle(_heading, Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg, 7f * dt);
                transform.rotation = Quaternion.Euler(0f, _heading, 0f);
                var step = transform.forward * speed * dt;
                if (Physics.SphereCast(transform.position + Vector3.up * 0.4f, 0.2f, transform.forward, out var wall, speed * dt + 0.35f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                { step = Vector3.zero; if (Current == State.Follow) { } else { Enter(State.Idle, 1f); _heading += 120f; } }
                var np = transform.position + step;
                if (AmbientWildlife.Ground(new Vector3(np.x, transform.position.y + 0.6f, np.z), out var g) && Mathf.Abs(g.y - transform.position.y) < 0.6f) np.y = Mathf.Lerp(transform.position.y, g.y, 0.5f);
                else if (step != Vector3.zero) { np = transform.position; Enter(State.Idle, 1f); _heading += 150f; }
                transform.position = np;
            }
            else if (Current == State.Follow || Current == State.Sit)
            {
                var look = player.position - transform.position; look.y = 0f;
                if (look.sqrMagnitude > 0.01f && toPlayer < 8f) { _heading = Mathf.LerpAngle(_heading, Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg, 3f * dt); transform.rotation = Quaternion.Euler(0f, _heading, 0f); }
            }
            _speedNow = Mathf.Lerp(_speedNow, moving ? speed : 0f, 8f * dt);
            AnimateQuad(dog, dt, toPlayer);
        }

        private void AnimateQuad(bool dog, float dt, float toPlayer)
        {
            float s = dog ? 1f : 0.7f;
            float swing = Mathf.Clamp01(_speedNow / (_walkSpeed * 2f));
            float f = _phase * (6f + _speedNow * 3f);
            float amp = Mathf.Lerp(0f, 38f, Mathf.Min(1f, _speedNow / _walkSpeed));
            bool sit = Current == State.Sit, sleep = Current == State.Sleep;
            float bodyY = sleep ? 0.13f * s : sit ? 0.27f * s : 0.38f * s;
            bodyY += Mathf.Abs(Mathf.Sin(f)) * 0.03f * swing;
            _body.localPosition = Vector3.Lerp(_body.localPosition, new Vector3(0f, bodyY, 0f), 10f * dt);
            _body.localRotation = Quaternion.Slerp(_body.localRotation, Quaternion.Euler(sit ? -35f : 0f, 0f, sleep ? 8f : 0f), 8f * dt);
            _legFL.localRotation = Quaternion.Euler(sleep ? -80f : sit ? 35f : Mathf.Sin(f) * amp, 0f, 0f);
            _legFR.localRotation = Quaternion.Euler(sleep ? -80f : sit ? 35f : -Mathf.Sin(f) * amp, 0f, 0f);
            _legBL.localRotation = Quaternion.Euler(sleep ? 80f : sit ? -90f : -Mathf.Sin(f) * amp, 0f, 0f);
            _legBR.localRotation = Quaternion.Euler(sleep ? 80f : sit ? -90f : Mathf.Sin(f) * amp, 0f, 0f);
            float headPitch = sleep ? 30f : (Current == State.Idle && Mathf.Sin(_phase * 0.8f) > 0.6f) ? 35f : sit ? 25f : 0f;
            _head.localRotation = Quaternion.Slerp(_head.localRotation, Quaternion.Euler(headPitch, sleep ? 0f : Mathf.Sin(_phase * 0.9f) * 20f, 0f), 5f * dt);
            float wag = (Time.time < FriendUntil || Current == State.Play) ? 40f * Mathf.Sin(_phase * 18f) : 8f * Mathf.Sin(_phase * 2f);
            _tail.localRotation = Quaternion.Euler(sleep ? 0f : -10f, dog ? wag : Mathf.Sin(_phase * 1.5f) * 18f, 0f);
            if (sleep) { float b = 1f + Mathf.Sin(_phase * 1.6f) * 0.04f; _body.localScale = new Vector3(1f, b, 1f); } else _body.localScale = Vector3.one;
        }

        // ── squirrels
        private void UpdateSquirrel(float dt, Transform player)
        {
            float toPlayer = Vector3.Distance(transform.position, player.position);
            if ((Current == State.Forage || Current == State.Nibble || Current == State.Idle) && toPlayer < 3.2f && _owner.NearestTree(transform.position, 25f, out _treeBase, out _treeTop)) { Current = State.ToTree; _stateEnd = Time.time + 8f; }
            switch (Current)
            {
                case State.Forage:
                    {
                        var dir = _goal - transform.position; dir.y = 0f;
                        if (dir.sqrMagnitude < 0.3f || Time.time >= _stateEnd) { Enter(Random.value < 0.55f ? State.Nibble : State.Forage, Random.Range(3f, 7f)); break; }
                        MoveSquirrel(dir.normalized, 2.4f, dt);
                        _body.localPosition = new Vector3(0f, 0.12f + Mathf.Abs(Mathf.Sin(_phase * 12f)) * 0.06f, 0f);
                        _tail.localRotation = Quaternion.Euler(-20f + Mathf.Sin(_phase * 12f) * 12f, 0f, 0f);
                    }
                    break;
                case State.Nibble:
                    _body.localPosition = new Vector3(0f, 0.15f, 0f);
                    _body.localRotation = Quaternion.Slerp(_body.localRotation, Quaternion.Euler(-50f, 0f, 0f), 6f * dt);   // sits up on its haunches
                    _head.localPosition = new Vector3(0f, 0.05f + Mathf.Abs(Mathf.Sin(_phase * 14f)) * 0.012f, 0.11f);       // chewing
                    _tail.localRotation = Quaternion.Euler(Mathf.Sin(_phase * 3f) * 10f, 0f, 0f);
                    if (Time.time >= _stateEnd) { _body.localRotation = Quaternion.identity; Enter(State.Forage, Random.Range(3f, 6f)); }
                    break;
                case State.ToTree:
                    {
                        if (_treeBase == default && !_owner.NearestTree(transform.position, 40f, out _treeBase, out _treeTop)) { Enter(State.Forage, 4f); break; }
                        _body.localRotation = Quaternion.identity;
                        var dir = new Vector3(_treeBase.x, transform.position.y, _treeBase.z) - transform.position;
                        if (dir.magnitude < 0.9f || Time.time >= _stateEnd) { Current = State.Climb; _climbT = 0f; _stateEnd = Time.time + 30f; break; }
                        MoveSquirrel(dir.normalized, 5f, dt);
                        _body.localPosition = new Vector3(0f, 0.12f + Mathf.Abs(Mathf.Sin(_phase * 16f)) * 0.07f, 0f);
                    }
                    break;
                case State.Climb:
                    {
                        float height = Mathf.Min(_treeTop - _treeBase.y - 0.5f, 5f);
                        _climbT += dt * 2.2f / Mathf.Max(1f, height);
                        float y = _treeBase.y + Mathf.Clamp01(_climbT) * height;
                        var radial = (new Vector3(transform.position.x, 0f, transform.position.z) - new Vector3(_treeBase.x, 0f, _treeBase.z));
                        if (radial.sqrMagnitude < 0.01f) radial = Vector3.right;
                        radial = radial.normalized * 0.45f;
                        transform.position = new Vector3(_treeBase.x + radial.x, y, _treeBase.z + radial.z);
                        transform.rotation = Quaternion.LookRotation(-radial.normalized, Vector3.up);
                        _body.localRotation = Quaternion.Euler(-85f, 0f, 0f);     // clings upright to the trunk
                        _body.localPosition = new Vector3(0f, 0.12f + Mathf.Abs(Mathf.Sin(_phase * 10f)) * 0.03f, 0f);
                        if (_climbT >= 1f) { Current = State.OnTree; _stateEnd = Time.time + Random.Range(6f, 12f); }
                    }
                    break;
                case State.OnTree:
                    _body.localRotation = Quaternion.Slerp(_body.localRotation, Quaternion.Euler(-60f, 0f, 0f), 4f * dt);
                    _head.localPosition = new Vector3(0f, 0.05f + Mathf.Abs(Mathf.Sin(_phase * 14f)) * 0.012f, 0.11f);
                    if (Time.time >= _stateEnd && toPlayer > 5f) { Current = State.Descend; _climbT = 1f; }
                    break;
                case State.Descend:
                    {
                        float height = Mathf.Min(_treeTop - _treeBase.y - 0.5f, 5f);
                        _climbT -= dt * 2.6f / Mathf.Max(1f, height);
                        float y = _treeBase.y + Mathf.Clamp01(_climbT) * height;
                        transform.position = new Vector3(transform.position.x, y, transform.position.z);
                        _body.localRotation = Quaternion.Euler(85f, 0f, 0f);
                        if (_climbT <= 0f) { _body.localRotation = Quaternion.identity; transform.position = new Vector3(transform.position.x, _treeBase.y, transform.position.z); Enter(State.Forage, 5f); }
                    }
                    break;
                default:
                    Enter(State.Forage, 4f);
                    break;
            }
        }

        private void MoveSquirrel(Vector3 dir, float speed, float dt)
        {
            _heading = Mathf.LerpAngle(_heading, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 9f * dt);
            transform.rotation = Quaternion.Euler(0f, _heading, 0f);
            var np = transform.position + transform.forward * speed * dt;
            if (AmbientWildlife.Ground(new Vector3(np.x, transform.position.y + 0.6f, np.z), out var g) && Mathf.Abs(g.y - transform.position.y) < 0.5f) { np.y = g.y; transform.position = np; }
            else { _goal = RandomGoal(6f); }
            _head.localPosition = new Vector3(0f, 0.05f, 0.11f);
        }

        // ───── interaction ─────
        public string GetPromptJa() => Kind == WildKind.Dog ? "いぬをなでる" : Kind == WildKind.Cat ? "ねこをなでる" : "";
        public string GetpromptEn()
        {
            if (Kind == WildKind.Dog) return Loc.T("Vuốt ve chú chó", "Pet the dog", "いぬをなでる");
            if (Kind == WildKind.Cat) return Loc.T("Vuốt ve chú mèo", "Pet the cat", "ねこをなでる");
            return Loc.T("Chú sóc", "Squirrel", "リス");
        }
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (Kind == WildKind.Squirrel || Kind == WildKind.Bird) return;
            bool dog = Kind == WildKind.Dog;
            FriendUntil = Time.time + (dog ? 120f : 40f);
            if (Current == State.Sleep) Enter(State.Idle, 1f);
            Enter(State.Follow, 4f);
            HudFeed.Post(dog ? Loc.T("Chú chó vẫy đuôi và đi theo bạn.", "The dog wags its tail and follows you.", "いぬがしっぽをふって、ついてくる。")
                             : Loc.T("Chú mèo kêu grừ grừ và quấn quanh chân bạn.", "The cat purrs and rubs against your legs.", "ねこがのどをならして、すりよってくる。"), HudFeed.Kind.Reward, 3.5f);
            ShowHeart();
        }

        private void ShowHeart()
        {
            if (_heart != null) Destroy(_heart);
            _heart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(_heart.GetComponent<Collider>());
            _heart.name = "Heart";
            _heart.transform.position = transform.position + Vector3.up * 1.0f;
            _heart.transform.localScale = Vector3.one * 0.16f;
            _heart.GetComponent<Renderer>().sharedMaterial = AmbientWildlife.MaterialFor(new Color(0.95f, 0.2f, 0.35f));
            _heartUntil = Time.time + 1.6f;
        }
    }
}