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
        public string GetpromptEn() => IslandTrainService.HasTicket ? "Soát vé và lên tàu về Hibari" : "Cần vé — mua ở máy bán vé";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            string error = IslandTrainService.Depart(this);
            if (error != null) IslandUI.Toast(error, true);
        }
    }
}
