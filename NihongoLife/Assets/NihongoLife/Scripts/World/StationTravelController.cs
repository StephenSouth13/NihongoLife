using System.Collections;
using System.Collections.Generic;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NihongoLife.World
{
    public enum StationAction
    {
        BuyTicket,
        PassGate,
        BoardTrain,
        StartRide,
        LeaveTrain,
        TalkPassenger
        ,TalkStationStaff
    }

    /// <summary>
    /// Hibari Station: one line, platform 2, ひばり → がくえんまえ (¥180) → ミナト (¥320) → みどりじま (¥450, island terminus).
    /// Kimura at the counter or the ticket machine sells the ticket, the gate checks it, the train is
    /// called in and holds its doors for a validated passenger, and the ride stops at every station.
    /// Alighting at Gakuen-mae lands in front of Hibari School, Minato in front of Sushi Hibari
    /// (SceneFlowController.TransferZone) — the trip no longer drops the player back on the platform.
    /// The station scenario (scenario.station.buy_ticket) only receives objective progress, and is
    /// wrapped up with its recap once the player actually reaches Minato.
    /// </summary>
    public sealed partial class StationTravelController : MonoBehaviour
    {
        [SerializeField] private Transform platformSpawn;
        [SerializeField] private Transform carriageSpawn;
        [SerializeField] private Transform sceneryRoot;
        [SerializeField] private int ticketPrice = 320;
        [SerializeField] private float rideDuration = 18f;
        [SerializeField] private float scenerySpeed = 8f;
        [SerializeField] private float sceneryLoopWidth = 48f;
        [SerializeField] private float serviceInterval = 60f;
        [SerializeField] private float boardingWindow = 15f;
        [SerializeField] private float trainTravelDistance = 75f;

        public sealed class Stop
        {
            public Stop(string id, string ja, string vi, string place, string placeEn, string placeJa, string scene, string spawn, int price)
            {
                Id = id; Ja = ja; Vi = vi; _place = place; _placeEn = placeEn; _placeJa = placeJa; Scene = scene; Spawn = spawn; Price = price;
            }

            private readonly string _place, _placeEn, _placeJa;

            public string Id { get; }
            public string Ja { get; }
            public string Vi { get; }
            /// <summary>Station name in the interface language (kana in Japanese, romaji otherwise).</summary>
            public string Name => NihongoLife.Core.Loc.IsJapanese ? Ja : Vi;
            /// <summary>What is at the stop, in the interface language.</summary>
            public string Place => NihongoLife.Core.Loc.Pick(_place, _placeEn, _placeJa);
            public string Scene { get; }
            public string Spawn { get; }
            public int Price { get; }
        }

        /// <summary>Stops in running order; index 0 is Hibari itself.</summary>
        public static readonly Stop[] Line =
        {
            new Stop("hibari", "ひばり", "Hibari", "Ga Hibari", "Hibari Station", "ひばり駅", WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, 0),
            new Stop("gakuen", "がくえんまえ", "Gakuen-mae", "Trường Nhật ngữ Hibari", "Hibari Japanese School", "ひばり日本語学校", WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance, 180),
            new Stop("minato", "ミナト", "Minato", "phố cảng · Sushi Hibari", "harbour town · Sushi Hibari", "みなと町・ひばり寿司", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance, 320),
            new Stop("midori", "みどりじま", "Midori Island", "Đảo Xanh · nông trại", "Midori Island · farm", "みどりじま・農場", WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation, 450),
        };

        public const string Destination = "ミナト";
        public const string DestinationVi = "Minato";
        public const string TicketItemId = "train_ticket_minato";
        public const int Platform = 2;
        private const string ScenarioId = "scenario.station.buy_ticket";
        private const float LegSeconds = 10f;
        private const float DwellSeconds = 4f;

        private bool _gatePassed;
        private bool _onboard;
        private bool _riding;
        private bool _atStop;
        private bool _alighting;
        private int _ticketStop = -1;
        private int _nextStop = 1;
        private float _legProgress;
        private float _serviceClock;
        private RectTransform _hudRoot;
        private TextMeshProUGUI _routeText;
        private TextMeshProUGUI _statusText;
        private readonly List<(Image bg, TextMeshProUGUI text)> _steps = new();
        private RectTransform _stationHud;
        private RectTransform _toast;
        private TextMeshProUGUI _toastText;
        private float _toastUntil;
        private RectTransform _ticketWindow;
        private TextMeshProUGUI _ticketWallet;
        private RectTransform _onboardHud;
        private TextMeshProUGUI _ledText;
        private TextMeshProUGUI _onboardHint;
        private TextMeshProUGUI _ledTimer;
        private TextMeshProUGUI _ticketChip;
        private TextMeshProUGUI _doorChip;
        private Button _viewButton;
        private RectTransform _lineDone;
        private RectTransform _windowCaption;
        private readonly List<TextMeshProUGUI> _stopLabels = new();
        private RectTransform _lineTrack;
        private RectTransform _trainMarker;
        private readonly List<Image> _stopDots = new();
        private TMP_FontAsset _font;
        private readonly List<(Transform transform, Vector3 platformPosition)> _platformTrain = new();
        private readonly List<Collider> _trainColliders = new();
        private bool _playerTrainCollisionIgnored;
        private GameObject _carriageInterior;
        private TrainWindowScenery _windowScenery;
        private Camera _windowCamera;
        private readonly List<TextMeshPro> _carriageLeds = new();
        private Transform _platformDoorLeft;
        private Transform _platformDoorRight;
        private Vector3 _platformDoorLeftClosed;
        private Vector3 _platformDoorRightClosed;
        private TextMeshPro _departureBoard;

        public string LastAnnouncement { get; private set; }
        public bool HasTicket => _ticketStop > 0;
        public Stop TicketStop => HasTicket ? Line[_ticketStop] : null;
        public bool GatePassed => _gatePassed;
        public bool IsOnboard => _onboard;
        public bool IsRiding => _riding;
        public bool IsAtStop => _atStop;
        public Stop NextStop => Line[Mathf.Clamp(_nextStop, 0, Line.Length - 1)];
        public bool IsTrainBoarding => Phase < boardingWindow;
        public bool IsTicketMachineOpen => _ticketWindow != null && _ticketWindow.gameObject.activeSelf;
        public bool IsWindowViewOpen => _windowCamera != null && _windowCamera.enabled;
        public Camera WindowCamera => _windowCamera;

        /// <summary>World centre of the carriage (the builder moves the carriage far from the hall).</summary>
        public Vector3 CarriageCenter => carriageSpawn != null && carriageSpawn.parent != null
            ? carriageSpawn.parent.TransformPoint(new Vector3(800f, 2f, 42f))
            : new Vector3(800f, 2f, 42f);
        private float Phase => Mathf.Repeat(_serviceClock, serviceInterval);

        public void Configure(Transform platform, Transform carriage, Transform movingScenery)
        {
            platformSpawn = platform;
            carriageSpawn = carriage;
            sceneryRoot = movingScenery;
        }

        private void Awake()
        {
            _carriageInterior = carriageSpawn != null && carriageSpawn.parent != null
                ? carriageSpawn.parent.gameObject
                : SceneRefs.FindNamed("TrainCarriageInterior");
            if (_carriageInterior != null)
            {
                _windowScenery = _carriageInterior.GetComponentInChildren<TrainWindowScenery>(true);
                foreach (var cam in _carriageInterior.GetComponentsInChildren<Camera>(true))
                    if (cam.name == "TrainWindowCamera") _windowCamera = cam;
                foreach (var led in _carriageInterior.GetComponentsInChildren<TextMeshPro>(true))
                    if (led.name.StartsWith("CarriageLED")) _carriageLeds.Add(led);
                _carriageInterior.SetActive(false);
            }
            if (_windowCamera != null) _windowCamera.enabled = false;
            _serviceClock = boardingWindow + 14f; // the first train is out of the station when the player arrives
            BuildTravelHud();
            CachePlatformTrain();
            CachePlatformFixtures();
            EnsureStationStaff();
            NormalizeStationInteractions();
            RefreshHud();
        }

        private void Start()
        {
            CompleteObjective("obj_arrive");
            NihongoLife.UI.UiModalStack.Register(this, () => IsTicketMachineOpen, CloseTicketPanel, "Ticket machine");
            NihongoLife.UI.UiModalStack.Register(this, () => IsWindowViewOpen, () => SetWindowView(false), "Window view");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (_onboard && keyboard != null && keyboard.qKey.wasPressedThisFrame && !NihongoLife.UI.UiModalStack.BlocksHotkeys && (DialogueManager.Instance == null || !DialogueManager.Instance.IsOpen))
                SetWindowView(!IsWindowViewOpen);

            AdvanceTimetable(Time.deltaTime);
            UpdatePlatformTrain();
            UpdatePlatformFixtures();
            IgnorePlayerTrainCollision(SceneRefs.Find<PlayerController>());
            RefreshHud();
            if (_toast != null && _toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil) _toast.gameObject.SetActive(false);
        }

        /// <summary>Normal 60 s service, except: a validated passenger calls the train in, and the doors
        /// stay open until that passenger boards.</summary>
        private void AdvanceTimetable(float dt)
        {
            bool waitingPassenger = _gatePassed && !_onboard;
            if (waitingPassenger)
            {
                if (Phase > boardingWindow + 12f && Phase < serviceInterval - 12f)
                {
                    _serviceClock += (serviceInterval - 12f) - Phase; // call the next train in now
                    Announce($"まもなく、{Platform}ばんせんに みどりじまゆきが まいります。", NihongoLife.Core.Loc.Gloss($"Tàu đi Đảo Xanh (qua Gakuen-mae, Minato) sắp vào sân ga số {Platform}.", $"The train for Midori Island (via Gakuen-mae, Minato) is arriving at platform {Platform}."));
                }
                if (Phase < boardingWindow && Phase + dt >= boardingWindow - 2f) return; // hold the doors
            }
            _serviceClock += dt;
        }

        public void Execute(StationAction action, GameObject player)
        {
            switch (action)
            {
                case StationAction.BuyTicket: BuyTicket(); break;
                case StationAction.PassGate: PassGate(); break;
                case StationAction.BoardTrain: Board(player); break;
                case StationAction.StartRide: SetWindowView(!IsWindowViewOpen); break;
                case StationAction.LeaveTrain: Leave(); break;
                case StationAction.TalkPassenger: TalkToPassenger("student"); break;
                case StationAction.TalkStationStaff: TalkStationStaff(); break;
            }
            RefreshHud();
        }

        // ─────────── Tickets ───────────

        private void BuyTicket()
        {
            if (HasTicket) { Announce("きっぷは もう あります。", NihongoLife.Core.Loc.Gloss($"Bạn đã có vé đi {TicketStop.Vi} — hãy qua cổng soát vé.", $"You already have a ticket to {TicketStop.Vi} — go through the ticket gate.")); return; }
            OpenTicketPanel();
        }

        /// <summary>Buys the Minato ticket (kept for the scenario wording and older tests).</summary>
        public bool PurchaseTicket() => PurchaseTicket("minato");

        public bool PurchaseTicket(string stopId)
        {
            int index = System.Array.FindIndex(Line, s => s.Id == stopId);
            if (index <= 0) return false;
            if (HasTicket) { Announce("きっぷは もう あります。", NihongoLife.Core.Loc.Gloss($"Bạn đã có vé đi {TicketStop.Vi}.", $"You already have a ticket to {TicketStop.Vi}.")); return false; }
            Stop stop = Line[index];
            var inventory = PlayerInventory.Instance;
            if (inventory == null || !inventory.SpendYen(stop.Price, NihongoLife.Core.Loc.Triple("Vé tàu", "Train ticket", "きっぷ")))
            {
                Announce("おかねが たりません。", NihongoLife.Core.Loc.Gloss($"Không đủ tiền. Vé đi {stop.Vi} giá ¥{stop.Price}.", $"Not enough money. A ticket to {stop.Vi} costs ¥{stop.Price}."));
                Cue(GameAudioCue.UiError);
                return false;
            }
            if (!inventory.AddItem(TicketItemId, $"きっぷ（{stop.Ja}）", NihongoLife.Core.Loc.Triple($"Vé tàu Hibari → {stop.Vi}", $"Train ticket Hibari → {stop.Vi}", $"きっぷ（ひばり → {stop.Ja}）"), stop.Price, 1))
            {
                inventory.AddYen(stop.Price, NihongoLife.Core.Loc.Triple("Hoàn vé tàu", "Ticket refund", "きっぷの払い戻し"));
                Announce("バッグが いっぱいです。", NihongoLife.Core.Loc.Gloss("Balo đã đầy, không nhận được vé.", "Your bag is full, so you can't take the ticket."));
                return false;
            }
            _ticketStop = index;
            CloseTicketPanel();
            CompleteObjective("obj_ticket");
            Announce("きっぷを かいました。", NihongoLife.Core.Loc.Gloss($"Đã mua vé đi {stop.Vi} (¥{stop.Price}). Qua cổng soát vé rồi ra sân ga số {Platform}.", $"Bought a ticket to {stop.Vi} (¥{stop.Price}). Go through the gate to platform {Platform}."));
            Cue(GameAudioCue.UiConfirm);
            return true;
        }

        private void PassGate()
        {
            if (!HasTicket) { Announce("きっぷが ひつようです。", NihongoLife.Core.Loc.Gloss("Cần có vé: hỏi nhân viên Kimura ở quầy hoặc dùng máy bán vé.", "You need a ticket: ask Kimura at the counter or use the ticket machine.")); Cue(GameAudioCue.UiError); return; }
            if (_gatePassed) { Announce($"{Platform}ばんせんへ どうぞ。", NihongoLife.Core.Loc.Gloss($"Bạn đã qua cổng. Ra sân ga số {Platform} chờ tàu.", $"You're through the gate. Wait for the train on platform {Platform}.")); return; }
            _gatePassed = true;
            CompleteObjective("obj_platform");
            Announce($"{Platform}ばんせんへ どうぞ。", NihongoLife.Core.Loc.Gloss($"Vé hợp lệ. Ra sân ga số {Platform} — tàu sẽ đợi bạn.", $"Ticket accepted. Go to platform {Platform} — the train will wait for you."));
            Cue(GameAudioCue.UiConfirm);
        }





        private void Announce(string japanese, string vietnamese, float seconds = 5f)
        {
            // Announcements are notifications: they go to the shared feed (top centre), never over the prompt lane.
            HudFeed.Post($"<color=#F2B233>{japanese}</color>  {vietnamese}", HudFeed.Kind.Info, seconds);
            LastAnnouncement = vietnamese;
            if (_toast == null) return;
            _toastText.text = $"<color=#F2B233>{japanese}</color>\n<size=85%>{vietnamese}</size>";
            _toastUntil = Time.unscaledTime + seconds;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var eventSystemObject = new GameObject("StationEventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void Cue(GameAudioCue cue)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, 0.75f);
        }

        private void OnDestroy()
        {
            if (_hudRoot != null) Destroy(_hudRoot.gameObject);
        }
    }

    public sealed class StationStaffInteractable : MonoBehaviour, IInteractable
    {
        private StationTravelController _controller;

        public void Configure(StationTravelController controller) => _controller = controller;
        public string GetPromptJa() => "えきいんと はなす";
        public string GetpromptEn() => NihongoLife.Core.Loc.T("Nói chuyện với nhân viên ga", "Talk to the station staff", "えきいんと はなす");
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => _controller?.Execute(StationAction.TalkStationStaff, player);
    }

    public sealed class StationBillboardLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
        }
    }
}
