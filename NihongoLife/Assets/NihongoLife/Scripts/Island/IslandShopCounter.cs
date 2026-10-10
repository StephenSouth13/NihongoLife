using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>The Midori Store counter (shop with Agriculture / Fashion / Technology / Sell).</summary>
    public sealed class IslandShopCounter : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? "Midori Store" : "みどりしょうてん";
        public string GetpromptEn() => "Mở cửa hàng Midori (P)";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => IslandUI.OpenShop();
    }
}
