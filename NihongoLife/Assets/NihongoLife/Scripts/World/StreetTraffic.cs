using System.Collections.Generic;
using NihongoLife.Player;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Cars on one stretch of Sakura-dōri (built by CityStreetBuilder). Japan drives on the left: eastbound cars use the
    /// north lane, westbound the south lane. A light cycle at the zebra crossing (car green → yellow → all red →
    /// pedestrians green, flashing, → all red) drives the horizontal car signals and the walking-figure signals, which
    /// chirp "piyo piyo" while pedestrians may cross. Cars keep a gap to the car ahead, stop at the line on yellow/red,
    /// and always stop for the player in front of them or on the crossing. Off the ends of the street they wrap round.
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
            /// <summary>Centre to front bumper (from the renderers), so the bumper, not the middle, stops at the line.</summary>
            [System.NonSerialized] public float halfLength = 2.3f;
        }

        [System.Serializable]
        public sealed class Lamp
        {
            public Renderer renderer;
            public int phase;              // which phase lights it (see Phase)
            public Material on, off;
        }

        public enum Phase { CarGreen, CarYellow, AllRed1, WalkGreen, WalkFlash, AllRed2 }

        [SerializeField] private float laneEastZ = -8.4f, laneWestZ = -11.6f;
        [SerializeField] private float xMin = -64f, xMax = 64f;
        [SerializeField] private float crossX0 = 0.7f, crossX1 = 6.3f, crossZ0 = -13.6f, crossZ1 = -6.4f;
        [SerializeField] private float stopEastX = -1f, stopWestX = 8f;
        [SerializeField] private List<Car> cars = new();
        [SerializeField] private List<Lamp> lamps = new();
        [SerializeField] private AudioSource chirp;

        private static readonly float[] Durations = { 14f, 3f, 1.5f, 9f, 3f, 1.5f };
        private Phase _phase;
        private float _phaseTime;
        private Transform _player;
        private float _nextChirp;

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

        private void Start()
        {
            foreach (var c in cars)
            {
                c.speed = c.cruise;
                if (c.body == null) continue;
                var renderers = c.body.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                c.halfLength = Mathf.Max(1.2f, b.extents.x); // cars drive along x
            }
            ApplyLamps();
            if (chirp != null && chirp.clip == null) chirp.clip = MakeChirp();
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

            if (_player == null) { var p = FindFirstObjectByType<PlayerController>(); if (p != null) _player = p.transform; }
            foreach (var car in cars) Drive(car, dt);
        }

        private void Drive(Car car, float dt)
        {
            if (car.body == null) return;
            Vector3 pos = car.body.localPosition;
            float x = pos.x, dir = car.direction;
            float target = car.cruise;

            // Car ahead in the same lane.
            foreach (var other in cars)
            {
                if (other == car || other.body == null || other.direction != car.direction) continue;
                float ahead = (other.body.localPosition.x - x) * dir;
                float gap = ahead - car.halfLength - other.halfLength;  // bumper to bumper
                if (ahead > 0f && gap < 3f) target = Mathf.Min(target, Mathf.Max(0f, (gap - 1.2f) * 2f));
            }
            // Stop line on yellow / red.
            float stopX = dir > 0 ? stopEastX : stopWestX;
            float toLine = (stopX - x) * dir;
            // The bumper stops 0.4 m behind the line; a car whose bumper already passed it (on yellow) drives on.
            float bumperToLine = toLine - car.halfLength;
            bool beforeLine = bumperToLine > -0.3f && bumperToLine < 14f;
            if (!CarsMayGo && beforeLine) target = Mathf.Min(target, Braking(bumperToLine - 0.4f));
            // The player in front of the car, or on the crossing ahead of it.
            if (_player != null)
            {
                Vector3 p = transform.InverseTransformPoint(_player.position);
                float laneZ = dir > 0 ? laneEastZ : laneWestZ;
                float front = (p.x - x) * dir;
                if (Mathf.Abs(p.z - laneZ) < 2.4f && front > 0f && front < 9f + car.halfLength) target = Mathf.Min(target, Braking(front - car.halfLength - 1f));
                bool onCrossing = p.x > crossX0 - 0.6f && p.x < crossX1 + 0.6f && p.z > crossZ0 && p.z < crossZ1;
                if (onCrossing && beforeLine) target = Mathf.Min(target, Braking(bumperToLine - 0.4f));
            }

            car.speed = Mathf.MoveTowards(car.speed, target, (target < car.speed ? 9f : 3f) * dt);
            x += car.speed * dir * dt;
            if (dir > 0 && x > xMax) x = xMin;
            if (dir < 0 && x < xMin) x = xMax;
            car.body.localPosition = new Vector3(x, pos.y, pos.z);
            if (car.wheels != null)
                foreach (var w in car.wheels) if (w != null) w.Rotate(car.body.right, car.speed * dt / 0.33f * Mathf.Rad2Deg, Space.World); // spin about the axle
        }

        /// <summary>Speed that still stops within <paramref name="distance"/> at an even 3.5 m/s² (0 once there).</summary>
        private static float Braking(float distance) => distance <= 0.03f ? 0f : Mathf.Sqrt(2f * 3.5f * distance);

        private void ApplyLamps()
        {
            foreach (var lamp in lamps)
            {
                if (lamp.renderer == null) continue;
                bool lit = lamp.phase == (int)_phase
                    || (lamp.phase == (int)Phase.AllRed1 && (_phase == Phase.AllRed2 || _phase == Phase.WalkGreen || _phase == Phase.WalkFlash)) // car red during walk
                    || (lamp.phase == (int)Phase.AllRed2 && (_phase == Phase.CarGreen || _phase == Phase.CarYellow || _phase == Phase.AllRed1));  // walk red during cars
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
    }
}
