using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.World
{
    /// <summary>
    /// Cars on one stretch of Sakura-dōri (built by CityStreetBuilder). Japan drives on the left: eastbound cars use the
    /// north lane, westbound the south lane. A light cycle at the zebra crossing drives the signals. Cars keep a gap,
    /// stop with the whole vehicle behind the stop line (never queued on the stripes), and brake for a player who is
    /// actually in the lane. Off the ends of the street they drive out of view, shrink away, and rejoin from the far
    /// end — they no longer pop from one edge to the other.
    /// </summary>
    public sealed class StreetTraffic : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Car
        {
            public Transform body;
            public Transform[] wheels;
            public int direction = 1;      // +1 east, -1 west
            public float cruise = 7f;
            [System.NonSerialized] public float speed;
            /// <summary>Pivot to the further bumper along the street, so the whole car stays behind the line.</summary>
            [System.NonSerialized] public float halfLength = 2.3f;
            /// <summary>Lane centre to the car's side (from the renderers).</summary>
            [System.NonSerialized] public float halfWidth = 0.95f;
            [System.NonSerialized] public bool entering;
            [System.NonSerialized] public Transform visual;
            [System.NonSerialized] public Vector3 visualBase = Vector3.one;
            [System.NonSerialized] public Collider solid;
            [System.NonSerialized] public float hornReadyAt;
            [System.NonSerialized] public float harmReadyAt;
            [System.NonSerialized] public float hitReadyAt;
            [System.NonSerialized] public float blockedFor;
            [System.NonSerialized] public bool lightsReady;
            [System.NonSerialized] public GameObject[] bulbs;
        }

        [System.Serializable]
        public sealed class Lamp
        {
            public Renderer renderer;
            public int phase;              // which phase lights it (see Phase)
            public Material on, off;
        }

        public enum Phase { CarGreen, CarYellow, AllRed1, WalkGreen, WalkFlash, AllRed2 }

        /// <summary>How far past the street a car drives before it is allowed to rejoin at the other end.</summary>
        public const float ExitMargin = 26f;
        /// <summary>Bumper stops this far behind the painted stop line.</summary>
        public const float LineClearance = 0.6f;
        const float FollowGap = 2.7f;
        /// <summary>A person this close to the car's side counts as in its path (body radius + a step).</summary>
        const float SideMargin = 0.55f;

        [SerializeField] private float laneEastZ = -8.4f, laneWestZ = -11.6f;
        [SerializeField] private float xMin = -64f, xMax = 64f;
        [SerializeField] private float crossX0 = 0.7f, crossX1 = 6.3f, crossZ0 = -13.6f, crossZ1 = -6.4f;
        [SerializeField] private float stopEastX = -1f, stopWestX = 8f;
        [SerializeField] private float hornCooldown = 4.6f;
        [SerializeField] private float blockDamagePerSecond = 14f;
        [SerializeField] private float hitDamage = 18f;
        [SerializeField] private float hitKnockSpeed = 5.6f;
        [SerializeField] private List<Car> cars = new();
        [SerializeField] private List<Lamp> lamps = new();
        [SerializeField] private AudioSource chirp;

        private static readonly float[] Durations = { 14f, 3f, 1.5f, 9f, 3f, 1.5f };
        private static readonly (string ja, string vi, string en)[] Honks =
        {
            ("危ないよ！", "Nguy hiểm đó!", "Watch out!"),
            ("どいてよ！", "Tránh ra nào!", "Move it!"),
            ("道路だよ！", "Đây là đường xe mà!", "This is a road!"),
            ("びっくりした！", "Làm tớ hết hồn!", "You scared me!"),
            ("車が来るよ！", "Xe tới kìa!", "Car coming!"),
            ("あぶないってば！", "Nguy hiểm lắm đó!", "I said look out!")
        };

        private Phase _phase;
        private float _phaseTime;
        private Transform _player;
        private PlayerController _playerBody;
        private float _nextChirp;
        private static float _nextHorn;
        private AudioSource _horn;
        private AudioClip _hornClip;
        private Canvas _bubble;
        private TextMeshProUGUI _bubbleText;
        private float _bubbleUntil;
        private Transform _bubbleAnchor;
        private static Material _bulbMat;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public Phase Current => _phase;
        public IReadOnlyList<Car> Cars => cars;
        public bool CarsMayGo => _phase == Phase.CarGreen;

        public void Configure(float eastZ, float westZ, float x0, float x1, Rect crossing, float stopEast, float stopWest)
        {
            laneEastZ = eastZ; laneWestZ = westZ; xMin = x0; xMax = x1;
            crossX0 = crossing.xMin; crossX1 = crossing.xMax; crossZ0 = crossing.yMin; crossZ1 = crossing.yMax;
            stopEastX = stopEast; stopWestX = stopWest;
        }

        public void AddCar(Transform body, Transform[] wheels, int direction, float cruise) => cars.Add(new Car { body = body, wheels = wheels, direction = direction, cruise = cruise });
        public void AddLamp(Renderer r, Phase phase, Material on, Material off) => lamps.Add(new Lamp { renderer = r, phase = (int)phase, on = on, off = off });
        public void SetChirp(AudioSource source) => chirp = source;

        /// <summary>Tests: jump the light cycle to a phase.</summary>
        public void ForcePhase(Phase phase) { _phase = phase; _phaseTime = 0f; ApplyLamps(); }

        /// <summary>Local X of the bumper the tests and the stop line both use (pivot ± half-length).</summary>
        public static float BumperX(float originX, int direction, float halfLength) => originX + direction * halfLength;

        /// <summary>True when the car's full length, treated as pivot ± half-length, overlaps the zebra.</summary>
        public static bool OverlapsCrossing(float originX, float halfLength, float cross0, float cross1)
        {
            float a = originX - halfLength, b = originX + halfLength;
            return b > cross0 && a < cross1;
        }

        /// <summary>Speed that still stops within <paramref name="distance"/> at 3.5 m/s² (0 once there).</summary>
        public static float Braking(float distance) => distance <= 0.03f ? 0f : Mathf.Sqrt(2f * 3.5f * distance);

        private void Start()
        {
            foreach (var c in cars)
            {
                c.speed = c.cruise;
                if (c.body == null) continue;
                Measure(c);
                c.visual = c.body.Find("Model");
                if (c.visual == null) c.visual = c.body;
                c.visualBase = c.visual.localScale;
                c.solid = c.body.GetComponent<Collider>();
                PullOffCrossing(c);
                EnsureBulbs(c);
            }
            ApplyLamps();
            if (chirp != null && chirp.clip == null) chirp.clip = MakeChirp();
            EnsureHorn();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _phaseTime += dt;
            if (_phaseTime >= Durations[(int)_phase])
            {
                _phaseTime = 0f;
                _phase = (Phase)(((int)_phase + 1) % Durations.Length);
                ApplyLamps();
            }
            if (_phase == Phase.WalkFlash) Blink();
            if (_phase == Phase.WalkGreen && chirp != null && Time.time >= _nextChirp) { chirp.Play(); _nextChirp = Time.time + 1.1f; }

            if (_player == null)
            {
                _playerBody = SceneRefs.Find<PlayerController>();
                if (_playerBody != null) _player = _playerBody.transform;
            }
            Vector3 playerLocal = default;
            bool hasPlayer = _player != null;
            if (hasPlayer) playerLocal = transform.InverseTransformPoint(_player.position);

            Car leadBlock = null;
            float leadAhead = float.MaxValue;
            if (hasPlayer)
            {
                foreach (var car in cars)
                {
                    if (car.body == null || !InPath(car, playerLocal, out float ahead)) continue;
                    if (ahead < leadAhead) { leadAhead = ahead; leadBlock = car; }
                }
            }

            foreach (var car in cars) Drive(car, dt, hasPlayer, playerLocal, car == leadBlock);
            PlaceBubble();
            ApplyNightBulbs();
        }

        private void Drive(Car car, float dt, bool hasPlayer, Vector3 playerLocal, bool isLeadBlock)
        {
            if (car.body == null) return;
            Vector3 pos = car.body.localPosition;
            float x = pos.x, dir = car.direction;
            float target = car.cruise;
            float bumper = BumperX(x, car.direction, car.halfLength);
            float stopX = dir > 0 ? stopEastX : stopWestX;
            float bumperToLine = (stopX - bumper) * dir;
            bool committed = bumperToLine < -0.2f;
            bool playerOnCrossing = false, playerInLane = false;
            float ahead = 99f;
            if (hasPlayer)
            {
                playerInLane = InLane(car, playerLocal, out ahead);
                playerOnCrossing = OnCrossing(playerLocal) && ahead > -0.5f && ahead < 18f;
            }

            foreach (var other in cars)
            {
                if (other == car || other.body == null || other.direction != car.direction) continue;
                float otherRear = BumperX(other.body.localPosition.x, -other.direction, other.halfLength);
                float space = (otherRear - bumper) * dir;
                if (space > 0f && space < 14f) target = Mathf.Min(target, Braking(space - FollowGap));
            }

            // Red / yellow, or a person on the zebra: wait behind the line. A car already past the line clears
            // the crossing instead of freezing on the stripes.
            bool holdLine = !committed && (!CarsMayGo || playerOnCrossing);
            if (holdLine) target = Mathf.Min(target, Braking(bumperToLine - LineClearance));

            if (playerInLane)
            {
                if (!committed && playerOnCrossing) target = Mathf.Min(target, Braking(bumperToLine - LineClearance));
                else target = Mathf.Min(target, Braking(ahead - 2.4f));
            }

            car.speed = Mathf.MoveTowards(car.speed, target, (target < car.speed ? 9f : 3f) * dt);
            // Never creep onto the zebra while held at the line.
            if (holdLine && car.speed < 0.35f && bumperToLine < LineClearance + 0.15f)
            {
                float parked = stopX - dir * (car.halfLength + LineClearance);
                if ((parked - x) * dir < 0f) { x = parked; car.speed = 0f; }
            }
            else x += car.speed * dir * dt;

            float over = dir > 0 ? x - xMax : xMin - x;
            if (over > ExitMargin) { Recycle(car, ref x); over = dir > 0 ? x - xMax : xMin - x; }
            ApplyFade(car, x, over);

            car.body.localPosition = new Vector3(x, pos.y, pos.z);
            if (car.wheels != null && car.visual != null && car.visual.localScale.x > 0.2f)
                foreach (var w in car.wheels) if (w != null) w.Rotate(car.body.right, car.speed * dt / 0.33f * Mathf.Rad2Deg, Space.World);

            if (isLeadBlock && (playerInLane || playerOnCrossing)) ReactToPlayer(car, dt, ahead);
            else car.blockedFor = 0f;
            if (hasPlayer) SeparateFromPlayer(car, playerLocal);
        }

        private void ReactToPlayer(Car car, float dt, float ahead)
        {
            var status = PlayerStatus.Instance;
            bool stopped = car.speed < 1.15f;
            if (stopped) car.blockedFor += dt;
            else car.blockedFor = 0f;

            if (stopped && car.blockedFor > 0.35f && Time.time >= car.hornReadyAt)
            {
                Honk(car);
                car.hornReadyAt = Time.time + hornCooldown;
            }
            if (stopped && car.blockedFor > 0.45f && status != null && Time.time >= car.harmReadyAt)
            {
                status.ApplyHarm(blockDamagePerSecond * 0.5f);
                car.harmReadyAt = Time.time + 0.5f;
            }
        }

        /// <summary>
        /// The car body touches the player (walked into its side, stepped out too late, or climbed on it): the car
        /// stops, the player is set beside the car on the kerb side and nudged towards the pavement. A kinematic car
        /// must never push the CharacterController — Unity resolves that overlap upwards and the player ends up
        /// riding the car (and from there on the roofs).
        /// </summary>
        private void SeparateFromPlayer(Car car, Vector3 playerLocal)
        {
            if (_playerBody == null || car.body == null) return;
            Vector3 c = car.body.localPosition;
            var capsule = _playerBody.GetComponent<CharacterController>();
            float radius = capsule != null ? capsule.radius : 0.3f;
            // Actually touching: the capsule overlaps the car's footprint (a person on the centre line next to the lane is not).
            bool alongside = Mathf.Abs(playerLocal.x - c.x) < car.halfLength + radius;
            bool beside = Mathf.Abs(playerLocal.z - c.z) < car.halfWidth + radius - 0.05f;
            if (!alongside || !beside || playerLocal.y > c.y + 2.2f) return;

            car.speed = 0f;
            // Kerb side of this lane: eastbound (north lane) → north pavement, westbound → south pavement.
            float side = car.direction > 0 ? 1f : -1f;
            float kerbZ = car.direction > 0 ? crossZ1 + 0.9f : crossZ0 - 0.9f;
            float z = Mathf.Clamp(c.z + side * (car.halfWidth + 0.5f), Mathf.Min(c.z, kerbZ), Mathf.Max(c.z, kerbZ));
            Vector3 world = transform.TransformPoint(new Vector3(playerLocal.x, c.y + 0.08f, z));
            var controller = _playerBody.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            _playerBody.transform.position = world;
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
            _playerBody.Knockback(transform.TransformDirection(Vector3.forward * side) * hitKnockSpeed * 0.6f);

            if (Time.time >= car.hitReadyAt)
            {
                car.hitReadyAt = Time.time + 1.45f;
                PlayerStatus.Instance?.ApplyHarm(hitDamage);
                if (Time.time >= car.hornReadyAt - 1f) { Honk(car); car.hornReadyAt = Time.time + hornCooldown; }
            }
        }

        private bool InPath(Car car, Vector3 playerLocal, out float ahead)
        {
            float bumper = BumperX(car.body.localPosition.x, car.direction, car.halfLength);
            ahead = (playerLocal.x - bumper) * car.direction;
            return InLane(car, playerLocal, out _) || (OnCrossing(playerLocal) && ahead > -0.5f && ahead < 18f);
        }

        private bool InLane(Car car, Vector3 playerLocal, out float ahead)
        {
            float laneZ = car.direction > 0 ? laneEastZ : laneWestZ;
            float bumper = BumperX(car.body.localPosition.x, car.direction, car.halfLength);
            ahead = (playerLocal.x - bumper) * car.direction;
            return Mathf.Abs(playerLocal.z - laneZ) < car.halfWidth + SideMargin && ahead > -0.35f && ahead < 14f;
        }

        private bool OnCrossing(Vector3 p) =>
            p.x > crossX0 - 0.35f && p.x < crossX1 + 0.35f && p.z > crossZ0 - 0.2f && p.z < crossZ1 + 0.2f;

        private void Measure(Car car)
        {
            var renderers = car.body.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { car.halfLength = 2.2f; return; }
            float min = float.MaxValue, max = float.MinValue, zMin = float.MaxValue, zMax = float.MinValue;
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                Bounds b = r.bounds;
                Vector3 e = b.extents, c = b.center;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 world = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                    Vector3 local = transform.InverseTransformPoint(world);
                    if (local.x < min) min = local.x;
                    if (local.x > max) max = local.x;
                    if (local.z < zMin) zMin = local.z;
                    if (local.z > zMax) zMax = local.z;
                }
            }
            float laneCentre = car.body.localPosition.z;
            car.halfWidth = Mathf.Clamp(Mathf.Max(zMax - laneCentre, laneCentre - zMin), 0.7f, 1.3f);
            float pivot = car.body.localPosition.x;
            float front = car.direction > 0 ? max - pivot : pivot - min;
            float rear = car.direction > 0 ? pivot - min : max - pivot;
            car.halfLength = Mathf.Max(1.2f, front, rear);
        }

        /// <summary>A car baked on top of the zebra is rolled back behind the stop line before the first frame of traffic.</summary>
        private void PullOffCrossing(Car car)
        {
            if (!OverlapsCrossing(car.body.localPosition.x, car.halfLength, crossX0, crossX1)) return;
            float dir = car.direction;
            float stopX = dir > 0 ? stopEastX : stopWestX;
            float x = stopX - dir * (car.halfLength + LineClearance + 0.4f);
            for (int n = 0; n < 6 && Crowded(car, x); n++) x -= dir * (car.halfLength * 2f + FollowGap);
            var p = car.body.localPosition;
            car.body.localPosition = new Vector3(x, p.y, p.z);
        }

        private bool Crowded(Car car, float x)
        {
            foreach (var other in cars)
            {
                if (other == car || other.body == null || other.direction != car.direction) continue;
                if (Mathf.Abs(other.body.localPosition.x - x) < car.halfLength + other.halfLength + 1.2f) return true;
            }
            return false;
        }

        private void Recycle(Car car, ref float x)
        {
            float dir = car.direction;
            x = dir > 0 ? xMin - 3f : xMax + 3f;
            for (int n = 0; n < 5 && Crowded(car, x); n++) x -= dir * 8f;
            car.entering = true;
            car.speed = car.cruise;
            car.blockedFor = 0f;
        }

        private void ApplyFade(Car car, float x, float over)
        {
            float scale = 1f;
            if (car.entering)
            {
                float into = car.direction > 0 ? x - xMin : xMax - x;
                scale = Mathf.Lerp(0.02f, 1f, Mathf.Clamp01(into / 14f));
                if (into > 14f) car.entering = false;
            }
            else if (over > 0f) scale = Mathf.Lerp(1f, 0.02f, Mathf.Clamp01(over / ExitMargin));

            if (car.visual != null) car.visual.localScale = car.visualBase * scale;
            if (car.solid != null) car.solid.enabled = scale > 0.62f;
        }

        private void Honk(Car car)
        {
            if (Time.time < _nextHorn) return;
            _nextHorn = Time.time + 0.7f;
            EnsureHorn();
            if (_horn != null && car.body != null)
            {
                _horn.transform.position = car.body.position + Vector3.up * 1.1f;
                _horn.Play();
            }
            ShowBubble(car);
            SocialStanding.AddNotoriety(4, "blocking-traffic", 8f);
            SocialStanding.AddMoodlet("honked", SocialMood.Embarrassed, 0.55f, 0.7f);
        }

        private void ShowBubble(Car car)
        {
            EnsureBubble();
            if (_bubble == null) return;
            var line = Honks[Random.Range(0, Honks.Length)];
            string gloss = Loc.Gloss(line.vi, line.en);
            _bubbleText.text = string.IsNullOrEmpty(gloss) ? line.ja : $"{line.ja}\n<size=70%><color=#D5DDEA>{gloss}</color></size>";
            _bubbleAnchor = car.body;
            _bubbleUntil = Time.time + 2.7f;
            _bubble.gameObject.SetActive(true);
        }

        private void PlaceBubble()
        {
            if (_bubble == null || !_bubble.gameObject.activeSelf) return;
            if (_bubbleAnchor == null || Time.time > _bubbleUntil) { _bubble.gameObject.SetActive(false); return; }
            _bubble.transform.position = _bubbleAnchor.position + Vector3.up * 2.55f;
            var cam = Camera.main;
            if (cam != null) _bubble.transform.rotation = Quaternion.LookRotation(_bubble.transform.position - cam.transform.position);
        }

        private void EnsureHorn()
        {
            if (_horn != null) return;
            // Its own child object: the horn is moved to the honking car, and moving this component's transform would
            // carry the whole Traffic root — every car — along with it (cars "jumped" onto pavements and roofs).
            var hornObject = new GameObject("Horn");
            hornObject.transform.SetParent(transform, false);
            _horn = hornObject.AddComponent<AudioSource>();
            _horn.playOnAwake = false;
            _horn.spatialBlend = 1f;
            _horn.minDistance = 4f;
            _horn.maxDistance = 28f;
            _horn.volume = 0.55f;
            _horn.clip = _hornClip = MakeHorn();
        }

        private void EnsureBubble()
        {
            if (_bubble != null) return;
            var go = new GameObject("DriverBubble");
            go.transform.SetParent(transform, false);
            _bubble = go.AddComponent<Canvas>();
            _bubble.renderMode = RenderMode.WorldSpace;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 110f);
            rect.localScale = Vector3.one * 0.011f;
            var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(go.transform, false);
            var bg = bgGo.GetComponent<RectTransform>();
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(12f, 8f); tr.offsetMax = new Vector2(-12f, -8f);
            _bubbleText = textGo.AddComponent<TextMeshProUGUI>();
            _bubbleText.font = NLUi.ResolveFont();
            _bubbleText.fontSize = 28f;
            _bubbleText.alignment = TextAlignmentOptions.Center;
            _bubbleText.color = new Color(1f, 0.93f, 0.72f);
            _bubbleText.textWrappingMode = TextWrappingModes.Normal;
            go.SetActive(false);
        }

        private void EnsureBulbs(Car car)
        {
            if (car.lightsReady || car.body == null) return;
            car.lightsReady = true;
            Transform host = car.visual != null ? car.visual : car.body;
            float front = car.halfLength * 0.86f;
            car.bulbs = new[]
            {
                Bulb(host, new Vector3(-0.55f, 0.72f, front), new Color(1f, 0.93f, 0.72f), "Headlight_L"),
                Bulb(host, new Vector3(0.55f, 0.72f, front), new Color(1f, 0.93f, 0.72f), "Headlight_R"),
                Bulb(host, new Vector3(-0.5f, 0.62f, -front), new Color(1f, 0.22f, 0.16f), "Taillight_L"),
                Bulb(host, new Vector3(0.5f, 0.62f, -front), new Color(1f, 0.22f, 0.16f), "Taillight_R")
            };
            foreach (var b in car.bulbs) if (b != null) b.SetActive(false);
        }

        private GameObject Bulb(Transform parent, Vector3 local, Color color, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(0.22f, 0.12f, 0.08f);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = BulbMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            block.SetColor(EmissionId, color * 2.4f);
            renderer.SetPropertyBlock(block);
            return go;
        }

        private void ApplyNightBulbs()
        {
            float night = TownNightLights.Factor;
            bool on = night > 0.08f;
            foreach (var car in cars)
            {
                if (car.bulbs == null) continue;
                foreach (var b in car.bulbs) if (b != null && b.activeSelf != on) b.SetActive(on);
            }
        }

        private static Material BulbMaterial()
        {
            if (_bulbMat != null) return _bulbMat;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            _bulbMat = shader != null ? new Material(shader) { name = "CarBulb" } : null;
            return _bulbMat;
        }

        private void ApplyLamps()
        {
            foreach (var lamp in lamps)
            {
                if (lamp.renderer == null) continue;
                bool lit = lamp.phase == (int)_phase
                    || (lamp.phase == (int)Phase.AllRed1 && (_phase == Phase.AllRed2 || _phase == Phase.WalkGreen || _phase == Phase.WalkFlash))
                    || (lamp.phase == (int)Phase.AllRed2 && (_phase == Phase.CarGreen || _phase == Phase.CarYellow || _phase == Phase.AllRed1));
                lamp.renderer.sharedMaterial = lit ? lamp.on : lamp.off;
            }
        }

        private void Blink()
        {
            bool on = Mathf.Repeat(_phaseTime, 0.5f) < 0.25f;
            foreach (var lamp in lamps)
                if (lamp.renderer != null && lamp.phase == (int)Phase.WalkGreen) lamp.renderer.sharedMaterial = on ? lamp.on : lamp.off;
        }

        /// <summary>The "piyo piyo" two-tone chirp of Japanese pedestrian signals (synthesised, no recording needed).</summary>
        private static AudioClip MakeChirp()
        {
            const int rate = 22050;
            float length = 0.42f;
            var data = new float[(int)(rate * length)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float local = t < 0.16f ? t : t - 0.22f;
                if (t >= 0.16f && t < 0.22f) continue;
                float f = Mathf.Lerp(2900f, 2100f, local / 0.16f);
                float env = Mathf.Sin(Mathf.Clamp01(local / 0.16f) * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.35f;
            }
            var clip = AudioClip.Create("PiyoPiyo", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A short silly two-beep horn (synthesised, so WebGL needs no audio asset).</summary>
        private static AudioClip MakeHorn()
        {
            const int rate = 22050;
            float length = 0.62f;
            var data = new float[(int)(rate * length)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float env = 0f, f = 480f;
                if (t < 0.18f) { float u = t / 0.18f; f = Mathf.Lerp(640f, 380f, u); env = Mathf.Sin(u * Mathf.PI); }
                else if (t > 0.24f && t < 0.58f) { float u = (t - 0.24f) / 0.34f; f = Mathf.Lerp(340f, 560f, u); env = Mathf.Sin(u * Mathf.PI); }
                float wave = Mathf.Sin(2f * Mathf.PI * f * t) * 0.62f + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.18f;
                data[i] = wave * env * 0.45f;
            }
            var clip = AudioClip.Create("FunnyHorn", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
