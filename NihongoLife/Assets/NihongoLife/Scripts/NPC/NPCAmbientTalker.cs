using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.NPC
{
    public class NPCAmbientTalker : MonoBehaviour
    {
        [SerializeField] private float audibleDistance = 9f;
        [SerializeField] private Vector2 interval = new Vector2(7f, 16f);

        private AudioSource _source;
        private Transform _player;
        private float _nextTime;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 1f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = 1.5f;
            _source.maxDistance = audibleDistance;
        }

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) _player = player.transform;

            if (!HasConfiguredStreetVoice())
            {
                enabled = false;
                return;
            }

            Schedule();
        }

        private void Update()
        {
            if (Time.time < _nextTime) return;
            Schedule();
            if (_player == null || Vector3.Distance(transform.position, _player.position) > audibleDistance) return;

            AudioClip clip = PickConfiguredStreetVoice();
            if (clip != null)
            {
                _source.PlayOneShot(clip, 0.32f);
            }
        }

        private void Schedule()
        {
            _nextTime = Time.time + Random.Range(interval.x, interval.y);
        }

        private static bool HasConfiguredStreetVoice()
        {
            if (!GameServices.TryGet(out GameControlService control) || control.Database == null) return false;
            AudioClip[] clips = control.Database.streetVoiceClips;
            if (clips == null) return false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) return true;
            }

            return false;
        }

        private static AudioClip PickConfiguredStreetVoice()
        {
            if (!GameServices.TryGet(out GameControlService control) || control.Database == null) return null;
            AudioClip[] clips = control.Database.streetVoiceClips;
            if (clips == null || clips.Length == 0) return null;

            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != null) return clip;
            }

            return null;
        }
    }
}
