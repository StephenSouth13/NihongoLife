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

        private readonly List<Transform> _scenery = new();
        private bool _hasTicket;
        private bool _gatePassed;
        private bool _riding;
        private float _rideRemaining;
        private Canvas _canvas;
        private TextMeshProUGUI _routeText;
        private TextMeshProUGUI _messageText;
        private float _messageUntil;

        public void Configure(Transform platform, Transform carriage, Transform movingScenery)
        {
            platformSpawn = platform;
            carriageSpawn = carriage;
            sceneryRoot = movingScenery;
        }

        private void Awake()
        {
            if (sceneryRoot != null)
                foreach (Transform child in sceneryRoot) _scenery.Add(child);
            BuildTravelHud();
            RefreshHud();
        }

        private void Update()
        {
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

            if (_messageText != null && Time.unscaledTime > _messageUntil) _messageText.text = string.Empty;
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
            if (_hasTicket) { ShowMessage("Ban da co ve den ga Midori."); return; }
            if (PlayerInventory.Instance == null || !PlayerInventory.Instance.SpendYen(ticketPrice))
            {
                ShowMessage($"Khong du tien. Gia ve: Y{ticketPrice}.");
                return;
            }
            _hasTicket = true;
            ShowMessage("Da mua ve. Hay qua cong soat ve.");
            PlayConfirm();
        }

        private void PassGate()
        {
            if (!_hasTicket) { ShowMessage("Can mua ve truoc khi qua cong."); return; }
            _gatePassed = true;
            ShowMessage("Ve hop le. Tau den san so 1.");
            PlayConfirm();
        }

        private void Board(GameObject player)
        {
            if (!_gatePassed) { ShowMessage("Hay mua ve va qua cong soat truoc."); return; }
            Teleport(player, carriageSpawn);
            ShowMessage("Da len tau. Tim ghe ngoi va bat dau hanh trinh.");
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
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(560f, 72f);
            panel.AddComponent<Image>().color = new Color(0.025f, 0.055f, 0.075f, 0.94f);
            _routeText = CreateText(panel.transform, 22f, TextAlignmentOptions.Center);

            GameObject message = new GameObject("TravelMessage");
            message.transform.SetParent(canvasObject.transform, false);
            var messageRect = message.AddComponent<RectTransform>();
            messageRect.anchorMin = messageRect.anchorMax = new Vector2(0.5f, 0f);
            messageRect.pivot = new Vector2(0.5f, 0f);
            messageRect.anchoredPosition = new Vector2(0f, 32f);
            messageRect.sizeDelta = new Vector2(900f, 64f);
            message.AddComponent<Image>().color = new Color(0.025f, 0.035f, 0.045f, 0.92f);
            _messageText = CreateText(message.transform, 20f, TextAlignmentOptions.Center);
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
            string stage = _riding ? $"DANG DI  |  {_rideRemaining:0}s" : _gatePassed ? "SAN SO 1  |  MOI LEN TAU" : _hasTicket ? "VE MIDORI  |  QUA CONG" : $"MUA VE  |  Y{ticketPrice}";
            _routeText.text = $"SAKURA  >  MIDORI\n<size=70%><color=#78C7D4>{stage}</color></size>";
        }

        private void ShowMessage(string message, float seconds = 4f)
        {
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
