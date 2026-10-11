using System;
using System.Collections;
using System.IO;
using System.Text;
using NihongoLife.Exam.Ielts;
using UnityEngine;

namespace NihongoLife.Exam
{
    /// <summary>
    /// The game's one microphone recorder (exam Speaking answers in ExamPlayUI and IeltsTestUI): permission, device
    /// errors, a live level for the meter and a hard length cap. Records at 16 kHz mono, which is what speech
    /// recognition needs and keeps uploads small.
    /// </summary>
    public sealed class MicRecorder
    {
        public const int SampleRate = 16000;

        private string _device;
        private AudioClip _clip;
        private float _startedAt;
        private int _maxSeconds;

        public bool IsRecording { get; private set; }
        public string LastError { get; private set; }
        public float Elapsed => IsRecording ? Time.unscaledTime - _startedAt : 0f;
        public int MaxSeconds => _maxSeconds;

        public static bool HasDevice
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return false;
#else
                return Microphone.devices != null && Microphone.devices.Length > 0;
#endif
            }
        }

        /// <summary>Asks the OS for microphone access (a no-op where it is already granted).</summary>
        public static IEnumerator RequestPermission(Action<bool> done)
        {
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            done?.Invoke(Application.HasUserAuthorization(UserAuthorization.Microphone));
        }

        public bool Start(int maxSeconds)
        {
            LastError = null;
            if (IsRecording) return true;
            if (!HasDevice) { LastError = "Không tìm thấy micro. Hãy cắm micro hoặc tai nghe có micro rồi thử lại."; return false; }
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone)) { LastError = "Game chưa được cấp quyền dùng micro. Hãy cho phép trong cài đặt quyền riêng tư của hệ điều hành."; return false; }
            _device = Microphone.devices[0];
            _maxSeconds = Mathf.Clamp(maxSeconds, 1, 600);
            try { _clip = Microphone.Start(_device, false, _maxSeconds, SampleRate); }
            catch (Exception e) { LastError = "Không mở được micro: " + e.Message; return false; }
            if (_clip == null) { LastError = "Micro không trả về dữ liệu (thiết bị đang bận?)."; return false; }
            IsRecording = true;
            _startedAt = Time.unscaledTime;
            return true;
        }

        /// <summary>True once the cap is reached (the caller then stops and keeps the take).</summary>
        public bool ReachedLimit => IsRecording && (!Microphone.IsRecording(_device) || Elapsed >= _maxSeconds);

        /// <summary>Current input level 0-1 (RMS of the last 0.1 s), for the record meter.</summary>
        public float Level
        {
            get
            {
                if (!IsRecording || _clip == null) return 0f;
                int position = Microphone.GetPosition(_device);
                int window = SampleRate / 10;
                if (position < window) return 0f;
                var data = new float[window];
                _clip.GetData(data, position - window);
                double sum = 0;
                foreach (float s in data) sum += s * s;
                return Mathf.Clamp01((float)Math.Sqrt(sum / window) * 4f);
            }
        }

        /// <summary>Stops and returns the recorded samples (empty when nothing was captured).</summary>
        public float[] Stop(out int frequency)
        {
            frequency = _clip != null ? _clip.frequency : SampleRate;
            if (!IsRecording) return Array.Empty<float>();
            int position = Microphone.GetPosition(_device);
            if (!Microphone.IsRecording(_device) || position <= 0) position = Mathf.Min(_clip.samples, Mathf.RoundToInt(Elapsed * frequency));
            try { Microphone.End(_device); } catch (Exception e) { Debug.LogWarning("[MicRecorder] Stop failed: " + e.Message); }
            IsRecording = false;
            if (_clip == null || position <= 0) return Array.Empty<float>();
            var samples = new float[Mathf.Min(position, _clip.samples)];
            _clip.GetData(samples, 0);
            return samples;
        }

        public void Cancel()
        {
            if (!IsRecording) return;
            try { Microphone.End(_device); } catch (Exception) { /* device already gone */ }
            IsRecording = false;
        }
    }

    /// <summary>16-bit PCM mono WAV encode/decode and speech timing measured from the audio itself.</summary>
    public static class SpeechAudio
    {
        public static byte[] EncodeWav(float[] samples, int frequency)
        {
            int byteCount = samples.Length * 2;
            using var stream = new MemoryStream(44 + byteCount);
            using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + byteCount);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(frequency);
            writer.Write(frequency * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(byteCount);
            foreach (float s in samples) writer.Write((short)Mathf.Clamp(s * short.MaxValue, short.MinValue, short.MaxValue));
            return stream.ToArray();
        }

        /// <summary>Reads a 16-bit PCM WAV (mono, or the first channel of stereo).</summary>
        public static float[] DecodeWav(byte[] wav, out int frequency)
        {
            frequency = 0;
            if (wav == null || wav.Length < 44) return Array.Empty<float>();
            int channels = BitConverter.ToInt16(wav, 22);
            frequency = BitConverter.ToInt32(wav, 24);
            int bits = BitConverter.ToInt16(wav, 34);
            int pos = 12;
            while (pos + 8 <= wav.Length)
            {
                string id = Encoding.ASCII.GetString(wav, pos, 4);
                int size = BitConverter.ToInt32(wav, pos + 4);
                if (id == "data")
                {
                    if (bits != 16) return Array.Empty<float>();
                    int frames = Mathf.Min(size, wav.Length - pos - 8) / (2 * Mathf.Max(1, channels));
                    var samples = new float[frames];
                    for (int i = 0; i < frames; i++) samples[i] = BitConverter.ToInt16(wav, pos + 8 + i * 2 * channels) / 32768f;
                    return samples;
                }
                pos += 8 + size + (size & 1);
            }
            return Array.Empty<float>();
        }

        /// <summary>Resamples to <paramref name="target"/> Hz (linear), for recordings made at other rates.</summary>
        public static float[] Resample(float[] samples, int from, int target)
        {
            if (from == target || from <= 0 || samples.Length == 0) return samples;
            int length = Mathf.Max(1, (int)((long)samples.Length * target / from));
            var result = new float[length];
            double step = (double)from / target;
            for (int i = 0; i < length; i++)
            {
                double x = i * step;
                int a = (int)x;
                int b = Mathf.Min(a + 1, samples.Length - 1);
                result[i] = Mathf.Lerp(samples[a], samples[b], (float)(x - a));
            }
            return result;
        }

        /// <summary>
        /// Voice activity from 30 ms frames against the recording's own noise floor: how long the learner spoke,
        /// how often and how long they paused. These timings back up the Fluency judgement with measurements
        /// instead of guessing them from a transcript.
        /// </summary>
        public static IeltsSpeakingMetrics Analyze(float[] samples, int frequency)
        {
            var m = new IeltsSpeakingMetrics();
            if (samples == null || samples.Length == 0 || frequency <= 0) return m;
            m.seconds = samples.Length / (float)frequency;
            int frame = Mathf.Max(1, frequency * 30 / 1000);
            int frames = samples.Length / frame;
            if (frames == 0) return m;
            var energy = new float[frames];
            float peak = 0f;
            for (int f = 0; f < frames; f++)
            {
                double sum = 0;
                for (int i = 0; i < frame; i++)
                {
                    float s = samples[f * frame + i];
                    sum += s * s;
                    peak = Mathf.Max(peak, Mathf.Abs(s));
                }
                energy[f] = (float)Math.Sqrt(sum / frame);
            }
            m.peakLevel = peak;
            var sorted = (float[])energy.Clone();
            Array.Sort(sorted);
            float floor = sorted[Mathf.Clamp(frames / 10, 0, frames - 1)];
            float loud = sorted[Mathf.Clamp(frames * 95 / 100, 0, frames - 1)];
            float threshold = Mathf.Max(0.006f, floor + (loud - floor) * 0.12f);

            // Voiced frames joined across short gaps (stops, soft consonants): only silences of 0.3 s or more break a
            // run of speech, and only those of 0.5 s or more count as pauses.
            float frameSeconds = frame / (float)frequency;
            int first = -1, last = -1, silence = 0, runs = 0;
            float gapped = 0f;
            for (int f = 0; f < frames; f++)
            {
                if (energy[f] >= threshold)
                {
                    if (first < 0) { first = f; runs = 1; }
                    else if (silence > 0)
                    {
                        float gap = silence * frameSeconds;
                        if (gap >= 0.3f) { gapped += gap; runs++; }
                        if (gap >= 0.5f) { m.pauses++; m.longestPause = Mathf.Max(m.longestPause, gap); }
                    }
                    last = f;
                    silence = 0;
                }
                else if (first >= 0) silence++;
            }
            if (first >= 0) m.speechSeconds = Mathf.Max(0f, (last - first + 1) * frameSeconds - gapped);
            m.meanRunSeconds = runs > 0 ? m.speechSeconds / runs : 0f;
            return m;
        }

        public static int CountWords(string text) =>
            string.IsNullOrWhiteSpace(text) ? 0 : System.Text.RegularExpressions.Regex.Matches(text, @"[\p{L}\p{N}'’-]+").Count;
    }
}
