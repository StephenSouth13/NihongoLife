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
                return placeId switch { "station" => p.station, "farm" => p.farm, "shop" => p.shop, "barn" => p.barn, "view" => p.view, "pier" => p.pier ?? p.island, _ => p.island };
            }
        }
        public string GetPromptJa() => IslandLanguage.Primary(Word);
        public string GetpromptEn() => Loc.T("Học tên địa điểm", "Learn the place name", "場所の名前を覚える");
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            if (IslandState.Discover("place:" + placeId)) IslandUI.WordToast(Word, Loc.T("Địa điểm mới", "New place", "新しい場所"));
            else IslandUI.WordToast(Word, Loc.T("Địa điểm", "Place", "場所"));
            IslandAchievements.Check();
        }
    }
}
