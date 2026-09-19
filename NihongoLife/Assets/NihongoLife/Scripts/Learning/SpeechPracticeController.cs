using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Scenario;

namespace NihongoLife.Learning
{
    public class SpeechPracticeController : MonoBehaviour
    {
        [Serializable] private class InlineData { public string mimeType; public string data; }
        [Serializable] private class GeminiPart { public string text; public InlineData inlineData; }
        [Serializable] private class GeminiContent { public GeminiPart[] parts; }
        [Serializable] private class GeminiRequest { public GeminiContent[] contents; }
        [Serializable] private class GeminiCandidate { public GeminiContent content; }
        [Serializable] private class GeminiResponse { public GeminiCandidate[] candidates; }

        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private int maxRecordSeconds = 8;
        [SerializeField] private Key triggerKey = Key.V;

        private AudioClip _recording;
        private string _device;
        private bool _isRecording;
        private string _lastResult = "Press V to practice pronunciation";
        private AudioSource _feedbackSource;

        private void Awake()
        {
            _feedbackSource = gameObject.AddComponent<AudioSource>();
            _feedbackSource.playOnAwake = false;
            _feedbackSource.spatialBlend = 0f;
        }

        private void Update()
        {
            if (_isRecording && _recording != null && !string.IsNullOrEmpty(_device))
            {
                int position = Microphone.GetPosition(_device);
                if (position >= _recording.samples - 1)
                {
                    StopRecording();
                    return;
                }
            }

            if (!WasTriggerPressed()) return;

            if (_isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private bool WasTriggerPressed()
        {
            if (Keyboard.current != null && Keyboard.current[triggerKey].wasPressedThisFrame)
            {
                return true;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.V);
#else
            return false;
#endif
        }

        public void StartRecording()
        {
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                StartCoroutine(RequestMicPermissionThenStart());
                return;
            }

            StartRecordingNow();
        }

        private System.Collections.IEnumerator RequestMicPermissionThenStart()
        {
            _lastResult = "Requesting microphone permission...";
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                _lastResult = "Microphone permission denied.";
                PlayFeedbackTone(false);
                yield break;
            }

            StartRecordingNow();
        }

        private void StartRecordingNow()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[SpeechPractice] No microphone device found.");
                _lastResult = "No microphone found. Check Windows input device.";
                PlayFeedbackTone(false);
                return;
            }

            _device = Microphone.devices[0];
            try
            {
                _recording = Microphone.Start(_device, false, maxRecordSeconds, sampleRate);
            }
            catch (System.Exception ex)
            {
                _lastResult = "Microphone start failed: " + ex.Message;
                Debug.LogWarning("[SpeechPractice] Microphone start failed: " + ex);
                PlayFeedbackTone(false);
                return;
            }

            _isRecording = true;
            _lastResult = "Recording... press V again to stop";
            PlayFeedbackTone(true);
            Debug.Log($"[SpeechPractice] Recording started on '{_device}'. Press V again to stop.");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;

            int position = 0;
            try
            {
                position = Microphone.GetPosition(_device);
                Microphone.End(_device);
            }
            catch (System.Exception ex)
            {
                _isRecording = false;
                _lastResult = "Microphone stop failed: " + ex.Message;
                Debug.LogWarning("[SpeechPractice] Microphone stop failed: " + ex);
                PlayFeedbackTone(false);
                return;
            }
            _isRecording = false;

            float seconds = position / (float)sampleRate;
            float energy = EstimateEnergy(_recording, position);
            if (energy <= 0.012f || seconds <= 0.45f)
            {
                _lastResult = $"Voice too short or too quiet. Try again closer to the mic. ({seconds:0.0}s)";
                PlayFeedbackTone(false);
                return;
            }

            if (DialogueManager.Instance == null ||
                !DialogueManager.Instance.TryGetCurrentPracticePhrase(out string phrase, out _))
            {
                _lastResult = "No active practice sentence. Start a dialogue first.";
                PlayFeedbackTone(false);
                return;
            }

