using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// Scenery seen through the carriage windows. Each layer scrolls past at its own parallax factor
    /// (catenary poles fast, houses slower, hills barely) and wraps around, so a short loop reads as a
    /// long journey. <see cref="Throttle"/> (0..1) eases the train between stopped and cruising.
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

        [SerializeField] private List<Layer> layers = new();
        [SerializeField] private float cruiseSpeed = 17f;
        [SerializeField] private float acceleration = 5f;

        private float _speed;

        /// <summary>Target fraction of cruise speed: 0 = standing at a platform, 1 = cruising.</summary>
        public float Throttle { get; set; }
        public float CurrentSpeed => _speed;

        public void Configure(float cruise, params Layer[] sceneryLayers)
        {
            cruiseSpeed = cruise;
            layers = new List<Layer>(sceneryLayers);
        }

        private void Update()
        {
            _speed = Mathf.MoveTowards(_speed, cruiseSpeed * Mathf.Clamp01(Throttle), acceleration * Time.deltaTime);
            if (_speed <= 0.001f) return;
            foreach (Layer layer in layers)
            {
                if (layer.root == null) continue;
                float half = layer.loopWidth * 0.5f;
                float step = _speed * layer.parallax * Time.deltaTime;
                foreach (Transform item in layer.root)
                {
                    Vector3 p = item.localPosition;
                    p.x -= step;
                    if (p.x < -half) p.x += layer.loopWidth;
                    item.localPosition = p;
                }
            }
        }
    }
}
