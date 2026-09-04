using UnityEngine;
using UnityEngine.InputSystem;

namespace NihongoLife.Core
{
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private float cycleMinutes = 8f;
        [SerializeField, Range(0f, 24f)] private float startHour = 15.5f;
        [SerializeField] private bool runCycle = true;

        private float _hour;

        public float Hour => _hour;

        private void Awake()
        {
            _hour = startHour;
            if (sun == null)
            {
                sun = RenderSettings.sun != null ? RenderSettings.sun : FindFirstObjectByType<Light>();
            }

            ApplyLighting();
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.nKey.wasPressedThisFrame) SetHour(21f);
                if (Keyboard.current.mKey.wasPressedThisFrame) SetHour(9f);
            }

            if (!runCycle || cycleMinutes <= 0.01f) return;

            _hour += Time.deltaTime * 24f / (cycleMinutes * 60f);
            if (_hour >= 24f) _hour -= 24f;
            ApplyLighting();
        }

        public void SetHour(float hour)
        {
            _hour = Mathf.Repeat(hour, 24f);
            ApplyLighting();
        }

        private void ApplyLighting()
        {
            float dayAmount = Mathf.Clamp01(Mathf.Sin((_hour - 6f) / 12f * Mathf.PI));
            float duskAmount = 1f - Mathf.Abs(_hour - 18f) / 4f;
            duskAmount = Mathf.Clamp01(duskAmount);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(new Color(0.055f, 0.07f, 0.11f), new Color(0.58f, 0.62f, 0.66f), dayAmount);
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(new Color(0.025f, 0.034f, 0.055f), new Color(0.67f, 0.76f, 0.83f), dayAmount);
            RenderSettings.fogDensity = Mathf.Lerp(0.018f, 0.006f, dayAmount);

            if (sun == null) return;

            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(10f, 62f, dayAmount), Mathf.Lerp(-95f, -35f, dayAmount), 0f);
            sun.color = Color.Lerp(new Color(0.45f, 0.58f, 1f), new Color(1f, 0.93f, 0.78f), dayAmount);
            sun.color = Color.Lerp(sun.color, new Color(1f, 0.58f, 0.32f), duskAmount * 0.35f);
            sun.intensity = Mathf.Lerp(0.18f, 1.28f, dayAmount);
        }
    }
}
