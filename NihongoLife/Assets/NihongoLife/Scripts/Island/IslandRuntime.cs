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
            if (IslandVisit.Active)
            {
                // A friend's island: no starter kit, no toolbar — the visit bar (IslandDecorRuntime) takes over.
                IslandUI.Toast(NihongoLife.Core.Loc.F("Bạn đang thăm đảo của {0} — chỉ xem, không dùng ruộng hay cửa hàng. Thả tim nếu bạn thích!", "You are visiting {0}'s island — look around; the fields and store are theirs. Leave a heart if you like it!",
                    "{0}さんの島に来ています — 見るだけです。気に入ったらハートを！", IslandVisit.Current.display_name));
                IslandAchievements.Unlocked += OnAchievement;
                yield break;
            }
            var record = IslandState.Record;
            bool firstVisit = !record.welcomed;
            if (IslandEconomy.GiveStarterKit())
                IslandUI.Toast(NihongoLife.Core.Loc.T("Quà chào mừng: cuốc, bình tưới và 3 túi hạt cà rốt đã vào balo.", "Welcome gift: a hoe, a watering can and 3 bags of carrot seeds are in your bag.", "ようこそのプレゼント：くわ、じょうろ、にんじんの種3袋をかばんに入れました。"));
            record.welcomed = true;
            IslandState.Discover("place:island");
            IslandUI.ShowToolbar();
            IslandUI.ShowWelcome();
            if (!firstVisit) IslandUI.Toast(NihongoLife.Core.Loc.T("Cây trồng vẫn lớn trong lúc bạn đi vắng — ghé ruộng xem nhé.", "Your crops kept growing while you were away — go and check the fields.", "留守の間も作物は育っています。畑を見に行きましょう。"));
            IslandAchievements.Unlocked += OnAchievement;
        }

        private void OnDestroy() => IslandAchievements.Unlocked -= OnAchievement;

        private static void OnAchievement(IslandAchievements.Def def) => IslandUI.Toast(NihongoLife.Core.Loc.T("Thành tích mới: ", "New achievement: ", "新しい実績：") + def.Title);

        private void Update()
        {
            var input = GameInputService.Instance;
            if (input != null && input.WasPressed(GameInputId.Shop) && !UiModalStack.IsTyping && !IslandVisit.Active)
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
                IslandUI.Toast(NihongoLife.Core.Loc.T("Bạn đã ra khỏi đảo — đưa về ga Midori.", "You left the island — brought back to Midori Station.", "島の外に出たので、みどり駅に戻りました。"), true);
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
            if (inventory == null) return NihongoLife.Core.Loc.T("Không tìm thấy balo.", "Bag not found.", "かばんが見つかりません。");
            if (HasTicket) return NihongoLife.Core.Loc.T("Bạn đã có vé về Hibari — ra cửa tàu và nhấn F.", "You already have a ticket to Hibari — go to the train door and press F.", "ひばりへのきっぷはもうあります。電車のドアでFを押してください。");
            if (!IslandEconomy.CanStore(TicketItemId)) return NihongoLife.Core.Loc.T("Balo đầy — bán bớt nông sản ở cửa hàng rồi mua vé.", "Your bag is full — sell some produce at the store, then buy a ticket.", "かばんがいっぱいです。お店で作物を売ってからきっぷを買いましょう。");
            if (inventory.Yen < Price)
            {
                bool canSell = false;
                foreach (var crop in IslandCatalog.Load().crops) if (IslandEconomy.Owned(crop.ProduceItemId) > 0) canSell = true;
                if (canSell) return NihongoLife.Core.Loc.F("Không đủ tiền (vé {0}, bạn có {1}). Bán nông sản ở cửa hàng Midori trước nhé.", "Not enough money (ticket {0}, you have {1}). Sell produce at the Midori Store first.", "お金が足りません（きっぷ{0}、所持金{1}）。先にみどりしょうてんで作物を売りましょう。", NihongoLife.Core.Loc.Yen(Price), NihongoLife.Core.Loc.Yen(inventory.Yen));
                inventory.AddItem(TicketItemId, "きっぷ（ひばり）", NihongoLife.Core.Loc.Triple("Vé tàu Midori → Hibari (vé hỗ trợ)", "Train ticket Midori → Hibari (help ticket)", "きっぷ（みどりじま → ひばり・サポート）"), 0, 1);
                return NihongoLife.Core.Loc.T("Bạn không còn tiền và không có gì để bán — nhân viên ga tặng một vé hỗ trợ về Hibari.", "You have no money and nothing to sell — the station staff give you a help ticket to Hibari.", "お金も売る物もないので、駅員さんがひばりまでのサポートきっぷをくれました。");
            }
            if (!inventory.SpendYen(Price, NihongoLife.Core.Loc.Triple("Vé tàu", "Train ticket", "きっぷ"))) return NihongoLife.Core.Loc.F("Không đủ tiền (vé {0}).", "Not enough money (ticket {0}).", "お金が足りません（きっぷ{0}）。", NihongoLife.Core.Loc.Yen(Price));
            if (!inventory.AddItem(TicketItemId, "きっぷ（ひばり）", NihongoLife.Core.Loc.Triple("Vé tàu Midori → Hibari", "Train ticket Midori → Hibari", "きっぷ（みどりじま → ひばり）"), Price, 1))
            {
                inventory.AddYen(Price, NihongoLife.Core.Loc.Triple("Hoàn vé tàu", "Ticket refund", "きっぷの払い戻し"));
                return NihongoLife.Core.Loc.T("Balo đầy, không nhận được vé.", "Your bag is full, so you can't take the ticket.", "かばんがいっぱいで、きっぷを受け取れません。");
            }
            return null;
        }

        /// <summary>Validates the ticket and rides back to Hibari; returns an error message or null when the trip starts.</summary>
        public static string Depart(MonoBehaviour host)
        {
            if (_departing) return NihongoLife.Core.Loc.T("Tàu đang khởi hành.", "The train is departing.", "電車が出発します。");
            if (!HasTicket) return NihongoLife.Core.Loc.T("Cần vé về Hibari — mua ở máy bán vé cạnh sân ga.", "You need a ticket to Hibari — buy one at the machine by the platform.", "ひばりへのきっぷが必要です。ホームの横の券売機で買いましょう。");
            SceneFlowController flow = null;
            if (!GameServices.TryGet(out flow)) flow = Object.FindFirstObjectByType<SceneFlowController>();
            if (flow == null) return NihongoLife.Core.Loc.T("Không tìm thấy hệ thống chuyển cảnh.", "Scene transition system not found.", "シーン切り替えが見つかりません。");
            _departing = true;
            host.StartCoroutine(Ride(flow));
            return null;
        }

        private static IEnumerator Ride(SceneFlowController flow)
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) player.InputLocked = true;
            IslandUI.CloseFarm(); IslandUI.CloseShop(); IslandUI.CloseAnimal(); IslandUI.CloseProgress();
            yield return IslandUI.TravelScreen(NihongoLife.Core.Loc.T("Đảo Xanh", "Midori Island", "みどりじま"), NihongoLife.Core.Loc.T("Ga Hibari", "Hibari Station", "ひばりえき"), RideSeconds);
            PlayerInventory.Instance?.RemoveItem(TicketItemId);
            IslandState.Save();
            flow.TransferZone(WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, NihongoLife.Core.Loc.T("Ga Hibari", "Hibari Station", "ひばりえき"), () =>
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
}
