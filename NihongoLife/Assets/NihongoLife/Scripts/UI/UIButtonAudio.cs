using NihongoLife.Audio;
using NihongoLife.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NihongoLife.UI
{
    public class UIButtonAudio : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameAudioCue cue = GameAudioCue.UiClick;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, 0.7f);
        }
    }
}
