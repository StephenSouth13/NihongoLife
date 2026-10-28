using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Scenery seen through the carriage windows. Each layer scrolls past and wraps around, so a short loop reads as a
    /// long journey. <see cref="Throttle"/> (0..1) eases the train between stopped and cruising.
    /// The view follows the line on the map: every leg has its own theme (Hibari's houses and paddies → the school and
    /// its sports ground → Minato's harbour → the bridge to Midori Island → the airport), swapped inside a short tunnel
    /// so nothing pops; at each stop the platform with the station's name board slides in and stops at the window.
    /// </summary>
    public sealed class TrainWindowScenery : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            public Transform root;
            public float parallax = 1f;
            public float loopWidth = 120f;
        }

        /// <summary>The view of one leg: its scrolling layers and its static ground (a bridge has sea, not fields).</summary>
        [Serializable]
        public sealed class Theme
        {
            public string id;
            public Transform root;
            public Transform statics;
            public List<Layer> layers = new();
        }

        [SerializeField] private List<Layer> layers = new();
        [SerializeField] private List<Theme> themes = new();
        [SerializeField] private Transform platform;
        [SerializeField] private TextMeshPro platformSign;
        [SerializeField] private Transform tunnel;
        [SerializeField] private float cruiseSpeed = 17f;
        [SerializeField] private float acceleration = 5f;

        private float _speed;
        private int _theme = -1;
        private bool _platformShown;

        /// <summary>Target fraction of cruise speed: 0 = standing at a platform, 1 = cruising.</summary>
        public float Throttle { get; set; }
        public float CurrentSpeed => _speed;
        public int ThemeIndex => _theme;
        public string ThemeId => _theme >= 0 && _theme < themes.Count ? themes[_theme].id : null;
        public bool PlatformVisible => platform != null && platform.gameObject.activeSelf;
        public float PlatformX => platform != null ? platform.localPosition.x : 0f;
        public bool InTunnel => tunnel != null && tunnel.gameObject.activeSelf;

        public void Configure(float cruise, params Layer[] sceneryLayers)
        {
            cruiseSpeed = cruise;
            layers = new List<Layer>(sceneryLayers);
        }

        public void ConfigureRoute(List<Theme> legThemes, Transform stopPlatform, TextMeshPro sign, Transform tunnelWalls)
        {
            themes = legThemes;
            platform = stopPlatform;
            platformSign = sign;
            tunnel = tunnelWalls;
        }

        /// <summary>Shows the view of leg <paramref name="leg"/> at once (boarding).</summary>
        public void ShowLeg(int leg)
        {
            if (themes.Count == 0) return;
            _theme = Mathf.Clamp(leg, 0, themes.Count - 1);
            for (int i = 0; i < themes.Count; i++)
            {
                bool on = i == _theme;
                if (themes[i].root != null) themes[i].root.gameObject.SetActive(on);
                if (themes[i].statics != null) themes[i].statics.gameObject.SetActive(on);
            }
        }

        /// <summary>A short tunnel; the next leg's view is waiting on the other side.</summary>
        public void SwapInTunnel(int leg, float seconds = 1.6f)
        {
            if (themes.Count == 0 || leg == _theme) return;
            if (tunnel == null || !isActiveAndEnabled) { ShowLeg(leg); return; }
            StartCoroutine(Tunnel(leg, seconds));
        }

        private IEnumerator Tunnel(int leg, float seconds)
        {
            tunnel.gameObject.SetActive(true);
            yield return new WaitForSeconds(seconds * 0.5f);
            ShowLeg(leg);
            yield return new WaitForSeconds(seconds * 0.5f);
            tunnel.gameObject.SetActive(false);
        }

        /// <summary>Standing at a station: the platform is right outside.</summary>
        public void StandAt(string nameJa, string nameRomaji)
        {
            if (platform == null) return;
            SetSign(nameJa, nameRomaji);
            platform.localPosition = new Vector3(0f, platform.localPosition.y, platform.localPosition.z);
            platform.gameObject.SetActive(true);
            _platformShown = true;
        }

        /// <summary>Braking now: the platform comes in from ahead and stops at the window as the train does.</summary>
        public void ArriveAt(string nameJa, string nameRomaji)
        {
            if (platform == null) return;
            SetSign(nameJa, nameRomaji);
            float stopping = _speed * _speed / (2f * Mathf.Max(0.01f, acceleration));
            platform.localPosition = new Vector3(stopping, platform.localPosition.y, platform.localPosition.z);
            platform.gameObject.SetActive(true);
            _platformShown = true;
        }

        private void SetSign(string nameJa, string nameRomaji)
        {
            if (platformSign != null) platformSign.text = $"{nameJa}\n<size=45%>{nameRomaji}</size>";
        }

        private void Update()
        {
            _speed = Mathf.MoveTowards(_speed, cruiseSpeed * Mathf.Clamp01(Throttle), acceleration * Time.deltaTime);
            if (_speed <= 0.001f) return;
            float step = _speed * Time.deltaTime;
            foreach (Layer layer in layers) Scroll(layer, step);
            if (_theme >= 0 && _theme < themes.Count)
                foreach (Layer layer in themes[_theme].layers) Scroll(layer, step);
            if (_platformShown && platform != null)
            {
                // The platform does not wrap: it passes once and is gone.
                Vector3 p = platform.localPosition;
                p.x = Mathf.Max(p.x - step, -200f);
                platform.localPosition = p;
                if (p.x <= -160f) { platform.gameObject.SetActive(false); _platformShown = false; }
            }
        }

        private static void Scroll(Layer layer, float step)
        {
            if (layer.root == null) return;
            float half = layer.loopWidth * 0.5f;
            float move = step * layer.parallax;
            foreach (Transform item in layer.root)
            {
                Vector3 p = item.localPosition;
                p.x -= move;
                if (p.x < -half) p.x += layer.loopWidth;
                item.localPosition = p;
            }
        }
    }
}
