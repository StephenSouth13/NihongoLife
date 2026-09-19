using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Audio
{
    public class TownAmbientAudio : MonoBehaviour
    {
        [SerializeField] private float bgmVolume = 0.36f;
        [SerializeField] private float streetVoiceVolume = 0.5f;
        [SerializeField] private Vector2 streetVoiceInterval = new Vector2(4f, 9f);

        private AudioSource _bgmSource;
        private AudioSource _streetSource;
        private float _nextStreetVoiceTime;

        private void Awake()
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.spatialBlend = 0f;

            _streetSource = gameObject.AddComponent<AudioSource>();
            _streetSource.playOnAwake = false;
            _streetSource.spatialBlend = 0f;
        }

        private void Start()
        {
            if (GameServices.TryGet(out GameControlService control)
                && control.Database != null
                && !control.Database.enableTownAmbientAudio)
            {
                enabled = false;
                return;
            }

            AudioClip bgm = null;
            AudioClip[] streetClips = null;
            if (GameServices.TryGet(out GameControlService controlService) && controlService.Database != null)
            {
                bgm = controlService.Database.townBgmClip;
                streetClips = controlService.Database.streetVoiceClips;
            }

            if (bgm != null)
            {
                _bgmSource.clip = bgm;
                _bgmSource.volume = bgmVolume;
                _bgmSource.Play();
            }

            if (bgm == null && !HasUsableClip(streetClips))
            {
                enabled = false;
                return;
            }

            ScheduleNextStreetVoice();
        }

        private void Update()
        {
            if (Time.time < _nextStreetVoiceTime) return;

            AudioClip clip = PickStreetVoiceClip();
            if (clip != null)
            {
                _streetSource.PlayOneShot(clip, streetVoiceVolume);
            }
            ScheduleNextStreetVoice();
        }

        private static bool HasUsableClip(AudioClip[] clips)
        {
            if (clips == null) return false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) return true;
            }

            return false;
        }

        private AudioClip PickStreetVoiceClip()
        {
            if (!GameServices.TryGet(out GameControlService control) || control.Database == null) return null;
            var clips = control.Database.streetVoiceClips;
            if (clips == null || clips.Length == 0) return null;

            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != null) return clip;
            }

            return null;
        }

        private void ScheduleNextStreetVoice()
        {
            _nextStreetVoiceTime = Time.time + Random.Range(streetVoiceInterval.x, streetVoiceInterval.y);
        }

    }
}
