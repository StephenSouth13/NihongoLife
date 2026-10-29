using NihongoLife.Cameras;
using NihongoLife.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.World
{
    /// <summary>
    /// H5: falling cherry petals in spring and red/orange leaves in autumn over the open-air scenes (city, Midori). One pooled
    /// particle system that follows the camera; none in summer/winter, indoors or in the rain. No assets: small tinted quads.
    /// </summary>
    public sealed class SeasonalLeaves : MonoBehaviour
    {
        public static SeasonalLeaves Instance { get; private set; }
        public static Season? SeasonOverride;
        private ParticleSystem _fx;
        public float Rate { get; private set; }
        public Season CurrentSeason => SeasonOverride ?? (DayNightCycle.Active != null ? DayNightCycle.Active.Season : Season.Summer);
        public int AliveParticles => _fx != null ? _fx.particleCount : 0;
        public Color32 Tint { get; private set; }

        public static float RateFor(Season s, bool indoors, bool rain) => indoors || rain ? 0f : s == Season.Spring ? 9f : s == Season.Autumn ? 12f : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; SeasonOverride = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("SeasonalLeaves");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SeasonalLeaves>();
        }

        private void Build()
        {
            var go = new GameObject("LeafFx");
            go.transform.SetParent(transform, false);
            _fx = go.AddComponent<ParticleSystem>();
            _fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _fx.main;
            main.loop = true; main.startLifetime = 9f; main.startSpeed = 0.2f; main.maxParticles = 220;
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.17f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.gravityModifier = 0.025f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = _fx.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(40f, 0.2f, 40f);
            var vel = _fx.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.9f); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.5f);
            var noise = _fx.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.4f;
            var rot = _fx.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "SeasonLeaf" };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _fx.Play();
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            bool outdoor = AmbientWildlife.SceneHasWildlife(SceneManager.GetActiveScene().name);
            if (cam == null || !outdoor) { if (_fx != null && _fx.gameObject.activeSelf) _fx.gameObject.SetActive(false); Rate = 0f; return; }
            if (_fx == null) Build();
            if (!_fx.gameObject.activeSelf) _fx.gameObject.SetActive(true);
            _fx.transform.position = cam.transform.position + Vector3.up * 8f;
            bool indoors = StoreCameraZone.Contains(cam.transform.position);
            bool rain = WeatherCycle.Instance != null && WeatherCycle.Instance.IsRaining;
            var season = CurrentSeason;
            Rate = RateFor(season, indoors, rain);
            var emission = _fx.emission; emission.rateOverTime = Rate;
            var main = _fx.main;
            Tint = season == Season.Spring ? new Color32(255, 190, 210, 235) : new Color32(214, 96, 36, 235);
            main.startColor = new ParticleSystem.MinMaxGradient(Tint, season == Season.Spring ? new Color32(255, 225, 232, 235) : new Color32(168, 52, 28, 235));
        }
    }
}