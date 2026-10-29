using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Music
{
    /// <summary>A painting hung in the Hibiki gallery (55_MusicShop, built by MusicShopBuilder): standing in front of it,
    /// F opens its card (picture, artist, date, museum, licence).</summary>
    public sealed class ArtworkSpot : MonoBehaviour, IInteractable, IInteractionPriority
    {
        [SerializeField] private string artworkId;
        public string ArtworkId => artworkId;
        public void Configure(string id) => artworkId = id;
        public float InteractionPriority => 0.5f;
        public string GetPromptJa() => "え を みる";
        public string GetpromptEn()
        {
            var art = MusicCatalog.ArtworkById(artworkId);
            return art != null ? Loc.F("Xem tranh «{0}»", "Look at “{0}”", "「{0}」を見る", art.title) : Loc.T("Xem tranh", "Look at the painting", "絵を見る");
        }
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => MusicStudioUI.ShowPainting(artworkId);
    }
}
