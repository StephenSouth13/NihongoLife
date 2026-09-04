using UnityEngine;
using UnityEngine.InputSystem;

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

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current[triggerKey].wasPressedThisFrame) return;

            if (_isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        public void StartRecording()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[SpeechPractice] No microphone device found.");
                return;
            }

            _device = Microphone.devices[0];
            _recording = Microphone.Start(_device, false, maxRecordSeconds, sampleRate);
            _isRecording = true;
            _lastResult = "Recording... press V again to stop";
            Debug.Log("[SpeechPractice] Recording started. Press V again to stop.");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;

            int position = Microphone.GetPosition(_device);
            Microphone.End(_device);
            _isRecording = false;

            float seconds = position / (float)sampleRate;
            float energy = EstimateEnergy(_recording, position);
            string result = energy > 0.012f && seconds > 0.45f
                ? "Good voice input captured. Ready for pronunciation scoring."
                : "Voice too short or too quiet. Try again closer to the mic.";

            _lastResult = $"{result} ({seconds:0.0}s)";
            Debug.Log($"[SpeechPractice] {result} Duration: {seconds:0.00}s, energy: {energy:0.0000}");
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
