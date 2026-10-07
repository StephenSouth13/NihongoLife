using NihongoLife.Interaction;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>F on a seated commuter: opens that passenger's conversation through the station controller.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class TrainPassengerTalk : MonoBehaviour, IInteractable
    {
        [SerializeField] private string passengerId = "student";
        [SerializeField] private string displayName = "Hành khách";

        public string PassengerId => passengerId;

        public void Configure(string id, string label)
        {
            passengerId = id;
            displayName = label;
        }

        public string GetPromptJa() => "はなしかける";
        public string GetpromptEn() => "Nói chuyện với " + displayName;
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            var station = FindFirstObjectByType<StationTravelController>();
            if (station != null) station.TalkToPassenger(passengerId);
        }
    }
}
