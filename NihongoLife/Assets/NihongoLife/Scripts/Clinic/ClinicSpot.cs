using NihongoLife.Interaction;
using UnityEngine;

namespace NihongoLife.Clinic
{
    /// <summary>A patient spot in Hibari Clinic (reception, doctor, medicine window, scale, eye chart).</summary>
    public sealed class ClinicSpot : MonoBehaviour, IInteractable, IInteractionPriority
    {
        [SerializeField] private ClinicSpotKind kind;
        public ClinicSpotKind Kind => kind;
        public void Configure(ClinicSpotKind spotKind) => kind = spotKind;
        public float InteractionPriority => 0.6f;
        public string GetPromptJa() => kind switch
        {
            ClinicSpotKind.Reception => "うけつけ",
            ClinicSpotKind.Doctor => "しんさつしつ",
            ClinicSpotKind.Pharmacy => "おくすり",
            ClinicSpotKind.Scale => "しんちょう",
            _ => "しりょく",
        };
        public string GetpromptEn() => ClinicVisit.Prompt(kind);
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => ClinicVisit.Use(kind);
    }
}
