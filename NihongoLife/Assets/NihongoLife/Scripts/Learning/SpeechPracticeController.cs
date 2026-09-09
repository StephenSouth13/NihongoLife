using UnityEngine;
using UnityEngine.InputSystem;
using NihongoLife.Dialogue;
using NihongoLife.Scenario;

namespace NihongoLife.Learning
{
    public class SpeechPracticeController : MonoBehaviour
    {
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
            bool accepted = energy > 0.012f && seconds > 0.45f;
            string result;
            if (accepted && DialogueManager.Instance != null && DialogueManager.Instance.TryGetCurrentPracticePhrase(out string phrase, out string ipa))
            {
                result = string.IsNullOrWhiteSpace(ipa)
                    ? $"Good. Practice line: {phrase}"
                    : $"Good. Practice: {phrase}  / {ipa} /";
            }
            else
            {
                result = accepted
                    ? "Good voice input captured. Ready for pronunciation scoring."
                    : "Voice too short or too quiet. Try again closer to the mic.";
            }

            _lastResult = $"{result} ({seconds:0.0}s)";
            PlayFeedbackTone(accepted);
            if (accepted && ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.CompleteObjective("obj_practice_voice");
            }
            Debug.Log($"[SpeechPractice] {result} Duration: {seconds:0.00}s, energy: {energy:0.0000}");
        }

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
