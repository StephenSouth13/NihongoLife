using UnityEngine;
using System;
using System.Collections.Generic;

namespace NihongoLife.Core
{
    [Serializable]
    public class DayNightTimelineKey
    {
        [Range(0f, 24f)] public float hour = 12f;
        public Color sunColor = new Color(1f, 0.93f, 0.78f);
        public float sunIntensity = 1.2f;
        public Color ambientColor = new Color(0.58f, 0.62f, 0.66f);
        public Color fogColor = new Color(0.67f, 0.76f, 0.83f);
        [Range(0f, 0.08f)] public float fogDensity = 0.006f;
    }

    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private float cycleMinutes = 8f;
        [SerializeField, Range(0f, 24f)] private float startHour = 15.5f;
        [SerializeField] private bool runCycle = true;
        [SerializeField] private List<DayNightTimelineKey> timeline = new List<DayNightTimelineKey>
        {
            new DayNightTimelineKey { hour = 5.5f, sunColor = new Color(1f, 0.55f, 0.36f), sunIntensity = 0.28f, ambientColor = new Color(0.16f, 0.18f, 0.24f), fogColor = new Color(0.2f, 0.18f, 0.22f), fogDensity = 0.014f },
            new DayNightTimelineKey { hour = 9f, sunColor = new Color(1f, 0.93f, 0.78f), sunIntensity = 1.15f, ambientColor = new Color(0.58f, 0.62f, 0.66f), fogColor = new Color(0.67f, 0.76f, 0.83f), fogDensity = 0.006f },
            new DayNightTimelineKey { hour = 18f, sunColor = new Color(1f, 0.56f, 0.32f), sunIntensity = 0.52f, ambientColor = new Color(0.35f, 0.28f, 0.34f), fogColor = new Color(0.42f, 0.31f, 0.28f), fogDensity = 0.01f },
            new DayNightTimelineKey { hour = 22f, sunColor = new Color(0.44f, 0.55f, 1f), sunIntensity = 0.14f, ambientColor = new Color(0.055f, 0.07f, 0.11f), fogColor = new Color(0.025f, 0.034f, 0.055f), fogDensity = 0.018f }
        };

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
            if (sun == null)
            {
                sun = RenderSettings.sun != null ? RenderSettings.sun : FindFirstObjectByType<Light>();
            }

            var key = EvaluateTimeline(_hour);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = key.ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = key.fogColor;
            RenderSettings.fogDensity = key.fogDensity;

            if (sun == null) return;

            float sunAngle = (_hour / 24f) * 360f - 90f;
            sun.transform.rotation = Quaternion.Euler(sunAngle, -35f, 0f);
            sun.color = key.sunColor;
            sun.intensity = key.sunIntensity;
        }

        private DayNightTimelineKey EvaluateTimeline(float hour)
        {
            if (timeline == null || timeline.Count == 0)
            {
                return new DayNightTimelineKey();
            }

            timeline.Sort((a, b) => a.hour.CompareTo(b.hour));
            DayNightTimelineKey previous = timeline[0];
            DayNightTimelineKey next = timeline[timeline.Count - 1];

            for (int i = 0; i < timeline.Count; i++)
            {
                if (timeline[i].hour <= hour) previous = timeline[i];
                if (timeline[i].hour >= hour)
                {
                    next = timeline[i];
                    break;
                }
            }

            if (next.hour < previous.hour || Mathf.Approximately(previous.hour, next.hour))
            {
                return previous;
            }

            float t = Mathf.InverseLerp(previous.hour, next.hour, hour);
            return new DayNightTimelineKey
            {
                hour = hour,
                sunColor = Color.Lerp(previous.sunColor, next.sunColor, t),
                sunIntensity = Mathf.Lerp(previous.sunIntensity, next.sunIntensity, t),
                ambientColor = Color.Lerp(previous.ambientColor, next.ambientColor, t),
                fogColor = Color.Lerp(previous.fogColor, next.fogColor, t),
                fogDensity = Mathf.Lerp(previous.fogDensity, next.fogDensity, t)
            };
        }
    }
}
