using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Audio
{
    /// <summary>
    /// Generates a calm, loopable Japanese-style menu theme at runtime (no audio files, no licences):
    /// plucked koto-like strings (Karplus-Strong) playing a D "yo" pentatonic melody over a slow
    /// open-fifth pad, with a small reverb. Used only as a fallback: put a real track at
    /// Resources/Audio/Music/menu_bgm (any AudioClip format) and it is used instead.
    /// Generation is spread over several frames so the menu never stutters.
    /// </summary>
    public static class ProceduralMenuMusic
    {
        private const int SampleRate = 44100;
        private const int Bpm = 66;
        private const int Beats = 32;              // 4 chords x 8 beats
        private const float RootHz = 146.83f;      // D3

        // D yo scale (D E G A B) in semitones above D3, two and a half octaves.
        private static readonly int[] Scale = { 0, 2, 5, 7, 9, 12, 14, 17, 19, 21, 24, 26, 29, 31 };

        // Chord roots in semitones above D3; every chord tone stays inside the scale.
        private static readonly int[][] Chords =
        {
            new[] { 0, 7, 14 },    // D  A  E
            new[] { 5, 12, 19 },   // G  D  A
            new[] { 7, 14, 21 },   // A  E  B
            new[] { 0, 7, 14 }     // D  A  E
        };

        private class Note
        {
            public float startBeat;
            public int semitone;
            public float velocity;
            public float seconds;
        }

        public static IEnumerator Generate(Action<AudioClip> done)
        {
            float beat = 60f / Bpm;
            int length = Mathf.RoundToInt(Beats * beat * SampleRate);
            var mix = new float[length];
            var rng = new System.Random(20260920);

            List<Note> notes = BuildNotes(rng);

            // Plucked strings.
            int rendered = 0;
            foreach (var note in notes)
            {
                int start = Mathf.RoundToInt(note.startBeat * beat * SampleRate);
                float hz = RootHz * Mathf.Pow(2f, note.semitone / 12f);
                Pluck(mix, start, hz, note.velocity, note.seconds, rng);
                if (++rendered % 6 == 0) yield return null;
            }

            // Slow pad: open fifths that overlap between chords.
            for (int c = 0; c < Chords.Length; c++)
            {
                float centerBeat = c * 8f + 4f;
                foreach (int semitone in Chords[c])
                {
                    Pad(mix, centerBeat, 12f, RootHz * Mathf.Pow(2f, semitone / 12f), 0.05f, beat);
                    yield return null;
                    Pad(mix, centerBeat, 12f, RootHz * 2f * Mathf.Pow(2f, semitone / 12f) * 1.002f, 0.025f, beat);
                    yield return null;
                }
            }

            // Reverb in slices so a frame never takes long.
            float[] wet = new float[length];
            int[] delays = { 1873, 2251, 2887, 3433 };
            float[] gains = { 0.36f, 0.30f, 0.24f, 0.2f };
            for (int slice = 0; slice < length; slice += 200000)
            {
                int end = Mathf.Min(length, slice + 200000);
                for (int i = slice; i < end; i++)
                {
                    float acc = 0f;
                    for (int d = 0; d < delays.Length; d++)
                    {
                        int source = i - delays[d];
                        if (source < 0) source += length; // wraps so the loop stays seamless
                        acc += mix[source] * gains[d];
                    }

                    wet[i] = acc;
                }

                yield return null;
            }

            float peak = 0.0001f;
            for (int i = 0; i < length; i++)
            {
                mix[i] += wet[i] * 0.55f;
                peak = Mathf.Max(peak, Mathf.Abs(mix[i]));
            }

            float gain = 0.85f / peak;
            for (int i = 0; i < length; i++) mix[i] = Mathf.Clamp(mix[i] * gain, -1f, 1f);

            var clip = AudioClip.Create("ProceduralMenuMusic", length, 1, SampleRate, false);
            clip.SetData(mix, 0);
            done?.Invoke(clip);
        }

        private static List<Note> BuildNotes(System.Random rng)
        {
            var notes = new List<Note>();

            // Bass: one deep pluck on each chord change.
            for (int c = 0; c < Chords.Length; c++)
            {
                notes.Add(new Note { startBeat = c * 8f, semitone = Chords[c][0] - 12, velocity = 0.8f, seconds = 3.2f });
                notes.Add(new Note { startBeat = c * 8f + 4f, semitone = Chords[c][1] - 12, velocity = 0.5f, seconds = 2.4f });
            }

            // Soft arpeggio on every beat.
            for (int b = 0; b < Beats; b++)
            {
                int[] chord = Chords[b / 8];
                int tone = chord[(b % 3 + (b / 4) % 2) % 3] + (b % 2 == 0 ? 12 : 24);
                notes.Add(new Note { startBeat = b, semitone = tone, velocity = 0.22f, seconds = 1.6f });
            }

            // Melody: random walk on the scale, pulled towards chord tones on strong beats.
            int index = 5;
            for (int b = 0; b < Beats; b++)
            {
                double roll = rng.NextDouble();
                if (b % 8 == 7 || roll < 0.18) continue; // breathing space before each chord change

                bool strong = b % 4 == 0;
                int[] chord = Chords[b / 8];
                int steps = 1 + (roll > 0.6 ? 1 : 0) + (roll > 0.9 ? 1 : 0);
                for (int s = 0; s < steps; s++)
                {
                    if (strong && s == 0)
                    {
                        index = NearestScaleIndex(chord[rng.Next(chord.Length)] + 12, index);
                    }
                    else
                    {
                        int move = rng.Next(-2, 3);
                        index = Mathf.Clamp(index + move, 3, Scale.Length - 1);
                    }

                    float startBeat = b + s * (1f / steps);
                    notes.Add(new Note
                    {
                        startBeat = startBeat,
                        semitone = Scale[index],
                        velocity = strong && s == 0 ? 0.72f : 0.52f,
                        seconds = 2.4f
                    });
                }
            }

            return notes;
        }

        private static int NearestScaleIndex(int semitone, int hint)
        {
            int best = hint;
            int bestDistance = int.MaxValue;
            for (int i = 3; i < Scale.Length; i++)
            {
                int distance = Mathf.Abs(Scale[i] - semitone) * 4 + Mathf.Abs(i - hint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Karplus-Strong pluck; the tail wraps around the buffer end so the loop is seamless.</summary>
        private static void Pluck(float[] mix, int start, float hz, float velocity, float seconds, System.Random rng)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(SampleRate / hz));
            var line = new float[period];
            float previous = 0f;
            for (int i = 0; i < period; i++)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                previous = previous * 0.55f + noise * 0.45f; // softer attack than raw noise
                line[i] = previous;
            }

            int samples = Mathf.RoundToInt(seconds * SampleRate);
            int position = 0;
            const float damping = 0.9965f;
            int length = mix.Length;
            for (int i = 0; i < samples; i++)
            {
                float current = line[position];
                float next = line[(position + 1) % period];
                line[position] = damping * 0.5f * (current + next);
                position = (position + 1) % period;

                float fadeOut = i > samples - 2000 ? (samples - i) / 2000f : 1f;
                mix[(start + i) % length] += current * velocity * 0.5f * fadeOut;
            }
        }

        private static void Pad(float[] mix, float centerBeat, float widthBeats, float hz, float amplitude, float beat)
        {
            int length = mix.Length;
            int from = Mathf.RoundToInt((centerBeat - widthBeats * 0.5f) * beat * SampleRate);
            int count = Mathf.RoundToInt(widthBeats * beat * SampleRate);

            // Sine oscillator by recurrence (much cheaper than Math.Sin per sample).
            double omega = 2.0 * Math.PI * hz / SampleRate;
            double coefficient = 2.0 * Math.Cos(omega);
            double previous2 = 0.0;
            double previous1 = Math.Sin(omega);
            for (int i = 0; i < count; i++)
            {
                double value = i == 0 ? 0.0 : i == 1 ? previous1 : coefficient * previous1 - previous2;
                if (i >= 2)
                {
                    previous2 = previous1;
                    previous1 = value;
                }

                float t = (float)i / count;
                float envelope = Mathf.Sin(Mathf.PI * t);
                envelope *= envelope;
                int index = ((from + i) % length + length) % length;
                mix[index] += (float)value * envelope * amplitude;
            }
        }
    }
}
