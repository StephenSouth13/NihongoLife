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
        /// <summary>The voices. Piano and Guitar first (their numbers are kept); the others are synthesised models of
        /// folk instruments so each song sounds on the instrument it belongs to — all generated here, no samples.</summary>
        public enum Voice { Piano, Guitar, DanTranh, DanBau, SaoTruc, Koto, Shamisen, Shakuhachi, Balalaika, Violin }

        /// <summary>The voice for an instrument id from songs.json (unknown ids play on the piano).</summary>
        public static Voice VoiceFor(string instrument) => (instrument ?? "").ToLowerInvariant() switch
        {
            "guitar" => Voice.Guitar,
            "dan_tranh" or "gayageum" => Voice.DanTranh,
            "dan_bau" => Voice.DanBau,
            "sao_truc" or "dizi" => Voice.SaoTruc,
            "koto" => Voice.Koto,
            "shamisen" or "dan_nguyet" => Voice.Shamisen,
            "shakuhachi" => Voice.Shakuhachi,
            "balalaika" or "bandura" => Voice.Balalaika,
            "violin" or "erhu" => Voice.Violin,
            _ => Voice.Piano,
        };

        private const int Rate = 22050;
        private static readonly Dictionary<(Voice, int), AudioClip> Cache = new Dictionary<(Voice, int), AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();

        public static float Frequency(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        public static AudioClip Clip(Voice voice, int midi)
        {
            midi = Mathf.Clamp(midi, 21, 108);
            if (Cache.TryGetValue((voice, midi), out var clip) && clip != null) return clip;
            float f = Frequency(midi);
            float[] data = voice switch
            {
                Voice.Piano => Piano(f),
                Voice.Guitar => Pluck(f, midi),
                _ => Model(f, midi, Shape(voice)),
            };
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

        /// <summary>How a modelled instrument sounds: harmonic mix, envelope, vibrato, pitch bend, breath and tremolo.</summary>
        private struct Tone
        {
            public float[] partials;
            public float seconds, attack, decay, brightDecay, vibratoDepth, vibratoRate, vibratoDelay, bend, bendTime, breath, tremolo, gain;
        }

        private static Tone Shape(Voice voice) => voice switch
        {
            // Đàn tranh (16-string zither): a bright pluck, then the left hand's "rung" vibrato on the ringing string.
            Voice.DanTranh => new Tone { partials = new[] { 1f, 0.6f, 0.42f, 0.28f, 0.18f, 0.1f }, seconds = 1.7f, attack = 0.003f, decay = 2.0f, brightDecay = 3.5f, vibratoDepth = 0.012f, vibratoRate = 5.5f, vibratoDelay = 0.3f, gain = 0.3f },
            // Đàn bầu (monochord): flageolet harmonics, almost pure, the note slides up into place and wavers (the whammy rod).
            Voice.DanBau => new Tone { partials = new[] { 1f, 0.12f, 0.05f }, seconds = 1.9f, attack = 0.02f, decay = 0.9f, brightDecay = 1f, vibratoDepth = 0.02f, vibratoRate = 4.8f, vibratoDelay = 0.15f, bend = -0.04f, bendTime = 0.09f, gain = 0.38f },
            // Sáo trúc (bamboo flute): soft attack, breath noise, light vibrato, the tone held.
            Voice.SaoTruc => new Tone { partials = new[] { 1f, 0.22f, 0.09f, 0.04f }, seconds = 1.2f, attack = 0.05f, decay = 0.35f, brightDecay = 0.5f, vibratoDepth = 0.008f, vibratoRate = 5.5f, vibratoDelay = 0.18f, breath = 0.06f, gain = 0.34f },
            // Koto (13-string zither): crisp pluck, long ring, a slight late vibrato.
            Voice.Koto => new Tone { partials = new[] { 1f, 0.5f, 0.38f, 0.24f, 0.15f, 0.08f }, seconds = 1.6f, attack = 0.002f, decay = 2.4f, brightDecay = 4f, vibratoDepth = 0.006f, vibratoRate = 5f, vibratoDelay = 0.4f, gain = 0.3f },
            // Shamisen: a hard plectrum attack, many bright partials, a short buzzing (sawari) note.
            Voice.Shamisen => new Tone { partials = new[] { 1f, 0.85f, 0.7f, 0.55f, 0.45f, 0.35f, 0.25f, 0.18f }, seconds = 0.9f, attack = 0.001f, decay = 4.5f, brightDecay = 6f, breath = 0.03f, gain = 0.22f },
            // Shakuhachi (bamboo flute): breathy, slow attack, a dip into the note, a wide slow vibrato.
            Voice.Shakuhachi => new Tone { partials = new[] { 1f, 0.32f, 0.12f, 0.05f }, seconds = 1.5f, attack = 0.09f, decay = 0.3f, brightDecay = 0.4f, vibratoDepth = 0.012f, vibratoRate = 4.2f, vibratoDelay = 0.25f, bend = -0.02f, bendTime = 0.12f, breath = 0.16f, gain = 0.34f },
            // Balalaika: plucked strings played in tremolo (the fast strumming that holds long notes).
            Voice.Balalaika => new Tone { partials = new[] { 1f, 0.7f, 0.5f, 0.32f, 0.2f, 0.1f }, seconds = 1.4f, attack = 0.002f, decay = 0.9f, brightDecay = 2f, tremolo = 11f, gain = 0.3f },
            // Violin (also the erhu's bowed voice): a sawtooth-like bowed tone with vibrato.
            _ => new Tone { partials = new[] { 1f, 0.5f, 0.33f, 0.25f, 0.2f, 0.16f, 0.13f, 0.11f }, seconds = 1.3f, attack = 0.07f, decay = 0.25f, brightDecay = 0.3f, vibratoDepth = 0.01f, vibratoRate = 6f, vibratoDelay = 0.15f, gain = 0.22f },
        };

        /// <summary>Additive synthesis with one running phase (so vibrato and bends stay in tune across the harmonics).</summary>
        private static float[] Model(float f, int midi, Tone tone)
        {
            var data = new float[(int)(tone.seconds * Rate)];
            var random = new System.Random(midi * 104729 + (int)(tone.decay * 1000f)); // the same note always sounds the same
            double phase = 0.0;
            float noise = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float pitch = 1f;
                if (tone.bendTime > 0f) pitch += tone.bend * Mathf.Clamp01(1f - t / tone.bendTime);
                if (tone.vibratoDepth > 0f && t > tone.vibratoDelay)
                    pitch += tone.vibratoDepth * Mathf.Clamp01((t - tone.vibratoDelay) / 0.2f) * Mathf.Sin(2f * Mathf.PI * tone.vibratoRate * t);
                phase += 2.0 * System.Math.PI * f * pitch / Rate;
                float sum = 0f;
                for (int h = 0; h < tone.partials.Length; h++)
                {
                    if (f * (h + 1) > Rate * 0.45f) break;
                    sum += tone.partials[h] * Mathf.Exp(-t * tone.brightDecay * h) * (float)System.Math.Sin(phase * (h + 1));
                }
                if (tone.breath > 0f)
                {
                    noise = 0.8f * noise + 0.2f * (float)(random.NextDouble() * 2.0 - 1.0); // soft (low-passed) noise
                    sum += tone.breath * 4f * noise * Mathf.Exp(-t * 2f);
                }
                float env = Mathf.Clamp01(t / Mathf.Max(0.001f, tone.attack)) * Mathf.Exp(-t * tone.decay);
                if (tone.tremolo > 0f) env *= 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Mathf.PI * tone.tremolo * t));
                float release = Mathf.Clamp01((tone.seconds - t) / 0.08f);
                data[i] = tone.gain * env * release * sum;
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
