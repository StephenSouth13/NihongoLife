using UnityEngine;

namespace NihongoLife.Player
{
    /// <summary>
    /// Receiver for the events baked into the Kids pack clips (the run cycle calls FootstepEffect on each foot).
    /// Footstep sounds already come from PlayerController's own step timer, so the event only needs a receiver —
    /// without one Unity logs an error every step.
    /// </summary>
    public sealed class KidAnimationEvents : MonoBehaviour
    {
        public int Footsteps { get; private set; }
        public void FootstepEffect() => Footsteps++;
    }
}
