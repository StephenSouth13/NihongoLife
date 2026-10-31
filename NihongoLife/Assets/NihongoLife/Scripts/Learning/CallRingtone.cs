using UnityEngine;

namespace NihongoLife.Learning
{
    /// <summary>Short two-tone phone ring for an incoming Speaking call (generated, no audio asset). Throttled so the
    /// server broadcast and the peer broadcast of the same request ring once.</summary>
    public static class CallRingtone
    {
        public const float MinGapSeconds = 4f;
        private static float _last = -99f;
        private static AudioSource _source;
        private static AudioClip _clip;

        public static bool ShouldRing(float now, float last, float gap = MinGapSeconds) => now - last >= gap;

        public static AudioClip CreateClip(int rate = 22050)
        {
            int n = Mathf.RoundToInt(rate * 1.3f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float burst = t % 0.65f;                     // two rings: 0.0-0.45 s and 0.65-1.1 s
                if (burst > 0.45f) continue;
                float env = Mathf.Clamp01(burst / 0.02f) * Mathf.Clamp01((0.45f - burst) / 0.05f);
                float tone = (Mathf.Sin(2f * Mathf.PI * 880f * t) + Mathf.Sin(2f * Mathf.PI * 660f * t)) * 0.5f;
                float trill = Mathf.Sin(2f * Mathf.PI * 20f * t) > 0f ? 1f : 0.55f;
                data[i] = tone * env * trill * 0.8f;
            }
            var clip = AudioClip.Create("CallRingtone", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static void Ring()
        {
            if (!Application.isPlaying) return;
            float now = Time.unscaledTime;
            if (!ShouldRing(now, _last)) return;
            _last = now;
            if (_source == null)
            {
                var go = new GameObject("CallRingtone");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
            }
            if (_clip == null) _clip = CreateClip();
            _source.PlayOneShot(_clip, 0.6f);
        }
    }
}
