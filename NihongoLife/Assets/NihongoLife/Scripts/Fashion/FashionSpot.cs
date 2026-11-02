using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Fashion
{
    /// <summary>A clothes rail or the mirror at Hibari Mode (56_FashionStore): F opens the shop on that rail's category,
    /// the mirror opens the player's own wardrobe.</summary>
    public sealed class FashionSpot : MonoBehaviour, IInteractable, IInteractionPriority
    {
        [SerializeField] private string category;

        /// <summary>Empty = the mirror.</summary>
        public string Category => category;

        public void Configure(string categoryId) => category = categoryId;

        public float InteractionPriority => 0.5f;
        public string GetPromptJa() => string.IsNullOrEmpty(category) ? "かがみ" : "服を 見る";

        public string GetpromptEn()
        {
            if (string.IsNullOrEmpty(category)) return Loc.T("Soi gương, chọn đồ của bạn", "Mirror: your wardrobe", "かがみ：自分の服");
            foreach (var c in FashionCatalog.Load().categories)
                if (c.id == category) return Loc.F("Xem đồ: {0}", "Browse: {0}", "服を見る：{0}", c.Name);
            return Loc.T("Xem quần áo", "Browse the clothes", "服を見る");
        }

        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => FashionShopUI.Open(category);
    }
}
