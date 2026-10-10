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
    public sealed class StationTravelController : MonoBehaviour
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
            public Stop(string id, string ja, string vi, string place, string scene, string spawn, int price)
            {
                Id = id; Ja = ja; Vi = vi; Place = place; Scene = scene; Spawn = spawn; Price = price;
            }

            public string Id { get; }
            public string Ja { get; }
            public string Vi { get; }
            public string Place { get; }
            public string Scene { get; }
            public string Spawn { get; }
            public int Price { get; }
        }

        /// <summary>Stops in running order; index 0 is Hibari itself.</summary>
        public static readonly Stop[] Line =
        {
            new Stop("hibari", "ひばり", "Hibari", "Ga Hibari", WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, 0),
            new Stop("gakuen", "がくえんまえ", "Gakuen-mae", "Trường Nhật ngữ Hibari", WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance, 180),
            new Stop("minato", "ミナト", "Minato", "phố cảng · Sushi Hibari", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance, 320),
            new Stop("midori", "みどりじま", "Midori Island", "Đảo Xanh · nông trại", WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation, 450),
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
                : GameObject.Find("TrainCarriageInterior");
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
            IgnorePlayerTrainCollision(FindFirstObjectByType<PlayerController>());
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
                    Announce($"まもなく、{Platform}ばんせんに みどりじまゆきが まいります。", $"Tàu đi Đảo Xanh (qua Gakuen-mae, Minato) sắp vào sân ga số {Platform}.");
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
            if (HasTicket) { Announce("きっぷは もう あります。", $"Bạn đã có vé đi {TicketStop.Vi} — hãy qua cổng soát vé."); return; }
            OpenTicketPanel();
        }

        /// <summary>Buys the Minato ticket (kept for the scenario wording and older tests).</summary>
        public bool PurchaseTicket() => PurchaseTicket("minato");

        public bool PurchaseTicket(string stopId)
        {
            int index = System.Array.FindIndex(Line, s => s.Id == stopId);
            if (index <= 0) return false;
            if (HasTicket) { Announce("きっぷは もう あります。", $"Bạn đã có vé đi {TicketStop.Vi}."); return false; }
            Stop stop = Line[index];
            var inventory = PlayerInventory.Instance;
            if (inventory == null || !inventory.SpendYen(stop.Price))
            {
                Announce("おかねが たりません。", $"Không đủ tiền. Vé đi {stop.Vi} giá ¥{stop.Price}.");
                Cue(GameAudioCue.UiError);
                return false;
            }
            if (!inventory.AddItem(TicketItemId, $"きっぷ（{stop.Ja}）", $"Vé tàu Hibari → {stop.Vi}", stop.Price, 1))
            {
                inventory.AddYen(stop.Price);
                Announce("バッグが いっぱいです。", "Balo đã đầy, không nhận được vé.");
                return false;
            }
            _ticketStop = index;
            CloseTicketPanel();
            CompleteObjective("obj_ticket");
            Announce("きっぷを かいました。", $"Đã mua vé đi {stop.Vi} (¥{stop.Price}). Qua cổng soát vé rồi ra sân ga số {Platform}.");
            Cue(GameAudioCue.UiConfirm);
            return true;
        }

        private void PassGate()
        {
            if (!HasTicket) { Announce("きっぷが ひつようです。", "Cần có vé: hỏi nhân viên Kimura ở quầy hoặc dùng máy bán vé."); Cue(GameAudioCue.UiError); return; }
            if (_gatePassed) { Announce($"{Platform}ばんせんへ どうぞ。", $"Bạn đã qua cổng. Ra sân ga số {Platform} chờ tàu."); return; }
            _gatePassed = true;
            CompleteObjective("obj_platform");
            Announce($"{Platform}ばんせんへ どうぞ。", $"Vé hợp lệ. Ra sân ga số {Platform} — tàu sẽ đợi bạn.");
            Cue(GameAudioCue.UiConfirm);
        }

        // ─────────── Ride ───────────

        private void Board(GameObject player)
        {
            if (_onboard) return;
            if (!_gatePassed) { Announce("さきに かいさつを とおって ください。", "Hãy mua vé và qua cổng soát vé trước."); Cue(GameAudioCue.UiError); return; }
            if (!IsTrainBoarding) { Announce("でんしゃは まだ きません。", $"Tàu chưa vào ga — còn khoảng {SecondsUntilArrival():0} giây."); return; }
            if (_carriageInterior != null) _carriageInterior.SetActive(true);
            ShuffleCommuters();
            Teleport(player, carriageSpawn);
            var camera = FindFirstObjectByType<Cameras.ThirdPersonCameraController>();
            if (camera != null && carriageSpawn != null)
            {
                camera.SetIndoorMode(true);
                camera.SetOrbit(carriageSpawn.eulerAngles.y, 16f, 3.0f);
            }
            _onboard = true;
            _nextStop = 1;
            if (_stationHud != null) _stationHud.gameObject.SetActive(false);
            if (_onboardHud != null) _onboardHud.gameObject.SetActive(true);
            StartCoroutine(RideRoutine());
        }

        private IEnumerator RideRoutine()
        {
            Announce("ドアが しまります。ご注意ください。", "Cửa sắp đóng. Xin chú ý.", 3f);
            for (_nextStop = 1; _nextStop < Line.Length; _nextStop++)
            {
                Stop stop = Line[_nextStop];
                _riding = true;
                _atStop = false;
                _legProgress = 0f;
                if (_windowScenery != null) _windowScenery.Throttle = 1f;
                yield return new WaitForSeconds(1.5f);
                Announce($"つぎは {stop.Ja}、{stop.Ja}です。", $"Ga tiếp theo: {stop.Vi} ({stop.Place}). Bấm Q để ngắm cảnh, F để nói chuyện.");
                float elapsed = 0f;
                while (elapsed < LegSeconds)
                {
                    elapsed += Time.deltaTime;
                    _legProgress = Mathf.Clamp01(elapsed / LegSeconds);
                    if (_windowScenery != null && elapsed > LegSeconds - 3f) _windowScenery.Throttle = 0f; // brake into the platform
                    yield return null;
                }
                _riding = false;
                _atStop = true;
                _legProgress = 1f;
                Announce($"{stop.Ja}、{stop.Ja}です。おでぐちは ひだりがわです。", $"Đã tới ga {stop.Vi}. Cửa ra bên trái.");
                Cue(GameAudioCue.UiConfirm);
                if (_nextStop == _ticketStop)
                {
                    yield return new WaitForSeconds(2f);
                    AlightAt(_nextStop);
                    yield break;
                }
                yield return new WaitForSeconds(DwellSeconds);
                if (_alighting) yield break;
                Announce("ドアが しまります。", $"Cửa đóng — vé của bạn đi tới {TicketStop?.Vi}.", 3f);
            }
        }

        private void Leave()
        {
            if (!_onboard) return;
            if (_riding) { Announce("でんしゃが はしって います。", "Tàu đang chạy — đợi tới ga đã."); return; }
            if (_atStop) AlightAt(_nextStop);
        }

        private void AlightAt(int stopIndex)
        {
            if (_alighting) return;
            _alighting = true;
            Stop stop = Line[stopIndex];
            SetWindowView(false);
            PlayerInventory.Instance?.RemoveItem(TicketItemId);
            PlayerStatus.Instance?.AddKnowledge(5);
            string stopId = stop.Id;
            SceneFlowController flow = null;
            if (!GameServices.TryGet(out flow)) flow = FindFirstObjectByType<SceneFlowController>();
            if (flow == null)
            {
                Debug.LogError("[StationTravelController] No SceneFlowController — cannot leave the train.");
                _alighting = false;
                return;
            }
            flow.TransferZone(stop.Scene, stop.Spawn, $"{stop.Ja}えき / Ga {stop.Vi}", () => OnArrivedAt(stopId));
        }

        /// <summary>Runs on the persistent SceneFlowController after the destination zone is active
        /// (this station, and therefore this component, is unloaded by then).</summary>
        private static void OnArrivedAt(string stopId)
        {
            Stop stop = System.Array.Find(Line, s => s.Id == stopId);
            if (stop == null) return;
            if (stop.Scene != WorldLocationCatalog.MidoriIslandScene) ShowArrivalBanner(stop); // the island shows its own welcome
            var scenario = ScenarioManager.Instance;
            if (stopId != "minato" || scenario == null || scenario.CurrentScenario == null || scenario.CurrentScenario.id != ScenarioId) return;
            scenario.CompleteObjective("obj_board");
            if (scenario.CurrentScenario.GetNode("n_recap") != null) scenario.TransitionToNode("n_recap");
        }

        /// <summary>Arrival banner for trips that end at a stop without riding this line (e.g. the Midori return).</summary>
        public static void AnnounceArrival(string stopId)
        {
            Stop stop = System.Array.Find(Line, s => s.Id == stopId);
            if (stop != null) ShowArrivalBanner(stop);
        }

        private static void ShowArrivalBanner(Stop stop)
        {
            var hud = FindFirstObjectByType<HUDUI>();
            if (hud == null) return;
            var font = NLUi.ResolveFont();
            var canvas = NLUi.CreateCanvas("TrainArrivalBanner", 130, hud.transform);
            var card = NLUi.Panel((RectTransform)canvas.transform, "Card", NLUi.Ink, new RectOffset(30, 30, 16, 16), 4f);
            NLUi.Anchor(card, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(760f, 0f));
            NLUi.FitContent(card);
            NLUi.Label(card, "Title", $"{stop.Ja}えき に つきました", 30f, NLUi.Gold, font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(card, "Sub", $"Đã tới ga {stop.Vi} — {stop.Place}. Cửa ra đưa bạn về phố.", 19f, NLUi.Text, font, FontStyles.Normal, TextAlignmentOptions.Center);
            Destroy(canvas.gameObject, 6f);
        }

        public void SetWindowView(bool open)
        {
            if (_windowCamera == null) return;
            if (open && !_onboard) return;
            _windowCamera.enabled = open;
            _windowCamera.depth = 5f;
            if (_windowCaption != null) _windowCaption.gameObject.SetActive(open);
            if (_viewButton != null) _viewButton.GetComponentInChildren<TextMeshProUGUI>().text = open ? "Q · Về toa tàu" : "Q · Ngắm cảnh";
        }

        /// <summary>Different commuters every trip; the ones the player can talk to always ride.</summary>
        private void ShuffleCommuters()
        {
            if (_carriageInterior == null) return;
            foreach (var passenger in _carriageInterior.GetComponentsInChildren<SeatedPassenger>(true))
                passenger.gameObject.SetActive(passenger.AlwaysOnBoard || Random.value < 0.6f);
        }

        // ─────────── Conversations ───────────

        private void TalkStationStaff()
        {
            var dm = DialogueManager.Instance;
            if (dm == null || dm.IsOpen) return;
            const string staff = "えきいん · Kimura (nhân viên ga)";
            if (HasTicket)
            {
                dm.StartConversation(new List<ScenarioNode>
                {
                    Line_("st_has", staff, $"{TicketStop.Ja}ゆきの きっぷ ですね。かいさつは あちら、{Platform}ばんせん です。", $"{TicketStop.Ja}ゆきの きっぷ ですね。かいさつは あちら、にばんせん です。",
                        $"Vé đi {TicketStop.Vi} nhỉ. Cổng soát vé ở đằng kia, sân ga số {Platform}.", null),
                }, "st_has", null);
                return;
            }

            var nodes = new List<ScenarioNode>
            {
                Line_("st_hello", staff, "いらっしゃいませ。どちらまで ですか。", "いらっしゃいませ。どちらまで ですか。", "Xin chào quý khách. Quý khách đi đến đâu ạ?", null,
                    Choice("ミナトえきへ いきたいです。", "Tôi muốn đến ga Minato.", "st_fare_minato"),
                    Choice("がくえんまえへ いきたいです。", "Tôi muốn đến ga Gakuen-mae.", "st_fare_gakuen"),
                    Choice("みどりじまへ いきたいです。", "Tôi muốn đến Đảo Xanh (Midori).", "st_fare_midori"),
                    Choice("ミナト、どこ？", "Minato, đâu? (cộc lốc)", "st_polite"),
                    Choice("すみません、だいじょうぶです。", "Xin lỗi, không cần đâu ạ.", "st_bye")),
                Line_("st_polite", staff, "「〜へ いきたいです」と いうと ていねいですよ。", "「〜へ いきたいです」と いうと ていねいですよ。", "Nói 「〜へ いきたいです」 sẽ lịch sự hơn đó.", "st_hello"),
                FareLine("minato", staff), FareLine("gakuen", staff), FareLine("midori", staff),
                Line_("st_wrong", staff, $"いいえ、{Platform}ばんせん です。3ばんせんは はんたいほうこう ですよ。", "いいえ、にばんせん です。", $"Không, là sân ga số {Platform}. Số 3 là chiều ngược lại đó.", "st_hello"),
                Line_("st_machine", staff, "では、あちらの けんばいきで どうぞ。", "では、あちらの けんばいきで どうぞ。", "Vậy mời quý khách mua ở máy bán vé đằng kia.", null),
                Line_("st_bye", staff, "はい、どうぞ おきをつけて。", "はい、どうぞ おきをつけて。", "Vâng, quý khách đi cẩn thận.", null),
            };
            dm.StartConversation(nodes, "st_hello", ended =>
            {
                if (string.IsNullOrEmpty(ended) || ended == "cancel") return;
                bool buying = ended.StartsWith("buy:");
                if (ended == "st_machine" || buying) CompleteObjective("obj_ask");
                PlayerStatus.Instance?.AddKnowledge(2);
                if (buying) StartCoroutine(SellAtCounter(ended.Substring("buy:".Length)));
            });
        }

        private ScenarioNode FareLine(string stopId, string staff)
        {
            Stop stop = System.Array.Find(Line, s => s.Id == stopId);
            string yenReading = stop.Price switch { 320 => "さんびゃくにじゅうえん", 450 => "よんひゃくごじゅうえん", _ => "ひゃくはちじゅうえん" };
            return Line_("st_fare_" + stopId, staff, $"{stop.Ja}までは {stop.Price}えん、{Platform}ばんせん です。きっぷを おかいに なりますか。",
                $"{stop.Ja}までは {yenReading}、にばんせん です。", $"Đến {stop.Vi} là {stop.Price} yên, sân ga số {Platform}. Quý khách mua vé luôn không ạ?", null,
                Choice("きっぷを いちまい ください。", $"Cho tôi một vé ạ. (¥{stop.Price})", "buy:" + stopId),
                Choice("けんばいきで かいます。", "Tôi sẽ mua ở máy bán vé.", "st_machine"),
                Choice("3ばんせん ですか。", "Sân ga số 3 ạ?", "st_wrong"));
        }

        private IEnumerator SellAtCounter(string stopId)
        {
            yield return null; // let the first conversation close
            Stop stop = System.Array.Find(Line, s => s.Id == stopId);
            const string staff = "えきいん · Kimura (nhân viên ga)";
            bool sold = PurchaseTicket(stopId);
            var nodes = sold
                ? new List<ScenarioNode>
                {
                    Line_("st_sold", staff, $"{stop.Price}えん ちょうだい します。はい、{stop.Ja}ゆきの きっぷ です。{Platform}ばんせんへ どうぞ。",
                        $"{stop.Price}えん ちょうだい します。", $"Xin nhận {stop.Price} yên. Đây là vé đi {stop.Vi}. Mời quý khách ra sân ga số {Platform}.", null,
                        Choice("ありがとうございます。", "Cảm ơn anh ạ.", "st_thanks")),
                    Line_("st_thanks", staff, "いってらっしゃいませ。", "いってらっしゃいませ。", "Chúc quý khách đi vui vẻ.", null),
                }
                : new List<ScenarioNode>
                {
                    Line_("st_nomoney", staff, "もうしわけありません、おかねが たりない ようです。", "もうしわけありません、おかねが たりない ようです。",
                        $"Xin lỗi, có vẻ quý khách không đủ tiền (vé ¥{stop.Price}).", null),
                };
            DialogueManager.Instance?.StartConversation(nodes, nodes[0].id, null);
        }

        public void TalkToPassenger(string passengerId)
        {
            var dm = DialogueManager.Instance;
            if (dm == null || dm.IsOpen) return;
            SetWindowView(false);
            List<ScenarioNode> nodes = passengerId switch
            {
                "grandma" => new List<ScenarioNode>
                {
                    Line_("ps_q", "おばあさん · Bà cụ", "ミナトの さかなは おいしいですよ。あなたも ミナトへ いきますか。", "ミナトの さかなは おいしいですよ。",
                        "Cá ở Minato ngon lắm đó. Cháu cũng đi Minato à?", null,
                        Choice("はい、すしを たべに いきます。", "Vâng, cháu đi ăn sushi ạ.", "ps_ok"),
                        Choice("いいえ、がくえんまえで おります。", "Không, cháu xuống ở Gakuen-mae ạ.", "ps_school")),
                    Line_("ps_ok", "おばあさん · Bà cụ", "いいですね！ひばりずしが おすすめ ですよ。", "いいですね！", "Hay quá! Bà gợi ý quán Sushi Hibari đó.", null),
                    Line_("ps_school", "おばあさん · Bà cụ", "べんきょう、がんばって くださいね。", "べんきょう、がんばって くださいね。", "Học hành chăm chỉ nhé cháu.", null),
                },
                "worker" => new List<ScenarioNode>
                {
                    Line_("ps_q", "かいしゃいん · Nhân viên văn phòng", "すみません、いま なんじ ですか。", "すみません、いま なんじ ですか。", "Xin lỗi, bây giờ là mấy giờ ạ?", null,
                        Choice("ちょっと まって ください…くじ です。", "Chờ chút ạ… 9 giờ.", "ps_ok"),
                        Choice("しりません。", "Không biết.", "ps_meh")),
                    Line_("ps_ok", "かいしゃいん · Nhân viên văn phòng", "ありがとうございます。まにあいそう です。", "ありがとうございます。", "Cảm ơn bạn. Chắc là kịp giờ rồi.", null),
                    Line_("ps_meh", "かいしゃいん · Nhân viên văn phòng", "そうですか…。", "そうですか…。", "Vậy à…", null),
                },
                _ => _riding || _atStop
                    ? new List<ScenarioNode>
                    {
                        Line_("ps_q", "がくせい · Học sinh", $"すみません、つぎは {NextStop.Ja} ですか。", $"すみません、つぎは {NextStop.Ja} ですか。",
                            $"Xin lỗi, ga tiếp theo là {NextStop.Vi} phải không?", null,
                            Choice($"はい、つぎは {NextStop.Ja} です。", $"Vâng, ga tiếp theo là {NextStop.Vi}.", "ps_ok"),
                            Choice("わかりません。", "Tôi không biết.", "ps_meh")),
                        Line_("ps_ok", "がくせい · Học sinh", "よかった！ありがとうございます。", "よかった！", "May quá! Cảm ơn bạn.", null),
                        Line_("ps_meh", "がくせい · Học sinh", "アナウンスを きいて みましょう。", "アナウンスを きいて みましょう。", "Thử nghe thông báo trên tàu xem.", null),
                    }
                    : new List<ScenarioNode>
                    {
                        Line_("ps_q", "じょうきゃく · Hành khách", $"この でんしゃは ミナトへ いきますか。", "この でんしゃは ミナトへ いきますか。", "Tàu này có đi Minato không?", null,
                            Choice($"はい、{Platform}ばんせんの でんしゃです。", $"Có, là tàu ở sân ga số {Platform}.", "ps_ok"),
                            Choice("いいえ。", "Không.", "ps_meh")),
                        Line_("ps_ok", "じょうきゃく · Hành khách", "たすかりました。ありがとう！", "たすかりました。ありがとう！", "May quá. Cảm ơn nhé!", null),
                        Line_("ps_meh", "じょうきゃく · Hành khách", "えっ、そうですか…。えきいんさんに きいて みます。", "えっ、そうですか…。", "Ơ, vậy sao… Tôi sẽ hỏi nhân viên ga.", null),
                    },
            };
            dm.StartConversation(nodes, "ps_q", ended => { if (ended == "ps_ok") PlayerStatus.Instance?.AddKnowledge(2); });
        }

        private static ScenarioNode Line_(string id, string speaker, string ja, string reading, string vi, string next, params DialogueChoice[] choices) => new ScenarioNode
        {
            id = id, nodeType = ScenarioNodeType.Dialogue, speakerId = "station_" + id, speakerName = speaker,
            textJa = ja, textReading = reading, textEn = vi, nextNodeId = next,
            choices = new List<DialogueChoice>(choices)
        };

        private static DialogueChoice Choice(string ja, string vi, string next) => new DialogueChoice { textJa = ja, textEn = vi, nextNodeId = next };

        private static void CompleteObjective(string objectiveId)
        {
            var scenario = ScenarioManager.Instance;
            if (scenario != null && scenario.CurrentScenario != null && scenario.CurrentScenario.id == ScenarioId)
                scenario.CompleteObjective(objectiveId);
        }

        private float SecondsUntilArrival() => IsTrainBoarding ? 0f : serviceInterval - Phase;

        // ─────────── Station fixtures ───────────

        /// <summary>Kimura (npc_station_staff) sells tickets ahead of the narrated scenario chain; the
        /// clerk anchor without an NPC keeps a plain trigger.</summary>
        private void EnsureStationStaff()
        {
            NPCController kimura = null;
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (npc.gameObject.scene == gameObject.scene && npc.NpcId == "npc_station_staff") kimura = npc;
            if (kimura != null)
            {
                if (kimura.GetComponent<StationStaffService>() == null) kimura.gameObject.AddComponent<StationStaffService>();
                return;
            }
            var clerk = GameObject.Find("StationTicketClerk");
            if (clerk == null || clerk.GetComponentInChildren<StationStaffInteractable>(true) != null) return;
            var interaction = new GameObject("StationStaffInteraction");
            interaction.transform.SetParent(clerk.transform, false);
            interaction.transform.localPosition = Vector3.up * 1.1f;
            var collider = interaction.AddComponent<SphereCollider>();
            collider.isTrigger = true; collider.radius = 1.25f;
            interaction.AddComponent<StationStaffInteractable>().Configure(this);
        }

        private void NormalizeStationInteractions()
        {
            StationTravelInteractable firstTicketMachine = null;
            foreach (var interactable in FindObjectsByType<StationTravelInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (interactable == null || interactable.gameObject.scene != gameObject.scene) continue;
                string objectName = interactable.gameObject.name;
                if (objectName.IndexOf("TicketMachine", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    objectName.IndexOf("TicketPOS", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (firstTicketMachine == null) firstTicketMachine = interactable;
                    else if (interactable != firstTicketMachine) interactable.gameObject.SetActive(false);
                }
            }
            if (firstTicketMachine != null) firstTicketMachine.Configure(this, StationAction.BuyTicket, "きっぷを かう", "Mua vé (máy bán vé)");

            foreach (var interactable in FindObjectsByType<StationTravelInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (interactable == null || interactable.gameObject.scene != gameObject.scene) continue;
                switch (interactable.Action)
                {
                    case StationAction.BoardTrain: interactable.Configure(this, StationAction.BoardTrain, "でんしゃに のる", "Lên tàu (sân ga 2)"); break;
                    case StationAction.LeaveTrain: interactable.Configure(this, StationAction.LeaveTrain, "でんしゃを おりる", "Xuống tàu ở ga này"); break;
                    case StationAction.TalkPassenger: interactable.Configure(this, StationAction.TalkPassenger, "はなしかける", "Nói chuyện với hành khách"); break;
                    case StationAction.StartRide: interactable.Configure(this, StationAction.StartRide, "まどの そとを みる", "Ngắm cảnh qua cửa sổ (Q)"); break;
                }
            }

            var scanner = GameObject.Find("GateScanner");
            if (scanner == null) return;
            var scannerInteractable = scanner.GetComponent<StationTravelInteractable>();
            if (scannerInteractable == null)
            {
                var collider = scanner.GetComponent<Collider>();
                if (collider == null)
                {
                    var box = scanner.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.center = new Vector3(0f, 1f, 0f);
                    box.size = new Vector3(2.2f, 2.2f, 1.2f);
                }
                else collider.isTrigger = true; // a solid kiosk hid the prompt behind its mesh
                scannerInteractable = scanner.AddComponent<StationTravelInteractable>();
            }
            scannerInteractable.Configure(this, StationAction.PassGate, "かいさつを とおる", "Qua cổng soát vé");
        }

        private static void Teleport(GameObject player, Transform target)
        {
            if (player == null || target == null) return;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(target.position, target.rotation);
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
        }

        private void CachePlatformFixtures()
        {
            foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item.gameObject.scene != gameObject.scene) continue;
                if (item.name == "PlatformDoor_Left") { _platformDoorLeft = item; _platformDoorLeftClosed = item.position; }
                else if (item.name == "PlatformDoor_Right") { _platformDoorRight = item; _platformDoorRightClosed = item.position; }
                else if (item.name == "DepartureBoard") _departureBoard = item.GetComponent<TextMeshPro>();
            }
        }

        private void UpdatePlatformFixtures()
        {
            float phase = Phase;
            bool boarding = phase < boardingWindow;
            float openAmount = 0f;
            if (boarding)
            {
                const float transition = 1.25f;
                openAmount = phase < transition ? Mathf.SmoothStep(0f, 1f, phase / transition)
                    : phase > boardingWindow - transition ? Mathf.SmoothStep(1f, 0f, (phase - (boardingWindow - transition)) / transition)
                    : 1f;
            }
            if (_platformDoorLeft != null) _platformDoorLeft.position = _platformDoorLeftClosed + Vector3.left * (2.75f * openAmount);
            if (_platformDoorRight != null) _platformDoorRight.position = _platformDoorRightClosed + Vector3.right * (2.75f * openAmount);
            if (_departureBoard != null)
            {
                string state = boarding ? (_gatePassed && !_onboard ? "のりば で まって います" : "ただいま 乗車中") : $"つぎ {SecondsUntilArrival():00}s";
                _departureBoard.text = $"{Platform}ばんせん  みどりじまゆき\n<size=55%>がくえんまえ ¥180 · ミナト ¥320 · みどりじま ¥450    {state}</size>";
            }
            string led = _atStop ? $"{NextStop.Ja}  ·  {NextStop.Vi}" : $"つぎは  {NextStop.Ja}  ·  Next {NextStop.Vi}";
            foreach (var text in _carriageLeds) if (text != null) text.text = led;
        }

        private void CachePlatformTrain()
        {
            string[] names = { "HighSpeed_Front", "HighSpeed_Wagon_A", "HighSpeed_Wagon_B" };
            foreach (string trainName in names)
            {
                GameObject item = GameObject.Find(trainName);
                if (item == null) continue;
                _platformTrain.Add((item.transform, item.transform.position));
                foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
                    if (collider != null && !collider.isTrigger) _trainColliders.Add(collider);
            }
        }

        private void IgnorePlayerTrainCollision(PlayerController player)
        {
            if (_playerTrainCollisionIgnored || player == null || _trainColliders.Count == 0) return;
            foreach (Collider playerCollider in player.GetComponentsInChildren<Collider>(true))
            {
                if (playerCollider == null) continue;
                foreach (Collider trainCollider in _trainColliders)
                    if (trainCollider != null) Physics.IgnoreCollision(playerCollider, trainCollider, true);
            }
            _playerTrainCollisionIgnored = true;
        }

        private void UpdatePlatformTrain()
        {
            float phase = Phase;
            float offset;
            if (phase < boardingWindow) offset = 0f;
            else if (phase < boardingWindow + 12f) offset = Mathf.SmoothStep(0f, trainTravelDistance, (phase - boardingWindow) / 12f);
            else if (phase < serviceInterval - 12f) offset = trainTravelDistance;
            else offset = Mathf.SmoothStep(-trainTravelDistance, 0f, (phase - (serviceInterval - 12f)) / 12f);
            foreach (var item in _platformTrain)
                if (item.transform != null) item.transform.position = item.platformPosition + Vector3.right * offset;
        }

        // ─────────── HUD ───────────

        private void BuildTravelHud()
        {
            EnsureEventSystem();
            _font = NLUi.ResolveFont();
            HUDUI sharedHud = FindFirstObjectByType<HUDUI>();
            Transform parent = sharedHud != null ? sharedHud.transform : transform;
            var canvas = NLUi.CreateCanvas("StationTravelHUD", 120, parent);
            _hudRoot = (RectTransform)canvas.transform;

            _stationHud = new GameObject("StationLayer", typeof(RectTransform)).GetComponent<RectTransform>();
            _stationHud.SetParent(_hudRoot, false);
            NLUi.Stretch(_stationHud);

            var route = NLUi.Panel(_stationHud, "RouteCard", NLUi.Ink, new RectOffset(20, 20, 12, 12), 2f);
            NLUi.Anchor(route, new Vector2(1f, 1f), new Vector2(-28f, -96f), new Vector2(400f, 0f));
            NLUi.FitContent(route);
            _routeText = NLUi.Label(route, "Route", "ひばり  →  ？", 24f, NLUi.Text, _font, FontStyles.Bold);
            _statusText = NLUi.Label(route, "Status", "", 17f, NLUi.Muted, _font);

            var steps = NLUi.Group(_stationHud, "TravelSteps", false, 8f, TextAnchor.MiddleCenter, false);
            NLUi.Anchor(steps, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(900f, 44f));
            ((HorizontalLayoutGroup)steps.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            string[] labels = { "1  きっぷ · Mua vé", "2  かいさつ · Qua cổng", $"3  {Platform}ばんせん · Chờ tàu", "4  のる · Lên tàu" };
            foreach (string label in labels)
            {
                var pill = NLUi.Pill(steps, "Step", label, _font, NLUi.Card, NLUi.Muted, 16f);
                _steps.Add((pill.GetComponent<Image>(), pill.GetComponentInChildren<TextMeshProUGUI>()));
            }

            _toast = NLUi.Panel(_hudRoot, "Announcement", NLUi.Ink, new RectOffset(26, 26, 14, 14), 4f);
            NLUi.Anchor(_toast, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(980f, 0f));
            NLUi.FitContent(_toast);
            _toastText = NLUi.Label(_toast, "Text", "", 21f, NLUi.Text, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            _toast.gameObject.SetActive(false);
            BuildOnboardHud();
            BuildTicketPanel();
        }

        /// <summary>Japanese-train style onboard display: an amber LED board with the next stop and a countdown,
        /// a line diagram (travelled part green, the ticket's stop starred, a moving train marker), info chips
        /// and a clickable window-view action. A caption frames the window view.</summary>
        private void BuildOnboardHud()
        {
            _onboardHud = NLUi.Panel(_hudRoot, "OnboardHUD", new Color(0.03f, 0.04f, 0.06f, 0.95f), new RectOffset(24, 24, 14, 16), 10f);
            NLUi.Anchor(_onboardHud, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1060f, 0f));
            NLUi.FitContent(_onboardHud);

            var ledRow = NLUi.Panel(_onboardHud, "LED", new Color(0f, 0f, 0f, 1f), new RectOffset(22, 22, 8, 8), 0f, false);
            ((HorizontalLayoutGroup)ledRow.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            var ledOutline = ledRow.gameObject.AddComponent<Outline>();
            ledOutline.effectColor = new Color(1f, 0.55f, 0.1f, 0.35f);
            ledOutline.effectDistance = new Vector2(2f, -2f);
            _ledText = NLUi.Label(ledRow, "LEDText", "", 32f, new Color(1f, 0.6f, 0.1f), _font, FontStyles.Bold, TextAlignmentOptions.Left);
            NLUi.Size(_ledText, flexibleWidth: 1f);
            _ledTimer = NLUi.Label(ledRow, "LEDTimer", "", 22f, new Color(0.45f, 1f, 0.55f), _font, FontStyles.Bold, TextAlignmentOptions.Right);
            NLUi.Size(_ledTimer, 220f);

            var diagram = new GameObject("LineDiagram", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            diagram.SetParent(_onboardHud, false);
            diagram.GetComponent<LayoutElement>().preferredHeight = 84f;
            _lineTrack = Bar(diagram, "Track", new Color(1f, 1f, 1f, 0.14f), 0.08f, 0.92f, 8f);
            _lineDone = Bar(diagram, "TrackDone", new Color(0.3f, 0.78f, 0.52f), 0.08f, 0.08f, 8f);
            for (int i = 0; i < Line.Length; i++)
            {
                float x = Mathf.Lerp(0.08f, 0.92f, Line.Length == 1 ? 0f : i / (float)(Line.Length - 1));
                var ring = new GameObject("StopRing_" + Line[i].Id, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                ring.SetParent(diagram, false);
                ring.anchorMin = ring.anchorMax = new Vector2(x, 0.66f);
                ring.sizeDelta = new Vector2(30f, 30f);
                ring.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 1f);
                var dot = new GameObject("Stop_" + Line[i].Id, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                dot.SetParent(ring, false);
                dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 0.5f);
                dot.sizeDelta = new Vector2(20f, 20f);
                _stopDots.Add(dot.GetComponent<Image>());
                var label = NLUi.Label(diagram, "Label_" + Line[i].Id, StopLabel(i), 18f, NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center);
                var labelRect = label.rectTransform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(x, 0.66f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.anchoredPosition = new Vector2(0f, -18f);
                labelRect.sizeDelta = new Vector2(240f, 52f);
                _stopLabels.Add(label);
            }
            _trainMarker = NLUi.Pill(diagram, "Train", "電車", _font, NLUi.Gold, NLUi.Ink, 15f);
            _trainMarker.anchorMin = _trainMarker.anchorMax = new Vector2(0.08f, 0.66f);
            _trainMarker.pivot = new Vector2(0.5f, 0f);
            _trainMarker.anchoredPosition = new Vector2(0f, 18f);

            var chips = NLUi.Group(_onboardHud, "Chips", false, 10f, TextAnchor.MiddleCenter, false);
            ((HorizontalLayoutGroup)chips.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            _ticketChip = NLUi.Pill(chips, "Ticket", "", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Gold, 16f).GetComponentInChildren<TextMeshProUGUI>();
            _doorChip = NLUi.Pill(chips, "Door", "", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Text, 16f).GetComponentInChildren<TextMeshProUGUI>();
            _viewButton = NLUi.Button(chips, "WindowViewButton", "Q · Ngắm cảnh", _font, () => SetWindowView(!IsWindowViewOpen), new Color(0.13f, 0.32f, 0.45f), 16f, null, 40f);
            NLUi.Size(_viewButton, 190f, 40f);
            _onboardHint = NLUi.Label(chips, "Hint", "F · Nói chuyện với hành khách", 15f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            _onboardHud.gameObject.SetActive(false);

            _windowCaption = NLUi.Panel(_hudRoot, "WindowCaption", new Color(0.02f, 0.03f, 0.05f, 0.82f), new RectOffset(28, 28, 10, 12), 2f);
            NLUi.Anchor(_windowCaption, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(620f, 0f));
            NLUi.FitContent(_windowCaption);
            NLUi.Label(_windowCaption, "Title", "まどの そと  <size=70%><color=#A8B4C4>Ngắm cảnh qua cửa sổ</color></size>", 24f, NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(_windowCaption, "Hint", "Q · quay lại toa tàu", 15f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            _windowCaption.gameObject.SetActive(false);
        }

        private string StopLabel(int i) => (i == _ticketStop ? "★ " : "") + $"{Line[i].Ja}\n<size=68%><color=#A8B4C4>{Line[i].Vi}</color></size>";

        private static RectTransform Bar(RectTransform parent, string name, Color color, float from, float to, float height)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            bar.anchorMin = new Vector2(from, 0.66f);
            bar.anchorMax = new Vector2(to, 0.66f);
            bar.sizeDelta = new Vector2(0f, height);
            bar.GetComponent<Image>().color = color;
            return bar;
        }

        private void BuildTicketPanel()
        {
            _ticketWindow = NLUi.Panel(_hudRoot, "TicketMachine", NLUi.Ink, new RectOffset(30, 30, 24, 24), 12f);
            NLUi.Anchor(_ticketWindow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 0f));
            NLUi.FitContent(_ticketWindow);
            var header = NLUi.Group(_ticketWindow, "Header", false, 10f, TextAnchor.MiddleLeft);
            ((HorizontalLayoutGroup)header.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            NLUi.Size(NLUi.Label(header, "Title", "けんばいき  <size=70%><color=#A8B4C4>Máy bán vé</color></size>", 28f, NLUi.Text, _font, FontStyles.Bold), flexibleWidth: 1f);
            _ticketWallet = NLUi.Pill(header, "Wallet", "¥0", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Gold, 19f).GetComponentInChildren<TextMeshProUGUI>();
            NLUi.Label(_ticketWindow, "Hint", $"いきさきを えらんで ください — Chọn ga đến (tuyến sân ga số {Platform}).", 18f, NLUi.Muted, _font);
            for (int i = 1; i < Line.Length; i++)
            {
                Stop stop = Line[i];
                string id = stop.Id;
                NLUi.Button(_ticketWindow, "Ticket_" + id.ToUpperInvariant(), $"{stop.Ja}  ·  {stop.Vi}   —   {stop.Place}   —   ¥{stop.Price}", _font,
                    () => PurchaseTicket(id), new Color(0.12f, 0.4f, 0.5f), 21f, null, 60f);
            }
            var closeSpace = new GameObject("CloseSpace", typeof(RectTransform));
            closeSpace.transform.SetParent(header, false);
            NLUi.Size(closeSpace.transform, 46f, 46f);
            NLUi.CloseButton(_ticketWindow, _font, CloseTicketPanel, 46f, 18f);
            _ticketWindow.gameObject.SetActive(false);
        }

        private void OpenTicketPanel()
        {
            if (_ticketWindow == null) return;
            if (PlayerInventory.Instance != null) _ticketWallet.text = $"¥{PlayerInventory.Instance.Yen:N0}";
            _ticketWindow.gameObject.SetActive(true);
            _ticketWindow.SetAsLastSibling();
            UIStyleKit.PlayShowAnimation(_ticketWindow.gameObject);
            SetLocked(true);
        }

        private void CloseTicketPanel()
        {
            if (_ticketWindow == null || !_ticketWindow.gameObject.activeSelf) return;
            _ticketWindow.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            SetLocked(false);
        }

        private static void SetLocked(bool locked)
        {
            ScenarioManager.Instance?.SetPlayerInputLocked(locked);
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.InputLocked = locked;
            Cursor.lockState = CursorLockMode.None; // Sims-style: the cursor stays free in gameplay
            Cursor.visible = true;
        }

        private void RefreshHud()
        {
            if (_routeText == null) return;
            if (_onboard)
            {
                if (_ledText != null)
                    _ledText.text = _atStop ? $"{NextStop.Ja}  <size=62%><color=#FFD08A>Ga {NextStop.Vi}</color></size>" : $"つぎは  {NextStop.Ja}  <size=62%><color=#FFD08A>Next · {NextStop.Vi}</color></size>";
                if (_ledTimer != null)
                    _ledTimer.text = _atStop ? "停車中 · đang dừng" : $"còn {Mathf.CeilToInt((1f - _legProgress) * LegSeconds)}s";
                float position = Mathf.Clamp(_nextStop - 1 + _legProgress, 0f, Line.Length - 1) / (Line.Length - 1);
                float x = Mathf.Lerp(0.08f, 0.92f, position);
                if (_trainMarker != null) _trainMarker.anchorMin = _trainMarker.anchorMax = new Vector2(x, 0.66f);
                if (_lineDone != null) _lineDone.anchorMax = new Vector2(x, 0.66f);
                for (int i = 0; i < _stopDots.Count; i++)
                {
                    bool passed = i < _nextStop || (i == _nextStop && _atStop);
                    _stopDots[i].color = i == _ticketStop ? NLUi.Gold : passed ? new Color(0.3f, 0.78f, 0.52f) : new Color(0.75f, 0.8f, 0.88f);
                    if (i < _stopLabels.Count) _stopLabels[i].text = StopLabel(i);
                }
                if (_ticketChip != null) _ticketChip.text = TicketStop != null ? $"きっぷ  {TicketStop.Ja} · ¥{TicketStop.Price}" : "きっぷ —";
                if (_doorChip != null) _doorChip.text = "おでぐち · cửa bên trái";
                return;
            }
            _routeText.text = HasTicket ? $"ひばり  →  {TicketStop.Ja}" : "ひばり  →  ？";
            string status = IsTrainBoarding ? (_gatePassed ? $"Tàu đang đợi ở sân ga số {Platform} — lên tàu!" : "Tàu đang đón khách")
                : $"Chuyến kế tiếp sau {SecondsUntilArrival():0}s";
            _statusText.text = HasTicket ? $"{status}   ·   ¥{TicketStop.Price}" : $"{status}\nHỏi Kimura ở quầy hoặc dùng máy bán vé";
            int step = _gatePassed ? 3 : HasTicket ? 2 : 1;
            for (int i = 0; i < _steps.Count; i++)
            {
                bool done = i + 1 < step, current = i + 1 == step;
                _steps[i].bg.color = done ? new Color(0.16f, 0.4f, 0.27f) : current ? NLUi.Gold : NLUi.Card;
                _steps[i].text.color = current ? NLUi.Ink : done ? NLUi.Text : NLUi.Muted;
            }
        }

        private void Announce(string japanese, string vietnamese, float seconds = 5f)
        {
            if (_toast == null) return;
            _toastText.text = $"<color=#F2B233>{japanese}</color>\n<size=85%>{vietnamese}</size>";
            _toast.gameObject.SetActive(true);
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
        public string GetpromptEn() => "Nói chuyện với nhân viên ga";
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
