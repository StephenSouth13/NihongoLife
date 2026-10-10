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
        public string GetpromptEn() => IslandTrainService.HasTicket ? "Đã có vé — ra cửa tàu" : $"Mua vé về Hibari (¥{IslandTrainService.Price})";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            string error = IslandTrainService.BuyTicket();
            if (error == null) IslandUI.Toast($"Đã mua vé về Hibari (−¥{IslandTrainService.Price}). Ra cửa tàu và nhấn F để lên tàu.");
            else IslandUI.Toast(error, !error.StartsWith("Bạn không còn tiền"));
        }
    }
}
