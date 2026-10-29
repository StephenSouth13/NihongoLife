using NihongoLife.Interaction;
using UnityEngine;

namespace NihongoLife.Music
{
    public enum MusicSpotKind { Piano, Guitar, Theory, Gallery, Stage, DjBooth, Easel, Counter }

    /// <summary>A place to use in the Hibiki music shop (55_MusicShop): an instrument, the stage, the DJ booth, the
    /// easel, the gallery wall or the counter. An instrument or the stage may have a seat: the player is placed there
    /// and plays. What it does: MusicShop.Use.</summary>
    public sealed class MusicSpot : MonoBehaviour, IInteractable, IInteractionPriority
    {
        [SerializeField] private MusicSpotKind kind;
        [SerializeField] private bool hasSeat;
        [SerializeField] private Vector3 seat;
        [SerializeField] private float seatYaw;
        public MusicSpotKind Kind => kind;
        public bool HasSeat => hasSeat;
        public Vector3 Seat => seat;
        public float SeatYaw => seatYaw;

        public void Configure(MusicSpotKind spotKind) => kind = spotKind;

        public void Configure(MusicSpotKind spotKind, Vector3 seatPosition, float yaw)
        {
            kind = spotKind;
            hasSeat = true;
            seat = seatPosition;
            seatYaw = yaw;
        }

        public float InteractionPriority => 0.6f;
        public string GetPromptJa() => MusicShop.PromptJa(kind);
        public string GetpromptEn() => MusicShop.Prompt(kind);
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => MusicShop.Use(kind, this);
    }
}
