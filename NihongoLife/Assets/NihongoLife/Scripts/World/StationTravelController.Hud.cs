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
    /// <summary>Partial of StationTravelController: station and onboard HUD. The MonoBehaviour entry point stays StationTravelController.cs.</summary>
    public sealed partial class StationTravelController : MonoBehaviour
    {
        // ─────────── HUD ───────────

        private void BuildTravelHud()
        {
            EnsureEventSystem();
            _font = NLUi.ResolveFont();
            HUDUI sharedHud = SceneRefs.Find<HUDUI>();
            Transform parent = sharedHud != null ? sharedHud.transform : transform;
            var canvas = NLUi.CreateCanvas("StationTravelHUD", 120, parent);
            _hudRoot = (RectTransform)canvas.transform;

            _stationHud = new GameObject("StationLayer", typeof(RectTransform)).GetComponent<RectTransform>();
            _stationHud.SetParent(_hudRoot, false);
            NLUi.Stretch(_stationHud);

            var route = NLUi.Panel(_stationHud, "RouteCard", NLUi.Ink, new RectOffset(20, 20, 12, 12), 2f);
            // Bottom-right, clear of the objective chip (top-left), the feed (top-centre) and live toasts (top-right).
            NLUi.Anchor(route, new Vector2(1f, 0f), new Vector2(-24f, 118f), new Vector2(300f, 0f));
            NLUi.FitContent(route);
            _routeText = NLUi.Label(route, "Route", "ひばり  →  ？", 24f, NLUi.Text, _font, FontStyles.Bold);
            _statusText = NLUi.Label(route, "Status", "", 17f, NLUi.Muted, _font);

            var steps = NLUi.Group(_stationHud, "TravelSteps", false, 8f, TextAnchor.MiddleCenter, false);
            NLUi.Anchor(steps, new Vector2(0.5f, 1f), new Vector2(120f, -18f), new Vector2(560f, 44f));
            ((HorizontalLayoutGroup)steps.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            string[] labels = { StepLabel(0), StepLabel(1), StepLabel(2), StepLabel(3) };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var pill = NLUi.Pill(steps, "Step", labels[i], _font, NLUi.Card, NLUi.Muted, 16f);
                var pillText = pill.GetComponentInChildren<TextMeshProUGUI>();
                LocText.Set(pillText, () => StepLabel(index));
                _steps.Add((pill.GetComponent<Image>(), pillText));
            }

            // Announcements already go to HudFeed. A second 980px card here sat on the prompt and the status dock.
            BuildOnboardHud();
            BuildTicketPanel();
        }

        /// <summary>Japanese-train style onboard display: an amber LED board with the next stop and a countdown,
        /// a line diagram (travelled part green, the ticket's stop starred, a moving train marker), info chips
        /// and a clickable window-view action. A caption frames the window view.</summary>
        private void BuildOnboardHud()
        {
            _onboardHud = NLUi.Panel(_hudRoot, "OnboardHUD", NLUi.Ink, new RectOffset(18, 18, 10, 12), 8f);
            NLUi.Anchor(_onboardHud, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(700f, 0f));
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
            diagram.GetComponent<LayoutElement>().preferredHeight = 72f;
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
            _viewButton = NLUi.Button(chips, "WindowViewButton", "Q · " + NihongoLife.Core.Loc.T("Ngắm cảnh", "Look outside", "まどの そとを みる"), _font, () => SetWindowView(!IsWindowViewOpen), new Color(0.13f, 0.32f, 0.45f), 16f, null, 40f);
            NLUi.Size(_viewButton, 190f, 40f);
            _onboardHint = NLUi.Label(chips, "Hint", "", 15f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            LocText.Set(_onboardHint, "F · Nói chuyện với hành khách", "F · Talk to a passenger", "F · じょうきゃくに はなしかける");
            _onboardHud.gameObject.SetActive(false);

            _windowCaption = NLUi.Panel(_hudRoot, "WindowCaption", new Color(0.02f, 0.03f, 0.05f, 0.82f), new RectOffset(28, 28, 10, 12), 2f);
            NLUi.Anchor(_windowCaption, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(620f, 0f));
            NLUi.FitContent(_windowCaption);
            LocText.Set(NLUi.Label(_windowCaption, "Title", "", 24f, NLUi.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center), "Ngắm cảnh qua cửa sổ", "The view outside", "まどの そと");
            LocText.Set(NLUi.Label(_windowCaption, "Hint", "", 15f, NLUi.Muted, _font, FontStyles.Normal, TextAlignmentOptions.Center), "Q · quay lại toa tàu", "Q · back to the carriage", "Q · 車内にもどる");
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
            NLUi.Size(LocText.Set(NLUi.Label(header, "Title", "", 28f, NLUi.Text, _font, FontStyles.Bold), "Máy bán vé", "Ticket machine", "けんばいき") as TextMeshProUGUI, flexibleWidth: 1f);
            _ticketWallet = NLUi.Pill(header, "Wallet", "¥0", _font, new Color(1f, 1f, 1f, 0.08f), NLUi.Gold, 19f).GetComponentInChildren<TextMeshProUGUI>();
            LocText.Set(NLUi.Label(_ticketWindow, "Hint", "", 18f, NLUi.Muted, _font), () => NihongoLife.Core.Loc.F("Chọn ga đến (tuyến sân ga số {0}).", "Choose your destination (platform {0} line).", "いきさきを えらんで ください（{0}ばんせん）。", Platform));
            for (int i = 1; i < Line.Length; i++)
            {
                Stop stop = Line[i];
                string id = stop.Id;
                NLUi.Button(_ticketWindow, "Ticket_" + id.ToUpperInvariant(), $"{stop.Ja}  ·  {stop.Vi}   —   {stop.Place}   —   {NihongoLife.Core.Loc.Money(stop.Price)}", _font,
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
            if (PlayerInventory.Instance != null) _ticketWallet.text = $"{NihongoLife.Core.Loc.Money(PlayerInventory.Instance.Yen)}";
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
            var player = SceneRefs.Find<PlayerController>();
            if (player != null) player.InputLocked = locked;
            Cursor.lockState = CursorLockMode.None; // Sims-style: the cursor stays free in gameplay
            Cursor.visible = true;
        }

        private void RefreshHud()
        {
            if (_stationHud != null && _stationHud.gameObject.activeSelf == _onboard) _stationHud.gameObject.SetActive(!_onboard);
            if (_onboardHud != null && _onboardHud.gameObject.activeSelf != _onboard) _onboardHud.gameObject.SetActive(_onboard);
            NihongoLife.UI.HudFeed.SetClearance(_onboard ? 214f : 92f);
            if (_routeText == null) return;
            if (_onboard)
            {
                if (_ledText != null)
                    _ledText.text = _atStop ? $"{NextStop.Ja}  <size=62%><color=#FFD08A>{NihongoLife.Core.Loc.F("Ga {0}", "{0} Station", "{0}", NextStop.Vi)}</color></size>" : $"つぎは  {NextStop.Ja}  <size=62%><color=#FFD08A>{NihongoLife.Core.Loc.F("Tiếp: {0}", "Next · {0}", "{0}", NextStop.Vi)}</color></size>";
                if (_ledTimer != null)
                    _ledTimer.text = _atStop ? NihongoLife.Core.Loc.T("đang dừng", "stopped", "停車中") : NihongoLife.Core.Loc.F("còn {0}s", "{0}s to go", "あと{0}秒", Mathf.CeilToInt((1f - _legProgress) * LegSeconds));
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
                if (_ticketChip != null) _ticketChip.text = TicketStop != null ? $"きっぷ  {TicketStop.Ja} · {NihongoLife.Core.Loc.Money(TicketStop.Price)}" : "きっぷ —";
                if (_doorChip != null) _doorChip.text = NihongoLife.Core.Loc.T("Cửa ra bên trái", "Exit on the left", "おでぐちは ひだりがわ");
                return;
            }
            _routeText.text = HasTicket ? $"ひばり  →  {TicketStop.Ja}" : "ひばり  →  ？";
            string status = IsTrainBoarding ? (_gatePassed ? NihongoLife.Core.Loc.F("Tàu đang đợi ở sân ga số {0} — lên tàu!", "The train is waiting at platform {0} — get on!", "{0}ばんせんで でんしゃが まっています。のりましょう！", Platform) : NihongoLife.Core.Loc.T("Tàu đang đón khách", "The train is boarding", "じょうしゃ ちゅう"))
                : NihongoLife.Core.Loc.F("Chuyến kế tiếp sau {0}s", "Next train in {0}s", "つぎの でんしゃまで {0}秒", SecondsUntilArrival().ToString("0"));
            _statusText.text = HasTicket ? $"{status}   ·   {NihongoLife.Core.Loc.Yen(TicketStop.Price)}" : status + "\n" + NihongoLife.Core.Loc.T("Hỏi Kimura ở quầy hoặc dùng máy bán vé", "Ask Kimura at the counter or use the ticket machine", "まどぐちの きむらさんに きくか、けんばいきを つかって ください");
            int step = _gatePassed ? 3 : HasTicket ? 2 : 1;
            for (int i = 0; i < _steps.Count; i++)
            {
                bool done = i + 1 < step, current = i + 1 == step;
                _steps[i].bg.color = done ? new Color(0.16f, 0.4f, 0.27f) : current ? NLUi.Gold : NLUi.Card;
                _steps[i].text.color = current ? NLUi.Ink : done ? NLUi.Text : NLUi.Muted;
            }
        }

        private static string StepLabel(int index) => index switch
        {
            0 => "1  " + NihongoLife.Core.Loc.T("Mua vé", "Buy a ticket", "きっぷを かう"),
            1 => "2  " + NihongoLife.Core.Loc.T("Qua cổng", "Ticket gate", "かいさつ"),
            2 => "3  " + NihongoLife.Core.Loc.F("Sân ga {0} · Chờ tàu", "Platform {0} · Wait", "{0}ばんせんで まつ", Platform),
            _ => "4  " + NihongoLife.Core.Loc.T("Lên tàu", "Board", "のる"),
        };
    }
}
