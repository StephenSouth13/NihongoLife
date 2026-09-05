using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.NPC
{
    public class NPCAmbientTalker : MonoBehaviour
    {
        [SerializeField] private float audibleDistance = 9f;
        [SerializeField] private Vector2 interval = new Vector2(7f, 16f);
        [SerializeField] private string[] japaneseLines =
        {
            "こんにちは",
            "いい天気ですね",
            "コンビニへ行きます"
        };
        [SerializeField] private string[] englishLines =
        {
            "Good morning.",
            "Excuse me.",
            "Where is the station?"
        };

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
            Schedule();
        }

        private void Update()
        {
            if (Time.time < _nextTime) return;
            Schedule();
            if (_player == null || Vector3.Distance(transform.position, _player.position) > audibleDistance) return;

            string[] lines = japaneseLines;
            if (GameServices.TryGet(out GameSettingsService settings) && settings.Language == GameLanguage.English)
            {
                lines = englishLines;
            }

            string line = lines != null && lines.Length > 0 ? lines[Random.Range(0, lines.Length)] : "hello";
            _source.PlayOneShot(CreateMumble(line), 0.32f);
        }

        private void Schedule()
        {
            _nextTime = Time.time + Random.Range(interval.x, interval.y);
        }

        private static AudioClip CreateMumble(string seed)
        {
            const int sampleRate = 22050;
            int samples = sampleRate * Random.Range(1, 3);
            float[] data = new float[samples];
            int hash = Mathf.Abs(seed.GetHashCode());
            float baseFrequency = 150f + hash % 120;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float syllable = Mathf.Floor(t / 0.13f);
                float freq = baseFrequency + ((hash + (int)syllable * 41) % 90);
                float envelope = Mathf.Sin(Mathf.Clamp01((t % 0.13f) / 0.13f) * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.08f;
            }

            AudioClip clip = AudioClip.Create("GeneratedNpcMumble", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
