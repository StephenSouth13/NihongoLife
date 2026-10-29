using UnityEngine;

namespace NihongoLife.Gestures
{
    /// <summary>Analytic two-bone IK (arm or leg): moves the end bone to the target (clamped to what the limb can reach) and
    /// bends the middle joint toward the pole direction. Only rotations are written, so it works on any humanoid rig.</summary>
    public static class TwoBoneIk
    {
        /// <summary>Returns the distance left between the end bone and <paramref name="target"/> after the solve (0 = reached).</summary>
        public static float Solve(Transform upper, Transform mid, Transform end, Vector3 target, Vector3 pole)
        {
            Vector3 a = upper.position, b = mid.position, c = end.position;
            float l1 = (b - a).magnitude, l2 = (c - b).magnitude;
            Vector3 toTarget = target - a;
            float d = toTarget.magnitude;
            if (l1 < 1e-4f || l2 < 1e-4f || d < 1e-5f) return 0f;
            float dMax = (l1 + l2) * 0.999f, dMin = Mathf.Abs(l1 - l2) + 0.02f * (l1 + l2);
            float dd = Mathf.Clamp(d, dMin, dMax);
            Vector3 u = toTarget / d;
            float x = (l1 * l1 - l2 * l2 + dd * dd) / (2f * dd);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - x * x));
            Vector3 p = pole - Vector3.Dot(pole, u) * u;
            if (p.sqrMagnitude < 1e-6f) p = Vector3.Cross(u, Vector3.right);
            if (p.sqrMagnitude < 1e-6f) p = Vector3.Cross(u, Vector3.forward);
            p.Normalize();
            Vector3 bNew = a + u * x + p * h;
            Vector3 cNew = a + u * dd;

            var q1 = Quaternion.FromToRotation(b - a, bNew - a);
            upper.rotation = q1 * upper.rotation;
            Vector3 c1 = a + q1 * (c - a);
            var q2 = Quaternion.FromToRotation(c1 - bNew, cNew - bNew);
            mid.rotation = q2 * mid.rotation;
            return Mathf.Max(0f, d - dd);
        }
    }
}