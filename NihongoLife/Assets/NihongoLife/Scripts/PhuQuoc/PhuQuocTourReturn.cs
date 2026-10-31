using NihongoLife.Core;
using NihongoLife.Interaction;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>The bus-stop post on an island-tour arrival pad: back to the Dương Đông tour terminal.</summary>
    public sealed class PhuQuocTourReturn : MonoBehaviour, IInteractable
    {
        public Transform destination;

        public string GetPromptJa() => "ズオンドンへ戻る";
        public string GetpromptEn() => Loc.T("Về bến xe Dương Đông", "Back to the Duong Dong bus stop", "ズオンドンのバス停へ戻る");
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => PhuQuocTravel.Teleport(player, destination);
    }
}
