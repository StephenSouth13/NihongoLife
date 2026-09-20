using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Audio
{
    /// <summary>
    /// Plays the main-menu theme through the shared AudioService (so the BGM slider controls it).
    /// Uses Resources/Audio/Music/menu_bgm when present; otherwise a procedurally generated
    /// Japanese-style loop. Call FadeOut before leaving the menu.
    /// </summary>
    public class MenuMusicController : MonoBehaviour
    {
        private const string ResourcePath = "Audio/Music/menu_bgm";
        [SerializeField, Range(0f, 1f)] private float clipVolume = 0.7f;

        private static AudioClip _generated;
        private bool _stopped;

        private void Start()
        {
            AudioClip authored = Resources.Load<AudioClip>(ResourcePath);
            if (authored != null)
            {
                Play(authored);
                return;
            }

            if (_generated != null)
            {
                Play(_generated);
                return;
            }

            StartCoroutine(ProceduralMenuMusic.Generate(clip =>
            {
                _generated = clip;
                if (!_stopped) Play(clip);
            }));
        }

        public void FadeOut(float seconds)
        {
            _stopped = true;
            if (GameServices.TryGet(out IAudioService audio)) audio.FadeOutBGM(seconds);
        }

        private void Play(AudioClip clip)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayBGM(clip, true, clipVolume);
        }
    }
}
