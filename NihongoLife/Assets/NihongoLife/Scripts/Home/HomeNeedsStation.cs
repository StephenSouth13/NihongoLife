using UnityEngine;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;

namespace NihongoLife.Home
{
    /// <summary>
    /// Kitchenette points in the room. Sink: drink tap water (thirst). Fridge: eat or drink something the
    /// player bought (konbini onigiri, tea…) from the inventory — an empty bag tells the player to shop,
    /// which links the room back to the konbini scenario instead of handing out free food.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class HomeNeedsStation : MonoBehaviour, IInteractable
    {
        public enum Kind { Sink, Fridge }

        [SerializeField] private Kind kind = Kind.Sink;
        [SerializeField] private float waterThirstRestore = 35f;

        public Kind StationKind => kind;
        public string GetPromptJa() => kind == Kind.Sink ? "みずを のむ" : "れいぞうこを あける";
        public string GetpromptEn() => kind == Kind.Sink ? "Uống nước" : "Mở tủ lạnh (ăn đồ đã mua)";
        public Transform GetTransform() => transform;

        public void Configure(Kind stationKind) => kind = stationKind;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        public void Interact(GameObject player)
        {
            var room = HomeBedroomRuntime.Instance;
            var status = PlayerStatus.Instance;
            if (kind == Kind.Sink)
            {
                if (status == null) return;
                if (status.Thirst >= 98f)
                {
                    room?.ShowToast("のどが かわいて いません", "Bạn chưa khát.");
                    return;
                }
                status.RestoreNeeds(0f, waterThirstRestore, 0f);
                Play(GameAudioCue.Pickup);
                room?.ShowToast("みずを のみました", $"Đã uống nước · Nước +{waterThirstRestore:0}");
                return;
            }

            Play(GameAudioCue.DoorOpen);
            var inventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : FindFirstObjectByType<PlayerInventory>();
            InventoryEntry food = null;
            if (inventory != null)
                foreach (var item in inventory.Items)
                    if (item.useType != ItemUseType.None) { food = item; break; }

            if (food == null || status == null)
            {
                room?.ShowToast("れいぞうこは からっぽです", "Tủ lạnh trống — hãy mua onigiri hoặc đồ uống ở konbini.");
                return;
            }

            string name = food.displayNameJa;
            inventory.RemoveItem(food.itemId);
            status.RestoreNeeds(food.foodRestore, food.drinkRestore, food.energyRestore);
            room?.ShowToast(food.useType == ItemUseType.Drink ? "いただきます" : "いただきます！",
                $"Đã dùng {name} · No +{food.foodRestore:0} · Nước +{food.drinkRestore:0}");
        }

        private static void Play(GameAudioCue cue)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, 0.7f);
        }
    }
}
