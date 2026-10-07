using System.Collections;
using System.Collections.Generic;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
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
    /// Hibari Station travel loop, matching scenario.station.buy_ticket (Minato, ¥320, platform 2):
    /// ticket machine → ticket gate → platform 2 → board → ride → arrive at Minato → back on the platform.
    /// Once the player has passed the gate the next train is called in and holds its doors until the
    /// player boards (the old timetable expired the ticket 5 s after departure and closed the doors after
    /// 15 s, so most players never managed to board). Staff and passengers talk through the shared
    /// dialogue UI instead of plain status text.
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

        public const string Destination = "ミナト";
        public const string DestinationVi = "Minato";
        public const string TicketItemId = "train_ticket_minato";
        private const string ScenarioId = "scenario.station.buy_ticket";

        private readonly List<Transform> _scenery = new();
        private bool _hasTicket;
        private bool _gatePassed;
        private bool _onboard;
        private bool _riding;
        private float _rideRemaining;
        private float _serviceClock;
        private RectTransform _hudRoot;
        private TextMeshProUGUI _routeText;
        private TextMeshProUGUI _statusText;
        private readonly List<(Image bg, TextMeshProUGUI text)> _steps = new();
        private RectTransform _toast;
        private TextMeshProUGUI _toastText;
        private float _toastUntil;
        private RectTransform _ticketWindow;
        private TextMeshProUGUI _ticketWallet;
        private TMP_FontAsset _font;
        private readonly List<(Transform transform, Vector3 platformPosition)> _platformTrain = new();
        private readonly List<Collider> _trainColliders = new();
        private bool _playerTrainCollisionIgnored;
        private GameObject _carriageInterior;
        private Transform _platformDoorLeft;
        private Transform _platformDoorRight;
        private Vector3 _platformDoorLeftClosed;
        private Vector3 _platformDoorRightClosed;
        private TextMeshPro _departureBoard;

        public bool HasTicket => _hasTicket;
        public bool GatePassed => _gatePassed;
        public bool IsOnboard => _onboard;
        public bool IsRiding => _riding;
        public bool IsTrainBoarding => Phase < boardingWindow;
        public bool IsTicketMachineOpen => _ticketWindow != null && _ticketWindow.gameObject.activeSelf;
        private float Phase => Mathf.Repeat(_serviceClock, serviceInterval);

        public void Configure(Transform platform, Transform carriage, Transform movingScenery)
        {
            platformSpawn = platform;
            carriageSpawn = carriage;
            sceneryRoot = movingScenery;
        }

        private void Awake()
        {
            ticketPrice = 320;
            rideDuration = Mathf.Min(rideDuration, 18f);
            _carriageInterior = carriageSpawn != null && carriageSpawn.parent != null
                ? carriageSpawn.parent.gameObject
                : GameObject.Find("TrainCarriageInterior");
            if (sceneryRoot != null && _carriageInterior != null)
                sceneryRoot.SetParent(_carriageInterior.transform, true);
            if (sceneryRoot != null)
                foreach (Transform child in sceneryRoot) _scenery.Add(child);
            if (_carriageInterior != null) _carriageInterior.SetActive(false);
            _serviceClock = boardingWindow + 14f; // the first train is out of the station when the player arrives
            BuildTravelHud();
            CachePlatformTrain();
            CachePlatformFixtures();
            EnsureStationStaffInteractable();
            NormalizeStationInteractions();
            RefreshHud();
        }

        private void Update()
        {
            if (IsTicketMachineOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseTicketPanel();
                return;
            }

            AdvanceTimetable(Time.deltaTime);
            UpdatePlatformTrain();
            UpdatePlatformFixtures();
            IgnorePlayerTrainCollision(FindFirstObjectByType<PlayerController>());

            if (_riding)
            {
                _rideRemaining = Mathf.Max(0f, _rideRemaining - Time.deltaTime);
                MoveScenery();
                if (_rideRemaining <= 0f) StartCoroutine(Arrive());
            }

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
                    Announce("まもなく、2ばんせんに ミナトゆきが まいります。", "Tàu đi Minato sắp vào sân ga số 2.");
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
                case StationAction.StartRide: StartRide(); break;
                case StationAction.LeaveTrain: Leave(player); break;
                case StationAction.TalkPassenger: TalkPassenger(); break;
                case StationAction.TalkStationStaff: TalkStationStaff(); break;
            }
            RefreshHud();
        }

        // ─────────── Actions ───────────

        private void BuyTicket()
        {
            if (_hasTicket) { Announce("きっぷは もう あります。", "Bạn đã có vé đi Minato — hãy qua cổng soát vé."); return; }
            OpenTicketPanel();
        }

        /// <summary>Buys the Minato ticket (ticket machine button; also used by tests).</summary>
        public bool PurchaseTicket()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null || !inventory.SpendYen(ticketPrice))
            {
                Announce("おかねが たりません。", $"Không đủ tiền. Vé đi Minato giá ¥{ticketPrice}.");
                Cue(GameAudioCue.UiError);
                return false;
            }
            if (!inventory.AddItem(TicketItemId, "きっぷ（ミナト）", "Vé tàu đi Minato", ticketPrice, 1))
            {
                inventory.AddYen(ticketPrice);
                Announce("バッグが いっぱいです。", "Balo đã đầy, không nhận được vé.");
                return false;
            }
            _hasTicket = true;
            CloseTicketPanel();
            CompleteObjective("obj_ticket");
            Announce("きっぷを かいました。", $"Đã mua vé đi Minato (¥{ticketPrice}). Qua cổng soát vé rồi ra sân ga số 2.");
            Cue(GameAudioCue.UiConfirm);
            return true;
        }

        private void PassGate()
        {
            if (!_hasTicket) { Announce("きっぷが ひつようです。", "Cần mua vé ở máy bán vé trước."); Cue(GameAudioCue.UiError); return; }
            if (_gatePassed) { Announce("2ばんせんへ どうぞ。", "Bạn đã qua cổng. Ra sân ga số 2 chờ tàu."); return; }
            _gatePassed = true;
            CompleteObjective("obj_platform");
            Announce("2ばんせんへ どうぞ。", "Vé hợp lệ. Ra sân ga số 2 — tàu sẽ đợi bạn.");
            Cue(GameAudioCue.UiConfirm);
        }

        private void Board(GameObject player)
        {
            if (_onboard) return;
            if (!_gatePassed) { Announce("さきに かいさつを とおって ください。", "Hãy mua vé và qua cổng soát vé trước."); Cue(GameAudioCue.UiError); return; }
            if (!IsTrainBoarding) { Announce("でんしゃは まだ きません。", $"Tàu chưa vào ga — còn khoảng {SecondsUntilArrival():0} giây."); return; }
            if (_carriageInterior != null) _carriageInterior.SetActive(true);
            Teleport(player, carriageSpawn);
            PlayerInventory.Instance?.RemoveItem(TicketItemId);
            _onboard = true;
            StartRide();
        }

        private void StartRide()
        {
            if (!_onboard || _riding) return;
            _riding = true;
            _rideRemaining = rideDuration;
            Announce("ドアが しまります。つぎは ミナト、ミナトです。", "Cửa đóng. Ga tiếp theo: Minato. Hãy nói chuyện với hành khách hoặc ngắm cảnh.");
        }

        private IEnumerator Arrive()
        {
            if (!_riding) yield break;
            _riding = false;
            Announce("ミナト、ミナトです。おでぐちは ひだりがわです。", "Đã tới ga Minato! Cửa ra ở bên trái.");
            CompleteObjective("obj_board");
            PlayerStatus.Instance?.AddKnowledge(5);
            yield return new WaitForSeconds(2.5f);
            Leave(FindFirstObjectByType<PlayerController>()?.gameObject);
        }

        private void Leave(GameObject player)
        {
            if (_riding) { Announce("でんしゃが はしって います。", "Tàu đang chạy — đợi tới ga đã."); return; }
            if (!_onboard) return;
            Teleport(player, platformSpawn);
            if (_carriageInterior != null) _carriageInterior.SetActive(false);
            _onboard = false;
            _hasTicket = false;
            _gatePassed = false;
            Announce("おつかれさまでした。", "Đã xuống tàu. Đi qua lối 出口 để về thành phố.");
        }

        private void TalkStationStaff()
        {
            // While the station scenario owns the clerk, let it drive the conversation.
            var scenario = ScenarioManager.Instance;
            if (scenario != null && scenario.CurrentScenario != null && scenario.CurrentScenario.id == ScenarioId
                && scenario.OnNPCInteracted("npc_station_staff", null)) return;

            var dm = DialogueManager.Instance;
            if (dm == null) return;
            const string staff = "えきいん · Nhân viên ga";
            var nodes = new List<ScenarioNode>
            {
                Line("st_hello", staff, "いらっしゃいませ。どちらまで ですか。", "いらっしゃいませ。どちらまで ですか。", "Xin chào quý khách. Quý khách đi đến đâu ạ?", null,
                    Choice("ミナトえきへ いきたいです。", "Tôi muốn đến ga Minato.", "st_fare"),
                    Choice("ミナト、どこ？", "Minato, đâu? (cộc lốc)", "st_polite"),
                    Choice("すみません、だいじょうぶです。", "Xin lỗi, không cần đâu ạ.", "st_bye")),
                Line("st_polite", staff, "「〜へ いきたいです」と いうと ていねいですよ。", "「〜へ いきたいです」と いうと ていねいですよ。", "Nói 「〜へ いきたいです」 sẽ lịch sự hơn đó.", "st_hello"),
                Line("st_fare", staff, "ミナトまでは 320えん です。2ばんせん です。", "ミナトまでは さんびゃくにじゅうえん です。にばんせん です。", "Đến Minato là 320 yên, sân ga số 2.", null,
                    Choice("2ばんせんですね。ありがとうございます。", "Sân ga số 2 nhỉ. Cảm ơn ạ.", "st_machine"),
                    Choice("3ばんせんですね。", "Sân ga số 3 nhỉ.", "st_wrong")),
                Line("st_wrong", staff, "いいえ、2ばんせん です。3ばんせんは はんたいほうこう です。", "いいえ、にばんせん です。", "Không, là sân ga số 2. Số 3 là chiều ngược lại.", "st_fare"),
                Line("st_machine", staff, "きっぷは あの けんばいきで かって ください。", "きっぷは あの けんばいきで かって ください。", "Vé thì mua ở máy bán vé đằng kia nhé.", null),
                Line("st_bye", staff, "はい、どうぞ おきをつけて。", "はい、どうぞ おきをつけて。", "Vâng, quý khách đi cẩn thận.", null),
            };
            dm.StartConversation(nodes, "st_hello", ended => { if (ended != "cancel") PlayerStatus.Instance?.AddKnowledge(3); });
        }

        private void TalkPassenger()
        {
            var dm = DialogueManager.Instance;
            if (dm == null) return;
            const string passenger = "じょうきゃく · Hành khách";
            var nodes = _riding
                ? new List<ScenarioNode>
                {
                    Line("ps_q", passenger, "すみません、ミナトは まだ ですか。", "すみません、ミナトは まだ ですか。", "Xin lỗi, chưa tới Minato à?", null,
                        Choice("はい、つぎです。", "Vâng, ga tiếp theo ạ.", "ps_ok"),
                        Choice("わかりません。", "Tôi không biết.", "ps_meh")),
                    Line("ps_ok", passenger, "そうですか。ありがとうございます！", "そうですか。ありがとうございます！", "Vậy à. Cảm ơn bạn!", null),
                    Line("ps_meh", passenger, "アナウンスを きいて みましょう。", "アナウンスを きいて みましょう。", "Thử nghe thông báo trên tàu xem.", null),
                }
                : new List<ScenarioNode>
                {
                    Line("ps_q", passenger, "この でんしゃは ミナトへ いきますか。", "この でんしゃは ミナトへ いきますか。", "Tàu này có đi Minato không?", null,
                        Choice("はい、2ばんせんの でんしゃです。", "Có, là tàu ở sân ga số 2.", "ps_ok"),
                        Choice("いいえ。", "Không.", "ps_meh")),
                    Line("ps_ok", passenger, "たすかりました。ありがとう！", "たすかりました。ありがとう！", "May quá. Cảm ơn nhé!", null),
                    Line("ps_meh", passenger, "えっ、そうですか…。えきいんさんに きいて みます。", "えっ、そうですか…。", "Ơ, vậy sao… Tôi sẽ hỏi nhân viên ga.", null),
                };
            dm.StartConversation(nodes, "ps_q", ended => { if (ended == "ps_ok") PlayerStatus.Instance?.AddKnowledge(2); });
        }

        private static ScenarioNode Line(string id, string speaker, string ja, string reading, string vi, string next, params DialogueChoice[] choices) => new ScenarioNode
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

        private void EnsureStationStaffInteractable()
        {
            var clerk = GameObject.Find("StationTicketClerk");
            if (clerk == null || clerk.GetComponentInChildren<StationStaffInteractable>(true) != null) return;
            var interaction = new GameObject("StationStaffInteraction");
            interaction.transform.SetParent(clerk.transform, false);
            interaction.transform.localPosition = Vector3.up * 1.1f;
            var collider = interaction.AddComponent<SphereCollider>();
            collider.isTrigger = true; collider.radius = 1.25f;
            interaction.AddComponent<StationStaffInteractable>().Configure(this);
            AddRoleLabel(clerk.transform, "えきいん\nNhân viên ga", new Color(1f, 0.82f, 0.3f));
        }

        private void NormalizeStationInteractions()
        {
            StationTravelInteractable firstTicketMachine = null;
            foreach (var interactable in FindObjectsByType<StationTravelInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (interactable == null) continue;
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
                if (interactable == null) continue;
                switch (interactable.Action)
                {
                    case StationAction.BoardTrain: interactable.Configure(this, StationAction.BoardTrain, "でんしゃに のる", "Lên tàu đi Minato"); break;
                    case StationAction.LeaveTrain: interactable.Configure(this, StationAction.LeaveTrain, "でんしゃを おりる", "Xuống tàu"); break;
                    case StationAction.TalkPassenger: interactable.Configure(this, StationAction.TalkPassenger, "はなしかける", "Nói chuyện với hành khách"); break;
                    case StationAction.StartRide: interactable.Configure(this, StationAction.StartRide, "せきに すわる", "Ngồi xuống ghế"); break;
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
            if (scanner.GetComponentInChildren<TextMeshPro>(true) == null)
                AddRoleLabel(scanner.transform, "かいさつ\nCổng soát vé", new Color(0.35f, 0.9f, 1f));
        }

        private static void AddRoleLabel(Transform parent, string value, Color color)
        {
            var labelObject = new GameObject("StationStaffRoleLabel");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = Vector3.up * 2.35f;
            var label = labelObject.AddComponent<TextMeshPro>();
            label.text = value; label.fontSize = 2.2f; label.alignment = TextAlignmentOptions.Center;
            label.color = color; labelObject.AddComponent<StationBillboardLabel>();
        }

        private void MoveScenery()
        {
            foreach (Transform item in _scenery)
            {
                item.position += Vector3.left * (scenerySpeed * Time.deltaTime);
                if (item.localPosition.x < -sceneryLoopWidth * 0.5f)
                    item.localPosition += Vector3.right * sceneryLoopWidth;
            }
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
            if (_departureBoard == null) return;
            string state = boarding ? (_gatePassed && !_onboard ? "のりば で まって います" : "ただいま 乗車中") : $"つぎ {SecondsUntilArrival():00}s";
            _departureBoard.text = $"2ばんせん  ミナトゆき\n<size=65%>{state}     ¥{ticketPrice}</size>";
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

            var route = NLUi.Panel(_hudRoot, "RouteCard", NLUi.Ink, new RectOffset(20, 20, 12, 12), 2f);
            NLUi.Anchor(route, new Vector2(1f, 1f), new Vector2(-28f, -96f), new Vector2(380f, 0f));
            NLUi.FitContent(route);
            _routeText = NLUi.Label(route, "Route", "ひばり  →  ミナト", 24f, NLUi.Text, _font, FontStyles.Bold);
            _statusText = NLUi.Label(route, "Status", "", 17f, NLUi.Muted, _font);

            var steps = NLUi.Group(_hudRoot, "TravelSteps", false, 8f, TextAnchor.MiddleCenter, false);
            NLUi.Anchor(steps, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(900f, 44f));
            ((HorizontalLayoutGroup)steps.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            string[] labels = { "1  きっぷ · Mua vé", "2  かいさつ · Qua cổng", "3  2ばんせん · Chờ tàu", "4  のる · Lên tàu" };
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
            BuildTicketPanel();
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
            NLUi.Label(_ticketWindow, "Hint", "いきさきを えらんで ください — Chọn ga đến.", 18f, NLUi.Muted, _font);
            NLUi.Button(_ticketWindow, "Ticket_MINATO", $"ミナト  ·  Minato   —   2ばんせん   —   ¥{ticketPrice}", _font, () => PurchaseTicket(), new Color(0.12f, 0.4f, 0.5f), 22f, null, 60f);
            var later1 = NLUi.Button(_ticketWindow, "Ticket_SHINJUKU", "しんじゅく  ·  Shinjuku   —   じゅんびちゅう (sắp mở)", _font, null, NLUi.Card, 19f, NLUi.Muted, 52f);
            later1.interactable = false;
            var later2 = NLUi.Button(_ticketWindow, "Ticket_ASAKUSA", "あさくさ  ·  Asakusa   —   じゅんびちゅう (sắp mở)", _font, null, NLUi.Card, 19f, NLUi.Muted, 52f);
            later2.interactable = false;
            NLUi.Button(_ticketWindow, "Close", "とじる · Esc", _font, CloseTicketPanel, NLUi.Card, 17f, NLUi.Muted, 44f);
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
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        private void RefreshHud()
        {
            if (_routeText == null) return;
            string status = _riding ? $"Đang chạy — tới Minato sau {_rideRemaining:0}s"
                : _onboard ? "Đã tới Minato"
                : IsTrainBoarding ? (_gatePassed ? "Tàu đang đợi ở sân ga số 2 — lên tàu!" : "Tàu đang đón khách")
                : $"Chuyến kế tiếp sau {SecondsUntilArrival():0}s";
            _statusText.text = status + $"   ·   ¥{ticketPrice}";
            int step = _onboard ? 4 : _gatePassed ? 3 : _hasTicket ? 2 : 1;
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
