using NihongoLife.Interaction;
using UnityEngine;
using NihongoLife.Core;

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
        public string GetpromptEn() => NihongoLife.Core.Loc.F("Nói chuyện với {0}", "Talk to the {0}", "{0}に話しかける", Name);

        private string Name => displayName switch
        {
            "bà cụ" => NihongoLife.Core.Loc.T("bà cụ", "elderly woman", "おばあさん"),
            "nhân viên văn phòng" => NihongoLife.Core.Loc.T("nhân viên văn phòng", "office worker", "かいしゃいん"),
            "học sinh" => NihongoLife.Core.Loc.T("học sinh", "student", "がくせい"),
            "Hành khách" => NihongoLife.Core.Loc.T("hành khách", "passenger", "じょうきゃく"),
            _ => displayName,
        };
        public Transform GetTransform() => transform;

        public void Interact(GameObject player)
        {
            var station = SceneRefs.Find<StationTravelController>();
            if (station != null) station.TalkToPassenger(passengerId);
        }
    }
}
