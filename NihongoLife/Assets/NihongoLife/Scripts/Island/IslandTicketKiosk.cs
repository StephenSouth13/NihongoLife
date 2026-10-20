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
    /// <summary>Ticket kiosk at Midori station.</summary>
    public sealed class IslandTicketKiosk : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? $"Ticket to Hibari ¥{IslandTrainService.Price}" : $"ひばりゆき きっぷ ¥{IslandTrainService.Price}";
        public string GetpromptEn() => IslandTrainService.HasTicket ? Loc.T("Đã có vé — ra cửa tàu", "You have a ticket — go to the train door", "切符があります — 電車のドアへ") : Loc.F("Mua vé về Hibari (¥{0})", "Buy a ticket to Hibari (¥{0})", "ひばり行きの切符を買う（¥{0}）", IslandTrainService.Price);
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            string error = IslandTrainService.BuyTicket();
            if (error == null) IslandUI.Toast(Loc.F("Đã mua vé về Hibari (−¥{0}). Ra cửa tàu và nhấn F để lên tàu.", "Ticket to Hibari bought (−¥{0}). Go to the train door and press F.", "ひばり行きの切符を買いました（−¥{0}）。電車のドアでFを押しましょう。", IslandTrainService.Price));
            else IslandUI.Toast(error, !IslandTrainService.HasTicket);
        }
    }
}
