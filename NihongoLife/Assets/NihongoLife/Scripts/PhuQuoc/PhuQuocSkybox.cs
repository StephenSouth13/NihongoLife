using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>
    /// The Phú Quốc sky in play: while the island zone is loaded its panoramic sky (PQ_Skybox) replaces the town's
    /// day/night sky, dimmed and tinted by the day/night cycle's daylight (OutdoorSky.Active), with slowly drifting
    /// clouds. The previous sky comes back when the zone unloads.
    /// </summary>
    public sealed class PhuQuocSkybox : MonoBehaviour
    {
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int RotationId = Shader.PropertyToID("_Rotation");
        private static readonly Color NightTint = new Color(0.16f, 0.19f, 0.32f);

        [SerializeField] private Material sky;
        private Material _instance, _previous;
        private Color _dayTint = Color.white;
        private float _dayExposure = 1f;

        public Material Sky { get => sky; set => sky = value; }
        public Material Instance => _instance;

        /// <summary>Exposure for a daylight value (0 night, 1 full day).</summary>
        public static float ExposureFor(float dayExposure, float daylight) => Mathf.Lerp(0.14f, dayExposure, Mathf.Clamp01(daylight));

        private void OnEnable()
        {
            if (sky == null) return;
            _instance = new Material(sky) { name = sky.name + " (play)" };
            if (_instance.HasProperty(TintId)) _dayTint = _instance.GetColor(TintId);
            if (_instance.HasProperty(ExposureId)) _dayExposure = _instance.GetFloat(ExposureId);
            Take();
        }

        private void Take()
        {
            if (RenderSettings.skybox == _instance) return;
            _previous = RenderSettings.skybox;
            RenderSettings.skybox = _instance;
        }

        private void LateUpdate()
        {
            if (_instance == null) return;
            Take(); // the town's OutdoorSky re-applies its sky on scene changes
            var outdoors = OutdoorSky.Active;
            float daylight = outdoors != null ? outdoors.Daylight : 1f;
            _instance.SetFloat(ExposureId, ExposureFor(_dayExposure, daylight));
            _instance.SetColor(TintId, Color.Lerp(NightTint, _dayTint, daylight));
            _instance.SetFloat(RotationId, Mathf.Repeat(Time.time * 0.25f, 360f));
        }

        private void OnDisable()
        {
            if (_instance == null) return;
            if (RenderSettings.skybox == _instance) RenderSettings.skybox = _previous;
            Destroy(_instance);
            _instance = null;
        }
    }
}
