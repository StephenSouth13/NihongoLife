using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>A filled polygon in the UI (a coastline on the map), ear-clipped once when the points change.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiPolygon : MaskableGraphic
    {
        private readonly List<Vector2> _points = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();

        /// <summary>Points in this rect's local space (pivot-relative), in order around the shape.</summary>
        public void SetPoints(IList<Vector2> points)
        {
            _points.Clear();
            _points.AddRange(points);
            Triangulate();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_triangles.Count < 3) return;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            foreach (var p in _points) { vertex.position = p; vh.AddVert(vertex); }
            for (int i = 0; i + 2 < _triangles.Count; i += 3) vh.AddTriangle(_triangles[i], _triangles[i + 1], _triangles[i + 2]);
        }

        private void Triangulate()
        {
            _triangles.Clear();
            int n = _points.Count;
            if (n < 3) return;
            float area = 0f;
            for (int i = 0, j = n - 1; i < n; j = i++) area += _points[j].x * _points[i].y - _points[i].x * _points[j].y;
            var index = new List<int>(n);
            for (int i = 0; i < n; i++) index.Add(area > 0f ? i : n - 1 - i); // counter-clockwise
            int guard = n * n;
            while (index.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < index.Count; i++)
                {
                    int a = index[(i + index.Count - 1) % index.Count], b = index[i], c = index[(i + 1) % index.Count];
                    Vector2 pa = _points[a], pb = _points[b], pc = _points[c];
                    if ((pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x) <= 0f) continue; // reflex corner
                    bool inside = false;
                    foreach (int k in index)
                    {
                        if (k == a || k == b || k == c) continue;
                        if (InTriangle(_points[k], pa, pb, pc)) { inside = true; break; }
                    }
                    if (inside) continue;
                    _triangles.Add(a); _triangles.Add(b); _triangles.Add(c);
                    index.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            if (index.Count == 3) { _triangles.Add(index[0]); _triangles.Add(index[1]); _triangles.Add(index[2]); }
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
    }
}
