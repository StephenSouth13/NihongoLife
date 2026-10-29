using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Music
{
    /// <summary>
    /// Instrument sounds made in code (nothing to download — the web build stays small): a piano tone (a few harmonics,
    /// a fast attack and a decaying tail) and a plucked guitar string (Karplus–Strong). Each MIDI note is synthesised
    /// once and kept. AudioClip.Create works on every platform, the web included.
    /// </summary>
    public static class InstrumentSynth
    {
        public enum Voice { Piano, Guitar }

        private const int Rate = 22050;
        private static readonly Dictionary<(Voice, int), AudioClip> Cache = new Dictionary<(Voice, int), AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();

        public static float Frequency(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        public static AudioClip Clip(Voice voice, int midi)
        {
            midi = Mathf.Clamp(midi, 21, 108);
            if (Cache.TryGetValue((voice, midi), out var clip) && clip != null) return clip;
            float[] data = voice == Voice.Piano ? Piano(Frequency(midi)) : Pluck(Frequency(midi), midi);
            clip = AudioClip.Create($"{voice}_{midi}", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            Cache[(voice, midi)] = clip;
            return clip;
        }

        private static float[] Piano(float f)
        {
            float seconds = Mathf.Lerp(2.2f, 1.1f, Mathf.InverseLerp(130f, 1000f, f)); // low notes ring longer
            var data = new float[(int)(seconds * Rate)];
            float[] partials = { 1f, 0.55f, 0.28f, 0.16f, 0.08f, 0.05f };
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float attack = Mathf.Clamp01(t / 0.006f);
                float sum = 0f;
                for (int h = 0; h < partials.Length; h++)
                {
                    float fh = f * (h + 1) * (1f + 0.0004f * h * h); // slight inharmonicity, like a real string
                    if (fh > Rate * 0.45f) break;
                    sum += partials[h] * Mathf.Exp(-t * (2.2f + h * 1.4f)) * Mathf.Sin(2f * Mathf.PI * fh * t);
                }
                float release = Mathf.Clamp01((seconds - t) / 0.08f);
                data[i] = 0.32f * attack * release * sum;
            }
            return data;
        }

        private static float[] Pluck(float f, int midi)
        {
            int length = (int)(1.8f * Rate);
            var data = new float[length];
            int period = Mathf.Max(2, Mathf.RoundToInt(Rate / f));
            var ring = new float[period];
            var random = new System.Random(midi * 7919); // the same note always sounds the same
            for (int i = 0; i < period; i++) ring[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            int index = 0;
            float damping = 0.996f;
            for (int i = 0; i < length; i++)
            {
                int next = (index + 1) % period;
                float value = ring[index];
                ring[index] = damping * 0.5f * (ring[index] + ring[next]);
                index = next;
                float release = Mathf.Clamp01((length - i) / (0.05f * Rate));
                data[i] = 0.42f * value * release;
            }
            return data;
        }
    }
}
