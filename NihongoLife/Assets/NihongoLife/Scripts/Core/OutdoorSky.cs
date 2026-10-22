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
        private static readonly System.Collections.Generic.List<OutdoorSky> Loaded = new();

        /// <summary>Zones load additively over the city: the outdoors of the active scene wins, else the latest loaded.</summary>
        public static OutdoorSky Active
        {
            get
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                for (int i = Loaded.Count - 1; i >= 0; i--)
                    if (Loaded[i] != null && Loaded[i].gameObject.scene == activeScene) return Loaded[i];
                return Loaded.Count > 0 ? Loaded[Loaded.Count - 1] : null;
            }
        }

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
        private static readonly int OvercastId = Shader.PropertyToID("_Overcast");
        private static readonly int RotationId = Shader.PropertyToID("_Rotation");
        private static readonly int RainbowId = Shader.PropertyToID("_Rainbow");
        private static readonly int MoonPhaseId = Shader.PropertyToID("_MoonPhase");
        private float _moonPhase = 0.5f;
        private float _sunrise = 6f;
        private float _sunset = 18f;
        private float _cloudRotation;
        private ParticleSystem _rainFx;
        private AudioSource _rainAudio;

        /// <summary>Rain intensity last applied (0-1).</summary>
        public float Rain { get; private set; }
        public float RainParticlesPerSecond => _rainFx != null && _rainFx.gameObject.activeSelf ? _rainFx.emission.rateOverTime.constant : 0f;

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
            Loaded.Remove(this);
            Loaded.Add(this);
            if (skyMaterial != null && _sky == null) _sky = new Material(skyMaterial) { name = skyMaterial.name + " (day/night)" };
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            Refresh();
        }

        private void OnDisable()
        {
            Loaded.Remove(this);
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            var next = Active;
            if (next != null) next.Refresh();
        }

        private void HandleActiveSceneChanged(UnityEngine.SceneManagement.Scene _, UnityEngine.SceneManagement.Scene __) => Refresh();

        /// <summary>When this is the active outdoors: its sky goes up and the cycle re-applies the current hour.</summary>
        private void Refresh()
        {
            if (Active != this) return;
            if (_sky != null) RenderSettings.skybox = _sky;
            var cycle = SceneRefs.Find<DayNightCycle>();
            if (cycle != null) cycle.SetHour(cycle.Hour);
        }

        private void OnDestroy()
        {
            if (_sky != null) Destroy(_sky);
            if (_rainFx != null) Destroy(_rainFx.gameObject);
        }

        /// <summary>Clouds drift (faster under a heavy sky); rain follows the camera in outdoor scenes.</summary>
        private void Update()
        {
            if (Active != this) { if (_rainFx != null && _rainFx.gameObject.activeSelf) _rainFx.gameObject.SetActive(false); return; }
            float overcast = WeatherCycle.Instance != null ? WeatherCycle.Instance.Overcast : 0f;
            if (_sky != null)
            {
                _cloudRotation = Mathf.Repeat(_cloudRotation + (0.35f + 1.2f * overcast) * Time.deltaTime, 360f);
                _sky.SetFloat(RotationId, _cloudRotation);
            }
            if (!drivesAmbientAndFog) return;
            var cam = Camera.main;
            if (Rain <= 0.01f || cam == null)
            {
                if (_rainFx != null && _rainFx.gameObject.activeSelf) _rainFx.gameObject.SetActive(false);
                return;
            }
            if (_rainFx == null) BuildRain();
            if (!_rainFx.gameObject.activeSelf) _rainFx.gameObject.SetActive(true);
            _rainFx.transform.position = cam.transform.position + Vector3.up * 9f;
            bool inside = NihongoLife.Cameras.StoreCameraZone.Contains(cam.transform.position);
            var emission = _rainFx.emission;
            emission.rateOverTime = inside ? 0f : 1600f * Rain;
            float swell = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 0.23f, 0.5f);
            _rainAudio.volume = Rain * (inside ? 0.035f : 0.11f) * swell;
        }

        private void BuildRain()
        {
            var go = new GameObject("RainFx");
            go.transform.SetParent(transform, false);
            _rainFx = go.AddComponent<ParticleSystem>();
            _rainFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _rainFx.main;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = 15f;
            main.startSize = 0.03f;
            main.startColor = new Color(0.78f, 0.84f, 0.95f, 0.5f);
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = _rainFx.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 34f, 0.1f);
            shape.rotation = new Vector3(90f, 0f, 0f); // box faces down: drops fall
            var collision = _rainFx.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.lifetimeLoss = 1f; // drops stop at roofs and awnings
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 6f;
            renderer.velocityScale = 0.02f;
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "RainDrop" };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _rainFx.Play();

            // Rain hiss: two seconds of softened noise, looped (no audio asset needed).
            _rainAudio = go.AddComponent<AudioSource>();
            const int rate = 22050;
            var data = new float[rate * 2];
            var random = new System.Random(7);
            float low = 0f, lower = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                // Two low-pass stages: the patter of drops without the harsh white-noise hiss.
                low += 0.12f * (white - low);
                lower += 0.05f * (low - lower);
                data[i] = (low * 0.55f + lower * 0.9f) * 0.9f;
            }
            for (int i = 0; i < 2000; i++) { float t = i / 2000f; data[i] *= t; data[data.Length - 1 - i] *= t; }
            var clip = AudioClip.Create("RainNoise", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            _rainAudio.clip = clip;
            _rainAudio.loop = true;
            _rainAudio.spatialBlend = 0f;
            _rainAudio.volume = 0f;
            _rainAudio.Play();
        }

        /// <summary>Season, day length and moon phase for the next <see cref="Apply"/>.</summary>
        public void SetCalendarSky(float moonPhase, float sunrise, float sunset, Color seasonTint)
        {
            _moonPhase = Mathf.Repeat(moonPhase, 1f);
            _sunrise = sunrise;
            _sunset = Mathf.Max(sunrise + 1f, sunset);
        }

        /// <summary>Lights this scene for <paramref name="hour"/>. <paramref name="key"/> is the cycle's colour key for
        /// that hour and <paramref name="noon"/> its daytime key: ambient and fog keep the scene's own daytime look and
        /// follow the key's change relative to noon.</summary>
        public void Apply(float hour, DayNightTimelineKey key, DayNightTimelineKey noon)
        {
            // Sun above the horizon from 6:00 to 18:00; at night the same light becomes the moon (never lights from below).
            var weather = WeatherCycle.Instance;
            float overcast = weather != null ? weather.Overcast : 0f;
            float rain = weather != null ? weather.Rain : 0f;
            Rain = rain;
            // The clock hour is stretched onto an equinox day so summer sunsets stay late and winter ones early.
            float sunHour = GameCalendar.EquinoxHour(hour, _sunrise, _sunset);
            float angle = sunHour / 24f * 360f - 90f;
            float elevation = Mathf.Sin(angle * Mathf.Deg2Rad);
            Daylight = Mathf.Clamp01(Mathf.InverseLerp(0.14f, noon.sunIntensity, key.sunIntensity));
            if (sun != null)
            {
                float lightAngle = Mathf.Repeat(elevation >= 0f ? angle : angle - 180f, 360f);
                sun.transform.rotation = Quaternion.Euler(Mathf.Clamp(lightAngle, 6f, 174f), sunYaw, 0f);
                float horizonDip = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(Mathf.Abs(elevation) / 0.2f));
                sun.color = key.sunColor;
                sun.intensity = key.sunIntensity / Mathf.Max(0.01f, noon.sunIntensity) * sunNoonIntensity * horizonDip * (1f - 0.65f * overcast);
                sun.shadowStrength = Mathf.Lerp(0.35f, 0.85f, Daylight) * (1f - 0.6f * overcast);
                RenderSettings.sun = sun;
            }

            if (drivesAmbientAndFog)
            {
                // Overcast/rain: a dimmer, greyer light and thicker fog.
                Color ratio = Ratio(key.ambientColor, noon.ambientColor) * Mathf.Lerp(1f, 0.78f, overcast);
                RenderSettings.ambientMode = ambientMode;
                if (ambientMode == AmbientMode.Flat) RenderSettings.ambientLight = ambientSky * ratio;
                else
                {
                    RenderSettings.ambientSkyColor = ambientSky * ratio;
                    RenderSettings.ambientEquatorColor = ambientEquator * ratio;
                    RenderSettings.ambientGroundColor = ambientGround * ratio;
                }
                RenderSettings.fog = true;
                Color fog = fogColor * Ratio(key.fogColor, noon.fogColor);
                float grey = fog.grayscale;
                RenderSettings.fogColor = Color.Lerp(fog, new Color(grey, grey * 1.02f, grey * 1.06f) * 0.85f, 0.6f * overcast);
                RenderSettings.fogDensity = fogDensity * key.fogDensity / Mathf.Max(0.0001f, noon.fogDensity) * (1f + 1.6f * overcast + 1.8f * rain);
            }

            if (_sky != null)
            {
                float glow = Mathf.Clamp01(1f - Mathf.Abs(elevation) / 0.32f);
                _sky.SetFloat(NightId, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.08f, Daylight)));
                _sky.SetColor(TintId, Color.Lerp(Color.white, key.sunColor, 0.18f * glow));
                _sky.SetColor(HorizonColorId, key.sunColor);
                _sky.SetFloat(HorizonGlowId, 0.75f * glow);
                _sky.SetFloat(ExposureId, Mathf.Lerp(0.9f, 1.05f, Daylight) * Mathf.Lerp(1f, 0.85f, overcast));
                _sky.SetFloat(OvercastId, overcast);
                _sky.SetFloat(RainbowId, weather != null ? weather.Rainbow * Mathf.Clamp01(Daylight * 1.4f) * (1f - 0.7f * overcast) : 0f);
                Vector3 toSun = sun != null ? -(Quaternion.Euler(angle, sunYaw, 0f) * Vector3.forward) : Vector3.up;
                bool moon = elevation < 0f;
                _sky.SetVector(SunDirId, moon ? -toSun : toSun);
                _sky.SetColor(SunColorId, moon ? new Color(0.75f, 0.82f, 1f) * 0.9f : Color.Lerp(key.sunColor, Color.white, 0.4f) * 1.6f);
                _sky.SetFloat(SunSizeId, moon ? 0.99935f : 0.9988f);
                _sky.SetFloat(MoonPhaseId, moon ? _moonPhase : 0.5f);
            }

            // Street lamps come on at dusk and go out after dawn.
            float lamp = Mathf.Clamp01((0.5f - Daylight * (1f - 0.55f * overcast)) / 0.3f); // earlier under a rainy sky
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
