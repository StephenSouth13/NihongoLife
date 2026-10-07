using UnityEngine;
using NihongoLife.NPC;
using NihongoLife.UI;

namespace NihongoLife.Shop
{
    /// <summary>Lets the clerk (Ito) ring up the basket when the scenario is not using her.</summary>
    public sealed class KonbiniClerk : MonoBehaviour, INpcService
    {
        public bool HandleInteract(GameObject player)
        {
            if (KonbiniBasket.IsEmpty) return false;
            KonbiniShopUI.GetOrCreate().OpenCheckout(GetComponent<NPCController>());
            return true;
        }
    }
}
