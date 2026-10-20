using System.Collections;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.World;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>Door of the waiting train: validates the ticket and starts the ride.</summary>
    public sealed class IslandTrainDoor : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? "Board for Hibari" : "ひばりゆき に のる";
        public string GetpromptEn() => IslandTrainService.HasTicket ? NihongoLife.Core.Loc.T("Soát vé và lên tàu về Hibari", "Show your ticket and board for Hibari", "きっぷを見せて、ひばり行きに乗る") : NihongoLife.Core.Loc.T("Cần vé — mua ở máy bán vé", "You need a ticket — buy one at the machine", "きっぷが必要です — 券売機で買いましょう");
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            if (IslandVisit.Active) { IslandVisit.End(); return; } // visitors go back where they came from
            string error = IslandTrainService.Depart(this);
            if (error != null) IslandUI.Toast(error, true);
        }
    }
}
