using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>Pure traffic rules (signal cycle, car-following) so EditMode tests can check them without a scene.</summary>
    public static class TrafficRules
    {
        public const int Green = 0, Yellow = 1, Red = 2;
        public const int AxisEastWest = 0, AxisNorthSouth = 1;
        // East-west green 12 s, yellow 3 s, all-red 1 s, then north-south green 8 s, yellow 3 s, all-red 1 s.
        public const float EwGreen = 12f, NsGreen = 8f, YellowTime = 3f, AllRed = 1f;
        public const float Cycle = EwGreen + YellowTime + AllRed + NsGreen + YellowTime + AllRed;
        public const float MinGap = 1.6f, Brake = 4.5f, Accel = 2.2f;

        public static int Signal(float time, int axis)
        {
            float t = Mathf.Repeat(time, Cycle);
            float nsStart = EwGreen + YellowTime + AllRed;
            if (axis == AxisEastWest)
                return t < EwGreen ? Green : t < EwGreen + YellowTime ? Yellow : Red;
            if (t < nsStart) return Red;
            float u = t - nsStart;
            return u < NsGreen ? Green : u < NsGreen + YellowTime ? Yellow : Red;
        }

        /// <summary>Highest speed that still lets a vehicle stop within <paramref name="gap"/> metres.</summary>
        public static float SafeSpeed(float maxSpeed, float gap)
        {
            if (gap <= MinGap) return 0f;
            return Mathf.Min(maxSpeed, Mathf.Sqrt(2f * Brake * (gap - MinGap)));
        }

        public static float Approach(float speed, float target, float dt)
            => target < speed ? Mathf.Max(target, speed - Brake * 2f * dt) : Mathf.Min(target, speed + Accel * dt);
    }

    /// <summary>
    /// Waypoint traffic on Trần Hưng Đạo and its two cross streets (PhuQuocLifeBuilder places the bodies).
    /// Every lane is a straight start→end segment; vehicles keep a safe gap to the one ahead, stop for red/yellow
    /// lights at the stop lines and loop back to the lane start (scaling in/out at the ends). Driving is on the right,
    /// as in Việt Nam: motorbikes in the kerb lane, cars and the bus in the inner lane. No colliders, no allocations per frame.
    /// </summary>
    public sealed class PhuQuocTraffic : MonoBehaviour
    {
        public static bool Disabled;

        [System.Serializable]
        public sealed class Lane
        {
            public Vector3 start, end;
            public int axis;
            public float[] stops = new float[0]; // distances along the lane of the stop lines
        }

        [SerializeField] private Lane[] lanes = new Lane[0];
        [SerializeField] private Transform[] bodies = new Transform[0];
        [SerializeField] private int[] laneOf = new int[0];
        [SerializeField] private float[] startAt = new float[0];
        [SerializeField] private float[] maxSpeed = new float[0];
        [SerializeField] private float[] length = new float[0];
        [SerializeField] private Renderer[] lamps = new Renderer[0];   // three per head: red, yellow, green
        [SerializeField] private int[] lampAxis = new int[0];          // one per head
        [SerializeField] private Material[] lampOn = new Material[3];
        [SerializeField] private Material[] lampOff = new Material[3];

        private float[] _s, _v;
        private int[] _lastSignal = { -1, -1 };

        public int VehicleCount => bodies.Length;
        public IReadOnlyList<Lane> Lanes => lanes;

        public void Configure(Lane[] l, Transform[] b, int[] lane, float[] s0, float[] vmax, float[] len,
            Renderer[] lampRenderers, int[] axisPerHead, Material[] on, Material[] off)
        {
            lanes = l; bodies = b; laneOf = lane; startAt = s0; maxSpeed = vmax; length = len;
            lamps = lampRenderers; lampAxis = axisPerHead; lampOn = on; lampOff = off;
        }

        private void Start()
        {
            _s = (float[])startAt.Clone();
            _v = new float[bodies.Length];
            for (int i = 0; i < bodies.Length; i++) { _v[i] = maxSpeed[i] * 0.6f; Place(i); }
        }

        private void Update()
        {
            if (Disabled || _s == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f), now = Time.time;
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] == null) continue;
                var lane = lanes[laneOf[i]];
                float len = Vector3.Distance(lane.start, lane.end);
                float front = _s[i] + length[i] * 0.5f;
                float gap = float.MaxValue;
                // Leader in the same lane (smallest positive distance ahead, wrapping round the loop).
                for (int j = 0; j < bodies.Length; j++)
                {
                    if (j == i || laneOf[j] != laneOf[i]) continue;
                    float d = _s[j] - _s[i];
                    if (d <= 0f) d += len;
                    float g = d - (length[i] + length[j]) * 0.5f;
                    if (g < gap) gap = g;
                }
                int sig = TrafficRules.Signal(now, lane.axis);
                if (sig != TrafficRules.Green)
                    foreach (float stop in lane.stops)
                    {
                        float d = stop - front;
                        // Yellow: only stop if there is room to; red: always stop before the line.
                        if (d > -0.2f && d < 40f && (sig == TrafficRules.Red || d > _v[i] * _v[i] / (2f * TrafficRules.Brake)))
                            gap = Mathf.Min(gap, d + TrafficRules.MinGap);
                    }
                _v[i] = TrafficRules.Approach(_v[i], TrafficRules.SafeSpeed(maxSpeed[i], gap), dt);
                _s[i] += _v[i] * dt;
                if (_s[i] > len) _s[i] -= len;
                Place(i);
            }
            UpdateLamps(now);
        }

        private void Place(int i)
        {
            var lane = lanes[laneOf[i]];
            float len = Mathf.Max(1f, Vector3.Distance(lane.start, lane.end));
            var t = bodies[i];
            t.localPosition = Vector3.Lerp(lane.start, lane.end, _s[i] / len);
            var fwd = lane.end - lane.start; fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.01f) t.localRotation = Quaternion.LookRotation(fwd);
            float edge = Mathf.Min(_s[i], len - _s[i]);
            t.localScale = Vector3.one * Mathf.Clamp01(edge / 3f);
        }

        private void UpdateLamps(float now)
        {
            for (int axis = 0; axis < 2; axis++)
            {
                int sig = TrafficRules.Signal(now, axis);
                if (sig == _lastSignal[axis]) continue;
                _lastSignal[axis] = sig;
                for (int h = 0; h < lampAxis.Length; h++)
                {
                    if (lampAxis[h] != axis) continue;
                    for (int k = 0; k < 3; k++)
                    {
                        var r = lamps[h * 3 + k];
                        if (r == null) continue;
                        // k: 0 red, 1 yellow, 2 green
                        bool on = (k == 0 && sig == TrafficRules.Red) || (k == 1 && sig == TrafficRules.Yellow) || (k == 2 && sig == TrafficRules.Green);
                        r.sharedMaterial = on ? lampOn[k] : lampOff[k];
                    }
                }
            }
        }
    }
}
