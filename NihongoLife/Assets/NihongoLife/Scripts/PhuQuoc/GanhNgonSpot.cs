using NihongoLife.Core;
using NihongoLife.Interaction;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>A service station in Gánh Ngon (tables, reception, kitchen pass, pub bar, the shift manager).</summary>
    public sealed class GanhNgonSpot : MonoBehaviour, IInteractable
    {
        public GanhNgonStation station;
        public string GetPromptJa() => station switch
        {
            GanhNgonStation.Reception => "うけつけ",
            GanhNgonStation.Kitchen => "ちゅうぼう",
            GanhNgonStation.Bar => "バー",
            GanhNgonStation.Manager => "マイさん",
            _ => "テーブル",
        };
        public string GetpromptEn() => station switch
        {
            GanhNgonStation.Reception => Loc.T("Lễ tân", "Reception", "受付"),
            GanhNgonStation.Kitchen => Loc.T("Bếp", "Kitchen pass", "厨房"),
            GanhNgonStation.Bar => Loc.T("Quầy pub", "Pub bar", "バー"),
            GanhNgonStation.Manager => Loc.T("Chị Mai (quản lý ca)", "Mai (shift manager)", "マイさん"),
            _ => Loc.T("Bàn khách", "Guest table", "テーブル"),
        };
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => GanhNgonShift.Use(station);
    }
}
