using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Places a seated humanoid on a carriage bench. The Kids sit clip stores the hip drop in the
    /// animation root (RootT), and the rig plays with applyRootMotion off, so the body stays at standing
    /// height and leans through the wall. This solves a hip target in the seat root's local space
    /// (forward is into the aisle, -Z is the wall) and keeps the furthest bone off the wall.
    /// Pure math so edit-mode tests can lock the clearance.
    /// </summary>
    public static class SeatAnchor
    {
        /// <summary>Commuter root is 0.42 m in front of the carriage wall. Bones stay this far inside.</summary>
        public const float BodyRearLimit = -0.30f;
        public const float HipRearLimit = -0.22f;
        public const float HipFrontLimit = 0.30f;
        public const float HipLateralLimit = 0.20f;
        public const float HipMinY = 0.42f;
        public const float HipMaxY = 0.78f;

        /// <summary>Seated talk trigger: the torso on the cushion, not a standing capsule into the aisle.</summary>
        public static readonly Vector3 TalkCenter = new Vector3(0f, 0.62f, 0.28f);
        public static readonly Vector3 TalkSize = new Vector3(0.7f, 0.9f, 0.9f);

        /// <summary>Cushion-relative hip height. <paramref name="lossyScaleY"/> is the visual rig scale (Kids are 1.35).</summary>
        public static Vector3 HipTarget(float lossyScaleY, string rigName)
        {
            float scale = lossyScaleY <= 0.01f ? 1f : Mathf.Clamp(lossyScaleY, 0.7f, 1.8f);
            float y = 0.46f + 0.08f * Mathf.Min(scale, 1.35f);
            var bias = RigBias(rigName);
            return ClampHip(new Vector3(bias.x, y + bias.y, -0.04f + bias.z));
        }

        /// <summary>Per-rig nudge so a shorter kid and a taller clerk don't share one sit height.</summary>
        public static Vector3 RigBias(string rigName)
        {
            if (string.IsNullOrEmpty(rigName)) return Vector3.zero;
            string n = rigName.ToLowerInvariant();
            if (n.Contains("guide") || n.Contains("grandma") || n.Contains("elder") || n.Contains("sota")) return new Vector3(0f, -0.03f, -0.02f);
            if (n.Contains("kid") || n.Contains("neighbor") || n.Contains("sakura") || n.Contains("haruto")) return new Vector3(0f, -0.02f, 0.02f);
            if (n.Contains("cashier") || n.Contains("worker")) return new Vector3(0f, 0.02f, 0f);
            return Vector3.zero;
        }

        public static Vector3 ClampHip(Vector3 hip)
        {
            hip.x = Mathf.Clamp(hip.x, -HipLateralLimit, HipLateralLimit);
            hip.y = Mathf.Clamp(hip.y, HipMinY, HipMaxY);
            hip.z = Mathf.Clamp(hip.z, HipRearLimit, HipFrontLimit);
            return hip;
        }

        public static Vector3 Correction(Vector3 hipLocal, Vector3 target) => target - hipLocal;

        /// <summary>
        /// Hip Z that lands on <paramref name="desiredZ"/> unless that would leave a bone behind
        /// <see cref="BodyRearLimit"/>. <paramref name="hipZ"/> and <paramref name="rearZ"/> are the
        /// current pose in seat-root space.
        /// </summary>
        public static float SolvedHipZ(float hipZ, float rearZ, float desiredZ)
        {
            float predicted = rearZ + (desiredZ - hipZ);
            float z = desiredZ;
            if (predicted < BodyRearLimit) z += BodyRearLimit - predicted;
            return Mathf.Clamp(z, HipRearLimit, HipFrontLimit);
        }

        /// <summary>The seated trigger's back face stays off the wall (wall is at about -0.42).</summary>
        public static bool TalkVolumeClearsWall(float wallLocalZ = -0.42f)
        {
            float back = TalkCenter.z - TalkSize.z * 0.5f;
            return back > wallLocalZ + 0.08f;
        }
    }
}
