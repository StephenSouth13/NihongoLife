using UnityEngine;

namespace NihongoLife.Audio
{
    public enum GameAudioCue { UiClick, UiConfirm, UiBack, UiError, DoorOpen, DoorClose, Pickup, Drop, Portal, Punch, FootstepConcrete, FootstepWood, FootstepCarpet, UiHover, UiOpen, UiClose, UiTick }

    [CreateAssetMenu(fileName = "GameAudioCatalog", menuName = "NihongoLife/Audio Catalog")]
    public class GameAudioCatalog : ScriptableObject
    {
        public AudioClip uiClick, uiConfirm, uiBack, uiError;
        [Tooltip("Soft tick when the pointer moves over a button.")] public AudioClip uiHover;
        [Tooltip("A popup / panel opens.")] public AudioClip uiOpen;
        [Tooltip("A popup / panel closes.")] public AudioClip uiClose;
        [Tooltip("Slider or selection step.")] public AudioClip uiTick;
        public AudioClip doorOpen, doorClose, pickup, drop, portal, punch;
        public AudioClip[] footstepsConcrete, footstepsWood, footstepsCarpet;
    }
}
