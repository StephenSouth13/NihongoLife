using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    [Serializable]
    public sealed class PhuQuocPlace
    {
        public string id, category, nameVi, nameEn, nameJa, source, note;
        public double lat, lon;
        public string Name => Loc.Pick(nameVi, nameEn, nameJa);
    }

    [Serializable]
    public sealed class LatLon { public double lat, lon; }

    /// <summary>
    /// The real island at game scale (Resources/PhuQuoc/phu_quoc_map.json): places with their real WGS84
    /// coordinates (OpenStreetMap) and the coastline, projected around Gánh Ngon at 1 unit = 100 m
    /// (a literal 1:1 island of 575 km² is not playable). PhuQuocIslandBuilder builds the scene from it.
    /// </summary>
    [Serializable]
    public sealed class PhuQuocMap
    {
        public const string ResourcePath = "PhuQuoc/phu_quoc_map";
        public const double MetersPerDegreeLat = 110574.0;
        public const double MetersPerDegreeLonAtEquator = 111320.0;
        public static readonly string[] Categories = { "restaurant", "town", "market", "landmark", "factory", "farm", "transport", "beach", "nature", "village", "history", "entertainment", "island" };

        public string schema, note;
        public double originLat, originLon;
        public float metersPerUnit = 100f, verticalMetersPerUnit = 50f;
        public PhuQuocPlace[] places = Array.Empty<PhuQuocPlace>();
        public LatLon[] outline = Array.Empty<LatLon>();

        private static PhuQuocMap _cached;
        public static PhuQuocMap Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>(ResourcePath);
            _cached = text != null ? Parse(text.text) : new PhuQuocMap();
            return _cached;
        }
        public static PhuQuocMap Parse(string json) => JsonUtility.FromJson<PhuQuocMap>(json) ?? new PhuQuocMap();

        public PhuQuocPlace Place(string id) => places.FirstOrDefault(p => p.id == id);

        /// <summary>Real metres east (x) / north (y) of the origin.</summary>
        public Vector2 ToMeters(double lat, double lon)
        {
            double cos = Math.Cos(originLat * Math.PI / 180.0);
            return new Vector2((float)((lon - originLon) * MetersPerDegreeLonAtEquator * cos), (float)((lat - originLat) * MetersPerDegreeLat));
        }

        /// <summary>Game position on the ground (x east, z north) at the map scale.</summary>
        public Vector3 ToWorld(double lat, double lon)
        {
            var m = ToMeters(lat, lon) / metersPerUnit;
            return new Vector3(m.x, 0f, m.y);
        }

        public Vector3 ToWorld(PhuQuocPlace p) => ToWorld(p.lat, p.lon);

        public Vector2[] OutlineWorld() => outline.Select(o => { var w = ToWorld(o.lat, o.lon); return new Vector2(w.x, w.z); }).ToArray();

        public bool OnIsland(double lat, double lon)
        {
            var w = ToWorld(lat, lon);
            return PointInPolygon(new Vector2(w.x, w.z), OutlineWorld());
        }

        public static bool PointInPolygon(Vector2 p, IList<Vector2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (schema != "nihongolife.phuquoc.map.v1") e.Add("schema must be nihongolife.phuquoc.map.v1");
            if (metersPerUnit <= 0) e.Add("metersPerUnit must be positive");
            if (outline.Length < 8) e.Add("outline needs at least 8 points");
            var ids = new HashSet<string>();
            foreach (var p in places)
            {
                if (string.IsNullOrEmpty(p.id) || !ids.Add(p.id)) e.Add("place id missing or duplicate: " + p.id);
                if (!Categories.Contains(p.category)) e.Add($"place {p.id}: unknown category {p.category}");
                if (p.lat < 9.9 || p.lat > 10.5 || p.lon < 103.8 || p.lon > 104.1) e.Add($"place {p.id}: outside Phú Quốc");
                if (new[] { p.nameVi, p.nameEn, p.nameJa, p.source }.Any(string.IsNullOrWhiteSpace)) e.Add($"place {p.id}: needs vi/en/ja names and a source");
                if (p.category != "island" && !OnIsland(p.lat, p.lon)) e.Add($"place {p.id}: not on the island outline");
            }
            return e;
        }
    }
}
