using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>Island tour hops inside 65_PhuQuocIsland: moves the player (CharacterController off while moving) onto a
    /// destination point. Used by the tour terminal's tickets (PhuQuocTourTicket) and the way back (PhuQuocTourReturn).</summary>
    public static class PhuQuocTravel
    {
        public static int Trips { get; private set; }

        public static void Teleport(GameObject player, Transform to)
        {
            if (player == null || to == null) return;
            var body = player.GetComponentInParent<CharacterController>();
            var mover = body != null ? body.gameObject : player;
            bool was = body != null && body.enabled;
            if (body != null) body.enabled = false;
            mover.transform.SetPositionAndRotation(to.position + Vector3.up * 0.1f, Quaternion.Euler(0f, to.eulerAngles.y, 0f));
            Physics.SyncTransforms();
            if (body != null) body.enabled = was;
            Trips++;
        }
    }
}
