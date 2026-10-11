using UnityEngine;
using UnityEngine.Rendering;

namespace NihongoLife.Core
{
    /// <summary>
    /// The outdoors of one scene, as DayNightCycle sees it: its sun, its night lamps, its daytime ambient/fog (baked by
    /// the scene builder from the scene's own lighting) and the day/night sky. Outdoor scenes (Hibari, Midori) let the
    /// cycle drive ambient and fog; interiors only have one for the street seen through their door, so their own room
    /// lighting is never touched. Scenes without one are not lit by the cycle at all.
    /// </summary>
    public sealed class OutdoorSky : MonoBehaviour
    {
        public static OutdoorSky Active { get; private set; }

        [SerializeField] private Light sun;
        [SerializeField] private float sunYaw = -35f;
        [SerializeField] private float sunNoonIntensity = 1.15f;
        [SerializeField] private Material skyMaterial;
        [SerializeField] private bool drivesAmbientAndFog = true;
        [SerializeField] private AmbientMode ambientMode = AmbientMode.Flat;
        [SerializeField] private Color ambientSky = new Color(0.58f, 0.62f, 0.66f);
        [SerializeField] private Color ambientEquator = new Color(0.5f, 0.52f, 0.55f);
        [SerializeField] private Color ambientGround = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color fogColor = new Color(0.67f, 0.76f, 0.83f);
        [SerializeField] private float fogDensity = 0.006f;
        [SerializeField] private Light[] nightLamps = new Light[0];
        [SerializeField] private float[] lampIntensities = new float[0];
        [SerializeField] private Renderer[] lampGlows = new Renderer[0];

        private Material _sky;
        private MaterialPropertyBlock _glowBlock;
        private static readonly int NightId = Shader.PropertyToID("_Night");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int HorizonColorId = Shader.PropertyToID("_HorizonColor");
        private static readonly int HorizonGlowId = Shader.PropertyToID("_HorizonGlow");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int SunDirId = Shader.PropertyToID("_SunDir");
        private static readonly int SunColorId = Shader.PropertyToID("_SunColor");
        private static readonly int SunSizeId = Shader.PropertyToID("_SunSize");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public Light Sun => sun;
        public bool DrivesAmbientAndFog => drivesAmbientAndFog;
        /// <summary>0 at full night, 1 in full daylight (last applied hour).</summary>
        public float Daylight { get; private set; } = 1f;
        public int LampsOn { get; private set; }

#if UNITY_EDITOR
        /// <summary>Scene builders bake the scene's own daytime lighting and lamps here.</summary>
        public void Configure(Light sunLight, float yaw, Material sky, bool ambientAndFog, Light[] lamps, Renderer[] glows)
        {
            sun = sunLight;
            sunYaw = yaw;
            sunNoonIntensity = sunLight != null ? Mathf.Max(0.2f, sunLight.intensity) : 1f;
            skyMaterial = sky;
            drivesAmbientAndFog = ambientAndFog;
            ambientMode = RenderSettings.ambientMode == AmbientMode.Skybox ? AmbientMode.Trilight : RenderSettings.ambientMode;
            ambientSky = RenderSettings.ambientMode == AmbientMode.Flat ? RenderSettings.ambientLight : RenderSettings.ambientSkyColor;
            ambientEquator = RenderSettings.ambientEquatorColor;
            ambientGround = RenderSettings.ambientGroundColor;
            fogColor = RenderSettings.fogColor;
            fogDensity = RenderSettings.fogDensity;
            nightLamps = lamps ?? new Light[0];
            lampIntensities = new float[nightLamps.Length];
            for (int i = 0; i < nightLamps.Length; i++) lampIntensities[i] = nightLamps[i] != null ? nightLamps[i].intensity : 1f;
            lampGlows = glows ?? new Renderer[0];
        }
#endif

        private void OnEnable()
        {
            Active = this;
            if (skyMaterial != null)
            {
                _sky = new Material(skyMaterial) { name = skyMaterial.name + " (day/night)" };
                RenderSettings.skybox = _sky;
            }
            var cycle = FindFirstObjectByType<DayNightCycle>();
            if (cycle != null) cycle.SetHour(cycle.Hour);
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
        }

        private void OnDestroy()
        {
            if (_sky != null) Destroy(_sky);
        }

