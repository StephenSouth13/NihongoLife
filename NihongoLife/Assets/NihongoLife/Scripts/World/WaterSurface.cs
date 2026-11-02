using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.World
{
    /// <summary>
    /// A body of water the player can swim in, on the object that draws it. Water has no collider, so without this a step off
    /// the shore fell through the sea. Its size and height come from the renderer.
    /// </summary>
    public sealed class WaterSurface : MonoBehaviour
    {
        private static readonly List<WaterSurface> Active = new List<WaterSurface>();

        private Bounds _bounds;

        public float Y => _bounds.max.y;

        private void OnEnable()
        {
            var shape = GetComponent<Renderer>();
            if (shape == null) return;
            _bounds = shape.bounds;
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        /// <summary>The smallest water around a point, so a pool wins over the sea behind it. Null on dry land far from any.</summary>
        public static WaterSurface At(Vector3 position)
        {
            WaterSurface best = null;
            float bestArea = float.MaxValue;
            for (int i = 0; i < Active.Count; i++)
            {
                var water = Active[i];
                var box = water._bounds;
                if (position.x < box.min.x || position.x > box.max.x || position.z < box.min.z || position.z > box.max.z) continue;
                if (position.y > box.max.y + 2f || position.y < box.max.y - 8f) continue;
                float area = box.size.x * box.size.z;
                if (area < bestArea) { best = water; bestArea = area; }
            }
            return best;
        }
    }
}
