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
        public string GetpromptEn() => kind == Kind.Sink ? NihongoLife.Core.Loc.T("Uống nước", "Drink water", "みずを のむ") : NihongoLife.Core.Loc.T("Mở tủ lạnh (ăn đồ đã mua)", "Open the fridge (eat what you bought)", "れいぞうこを あける");
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
                    room?.ShowToast("のどが かわいて いません", NihongoLife.Core.Loc.Triple("Bạn chưa khát.", "You're not thirsty.", ""));
                    return;
                }
                status.RestoreNeeds(0f, waterThirstRestore, 0f);
                Play(GameAudioCue.Pickup);
                room?.ShowToast("みずを のみました", NihongoLife.Core.Loc.TripleF("Đã uống nước · Nước +{0:0}", "Drank water · Thirst +{0:0}", "水分 +{0:0}", waterThirstRestore));
                return;
            }

            Play(GameAudioCue.DoorOpen);
            var inventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : SceneRefs.Find<PlayerInventory>();
            InventoryEntry food = null;
            if (inventory != null)
                foreach (var item in inventory.Items)
                    if (item.useType == ItemUseType.Food || item.useType == ItemUseType.Drink) { food = item; break; } // not medicine

            if (food == null || status == null)
            {
                room?.ShowToast("れいぞうこは からっぽです", NihongoLife.Core.Loc.Triple("Tủ lạnh trống — hãy mua onigiri hoặc đồ uống ở konbini.", "The fridge is empty — buy onigiri or drinks at the konbini.", "コンビニでおにぎりや飲み物を買いましょう。"));
                return;
            }

            string name = string.IsNullOrEmpty(food.displayNameJa) ? food.Name : food.displayNameJa; // the Japanese word is the one to learn
            inventory.RemoveItem(food.itemId);
            status.RestoreNeeds(food.foodRestore, food.drinkRestore, food.energyRestore);
            room?.ShowToast(food.useType == ItemUseType.Drink ? "いただきます" : "いただきます！",
                NihongoLife.Core.Loc.TripleF("Đã dùng {0} · No +{1:0} · Nước +{2:0}", "Had {0} · Food +{1:0} · Drink +{2:0}", "{0} · 満腹 +{1:0} · 水分 +{2:0}", name, food.foodRestore, food.drinkRestore));
        }

        private static void Play(GameAudioCue cue)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, 0.7f);
        }
    }
}