        /// <summary>Lights this scene for <paramref name="hour"/>. <paramref name="key"/> is the cycle's colour key for
        /// that hour and <paramref name="noon"/> its daytime key: ambient and fog keep the scene's own daytime look and
        /// follow the key's change relative to noon.</summary>
        public void Apply(float hour, DayNightTimelineKey key, DayNightTimelineKey noon)
        {
            // Sun above the horizon from 6:00 to 18:00; at night the same light becomes the moon (never lights from below).
            float angle = hour / 24f * 360f - 90f;
            float elevation = Mathf.Sin(angle * Mathf.Deg2Rad);
            Daylight = Mathf.Clamp01(Mathf.InverseLerp(0.14f, noon.sunIntensity, key.sunIntensity));
            if (sun != null)
            {
                float lightAngle = Mathf.Repeat(elevation >= 0f ? angle : angle - 180f, 360f);
                sun.transform.rotation = Quaternion.Euler(Mathf.Clamp(lightAngle, 6f, 174f), sunYaw, 0f);
                float horizonDip = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(Mathf.Abs(elevation) / 0.2f));
                sun.color = key.sunColor;
                sun.intensity = key.sunIntensity / Mathf.Max(0.01f, noon.sunIntensity) * sunNoonIntensity * horizonDip;
                sun.shadowStrength = Mathf.Lerp(0.35f, 0.85f, Daylight);
                RenderSettings.sun = sun;
            }

            if (drivesAmbientAndFog)
            {
                Color ratio = Ratio(key.ambientColor, noon.ambientColor);
                RenderSettings.ambientMode = ambientMode;
                if (ambientMode == AmbientMode.Flat) RenderSettings.ambientLight = ambientSky * ratio;
                else
                {
                    RenderSettings.ambientSkyColor = ambientSky * ratio;
                    RenderSettings.ambientEquatorColor = ambientEquator * ratio;
                    RenderSettings.ambientGroundColor = ambientGround * ratio;
                }
                RenderSettings.fog = true;
                RenderSettings.fogColor = fogColor * Ratio(key.fogColor, noon.fogColor);
                RenderSettings.fogDensity = fogDensity * key.fogDensity / Mathf.Max(0.0001f, noon.fogDensity);
            }

            if (_sky != null)
            {
                float glow = Mathf.Clamp01(1f - Mathf.Abs(elevation) / 0.32f);
                _sky.SetFloat(NightId, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.08f, Daylight)));
                _sky.SetColor(TintId, Color.Lerp(Color.white, key.sunColor, 0.18f * glow));
                _sky.SetColor(HorizonColorId, key.sunColor);
                _sky.SetFloat(HorizonGlowId, 0.75f * glow);
                _sky.SetFloat(ExposureId, Mathf.Lerp(0.9f, 1.05f, Daylight));
                Vector3 toSun = sun != null ? -(Quaternion.Euler(angle, sunYaw, 0f) * Vector3.forward) : Vector3.up;
                bool moon = elevation < 0f;
                _sky.SetVector(SunDirId, moon ? -toSun : toSun);
                _sky.SetColor(SunColorId, moon ? new Color(0.75f, 0.82f, 1f) * 0.9f : Color.Lerp(key.sunColor, Color.white, 0.4f) * 1.6f);
                _sky.SetFloat(SunSizeId, moon ? 0.99935f : 0.9988f);
            }

            // Street lamps come on at dusk and go out after dawn.
            float lamp = Mathf.Clamp01((0.5f - Daylight) / 0.3f);
            int on = 0;
            for (int i = 0; i < nightLamps.Length; i++)
            {
                var l = nightLamps[i];
                if (l == null) continue;
                bool lit = lamp > 0.02f;
                if (l.enabled != lit) l.enabled = lit;
                if (lit) { l.intensity = lampIntensities[i] * lamp; on++; }
            }
            LampsOn = on;
            if (lampGlows.Length > 0)
            {
                _glowBlock ??= new MaterialPropertyBlock();
                _glowBlock.SetColor(EmissionId, new Color(1f, 0.78f, 0.45f) * (2.2f * lamp));
                foreach (var r in lampGlows) if (r != null) r.SetPropertyBlock(_glowBlock);
            }
        }

        private static Color Ratio(Color value, Color reference) => new Color(
            value.r / Mathf.Max(0.01f, reference.r), value.g / Mathf.Max(0.01f, reference.g), value.b / Mathf.Max(0.01f, reference.b), 1f);
    }
}
