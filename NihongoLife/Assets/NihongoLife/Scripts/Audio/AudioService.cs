using System.Collections;
using UnityEngine;

namespace NihongoLife.Audio
{
    public class AudioService : MonoBehaviour, IAudioService
    {
        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private AudioSource _voiceSource;

        private float _bgmVolume = 0.5f;
        private float _sfxVolume = 0.8f;
        private GameAudioCatalog _catalog;
        private int _footstepIndex;
        private Coroutine _bgmFade;
        private float _bgmClipVolume = 1f;

        public bool IsBgmPlaying => _bgmSource != null && _bgmSource.isPlaying;

        public void Initialize()
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _voiceSource = gameObject.AddComponent<AudioSource>();
            _voiceSource.playOnAwake = false;

            // Load saved settings if any
            _bgmVolume = PlayerPrefs.GetFloat("Volume_BGM", 0.5f);
            _sfxVolume = PlayerPrefs.GetFloat("Volume_SFX", 0.8f);

            _bgmSource.volume = _bgmVolume;
            _sfxSource.volume = _sfxVolume;
            _voiceSource.volume = _sfxVolume;
            _catalog = Resources.Load<GameAudioCatalog>("Audio/GameAudioCatalog");

            Debug.Log("[AudioService] Initialized AudioSources.");
        }

        /// <summary>Starts (or keeps) a looping BGM clip with a short fade-in; the BGM slider scales it.</summary>
        public void PlayBGM(AudioClip clip, bool loop = true, float volume = 1.0f)
        {
            if (clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            if (_bgmFade != null) StopCoroutine(_bgmFade);
            _bgmClipVolume = volume;
            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.volume = 0f;
            _bgmSource.Play();
            _bgmFade = StartCoroutine(FadeBgm(0f, _bgmVolume * _bgmClipVolume, 1.6f, false));
        }

        public void StopBGM()
        {
            if (_bgmFade != null) StopCoroutine(_bgmFade);
            _bgmFade = null;
            _bgmSource.Stop();
        }

        public void FadeOutBGM(float seconds)
        {
            if (_bgmSource == null || !_bgmSource.isPlaying) return;
            if (_bgmFade != null) StopCoroutine(_bgmFade);
            _bgmFade = StartCoroutine(FadeBgm(_bgmSource.volume, 0f, Mathf.Max(0.05f, seconds), true));
        }

        private IEnumerator FadeBgm(float from, float to, float seconds, bool stopAtEnd)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }

            _bgmSource.volume = to;
            if (stopAtEnd) _bgmSource.Stop();
            _bgmFade = null;
        }

        public void PlaySFX(AudioClip clip, float volume = 1.0f)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, _sfxVolume * volume);
        }

        public void PlayVoice(AudioClip clip, float volume = 1.0f)
        {
            if (clip == null) return;
            _voiceSource.Stop();
            _voiceSource.clip = clip;
            _voiceSource.volume = _sfxVolume * volume;
            _voiceSource.Play();
        }

        public void PlayCue(GameAudioCue cue, float volume = 1.0f)
        {
            if (_catalog == null) return;
            AudioClip clip = cue switch
            {
                GameAudioCue.UiClick => _catalog.uiClick,
                GameAudioCue.UiConfirm => _catalog.uiConfirm,
                GameAudioCue.UiBack => _catalog.uiBack,
                GameAudioCue.UiError => _catalog.uiError,
                GameAudioCue.DoorOpen => _catalog.doorOpen,
                GameAudioCue.DoorClose => _catalog.doorClose,
                GameAudioCue.Pickup => _catalog.pickup,
                GameAudioCue.Drop => _catalog.drop,
                GameAudioCue.Portal => _catalog.portal,
                GameAudioCue.Punch => _catalog.punch,
                GameAudioCue.UiHover => _catalog.uiHover,
                GameAudioCue.UiOpen => _catalog.uiOpen,
                GameAudioCue.UiClose => _catalog.uiClose,
                GameAudioCue.UiTick => _catalog.uiTick,
                GameAudioCue.FootstepConcrete => Next(_catalog.footstepsConcrete),
                GameAudioCue.FootstepWood => Next(_catalog.footstepsWood),
                GameAudioCue.FootstepCarpet => Next(_catalog.footstepsCarpet),
                _ => null
            };
            PlaySFX(clip, volume);
        }

        private AudioClip Next(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            AudioClip clip = clips[_footstepIndex % clips.Length];
            _footstepIndex = (_footstepIndex + 1) % clips.Length;
            return clip;
        }

        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            if (_bgmFade == null) _bgmSource.volume = _bgmVolume * _bgmClipVolume;
            PlayerPrefs.SetFloat("Volume_BGM", _bgmVolume);
            PlayerPrefs.Save();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            _sfxSource.volume = _sfxVolume;
            _voiceSource.volume = _sfxVolume;
            PlayerPrefs.SetFloat("Volume_SFX", _sfxVolume);
            PlayerPrefs.Save();
        }
    }
}
