using NihongoLife.NPC;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>Kimura at the ticket counter: asks where the player is going and sells the ticket.
    /// Runs before the station scenario, which only receives objective progress.</summary>
    public sealed class StationStaffService : MonoBehaviour, IPriorityNpcService
    {
        public bool HandleInteract(GameObject player)
        {
            var station = FindFirstObjectByType<StationTravelController>();
            if (station == null) return false;
            station.Execute(StationAction.TalkStationStaff, player);
            return true;
        }
    }
}
