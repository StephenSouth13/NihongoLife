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
    /// <summary>A sign or landmark that teaches its name once (station, field, barn, lookout…).</summary>
    public sealed class IslandWordSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] private string placeId = "farm";
        public void Configure(string id) => placeId = id;
        private IslandWord Word
        {
            get
            {
                var p = IslandCatalog.Load().places;
                return placeId switch { "station" => p.station, "farm" => p.farm, "shop" => p.shop, "barn" => p.barn, "view" => p.view, _ => p.island };
            }
        }
        public string GetPromptJa() => IslandLanguage.Primary(Word);
        public string GetpromptEn() => "Học tên địa điểm";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            if (IslandState.Discover("place:" + placeId)) IslandUI.WordToast(Word, "Địa điểm mới");
            else IslandUI.WordToast(Word, "Địa điểm");
            IslandAchievements.Check();
        }
    }
}
