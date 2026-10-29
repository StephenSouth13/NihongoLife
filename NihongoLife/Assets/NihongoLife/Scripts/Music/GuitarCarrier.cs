using NihongoLife.Core;
using NihongoLife.Gestures;
using NihongoLife.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Music
{
    /// <summary>
    /// The player's own guitar follows them: worn on the back (CarryHold "carry_guitar") and kept in the bag as an
    /// instrument (use it to busk). Ownership is the story flag <see cref="MusicShop.GuitarFlag"/>; the bag is not saved,
    /// so the item is put back after a load. Attached to the player when a scene loads; checks once a second.
    /// </summary>
    public sealed class GuitarCarrier : MonoBehaviour
    {
        private float _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Attach();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

        private static void Attach()
        {
            var player = SceneRefs.Find<PlayerController>();
            if (player != null && player.GetComponent<GuitarCarrier>() == null) player.gameObject.AddComponent<GuitarCarrier>();
        }

        /// <summary>Applies the ownership now (after buying, in tests).</summary>
        public static void Sync()
        {
            var player = SceneRefs.Find<PlayerController>();
            if (player == null) return;
            var carrier = player.GetComponent<GuitarCarrier>();
            if (carrier == null) carrier = player.gameObject.AddComponent<GuitarCarrier>();
            carrier.Apply();
        }

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 1f;
            Apply();
        }

        private void Apply()
        {
            bool owns = MusicShop.OwnsGuitar;
            var hold = owns ? CarryHold.On(gameObject) : GetComponent<CarryHold>();
            if (hold != null && (hold.Back == "carry_guitar") != owns) hold.SetBack(owns ? "carry_guitar" : null);
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return;
            bool inBag = inventory.HasItem(MusicShop.GuitarItem);
            if (owns && !inBag && !inventory.IsFull)
                inventory.AddItem(MusicShop.GuitarItem, "ギター", Loc.Triple("Đàn guitar của bạn", "Your guitar", "ギター"), MusicShop.GuitarPrice, 1, false, ItemUseType.Instrument);
            else if (!owns && inBag) inventory.RemoveItem(MusicShop.GuitarItem, inventory.GetItemQuantity(MusicShop.GuitarItem));
        }
    }
}
