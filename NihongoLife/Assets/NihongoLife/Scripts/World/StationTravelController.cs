using System.Collections;
using System.Collections.Generic;
using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
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
    }

    public sealed class StationTravelController : MonoBehaviour
    {
        [SerializeField] private Transform platformSpawn;
        [SerializeField] private Transform carriageSpawn;
        [SerializeField] private Transform sceneryRoot;
        [SerializeField] private int ticketPrice = 180;
        [SerializeField] private float rideDuration = 45f;
        [SerializeField] private float scenerySpeed = 8f;
        [SerializeField] private float sceneryLoopWidth = 48f;
        [SerializeField] private float serviceInterval = 60f;
        [SerializeField] private float boardingWindow = 15f;
        [SerializeField] private float trainTravelDistance = 75f;

        private readonly List<Transform> _scenery = new();
        private bool _hasTicket;
        private bool _gatePassed;
        private bool _riding;
        private float _rideRemaining;
        private Canvas _canvas;
        private TextMeshProUGUI _routeText;
        private TextMeshProUGUI _messageText;
        private GameObject _messagePanel;
        private TextMeshProUGUI _stationHelpText;
        private TextMeshProUGUI _walletText;
        private GameObject _ticketPanel;
        private string _destination = "MIDORI";
        private float _messageUntil;
        private float _ticketDeparture = -1f;
        private readonly List<(Transform transform, Vector3 platformPosition)> _platformTrain = new();
        private InteractionDetector _interactionDetector;
        private GameObject _carriageInterior;

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
            if (sceneryRoot != null && _carriageInterior != null)
                sceneryRoot.SetParent(_carriageInterior.transform, true);
            if (sceneryRoot != null)
                foreach (Transform child in sceneryRoot) _scenery.Add(child);
            if (_carriageInterior != null) _carriageInterior.SetActive(false);
            BuildTravelHud();
            CachePlatformTrain();
            RefreshHud();
        }

        private IEnumerator Start()
        {
            for (int attempt = 0; attempt < 30 && _interactionDetector == null; attempt++)
            {
                _interactionDetector = FindFirstObjectByType<InteractionDetector>();
                if (_interactionDetector == null) yield return null;
            }
            if (_interactionDetector != null)
            {
                _interactionDetector.OnInteractableChanged += HandleInteractableChanged;
                HandleInteractableChanged(_interactionDetector.CurrentInteractable);
            }
        }

        private void Update()
        {
            UpdatePlatformTrain();
            if (_hasTicket && Time.time > _ticketDeparture + 5f)
            {
                _hasTicket = false;
                _gatePassed = false;
                ShowMessage(Localize("Vé đã hết hiệu lực vì bạn lỡ chuyến.", "Your ticket expired because the train was missed.", "乗り遅れたため、切符は無効になりました。"));
            }

            if (_riding)
            {
                _rideRemaining = Mathf.Max(0f, _rideRemaining - Time.deltaTime);
                MoveScenery();
                if (_rideRemaining <= 0f)
                {
                    _riding = false;
                    ShowMessage("Tau da den ga Midori. Ban co the xuong tau.");
                }
                RefreshHud();
            }

            if (_messageText != null && Time.unscaledTime > _messageUntil)
            {
                _messageText.text = string.Empty;
                if (_messagePanel != null) _messagePanel.SetActive(false);
            }
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
            }
            RefreshHud();
        }

        private void BuyTicket()
        {
            if (_hasTicket) { ShowMessage(Localize("Bạn đã có vé cho chuyến kế tiếp.", "You already have a ticket for the next service.", "次の電車の切符を持っています。")); return; }
            if (_ticketPanel != null) _ticketPanel.SetActive(true);
            ShowMessage(Localize("Chọn ga đến trên máy bán vé.", "Choose a destination on the ticket machine.", "券売機で行き先を選んでください。"));
        }

        private void PurchaseTicket(string destination, int fare)
        {
            if (PlayerInventory.Instance == null || !PlayerInventory.Instance.SpendYen(fare))
            {
                ShowMessage(Localize($"Không đủ tiền. Giá vé: ¥{fare}.", $"Not enough money. Fare: ¥{fare}.", $"お金が足りません。運賃は¥{fare}です。"));
                return;
            }
            _destination = destination;
            _hasTicket = true;
            float cycleStart = Mathf.Floor(Time.time / serviceInterval) * serviceInterval;
            _ticketDeparture = cycleStart + boardingWindow;
            if (Time.time > _ticketDeparture) _ticketDeparture += serviceInterval;
            if (_ticketPanel != null) _ticketPanel.SetActive(false);
            ShowMessage(Localize($"Đã mua vé {_destination}. Vé chỉ dùng cho chuyến kế tiếp.", $"{_destination} ticket purchased. Valid only for the next service.", $"{_destination}行きの切符を購入しました。次の電車のみ有効です。"));
            PlayConfirm();
        }

        private void PassGate()
        {
            if (!_hasTicket) { ShowMessage(Localize("Bạn cần mua vé hợp lệ trước.", "A valid ticket is required.", "有効な切符が必要です。")); return; }
            _gatePassed = true;
            ShowMessage(Localize("Vé hợp lệ. Hãy đến sân ga số 1 đúng giờ.", "Ticket accepted. Go to platform 1 on time.", "切符は有効です。時間どおり1番線へお越しください。"));
            PlayConfirm();
        }

        private void Board(GameObject player)
        {
            if (!_gatePassed) { ShowMessage(Localize("Hãy mua vé và qua cổng soát vé trước.", "Buy a ticket and pass the gate first.", "先に切符を購入して改札を通ってください。")); return; }
            if (!IsTrainBoarding()) { ShowMessage(Localize("Tàu chưa vào ga hoặc đã đóng cửa.", "The train is not boarding now.", "現在、この電車には乗車できません。")); return; }
            if (_carriageInterior != null) _carriageInterior.SetActive(true);
            Teleport(player, carriageSpawn);
            ShowMessage(Localize("Đã lên tàu. Hãy tìm chỗ ngồi.", "You boarded the train. Please find a seat.", "乗車しました。席をお探しください。"));
        }

        private void StartRide()
        {
            if (_riding) { ShowMessage("Tau dang di den ga Midori."); return; }
            _riding = true;
            _rideRemaining = rideDuration;
            ShowMessage("Tau khoi hanh. Co the nhin qua cua so hoac noi chuyen voi hanh khach.");
        }

        private void Leave(GameObject player)
        {
            if (_riding) { ShowMessage("Tau dang chay. Vui long doi den ga."); return; }
            Teleport(player, platformSpawn);
            if (_carriageInterior != null) _carriageInterior.SetActive(false);
            _hasTicket = false;
            _gatePassed = false;
            ShowMessage("Da xuong tau an toan.");
        }

        private void TalkPassenger()
        {
            string line = _riding
                ? "Hanh khach: Midori wa mada desu ka? / Ga Midori van chua den a?"
                : "Hanh khach: Kono densha wa Midori ni ikimasu. / Tau nay di den Midori.";
            ShowMessage(line, 7f);
            PlayerStatus.Instance?.AddKnowledge(2);
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
        }

        private void BuildTravelHud()
        {
            var canvasObject = new GameObject("StationTravelHUD");
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 80;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panel = new GameObject("RoutePanel");
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(500f, 72f);
            panel.AddComponent<Image>().color = new Color(0.025f, 0.055f, 0.075f, 0.94f);
            _routeText = CreateText(panel.transform, 22f, TextAlignmentOptions.Center);

            GameObject wallet = new GameObject("StationWallet");
            wallet.transform.SetParent(canvasObject.transform, false);
            var walletRect = wallet.AddComponent<RectTransform>();
            walletRect.anchorMin = walletRect.anchorMax = new Vector2(0f, 1f);
            walletRect.pivot = new Vector2(0f, 1f);
            walletRect.anchoredPosition = new Vector2(24f, -24f);
            walletRect.sizeDelta = new Vector2(230f, 56f);
            wallet.AddComponent<Image>().color = new Color(0.025f, 0.045f, 0.06f, 0.9f);
            _walletText = CreateText(wallet.transform, 21f, TextAlignmentOptions.Center);

            GameObject message = new GameObject("TravelMessage");
            message.transform.SetParent(canvasObject.transform, false);
            var messageRect = message.AddComponent<RectTransform>();
            messageRect.anchorMin = messageRect.anchorMax = new Vector2(0.5f, 0f);
            messageRect.pivot = new Vector2(0.5f, 0f);
            messageRect.anchoredPosition = new Vector2(0f, 32f);
            messageRect.sizeDelta = new Vector2(900f, 64f);
            message.AddComponent<Image>().color = new Color(0.025f, 0.035f, 0.045f, 0.92f);
            _messagePanel = message;
            _messageText = CreateText(message.transform, 20f, TextAlignmentOptions.Center);
            _messagePanel.SetActive(false);

            GameObject help = new GameObject("StationHelp");
            help.transform.SetParent(canvasObject.transform, false);
            var helpRect = help.AddComponent<RectTransform>();
            helpRect.anchorMin = helpRect.anchorMax = Vector2.zero;
            helpRect.pivot = Vector2.zero;
            helpRect.anchoredPosition = new Vector2(24f, 24f);
            helpRect.sizeDelta = new Vector2(570f, 66f);
            help.AddComponent<Image>().color = new Color(0.025f, 0.045f, 0.06f, 0.9f);
            _stationHelpText = CreateText(help.transform, 17f, TextAlignmentOptions.Left);
            _stationHelpText.text = Localize("F Tương tác | B Balo | M Bản đồ | Esc Cài đặt", "F Interact | B Bag | M Map | Esc Settings", "F 調べる | B バッグ | M 地図 | Esc 設定");
            BuildTicketPanel(canvasObject.transform);
        }

        private void BuildTicketPanel(Transform parent)
        {
            _ticketPanel = new GameObject("TicketMachinePanel");
            _ticketPanel.transform.SetParent(parent, false);
            var rect = _ticketPanel.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 330f);
            _ticketPanel.AddComponent<Image>().color = new Color(0.025f, 0.045f, 0.06f, 0.98f);

            TextMeshProUGUI title = CreateText(_ticketPanel.transform, 28f, TextAlignmentOptions.Top);
            title.rectTransform.offsetMin = new Vector2(24f, 250f);
            title.rectTransform.offsetMax = new Vector2(-24f, -24f);
            title.text = Localize("CHỌN GA ĐẾN", "SELECT DESTINATION", "行き先を選択");

            CreateTicketButton("MIDORI", 180, 175f);
            CreateTicketButton("SHINJUKU", 260, 105f);
            CreateTicketButton("ASAKUSA", 320, 35f);
            _ticketPanel.SetActive(false);
        }

        private void CreateTicketButton(string destination, int fare, float y)
        {
            var buttonObject = new GameObject("Ticket_" + destination);
            buttonObject.transform.SetParent(_ticketPanel.transform, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(500f, 54f);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.32f, 0.42f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            TextMeshProUGUI label = CreateText(buttonObject.transform, 20f, TextAlignmentOptions.Center);
            label.text = $"{destination}     ¥{fare}";
            button.onClick.AddListener(() => PurchaseTicket(destination, fare));
        }

        private static TextMeshProUGUI CreateText(Transform parent, float size, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 8f);
            rect.offsetMax = new Vector2(-18f, -8f);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.94f, 0.96f, 0.98f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.raycastTarget = false;
            return text;
        }

        private void RefreshHud()
        {
            if (_routeText == null) return;
            if (_walletText != null)
                _walletText.text = PlayerInventory.Instance != null ? $"¥ {PlayerInventory.Instance.Yen:N0}" : "¥ --";
            float wait = SecondsUntilBoardingEnds();
            string stage = _riding
                ? Localize($"ĐANG ĐI | {_rideRemaining:0}s", $"EN ROUTE | {_rideRemaining:0}s", $"走行中 | {_rideRemaining:0}秒")
                : IsTrainBoarding()
                    ? Localize($"ĐANG ĐÓN KHÁCH | {wait:0}s", $"BOARDING | {wait:0}s", $"乗車中 | {wait:0}秒")
                    : Localize($"CHUYẾN KẾ | {wait:0}s", $"NEXT TRAIN | {wait:0}s", $"次の電車 | {wait:0}秒");
            _routeText.text = $"SAKURA  >  {_destination}\n<size=70%><color=#78C7D4>{stage}</color></size>";
        }

        private bool IsTrainBoarding() => Mathf.Repeat(Time.time, serviceInterval) < boardingWindow;

        private float SecondsUntilBoardingEnds()
        {
            float phase = Mathf.Repeat(Time.time, serviceInterval);
            return phase < boardingWindow ? boardingWindow - phase : serviceInterval - phase;
        }

        private void CachePlatformTrain()
        {
            string[] names = { "HighSpeed_Front", "HighSpeed_Wagon_A", "HighSpeed_Wagon_B" };
            foreach (string trainName in names)
            {
                GameObject item = GameObject.Find(trainName);
                if (item != null) _platformTrain.Add((item.transform, item.transform.position));
            }
        }

        private void UpdatePlatformTrain()
        {
            float phase = Mathf.Repeat(Time.time, serviceInterval);
            float offset;
            if (phase < boardingWindow) offset = 0f;
            else if (phase < boardingWindow + 12f) offset = Mathf.SmoothStep(0f, trainTravelDistance, (phase - boardingWindow) / 12f);
            else if (phase < serviceInterval - 12f) offset = trainTravelDistance;
            else offset = Mathf.SmoothStep(-trainTravelDistance, 0f, (phase - (serviceInterval - 12f)) / 12f);

            foreach (var item in _platformTrain)
                if (item.transform != null) item.transform.position = item.platformPosition + Vector3.right * offset;
        }

        private static string Localize(string vi, string en, string ja)
        {
            return GameServices.TryGet(out GameSettingsService settings) ? settings.Text(vi, en, ja) : vi;
        }

        private void HandleInteractableChanged(IInteractable interactable)
        {
            if (_stationHelpText == null) return;
            if (interactable == null)
            {
                _stationHelpText.text = Localize("F Tương tác | B Balo | M Bản đồ | Esc Cài đặt", "F Interact | B Bag | M Map | Esc Settings", "F 調べる | B バッグ | M 地図 | Esc 設定");
                return;
            }

            bool japanese = GameServices.TryGet(out GameSettingsService settings) && settings.Language == GameLanguage.Japanese;
            _stationHelpText.text = $"[F] {(japanese ? interactable.GetPromptJa() : interactable.GetpromptEn())}";
        }

        private void OnDestroy()
        {
            if (_interactionDetector != null) _interactionDetector.OnInteractableChanged -= HandleInteractableChanged;
        }

        private void ShowMessage(string message, float seconds = 4f)
        {
            if (_messagePanel != null) _messagePanel.SetActive(!string.IsNullOrEmpty(message));
            if (_messageText != null) _messageText.text = message;
            _messageUntil = Time.unscaledTime + seconds;
        }

        private static void PlayConfirm()
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(GameAudioCue.UiConfirm, 0.75f);
        }
    }

    [RequireComponent(typeof(Collider))]
    public sealed class StationTravelInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private StationTravelController controller;
        [SerializeField] private StationAction action;
        [SerializeField] private string promptJa = "利用する";
        [SerializeField] private string promptEn = "Su dung";

        public void Configure(StationTravelController travelController, StationAction stationAction, string ja, string en)
        {
            controller = travelController;
            action = stationAction;
            promptJa = ja;
            promptEn = en;
        }

        public string GetPromptJa() => promptJa;
        public string GetpromptEn() => promptEn;
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => controller?.Execute(action, player);
    }
}