            StartCoroutine(RecognizeAndScore(phrase, position, seconds));
        }

        private IEnumerator RecognizeAndScore(string expectedPhrase, int sampleFrames, float seconds)
        {
            if (!GameServices.TryGet(out GameControlService controls) || controls.Database == null)
            {
                _lastResult = "Speech service is not configured.";
                yield break;
            }

            GameControlDatabase database = controls.Database;
            string apiKey = Environment.GetEnvironmentVariable(database.geminiApiKeyEnvironmentKey);
            if (!database.enableGeminiConversation || !database.allowGeminiDirectClientCalls || string.IsNullOrWhiteSpace(apiKey))
            {
                _lastResult = $"Set {database.geminiApiKeyEnvironmentKey} to enable real speech recognition.";
                PlayFeedbackTone(false);
                yield break;
            }

            _lastResult = "Analyzing recorded speech...";
            byte[] wav = EncodeWav(_recording, sampleFrames);
            string prompt = "Transcribe only the spoken words in this audio. Preserve Japanese script when Japanese is spoken. Return the transcript only, without notes or punctuation.";
            var body = new GeminiRequest
            {
                contents = new[]
                {
                    new GeminiContent
                    {
                        parts = new[]
                        {
                            new GeminiPart { text = prompt },
                            new GeminiPart { inlineData = new InlineData { mimeType = "audio/wav", data = Convert.ToBase64String(wav) } }
                        }
                    }
                }
            };

            string model = string.IsNullOrWhiteSpace(database.geminiModel) ? "gemini-2.5-flash" : database.geminiModel;
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{UnityWebRequest.EscapeURL(model)}:generateContent";
            byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = 30;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    _lastResult = $"Speech recognition failed ({request.responseCode}). Check network/API key.";
                    Debug.LogWarning($"[SpeechPractice] Gemini failed: {request.responseCode} {request.downloadHandler.text}");
                    PlayFeedbackTone(false);
                    yield break;
                }

                string transcript = ReadTranscript(request.downloadHandler.text);
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    _lastResult = "No speech could be recognized. Please try again.";
                    PlayFeedbackTone(false);
                    yield break;
                }

                float match = Similarity(expectedPhrase, transcript);
                bool accepted = match >= 0.58f;
                _lastResult = accepted
                    ? $"Recognized: {transcript} | Match {match:P0} ({seconds:0.0}s)"
                    : $"Heard: {transcript} | Try: {expectedPhrase} | Match {match:P0}";
                PlayFeedbackTone(accepted);
                if (accepted && ScenarioManager.Instance != null)
                {
                    ScenarioManager.Instance.CompleteObjective("obj_practice_voice");
                }
            }
        }

        private static string ReadTranscript(string json)
        {
            try
            {
                var response = JsonUtility.FromJson<GeminiResponse>(json);
                if (response?.candidates == null || response.candidates.Length == 0) return string.Empty;
                GeminiPart[] parts = response.candidates[0].content?.parts;
                return parts != null && parts.Length > 0 ? (parts[0].text ?? string.Empty).Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SpeechPractice] Invalid recognition response: " + ex.Message);
                return string.Empty;
            }
        }

        private static byte[] EncodeWav(AudioClip clip, int sampleFrames)
        {
            int channels = clip.channels;
            int frames = Mathf.Clamp(sampleFrames, 1, clip.samples);
            float[] source = new float[frames * channels];
            clip.GetData(source, 0);
            byte[] wav = new byte[44 + source.Length * 2];
            WriteAscii(wav, 0, "RIFF");
            WriteInt(wav, 4, wav.Length - 8);
            WriteAscii(wav, 8, "WAVEfmt ");
            WriteInt(wav, 16, 16);
            WriteShort(wav, 20, 1);
            WriteShort(wav, 22, (short)channels);
            WriteInt(wav, 24, clip.frequency);
            WriteInt(wav, 28, clip.frequency * channels * 2);
            WriteShort(wav, 32, (short)(channels * 2));
            WriteShort(wav, 34, 16);
            WriteAscii(wav, 36, "data");
            WriteInt(wav, 40, source.Length * 2);
            for (int i = 0; i < source.Length; i++)
            {
                short value = (short)(Mathf.Clamp(source[i], -1f, 1f) * short.MaxValue);
                WriteShort(wav, 44 + i * 2, value);
            }
            return wav;
        }

        private static float Similarity(string expected, string actual)
        {
            string a = Normalize(expected);
            string b = Normalize(actual);
            if (a.Length == 0 || b.Length == 0) return 0f;
            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) previous[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Mathf.Min(Mathf.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }
                var swap = previous; previous = current; current = swap;
            }
            return 1f - previous[b.Length] / (float)Mathf.Max(a.Length, b.Length);
        }

        private static string Normalize(string value)
        {
            var result = new StringBuilder(value.Length);
            foreach (char c in value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) result.Append(c);
            }
            return result.ToString();
        }

        private static void WriteAscii(byte[] target, int offset, string value) => Encoding.ASCII.GetBytes(value, 0, value.Length, target, offset);
        private static void WriteInt(byte[] target, int offset, int value) => Array.Copy(BitConverter.GetBytes(value), 0, target, offset, 4);
        private static void WriteShort(byte[] target, int offset, short value) => Array.Copy(BitConverter.GetBytes(value), 0, target, offset, 2);

        private void PlayFeedbackTone(bool positive)
        {
            if (_feedbackSource == null) return;
            _feedbackSource.PlayOneShot(CreateTone(positive ? 720f : 180f, positive ? 0.09f : 0.18f), 0.35f);
        }

        private static AudioClip CreateTone(float frequency, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - Mathf.Clamp01(t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.18f;
            }

            AudioClip clip = AudioClip.Create("SpeechPracticeFeedback", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_lastResult)) return;

            GUI.color = _isRecording ? new Color(1f, 0.55f, 0.35f, 1f) : new Color(0.9f, 0.95f, 1f, 1f);
            GUI.Label(new Rect(24f, Screen.height - 54f, 620f, 32f), "[V] Mic: " + _lastResult);
            GUI.color = Color.white;
        }

        private static float EstimateEnergy(AudioClip clip, int samplesToRead)
        {
            if (clip == null || samplesToRead <= 0) return 0f;

            int sampleCount = Mathf.Min(samplesToRead * clip.channels, clip.samples * clip.channels);
            float[] samples = new float[sampleCount];
            clip.GetData(samples, 0);

            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += Mathf.Abs(samples[i]);
            }

            return samples.Length > 0 ? sum / samples.Length : 0f;
        }
    }
}
