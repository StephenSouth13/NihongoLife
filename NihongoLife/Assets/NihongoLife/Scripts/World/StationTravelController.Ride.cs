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
    /// <summary>Partial of StationTravelController: boarding, the ride, and alighting. The MonoBehaviour entry point stays StationTravelController.cs.</summary>
    public sealed partial class StationTravelController : MonoBehaviour
    {
        // ─────────── Ride ───────────

        private void Board(GameObject player)
        {
            if (_onboard) return;
            if (!_gatePassed) { Announce("さきに かいさつを とおって ください。", NihongoLife.Core.Loc.Gloss("Hãy mua vé và qua cổng soát vé trước.", "Buy a ticket and go through the ticket gate first.")); Cue(GameAudioCue.UiError); return; }
            if (!IsTrainBoarding) { Announce("でんしゃは まだ きません。", NihongoLife.Core.Loc.Gloss($"Tàu chưa vào ga — còn khoảng {SecondsUntilArrival():0} giây.", $"The train isn't here yet — about {SecondsUntilArrival():0} seconds.")); return; }
            if (_carriageInterior != null) _carriageInterior.SetActive(true);
            ShuffleCommuters();
            Teleport(player, carriageSpawn);
            var camera = SceneRefs.Find<Cameras.ThirdPersonCameraController>();
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
            Announce("ドアが しまります。ご注意ください。", NihongoLife.Core.Loc.Gloss("Cửa sắp đóng. Xin chú ý.", "The doors are closing. Please be careful."), 3f);
            for (_nextStop = 1; _nextStop < Line.Length; _nextStop++)
            {
                Stop stop = Line[_nextStop];
                _riding = true;
                _atStop = false;
                _legProgress = 0f;
                if (_windowScenery != null) _windowScenery.Throttle = 1f;
                yield return new WaitForSeconds(1.5f);
                Announce($"つぎは {stop.Ja}、{stop.Ja}です。", NihongoLife.Core.Loc.Gloss($"Ga tiếp theo: {stop.Vi} ({stop.Place}). Bấm Q để ngắm cảnh, F để nói chuyện.", $"Next stop: {stop.Vi} ({stop.Place}). Press Q to look outside, F to talk."));
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
                Announce($"{stop.Ja}、{stop.Ja}です。おでぐちは ひだりがわです。", NihongoLife.Core.Loc.Gloss($"Đã tới ga {stop.Vi}. Cửa ra bên trái.", $"Arrived at {stop.Vi}. Doors open on the left."));
                Cue(GameAudioCue.UiConfirm);
                if (_nextStop == _ticketStop)
                {
                    yield return new WaitForSeconds(2f);
                    AlightAt(_nextStop);
                    yield break;
                }
                yield return new WaitForSeconds(DwellSeconds);
                if (_alighting) yield break;
                Announce("ドアが しまります。", NihongoLife.Core.Loc.Gloss($"Cửa đóng — vé của bạn đi tới {TicketStop?.Vi}.", $"Doors closing — your ticket goes to {TicketStop?.Vi}."), 3f);
            }
        }

        private void Leave()
        {
            if (!_onboard) return;
            if (_riding) { Announce("でんしゃが はしって います。", NihongoLife.Core.Loc.Gloss("Tàu đang chạy — đợi tới ga đã.", "The train is moving — wait for the next station.")); return; }
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
            if (!GameServices.TryGet(out flow)) flow = SceneRefs.Find<SceneFlowController>();
            if (flow == null)
            {
                Debug.LogError("[StationTravelController] No SceneFlowController — cannot leave the train.");
                _alighting = false;
                return;
            }
            flow.TransferZone(stop.Scene, stop.Spawn, NihongoLife.Core.Loc.Triple($"Ga {stop.Vi}", $"{stop.Vi} Station", $"{stop.Ja}えき"), () => OnArrivedAt(stopId));
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
            var hud = SceneRefs.Find<HUDUI>();
            if (hud == null) return;
            var font = NLUi.ResolveFont();
            var canvas = NLUi.CreateCanvas("TrainArrivalBanner", 130, hud.transform);
            var card = NLUi.Panel((RectTransform)canvas.transform, "Card", NLUi.Ink, new RectOffset(30, 30, 16, 16), 4f);
            NLUi.Anchor(card, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(760f, 0f));
            NLUi.FitContent(card);
            NLUi.Label(card, "Title", $"{stop.Ja}えき に つきました", 30f, NLUi.Gold, font, FontStyles.Bold, TextAlignmentOptions.Center);
            NLUi.Label(card, "Sub", NihongoLife.Core.Loc.F("Đã tới ga {0} — {1}. Cửa ra đưa bạn về phố.", "Arrived at {0} — {1}. The exit takes you into town.", "{0}駅 — {1}。出口から町に出られます。", stop.Name, stop.Place), 19f, NLUi.Text, font, FontStyles.Normal, TextAlignmentOptions.Center);
            Destroy(canvas.gameObject, 6f);
        }

        public void SetWindowView(bool open)
        {
            if (_windowCamera == null) return;
            if (open && !_onboard) return;
            _windowCamera.enabled = open;
            _windowCamera.depth = 5f;
            if (_windowCaption != null) _windowCaption.gameObject.SetActive(open);
            if (_viewButton != null) _viewButton.GetComponentInChildren<TextMeshProUGUI>().text = "Q · " + (open ? NihongoLife.Core.Loc.T("Về toa tàu", "Back to the carriage", "車内にもどる") : NihongoLife.Core.Loc.T("Ngắm cảnh", "Look outside", "まどの そとを みる"));
        }

        /// <summary>Different commuters every trip; the ones the player can talk to always ride.</summary>
        private void ShuffleCommuters()
        {
            if (_carriageInterior == null) return;
            foreach (var passenger in _carriageInterior.GetComponentsInChildren<SeatedPassenger>(true))
                passenger.gameObject.SetActive(passenger.AlwaysOnBoard || Random.value < 0.6f);
        }
    }
}
