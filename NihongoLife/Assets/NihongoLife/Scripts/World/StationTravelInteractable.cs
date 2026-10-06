using UnityEngine;
using NihongoLife.Interaction;

namespace NihongoLife.World
{
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
