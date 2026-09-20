using UnityEngine;

namespace NihongoLife.Audio
{
    public enum GameAudioCue { UiClick, UiConfirm, UiBack, UiError, DoorOpen, DoorClose, Pickup, Drop, Portal, Punch, FootstepConcrete, FootstepWood, FootstepCarpet }

    [CreateAssetMenu(fileName = "GameAudioCatalog", menuName = "NihongoLife/Audio Catalog")]
    public class GameAudioCatalog : ScriptableObject
    {
        public AudioClip uiClick, uiConfirm, uiBack, uiError;
        public AudioClip doorOpen, doorClose, pickup, drop, portal, punch;
        public AudioClip[] footstepsConcrete, footstepsWood, footstepsCarpet;
    }
}
