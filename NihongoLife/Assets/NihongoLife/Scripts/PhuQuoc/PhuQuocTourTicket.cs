using NihongoLife.Core;
using NihongoLife.Interaction;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>One signpost at the Dương Đông island-tour terminal: a ride to one of the 31 real places on the map
    /// (phu_quoc_map.json). Places inside the 1:1 town lead to their spot there; the others to an arrival pad at the
    /// place's marker with a way back (PhuQuocTourReturn).</summary>
    public sealed class PhuQuocTourTicket : MonoBehaviour, IInteractable
    {
        public string placeId, nameVi, nameEn, nameJa;
        public Transform destination;

        public string GetPromptJa() => "バスで" + nameJa + "へ";
        public string GetpromptEn() => Loc.T("Đi xe buýt tới", "Take the bus to", "バスで行く") + ": " + Loc.Pick(nameVi, nameEn, nameJa);
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => PhuQuocTravel.Teleport(player, destination);
    }
}
