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
            if (GameServices.TryGet(out GameControlService controlService) && controlService.Database != null)
            {
                bgm = controlService.Database.townBgmClip;
            }

            _bgmSource.clip = bgm != null ? bgm : CreateTownLoop();
            _bgmSource.volume = bgmVolume;
            _bgmSource.Play();
            ScheduleNextStreetVoice();
        }

        private void Update()
        {
            if (Time.time < _nextStreetVoiceTime) return;

            AudioClip clip = PickStreetVoiceClip();
            _streetSource.PlayOneShot(clip != null ? clip : CreateStreetMurmur(), streetVoiceVolume);
            ScheduleNextStreetVoice();
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

        private static AudioClip CreateTownLoop()
        {
            const int sampleRate = 22050;
            int samples = sampleRate * 12;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float pad = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.025f;
                float bell = Mathf.Sin(2f * Mathf.PI * 440f * t) * Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 0.5f)) * 0.012f;
                data[i] = pad + bell;
            }

            AudioClip clip = AudioClip.Create("GeneratedTownBgm", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateStreetMurmur()
        {
            const int sampleRate = 22050;
            int samples = sampleRate * 2;
            float[] data = new float[samples];
            float f1 = Random.Range(170f, 240f);
            float f2 = Random.Range(260f, 360f);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float gate = (t % 0.42f) < 0.28f ? 1f : 0.2f;
                float envelope = Mathf.Sin(Mathf.Clamp01(t / 2f) * Mathf.PI);
                data[i] = (Mathf.Sin(2f * Mathf.PI * f1 * t) + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.55f) * gate * envelope * 0.045f;
            }

            AudioClip clip = AudioClip.Create("GeneratedStreetMurmur", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
