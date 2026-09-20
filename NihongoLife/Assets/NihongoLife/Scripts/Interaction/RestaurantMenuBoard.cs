using UnityEngine;
using NihongoLife.Data;
using NihongoLife.UI;

namespace NihongoLife.Interaction
{
    /// <summary>
    /// Interactable menu board. Opens RestaurantMenuUI for the menu asset found at
    /// <see cref="menuResourcePath"/> (under any Resources folder). Reuse it in any restaurant by
    /// changing only the path, so new restaurants and updated menus are data-only changes.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RestaurantMenuBoard : MonoBehaviour, IInteractable
    {
        [SerializeField] private string menuResourcePath = "Restaurants/menu_sushi_hibari";
        [SerializeField] private string promptJa = "メニューを見る";
        [SerializeField] private string promptEn = "Xem thực đơn";

        private RestaurantMenuDefinition _menu;

        public string GetPromptJa() => promptJa;
        public string GetpromptEn() => promptEn;
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            if (_menu == null)
            {
                _menu = Resources.Load<RestaurantMenuDefinition>(menuResourcePath);
            }

            if (_menu == null)
            {
                Debug.LogWarning($"[RestaurantMenuBoard] Menu asset not found at Resources/{menuResourcePath}.");
                return;
            }

            RestaurantMenuUI.GetOrCreate().Show(_menu);
        }
    }
}
