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
    /// <summary>Partial of StationTravelController: platform fixtures and the moving train. The MonoBehaviour entry point stays StationTravelController.cs.</summary>
    public sealed partial class StationTravelController : MonoBehaviour
    {
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
            var clerk = SceneRefs.FindNamed("StationTicketClerk");
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
            if (firstTicketMachine != null) firstTicketMachine.Configure(this, StationAction.BuyTicket, "きっぷを かう", NihongoLife.Core.Loc.Triple("Mua vé (máy bán vé)", "Buy a ticket (machine)", "きっぷを かう（けんばいき）"));

            foreach (var interactable in FindObjectsByType<StationTravelInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (interactable == null || interactable.gameObject.scene != gameObject.scene) continue;
                switch (interactable.Action)
                {
                    case StationAction.BoardTrain: interactable.Configure(this, StationAction.BoardTrain, "でんしゃに のる", NihongoLife.Core.Loc.Triple("Lên tàu (sân ga 2)", "Board the train (platform 2)", "でんしゃに のる（2ばんせん）")); break;
                    case StationAction.LeaveTrain: interactable.Configure(this, StationAction.LeaveTrain, "でんしゃを おりる", NihongoLife.Core.Loc.Triple("Xuống tàu ở ga này", "Get off at this station", "この駅で おりる")); break;
                    case StationAction.TalkPassenger: interactable.Configure(this, StationAction.TalkPassenger, "はなしかける", NihongoLife.Core.Loc.Triple("Nói chuyện với hành khách", "Talk to a passenger", "じょうきゃくに はなしかける")); break;
                    case StationAction.StartRide: interactable.Configure(this, StationAction.StartRide, "まどの そとを みる", NihongoLife.Core.Loc.Triple("Ngắm cảnh qua cửa sổ (Q)", "Look out of the window (Q)", "まどの そとを みる（Q）")); break;
                }
            }

            var scanner = SceneRefs.FindNamed("GateScanner");
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
            scannerInteractable.Configure(this, StationAction.PassGate, "かいさつを とおる", NihongoLife.Core.Loc.Triple("Qua cổng soát vé", "Go through the ticket gate", "かいさつを とおる"));
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
            foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item == null || item.gameObject.scene != gameObject.scene ||
                    !item.name.StartsWith("HighspeedTrain_", System.StringComparison.OrdinalIgnoreCase)) continue;
                // Only the top object of each car: a child with the same prefix would otherwise move twice.
                bool nested = false;
                for (var p = item.parent; p != null && !nested; p = p.parent)
                    nested = p.name.StartsWith("HighspeedTrain_", System.StringComparison.OrdinalIgnoreCase);
                if (nested) continue;

                _platformTrain.Add((item, item.position));
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
    }
}
