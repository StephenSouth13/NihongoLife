using System.Collections;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.World;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>
    /// Lives on the root of 60_MidoriIsland. On arrival: gives the one-time starter kit, shows the welcome banner and
    /// the island toolbar, and makes the island's own sun the only directional light (the host city's sun is switched
    /// off while the island is loaded and restored when it unloads). P opens the Midori store. A fall guard returns the
    /// player to the station if they ever leave the walkable island.
    /// </summary>
    public sealed class IslandRuntime : MonoBehaviour
    {
        [SerializeField] private Transform respawn;
        [SerializeField] private float fallY = -4f;

        private readonly List<Light> _foreignSuns = new();
        private PlayerController _player;
        public static IslandRuntime Active { get; private set; }

        public void Configure(Transform respawnPoint) => respawn = respawnPoint;

        private void OnEnable()
        {
            Active = this;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional || light.gameObject.scene == gameObject.scene || !light.enabled) continue;
                light.enabled = false;
                _foreignSuns.Add(light);
            }
        }

        private void OnDisable()
        {
            foreach (var light in _foreignSuns) if (light != null) light.enabled = true;
            _foreignSuns.Clear();
            if (Active == this) Active = null;
            IslandUI.DestroyAll();
        }

        private IEnumerator Start()
        {
            // Wait for the scene transition fade to finish so the banner is not hidden behind it.
            yield return new WaitForSecondsRealtime(0.6f);
            var record = IslandState.Record;
            bool firstVisit = !record.welcomed;
            if (IslandEconomy.GiveStarterKit())
                IslandUI.Toast("Quà chào mừng: cuốc, bình tưới và 3 túi hạt cà rốt đã vào balo.");
            record.welcomed = true;
            IslandState.Discover("place:island");
            IslandUI.ShowToolbar();
            IslandUI.ShowWelcome();
            if (!firstVisit) IslandUI.Toast("Cây trồng vẫn lớn trong lúc bạn đi vắng — ghé ruộng xem nhé.");
            IslandAchievements.Unlocked += OnAchievement;
        }

        private void OnDestroy() => IslandAchievements.Unlocked -= OnAchievement;

        private static void OnAchievement(IslandAchievements.Def def) => IslandUI.Toast($"Thành tích mới: {def.Ja} — {def.Vi}");

        private void Update()
        {
            var input = GameInputService.Instance;
            if (input != null && input.WasPressed(GameInputId.Shop) && !UiModalStack.IsTyping)
            {
                if (IslandUI.ShopOpen) IslandUI.CloseShop();
                else if (!UiModalStack.AnyOpen) IslandUI.OpenShop();
            }

            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            var player = _player;
            if (player != null && player.transform.position.y < fallY && respawn != null && player.gameObject.scene.IsValid())
            {
                var controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                player.transform.SetPositionAndRotation(respawn.position, respawn.rotation);
                if (controller != null) controller.enabled = true;
                IslandUI.Toast("Bạn đã ra khỏi đảo — đưa về ga Midori.", true);
            }
        }
    }

    /// <summary>
    /// Return trip from Midori Island: buy the ticket to Hibari at the kiosk (paid from the one shared wallet), validate it
    /// at the train door, ride (travel screen), and arrive at Hibari Station. A player who is broke and has nothing to sell
    /// receives a one-off courtesy ticket, so the island can never become a dead end.
    /// </summary>
    public static class IslandTrainService
    {
        public const string TicketItemId = "train_ticket_hibari";
        public static float RideSeconds = 4.5f;
        private static bool _departing;

        public static bool Departing => _departing;
        public static int Price => Mathf.Max(0, IslandCatalog.Load().ticketPrice);
        public static bool HasTicket => PlayerInventory.Instance != null && PlayerInventory.Instance.HasItem(TicketItemId);

        public static string BuyTicket()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return "Không tìm thấy balo.";
            if (HasTicket) return "Bạn đã có vé về Hibari — ra cửa tàu và nhấn F.";
            if (!IslandEconomy.CanStore(TicketItemId)) return "Balo đầy — bán bớt nông sản ở cửa hàng rồi mua vé.";
            if (inventory.Yen < Price)
            {
                bool canSell = false;
                foreach (var crop in IslandCatalog.Load().crops) if (IslandEconomy.Owned(crop.ProduceItemId) > 0) canSell = true;
                if (canSell) return $"Không đủ tiền (vé ¥{Price}, bạn có ¥{inventory.Yen}). Bán nông sản ở cửa hàng Midori trước nhé.";
                inventory.AddItem(TicketItemId, "きっぷ（ひばり）", "Vé tàu Midori → Hibari (vé hỗ trợ)", 0, 1);
                return "Bạn không còn tiền và không có gì để bán — nhân viên ga tặng một vé hỗ trợ về Hibari.";
            }
            if (!inventory.SpendYen(Price)) return $"Không đủ tiền (vé ¥{Price}).";
            if (!inventory.AddItem(TicketItemId, "きっぷ（ひばり）", "Vé tàu Midori → Hibari", Price, 1))
            {
                inventory.AddYen(Price);
                return "Balo đầy, không nhận được vé.";
            }
            return null;
        }

        /// <summary>Validates the ticket and rides back to Hibari; returns an error message or null when the trip starts.</summary>
        public static string Depart(MonoBehaviour host)
        {
            if (_departing) return "Tàu đang khởi hành.";
            if (!HasTicket) return "Cần vé về Hibari — mua ở máy bán vé cạnh sân ga.";
            SceneFlowController flow = null;
            if (!GameServices.TryGet(out flow)) flow = Object.FindFirstObjectByType<SceneFlowController>();
            if (flow == null) return "Không tìm thấy hệ thống chuyển cảnh.";
            _departing = true;
            host.StartCoroutine(Ride(flow));
            return null;
        }

        private static IEnumerator Ride(SceneFlowController flow)
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) player.InputLocked = true;
            IslandUI.CloseFarm(); IslandUI.CloseShop(); IslandUI.CloseAnimal(); IslandUI.CloseProgress();
            yield return IslandUI.TravelScreen("みどりじま · Đảo Xanh", "ひばり · Ga Hibari", RideSeconds);
            PlayerInventory.Instance?.RemoveItem(TicketItemId);
            IslandState.Save();
            flow.TransferZone(WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, "ひばりえき / Ga Hibari", () =>
            {
                _departing = false;
                var p = Object.FindFirstObjectByType<PlayerController>();
                if (p != null) p.InputLocked = false;
                StationTravelController.AnnounceArrival("hibari");
            });
            // If the transfer could not start, release the player instead of leaving them on the travel screen.
            yield return null;
            if (!flow.IsLoading)
            {
                _departing = false;
                IslandUI.EndTravelScreen();
                if (player != null) player.InputLocked = false;
            }
        }
    }

    /// <summary>Ticket kiosk at Midori station.</summary>
    public sealed class IslandTicketKiosk : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? $"Ticket to Hibari ¥{IslandTrainService.Price}" : $"ひばりゆき きっぷ ¥{IslandTrainService.Price}";
        public string GetpromptEn() => IslandTrainService.HasTicket ? "Đã có vé — ra cửa tàu" : $"Mua vé về Hibari (¥{IslandTrainService.Price})";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            string error = IslandTrainService.BuyTicket();
            if (error == null) IslandUI.Toast($"Đã mua vé về Hibari (−¥{IslandTrainService.Price}). Ra cửa tàu và nhấn F để lên tàu.");
            else IslandUI.Toast(error, !error.StartsWith("Bạn không còn tiền"));
        }
    }

    /// <summary>Door of the waiting train: validates the ticket and starts the ride.</summary>
    public sealed class IslandTrainDoor : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? "Board for Hibari" : "ひばりゆき に のる";
        public string GetpromptEn() => IslandTrainService.HasTicket ? "Soát vé và lên tàu về Hibari" : "Cần vé — mua ở máy bán vé";
        public Transform GetTransform() => transform;
        public void Interact(GameObject player)
        {
            string error = IslandTrainService.Depart(this);
            if (error != null) IslandUI.Toast(error, true);
        }
    }
}
